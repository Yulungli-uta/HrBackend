-- =============================================
-- BLOQUE 3: ÍNDICES PARA MEJORAR RENDIMIENTO
-- =============================================
-- =============================================
-- BLOQUE 3: ÍNDICES PARA MEJORAR RENDIMIENTO
-- =============================================
SET NOCOUNT ON;
PRINT 'INICIANDO CREACIÓN DE ÍNDICES...';

-- 1. ÍNDICES PARA TABLA DE PERSONAS
PRINT '1. Creando índices para HR.tbl_People...';
CREATE INDEX IX_People_IDCard ON HR.tbl_People(IDCard);
CREATE INDEX IX_People_LastName ON HR.tbl_People(LastName);
CREATE INDEX IX_People_Email ON HR.tbl_People(Email);
CREATE INDEX IX_People_IsActive ON HR.tbl_People(IsActive);
CREATE INDEX IX_People_Country ON HR.tbl_People(CountryID);
CREATE INDEX IX_People_Province ON HR.tbl_People(ProvinceID);
CREATE INDEX IX_People_Canton ON HR.tbl_People(CantonID);

-- =============================================
-- ÍNDICES PARA HR.tbl_Occupational_Groups
-- =============================================

-- Índice para la relación con Degrees
CREATE NONCLUSTERED INDEX IX_tbl_Groups_DegreeID
ON HR.tbl_Occupational_Groups(DegreeID);
GO

-- Índice para búsquedas por estado activo
CREATE NONCLUSTERED INDEX IX_tbl_Groups_IsActive
ON HR.tbl_Occupational_Groups(IsActive);
GO

-- Índice para búsquedas por descripción
CREATE NONCLUSTERED INDEX IX_tbl_Groups_Description
ON HR.tbl_Occupational_Groups(Description);
GO

-- Índice compuesto para consultas comunes
CREATE NONCLUSTERED INDEX IX_tbl_Groups_DegreeID_IsActive
ON HR.tbl_Occupational_Groups(DegreeID, IsActive);
GO


-- Índice para la relación con Groups
CREATE NONCLUSTERED INDEX IX_tbl_Jobs_GroupID
ON HR.tbl_Jobs(GroupID);
GO

-- Índice para JobTypeID
CREATE NONCLUSTERED INDEX IX_tbl_Jobs_JobTypeID
ON HR.tbl_Jobs(JobTypeID);
GO

-- Índice para búsquedas por estado activo
CREATE NONCLUSTERED INDEX IX_tbl_Jobs_IsActive
ON HR.tbl_Jobs(IsActive);
GO

-- Índice para búsquedas por descripción
--CREATE NONCLUSTERED INDEX IX_tbl_Jobs_Description
--ON HR.tbl_Jobs(Description);
--GO

-- Índice compuesto para consultas comunes
CREATE NONCLUSTERED INDEX IX_tbl_Jobs_GroupID_IsActive
ON HR.tbl_Jobs(GroupID, IsActive);
GO

-- 2. ÍNDICES PARA TABLA DE EMPLEADOS
PRINT '2. Creando índices para HR.tbl_Employees...';
CREATE INDEX IX_Employees_Department ON HR.tbl_Employees(DepartmentID);
CREATE INDEX IX_Employees_Boss ON HR.tbl_Employees(ImmediateBossID);
CREATE INDEX IX_Employees_Type ON HR.tbl_Employees(EmployeeType);
CREATE INDEX IX_Employees_HireDate ON HR.tbl_Employees(HireDate);
CREATE INDEX IX_Employees_IsActive ON HR.tbl_Employees(IsActive);
CREATE INDEX IX_Employees_Email ON HR.tbl_Employees(Email);

