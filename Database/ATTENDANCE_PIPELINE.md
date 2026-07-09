# Pipeline de Asistencia — Documentación Técnica

## Estado: Vigente al 2026-07-03

---

## Pipeline activo (único flujo válido)

```
Quartz "DailyAttendanceCalculationJob" (07:00, procesa fecha_ayer)
[2026-07-03: se había migrado a SQL Server Agent, pero se revirtió el mismo día
 porque el servicio SQL Server Agent está detenido en el servidor. Ver
 QUARTZ_JOBS_README.md, sección "Job revertido de vuelta a Quartz".]
  └─ sp_ProcessAttendanceRunRange(@FromDate, @ToDate)
       └─ sp_ProcessAttendanceRunDate(@WorkDate)  [por cada fecha]
            ├─ 1. sp_ProcessAttendanceBaseDay        — asistencia base, atrasos, nocturnos
            │      (2026-07-03: fix jornada única — ver "Cambios recientes")
            ├─ 2. sp_ProcessAttendanceLeavesDay       — permisos, vacaciones, licencias
            │      (2026-07-03: fix AbsentMinutes vs vacaciones/permisos)
            ├─ 3. sp_ProcessAttendanceJustificationsDay — justificaciones aprobadas
            ├─ 4. sp_ProcessAttendanceRecoveryDay     — minutos recuperados
            ├─ 5. sp_ProcessAttendancePlanningDay     — horas extra planificadas
            │      (delega en sp_ProcessTimePlanningForEmployeeDay, ver nota
            │       de corrección más abajo — 2026-07-03: varios fixes)
            ├─ 6. sp_ProcessAttendanceFinalizeDay     — normalización, subsidio, sellado
            └─ 7. sp_ProcessGuardAttendanceDate       — guardias (pipeline paralelo)
                   ├─ sp_ProcessAttendanceBaseDay
                   ├─ sp_ProcessAttendanceLeavesDay
                   ├─ sp_ProcessAttendanceJustificationsDay
                   ├─ sp_ProcessAttendanceRecoveryDay
                   ├─ sp_ProcessAttendancePlanningDay  ← agregado 2026-07-03
                   │      (antes los guardias NUNCA pasaban por este paso;
                   │       sus horas extra ejecutadas nunca llegaban a
                   │       tbl_Overtime. Se le pasa el horario ya resuelto
                   │       del turno vía @OverrideEntryTime/@OverrideExitTime,
                   │       porque este SP por defecto resuelve horario desde
                   │       tbl_EmployeeSchedules, tabla que los guardias no usan)
                   └─ sp_ProcessAttendanceFinalizeDay
```

