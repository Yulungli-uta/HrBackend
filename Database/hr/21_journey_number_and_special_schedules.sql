-- ============================================================
-- Extensión de esquema: turno doble de guardias (JourneyNumber) +
-- horarios especiales de empleados (sustituto/maternidad/lactancia/otro)
-- Generado: 2026-09-09 / 2026-09-10
--
-- 1) HR.tbl_AttendanceCalculations.JourneyNumber: distingue la 1ra/2da
--    jornada del mismo empleado el mismo WorkDate (turno doble de guardias
--    en grupos especiales, ej. CELESTE mañana+noche). La clave única pasa
--    de (EmployeeID, WorkDate) a (EmployeeID, WorkDate, JourneyNumber).
-- 2) HR.tbl_EmployeeSpecialSchedules: horario especial individual, tabla
--    autocontenida (sin FK a tbl_Schedules a propósito, para no ensuciar
--    el catálogo compartido con horarios de un solo uso). No lleva
--    ValidFrom/ValidTo propio — la vigencia la lleva tbl_EmployeeSchedules.
-- 3) HR.tbl_EmployeeSchedules.ScheduleID pasa a NULLABLE +
--    EmployeeSpecialScheduleId (nullable, FK) — mutuamente excluyentes
--    (CK_EmployeeSchedules_ScheduleOrSpecial).
-- 4) HR.tbl_AttendanceCalculations.EmployeeSpecialScheduleId: registra qué
--    horario especial (si aplica) se usó ese día, para trazabilidad.
-- 5) Catálogo ref_Types: categoría EMPLOYEE_SPECIAL_SCHEDULE_TYPE
--    (SUSTITUTO/MATERNIDAD/LACTANCIA/OTRO).
-- 6) 2 horarios de catálogo nuevos (HR.tbl_Schedules), jornada única sin
--    almuerzo: 06:00-14:30 y 14:00-22:30 — patrones de conteo>1 del Excel
--    HORARIOS UNIFICADOS 2026 que no existían así en el catálogo.
--
-- Los 8 SP del pipeline de asistencia que usan JourneyNumber/horario
-- especial (sp_ProcessAttendanceBaseDay, LeavesDay, RecoveryDay,
-- FinalizeDay, JustificationsDay, PlanningDay, TimePlanningForEmployeeDay,
-- RunDate) están en 06_procedures.sql — sp_ProcessGuardAttendanceDate en
-- 08_guard_attendance.sql.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar.
-- ============================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
GO

-- 1) Catálogo de tipo de caso especial ---------------------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_SPECIAL_SCHEDULE_TYPE' AND [Name] = 'SUSTITUTO')
    INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive]) VALUES ('EMPLOYEE_SPECIAL_SCHEDULE_TYPE', 'SUSTITUTO', 'Horario especial por sustitucion', 1);
GO
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_SPECIAL_SCHEDULE_TYPE' AND [Name] = 'MATERNIDAD')
    INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive]) VALUES ('EMPLOYEE_SPECIAL_SCHEDULE_TYPE', 'MATERNIDAD', 'Horario especial por maternidad', 1);
GO
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_SPECIAL_SCHEDULE_TYPE' AND [Name] = 'LACTANCIA')
    INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive]) VALUES ('EMPLOYEE_SPECIAL_SCHEDULE_TYPE', 'LACTANCIA', 'Horario especial por lactancia', 1);
GO
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'EMPLOYEE_SPECIAL_SCHEDULE_TYPE' AND [Name] = 'OTRO')
    INSERT INTO [HR].[ref_Types] ([Category], [Name], [Description], [IsActive]) VALUES ('EMPLOYEE_SPECIAL_SCHEDULE_TYPE', 'OTRO', 'Otro caso especial no clasificado', 1);
GO

-- 2) Tabla de horarios especiales ---------------------------------------------
IF OBJECT_ID('[HR].[tbl_EmployeeSpecialSchedules]') IS NULL
BEGIN
    CREATE TABLE [HR].[tbl_EmployeeSpecialSchedules]
    (
        [EmployeeSpecialScheduleId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EmployeeID]        INT NOT NULL,
        [EntryTime]         TIME NOT NULL,
        [ExitTime]          TIME NOT NULL,
        [HasLunchBreak]     BIT NOT NULL DEFAULT(0),
        [LunchStart]        TIME NULL,
        [LunchEnd]          TIME NULL,
        [CaseTypeId]        INT NOT NULL,
        [Reason]            NVARCHAR(500) NULL,
        [DocumentReference] NVARCHAR(200) NULL,
        [RequiresApproval]  BIT NOT NULL DEFAULT(0),
        [IsActive]          BIT NOT NULL DEFAULT(1),
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT(GETDATE()),
        [CreatedBy]         INT NULL,
        [UpdatedAt]         DATETIME2 NULL,
        [UpdatedBy]         INT NULL,
        [RowVersion]        ROWVERSION,
        CONSTRAINT [FK_EmployeeSpecialSchedules_CaseType] FOREIGN KEY ([CaseTypeId]) REFERENCES [HR].[ref_Types]([TypeId]),
        CONSTRAINT [FK_EmployeeSpecialSchedules_Employee] FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees]([EmployeeID])
    );
END
GO

-- 2.1) 2026-09-10: tbl_EmployeeSpecialSchedules ya existia sin EmployeeID
-- directo (la relacion era solo indirecta via tbl_EmployeeSchedules.
-- EmployeeSpecialScheduleId), lo que impedia saber "de quien es este horario
-- especial" sin JOIN. Se agrega EmployeeID directo (nullable primero para
-- poder rellenar los 12 registros ya existentes desde su
-- tbl_EmployeeSchedules asociado, luego se endurece a NOT NULL).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EmployeeSpecialSchedules]') AND name = 'EmployeeID')
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSpecialSchedules] ADD [EmployeeID] INT NULL;
END
GO

