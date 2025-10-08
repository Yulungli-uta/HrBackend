-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE PROCEDURE HR.sp_RequestVacation
    @EmployeeID INT,
    @StartDate  DATE,
    @EndDate    DATE,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @days INT = DATEDIFF(DAY,@StartDate,@EndDate) + 1;

    DECLARE @avail INT = ISNULL((SELECT TOP 1 DaysGranted - DaysTaken FROM HR.tbl_Vacations WHERE EmployeeID=@EmployeeID ORDER BY VacationID DESC),0);
    IF @avail < @days
        RAISERROR('Días de vacaciones insuficientes.',16,1);

    INSERT INTO HR.tbl_Vacations(EmployeeID,StartDate,EndDate,DaysGranted,Status)
    VALUES(@EmployeeID,@StartDate,@EndDate,@days,'Planned');

    DECLARE @vacationID INT = SCOPE_IDENTITY();

    INSERT INTO HR.tbl_Permissions(EmployeeID,PermissionTypeID,StartDate,EndDate,ApprovedBy,Status,VacationID)
    VALUES(@EmployeeID,(SELECT TOP 1 TypeID FROM HR.tbl_PermissionTypes WHERE Name='Vacaciones'),
           @StartDate,@EndDate,@ApproverID,'Pending',@vacationID);

    SELECT @vacationID AS VacationID;
END
GO

-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE PROCEDURE HR.sp_PlanOvertime
    @EmployeeID INT,
    @Date DATE,
    @Hours DECIMAL(5,2),
    @OvertimeType VARCHAR(50),
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Factor DECIMAL(5,2) = ISNULL((SELECT Factor FROM HR.tbl_OvertimeConfig WHERE OvertimeType=@OvertimeType),1.25);
    INSERT INTO HR.tbl_Overtime(EmployeeID,WorkDate,OvertimeType,Hours,Status,ApprovedBy,Factor)
    VALUES(@EmployeeID,@Date,@OvertimeType,@Hours,'Planned',@ApproverID,@Factor);
    SELECT SCOPE_IDENTITY() AS OvertimeID;
END
GO */

-------------------------------------------------------------------------------
-- 5) INSERTS DE PRUEBA
-------------------------------------------------------------------------------

/* =========================================================
   INSERCIÓN DE DATOS INICIALES EN TABLA DE TIPOS
   ========================================================= */
