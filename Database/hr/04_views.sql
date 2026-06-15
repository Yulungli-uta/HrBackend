-- ============================================================
-- VISTAS : esquema [HR]
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- [vw_AttendanceDay]
CREATE   VIEW HR.vw_AttendanceDay AS
SELECT 
  e.EmployeeID,
  c.D AS WorkDate,
  s.RequiredHoursPerDay,
  s.EntryTime, s.ExitTime, s.HasLunchBreak, s.LunchStart, s.LunchEnd,
  pd.FirstIn, pd.LastOut,
  CASE WHEN c.IsHoliday=1 OR c.IsWeekend=1 THEN 0 ELSE s.RequiredHoursPerDay*60 END AS RequiredMin
FROM HR.tbl_Employees e
JOIN HR.vw_Calendar c ON 1=1
LEFT JOIN HR.vw_EmployeeScheduleAtDate s ON s.EmployeeID=e.EmployeeID AND s.D=c.D
LEFT JOIN HR.vw_PunchDay pd ON pd.EmployeeID=e.EmployeeID AND pd.WorkDate=c.D;
GO

-- [vw_Authority]
CREATE VIEW HR.vw_Authority AS
SELECT
    da.AuthorityID,
    da.DepartmentID,
    dep.Code        AS DepartmentCode,
    dep.Name        AS DepartmentName,
    da.EmployeeID,
    p.IDCard        AS EmployeeIDCard,
    CONCAT(p.FirstName, ' ', p.LastName) AS EmployeeFullName,
    da.AuthorityTypeID,
    at_ref.Name     AS AuthorityTypeName,
    at_ref.Description AS AuthorityTypeDescription,
    da.JobID,
    j.Description   AS JobDescription,
    da.Denomination,
    da.StartDate,
    da.EndDate,
    da.ResolutionCode,
    da.Notes,
    da.IsActive,
    da.CreatedAt,
    da.UpdatedAt
FROM  HR.tbl_DepartmentAuthorities  da
LEFT JOIN HR.tbl_Departments   dep    ON dep.DepartmentID = da.DepartmentID
LEFT JOIN HR.tbl_Employees     emp    ON emp.EmployeeID   = da.EmployeeID
LEFT JOIN HR.tbl_People        p      ON p.PersonID       = emp.PersonID
LEFT JOIN HR.ref_Types         at_ref ON at_ref.TypeID    = da.AuthorityTypeID
LEFT JOIN HR.tbl_jobs          j      ON j.JobID           = da.JobID;
GO

-- [vw_Calendar]
CREATE   VIEW HR.vw_Calendar AS
WITH
StartDate AS (
SELECT DATEFROMPARTS(YEAR(GETDATE()) - 1, 1, 1) AS StartD
),
EndDate AS (
SELECT DATEADD(YEAR, 3, CAST(GETDATE() AS DATE)) AS EndD
),
E1 AS (
SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL
SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL SELECT 1 AS c UNION ALL
SELECT 1 AS c UNION ALL SELECT 1 AS c
),
E2 AS (SELECT 1 AS c FROM E1 a, E1 b),
E4 AS (SELECT 1 AS c FROM E2 a, E2 b),
Numbers AS (
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
FROM E4
),
Dates AS (
SELECT
DATEADD(DAY, n, s.StartD) AS D
FROM Numbers
CROSS JOIN StartDate s
CROSS JOIN EndDate e
WHERE DATEADD(DAY, n, s.StartD) <= e.EndD
)
SELECT
D,
DATENAME(WEEKDAY, D) AS WeekdayName,
CASE
WHEN EXISTS (
SELECT 1
FROM HR.tbl_Holidays h
WHERE h.HolidayDate = D AND h.IsActive = 1
) THEN 1
ELSE 0
END AS IsHoliday,
CASE
WHEN DATEPART(WEEKDAY, D) IN (1, 7) THEN 1
ELSE 0
END AS IsWeekend
FROM Dates;

GO

