---------------------------------------------------------------------------------------------------
-- PROCEDURE: HR.sp_Attendance_CalculateRange
-- DESCRIPTION: Calcula y actualiza masivamente las asistencias en un rango de fechas
--              CON LOGS DE DEPURACIÓN para diagnosticar cálculo de tardanzas
-- AUTHOR: Sistema de Gestión de Asistencia
-- CREATED: 2024
-- UPDATED: 2024-10-27 - Agregados logs detallados
---------------------------------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE HR.sp_Attendance_CalculateRange
  @FromDate    DATE,
  @ToDate      DATE,
  @EmployeeID  INT  = NULL,
  @Debug       BIT  = 0
AS
BEGIN
  SET NOCOUNT ON;

  -------------------------------------------------------------
  -- 1) Parámetros con defaults seguros
  -------------------------------------------------------------
  DECLARE 
    @GraceMin      INT,
    @LunchMin      INT,
    @OTMin         INT,
    @NightStart    TIME,
    @NightEnd      TIME,
    @FromDate_l    DATE,
    @ToDate_l      DATE,
    @EmployeeID_l  INT;

  SELECT @GraceMin   = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='TARDINESS_GRACE_MIN') AS INT);
  SELECT @LunchMin   = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='LUNCH_MINUTES') AS INT);
  SELECT @OTMin      = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='OT_MIN_THRESHOLD_MIN') AS INT);
  SELECT @NightStart = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_START') AS TIME);
  SELECT @NightEnd   = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_END')   AS TIME);

  SET @GraceMin   = ISNULL(@GraceMin, 0);
  SET @LunchMin   = ISNULL(@LunchMin, 0);
  SET @OTMin      = ISNULL(@OTMin, 0);
  SET @NightStart = ISNULL(@NightStart, TRY_CAST('22:00' AS TIME));
  SET @NightEnd   = ISNULL(@NightEnd,   TRY_CAST('06:00' AS TIME));

  SET @FromDate_l   = @FromDate;
  SET @ToDate_l     = @ToDate;
  SET @EmployeeID_l = @EmployeeID;

  IF @Debug = 1
  BEGIN
    PRINT '============================================================';
    PRINT 'INICIO: HR.sp_Attendance_CalculateRange';
    PRINT 'Rango: ' + CONVERT(VARCHAR(10), @FromDate_l, 120) + ' → ' + CONVERT(VARCHAR(10), @ToDate_l, 120);
    PRINT 'EmpID: ' + COALESCE(CAST(@EmployeeID_l AS VARCHAR(12)), 'TODOS');
    PRINT 'Params: Grace=' + CAST(@GraceMin AS VARCHAR(10)) 
        + ', Lunch=' + CAST(@LunchMin AS VARCHAR(10)) 
        + ', OTMin=' + CAST(@OTMin AS VARCHAR(10)) 
        + ', Night=' + CONVERT(VARCHAR(8), @NightStart, 108) + '→' + CONVERT(VARCHAR(8), @NightEnd, 108);
  END;

  -------------------------------------------------------------
  -- 2) Staging: Calendario y Licencias (rango acotado)
  -------------------------------------------------------------
  IF OBJECT_ID('tempdb..#Cal') IS NOT NULL DROP TABLE #Cal;
  CREATE TABLE #Cal (
    D          DATE NOT NULL PRIMARY KEY,
    IsHoliday  BIT  NOT NULL,
    IsWeekend  BIT  NOT NULL
  );

  INSERT INTO #Cal (D, IsHoliday, IsWeekend)
  SELECT c.D, c.IsHoliday, c.IsWeekend
  FROM HR.vw_Calendar AS c
  WHERE c.D BETWEEN @FromDate_l AND @ToDate_l;

  IF OBJECT_ID('tempdb..#Leave') IS NOT NULL DROP TABLE #Leave;
  CREATE TABLE #Leave (
    EmployeeID INT NOT NULL,
    D          DATE NOT NULL,
    PRIMARY KEY (EmployeeID, D)
  );

  INSERT INTO #Leave (EmployeeID, D)
  SELECT DISTINCT l.EmployeeID, d.D
  FROM HR.vw_LeaveWindows l
  JOIN #Cal d
    ON d.D >= CAST(l.FromDT AS DATE)
   AND d.D <  CAST(l.ToDT   AS DATE)
  WHERE (@EmployeeID_l IS NULL OR l.EmployeeID = @EmployeeID_l);

  -------------------------------------------------------------
  -- 3) Staging: Asistencia por día (materializar la vista)
  -------------------------------------------------------------
  IF OBJECT_ID('tempdb..#A') IS NOT NULL DROP TABLE #A;
  CREATE TABLE #A (
    EmployeeID     INT,
    WorkDate       DATE,
    RequiredMin    INT,
    EntryTime      TIME NULL,
    ExitTime       TIME NULL,
    HasLunchBreak  BIT,
    LunchStart     TIME NULL,
    LunchEnd       TIME NULL,
    FirstIn        DATETIME2 NULL,
    LastOut        DATETIME2 NULL
  );

  INSERT INTO #A (EmployeeID, WorkDate, RequiredMin, EntryTime, ExitTime, HasLunchBreak, LunchStart, LunchEnd, FirstIn, LastOut)
  SELECT a.EmployeeID, a.WorkDate, a.RequiredMin, a.EntryTime, a.ExitTime,
         a.HasLunchBreak, a.LunchStart, a.LunchEnd, a.FirstIn, a.LastOut
  FROM HR.vw_AttendanceDay a
  WHERE a.WorkDate BETWEEN @FromDate_l AND @ToDate_l
    AND (@EmployeeID_l IS NULL OR a.EmployeeID = @EmployeeID_l);

  CREATE CLUSTERED INDEX IX_A ON #A (WorkDate, EmployeeID);

  -------------------------------------------------------------
  -- 4) Cook: unir Calendar + Leave y calcular métricas
  --    NightMinutes con cruce de medianoche y picadas crudas
  -------------------------------------------------------------
  IF OBJECT_ID('tempdb..#Cooked') IS NOT NULL DROP TABLE #Cooked;
  CREATE TABLE #Cooked (
    EmployeeID     INT,
    WorkDate       DATE,
    RequiredMin    INT,
    EntryTime      TIME NULL,
    FirstIn        DATETIME2 NULL,
    LastOut        DATETIME2 NULL,
    IsHoliday      BIT NOT NULL DEFAULT(0),
    IsWeekend      BIT NOT NULL DEFAULT(0),
    HasLeave       BIT NOT NULL DEFAULT(0),
    RawWorkedMin   INT NOT NULL DEFAULT(0),
    TardinessMin   INT NOT NULL DEFAULT(0),
    MinutesLate    INT NOT NULL DEFAULT(0),
    NightMinutes   INT NOT NULL DEFAULT(0)
  );

  INSERT INTO #Cooked (EmployeeID, WorkDate, RequiredMin, EntryTime, FirstIn, LastOut,
                       IsHoliday, IsWeekend, HasLeave, RawWorkedMin, TardinessMin, MinutesLate, NightMinutes)
  SELECT
    a.EmployeeID,
    a.WorkDate,
    ISNULL(a.RequiredMin,0) AS RequiredMin,
    a.EntryTime,

    -- Guardamos lo que venía de la vista
    a.FirstIn,
    a.LastOut,

    cal.IsHoliday,
    cal.IsWeekend,
    CASE WHEN l.EmployeeID IS NOT NULL THEN 1 ELSE 0 END AS HasLeave,

    -- RawWorkedMin = MAX(0, (OutDT - InDT) - @LunchMin) usando ventanas efectivas
    CASE 
      WHEN Eff.InDT IS NULL OR Eff.OutDT IS NULL OR Eff.OutDT < Eff.InDT THEN 0
      ELSE CASE 
             WHEN DATEDIFF(MINUTE, Eff.InDT, Eff.OutDT) - @LunchMin < 0 THEN 0
             ELSE DATEDIFF(MINUTE, Eff.InDT, Eff.OutDT) - @LunchMin
           END
    END AS RawWorkedMin,

    -- TardinessMin = MAX(0, (Eff.InDT - ScheduledEntry) - Grace)
    CASE 
      WHEN Eff.InDT IS NULL OR a.EntryTime IS NULL THEN 0
      ELSE 
        CASE 
          WHEN DATEDIFF(
                 MINUTE,
                 DATEADD(SECOND, DATEDIFF(SECOND, 0, a.EntryTime), CAST(a.WorkDate AS DATETIME2)),
                 Eff.InDT
               ) - @GraceMin < 0 THEN 0
          ELSE DATEDIFF(
                 MINUTE,
                 DATEADD(SECOND, DATEDIFF(SECOND, 0, a.EntryTime), CAST(a.WorkDate AS DATETIME2)),
                 Eff.InDT
               ) - @GraceMin
        END
    END AS TardinessMin,

    -- MinutesLate (si quieres clamp 0, aplica CASE WHEN <0 THEN 0)
    CASE 
      WHEN Eff.InDT IS NULL OR a.EntryTime IS NULL THEN 0
      ELSE DATEDIFF(
             MINUTE,
             DATEADD(SECOND, DATEDIFF(SECOND, 0, a.EntryTime), CAST(a.WorkDate AS DATETIME2)),
             Eff.InDT
           )
    END AS MinutesLate,

    -- NightMinutes con cruce de medianoche y picadas extendidas
    CASE 
      WHEN Eff.InDT IS NULL OR Eff.OutDT IS NULL OR Eff.OutDT <= Eff.InDT THEN 0
      ELSE 
        CASE 
          WHEN NA.OverlapEnd <= NA.OverlapStart THEN 0
          ELSE DATEDIFF(MINUTE, NA.OverlapStart, NA.OverlapEnd)
        END
    END AS NightMinutes
  FROM #A a
  JOIN #Cal cal 
    ON cal.D = a.WorkDate
  LEFT JOIN #Leave l 
    ON l.EmployeeID = a.EmployeeID AND l.D = a.WorkDate

  -- 1) Ventana nocturna por WorkDate (maneja NightEnd <= NightStart → +1 día)
  OUTER APPLY (
    SELECT 
      DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightStart), CAST(a.WorkDate AS DATETIME2)) AS NightStartDT,
      CASE 
        WHEN @NightEnd <= @NightStart 
        THEN DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(a.WorkDate AS DATETIME2)))
        ELSE DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(a.WorkDate AS DATETIME2))
      END AS NightEndDT
  ) N

  -- 2) Reconstrucción de In/Out efectivos a partir de picadas crudas
  --    Rango: [WorkDate 00:00, NightEndDT), incluyendo madrugada del día siguiente
  OUTER APPLY (
    SELECT
      MIN(p.PunchTime) AS MinPunch,
      MAX(p.PunchTime) AS MaxPunch
    FROM HR.tbl_AttendancePunches p
    WHERE p.EmployeeID = a.EmployeeID
      AND p.PunchTime >= CAST(a.WorkDate AS DATETIME2)
      AND p.PunchTime <  N.NightEndDT
  ) P

  -- 3) Definimos In/Out efectivos
  OUTER APPLY (
    SELECT
      COALESCE(a.FirstIn, P.MinPunch) AS InDT,
      COALESCE(a.LastOut, P.MaxPunch) AS OutDT
  ) Eff

  -- 4) Traslape real: [max(InDT, NightStartDT), min(OutDT, NightEndDT)]
  OUTER APPLY (
    SELECT 
      CASE WHEN Eff.InDT  > N.NightStartDT THEN Eff.InDT  ELSE N.NightStartDT END AS OverlapStart,
      CASE WHEN Eff.OutDT < N.NightEndDT   THEN Eff.OutDT ELSE N.NightEndDT    END AS OverlapEnd
  ) NA;

  CREATE CLUSTERED INDEX IX_Cooked ON #Cooked (WorkDate, EmployeeID);

  -------------------------------------------------------------
  -- 5) Debug (sin subconsultas en PRINT/DECLARE)
  -------------------------------------------------------------
  IF @Debug = 1
  BEGIN
    DECLARE @negN INT, @rowsCooked INT;
    SELECT @negN = COUNT(*) FROM #Cooked WHERE NightMinutes < 0;
    SELECT @rowsCooked = COUNT(*) FROM #Cooked;

    PRINT 'Cooked filas: ' + CAST(@rowsCooked AS VARCHAR(20));
    IF @negN > 0
      PRINT N'⚠️ NightMinutes negativos detectados: ' + CAST(@negN AS VARCHAR(10));
  END;

  -------------------------------------------------------------
  -- 6) UPSERT (UPDATE + INSERT)
  -------------------------------------------------------------
  -- UPDATE existentes
  UPDATE T
  SET 
    T.TotalWorkedMinutes = CASE WHEN C.HasLeave = 1 THEN 0 ELSE C.RawWorkedMin END,
    T.RegularMinutes     = CASE 
                             WHEN (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END) 
                                  <= CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END
                             THEN (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END)
                             ELSE (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END)
                           END,
    T.OvertimeMinutes    = CASE 
                             WHEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) - 
                                  (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END)
                                  >= @OTMin
                             THEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) - 
                                  (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END)
                             ELSE 0
                           END,
    T.NightMinutes       = CASE WHEN C.NightMinutes < 0 THEN 0 ELSE C.NightMinutes END,
    T.HolidayMinutes     = CASE WHEN C.IsHoliday=1 OR C.IsWeekend=1 THEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) ELSE 0 END,
    T.TardinessMin       = CASE WHEN C.TardinessMin < 0 THEN 0 ELSE C.TardinessMin END,
    T.RequiredMinutes    = (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END),
    T.MinutesLate        = C.MinutesLate,
    T.FirstPunchIn       = C.FirstIn,
    T.LastPunchOut       = C.LastOut,
    T.Status             = 'Approved'
  FROM HR.tbl_AttendanceCalculations AS T WITH (UPDLOCK, ROWLOCK)
  JOIN #Cooked C
    ON C.EmployeeID = T.EmployeeID
   AND C.WorkDate   = T.WorkDate;

  -- INSERT nuevos
  INSERT INTO HR.tbl_AttendanceCalculations (
    EmployeeID, WorkDate, FirstPunchIn, LastPunchOut,
    TotalWorkedMinutes, RegularMinutes, OvertimeMinutes,
    NightMinutes, HolidayMinutes, TardinessMin, RequiredMinutes,
    MinutesLate, Status
  )
  SELECT 
    C.EmployeeID,
    C.WorkDate,
    C.FirstIn, 
    C.LastOut,
    CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END AS TotalWorkedMinutes,
    CASE 
      WHEN (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END) <= 
           CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END
      THEN (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END)
      ELSE (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END)
    END AS RegularMinutes,
    CASE 
      WHEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) - 
           (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END) >= @OTMin
      THEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) - 
           (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END)
      ELSE 0
    END AS OvertimeMinutes,
    CASE WHEN C.NightMinutes < 0 THEN 0 ELSE C.NightMinutes END,
    CASE WHEN C.IsHoliday=1 OR C.IsWeekend=1 THEN (CASE WHEN C.HasLeave=1 THEN 0 ELSE C.RawWorkedMin END) ELSE 0 END,
    CASE WHEN C.TardinessMin < 0 THEN 0 ELSE C.TardinessMin END,
    (CASE WHEN C.HasLeave=1 OR C.IsHoliday=1 OR C.IsWeekend=1 THEN 0 ELSE C.RequiredMin END) AS RequiredMinutes,
    C.MinutesLate,
    'Approved'
  FROM #Cooked C
  WHERE NOT EXISTS (
    SELECT 1 FROM HR.tbl_AttendanceCalculations T 
    WHERE T.EmployeeID = C.EmployeeID AND T.WorkDate = C.WorkDate
  );

  -------------------------------------------------------------
  -- 7) Debug final (opcional)
  -------------------------------------------------------------
  IF @Debug = 1
  BEGIN
    PRINT 'Muestra TOP (20):';
    SELECT TOP (20) EmployeeID, WorkDate, FirstIn, LastOut,
           EntryTime, RawWorkedMin, TardinessMin, MinutesLate, NightMinutes
    FROM #Cooked
    ORDER BY WorkDate DESC, EmployeeID;
  END;

  -------------------------------------------------------------
  -- 8) Limpieza
  -------------------------------------------------------------
  DROP TABLE IF EXISTS #Cooked;
  DROP TABLE IF EXISTS #A;
  DROP TABLE IF EXISTS #Leave;
  DROP TABLE IF EXISTS #Cal;

