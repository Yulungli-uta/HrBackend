USE dbUtaSystem;
GO

IF DB_NAME() <> N'dbUtaSystem' SET NOEXEC ON;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* =====================================================================================
   MODULO: GESTION DE GUARDIAS ROTATIVOS — SQL FINAL CORREGIDO
   MOTOR: SQL SERVER 2022
   BASE: sqlserver2022_modulo_guardias_rotativos_es.sql
   CORRECCIONES APLICADAS:
   - HR.ref_Types NO tiene SortOrder, CreatedBy, UpdatedBy, UpdatedAt.
     El INSERT solo usa columnas reales: Category, Name, Description, IsActive, CreatedAt.
   - Todas las demás FKs y nombres de columna verificados contra EF Core configurations.
   ===================================================================================== */

/* ─── Procedimiento temporal para MS_Description ─────────────────────────── */
CREATE OR ALTER PROCEDURE #SetMSDescription
    @SchemaName SYSNAME,
    @TableName  SYSNAME,
    @ColumnName SYSNAME = NULL,
    @Description NVARCHAR(4000)
AS
BEGIN
    SET NOCOUNT ON;
    IF @ColumnName IS NULL
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.extended_properties ep
            INNER JOIN sys.tables t ON ep.major_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE ep.name = N'MS_Description' AND s.name = @SchemaName
              AND t.name = @TableName AND ep.minor_id = 0
        )
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=@SchemaName,
                @level1type=N'TABLE',  @level1name=@TableName;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=@SchemaName,
                @level1type=N'TABLE',  @level1name=@TableName;
    END
    ELSE
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.extended_properties ep
            INNER JOIN sys.tables t  ON ep.major_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.columns c ON c.object_id = t.object_id AND c.column_id = ep.minor_id
            WHERE ep.name = N'MS_Description' AND s.name = @SchemaName
              AND t.name = @TableName AND c.name = @ColumnName
        )
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=@SchemaName,
                @level1type=N'TABLE',  @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=@SchemaName,
                @level1type=N'TABLE',  @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
    END
END;
GO

/* ─────────────────────────────────────────────────────────────────────────────
   SECCIÓN 1 — CATÁLOGOS EN HR.ref_Types
   CORRECCIÓN: ref_Types solo tiene (TypeID, Category, Name, Description, IsActive, CreatedAt)
               NO tiene SortOrder, CreatedBy, UpdatedBy, UpdatedAt.
   ───────────────────────────────────────────────────────────────────────────── */
PRINT N'Insertando catálogos del módulo de guardias en HR.ref_Types';
GO

-- ;WITH TypesToInsert AS (
    -- SELECT N'GUARD_LOCATION_TYPE'    AS Category, N'CAMPUS'                  AS Name, N'Campus o sede principal'                            AS Description UNION ALL
    -- SELECT N'GUARD_LOCATION_TYPE',               N'BUILDING',                N'Edificio dentro de una sede'                                              UNION ALL
    -- SELECT N'GUARD_LOCATION_TYPE',               N'ZONE',                    N'Zona operativa'                                                           UNION ALL
    -- SELECT N'GUARD_LOCATION_TYPE',               N'POST',                    N'Puesto de seguridad asignable'                                            UNION ALL
    -- SELECT N'GUARD_LOCATION_TYPE',               N'MONITORING',              N'Puesto o sala de monitoreo'                                               UNION ALL
    -- SELECT N'GUARD_LOCATION_TYPE',               N'ADMIN_SITE',              N'Sede administrativa o ubicación especial'                                 UNION ALL
    -- SELECT N'ROTATION_PATTERN_TYPE',             N'ROTATING',                N'Patrón de turnos rotativos'                                               UNION ALL
    -- SELECT N'ROTATION_PATTERN_TYPE',             N'FIXED',                   N'Patrón fijo'                                                              UNION ALL
    -- SELECT N'ROTATION_PATTERN_TYPE',             N'MONTHLY_ALTERNATING',     N'Patrón alternado por mes'                                                 UNION ALL
    -- SELECT N'GUARD_PLANNING_SOURCE',             N'AUTO',                    N'Planificación generada automáticamente'                                   UNION ALL
    -- SELECT N'GUARD_PLANNING_SOURCE',             N'MANUAL',                  N'Planificación creada manualmente'                                         UNION ALL
    -- SELECT N'GUARD_PLANNING_SOURCE',             N'IMPORT',                  N'Planificación importada desde archivo externo'                            UNION ALL
    -- SELECT N'GUARD_PLANNING_SOURCE',             N'ADJUSTMENT',              N'Planificación por ajuste administrativo'                                  UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'PLANNED',                 N'Turno planificado activo'                                                 UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'CHANGED',                 N'Turno modificado'                                                         UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'REPLACED',                N'Turno cubierto por reemplazo'                                             UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'CANCELLED',               N'Turno cancelado'                                                          UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'COMPLETED',               N'Turno cumplido'                                                           UNION ALL
    -- SELECT N'GUARD_PLANNING_STATUS',             N'ABSENT',                  N'Turno no cumplido'                                                        UNION ALL
    -- SELECT N'GUARD_CHANGE_TYPE',                 N'REPLACEMENT',             N'Reemplazo de turno'                                                       UNION ALL
    -- SELECT N'GUARD_CHANGE_TYPE',                 N'SWAP',                    N'Intercambio de turno'                                                     UNION ALL
    -- SELECT N'GUARD_CHANGE_TYPE',                 N'SCHEDULE_CHANGE',         N'Cambio de horario'                                                        UNION ALL
    -- SELECT N'GUARD_CHANGE_TYPE',                 N'COVERAGE',                N'Cobertura adicional o refuerzo'                                           UNION ALL
    -- SELECT N'GUARD_CHANGE_TYPE',                 N'EMERGENCY',               N'Cambio por emergencia'                                                    UNION ALL
    -- SELECT N'GUARD_CHANGE_STATUS',               N'PENDING',                 N'Solicitud pendiente'                                                      UNION ALL
    -- SELECT N'GUARD_CHANGE_STATUS',               N'APPROVED',                N'Solicitud aprobada'                                                       UNION ALL
    -- SELECT N'GUARD_CHANGE_STATUS',               N'REJECTED',                N'Solicitud rechazada'                                                      UNION ALL
    -- SELECT N'GUARD_CHANGE_STATUS',               N'CANCELLED',               N'Solicitud cancelada'                                                      UNION ALL
    -- SELECT N'GUARD_CHANGE_STATUS',               N'APPLIED',                 N'Cambio aplicado'                                                          UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'PERMISSION',              N'Bloqueo generado por permiso aprobado'                                    UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'VACATION',                N'Bloqueo generado por vacaciones aprobadas'                                UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'MEDICAL_LEAVE',           N'Bloqueo generado por licencia médica'                                     UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'MANUAL_BLOCK',            N'Bloqueo manual'                                                           UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'SUSPENSION',              N'Bloqueo por suspensión'                                                   UNION ALL
    -- SELECT N'GUARD_BLOCK_SOURCE',                N'TRAINING',                N'Bloqueo por capacitación'                                                 UNION ALL
    -- SELECT N'GUARD_BLOCK_STATUS',                N'ACTIVE',                  N'Bloqueo activo'                                                           UNION ALL
    -- SELECT N'GUARD_BLOCK_STATUS',                N'CANCELLED',               N'Bloqueo cancelado'                                                        UNION ALL
    -- SELECT N'GUARD_BLOCK_STATUS',                N'EXPIRED',                 N'Bloqueo vencido'                                                          UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'DOUBLE_SHIFT',            N'Doble turno por empleado y fecha'                                         UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'SCHEDULE_OVERLAP',        N'Cruce de horarios'                                                        UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'VACATION_CONFLICT',       N'Conflicto con vacaciones'                                                 UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'PERMISSION_CONFLICT',     N'Conflicto con permiso'                                                    UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'MIN_REST_NOT_MET',        N'Descanso mínimo no cumplido'                                              UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'INACTIVE_EMPLOYEE',       N'Empleado inactivo'                                                        UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'POST_COVERAGE_MISSING',   N'Puesto sin cobertura'                                                     UNION ALL
    -- SELECT N'GUARD_VALIDATION_TYPE',             N'REPLACEMENT_NOT_AVAILABLE', N'Reemplazante no disponible'                                             UNION ALL
    -- SELECT N'GUARD_VALIDATION_RESULT',           N'PASSED',                  N'Validación aprobada'                                                      UNION ALL
    -- SELECT N'GUARD_VALIDATION_RESULT',           N'WARNING',                 N'Validación con advertencia'                                               UNION ALL
    -- SELECT N'GUARD_VALIDATION_RESULT',           N'FAILED',                  N'Validación fallida'                                                       UNION ALL
    -- SELECT N'GUARD_VALIDATION_RESULT',           N'OVERRIDDEN',              N'Validación exceptuada por autorización'                                   UNION ALL
    -- SELECT N'GUARD_VALIDATION_SEVERITY',         N'INFO',                    N'Informativo'                                                              UNION ALL
    -- SELECT N'GUARD_VALIDATION_SEVERITY',         N'WARNING',                 N'Advertencia'                                                              UNION ALL
    -- SELECT N'GUARD_VALIDATION_SEVERITY',         N'BLOCKING',                N'Regla bloqueante'
-- )
-- INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, CreatedAt)
-- SELECT t.Category, t.Name, t.Description, 1, GETDATE()
-- FROM TypesToInsert t
-- WHERE NOT EXISTS (
    -- SELECT 1 FROM HR.ref_Types r WHERE r.Category = t.Category AND r.Name = t.Name
-- );
-- GO

