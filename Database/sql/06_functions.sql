-- Source: 01-dbUtaSystem_HR_ordered_with_seed.sql
CREATE FUNCTION HR.fn_GetBusinessDays(@StartDate date, @EndDate date)
RETURNS INT
AS
BEGIN
    DECLARE @d date = @StartDate, @cnt int = 0;
    WHILE @d <= @EndDate
    BEGIN
        IF DATENAME(WEEKDAY,@d) NOT IN ('Saturday','Sunday') SET @cnt += 1;
        SET @d = DATEADD(DAY,1,@d);
    END
    RETURN @cnt;
END
GO