-- [vw_CurrentAuthorities]
-- PASO 4: CREATE VIEW HR.vw_CurrentAuthorities
CREATE   VIEW HR.vw_CurrentAuthorities
AS
SELECT
    -- Autoridad
    da.AuthorityID,
    da.DepartmentID,
    da.EmployeeID,
    da.AuthorityTypeID,
    da.JobID,
    da.Denomination,                          -- texto exacto para impresión en reportes
    da.StartDate,
    da.EndDate,
    da.ResolutionCode,
    da.Notes,
    da.IsActive,

    -- Tipo de autoridad (clasificador jerárquico)
    rt.Name          AS AuthorityTypeName,
    rt.SortOrder     AS AuthorityHierarchyOrder,

    -- Departamento al que pertenece la autoridad
    dep.Name         AS DepartmentName,
    dep.Code         AS DepartmentCode,
    dep.ParentID     AS DepartmentParentID,
    dep.DepartmentType,
    deptype.Name     AS DepartmentTypeName,

    -- Departamento padre (Facultad/Unidad superior)
    parent.Name      AS ParentDepartmentName,
    parent.Code      AS ParentDepartmentCode,

    -- Datos personales del empleado (sin duplicar — vía JOIN)
    p.FirstName      AS AuthorityFirstName,
    p.LastName       AS AuthorityLastName,
    p.FirstName + ' ' + p.LastName  AS AuthorityFullName,
    p.IDCard         AS AuthorityIDCard,
    COALESCE(e.Email, p.Email)      AS AuthorityEmail,

    -- Cargo contractual (referencia legal)
    CONVERT(NVARCHAR(200), j.Description) AS JobDescription,

    -- Subrogación vigente (si existe alguien reemplazando a esta autoridad hoy)
    sub.SubrogationID,
    sub.SubrogatingEmployeeID,
    sub.StartDate    AS SubrogationStartDate,
    sub.EndDate      AS SubrogationEndDate,
    psub.FirstName + ' ' + psub.LastName AS SubrogatingFullName,
    psub.IDCard      AS SubrogatingIDCard

FROM HR.tbl_DepartmentAuthorities da

-- Tipo de autoridad
JOIN  HR.ref_Types          rt      ON da.AuthorityTypeID = rt.TypeID

-- Departamento de la autoridad
JOIN  HR.tbl_Departments    dep     ON da.DepartmentID    = dep.DepartmentID

-- Tipo de departamento
LEFT JOIN HR.ref_Types      deptype ON dep.DepartmentType = deptype.TypeID

-- Departamento padre
LEFT JOIN HR.tbl_Departments parent ON dep.ParentID       = parent.DepartmentID

-- Empleado → Persona
JOIN  HR.tbl_Employees      e       ON da.EmployeeID      = e.EmployeeID
JOIN  HR.tbl_People         p       ON e.PersonID         = p.PersonID

-- Cargo contractual (opcional)
LEFT JOIN HR.tbl_jobs       j       ON da.JobID           = j.JobID

-- Subrogación vigente hoy sobre esta autoridad
LEFT JOIN HR.tbl_Subrogations sub
    ON  sub.SubrogatedEmployeeID = da.EmployeeID
    AND CAST(GETDATE() AS DATE) BETWEEN sub.StartDate AND sub.EndDate

-- Persona del subrogante
LEFT JOIN HR.tbl_Employees  esub    ON sub.SubrogatingEmployeeID = esub.EmployeeID
LEFT JOIN HR.tbl_People     psub    ON esub.PersonID             = psub.PersonID

WHERE da.IsActive = 1;
GO

-- [vw_Departments_WithType]
CREATE   VIEW HR.vw_Departments_WithType AS
SELECT 
    d.DepartmentID,
    d.Code,
    d.Name                          AS DepartmentName,
    d.ShortName,
    d.ParentID,
    dp.Name                         AS ParentDepartmentName,

    rt.TypeID                       AS DepartmentTypeID,
    rt.Name                         AS DepartmentTypeName,
    rt.Description                  AS DepartmentTypeDescription,

    rs.TypeID                       AS DepartmentScopeID,
    rs.Name                         AS DepartmentScopeName,
    rs.Description                  AS DepartmentScopeDescription,

    d.Email,
    d.Phone,
    d.Location,
    d.DeanDirector,
    d.BudgetCode,
    d.IsActive,
    d.CreatedAt
