---------------------------------------------------------------------------------------------------
-- PROCEDURE: HR.sp_Attendance_CalculateRange
-- DESCRIPTION: Calcula y actualiza masivamente las asistencias en un rango de fechas
--              CON LOGS DE DEPURACIÓN para diagnosticar cálculo de tardanzas
-- AUTHOR: Sistema de Gestión de Asistencia
-- CREATED: 2024
-- UPDATED: 2024-10-27 - Agregados logs detallados
---------------------------------------------------------------------------------------------------
/*
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
             --ELSE DATEDIFF(MINUTE, Eff.InDT, Eff.OutDT) - @LunchMin
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
GO*/
----------------------------------------------------------------------------------------------------------------------

/* =======================================================================
   HR.sp_Attendance_CalculateRange
   Autor:        (tu equipo / DTH)
   Propósito:    Calcular métricas diarias de asistencia por empleado en
                 un rango de fechas, tomando el horario asignado efectivo
                 desde HR.tbl_EmployeeSchedules + HR.tbl_Schedules
                 y descansos de almuerzo desde LunchStart/LunchEnd.

   Métricas calculadas / actualizadas en HR.tbl_AttendanceCalculations:
     - TotalWorkedMinutes    : Trabajo neto del día (Out-In) - almuerzo.
     - RegularMinutes        : Min(RequiredMin, TotalWorkedMinutes) salvo feriados/licencias.
     - OvertimeMinutes       : Exceso sobre RequiredMin si supera umbral @OTMin.
     - NightMinutes          : Traslape con ventana nocturna parametrizable.
     - HolidayMinutes        : Trabajo en feriado/fin de semana.
     - TardinessMin          : Tardanza sancionable = MAX(0, (In-Entry) - Grace).
     - MinutesLate           : Retraso bruto (puede ser negativo si llegó antes).
     - RequiredMinutes       : Minutos requeridos según horario del día.
     - FirstPunchIn/LastPunchOut : Picadas extremas efectivas del día.
     - ScheduledWorkedMin    : Minutos trabajados DENTRO del horario (mañana/tarde).
     - OffScheduleMin        : Minutos trabajados FUERA del horario (neto ya sin almuerzo).

   Supuestos:
     - El horario del día proviene de la asignación vigente (ValidFrom/ValidTo).
     - LunchStart/LunchEnd definen el descanso (si HasLunchBreak=1).
     - Si ExitTime < EntryTime, el turno cruza medianoche.
     - Las picadas se buscan en [WorkDate 00:00, NightEndDT) para incluir
       madrugada del día siguiente (configurado por @NightEnd).
     - Si hay licencia o feriado/fin de semana, RequiredMinutes se fuerza a 0
       (según reglas de negocio actuales).

   Parámetros de configuración (HR.tbl_Parameters):
     - TARDINESS_GRACE_MIN
     - OT_MIN_THRESHOLD_MIN
     - NIGHT_START, NIGHT_END

   Extensiones futuras:
     - Manejar múltiples tramos de horario en un mismo día.
     - Comportamiento especial en teletrabajo o turnos rotativos.
     - Integración de justificaciones antes/después de este cálculo.

   Última actualización: (yyyy-mm-dd)
   ======================================================================= */
/* ============================================================================
   PROCEDIMIENTO: HR.sp_Attendance_CalculateRange
   DESCRIPCIÓN:   Calcula y registra asistencia, horarios regulares, horas extras, 
                  tardanzas y otros indicadores de tiempo laboral para empleados
   VERSIÓN:       2.2 - Con control preciso de almuerzo y suma de diferencias de picadas
   FECHA:         2025-11-17
   AUTOR:         Sistema de Gestión de Asistencia
   PARÁMETROS:
      @FromDate     - Fecha inicial del rango
      @ToDate       - Fecha final del rango  
      @EmployeeID   - ID específico de empleado (NULL para todos)
      @Debug        - 1 para mostrar información de depuración
      @PersistLog   - 1 para guardar log en tabla permanente
      @OnlySuspects - 1 para mostrar solo registros sospechosos en log
============================================================================ */


