CREATE OR ALTER VIEW HR.vw_EmployeeDetails AS
SELECT 
    e.EmployeeID      AS EmployeeID,
    p.FirstName, 
    p.LastName, 
    p.IDCard, 
    e.Email,
	e.ImmediateBossID,
    e.EmployeeType    AS EmployeeType,
    rt.Name           AS ContractType,
    es_current.ScheduleID AS ScheduleID,
    CAST(ts.EntryTime AS VARCHAR(5)) + ' - ' + CAST(ts.ExitTime AS VARCHAR(5)) AS Schedule,
    d.Name            AS Department,
    1.00              AS BaseSalary,
    e.HireDate
FROM HR.tbl_People p
JOIN HR.tbl_Employees e ON e.PersonID = p.PersonID	
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
LEFT JOIN HR.ref_Types rt ON rt.TypeID = e.EmployeeType 
                          AND rt.Category = 'CONTRACT_TYPE'
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

CREATE OR ALTER VIEW HR.vw_EmployeeDetails2 AS
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

PRINT (N'Create or alter view [HR].[vw_EmployeeComplete]')
GO
CREATE OR ALTER VIEW HR.vw_EmployeeComplete AS
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

PRINT (N'Create or alter view [HR].[vw_EmployeeDetails]')
GO

CREATE OR ALTER VIEW HR.vw_EmployeeDetails AS
SELECT 
    p.PersonID      AS EmployeeID,
    p.FirstName, p.LastName, p.IDCard, E.Email,
    e.EmployeeType          AS EmployeeType,
    d.Name          AS Department,
    --f.Name          AS Faculty,
    --c.BaseSalary,
	1.00 as BaseSalary,
    e.HireDate
FROM HR.tbl_People p
JOIN HR.tbl_Employees e ON e.PersonID = p.PersonID
LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
--LEFT JOIN HR.tbl_Faculties   f ON f.FacultyID = d.FacultyID
/*OUTER APPLY (
    SELECT TOP 1 c1.BaseSalary
    FROM HR.tbl_Contracts c1
    WHERE c1.PersonID = e.PersonID
      AND GETDATE() BETWEEN c1.StartDate AND ISNULL(c1.EndDate,'9999-12-31')
    ORDER BY c1.StartDate DESC
) c*/
GO

-- =============================================
-- Vista: HR.vw_Jobs_Complete
-- Descripción: Muestra puestos con toda la información relacionada
-- =============================================
CREATE OR ALTER VIEW HR.vw_Jobs_Complete
AS
SELECT 
    j.JobID,
    j.Description AS JobDescription,
    j.JobTypeID,
    j.GroupID,
    g.Description AS GroupDescription,
    g.RMU,
    g.DegreeID,
    d.Description AS DegreeDescription,
    j.IsActive AS JobIsActive,
    g.IsActive AS GroupIsActive,
    d.IsActive AS DegreeIsActive,
    j.CreatedAt,
    j.UpdatedAt
FROM HR.tbl_Jobs j
LEFT JOIN HR.tbl_Occupational_Groups g ON j.GroupID = g.GroupID
LEFT JOIN HR.tbl_Degrees d ON g.DegreeID = d.DegreeID;
GO

/*calculo de nomina **/

/*una año anterior y 3 años a futuro partiendo del dia actual*/
/*CREATE OR ALTER VIEW HR.vw_Calendar AS
WITH Numbers AS (
    SELECT TOP (1500) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
    FROM master.dbo.spt_values
),
Dates AS (
    SELECT 
        DATEADD(DAY, n, DATEADD(YEAR, -1, CAST(GETDATE() AS DATE))) AS D
    FROM Numbers
    WHERE DATEADD(DAY, n, DATEADD(YEAR, -1, CAST(GETDATE() AS DATE))) <= DATEADD(YEAR, 3, CAST(GETDATE() AS DATE))
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
FROM Dates;*/
CREATE OR ALTER VIEW HR.vw_Calendar AS
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


/*Horario Vigente del empleado */
CREATE OR ALTER VIEW HR.vw_EmployeeScheduleAtDate AS
SELECT es.EmployeeID, c.D, s.*
FROM HR.tbl_EmployeeSchedules es
JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
JOIN HR.vw_Calendar c     ON c.D BETWEEN es.ValidFrom AND ISNULL(es.ValidTo,'2099-12-31');

/*picadas existentes */
CREATE OR ALTER VIEW HR.vw_PunchDay AS
SELECT 
  p.EmployeeID,
  CAST(p.PunchTime AS DATE) AS WorkDate,
  MIN(CASE WHEN p.PunchType='In'  THEN p.PunchTime END) AS FirstIn,
  MAX(CASE WHEN p.PunchType='Out' THEN p.PunchTime END) AS LastOut
FROM HR.tbl_AttendancePunches p
GROUP BY p.EmployeeID, CAST(p.PunchTime AS DATE);

/*Ventanas de ausencias justificadas (vacaciones y permisos aprobados)*/
-- CREATE OR ALTER VIEW HR.vw_LeaveWindows AS
-- SELECT v.EmployeeID, v.StartDate AS FromDT, DATEADD(DAY,1,v.EndDate) AS ToDT, 'VACATION' AS LeaveType
-- FROM HR.tbl_Vacations v WHERE v.Status IN ('Planned','InProgress')
-- UNION ALL
-- SELECT p.EmployeeID, p.StartDate, p.EndDate, 'PERMISSION'
-- FROM HR.tbl_Permissions p WHERE p.Status='Approved';

CREATE OR ALTER VIEW HR.vw_LeaveWindows AS
SELECT 
    v.EmployeeID, 
    v.StartDate AS FromDT, 
    DATEADD(DAY, 1, v.EndDate) AS ToDT, 
    'VACATION' AS LeaveType, 
    0 AS HourTaken,
    CONCAT('VACATIONID: ', v.VacationID) AS SourceID,
    --'VACATIONID' + v.VacationID 
    v.Status
FROM HR.tbl_Vacations v 
WHERE v.Status IN ('Planned', 'InProgress', 'Completed')
UNION ALL
SELECT 
    p.EmployeeID, 
    p.StartDate AS FromDT, 
    p.EndDate AS ToDT, 
    'PERMISSION:' AS LeaveType, 
    ISNULL(p.HourTaken, 0) AS HourTaken,
    CONCAT('PERMISSIONID: ', p.PermissionID) AS SourceID,
    p.Status
FROM HR.tbl_Permissions p 
WHERE p.Status = 'Approved';


/*----Día laboral esperado vs trabajado (base para atraso/HE)*/
CREATE OR ALTER VIEW HR.vw_AttendanceDay AS
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

/*horarios vigentes del empleado */
CREATE OR ALTER VIEW HR.Vw_EmployeeCurrentSchedule
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


/*vw_Permissions — Permisos*/
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

/*vw_Vacations — Vacaciones*/
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


/*vw_PunchJustifications — Justificaciones de marcación*/
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

/*vw_OvertimePlanning — Horas extras con planificación*/

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


/*calendario */
CREATE VIEW HR.vw_Calendar AS
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