-- 3. ÍNDICES PARA TABLA DE DEPARTAMENTOS
PRINT '3. Creando índices para HR.tbl_Departments...';
CREATE INDEX IX_Departments_Parent ON HR.tbl_Departments(ParentID);
CREATE INDEX IX_Departments_Type ON HR.tbl_Departments(DepartmentType);
CREATE INDEX IX_Departments_Code ON HR.tbl_Departments(Code);
CREATE INDEX IX_Departments_IsActive ON HR.tbl_Departments(IsActive);
CREATE INDEX IX_Departments_Dean ON HR.tbl_Departments(DeanDirector);

-- 4. ÍNDICES PARA TABLA DE CONTRATOS
PRINT '4. Creando índices para HR.tbl_Contracts...';
CREATE INDEX IX_Contracts_Employee ON HR.tbl_Contracts(PersonID);
CREATE INDEX IX_Contracts_Type ON HR.tbl_Contracts(ContractTypeID);
CREATE INDEX IX_Contracts_Job ON HR.tbl_Contracts(JobID);
CREATE INDEX IX_Contracts_StartDate ON HR.tbl_Contracts(StartDate);
CREATE INDEX IX_Contracts_EndDate ON HR.tbl_Contracts(EndDate);
CREATE INDEX IX_Contracts_DateRange ON HR.tbl_Contracts(StartDate, EndDate);

-- 5. ÍNDICES PARA TABLA DE ASISTENCIA (PICADAS)
PRINT '5. Creando índices para HR.tbl_AttendancePunches...';
CREATE INDEX IX_AttendancePunches_Employee ON HR.tbl_AttendancePunches(EmployeeID);
CREATE INDEX IX_AttendancePunches_DateTime ON HR.tbl_AttendancePunches(PunchTime);
CREATE INDEX IX_AttendancePunches_EmployeeDate ON HR.tbl_AttendancePunches(EmployeeID, PunchTime );
CREATE INDEX IX_AttendancePunches_Type ON HR.tbl_AttendancePunches(PunchType);
CREATE INDEX IX_AttendancePunches_EmployeeDateTime ON HR.tbl_AttendancePunches(EmployeeID, PunchTime);

-- 6. ÍNDICES PARA TABLA DE CÁLCULOS DE ASISTENCIA
PRINT '6. Creando índices para HR.tbl_AttendanceCalculations...';
CREATE INDEX IX_AttendanceCalculations_Employee ON HR.tbl_AttendanceCalculations(EmployeeID);
CREATE INDEX IX_AttendanceCalculations_WorkDate ON HR.tbl_AttendanceCalculations(WorkDate);
CREATE INDEX IX_AttendanceCalculations_EmployeeDate ON HR.tbl_AttendanceCalculations(EmployeeID, WorkDate);
CREATE INDEX IX_AttendanceCalculations_Status ON HR.tbl_AttendanceCalculations(Status);

-- 7. ÍNDICES PARA TABLA DE JUSTIFICACIONES
PRINT '7. Creando índices para HR.tbl_PunchJustifications...';
CREATE INDEX IX_PunchJustifications_Employee ON HR.tbl_PunchJustifications(EmployeeID, Status);
CREATE INDEX IX_PunchJustifications_Boss ON HR.tbl_PunchJustifications(BossEmployeeID, Status);
CREATE INDEX IX_PunchJustifications_Type ON HR.tbl_PunchJustifications(JustificationTypeID);
CREATE INDEX IX_PunchJustifications_Date ON HR.tbl_PunchJustifications(JustificationDate);
CREATE INDEX IX_PunchJustifications_Status ON HR.tbl_PunchJustifications(Status);
CREATE INDEX IX_PunchJustifications_CreatedAt ON HR.tbl_PunchJustifications(CreatedAt);

-- 8. ÍNDICES PARA TABLA DE VACACIONES
PRINT '8. Creando índices para HR.tbl_Vacations...';
CREATE INDEX IX_Vacations_Employee ON HR.tbl_Vacations(EmployeeID);
CREATE INDEX IX_Vacations_DateRange ON HR.tbl_Vacations(StartDate, EndDate);
CREATE INDEX IX_Vacations_Status ON HR.tbl_Vacations(Status);
CREATE INDEX IX_Vacations_StartDate ON HR.tbl_Vacations(StartDate);

