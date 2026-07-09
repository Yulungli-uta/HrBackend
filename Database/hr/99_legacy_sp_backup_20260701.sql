-- ============================================================
-- RESPALDO SPs LEGACY HR — 2026-07-01 08:12
-- Motivo: reemplazados por pipeline sp_ProcessAttendanceRunRange
-- Pipeline vigente:
--   sp_ProcessAttendanceRunRange
--     -> sp_ProcessAttendanceRunDate (por cada fecha)
--        -> sp_ProcessAttendanceBaseDay
--        -> sp_ProcessAttendanceLeavesDay
--        -> sp_ProcessAttendanceJustificationsDay
--        -> sp_ProcessAttendanceRecoveryDay
--        -> sp_ProcessAttendancePlanningDay
--        -> sp_ProcessAttendanceFinalizeDay
--        -> sp_ProcessGuardAttendanceDate
-- NOTA: sp_Justifications_Apply NO está en este respaldo porque
--       sigue activo en DailyJustificationsJob y JustificationsController.
-- ============================================================

-- [sp_Attendance_CalcNightMinutes]
--Minutos nocturnos  --(Distribuye minutos trabajados en [NIGHT_START, NIGHT_END])
CREATE OR ALTER PROCEDURE HR.sp_Attendance_CalcNightMinutes
  @FromDate DATE, @ToDate DATE, @EmployeeID INT = NULL
AS
BEGIN
  SET NOCOUNT ON;
  
  DECLARE @NightStart TIME = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_START') AS TIME),
          @NightEnd   TIME = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_END')   AS TIME);
  
  ;WITH punches AS (
    SELECT EmployeeID, 
           CAST(PunchTime AS DATE) D, 
           MIN(PunchTime) FirstIn, 
           MAX(PunchTime) LastOut
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
             ELSE DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightStart), p.D)
      END,
      CASE 
        WHEN CAST(p.LastOut AS TIME) < @NightEnd 
             THEN DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), DATEADD(DAY, 1, p.D))
             ELSE p.LastOut
      END
    )
  FROM HR.tbl_AttendanceCalculations ac
  JOIN punches p ON p.EmployeeID=ac.EmployeeID AND p.D=ac.WorkDate;
END
GO


-- [sp_Attendance_CalculateRange]
CREATE OR ALTER PROCEDURE HR.sp_Attendance_CalculateRange
  @FromDate     DATE,
  @ToDate       DATE,
  @EmployeeID   INT = NULL,
  @Debug        BIT = 1,
  @PersistLog   BIT = 0,
  @OnlySuspects BIT = 1
