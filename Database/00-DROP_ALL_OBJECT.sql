-- =============================================
-- BLOQUE 0: ELIMINACIÓN DE OBJETOS EN ORDEN CORRECTO
-- =============================================

-- SCRIPT PARA ELIMINAR TODAS LAS FOREIGN KEYS Y TABLAS DE UN ESQUEMA
-- ADVERTENCIA: USAR SOLO EN ENTORNOS DE DESARROLLO

DECLARE @sql NVARCHAR(MAX) = N'';

-- 1. Generar comandos para eliminar todas las FOREIGN KEY constraints
SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
    + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + 
    ' DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.foreign_keys
WHERE OBJECT_SCHEMA_NAME(parent_object_id) = 'HR';

-- 2. Generar comandos para eliminar todas las tablas
SELECT @sql += 'DROP TABLE ' + QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME) + ';' + CHAR(13)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'HR' AND TABLE_TYPE = 'BASE TABLE';

-- Imprime el script para revisarlo (opcional, pero recomendado)
PRINT @sql;

-- Ejecuta el script generado
EXEC sp_executesql @sql;

PRINT 'Esquema HR limpiado exitosamente.';

/* 
SET NOCOUNT ON;
PRINT 'INICIANDO ELIMINACIÓN DE OBJETOS DE LA BASE DE DATOS...';

-- 1. ELIMINAR PROCEDIMIENTOS ALMACENADOS PRIMERO
PRINT '1. Eliminando procedimientos almacenados...';
IF OBJECT_ID('HR.sp_CalculateDailyAttendance','P') IS NOT NULL DROP PROCEDURE HR.sp_CalculateDailyAttendance;
IF OBJECT_ID('HR.sp_CalculateMonthlyPayroll','P') IS NOT NULL DROP PROCEDURE HR.sp_CalculateMonthlyPayroll;
IF OBJECT_ID('HR.sp_GenerateMonthlyAttendanceReport','P') IS NOT NULL DROP PROCEDURE HR.sp_GenerateMonthlyAttendanceReport;
IF OBJECT_ID('HR.sp_RegisterPersonnelMovement','P') IS NOT NULL DROP PROCEDURE HR.sp_RegisterPersonnelMovement;
IF OBJECT_ID('HR.sp_AssignEmployeeSubsidy','P') IS NOT NULL DROP PROCEDURE HR.sp_AssignEmployeeSubsidy;
IF OBJECT_ID('HR.sp_ApproveNightOvertime','P') IS NOT NULL DROP PROCEDURE HR.sp_ApproveNightOvertime;
IF OBJECT_ID('HR.sp_RequestVacation','P') IS NOT NULL DROP PROCEDURE HR.sp_RequestVacation;
IF OBJECT_ID('HR.sp_ApprovePermission','P') IS NOT NULL DROP PROCEDURE HR.sp_ApprovePermission;
IF OBJECT_ID('HR.sp_CalculateSubsidies','P') IS NOT NULL DROP PROCEDURE HR.sp_CalculateSubsidies;
IF OBJECT_ID('HR.sp_PlanOvertime','P') IS NOT NULL DROP PROCEDURE HR.sp_PlanOvertime;
IF OBJECT_ID('HR.sp_GenerateSubsidyComplianceReport','P') IS NOT NULL DROP PROCEDURE HR.sp_GenerateSubsidyComplianceReport;
IF OBJECT_ID('HR.sp_UpsertEmployeeSchedule','P') IS NOT NULL DROP PROCEDURE HR.sp_UpsertEmployeeSchedule;
IF OBJECT_ID('HR.sp_GetActiveSchedule','P') IS NOT NULL DROP PROCEDURE HR.sp_GetActiveSchedule;
IF OBJECT_ID('HR.sp_PlanTimeRecovery','P') IS NOT NULL DROP PROCEDURE HR.sp_PlanTimeRecovery;
IF OBJECT_ID('HR.sp_ApproveTimeRecovery','P') IS NOT NULL DROP PROCEDURE HR.sp_ApproveTimeRecovery;
IF OBJECT_ID('HR.sp_LogTimeRecovery','P') IS NOT NULL DROP PROCEDURE HR.sp_LogTimeRecovery;
IF OBJECT_ID('HR.sp_GetTimeRecoveryBalance','P') IS NOT NULL DROP PROCEDURE HR.sp_GetTimeRecoveryBalance;
IF OBJECT_ID('HR.sp_JustifyPunch','P') IS NOT NULL DROP PROCEDURE HR.sp_JustifyPunch;
IF OBJECT_ID('HR.sp_RecalculateAttendanceRange','P') IS NOT NULL DROP PROCEDURE HR.sp_RecalculateAttendanceRange;
IF OBJECT_ID('HR.sp_BulkImportPunches','P') IS NOT NULL DROP PROCEDURE HR.sp_BulkImportPunches;
IF OBJECT_ID('HR.sp_ValidateSubrogationWindow','P') IS NOT NULL DROP PROCEDURE HR.sp_ValidateSubrogationWindow;
IF OBJECT_ID('HR.sp_EmployeesOnLeave','P') IS NOT NULL DROP PROCEDURE HR.sp_EmployeesOnLeave;
IF OBJECT_ID('HR.sp_EmployeesAboutToVacation','P') IS NOT NULL DROP PROCEDURE HR.sp_EmployeesAboutToVacation;
IF OBJECT_ID('HR.sp_ListPendingPermissions','P') IS NOT NULL DROP PROCEDURE HR.sp_ListPendingPermissions;
IF OBJECT_ID('HR.sp_GetPayrollBreakdown','P') IS NOT NULL DROP PROCEDURE HR.sp_GetPayrollBreakdown;
IF OBJECT_ID('HR.sp_ClosePayrollPeriod','P') IS NOT NULL DROP PROCEDURE HR.sp_ClosePayrollPeriod;
IF OBJECT_ID('HR.sp_ReopenPayrollPeriod','P') IS NOT NULL DROP PROCEDURE HR.sp_ReopenPayrollPeriod;

-- 2. ELIMINAR VISTAS
PRINT '2. Eliminando vistas...';
IF OBJECT_ID('HR.vw_EmployeeDetails','V') IS NOT NULL DROP VIEW HR.vw_EmployeeDetails;
IF OBJECT_ID('HR.vw_EmployeeLeaveStatus','V') IS NOT NULL DROP VIEW HR.vw_EmployeeLeaveStatus;
IF OBJECT_ID('HR.vw_EmployeeMovementHistory','V') IS NOT NULL DROP VIEW HR.vw_EmployeeMovementHistory;
IF OBJECT_ID('HR.vw_PendingOvertimeApproval','V') IS NOT NULL DROP VIEW HR.vw_PendingOvertimeApproval;

-- 3. ELIMINAR TRIGGERS
PRINT '3. Eliminando triggers...';
IF OBJECT_ID('HR.trg_Contracts_SalaryHistory','TR') IS NOT NULL DROP TRIGGER HR.trg_Contracts_SalaryHistory;
IF OBJECT_ID('HR.trg_Punch_Validations','TR') IS NOT NULL DROP TRIGGER HR.trg_Punch_Validations;
IF OBJECT_ID('HR.trg_Subrogations_NoOverlap','TR') IS NOT NULL DROP TRIGGER HR.trg_Subrogations_NoOverlap;

-- 4. ELIMINAR FUNCIONES
PRINT '4. Eliminando funciones...';
IF OBJECT_ID('HR.fn_GetBusinessDays') IS NOT NULL DROP FUNCTION HR.fn_GetBusinessDays;

-- 5. ELIMINAR TIPOS DE TABLA DEFINIDOS POR EL USUARIO
PRINT '5. Eliminando tipos de tabla...';
IF TYPE_ID('HR.tt_PunchImport') IS NOT NULL DROP TYPE HR.tt_PunchImport;

-- 6. ELIMINAR TABLAS HIJO PRIMERO (CON FOREIGN KEYS)
PRINT '6. Eliminando tablas hijas...';

-- 6.1 Tablas de nómina y líneas
IF OBJECT_ID('HR.tbl_PayrollLines','U') IS NOT NULL DROP TABLE HR.tbl_PayrollLines;
IF OBJECT_ID('HR.tbl_Payroll','U') IS NOT NULL DROP TABLE HR.tbl_Payroll;

-- 6.2 Tablas de movimientos y subrogaciones
IF OBJECT_ID('HR.tbl_PersonnelMovements','U') IS NOT NULL DROP TABLE HR.tbl_PersonnelMovements;
IF OBJECT_ID('HR.tbl_Subrogations','U') IS NOT NULL DROP TABLE HR.tbl_Subrogations;

-- 6.3 Tablas de planificación de tiempo (nuevas)
IF OBJECT_ID('HR.tbl_TimePlanningExecution','U') IS NOT NULL DROP TABLE HR.tbl_TimePlanningExecution;
IF OBJECT_ID('HR.tbl_TimePlanningEmployees','U') IS NOT NULL DROP TABLE HR.tbl_TimePlanningEmployees;
IF OBJECT_ID('HR.tbl_TimePlanning','U') IS NOT NULL DROP TABLE HR.tbl_TimePlanning;

-- 6.4 Tablas de recuperación de tiempo
IF OBJECT_ID('HR.tbl_TimeRecoveryLogs','U') IS NOT NULL DROP TABLE HR.tbl_TimeRecoveryLogs;
IF OBJECT_ID('HR.tbl_TimeRecoveryPlans','U') IS NOT NULL DROP TABLE HR.tbl_TimeRecoveryPlans;

-- 6.5 Tablas de horas extra
IF OBJECT_ID('HR.tbl_Overtime','U') IS NOT NULL DROP TABLE HR.tbl_Overtime;

-- 6.6 Tablas de asistencia y cálculos
IF OBJECT_ID('HR.tbl_AttendanceCalculations','U') IS NOT NULL DROP TABLE HR.tbl_AttendanceCalculations;
IF OBJECT_ID('HR.tbl_PunchJustifications','U') IS NOT NULL DROP TABLE HR.tbl_PunchJustifications;
IF OBJECT_ID('HR.tbl_AttendancePunches','U') IS NOT NULL DROP TABLE HR.tbl_AttendancePunches;

-- 6.7 Tablas de permisos y vacaciones
IF OBJECT_ID('HR.tbl_Permissions','U') IS NOT NULL DROP TABLE HR.tbl_Permissions;
IF OBJECT_ID('HR.tbl_Vacations','U') IS NOT NULL DROP TABLE HR.tbl_Vacations;

-- 6.8 Tablas de historial salarial
IF OBJECT_ID('HR.tbl_SalaryHistory','U') IS NOT NULL DROP TABLE HR.tbl_SalaryHistory;

-- 6.9 Tablas de contratos
IF OBJECT_ID('HR.tbl_Contracts','U') IS NOT NULL DROP TABLE HR.tbl_Contracts;

-- 6.10 Tablas de horarios
IF OBJECT_ID('HR.tbl_EmployeeSchedules','U') IS NOT NULL DROP TABLE HR.tbl_EmployeeSchedules;
IF OBJECT_ID('HR.tbl_Schedules','U') IS NOT NULL DROP TABLE HR.tbl_Schedules;

-- 6.11 Tablas de historia de vida (dependen de personas)
IF OBJECT_ID('HR.tbl_Books','U') IS NOT NULL DROP TABLE HR.tbl_Books;
IF OBJECT_ID('HR.tbl_Publications','U') IS NOT NULL DROP TABLE HR.tbl_Publications;
IF OBJECT_ID('HR.tbl_BankAccounts','U') IS NOT NULL DROP TABLE HR.tbl_BankAccounts;
IF OBJECT_ID('HR.tbl_WorkExperiences','U') IS NOT NULL DROP TABLE HR.tbl_WorkExperiences;
IF OBJECT_ID('HR.tbl_Trainings','U') IS NOT NULL DROP TABLE HR.tbl_Trainings;
IF OBJECT_ID('HR.tbl_FamilyBurden','U') IS NOT NULL DROP TABLE HR.tbl_FamilyBurden;
IF OBJECT_ID('HR.tbl_CatastrophicIllnesses','U') IS NOT NULL DROP TABLE HR.tbl_CatastrophicIllnesses;
IF OBJECT_ID('HR.tbl_EmergencyContacts','U') IS NOT NULL DROP TABLE HR.tbl_EmergencyContacts;
IF OBJECT_ID('HR.tbl_EducationLevels','U') IS NOT NULL DROP TABLE HR.tbl_EducationLevels;
IF OBJECT_ID('HR.tbl_Institutions','U') IS NOT NULL DROP TABLE HR.tbl_Institutions;
IF OBJECT_ID('HR.tbl_Addresses','U') IS NOT NULL DROP TABLE HR.tbl_Addresses;

-- 6.12 Tablas geográficas (dependen entre sí)
IF OBJECT_ID('HR.tbl_Cantons','U') IS NOT NULL DROP TABLE HR.tbl_Cantons;
IF OBJECT_ID('HR.tbl_Provinces','U') IS NOT NULL DROP TABLE HR.tbl_Provinces;
IF OBJECT_ID('HR.tbl_Countries','U') IS NOT NULL DROP TABLE HR.tbl_Countries;

-- 7. ELIMINAR TABLAS PADRE (SIN DEPENDENCIAS O CON AUTO-REFERENCIAS)
PRINT '7. Eliminando tablas padre...';

-- 7.1 Tablas de empleados (tiene auto-referencia)
IF OBJECT_ID('HR.tbl_Employees','U') IS NOT NULL DROP TABLE HR.tbl_Employees;

-- 7.2 Tablas de departamentos (tiene auto-referencia)
IF OBJECT_ID('HR.tbl_Departments','U') IS NOT NULL DROP TABLE HR.tbl_Departments;

-- 7.3 Otras tablas padre
IF OBJECT_ID('HR.tbl_jobs','U') IS NOT NULL DROP TABLE HR.tbl_jobs;
IF OBJECT_ID('HR.tbl_Holidays','U') IS NOT NULL DROP TABLE HR.tbl_Holidays;
IF OBJECT_ID('HR.tbl_PermissionTypes','U') IS NOT NULL DROP TABLE HR.tbl_PermissionTypes;
IF OBJECT_ID('HR.tbl_OvertimeConfig','U') IS NOT NULL DROP TABLE HR.tbl_OvertimeConfig;

-- 7.4 Tabla maestra de personas
IF OBJECT_ID('HR.tbl_People','U') IS NOT NULL DROP TABLE HR.tbl_People;

-- 7.5 Tabla de tipos de referencia (usada por muchas tablas)
IF OBJECT_ID('HR.ref_Types','U') IS NOT NULL DROP TABLE HR.ref_Types;

-- 7.6 Tabla de auditoría (independiente)
IF OBJECT_ID('HR.tbl_Audit','U') IS NOT NULL DROP TABLE HR.tbl_Audit;

-- 8. VERIFICAR Y ELIMINAR ESQUEMA SI ESTÁ VACÍO
PRINT '8. Verificando estado final...';
DECLARE @ObjectCount INT = 0;
SELECT @ObjectCount = COUNT(*) 
FROM sys.objects 
WHERE schema_id = SCHEMA_ID('HR') 
  AND type IN ('U', 'V', 'P', 'FN', 'TR');

IF @ObjectCount = 0
BEGIN
    PRINT 'Esquema HR está vacío. Considerar eliminar esquema con: DROP SCHEMA HR;';
END
ELSE
BEGIN
    PRINT 'Atención: Todavía existen ' + CAST(@ObjectCount AS VARCHAR) + ' objetos en el esquema HR.';
    -- Listar objetos restantes para debugging
    SELECT 
        name AS ObjetoRestante,
        type_desc AS Tipo
    FROM sys.objects 
    WHERE schema_id = SCHEMA_ID('HR') 
    ORDER BY type_desc, name;
END

PRINT 'ELIMINACIÓN COMPLETADA. Todos los objetos eliminados en orden correcto.';
GO */