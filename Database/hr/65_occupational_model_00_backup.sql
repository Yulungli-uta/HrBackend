/* =====================================================================
   65_occupational_model_00_backup.sql
   Respaldo previo a la reestructuración Cargo -> Escala (Grupo x Grado).
   - Solo LEE las tablas de origen y CREA tablas espejo con sufijo _bak_20261125.
   - Idempotente: si la tabla de respaldo ya existe NO se vuelve a crear ni se sobrescribe.
   - Fotos de lo que resuelven hoy la función de sueldo y las vistas, para compararlas
     después de la migración (deben quedar idénticas).
   ===================================================================== */
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @sfx NVARCHAR(20) = N'_bak_20261125';

/* ---- 1) Tablas espejo de datos --------------------------------------------------- */
IF OBJECT_ID(N'HR.tbl_Occupational_Groups_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.tbl_Occupational_Groups_bak_20261125 FROM HR.tbl_Occupational_Groups;
IF OBJECT_ID(N'HR.tbl_Degrees_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.tbl_Degrees_bak_20261125 FROM HR.tbl_Degrees;
IF OBJECT_ID(N'HR.tbl_jobs_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.tbl_jobs_bak_20261125 FROM HR.tbl_jobs;
-- Solo las categorías de ref_Types que esta migración lee o modifica (grupos y escala UEP)
IF OBJECT_ID(N'HR.ref_Types_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.ref_Types_bak_20261125 FROM HR.ref_Types
    WHERE Category IN (N'OCCUPATIONAL_GROUP_FAMILY', N'UEP_SCALE_TYPE');

/* ---- 2) Vínculos que apuntan al cargo (no se modifican; sirven para verificar) ---- */
IF OBJECT_ID(N'HR.snap_Employees_Job_bak_20261125', N'U') IS NULL
    SELECT EmployeeID, JobID INTO HR.snap_Employees_Job_bak_20261125 FROM HR.tbl_Employees;
IF OBJECT_ID(N'HR.snap_Contracts_Job_bak_20261125', N'U') IS NULL
    SELECT ContractID, JobID INTO HR.snap_Contracts_Job_bak_20261125 FROM HR.tbl_Contracts;
IF OBJECT_ID(N'HR.snap_PersonnelActions_Job_bak_20261125', N'U') IS NULL
    SELECT ActionID, OriginJobId, DestinationJobId INTO HR.snap_PersonnelActions_Job_bak_20261125 FROM HR.tbl_PersonnelActions;

/* ---- 3) Foto de lo que se IMPRIME hoy en las acciones de personal ---------------- */
IF OBJECT_ID(N'HR.snap_PersonnelActions_Print_bak_20261125', N'U') IS NULL
    SELECT pa.ActionID, pa.OriginJobId, pa.DestinationJobId,
           ogo.Description AS OriginGroup,  dgo.Description AS OriginDegree,  uo.Name AS OriginUep,
           ogd.Description AS DestGroup,    dgd.Description AS DestDegree,    ud.Name AS DestUep
    INTO HR.snap_PersonnelActions_Print_bak_20261125
    FROM HR.tbl_PersonnelActions pa
    LEFT JOIN HR.tbl_jobs jo ON jo.JobID = pa.OriginJobId
    LEFT JOIN HR.tbl_Occupational_Groups ogo ON ogo.GroupID = jo.GroupID
    LEFT JOIN HR.tbl_Degrees dgo ON dgo.DegreeID = ogo.DegreeID
    LEFT JOIN HR.ref_Types uo ON uo.TypeID = ogo.UepScaleTypeId
    LEFT JOIN HR.tbl_jobs jd ON jd.JobID = pa.DestinationJobId
    LEFT JOIN HR.tbl_Occupational_Groups ogd ON ogd.GroupID = jd.GroupID
    LEFT JOIN HR.tbl_Degrees dgd ON dgd.DegreeID = ogd.DegreeID
    LEFT JOIN HR.ref_Types ud ON ud.TypeID = ogd.UepScaleTypeId;

/* ---- 4) Fotos de las vistas y de la función de sueldo ---------------------------- */
IF OBJECT_ID(N'HR.snap_vw_Jobs_WithDegreeAndGroup_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.snap_vw_Jobs_WithDegreeAndGroup_bak_20261125 FROM HR.vw_Jobs_WithDegreeAndGroup;
IF OBJECT_ID(N'HR.snap_vw_EmployeeAcademicGroup_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.snap_vw_EmployeeAcademicGroup_bak_20261125 FROM HR.vw_EmployeeAcademicGroup;
IF OBJECT_ID(N'HR.snap_vw_PositionStructures_Unified_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.snap_vw_PositionStructures_Unified_bak_20261125 FROM HR.vw_PositionStructures_Unified;
IF OBJECT_ID(N'HR.snap_vw_Job_Activities_bak_20261125', N'U') IS NULL
    SELECT * INTO HR.snap_vw_Job_Activities_bak_20261125 FROM HR.vw_Job_Activities;
IF OBJECT_ID(N'HR.snap_fn_ResolveEmployeeRate_bak_20261125', N'U') IS NULL
BEGIN
    DECLARE @hoy DATE = CAST(SYSDATETIME() AS DATE);
    SELECT *, @hoy AS AsOfDate INTO HR.snap_fn_ResolveEmployeeRate_bak_20261125
    FROM HR.fn_ResolveEmployeeRate(@hoy, NULL);
END;

/* ---- 5) Definición actual de vistas y función (para poder revertir) -------------- */
IF OBJECT_ID(N'HR.snap_ModuleDefinitions_bak_20261125', N'U') IS NULL
    SELECT o.name AS ObjectName, o.type_desc AS ObjectType, m.definition AS Definition
    INTO HR.snap_ModuleDefinitions_bak_20261125
    FROM sys.sql_modules m
    JOIN sys.objects o ON o.object_id = m.object_id
    WHERE o.schema_id = SCHEMA_ID(N'HR')
      AND o.name IN (N'vw_Jobs_WithDegreeAndGroup', N'vw_EmployeeAcademicGroup',
                     N'vw_PositionStructures_Unified', N'vw_Job_Activities', N'fn_ResolveEmployeeRate');

/* ---- 6) Manifiesto: filas y suma de verificación de cada respaldo ----------------- */
IF OBJECT_ID(N'HR.bak_Manifest_20261125', N'U') IS NULL
BEGIN
    CREATE TABLE HR.bak_Manifest_20261125 (
        BackupTable   SYSNAME       NOT NULL PRIMARY KEY,
        SourceObject  SYSNAME       NOT NULL,
        SourceRows    INT           NOT NULL,
        BackupRows    INT           NOT NULL,
        CreatedAt     DATETIME2     NOT NULL DEFAULT SYSDATETIME()
    );

    INSERT INTO HR.bak_Manifest_20261125 (BackupTable, SourceObject, SourceRows, BackupRows)
    SELECT N'tbl_Occupational_Groups_bak_20261125', N'tbl_Occupational_Groups',
           (SELECT COUNT(*) FROM HR.tbl_Occupational_Groups), (SELECT COUNT(*) FROM HR.tbl_Occupational_Groups_bak_20261125)
    UNION ALL SELECT N'tbl_Degrees_bak_20261125', N'tbl_Degrees',
           (SELECT COUNT(*) FROM HR.tbl_Degrees), (SELECT COUNT(*) FROM HR.tbl_Degrees_bak_20261125)
    UNION ALL SELECT N'tbl_jobs_bak_20261125', N'tbl_jobs',
           (SELECT COUNT(*) FROM HR.tbl_jobs), (SELECT COUNT(*) FROM HR.tbl_jobs_bak_20261125)
    UNION ALL SELECT N'ref_Types_bak_20261125', N'ref_Types (grupos y UEP)',
           (SELECT COUNT(*) FROM HR.ref_Types WHERE Category IN (N'OCCUPATIONAL_GROUP_FAMILY', N'UEP_SCALE_TYPE')),
           (SELECT COUNT(*) FROM HR.ref_Types_bak_20261125)
    UNION ALL SELECT N'snap_Employees_Job_bak_20261125', N'tbl_Employees',
           (SELECT COUNT(*) FROM HR.tbl_Employees), (SELECT COUNT(*) FROM HR.snap_Employees_Job_bak_20261125)
    UNION ALL SELECT N'snap_Contracts_Job_bak_20261125', N'tbl_Contracts',
           (SELECT COUNT(*) FROM HR.tbl_Contracts), (SELECT COUNT(*) FROM HR.snap_Contracts_Job_bak_20261125)
    UNION ALL SELECT N'snap_PersonnelActions_Job_bak_20261125', N'tbl_PersonnelActions',
           (SELECT COUNT(*) FROM HR.tbl_PersonnelActions), (SELECT COUNT(*) FROM HR.snap_PersonnelActions_Job_bak_20261125)
    UNION ALL SELECT N'snap_PersonnelActions_Print_bak_20261125', N'tbl_PersonnelActions',
           (SELECT COUNT(*) FROM HR.tbl_PersonnelActions), (SELECT COUNT(*) FROM HR.snap_PersonnelActions_Print_bak_20261125)
    UNION ALL SELECT N'snap_vw_Jobs_WithDegreeAndGroup_bak_20261125', N'vw_Jobs_WithDegreeAndGroup',
           (SELECT COUNT(*) FROM HR.vw_Jobs_WithDegreeAndGroup), (SELECT COUNT(*) FROM HR.snap_vw_Jobs_WithDegreeAndGroup_bak_20261125)
    UNION ALL SELECT N'snap_vw_EmployeeAcademicGroup_bak_20261125', N'vw_EmployeeAcademicGroup',
           (SELECT COUNT(*) FROM HR.vw_EmployeeAcademicGroup), (SELECT COUNT(*) FROM HR.snap_vw_EmployeeAcademicGroup_bak_20261125)
    UNION ALL SELECT N'snap_vw_PositionStructures_Unified_bak_20261125', N'vw_PositionStructures_Unified',
           (SELECT COUNT(*) FROM HR.vw_PositionStructures_Unified), (SELECT COUNT(*) FROM HR.snap_vw_PositionStructures_Unified_bak_20261125)
    UNION ALL SELECT N'snap_vw_Job_Activities_bak_20261125', N'vw_Job_Activities',
           (SELECT COUNT(*) FROM HR.vw_Job_Activities), (SELECT COUNT(*) FROM HR.snap_vw_Job_Activities_bak_20261125)
    UNION ALL SELECT N'snap_fn_ResolveEmployeeRate_bak_20261125', N'fn_ResolveEmployeeRate',
           (SELECT COUNT(*) FROM HR.fn_ResolveEmployeeRate(CAST(SYSDATETIME() AS DATE), NULL)),
           (SELECT COUNT(*) FROM HR.snap_fn_ResolveEmployeeRate_bak_20261125)
    UNION ALL SELECT N'snap_ModuleDefinitions_bak_20261125', N'sql_modules', 5,
           (SELECT COUNT(*) FROM HR.snap_ModuleDefinitions_bak_20261125);
END;

SELECT BackupTable, SourceObject, SourceRows, BackupRows,
       CASE WHEN SourceRows = BackupRows THEN 'OK' ELSE 'REVISAR' END AS Estado
FROM HR.bak_Manifest_20261125 ORDER BY BackupTable;
