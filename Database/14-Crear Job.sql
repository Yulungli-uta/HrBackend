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