END
GO

  -------------------------------------------------------------------
  -- PROCEDIMIENTO PARA CALCULAR HORAS EXTRAS
  -------------------------------------------------------------------
  CREATE OR ALTER PROCEDURE HR.sp_Overtime_Calculate
  @FromDate   DATE,
  @ToDate     DATE,
  @EmployeeID INT = NULL,
  @Debug      BIT = 0
AS
BEGIN
  SET NOCOUNT ON;
  SET XACT_ABORT ON;

  -- Declarar tabla variable para los estados permitidos
  DECLARE @AllowedStatusTypes TABLE (TypeID INT);
  DECLARE @CountWork INT, @CountCap INT; -- Variables para conteos
  
  INSERT INTO @AllowedStatusTypes (TypeID)
  SELECT TypeID FROM HR.ref_Types 
  WHERE Category = 'PLAN_STATUS' AND Name IN ('Aprobado','En Progreso','Borrador');

  ------------------------------------------------------------------
  -- 1) Fuente: AttendanceCalculations (solo HE>0)
  ------------------------------------------------------------------
  SELECT a.EmployeeID, a.WorkDate,
         ISNULL(a.OvertimeMinutes,0) AS OrdMin,
         ISNULL(a.NightMinutes,0)    AS NightMin,
         ISNULL(a.HolidayMinutes,0)  AS HolMin,
         (ISNULL(a.OvertimeMinutes,0)+ISNULL(a.NightMinutes,0)+ISNULL(a.HolidayMinutes,0)) AS RawOTMin
  INTO #AC
  FROM HR.tbl_AttendanceCalculations a
  WHERE a.WorkDate BETWEEN @FromDate AND @ToDate
    AND (@EmployeeID IS NULL OR a.EmployeeID = @EmployeeID)
    AND (ISNULL(a.OvertimeMinutes,0)+ISNULL(a.NightMinutes,0)+ISNULL(a.HolidayMinutes,0)) > 0;

  ------------------------------------------------------------------
  -- 2) Planes OVERTIME aprobados/en progreso + sus empleados
  ------------------------------------------------------------------
  SELECT p.PlanID, p.StartDate, p.EndDate, p.StartTime, p.EndTime,
         p.PlanStatusTypeID, p.OvertimeType, p.Factor
  INTO #Plans
  FROM HR.tbl_TimePlanning p
  WHERE p.PlanType = 'Overtime'
    AND p.EndDate   >= @FromDate
    AND p.StartDate <= @ToDate
    AND p.PlanStatusTypeID IN (SELECT TypeID FROM @AllowedStatusTypes);

  SELECT pe.PlanEmployeeID, pe.PlanID, pe.EmployeeID,
         ISNULL(pe.AssignedHours,0)*60 + ISNULL(pe.AssignedMinutes,0) AS AssignedTotalMin
  INTO #PlanEmp
  FROM HR.tbl_TimePlanningEmployees pe;

  -- Match por empleado/fecha a 1 plan (elige el de mayor PlanID)
  ;WITH MatchPlan AS (
    SELECT ac.EmployeeID, ac.WorkDate, ac.OrdMin, ac.NightMin, ac.HolMin, ac.RawOTMin,
           pe.PlanEmployeeID, pe.PlanID,
           pl.StartTime, pl.EndTime,
           pl.OvertimeType AS PlanOvertimeType,
           pl.Factor       AS PlanFactor,
           pe.AssignedTotalMin,
           ROW_NUMBER() OVER (PARTITION BY ac.EmployeeID, ac.WorkDate ORDER BY pe.PlanID DESC) AS rn
    FROM #AC ac
    JOIN #PlanEmp pe ON pe.EmployeeID = ac.EmployeeID
    JOIN #Plans  pl ON pl.PlanID = pe.PlanID
                  AND ac.WorkDate BETWEEN pl.StartDate AND pl.EndDate
  )
  SELECT * INTO #Work FROM MatchPlan WHERE rn=1;

  -- Corregido: Usar variable para el conteo
  SELECT @CountWork = COUNT(*) FROM #Work;
  IF @Debug=1 PRINT 'Filas con plan emparejado: ' + CAST(@CountWork AS VARCHAR(12));

  IF NOT EXISTS(SELECT 1 FROM #Work)
  BEGIN
    IF @Debug=1 PRINT 'No hay planificaciones Overtime que cubran el rango/empleados indicados.';
    RETURN;
  END;

  ------------------------------------------------------------------
  -- 3) Cálculo de límites (ventana diaria del plan y cuota restante)
  ------------------------------------------------------------------
  -- Primero calculamos los totales ejecutados por PlanEmployee
  SELECT x.PlanEmployeeID,
         SUM(ISNULL(x.OvertimeMinutes,0)+ISNULL(x.NightMinutes,0)+ISNULL(x.HolidayMinutes,0)) AS ExecSoFarMin
  INTO #ExecAgg
  FROM HR.tbl_TimePlanningExecution x
  GROUP BY x.PlanEmployeeID;

  -- Base con todos los datos
  SELECT 
    w.PlanEmployeeID, w.EmployeeID, w.PlanID, w.WorkDate,
    w.OrdMin, w.NightMin, w.HolMin, w.RawOTMin,
    CAST(w.StartTime AS DATETIME2) AS PlanStartDT,
    CAST(w.EndTime   AS DATETIME2) AS PlanEndDT,
    w.AssignedTotalMin,
    ISNULL(e.ExecSoFarMin,0) AS ExecSoFarMin,
    w.PlanOvertimeType, w.PlanFactor
  INTO #Base
  FROM #Work w
  LEFT JOIN #ExecAgg e ON e.PlanEmployeeID = w.PlanEmployeeID;

  -- Calcular límites
  SELECT b.*,
         CASE 
           WHEN DATEDIFF(MINUTE, b.PlanStartDT, b.PlanEndDT) < 0 THEN 0
           ELSE DATEDIFF(MINUTE, b.PlanStartDT, b.PlanEndDT)
         END AS PlanWindowMin,
         CASE 
           WHEN ISNULL(b.AssignedTotalMin,0) <= 0 THEN NULL
           ELSE CASE 
                  WHEN b.AssignedTotalMin - b.ExecSoFarMin < 0 THEN 0 
                  ELSE b.AssignedTotalMin - b.ExecSoFarMin 
                END
         END AS RemainingPlanMin
  INTO #Limits
  FROM #Base b;

  -- Aplicar límite de ventana del plan
  SELECT 
    l.*,
    CASE 
      WHEN l.RawOTMin <= 0 THEN 0
      WHEN l.PlanWindowMin IS NULL THEN l.RawOTMin
      WHEN l.RawOTMin <= l.PlanWindowMin THEN l.RawOTMin 
      ELSE l.PlanWindowMin 
    END AS TempCap
  INTO #Cap1
  FROM #Limits l;

  -- Aplicar límite de cuota restante y guardar resultado final
  SELECT 
    c.PlanEmployeeID, c.EmployeeID, c.PlanID, c.WorkDate,
    c.OrdMin, c.NightMin, c.HolMin, c.RawOTMin,
    c.PlanStartDT, c.PlanEndDT, c.AssignedTotalMin, c.ExecSoFarMin,
    c.PlanOvertimeType, c.PlanFactor, c.PlanWindowMin, c.RemainingPlanMin, c.TempCap,
    CASE 
      WHEN c.RemainingPlanMin IS NULL THEN c.TempCap
      WHEN c.TempCap <= c.RemainingPlanMin THEN c.TempCap 
      ELSE c.RemainingPlanMin 
    END AS CappedOTMin
  INTO #Cap
  FROM #Cap1 c
  WHERE CASE 
          WHEN c.RemainingPlanMin IS NULL THEN c.TempCap
          WHEN c.TempCap <= c.RemainingPlanMin THEN c.TempCap 
          ELSE c.RemainingPlanMin 
        END > 0;

  -- Corregido: Usar variable para el conteo
  SELECT @CountCap = COUNT(*) FROM #Cap;
  IF @Debug=1 PRINT 'Filas a ejecutar (capadas): ' + CAST(@CountCap AS VARCHAR(12));

  ------------------------------------------------------------------
  -- 4) Distribuir proporcionalmente (Ord / Noct / Feriado) tras el capado
  ------------------------------------------------------------------
  SELECT 
    c.PlanEmployeeID, c.EmployeeID, c.WorkDate, c.CappedOTMin,
    c.OrdMin, c.NightMin, c.HolMin,
    (ISNULL(c.OrdMin,0)+ISNULL(c.NightMin,0)+ISNULL(c.HolMin,0)) AS RawSum
  INTO #Parts
  FROM #Cap c;

  SELECT 
    p.PlanEmployeeID, p.EmployeeID, p.WorkDate, p.CappedOTMin,
    CASE WHEN p.RawSum>0 THEN CAST(p.CappedOTMin * (ISNULL(p.OrdMin,0)   *1.0)/p.RawSum AS INT) ELSE 0 END AS NewOrdMin,
    CASE WHEN p.RawSum>0 THEN CAST(p.CappedOTMin * (ISNULL(p.NightMin,0) *1.0)/p.RawSum AS INT) ELSE 0 END AS NewNightMin,
    CASE WHEN p.RawSum>0 THEN CAST(p.CappedOTMin * (ISNULL(p.HolMin,0)   *1.0)/p.RawSum AS INT) ELSE 0 END AS NewHolMin
  INTO #Dist
  FROM #Parts p;

  SELECT d.*,
         (d.CappedOTMin - (d.NewOrdMin + d.NewNightMin + d.NewHolMin)) AS DiffMin
  INTO #DistFix
  FROM #Dist d;

  SELECT 
    PlanEmployeeID, EmployeeID, WorkDate, CappedOTMin,
    (NewOrdMin + CASE WHEN DiffMin>0 THEN DiffMin ELSE 0 END) AS NewOrdMin,
    NewNightMin,
    NewHolMin
  INTO #Exec
  FROM #DistFix;

  ------------------------------------------------------------------
  -- 5) UPSERT en HR.tbl_TimePlanningExecution
  ------------------------------------------------------------------
  MERGE HR.tbl_TimePlanningExecution AS T
  USING (
    SELECT 
      e.PlanEmployeeID, e.WorkDate,
      e.CappedOTMin AS TotalMinutes,
      e.NewOrdMin   AS OvertimeMinutes,
      e.NewNightMin AS NightMinutes,
      e.NewHolMin   AS HolidayMinutes
    FROM #Exec e
  ) AS S
    ON (T.PlanEmployeeID = S.PlanEmployeeID AND T.WorkDate = S.WorkDate)
  WHEN MATCHED THEN
    UPDATE SET 
      T.TotalMinutes    = S.TotalMinutes,
      T.OvertimeMinutes = S.OvertimeMinutes,
      T.NightMinutes    = S.NightMinutes,
      T.HolidayMinutes  = S.HolidayMinutes
      --T.UpdatedAt       = GETDATE()
  WHEN NOT MATCHED THEN
    INSERT (PlanEmployeeID, WorkDate, TotalMinutes, RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, CreatedAt)
    VALUES (S.PlanEmployeeID, S.WorkDate, S.TotalMinutes, 0, S.OvertimeMinutes, S.NightMinutes, S.HolidayMinutes, GETDATE());

    ------------------------------------------------------------------
  -- 6) Replica a HR.tbl_Overtime (EXECUTED; priorizando Factor del Plan)
  ------------------------------------------------------------------
  ;WITH ExecPerDay AS (
    SELECT 
      pe.EmployeeID, 
      x.WorkDate,
      -- minutos consolidados por tipo desde la ejecución del plan
      SUM(ISNULL(x.OvertimeMinutes,0)) AS OrdMin,
      SUM(ISNULL(x.NightMinutes,0))    AS NightMin,
      SUM(ISNULL(x.HolidayMinutes,0))  AS HolMin,
      -- Factor y Tipo definidos en la planificacion
      -- Si por cualquier razón hay múltiples planes en un mismo día (no deseado),
      -- usamos MAX por simplicidad. Si quieres pagar por tipo/plan, te doy variante multi-fila.
      MAX(p.Factor)       AS PlanFactor,
      MAX(p.OvertimeType) AS PlanOvertimeType
    FROM HR.tbl_TimePlanningExecution x
    JOIN HR.tbl_TimePlanningEmployees pe ON pe.PlanEmployeeID = x.PlanEmployeeID
    JOIN HR.tbl_TimePlanning p          ON p.PlanID = pe.PlanID
    WHERE p.PlanType = 'Overtime'
      AND x.WorkDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR pe.EmployeeID = @EmployeeID)
    GROUP BY pe.EmployeeID, x.WorkDate
  ),
  Resolved AS (
    SELECT 
      e.EmployeeID,
      e.WorkDate,
      CAST((ISNULL(e.OrdMin,0)+ISNULL(e.NightMin,0)+ISNULL(e.HolMin,0)) AS DECIMAL(10,2))/60.0 AS Hours,
      -- Prioriza el tipo según la mezcla ejecutada; si quieres forzar el del plan, ver nota abajo
      CASE 
        WHEN ISNULL(e.HolMin,0)   > 0 THEN 'Feriado'
        WHEN ISNULL(e.NightMin,0) > 0 THEN 'Nocturna'
        ELSE 'Ordinaria'
      END AS OvertimeType_Executed,
      e.PlanFactor,
      e.PlanOvertimeType
    FROM ExecPerDay e
    WHERE (ISNULL(e.OrdMin,0)+ISNULL(e.NightMin,0)+ISNULL(e.HolMin,0)) > 0
  )
  MERGE HR.tbl_Overtime AS T
  USING (
    SELECT 
      r.EmployeeID, 
      r.WorkDate, 
      r.Hours, 
      -- Si quieres respetar SIEMPRE el tipo de la planificación, usa r.PlanOvertimeType aquí.
      -- De momento mantenemos el tipo "ejecutado" (dominante del día) para coherencia con minutos.
      r.OvertimeType_Executed AS OvertimeType,
      -- PRIORIDAD: Factor del Plan -> Factor de Config -> 1.0
      COALESCE(r.PlanFactor, oc.Factor, 1.0) AS Factor
    FROM Resolved r
    LEFT JOIN HR.tbl_OvertimeConfig oc ON oc.OvertimeType = r.OvertimeType_Executed
  ) AS S
    ON (T.EmployeeID = S.EmployeeID AND T.WorkDate = S.WorkDate)
  WHEN MATCHED THEN
    UPDATE SET
      -- No pisar APPROVED/PAID
      T.OvertimeType = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.OvertimeType ELSE S.OvertimeType END,
      T.Hours        = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Hours        ELSE S.Hours        END,
      T.ActualHours  = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.ActualHours  ELSE S.Hours        END,
      T.Factor       = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Factor       ELSE S.Factor       END,
      T.Status       = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Status ELSE 'EXECUTED' END
  WHEN NOT MATCHED THEN
    INSERT (EmployeeID, WorkDate, OvertimeType, Hours, ActualHours, Factor, Status, CreatedAt)
    VALUES (S.EmployeeID, S.WorkDate, S.OvertimeType, S.Hours, S.Hours, S.Factor, 'EXECUTED', GETDATE());


  IF @Debug=1 PRINT 'Consolidación completada.';