FROM HR.tbl_Departments d
LEFT JOIN HR.ref_Types rt 
    ON d.DepartmentType = rt.TypeID
LEFT JOIN HR.ref_Types rs
    ON d.DepartmentScope = rs.TypeID
LEFT JOIN HR.tbl_Departments dp 
    ON d.ParentID = dp.DepartmentID;
GO

-- [vw_EmployeeComplete]
CREATE   VIEW HR.vw_EmployeeComplete AS
SELECT 
    e.EmployeeID,
    p.FirstName,
    p.LastName,
    p.FirstName + ' ' + p.LastName AS FullName,
    p.IDCard,
    p.Email,
    p.Phone,
    p.BirthDate,
    p.Sex,
    p.Gender,
    p.Address,
    p.IsActive AS PersonIsActive,
    e.employeeType AS EmployeeType,
    rt.Name AS EmployeeTypeName,
    e.HireDate,
    e.IsActive AS EmployeeIsActive,
    d.Name AS Department,
    --f.Name AS Faculty,
    boss.FirstName + ' ' + boss.LastName AS ImmediateBoss,
    DATEDIFF(YEAR, e.HireDate, GETDATE()) AS YearsOfService,
    -- Información adicional de hoja de vida
    p.MaritalStatusTypeID,
    ms.Name AS MaritalStatus,
    p.EthnicityTypeID,
    eth.Name AS Ethnicity,
    p.BloodTypeTypeID,
    bt.Name AS BloodType,
    p.DisabilityPercentage,
    p.CONADISCard,
    -- Campos geográficos
    co.CountryName,
    pr.ProvinceName,
    ca.CantonName
FROM HR.tbl_Employees e
JOIN HR.tbl_People p ON e.PersonID = p.PersonID
--JOIN HR.tbl_People p ON e.EmployeeID = p.PersonID
LEFT JOIN HR.ref_Types rt ON e.employeeType = rt.TypeID
LEFT JOIN HR.tbl_Departments d ON e.DepartmentID = d.DepartmentID
--LEFT JOIN HR.tbl_Faculties f ON d.FacultyID = f.FacultyID
LEFT JOIN HR.tbl_Employees bossEmp ON e.ImmediateBossID = bossEmp.EmployeeID
LEFT JOIN HR.tbl_People boss ON bossEmp.PersonID = boss.PersonID
LEFT JOIN HR.ref_Types ms ON p.MaritalStatusTypeID = ms.TypeID
LEFT JOIN HR.ref_Types eth ON p.EthnicityTypeID = eth.TypeID
LEFT JOIN HR.ref_Types bt ON p.BloodTypeTypeID = bt.TypeID
LEFT JOIN HR.tbl_Countries co ON p.CountryID = co.CountryID
LEFT JOIN HR.tbl_Provinces pr ON p.ProvinceID = pr.ProvinceID
LEFT JOIN HR.tbl_Cantons ca ON p.CantonID = ca.CantonID

GO

-- [vw_EmployeeCurrentSchedule]
CREATE   VIEW HR.vw_EmployeeCurrentSchedule
AS
SELECT
    e.EmployeeID,
    e.PersonID,
    e.EmployeeType,
    e.DepartmentID,
    e.ImmediateBossID,
    e.HireDate,
    e.Email,
    e.IsActive,

    es.EmpScheduleID,
    es.ScheduleID,
    es.ValidFrom,
    es.ValidTo,
    es.CreatedAt  AS ScheduleAssignedAt,
    es.CreatedBy  AS ScheduleAssignedBy,

    s.Description AS ScheduleDescription,
    s.EntryTime,
    s.ExitTime,
    s.WorkingDays,
    s.RequiredHoursPerDay,
    s.HasLunchBreak,
    s.LunchStart,
    s.LunchEnd,
    s.IsRotating,
    s.RotationPattern
FROM HR.tbl_Employees e
INNER JOIN HR.tbl_EmployeeSchedules es
    ON es.EmployeeID = e.EmployeeID
INNER JOIN HR.tbl_Schedules s
    ON s.ScheduleID = es.ScheduleID
WHERE
    e.IsActive = 1
    AND es.ValidFrom <= CAST(GETDATE() AS DATE)
    AND (es.ValidTo IS NULL OR es.ValidTo >= CAST(GETDATE() AS DATE));
