# Quartz.NET Jobs - HrBackend

## Descripcion general

Este proyecto usa Quartz.NET para registrar trabajos en segundo plano dentro del proceso ASP.NET Core.

La configuracion real esta en:

- `Program.cs`
- `Infrastructure/DependencyInjection/QuartzConfiguration.cs`
- `Infrastructure/Jobs/`

Importante: los jobs solo se registran si `Quartz:EnableJobs` esta en `true`.

## Activacion

`Program.cs` llama:

```csharp
builder.Services.AddQuartzJobs(builder.Configuration);
```

Luego `QuartzConfiguration.AddQuartzJobs(...)` lee:

```csharp
var enableJobs = configuration.GetValue<bool>("Quartz:EnableJobs");

if (!enableJobs)
    return services;
```

Configuracion actual:

```json
// appsettings.json
{
  "Quartz": {
    "EnableJobs": false
  }
}
```

```json
// appsettings.Production.json
{
  "Quartz": {
    "EnableJobs": true
  }
}
```

Por tanto:

- En ambiente que solo carga `appsettings.json`, los jobs quedan apagados.
- En ambiente `Production`, si carga `appsettings.Production.json`, los jobs quedan activos.

## Zona horaria

Todos los triggers registrados usan:

```csharp
const string timeZone = "America/Guayaquil";
```

Los calculos de "dia anterior" y los horarios de disparo deben interpretarse con esa zona horaria.

## Jobs programados actualmente

Estos son los jobs que SI estan registrados en `QuartzConfiguration.cs`.

| JobKey | Clase | Horario | Cron | Funcion |
|---|---|---:|---|---|
| `DailyContractExpirationJob` | `DailyContractExpirationJob` | Diario 02:00 | `0 0 2 * * ?` | Procesa contratos vigentes vencidos y deshabilita cuentas AD si corresponde. |
| `MonthlyAccrueVacationBalanceJob` | `DailyAccrueVacationBalance` | Dia 1 de cada mes 00:30 | `0 30 0 1 * ?` | Acredita vacaciones del mes anterior a empleados activos. |

## Job revertido de vuelta a Quartz (2026-07-03, mismo día)

La migración a SQL Server Agent descrita abajo se revirtió el mismo día: el servicio "SQL Server
Agent" está detenido en el servidor y no había forma inmediata de reactivarlo. El bloque de
`DailyAttendanceCalculationJob` en `QuartzConfiguration.cs` está de vuelta activo (sin comentar).
El script `Database/SqlAgent_DailyAttendanceJob.sql` queda en el repo por si en el futuro se
reactiva el servicio de Agent y se quiere retomar esa migración — en ese caso, comentar de nuevo
el bloque de Quartz para evitar doble ejecución.

## (Histórico) Job migrado a SQL Server Agent (2026-07-03)

`DailyAttendanceCalculationJob` (calculo diario de asistencia, antes 07:00 via Quartz) fue
**dado de baja de Quartz** y movido a **SQL Server Agent**. Motivo: el job corria dentro del
proceso ASP.NET Core/IIS; si el application pool estaba dormido o reciclado a las 07:00 (idle
timeout de IIS sin trafico durante la madrugada), el job simplemente no disparaba, sin error
visible — el scheduler de Quartz (en memoria, no persistente) ni siquiera existia en ese
instante. SQL Server Agent corre como servicio de Windows independiente de IIS, evitando ese
problema de raiz.

- El bloque `AddJob<DailyAttendanceCalculationJob>`/`AddTrigger` en `QuartzConfiguration.cs`
  quedo **comentado, no borrado** (para poder revertir facilmente si hiciera falta).
- El job de SQL Server Agent se crea con `Database/SqlAgent_DailyAttendanceJob.sql` (idempotente,
  seguro de re-ejecutar). Llama directo a `HR.sp_ProcessAttendanceRunRange` para el dia anterior,
  envuelto en `TRY/CATCH` con 1 reintento automatico a los 5 minutos.
- **Importante:** el horario "07:00" del job de SQL Agent es la hora del **reloj del servidor
  SQL**, no tiene el manejo de zona horaria "America/Guayaquil" que si maneja Quartz.NET. Ver la
  cabecera de `Database/SqlAgent_DailyAttendanceJob.sql` antes de desplegar.
- La clase C# `DailyAttendanceCalculationJob.cs` y el metodo
  `AttendanceCalculationService.ProcessAttendanceRunRangeAsync` **siguen existiendo y
  compilando** — se usan para el endpoint manual (`POST
  api/v1/rh/attendance-calculations/process-range`), solo se quito el disparo automatico via
  Quartz.

## Log de ejecuciones de jobs (Fase 0, 2026-07-03)

Todos los jobs de Quartz (via `BaseJob.cs`) y el job de asistencia en SQL Server Agent registran
cada corrida en `HR.tbl_JobExecutionLog` (inicio, fin, estado `Started`/`Success`/`Failed`,
mensaje de error si aplica, duracion en ms). Fuente: `Database/hr/09_job_execution_log.sql`.
Util para diagnosticar el caso de "el job no corrio" sin depender solo de los logs de texto de
Serilog:

```sql
SELECT TOP 20 * FROM HR.tbl_JobExecutionLog ORDER BY StartedAt DESC;
```

## Jobs registrados sin horario fijo

