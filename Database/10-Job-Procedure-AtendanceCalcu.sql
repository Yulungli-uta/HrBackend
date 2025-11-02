---------------------------------------------------------------------------------------------------
-- PROCEDURE: HR.sp_Attendance_CalculateRange
-- DESCRIPTION: Calcula y actualiza masivamente las asistencias en un rango de fechas
--              CON LOGS DE DEPURACIÓN para diagnosticar cálculo de tardanzas
-- AUTHOR: Sistema de Gestión de Asistencia
-- CREATED: 2024
-- UPDATED: 2024-10-27 - Agregados logs detallados
---------------------------------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE HR.sp_Attendance_CalculateRange
  @FromDate DATE, 
  @ToDate DATE, 
  @EmployeeID INT = NULL,
  @Debug BIT = 0  -- NUEVO: Activar logs de depuración
AS
BEGIN
  SET NOCOUNT ON;

  -------------------------------------------------------------------
  -- DECLARACIÓN DE PARÁMETROS DEL SISTEMA
  -------------------------------------------------------------------
  DECLARE 
    @GraceMin INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='TARDINESS_GRACE_MIN') AS INT),
    @LunchMin INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='LUNCH_MINUTES') AS INT),
    @OTMin   INT = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='OT_MIN_THRESHOLD_MIN') AS INT);

  -- LOG 1: Parámetros del sistema
  PRINT '============================================================';
  PRINT 'INICIANDO CÁLCULO DE ASISTENCIA';
  PRINT '============================================================';
  PRINT 'Rango de fechas: ' + CONVERT(VARCHAR(10), @FromDate, 120) + ' al ' + CONVERT(VARCHAR(10), @ToDate, 120);
  PRINT 'EmployeeID filtrado: ' + ISNULL(CAST(@EmployeeID AS VARCHAR(10)), 'TODOS');
  PRINT '';
  PRINT 'PARÁMETROS DEL SISTEMA:';
  PRINT '  - Minutos de gracia (tardanzas): ' + CAST(@GraceMin AS VARCHAR(10));
  PRINT '  - Minutos de almuerzo: ' + CAST(@LunchMin AS VARCHAR(10));
  PRINT '  - Umbral mínimo horas extras: ' + CAST(@OTMin AS VARCHAR(10));
  PRINT '';

  -------------------------------------------------------------------
  -- TABLA TEMPORAL PARA CÁLCULOS
  -------------------------------------------------------------------
  DROP TABLE IF EXISTS #CookedData;
  
  CREATE TABLE #CookedData (
    EmployeeID INT,
    WorkDate DATE,
    RequiredMin INT,
    EntryTime TIME,
    ExitTime TIME,
    HasLunchBreak BIT,
    LunchStart TIME,
    LunchEnd TIME,
    FirstIn DATETIME2,
    LastOut DATETIME2,
    HasLeave BIT,
    IsHoliday BIT,
    IsWeekend BIT,
    RawWorkedMin INT,
    TardinessMin INT NOT NULL DEFAULT 0,
    ScheduledEntry DATETIME2,
    MinutesLate INT
  );

  -------------------------------------------------------------------
  -- CÁLCULO Y ALMACENAMIENTO EN TABLA TEMPORAL
  -------------------------------------------------------------------
  INSERT INTO #CookedData
  SELECT 
    b.EmployeeID,
    b.WorkDate,
    b.RequiredMin,
    b.EntryTime,
    b.ExitTime,
    b.HasLunchBreak,
    b.LunchStart,
    b.LunchEnd,
    b.FirstIn,
    b.LastOut,
    b.HasLeave,
    b.IsHoliday,
    b.IsWeekend,
    
    -- CÁLCULO DE MINUTOS TRABAJADOS BRUTOS
    CASE 
      WHEN b.FirstIn IS NULL OR b.LastOut IS NULL THEN 0
      ELSE 
        DATEDIFF(MINUTE, b.FirstIn, b.LastOut)
        - CASE 
            WHEN b.HasLunchBreak = 1 AND b.LunchStart IS NOT NULL AND b.LunchEnd IS NOT NULL 
            THEN DATEDIFF(
              MINUTE, 
              CAST(CONVERT(varchar(10), b.WorkDate, 120) + ' ' + CONVERT(varchar(8), b.LunchEnd, 108) AS DATETIME2),
              CAST(CONVERT(varchar(10), b.WorkDate, 120) + ' ' + CONVERT(varchar(8), b.LunchStart, 108) AS DATETIME2)
            ) * -1
            ELSE 0 
          END
    END AS RawWorkedMin,

    -- ✅ CÁLCULO CORREGIDO DE MINUTOS DE TARDANZA
    CASE 
      WHEN b.FirstIn IS NULL OR b.EntryTime IS NULL THEN 0
      ELSE 
        CASE 
          WHEN DATEDIFF(
            MINUTE,
            CAST(CONVERT(VARCHAR(10), b.WorkDate, 120) + ' ' + CONVERT(VARCHAR(8), b.EntryTime, 108) AS DATETIME2),
            b.FirstIn
          ) - @GraceMin <= 0 
          THEN 0 
          ELSE DATEDIFF(
            MINUTE,
            CAST(CONVERT(VARCHAR(10), b.WorkDate, 120) + ' ' + CONVERT(VARCHAR(8), b.EntryTime, 108) AS DATETIME2),
            b.FirstIn
          ) - @GraceMin
        END
    END AS TardinessMin,

    -- HORA PROGRAMADA DE ENTRADA
    CAST(CONVERT(varchar(10), b.WorkDate, 120) + ' ' + CONVERT(varchar(8), b.EntryTime, 108) AS DATETIME2) AS ScheduledEntry,
    
    -- MINUTOS DE RETRASO (SIN GRACIA)
    CASE 
      WHEN b.FirstIn IS NULL OR b.EntryTime IS NULL THEN 0
      ELSE DATEDIFF(
        MINUTE,
        CAST(CONVERT(varchar(10), b.WorkDate, 120) + ' ' + CONVERT(varchar(8), b.EntryTime, 108) AS DATETIME2),
        b.FirstIn
      )
    END AS MinutesLate

  FROM (
    SELECT 
      a.EmployeeID, 
      a.WorkDate, 
      a.RequiredMin,
      a.EntryTime,
      a.ExitTime,
      a.HasLunchBreak,
      a.LunchStart,
      a.LunchEnd,
      a.FirstIn,
      a.LastOut,
      
      CASE WHEN EXISTS (
        SELECT 1 
        FROM HR.vw_LeaveWindows l 
        WHERE l.EmployeeID = a.EmployeeID 
          AND a.WorkDate >= CAST(l.FromDT AS DATE) 
          AND a.WorkDate <  CAST(l.ToDT   AS DATE)
      ) THEN 1 ELSE 0 END AS HasLeave,
      
      (SELECT TOP 1 IsHoliday FROM HR.vw_Calendar c WHERE c.D = a.WorkDate) AS IsHoliday,
      (SELECT TOP 1 IsWeekend FROM HR.vw_Calendar c WHERE c.D = a.WorkDate) AS IsWeekend
    
    FROM HR.vw_AttendanceDay a
    WHERE a.WorkDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR a.EmployeeID = @EmployeeID)
  ) b;

  -------------------------------------------------------------------
  -- LOGS DE DEPURACIÓN
  -------------------------------------------------------------------
  DECLARE @TotalRecords INT = (SELECT COUNT(*) FROM #CookedData);
  DECLARE @NullEntryTime INT = (SELECT COUNT(*) FROM #CookedData WHERE EntryTime IS NULL);
  DECLARE @NullFirstIn INT = (SELECT COUNT(*) FROM #CookedData WHERE FirstIn IS NULL);
  DECLARE @RecordsWithTardiness INT = (SELECT COUNT(*) FROM #CookedData WHERE TardinessMin > 0);

  PRINT '🔍 VERIFICACIÓN DE CAMPOS CRÍTICOS';
  PRINT '------------------------------------------------------------';
  PRINT 'Total registros procesados: ' + CAST(@TotalRecords AS VARCHAR(10));
  PRINT 'Registros sin EntryTime (hora programada): ' + CAST(@NullEntryTime AS VARCHAR(10));
  PRINT 'Registros sin FirstIn (primera entrada): ' + CAST(@NullFirstIn AS VARCHAR(10));
  PRINT 'Registros con tardanza calculada: ' + CAST(@RecordsWithTardiness AS VARCHAR(10));
  PRINT '';

  IF @NullEntryTime > 0 OR @NullFirstIn > 0
  BEGIN
    PRINT '⚠️ PROBLEMA DETECTADO: Campos NULL impiden calcular tardanzas';
    PRINT '   Verifique la vista HR.vw_AttendanceDay';
    PRINT '';
  END

  -- Logs detallados si hay problemas o si Debug está activo
  IF @Debug = 1 OR EXISTS(SELECT 1 FROM #CookedData WHERE MinutesLate > @GraceMin AND TardinessMin = 0 AND FirstIn IS NOT NULL)
  BEGIN
    PRINT '============================================================';
    PRINT 'DETALLE DE CÁLCULOS DE TARDANZA';
    PRINT '============================================================';
    PRINT '';

    -- Mostrar registros con problema
    IF EXISTS(SELECT 1 FROM #CookedData WHERE MinutesLate > @GraceMin AND TardinessMin = 0)
    BEGIN
      PRINT '⚠️ ALERTAS: Registros con llegada tardía pero tardanza = 0';
      PRINT '------------------------------------------------------------';
      
      DECLARE @AlertMsg NVARCHAR(MAX);
      DECLARE alert_cursor CURSOR FOR
      SELECT 
        'Empleado: ' + CAST(EmployeeID AS VARCHAR(10)) +
        ' | Fecha: ' + CONVERT(VARCHAR(10), WorkDate, 120) +
        ' | Programada: ' + ISNULL(CONVERT(VARCHAR(8), ScheduledEntry, 108), 'NULL') +
        ' | Real: ' + ISNULL(CONVERT(VARCHAR(8), FirstIn, 108), 'NULL') +
        ' | Tarde: ' + CAST(MinutesLate AS VARCHAR(10)) + ' min' +
        ' | Gracia: ' + CAST(@GraceMin AS VARCHAR(10)) + ' min' +
        ' | Tardanza: ' + CAST(TardinessMin AS VARCHAR(10)) + ' min' +
        CASE WHEN HasLeave = 1 THEN ' [LICENCIA]' ELSE '' END +
        CASE WHEN IsHoliday = 1 THEN ' [FESTIVO]' ELSE '' END +
        CASE WHEN IsWeekend = 1 THEN ' [FIN DE SEMANA]' ELSE '' END
      FROM #CookedData
      WHERE MinutesLate > @GraceMin AND TardinessMin = 0
      ORDER BY WorkDate, EmployeeID;

      OPEN alert_cursor;
      FETCH NEXT FROM alert_cursor INTO @AlertMsg;
      
      WHILE @@FETCH_STATUS = 0
      BEGIN
        PRINT @AlertMsg;
        FETCH NEXT FROM alert_cursor INTO @AlertMsg;
      END
      
      CLOSE alert_cursor;
      DEALLOCATE alert_cursor;
      PRINT '';
    END

    -- Mostrar todos los registros si Debug = 1
    IF @Debug = 1
    BEGIN
      PRINT '📊 TODOS LOS REGISTROS PROCESADOS';
      PRINT '------------------------------------------------------------';
      
      DECLARE @DebugMsg NVARCHAR(MAX);
      DECLARE debug_cursor CURSOR FOR
      SELECT 
        'Emp: ' + CAST(EmployeeID AS VARCHAR(10)) +
        ' | ' + CONVERT(VARCHAR(10), WorkDate, 120) +
        ' | Prog: ' + ISNULL(CONVERT(VARCHAR(5), EntryTime, 108), 'NULL') +
        ' | Real: ' + ISNULL(CONVERT(VARCHAR(8), FirstIn, 108), 'NULL') +
        ' | Tarde: ' + CAST(MinutesLate AS VARCHAR(10)) + ' min' +
        ' | Tardanza: ' + CAST(TardinessMin AS VARCHAR(10)) + ' min'
      FROM #CookedData
      ORDER BY WorkDate, EmployeeID;

      OPEN debug_cursor;
      FETCH NEXT FROM debug_cursor INTO @DebugMsg;
      
      WHILE @@FETCH_STATUS = 0
      BEGIN
        PRINT @DebugMsg;
        FETCH NEXT FROM debug_cursor INTO @DebugMsg;
      END
      
      CLOSE debug_cursor;
      DEALLOCATE debug_cursor;
      PRINT '';
    END
  END

  -------------------------------------------------------------------
  -- OPERACIÓN MERGE - CORREGIDA
  -------------------------------------------------------------------
  PRINT '============================================================';
  PRINT 'EJECUTANDO MERGE EN tbl_AttendanceCalculations';
  PRINT '============================================================';

  MERGE HR.tbl_AttendanceCalculations AS T
  USING (
    SELECT 
      EmployeeID, 
      WorkDate,
	  FirstIn,
      LastOut,
      CASE 
        WHEN HasLeave = 1 THEN 0 
        ELSE ISNULL(RawWorkedMin, 0) 
      END AS TotalWorkedMinutes,
      
      CASE 
        WHEN HasLeave = 1 OR IsHoliday = 1 OR IsWeekend = 1 THEN 0
        ELSE ISNULL(RequiredMin, 0)
      END AS ReqMin,
      MinutesLate,
      TardinessMin,
      IsHoliday,
      IsWeekend
    FROM #CookedData
  ) S ON T.EmployeeID = S.EmployeeID AND T.WorkDate = S.WorkDate

  WHEN MATCHED THEN 
    UPDATE SET
      T.TotalWorkedMinutes = S.TotalWorkedMinutes,
      T.RegularMinutes = CASE 
        WHEN S.TotalWorkedMinutes >= S.ReqMin THEN S.ReqMin 
        ELSE S.TotalWorkedMinutes 
      END,
      T.OvertimeMinutes = CASE 
        WHEN S.TotalWorkedMinutes - S.ReqMin >= @OTMin THEN S.TotalWorkedMinutes - S.ReqMin 
        ELSE 0 
      END,
      T.NightMinutes = T.NightMinutes,
      T.HolidayMinutes = CASE 
        WHEN S.IsHoliday = 1 OR S.IsWeekend = 1 THEN S.TotalWorkedMinutes 
        ELSE 0 
      END,
      T.TardinessMin = S.TardinessMin,
      T.RequiredMinutes = S.ReqMin,     -- ✅ Solo una asignación
      T.MinutesLate = S.MinutesLate,    -- ✅ Campo separado para MinutesLate
      T.Status = 'Approved'

  WHEN NOT MATCHED THEN 
    INSERT (
      EmployeeID,
      WorkDate,
	  FirstPunchIn,
	  LastPunchOut,
      TotalWorkedMinutes,
      RegularMinutes,
      OvertimeMinutes,
      NightMinutes,
      HolidayMinutes,
      TardinessMin,
      RequiredMinutes,
	  MinutesLate,
      Status
    )
    VALUES (
      S.EmployeeID,
      S.WorkDate,
	  S.FirstIn,
      S.LastOut,
      S.TotalWorkedMinutes,
      CASE 
        WHEN S.TotalWorkedMinutes >= S.ReqMin THEN S.ReqMin 
        ELSE S.TotalWorkedMinutes 
      END,
      CASE 
        WHEN S.TotalWorkedMinutes - S.ReqMin >= @OTMin THEN S.TotalWorkedMinutes - S.ReqMin 
        ELSE 0 
      END,
      0,
      CASE 
        WHEN S.IsHoliday = 1 OR S.IsWeekend = 1 THEN S.TotalWorkedMinutes 
        ELSE 0 
      END,
      S.TardinessMin,
      S.ReqMin,
	  S.MinutesLate,
      'Approved'
    );

  DECLARE @RowsAffected INT = @@ROWCOUNT;

  -- LOG FINAL
  PRINT '';
  PRINT '✅ MERGE COMPLETADO';
  PRINT 'Registros afectados: ' + CAST(@RowsAffected AS VARCHAR(10));
  PRINT '';
  PRINT '============================================================';
  PRINT 'CÁLCULO FINALIZADO EXITOSAMENTE';
  PRINT '============================================================';

  -- Limpiar tabla temporal
  DROP TABLE IF EXISTS #CookedData;

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








--Minutos nocturnos  --(Distribuye minutos trabajados en [NIGHT_START, NIGHT_END])

CREATE OR ALTER PROCEDURE HR.sp_Attendance_CalcNightMinutes
  @FromDate DATE, @ToDate DATE, @EmployeeID INT = NULL
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @NightStart TIME = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_START') AS TIME),
          @NightEnd   TIME = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_END')   AS TIME);

  ;WITH punches AS (
    SELECT EmployeeID, CAST(PunchTime AS DATE) D, MIN(PunchTime) FirstIn, MAX(PunchTime) LastOut
    FROM HR.tbl_AttendancePunches
    WHERE CAST(PunchTime AS DATE) BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR EmployeeID=@EmployeeID)
    GROUP BY EmployeeID, CAST(PunchTime AS DATE)
  )
  UPDATE ac
  SET NightMinutes = 
    DATEDIFF(MINUTE,
      CASE 
        WHEN CAST(p.FirstIn AS TIME) > @NightStart 
             THEN p.FirstIn
             ELSE CAST(CAST(p.D AS DATETIME2)+CAST(@NightStart AS DATETIME2) AS DATETIME2)
      END,
      CASE 
        WHEN CAST(p.LastOut AS TIME) < @NightEnd 
             THEN CAST(CAST(DATEADD(DAY,1,p.D) AS DATETIME2)+CAST(@NightEnd AS DATETIME2) AS DATETIME2)
             ELSE p.LastOut
      END
    )
  FROM HR.tbl_AttendanceCalculations ac
  JOIN punches p ON p.EmployeeID=ac.EmployeeID AND p.D=ac.WorkDate;
END
GO


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
