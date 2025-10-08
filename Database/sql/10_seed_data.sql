-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
IF NOT EXISTS (SELECT 1 FROM HR.ref_Types WHERE Category = 'EMPLOYEE_PLAN_STATUS')
BEGIN
    INSERT INTO HR.ref_Types (Category, Name, Description) VALUES
    ('EMPLOYEE_PLAN_STATUS', 'Asignado', 'Empleado asignado a la planificación'),
    ('EMPLOYEE_PLAN_STATUS', 'En Proceso', 'Empleado ejecutando las horas asignadas'),
    ('EMPLOYEE_PLAN_STATUS', 'Ejecutado', 'Empleado completó todas las horas asignadas'),
    ('EMPLOYEE_PLAN_STATUS', 'Cancelado', 'Participación del empleado fue cancelada');
    
    PRINT 'Estados de empleado en planificación insertados en ref_Types (EMPLOYEE_PLAN_STATUS)';
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
-------------------------------------------------------------------------------
-- 8. INSERTAR DATOS BÁSICOS DE CONFIGURACIÓN
-------------------------------------------------------------------------------

-- 8.1 Insertar feriados básicos de Ecuador 2025
IF NOT EXISTS (SELECT 1 FROM HR.tbl_Holidays WHERE YEAR(HolidayDate) = 2025)
BEGIN
    INSERT INTO HR.tbl_Holidays (Name, HolidayDate, IsActive, Description) VALUES
    ('Año Nuevo', '2025-01-01', 1, 'Año Nuevo'),
    ('Carnaval', '2025-03-03', 1, 'Carnaval'),
    ('Carnaval', '2025-03-04', 1, 'Carnaval'),
    ('Viernes Santo', '2025-04-18', 1, 'Semana Santa'),
    ('Día del Trabajo', '2025-05-01', 1, 'Día Internacional del Trabajo'),
    ('Batalla de Pichincha', '2025-05-24', 1, 'Batalla de Pichincha'),
    ('Primer Grito de Independencia', '2025-08-10', 1, 'Primer Grito de Independencia'),
    ('Independencia de Guayaquil', '2025-10-09', 1, 'Independencia de Guayaquil'),
    ('Día de los Difuntos', '2025-11-02', 1, 'Día de los Difuntos'),
    ('Independencia de Cuenca', '2025-11-03', 1, 'Independencia de Cuenca'),
    ('Navidad', '2025-12-25', 1, 'Navidad');
    
    PRINT 'Feriados 2025 insertados';
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
/*se agrega esta parte para alterar el trigger para corregir la validacion de hora de picada */
ALTER TRIGGER [HR].[trg_Punch_Validations]
ON [HR].[tbl_AttendancePunches]
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Crear tabla temporal para almacenar los inserted con IDs generados
    DECLARE @OutputTable TABLE (PunchId INT);

    -- Verificar vacaciones
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN HR.tbl_Vacations v
          ON v.EmployeeID = i.EmployeeID
         AND v.Status IN ('InProgress')
         AND CAST(i.PunchTime AS date) BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR(N'El empleado está de vacaciones: no se permiten picadas.',16,1);
        RETURN;
    END

    -- Verificar diferencia de tiempo
    IF EXISTS (
        SELECT 1
        FROM inserted i
        CROSS APPLY (
            SELECT TOP 1 p.PunchTime
            FROM HR.tbl_AttendancePunches p
            WHERE p.EmployeeID = i.EmployeeID
            ORDER BY p.PunchTime DESC
        ) prev
        WHERE DATEDIFF(MINUTE, prev.PunchTime, i.PunchTime) < 5
    )
    BEGIN
        RAISERROR(N'La diferencia entre picadas debe ser al menos de 5 minutos.',16,1);
        RETURN;
    END

    -- Insertar válido y capturar los IDs generados
    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt)
    OUTPUT INSERTED.PunchId INTO @OutputTable
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude, CreatedAt
    FROM inserted;

    -- Devolver los IDs generados
    SELECT PunchId FROM @OutputTable;
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
INSERT INTO HR.ref_Types (Category, Name) 
VALUES 
    ('GENDER_TYPE', 'Hombre'),
    ('GENDER_TYPE', 'Mujer'),
	('GENDER_TYPE', 'No binario'),
	('GENDER_TYPE', 'AGénero'),
	('GENDER_TYPE', 'Mujer Trans'),
	('GENDER_TYPE', 'Hombre Trans'),
	('GENDER_TYPE', 'Prefiero no decir'),
	('GENDER_TYPE', 'Otros'),
	('SEX_TYPE', 'Masculino'),
	('SEX_TYPE', 'Femenino'),
	('SEX_TYPE', 'Prefiero no decir');
	
	
	INSERT INTO HR.ref_Types (Category, Name)
	VALUES
    ('ACTION_TYPE', 'INGRESO'),
    ('ACTION_TYPE', 'REINGRESO'),
    ('ACTION_TYPE', 'RESTITUCIÓN'),
    ('ACTION_TYPE', 'ASCENSO'),
    ('ACTION_TYPE', 'TRASLADO'),
    ('ACTION_TYPE', 'TRASPASO'),
    ('ACTION_TYPE', 'CAMBIO ADMINISTRATIVO'),
    ('ACTION_TYPE', 'INTERCAMBIO VOLUNTARIO'),
    ('ACTION_TYPE', 'LICENCIA'),
    ('ACTION_TYPE', 'COMISIÓN DE SERVICIOS'),
    ('ACTION_TYPE', 'SANCIONES'),
    ('ACTION_TYPE', 'INCREMENTO RMU'),
    ('ACTION_TYPE', 'SUBROGACIÓN'),
    ('ACTION_TYPE', 'ENCARGO'),
    ('ACTION_TYPE', 'CESACIÓN DE FUNCIONES'),
    ('ACTION_TYPE', 'DESTITUCIÓN'),
    ('ACTION_TYPE', 'VACACIONES'),
    ('ACTION_TYPE', 'REVISIÓN CLASI. PUESTO'),
    ('ACTION_TYPE', 'OTRO(DETALLAR)'),
    ('ACTION_TYPE', 'NOMBRAMIENTO PROVISIONAL');
	
	/*INSERT INTO HR.ref_Types (Category, Name)
	VALUES
    ('ROW_STATUS', 'ACTIVO'),
    ('ROW_STATUS', 'INACTIVO'),
	('ACTION_STATUS', 'ACTIVO'),
    ('ACTION_STATUS', 'INACTIVO'),*/
	
	-- Tipos principales de justificación
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION', 'PUNCH', 'Justificación de picada específica (entrada/salida olvidada)', 1),
('JUSTIFICATION', 'HOURS', 'Justificación de horas específicas (ausencia parcial)', 1),
('JUSTIFICATION', 'FULL_DAY', 'Justificación de día completo (ausencia total)', 1);

