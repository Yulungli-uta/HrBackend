/* =====================================================================
   65_occupational_model_01_structure.sql
   Reestructuración Cargo -> Escala (Grupo x Grado) — ETAPA A (solo agrega, compatible).
   Requiere haber ejecutado 65_occupational_model_00_backup.sql.
   - HR.tbl_Occupational_Groups sigue siendo la ESCALA (conserva nombre, ids y columnas).
   - Agrega la columna Level (nivel dentro del grupo; opcional).
   - RMU pasa a aceptar NULL: es solo referencial y hay escalas sin monto oficial
     (el sueldo real vive en HR.tbl_SalaryHistory).
   Idempotente.
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'HR.bak_Manifest_20261125', N'U') IS NULL
    THROW 50001, 'Falta el respaldo: ejecutar primero 65_occupational_model_00_backup.sql', 1;

IF COL_LENGTH(N'HR.tbl_Occupational_Groups', N'Level') IS NULL
    ALTER TABLE HR.tbl_Occupational_Groups ADD [Level] INT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'HR.tbl_Occupational_Groups') AND name = N'RMU' AND is_nullable = 0)
    ALTER TABLE HR.tbl_Occupational_Groups ALTER COLUMN RMU DECIMAL(10,2) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_tbl_Occupational_Groups_Level')
    ALTER TABLE HR.tbl_Occupational_Groups
        ADD CONSTRAINT CK_tbl_Occupational_Groups_Level CHECK ([Level] IS NULL OR [Level] BETWEEN 1 AND 99);
GO

-- Un nivel no se repite dentro de la misma familia (solo para escalas que lo usan)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OccupationalGroups_Family_Level' AND object_id = OBJECT_ID(N'HR.tbl_Occupational_Groups'))
    CREATE UNIQUE INDEX UX_OccupationalGroups_Family_Level
        ON HR.tbl_Occupational_Groups (FamilyTypeId, [Level])
        WHERE FamilyTypeId IS NOT NULL AND [Level] IS NOT NULL;
GO

SELECT 'estructura OK' AS Estado,
       COL_LENGTH(N'HR.tbl_Occupational_Groups', N'Level') AS LevelCol,
       (SELECT is_nullable FROM sys.columns WHERE object_id = OBJECT_ID(N'HR.tbl_Occupational_Groups') AND name = N'RMU') AS RmuNullable;
