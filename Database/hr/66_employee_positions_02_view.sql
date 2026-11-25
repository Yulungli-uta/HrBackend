/* =====================================================================
   66_employee_positions_02_view.sql
   HR.vw_EmployeePositions: una fila por POSICIÓN VIGENTE de cada empleado (no por persona).
   Tipos: Principal / Segundo régimen (régimen laboral vigente), Autoridad (DepartmentAuthorities vigente)
          y Temporal (encargo, subrogación o comisión en curso, con fecha de fin real).
   Cargo/unidad del Principal: la fila de régimen y, si está vacía, la estructura docente (LOES) y luego la ficha.
   Dedicación (LOES): estructura docente -> modalidad del contrato -> sufijo del nombre del cargo. JobBaseName = cargo sin la dedicación.
   Solo lectura. Idempotente (CREATE OR ALTER).
   ===================================================================== */
SET NOCOUNT ON;
GO
CREATE OR ALTER VIEW HR.vw_EmployeePositions AS
WITH pos AS (
    SELECT r.EmployeeId AS EmployeeID,
           CASE WHEN r.IsPrincipal = 1 THEN N'Principal' ELSE N'Segundo régimen' END AS PositionType,
           r.LaborRegimeId AS LaborRegimeID, r.JobId AS JobID, r.DepartmentId AS DepartmentID, r.DocumentNumber,
           CAST(r.EffectiveFrom AS DATE) AS StartDate, CAST(r.EffectiveTo AS DATE) AS EndDate, N'REGIMEN' AS Source,
           r.SourceContractId AS ContractID, CAST(r.IsPrincipal AS INT) AS IsPrincipal
    FROM HR.tbl_EmployeeLaborRegime r
    WHERE r.IsActive = 1 AND r.EffectiveFrom <= CAST(SYSDATETIME() AS DATE) AND (r.EffectiveTo IS NULL OR r.EffectiveTo >= CAST(SYSDATETIME() AS DATE))
    UNION ALL
    SELECT a.EmployeeID, N'Autoridad', NULL, a.JobID, a.DepartmentID, a.ResolutionCode,
           CAST(a.StartDate AS DATE), CAST(a.EndDate AS DATE), N'AUTORIDAD', NULL, 0
    FROM HR.tbl_DepartmentAuthorities a
    WHERE a.IsActive = 1 AND a.StartDate <= CAST(SYSDATETIME() AS DATE) AND (a.EndDate IS NULL OR a.EndDate >= CAST(SYSDATETIME() AS DATE))
    UNION ALL
    SELECT pa.EmployeeID, N'Temporal', NULL, pa.DestinationJobId, pa.DestinationDepartmentId, pa.ActionNumber,
           CAST(ISNULL(pa.EffectiveDate, pa.ActionDate) AS DATE), CAST(pa.EndDate AS DATE), N'ACCION_TEMPORAL', NULL, 0
    FROM HR.tbl_PersonnelActions pa
    JOIN HR.tbl_Personnel_Action_Type ty ON ty.PersonnelActionTypeID = pa.ActionTypeID
    WHERE pa.IsDeleted = 0 AND pa.EmployeeID IS NOT NULL AND pa.Status IN ('VIGENTE', 'FIRMADO_CARGADO')
      AND (ty.Name LIKE N'Encargo%' OR ty.Name LIKE N'Subrog%' OR ty.Name LIKE N'Comisi%')
      AND (pa.DestinationJobId IS NOT NULL OR pa.DestinationDepartmentId IS NOT NULL)
      AND ISNULL(pa.EffectiveDate, pa.ActionDate) <= CAST(SYSDATETIME() AS DATE)
      AND pa.EndDate IS NOT NULL AND YEAR(pa.EndDate) < 9000 AND pa.EndDate >= CAST(SYSDATETIME() AS DATE)
)
SELECT
    p.EmployeeID,
    e.PersonID                                              AS PersonID,
    pe.IDCard                                               AS IDCard,
    LTRIM(RTRIM(CONCAT(pe.LastName, N' ', pe.FirstName)))   AS FullName,
    pe.LastName                                             AS LastName,
    pe.FirstName                                            AS FirstName,
    p.PositionType,
    p.LaborRegimeID,
    rt.Name                                                 AS LaborRegimeName,
    x.JobID                                                 AS JobID,
    jf.Description                                          AS JobName,
    LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(jf.Description, N' TIEMPO COMPLETO', N''), N' MEDIO TIEMPO', N''), N' TIEMPO PARCIAL', N''), N' TIEMPO', N''))) AS JobBaseName,
    x.JobSource                                             AS JobSource,
    x.DepartmentID                                          AS DepartmentID,
    d.Name                                                  AS DepartmentName,
    dc.Code                                                 AS DedicationCode,
    CASE dc.Code WHEN N'EXCLUSIVA' THEN N'Dedicación Exclusiva' WHEN N'TC' THEN N'Tiempo Completo' WHEN N'MT' THEN N'Medio Tiempo' WHEN N'HORAS' THEN N'Por Horas (Tiempo Parcial)' END AS DedicationName,
    dc.Source                                               AS DedicationSource,
    mm.ModalityName                                         AS ModalityName,
    mm.ContractedHours                                      AS ContractedHours,
    p.DocumentNumber,
    p.StartDate,
    p.EndDate,
    p.Source