-- 9. ÍNDICES PARA TABLA DE PERMISOS
PRINT '9. Creando índices para HR.tbl_Permissions...';
CREATE INDEX IX_Permissions_Employee ON HR.tbl_Permissions(EmployeeID);
CREATE INDEX IX_Permissions_DateRange ON HR.tbl_Permissions(StartDate, EndDate);
CREATE INDEX IX_Permissions_Status ON HR.tbl_Permissions(Status);
CREATE INDEX IX_Permissions_Type ON HR.tbl_Permissions(PermissionTypeID);
CREATE INDEX IX_Permissions_RequestDate ON HR.tbl_Permissions(RequestDate);

-- 10. ÍNDICES PARA TABLA DE HORAS EXTRA
PRINT '10. Creando índices para HR.tbl_Overtime...';
CREATE INDEX IX_Overtime_Employee ON HR.tbl_Overtime(EmployeeID);
CREATE INDEX IX_Overtime_WorkDate ON HR.tbl_Overtime(WorkDate);
CREATE INDEX IX_Overtime_Status ON HR.tbl_Overtime(Status);
CREATE INDEX IX_Overtime_Type ON HR.tbl_Overtime(OvertimeType);
CREATE INDEX IX_Overtime_EmployeeDate ON HR.tbl_Overtime(EmployeeID, WorkDate);

-- 11. ÍNDICES PARA TABLA DE PLANIFICACIÓN DE TIEMPO
PRINT '11. Creando índices para tablas de planificación...';
CREATE INDEX IX_TimePlanning_Dates ON HR.tbl_TimePlanning(StartDate, EndDate);
CREATE INDEX IX_TimePlanning_Status ON HR.tbl_TimePlanning(PlanStatusTypeID);
CREATE INDEX IX_TimePlanning_CreatedBy ON HR.tbl_TimePlanning(CreatedBy);
CREATE INDEX IX_TimePlanning_Type ON HR.tbl_TimePlanning(PlanType);

CREATE INDEX IX_TimePlanningEmployees_Employee ON HR.tbl_TimePlanningEmployees(EmployeeID);
CREATE INDEX IX_TimePlanningEmployees_Plan ON HR.tbl_TimePlanningEmployees(PlanID);
CREATE INDEX IX_TimePlanningEmployees_Status ON HR.tbl_TimePlanningEmployees(EmployeeStatusTypeID);

CREATE INDEX IX_TimePlanningExecution_Date ON HR.tbl_TimePlanningExecution(WorkDate, PlanEmployeeID);
CREATE INDEX IX_TimePlanningExecution_PlanEmployee ON HR.tbl_TimePlanningExecution(PlanEmployeeID);

-- 12. ÍNDICES PARA TABLA DE NÓMINA
PRINT '12. Creando índices para HR.tbl_Payroll...';
CREATE INDEX IX_Payroll_Employee ON HR.tbl_Payroll(EmployeeID);
CREATE INDEX IX_Payroll_Period ON HR.tbl_Payroll(Period);
CREATE INDEX IX_Payroll_Status ON HR.tbl_Payroll(Status);
CREATE INDEX IX_Payroll_EmployeePeriod ON HR.tbl_Payroll(EmployeeID, Period);

CREATE INDEX IX_PayrollLines_Payroll ON HR.tbl_PayrollLines(PayrollID);
CREATE INDEX IX_PayrollLines_Type ON HR.tbl_PayrollLines(LineType);