END
GO
  
  -------------------------------------------------------------------
  -- FIN -- PROCEDIMIENTO PARA CALCULAR HORAS EXTRAS
  -------------------------------------------------------------------
  -------------------------------------------------------------------
  -- Inicio -- PROCEDIMIENTO PARA EJECUTAR TODOS LOS PROCEDIMIENTOS DE ASISTENCIA, PERMISOS, VACACIONES 
  -------------------------------------------------------------------  
  
  CREATE OR ALTER PROCEDURE HR.sp_Attendance_RunAll
  @FromDate            DATE,
  @ToDate              DATE,
  @EmployeeID          INT = NULL,
  @Debug               BIT = 0,
  @ApplyJustifications BIT = 1,
  @ApplyRecovery       BIT = 1,
  @RunOvertime         BIT = 1   -- << activa o no la consolidación de Overtime
	AS
	BEGIN
	  SET NOCOUNT ON;
	  SET XACT_ABORT ON;

	  BEGIN TRY
		BEGIN TRAN;

		-- 1) Cálculo base (asistencia, tardanzas, night, feriados)
		EXEC HR.sp_Attendance_CalculateRange
			 @FromDate = @FromDate,
			 @ToDate   = @ToDate,
			 @EmployeeID = @EmployeeID,
			 @Debug    = @Debug;

		-- 2) Post-proceso: justificaciones y recuperaciones (si corresponden)
		EXEC HR.sp_Attendance_PostProcess
			 @FromDate   = @FromDate,
			 @ToDate     = @ToDate,
			 @EmployeeID = @EmployeeID,
			 @ApplyJustifications = @ApplyJustifications,
			 @ApplyRecovery       = @ApplyRecovery;

		-- 3) Consolidación Overtime (cap por plan, factor, réplica)
		IF @RunOvertime = 1
		BEGIN
		  EXEC HR.sp_Overtime_Calculate
			   @FromDate   = @FromDate,
			   @ToDate     = @ToDate,
			   @EmployeeID = @EmployeeID,
			   @Debug      = @Debug;
		END

		COMMIT TRAN;
	  END TRY
	  BEGIN CATCH
		IF XACT_STATE() <> 0 ROLLBACK TRAN;

		DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE(),
				@ErrNum INT = ERROR_NUMBER(),
				@ErrSev INT = ERROR_SEVERITY(),
				@ErrSta INT = ERROR_STATE(),
				@ErrLin INT = ERROR_LINE();

		RAISERROR('sp_Attendance_RunAll: %s (Num:%d, Sev:%d, Sta:%d, Lin:%d)',
				  @ErrSev, 1, @ErrMsg, @ErrNum, @ErrSev, @ErrSta, @ErrLin);
	  END CATCH
	END
	GO

  ------------------------------------------------------------------- 
  -- FIN -- PROCEDIMIENTO PARA EJECUTAR TODOS LOS PROCEDIMIENTOS DE ASISTENCIA, PERMISOS, VACACIONES 
  -------------------------------------------------------------------




