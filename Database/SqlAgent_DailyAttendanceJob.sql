/* ============================================================
   SQL SERVER AGENT — Job de cálculo diario de asistencia
   ============================================================
   Reemplaza al job Quartz "DailyAttendanceCalculationJob"
   (comentado en Infrastructure/DependencyInjection/QuartzConfiguration.cs).

   Motivo de la migración: el job corría dentro del proceso de la app
   (IIS/app pool), por lo que si el pool estaba dormido o reciclado a
   las 07:00 el job simplemente no disparaba. Al vivir en SQL Server
   Agent, se ejecuta directo en el motor de base de datos sin depender
   de que la aplicación esté despierta.

   Este script:
     1) Elimina el job si ya existe (idempotente, seguro de re-ejecutar).
     2) Lo vuelve a crear con un único step T-SQL que llama a
        HR.sp_ProcessAttendanceRunRange, envuelto en el logging de
        HR.sp_JobExecutionLog_Start / HR.sp_JobExecutionLog_Finish
        (Fase 0 de esta migración).
     3) Programa el step diariamente a las 07:00 (hora del SERVIDOR SQL).
     4) Adjunta el job al servidor local.

   IMPORTANTE — ZONA HORARIA:
   SQL Server Agent no tiene el concepto de zona horaria "America/Guayaquil"
   que sí maneja Quartz.NET internamente. El horario "07:00" de abajo se
   interpreta literalmente como la hora del RELOJ DEL SERVIDOR SQL. Este
   script asume que el servidor SQL está configurado en hora de
   Ecuador/Guayaquil (UTC-5). Si el servidor corre en otra zona horaria
   (ej. UTC, o el servidor de BD está en otro datacenter), hay que ajustar
   manualmente el valor de @active_start_time más abajo antes de desplegar.

   IMPORTANTE — EJECUCIÓN:
   Este script NO se ejecuta desde aquí (no hay conexión a la BD de
   producción en este entorno). Se entrega para revisión y despliegue
   manual en SSMS por el DBA/usuario responsable.
   ============================================================ */

USE msdb;
GO

/* ------------------------------------------------------------
   1) IDEMPOTENCIA: si el job ya existe, se elimina por completo
      (incluye sus steps y schedules asociados) para recrearlo
      limpio a continuación. Evita quedar en un estado mixto si
      se corre este script más de una vez o tras un cambio.
   ------------------------------------------------------------ */
IF EXISTS (
    SELECT 1 FROM msdb.dbo.sysjobs
    WHERE name = N'HR_DailyAttendanceCalculation'
)
BEGIN
    EXEC msdb.dbo.sp_delete_job
        @job_name = N'HR_DailyAttendanceCalculation',
        @delete_unused_schedule = 1;
END
GO

/* ------------------------------------------------------------
   2) CREAR EL JOB
   ------------------------------------------------------------ */
EXEC msdb.dbo.sp_add_job
    @job_name         = N'HR_DailyAttendanceCalculation',
    @enabled          = 1,
    @description      = N'Calcula la asistencia (atrasos, horas extra, ausencias) del día anterior '
                       + N'ejecutando HR.sp_ProcessAttendanceRunRange. Reemplaza al job Quartz '
                       + N'"DailyAttendanceCalculationJob". Registra su ejecución en HR.tbl_JobExecutionLog.',
    @category_name    = N'[Uncategorized (Local)]',
    @owner_login_name = N'sa';
GO

/* ------------------------------------------------------------
   3) STEP ÚNICO: T-SQL que llama al pipeline de asistencia,
      envuelto en el logging de Fase 0 (Start/Finish) con
      manejo de errores vía TRY/CATCH + THROW para que el job
      de Agent quede marcado como fallido si algo se rompe.

      NOTA: @database_name se tomó de la connection string "SqlServerConn"
      en appsettings.json ("Database=dbutasystem"). appsettings.Production.json
      no la sobreescribe en este repo, pero en despliegue real la cadena de
      conexión suele venir de una variable de entorno que NO está en el
      repositorio — CONFIRMAR con el DBA que 'dbutasystem' sigue siendo el
      nombre real de la base de producción antes de ejecutar este script.
   ------------------------------------------------------------ */
DECLARE @JobStepCommand NVARCHAR(MAX) = N'
DECLARE @LogID BIGINT, @ErrorMsg NVARCHAR(MAX), @FromDate DATE = CAST(DATEADD(DAY,-1,GETDATE()) AS DATE);
EXEC HR.sp_JobExecutionLog_Start @JobName = N''HR_DailyAttendanceCalculation'', @Source = N''SQLAgent'', @LogID = @LogID OUTPUT;
BEGIN TRY
    EXEC HR.sp_ProcessAttendanceRunRange @FromDate = @FromDate, @ToDate = @FromDate, @Debug = 0;
    EXEC HR.sp_JobExecutionLog_Finish @LogID = @LogID, @Status = N''Success'', @ErrorMessage = NULL;
END TRY
BEGIN CATCH
    SET @ErrorMsg = ERROR_MESSAGE();
    EXEC HR.sp_JobExecutionLog_Finish @LogID = @LogID, @Status = N''Failed'', @ErrorMessage = @ErrorMsg;
    THROW;
END CATCH';

EXEC msdb.dbo.sp_add_jobstep
    @job_name           = N'HR_DailyAttendanceCalculation',
    @step_name          = N'RunDailyAttendanceCalculation',
    @step_id            = 1,
    @subsystem          = N'TSQL',
    @command            = @JobStepCommand,
    @database_name      = N'dbutasystem',  -- TODO: confirmar contra el nombre real de la BD de producción antes de desplegar (ver nota arriba)
    @retry_attempts     = 1,               -- 1 reintento automático si falla (ej. bloqueo transitorio de BD)
    @retry_interval     = 5,               -- minutos de espera antes del reintento
    @on_success_action  = 1,               -- 1 = Salir del job reportando éxito
    @on_fail_action     = 2;               -- 2 = Salir del job reportando fallo
GO

/* ------------------------------------------------------------
   4) SCHEDULE: diario a las 07:00 (hora del servidor SQL, ver
      nota de zona horaria al inicio del script).
   ------------------------------------------------------------ */
EXEC msdb.dbo.sp_add_schedule
    @schedule_name     = N'HR_DailyAttendanceCalculation_Schedule',
    @enabled            = 1,
    @freq_type          = 4,       -- 4 = Diario
    @freq_interval      = 1,       -- cada 1 día
    @freq_subday_type   = 1,       -- 1 = una sola vez ese día (no repetir dentro del día)
    @active_start_time  = 070000;  -- 07:00:00, formato HHMMSS
GO

EXEC msdb.dbo.sp_attach_schedule
    @job_name      = N'HR_DailyAttendanceCalculation',
    @schedule_name = N'HR_DailyAttendanceCalculation_Schedule';
GO

/* ------------------------------------------------------------
   5) ADJUNTAR EL JOB AL SERVIDOR LOCAL (obligatorio para que
      SQL Server Agent efectivamente lo ejecute).
   ------------------------------------------------------------ */
EXEC msdb.dbo.sp_add_jobserver
    @job_name    = N'HR_DailyAttendanceCalculation',
    @server_name = N'(local)';
GO
