-- ============================================================
-- FUNCIONES : esquema [HR]
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- [fn_CalculateNightMinutes]
CREATE OR ALTER FUNCTION HR.fn_CalculateNightMinutes(
    @StartTime DATETIME,
    @EndTime DATETIME
)
RETURNS INT
AS
BEGIN
    DECLARE @NightMinutes INT = 0;
    DECLARE @CurrentTime DATETIME = @StartTime;
    
    WHILE @CurrentTime < @EndTime
    BEGIN
        DECLARE @NextMinute DATETIME = DATEADD(MINUTE, 1, @CurrentTime);
        DECLARE @Hour INT = DATEPART(HOUR, @CurrentTime);
        
        -- Horario nocturno: 22:00 a 06:00
        IF @Hour >= 22 OR @Hour < 6
            SET @NightMinutes = @NightMinutes + 1;
            
        SET @CurrentTime = @NextMinute;
    END
    
    RETURN @NightMinutes;
END

GO

-- [fn_GetBusinessDays]
CREATE OR ALTER FUNCTION HR.fn_GetBusinessDays(
    @StartDate DATE,
    @EndDate DATE
)
RETURNS INT
AS
BEGIN
    DECLARE @TotalDays INT = DATEDIFF(DAY, @StartDate, @EndDate) + 1;
    DECLARE @WeekendDays INT = 0;
    DECLARE @CurrentDate DATE = @StartDate;
    
    WHILE @CurrentDate <= @EndDate
    BEGIN
        IF DATEPART(WEEKDAY, @CurrentDate) IN (1, 7) -- 1=Domingo, 7=Sábado
            SET @WeekendDays = @WeekendDays + 1;
        SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
    END
    
    RETURN @TotalDays - @WeekendDays;
END

GO

-- [fn_hr_CountWorkingDays]

/* ============================================================
   1) FUNCIÓN: Días laborables (Lunes a Viernes)
   ============================================================ */
CREATE OR ALTER FUNCTION HR.fn_hr_CountWorkingDays
(
    @StartDate DATE,
    @EndDate   DATE
)
RETURNS INT
AS
BEGIN
    DECLARE @Days INT = 0;
    DECLARE @d DATE = @StartDate;

    WHILE @d <= @EndDate
    BEGIN
        DECLARE @isoWeekday INT = ((DATEPART(WEEKDAY, @d) + @@DATEFIRST - 2) % 7) + 1;
        IF @isoWeekday BETWEEN 1 AND 5 SET @Days += 1;
        SET @d = DATEADD(DAY, 1, @d);
    END

    RETURN @Days;
END

GO

-- [fn_IsHoliday]
CREATE OR ALTER FUNCTION HR.fn_IsHoliday(
    @CheckDate DATE
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsHoliday BIT = 0;
    
    IF EXISTS (
        SELECT 1 
        FROM HR.tbl_Holidays 
        WHERE HolidayDate = @CheckDate 
          AND IsActive = 1
    )
        SET @IsHoliday = 1;
        
    RETURN @IsHoliday;
END

GO

-- [fn_GetActiveSchedule]
CREATE OR ALTER FUNCTION HR.fn_GetActiveSchedule(
    @EmployeeID INT,
    @ForDate DATE
)
RETURNS TABLE
AS
RETURN (
    SELECT TOP 1 s.*
    FROM HR.tbl_EmployeeSchedules es
    JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
    WHERE es.EmployeeID = @EmployeeID
      AND @ForDate BETWEEN es.ValidFrom AND ISNULL(es.ValidTo, '9999-12-31')
    ORDER BY es.ValidFrom DESC
);

GO