--Aplicar justificaciones (anula atraso o ausencia)
CREATE OR ALTER PROCEDURE HR.sp_Justifications_Apply
  @FromDate DATE, @ToDate DATE, @EmployeeID INT = NULL
AS
BEGIN
  UPDATE ac
  SET TardinessMin = 0
  FROM HR.tbl_AttendanceCalculations ac
  WHERE ac.WorkDate BETWEEN @FromDate AND @ToDate
    AND (@EmployeeID IS NULL OR ac.EmployeeID=@EmployeeID)
    AND EXISTS(SELECT 1 FROM HR.tbl_PunchJustifications j
               WHERE j.EmployeeID=ac.EmployeeID 
                 AND j.Status='APPROVED'
                 AND CAST(j.JustificationDate AS DATE)=ac.WorkDate);
END
GO


--Consolidar recuperaciones (resta deuda → OwedMinutes)
CREATE OR ALTER PROCEDURE HR.sp_Recovery_Apply
  @FromDate DATE, @ToDate DATE, @EmployeeID INT = NULL
AS
BEGIN
  -- Minutos faltantes = max(0, ReqMin - TotalWorked)
  ;WITH debt AS (
    SELECT EmployeeID, WorkDate,
           GREATEST(0, RegularMinutes + OvertimeMinutes + HolidayMinutes - TotalWorkedMinutes) AS NegGap
    FROM HR.tbl_AttendanceCalculations
    WHERE WorkDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR EmployeeID=@EmployeeID)
  ),
  rec AS (
    SELECT p.EmployeeID, l.ExecutedDate, SUM(l.MinutesRecovered) AS Recovered
    FROM HR.tbl_TimeRecoveryPlans p
    JOIN HR.tbl_TimeRecoveryLogs l ON l.RecoveryPlanID=p.RecoveryPlanID
    WHERE l.ExecutedDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR p.EmployeeID=@EmployeeID)
    GROUP BY p.EmployeeID, l.ExecutedDate
  )
  UPDATE ac
  SET TotalWorkedMinutes = TotalWorkedMinutes + ISNULL(r.Recovered,0)
  FROM HR.tbl_AttendanceCalculations ac
  LEFT JOIN rec r ON r.EmployeeID=ac.EmployeeID AND r.ExecutedDate=ac.WorkDate;
