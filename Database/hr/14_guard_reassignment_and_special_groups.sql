-- ============================================================
-- Extensión de esquema: reasignación de turnos y grupos especiales (doble turno)
-- Generado: 2026-07-23
--
-- 1) HR.tbl_GuardRotationGroups.IsSpecial: marca un grupo como "especial",
--    exceptuando a sus miembros de la restricción de un solo turno activo
--    por día (ver punto 3).
-- 2) HR.tbl_GuardShiftPlanning.AllowDoubleShift: se copia desde el grupo
--    especial del empleado al momento de crear/generar el turno.
-- 3) Redefinición de UX_GuardShiftPlanning_NoDoubleActiveShift para excluir
--    de la restricción de unicidad las filas con AllowDoubleShift=1.
-- 4) HR.tbl_GuardShiftChanges.NewWorkDate / NewLocationID: nuevo estado
--    (fecha/ubicación) para el botón "Reasignar" del Detalle del turno.
-- 5) Catálogo ref_Types: nuevo tipo de cambio REASSIGNMENT (Category=GUARD_CHANGE_TYPE).
--
-- Solo aditivo / idempotente — seguro de re-ejecutar.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) Grupo especial --------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardRotationGroups]') AND name = 'IsSpecial')
ALTER TABLE [HR].[tbl_GuardRotationGroups]
    ADD [IsSpecial] BIT NOT NULL CONSTRAINT [DF_GuardRotationGroups_IsSpecial] DEFAULT ((0));
GO

-- 2) Doble turno en la planificación ----------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]') AND name = 'AllowDoubleShift')
ALTER TABLE [HR].[tbl_GuardShiftPlanning]
    ADD [AllowDoubleShift] BIT NOT NULL CONSTRAINT [DF_GuardShiftPlanning_AllowDoubleShift] DEFAULT ((0));
GO

-- 3) Redefinir índice único de doble turno para excluir AllowDoubleShift=1 --
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GuardShiftPlanning_NoDoubleActiveShift' AND object_id = OBJECT_ID('[HR].[tbl_GuardShiftPlanning]'))
    DROP INDEX [UX_GuardShiftPlanning_NoDoubleActiveShift] ON [HR].[tbl_GuardShiftPlanning];
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_GuardShiftPlanning_NoDoubleActiveShift]
    ON [HR].[tbl_GuardShiftPlanning] ([EmployeeID], [WorkDate])
    WHERE [IsActiveForAssignment] = 1 AND [AllowDoubleShift] = 0;
GO

-- 4) Reasignación de turno (GuardShiftChanges) ------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftChanges]') AND name = 'NewWorkDate')
ALTER TABLE [HR].[tbl_GuardShiftChanges]
    ADD [NewWorkDate] DATE NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_GuardShiftChanges]') AND name = 'NewLocationID')
ALTER TABLE [HR].[tbl_GuardShiftChanges]
    ADD [NewLocationID] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GuardShiftChanges_NewLocation')
ALTER TABLE [HR].[tbl_GuardShiftChanges]
    ADD CONSTRAINT [FK_GuardShiftChanges_NewLocation]
    FOREIGN KEY ([NewLocationID]) REFERENCES [HR].[tbl_GuardServiceLocations] ([LocationID]);
GO

-- 5) Catálogo: tipo de cambio REASSIGNMENT ----------------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'GUARD_CHANGE_TYPE' AND [Name] = 'REASSIGNMENT')
INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive])
VALUES ('GUARD_CHANGE_TYPE', 'REASSIGNMENT', 'Reasignación del mismo guardia a otra fecha/horario/ubicación', 1);
GO