;WITH TypesToInsert AS (
    -- GUARD_LOCATION_TYPE
    SELECT N'GUARD_LOCATION_TYPE' AS Category, N'CAMPUS' AS Name, N'Campus o sede principal' AS Description, 1 AS SortOrder UNION ALL
    SELECT N'GUARD_LOCATION_TYPE', N'BUILDING', N'Edificio dentro de una sede', 2 UNION ALL
    SELECT N'GUARD_LOCATION_TYPE', N'ZONE', N'Zona operativa', 3 UNION ALL
    SELECT N'GUARD_LOCATION_TYPE', N'POST', N'Puesto de seguridad asignable', 4 UNION ALL
    SELECT N'GUARD_LOCATION_TYPE', N'MONITORING', N'Puesto o sala de monitoreo', 5 UNION ALL
    SELECT N'GUARD_LOCATION_TYPE', N'ADMIN_SITE', N'Sede administrativa o ubicación especial', 6 UNION ALL
    
    -- ROTATION_PATTERN_TYPE
    SELECT N'ROTATION_PATTERN_TYPE', N'ROTATING', N'Patrón de turnos rotativos', 1 UNION ALL
    SELECT N'ROTATION_PATTERN_TYPE', N'FIXED', N'Patrón fijo', 2 UNION ALL
    SELECT N'ROTATION_PATTERN_TYPE', N'MONTHLY_ALTERNATING', N'Patrón alternado por mes', 3 UNION ALL
    
    -- GUARD_PLANNING_SOURCE
    SELECT N'GUARD_PLANNING_SOURCE', N'AUTO', N'Planificación generada automáticamente', 1 UNION ALL
    SELECT N'GUARD_PLANNING_SOURCE', N'MANUAL', N'Planificación creada manualmente', 2 UNION ALL
    SELECT N'GUARD_PLANNING_SOURCE', N'IMPORT', N'Planificación importada desde archivo externo', 3 UNION ALL
    SELECT N'GUARD_PLANNING_SOURCE', N'ADJUSTMENT', N'Planificación por ajuste administrativo', 4 UNION ALL
    
    -- GUARD_PLANNING_STATUS
    SELECT N'GUARD_PLANNING_STATUS', N'PLANNED', N'Turno planificado activo', 1 UNION ALL
    SELECT N'GUARD_PLANNING_STATUS', N'CHANGED', N'Turno modificado', 2 UNION ALL
    SELECT N'GUARD_PLANNING_STATUS', N'REPLACED', N'Turno cubierto por reemplazo', 3 UNION ALL
    SELECT N'GUARD_PLANNING_STATUS', N'CANCELLED', N'Turno cancelado', 4 UNION ALL
    SELECT N'GUARD_PLANNING_STATUS', N'COMPLETED', N'Turno cumplido', 5 UNION ALL
    SELECT N'GUARD_PLANNING_STATUS', N'ABSENT', N'Turno no cumplido', 6 UNION ALL
    
    -- GUARD_CHANGE_TYPE
    SELECT N'GUARD_CHANGE_TYPE', N'REPLACEMENT', N'Reemplazo de turno', 1 UNION ALL
    SELECT N'GUARD_CHANGE_TYPE', N'SWAP', N'Intercambio de turno', 2 UNION ALL
    SELECT N'GUARD_CHANGE_TYPE', N'SCHEDULE_CHANGE', N'Cambio de horario', 3 UNION ALL
    SELECT N'GUARD_CHANGE_TYPE', N'COVERAGE', N'Cobertura adicional o refuerzo', 4 UNION ALL
    SELECT N'GUARD_CHANGE_TYPE', N'EMERGENCY', N'Cambio por emergencia', 5 UNION ALL
    
    -- GUARD_CHANGE_STATUS
    SELECT N'GUARD_CHANGE_STATUS', N'PENDING', N'Solicitud pendiente', 1 UNION ALL
    SELECT N'GUARD_CHANGE_STATUS', N'APPROVED', N'Solicitud aprobada', 2 UNION ALL
    SELECT N'GUARD_CHANGE_STATUS', N'REJECTED', N'Solicitud rechazada', 3 UNION ALL
    SELECT N'GUARD_CHANGE_STATUS', N'CANCELLED', N'Solicitud cancelada', 4 UNION ALL
    SELECT N'GUARD_CHANGE_STATUS', N'APPLIED', N'Cambio aplicado', 5 UNION ALL
    
    -- GUARD_BLOCK_SOURCE
    SELECT N'GUARD_BLOCK_SOURCE', N'PERMISSION', N'Bloqueo generado por permiso aprobado', 1 UNION ALL
    SELECT N'GUARD_BLOCK_SOURCE', N'VACATION', N'Bloqueo generado por vacaciones aprobadas', 2 UNION ALL
    SELECT N'GUARD_BLOCK_SOURCE', N'MEDICAL_LEAVE', N'Bloqueo generado por licencia médica', 3 UNION ALL
    SELECT N'GUARD_BLOCK_SOURCE', N'MANUAL_BLOCK', N'Bloqueo manual', 4 UNION ALL
    SELECT N'GUARD_BLOCK_SOURCE', N'SUSPENSION', N'Bloqueo por suspensión', 5 UNION ALL
    SELECT N'GUARD_BLOCK_SOURCE', N'TRAINING', N'Bloqueo por capacitación', 6 UNION ALL
    
    -- GUARD_BLOCK_STATUS
    SELECT N'GUARD_BLOCK_STATUS', N'ACTIVE', N'Bloqueo activo', 1 UNION ALL
    SELECT N'GUARD_BLOCK_STATUS', N'CANCELLED', N'Bloqueo cancelado', 2 UNION ALL
    SELECT N'GUARD_BLOCK_STATUS', N'EXPIRED', N'Bloqueo vencido', 3 UNION ALL
    
    -- GUARD_VALIDATION_TYPE
    SELECT N'GUARD_VALIDATION_TYPE', N'DOUBLE_SHIFT', N'Doble turno por empleado y fecha', 1 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'SCHEDULE_OVERLAP', N'Cruce de horarios', 2 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'VACATION_CONFLICT', N'Conflicto con vacaciones', 3 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'PERMISSION_CONFLICT', N'Conflicto con permiso', 4 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'MIN_REST_NOT_MET', N'Descanso mínimo no cumplido', 5 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'INACTIVE_EMPLOYEE', N'Empleado inactivo', 6 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'POST_COVERAGE_MISSING', N'Puesto sin cobertura', 7 UNION ALL
    SELECT N'GUARD_VALIDATION_TYPE', N'REPLACEMENT_NOT_AVAILABLE', N'Reemplazante no disponible', 8 UNION ALL
    
    -- GUARD_VALIDATION_RESULT
    SELECT N'GUARD_VALIDATION_RESULT', N'PASSED', N'Validación aprobada', 1 UNION ALL
    SELECT N'GUARD_VALIDATION_RESULT', N'WARNING', N'Validación con advertencia', 2 UNION ALL
    SELECT N'GUARD_VALIDATION_RESULT', N'FAILED', N'Validación fallida', 3 UNION ALL
    SELECT N'GUARD_VALIDATION_RESULT', N'OVERRIDDEN', N'Validación exceptuada por autorización', 4 UNION ALL
    
    -- GUARD_VALIDATION_SEVERITY
    SELECT N'GUARD_VALIDATION_SEVERITY', N'INFO', N'Informativo', 1 UNION ALL
    SELECT N'GUARD_VALIDATION_SEVERITY', N'WARNING', N'Advertencia', 2 UNION ALL
    SELECT N'GUARD_VALIDATION_SEVERITY', N'BLOCKING', N'Regla bloqueante', 3
)
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive, SortOrder, CreatedAt)
SELECT t.Category, t.Name, t.Description, 1, t.SortOrder, GETDATE()
FROM TypesToInsert t
WHERE NOT EXISTS (
    SELECT 1 FROM HR.ref_Types r 
    WHERE r.Category = t.Category AND r.Name = t.Name
);
GO

