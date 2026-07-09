# Personal con 1 a N regímenes laborales simultáneos — Mapeo técnico

## Estado: Mapeo + implementación parcial. Última actualización 2026-07-06.

Este documento nació como mapeo de solo lectura (vigente al 2026-07-03). Desde entonces se
implementó y desplegó en producción una propuesta por fases para resolver la tarifa de nómina y
separar los saldos/registros por régimen — ver la sección **"6. Implementación (2026-07-06)"**
más abajo para el detalle completo de lo hecho, y **"7. Pendiente"** para lo que falta.

## Resumen ejecutivo

El **modelo de datos ya está preparado** para que una persona tenga varios regímenes laborales
activos a la vez (ej. nombramiento LOSEP + contrato ocasional LOES como docente) — la tabla
`HR.tbl_EmployeeLaborRegime` fue diseñada explícitamente para esto. **Pero casi todo el resto del
sistema (horario, asistencia, nómina, vacaciones, permisos, horas extra) todavía asume UN SOLO
régimen por persona**, leyendo siempre el régimen "principal" e ignorando por completo cualquier
régimen secundario. Es una arquitectura a medio migrar: el dato existe, la lógica de negocio que
lo usaría todavía no.

**Estado en producción:** sin casos reales confirmados a la fecha (2026-07-01). Es un riesgo
latente, no un incidente activo — pero el día que aparezca un caso real, varios cálculos saldrán
mal sin que el sistema lo señale como error (ver sección "Impacto concreto").

---

## 1. Qué es un "régimen laboral" en el sistema

`HR.tbl_EmployeeLaborRegime` (`Database/hr/01_tables.sql:2114-2157`, constraints en
`02_constraints.sql:2743-2783`) es la fuente de verdad. El comentario del propio script lo dice
sin ambigüedad:

> *"Un empleado puede tener varias filas activas simultáneas (ej. nombramiento LOSEP en Dirección
> Administrativa + contrato LOES ocasional como docente en otra facultad)"*

- `LaborRegimeId` → catálogo `ref_Types` (Category='CONTRACT_TYPE'): `57=LOSEP`, `58=LOES`,
  `59=Código de Trabajo` (`Models/EmployeeLaborRegime.cs:18`).
- Único índice único: `IX_EmployeeLaborRegime_Employee_Regime_Active`
  (`02_constraints.sql:2779-2782`) — `UNIQUE (EmployeeId, LaborRegimeId) WHERE IsActive=1`. Esto
  **solo impide duplicar el mismo régimen dos veces activo**; NO impide tener LOSEP + LOES activos
  al mismo tiempo (son valores de `LaborRegimeId` distintos). **El esquema permite multi-régimen
  hoy mismo, sin ninguna restricción técnica que lo bloquee.**
- `IsPrincipal` (`EmployeeLaborRegime.cs:50-55`): lo calcula `IEmployeeLaborRegimeService`. Gana
  el régimen con nombramiento (`IsIndefinite`); si ninguno es nombramiento, gana LOSEP. Solo un
  régimen activo puede tener `IsPrincipal=true` en un momento dado.
- **El dato crítico está en el propio comentario del script** (`01_tables.sql:2118-2120`):
  > *"Reemplaza a HR.tbl_Employees.EmployeeType como fuente de verdad; ese campo se mantiene como
  > espejo del régimen IsPrincipal para no romper a los consumidores existentes."*

  Es decir: **todo lo que todavía lee `ContractType`/`EmployeeType`** (vía
  `vw_EmployeeDetails.ContractType`, que es lo que usa `sp_ProcessAttendanceBaseDay` y
  `sp_ProcessAttendanceFinalizeDay`) **solo ve el régimen PRINCIPAL**. El régimen secundario es
  invisible para esa lógica, aunque la tabla que sí lo modela ya exista y esté poblada.

`ContractType` (`Models/ContractType.cs`) es un concepto **distinto**, no confundir por el
nombre parecido: es el catálogo de *tipos de documento de contrato* (plantilla, prefijo de
numeración, flags de aprovisionamiento AD). No tiene vínculo directo a LOES/LOSEP/CT; ese vínculo
solo existe vía `EmployeeLaborRegime` (a través de `SourceContractId`/`SourcePersonnelActionId`).

## 2. Contratos múltiples simultáneos

`HR.tbl_Contracts` **no tiene ninguna restricción que impida 2+ contratos `Status=VIGENTE`
simultáneos** para la misma persona bajo distinto `ContractTypeID` — no hay índice único sobre
`PersonID+Status`.