GO

-- [vw_EmployeeDetails]
CREATE   VIEW HR.vw_EmployeeDetails AS
SELECT 
    e.EmployeeID      AS EmployeeID,
    p.FirstName, 
    p.LastName, 
    p.IDCard, 
    e.Email,
	  p.Email           AS PersonnelEmail,
	  e.ImmediateBossID,
    e.EmployeeType    AS EmployeeType,
    rt.Name           AS ContractType,
    e.JobID,
    j.Description     AS JobName,           
    es_current.ScheduleID AS ScheduleID,
    CAST(ts.EntryTime AS VARCHAR(5)) + ' - ' + CAST(ts.ExitTime AS VARCHAR(5)) AS Schedule,
	d.DepartmentID,
    d.Name            AS Department,
    1.00              AS BaseSalary,
    e.HireDate
FROM HR.tbl_People p
JOIN HR.tbl_Employees e ON e.PersonID = p.PersonID	
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
LEFT JOIN HR.ref_Types rt ON rt.TypeID = e.EmployeeType 
                          AND rt.Category = 'CONTRACT_TYPE'
LEFT JOIN HR.tbl_jobs j ON j.JobID = e.JobID
OUTER APPLY (
    SELECT TOP 1 
        es.ScheduleID,
        es.ValidFrom,
        es.ValidTo
    FROM HR.tbl_EmployeeSchedules es
    WHERE es.EmployeeID = e.EmployeeID
    ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC
) es_current
LEFT JOIN HR.tbl_Schedules ts ON ts.ScheduleID = es_current.ScheduleID
WHERE e.IsActive = 1

GO

-- [vw_EmployeeDetails2]

CREATE   VIEW HR.vw_EmployeeDetails2 AS
SELECT 
    e.EmployeeID,
    p.FirstName                                             AS FirstName,
    p.LastName                                              AS LastName,
    p.IDCard,
    e.Email,
    e.ImmediateBossID,

    -- Datos del jefe inmediato
    --p1.FirstName                                            AS BossFirstName,
    --p1.LastName                                             AS BossLastName,
    p1.FirstName + ' ' + p1.LastName                        AS BossCompleteName,
    e1.Email                                                AS BossWorkEmail,

    e.EmployeeType,
    rt.Name                                                 AS ContractType,

    es_current.ScheduleID,
    CONVERT(VARCHAR(5), ts.EntryTime, 108) 
        + ' - ' + 
    CONVERT(VARCHAR(5), ts.ExitTime, 108)                   AS Schedule,

    e.DepartmentID,
    d.Name                                                  AS Department,

    1.00                                                    AS BaseSalary,  -- TODO: reemplazar con columna real
    e.HireDate

FROM HR.tbl_People          p
JOIN  HR.tbl_Employees      e   ON e.PersonID        = p.PersonID
                                AND e.IsActive        = 1              -- <-- movido aquí para filtrar antes de los JOINs

-- Jefe inmediato: empleado
LEFT JOIN HR.tbl_Employees  e1  ON e1.EmployeeID     = e.ImmediateBossID
-- Jefe inmediato: persona (nombre)
LEFT JOIN HR.tbl_People     p1  ON p1.PersonID       = e1.PersonID

LEFT JOIN HR.tbl_Departments    d   ON d.DepartmentID    = e.DepartmentID

LEFT JOIN HR.ref_Types          rt  ON rt.TypeID         = e.EmployeeType
                                   AND rt.Category       = 'CONTRACT_TYPE'

-- Último horario asignado al empleado
OUTER APPLY (
    SELECT TOP 1
        es.ScheduleID,
        es.ValidFrom,
        es.ValidTo
    FROM HR.tbl_EmployeeSchedules es
    WHERE es.EmployeeID = e.EmployeeID
    ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC
) es_current

LEFT JOIN HR.tbl_Schedules  ts  ON ts.ScheduleID    = es_current.ScheduleID;


GO

-- [vw_EmployeeScheduleAtDate]
CREATE   VIEW HR.vw_EmployeeScheduleAtDate AS
SELECT es.EmployeeID, c.D, s.*
FROM HR.tbl_EmployeeSchedules es
JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
JOIN HR.vw_Calendar c     ON c.D BETWEEN es.ValidFrom AND ISNULL(es.ValidTo,'2099-12-31');
GO