| JobKey | Clase | Estado | Funcion |
|---|---|---|---|
| `DailyStudentEnrollmentSyncJob` | `DailyStudentEnrollmentSyncJob` | Durable, sin trigger cron | Sincroniza matriculas de estudiantes. Requiere `PeriodCode` y opcionalmente `PreviousPeriod`. |

Este job no corre automaticamente porque no tiene trigger con `WithCronSchedule(...)`.

## Jobs existentes pero no programados

Estas clases existen en `Infrastructure/Jobs/`, pero no estan registradas en `QuartzConfiguration.cs`.
Actualmente no corren de forma automatica.

| Clase | Funcion |
|---|---|
| `DailyNightMinutesCalculationJob` | Calcula minutos nocturnos del dia anterior con `CalculateNightMinutesAsync`. |
| `DailyJustificationsJob` | Aplica justificaciones aprobadas con `ApplyJustificationsAsync`. |
| `DailyRecoveryJob` | Aplica recuperaciones con `ApplyRecoveryAsync`. |
| `DailyOvertimeRecoveryCalculation` | Aplica recuperacion/horas extra con `ProcessApplyOvertimeRecovery`. |
| `MonthlyOvertimePriceJob` | Calcula precio de horas extra del mes anterior con `CalculateOvertimePriceAsync`. |
| `MonthlyPayrollDiscountsJob` | Calcula descuentos de nomina del mes anterior con `CalculateDiscountsAsync`. |
| `MonthlyPayrollSubsidiesJob` | Calcula subsidios y recargos del mes anterior con `CalculateSubsidiesAsync`. |

Si alguno de estos debe ejecutarse automaticamente, hay que agregar su `AddJob` y `AddTrigger` en `QuartzConfiguration.cs`.

## Logs esperados

Todos los jobs que heredan de `BaseJob` registran marcas estandar:

```text
JOB_START
JOB_OK
JOB_FAIL
```

Ejemplos de mensajes especificos:

- `Daily attendance pipeline targetDate=...`
- `Inicio proceso contratos vencidos...`
- `Daily accrue vacation balance targetDate=...`
- `[STUDENT-JOB] ...`

Serilog escribe en:

```text
logs/log-.txt
```

Si no aparecen `JOB_START`, `JOB_OK` o `JOB_FAIL`, revisar primero:

1. Que el proceso este corriendo con `Quartz:EnableJobs = true`.
2. Que el ambiente sea el esperado, por ejemplo `ASPNETCORE_ENVIRONMENT=Production`.
3. Que se esten revisando los logs de la misma instancia/carpeta donde corre la aplicacion.
4. Que el job esperado este realmente registrado en `QuartzConfiguration.cs`.

## Formato cron de Quartz

Quartz.NET usa cron de 6 campos:

```text
segundos minutos horas dia-del-mes mes dia-de-semana
```

Ejemplos:

- `0 0 2 * * ?`: todos los dias a las 02:00.
- `0 0 7 * * ?`: todos los dias a las 07:00.
- `0 30 0 1 * ?`: dia 1 de cada mes a las 00:30.

## Ejecucion manual disponible

Hay endpoints manuales para algunos procesos en `Controllers/HR/ScheduledJobsController.cs`.

| Endpoint | Funcion |
|---|---|
| `POST /api/v1/rh/scheduled-jobs/contract-expiration/run` | Ejecuta manualmente el proceso de contratos vencidos. |
| `POST /api/v1/rh/scheduled-jobs/student-enrollment/run?periodCode=...&previousPeriod=...` | Ejecuta manualmente la sincronizacion de matriculas. |

Estos endpoints no dependen de esperar el trigger cron.

## Consideraciones

- Los jobs usan `[DisallowConcurrentExecution]` para evitar ejecuciones concurrentes del mismo job.
- `DailyAccrueVacationBalance` se ejecuta en background y usa `ICurrentUserService.EmployeeId`; en un proceso sin request HTTP ese valor normalmente puede ser `null`.
- La documentacion debe mantenerse alineada con `QuartzConfiguration.cs`; ese archivo es la fuente real de jobs activos.

## Checklist de diagnostico

1. Confirmar `Quartz:EnableJobs`.
2. Confirmar ambiente cargado por la aplicacion.
3. Buscar `JOB_START`, `JOB_OK` o `JOB_FAIL` en logs, **o consultar directo
   `SELECT TOP 20 * FROM HR.tbl_JobExecutionLog ORDER BY StartedAt DESC;`**
   (cubre tanto jobs de Quartz como el de SQL Server Agent, sin depender de
   tener acceso a los archivos de log del servidor de aplicaciones).
4. Confirmar que el job esta registrado en `QuartzConfiguration.cs` —
   **excepto `DailyAttendanceCalculationJob`, que vive en SQL Server Agent
   desde 2026-07-03** (ver seccion arriba). Para ese, revisar el historial
   del job en SSMS (`msdb.dbo.sysjobhistory`) o `tbl_JobExecutionLog` con
   `Source = 'SQLAgent'`.
5. Confirmar cron y zona horaria (Quartz usa "America/Guayaquil" explicito;
   SQL Server Agent usa la hora del reloj del servidor SQL, sin conversion).
6. Revisar errores `JOB_FAIL` o errores de inyeccion de dependencias.

Ultima actualizacion: 2026-07-03.