/* ─────────────────────────────────────────────────────────────────────────────
   SECCIÓN 2 — AJUSTES A TABLAS EXISTENTES
   ───────────────────────────────────────────────────────────────────────────── */
PRINT N'Ajustando HR.tbl_Schedules';
GO
IF COL_LENGTH('HR.tbl_Schedules', 'ScheduleCode') IS NULL
    ALTER TABLE HR.tbl_Schedules ADD ScheduleCode NVARCHAR(20) NULL;
GO
IF COL_LENGTH('HR.tbl_Schedules', 'CrossesMidnight') IS NULL
    ALTER TABLE HR.tbl_Schedules ADD CrossesMidnight BIT NOT NULL
        CONSTRAINT DF_tbl_Schedules_CrossesMidnight DEFAULT (0);
GO
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_tbl_Schedules_ScheduleCode'
      AND object_id = OBJECT_ID(N'HR.tbl_Schedules')
)
    CREATE INDEX IX_tbl_Schedules_ScheduleCode
        ON HR.tbl_Schedules (ScheduleCode)
        WHERE ScheduleCode IS NOT NULL;
GO

PRINT N'Ajustando HR.tbl_AttendanceCalculations para guardias';
GO
IF COL_LENGTH('HR.tbl_AttendanceCalculations', 'GuardShiftPlanningID') IS NULL
    ALTER TABLE HR.tbl_AttendanceCalculations ADD GuardShiftPlanningID INT NULL;
GO
IF COL_LENGTH('HR.tbl_AttendanceCalculations', 'GuardShiftChangeID') IS NULL
    ALTER TABLE HR.tbl_AttendanceCalculations ADD GuardShiftChangeID INT NULL;
GO
IF COL_LENGTH('HR.tbl_AttendanceCalculations', 'OriginalEmployeeID') IS NULL
    ALTER TABLE HR.tbl_AttendanceCalculations ADD OriginalEmployeeID INT NULL;
GO
IF COL_LENGTH('HR.tbl_AttendanceCalculations', 'EffectiveEmployeeID') IS NULL
    ALTER TABLE HR.tbl_AttendanceCalculations ADD EffectiveEmployeeID INT NULL;
GO
IF COL_LENGTH('HR.tbl_AttendanceCalculations', 'IsReplacement') IS NULL
    ALTER TABLE HR.tbl_AttendanceCalculations ADD IsReplacement BIT NOT NULL
        CONSTRAINT DF_AttendanceCalculations_IsReplacement DEFAULT (0);
GO

/* ─────────────────────────────────────────────────────────────────────────────
   SECCIÓN 3 — TABLAS NUEVAS
   Orden: Locations → Groups → GroupEmployees → Patterns → PatternDetails →
          GroupPatterns → CoverageRequirements → ShiftPlanning → ShiftChanges →
          AvailabilityBlocks → AssignmentValidations
   ───────────────────────────────────────────────────────────────────────────── */