END
GO


/*Horas extra: valuación y líneas de pago

	Base: HR.tbl_Overtime (planificadas/ejecutadas con factor en HR.tbl_OvertimeConfig).

	Valor hora: RMU / (BASE_HOURS_PER_DAY * 30) o desde Payroll.
	*/
	CREATE OR ALTER PROCEDURE HR.sp_Overtime_Price
  @Period CHAR(7) -- 'YYYY-MM'
AS
BEGIN
  DECLARE @BaseHoursPerDay INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='BASE_HOURS_PER_DAY') AS INT);

  ;WITH rmu AS (
    SELECT e.EmployeeID, og.RMU,
           CAST((og.RMU / (@BaseHoursPerDay*30.0)) AS DECIMAL(12,4)) AS HourRate
    FROM HR.tbl_Employees e
    LEFT JOIN HR.tbl_jobs j ON j.JobID = (SELECT TOP 1 JobID FROM HR.tbl_Contracts c 
                                          WHERE c.PersonID=e.PersonID AND (c.EndDate IS NULL OR c.EndDate >= GETDATE())
                                          ORDER BY c.StartDate DESC)
    LEFT JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID
  ),
  ot AS (
    SELECT o.EmployeeID, o.OvertimeType, o.Hours, oc.Factor
    FROM HR.tbl_Overtime o 
    JOIN HR.tbl_OvertimeConfig oc ON oc.OvertimeType=o.OvertimeType
    WHERE CONVERT(CHAR(7), o.WorkDate, 126) = @Period
      AND o.Status IN ('Verified','Paid')
  )
  SELECT o.EmployeeID, o.OvertimeType, SUM(o.Hours) AS Hours,
         MAX(o.Factor) AS Factor,
         MAX(r.HourRate) AS HourRate,
         CAST(SUM(o.Hours) * MAX(o.Factor) * MAX(r.HourRate) AS DECIMAL(12,2)) AS Amount
  INTO #OTPrice
  FROM ot o
  JOIN rmu r ON r.EmployeeID=o.EmployeeID
  GROUP BY o.EmployeeID, o.OvertimeType;

  -- Generar/actualizar PayrollLines
  MERGE HR.tbl_PayrollLines AS T
  USING (
    SELECT p.PayrollID, o.EmployeeID, 
           'Overtime' AS LineType,
           CONCAT('HE ', o.OvertimeType) AS Concept,
           o.Hours AS Quantity,
           o.HourRate * o.Factor AS UnitValue
    FROM HR.tbl_Payroll p
    JOIN #OTPrice o ON o.EmployeeID=p.EmployeeID
    WHERE p.Period=@Period
  ) S ON 1=0
  WHEN NOT MATCHED THEN
    INSERT (PayrollID, LineType, Concept, Quantity, UnitValue)
    VALUES (S.PayrollID, S.LineType, S.Concept, S.Quantity, S.UnitValue);

  DROP TABLE #OTPrice;