-- Subtipos más específicos para mayor granularidad (opcional)
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION_REASON', 'MEDICAL', 'Cita médica o incapacidad médica', 1),
('JUSTIFICATION_REASON', 'PERSONAL', 'Asunto personal urgente', 1),
('JUSTIFICATION_REASON', 'FAMILY_EMERGENCY', 'Emergencia familiar', 1),
('JUSTIFICATION_REASON', 'TECHNICAL_ISSUE', 'Problemas técnicos (sistema, tarjeta, etc.)', 1),
('JUSTIFICATION_REASON', 'WORK_MEETING', 'Reunión de trabajo externa', 1),
('JUSTIFICATION_REASON', 'TRAINING', 'Capacitación o entrenamiento', 1),
('JUSTIFICATION_REASON', 'COURT_DUTY', 'Deber cívico (jurado, testigo, etc.)', 1),
('JUSTIFICATION_REASON', 'BEREAVEMENT', 'Duelo familiar', 1),
('JUSTIFICATION_REASON', 'TRANSPORTATION', 'Problemas de transporte', 1),
('JUSTIFICATION_REASON', 'WEATHER', 'Condiciones climáticas adversas', 1);

-- Tipos de estado para el flujo de aprobación (si quieres usar la tabla de referencia también para esto)
INSERT INTO HR.ref_Types (Category, Name, Description, IsActive) VALUES
('JUSTIFICATION_STATUS', 'PENDING', 'Pendiente de aprobación', 1),
('JUSTIFICATION_STATUS', 'APPROVED', 'Aprobado por supervisor', 1),
('JUSTIFICATION_STATUS', 'REJECTED', 'Rechazado por supervisor', 1),
('JUSTIFICATION_STATUS', 'CANCELLED', 'Cancelado por el empleado', 1);
--('JUSTIFICATION_STATUS', 'UNDER_REVIEW', 'En revisión por RH', 1);
	
	
-- Catálogos básicos
INSERT INTO HR.tbl_OvertimeConfig(OvertimeType, Factor, Description)
VALUES
 ('Ordinaria', 1.50, 'Hora extra ordinaria'),
 ('Nocturna', 1.75, 'Hora extra nocturna'),
 ('Feriado',  2.00, 'Hora extra en feriado');