/* ── HR.tbl_GuardServiceLocations ── */
PRINT N'Creando HR.tbl_GuardServiceLocations';
GO
IF OBJECT_ID(N'HR.tbl_GuardServiceLocations', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardServiceLocations (
        LocationID      INT           IDENTITY(1,1) NOT NULL,
        ParentLocationID INT          NULL,
        RootLocationID  INT           NULL,
        LocationTypeID  INT           NOT NULL,
        LocationCode    NVARCHAR(30)  NULL,
        LocationName    NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(500) NULL,
        LocationPath    NVARCHAR(900) NULL,
        [Level]         INT           NOT NULL CONSTRAINT DF_GuardServiceLocations_Level        DEFAULT (0),
        RequiresCoverage BIT          NOT NULL CONSTRAINT DF_GuardServiceLocations_Requires     DEFAULT (0),
        IsAssignable    BIT           NOT NULL CONSTRAINT DF_GuardServiceLocations_IsAssignable DEFAULT (0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_GuardServiceLocations_IsActive     DEFAULT (1),
        CreatedBy       INT           NULL,
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_GuardServiceLocations_CreatedAt    DEFAULT (GETDATE()),
        UpdatedBy       INT           NULL,
        UpdatedAt       DATETIME2     NULL,
        RowVersion      ROWVERSION,
        CONSTRAINT PK_GuardServiceLocations PRIMARY KEY CLUSTERED (LocationID),
        CONSTRAINT FK_GuardServiceLocations_Parent      FOREIGN KEY (ParentLocationID) REFERENCES HR.tbl_GuardServiceLocations(LocationID),
        CONSTRAINT FK_GuardServiceLocations_Root        FOREIGN KEY (RootLocationID)   REFERENCES HR.tbl_GuardServiceLocations(LocationID),
        CONSTRAINT FK_GuardServiceLocations_LocationType FOREIGN KEY (LocationTypeID)  REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardServiceLocations_CreatedBy   FOREIGN KEY (CreatedBy)        REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardServiceLocations_UpdatedBy   FOREIGN KEY (UpdatedBy)        REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_GuardServiceLocations_Level CHECK ([Level] >= 0)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardServiceLocations_Parent'     AND object_id=OBJECT_ID(N'HR.tbl_GuardServiceLocations'))
    CREATE INDEX IX_GuardServiceLocations_Parent ON HR.tbl_GuardServiceLocations (ParentLocationID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardServiceLocations_Root'       AND object_id=OBJECT_ID(N'HR.tbl_GuardServiceLocations'))
    CREATE INDEX IX_GuardServiceLocations_Root ON HR.tbl_GuardServiceLocations (RootLocationID, IsActive)
        INCLUDE (LocationID, LocationName, LocationCode, IsAssignable);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardServiceLocations_Assignable' AND object_id=OBJECT_ID(N'HR.tbl_GuardServiceLocations'))
    CREATE INDEX IX_GuardServiceLocations_Assignable ON HR.tbl_GuardServiceLocations (IsAssignable, IsActive)
        INCLUDE (LocationID, LocationName, LocationCode, RootLocationID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_GuardServiceLocations_Code'       AND object_id=OBJECT_ID(N'HR.tbl_GuardServiceLocations'))
    CREATE UNIQUE INDEX UX_GuardServiceLocations_Code ON HR.tbl_GuardServiceLocations (LocationCode)
        WHERE LocationCode IS NOT NULL AND IsActive = 1;
GO

/* ── HR.tbl_GuardRotationGroups ── */
PRINT N'Creando HR.tbl_GuardRotationGroups';
GO
IF OBJECT_ID(N'HR.tbl_GuardRotationGroups', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardRotationGroups (
        GroupID     INT           IDENTITY(1,1) NOT NULL,
        GroupCode   NVARCHAR(30)  NULL,
        Name        NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_GuardRotationGroups_IsActive    DEFAULT (1),
        CreatedBy   INT           NULL,
        CreatedAt   DATETIME2     NOT NULL CONSTRAINT DF_GuardRotationGroups_CreatedAt   DEFAULT (GETDATE()),
        UpdatedBy   INT           NULL,
        UpdatedAt   DATETIME2     NULL,
        RowVersion  ROWVERSION,
        CONSTRAINT PK_GuardRotationGroups PRIMARY KEY CLUSTERED (GroupID),
        CONSTRAINT FK_GuardRotationGroups_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardRotationGroups_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees(EmployeeID)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_GuardRotationGroups_GroupCode' AND object_id=OBJECT_ID(N'HR.tbl_GuardRotationGroups'))
    CREATE UNIQUE INDEX UX_GuardRotationGroups_GroupCode ON HR.tbl_GuardRotationGroups (GroupCode)
        WHERE GroupCode IS NOT NULL AND IsActive = 1;
GO

/* ── HR.tbl_GuardRotationGroupEmployees ── */
PRINT N'Creando HR.tbl_GuardRotationGroupEmployees';
GO
IF OBJECT_ID(N'HR.tbl_GuardRotationGroupEmployees', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardRotationGroupEmployees (
        GroupEmployeeID INT       IDENTITY(1,1) NOT NULL,
        GroupID         INT       NOT NULL,
        EmployeeID      INT       NOT NULL,
        ValidFrom       DATE      NOT NULL,
        ValidTo         DATE      NULL,
        IsActive        BIT       NOT NULL CONSTRAINT DF_GuardRotationGroupEmployees_IsActive  DEFAULT (1),
        Notes           NVARCHAR(500) NULL,
        CreatedBy       INT       NULL,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_GuardRotationGroupEmployees_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy       INT       NULL,
        UpdatedAt       DATETIME2 NULL,
        RowVersion      ROWVERSION,
        CONSTRAINT PK_GuardRotationGroupEmployees PRIMARY KEY CLUSTERED (GroupEmployeeID),
        CONSTRAINT FK_GuardRotGroupEmp_Group     FOREIGN KEY (GroupID)    REFERENCES HR.tbl_GuardRotationGroups(GroupID),
        CONSTRAINT FK_GuardRotGroupEmp_Employee  FOREIGN KEY (EmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardRotGroupEmp_CreatedBy FOREIGN KEY (CreatedBy)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardRotGroupEmp_UpdatedBy FOREIGN KEY (UpdatedBy)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_GuardRotGroupEmp_Dates CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardRotGroupEmp_Group'    AND object_id=OBJECT_ID(N'HR.tbl_GuardRotationGroupEmployees'))
    CREATE INDEX IX_GuardRotGroupEmp_Group ON HR.tbl_GuardRotationGroupEmployees (GroupID, IsActive)
        INCLUDE (EmployeeID, ValidFrom, ValidTo);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardRotGroupEmp_Employee' AND object_id=OBJECT_ID(N'HR.tbl_GuardRotationGroupEmployees'))
    CREATE INDEX IX_GuardRotGroupEmp_Employee ON HR.tbl_GuardRotationGroupEmployees (EmployeeID, ValidFrom DESC)
        INCLUDE (GroupID, ValidTo, IsActive);
GO

/* ── HR.tbl_RotationPatterns ── */
PRINT N'Creando HR.tbl_RotationPatterns';
GO
IF OBJECT_ID(N'HR.tbl_RotationPatterns', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_RotationPatterns (
        PatternID      INT           IDENTITY(1,1) NOT NULL,
        PatternCode    NVARCHAR(30)  NULL,
        Name           NVARCHAR(150) NOT NULL,
        Description    NVARCHAR(500) NULL,
        PatternTypeID  INT           NOT NULL,
        CycleDays      INT           NOT NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_RotationPatterns_IsActive  DEFAULT (1),
        CreatedBy      INT           NULL,
        CreatedAt      DATETIME2     NOT NULL CONSTRAINT DF_RotationPatterns_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy      INT           NULL,
        UpdatedAt      DATETIME2     NULL,
        RowVersion     ROWVERSION,
        CONSTRAINT PK_RotationPatterns            PRIMARY KEY CLUSTERED (PatternID),
        CONSTRAINT FK_RotationPatterns_Type       FOREIGN KEY (PatternTypeID) REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_RotationPatterns_CreatedBy  FOREIGN KEY (CreatedBy)     REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_RotationPatterns_UpdatedBy  FOREIGN KEY (UpdatedBy)     REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_RotationPatterns_CycleDays CHECK (CycleDays > 0)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_RotationPatterns_PatternCode' AND object_id=OBJECT_ID(N'HR.tbl_RotationPatterns'))
    CREATE UNIQUE INDEX UX_RotationPatterns_PatternCode ON HR.tbl_RotationPatterns (PatternCode)
        WHERE PatternCode IS NOT NULL AND IsActive = 1;
GO

/* ── HR.tbl_RotationPatternDetails ── */
PRINT N'Creando HR.tbl_RotationPatternDetails';
GO
IF OBJECT_ID(N'HR.tbl_RotationPatternDetails', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_RotationPatternDetails (
        PatternDetailID INT           IDENTITY(1,1) NOT NULL,
        PatternID       INT           NOT NULL,
        DayOrder        INT           NOT NULL,
        ScheduleID      INT           NULL,
        IsRestDay       BIT           NOT NULL CONSTRAINT DF_RotationPatternDetails_IsRestDay DEFAULT (0),
        Notes           NVARCHAR(300) NULL,
        CreatedBy       INT           NULL,
        CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_RotationPatternDetails_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy       INT           NULL,
        UpdatedAt       DATETIME2     NULL,
        RowVersion      ROWVERSION,
        CONSTRAINT PK_RotationPatternDetails            PRIMARY KEY CLUSTERED (PatternDetailID),
        CONSTRAINT FK_RotationPatternDetails_Pattern    FOREIGN KEY (PatternID)  REFERENCES HR.tbl_RotationPatterns(PatternID),
        CONSTRAINT FK_RotationPatternDetails_Schedule   FOREIGN KEY (ScheduleID) REFERENCES HR.tbl_Schedules(ScheduleID),
        CONSTRAINT FK_RotationPatternDetails_CreatedBy  FOREIGN KEY (CreatedBy)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_RotationPatternDetails_UpdatedBy  FOREIGN KEY (UpdatedBy)  REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT UQ_RotationPatternDetails_PatternDay UNIQUE (PatternID, DayOrder),
        CONSTRAINT CK_RotationPatternDetails_DayOrder CHECK (DayOrder > 0),
        CONSTRAINT CK_RotationPatternDetails_ScheduleOrRest
            CHECK ((IsRestDay = 1 AND ScheduleID IS NULL) OR (IsRestDay = 0 AND ScheduleID IS NOT NULL))
    );
END;
GO

/* ── HR.tbl_GuardGroupRotationPatterns ── */
PRINT N'Creando HR.tbl_GuardGroupRotationPatterns';
GO
IF OBJECT_ID(N'HR.tbl_GuardGroupRotationPatterns', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardGroupRotationPatterns (
        GroupPatternID  INT       IDENTITY(1,1) NOT NULL,
        GroupID         INT       NOT NULL,
        PatternID       INT       NOT NULL,
        StartCycleDate  DATE      NOT NULL,
        ValidFrom       DATE      NOT NULL,
        ValidTo         DATE      NULL,
        IsActive        BIT       NOT NULL CONSTRAINT DF_GuardGroupRotPatterns_IsActive  DEFAULT (1),
        Notes           NVARCHAR(500) NULL,
        CreatedBy       INT       NULL,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_GuardGroupRotPatterns_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy       INT       NULL,
        UpdatedAt       DATETIME2 NULL,
        RowVersion      ROWVERSION,
        CONSTRAINT PK_GuardGroupRotationPatterns PRIMARY KEY CLUSTERED (GroupPatternID),
        CONSTRAINT FK_GuardGroupRotPat_Group     FOREIGN KEY (GroupID)   REFERENCES HR.tbl_GuardRotationGroups(GroupID),
        CONSTRAINT FK_GuardGroupRotPat_Pattern   FOREIGN KEY (PatternID) REFERENCES HR.tbl_RotationPatterns(PatternID),
        CONSTRAINT FK_GuardGroupRotPat_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardGroupRotPat_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_GuardGroupRotPat_Dates CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardGroupRotPat_Group' AND object_id=OBJECT_ID(N'HR.tbl_GuardGroupRotationPatterns'))
    CREATE INDEX IX_GuardGroupRotPat_Group ON HR.tbl_GuardGroupRotationPatterns (GroupID, IsActive, ValidFrom DESC)
        INCLUDE (PatternID, StartCycleDate, ValidTo);
GO

/* ── HR.tbl_GuardShiftCoverageRequirements ── */
PRINT N'Creando HR.tbl_GuardShiftCoverageRequirements';
GO
IF OBJECT_ID(N'HR.tbl_GuardShiftCoverageRequirements', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardShiftCoverageRequirements (
        RequirementID   INT       IDENTITY(1,1) NOT NULL,
        LocationID      INT       NOT NULL,
        ScheduleID      INT       NOT NULL,
        DayOfWeek       TINYINT   NOT NULL,
        RequiredGuards  INT       NOT NULL CONSTRAINT DF_GuardShiftCovReq_RequiredGuards DEFAULT (1),
        ValidFrom       DATE      NOT NULL,
        ValidTo         DATE      NULL,
        IsActive        BIT       NOT NULL CONSTRAINT DF_GuardShiftCovReq_IsActive       DEFAULT (1),
        Notes           NVARCHAR(500) NULL,
        CreatedBy       INT       NULL,
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_GuardShiftCovReq_CreatedAt      DEFAULT (GETDATE()),
        UpdatedBy       INT       NULL,
        UpdatedAt       DATETIME2 NULL,
        RowVersion      ROWVERSION,
        CONSTRAINT PK_GuardShiftCoverageRequirements PRIMARY KEY CLUSTERED (RequirementID),
        CONSTRAINT FK_GuardShiftCovReq_Location  FOREIGN KEY (LocationID)  REFERENCES HR.tbl_GuardServiceLocations(LocationID),
        CONSTRAINT FK_GuardShiftCovReq_Schedule  FOREIGN KEY (ScheduleID)  REFERENCES HR.tbl_Schedules(ScheduleID),
        CONSTRAINT FK_GuardShiftCovReq_CreatedBy FOREIGN KEY (CreatedBy)   REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftCovReq_UpdatedBy FOREIGN KEY (UpdatedBy)   REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_GuardShiftCovReq_DayOfWeek CHECK (DayOfWeek BETWEEN 1 AND 7),
        CONSTRAINT CK_GuardShiftCovReq_Guards    CHECK (RequiredGuards > 0),
        CONSTRAINT CK_GuardShiftCovReq_Dates     CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftCovReq_LocSchedDay' AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftCoverageRequirements'))
    CREATE INDEX IX_GuardShiftCovReq_LocSchedDay
        ON HR.tbl_GuardShiftCoverageRequirements (LocationID, ScheduleID, DayOfWeek, IsActive)
        INCLUDE (RequiredGuards, ValidFrom, ValidTo);
GO

/* ── HR.tbl_GuardShiftPlanning ── */
PRINT N'Creando HR.tbl_GuardShiftPlanning';
GO
IF OBJECT_ID(N'HR.tbl_GuardShiftPlanning', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardShiftPlanning (
        PlanningID             INT       IDENTITY(1,1) NOT NULL,
        EmployeeID             INT       NOT NULL,
        GroupID                INT       NULL,
        LocationID             INT       NOT NULL,
        WorkDate               DATE      NOT NULL,
        ScheduleID             INT       NOT NULL,
        PlanningSourceTypeID   INT       NOT NULL,
        StatusTypeID           INT       NOT NULL,
        IsAutoGenerated        BIT       NOT NULL CONSTRAINT DF_GuardShiftPlanning_IsAutoGenerated      DEFAULT (1),
        IsActiveForAssignment  BIT       NOT NULL CONSTRAINT DF_GuardShiftPlanning_IsActiveForAssignment DEFAULT (1),
        Notes                  NVARCHAR(500) NULL,
        CreatedBy              INT       NULL,
        CreatedAt              DATETIME2 NOT NULL CONSTRAINT DF_GuardShiftPlanning_CreatedAt             DEFAULT (GETDATE()),
        UpdatedBy              INT       NULL,
        UpdatedAt              DATETIME2 NULL,
        RowVersion             ROWVERSION,
        CONSTRAINT PK_GuardShiftPlanning            PRIMARY KEY CLUSTERED (PlanningID),
        CONSTRAINT FK_GuardShiftPlanning_Employee   FOREIGN KEY (EmployeeID)           REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftPlanning_Group      FOREIGN KEY (GroupID)              REFERENCES HR.tbl_GuardRotationGroups(GroupID),
        CONSTRAINT FK_GuardShiftPlanning_Location   FOREIGN KEY (LocationID)           REFERENCES HR.tbl_GuardServiceLocations(LocationID),
        CONSTRAINT FK_GuardShiftPlanning_Schedule   FOREIGN KEY (ScheduleID)           REFERENCES HR.tbl_Schedules(ScheduleID),
        CONSTRAINT FK_GuardShiftPlanning_SourceType FOREIGN KEY (PlanningSourceTypeID) REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardShiftPlanning_StatusType FOREIGN KEY (StatusTypeID)         REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardShiftPlanning_CreatedBy  FOREIGN KEY (CreatedBy)            REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftPlanning_UpdatedBy  FOREIGN KEY (UpdatedBy)            REFERENCES HR.tbl_Employees(EmployeeID)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftPlanning_WorkDate'           AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftPlanning'))
    CREATE INDEX IX_GuardShiftPlanning_WorkDate ON HR.tbl_GuardShiftPlanning (WorkDate)
        INCLUDE (EmployeeID, ScheduleID, LocationID, StatusTypeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftPlanning_EmployeeDate'       AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftPlanning'))
    CREATE INDEX IX_GuardShiftPlanning_EmployeeDate ON HR.tbl_GuardShiftPlanning (EmployeeID, WorkDate)
        INCLUDE (ScheduleID, LocationID, StatusTypeID, GroupID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftPlanning_LocationDateSched'  AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftPlanning'))
    CREATE INDEX IX_GuardShiftPlanning_LocationDateSched ON HR.tbl_GuardShiftPlanning (LocationID, WorkDate, ScheduleID)
        INCLUDE (EmployeeID, StatusTypeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_GuardShiftPlanning_NoDoubleActiveShift' AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftPlanning'))
    CREATE UNIQUE INDEX UX_GuardShiftPlanning_NoDoubleActiveShift
        ON HR.tbl_GuardShiftPlanning (EmployeeID, WorkDate)
        WHERE IsActiveForAssignment = 1;
GO

/* ── HR.tbl_GuardShiftChanges ── */
PRINT N'Creando HR.tbl_GuardShiftChanges';
GO
IF OBJECT_ID(N'HR.tbl_GuardShiftChanges', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardShiftChanges (
        ShiftChangeID        INT            IDENTITY(1,1) NOT NULL,
        PlanningID           INT            NOT NULL,
        OriginalEmployeeID   INT            NOT NULL,
        ReplacementEmployeeID INT           NULL,
        OriginalScheduleID   INT            NOT NULL,
        NewScheduleID        INT            NULL,
        ChangeTypeID         INT            NOT NULL,
        StatusTypeID         INT            NOT NULL,
        IsActiveForAttendance BIT           NOT NULL CONSTRAINT DF_GuardShiftChanges_IsActiveForAtt DEFAULT (0),
        Reason               NVARCHAR(1000) NOT NULL,
        RequestedBy          INT            NULL,
        RequestedAt          DATETIME2      NOT NULL CONSTRAINT DF_GuardShiftChanges_RequestedAt    DEFAULT (GETDATE()),
        ApprovedBy           INT            NULL,
        ApprovedAt           DATETIME2      NULL,
        RejectionReason      NVARCHAR(500)  NULL,
        CreatedBy            INT            NULL,
        CreatedAt            DATETIME2      NOT NULL CONSTRAINT DF_GuardShiftChanges_CreatedAt      DEFAULT (GETDATE()),
        UpdatedBy            INT            NULL,
        UpdatedAt            DATETIME2      NULL,
        RowVersion           ROWVERSION,
        CONSTRAINT PK_GuardShiftChanges                 PRIMARY KEY CLUSTERED (ShiftChangeID),
        CONSTRAINT FK_GuardShiftChanges_Planning        FOREIGN KEY (PlanningID)            REFERENCES HR.tbl_GuardShiftPlanning(PlanningID),
        CONSTRAINT FK_GuardShiftChanges_OrigEmp         FOREIGN KEY (OriginalEmployeeID)    REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftChanges_ReplEmp         FOREIGN KEY (ReplacementEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftChanges_OrigSched       FOREIGN KEY (OriginalScheduleID)    REFERENCES HR.tbl_Schedules(ScheduleID),
        CONSTRAINT FK_GuardShiftChanges_NewSched        FOREIGN KEY (NewScheduleID)         REFERENCES HR.tbl_Schedules(ScheduleID),
        CONSTRAINT FK_GuardShiftChanges_ChangeType      FOREIGN KEY (ChangeTypeID)          REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardShiftChanges_StatusType      FOREIGN KEY (StatusTypeID)          REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardShiftChanges_RequestedBy     FOREIGN KEY (RequestedBy)           REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftChanges_ApprovedBy      FOREIGN KEY (ApprovedBy)            REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftChanges_CreatedBy       FOREIGN KEY (CreatedBy)             REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardShiftChanges_UpdatedBy       FOREIGN KEY (UpdatedBy)             REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_GuardShiftChanges_DiffReplacement CHECK (ReplacementEmployeeID IS NULL OR ReplacementEmployeeID <> OriginalEmployeeID)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftChanges_Planning'    AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftChanges'))
    CREATE INDEX IX_GuardShiftChanges_Planning ON HR.tbl_GuardShiftChanges (PlanningID, StatusTypeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardShiftChanges_Replacement' AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftChanges'))
    CREATE INDEX IX_GuardShiftChanges_Replacement ON HR.tbl_GuardShiftChanges (ReplacementEmployeeID, StatusTypeID)
        INCLUDE (PlanningID, OriginalEmployeeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_GuardShiftChanges_OneActiveAtt' AND object_id=OBJECT_ID(N'HR.tbl_GuardShiftChanges'))
    CREATE UNIQUE INDEX UX_GuardShiftChanges_OneActiveAtt ON HR.tbl_GuardShiftChanges (PlanningID)
        WHERE IsActiveForAttendance = 1;
GO

/* ── HR.tbl_EmployeeAvailabilityBlocks ── */
PRINT N'Creando HR.tbl_EmployeeAvailabilityBlocks';
GO
IF OBJECT_ID(N'HR.tbl_EmployeeAvailabilityBlocks', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_EmployeeAvailabilityBlocks (
        BlockID        INT            IDENTITY(1,1) NOT NULL,
        EmployeeID     INT            NOT NULL,
        SourceTypeID   INT            NOT NULL,
        SourceTable    SYSNAME        NULL,
        SourceID       NVARCHAR(128)  NULL,
        StartDateTime  DATETIME2      NOT NULL,
        EndDateTime    DATETIME2      NOT NULL,
        StatusTypeID   INT            NOT NULL,
        Reason         NVARCHAR(500)  NULL,
        CreatedBy      INT            NULL,
        CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_EmpAvailBlocks_CreatedAt DEFAULT (GETDATE()),
        UpdatedBy      INT            NULL,
        UpdatedAt      DATETIME2      NULL,
        RowVersion     ROWVERSION,
        CONSTRAINT PK_EmployeeAvailabilityBlocks          PRIMARY KEY CLUSTERED (BlockID),
        CONSTRAINT FK_EmpAvailBlocks_Employee   FOREIGN KEY (EmployeeID)   REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_EmpAvailBlocks_SourceType FOREIGN KEY (SourceTypeID) REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_EmpAvailBlocks_StatusType FOREIGN KEY (StatusTypeID) REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_EmpAvailBlocks_CreatedBy  FOREIGN KEY (CreatedBy)    REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_EmpAvailBlocks_UpdatedBy  FOREIGN KEY (UpdatedBy)    REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT CK_EmpAvailBlocks_Dates CHECK (EndDateTime >= StartDateTime)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_EmpAvailBlocks_EmpDates' AND object_id=OBJECT_ID(N'HR.tbl_EmployeeAvailabilityBlocks'))
    CREATE INDEX IX_EmpAvailBlocks_EmpDates
        ON HR.tbl_EmployeeAvailabilityBlocks (EmployeeID, StartDateTime, EndDateTime, StatusTypeID)
        INCLUDE (SourceTypeID, SourceTable, SourceID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_EmpAvailBlocks_Source'    AND object_id=OBJECT_ID(N'HR.tbl_EmployeeAvailabilityBlocks'))
    CREATE INDEX IX_EmpAvailBlocks_Source ON HR.tbl_EmployeeAvailabilityBlocks (SourceTable, SourceID);
GO

/* ── HR.tbl_GuardAssignmentValidations ── */
PRINT N'Creando HR.tbl_GuardAssignmentValidations';
GO
IF OBJECT_ID(N'HR.tbl_GuardAssignmentValidations', N'U') IS NULL
BEGIN
    CREATE TABLE HR.tbl_GuardAssignmentValidations (
        ValidationID     BIGINT         IDENTITY(1,1) NOT NULL,
        EmployeeID       INT            NOT NULL,
        PlanningID       INT            NULL,
        ShiftChangeID    INT            NULL,
        ValidationTypeID INT            NOT NULL,
        ResultTypeID     INT            NOT NULL,
        SeverityTypeID   INT            NOT NULL,
        ValidationDate   DATETIME2      NOT NULL CONSTRAINT DF_GuardAssignValids_ValidationDate DEFAULT (GETDATE()),
        Message          NVARCHAR(1000) NOT NULL,
        Details          NVARCHAR(MAX)  NULL,
        CreatedBy        INT            NULL,
        CreatedAt        DATETIME2      NOT NULL CONSTRAINT DF_GuardAssignValids_CreatedAt      DEFAULT (GETDATE()),
        UpdatedBy        INT            NULL,
        UpdatedAt        DATETIME2      NULL,
        RowVersion       ROWVERSION,
        CONSTRAINT PK_GuardAssignmentValidations           PRIMARY KEY CLUSTERED (ValidationID),
        CONSTRAINT FK_GuardAssignValids_Employee           FOREIGN KEY (EmployeeID)       REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardAssignValids_Planning           FOREIGN KEY (PlanningID)       REFERENCES HR.tbl_GuardShiftPlanning(PlanningID),
        CONSTRAINT FK_GuardAssignValids_ShiftChange        FOREIGN KEY (ShiftChangeID)    REFERENCES HR.tbl_GuardShiftChanges(ShiftChangeID),
        CONSTRAINT FK_GuardAssignValids_ValidationType     FOREIGN KEY (ValidationTypeID) REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardAssignValids_ResultType         FOREIGN KEY (ResultTypeID)     REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardAssignValids_SeverityType       FOREIGN KEY (SeverityTypeID)   REFERENCES HR.ref_Types(TypeID),
        CONSTRAINT FK_GuardAssignValids_CreatedBy          FOREIGN KEY (CreatedBy)        REFERENCES HR.tbl_Employees(EmployeeID),
        CONSTRAINT FK_GuardAssignValids_UpdatedBy          FOREIGN KEY (UpdatedBy)        REFERENCES HR.tbl_Employees(EmployeeID)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GuardAssignValids_EmpDate' AND object_id=OBJECT_ID(N'HR.tbl_GuardAssignmentValidations'))
    CREATE INDEX IX_GuardAssignValids_EmpDate
        ON HR.tbl_GuardAssignmentValidations (EmployeeID, ValidationDate DESC)
        INCLUDE (ValidationTypeID, ResultTypeID, SeverityTypeID, PlanningID, ShiftChangeID);
GO

/* ─────────────────────────────────────────────────────────────────────────────
   SECCIÓN 4 — FKs DIFERIDAS EN HR.tbl_AttendanceCalculations
   (deben crearse después de tbl_GuardShiftPlanning y tbl_GuardShiftChanges)
   ───────────────────────────────────────────────────────────────────────────── */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AttCalc_GuardShiftPlanning')
    ALTER TABLE HR.tbl_AttendanceCalculations
        ADD CONSTRAINT FK_AttCalc_GuardShiftPlanning
            FOREIGN KEY (GuardShiftPlanningID) REFERENCES HR.tbl_GuardShiftPlanning(PlanningID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AttCalc_GuardShiftChange')
    ALTER TABLE HR.tbl_AttendanceCalculations
        ADD CONSTRAINT FK_AttCalc_GuardShiftChange
            FOREIGN KEY (GuardShiftChangeID) REFERENCES HR.tbl_GuardShiftChanges(ShiftChangeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AttCalc_OriginalEmployee')
    ALTER TABLE HR.tbl_AttendanceCalculations
        ADD CONSTRAINT FK_AttCalc_OriginalEmployee
            FOREIGN KEY (OriginalEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AttCalc_EffectiveEmployee')
    ALTER TABLE HR.tbl_AttendanceCalculations
        ADD CONSTRAINT FK_AttCalc_EffectiveEmployee
            FOREIGN KEY (EffectiveEmployeeID) REFERENCES HR.tbl_Employees(EmployeeID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AttCalc_GuardShiftPlanning' AND object_id=OBJECT_ID(N'HR.tbl_AttendanceCalculations'))
    CREATE INDEX IX_AttCalc_GuardShiftPlanning ON HR.tbl_AttendanceCalculations (GuardShiftPlanningID)
        WHERE GuardShiftPlanningID IS NOT NULL;
GO

/* ─────────────────────────────────────────────────────────────────────────────
   SECCIÓN 5 — DESCRIPCIONES MS_Description (resumen por tabla)
   ───────────────────────────────────────────────────────────────────────────── */
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', NULL,
    N'Ubicaciones recursivas de seguridad: campus, edificios, zonas, puestos y sitios de monitoreo.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'LocationID',       N'Identificador único de ubicación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'ParentLocationID', N'Ubicación padre en la jerarquía.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'RootLocationID',   N'Raíz de la jerarquía para consultas rápidas por campus.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'LocationTypeID',   N'Tipo de ubicación desde HR.ref_Types categoría GUARD_LOCATION_TYPE.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'LocationCode',     N'Código operativo. Ejemplo: 90, 90.1, HUA, REC.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'LocationName',     N'Nombre de la ubicación o puesto.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'LocationPath',     N'Ruta materializada. Ejemplo: /1/4/9/.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'Level',            N'Profundidad en la jerarquía.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'RequiresCoverage', N'Indica si la ubicación requiere cobertura operativa.';
EXEC #SetMSDescription N'HR', N'tbl_GuardServiceLocations', N'IsAssignable',     N'Indica si se puede asignar un guardia directamente a esta ubicación.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroups', NULL,          N'Grupos de rotación operativa de guardias.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroups', N'GroupID',    N'Identificador único del grupo.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroups', N'GroupCode',  N'Código corto del grupo.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroups', N'Name',       N'Nombre del grupo.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroupEmployees', NULL,             N'Historial de empleados asignados a grupos de rotación de guardias.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroupEmployees', N'GroupEmployeeID', N'Identificador único de asignación empleado-grupo.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroupEmployees', N'ValidFrom',       N'Fecha inicio de membresía.';
EXEC #SetMSDescription N'HR', N'tbl_GuardRotationGroupEmployees', N'ValidTo',         N'Fecha fin de membresía.';
GO
EXEC #SetMSDescription N'HR', N'tbl_RotationPatterns', NULL,           N'Patrones de rotación o fijos para generación de planificación.';
EXEC #SetMSDescription N'HR', N'tbl_RotationPatterns', N'PatternID',   N'Identificador único del patrón.';
EXEC #SetMSDescription N'HR', N'tbl_RotationPatterns', N'CycleDays',   N'Número de días del ciclo.';
GO
EXEC #SetMSDescription N'HR', N'tbl_RotationPatternDetails', NULL,            N'Detalle día a día de un patrón de rotación.';
EXEC #SetMSDescription N'HR', N'tbl_RotationPatternDetails', N'DayOrder',     N'Orden del día dentro del ciclo.';
EXEC #SetMSDescription N'HR', N'tbl_RotationPatternDetails', N'ScheduleID',   N'Horario asignado. Nulo cuando es día de descanso.';
EXEC #SetMSDescription N'HR', N'tbl_RotationPatternDetails', N'IsRestDay',    N'Indica si el día es día de descanso.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardGroupRotationPatterns', NULL,               N'Asignación de patrón a grupos de rotación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardGroupRotationPatterns', N'StartCycleDate',  N'Fecha base para cálculo del ciclo.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftCoverageRequirements', NULL,              N'Requerimientos mínimos de cobertura por ubicación, horario y día de semana.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftCoverageRequirements', N'DayOfWeek',     N'Día de semana: 1=Lunes a 7=Domingo.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftCoverageRequirements', N'RequiredGuards',N'Número mínimo requerido de guardias.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', NULL,                       N'Planificación real de guardias por empleado, fecha, horario y ubicación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'PlanningID',              N'Identificador único de planificación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'EmployeeID',              N'Empleado titular planificado.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'WorkDate',                N'Fecha operativa del turno.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'PlanningSourceTypeID',    N'Origen de la planificación desde HR.ref_Types categoría GUARD_PLANNING_SOURCE.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'StatusTypeID',            N'Estado de la planificación desde HR.ref_Types categoría GUARD_PLANNING_STATUS.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftPlanning', N'IsActiveForAssignment',   N'Control técnico para evitar doble turno activo por empleado y fecha.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftChanges', NULL,                      N'Cambios, reemplazos, coberturas e intercambios sobre un registro de planificación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftChanges', N'IsActiveForAttendance',  N'Indica si este cambio aprobado define quién debe marcar asistencia.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftChanges', N'OriginalEmployeeID',     N'Empleado titular original.';
EXEC #SetMSDescription N'HR', N'tbl_GuardShiftChanges', N'ReplacementEmployeeID',  N'Empleado que reemplaza o cubre el turno.';
GO
EXEC #SetMSDescription N'HR', N'tbl_EmployeeAvailabilityBlocks', NULL,             N'Bloqueos de disponibilidad de empleados por permisos aprobados, vacaciones u otros eventos.';
EXEC #SetMSDescription N'HR', N'tbl_EmployeeAvailabilityBlocks', N'SourceTypeID',  N'Tipo de origen desde HR.ref_Types categoría GUARD_BLOCK_SOURCE.';
EXEC #SetMSDescription N'HR', N'tbl_EmployeeAvailabilityBlocks', N'SourceTable',   N'Tabla origen. Ejemplo: HR.tbl_Permissions o HR.tbl_Vacations.';
EXEC #SetMSDescription N'HR', N'tbl_EmployeeAvailabilityBlocks', N'SourceID',      N'Identificador del registro origen.';
GO
EXEC #SetMSDescription N'HR', N'tbl_GuardAssignmentValidations', NULL,              N'Registro de validaciones, advertencias y bloqueos para asignaciones y reemplazos.';
EXEC #SetMSDescription N'HR', N'tbl_GuardAssignmentValidations', N'ValidationID',  N'Identificador único de validación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardAssignmentValidations', N'Message',       N'Mensaje funcional de validación.';
EXEC #SetMSDescription N'HR', N'tbl_GuardAssignmentValidations', N'Details',       N'Detalles técnicos opcionales o JSON.';
GO
EXEC #SetMSDescription N'HR', N'tbl_Schedules', N'ScheduleCode',     N'Código corto del turno. Ejemplo: M, T, N, L, 24H.';
EXEC #SetMSDescription N'HR', N'tbl_Schedules', N'CrossesMidnight',  N'Indica si el horario inicia un día y termina al día siguiente.';
GO
EXEC #SetMSDescription N'HR', N'tbl_AttendanceCalculations', N'GuardShiftPlanningID',  N'Registro de planificación de guardia usado en el cálculo de asistencia.';
EXEC #SetMSDescription N'HR', N'tbl_AttendanceCalculations', N'GuardShiftChangeID',    N'Reemplazo o cambio usado en el cálculo de asistencia.';
EXEC #SetMSDescription N'HR', N'tbl_AttendanceCalculations', N'OriginalEmployeeID',    N'Empleado titular asignado al turno.';
EXEC #SetMSDescription N'HR', N'tbl_AttendanceCalculations', N'EffectiveEmployeeID',   N'Empleado que efectivamente debía marcar asistencia.';
EXEC #SetMSDescription N'HR', N'tbl_AttendanceCalculations', N'IsReplacement',         N'Indica si el cálculo corresponde a un turno de reemplazo.';
GO

/* ─────────────────────────────────────────────────────────────────────────────
   NOTAS DE IMPLEMENTACIÓN
   1. HR.tbl_PermissionTypes NO se modifica.
      Todo permiso aprobado en el rango fecha/hora debe generar una fila ACTIVE
      en HR.tbl_EmployeeAvailabilityBlocks con source_type PERMISSION.
   2. Toda vacación aprobada en el rango debe generar una fila ACTIVE
      con source_type VACATION.
   3. Doble turno activo controlado por índice único filtrado
      UX_GuardShiftPlanning_NoDoubleActiveShift (EmployeeID, WorkDate) WHERE IsActiveForAssignment=1.
   4. Sin reemplazo aprobado: EffectiveEmployeeID = GuardShiftPlanning.EmployeeID.
      Con reemplazo aprobado IsActiveForAttendance=1:
      EffectiveEmployeeID = GuardShiftChanges.ReplacementEmployeeID.
   ───────────────────────────────────────────────────────────────────────────── */

PRINT N'Script módulo guardias rotativos FINAL ejecutado correctamente.';
GO

SET NOEXEC OFF;
GO