-- [vw_Job_Activities]

-- =============================================
-- VISTA 3: Actividades correspondientes al cargo
-- =============================================
CREATE   VIEW hr.vw_Job_Activities AS
SELECT 
    j.JobID,
    CAST(j.Description AS NVARCHAR(500))    AS JobDescription,
    rt.Name                                  AS JobTypeName,
    og.Description                           AS OccupationalGroup,
    a.ActivitiesID,
    CAST(a.Description AS NVARCHAR(1000))    AS ActivityDescription,
    a.ActivitiesType,
    ja.IsActive                              AS ActivityAssignmentActive
FROM hr.tbl_JobActivities ja
INNER JOIN hr.tbl_jobs j 
    ON ja.JobID = j.JobID
INNER JOIN hr.tbl_Activities a 
    ON ja.ActivitiesID = a.ActivitiesID
INNER JOIN hr.ref_Types rt 
    ON j.JobTypeID = rt.TypeID
INNER JOIN hr.tbl_Occupational_Groups og 
    ON j.GroupID = og.GroupID;

GO

-- [vw_Jobs_WithDegreeAndGroup]
CREATE   VIEW HR.vw_Jobs_WithDegreeAndGroup AS
SELECT
    j.JobID,
    j.Description                          AS JobDescription,
    rt_type.Name                           AS JobTypeName,
    rt_regime.Name                         AS LaborRegimeName,
    og.GroupID,
    og.Description                         AS OccupationalGroup,
    og.RMU,
    d.DegreeID,
    d.Description                          AS Degree,
    d.IsActive                             AS DegreeIsActive
FROM HR.tbl_jobs j
LEFT JOIN HR.ref_Types rt_type   ON j.JobTypeID     = rt_type.TypeID
LEFT JOIN HR.ref_Types rt_regime ON j.LaborRegimeID = rt_regime.TypeID
LEFT JOIN HR.tbl_Occupational_Groups og ON j.GroupID  = og.GroupID
LEFT JOIN HR.tbl_Degrees d              ON og.DegreeID = d.DegreeID;
GO

-- [vw_LeaveWindows]
CREATE   VIEW HR.vw_LeaveWindows AS
SELECT v.EmployeeID, v.StartDate AS FromDT, DATEADD(DAY,1,v.EndDate) AS ToDT, 'VACATION' AS LeaveType
FROM HR.tbl_Vacations v WHERE v.Status IN ('Planned','InProgress')
UNION ALL
SELECT p.EmployeeID, p.StartDate, p.EndDate, 'PERMISSION'
FROM HR.tbl_Permissions p WHERE p.Status='Approved';
GO

-- [vw_OvertimePlanning]