UPDATE ss
SET ss.EmployeeID = es.EmployeeID
FROM [HR].[tbl_EmployeeSpecialSchedules] ss
INNER JOIN [HR].[tbl_EmployeeSchedules] es ON es.EmployeeSpecialScheduleId = ss.EmployeeSpecialScheduleId
WHERE ss.EmployeeID IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
           WHERE t.name = 'tbl_EmployeeSpecialSchedules' AND c.name = 'EmployeeID' AND c.is_nullable = 1)
   AND NOT EXISTS (SELECT 1 FROM [HR].[tbl_EmployeeSpecialSchedules] WHERE EmployeeID IS NULL)
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSpecialSchedules] ALTER COLUMN [EmployeeID] INT NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EmployeeSpecialSchedules_Employee')
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSpecialSchedules] ADD CONSTRAINT [FK_EmployeeSpecialSchedules_Employee]
        FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees]([EmployeeID]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeSpecialSchedules_EmployeeID' AND object_id = OBJECT_ID('[HR].[tbl_EmployeeSpecialSchedules]'))
BEGIN
    CREATE INDEX [IX_EmployeeSpecialSchedules_EmployeeID] ON [HR].[tbl_EmployeeSpecialSchedules]([EmployeeID]);
END
GO

-- 3) tbl_EmployeeSchedules: ScheduleId nullable + FK a horario especial + CHECK XOR
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
           WHERE t.name = 'tbl_EmployeeSchedules' AND c.name = 'ScheduleID' AND c.is_nullable = 0)
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSchedules] ALTER COLUMN [ScheduleID] INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EmployeeSchedules]') AND name = 'EmployeeSpecialScheduleId')
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSchedules] ADD [EmployeeSpecialScheduleId] INT NULL;
    ALTER TABLE [HR].[tbl_EmployeeSchedules] ADD CONSTRAINT [FK_EmployeeSchedules_SpecialSchedule]
        FOREIGN KEY ([EmployeeSpecialScheduleId]) REFERENCES [HR].[tbl_EmployeeSpecialSchedules]([EmployeeSpecialScheduleId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EmployeeSchedules_ScheduleOrSpecial')
BEGIN
    ALTER TABLE [HR].[tbl_EmployeeSchedules] WITH CHECK ADD CONSTRAINT [CK_EmployeeSchedules_ScheduleOrSpecial]
        CHECK ( ([ScheduleID] IS NOT NULL AND [EmployeeSpecialScheduleId] IS NULL)
             OR ([ScheduleID] IS NULL AND [EmployeeSpecialScheduleId] IS NOT NULL) );
END
GO

-- 4) tbl_AttendanceCalculations: JourneyNumber + EmployeeSpecialScheduleId,
--    cambio de clave unica ------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]') AND name = 'JourneyNumber')
BEGIN
    ALTER TABLE [HR].[tbl_AttendanceCalculations] ADD [JourneyNumber] INT NOT NULL CONSTRAINT [DF_AttendanceCalculations_JourneyNumber] DEFAULT(1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]') AND name = 'EmployeeSpecialScheduleId')
BEGIN
    ALTER TABLE [HR].[tbl_AttendanceCalculations] ADD [EmployeeSpecialScheduleId] INT NULL;
    ALTER TABLE [HR].[tbl_AttendanceCalculations] ADD CONSTRAINT [FK_AttendanceCalculations_SpecialSchedule]
        FOREIGN KEY ([EmployeeSpecialScheduleId]) REFERENCES [HR].[tbl_EmployeeSpecialSchedules]([EmployeeSpecialScheduleId]);
END
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AttendanceCalculations_Employee_WorkDate' AND object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]'))
BEGIN
    DROP INDEX [UX_AttendanceCalculations_Employee_WorkDate] ON [HR].[tbl_AttendanceCalculations];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AttendanceCalculations_Employee_WorkDate_Journey' AND object_id = OBJECT_ID('[HR].[tbl_AttendanceCalculations]'))
BEGIN
    CREATE UNIQUE INDEX [UX_AttendanceCalculations_Employee_WorkDate_Journey]
        ON [HR].[tbl_AttendanceCalculations]([EmployeeID], [WorkDate], [JourneyNumber]);
END
GO

-- 5) Horarios de catálogo nuevos (patrones de conteo>1 sin match exacto) -----
IF NOT EXISTS (SELECT 1 FROM [HR].[tbl_Schedules] WHERE [Description] = 'Código Trabajo 52')
    INSERT INTO [HR].[tbl_Schedules] ([Description], [EntryTime], [ExitTime], [WorkingDays], [RequiredHoursPerDay], [HasLunchBreak], [LunchStart], [LunchEnd], [IsRotating], [IsActive], [CrossesMidnight], [LaborRegimeId], [CreatedAt])
    VALUES ('Código Trabajo 52', '06:00', '14:30', 'Lunes a Viernes', 8.50, 0, NULL, NULL, 0, 1, 0, 59, GETDATE());
GO
IF NOT EXISTS (SELECT 1 FROM [HR].[tbl_Schedules] WHERE [Description] = 'Código Trabajo 53')
    INSERT INTO [HR].[tbl_Schedules] ([Description], [EntryTime], [ExitTime], [WorkingDays], [RequiredHoursPerDay], [HasLunchBreak], [LunchStart], [LunchEnd], [IsRotating], [IsActive], [CrossesMidnight], [LaborRegimeId], [CreatedAt])
    VALUES ('Código Trabajo 53', '14:00', '22:30', 'Lunes a Viernes', 8.50, 0, NULL, NULL, 0, 1, 0, 59, GETDATE());
GO