INSERT INTO HR.tbl_PermissionTypes(Name, DeductsFromVacation, RequiresApproval, MaxDays)
VALUES
 ('Vacaciones', 1, 1, 30),
 ('Comisión de servicios', 0, 1, 10),
 ('Enfermedad', 0, 1, 15),
 ('Asuntos personales', 0, 1, 5);

-- Facultades y Departamentos
INSERT INTO HR.tbl_Faculties(Name, IsActive) VALUES
 ('Ingeniería en Sistemas', 1),
 ('Jurídicas y Sociales', 1);

/*INSERT INTO HR.tbl_Departments(FacultyID, Name, IsActive) VALUES
 (1, 'DITIC', 1),
 (1, 'Académico', 1),
 (2, 'Administración', 1);*/

-- Personas
INSERT INTO HR.tbl_People(FirstName, LastName, IDCard, Email, Phone, BirthDate, Sex, Address)
VALUES
 ('Henry','Flores','1801234567','henry.flores@uta.edu.ec','099000111','1988-05-10','M','Ambato'),
 ('Ana','Pérez','1807654321','ana.perez@uta.edu.ec','098111222','1990-09-21','F','Ambato'),
 ('Luis','García','1712345678','luis.garcia@uta.edu.ec','097222333','1985-02-12','M','Quito'),
 ('María','Lozano','1711122233','maria.lozano@uta.edu.ec','096333444','1992-11-30','F','Ambato');

-- Empleados (EmployeeID = PersonID)
INSERT INTO HR.tbl_Employees(EmployeeID, Type, DepartmentID, ImmediateBossID, HireDate, IsActive)
VALUES
 (1, 'Administrative_LOSEP', 1, NULL, '2020-03-01', 1),
 (2, 'Employee_CT', 1, 1, '2022-01-15', 1),
 (3, 'Teacher_LOSE', 2, 1, '2019-09-01', 1),
 (4, 'Coordinator', 3, 1, '2021-07-01', 1);

-- Asignar decano (debe existir empleado)
UPDATE HR.tbl_Faculties SET DeanEmployeeID = 1 WHERE FacultyID = 1;

-- Schedules
INSERT INTO HR.tbl_Schedules(Description, EntryTime, ExitTime, WorkingDays, RequiredHoursPerDay, HasLunchBreak, LunchStart, LunchEnd, IsRotating)
VALUES
 ('Admin L-V 08:00-17:00', '08:00', '17:00', '1,2,3,4,5', 8.00, 1, '12:30','13:30', 0),
 ('Docente L-V 07:00-15:00', '07:00', '15:00', '1,2,3,4,5', 8.00, 1, '12:00','13:00', 0);

-- EmployeeSchedules
INSERT INTO HR.tbl_EmployeeSchedules(EmployeeID, ScheduleID, ValidFrom) VALUES
 (1, 1, '2023-01-01'),
 (2, 1, '2023-01-01'),
 (3, 2, '2023-01-01'),
 (4, 1, '2023-01-01');

-- Contratos
INSERT INTO HR.tbl_Contracts(EmployeeID, ContractType, StartDate, EndDate, BaseSalary, CreatedBy)
VALUES
 (1, 'Indefinido', '2020-03-01', NULL, 1800.00, 1),
 (2, 'Temporal',  '2022-01-15', NULL, 1200.00, 1),
 (3, 'Indefinido', '2019-09-01', NULL, 2200.00, 1),
 (4, 'Indefinido', '2021-07-01', NULL, 2000.00, 1);