CREATE or ALTER PROCEDURE HR.sp_ProcessAttendanceEmployeeDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    /**************************************************************************
        0. PARÁMETROS GENERALES DEL SISTEMA (TOLERANCIA, OT, NOCTURNO)
        -----------------------------------------------------------------------
        Se obtienen desde HR.tbl_Parameters:
          - TARDINESS_GRACE_MIN      : minutos de gracia para atrasos
          - OT_MIN_THRESHOLD_MIN     : umbral mínimo para considerar OT dentro
          - NIGHT_START / NIGHT_END  : rango horario nocturno
    **************************************************************************/
    DECLARE 
        @GraceMinRaw   NVARCHAR(50),
        @OTMinRaw      NVARCHAR(50),
        @NightStartRaw NVARCHAR(50),
        @NightEndRaw   NVARCHAR(50),
		@ContractType  NVARCHAR(100);

    SELECT @GraceMinRaw = Pvalues
    FROM HR.tbl_Parameters
    WHERE name = 'TARDINESS_GRACE_MIN';

    SELECT @OTMinRaw = Pvalues
    FROM HR.tbl_Parameters
    WHERE name = 'OT_MIN_THRESHOLD_MIN';

    SELECT @NightStartRaw = Pvalues
    FROM HR.tbl_Parameters
    WHERE name = 'NIGHT_START';

    SELECT @NightEndRaw = Pvalues
    FROM HR.tbl_Parameters
    WHERE name = 'NIGHT_END';
	
	-- Obtener el tipo de contrato del empleado
	SELECT @ContractType = ved.ContractType
	FROM hr.vw_EmployeeDetails ved
	WHERE ved.EmployeeID = @EmployeeID;

    DECLARE 
        @GraceMin   INT  = TRY_CAST(@GraceMinRaw AS INT),
        @OTMin      INT  = TRY_CAST(@OTMinRaw AS INT),
        @NightStart TIME = TRY_CAST(@NightStartRaw AS TIME),
        @NightEnd   TIME = TRY_CAST(@NightEndRaw AS TIME);

    IF @GraceMin IS NULL SET @GraceMin = 0;
    IF @OTMin    IS NULL SET @OTMin    = 0;

    /**************************************************************************
        1. HORARIO ASIGNADO PARA EL EMPLEADO EN ESA FECHA
        -----------------------------------------------------------------------
        Usamos HR.tbl_EmployeeSchedules + HR.tbl_Schedules
        Tomamos el horario vigente (último por ValidFrom) que cubra @WorkDate.
    **************************************************************************/
    DECLARE @ScheduleID INT,
            @EntryTime   TIME,
            @ExitTime    TIME,
            @HasLunch    BIT,
            @LunchStartT TIME,
            @LunchEndT   TIME;

    SELECT TOP 1
        @ScheduleID  = es.ScheduleID,
        @EntryTime   = s.EntryTime,
        @ExitTime    = s.ExitTime,
        @HasLunch    = s.HasLunchBreak,
        @LunchStartT = s.LunchStart,
        @LunchEndT   = s.LunchEnd
    FROM HR.tbl_EmployeeSchedules es
    INNER JOIN HR.tbl_Schedules s ON s.ScheduleID = es.ScheduleID
    WHERE es.EmployeeID = @EmployeeID
      AND es.ValidFrom <= @WorkDate
      AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
    ORDER BY es.ValidFrom DESC;

    -- Si no tiene horario asignado ese día, no calculamos nada
    IF @ScheduleID IS NULL
        RETURN;

    /**************************************************************************
        2. INICIO / FIN DE TURNO EN DATETIME2 (SOPORTA CRUCE DE MEDIANOCHE)
    **************************************************************************/
    DECLARE @BaseDate   DATETIME2 = CAST(@WorkDate AS DATETIME2);
    DECLARE @ShiftStart DATETIME2,
            @ShiftEnd   DATETIME2;

    -- Inicio de turno en la fecha base
    SET @ShiftStart = DATEADD(SECOND,
                              DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @EntryTime),
                              @BaseDate);

    -- Si la salida es <= entrada, asumimos que cruza medianoche
    IF (@ExitTime <= @EntryTime)
    BEGIN
        SET @ShiftEnd = DATEADD(DAY, 1, @BaseDate);
        SET @ShiftEnd = DATEADD(SECOND,
                                DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime),
                                @ShiftEnd);
    END
    ELSE
    BEGIN
        SET @ShiftEnd = DATEADD(SECOND,
                                DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime),
                                @BaseDate);
    END;

    /**************************************************************************
        3. VENTANA DE ALMUERZO EN DATETIME2 (SI EL HORARIO TIENE ALMUERZO)
        -----------------------------------------------------------------------
        Si @HasLunch = 1, definimos LunchStart/LunchEnd.
        También soporta el caso raro de almuerzo que cruza medianoche.
    **************************************************************************/
    DECLARE @LunchStart DATETIME2 = NULL,
            @LunchEnd   DATETIME2 = NULL;

    IF (@HasLunch = 1 AND @LunchStartT IS NOT NULL AND @LunchEndT IS NOT NULL)
    BEGIN
        SET @LunchStart = DATEADD(SECOND,
                                  DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchStartT),
                                  @BaseDate);

        SET @LunchEnd = DATEADD(SECOND,
                                DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchEndT),
                                @BaseDate);

        IF (@LunchEndT <= @LunchStartT)
        BEGIN
            -- Almuerzo que cruza medianoche (poco común pero soportado)
            SET @LunchEnd = DATEADD(DAY, 1, @LunchEnd);
        END;
    END;

    /**************************************************************************
        4. FRANJA NOCTURNA EN DATETIME2 (SEGÚN PARÁMETROS)
        -----------------------------------------------------------------------
        - Si NIGHT_END > NIGHT_START  → mismo día
        - Si NIGHT_END <= NIGHT_START → cruce de medianoche (ej. 22–06)
    **************************************************************************/
    DECLARE @NightStartDT DATETIME2 = NULL,
            @NightEndDT   DATETIME2 = NULL;

    IF (@NightStart IS NOT NULL AND @NightEnd IS NOT NULL)
    BEGIN
        SET @NightStartDT = DATEADD(SECOND,
                                    DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightStart),
                                    @BaseDate);

        IF (@NightEnd > @NightStart)
        BEGIN
            SET @NightEndDT = DATEADD(SECOND,
                                      DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd),
                                      @BaseDate);
        END
        ELSE
        BEGIN
            -- Rango nocturno que se extiende al día siguiente
            SET @NightEndDT = DATEADD(SECOND,
                                      DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd),
                                      DATEADD(DAY, 1, @BaseDate));
        END;
    END;

    /**************************************************************************
        5. MARCACIONES DEL DÍA
        -----------------------------------------------------------------------
        Traemos las picadas del empleado en una ventana desde 4h antes del
        inicio de turno hasta 4h después del final de turno. Esto permite
        capturar bien cruces de medianoche.
    **************************************************************************/
    DECLARE @WindowStart DATETIME2 = DATEADD(HOUR, -4, @ShiftStart);
    DECLARE @WindowEnd   DATETIME2 = DATEADD(HOUR,  4, @ShiftEnd);

    ;WITH PunchesOrdered AS
    (
        SELECT
            ap.PunchID,
            ap.EmployeeID,
            ap.PunchTime,
            ap.PunchType,
            ROW_NUMBER() OVER (ORDER BY ap.PunchTime) AS rn
        FROM HR.tbl_AttendancePunches ap
        WHERE ap.EmployeeID = @EmployeeID
          AND ap.PunchTime >= @WindowStart
          AND ap.PunchTime <= @WindowEnd
    )
    SELECT * INTO #Punches FROM PunchesOrdered;

    -- Si no hay marcaciones, no calculamos nada
    IF NOT EXISTS (SELECT 1 FROM #Punches)
    BEGIN
        DROP TABLE #Punches;
        RETURN;
    END;

    /**************************************************************************
        6. HEURÍSTICA CASO 5 (3 PICADAS 08–14–17 SIN UNA DE ALMUERZO)
        -----------------------------------------------------------------------
        Si:
          - hay exactamente 3 marcaciones
          - el horario tiene almuerzo
          - patrón: 1ª antes de LunchStart y 2ª después de LunchEnd
        Entonces:
          - se asume que la primera picada pertenece a otra jornada
          - se elimina la primera para que cuente solo la tarde.
    **************************************************************************/
    DECLARE @PunchCount INT;
    SELECT @PunchCount = COUNT(*) FROM #Punches;

    IF @HasLunch = 1 
       AND @PunchCount = 3 
       AND @LunchStart IS NOT NULL 
       AND @LunchEnd IS NOT NULL
    BEGIN
        DECLARE @t1 DATETIME2, @t2 DATETIME2, @t3 DATETIME2;
        SELECT
            @t1 = MIN(CASE WHEN rn = 1 THEN PunchTime END),
            @t2 = MIN(CASE WHEN rn = 2 THEN PunchTime END),
            @t3 = MIN(CASE WHEN rn = 3 THEN PunchTime END)
        FROM #Punches;

        IF (@t1 < @LunchStart AND @t2 >= @LunchEnd AND @t3 > @t2)
        BEGIN
            DELETE FROM #Punches WHERE rn = 1;
        END;
    END;

    /**************************************************************************
        7. SEGMENTOS TRABAJADOS (IN → OUT)
        -----------------------------------------------------------------------
        Se construyen pares StartTime/EndTime válidos:
          - Solo cuando hay un IN seguido de un OUT
          - OUT sin IN previo → no se toma
          - IN sin OUT posterior → no se toma
    **************************************************************************/
    ;WITH P AS
    (
        SELECT
            PunchTime,
            PunchType,
            rn,
            LAG(PunchTime) OVER (ORDER BY PunchTime) AS PrevTime,
            LAG(PunchType) OVER (ORDER BY PunchTime) AS PrevType
        FROM #Punches
    )
    SELECT
        PrevTime AS StartTime,
        PunchTime AS EndTime
    INTO #Segments
    FROM P
    WHERE PrevType = 'In'
      AND PunchType = 'Out'
      AND PunchTime > PrevTime;

    -- Sin segmentos válidos → no hay nada que calcular
    IF NOT EXISTS (SELECT 1 FROM #Segments)
    BEGIN
        DROP TABLE #Segments;
        DROP TABLE #Punches;
        RETURN;
    END;

    /**************************************************************************
        8. PRIMERA ENTRADA Y ÚLTIMA SALIDA DEL DÍA (SEGÚN SEGMENTOS)
    **************************************************************************/
    DECLARE @FirstIn DATETIME2, @LastOut DATETIME2;

    SELECT 
        @FirstIn = MIN(StartTime),
        @LastOut = MAX(EndTime)
    FROM #Segments;

    /**************************************************************************
        9. CÁLCULO DE MINUTOS:
           - TotalWorkedSegments  : minutos trabajados en todos los segmentos
           - InsideShiftMinutes   : intersección con el turno completo (08–17)
           - InsideBlock1         : intersección con bloque 1 (antes de almuerzo)
           - InsideBlock2         : intersección con bloque 2 (después de almuerzo)
           - NightMinutes         : intersección con franja nocturna
    **************************************************************************/
    ;WITH SegCalc AS
    (
        SELECT
            StartTime,
            EndTime,

            -- 9.1 Minutos del segmento completo
            CAST(DATEDIFF(MINUTE, StartTime, EndTime) AS FLOAT) AS SegmentMinutes,

            -- 9.2 Minutos dentro del turno total (08–17), sin considerar almuerzo
            CASE 
                WHEN EndTime <= @ShiftStart OR StartTime >= @ShiftEnd THEN 0
                ELSE CAST(
                    DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @ShiftEnd   THEN EndTime   ELSE @ShiftEnd   END
                    ) AS FLOAT
                )
            END AS InsideShiftMinutes,

            -- 9.3 Minutos dentro del bloque de la mañana (ej. 08–13)
            CASE 
                WHEN @HasLunch = 0 OR @LunchStart IS NULL THEN 0
                WHEN EndTime <= @ShiftStart OR StartTime >= @LunchStart THEN 0
                ELSE CAST(
                    DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @LunchStart THEN EndTime   ELSE @LunchStart END
                    ) AS FLOAT
                )
            END AS InsideBlock1,

            -- 9.4 Minutos dentro del bloque de la tarde (ej. 14–17)
            CASE 
                WHEN @HasLunch = 0 OR @LunchEnd IS NULL THEN 0
                WHEN EndTime <= @LunchEnd OR StartTime >= @ShiftEnd THEN 0
                ELSE CAST(
                    DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @LunchEnd THEN StartTime ELSE @LunchEnd END,
                        CASE WHEN EndTime   < @ShiftEnd THEN EndTime   ELSE @ShiftEnd END
                    ) AS FLOAT
                )
            END AS InsideBlock2,

            -- 9.5 Minutos nocturnos (según NIGHT_START / NIGHT_END)
            CASE 
                WHEN @NightStartDT IS NULL OR @NightEndDT IS NULL THEN 0
                WHEN EndTime <= @NightStartDT OR StartTime >= @NightEndDT THEN 0
                ELSE CAST(
                    DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @NightStartDT THEN StartTime ELSE @NightStartDT END,
                        CASE WHEN EndTime   < @NightEndDT   THEN EndTime   ELSE @NightEndDT   END
                    ) AS FLOAT
                )
            END AS NightMinutes
        FROM #Segments
    )
    SELECT
        SUM(SegmentMinutes)              AS TotalWorkedSegments,
        SUM(InsideShiftMinutes)          AS TotalInsideShift,
        SUM(InsideBlock1 + InsideBlock2) AS TotalInsideWorkBands,
        SUM(NightMinutes)                AS TotalNightMinutes
    INTO #Totals
    FROM SegCalc;

    DECLARE 
        @TotalWorkedSegments FLOAT,
        @InsideShift         FLOAT,
        @InsideWorkBands     FLOAT,
        @NightMinutes        INT;

    SELECT
        @TotalWorkedSegments = TotalWorkedSegments,
        @InsideShift         = TotalInsideShift,
        @InsideWorkBands     = TotalInsideWorkBands,
        @NightMinutes        = CAST(TotalNightMinutes AS INT)
    FROM #Totals;

    DROP TABLE #Totals;

    /**************************************************************************
        10. MINUTOS DENTRO Y FUERA DE HORARIO
        -----------------------------------------------------------------------
        - Con almuerzo → trabajo normal solo en bloques 08–13 y 14–17
        - Sin almuerzo → trabajo normal en todo el turno 08–17
    **************************************************************************/
    DECLARE @InsideMinutes  FLOAT,
            @OutsideMinutes FLOAT;

    IF (@HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL)
    BEGIN
        SET @InsideMinutes = @InsideWorkBands;
    END
    ELSE
    BEGIN
        SET @InsideMinutes = @InsideShift;
    END;

    SET @OutsideMinutes = @TotalWorkedSegments - @InsideMinutes;
    IF (@OutsideMinutes < 0) SET @OutsideMinutes = 0;

    /**************************************************************************
        11. MINUTOS TEÓRICOS (REQUERIDOS) Y AUSENCIAS
        -----------------------------------------------------------------------
        - RequiredMinutes = duración del turno - almuerzo (si aplica)
        - AbsentMinutes   = RequiredMinutes - InsideMinutes (si es positivo)
    **************************************************************************/
    DECLARE @TheoreticalShiftMinutes INT =
        DATEDIFF(MINUTE, @ShiftStart, @ShiftEnd);

    DECLARE @TheoreticalLunchMinutes INT = 0;
    IF (@HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL)
        SET @TheoreticalLunchMinutes = DATEDIFF(MINUTE, @LunchStart, @LunchEnd);

    DECLARE @RequiredMinutes INT;
    IF (@HasLunch = 1)
        SET @RequiredMinutes = @TheoreticalShiftMinutes - @TheoreticalLunchMinutes;
    ELSE
        SET @RequiredMinutes = @TheoreticalShiftMinutes;

    IF (@RequiredMinutes < 0) SET @RequiredMinutes = 0;

    DECLARE @AbsentMinutes INT = 0;
    IF (@InsideMinutes < @RequiredMinutes)
        SET @AbsentMinutes = @RequiredMinutes - CAST(@InsideMinutes AS INT);

    /**************************************************************************
        12. TARDANZA (MinutesLate / TardinessMin) CON GRACIA
        -----------------------------------------------------------------------
        - Se toma la primera entrada que toca el turno
        - Si llega después de la hora de entrada → tardanza
        - Se aplica @GraceMin (si tardanza <= @GraceMin → se pone en 0)
    **************************************************************************/
    DECLARE @FirstInInside DATETIME2 = NULL;

    ;WITH FirstInInsideCTE AS
    (
        SELECT TOP 1
            CASE 
                WHEN s.StartTime <= @ShiftStart AND s.EndTime > @ShiftStart THEN @ShiftStart
                ELSE s.StartTime
            END AS FirstInInside
        FROM #Segments s
        WHERE s.EndTime > @ShiftStart
        ORDER BY s.StartTime
    )
    SELECT @FirstInInside = FirstInInside FROM FirstInInsideCTE;

    DECLARE @TardinessMin INT = 0;

    IF (@FirstInInside IS NOT NULL AND @FirstInInside > @ShiftStart)
        SET @TardinessMin = DATEDIFF(MINUTE, @ShiftStart, @FirstInInside);

    -- Aplicar minutos de gracia para atrasos pequeños
    IF (@TardinessMin <= @GraceMin)
        SET @TardinessMin = 0;

    /**************************************************************************
        13. OVERTIME, REGULAR Y TOTALES
        -----------------------------------------------------------------------
        - OvertimeWithinSchedule = exceso de minutos dentro del horario
        - Si ese exceso < @OTMin → se ignora (se suma a RegularMinutes)
        - Todo lo fuera del horario (@OutsideMinutes) se considera OT
    **************************************************************************/
    DECLARE @OvertimeWithinSchedule FLOAT = 0,
            @OvertimeMinutes        INT   = 0,
            @RegularMinutes         INT   = 0,
            @OffScheduleMin         INT   = CAST(@OutsideMinutes AS INT),
            @TotalWorkedMinutes     INT   = CAST(@InsideMinutes + @OutsideMinutes AS INT);

    IF (@InsideMinutes > @RequiredMinutes)
        SET @OvertimeWithinSchedule = @InsideMinutes - @RequiredMinutes;

    -- Se aplica umbral mínimo de OT dentro del horario
    IF (@OvertimeWithinSchedule < @OTMin)
        SET @OvertimeWithinSchedule = 0;

    SET @OvertimeMinutes = CAST(@OvertimeWithinSchedule + @OutsideMinutes AS INT);
    SET @RegularMinutes  = CAST(@InsideMinutes - @OvertimeWithinSchedule AS INT);
    IF (@RegularMinutes < 0) SET @RegularMinutes = 0;

    /**************************************************************************
        14. SUBSIDIO DE ALIMENTACIÓN
        -----------------------------------------------------------------------
        Ejemplo simple: 1 si cumple los minutos requeridos, 0 si no.
        (Luego lo puedes sofisticar si RRHH cambia reglas)
    **************************************************************************/
    --DECLARE @FoodSubsidy INT = CASE WHEN @InsideMinutes >= @RequiredMinutes THEN 1 ELSE 0 END;
	--DECLARE @FoodSubsidy INT = CASE WHEN (@ContractType = N'Código Trabajo' AND @InsideMinutes >= @RequiredMinutes) THEN 1 ELSE 0 END;
	-- Calcular FoodSubsidy SOLO si es "Código Trabajo"
	DECLARE @FoodSubsidy INT;

	--IF (@ContractType = N'Código Trabajo' AND (@InsideMinutes + @TardinessMin) >= @RequiredMinutes)
	IF (@ContractType = N'Código Trabajo' AND (@RegularMinutes + @TardinessMin) >= @RequiredMinutes)
		SET @FoodSubsidy = 1;
	ELSE
		SET @FoodSubsidy = 0;


    /**************************************************************************
        15. FERIADOS Y FIN DE SEMANA (USANDO HR.vw_Calendar)
        -----------------------------------------------------------------------
        Regla que me diste:
        - Si el día es feriado o fin de semana  → HolidayMinutes = TotalWorkedMinutes
        - Si no                               → HolidayMinutes = 0
        No usamos un campo separado para WeekendMinutes.
    **************************************************************************/
    DECLARE @IsHoliday BIT = 0,
            @IsWeekend BIT = 0;

    SELECT 
        @IsHoliday = IsHoliday,
        @IsWeekend = IsWeekend
    FROM HR.vw_Calendar
    WHERE D = @WorkDate;

    DECLARE @HolidayMinutes INT = 
        CASE 
            WHEN @IsHoliday = 1 OR @IsWeekend = 1 
                THEN @TotalWorkedMinutes 
            ELSE 0 
        END;

    /**************************************************************************
        16. ESCRITURA EN HR.tbl_AttendanceCalculations
        -----------------------------------------------------------------------
        - Si ya existe registro para (EmployeeID, WorkDate) → UPDATE
        - Si no existe                                      → INSERT
    **************************************************************************/
    IF EXISTS (SELECT 1 FROM HR.tbl_AttendanceCalculations
               WHERE EmployeeID = @EmployeeID AND WorkDate = @WorkDate)
    BEGIN
        UPDATE HR.tbl_AttendanceCalculations
        SET
            FirstPunchIn       = @FirstIn,
            LastPunchOut       = @LastOut,
            TotalWorkedMinutes = @TotalWorkedMinutes,
            RegularMinutes     = @RegularMinutes,
            OvertimeMinutes    = @OvertimeMinutes,
            NightMinutes       = @NightMinutes,
            HolidayMinutes     = @HolidayMinutes,
            RequiredMinutes    = @RequiredMinutes,
            TardinessMin       = @TardinessMin,
            AbsentMinutes      = @AbsentMinutes,
            MinutesLate        = @TardinessMin,
            ScheduledWorkedMin = CAST(@InsideMinutes AS INT),
            OffScheduleMin     = @OffScheduleMin,
            JustificationApply = 0,
            FoodSubsidy        = @FoodSubsidy,
            JustificationMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;
    END
    ELSE
    BEGIN
        INSERT INTO HR.tbl_AttendanceCalculations
        (
            EmployeeID,
            WorkDate,
            FirstPunchIn,
            LastPunchOut,
            TotalWorkedMinutes,
            RegularMinutes,
            OvertimeMinutes,
            NightMinutes,
            HolidayMinutes,
            RequiredMinutes,
            TardinessMin,
            AbsentMinutes,
            MinutesLate,
            ScheduledWorkedMin,
            OffScheduleMin,
            JustificationApply,
            FoodSubsidy,
            JustificationMinutes
        )
        VALUES
        (
            @EmployeeID,
            @WorkDate,
            @FirstIn,
            @LastOut,
            @TotalWorkedMinutes,
            @RegularMinutes,
            @OvertimeMinutes,
            @NightMinutes,
            @HolidayMinutes,
            @RequiredMinutes,
            @TardinessMin,
            @AbsentMinutes,
            @TardinessMin,
            CAST(@InsideMinutes AS INT),
            @OffScheduleMin,
            0,
            @FoodSubsidy,
            0
        );
    END;

    /**************************************************************************
        17. LIMPIEZA DE TABLAS TEMPORALES
    **************************************************************************/
    DROP TABLE IF EXISTS #Segments;
    DROP TABLE IF EXISTS #Punches;