`ContractExpirationService.ProcessExpiredContractsAsync`
(`Application/Services/ContractExpirationService.cs:113-115`) ya asume esto explícitamente:

```csharp
var hasOtherActiveRegime = await _db.Set<EmployeeLaborRegime>()
    .AsNoTracking()
    .AnyAsync(r => r.EmployeeId == employeeId && r.IsActive, ct);
```

Cuando un contrato vence: primero cierra el `EmployeeLaborRegime` de ESE contrato
(`CloseLaborRegimeForContractAsync`), y **solo si no queda ningún otro régimen activo** deshabilita
la cuenta AD (`ContractExpirationService.cs:117-135`).

**Este es, hoy, el único punto del sistema que maneja multi-régimen correctamente** — no le corta
el acceso a alguien que sigue activo bajo su otro régimen aunque uno de sus contratos haya vencido.

## 3. Dónde el sistema ya asume "un solo régimen" — mapa completo

| Área | Archivo:línea | Qué asume hoy |
|---|---|---|
| Horario diario | `sp_ProcessAttendanceBaseDay` (`06_procedures.sql:~3462-3475`) | `TOP 1 ... ORDER BY es.ValidFrom DESC` sobre `tbl_EmployeeSchedules` — un solo horario por `EmployeeID+WorkDate`. `tbl_EmployeeSchedules` no tiene columna de régimen para distinguir "horario LOSEP" de "horario LOES". |
| Consolidado diario | `HR.tbl_AttendanceCalculations` | El `MERGE` de `sp_ProcessAttendanceBaseDay` hace match por `EmployeeID+WorkDate` — una sola fila por empleado y día, sin partición por régimen. |
| Subsidio/descuento | `sp_ProcessAttendanceBaseDay` / `sp_ProcessAttendanceFinalizeDay` (`06_procedures.sql:~3846`, `~4593`) | `IF (@ContractType = N'Código Trabajo' ...)` recibe **un solo** `@ContractType`, tomado de `vw_EmployeeDetails.ContractType` (el espejo del régimen `IsPrincipal`). Un régimen secundario nunca se evalúa. |
| Nómina | `HR.tbl_Payroll` (`01_tables.sql:1380-1391`) | Sin `ContractID` ni `LaborRegimeId` en la tabla. Un `BaseSalary` único por `EmployeeID+Period`, sin flujo que genere/reconcilie una fila de nómina por régimen. |
| Vacaciones | `HR.tbl_TimeBalances` (`01_tables.sql:1874-1880`) | PK sobre `EmployeeID` solamente — un único saldo de vacaciones por persona, aunque LOES y LOSEP puedan tener reglas de acumulación distintas. |
| Permisos | `HR.tbl_Permissions` (`01_tables.sql:1446-1465`) | Sin columna de régimen/contrato — una solicitud no puede indicar a cuál régimen aplica. |
| Horas extra | `HR.tbl_OvertimeConfig` / `HR.tbl_Overtime` | Ninguna tiene `LaborRegimeId`. El factor de pago depende solo de `OvertimeType`, no del régimen bajo el cual se generó. |

## 4. Caso LOES (docentes) — corrección importante

`Database/ATTENDANCE_PIPELINE.md` decía que la distribución de jornada por franjas/materia estaba
"pendiente de diseño detallado", dando a entender que existe algo parcial. **Verificado con más
precisión: no existe nada, ni siquiera parcialmente.**

Se revisaron `Activity`, `JobActivity` y `AdditionalActivity` completas — son catálogos delgados:

- `Activity`: `ActivitiesId, Description, ActivitiesType, IsActive` — sin hora, sin día, sin duración.
- `JobActivity`: `ActivitiesId, JobID, IsActive` — solo vincula un `Job` a una actividad genérica.
- `AdditionalActivity`: `ActivitiesId, ContractId, IsActive` — solo vincula un `Contract` a una actividad genérica.

Ninguna tiene `StartTime`/`EndTime`/`DayOfWeek`/`ScheduleId`. Ninguna de las tres se referencia en
`06_procedures.sql` — el pipeline de asistencia no las toca en absoluto. Son un catálogo para
reportes/documentos, no un mecanismo de horario distribuido.

**Conclusión:** un docente LOES con carga horaria repartida en varias materias en distintos
horarios **no tiene ningún soporte de datos hoy**. El sistema le intentaría resolver un único
`tbl_EmployeeSchedules`, igual que a cualquier empleado de oficina.

## 5. Impacto concreto — escenario real

Ejemplo: Juan Pérez, nombramiento LOSEP en Dirección Financiera (jornada fija 08:00-17:00) +
contrato LOES ocasional como docente 2 tardes/semana en otra facultad (horario distinto).