-- Vacaciones (una en progreso para empleado 2)
INSERT INTO HR.tbl_Vacations(EmployeeID, StartDate, EndDate, DaysGranted, DaysTaken, Status)
VALUES
 (1, '2025-02-03', '2025-02-07', 5, 5, 'Completed'),
 (2, '2025-08-11', '2025-08-15', 5, 0, 'InProgress'); -- cuidado con picadas en estas fechas

-- Permisos
INSERT INTO HR.tbl_Permissions(EmployeeID, PermissionTypeID, StartDate, EndDate, ChargedToVacation, ApprovedBy, Justification, Status)
VALUES
 (3, 3, '2025-03-10', '2025-03-12', 0, 1, 'Reposo médico', 'Approved'),
 (4, 4, '2025-04-22', '2025-04-22', 0, 1, 'Trámite personal', 'Pending');

-- Picadas (válidas: separadas por >=5 min y fuera de vacaciones en progreso)
INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID)
VALUES
 (1, '2025-08-05 08:01', 'In',  'BIO-01'),
 (1, '2025-08-05 12:31', 'Out', 'BIO-01'),
 (1, '2025-08-05 13:35', 'In',  'BIO-01'),
 (1, '2025-08-05 17:05', 'Out', 'BIO-01'),
 (2, '2025-08-08 08:00', 'In',  'BIO-01'), -- 08/11-08/15 está en vacaciones; 08/08 permite
 (2, '2025-08-08 17:10', 'Out', 'BIO-01');

-- Justificación de picadas
INSERT INTO HR.tbl_PunchJustifications(PunchID, BossEmployeeID, Reason, Approved, ApprovedAt)
VALUES
 (1, 1, 'Retraso por tráfico', 1, '2025-08-05 09:00');

-- Cálculos (ejemplo)
INSERT INTO HR.tbl_AttendanceCalculations(EmployeeID, WorkDate, FirstPunchIn, LastPunchOut, TotalWorkedMinutes, RegularMinutes, OvertimeMinutes, Status)
VALUES
 (1, '2025-08-05', '2025-08-05 08:01', '2025-08-05 17:05', 544, 480, 64, 'Approved');

/* -- Horas extra (empleado 3 NO debe tener permiso ese día)
INSERT INTO HR.tbl_Overtime(EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, Factor, ActualHours, PaymentAmount)
VALUES
 (3, '2025-05-03', 'Ordinaria', 2.00, 'Planned', 1, 1.50, 0, 0); */

-- Plan de recuperación de horas
INSERT INTO HR.tbl_TimeRecoveryPlans(EmployeeID, OwedMinutes, PlanDate, FromTime, ToTime, Reason, CreatedBy)
VALUES
 (2, 60, '2025-08-18', '17:00', '18:10', 'Recuperar 1h por salida anticipada', 1);

INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
VALUES
 (1, '2025-08-18', 70, 1, '2025-08-18 18:15');

-- Subrogación (sin solapamiento)
INSERT INTO HR.tbl_Subrogations(SubrogatedEmployeeID, SubrogatingEmployeeID, StartDate, EndDate, PermissionID, VacationID, Reason)
VALUES
 (1, 2, '2025-06-01', '2025-06-15', NULL, NULL, 'Cobertura por comisión');

-- Movimiento de personal
INSERT INTO HR.tbl_PersonnelMovements(EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID, MovementDate, MovementType, Reason, CreatedBy)
VALUES
 (2, 2, 1, 2, '2025-07-01', 'Transfer', 'Rotación a área académica', 1);

-- Nómina y líneas
INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary, Status, PaymentDate, BankAccount)
VALUES
 (1, '2025-08', 1800.00, 'Paid', '2025-08-10', 'BANCO-001-XXXX'),
 (2, '2025-08', 1200.00, 'Pending', NULL, 'BANCO-002-XXXX');

INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
VALUES
 (1, 'Earning',  'Sueldo Base', 1, 1800.00, NULL),
 (1, 'Overtime', 'Horas extra',  8,   5.00, 'Pago variable'),
 (1, 'Deduction','Aporte IESS',  1,  -9.45, 'Porcentaje aplicado'),
 (2, 'Earning',  'Sueldo Base',  1, 1200.00, NULL),
 (2, 'Subsidy',  'Transporte',   1,   30.00, 'Política institucional');

PRINT 'Datos de prueba insertados correctamente.';
GO