FROM pos p
JOIN HR.tbl_Employees e   ON e.EmployeeID = p.EmployeeID
JOIN HR.tbl_People pe     ON pe.PersonID = e.PersonID
LEFT JOIN HR.ref_Types rt ON rt.TypeID = p.LaborRegimeID
OUTER APPLY (SELECT TOP 1 t.DepartmentId, t.LadderId, t.DedicationTypeID FROM HR.tbl_TeacherStructure t WHERE t.EmployeeID = p.EmployeeID AND t.IsActive = 1 ORDER BY t.TeacherStructureID DESC) ts
OUTER APPLY (SELECT TOP 1 j2.JobID FROM HR.tbl_jobs j2 WHERE j2.AcademicLadderId = ts.LadderId AND j2.IsActive = 1 ORDER BY j2.JobID) lj
OUTER APPLY (
    SELECT TOP 1 wm.Name AS ModalityName, c.ContractedHours,
           CASE c.WorkModalityID WHEN 144 THEN N'TC' WHEN 143 THEN N'MT' WHEN 145 THEN N'HORAS' END AS DedicationFromModality,
           CASE COALESCE(pc.ContractTypeID, c.ContractTypeID) WHEN 17 THEN 117 WHEN 51 THEN 117 WHEN 23 THEN 238 WHEN 52 THEN 238 WHEN 41 THEN 238 WHEN 47 THEN 247 WHEN 53 THEN 247 WHEN 48 THEN 247 END AS ContractJobID
    FROM HR.tbl_Contracts c
    LEFT JOIN HR.tbl_Contracts pc ON pc.ContractID = c.ParentID
    LEFT JOIN HR.ref_Types wm ON wm.TypeID = c.WorkModalityID
    WHERE (c.ContractID = p.ContractID)
       OR (p.ContractID IS NULL AND p.Source = N'REGIMEN' AND c.PersonID = e.PersonID AND c.LaborRegimeID = p.LaborRegimeID AND CAST(c.startdate AS DATE) <= CAST(SYSDATETIME() AS DATE) AND CAST(c.enddate AS DATE) >= CAST(SYSDATETIME() AS DATE))
    ORDER BY CASE WHEN c.ContractID = p.ContractID THEN 0 ELSE 1 END, c.startdate DESC
) mm
OUTER APPLY (
    SELECT COALESCE(p.JobID, CASE WHEN p.LaborRegimeID = 58 THEN mm.ContractJobID END, CASE WHEN p.IsPrincipal = 1 THEN COALESCE(lj.JobID, e.JobID) END) AS JobID,
           CASE WHEN p.JobID IS NOT NULL THEN N'FILA' WHEN p.LaborRegimeID = 58 AND mm.ContractJobID IS NOT NULL THEN N'TIPO_DE_CONTRATO'
                WHEN p.IsPrincipal = 1 AND lj.JobID IS NOT NULL THEN N'ESTRUCTURA_DOCENTE' WHEN p.IsPrincipal = 1 AND e.JobID IS NOT NULL THEN N'FICHA' END AS JobSource,
           COALESCE(p.DepartmentID, CASE WHEN p.IsPrincipal = 1 THEN COALESCE(ts.DepartmentId, e.DepartmentID) END) AS DepartmentID
) x
LEFT JOIN HR.tbl_jobs jf ON jf.JobID = x.JobID
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = x.DepartmentID
OUTER APPLY (
    SELECT CASE WHEN rt.Name LIKE N'LOES%' OR p.LaborRegimeID = 58 THEN
                COALESCE((SELECT dd.Name FROM HR.ref_Types dd WHERE dd.TypeID = ts.DedicationTypeID), mm.DedicationFromModality,
                         CASE WHEN jf.Description LIKE N'%TIEMPO COMPLETO%' THEN N'TC' WHEN jf.Description LIKE N'%MEDIO TIEMPO%' THEN N'MT' WHEN jf.Description LIKE N'%TIEMPO PARCIAL%' THEN N'HORAS' END) END AS Code,
           CASE WHEN rt.Name LIKE N'LOES%' OR p.LaborRegimeID = 58 THEN
                CASE WHEN ts.DedicationTypeID IS NOT NULL THEN N'ESTRUCTURA_DOCENTE' WHEN mm.DedicationFromModality IS NOT NULL THEN N'MODALIDAD_CONTRATO' WHEN jf.Description LIKE N'%TIEMPO%' THEN N'NOMBRE_DEL_CARGO' END END AS Source
) dc;
GO