INSERT INTO HR.ref_Types (Category, Name) 
VALUES 
    ('MARITAL_STATUS', 'Soltero/a'),
    ('MARITAL_STATUS', 'Casado/a'),
    ('MARITAL_STATUS', 'Divorciado/a'),
    ('MARITAL_STATUS', 'Viudo/a'),
    ('MARITAL_STATUS', 'Unión libre'),
    ('ETHNICITY', 'Mestizo'),
    ('ETHNICITY', 'Afroecuatoriano'),
    ('ETHNICITY', 'Indígena'),
    ('ETHNICITY', 'Montubio'),
    ('ETHNICITY', 'Blanco'),
    ('ETHNICITY', 'Otro'),
    ('BLOOD_TYPE', 'A+'),
    ('BLOOD_TYPE', 'A-'),
    ('BLOOD_TYPE', 'B+'),
    ('BLOOD_TYPE', 'B-'),
    ('BLOOD_TYPE', 'AB+'),
    ('BLOOD_TYPE', 'AB-'),
    ('BLOOD_TYPE', 'O+'),
    ('BLOOD_TYPE', 'O-'),
    ('SPECIAL_NEEDS', 'Trastorno emocional'),
    ('SPECIAL_NEEDS', 'Trastorno de personalidad'),
    ('SPECIAL_NEEDS', 'Problemas de aprendizaje'),
    ('SPECIAL_NEEDS', 'Otras necesidades especiales'),
    ('DISABILITY_TYPE', 'Física'),
    ('DISABILITY_TYPE', 'Mental'),
    ('DISABILITY_TYPE', 'Auditiva'),
    ('DISABILITY_TYPE', 'Visual'),
    ('DISABILITY_TYPE', 'Otra'),
    ('RELATIONSHIP', 'Cónyuge'),
    ('RELATIONSHIP', 'Hijo/a'),
    ('RELATIONSHIP', 'Padre'),
    ('RELATIONSHIP', 'Madre'),
    ('RELATIONSHIP', 'Hermano/a'),
    ('RELATIONSHIP', 'Abuelo/a'),
    ('RELATIONSHIP', 'Tío/a'),
    ('RELATIONSHIP', 'Primo/a'),
    ('RELATIONSHIP', 'Otro'),
    ('INSTITUTION_TYPE', 'Educativa'),
    ('INSTITUTION_TYPE', 'Gubernamental'),
    ('INSTITUTION_TYPE', 'Privada'),
    ('INSTITUTION_TYPE', 'ONG'),
    ('INSTITUTION_TYPE', 'Religiosa'),
    ('INSTITUTION_TYPE', 'Otro'),
    ('EVENT_TYPE', 'Charla'),
    ('EVENT_TYPE', 'Conferencia'),
    ('EVENT_TYPE', 'Taller'),
    ('EVENT_TYPE', 'Seminario'),
    ('EVENT_TYPE', 'Diplomado'),
    ('EVENT_TYPE', 'Curso'),
    ('EVENT_TYPE', 'Otro'),
    ('KNOWLEDGE_AREA', 'Tecnológico'),
    ('KNOWLEDGE_AREA', 'Educativo'),
    ('KNOWLEDGE_AREA', 'Salud'),
    ('KNOWLEDGE_AREA', 'Administrativo'),
    ('KNOWLEDGE_AREA', 'Humanístico'),
    ('KNOWLEDGE_AREA', 'Otro'),
    ('ADDRESS_TYPE', 'Domicilio'),
    ('ADDRESS_TYPE', 'Trabajo'),
    ('ADDRESS_TYPE', 'Temporal'),
    ('ACCOUNT_TYPE', 'Ahorros'),
    ('ACCOUNT_TYPE', 'Corriente'),
    ('EDUCATION_LEVEL', 'Primaria'),
    ('EDUCATION_LEVEL', 'Secundaria'),
    ('EDUCATION_LEVEL', 'Técnico'),
    ('EDUCATION_LEVEL', 'Tecnológico'),
    ('EDUCATION_LEVEL', 'Tercer Nivel'),
    ('EDUCATION_LEVEL', 'Cuarto Nivel'),
    ('PUBLICATION_TYPE', 'Artículo científico'),
    ('PUBLICATION_TYPE', 'Artículo de revisión'),
    ('PUBLICATION_TYPE', 'Ensayo'),
    ('JOURNAL_TYPE', 'Internacional'),
    ('JOURNAL_TYPE', 'Nacional'),
    ('JOURNAL_TYPE', 'Regional'),
    ('CERTIFICATE_TYPE', 'Físico'),
    ('CERTIFICATE_TYPE', 'Digital'),
    ('APPROVAL_TYPE', 'Aprobado'),
    ('APPROVAL_TYPE', 'Reprobado'),
    ('EXPERIENCE_TYPE', 'Público'),
    ('EXPERIENCE_TYPE', 'Privado'),
    ('EXPERIENCE_TYPE', 'ONG'),
    ('ILLNESS_TYPE', 'Cáncer'),
    ('ILLNESS_TYPE', 'Diabetes'),
    ('ILLNESS_TYPE', 'Hipertensión'),
    ('ILLNESS_TYPE', 'Cardiopatía'),
    ('ILLNESS_TYPE', 'Otra'),
    ('IDENTIFICATION_TYPE', 'Cédula'),
    ('IDENTIFICATION_TYPE', 'Pasaporte'),
    ('IDENTIFICATION_TYPE', 'RUC');
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateDailyAttendance
    @EmployeeID INT,
    @WorkDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        ;WITH Punches AS (
            SELECT
                PunchTime,
                PunchType,
                ROW_NUMBER() OVER (ORDER BY PunchTime) AS RN
            FROM HR.tbl_AttendancePunches
            WHERE EmployeeID = @EmployeeID
              AND CAST(PunchTime AS date) = @WorkDate
        ),
        Paired AS (
            SELECT
                i.PunchTime AS InTime,
                o.PunchTime AS OutTime
            FROM Punches i
            LEFT JOIN Punches o
              ON o.RN = i.RN + 1
            WHERE i.PunchType = 'In' AND o.PunchType = 'Out'
        )
        SELECT
            @EmployeeID AS EmployeeID,
            @WorkDate   AS WorkDate,
            MIN(InTime)  AS FirstPunchIn,
            MAX(OutTime) AS LastPunchOut,
            SUM(CASE WHEN OutTime IS NOT NULL THEN DATEDIFF(MINUTE, InTime, OutTime) ELSE 0 END) AS TotalMinutes
        INTO #Agg
        FROM Paired;

        DECLARE @first DATETIME2, @last DATETIME2, @total INT;
        SELECT @first = FirstPunchIn, @last = LastPunchOut, @total = ISNULL(TotalMinutes,0) FROM #Agg;

        DECLARE @regular INT = IIF(@total > 480, 480, @total);
        DECLARE @overtime INT = IIF(@total > 480, @total - 480, 0);

        MERGE HR.tbl_AttendanceCalculations AS T
        USING (SELECT @EmployeeID AS EmployeeID, @WorkDate AS WorkDate) AS S
        ON (T.EmployeeID = S.EmployeeID AND T.WorkDate = S.WorkDate)
        WHEN MATCHED THEN UPDATE SET
            FirstPunchIn = @first,
            LastPunchOut = @last,
            TotalWorkedMinutes = @total,
            RegularMinutes = @regular,
            OvertimeMinutes = @overtime,
            Status = 'Approved'
        WHEN NOT MATCHED THEN INSERT (EmployeeID, WorkDate, FirstPunchIn, LastPunchOut, TotalWorkedMinutes, RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, Status)
        VALUES (@EmployeeID, @WorkDate, @first, @last, @total, @regular, @overtime, 0, 0, 'Approved');

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Asistencia diaria calculada' AS Msg, @total AS TotalMinutes, @overtime AS OvertimeMinutes;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateMonthlyPayroll
    @EmployeeID INT,
    @Period CHAR(7) -- 'YYYY-MM'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @first DATE = CONVERT(date, @Period + '-01');
        DECLARE @last  DATE = EOMONTH(@first);

        DECLARE @salary DECIMAL(12,2) =
        (
            SELECT TOP 1 BaseSalary
            FROM HR.tbl_Contracts
            WHERE EmployeeID = @EmployeeID
              AND @first BETWEEN StartDate AND ISNULL(EndDate,'9999-12-31')
            ORDER BY StartDate DESC
        );

        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period)
        BEGIN
            INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary, Status)
            VALUES(@EmployeeID, @Period, ISNULL(@salary,0), 'Pending');
        END
        ELSE
        BEGIN
            UPDATE HR.tbl_Payroll SET BaseSalary = ISNULL(@salary, BaseSalary) WHERE EmployeeID=@EmployeeID AND Period=@Period;
        END

        DECLARE @payrollID INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);

        ;WITH OT AS (
            SELECT WorkDate, OvertimeType, SUM(ActualHours) AS Hours, MAX(Factor) AS Factor
            FROM HR.tbl_Overtime
            WHERE EmployeeID=@EmployeeID AND Status='Verified' AND WorkDate BETWEEN @first AND @last
            GROUP BY WorkDate, OvertimeType
        )
        INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
        SELECT @payrollID, 'Overtime',
               CONCAT('Horas extra ', OvertimeType, ' ', CONVERT(char(10), WorkDate, 23)) AS Concept,
               Hours, 
               ROUND(ISNULL(@salary,0) / 240.0 * Factor, 2), -- valor hora * factor
               NULL
        FROM OT o
        WHERE NOT EXISTS (
            SELECT 1 FROM HR.tbl_PayrollLines pl
            WHERE pl.PayrollID=@payrollID AND pl.LineType='Overtime' AND pl.Concept=CONCAT('Horas extra ', o.OvertimeType, ' ', CONVERT(char(10), o.WorkDate, 23))
        );

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Nómina calculada/actualizada' AS Msg, @payrollID AS PayrollID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_GenerateMonthlyAttendanceReport
    @Period CHAR(7),             -- 'YYYY-MM'
    @DepartmentID INT = NULL     -- NULL = todos
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @first DATE = CONVERT(date, @Period + '-01');
    DECLARE @last  DATE = EOMONTH(@first);

    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS Department,
        ac.WorkDate,
        ac.TotalWorkedMinutes,
        ac.RegularMinutes,
        ac.OvertimeMinutes,
        ac.NightMinutes,
        ac.HolidayMinutes,
        ac.Status
    FROM HR.tbl_AttendanceCalculations ac
    JOIN HR.tbl_Employees e ON e.EmployeeID = ac.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE ac.WorkDate BETWEEN @first AND @last
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    ORDER BY Department, EmployeeName, ac.WorkDate;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_RegisterPersonnelMovement
    @EmployeeID INT,
    @ContractID INT,
    @OriginDepartmentID INT = NULL,
    @DestinationDepartmentID INT,
    @MovementDate DATE,
    @MovementType VARCHAR(30),
    @Reason VARCHAR(500) = NULL,
    @DocumentLocation VARCHAR(255) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO HR.tbl_PersonnelMovements(EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID, MovementDate, MovementType, DocumentLocation, Reason, CreatedBy)
        VALUES(@EmployeeID, @ContractID, @OriginDepartmentID, @DestinationDepartmentID, @MovementDate, @MovementType, @DocumentLocation, @Reason, @CreatedBy);

        -- actualizar depto actual
        UPDATE HR.tbl_Employees SET DepartmentID = @DestinationDepartmentID, UpdatedBy = @CreatedBy, UpdatedAt = SYSDATETIME()
        WHERE EmployeeID = @EmployeeID;

        COMMIT TRANSACTION;
        SELECT SCOPE_IDENTITY() AS MovementID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_AssignEmployeeSubsidy
    @EmployeeID INT,
    @Period CHAR(7),
    @Concept VARCHAR(120),
    @Amount DECIMAL(12,2)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period)
        BEGIN
            DECLARE @salary DECIMAL(12,2) = ISNULL((SELECT TOP 1 BaseSalary FROM HR.tbl_Contracts WHERE EmployeeID=@EmployeeID ORDER BY StartDate DESC),0);
            INSERT INTO HR.tbl_Payroll(EmployeeID, Period, BaseSalary) VALUES(@EmployeeID, @Period, @salary);
        END

        DECLARE @payrollID INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);
        IF NOT EXISTS (SELECT 1 FROM HR.tbl_PayrollLines WHERE PayrollID=@payrollID AND LineType='Subsidy' AND Concept=@Concept)
        BEGIN
            INSERT INTO HR.tbl_PayrollLines(PayrollID, LineType, Concept, Quantity, UnitValue, Notes)
            VALUES(@payrollID, 'Subsidy', @Concept, 1, @Amount, NULL);
        END
        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Subsidio asignado' AS Msg;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_ApproveNightOvertime
    @OvertimeID INT,
    @FirstApprover INT,
    @SecondApprover INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Overtime
    SET ApprovedBy = @FirstApprover,
        SecondApprover = @SecondApprover,
        Status = 'Verified'
    WHERE OvertimeID = @OvertimeID AND OvertimeType = 'Nocturna';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_RequestVacation
    @EmployeeID INT,
    @StartDate  DATE,
    @EndDate    DATE,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @days INT = DATEDIFF(DAY,@StartDate,@EndDate) + 1;
    DECLARE @avail INT = ISNULL((SELECT TOP 1 DaysGranted - DaysTaken FROM HR.tbl_Vacations WHERE EmployeeID=@EmployeeID ORDER BY VacationID DESC),0);
    IF @avail < @days
        RAISERROR('Días de vacaciones insuficientes.',16,1);

    INSERT INTO HR.tbl_Vacations(EmployeeID,StartDate,EndDate,DaysGranted,Status)
    VALUES(@EmployeeID,@StartDate,@EndDate,@days,'Planned');

    DECLARE @vacationID INT = SCOPE_IDENTITY();
    INSERT INTO HR.tbl_Permissions(EmployeeID,PermissionTypeID,StartDate,EndDate,ApprovedBy,Status,VacationID)
    VALUES(@EmployeeID,(SELECT TOP 1 TypeID FROM HR.tbl_PermissionTypes WHERE Name='Vacaciones'),
           @StartDate,@EndDate,@ApproverID,'Pending',@vacationID);

    SELECT @vacationID AS VacationID;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_ApprovePermission
    @PermissionID INT,
    @ApproverID INT,
    @Approve BIT,
    @Reason NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Permissions
    SET Status = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
        ApprovedBy = @ApproverID,
        Justification = COALESCE(Justification,'') + ISNULL(' | ' + @Reason,'')
    WHERE PermissionID = @PermissionID;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_CalculateSubsidies
    @Period CHAR(7),
    @DepartmentID INT = NULL,
    @Concept VARCHAR(120) = 'Transporte',
    @Amount DECIMAL(12,2) = 30.00
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @first DATE = CONVERT(date, @Period + '-01');
        ;WITH Emp AS (
            SELECT e.EmployeeID
            FROM HR.tbl_Employees e
            WHERE e.IsActive = 1 AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
        )
        SELECT * INTO #Emp FROM Emp;

        DECLARE cur CURSOR LOCAL FOR SELECT EmployeeID FROM #Emp;
        DECLARE @eid INT;
        OPEN cur; FETCH NEXT FROM cur INTO @eid;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC HR.sp_AssignEmployeeSubsidy @EmployeeID=@eid, @Period=@Period, @Concept=@Concept, @Amount=@Amount;
            FETCH NEXT FROM cur INTO @eid;
        END
        CLOSE cur; DEALLOCATE cur;

        COMMIT TRANSACTION;
        SELECT 1 AS Ok, 'Subsidios calculados/asignados' AS Msg;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_PlanOvertime
    @EmployeeID INT,
    @WorkDate DATE,
    @Hours DECIMAL(5,2),
    @OvertimeType VARCHAR(50),
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Factor DECIMAL(5,2) = ISNULL((SELECT Factor FROM HR.tbl_OvertimeConfig WHERE OvertimeType=@OvertimeType),1.25);
    INSERT INTO HR.tbl_Overtime(EmployeeID, WorkDate, OvertimeType, Hours, Status, ApprovedBy, Factor)
    VALUES(@EmployeeID, @WorkDate, @OvertimeType, @Hours, 'Planned', @ApproverID, @Factor);
    SELECT SCOPE_IDENTITY() AS OvertimeID;