-- ============================================================
-- Vista 4: vw_OvertimePlanning — Horas extras con planificación
-- Incluye cabecera (tbl_TimePlanning) + empleados (tbl_TimePlanningEmployees)
-- + ejecuciones (tbl_TimePlanningExecution) + horas extras directas (tbl_Overtime)
-- ============================================================
CREATE   VIEW HR.vw_OvertimePlanning AS
SELECT
    -- ── Cabecera de planificación ──────────────────────────
    tp.PlanID,
    tp.PlanType,
    tp.Title                               AS PlanTitle,
    tp.Description                         AS PlanDescription,
    tp.StartDate                           AS PlanStartDate,
    tp.EndDate                             AS PlanEndDate,
    tp.StartTime                           AS PlanStartTime,
    tp.EndTime                             AS PlanEndTime,

    -- Tipo de HE y factor desde OvertimeConfig
    tp.OvertimeType,
    oc.Factor                              AS ConfigFactor,
    oc.Description                         AS OvertimeConfigDescription,
    tp.Factor                              AS PlanFactor,
    tp.OwedMinutes,

    -- Estado de la planificación (PLAN_STATUS)
    tp.PlanStatusTypeID,
    rps.Name                               AS PlanStatusName,

    -- Aprobación de la planificación
    tp.RequiresApproval,
    tp.ApprovedBy                          AS PlanApprovedBy,
    CONCAT(pap.FirstName,' ',pap.LastName) AS PlanApprovedByName,
    tp.ApprovedAt                          AS PlanApprovedAt,

    -- Creador de la planificación (jefe)
    tp.CreatedBy                           AS PlanCreatedBy,
    CONCAT(pcr.FirstName,' ',pcr.LastName) AS PlanCreatedByName,
    tp.CreatedAt                           AS PlanCreatedAt,

    -- ── Empleado en la planificación ──────────────────────
    tpe.PlanEmployeeID,
    tpe.EmployeeID,
    CONCAT(pe.FirstName, ' ', pe.LastName) AS EmployeeFullName,
    pe.IDCard                              AS EmployeeIDCard,
    d.DepartmentID,
    d.Name                                 AS Department,

    -- Horas asignadas vs ejecutadas
    tpe.AssignedHours,
    tpe.AssignedMinutes,
    tpe.ActualHours,
    tpe.ActualMinutes,
    tpe.PaymentAmount,
    tpe.IsEligible,
    tpe.EligibilityReason,

    -- Estado del empleado en la planificación (EMPLOYEE_PLAN_STATUS)
    tpe.EmployeeStatusTypeID,
    reps.Name                              AS EmployeeStatusName,

    -- ── Ejecución ─────────────────────────────────────────
    tex.ExecutionID,
    tex.WorkDate                           AS ExecutionWorkDate,
    tex.StartTime                          AS ExecutionStartTime,
    tex.EndTime                            AS ExecutionEndTime,
    tex.TotalMinutes                       AS ExecutionTotalMinutes,
    tex.RegularMinutes,
    tex.OvertimeMinutes,
    tex.NightMinutes,
    tex.HolidayMinutes,
    tex.Comments                           AS ExecutionComments,
    tex.VerifiedBy,
    CONCAT(pv.FirstName, ' ', pv.LastName) AS VerifiedByName,
    tex.VerifiedAt,

    -- ── Auditoría del registro de empleado ────────────────
    tpe.CreatedAt                          AS PlanEmployeeCreatedAt

FROM HR.tbl_TimePlanning tp

-- Estado de la planificación
LEFT JOIN HR.ref_Types rps  ON rps.TypeID  = tp.PlanStatusTypeID
                            AND rps.Category = 'PLAN_STATUS'

-- Configuración de horas extras (FK por OvertimeType)
LEFT JOIN HR.tbl_OvertimeConfig oc ON oc.OvertimeType = tp.OvertimeType

-- Aprobador de la planificación
LEFT JOIN HR.tbl_Employees  eap ON eap.EmployeeID = tp.ApprovedBy
LEFT JOIN HR.tbl_People     pap ON pap.PersonID    = eap.PersonID

-- Creador de la planificación
LEFT JOIN HR.tbl_Employees  ecr ON ecr.EmployeeID = tp.CreatedBy
LEFT JOIN HR.tbl_People     pcr ON pcr.PersonID    = ecr.PersonID

-- Empleados asignados a la planificación
JOIN HR.tbl_TimePlanningEmployees tpe ON tpe.PlanID = tp.PlanID

-- Estado del empleado en la planificación
LEFT JOIN HR.ref_Types reps ON reps.TypeID  = tpe.EmployeeStatusTypeID
                            AND reps.Category = 'EMPLOYEE_PLAN_STATUS'

-- Datos del empleado
JOIN HR.tbl_Employees  e  ON e.EmployeeID  = tpe.EmployeeID
JOIN HR.tbl_People     pe ON pe.PersonID   = e.PersonID
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID

-- Ejecuciones (LEFT JOIN — puede no tener ejecuciones aún)
LEFT JOIN HR.tbl_TimePlanningExecution tex ON tex.PlanEmployeeID = tpe.PlanEmployeeID

-- Verificador de ejecución
LEFT JOIN HR.tbl_Employees  ev ON ev.EmployeeID = tex.VerifiedBy
LEFT JOIN HR.tbl_People     pv ON pv.PersonID   = ev.PersonID;

GO

-- [vw_Permissions]