END
GO


--Descuentos y subsidios (nómina)
--E1) Descuento por atrasos/ausencias
CREATE OR ALTER PROCEDURE HR.sp_Payroll_Discounts
  @Period CHAR(7)
AS
BEGIN
  DECLARE @BaseHoursPerDay INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='BASE_HOURS_PER_DAY') AS INT),
          @TardyRate       DECIMAL(6,2) = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='TARDINESS_DISCOUNT_RATE') AS DECIMAL(6,2));

  ;WITH rmu AS (
    SELECT e.EmployeeID, og.RMU,
           CAST((og.RMU / (@BaseHoursPerDay*30.0)) AS DECIMAL(12,4)) AS HourRate
    FROM HR.tbl_Employees e
    LEFT JOIN HR.tbl_jobs j ON j.JobID = (SELECT TOP 1 JobID FROM HR.tbl_Contracts c 
                                          WHERE c.PersonID=e.PersonID AND (c.EndDate IS NULL OR c.EndDate >= GETDATE())
                                          ORDER BY c.StartDate DESC)
    LEFT JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID
  ),
  agg AS (
    SELECT ac.EmployeeID,
           SUM(CASE WHEN CONVERT(CHAR(7), ac.WorkDate, 126)=@Period THEN ac.TardinessMin ELSE 0 END) AS TardyMin,
           SUM(CASE WHEN CONVERT(CHAR(7), ac.WorkDate, 126)=@Period 
                    THEN GREATEST(0, ac.RequiredMinutes - ac.TotalWorkedMinutes) ELSE 0 END) AS AbsenceMin
    FROM HR.tbl_AttendanceCalculations ac
    GROUP BY ac.EmployeeID
  )
  SELECT p.PayrollID, a.EmployeeID,
         (a.TardyMin + a.AbsenceMin) / 60.0 AS QtyHours,
         r.HourRate * @TardyRate AS UnitValue,
         CAST(((a.TardyMin + a.AbsenceMin) / 60.0) * (r.HourRate * @TardyRate) AS DECIMAL(12,2)) AS Amount
  INTO #Disc
  FROM HR.tbl_Payroll p
  JOIN agg a ON a.EmployeeID=p.EmployeeID
  JOIN rmu r ON r.EmployeeID=a.EmployeeID
  WHERE p.Period=@Period;

  -- Línea de deducción
  MERGE HR.tbl_PayrollLines AS T
  USING (
    SELECT PayrollID,'Deduction' AS LineType,'Descuento por atrasos/ausencias' AS Concept, QtyHours AS Quantity, UnitValue
    FROM #Disc WHERE QtyHours>0
  ) S ON 1=0
  WHEN NOT MATCHED THEN
    INSERT (PayrollID, LineType, Concept, Quantity, UnitValue)
    VALUES (S.PayrollID, S.LineType, S.Concept, S.Quantity, S.UnitValue);

  DROP TABLE #Disc;
