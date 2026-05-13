USE [msdb];
GO

DECLARE @jobName NVARCHAR(100) = N'Mantenimiento_Asistencia_Diario';
DECLARE @dbName NVARCHAR(100) = N'dbUtaSystem';
DECLARE @jobId BINARY(16);

-- 1. Crear el Job
EXEC dbo.sp_add_job 
    @job_name = @jobName, 
    @enabled = 1, 
    @description = N'Procesa la asistencia del día anterior todas las noches.',
    @job_id = @jobId OUTPUT;

-- 2. Agregar el Paso (Step)
-- Calculamos 'ayer' dinámicamente para pasarlo como @FromDate y @ToDate
EXEC sp_add_jobstep 
    @job_id = @jobId, 
    @step_name = N'Ejecutar_Procesamiento_Asistencia', 
    @subsystem = N'TSQL', 
    @command = N'DECLARE @ayer DATE = DATEADD(DAY, -1, GETDATE());
                 EXEC HR.sp_ProcessAttendanceRange @FromDate = @ayer, @ToDate = @ayer;', 
    @database_name = @dbName;

-- 3. Programación (Schedule)
-- Configurado para las 01:00 AM (cuando hay menos carga en el servidor)
EXEC dbo.sp_add_jobschedule 
    @job_id = @jobId, 
    @name = N'Ejecucion_Madrugada', 
    @freq_type = 4, -- Diario
    @freq_interval = 1, 
    @active_start_time = 010000; -- Formato HHMMSS (01:00:00)

-- 4. Asignar al servidor
EXEC dbo.sp_add_jobserver 
    @job_id = @jobId, 
    @server_name = N'(local)';
GO


/*visualizar los job creados */
SELECT 
    j.name AS [Nombre_del_Job],
    j.enabled AS [Activo],
    s.step_name AS [Nombre_del_Paso],
    s.database_name AS [Base_de_Datos],
    s.command AS [Comando_SQL],
    sched.name AS [Nombre_Horario],
    CASE 
        WHEN next_run_date > 0 THEN 
            CAST(CAST(next_run_date AS CHAR(8)) + ' ' + 
            STUFF(STUFF(RIGHT('000000' + CAST(next_run_time AS VARCHAR(6)), 6), 3, 0, ':'), 6, 0, ':') AS DATETIME)
        ELSE NULL 
    END AS [Proxima_Ejecucion]
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobsteps s ON j.job_id = s.job_id
LEFT JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
LEFT JOIN msdb.dbo.sysschedules sched ON js.schedule_id = sched.schedule_id
ORDER BY j.name;