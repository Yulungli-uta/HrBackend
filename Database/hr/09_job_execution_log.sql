-- ============================================================
-- JOB EXECUTION LOG : esquema [HR]
-- Generado: 2026-07-02
-- Registra el inicio/fin de cada ejecucion de job (Quartz y, a
-- futuro, SQL Server Agent) para trazabilidad y auditoria.
-- ============================================================

SET NOCOUNT ON;
GO

-- [tbl_JobExecutionLog]

IF OBJECT_ID('[HR].[tbl_JobExecutionLog]') IS NULL
CREATE TABLE [HR].[tbl_JobExecutionLog] (
    [JobLogID]     BIGINT IDENTITY(1,1) NOT NULL,
    [JobName]      NVARCHAR(200) NOT NULL,
    [Source]       NVARCHAR(20)  NOT NULL,
    [StartedAt]    DATETIME2     NOT NULL,
    [FinishedAt]   DATETIME2     NULL,
    [Status]       NVARCHAR(20)  NOT NULL DEFAULT ('Started'),
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [DurationMs]   INT           NULL,
    CONSTRAINT [PK_JobExecutionLog] PRIMARY KEY CLUSTERED ([JobLogID])
);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_JobExecutionLog_JobName_StartedAt'
      AND object_id = OBJECT_ID('[HR].[tbl_JobExecutionLog]')
)
CREATE INDEX [IX_JobExecutionLog_JobName_StartedAt]
    ON [HR].[tbl_JobExecutionLog] ([JobName], [StartedAt] DESC);
GO

-- [sp_JobExecutionLog_Start]

CREATE OR ALTER PROCEDURE HR.sp_JobExecutionLog_Start
(
    @JobName NVARCHAR(200),
    @Source  NVARCHAR(20),
    @LogID   BIGINT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO HR.tbl_JobExecutionLog (JobName, Source, StartedAt, Status)
    VALUES (@JobName, @Source, SYSUTCDATETIME(), 'Started');

    SET @LogID = SCOPE_IDENTITY();
END
GO

-- [sp_JobExecutionLog_Finish]

CREATE OR ALTER PROCEDURE HR.sp_JobExecutionLog_Finish
(
    @LogID        BIGINT,
    @Status       NVARCHAR(20),
    @ErrorMessage NVARCHAR(MAX) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE HR.tbl_JobExecutionLog
    SET FinishedAt   = SYSUTCDATETIME(),
        Status       = @Status,
        ErrorMessage = @ErrorMessage,
        DurationMs   = DATEDIFF(MILLISECOND, StartedAt, SYSUTCDATETIME())
    WHERE JobLogID = @LogID;
END
GO