END
GO

-- Source: 02-HR_stored_procedures_revised.sql
CREATE PROCEDURE HR.sp_GenerateSubsidyComplianceReport
    @Period CHAR(7),
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        d.Name AS Department,
        SUM(CASE WHEN pl.LineType='Subsidy' THEN pl.Quantity * pl.UnitValue ELSE 0 END) AS TotalSubsidies
    FROM HR.tbl_Payroll py
    JOIN HR.tbl_PayrollLines pl ON pl.PayrollID = py.PayrollID
    JOIN HR.tbl_Employees e ON e.EmployeeID = py.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE py.Period = @Period
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    GROUP BY e.EmployeeID, p.FirstName, p.LastName, d.Name
    ORDER BY Department, EmployeeName;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_UpsertEmployeeSchedule
    @EmployeeID INT,
    @ScheduleID INT,
    @ValidFrom  DATE,
    @ValidTo    DATE = NULL           -- NULL = vigente
AS
BEGIN
    SET NOCOUNT ON;
    IF @ValidTo IS NOT NULL AND @ValidTo < @ValidFrom
        RAISERROR('ValidTo no puede ser menor a ValidFrom',16,1);

    -- chequear solapamiento
    IF EXISTS(
        SELECT 1 FROM HR.tbl_EmployeeSchedules es
        WHERE es.EmployeeID = @EmployeeID
          AND (@ValidTo IS NULL OR es.ValidFrom <= @ValidTo)
          AND (es.ValidTo IS NULL OR es.ValidTo >= @ValidFrom)
    )
    BEGIN
        RAISERROR('Existe una asignación de horario que se solapa en el rango indicado.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_EmployeeSchedules(EmployeeID, ScheduleID, ValidFrom, ValidTo)
    VALUES(@EmployeeID, @ScheduleID, @ValidFrom, @ValidTo);
    SELECT SCOPE_IDENTITY() AS EmpScheduleID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetActiveSchedule
    @EmployeeID INT,
    @ForDate    DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 s.*
    FROM HR.tbl_EmployeeSchedules es
    JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
    WHERE es.EmployeeID = @EmployeeID
      AND @ForDate BETWEEN es.ValidFrom AND ISNULL(es.ValidTo,'9999-12-31')
    ORDER BY es.ValidFrom DESC;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_PlanTimeRecovery
    @EmployeeID INT,
    @OwedMinutes INT,
    @PlanDate DATE,
    @FromTime TIME,
    @ToTime TIME,
    @Reason VARCHAR(300) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF DATEDIFF(MINUTE, @FromTime, @ToTime) < @OwedMinutes OR DATEDIFF(MINUTE, @FromTime, @ToTime) < 60
        RAISERROR('El bloque planificado debe cubrir al menos los minutos adeudados y ser >= 60 minutos.',16,1);

    -- Evitar plan en vacaciones
    IF EXISTS (
        SELECT 1 FROM HR.tbl_Vacations v
        WHERE v.EmployeeID = @EmployeeID
          AND v.Status IN ('Planned','InProgress')
          AND @PlanDate BETWEEN v.StartDate AND v.EndDate
    )
    BEGIN
        RAISERROR('No se puede planificar recuperación durante vacaciones.',16,1);
        RETURN;
    END

    INSERT INTO HR.tbl_TimeRecoveryPlans(EmployeeID,OwedMinutes,PlanDate,FromTime,ToTime,Reason,CreatedBy)
    VALUES(@EmployeeID,@OwedMinutes,@PlanDate,@FromTime,@ToTime,@Reason,@CreatedBy);

    SELECT SCOPE_IDENTITY() AS RecoveryPlanID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ApproveTimeRecovery
    @RecoveryPlanID INT,
    @ApproverID INT
AS
BEGIN
    SET NOCOUNT ON;
    -- (opcional: agregar columna de estado; por ahora, registramos en logs con 0 minutos como "aprobación")
    INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
    VALUES(@RecoveryPlanID, CAST(SYSDATETIME() AS DATE), 0, @ApproverID, SYSDATETIME());
    SELECT 1 AS Ok, 'Plan de recuperación aprobado (log registrado)' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_LogTimeRecovery
    @RecoveryPlanID INT,
    @ExecutedDate DATE,
    @MinutesRecovered INT,
    @ApproverID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO HR.tbl_TimeRecoveryLogs(RecoveryPlanID, ExecutedDate, MinutesRecovered, ApprovedBy, ApprovedAt)
    VALUES(@RecoveryPlanID, @ExecutedDate, @MinutesRecovered, @ApproverID, CASE WHEN @ApproverID IS NULL THEN NULL ELSE SYSDATETIME() END);
    SELECT SCOPE_IDENTITY() AS RecoveryLogID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetTimeRecoveryBalance
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        @EmployeeID AS EmployeeID,
        ISNULL(SUM(p.OwedMinutes),0) AS MinutesOwed,
        ISNULL((SELECT SUM(l.MinutesRecovered) FROM HR.tbl_TimeRecoveryLogs l JOIN HR.tbl_TimeRecoveryPlans p2 ON p2.RecoveryPlanID=l.RecoveryPlanID WHERE p2.EmployeeID=@EmployeeID),0) AS MinutesRecovered,
        ISNULL(SUM(p.OwedMinutes),0) - ISNULL((SELECT SUM(l.MinutesRecovered) FROM HR.tbl_TimeRecoveryLogs l JOIN HR.tbl_TimeRecoveryPlans p2 ON p2.RecoveryPlanID=l.RecoveryPlanID WHERE p2.EmployeeID=@EmployeeID),0) AS BalanceMinutes
    FROM HR.tbl_TimeRecoveryPlans p
    WHERE p.EmployeeID = @EmployeeID;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_JustifyPunch
    @PunchID INT,
    @BossEmployeeID INT,
    @Reason VARCHAR(500),
    @Approve BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM HR.tbl_PunchJustifications WHERE PunchID=@PunchID)
        UPDATE HR.tbl_PunchJustifications
           SET Reason=@Reason, BossEmployeeID=@BossEmployeeID, Approved=@Approve, ApprovedAt=CASE WHEN @Approve=1 THEN SYSDATETIME() ELSE NULL END
         WHERE PunchID=@PunchID;
    ELSE
        INSERT INTO HR.tbl_PunchJustifications(PunchID,BossEmployeeID,Reason,Approved,ApprovedAt)
        VALUES(@PunchID,@BossEmployeeID,@Reason,@Approve,CASE WHEN @Approve=1 THEN SYSDATETIME() ELSE NULL END);

    SELECT 1 AS Ok, 'Justificación registrada' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_RecalculateAttendanceRange
    @EmployeeID INT,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @ToDate < @FromDate RAISERROR('Rango de fechas inválido.',16,1);

    DECLARE @d DATE = @FromDate;
    WHILE @d <= @ToDate
    BEGIN
        EXEC HR.sp_CalculateDailyAttendance @EmployeeID=@EmployeeID, @WorkDate=@d;
        SET @d = DATEADD(DAY, 1, @d);
    END

    SELECT 1 AS Ok, 'Asistencia recalculada para el rango' AS Msg;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_BulkImportPunches
    @Rows HR.tt_PunchImport READONLY
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO HR.tbl_AttendancePunches(EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude)
    SELECT EmployeeID, PunchTime, PunchType, DeviceID, Longitude, Latitude
    FROM @Rows;  -- el trigger HR.trg_Punch_Validations aplicará reglas
    SELECT @@ROWCOUNT AS Imported;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ValidateSubrogationWindow
    @SubrogatedEmployeeID INT,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.SubrogationID, s.StartDate, s.EndDate
    FROM HR.tbl_Subrogations s
    WHERE s.SubrogatedEmployeeID = @SubrogatedEmployeeID
      AND (@StartDate <= s.EndDate AND @EndDate >= s.StartDate);
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_EmployeesOnLeave
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName, v.StartDate, v.EndDate, v.Status
    FROM HR.tbl_Vacations v
    JOIN HR.tbl_Employees e ON e.EmployeeID = v.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    WHERE v.StartDate <= @ToDate AND v.EndDate >= @FromDate;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_EmployeesAboutToVacation
    @WithinDays INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @today DATE = CAST(SYSDATETIME() AS DATE);
    DECLARE @limit DATE = DATEADD(DAY, @WithinDays, @today);

    SELECT e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName, v.StartDate, v.EndDate, v.Status
    FROM HR.tbl_Vacations v
    JOIN HR.tbl_Employees e ON e.EmployeeID = v.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    WHERE v.StartDate BETWEEN @today AND @limit
      AND v.Status IN ('Planned','InProgress');
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ListPendingPermissions
    @DepartmentID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pr.PermissionID, e.EmployeeID, p.FirstName + ' ' + p.LastName AS EmployeeName,
           d.Name AS Department, t.Name AS PermissionType, pr.StartDate, pr.EndDate, pr.RequestDate
    FROM HR.tbl_Permissions pr
    JOIN HR.tbl_PermissionTypes t ON t.TypeID = pr.PermissionTypeID
    JOIN HR.tbl_Employees e ON e.EmployeeID = pr.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.EmployeeID
    LEFT JOIN HR.tbl_Departments d ON d.DepartmentID = e.DepartmentID
    WHERE pr.Status = 'Pending'
      AND (@DepartmentID IS NULL OR e.DepartmentID = @DepartmentID)
    ORDER BY pr.RequestDate DESC;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_GetPayrollBreakdown
    @EmployeeID INT,
    @Period CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @pid INT = (SELECT PayrollID FROM HR.tbl_Payroll WHERE EmployeeID=@EmployeeID AND Period=@Period);
    IF @pid IS NULL
    BEGIN
        SELECT 0 AS Exists, NULL AS PayrollID, 0.00 AS TotalEarnings, 0.00 AS TotalOvertime, 0.00 AS TotalSubsidies, 0.00 AS TotalDeductions, 0.00 AS NetPay;
        RETURN;
    END

    SELECT
        1 AS Exists,
        @pid AS PayrollID,
        SUM(CASE WHEN LineType='Earning'  THEN Quantity*UnitValue ELSE 0 END) AS TotalEarnings,
        SUM(CASE WHEN LineType='Overtime' THEN Quantity*UnitValue ELSE 0 END) AS TotalOvertime,
        SUM(CASE WHEN LineType='Subsidy'  THEN Quantity*UnitValue ELSE 0 END) AS TotalSubsidies,
        SUM(CASE WHEN LineType='Deduction'THEN Quantity*UnitValue ELSE 0 END) AS TotalDeductions,
        SUM(CASE WHEN LineType IN ('Earning','Overtime','Subsidy') THEN Quantity*UnitValue ELSE -Quantity*UnitValue END) AS NetPay
    FROM HR.tbl_PayrollLines
    WHERE PayrollID=@pid;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ClosePayrollPeriod
    @Period CHAR(7),
    @PaymentDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Payroll
      SET Status='Reconciled',
          PaymentDate = COALESCE(@PaymentDate, PaymentDate, CAST(SYSDATETIME() AS DATE))
    WHERE Period=@Period;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Source: 03-HR_additional_procedures.sql
CREATE PROCEDURE HR.sp_ReopenPayrollPeriod
    @Period CHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HR.tbl_Payroll SET Status='Pending' WHERE Period=@Period AND Status='Reconciled';
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO