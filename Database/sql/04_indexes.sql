-- Source: 01_1-ScriptHojaVida.sql
/* =========================================================
   ÍNDICES PARA OPTIMIZACIÓN
   ========================================================= */
-- Personas
CREATE INDEX IX_People_IDCard ON HR.tbl_People(IDCard);
CREATE INDEX IX_People_LastName ON HR.tbl_People(LastName);

-- Direcciones
CREATE INDEX IX_Addresses_Person ON HR.tbl_Addresses(PersonID);

-- Educación
CREATE INDEX IX_Education_Person ON HR.tbl_EducationLevels(PersonID);

-- Experiencia Laboral
CREATE INDEX IX_WorkExp_Person ON HR.tbl_WorkExperiences(PersonID);
CREATE INDEX IX_WorkExp_IsCurrent ON HR.tbl_WorkExperiences(IsCurrent);

-- Libros y Publicaciones
CREATE INDEX IX_Books_Person ON HR.tbl_Books(PersonID);
CREATE INDEX IX_Publications_Person ON HR.tbl_Publications(PersonID);

-- Índices geográficos
CREATE INDEX IX_Provinces_Country ON HR.tbl_Provinces(CountryID);
CREATE INDEX IX_Cantons_Province ON HR.tbl_Cantons(ProvinceID);
CREATE INDEX IX_Addresses_Geographic ON HR.tbl_Addresses(CountryID, ProvinceID, CantonID);
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 5. AGREGAR CONSTRAINTS CHECK
-------------------------------------------------------------------------------

-- 5.1 Check constraints para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanning_PlanType')
    ALTER TABLE HR.tbl_TimePlanning ADD CONSTRAINT CK_TimePlanning_PlanType 
    CHECK (PlanType IN ('Overtime','Recovery'));

-- 5.2 Check constraints para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Hours')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Hours 
    CHECK (AssignedHours >= 0 OR AssignedHours IS NULL);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TimePlanningEmployees_Minutes')
    ALTER TABLE HR.tbl_TimePlanningEmployees ADD CONSTRAINT CK_TimePlanningEmployees_Minutes 
    CHECK (AssignedMinutes >= 0 OR AssignedMinutes IS NULL);

-- 5.3 Unique constraint para feriados (misma fecha no puede repetirse)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Holidays_Date' AND object_id = OBJECT_ID('HR.tbl_Holidays'))
    CREATE UNIQUE INDEX UX_Holidays_Date ON HR.tbl_Holidays(HolidayDate) WHERE IsActive = 1;

PRINT 'Check constraints agregados exitosamente';
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 6. CREAR ÍNDICES
-------------------------------------------------------------------------------

-- 6.1 Índices para tbl_TimePlanning
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanning_Dates' AND object_id = OBJECT_ID('HR.tbl_TimePlanning'))
    CREATE INDEX IX_TimePlanning_Dates ON HR.tbl_TimePlanning(StartDate, EndDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanning_Status' AND object_id = OBJECT_ID('HR.tbl_TimePlanning'))
    CREATE INDEX IX_TimePlanning_Status ON HR.tbl_TimePlanning(PlanStatusTypeID);

-- 6.2 Índices para tbl_TimePlanningEmployees
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Employee' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Employee ON HR.tbl_TimePlanningEmployees(EmployeeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Status' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Status ON HR.tbl_TimePlanningEmployees(EmployeeStatusTypeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningEmployees_Plan' AND object_id = OBJECT_ID('HR.tbl_TimePlanningEmployees'))
    CREATE INDEX IX_TimePlanningEmployees_Plan ON HR.tbl_TimePlanningEmployees(PlanID);

-- 6.3 Índices para tbl_TimePlanningExecution
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningExecution_Date' AND object_id = OBJECT_ID('HR.tbl_TimePlanningExecution'))
    CREATE INDEX IX_TimePlanningExecution_Date ON HR.tbl_TimePlanningExecution(WorkDate, PlanEmployeeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimePlanningExecution_PlanEmployee' AND object_id = OBJECT_ID('HR.tbl_TimePlanningExecution'))
    CREATE INDEX IX_TimePlanningExecution_PlanEmployee ON HR.tbl_TimePlanningExecution(PlanEmployeeID);

-- 6.4 Índices para tbl_Holidays
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Holidays_Date' AND object_id = OBJECT_ID('HR.tbl_Holidays'))
    CREATE INDEX IX_Holidays_Date ON HR.tbl_Holidays(HolidayDate) WHERE IsActive = 1;

PRINT 'Índices creados exitosamente';
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 3) ÍNDICES
-------------------------------------------------------------------------------
CREATE INDEX IX_Employees_Department       ON HR.tbl_Employees(DepartmentID);
CREATE INDEX IX_Employees_Boss             ON HR.tbl_Employees(ImmediateBossID);
CREATE INDEX IX_Punch_EmployeeDate         ON HR.tbl_AttendancePunches(EmployeeID, CONVERT(date, PunchTime));
CREATE INDEX IX_OT_EmployeeDate            ON HR.tbl_Overtime(EmployeeID, WorkDate);
CREATE INDEX IX_Vacations_EmployeeDate     ON HR.tbl_Vacations(EmployeeID, StartDate, EndDate);
CREATE INDEX IX_Permissions_EmployeeDate   ON HR.tbl_Permissions(EmployeeID, StartDate, EndDate);

CREATE UNIQUE INDEX UX_hr_employees_Email ON hr.tbl_employees(Email);

-- Índices adicionales para mejorar performance
CREATE NONCLUSTERED INDEX [IX_PunchJustifications_Employee] ON [HR].[tbl_PunchJustifications]
(
	[EmployeeID] ASC,
	[Status] ASC
)
INCLUDE ([JustificationTypeID], [JustificationDate], [CreatedAt])
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE NONCLUSTERED INDEX [IX_PunchJustifications_Boss] ON [HR].[tbl_PunchJustifications]
(
	[BossEmployeeID] ASC,
	[Status] ASC
)
GO