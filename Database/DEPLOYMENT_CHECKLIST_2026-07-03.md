# Checklist de despliegue — Auditoría vacaciones/asistencia/horas extra (2026-07-03)

## Estado: TODO DESPLEGADO Y VERIFICADO contra `10.102.12.83` / `dbutasystem` (2026-07-03)

Este documento originalmente listaba los pasos pendientes de ejecutar. Se actualiza aquí con el
resultado real de cada paso, verificado directamente contra la base de datos (no solo revisión de
archivos). Importante para quien lea esto después: **no se ejecutaron los archivos `.sql`
completos** (varios contienen `CREATE PROCEDURE` simple para objetos que ya existen en la BD, y
correr el archivo entero habría fallado con "objeto ya existe"). En su lugar, se extrajeron y
ejecutaron solo los bloques `CREATE OR ALTER`/`ALTER TABLE` realmente modificados por esta tarea.

## 1. `Database/hr/09_job_execution_log.sql` — ✅ Desplegado

`HR.tbl_JobExecutionLog` + `HR.sp_JobExecutionLog_Start`/`_Finish` creados. Verificado con
`sys.tables`/`sys.procedures`.

## 2. Columnas nuevas en `HR.tbl_OvertimeConfig` (de `01_tables.sql`) — ✅ Desplegado

`MaxDailyMinutes`/`MaxWeeklyMinutes` (nullable) agregadas. Verificado con `sys.columns` — ambas en
`NULL` para todos los tipos existentes (sin tope activo, cero impacto).

## 3. Procedimientos modificados (de `06_procedures.sql` y `08_guard_attendance.sql`) — ✅ Desplegados

Los 9 procedimientos tocados en las Fases 2-4, cada uno vía `CREATE OR ALTER` extraído
individualmente del archivo fuente y ejecutado directo contra la BD:

| Procedimiento | Fase | `uses_quoted_identifier` verificado |
|---|---|---|
| `sp_hr_AccrueVacationBalance` | 3 | ✅ 1 |
| `sp_hr_ReserveVacationBalance` | 3 | ✅ 1 |
| `sp_ProcessAttendanceBaseDay` | 2 | ✅ 1 |
| `sp_ProcessAttendanceLeavesDay` | 2 | ✅ 1 |
| `sp_ProcessAttendancePlanningDay` | 4 | ✅ 1 |
| `sp_ProcessTimePlanningForEmployeeDay` | 4 | ✅ 1 |
| `sp_ProcessGuardAttendanceDate` | 4 | ✅ 1 |
| `sp_ProcessAttendanceRunDate` | Post-hoc (filtro `@FilterEmployeeID`) | ✅ 1 |
| `sp_ProcessAttendanceRunRange` | Post-hoc (filtro `@FilterEmployeeID`) | ✅ 1 |

**Incidente encontrado y resuelto durante el despliegue:** el primer intento de desplegar
`sp_ProcessAttendanceBaseDay` reveló el bug real de `QUOTED_IDENTIFIER` (ver
`ATTENDANCE_PIPELINE.md`) — se agregó `SET ANSI_NULLS ON`/`SET QUOTED_IDENTIFIER ON` al inicio de
cada script de despliegue antes de cada `CREATE OR ALTER`, y se corrigió también dentro de
`06_procedures.sql`/`08_guard_attendance.sql` para que futuros despliegues no repitan el problema.
También se detectó que `sp_ProcessTimePlanningForEmployeeDay` dependía de las columnas de
`tbl_OvertimeConfig` (paso 2) — se desplegó el esquema primero, luego los procedimientos.

## 4. `Database/SqlAgent_DailyAttendanceJob.sql` — ❌ NO desplegado, decisión revertida

Se encontró que el servicio **"SQL Server Agent" está detenido (`Stopped`)** en el servidor
`10.102.12.83` y no había forma inmediata de reactivarlo. Se decidió **revertir la migración**:
el job `DailyAttendanceCalculationJob` volvió a activarse en Quartz/backend (descomentado en
`Infrastructure/DependencyInjection/QuartzConfiguration.cs`, ver `QUARTZ_JOBS_README.md`, sección
"Job revertido de vuelta a Quartz"). El script de SQL Agent queda en el repo sin usar, por si en
el futuro se reactiva el servicio y se quiere retomar esa migración.

Dato de contexto verificado por si se retoma: la hora del servidor SQL coincide con Guayaquil
(UTC-5, diferencia exacta de 5h contra `SYSUTCDATETIME()`), así que el horario `07:00` del script
ya está bien calculado, no necesitaría ajuste.

## 5. Fix adicional no planeado originalmente: filtro `@FilterEmployeeID`

Durante las pruebas desde el frontend se detectó que el campo "ID Empleado (Opcional)" del
formulario de "Procesar Rango de Asistencia" nunca tuvo efecto real — ni `sp_ProcessAttendanceRunDate`
ni `sp_ProcessAttendanceRunRange` tenían parámetro de empleado. Se agregó `@FilterEmployeeID INT
= NULL` a ambos (comportamiento por defecto sin filtrar, igual que antes) y se conectó de punta a
punta: Controller (`AttendanceCalculationRequestDto.EmployeeId`) → Service → SPs. Desplegado y
probado en vivo contra la BD.

## 6. Fix adicional: logging manual vs automático

`AttendanceCalculationsController` (`process-range`, `process-date`) ahora registra en
`HR.tbl_JobExecutionLog` con `Source='Manual'` cada vez que se ejecuta desde el frontend, además
del `Source='Quartz'` que ya registraba la ejecución automática de las 07:00. Solo cambio de
código C# (`Controllers/HR/AttendanceCalculationsController.cs`), no requirió despliegue de SQL
adicional — usa los SPs de logging ya desplegados en el paso 1.

## Verificación post-despliegue (ya ejecutada)

```sql
-- Confirmar procedimientos con settings correctos
SELECT o.name, m.uses_quoted_identifier, o.modify_date
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name IN (
  'sp_hr_AccrueVacationBalance','sp_hr_ReserveVacationBalance','sp_ProcessAttendanceBaseDay',
  'sp_ProcessAttendanceLeavesDay','sp_ProcessAttendancePlanningDay','sp_ProcessAttendanceRunDate',
  'sp_ProcessAttendanceRunRange','sp_ProcessTimePlanningForEmployeeDay','sp_ProcessGuardAttendanceDate'
);

-- Confirmar columnas de tope (deben seguir en NULL)
SELECT OvertimeType, MaxDailyMinutes, MaxWeeklyMinutes FROM HR.tbl_OvertimeConfig;

-- Ver ejecuciones registradas (manuales y automáticas)
SELECT TOP 20 * FROM HR.tbl_JobExecutionLog ORDER BY StartedAt DESC;
```

Ver `Database/hr/verification_fase4_2026-07-03.sql` para las consultas detalladas de validación
por punto (estados de autorización, factor de feriado, ActualMinutes, tope, guardias).

## Rollback

Todos los cambios de SP son `CREATE OR ALTER` — para revertir, restaurar la versión anterior desde
el historial de git y volver a ejecutar contra la BD. La columna `MaxDailyMinutes`/`MaxWeeklyMinutes`
no requiere rollback — es aditiva y no tiene efecto mientras esté en `NULL`. El job de Quartz puede
volver a comentarse en `QuartzConfiguration.cs` si se reactiva SQL Server Agent más adelante.

## Referencias

- Detalle técnico completo de cada fix: `Database/ATTENDANCE_PIPELINE.md` (sección "Cambios
  recientes")
- Detalle de la migración/reversión de jobs: `QUARTZ_JOBS_README.md`