- **Marcación/horario:** solo se resuelve el horario LOSEP (el que quede como `TOP 1` más
  reciente). Si marca en su horario de docencia, esas marcaciones se evalúan contra el horario
  LOSEP incorrecto → atrasos falsos, horas extra mal calculadas, o ausencias falsas.
- **Reportes de atrasos/horas extra:** una sola fila mezclada por día — Dirección Financiera vería
  atrasos que en realidad son de su rol docente, y Docencia no vería reflejadas sus horas de clase.
- **Nómina:** `tbl_Payroll` no puede representar "sueldo LOSEP" + "honorarios LOES" por separado
  para el mismo período — requeriría carga manual ad-hoc, sin garantía de que descuentos/subsidios
  se apliquen coherentemente sobre ese monto combinado.
- **Vacaciones:** un solo saldo mezclado, aunque cada régimen pudiera tener reglas de acumulación
  distintas (días/año diferentes).
- **Subsidio de alimentación:** se evalúa contra el régimen principal — si el principal es LOSEP,
  las horas trabajadas bajo LOES no activan el subsidio aunque debieran.
- **Horas extra:** mismo factor de pago sin distinguir régimen, aunque la normativa de cada uno
  pudiera exigir tarifas distintas.
- **Lo único que funciona bien hoy:** el aprovisionamiento de cuenta AD (`ContractExpirationService`)
  no deshabilita el acceso mientras quede otro régimen activo.

---

## 6. Implementación (2026-07-06)

Confirmado con datos reales: los empleados 1 y 4 (EmployeeID) son casos reales activos de
multi-régimen (LOSEP + LOES simultáneo), no un caso hipotético. Se implementó por fases:

### Fase 0 — Evitar duplicación de líneas de nómina al reprocesar
`sp_Overtime_Price`, `sp_Payroll_Discounts`, `sp_Payroll_Subsidies` usaban `MERGE ... ON 1=0`
(nunca hacía match → cada reproceso del mismo período insertaba líneas duplicadas). Se cambió a
llave real `(PayrollID, LineType, Concept)`, respaldada por el índice único
`UQ_PayrollLines_Payroll_Line_Concept` en `tbl_PayrollLines`.

### Fase 1 — Un adendum anula a su contrato padre
`Application/Services/ContractsService.cs` — nuevo método `AnnulParentContractAsync`, llamado
desde `UploadSignedDocumentAsync` (cuando el adendum se firma/carga) y desde `ChangeStatusAsync`
(respaldo si el estado se cambia manualmente). No pisa un estado terminal más específico que el
padre ya tuviera (`FINALIZADO`, `VENCIDO`, `RENUNCIA`), y es no-op si ya está anulado. El frontend
(`ContractDetail.tsx`) ya muestra esto sin cambios, porque renderiza el historial de estados con su
comentario.

### Fase 2 — Motor de tarifa con 2 rutas independientes
`HR.fn_ResolveEmployeeRate(@AsOfDate, @EmployeeID = NULL)` (nueva función, tabla en línea):
- **Ruta CONTRATO**: contrato de `tbl_Contracts` vigente en `@AsOfDate` (no `GETDATE()` como antes).
  Si hay contratos superpuestos del mismo régimen, prioriza el no-`ANULADO` y, en empate, el
  `ContractID` más reciente (antes era un `TOP 1 ORDER BY StartDate DESC` no determinístico).
- **Ruta NOMBRAMIENTO**: `tbl_PersonnelActions` con `Status='FIRMADO_CARGADO'` y
  `EffectiveDate <= @AsOfDate`, usando `NewRmu`. Antes esta fuente nunca se consultaba — un cambio
  de sueldo por acción de personal sin contrato nuevo era invisible para nómina.
- Devuelve todas las filas candidatas (una por régimen), sin colapsar, con columna `IsPrincipal`.
- `@EmployeeID` opcional (2026-07-06, tarde): acota el cálculo a un solo empleado en vez de
  recorrer todos — los 3 SP de nómina siguen llamándola sin filtro (`DEFAULT`, necesitan todos los
  empleados del período).
- `BASE_HOURS_PER_DAY` nunca existía en `tbl_Parameters` (causaba `HourRate` NULL en silencio) —
  confirmado y sembrado en 8 por el usuario.