-- 13. ÍNDICES PARA TABLA DE MOVIMIENTOS DE PERSONAL
PRINT '13. Creando índices para HR.tbl_PersonnelMovements...';
CREATE INDEX IX_PersonnelMovements_Employee ON HR.tbl_PersonnelMovements(EmployeeID);
CREATE INDEX IX_PersonnelMovements_Contract ON HR.tbl_PersonnelMovements(ContractID);
CREATE INDEX IX_PersonnelMovements_Destination ON HR.tbl_PersonnelMovements(DestinationDepartmentID);
CREATE INDEX IX_PersonnelMovements_Date ON HR.tbl_PersonnelMovements(MovementDate);
CREATE INDEX IX_PersonnelMovements_Type ON HR.tbl_PersonnelMovements(MovementType);

-- 14. ÍNDICES PARA TABLA DE SUBROGACIONES
PRINT '14. Creando índices para HR.tbl_Subrogations...';
CREATE INDEX IX_Subrogations_Subrogated ON HR.tbl_Subrogations(SubrogatedEmployeeID);
CREATE INDEX IX_Subrogations_Subrogating ON HR.tbl_Subrogations(SubrogatingEmployeeID);
CREATE INDEX IX_Subrogations_DateRange ON HR.tbl_Subrogations(StartDate, EndDate);

-- 15. ÍNDICES PARA TABLAS GEOGRÁFICAS
PRINT '15. Creando índices para tablas geográficas...';
CREATE INDEX IX_Provinces_Country ON HR.tbl_Provinces(CountryID);
CREATE INDEX IX_Cantons_Province ON HR.tbl_Cantons(ProvinceID);

-- 16. ÍNDICES PARA TABLAS DE HISTORIA DE VIDA
PRINT '16. Creando índices para tablas de historia de vida...';
CREATE INDEX IX_Addresses_Person ON HR.tbl_Addresses(PersonID);
CREATE INDEX IX_Addresses_Geographic ON HR.tbl_Addresses(CountryID, ProvinceID, CantonID);

CREATE INDEX IX_EducationLevels_Person ON HR.tbl_EducationLevels(PersonID);
CREATE INDEX IX_EducationLevels_Institution ON HR.tbl_EducationLevels(InstitutionID);

CREATE INDEX IX_WorkExperiences_Person ON HR.tbl_WorkExperiences(PersonID);
CREATE INDEX IX_WorkExperiences_IsCurrent ON HR.tbl_WorkExperiences(IsCurrent);

CREATE INDEX IX_EmergencyContacts_Person ON HR.tbl_EmergencyContacts(PersonID);

CREATE INDEX IX_Trainings_Person ON HR.tbl_Trainings(PersonID);
CREATE INDEX IX_Trainings_DateRange ON HR.tbl_Trainings(StartDate, EndDate);

CREATE INDEX IX_Publications_Person ON HR.tbl_Publications(PersonID);
CREATE INDEX IX_Books_Person ON HR.tbl_Books(PersonID);

-- 17. ÍNDICES ESPECIALES PARA CONSULTAS FRECUENTES
PRINT '17. Creando índices especiales...';

-- Índice para búsqueda de empleados activos por departamento
CREATE INDEX IX_Employees_ActiveByDepartment ON HR.tbl_Employees(DepartmentID, IsActive) 
WHERE IsActive = 1;

-- Índice para vacaciones activas
CREATE INDEX IX_Vacations_Active ON HR.tbl_Vacations(EmployeeID, Status) 
WHERE Status IN ('Planned', 'InProgress');

-- Índice para permisos pendientes
CREATE INDEX IX_Permissions_Pending ON HR.tbl_Permissions(EmployeeID, Status) 
WHERE Status = 'Pending';

-- Índice para horas extra pendientes de aprobación
CREATE INDEX IX_Overtime_Pending ON HR.tbl_Overtime(EmployeeID, Status) 
WHERE Status IN ('Planned', 'Verified');

-- Índice para feriados activos
CREATE INDEX IX_Holidays_Active ON HR.tbl_Holidays(HolidayDate, IsActive) 
WHERE IsActive = 1;

PRINT 'TODOS LOS ÍNDICES CREADOS EXITOSAMENTE.';
GO