-- ============================================================
-- Vista 1: vw_Permissions — Permisos con datos completos
-- ============================================================
CREATE   VIEW HR.vw_Permissions AS
SELECT
    -- Identificadores
    p.PermissionID,
    p.EmployeeID,

    -- Datos del empleado
    CONCAT(pe.FirstName, ' ', pe.LastName) AS EmployeeFullName,
    pe.IDCard                              AS EmployeeIDCard,
    d.DepartmentID,
    d.Name                                 AS Department,

    -- Jefe inmediato
    e.ImmediateBossID,
    CONCAT(pb.FirstName, ' ', pb.LastName) AS BossFullName,

    -- Tipo de permiso
    pt.TypeID                              AS PermissionTypeID,
    pt.Name                                AS PermissionTypeName,
    pt.IsMedical,
    pt.DeductsFromVacation,
    pt.RequiresApproval,
    pt.MaxDays                             AS PermissionMaxDays,
    pt.AttachedFile                        AS RequiresAttachment,

    -- Detalle del permiso
    p.StartDate,
    p.EndDate,
    DATEDIFF(DAY, p.StartDate, p.EndDate) + 1 AS DurationDays,
    p.HourTaken,
    p.ChargedToVacation,
    p.Justification,
    p.Status,
    p.VacationID,

    -- Aprobación
    p.ApprovedBy,
    CONCAT(pa.FirstName, ' ', pa.LastName) AS ApprovedByName,
    p.ApprovedAt,

    -- Auditoría
    p.CreatedBy,
    p.CreatedAt,
    p.UpdatedBy,
    p.UpdatedAt

FROM HR.tbl_Permissions p
JOIN HR.tbl_Employees      e  ON e.EmployeeID     = p.EmployeeID
JOIN HR.tbl_People         pe ON pe.PersonID       = e.PersonID
LEFT JOIN HR.tbl_Departments d  ON d.DepartmentID  = e.DepartmentID
LEFT JOIN HR.tbl_PermissionTypes pt ON pt.TypeID   = p.PermissionTypeID
-- Jefe inmediato
LEFT JOIN HR.tbl_Employees  eb  ON eb.EmployeeID   = e.ImmediateBossID
LEFT JOIN HR.tbl_People     pb  ON pb.PersonID      = eb.PersonID
-- Aprobador
LEFT JOIN HR.tbl_Employees  ea  ON ea.EmployeeID   = p.ApprovedBy
LEFT JOIN HR.tbl_People     pa  ON pa.PersonID      = ea.PersonID;

GO

-- [vw_PositionStructures_Unified]
CREATE   VIEW HR.vw_PositionStructures_Unified AS
SELECT
    j.JobID,
    j.Description                          AS JobDescription,
    j.LaborRegimeID,
    rt_regime.Name                         AS LaborRegimeCode,
    j.GroupID,
    og.Description                         AS OccupationalGroup,
    og.RMU                                 AS GroupRMU,
    d.DegreeID,
    d.Description                          AS Degree,
    j.IsActive
FROM HR.tbl_jobs j
LEFT JOIN HR.ref_Types rt_regime            ON j.LaborRegimeID = rt_regime.TypeID
LEFT JOIN HR.tbl_Occupational_Groups og     ON j.GroupID       = og.GroupID
LEFT JOIN HR.tbl_Degrees d                  ON og.DegreeID     = d.DegreeID
WHERE j.IsActive = 1;
GO

-- [vw_PunchDay]
CREATE   VIEW HR.vw_PunchDay AS
SELECT 
  p.EmployeeID,
  CAST(p.PunchTime AS DATE) AS WorkDate,
  MIN(CASE WHEN p.PunchType='In'  THEN p.PunchTime END) AS FirstIn,
  MAX(CASE WHEN p.PunchType='Out' THEN p.PunchTime END) AS LastOut
FROM HR.tbl_AttendancePunches p
GROUP BY p.EmployeeID, CAST(p.PunchTime AS DATE);
GO

-- [vw_PunchJustifications]