AS
BEGIN
  SET NOCOUNT ON;
  
  DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
  DECLARE @ErrorMsg NVARCHAR(4000);
  DECLARE @ErrorSeverity INT;
  DECLARE @ErrorState INT;
  DECLARE @RowsAffected INT = 0;

  BEGIN TRY
    /* ============================================================================
       SECCIÓN 1: VALIDACIÓN DE PARÁMETROS Y CONFIGURACIÓN INICIAL
    ============================================================================ */
    
    -- Validar parámetros de entrada
    IF @FromDate IS NULL OR @ToDate IS NULL
    BEGIN
      RAISERROR('Los parámetros @FromDate y @ToDate son obligatorios.', 16, 1);
      RETURN;
    END;

    IF @FromDate > @ToDate
    BEGIN
      RAISERROR('@FromDate no puede ser mayor que @ToDate.', 16, 1);
      RETURN;
    END;

    -- Obtener parámetros del sistema
    DECLARE 
      @GraceMin   INT  = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='TARDINESS_GRACE_MIN') AS INT),
      @OTMin      INT  = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='OT_MIN_THRESHOLD_MIN') AS INT),
      @NightStart TIME = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_START') AS TIME),
      @NightEnd   TIME = TRY_CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='NIGHT_END')   AS TIME);

    -- Establecer valores por defecto para parámetros NULL
    SET @GraceMin   = ISNULL(@GraceMin, 0);
    SET @OTMin      = ISNULL(@OTMin, 0);
    SET @NightStart = ISNULL(@NightStart, TRY_CAST('22:00' AS TIME));
    SET @NightEnd   = ISNULL(@NightEnd,   TRY_CAST('06:00' AS TIME));

    -- Log de inicio y parámetros
    IF @Debug = 1
    BEGIN
      PRINT '=================================================================';
      PRINT 'INICIO EJECUCIÓN: HR.sp_Attendance_CalculateRange';
      PRINT '=================================================================';
      PRINT 'Parámetros de entrada:';
      PRINT '  Rango fechas : ' + CONVERT(VARCHAR(10), @FromDate, 120) + ' -> ' + CONVERT(VARCHAR(10), @ToDate, 120);
      PRINT '  Empleado     : ' + COALESCE(CAST(@EmployeeID AS VARCHAR(12)), 'TODOS');
      PRINT '  Debug        : ' + CAST(@Debug AS VARCHAR(1));
      PRINT '  PersistLog   : ' + CAST(@PersistLog AS VARCHAR(1));
      PRINT '  OnlySuspects : ' + CAST(@OnlySuspects AS VARCHAR(1));
      PRINT '';
      PRINT 'Parámetros del sistema:';
      PRINT '  Tolerancia tardanza : ' + CAST(@GraceMin AS VARCHAR(10)) + ' min';
      PRINT '  Mínimo horas extras : ' + CAST(@OTMin AS VARCHAR(10)) + ' min'; 
      PRINT '  Horario nocturno    : ' + CONVERT(VARCHAR(8), @NightStart, 108) + ' -> ' + CONVERT(VARCHAR(8), @NightEnd, 108);
      PRINT '=================================================================';
    END;

    /* ============================================================================
       SECCIÓN 2: PREPARACIÓN DE DATOS BASE
    ============================================================================ */

    -- Tabla temporal: Calendario laboral
    IF OBJECT_ID('tempdb..#Calendario') IS NOT NULL DROP TABLE #Calendario;
    CREATE TABLE #Calendario (
      Fecha DATE PRIMARY KEY,
      EsFestivo BIT NOT NULL,
      EsFinSemana BIT NOT NULL
    );

    INSERT INTO #Calendario (Fecha, EsFestivo, EsFinSemana)
    SELECT 
      D AS Fecha,
      IsHoliday AS EsFestivo,
      IsWeekend AS EsFinSemana
    FROM HR.vw_Calendar
    WHERE D BETWEEN @FromDate AND @ToDate;

    -- Validar que existen fechas en el rango
    IF NOT EXISTS (SELECT 1 FROM #Calendario)
    BEGIN
      RAISERROR('No existen datos de calendario para el rango de fechas especificado.', 16, 1);
      RETURN;
    END;

    -- Tabla temporal: Empleados en scope
    IF OBJECT_ID('tempdb..#Empleados') IS NOT NULL DROP TABLE #Empleados;
    CREATE TABLE #Empleados (
      EmpleadoID INT PRIMARY KEY,
      Nombre NVARCHAR(100) NULL
    );

    INSERT INTO #Empleados (EmpleadoID, Nombre)
    SELECT 
      e.EmployeeID,
      e.FirstName + ' ' + e.LastName
    FROM HR.vw_EmployeeDetails e
    WHERE (@EmployeeID IS NULL OR e.EmployeeID = @EmployeeID);

    -- Validar que existen empleados
    IF NOT EXISTS (SELECT 1 FROM #Empleados)
    BEGIN
      RAISERROR('No se encontraron empleados para procesar.', 16, 1);
      RETURN;
    END;

    -- Tabla temporal: Licencias
    IF OBJECT_ID('tempdb..#Licencias') IS NOT NULL DROP TABLE #Licencias;
    CREATE TABLE #Licencias (
      EmpleadoID INT,
      Fecha DATE,
      PRIMARY KEY (EmpleadoID, Fecha)
    );

    INSERT INTO #Licencias (EmpleadoID, Fecha)
    SELECT DISTINCT 
      l.EmployeeID,
      c.Fecha
    FROM HR.vw_LeaveWindows l
    INNER JOIN #Calendario c
      ON c.Fecha >= CAST(l.FromDT AS DATE)
     AND c.Fecha < CAST(l.ToDT AS DATE)
    WHERE EXISTS (SELECT 1 FROM #Empleados e WHERE e.EmpleadoID = l.EmployeeID);

    /* ============================================================================
       SECCIÓN 3: HORARIOS Y REQUERIMIENTOS
    ============================================================================ */

    -- Tabla temporal: Horarios por empleado y día
    IF OBJECT_ID('tempdb..#HorariosEmpleados') IS NOT NULL DROP TABLE #HorariosEmpleados;
    CREATE TABLE #HorariosEmpleados (
      EmpleadoID INT NOT NULL,
      FechaTrabajo DATE NOT NULL,
      HorarioID INT NULL,
      HoraEntrada TIME NULL,
      HoraSalida TIME NULL,
      TieneAlmuerzo BIT NULL,
      InicioAlmuerzo TIME NULL,
      FinAlmuerzo TIME NULL,
      MinutosRequeridos INT NOT NULL DEFAULT(0),
      PRIMARY KEY (EmpleadoID, FechaTrabajo)
    );

    -- Obtener horarios efectivos para cada empleado y día
    WITH Combinaciones AS (
      SELECT 
        e.EmpleadoID,
        c.Fecha AS FechaTrabajo
      FROM #Empleados e
      CROSS JOIN #Calendario c
    ),
    HorariosEfectivos AS (
      SELECT 
        c.EmpleadoID,
        c.FechaTrabajo,
        (SELECT TOP 1 
            es.ScheduleID
         FROM HR.tbl_EmployeeSchedules es
         WHERE es.EmployeeID = c.EmpleadoID
           AND es.ValidFrom <= c.FechaTrabajo
           AND (es.ValidTo IS NULL OR c.FechaTrabajo <= es.ValidTo)
         ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC) AS HorarioID
      FROM Combinaciones c
    )
    INSERT INTO #HorariosEmpleados (
      EmpleadoID, FechaTrabajo, HorarioID, HoraEntrada, HoraSalida, 
      TieneAlmuerzo, InicioAlmuerzo, FinAlmuerzo, MinutosRequeridos
    )
    SELECT
      he.EmpleadoID,
      he.FechaTrabajo,
      he.HorarioID,
      s.EntryTime AS HoraEntrada,
      s.ExitTime AS HoraSalida,
      s.HasLunchBreak AS TieneAlmuerzo,
      s.LunchStart AS InicioAlmuerzo,
      s.LunchEnd AS FinAlmuerzo,
      -- Cálculo de minutos requeridos según horario
      CASE 
        WHEN s.ScheduleID IS NULL THEN 0
        WHEN s.RequiredHoursPerDay IS NOT NULL THEN 
          CAST(ROUND(s.RequiredHoursPerDay * 60.0, 0) AS INT)
        WHEN s.EntryTime IS NULL OR s.ExitTime IS NULL THEN 0
        ELSE 
          -- Diferencia entre hora de entrada y salida
          CASE 
            WHEN s.ExitTime >= s.EntryTime THEN
              DATEDIFF(MINUTE, s.EntryTime, s.ExitTime)
            ELSE
              -- Manejo de horarios que cruzan la medianoche
              DATEDIFF(MINUTE, s.EntryTime, CAST('23:59:59' AS TIME)) + 1 +
              DATEDIFF(MINUTE, CAST('00:00:00' AS TIME), s.ExitTime)
          END
          -- Restar tiempo de almuerzo si está configurado
          - CASE 
              WHEN s.HasLunchBreak = 1 
                 AND s.LunchStart IS NOT NULL 
                 AND s.LunchEnd IS NOT NULL THEN
                DATEDIFF(MINUTE, s.LunchStart, s.LunchEnd)
              ELSE 0
            END
      END AS MinutosRequeridos
    FROM HorariosEfectivos he
    LEFT JOIN HR.tbl_Schedules s ON s.ScheduleID = he.HorarioID;

    /* ============================================================================
       SECCIÓN 4: PROCESAMIENTO DE PICADAS Y CÁLCULO DE TIEMPOS TRABAJADOS - CORREGIDA
    ============================================================================ */

    -- Tabla temporal: Picadas y cálculos base
    IF OBJECT_ID('tempdb..#ProcesamientoPicadas') IS NOT NULL DROP TABLE #ProcesamientoPicadas;
    CREATE TABLE #ProcesamientoPicadas (
      EmpleadoID INT NOT NULL,
      FechaTrabajo DATE NOT NULL,
      FinVentanaNocturna DATETIME2 NOT NULL,
      PrimeraEntrada DATETIME2 NULL,
      UltimaSalida DATETIME2 NULL,
      TotalMinutosTrabajados INT NOT NULL DEFAULT(0),
      CantidadPicadas INT NOT NULL DEFAULT(0),
      PRIMARY KEY (EmpleadoID, FechaTrabajo)
    );

    -- Configurar ventana nocturna y obtener picadas
    INSERT INTO #ProcesamientoPicadas (
      EmpleadoID, FechaTrabajo, FinVentanaNocturna, 
      PrimeraEntrada, UltimaSalida, TotalMinutosTrabajados, CantidadPicadas
    )
    SELECT
      h.EmpleadoID,
      h.FechaTrabajo,
      -- Calcular fin de ventana nocturna (maneja cruce de medianoche)
      CASE
        WHEN @NightEnd <= @NightStart THEN
          DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2)))
        ELSE
          DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2))
      END AS FinVentanaNocturna,
      NULL, NULL, 0, 0
    FROM #HorariosEmpleados h;

    -- Calcular primera entrada, última salida y suma de diferencias entre picadas - CORREGIDO
    UPDATE pp
    SET 
      PrimeraEntrada        = datos.PrimeraEntrada,
      UltimaSalida          = datos.UltimaSalida,
      TotalMinutosTrabajados = ISNULL(datos.TotalMinutosTrabajados, 0),
      CantidadPicadas       = ISNULL(datos.CantidadPicadas, 0)
    FROM #ProcesamientoPicadas pp
    OUTER APPLY (
      SELECT 
        MIN(p2.PunchTime) AS PrimeraEntrada,
        MAX(p2.PunchTime) AS UltimaSalida,
        COUNT(*)          AS CantidadPicadas,
        -- Suma de diferencias entre picadas consecutivas SOLO en filas impares (entradas)
        SUM(
          CASE 
            WHEN p2.NumeroFila % 2 = 1 
                 AND p2.SiguientePicada IS NOT NULL 
              THEN DATEDIFF(MINUTE, p2.PunchTime, p2.SiguientePicada)
            ELSE 0
          END
        ) AS TotalMinutosTrabajados
      FROM (
        SELECT 
          p.PunchTime,
          LEAD(p.PunchTime) OVER (ORDER BY p.PunchTime) AS SiguientePicada,
          ROW_NUMBER()       OVER (ORDER BY p.PunchTime) AS NumeroFila
        FROM HR.tbl_AttendancePunches p
        WHERE p.EmployeeID = pp.EmpleadoID
          AND p.PunchTime >= CAST(pp.FechaTrabajo AS DATETIME2)
          AND p.PunchTime < pp.FinVentanaNocturna
      ) p2
    ) datos;


    /* ============================================================================
       SECCIÓN 5: CÁLCULOS DETALLADOS POR DÍA - CORREGIDA
    ============================================================================ */

    -- Tabla temporal: Resultados procesados
    IF OBJECT_ID('tempdb..#ResultadosCalculados') IS NOT NULL DROP TABLE #ResultadosCalculados;
    CREATE TABLE #ResultadosCalculados (
      EmpleadoID INT NOT NULL,
      FechaTrabajo DATE NOT NULL,
      MinutosRequeridos INT NOT NULL,
      HoraEntrada TIME NULL,
      HoraSalida TIME NULL,
      TieneAlmuerzo BIT NULL,
      InicioAlmuerzo TIME NULL,
      FinAlmuerzo TIME NULL,
      MinutosAlmuerzo INT NOT NULL DEFAULT(0),
      FechaHoraEntrada DATETIME2 NULL,
      FechaHoraSalida DATETIME2 NULL,
      FechaHoraInicioAlmuerzo DATETIME2 NULL,
      FechaHoraFinAlmuerzo DATETIME2 NULL,
      PrimeraEntradaReal DATETIME2 NULL,
      UltimaSalidaReal DATETIME2 NULL,
      EsFestivo BIT NOT NULL DEFAULT(0),
      EsFinSemana BIT NOT NULL DEFAULT(0),
      TieneLicencia BIT NOT NULL DEFAULT(0),
      MinutosTrabajadosBrutos INT NOT NULL DEFAULT(0),
      MinutosTrabajadosNetos INT NOT NULL DEFAULT(0),
      MinutosTardanza INT NOT NULL DEFAULT(0),
      MinutosRetraso INT NOT NULL DEFAULT(0),
      MinutosNocturnos INT NOT NULL DEFAULT(0),
      InicioManana DATETIME2 NULL,
      FinManana DATETIME2 NULL,
      InicioTarde DATETIME2 NULL,
      FinTarde DATETIME2 NULL,
      MinutosSolapadosManana INT NOT NULL DEFAULT(0),
      MinutosSolapadosTarde INT NOT NULL DEFAULT(0),
      MinutosTrabajadosProgramados INT NOT NULL DEFAULT(0),
      MinutosFueraHorario INT NOT NULL DEFAULT(0),
      MinutosRegularesFinales INT NOT NULL DEFAULT(0),
      FlagRegularesIgualTotal BIT NOT NULL DEFAULT(0),
      RazonRegularesIgualTotal NVARCHAR(200) NULL,
      CantidadPicadas INT NOT NULL DEFAULT(0),
      PRIMARY KEY (EmpleadoID, FechaTrabajo)
    );

    -- Insertar datos base y calcular tiempos trabajados - CORREGIDO
    INSERT INTO #ResultadosCalculados (
      EmpleadoID, FechaTrabajo, MinutosRequeridos,
      HoraEntrada, HoraSalida, TieneAlmuerzo, InicioAlmuerzo, FinAlmuerzo, MinutosAlmuerzo,
      FechaHoraEntrada, FechaHoraSalida, FechaHoraInicioAlmuerzo, FechaHoraFinAlmuerzo,
      PrimeraEntradaReal, UltimaSalidaReal, EsFestivo, EsFinSemana, TieneLicencia,
      MinutosTrabajadosBrutos, MinutosTrabajadosNetos, MinutosTardanza, MinutosRetraso, 
      MinutosNocturnos, CantidadPicadas
    )
    SELECT
      h.EmpleadoID,
      h.FechaTrabajo,
      ISNULL(h.MinutosRequeridos, 0),
      h.HoraEntrada,
      h.HoraSalida,
      h.TieneAlmuerzo,
      h.InicioAlmuerzo,
      h.FinAlmuerzo,
      CASE 
        WHEN h.TieneAlmuerzo = 1 
          AND h.InicioAlmuerzo IS NOT NULL 
          AND h.FinAlmuerzo IS NOT NULL THEN
          DATEDIFF(MINUTE, h.InicioAlmuerzo, h.FinAlmuerzo)
        ELSE 0
      END,
      CASE 
        WHEN h.HoraEntrada IS NULL THEN NULL
        ELSE DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraEntrada), CAST(h.FechaTrabajo AS DATETIME2))
      END,
      CASE 
        WHEN h.HoraSalida IS NULL THEN NULL
        ELSE 
          CASE 
            WHEN h.HoraSalida >= h.HoraEntrada THEN
              DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraSalida), CAST(h.FechaTrabajo AS DATETIME2))
            ELSE
              DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraSalida), DATEADD(DAY, 1, CAST(h.FechaTrabajo AS DATETIME2)))
          END
      END,
      CASE 
        WHEN h.TieneAlmuerzo = 1 AND h.InicioAlmuerzo IS NOT NULL THEN
          DATEADD(SECOND, DATEDIFF(SECOND, 0, h.InicioAlmuerzo), CAST(h.FechaTrabajo AS DATETIME2))
        ELSE NULL
      END,
      CASE 
        WHEN h.TieneAlmuerzo = 1 AND h.FinAlmuerzo IS NOT NULL THEN
          DATEADD(SECOND, DATEDIFF(SECOND, 0, h.FinAlmuerzo), CAST(h.FechaTrabajo AS DATETIME2))
        ELSE NULL
      END,
      pp.PrimeraEntrada,
      pp.UltimaSalida,
      c.EsFestivo,
      c.EsFinSemana,
      CASE WHEN l.EmpleadoID IS NOT NULL THEN 1 ELSE 0 END,
      -- CORRECCIÓN: Usar ISNULL para evitar NULL en cálculos
      ISNULL(pp.TotalMinutosTrabajados, 0) AS MinutosTrabajadosBrutos,
      -- LÓGICA PRINCIPAL: Cálculo de minutos trabajados netos - CORREGIDO
      CASE 
        WHEN h.TieneAlmuerzo = 1 
          AND h.InicioAlmuerzo IS NOT NULL 
          AND h.FinAlmuerzo IS NOT NULL 
          AND pp.CantidadPicadas = 2 
          AND ISNULL(pp.TotalMinutosTrabajados, 0) > h.MinutosRequeridos THEN
          -- Condición específica: restar almuerzo
          ISNULL(pp.TotalMinutosTrabajados, 0) - DATEDIFF(MINUTE, h.InicioAlmuerzo, h.FinAlmuerzo)
        ELSE
          -- Cualquier otro caso: usar total sin modificar
          ISNULL(pp.TotalMinutosTrabajados, 0)
      END AS MinutosTrabajadosNetos,
      CASE 
        WHEN pp.PrimeraEntrada IS NULL OR h.HoraEntrada IS NULL THEN 0
        ELSE 
          CASE 
            WHEN DATEDIFF(MINUTE, 
                  DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraEntrada), CAST(h.FechaTrabajo AS DATETIME2)), 
                  pp.PrimeraEntrada) - @GraceMin < 0 THEN 0
            ELSE 
              DATEDIFF(MINUTE,
                DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraEntrada), CAST(h.FechaTrabajo AS DATETIME2)), 
                pp.PrimeraEntrada) - @GraceMin
          END
      END,
      CASE 
        WHEN pp.PrimeraEntrada IS NULL OR h.HoraEntrada IS NULL THEN 0
        ELSE 
          DATEDIFF(MINUTE,
            DATEADD(SECOND, DATEDIFF(SECOND, 0, h.HoraEntrada), CAST(h.FechaTrabajo AS DATETIME2)), 
            pp.PrimeraEntrada)
      END,
      CASE
        WHEN pp.PrimeraEntrada IS NULL OR pp.UltimaSalida IS NULL OR pp.UltimaSalida <= pp.PrimeraEntrada THEN 0
        ELSE
          DATEDIFF(MINUTE,
            CASE 
              WHEN DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightStart), CAST(h.FechaTrabajo AS DATETIME2)) > pp.PrimeraEntrada
                THEN DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightStart), CAST(h.FechaTrabajo AS DATETIME2))
              ELSE pp.PrimeraEntrada
            END,
            CASE 
              WHEN (CASE 
                      WHEN @NightEnd <= @NightStart THEN
                        DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2)))
                      ELSE
                        DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2))
                    END) < pp.UltimaSalida
                THEN (CASE 
                        WHEN @NightEnd <= @NightStart THEN
                          DATEADD(DAY, 1, DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2)))
                        ELSE
                          DATEADD(SECOND, DATEDIFF(SECOND, 0, @NightEnd), CAST(h.FechaTrabajo AS DATETIME2))
                      END)
              ELSE pp.UltimaSalida
            END
          )
      END,
      ISNULL(pp.CantidadPicadas, 0) AS CantidadPicadas
    FROM #HorariosEmpleados h
    INNER JOIN #Calendario c ON c.Fecha = h.FechaTrabajo
    LEFT JOIN #Licencias l ON l.EmpleadoID = h.EmpleadoID AND l.Fecha = h.FechaTrabajo
    INNER JOIN #ProcesamientoPicadas pp ON pp.EmpleadoID = h.EmpleadoID AND pp.FechaTrabajo = h.FechaTrabajo;

    /* ============================================================================
       SECCIÓN 6: CÁLCULO DE HORARIO REGULAR Y TRASLAPES
    ============================================================================ */

    -- Calcular ventanas de tiempo para horario de la mañana
    UPDATE #ResultadosCalculados
    SET 
      InicioManana = CASE 
          WHEN PrimeraEntradaReal IS NULL OR FechaHoraEntrada IS NULL THEN NULL
          ELSE CASE 
              WHEN PrimeraEntradaReal > FechaHoraEntrada THEN PrimeraEntradaReal
              ELSE FechaHoraEntrada
            END
        END,
      FinManana = CASE 
          WHEN UltimaSalidaReal IS NULL OR FechaHoraSalida IS NULL THEN NULL
          ELSE
            CASE 
              WHEN (CASE 
                      WHEN FechaHoraInicioAlmuerzo IS NOT NULL THEN FechaHoraInicioAlmuerzo
                      ELSE FechaHoraSalida
                    END) < UltimaSalidaReal
                THEN (CASE 
                        WHEN FechaHoraInicioAlmuerzo IS NOT NULL THEN FechaHoraInicioAlmuerzo
                        ELSE FechaHoraSalida
                      END)
              ELSE UltimaSalidaReal
            END
        END;

    UPDATE #ResultadosCalculados
    SET MinutosSolapadosManana =
        CASE 
          WHEN InicioManana IS NULL OR FinManana IS NULL OR FinManana <= InicioManana THEN 0
          ELSE DATEDIFF(MINUTE, InicioManana, FinManana)
        END;

    -- Calcular ventanas de tiempo para horario de la tarde (solo si tiene almuerzo)
    UPDATE #ResultadosCalculados
    SET 
      InicioTarde = CASE 
          WHEN PrimeraEntradaReal IS NULL OR FechaHoraFinAlmuerzo IS NULL THEN NULL
          ELSE CASE 
              WHEN PrimeraEntradaReal > FechaHoraFinAlmuerzo THEN PrimeraEntradaReal
              ELSE FechaHoraFinAlmuerzo
            END
        END,
      FinTarde = CASE 
          WHEN UltimaSalidaReal IS NULL OR FechaHoraSalida IS NULL OR FechaHoraFinAlmuerzo IS NULL THEN NULL
          ELSE CASE 
              WHEN FechaHoraSalida < UltimaSalidaReal THEN FechaHoraSalida
              ELSE UltimaSalidaReal
            END
        END;

    UPDATE #ResultadosCalculados
    SET MinutosSolapadosTarde =
        CASE 
          WHEN InicioTarde IS NULL OR FinTarde IS NULL OR FinTarde <= InicioTarde THEN 0
          ELSE DATEDIFF(MINUTE, InicioTarde, FinTarde)
        END;

    -- Calcular minutos trabajados dentro del horario programado
    UPDATE #ResultadosCalculados
    SET 
      MinutosTrabajadosProgramados = ISNULL(MinutosSolapadosManana, 0) + ISNULL(MinutosSolapadosTarde, 0),
      MinutosFueraHorario = 
        CASE 
          WHEN (ISNULL(MinutosSolapadosManana, 0) + ISNULL(MinutosSolapadosTarde, 0)) >= MinutosTrabajadosNetos
            THEN 0
          ELSE MinutosTrabajadosNetos - (ISNULL(MinutosSolapadosManana, 0) + ISNULL(MinutosSolapadosTarde, 0))
        END;

    -- Calcular minutos regulares finales (nunca mayores que los minutos trabajados netos)
    UPDATE #ResultadosCalculados
    SET 
      MinutosRegularesFinales = CASE 
          WHEN (EsFestivo = 1 OR EsFinSemana = 1 OR TieneLicencia = 1) THEN 0
          ELSE
            CASE
              WHEN MinutosRequeridos <= MinutosTrabajadosProgramados THEN 
                CASE 
                  WHEN MinutosRequeridos > MinutosTrabajadosNetos THEN MinutosTrabajadosNetos
                  ELSE MinutosRequeridos
                END
              ELSE
                CASE 
                  WHEN MinutosTrabajadosProgramados > MinutosTrabajadosNetos THEN MinutosTrabajadosNetos
                  ELSE MinutosTrabajadosProgramados
                END
            END
        END,
      FlagRegularesIgualTotal = CASE 
          WHEN (CASE WHEN TieneLicencia = 1 THEN 0 ELSE MinutosTrabajadosNetos END) = 
               (CASE 
                  WHEN (EsFestivo = 1 OR EsFinSemana = 1 OR TieneLicencia = 1) THEN 0
                  ELSE
                    CASE
                      WHEN MinutosRequeridos <= MinutosTrabajadosProgramados THEN 
                        CASE 
                          WHEN MinutosRequeridos > MinutosTrabajadosNetos THEN MinutosTrabajadosNetos
                          ELSE MinutosRequeridos
                        END
                      ELSE
                        CASE 
                          WHEN MinutosTrabajadosProgramados > MinutosTrabajadosNetos THEN MinutosTrabajadosNetos
                          ELSE MinutosTrabajadosProgramados
                        END
                    END
                END)
          THEN 1 
          ELSE 0 
        END,
      RazonRegularesIgualTotal = CASE 
          WHEN PrimeraEntradaReal IS NULL OR UltimaSalidaReal IS NULL THEN N'SIN_PICADAS'
          WHEN MinutosRequeridos = 0 THEN N'MINUTOS_REQUERIDOS_CERO'
          WHEN MinutosFueraHorario = 0 
            AND MinutosTrabajadosNetos = MinutosTrabajadosProgramados 
            AND MinutosRequeridos >= MinutosTrabajadosProgramados THEN 
            N'TODO_DENTRO_HORARIO_Y_REQUERIDO>=PROGRAMADO'
          WHEN MinutosFueraHorario = 0 
            AND MinutosTrabajadosNetos = MinutosTrabajadosProgramados 
            AND MinutosRequeridos < MinutosTrabajadosProgramados THEN 
            N'TODO_DENTRO_HORARIO_PER_REQUERIDO_MENOR'
          WHEN FechaHoraEntrada IS NULL OR FechaHoraSalida IS NULL THEN N'HORARIO_SIN_ENTRADA_O_SALIDA'
          ELSE N'OTRA_COINCIDENCIA'
        END;

    /* ============================================================================
       SECCIÓN 7: ACTUALIZACIÓN DE LA TABLA DE ASISTENCIA - CORREGIDA
    ============================================================================ */

    BEGIN TRANSACTION;

    -- Actualizar registros existentes
    UPDATE T
    SET 
      TotalWorkedMinutes = CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END,
      RegularMinutes = C.MinutosRegularesFinales,
      OvertimeMinutes = 
        CASE 
          WHEN (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END) -
               (CASE WHEN C.TieneLicencia = 1 OR C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 0 ELSE C.MinutosRequeridos END) >= @OTMin
            THEN (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END) -
                 (CASE WHEN C.TieneLicencia = 1 OR C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 0 ELSE C.MinutosRequeridos END)
          ELSE 0
        END,
      NightMinutes = CASE WHEN C.MinutosNocturnos < 0 THEN 0 ELSE C.MinutosNocturnos END,
      HolidayMinutes = 
        CASE 
          WHEN C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 
            (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END)
          ELSE 0
        END,
      TardinessMin = CASE WHEN C.MinutosTardanza < 0 THEN 0 ELSE C.MinutosTardanza END,
      RequiredMinutes = C.MinutosRequeridos,
      MinutesLate = C.MinutosRetraso,
      FirstPunchIn = C.PrimeraEntradaReal,
      LastPunchOut = C.UltimaSalidaReal,
      ScheduledWorkedMin = C.MinutosTrabajadosProgramados,
      OffScheduleMin = C.MinutosFueraHorario,
      Status = 'Approved'
      -- Comentado porque ModifiedDate no existe en la tabla
      -- ModifiedDate = SYSUTCDATETIME()
    FROM HR.tbl_AttendanceCalculations T WITH (UPDLOCK, ROWLOCK)
    INNER JOIN #ResultadosCalculados C
      ON C.EmpleadoID = T.EmployeeID AND C.FechaTrabajo = T.WorkDate;

    SET @RowsAffected = @RowsAffected + @@ROWCOUNT;

    -- Insertar nuevos registros
    INSERT INTO HR.tbl_AttendanceCalculations (
      EmployeeID, WorkDate, FirstPunchIn, LastPunchOut,
      TotalWorkedMinutes, RegularMinutes, OvertimeMinutes,
      NightMinutes, HolidayMinutes, TardinessMin, RequiredMinutes,
      MinutesLate, ScheduledWorkedMin, OffScheduleMin, Status, FoodSubsidy
      -- Comentado porque CreatedDate y ModifiedDate no existen en la tabla
      -- CreatedDate, ModifiedDate
    )
    SELECT
      C.EmpleadoID,
      C.FechaTrabajo,
      C.PrimeraEntradaReal,
      C.UltimaSalidaReal,
      CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END,
      C.MinutosRegularesFinales,
      CASE 
        WHEN (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END) -
             (CASE WHEN C.TieneLicencia = 1 OR C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 0 ELSE C.MinutosRequeridos END) >= @OTMin
          THEN (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END) -
               (CASE WHEN C.TieneLicencia = 1 OR C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 0 ELSE C.MinutosRequeridos END)
        ELSE 0
      END,
      CASE WHEN C.MinutosNocturnos < 0 THEN 0 ELSE C.MinutosNocturnos END,
      CASE 
        WHEN C.EsFestivo = 1 OR C.EsFinSemana = 1 THEN 
          (CASE WHEN C.TieneLicencia = 1 THEN 0 ELSE C.MinutosTrabajadosNetos END)
        ELSE 0
      END,
      CASE WHEN C.MinutosTardanza < 0 THEN 0 ELSE C.MinutosTardanza END,
      C.MinutosRequeridos,
      C.MinutosRetraso,
      C.MinutosTrabajadosProgramados,
      C.MinutosFueraHorario,
      'Approved',
      0
      -- Comentado porque CreatedDate y ModifiedDate no existen en la tabla
      -- SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM #ResultadosCalculados C
    WHERE NOT EXISTS (
      SELECT 1 
      FROM HR.tbl_AttendanceCalculations T 
      WHERE T.EmployeeID = C.EmpleadoID AND T.WorkDate = C.FechaTrabajo
    );

    SET @RowsAffected = @RowsAffected + @@ROWCOUNT;

    COMMIT TRANSACTION;

    /* ============================================================================
       SECCIÓN 8: CÁLCULO DE SUBSIDIO DE ALIMENTACIÓN
    ============================================================================ */

    -- Actualizar subsidio de alimentación para contrato "Código Trabajo"
    ;WITH ContratoCodigoTrabajo AS (
      SELECT rt.TypeID
      FROM HR.ref_Types rt
      WHERE rt.Category = 'CONTRACT_TYPE' 
        AND UPPER(rt.Name) = UPPER(N'Código Trabajo')
    )
    UPDATE ac
    SET 
      ac.FoodSubsidy = CASE WHEN ac.RegularMinutes >= ac.RequiredMinutes THEN 1 ELSE ac.FoodSubsidy END
    FROM HR.tbl_AttendanceCalculations ac
    INNER JOIN HR.tbl_Employees e ON e.EmployeeID = ac.EmployeeID
    INNER JOIN ContratoCodigoTrabajo ct ON ct.TypeID = e.EmployeeType
    WHERE ac.WorkDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR ac.EmployeeID = @EmployeeID);

    /* ============================================================================
       SECCIÓN 9: REGISTRO DE LOGS TEMPORAL
    ============================================================================ */

    -- Tabla temporal para log detallado (solo para debug)
    IF OBJECT_ID('tempdb..#LogDetallado') IS NOT NULL DROP TABLE #LogDetallado;
    CREATE TABLE #LogDetallado (
        EmpleadoID INT,
        FechaTrabajo DATE,
        MinutosRequeridos INT,
        HoraEntrada TIME NULL,
        HoraSalida TIME NULL,
        TieneAlmuerzo BIT,
        InicioAlmuerzo TIME NULL,
        FinAlmuerzo TIME NULL,
        MinutosAlmuerzoConfig INT,
        FechaHoraEntrada DATETIME2 NULL,
        FechaHoraSalida DATETIME2 NULL,
        FechaHoraInicioAlmuerzo DATETIME2 NULL,
        FechaHoraFinAlmuerzo DATETIME2 NULL,
        PrimeraEntradaReal DATETIME2 NULL,
        UltimaSalidaReal DATETIME2 NULL,
        MinutosTrabajadosBrutos INT,
        MinutosTrabajadosNetos INT,
        InicioManana DATETIME2 NULL,
        FinManana DATETIME2 NULL,
        MinutosSolapadosManana INT,
        InicioTarde DATETIME2 NULL,
        FinTarde DATETIME2 NULL,
        MinutosSolapadosTarde INT,
        MinutosTrabajadosProgramados INT,
        MinutosFueraHorario INT,
        MinutosTardanza INT,
        MinutosRetraso INT,
        MinutosNocturnos INT,
        MinutosRegularesFinales INT,
        CantidadPicadas INT
    );

    INSERT INTO #LogDetallado
    SELECT
        rc.EmpleadoID,
        rc.FechaTrabajo,
        rc.MinutosRequeridos,
        rc.HoraEntrada,
        rc.HoraSalida,
        rc.TieneAlmuerzo,
        rc.InicioAlmuerzo,
        rc.FinAlmuerzo,
        rc.MinutosAlmuerzo,
        rc.FechaHoraEntrada,
        rc.FechaHoraSalida,
        rc.FechaHoraInicioAlmuerzo,
        rc.FechaHoraFinAlmuerzo,
        rc.PrimeraEntradaReal,
        rc.UltimaSalidaReal,
        rc.MinutosTrabajadosBrutos,
        rc.MinutosTrabajadosNetos,
        rc.InicioManana,
        rc.FinManana,
        rc.MinutosSolapadosManana,
        rc.InicioTarde,
        rc.FinTarde,
        rc.MinutosSolapadosTarde,
        rc.MinutosTrabajadosProgramados,
        rc.MinutosFueraHorario,
        rc.MinutosTardanza,
        rc.MinutosRetraso,
        rc.MinutosNocturnos,
        rc.MinutosRegularesFinales,
        rc.CantidadPicadas
    FROM #ResultadosCalculados rc;

    -- Mensaje sobre persistencia deshabilitada
    IF @PersistLog = 1 AND @Debug = 1
    BEGIN
        PRINT 'NOTA: Persistencia de log deshabilitada - Log solo disponible temporalmente durante esta ejecución.';
    END;

    /* ============================================================================
       SECCIÓN 10: SALIDA Y REPORTE FINAL
    ============================================================================ */

    DECLARE @EndTime DATETIME2 = SYSUTCDATETIME();
    DECLARE @DurationMs INT = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

    IF @Debug = 1
    BEGIN
        PRINT '';
        PRINT '=================================================================';
        PRINT 'RESUMEN EJECUCIÓN: HR.sp_Attendance_CalculateRange';
        PRINT '=================================================================';
        PRINT 'Estadísticas:';
        PRINT '  Registros procesados : ' + CAST(@RowsAffected AS VARCHAR(10));
        PRINT '  Tiempo ejecución     : ' + CAST(@DurationMs AS VARCHAR(10)) + ' ms';
        PRINT '  Fecha inicio         : ' + CONVERT(VARCHAR(23), @StartTime, 121);
        PRINT '  Fecha fin            : ' + CONVERT(VARCHAR(23), @EndTime, 121);
        PRINT '';
        
        -- Mostrar log detallado desde tabla temporal
        PRINT 'LOG DETALLADO TEMPORAL (Registros procesados):';
        SELECT 
            EmpleadoID AS [ID],
            FechaTrabajo AS [Fecha],
            MinutosRequeridos AS [Req],
            MinutosTrabajadosNetos AS [Total],
            MinutosTrabajadosProgramados AS [Prog],
            MinutosFueraHorario AS [Fuera],
            MinutosRegularesFinales AS [Reg],
            MinutosTardanza AS [Tard],
            CantidadPicadas AS [Picadas]
        FROM #LogDetallado
        ORDER BY FechaTrabajo, EmpleadoID;

        PRINT '';
        PRINT 'RESULTADOS EN TABLA FINAL:';
        SELECT 
            ac.EmployeeID AS [ID],
            ac.WorkDate AS [Fecha],
            ac.RequiredMinutes AS [Req],
            ac.TotalWorkedMinutes AS [Total],
            ac.ScheduledWorkedMin AS [Prog],
            ac.OffScheduleMin AS [Fuera],
            ac.RegularMinutes AS [Reg],
            ac.OvertimeMinutes AS [Extra],
            ac.TardinessMin AS [Tard],
            ac.FirstPunchIn AS [Entrada],
            ac.LastPunchOut AS [Salida],
            ac.FoodSubsidy AS [Subsidio]
        FROM HR.tbl_AttendanceCalculations ac
        WHERE ac.WorkDate BETWEEN @FromDate AND @ToDate
            AND EXISTS (SELECT 1 FROM #Empleados e WHERE e.EmpleadoID = ac.EmployeeID)
        ORDER BY ac.WorkDate, ac.EmployeeID;

        PRINT '';
        PRINT 'FIN EJECUCIÓN EXITOSA: HR.sp_Attendance_CalculateRange';
        PRINT '=================================================================';
    END;

    /* ============================================================================
       SECCIÓN 11: LIMPIEZA DE RECURSOS
    ============================================================================ */

    DROP TABLE IF EXISTS #LogDetallado;
    DROP TABLE IF EXISTS #ResultadosCalculados;
    DROP TABLE IF EXISTS #ProcesamientoPicadas;
    DROP TABLE IF EXISTS #HorariosEmpleados;
    DROP TABLE IF EXISTS #Licencias;
    DROP TABLE IF EXISTS #Empleados;
    DROP TABLE IF EXISTS #Calendario;

  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0
      ROLLBACK TRANSACTION;

    SELECT 
      @ErrorMsg = ERROR_MESSAGE(),
      @ErrorSeverity = ERROR_SEVERITY(),
      @ErrorState = ERROR_STATE();

    PRINT 'ERROR en HR.sp_Attendance_CalculateRange:';
    PRINT '  Mensaje : ' + @ErrorMsg;
    PRINT '  Severidad: ' + CAST(@ErrorSeverity AS VARCHAR(10));
    PRINT '  Estado   : ' + CAST(@ErrorState AS VARCHAR(10));
    PRINT '  Línea    : ' + CAST(ERROR_LINE() AS VARCHAR(10));

    RAISERROR(@ErrorMsg, @ErrorSeverity, @ErrorState);
  END CATCH;
END;
GO


-- [sp_ProcessAttendanceEmployeeDay]
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceEmployeeDay
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


-- [sp_ProcessAttendanceForDate]
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


-- [sp_ProcessAttendanceRange]
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


-- [sp_ProcessTimePlanningForEmployeeDay]
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
        PRINT 'sp_ProcessTimePlanningForEmployeeDay INICIO - EmpID=' 
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
    END;

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

        -- MOD: Si no hay horario pero sí podría haber quedado HE/Recovery de antes, los ponemos en 0
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            recoveredMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END;

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

        -- MOD: Si NO hay planificación, NO se permiten HE/REC de cálculo. Se ponen en 0.
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            recoveredMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END;

    IF @Debug = 1
    BEGIN
        DECLARE @CountPlans INT;
        SELECT @CountPlans = COUNT(*) FROM #Plans;
        PRINT 'Planes encontrados (sin filtrar por horario): ' + CAST(@CountPlans AS VARCHAR(12));
    END;

    --------------------------------------------------------------------
    -- 4) Filtrar planes que estén completamente fuera del horario normal
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

        -- MOD: No hay NINGÚN plan válido → HE/Recovery deben quedar en 0
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            recoveredMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END;

    IF @Debug = 1
    BEGIN
        DECLARE @CountValidPlans INT;
        SELECT @CountValidPlans = COUNT(*) FROM #ValidPlans;
        PRINT 'Planes válidos (fuera de horario normal): ' 
              + CAST(@CountValidPlans AS VARCHAR(12));
    END;

    --------------------------------------------------------------------
    -- 5) Obtener ventana de trabajo real (picadas)
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

        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes   = 0,
            recoveredMinutes  = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END;

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

    DELETE FROM #ExecPlans
    WHERE ExecutedMinutes <= 0;

    IF NOT EXISTS (SELECT 1 FROM #ExecPlans)
    BEGIN
        IF @Debug = 1 
            PRINT 'No hubo minutos realmente trabajados dentro de las ventanas de los planes.';
        
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            recoveredMinutes = 0
        WHERE EmployeeID = @EmployeeID
          AND WorkDate   = @WorkDate;

        RETURN;
    END;

    IF @Debug = 1
    BEGIN
        DECLARE @CountExecPlans INT;
        SELECT @CountExecPlans = COUNT(*) FROM #ExecPlans;
        PRINT 'Planes con minutos ejecutados: ' 
              + CAST(@CountExecPlans AS VARCHAR(12));
    END;

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
    END;

    --------------------------------------------------------------------
    -- 8) Actualizar tbl_AttendanceCalculations con minutos verificados
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
    END;

    --------------------------------------------------------------------
    -- 10) Registrar ejecución en HR.tbl_TimePlanningExecution
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
            NULL,
            NULL,
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
    -- 11) Consolidar horas extra en HR.tbl_Overtime (solo Overtime)
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
    END;

    --------------------------------------------------------------------
    -- 12) FIN
    --------------------------------------------------------------------
    IF @Debug = 1 
        PRINT 'sp_ProcessTimePlanningForEmployeeDay FIN - EmpID=' 
              + CAST(@EmployeeID AS VARCHAR(10)) 
              + ' Fecha=' + CONVERT(VARCHAR(10), @WorkDate, 120);