### Fase 3 — Etiquetado por régimen + separación de saldos
- `tbl_Overtime.LaborRegimeId` y `tbl_PayrollLines.LaborRegimeId` (columnas nuevas, nullable).
  Confirmado con el usuario: **solo LOSEP (57) genera horas extra** — `sp_Overtime_Price` filtra
  `fn_ResolveEmployeeRate` por `LaborRegimeID=57` explícitamente (no por `IsPrincipal`), y
  `sp_ProcessTimePlanningForEmployeeDay` (único punto activo que escribe `tbl_Overtime` —
  `sp_Overtime_Calculate` está `[Obsolete]` en C#) etiqueta cada fila con `57` siempre.
  `sp_Payroll_Discounts`/`sp_Payroll_Subsidies` siguen colapsando por `IsPrincipal` (no separados
  por régimen todavía, `tbl_PayrollLines` no distingue esos conceptos por línea).
- `tbl_TimeBalances`: PK cambiada de `(EmployeeID)` a `(EmployeeID, LaborRegimeId)`. Backfill
  ejecutado (674→676 filas): saldo existente → régimen `IsPrincipal` (o `EmployeeType` como espejo
  para 6 empleados inactivos sin fila en `tbl_EmployeeLaborRegime`, o `57` como último respaldo para
  1 fila huérfana); régimen secundario arranca en 0 (no se inventa historia).
  `sp_hr_EnsureTimeBalanceRow` ahora asegura una fila por cada régimen activo.
  `sp_hr_GetEmployeeBalances` devuelve una fila por régimen (con nombre LOSEP/LOES), no una
  mezclada.
- Las 7 procedimientos transaccionales existentes (`sp_hr_AccrueVacationBalance` [3 modos],
  `sp_hr_ConsumeReservation`, `sp_hr_DebitRecoveryBalance`, `sp_hr_ProcessRecoveryBalance`,
  `sp_hr_ReleaseReservation`, `sp_hr_ReservePermissionBalance`, `sp_hr_ReserveVacationBalance`) se
  acotaron explícitamente a `LaborRegimeId=57` — confirmado con el usuario que son mecanismos de
  LOSEP. Esto evita el problema de que el esquema de anti-duplicación (`SourceID` tipo
  `'VAC_TOTAL|fecha'`) no distinguía régimen.
- **Nuevos procedimientos de LOES** (regla confirmada: la diferencia no es solo el número de días,
  sino la *jornada* sobre la que se convierte el derecho — LOSEP usa la jornada administrativa fija
  (`WORK_MINUTES_PER_DAY`=480), LOES usa la dedicación académica del contrato vigente
  (`tbl_Contracts.ContractedHours`, ej. 40h/semana)):
  - `HR.sp_hr_AccrueVacationBalance_LOES` (solo modo MONTHLY implementado) — reutiliza el mismo
    `VACATION_PER_YEAR=30` global (no se confirmó un número distinto para LOES), pero calcula
    `MinutesPerDayLOES = (ContractedHours/5.0)*60` en vez del día administrativo fijo. Probado con
    el empleado 4 real (contrato 40h/semana): +1222 min para agosto 2026, verificado
    matemáticamente. Escribe siempre en `LaborRegimeId=58`.
  - `HR.sp_hr_ReserveVacationBalance_LOES` — espejo de la versión LOSEP, descuenta contra el saldo
    58.

### Fase 4 — Reporte consolidado
`HR.sp_GetConsolidatedRemunerationReport(@Period, @EmployeeID = NULL)` — reporte de solo lectura
sobre `tbl_PayrollLines` ya calculado (no recalcula nada). Dos resultsets: detalle por
régimen/línea, y total consolidado por empleado (`Overtime`/`Subsidy` suman, `Deduction` resta).

## 7. Pendiente

- **Modo DAILY de LOES** — no se construyó (no se pidió); solo existen MONTHLY y TOTAL.
- **`sp_hr_ReservePermissionBalance_LOES`** — equivalente LOES de la reserva de permiso contra
  vacaciones, no construido todavía (solo se hizo el de vacación completa).
- **Horario por régimen** (LOES por franjas/materia) — sigue siendo un problema de modelado aparte
  y más grande, no tocado.
- **`tbl_TimePlanning.RequiresApproval`/`ApprovedBy`/`SecondApprover`** — flujo de aprobación de
  planes de horas extra/recuperación, sigue inalcanzable desde la API (endpoints comentados),
  pendiente de decisión de producto.

## 8. Resuelto después de la sección 7 (2026-07-06, tarde)

- **`sp_hr_AccrueVacationBalance_LOES`** ahora acepta `@Mode` ('MONTHLY' | 'TOTAL'), mismo patrón
  que la versión LOSEP. Probado con el empleado 4 real: TOTAL calculó 14942 min teóricos desde su
  `HireDate`, restó los 1222 ya acreditados por MONTHLY, dio +13720 de delta — verificado
  matemáticamente, sin doble conteo.
- **Bug de guardias en fechas futuras — corregido.** `HR.sp_ProcessGuardAttendanceDate` ahora solo
  actualiza `tbl_GuardShiftPlanning.StatusTypeId` (COMPLETED/ABSENT) si `@WorkDate <= GETDATE()`.
  Antes, reprocesar una fecha futura sin marcaciones marcaba `ABSENT` a un turno que todavía no
  había ocurrido (la causa raíz del incidente de la prueba controlada de `PlanEmployeeID`, donde 50
  guardias reales quedaron `ABSENT` por error). Verificado con un guardia real
  (`PlanningId=3802`, 2026-07-07): tras reprocesar, el turno queda `PLANNED` (304) en vez de
  `ABSENT`, y el resto del pipeline (cálculo de asistencia) sigue funcionando normal.

## 9. Bug de contratos VIGENTE — corregido (2026-07-06, tarde-noche)

Encontrado al revisar por qué el reporte "Contratos Vigentes" siempre devolvía 0 resultados
(sección 7 original). `Application/Services/ContractsService.cs` —
`UploadSignedDocumentAsync`: la transición `FIRMADO_CARGADO → VIGENTE` solo se disparaba **si el
aprovisionamiento de cuenta AD tenía éxito**, y solo para contratos raíz (`ParentID IS NULL`). Un
contrato de un tipo que no requiere AD (`RequiresAdUserCreation=false`), o cuyo aprovisionamiento
fallaba, se quedaba en `FIRMADO_CARGADO` para siempre — confirmado: 0 contratos en `VIGENTE` en
producción antes de este fix.

**Corregido:** ahora `FIRMADO_CARGADO` + documento cargado siempre pasa a `VIGENTE`, para
contratos raíz y adendums por igual. El aprovisionamiento AD sigue disparándose cuando aplica,
pero como efecto secundario independiente, no como condición para estar vigente. Confirmado en el
catálogo (`tbl_contract_status_transitions`) que `FIRMADO_CARGADO → VIGENTE` ya estaba permitido
como transición — el problema era que el código nunca la ejecutaba en el camino sin AD.

**Corrección retroactiva ejecutada:** los 4 contratos que ya estaban firmados y con documento
cargado (`ContractID` 14, 16, 18, 19) pasaron a `VIGENTE`, con entrada en
`tbl_contract_status_history` explicando el motivo. El reporte "Contratos Vigentes" ahora devuelve
4 resultados en vez de 0.

**Acciones de Personal — revisadas, sin el mismo bug.** `PersonnelActionService.cs` tiene un flujo
distinto: para acciones de tipo ingreso/baja (`RequiresAdUserCreation`/`RequiresAdUserDisable`), el
frontend (`UploadSignedDocumentDialog.tsx`) llama a `Finalizar` **antes** de intentar el
aprovisionamiento AD, no después — así que nunca queda condicionado a que el AD tenga éxito. Para
el resto de tipos de acción, `FIRMADO_CARGADO → FINALIZADO` es un paso manual deliberado (botón
"Finalizar" con advertencia "no se puede revertir") — no automático, pero tampoco roto. Las 5
acciones reales que siguen en `FIRMADO_CARGADO` en este ambiente están así porque nadie ha
presionado ese botón todavía, no por un defecto del código.

## 10. Tablas que se escriben al contratar/nombrar a una persona (2026-07-06)

Investigación de qué tablas participan cuando una persona es contratada (vía `tbl_Contracts`) o
nombrada (vía `tbl_PersonnelActions`, "Acción de Personal"). No es exclusivo de multi-régimen, pero
queda documentado aquí junto al resto del trabajo de esa sesión.

| Tabla | Cuándo se escribe |
|---|---|
| `HR.tbl_People` | **Solo si la persona es nueva.** Paso previo e independiente a crear el contrato/acción: si al buscarla (por nombre/cédula) no aparece, el botón "Registrar Nueva Persona" (`PersonSearchCombobox.tsx` → `PersonCreateDialog.tsx`) crea la fila con un INSERT simple (sin efectos secundarios en otras tablas). |
| `HR.tbl_Contracts` / `HR.tbl_contract_status_history` / `HR.tbl_contractRequest` | Flujo de Contrato — el contrato en sí, su auditoría de estados, y el contador de contratados de la certificación (solo contrato raíz con `CertificationID`). |
| `HR.tbl_PersonnelActions` / `HR.tbl_PersonnelActionStatusHistory` | Flujo de Acción de Personal — la acción y su auditoría de estados. |
| `HR.tbl_GeneratedDocuments` / `HR.tbl_GeneratedDocumentFields` | Ambos flujos — el documento institucional generado desde plantilla. |
| `HR.tbl_Employees` | Ambos — **solo si es el primer vínculo activo** de esa persona (no existe ya un empleado activo para ese `PersonID`). |
| `HR.tbl_EmployeeLaborRegime` / `HR.tbl_PersonnelMovements` | Ambos, pero por **3 mecanismos distintos**: `ContractsService` (contrato raíz, empleado ya existente), `PersonnelActionService` (solo acciones categoría MOVEMENT, empleado ya existente), y `EmployeeProvisioningOrchestrator` (dispara solo cuando se crea la fila nueva en `tbl_Employees` — el verdadero primer ingreso). |
| `HR.tbl_EmailLogs` | Ambos — correo de bienvenida, solo si el aprovisionamiento de cuenta institucional (AD) tuvo éxito. |

**No se escriben aquí:** `HR.TBL_StoredFile` (el archivo firmado ya debe existir de una subida
aparte, solo se enlaza su ID) y los objetos de autenticación/AD (viven en RepositoryUta, fuera de
esta base de datos).

## 11. Refuerzo backend: no regenerar documento tras firmarlo (2026-07-06)

El frontend (`ContractActions.tsx`, `PersonnelActionActions.tsx`) ya ocultaba correctamente el
botón "Generar/Regenerar Documento" a partir de `FIRMADO_CARGADO` — pero el backend no lo exigía,
violando la regla de no confiar solo en ocultar en frontend:

- **Contratos** (`ContractsService.GenerateDocumentAsync`): el único freno era
  `contract.IsDocumentFrozen`, salteable con `ForceRegenerate=true` — sin ningún chequeo por
  **estado** del contrato. Una llamada directa a la API con `ForceRegenerate=true` podía
  regenerar el documento de un contrato ya `VIGENTE`. **Corregido:** nuevo bloqueo incondicional
  (no se salta con `ForceRegenerate`) si el estado no es `BORRADOR` ni `GENERADO`.
- **Acciones de Personal** (`PersonnelActionService.GenerateDocumentAsync`): la lista de estados
  bloqueados (`FINALIZADO`, `ANULADO`, `CANCELLED`) no incluía `FIRMADO_CARGADO`. **Corregido:**
  agregado a la lista.

Build verificado sin errores tras ambos cambios.

## 12. Estado VIGENTE en Acciones de Personal (2026-07-06, noche)

Regla confirmada por el usuario: `VIGENTE` es la fuente de verdad de la que se lee el
sueldo/departamento/horario **actual** de la persona — solo puede haber **una** acción vigente por
empleado a la vez, sin importar el tipo. Cuando entra una nueva, la que estuviera vigente se cierra
a `FINALIZADO`, sin importar si esa anterior definía sueldo, departamento u horario.

### Diseño
En vez de codificar en C# una lista de categorías, se agregó una columna de configuración al
catálogo mismo (mismo patrón que `RequiresAdUserCreation`/`RequiresAdUserDisable`):

- `HR.tbl_personnel_action_type.ReachesVigente BIT` — marca qué tipos participan en la cadena de
  vigencia.
- `Nombramiento`, `Traslado`, `Encargo de Funciones` → `ReachesVigente=1` (ya existían).
- **5 tipos nuevos creados** (reutilizan la plantilla genérica `TemplateId=1`, "Acción de
  Personal", igual que los 5 tipos existentes):

  | Tipo | Categoría | ReachesVigente |
  |---|---|---|
  | Cambio de Sueldo | `SALARY_CHANGE` | 1 |
  | Asistencia / Afectación de Horario | `SCHEDULE` | 1 |
  | Sanción Disciplinaria | `DISCIPLINARY` | 0 |
  | Vulnerabilidad | `VULNERABILITY` | 0 |
  | Vacaciones (acción) | `VACATION` | 0 |

  Vulnerabilidad y Vacaciones se dejaron en `0` (van directo a `FINALIZADO`, igual que
  Comisión/Licencia) porque ninguna redefine sueldo/departamento/horario — de lo contrario
  cerrarían el Nombramiento vigente sin necesidad.

### Backend (`PersonnelActionService.cs`)
- `AllowedTransitions`: `FIRMADO_CARGADO → {VIGENTE, FINALIZADO, ANULADO}`, nuevo
  `VIGENTE → {FINALIZADO}`.
- `UploadSignedDocumentAsync`: si `ActionType.ReachesVigente=1`, transiciona
  **automáticamente** `FIRMADO_CARGADO → VIGENTE` al cargar el documento firmado (sin botón
  manual) y llama a `CloseSupersededVigenteActionAsync` (nuevo método: busca la acción `VIGENTE`
  de ese mismo empleado, sin importar tipo, y la cierra a `FINALIZADO`).
- `RegisterMovementAndRegimeFromActionAsync` — antes se disparaba en `FinalizeAsync` solo para
  categoría `MOVEMENT`. Se movió a la transición automática a `VIGENTE` (ya que esos tipos ya no
  pasan por `FinalizeAsync` manual); el método ya es defensivo así que es seguro para los 5 tipos
  `ReachesVigente=1`, no solo `MOVEMENT`.
- `GenerateDocumentAsync`: `VIGENTE` agregado a los estados bloqueados para regenerar documento
  (ver sección 11 — nuevo estado que necesitaba el mismo refuerzo).
- `CHK_PersonnelActions_Status` (constraint existente fuera de estos scripts) no permitía
  `VIGENTE` — recreado con el valor agregado.

### Corrección retroactiva ejecutada
5 acciones reales ya estaban en `FIRMADO_CARGADO` con tipos `ReachesVigente=1` (creadas antes de
este cambio): `ActionID` 14, 23, 28, 29 → `VIGENTE`. `ActionID=24` (Nombramiento del mismo empleado
5406 que el 28, pero un día más viejo) → `FINALIZADO` (superado por el 28). Verificado con
`fn_ResolveEmployeeRate` que el empleado 4 sigue resolviendo correctamente su tarifa LOSEP (1354)
ahora leyendo `Status='VIGENTE'` en vez de la heurística anterior de "más reciente por fecha".

### `HR.fn_ResolveEmployeeRate` — corrección de fondo
La ruta NOMBRAMIENTO cambió de `WHERE pa.Status='FIRMADO_CARGADO'` (heurística "el más reciente
gana") a `WHERE pa.Status='VIGENTE'` (señal explícita y correcta, ya no depende de fecha).

### Frontend
- `PersonnelActionActions.tsx`: nuevo estado `VIGENTE` en el tipo; botón "Finalizar" oculto para
  tipos `reachesVigente=true` en `FIRMADO_CARGADO` (pasa solo); nuevo indicador visual "Vigente";
  `isTerminal` incluye `VIGENTE` (no se puede anular algo que representa el estado actual).
- `UploadSignedDocumentDialog.tsx`: si `reachesVigente=true`, ya no llama a `onAutoFinalize`/
  `onFinalizePreviousAction` (el backend ya hizo todo dentro de la misma llamada de carga) —
  **se encontró que llamarlos igual habría movido la acción recién vigente a `FINALIZADO`
  inmediatamente**, deshaciendo el propósito.
- `PersonnelActionDetailDto` (backend): se agregó `ActionTypeReachesVigente`, `ActionTypeRequiresAdUserCreation`
  y `ActionTypeRequiresAdUserDisable` — estos 2 últimos **ya eran esperados por el frontend desde antes**
  pero nunca se serializaban (el DTO no los incluía) — gap preexistente sin relación con esta tarea,
  corregido de paso porque afectaba directamente la lógica que se estaba tocando. Consecuencia real:
  antes, cargar el documento de una acción "Comisión de Servicios" (que tiene
  `RequiresAdUserDisable=1`) nunca deshabilitaba la cuenta AD de la persona porque el flag llegaba
  siempre en `false`; con el fix, ahora sí se dispara.

Build backend y typecheck de frontend verificados sin errores nuevos (los errores de TypeScript
preexistentes en archivos no relacionados — `ContractForm.tsx`, `EmployeeForm.tsx`,
`FacultyForm.tsx`, `PayrollForm.tsx` — ya existían antes de esta sesión).

## 13. Módulo Renuncia/Jubilación (2026-07-06)

Nuevo módulo end-to-end: solicitudes de renuncia y jubilación registradas por el propio empleado
autenticado y revisadas por Recursos Humanos.

### Regla de seguridad central
El `EmployeeId` de la solicitud **nunca** viaja editable desde el frontend. Se resuelve siempre en
`ResignationRetirementRequestsController` vía `ICurrentUserService.EmployeeId` (claim `employeeId`
del JWT). No existe ningún endpoint que acepte un `EmployeeId` de otro empleado para crear/editar
"mi" solicitud.

### Base de datos (`Database/hr/10_resignation_retirement.sql`)
- `HR.tbl_ResignationRetirementRequests`: `RequestType` (RESIGNATION/RETIREMENT), `Status`
  (PENDIENTE/EN_REVISION/DEVUELTO/APROBADO/RECHAZADO/ANULADO), `RowVersion` para concurrencia,
  `LinkedPersonnelActionId` reservado (no poblado automáticamente) para la futura acción de
  desvinculación.
- `HR.tbl_ResignationRetirementStatusHistory`: auditoría de cada cambio de estado.
- Índice único filtrado `UQ_ResignationRetirement_ActiveByEmployeeType`: impide 2 solicitudes
  activas (PENDIENTE/EN_REVISION/DEVUELTO) del mismo tipo para el mismo empleado.
- Documentos: **no se creó tabla nueva de archivos**. Se reutiliza `HR.TBL_StoredFile` +
  `HR.TBL_DirectoryParameters` con `DirectoryCode='HR_RESIGNATION_RETIREMENT'`
  (`\\nas11.uta.edu.ec\ArchUTA1\HR\resignation_retirement\`, `.pdf`, 25MB — mismo patrón que
  `HRCONTRACT`/`HRPERSONNEL_ACTION`, verificado contra la BD antes de insertar el seed).

### Backend
- `ResignationRetirementRepository.GetEmployeeConsolidatedInfoAsync`: arma la info consolidada
  (personal + laboral + contractual + vacaciones + tiempo de servicio) sin duplicar datos —
  reutiliza `Employees`/`People`/`Departments`/`Jobs`/`RefTypes` y resuelve el "vigente" con el
  mismo criterio que `fn_ResolveEmployeeRate` (sección 12): primero contrato
  `Status='VIGENTE'` por `PersonID`, si no existe cae a acción de personal `Status='VIGENTE'`
  por `EmployeeID`. Vacaciones desde `HR.tbl_TimeBalances.VacationAvailableMin` (sumado si hay
  más de un régimen).
- `ResignationRetirementService`: máquina de estados PENDIENTE → (EN_REVISION) → APROBADO |
  RECHAZADO | DEVUELTO (→ PENDIENTE al reenviar) | ANULADO. Reglas duras: no aprobar sin
  contrato/acción vigente; observación obligatoria en rechazar/devolver/anular; RowVersion
  verificado a mano antes de cada mutación (concurrencia optimista).
- Controller expone dos superficies: `/my/*` (solicitante, siempre filtra por su propio
  `EmployeeId`) y raíz (RRHH, filtrado por `IUserAccessScopeService` con
  `moduleCode='RESIGNATION_RETIREMENT_REQUESTS'`, mismo patrón que `PersonnelActionsController`).

### Riesgo señalado (no introducido por este módulo, ya preexistente)
No hay `[Authorize(Roles=...)]` ni claim de rol en el JWT en ningún controller de este sistema —
la protección de "esto es una acción de RRHH" vive en el menú/rutas del frontend
(`requiredPath` en `routes.config.tsx`, resuelto contra `dbo.vw_RoleMenuItems`). Este módulo sigue
el mismo nivel de rigor que ya existe (ownership real en `/my/*` verificado en backend), pero no
inventa infraestructura de roles que no existe. Un endurecimiento real sería un cambio transversal
a aprobar aparte.

### Frontend
- `types/resignation-retirement.ts`, `lib/api/services/resignationRetirement.ts`.
- `components/resignationRetirement/`: `RequestStatusBadge`, `EmployeeInfoCard` (bloque de solo
  lectura), `RequestHistoryTimeline`, `RequestForm` (crear/editar, con `useUnsavedChangesGuard` +
  `UnsavedChangesDialog` como exige el patrón de diálogos con formulario), `ReviewActionDialog`
  (aprobar/rechazar/devolver/anular).
- `pages/MyResignationRetirementRequestsPage.tsx` + `MyResignationRetirementRequestDetail.tsx`
  (solicitante) y `pages/HrResignationRetirementRequestsPage.tsx` +
  `HrResignationRetirementRequestDetail.tsx` (RRHH, incluye `ReusableDocumentManager` reutilizado
  para documentos de respaldo).
- Rutas nuevas en `routes.config.tsx`: `/my-resignation-retirement-requests(/:id)` y
  `/resignation-retirement-requests(/:id)`. **Pendiente fuera de código**: crear las entradas de
  menú correspondientes (tablas detrás de `dbo.vw_RoleMenuItems`) para que aparezcan en el sidebar
  según rol — es dato de configuración, no algo que deba inventarse en el código.

Build backend (`dotnet build`, 0 errores) y `tsc --noEmit` de frontend verificados: 0 errores en
los archivos nuevos de este módulo (los ~105 errores preexistentes en archivos no relacionados ya
existían antes de esta sesión).