END
GO

--E2)Subsidios/recargos (nocturno/feriado)

--(agrega líneas positivas tipo “Subsidy” si tu política paga recargos)

CREATE OR ALTER PROCEDURE HR.sp_Payroll_Subsidies
  @Period CHAR(7)
AS
BEGIN
  DECLARE @BaseHoursPerDay INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='BASE_HOURS_PER_DAY') AS INT);

  ;WITH rmu AS (
    SELECT e.EmployeeID, og.RMU,
           CAST((og.RMU / (@BaseHoursPerDay*30.0)) AS DECIMAL(12,4)) AS HourRate
    FROM HR.tbl_Employees e
    LEFT JOIN HR.tbl_jobs j ON j.JobID = (SELECT TOP 1 JobID FROM HR.tbl_Contracts c 
                                          WHERE c.PersonID=e.PersonID AND (c.EndDate IS NULL OR c.EndDate >= GETDATE())
                                          ORDER BY c.StartDate DESC)
    LEFT JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID
  ),
  agg AS (
    SELECT EmployeeID,
      SUM(CASE WHEN CONVERT(CHAR(7),WorkDate,126)=@Period THEN NightMinutes ELSE 0 END)/60.0 AS NightHours,
      SUM(CASE WHEN CONVERT(CHAR(7),WorkDate,126)=@Period THEN HolidayMinutes ELSE 0 END)/60.0 AS HolidayHours
    FROM HR.tbl_AttendanceCalculations
    GROUP BY EmployeeID
  )
  SELECT p.PayrollID, a.EmployeeID, 
         'Subsidy' AS LineType,
         CASE WHEN a.NightHours>0 THEN 'Recargo nocturno' ELSE 'Recargo feriado' END AS Concept,
         CASE WHEN a.NightHours>0 THEN a.NightHours ELSE a.HolidayHours END AS Quantity,
         r.HourRate AS UnitValue
  FROM HR.tbl_Payroll p
  JOIN agg a ON a.EmployeeID=p.EmployeeID
  JOIN rmu r ON r.EmployeeID=a.EmployeeID
  WHERE p.Period=@Period AND (a.NightHours>0 OR a.HolidayHours>0);
END
GO
