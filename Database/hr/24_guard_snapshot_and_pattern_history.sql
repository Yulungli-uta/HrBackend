-- ============================================================
-- Extensión de esquema: snapshot histórico de planificación de guardias
-- y archivo de versiones de patrones de rotación
-- Generado: 2026-09-29
--
-- 1) HR.tbl_GuardShiftPlanning: columnas snapshot (nullable) que capturan
--    nombre/color de grupo, nombre de empleado y código/descripción de
--    horario en el momento en que se crea el turno — para que las vistas
--    históricas no cambien retroactivamente si el grupo/empleado/horario
--    se edita después. Filas existentes quedan en NULL (no se puede
--    reconstruir el valor histórico real; el código hace fallback al
--    dato en vivo para esas filas, igual que el comportamiento actual).
-- 2) HR.tbl_RotationPatternDetailHistory: archivo de los días de un
--    patrón justo antes de reemplazarlos al editar ("Configurar días").
--    No afecta la tabla activa RotationPatternDetails ni cómo la lee
--    la generación de turnos.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) Snapshot en GuardShiftPlanning --------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'GroupNameSnapshot')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [GroupNameSnapshot] NVARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'GroupColorSnapshot')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [GroupColorSnapshot] NVARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'EmployeeNameSnapshot')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [EmployeeNameSnapshot] NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'ScheduleCodeSnapshot')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [ScheduleCodeSnapshot] NVARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'ScheduleDescriptionSnapshot')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [ScheduleDescriptionSnapshot] NVARCHAR(150) NULL;
GO

-- 2) Archivo de versiones de detalle de patrón -----------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('HR') AND name = 'tbl_RotationPatternDetailHistory')
BEGIN
    CREATE TABLE [HR].[tbl_RotationPatternDetailHistory]
    (
        [HistoryId]       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PatternId]       INT NOT NULL,
        [PatternDetailId] INT NOT NULL,
        [DayOrder]        INT NOT NULL,
        [ScheduleId]      INT NULL,
        [IsRestDay]       BIT NOT NULL,
        [Notes]           NVARCHAR(300) NULL,
        [ArchivedAt]      DATETIME2 NOT NULL CONSTRAINT [DF_RotationPatternDetailHistory_ArchivedAt] DEFAULT (GETDATE()),
        [ArchivedBy]      INT NULL,
        CONSTRAINT [FK_RotationPatternDetailHistory_Pattern]
            FOREIGN KEY ([PatternId]) REFERENCES [HR].[tbl_RotationPatterns]([PatternID])
    );

    CREATE NONCLUSTERED INDEX [IX_RotationPatternDetailHistory_PatternId]
        ON [HR].[tbl_RotationPatternDetailHistory] ([PatternId], [ArchivedAt]);
END
GO