END;
GO

/**************************************************************************
        INICIO - Procedimiento para procesar las Horas Extras 
**************************************************************************/
CREATE OR ALTER PROCEDURE HR.sp_ProcessTimePlanningForEmployeeDay
(
    @EmployeeID INT,
    @WorkDate   DATE,
    @Debug      BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    --------------------------------------------------------------------
    -- 0) LOG INICIAL
    --------------------------------------------------------------------
    IF @Debug = 1 
        PRINT '============================================================';
    IF @Debug = 1 
        PRINT 'sp_ApplyTimePlanningForEmployeeDay INICIO - EmpID=' 
              + CAST(@EmployeeID AS VARCHAR(10)) 
              + ' Fecha=' + CONVERT(VARCHAR(10), @WorkDate, 120);

    --------------------------------------------------------------------
    -- 1) Verificar que exista cálculo de asistencia para ese día
    --------------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1
        FROM HR.tbl_AttendanceCalculations ac
        WHERE ac.EmployeeID = @EmployeeID
          AND ac.WorkDate   = @WorkDate
    )
    BEGIN
        IF @Debug = 1 
            PRINT 'No existe registro en HR.tbl_AttendanceCalculations para este empleado y fecha. Se aborta.';
        RETURN;
    END

    --------------------------------------------------------------------
    -- 2) Obtener horario normal (Schedule) vigente para ese día
    --------------------------------------------------------------------
    DECLARE @EntryTime TIME = NULL,
            @ExitTime  TIME = NULL;

    ;WITH EmpSched AS (
        SELECT 
            es.EmployeeID,
            es.ScheduleID,
            s.EntryTime,
            s.ExitTime,
            ROW_NUMBER() OVER (ORDER BY es.ValidFrom DESC) AS rn
        FROM HR.tbl_EmployeeSchedules es
        JOIN HR.tbl_Schedules s 
             ON s.ScheduleID = es.ScheduleID
        WHERE es.EmployeeID = @EmployeeID
          AND es.ValidFrom <= @WorkDate
          AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
    )
    SELECT 
        @EntryTime = EntryTime,
        @ExitTime  = ExitTime
    FROM EmpSched
    WHERE rn = 1;

    IF @EntryTime IS NULL OR @ExitTime IS NULL
    BEGIN
        -- No se encontró un horario claro para este día
        IF @Debug = 1 
            PRINT 'No se encontró horario (Schedule) para el empleado en la fecha indicada. No se aplicará planificación.';
        RETURN;
    END

    IF @Debug = 1
        PRINT 'Horario normal detectado: ' 
              + CONVERT(VARCHAR(8), @EntryTime, 108) 
              + ' - ' 
              + CONVERT(VARCHAR(8), @ExitTime, 108);

    --------------------------------------------------------------------
    -- 3) Obtener planificación vigente para ese empleado/día
    --    PlanType IN ('Overtime','Recovery') y estado permitido.
    --------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#Plans') IS NOT NULL DROP TABLE #Plans;

    SELECT 
        p.PlanID,
        p.PlanType,              -- 'Overtime' o 'Recovery'
        p.StartDate,
        p.EndDate,
        p.StartTime,
        p.EndTime,
        p.OvertimeType,
        p.Factor,
        pe.PlanEmployeeID
    INTO #Plans
    FROM HR.tbl_TimePlanning p
    JOIN HR.tbl_TimePlanningEmployees pe
         ON pe.PlanID     = p.PlanID
        AND pe.EmployeeID = @EmployeeID
    -- Opcional: filtrar por estado del plan si tienes ref_Types
    LEFT JOIN HR.ref_Types st
         ON st.TypeID   = p.PlanStatusTypeID
        AND st.Category = 'PLAN_STATUS'
    WHERE @WorkDate BETWEEN p.StartDate AND p.EndDate
      AND p.PlanType IN ('Overtime','Recovery')
      AND (st.TypeID IS NULL OR st.Name IN ('Aprobado','En Progreso','Borrador'));

    IF NOT EXISTS(SELECT 1 FROM #Plans)
    BEGIN
        IF @Debug = 1 
            PRINT 'No hay planificación Overtime/Recovery aplicable para este empleado en la fecha.';
        RETURN;
    END

    IF @Debug = 1 
        PRINT 'Planes encontrados (sin filtrar por horario): ' 
              + CAST((SELECT COUNT(*) FROM #Plans) AS VARCHAR(12));

    --------------------------------------------------------------------
    -- 4) Filtrar planes que estén completamente fuera del horario normal
    --    Regla: Plan válido si:
    --      EndTime <= EntryTime  OR  StartTime >= ExitTime
    --    Cualquier solapamiento con el horario normal → se descarta.
    --------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ValidPlans') IS NOT NULL DROP TABLE #ValidPlans;

    SELECT 
        p.*,
        CASE 
            WHEN p.EndTime   <= @EntryTime 
                 OR p.StartTime >= @ExitTime 
                 THEN 1 
            ELSE 0 
        END AS IsOutsideSchedule
    INTO #ValidPlans
    FROM #Plans p;

    DELETE FROM #ValidPlans WHERE IsOutsideSchedule = 0;

    IF NOT EXISTS (SELECT 1 FROM #ValidPlans)
    BEGIN
        IF @Debug = 1 
            PRINT 'Todos los planes se solapan con el horario normal. No se aplica ninguno.';
        RETURN;
    END

    IF @Debug = 1 
        PRINT 'Planes válidos (fuera de horario normal): ' 
              + CAST((SELECT COUNT(*) FROM #ValidPlans) AS VARCHAR(12));

    --------------------------------------------------------------------
    -- 5) Obtener ventana de trabajo real (picadas) desde AttendanceCalculations
    --------------------------------------------------------------------
    DECLARE @FirstPunchIn DATETIME2,
            @LastPunchOut DATETIME2;

    SELECT 
        @FirstPunchIn = ac.FirstPunchIn,
        @LastPunchOut = ac.LastPunchOut
    FROM HR.tbl_AttendanceCalculations ac
    WHERE ac.EmployeeID = @EmployeeID
      AND ac.WorkDate   = @WorkDate;

    IF @FirstPunchIn IS NULL OR @LastPunchOut IS NULL
    BEGIN
        IF @Debug = 1 
            PRINT 'No hay FirstPunchIn o LastPunchOut para este día. No se consideran minutos ejecutados en planificación.';
        -- A pesar de no haber picadas, podríamos querer poner en 0 los campos:
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes   = 0,
            recoveredMinutes  = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END

    IF @Debug = 1
        PRINT 'Ventana trabajada según picadas: '
              + CONVERT(VARCHAR(19), @FirstPunchIn, 120)
              + ' - '
              + CONVERT(VARCHAR(19), @LastPunchOut, 120);

    --------------------------------------------------------------------
    -- 6) Calcular minutos de solapamiento (ejecución real) por plan
    --------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#ExecPlans') IS NOT NULL DROP TABLE #ExecPlans;

    ;WITH PlanWindows AS (
        SELECT
            vp.PlanID,
            vp.PlanEmployeeID,
            vp.PlanType,
            vp.OvertimeType,
            vp.Factor,
            -- Construimos la fecha/hora del plan para el @WorkDate
            PlanStartDT = DATEADD(MINUTE, 
                                  DATEDIFF(MINUTE, CAST('00:00:00' AS TIME), vp.StartTime),
                                  CAST(@WorkDate AS DATETIME2)),
            PlanEndDT   = DATEADD(MINUTE, 
                                  DATEDIFF(MINUTE, CAST('00:00:00' AS TIME), vp.EndTime),
                                  CAST(@WorkDate AS DATETIME2))
        FROM #ValidPlans vp
    ),
    Overlaps AS (
        SELECT
            w.PlanID,
            w.PlanEmployeeID,
            w.PlanType,
            w.OvertimeType,
            w.Factor,
            OverlapStart = CASE 
                             WHEN @FirstPunchIn > w.PlanStartDT THEN @FirstPunchIn 
                             ELSE w.PlanStartDT 
                           END,
            OverlapEnd   = CASE 
                             WHEN @LastPunchOut < w.PlanEndDT THEN @LastPunchOut 
                             ELSE w.PlanEndDT 
                           END
        FROM PlanWindows w
    )
    SELECT
        o.PlanID,
        o.PlanEmployeeID,
        o.PlanType,
        o.OvertimeType,
        o.Factor,
        ExecutedMinutes =
            CASE 
                WHEN o.OverlapEnd > o.OverlapStart 
                     THEN DATEDIFF(MINUTE, o.OverlapStart, o.OverlapEnd)
                ELSE 0
            END
    INTO #ExecPlans
    FROM Overlaps o;

    -- Eliminamos planes con 0 minutos ejecutados
    DELETE FROM #ExecPlans
    WHERE ExecutedMinutes <= 0;

    IF NOT EXISTS (SELECT 1 FROM #ExecPlans)
    BEGIN
        IF @Debug = 1 
            PRINT 'No hubo minutos realmente trabajados dentro de las ventanas de los planes.';
        
        -- Dejamos minutos de Overtime/Recovery en 0 para este día
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            recoveredMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END

    IF @Debug = 1
        PRINT 'Planes con minutos ejecutados: '
              + CAST((SELECT COUNT(*) FROM #ExecPlans) AS VARCHAR(12));

    --------------------------------------------------------------------
    -- 7) Totalizar minutos por tipo (Overtime / Recovery)
    --------------------------------------------------------------------
    DECLARE @TotalOvertimeMin INT = 0,
            @TotalRecoveryMin INT = 0;

    SELECT 
        @TotalOvertimeMin = ISNULL(SUM(CASE WHEN PlanType = 'Overtime' THEN ExecutedMinutes ELSE 0 END), 0),
        @TotalRecoveryMin = ISNULL(SUM(CASE WHEN PlanType = 'Recovery' THEN ExecutedMinutes ELSE 0 END), 0)
    FROM #ExecPlans;

    IF @Debug = 1
    BEGIN
        PRINT 'Minutos ejecutados Overtime:  ' + CAST(@TotalOvertimeMin AS VARCHAR(12));
        PRINT 'Minutos ejecutados Recovery: ' + CAST(@TotalRecoveryMin AS VARCHAR(12));
    END

    --------------------------------------------------------------------
    -- 8) Actualizar tbl_AttendanceCalculations con minutos verificados
    --    IMPORTANTE: aquí se establece el valor en función de la planificación
    --    y las picadas. El procedimiento es idempotente.
    --------------------------------------------------------------------
    UPDATE HR.tbl_AttendanceCalculations
    SET OvertimeMinutes  = @TotalOvertimeMin,
        recoveredMinutes = @TotalRecoveryMin
    WHERE EmployeeID = @EmployeeID
      AND WorkDate   = @WorkDate;

    IF @Debug = 1
        PRINT 'Actualizados OvertimeMinutes y recoveredMinutes en HR.tbl_AttendanceCalculations.';

    --------------------------------------------------------------------
    -- 9) Actualizar saldo de recuperación en HR.tbl_TimeBalances
    --    RecoveryPendingMin = max(SaldoActual - TotalRecoveryMin, 0)
    --------------------------------------------------------------------
    IF @TotalRecoveryMin > 0
    BEGIN
        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID)
        BEGIN
            UPDATE HR.tbl_TimeBalances
            SET RecoveryPendingMin = CASE 
                                        WHEN RecoveryPendingMin - @TotalRecoveryMin < 0 
                                             THEN 0 
                                        ELSE RecoveryPendingMin - @TotalRecoveryMin 
                                     END,
                LastUpdated        = SYSDATETIME()
            WHERE EmployeeID = @EmployeeID;

            IF @Debug = 1
                PRINT 'Actualizado HR.tbl_TimeBalances.RecoveryPendingMin para el empleado.';
        END
        ELSE
        BEGIN
            IF @Debug = 1
                PRINT 'No existe fila en HR.tbl_TimeBalances para el empleado. No se descuenta RecoveryPendingMin.';
        END
    END

    --------------------------------------------------------------------
    -- 10) Registrar ejecución en HR.tbl_TimePlanningExecution
    --     Por cada PlanEmployeeID y WorkDate.
    --     Para Overtime se usa OvertimeMinutes; para Recovery se guarda en RegularMinutes.
    --------------------------------------------------------------------
    MERGE HR.tbl_TimePlanningExecution AS T
    USING (
        SELECT 
            ep.PlanEmployeeID,
            @WorkDate AS WorkDate,
            ep.ExecutedMinutes,
            ep.PlanType
        FROM #ExecPlans ep
    ) AS S
    ON T.PlanEmployeeID = S.PlanEmployeeID
       AND T.WorkDate   = S.WorkDate
    WHEN MATCHED THEN
        UPDATE SET
            T.TotalMinutes    = S.ExecutedMinutes,
            T.OvertimeMinutes = CASE WHEN S.PlanType = 'Overtime' THEN S.ExecutedMinutes ELSE 0 END,
            T.RegularMinutes  = CASE WHEN S.PlanType = 'Recovery' THEN S.ExecutedMinutes ELSE T.RegularMinutes END
    WHEN NOT MATCHED THEN
        INSERT (PlanEmployeeID, WorkDate, StartTime, EndTime, TotalMinutes, RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, CreatedAt)
        VALUES (
            S.PlanEmployeeID,
            S.WorkDate,
            NULL,                           -- StartTime real no se detalla aquí
            NULL,                           -- EndTime real no se detalla aquí
            S.ExecutedMinutes,
            CASE WHEN S.PlanType = 'Recovery' THEN S.ExecutedMinutes ELSE 0 END,
            CASE WHEN S.PlanType = 'Overtime' THEN S.ExecutedMinutes ELSE 0 END,
            0,
            0,
            SYSDATETIME()
        );

    IF @Debug = 1
        PRINT 'Actualizada/insertada ejecución en HR.tbl_TimePlanningExecution.';

    --------------------------------------------------------------------
    -- 11) Consolidar horas extra en HR.tbl_Overtime (solo PlanType = 'Overtime')
    --------------------------------------------------------------------
    IF @TotalOvertimeMin > 0
    BEGIN
        DECLARE @HoursOT DECIMAL(5,2) = CAST(@TotalOvertimeMin AS DECIMAL(10,2)) / 60.0;

        MERGE HR.tbl_Overtime AS T
        USING (
            SELECT 
                @EmployeeID AS EmployeeID,
                @WorkDate   AS WorkDate,
                @HoursOT    AS Hours
        ) AS S
        ON T.EmployeeID = S.EmployeeID
           AND T.WorkDate = S.WorkDate
        WHEN MATCHED THEN
            UPDATE SET
                -- Si ya está APPROVED o PAID, no se pisa
                T.Hours       = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Hours       ELSE S.Hours END,
                T.ActualHours = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.ActualHours ELSE S.Hours END,
                T.Status      = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Status      ELSE 'EXECUTED' END
        WHEN NOT MATCHED THEN
            INSERT (EmployeeID, WorkDate, OvertimeType, Hours, Status, Factor, ActualHours, PaymentAmount, CreatedAt)
            VALUES (S.EmployeeID, S.WorkDate, 'Ordinaria', S.Hours, 'EXECUTED', 1.0, S.Hours, 0, SYSDATETIME());

        IF @Debug = 1
            PRINT 'Actualizada/insertada consolidación en HR.tbl_Overtime.';
    END
    ELSE
    BEGIN
        IF @Debug = 1
            PRINT 'No se registran horas en HR.tbl_Overtime porque no hubo minutos de Overtime ejecutados.';
    END

    --------------------------------------------------------------------
    -- 12) FIN
    --------------------------------------------------------------------
    IF @Debug = 1 
        PRINT 'sp_ApplyTimePlanningForEmployeeDay FIN - EmpID=' 
              + CAST(@EmployeeID AS VARCHAR(10)) 
              + ' Fecha=' + CONVERT(VARCHAR(10), @WorkDate, 120);