-- ============================================================
-- Vista 3: vw_PunchJustifications — Justificaciones de marcación
-- ============================================================
CREATE   VIEW HR.vw_PunchJustifications AS
SELECT
    -- Identificadores
    j.PunchJustID,
    j.EmployeeID,

    -- Datos del empleado
    CONCAT(pe.FirstName, ' ', pe.LastName) AS EmployeeFullName,
    pe.IDCard                              AS EmployeeIDCard,
    d.DepartmentID,
    d.Name                                 AS Department,

    -- Jefe que gestiona la justificación
    j.BossEmployeeID,
    CONCAT(pb.FirstName, ' ', pb.LastName) AS BossFullName,

    -- Tipo de justificación (JUSTIFICATION: 93=Picada, 94=Horas, 95=Día)
    rj.TypeID                              AS JustificationTypeID,
    rj.Name                                AS JustificationTypeName,

    -- Tipo de marcación (PUNCH_TYPE: 146=Entrada, 148=Salida Almuerzo, etc.)
    rp.TypeID                              AS PunchTypeID,
    rp.Name                                AS PunchTypeName,

    -- Detalle
    j.JustificationDate,
    j.StartDate,
    j.EndDate,
    j.Reason,
    j.HoursRequested,
    j.Comments,
    j.Status,
    j.Approved,
    j.ApprovedAt,

    -- Auditoría
    j.CreatedBy,
    j.CreatedAt

FROM HR.tbl_PunchJustifications j
JOIN HR.tbl_Employees      e  ON e.EmployeeID     = j.EmployeeID
JOIN HR.tbl_People         pe ON pe.PersonID       = e.PersonID
LEFT JOIN HR.tbl_Departments d  ON d.DepartmentID  = e.DepartmentID
-- Jefe
LEFT JOIN HR.tbl_Employees  eb  ON eb.EmployeeID   = j.BossEmployeeID
LEFT JOIN HR.tbl_People     pb  ON pb.PersonID      = eb.PersonID
-- Tipo de justificación
LEFT JOIN HR.ref_Types rj ON rj.TypeID = j.JustificationTypeID
                          AND rj.Category = 'JUSTIFICATION'
-- Tipo de marcación
LEFT JOIN HR.ref_Types rp ON rp.TypeID = j.PunchTypeID
                          AND rp.Category = 'PUNCH_TYPE';

GO

-- [vw_Vacations]

-- ============================================================
-- Vista 2: vw_Vacations — Vacaciones con datos completos
-- ============================================================
CREATE   VIEW HR.vw_Vacations AS
SELECT
    -- Identificadores
    v.VacationID,
    v.EmployeeID,

    -- Datos del empleado
    CONCAT(pe.FirstName, ' ', pe.LastName) AS EmployeeFullName,
    pe.IDCard                              AS EmployeeIDCard,
    d.DepartmentID,
    d.Name                                 AS Department,

    -- Jefe inmediato
    e.ImmediateBossID,
    CONCAT(pb.FirstName, ' ', pb.LastName) AS BossFullName,

    -- Detalle de vacaciones
    v.StartDate,
    v.EndDate,
    DATEDIFF(DAY, v.StartDate, v.EndDate) + 1 AS PeriodDays,
    v.DaysGranted,
    v.DaysTaken,
    v.DaysGranted - v.DaysTaken            AS DaysRemaining,
    v.Status,

    -- Aprobación
    v.ApprovedBy,
    CONCAT(pa.FirstName, ' ', pa.LastName) AS ApprovedByName,
    v.ApprovedAt,

    -- Auditoría
    v.CreatedBy,
    v.CreatedAt,
    v.UpdatedBy,
    v.UpdatedAt

FROM HR.tbl_Vacations v
JOIN HR.tbl_Employees      e  ON e.EmployeeID    = v.EmployeeID
JOIN HR.tbl_People         pe ON pe.PersonID      = e.PersonID
LEFT JOIN HR.tbl_Departments d  ON d.DepartmentID = e.DepartmentID
-- Jefe inmediato
LEFT JOIN HR.tbl_Employees  eb  ON eb.EmployeeID  = e.ImmediateBossID
LEFT JOIN HR.tbl_People     pb  ON pb.PersonID     = eb.PersonID
-- Aprobador
LEFT JOIN HR.tbl_Employees  ea  ON ea.EmployeeID  = v.ApprovedBy
LEFT JOIN HR.tbl_People     pa  ON pa.PersonID     = ea.PersonID;

GO