**Puntos de entrada C#:**
- `AttendanceCalculationService.ProcessAttendanceRunRangeAsync` → `sp_ProcessAttendanceRunRange`
- `AttendanceCalculationService.ProcessAttendanceRunDateAsync` → `sp_ProcessAttendanceRunDate`
- Disparo diario: Quartz `DailyAttendanceCalculationJob` (07:00, dentro del proceso backend).
- Endpoint manual: `POST /attendance/calculations/process-range` y `/process-date`
  (mismo `DailyAttendanceCalculationJob`/`AttendanceCalculationService` en C#)

---

## Cobertura por concepto

| Concepto | SP responsable | Tabla destino |
|---|---|---|
| Asistencia base / días trabajados | `sp_ProcessAttendanceBaseDay` | `tbl_AttendanceCalculations` |
| Atrasos (TardinessMin, MinutesLate) | `sp_ProcessAttendanceBaseDay` | `tbl_AttendanceCalculations` |
| Permisos (paid/unpaid/médico/cargo vacaciones) | `sp_ProcessAttendanceLeavesDay` | `tbl_AttendanceCalculations` |
| Cruce con vacaciones | `sp_ProcessAttendanceLeavesDay` | `tbl_AttendanceCalculations` |
| Justificaciones | `sp_ProcessAttendanceJustificationsDay` | `tbl_AttendanceCalculations` (solo) |
| Recuperaciones | `sp_ProcessAttendanceRecoveryDay` | `tbl_AttendanceCalculations` |
| Horas extras / planificación | `sp_ProcessAttendancePlanningDay` | `tbl_AttendanceCalculations`, `tbl_TimePlanningExecution`, `tbl_Overtime` |
| Minutos nocturnos | `sp_ProcessAttendanceBaseDay` + `FinalizeDay` | `tbl_AttendanceCalculations` |
| Subsidio alimentación | `sp_ProcessAttendanceFinalizeDay` | `tbl_AttendanceCalculations` |
| Asistencia guardias | `sp_ProcessGuardAttendanceDate` | `tbl_AttendanceCalculations`, `tbl_GuardShiftPlanning` |
| Vacaciones guardias (LOSEP / Código Trabajo) | `sp_ProcessAttendanceLeavesDay` sobre EffectiveEmployeeID | `tbl_AttendanceCalculations` |

**Anti-duplicado guardias:** `sp_ProcessAttendanceRunDate` excluye del loop normal a empleados con `GuardShiftPlanning` activo en la fecha. Los guardias son procesados exclusivamente por `sp_ProcessGuardAttendanceDate`.

---

## Gap activo: Justificaciones retroactivas

### Problema
Cuando un jefe aprueba una justificación posterior a la fecha del evento:
1. `JustificationsService.UpdateWithNotifyAsync` actualiza `tbl_PunchJustifications.Status = APPROVED`
2. **Nada recalcula** `tbl_AttendanceCalculations` para los días pasados afectados
3. El Job diario no reprocesa fechas anteriores
4. El resultado: la ausencia/tardanza aparece injustificada en reportes hasta el siguiente reproceso manual

### Tablas afectadas por `sp_ProcessAttendanceJustificationsDay`
- **Lee:** `tbl_PunchJustifications` (Status IN 'APPROVED','APPLIED')
- **Escribe:** `tbl_AttendanceCalculations` — campos: `JustificationMinutes`, `JustificationApply`, `HasJustification`, `TardinessMin`, `AbsentMinutes`, `UpdatedAt`
- **No toca ninguna otra tabla.**

### Solución pendiente de implementar
En `JustificationsService.UpdateWithNotifyAsync`, cuando `newStatus == "APPROVED"`:
1. Determinar el rango de fechas afectado (StartDate..EndDate o JustificationDate)
2. Llamar `_attendanceCalculationService.ProcessAttendanceRunRangeAsync(startDate, endDate)` para ese empleado/rango
3. Esto garantiza recalcular solo `tbl_AttendanceCalculations` para los días afectados

**Precaución:** el reproceso completo pasa por todos los sub-SPs del pipeline. Si se quiere un impacto mínimo, se puede llamar directamente a `sp_ProcessAttendanceJustificationsDay(@EmployeeID, @WorkDate)` por cada fecha — ese SP solo toca `tbl_AttendanceCalculations`.

---

## SPs eliminados el 2026-07-01

Los siguientes SPs del pipeline legacy fueron eliminados de la BD. El respaldo completo de sus definiciones está en:
`Database/hr/99_legacy_sp_backup_20260701.sql`

| SP eliminado | Razón |
|---|---|
| `HR.sp_Attendance_CalcNightMinutes` | Integrado dentro de `sp_ProcessAttendanceBaseDay` |
| `HR.sp_Attendance_CalculateRange` | Reemplazado por `sp_ProcessAttendanceRunRange` |
| `HR.sp_ProcessAttendanceEmployeeDay` | Pipeline viejo — reemplazado por `sp_ProcessAttendanceBaseDay` |
| `HR.sp_ProcessAttendanceForDate` | Orquestador viejo por fecha |
| `HR.sp_ProcessAttendanceRange` | Orquestador viejo por rango |
| `HR.sp_Recovery_Apply` | Integrado dentro de `sp_ProcessAttendanceRecoveryDay` |

**Corrección 2026-07-03:** esta tabla decía que `HR.sp_ProcessTimePlanningForEmployeeDay` fue
eliminado y reemplazado por `sp_ProcessAttendancePlanningDay`. **Eso es incorrecto** — al
verificar el código real, `sp_ProcessAttendancePlanningDay` es un wrapper delgado (~25 líneas)
que delega toda la lógica real en `sp_ProcessTimePlanningForEmployeeDay`, que sigue existiendo
y es donde vive el 100% del procesamiento de horas extra/recuperación planificada (incluidos los
fixes de la sección siguiente). **No eliminar este SP** — si alguien lo hace guiándose por la
tabla anterior, rompe todo el subsistema de horas extra.

**SP conservado activo (no eliminar):** `HR.sp_Justifications_Apply`
— Llamado por `DailyJustificationsJob` (Quartz) y endpoint `POST .../justifications/apply`

---

## Empleados LOES + LOSEP simultáneo

### Estado: sin casos en producción (2026-07-01)

Ver `Database/MULTI_REGIME_EMPLOYEES.md` para el mapeo técnico completo de este tema (modelo de
datos, puntos donde el sistema asume un solo régimen, impacto concreto). Resumen rápido:

### Problema arquitectónico
El pipeline actual asume un único régimen por empleado:
- Un horario activo (`TOP 1 ORDER BY ValidFrom DESC` en `tbl_EmployeeSchedules`)
- Una fila en `tbl_AttendanceCalculations` por `(EmployeeID, WorkDate)`
- Un `ContractType` en `sp_ProcessAttendanceFinalizeDay` para calcular subsidio y reglas de descuento

### Particularidad LOES (docentes)
- El docente no tiene horario de entrada/salida fijo
- Su jornada se distribuye en **franjas por materia** (puede tener 2-3 materias en distintos bloques horarios el mismo día)
- Las picadas son múltiples entradas/salidas correspondientes a cada bloque de cátedra
- El pipeline actual trata todas las picadas del día como una sola jornada continua
- **Corrección 2026-07-03:** no es que esto esté "parcialmente implementado" — se verificó que
  `Activity`/`JobActivity`/`AdditionalActivity` son catálogos sin ningún campo de horario, y no
  se referencian en el pipeline de asistencia. Es 100% inexistente, no parcial (detalle en
  `Database/MULTI_REGIME_EMPLOYEES.md`).

### Enfoque acordado (pendiente de diseño detallado)
Garantizar que las picadas sean el dato central y reflejen correctamente cada bloque de cátedra.
Diseño específico pendiente de aprobación antes de implementar.

### Restricción confirmada
No implementar hasta que existan casos reales en producción. Cuando ocurra, proponer diseño completo y esperar aprobación antes de tocar el esquema o el pipeline.

---

## Cambios recientes (2026-07-03, Fases 0-4)

Auditoría completa de acreditación/descuento de vacaciones, atrasos y horas extra. Cambios
aplicados en orden, cada uno validado con `dotnet build` (0 errores) y trazado manual antes de
pasar al siguiente. Detalle de cada fase:

**Fase 0 — Logging de ejecuciones de job**
Nueva tabla `HR.tbl_JobExecutionLog` + SPs `sp_JobExecutionLog_Start`/`_Finish`. Integrado en
`BaseJob.cs`, aplica automáticamente a todos los jobs de Quartz.

**Fase 1 — Asistencia migrada a SQL Server Agent**
Ver sección "Pipeline activo" arriba y `QUARTZ_JOBS_README.md`.

**Fase 2 — `sp_ProcessAttendanceBaseDay` / `sp_ProcessAttendanceLeavesDay`**
- *Jornada única:* si el horario tiene almuerzo (2 jornadas: mañana/tarde) y el empleado marca
  completa solo una, antes el sistema calculaba el atraso/salida-anticipada comparando la
  marcación existente contra el horario del DÍA COMPLETO (ej. entrada de la tarde comparada
  contra el inicio del turno de la mañana → atraso falso de horas). Ahora cada jornada se evalúa
  contra su propio rango. Guardia de seguridad: si los datos de almuerzo vienen inconsistentes
  (`LunchEnd<=LunchStart`, fuera del turno, o dura más de 4h), cae al comportamiento anterior
  (jornada única) en vez de fallar. El caso de ausencia total (ninguna jornada con marcación)
  no cambió — sigue siendo ausencia de día completo, por decisión de negocio explícita.
- *`AbsentMinutes` vs vacaciones/permisos:* antes un día de vacación o permiso aprobado, sin
  marcación (normal, el empleado no va a trabajar), quedaba con `AbsentMinutes` = jornada
  completa → generaba descuento de nómina indebido ADEMÁS del descuento de saldo de vacaciones.
  Ahora `AbsentMinutes` se reduce por `VacationMinutes+PermissionMinutes+MedicalLeaveMinutes`,
  igual que ya hacía `sp_ProcessAttendanceJustificationsDay` para justificaciones.

**Fase 3 — `sp_hr_AccrueVacationBalance` / `sp_hr_ReserveVacationBalance`**
- Redondeo modo `TOTAL` alineado con `MONTHLY`/`DAILY` (antes truncaba en vez de redondear).
- `WITH (UPDLOCK, HOLDLOCK)` en la lectura de saldo al reservar vacaciones — cierra una condición
  de carrera que podía dejar el saldo en negativo con dos solicitudes concurrentes.
- Días calendario cobrados: reemplazada la aproximación por semanas completas (dependía del día
  de la semana en que arrancaba el período, podía "regalar" días) por el span calendario real
  (`DATEDIFF(DAY,@StartDate,@EndDate)+1`).
- Liquidación proporcional al dar de baja a mitad de mes: antes un empleado desvinculado a mitad
  de mes quedaba excluido por completo del batch de acreditación mensual (filtro `IsActive`
  previo a invocar el SP) y perdía la acreditación de los días sí trabajados ese mes. Ahora se
  prorratea hasta `Contracts.EndDate` del último contrato terminal (misma regla de "sin addendum
  que lo extienda" que ya usa `ContractExpirationService`). Empate de múltiples contratos
  terminales el mismo mes: se toma el de `EndDate` más reciente (documentado en el SP).

**Fase 4 — `sp_ProcessTimePlanningForEmployeeDay` / `sp_ProcessAttendancePlanningDay`**
- Estados válidos de autorización de horas extra: antes aceptaba `'Borrador'` e incluso estado
  nulo (fail-open) como autorización válida para pagar horas extra. Ahora solo `'Aprobado'`
  cuenta. (Nota: si un plan ya había sido consolidado en `tbl_Overtime` bajo la regla vieja antes
  de este fix, reprocesar el día no retira esa fila histórica — es un gap preexistente separado,
  no resuelto automáticamente.)
- Factor/tipo real de feriado: antes se hardcodeaba `OvertimeType='Ordinaria'`/`Factor=1.0` al
  consolidar en `tbl_Overtime`, sin importar el tipo real del plan. Ahora usa el tipo/factor real
  del plan (si hay varios planes de Overtime el mismo día con tipos distintos, gana el de mayor
  `Factor`).
- `tbl_TimePlanningEmployees.ActualMinutes`/`ActualHours`: antes eran campos muertos, siempre 0.
  Ahora se recalculan desde `tbl_TimePlanningExecution` en cada corrida (no se suman
  incrementalmente, para que reprocesar el mismo día no duplique).
- Tope de horas extra: nuevos campos opcionales `MaxDailyMinutes`/`MaxWeeklyMinutes` en
  `HR.tbl_OvertimeConfig` (NULL por defecto = sin tope, cero impacto hasta que se configuren).
  Si se activan, truncan solo lo que se paga en `tbl_Overtime`; `tbl_AttendanceCalculations` y
  `tbl_TimePlanningExecution` siguen con el minuto real ejecutado sin recortar.
- Guardias sin horas extra: ver nota en el diagrama del pipeline arriba.

---

## SPs activos en BD — inventario final (32 SPs)

### Reportería
- `sp_GetAttendanceReport`
- `sp_GetDepartmentsReport`
- `sp_GetEmployeesReport`
- `sp_GetReportAttendanceSumary`
- `sp_GetReportAudits`
- `sp_InsertReportAudit`

### Saldos de tiempo y vacaciones
- `sp_hr_AccrueVacationBalance`
- `sp_hr_ConsumeReservation`
- `sp_hr_DebitRecoveryBalance`
- `sp_hr_EnsureTimeBalanceRow` (helper interno)
- `sp_hr_GetEmployeeBalances`
- `sp_hr_GetVacationParams` (helper interno)
- `sp_hr_ProcessRecoveryBalance`
- `sp_hr_ReleaseReservation`
- `sp_hr_ReservePermissionBalance`
- `sp_hr_ReserveVacationBalance`

### Pipeline de asistencia (vigente)
- `sp_ProcessAttendanceBaseDay`
- `sp_ProcessAttendanceFinalizeDay`
- `sp_ProcessAttendanceJustificationsDay`
- `sp_ProcessAttendanceLeavesDay`
- `sp_ProcessAttendancePlanningDay`
- `sp_ProcessAttendanceRecoveryDay`
- `sp_ProcessAttendanceRunDate`
- `sp_ProcessAttendanceRunRange`
- `sp_ProcessGuardAttendanceDate`

### Nómina
- `sp_Overtime_Calculate`
- `sp_Overtime_Price`
- `sp_Payroll_Discounts`
- `sp_Payroll_Subsidies`

### Justificaciones (legacy activo)
- `sp_Justifications_Apply`

### Otros
- `sp_RegisterPersonnelMovement`
- `usp_ExecuteScheduleChangePlans`