END;
GO


-- [sp_Recovery_Apply]
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


-- ============================================================
-- SCRIPT DROP (ejecutado el 2026-07-01)
-- ============================================================
DROP PROCEDURE IF EXISTS HR.sp_Attendance_CalcNightMinutes;
GO

DROP PROCEDURE IF EXISTS HR.sp_Attendance_CalculateRange;
GO

DROP PROCEDURE IF EXISTS HR.sp_ProcessAttendanceEmployeeDay;
GO

DROP PROCEDURE IF EXISTS HR.sp_ProcessAttendanceForDate;
GO

DROP PROCEDURE IF EXISTS HR.sp_ProcessAttendanceRange;
GO

DROP PROCEDURE IF EXISTS HR.sp_ProcessTimePlanningForEmployeeDay;
GO

DROP PROCEDURE IF EXISTS HR.sp_Recovery_Apply;
GO

-- ============================================================
-- RESPALDO SP LEGACY HR — 2026-07-01
-- Motivo: [HR].[sp_InsertReportAudit] reemplazado por inserción
-- vía Entity Framework Core (ReportAuditRepository.CreateAuditAsync,
-- usando el repositorio genérico IRepository<ReportAudit,int>).
-- ============================================================

-- [sp_InsertReportAudit]

CREATE PROCEDURE [HR].[sp_InsertReportAudit]
    @UserId UNIQUEIDENTIFIER,
    @UserEmail NVARCHAR(255),
    @ReportType NVARCHAR(50),
    @ReportFormat NVARCHAR(10),
    @FiltersApplied NVARCHAR(MAX) = NULL,
    @FileSizeBytes BIGINT = NULL,
    @GenerationTimeMs INT = NULL,
    @ClientIp NVARCHAR(50) = NULL,
    @Success BIT = 1,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @FileName NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [HR].[tbl_ReportAudit] (
        UserId, UserEmail, ReportType, ReportFormat, FiltersApplied,
        GeneratedAt, FileSizeBytes, GenerationTimeMs, ClientIp,
        Success, ErrorMessage, FileName
    )
    VALUES (
        @UserId, @UserEmail, @ReportType, @ReportFormat, @FiltersApplied,
        GETUTCDATE(), @FileSizeBytes, @GenerationTimeMs, @ClientIp,
        @Success, @ErrorMessage, @FileName
    );

    SELECT SCOPE_IDENTITY() AS AuditId;
END

GO

-- ============================================================
-- SCRIPT DROP — pendiente de confirmación explícita antes de ejecutar
-- ============================================================
DROP PROCEDURE IF EXISTS HR.sp_InsertReportAudit;
GO