END;
GO


/**************************************************************************
        FIN - Procedimiento para procesar las Horas Extras 
**************************************************************************/



CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceForDate
(
    @WorkDate DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EmployeeID INT;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT EmployeeID
        FROM HR.tbl_Employees
        WHERE IsActive = 1;

    OPEN cur;
    FETCH NEXT FROM cur INTO @EmployeeID;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- 1) Procesa la asistencia base (genera tbl_AttendanceCalculations)
        EXEC HR.sp_ProcessAttendanceEmployeeDay 
             @EmployeeID = @EmployeeID,
             @WorkDate   = @WorkDate;

        -- 2) Aplica planificación (Overtime / Recovery) y actualiza 
        --    AttendanceCalculations, TimeBalances, TimePlanningExecution y Overtime
        EXEC HR.sp_ProcessTimePlanningForEmployeeDay
             @EmployeeID = @EmployeeID,
             @WorkDate   = @WorkDate,
             @Debug      = 0;   -- pon 1 para ver logs detallados									
        FETCH NEXT FROM cur INTO @EmployeeID;
    END;

    CLOSE cur;
    DEALLOCATE cur;
END;
GO

CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRange
(
    @FromDate DATE,
    @ToDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @d DATE = @FromDate;

    WHILE @d <= @ToDate
    BEGIN
        EXEC HR.sp_ProcessAttendanceForDate @WorkDate = @d;
        SET @d = DATEADD(DAY, 1, @d);
    END;
END;
GO

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--********************************************************************************************************************************************************************************

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
