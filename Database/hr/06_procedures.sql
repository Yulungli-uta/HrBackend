-- ============================================================
-- PROCEDIMIENTOS : esquema [HR]
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- Fase 2-4 (2026-07-03): forzar estas dos configuraciones de sesión ANTES de
-- crear/alterar cualquier procedimiento en este archivo. SQL Server graba
-- ANSI_NULLS/QUOTED_IDENTIFIER dentro del procedimiento al momento de
-- compilarlo, y los usa siempre en su ejecución sin importar la sesión de
-- quien lo llame después. Si quien despliega este script tiene
-- QUOTED_IDENTIFIER OFF, cualquier CREATE OR ALTER queda "envenenado" y
-- operaciones como MERGE fallan en tiempo de ejecución (error 1934). Puesto
-- una sola vez aquí protege a TODOS los procedimientos del archivo.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- [sp_Attendance_CalcNightMinutes]

--Minutos nocturnos  --(Distribuye minutos trabajados en [NIGHT_START, NIGHT_END])
CREATE   PROCEDURE HR.sp_Attendance_CalcNightMinutes
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
CREATE   PROCEDURE HR.sp_Attendance_CalculateRange
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

-- [sp_GetAttendanceReport]

CREATE PROCEDURE [HR].[sp_GetAttendanceReport]
    @StartDate DATE,
    @EndDate DATE,
    @EmployeeId INT = NULL,
    @DepartmentId INT = NULL,
    @FacultyId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH AttendanceSummary AS (
        SELECT 
            a.AttendanceDate,
            a.EmployeeId,
            MIN(CASE WHEN a.PunchType = 'In' THEN a.PunchTime END) AS CheckIn,
            MAX(CASE WHEN a.PunchType = 'Out' THEN a.PunchTime END) AS CheckOut,
            a.AttendanceType
        FROM [HR].[tbl_Attendance] a
        WHERE a.AttendanceDate BETWEEN @StartDate AND @EndDate
        GROUP BY a.AttendanceDate, a.EmployeeId, a.AttendanceType
    )
    SELECT 
        a.AttendanceDate,
        e.Id AS EmployeeId,
        CONCAT(p.FirstName, ' ', p.LastName) AS EmployeeName,
        p.IdNumber AS IdentificationNumber,
        d.Name AS DepartmentName,
        f.Name AS FacultyName,
        a.CheckIn,
        a.CheckOut,
        CASE 
            WHEN a.CheckIn IS NOT NULL AND a.CheckOut IS NOT NULL THEN
                CAST(DATEDIFF(MINUTE, a.CheckIn, a.CheckOut) / 60.0 AS DECIMAL(10,2))
            ELSE NULL
        END AS HoursWorked,
        a.AttendanceType,
        CASE 
            WHEN a.CheckIn IS NULL THEN 'Sin Entrada'
            WHEN a.CheckOut IS NULL THEN 'Sin Salida'
            WHEN CAST(a.CheckIn AS TIME) > '08:30:00' THEN 'Tardanza'
            ELSE 'Normal'
        END AS Status
    FROM AttendanceSummary a
    INNER JOIN [HR].[tbl_Employees] e ON a.EmployeeId = e.Id
    INNER JOIN [HR].[tbl_People] p ON e.PersonId = p.Id
    LEFT JOIN [HR].[tbl_Departments] d ON e.DepartmentId = d.Id
    LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    WHERE 
        (@EmployeeId IS NULL OR a.EmployeeId = @EmployeeId)
        AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        AND (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
    ORDER BY a.AttendanceDate DESC, EmployeeName;
END

GO

-- [sp_GetDepartmentsReport]

CREATE PROCEDURE [HR].[sp_GetDepartmentsReport]
    @FacultyId INT = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        d.Id,
        d.Name AS DepartmentName,
        d.Code AS DepartmentCode,
        f.Name AS FacultyName,
        f.Code AS FacultyCode,
        d.IsActive,
        COUNT(DISTINCT e.Id) AS TotalEmployees,
        COUNT(DISTINCT CASE WHEN e.IsActive = 1 THEN e.Id END) AS ActiveEmployees,
        COUNT(DISTINCT CASE WHEN e.IsActive = 0 THEN e.Id END) AS InactiveEmployees,
        ISNULL(AVG(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS AverageSalary,
        ISNULL(SUM(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS TotalSalaries,
        ISNULL(MIN(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS MinSalary,
        ISNULL(MAX(CASE WHEN c.IsActive = 1 THEN c.BaseSalary END), 0) AS MaxSalary,
        d.CreatedAt,
        d.UpdatedAt
    FROM [HR].[tbl_Departments] d
    LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    LEFT JOIN [HR].[tbl_Employees] e ON d.Id = e.DepartmentId
    LEFT JOIN [HR].[tbl_Contracts] c ON e.Id = c.EmployeeId AND c.IsActive = 1
    WHERE 
        (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
        AND (@IncludeInactive = 1 OR d.IsActive = 1)
    GROUP BY 
        d.Id, d.Name, d.Code, f.Name, f.Code, d.IsActive, d.CreatedAt, d.UpdatedAt
    ORDER BY f.Name, d.Name;
END

GO

-- [sp_GetEmployeesReport]
CREATE OR ALTER PROCEDURE [HR].[sp_GetEmployeesReport]
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @DepartmentId INT = NULL,
    @FacultyId INT = NULL,
    -- 2026-07-03: era NVARCHAR(50) pero e.EmployeeType es INT (TypeID de
    -- ref_Types, Category='CONTRACT_TYPE'). El tipo suelto ocultaba que el
    -- DTO del frontend mandaba el valor bajo un campo string equivocado
    -- (mismo bug que en sp_GetReportAttendanceSumary). Corregido a INT para
    -- que coincida con la columna real, sin depender de conversión implícita.
    @EmployeeType INT = NULL,
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.EmployeeId,
        CONCAT(p.FirstName, ' ', p.LastName) AS FullName,
        p.FirstName,
        --p.MiddleName,
        p.LastName,
        p.IDCard AS IdentificationNumber,
        e.Email,
        d.Name AS DepartmentName,
        d.Code AS DepartmentCode,
        --f.Name AS FacultyName,
        e.EmployeeType AS EmployeeType,
        e.IsActive,
        --ISNULL(c.BaseSalary, 0) AS BaseSalary,
        --ISNULL(c.NetSalary, 0) AS NetSalary,
        c.ContractTypeID,
        c.StartDate AS ContractStartDate,
        c.EndDate AS ContractEndDate,
        e.HireDate,
        e.CreatedAt,
        e.UpdatedAt
    FROM [HR].[tbl_Employees] e
    INNER JOIN [HR].[tbl_People] p ON e.PersonId = p.PersonId
    LEFT JOIN [HR].[tbl_Departments] d ON e.DepartmentId = d.DepartmentId
    --LEFT JOIN [HR].[tbl_Faculties] f ON d.FacultyId = f.Id
    LEFT JOIN (
        SELECT PersonID, --BaseSalary, 
                ContractTypeID, StartDate, EndDate
        FROM [HR].[tbl_Contracts]
        --WHERE IsActive = 1
    ) c ON e.PersonID = c.personid
    WHERE 
        (@StartDate IS NULL OR e.HireDate >= @StartDate)
        AND (@EndDate IS NULL OR e.HireDate <= @EndDate)
        AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
        --AND (@FacultyId IS NULL OR d.FacultyId = @FacultyId)
        AND (@EmployeeType IS NULL OR e.EmployeeType = @EmployeeType)
        AND (@IsActive IS NULL OR e.IsActive = @IsActive)
    ORDER BY d.Name, FullName;
END

GO

-- [sp_GetReportAttendanceSumary]

CREATE   PROCEDURE [HR].[sp_GetReportAttendanceSumary]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @EmployeeID INT = NULL,
    @EmployeeType INT = NULL
	--@DepartmentId INT = NULL
    
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT tac.EmployeeID,
		ved.IDCard		,
		concat(ved.FirstName, ved.LastName) NombreCompleto,
		ved.EmployeeType,
		ved.ContractType,		
		tac.WorkDate, 
		tac.TotalWorkedMinutes, 
		tac.RegularMinutes,  
		tac.OvertimeMinutes, 
		tac.NightMinutes,
		tac.HolidayMinutes AS MinFeriado, 
		tac.RequiredMinutes AS MinTotLaboral, 
		tac.TardinessMin AS Atrazos,
		tac.FoodSubsidy AS Alimentacion, 
		tac.JustificationMinutes AS MinJustificacion
	FROM hr.tbl_AttendanceCalculations tac
	INNER JOIN Hr.vw_EmployeeDetails ved ON (tac.EmployeeID = ved.EmployeeID)
    WHERE 
		(@StartDate IS NULL OR tac.WorkDate >= @StartDate)
        AND (@EndDate IS NULL OR tac.WorkDate <= @EndDate)        
        AND (@EmployeeID IS NULL OR tac.EmployeeID = @EmployeeID)
		AND (@EmployeeType IS NULL OR ved.EmployeeType = @EmployeeType)
		--AND (@DepartmentId IS NULL OR tac.DepartmentId = @DepartmentId)
    ORDER BY tac.WorkDate DESC;
END

GO

-- [sp_GetReportAudits]

CREATE PROCEDURE [HR].[sp_GetReportAudits]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @ReportType NVARCHAR(50) = NULL,
    @UserId UNIQUEIDENTIFIER = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@Top)
        Id,
        UserId,
        UserEmail,
        ReportType,
        ReportFormat,
        FiltersApplied,
        GeneratedAt,
        FileSizeBytes,
        GenerationTimeMs,
        ClientIp,
        Success,
        ErrorMessage,
        FileName
    FROM [HR].[tbl_ReportAudit]
    WHERE 
        (@StartDate IS NULL OR GeneratedAt >= @StartDate)
        AND (@EndDate IS NULL OR GeneratedAt <= @EndDate)
        AND (@ReportType IS NULL OR ReportType = @ReportType)
        AND (@UserId IS NULL OR UserId = @UserId)
    ORDER BY GeneratedAt DESC;
END

GO

-- [sp_hr_AccrueVacationBalance]
/* ============================================================
   STORED PROCEDURE: Acreditar Vacaciones (TOTAL / MONTHLY / DAILY)

   MEJORAS IMPLEMENTADAS (este script):
   ✅ Control anti-duplicación por SourceID (TOTAL/MONTHLY/DAILY)
   ✅ DAILY bloqueado si ya existe MONTHLY del mes (evita doble acreditación)
   ✅ Locks (UPDLOCK, HOLDLOCK) en validaciones de existencia (reduce race conditions)
   ✅ Mantiene tu lógica actual y tus mensajes

   MODOS:
   - TOTAL: Setup inicial, recalcula desde HireDate
   - MONTHLY: Acreditación mensual estándar (recomendado)
   - DAILY: Catch-up, pero NO permitido si ya se acreditó el mes con MONTHLY

   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_AccrueVacationBalance
(
    @EmployeeID INT,
    @AsOfDate DATE = NULL,
    @Mode VARCHAR(10) = 'MONTHLY',
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        -- Variables de empleado
        @HireDate DATE,
        @EmployeeName NVARCHAR(50),

        -- Parámetros de vacaciones
        @VacationPerYear DECIMAL(10,2),
        @MinutesPerDay INT,

        -- Variables de cálculo
        @Delta INT,
        @TotalEarnedMinutes INT,
        @AlreadyCredited INT,
        @DailyEarnedMinutes DECIMAL(18,6),
        @MonthlyEarnedMinutes DECIMAL(18,6),

        -- Control de ejecución
        @SourceID NVARCHAR(128),
        @LastAccrualDate DATE,
        @LastAccrualModule NVARCHAR(50),
        @DaysPending INT,
        @MonthsPending INT,
        @DaysInMonth INT,

        -- Control transaccional
        @StartedTran BIT = 0,
        @SavepointName NVARCHAR(128) = 'sp_AccrueVacation_' + CAST(NEWID() AS NVARCHAR(36)),

        -- Tope de acumulación (2026-07-22)
        @MaxAccumulationPeriods INT,
        @CapMinutes INT,
        @CurrentBalanceForCap INT;

    -- Inicialización
    SET @AsOfDate = ISNULL(@AsOfDate, CAST(GETDATE() AS DATE));
    SET @StatusCode = 0;
    SET @Message = N'';
    SET @Delta = 0;

    BEGIN TRY
        -- ============================================================
        -- 1. INICIAR TRANSACCIÓN
        -- ============================================================
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran = 1;
            BEGIN TRANSACTION;
        END
        ELSE
        BEGIN
            SAVE TRANSACTION @SavepointName;
        END

        -- ============================================================
        -- 2. VALIDACIONES BÁSICAS
        -- ============================================================
        IF UPPER(@Mode) NOT IN ('TOTAL', 'MONTHLY', 'DAILY')
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'Modo inválido. Use: TOTAL, MONTHLY o DAILY';
            GOTO ErrorExit;
        END

        SELECT
            @HireDate = HireDate,
            @EmployeeName = LEFT(FirstName + ' ' + LastName, 50)
        FROM HR.vw_EmployeeDetails ved WITH (UPDLOCK)
        WHERE EmployeeID = @EmployeeID;

        IF @HireDate IS NULL
        BEGIN
            SET @StatusCode = 404;
            SET @Message = LEFT(N'Empleado no existe o inactivo (ID:' + CAST(@EmployeeID AS NVARCHAR(10)) + N')', 500);
            GOTO ErrorExit;
        END

        IF @HireDate > @AsOfDate
        BEGIN
            SET @StatusCode = 400;
            SET @Message = LEFT(
                N'HireDate >' + CONVERT(NVARCHAR(10), @HireDate, 120) + N' > AsOfDate',
                500
            );
            GOTO ErrorExit;
        END

        EXEC HR.sp_hr_GetVacationParams
            @VacationPerYear = @VacationPerYear OUTPUT,
            @MinutesPerDay   = @MinutesPerDay OUTPUT;

        IF @VacationPerYear IS NULL OR @MinutesPerDay IS NULL
        BEGIN
            SET @StatusCode = 500;
            SET @Message = N'Error al obtener parámetros de vacaciones';
            GOTO ErrorExit;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID = @EmployeeID;

        -- Tope de acumulación institucional (2026-07-22, ver hr.TBL_PARAMETERS
        -- 'VACATION_MAX_ACCUMULATION_PERIODS'): el saldo de vacaciones no debe
        -- superar N períodos anuales completos del derecho vigente del empleado.
        -- Se calcula una sola vez aquí y se aplica en los 3 modos (TOTAL/MONTHLY/DAILY).
        SELECT @MaxAccumulationPeriods = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_MAX_ACCUMULATION_PERIODS' AND IsActive = 1;
        IF @MaxAccumulationPeriods IS NULL OR @MaxAccumulationPeriods <= 0
            SET @MaxAccumulationPeriods = 2;

        SET @CapMinutes = CAST(ROUND(@MaxAccumulationPeriods * @VacationPerYear * @MinutesPerDay, 0) AS INT);

        -- ============================================================
        -- 3. OBTENER ÚLTIMA ACREDITACIÓN (CUALQUIER MODO)
        -- ============================================================
        SELECT TOP 1
            @LastAccrualDate = CAST(MovementAt AS DATE),
            @LastAccrualModule = SourceModule
        FROM HR.tbl_TimeBalanceMovements WITH (NOLOCK)
        WHERE EmployeeID = @EmployeeID
          AND SourceModule IN ('VACATION_ACCRUAL_TOTAL', 'VACATION_ACCRUAL_MONTHLY', 'VACATION_ACCRUAL_DAILY')
        ORDER BY MovementAt DESC;

        -- ============================================================
        -- 4. MODO TOTAL
        -- ============================================================
        IF UPPER(@Mode) = 'TOTAL'
        BEGIN
            SET @SourceID = 'VAC_TOTAL|' + CONVERT(VARCHAR(8), @AsOfDate, 112);

            -- Anti-duplicación por SourceID (con lock)
            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_TOTAL'
                  AND SourceID = @SourceID
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(N'TOTAL ya ejecutado: ' + CONVERT(NVARCHAR(10), @AsOfDate, 120), 500);
                GOTO ErrorExit;
            END

            -- 3.1: redondeo consistente con MONTHLY/DAILY (antes truncaba con CAST directo)
            SET @TotalEarnedMinutes = CAST(
                ROUND(
                    (DATEDIFF(DAY, @HireDate, @AsOfDate) / 365.25) *
                    @VacationPerYear *
                    @MinutesPerDay,
                    0
                )
                AS INT
            );

            SELECT @AlreadyCredited = ISNULL(SUM(DeltaVacationMin), 0)
            FROM HR.tbl_TimeBalanceMovements WITH (NOLOCK)
            WHERE EmployeeID = @EmployeeID
              AND SourceModule IN ('VACATION_ACCRUAL_TOTAL', 'VACATION_ACCRUAL_MONTHLY', 'VACATION_ACCRUAL_DAILY');

            SET @Delta = @TotalEarnedMinutes - @AlreadyCredited;

            IF @Delta <= 0
            BEGIN
                SET @StatusCode = 204;
                SET @Message = LEFT(
                    N'TOTAL: Sin delta. Teórico=' + CAST(@TotalEarnedMinutes AS NVARCHAR(50)) +
                    N' YaAcred=' + CAST(@AlreadyCredited AS NVARCHAR(50)),
                    500
                );
                GOTO SuccessExit;
            END

            -- Tope de acumulación: nunca acredita por encima de @CapMinutes
            -- (no resta saldo ya existente aunque esté por encima, solo detiene
            -- la acreditación de este período).
            SELECT @CurrentBalanceForCap = VacationAvailableMin
            FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;
            SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
            IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
                SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

            -- 2026-07-06: acumulación de vacaciones LOSEP siempre (57) — LOES
            -- se calcula distinto (jornada por dedicación académica), ver
            -- procedimientos LOES pendientes de diseño con la regla confirmada.
            UPDATE HR.tbl_TimeBalances
            SET VacationAvailableMin = VacationAvailableMin + @Delta,
                LastUpdated = GETDATE()
            WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;

            INSERT INTO HR.tbl_TimeBalanceMovements
            (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
             SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
            VALUES
            (@EmployeeID, @Delta, 0, GETDATE(),
             'VACATION_ACCRUAL_TOTAL', 'CALC', @SourceID, @PerformedByEmpID,
             LEFT(
                 N'[TOTAL] ' + @EmployeeName +
                 N' | ' + CONVERT(NVARCHAR(10), @HireDate, 120) + N'->' + CONVERT(NVARCHAR(10), @AsOfDate, 120) +
                 N' | Teórico:' + CAST(@TotalEarnedMinutes AS NVARCHAR(50)) +
                 N' YaAcred:' + CAST(@AlreadyCredited AS NVARCHAR(50)) +
                 N' Delta:+' + CAST(@Delta AS NVARCHAR(50)) +
                 N' | ' + CAST(@VacationPerYear AS NVARCHAR(50)) + N'd/año',
                 400
             ),
             57
            );

            SET @StatusCode = 200;
            SET @Message = LEFT(
                N'✓ TOTAL: +' + CAST(@Delta AS NVARCHAR(50)) + N' min (' +
                CAST(CAST(@Delta AS DECIMAL(18,2)) / NULLIF(@MinutesPerDay,0) AS NVARCHAR(50)) + N' días)',
                500
            );
            GOTO SuccessExit;
        END

        -- ============================================================
        -- 5. MODO MONTHLY
        -- ============================================================
        IF UPPER(@Mode) = 'MONTHLY'
        BEGIN
            DECLARE @Year INT = YEAR(@AsOfDate);
            DECLARE @Month INT = MONTH(@AsOfDate);

            SET @SourceID = 'VAC_MONTHLY|' +
                           CAST(@Year AS VARCHAR(4)) +
                           RIGHT('0' + CAST(@Month AS VARCHAR(2)), 2);

            -- Anti-duplicación por SourceID (más consistente que MovementAt) + lock
            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_MONTHLY'
                  AND SourceID = @SourceID
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(
                    N'Ya existe acred. MONTHLY para ' + CAST(@Year AS NVARCHAR(4)) + N'-' + RIGHT('0' + CAST(@Month AS NVARCHAR(2)), 2),
                    500
                );
                GOTO ErrorExit;
            END

            -- Si quieres bloquear MONTHLY cuando ya hubo DAILY en ese mes (para evitar “doble” al revés)
            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_DAILY'
                  AND SourceID LIKE 'VAC_DAILY|' + CAST(@Year AS VARCHAR(4)) + RIGHT('0' + CAST(@Month AS VARCHAR(2)), 2) + '%'
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(
                    N'No se puede MONTHLY: ya existen acreditaciones DAILY en el mes ' +
                    CAST(@Year AS NVARCHAR(4)) + N'-' + RIGHT('0' + CAST(@Month AS NVARCHAR(2)), 2),
                    500
                );
                GOTO ErrorExit;
            END

            IF @Year < YEAR(@HireDate) OR (@Year = YEAR(@HireDate) AND @Month < MONTH(@HireDate))
            BEGIN
                SET @StatusCode = 400;
                SET @Message = LEFT(
                    N'Periodo inválido. HireDate: ' + CONVERT(NVARCHAR(10), @HireDate, 120),
                    500
                );
                GOTO ErrorExit;
            END

            /* ============================================================
               PUNTO 3.4 — Liquidación proporcional al dar de baja a mitad
               de mes. Se busca el último contrato del empleado cuyo
               EndDate cae dentro de @Year/@Month y que NO tiene un
               addendum (ParentID) que lo extienda más allá de esa fecha —
               misma verificación de "contrato realmente terminado" que ya
               usa ContractExpirationService.ProcessExpiredContractsAsync
               (Application/Services/ContractExpirationService.cs), para no
               reinventar la regla. Si el empleado tuviera más de un
               contrato "terminal" con EndDate en el mismo mes (caso raro:
               múltiples contratos simultáneos), se toma el de EndDate más
               reciente — simplificación deliberada; no existe una regla de
               negocio explícita para desempatar varios contratos
               terminales simultáneos en el mismo mes.
            ============================================================ */
            DECLARE @PeriodStart DATE = DATEFROMPARTS(@Year, @Month, 1);
            DECLARE @PeriodEnd   DATE = EOMONTH(@PeriodStart);
            DECLARE @TerminationDate DATE = NULL;

            SELECT TOP 1 @TerminationDate = CAST(c.EndDate AS DATE)
            FROM HR.tbl_Contracts c
            INNER JOIN HR.tbl_Employees e ON e.PersonID = c.PersonID
            WHERE e.EmployeeID = @EmployeeID
              AND CAST(c.EndDate AS DATE) BETWEEN @PeriodStart AND @PeriodEnd
              AND NOT EXISTS (
                  SELECT 1 FROM HR.tbl_Contracts a
                  WHERE a.ParentID = c.ContractID
                    AND a.EndDate >= c.EndDate
              )
            ORDER BY c.EndDate DESC;

            -- Recorta el período al rango realmente trabajado dentro del mes:
            -- desde HireDate si contrató a mitad de mes, hasta TerminationDate
            -- si terminó a mitad de mes (cubre también el caso de contratar
            -- y terminar dentro del mismo mes calendario).
            IF (@HireDate > @PeriodStart) SET @PeriodStart = @HireDate;
            IF (@TerminationDate IS NOT NULL AND @TerminationDate < @PeriodEnd) SET @PeriodEnd = @TerminationDate;

            -- Freno adicional: si existe una solicitud de renuncia/jubilación viva (no
            -- RECHAZADO ni ANULADO) con fecha de salida propuesta dentro o antes del
            -- período, se detiene la acreditación desde esa fecha — sin esperar a que
            -- se suba el documento firmado ni se cierre el régimen (ese trámite puede
            -- demorar más de lo que tarda en llegar el próximo corte mensual).
            DECLARE @ResignationExitDate DATE = NULL;
            SELECT TOP 1 @ResignationExitDate = rr.ProposedExitDate
            FROM HR.tbl_ResignationRetirementRequests rr
            WHERE rr.EmployeeID = @EmployeeID
              AND rr.Status NOT IN ('RECHAZADO', 'ANULADO')
              AND rr.ProposedExitDate <= @PeriodEnd
            ORDER BY rr.ProposedExitDate ASC;

            IF (@ResignationExitDate IS NOT NULL AND @ResignationExitDate < @PeriodEnd) SET @PeriodEnd = @ResignationExitDate;

            SET @DaysInMonth = DATEDIFF(DAY, @PeriodStart, @PeriodEnd) + 1;
            IF @DaysInMonth < 0 SET @DaysInMonth = 0;

            SET @MonthlyEarnedMinutes = (@DaysInMonth / 365.25) * @VacationPerYear * @MinutesPerDay;
            SET @Delta = CAST(ROUND(@MonthlyEarnedMinutes, 0) AS INT);

            IF @Delta <= 0
            BEGIN
                SET @StatusCode = 400;
                SET @Message = N'MONTHLY: Delta calculado es 0. Revise parámetros.';
                GOTO ErrorExit;
            END

            -- Tope de acumulación (ver nota en modo TOTAL).
            SELECT @CurrentBalanceForCap = VacationAvailableMin
            FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;
            SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
            IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
                SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

            -- 2026-07-06: LOSEP siempre (57) — ver nota en modo TOTAL.
            UPDATE HR.tbl_TimeBalances
            SET VacationAvailableMin = VacationAvailableMin + @Delta,
                LastUpdated = GETDATE()
            WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;

            INSERT INTO HR.tbl_TimeBalanceMovements
            (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
             SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
            VALUES
            (@EmployeeID, @Delta, 0, GETDATE(),
             'VACATION_ACCRUAL_MONTHLY', 'CALC', @SourceID, @PerformedByEmpID,
             LEFT(
                 N'[MONTHLY] ' + @EmployeeName +
                 N' | ' + LEFT(DATENAME(MONTH, @AsOfDate), 3) + N'-' + CAST(@Year AS NVARCHAR(4)) +
                 N' | Días:' + CAST(@DaysInMonth AS NVARCHAR(10)) +
                 N' Acred:+' + CAST(@Delta AS NVARCHAR(50)) +
                 N' (' + CAST(CAST(@Delta AS DECIMAL(18,2)) / NULLIF(@MinutesPerDay,0) AS NVARCHAR(50)) + N'd)',
                 400
             ),
             57
            );

            SET @StatusCode = 200;
            SET @Message = LEFT(
                N'✓ MONTHLY ' + LEFT(DATENAME(MONTH, @AsOfDate), 3) + N'-' +
                CAST(@Year AS NVARCHAR(4)) + N': +' + CAST(@Delta AS NVARCHAR(50)) + N' min',
                500
            );
            GOTO SuccessExit;
        END

        -- ============================================================
        -- 6. MODO DAILY
        -- ============================================================
        IF UPPER(@Mode) = 'DAILY'
        BEGIN
            DECLARE @YearD INT = YEAR(@AsOfDate);
            DECLARE @MonthD INT = MONTH(@AsOfDate);
            DECLARE @MonthlySourceID NVARCHAR(128) =
                'VAC_MONTHLY|' + CAST(@YearD AS VARCHAR(4)) + RIGHT('0' + CAST(@MonthD AS VARCHAR(2)), 2);

            SET @SourceID = 'VAC_DAILY|' + CONVERT(VARCHAR(8), @AsOfDate, 112);

            -- 1) Anti-duplicación del día (por SourceID) + lock
            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_DAILY'
                  AND SourceID = @SourceID
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(N'DAILY ya ejecutado: ' + CONVERT(NVARCHAR(10), @AsOfDate, 120), 500);
                GOTO ErrorExit;
            END

            -- 2) Bloquear DAILY si ya existe MONTHLY de ese mes (evita doble acreditación)
            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_MONTHLY'
                  AND SourceID = @MonthlySourceID
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(
                    N'DAILY bloqueado: ya existe MONTHLY para el mes ' +
                    CAST(@YearD AS NVARCHAR(4)) + N'-' + RIGHT('0' + CAST(@MonthD AS NVARCHAR(2)), 2),
                    500
                );
                GOTO ErrorExit;
            END

            SET @LastAccrualDate = ISNULL(@LastAccrualDate, DATEADD(DAY, -1, @HireDate));
            SET @DaysPending = DATEDIFF(DAY, @LastAccrualDate, @AsOfDate);

            IF @DaysPending <= 0
            BEGIN
                SET @StatusCode = 204;
                SET @Message = LEFT(
                    N'DAILY: Sin días pend. Última: ' + CONVERT(NVARCHAR(10), @LastAccrualDate, 120),
                    500
                );
                GOTO SuccessExit;
            END

            SET @DailyEarnedMinutes = (@VacationPerYear / 365.25) * @MinutesPerDay;
            SET @Delta = CAST(ROUND(@DailyEarnedMinutes * @DaysPending, 0) AS INT);

            IF @Delta <= 0
            BEGIN
                SET @StatusCode = 400;
                SET @Message = N'DAILY: Delta calculado es 0. Revise parámetros.';
                GOTO ErrorExit;
            END

            -- Tope de acumulación (ver nota en modo TOTAL).
            SELECT @CurrentBalanceForCap = VacationAvailableMin
            FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;
            SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
            IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
                SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

            -- 2026-07-06: LOSEP siempre (57) — ver nota en modo TOTAL.
            UPDATE HR.tbl_TimeBalances
            SET VacationAvailableMin = VacationAvailableMin + @Delta,
                LastUpdated = GETDATE()
            WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;

            INSERT INTO HR.tbl_TimeBalanceMovements
            (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
             SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
            VALUES
            (@EmployeeID, @Delta, 0, GETDATE(),
             'VACATION_ACCRUAL_DAILY', 'CALC', @SourceID, @PerformedByEmpID,
             LEFT(
                 N'[DAILY] ' + @EmployeeName +
                 N' | ' + CONVERT(NVARCHAR(10), @AsOfDate, 120) +
                 N' | Días:' + CAST(@DaysPending AS NVARCHAR(10)) +
                 N' Desde:' + CONVERT(NVARCHAR(10), @LastAccrualDate, 120) +
                 N' | +' + CAST(@Delta AS NVARCHAR(50)) + N'min',
                 400
             ),
             57
            );

            SET @StatusCode = 200;
            SET @Message = LEFT(
                N'✓ DAILY: ' + CAST(@DaysPending AS NVARCHAR(10)) + N' días, +' +
                CAST(@Delta AS NVARCHAR(50)) + N' min',
                500
            );
            GOTO SuccessExit;
        END

        -- ============================================================
        -- 7. COMMIT EXITOSO
        -- ============================================================
        SuccessExit:
        IF @StartedTran = 1
        BEGIN
            COMMIT TRANSACTION;
        END
        RETURN;

        -- ============================================================
        -- 8. ROLLBACK POR ERROR LÓGICO
        -- ============================================================
        ErrorExit:
        IF @StartedTran = 1
        BEGIN
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;
        END
        ELSE
        BEGIN
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION @SavepointName;
        END
        RETURN;

    END TRY
    BEGIN CATCH
        DECLARE @ErrorNumber INT = ERROR_NUMBER();
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorLine INT = ERROR_LINE();

        IF XACT_STATE() = -1
        BEGIN
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;
        END
        ELSE IF XACT_STATE() = 1
        BEGIN
            IF @StartedTran = 1
            BEGIN
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION @SavepointName;
            END
        END

        SET @StatusCode = @ErrorNumber;
        SET @Message = LEFT(
            N'ERR[' + CAST(@ErrorNumber AS NVARCHAR(10)) + N'] L' +
            CAST(@ErrorLine AS NVARCHAR(10)) + N': ' + LEFT(@ErrorMessage, 400),
            500
        );

        THROW;
    END CATCH
END

GO

-- [sp_hr_ConsumeReservation]

/* ============================================================
   OBSOLETA (migrada 2026-07-22): VacationsService/PermissionsService ya no la llaman —
   usan IVacationBalanceAdjustmentService.MarkReservationConsumedAsync (EF Core). Se deja
   sin borrar por si algo externo la invoca directamente, pero no forma parte del flujo
   normal del sistema. Ver HrBalanceRepository.ConsumeReservationAsync ([Obsolete] en C#).

   7) SP: Consumir reserva (APROBAR) - auditoría
   - CORREGIDO: transaction-safe
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_ConsumeReservation
(
    @ReserveSourceID NVARCHAR(128),
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmployeeID INT, @StartedTran BIT=0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_ConsumeReservation;
        END

        SELECT @EmployeeID = EmployeeID
        FROM HR.tbl_TimeBalanceMovements
        WHERE SourceID = @ReserveSourceID;

        IF @EmployeeID IS NULL
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'No existe reserva: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ConsumeReservation;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@ReserveSourceID + N'|USE')
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya consumida: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ConsumeReservation;
            RETURN;
        END

        -- 2026-07-06: LOSEP siempre (57) — este mecanismo de reserva/consumo
        -- es exclusivo de LOSEP, ver nota en sp_hr_AccrueVacationBalance.
        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, 0, 0,
            GETDATE(), 'RESERVATION_CONSUME', 'SYSTEM', @ReserveSourceID + N'|USE',
            @PerformedByEmpID,
            N'Consumo (aprobación) de reserva: ' + @ReserveSourceID,
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Reserva consumida (audit) OK';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_ConsumeReservation;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_DebitRecoveryBalance]

/* ============================================================
   10) SP: Debitar recuperación (pago)
   - (ajustado a transaction-safe)
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_DebitRecoveryBalance
(
    @RecoveryLogID INT,
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeID INT,
        @MinutesRecovered INT,
        @Balance INT,
        @SourceID NVARCHAR(128),
        @StartedTran BIT=0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_DebitRecoveryBalance;
        END

        SELECT
            @EmployeeID = rp.EmployeeID,
            @MinutesRecovered = rl.MinutesRecovered
        FROM HR.tbl_TimeRecoveryLogs rl
        INNER JOIN HR.tbl_TimeRecoveryPlans rp ON rp.RecoveryPlanID = rl.RecoveryPlanID
        WHERE rl.RecoveryLogID = @RecoveryLogID;

        IF @EmployeeID IS NULL
            RAISERROR('RecoveryLog no existe', 16, 1);

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID=@EmployeeID;

        SET @SourceID = 'RECOVERY_USE|' + CAST(@RecoveryLogID AS NVARCHAR(20));

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@SourceID)
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Recovery ya debitado: ' + @SourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_DebitRecoveryBalance;
            RETURN;
        END

        -- 2026-07-06: LOSEP siempre (57) — tiempo de recuperación (atrasos/
        -- horario fijo) es exclusivo de LOSEP, igual que horas extra (LOES no
        -- tiene horario de oficina fijo contra el cual generar atrasos).
        SELECT @Balance = RecoveryPendingMin
        FROM HR.tbl_TimeBalances
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        IF @Balance < @MinutesRecovered
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'Saldo recuperación insuficiente. Disponible=' + CAST(@Balance AS NVARCHAR(20))
                         + N' min, Requerido=' + CAST(@MinutesRecovered AS NVARCHAR(20)) + N' min';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_DebitRecoveryBalance;
            RETURN;
        END

        UPDATE HR.tbl_TimeBalances
        SET RecoveryPendingMin = RecoveryPendingMin - @MinutesRecovered,
            LastUpdated = GETDATE()
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, 0, -@MinutesRecovered,
            GETDATE(), 'RECOVERY_USE', 'tbl_TimeRecoveryLogs', @SourceID,
            @PerformedByEmpID,
            N'Pago recuperación. -' + CAST(@MinutesRecovered AS NVARCHAR(20)) + N' min',
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Recovery debit OK';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_DebitRecoveryBalance;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_EnsureTimeBalanceRow]

/* ============================================================
   3) SP: Asegurar fila en HR.tbl_TimeBalances
   2026-07-06 (Fase 3, propuesta multi-régimen): ahora asegura una fila POR
   CADA régimen activo del empleado (antes era una sola fila por EmployeeID).
   Un empleado con 2 regímenes simultáneos (ej. nombramiento LOSEP + contrato
   docencia LOES) necesita 2 saldos independientes, no uno mezclado.
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_EnsureTimeBalanceRow
(
    @EmployeeID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO HR.tbl_TimeBalances (EmployeeID, LaborRegimeId, VacationAvailableMin, RecoveryPendingMin)
    SELECT elr.EmployeeId, elr.LaborRegimeId, 0, 0
    FROM HR.tbl_EmployeeLaborRegime elr
    WHERE elr.EmployeeId = @EmployeeID
      AND elr.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM HR.tbl_TimeBalances tb
          WHERE tb.EmployeeID = elr.EmployeeId AND tb.LaborRegimeId = elr.LaborRegimeId
      );

    -- Respaldo: si el empleado no tiene ninguna fila activa en
    -- tbl_EmployeeLaborRegime (dato incompleto), se asegura al menos una fila
    -- con el régimen espejo de tbl_Employees.EmployeeType, para no dejar al
    -- empleado sin ningún saldo por un hueco de datos.
    IF NOT EXISTS (SELECT 1 FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID)
    BEGIN
        INSERT INTO HR.tbl_TimeBalances (EmployeeID, LaborRegimeId, VacationAvailableMin, RecoveryPendingMin)
        SELECT @EmployeeID, ISNULL(e.EmployeeType, 57), 0, 0
        FROM HR.tbl_Employees e
        WHERE e.EmployeeID = @EmployeeID;
    END
END

GO

-- [sp_hr_GetEmployeeBalances]

/* ============================================================
   11) SP: Consultar saldos + últimos movimientos
   2026-07-06 (Fase 3, propuesta multi-régimen): antes devolvía una sola fila
   de saldo por empleado (LEFT JOIN 1:1). Ahora tbl_TimeBalances tiene una
   fila por régimen activo, así que este SELECT devuelve una fila POR
   RÉGIMEN — un empleado con 2 regímenes simultáneos ve sus 2 saldos por
   separado, no uno mezclado. Los movimientos siguen siendo por empleado (la
   columna LaborRegimeId ahí es nullable/histórica, no se filtra por ahora).
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_GetEmployeeBalances
(
    @EmployeeID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MinutesPerDay INT;

    SELECT @MinutesPerDay = CAST(Pvalues AS INT)
    FROM hr.TBL_PARAMETERS
    WHERE name='WORK_MINUTES_PER_DAY' AND IsActive=1;

    SET @MinutesPerDay = ISNULL(@MinutesPerDay, 480);

    SELECT
        e.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        e.HireDate,
        tb.LaborRegimeId,
        rt.Name AS LaborRegimeName,
        tb.IsPrincipal,
        ISNULL(tb.VacationAvailableMin,0) AS VacationMinutes,
        CAST(ISNULL(tb.VacationAvailableMin,0) / CAST(@MinutesPerDay AS DECIMAL(10,2)) AS DECIMAL(10,2)) AS VacationDays,
        ISNULL(tb.RecoveryPendingMin,0) AS RecoveryMinutes,
        CAST(ISNULL(tb.RecoveryPendingMin,0) / 60.0 AS DECIMAL(10,2)) AS RecoveryHours,
        tb.LastUpdated
    FROM HR.tbl_Employees e
    INNER JOIN HR.tbl_People p ON p.PersonID = e.PersonID
    LEFT JOIN (
        SELECT tb.*, elr.IsPrincipal
        FROM HR.tbl_TimeBalances tb
        LEFT JOIN HR.tbl_EmployeeLaborRegime elr
            ON elr.EmployeeId = tb.EmployeeID AND elr.LaborRegimeId = tb.LaborRegimeId AND elr.IsActive = 1
    ) tb ON tb.EmployeeID = e.EmployeeID
    LEFT JOIN HR.ref_Types rt ON rt.TypeId = tb.LaborRegimeId AND rt.Category = 'CONTRACT_TYPE'
    WHERE e.EmployeeID = @EmployeeID
    ORDER BY CASE WHEN tb.IsPrincipal = 1 THEN 0 ELSE 1 END, tb.LaborRegimeId;

    SELECT TOP 20
        MovementID, MovementAt, SourceModule, SourceID,
        DeltaVacationMin, DeltaRecoveryMin,
        Note, PerformedByEmpID
    FROM HR.tbl_TimeBalanceMovements
    WHERE EmployeeID=@EmployeeID
    ORDER BY MovementAt DESC;
END

GO

-- [sp_hr_GetVacationParams]

/* ============================================================
   2) SP: Obtener parámetros del sistema
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_GetVacationParams
(
    @VacationPerYear DECIMAL(10,2) OUTPUT,
    @MinutesPerDay   INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @VacationPerYear = CAST(Pvalues AS DECIMAL(10,2))
    FROM hr.TBL_PARAMETERS
    WHERE name = 'VACATION_PER_YEAR' AND IsActive = 1;

    SELECT @MinutesPerDay = CAST(Pvalues AS INT)
    FROM hr.TBL_PARAMETERS
    WHERE name = 'WORK_MINUTES_PER_DAY' AND IsActive = 1;

    IF @VacationPerYear IS NULL OR @VacationPerYear <= 0
    BEGIN
        RAISERROR('Parametro VACATION_PER_YEAR invalido o no configurado', 16, 1);
        RETURN;
    END

    IF @MinutesPerDay IS NULL OR @MinutesPerDay <= 0
    BEGIN
        RAISERROR('Parametro WORK_MINUTES_PER_DAY invalido o no configurado', 16, 1);
        RETURN;
    END
END

GO

-- [sp_hr_ProcessRecoveryBalance]

/* ============================================================
   9) SP: Procesar deuda de recuperación
   - (Puedes corregirlo igual, pero lo dejo como estaba salvo transaction-safe)
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_ProcessRecoveryBalance
(
    @EmployeeID INT,
    @StartDate DATE,
    @EndDate   DATE,
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NetMovement INT;
    DECLARE @SourceID NVARCHAR(128);
    DECLARE @StartedTran BIT=0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_ProcessRecoveryBalance;
        END

        IF @StartDate > @EndDate
            RAISERROR('StartDate no puede ser mayor a EndDate', 16, 1);

        IF NOT EXISTS (SELECT 1 FROM HR.tbl_Employees WHERE EmployeeID=@EmployeeID AND IsActive=1)
            RAISERROR('Empleado no existe o inactivo', 16, 1);

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID=@EmployeeID;

        SET @SourceID = 'RECOVERY|' + CONVERT(VARCHAR(8),@StartDate,112) + '|' + CONVERT(VARCHAR(8),@EndDate,112);

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@SourceID)
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Periodo ya procesado: ' + @SourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ProcessRecoveryBalance;
            RETURN;
        END

        SELECT
            @NetMovement =
              ISNULL(SUM(
                (ISNULL(AbsentMinutes,0) + ISNULL(TardinessMin,0) + ISNULL(MinutesLate,0))
                - ISNULL(JustificationMinutes,0)
                - ISNULL(recoveredMinutes,0)
              ), 0)
        FROM HR.tbl_AttendanceCalculations
        WHERE EmployeeID=@EmployeeID
          AND WorkDate BETWEEN @StartDate AND @EndDate;

        IF @NetMovement = 0
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'No hay movimiento neto de recuperación en el periodo';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ProcessRecoveryBalance;
            RETURN;
        END

        -- 2026-07-06: LOSEP siempre (57) — ver nota en sp_hr_DebitRecoveryBalance.
        UPDATE HR.tbl_TimeBalances
        SET RecoveryPendingMin = ISNULL(RecoveryPendingMin,0) + @NetMovement,
            LastUpdated = GETDATE()
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, 0, @NetMovement,
            GETDATE(), 'RECOVERY_PROCESS', 'tbl_AttendanceCalculations', @SourceID,
            @PerformedByEmpID,
            N'Procesamiento recuperación. Neto=' + CAST(@NetMovement AS NVARCHAR(20)) + N' min',
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Recovery procesado. Neto=' + CAST(@NetMovement AS NVARCHAR(20)) + N' min';

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_ProcessRecoveryBalance;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_ReleaseReservation]

/* ============================================================
   OBSOLETA (migrada 2026-07-22): VacationsService/PermissionsService ya no la llaman —
   usan IVacationBalanceAdjustmentService.ReleaseReservationAsync (EF Core). Esta SP
   además tenía el régimen 57 (LOSEP) hardcodeado — confirmado con prueba real que no
   liberaba nada para empleados de Código de Trabajo/LOES. Se deja sin borrar por
   trazabilidad histórica, no forma parte del flujo normal del sistema.

   8) SP: Liberar reserva (RECHAZAR / CANCELAR)
   - CORREGIDO: transaction-safe
   - CORREGIDO: no libera si la reserva no existe o no es negativa
   - CORREGIDO: evita doble liberación
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_ReleaseReservation
(
    @ReserveSourceID NVARCHAR(128),
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeID INT,
        @ReservedDelta INT,
        @ReleaseAmount INT,
        @StartedTran BIT = 0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_ReleaseReservation;
        END

        -- La reserva debe existir
        SELECT
            @EmployeeID = EmployeeID,
            @ReservedDelta = DeltaVacationMin
        FROM HR.tbl_TimeBalanceMovements
        WHERE SourceID = @ReserveSourceID;

        IF @EmployeeID IS NULL
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva no encontrada: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReleaseReservation;
            RETURN;
        END

        -- Si ya liberó, no vuelve a liberar
        IF EXISTS (
            SELECT 1
            FROM HR.tbl_TimeBalanceMovements
            WHERE EmployeeID = @EmployeeID
              AND SourceID = @ReserveSourceID + N'|REL'
        )
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya liberada: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReleaseReservation;
            RETURN;
        END

        -- Si ya se consumió (aprobado), NO se debe liberar
        IF EXISTS (
            SELECT 1
            FROM HR.tbl_TimeBalanceMovements
            WHERE EmployeeID = @EmployeeID
              AND SourceID = @ReserveSourceID + N'|USE'
        )
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya consumida (aprobada), no se libera: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReleaseReservation;
            RETURN;
        END

        -- Seguridad: la reserva debería ser negativa. Si no, no liberes para no sumar mal.
        IF @ReservedDelta >= 0
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'Reserva inválida (DeltaVacationMin no es negativo) para: ' + @ReserveSourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReleaseReservation;
            RETURN;
        END

        SET @ReleaseAmount = ABS(@ReservedDelta);

        -- 2026-07-06: LOSEP siempre (57) — ver nota en sp_hr_AccrueVacationBalance.
        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin + @ReleaseAmount,
            LastUpdated = GETDATE()
        WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 57;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, @ReleaseAmount, 0,
            GETDATE(), 'RESERVATION_RELEASE', 'SYSTEM', @ReserveSourceID + N'|REL',
            @PerformedByEmpID,
            N'Liberación de reserva: ' + @ReserveSourceID +
            N'. Devuelto=' + CAST(@ReleaseAmount AS NVARCHAR(20)) + N' min',
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Reserva liberada OK';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_ReleaseReservation;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_ReservePermissionBalance]

/* ============================================================
   OBSOLETA (migrada 2026-07-22): PermissionsService ya no la llama — usa
   IVacationBalanceAdjustmentService.ReserveAsync (EF Core), que resuelve el régimen
   real del empleado en vez de "LOSEP siempre (57)". Confirmado con prueba real:
   para permisos con cargo a vacaciones de empleados de Código de Trabajo/LOES, esta
   SP reportaba éxito pero nunca descontaba el saldo real. Se deja sin borrar por
   trazabilidad histórica.

   5) SP: Reservar PERMISO contra vacaciones
   - CORREGIDO: transaction-safe
   - SourceID estándar: PERM_RESERVE|<PermissionID>
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_ReservePermissionBalance
(
    @PermissionID INT,
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeID INT,
        @StartDate DATETIME2,
        @EndDate DATETIME2,
        @BaseMinutes INT,
        @ChargedMinutes INT,
        @MinutesPerDay INT,
        @Balance INT,
        @Factor DECIMAL(10,4) = (7.0/5.0),
        @SourceID NVARCHAR(128),
        @StartedTran BIT = 0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_ReservePermissionBalance;
        END

        SELECT @MinutesPerDay = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS
        WHERE name='WORK_MINUTES_PER_DAY' AND IsActive=1;

        IF @MinutesPerDay IS NULL OR @MinutesPerDay <= 0
            RAISERROR('WORK_MINUTES_PER_DAY invalido', 16, 1);

        SELECT
            @EmployeeID = EmployeeID,
            @StartDate = StartDate,
            @EndDate = EndDate,
            @BaseMinutes =
                CASE
                    WHEN HourTaken IS NOT NULL THEN CAST(HourTaken AS INT)
                    ELSE HR.fn_hr_CountWorkingDays(CAST(StartDate AS DATE), CAST(EndDate AS DATE)) * @MinutesPerDay
                END
        FROM HR.tbl_Permissions
        WHERE PermissionID=@PermissionID
          AND ChargedToVacation=1
          AND Status='Pending';

        IF @EmployeeID IS NULL
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Permiso no requiere reserva (o no está Pending / ChargedToVacation=0)';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReservePermissionBalance;
            RETURN;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID=@EmployeeID;

        SET @ChargedMinutes = CAST(CEILING(@BaseMinutes * @Factor) AS INT);
        SET @SourceID = 'PERM_RESERVE|' + CAST(@PermissionID AS NVARCHAR(20));

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@SourceID)
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya existe: ' + @SourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReservePermissionBalance;
            RETURN;
        END

        -- 2026-07-06: LOSEP siempre (57) — tbl_Permissions no distingue régimen
        -- todavía, y este mecanismo de reserva es LOSEP por ahora (ver nota en
        -- sp_hr_AccrueVacationBalance).
        SELECT @Balance = VacationAvailableMin
        FROM HR.tbl_TimeBalances
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        IF @Balance < @ChargedMinutes
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'Saldo insuficiente. Disponible=' + CAST(@Balance AS NVARCHAR(20))
                         + N' min, Requerido=' + CAST(@ChargedMinutes AS NVARCHAR(20)) + N' min';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReservePermissionBalance;
            RETURN;
        END

        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin - @ChargedMinutes,
            LastUpdated = GETDATE()
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, -@ChargedMinutes, 0,
            GETDATE(), 'PERMISSION_RESERVE', 'tbl_Permissions', @SourceID,
            @PerformedByEmpID,
            N'Reserva permiso. BaseMin=' + CAST(@BaseMinutes AS NVARCHAR(20)) +
            N' Factor(7/5)=' + CAST(@Factor AS NVARCHAR(20)) +
            N' CobradoMin=' + CAST(@ChargedMinutes AS NVARCHAR(20)) +
            N' Rango=' + CONVERT(VARCHAR(19), @StartDate, 120) + N' a ' + CONVERT(VARCHAR(19), @EndDate, 120),
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Reserva permiso OK. Cobrado=' + CAST(@ChargedMinutes AS NVARCHAR(20)) + N' min';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_ReservePermissionBalance;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_ReserveVacationBalance]
/* ============================================================
   OBSOLETA (migrada 2026-07-22): VacationsService ya no la llama — usa
   IVacationBalanceAdjustmentService.ReserveAsync (EF Core), que resuelve el régimen
   real del empleado en vez de "LOSEP siempre (57)" (línea de abajo,
   "WHERE ... AND LaborRegimeId = 57"). Confirmado con prueba real (empleado 5409,
   Código de Trabajo): esta SP reportaba "Reserva vacaciones OK" pero el saldo real
   (régimen 59) nunca cambiaba — el UPDATE afectaba 0 filas porque buscaba régimen 57,
   que no existe para ese empleado. Se deja sin borrar por trazabilidad histórica.
   ============================================================ */
CREATE OR ALTER PROCEDURE HR.sp_hr_ReserveVacationBalance
(
    @VacationID INT,
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeID INT,
        @StartDate DATE,
        @EndDate DATE,
        @WorkingDays INT,
        @FullWeeks INT,
        @ChargedDays INT,
        @MinutesPerDay INT,
        @ChargedMinutes INT,
        @Balance INT,
        @SourceID NVARCHAR(128),
        @StartedTran BIT = 0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN sp_hr_ReserveVacationBalance;
        END

        SELECT @MinutesPerDay = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS
        WHERE name='WORK_MINUTES_PER_DAY' AND IsActive=1;

        IF @MinutesPerDay IS NULL OR @MinutesPerDay <= 0
            RAISERROR('WORK_MINUTES_PER_DAY invalido', 16, 1);

        SELECT
            @EmployeeID = EmployeeID,
            @StartDate = StartDate,
            @EndDate = EndDate
        FROM HR.tbl_Vacations
        WHERE VacationID=@VacationID
          AND Status='Planned';

        IF @EmployeeID IS NULL
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Vacación no existe o no está Planned';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReserveVacationBalance;
            RETURN;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID=@EmployeeID;

        -- 3.3: días calendario reales del período (proporcional a cada día,
        -- sin depender del día de la semana en que arranca la vacación).
        -- @WorkingDays/@FullWeeks se conservan solo para el texto informativo
        -- de auditoría (Note más abajo), ya no participan del cálculo de
        -- minutos cobrados.
        SET @WorkingDays = HR.fn_hr_CountWorkingDays(@StartDate, @EndDate);
        SET @FullWeeks = @WorkingDays / 5;
        SET @ChargedDays = DATEDIFF(DAY, @StartDate, @EndDate) + 1;

        SET @ChargedMinutes = @ChargedDays * @MinutesPerDay;
        SET @SourceID = 'VAC_RESERVE|' + CAST(@VacationID AS NVARCHAR(20));

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@SourceID)
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya existe: ' + @SourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReserveVacationBalance;
            RETURN;
        END

        -- 3.2: UPDLOCK+HOLDLOCK para que dos reservas concurrentes del mismo
        -- empleado se serialicen sobre esta fila y no puedan leer el mismo
        -- saldo "viejo" y dejarlo en negativo (condición de carrera).
        -- 2026-07-06: LOSEP siempre (57) — ver nota en sp_hr_AccrueVacationBalance.
        SELECT @Balance = VacationAvailableMin
        FROM HR.tbl_TimeBalances WITH (UPDLOCK, HOLDLOCK)
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        IF @Balance < @ChargedMinutes
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'Saldo insuficiente. Disponible=' + CAST(@Balance AS NVARCHAR(20))
                         + N' min, Requerido=' + CAST(@ChargedMinutes AS NVARCHAR(20)) + N' min';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN sp_hr_ReserveVacationBalance;
            RETURN;
        END

        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin - @ChargedMinutes,
            LastUpdated = GETDATE()
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 57;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, -@ChargedMinutes, 0,
            GETDATE(), 'VACATION_RESERVE', 'tbl_Vacations', @SourceID,
            @PerformedByEmpID,
            N'Reserva vacaciones. Rango=' + CONVERT(VARCHAR(10),@StartDate,120) + N' a ' + CONVERT(VARCHAR(10),@EndDate,120) +
            N' Laborables=' + CAST(@WorkingDays AS NVARCHAR(10)) +
            N' Semanas=' + CAST(@FullWeeks AS NVARCHAR(10)) +
            N' DiasCobrados=' + CAST(@ChargedDays AS NVARCHAR(10)) +
            N' MinCobrados=' + CAST(@ChargedMinutes AS NVARCHAR(20)),
            57
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Reserva vacaciones OK. MinCobrados=' + CAST(@ChargedMinutes AS NVARCHAR(20));
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN sp_hr_ReserveVacationBalance;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_AccrueVacationBalance_LOES]

/*
  HR.sp_hr_AccrueVacationBalance_LOES
  =====================================
  2026-07-06 (propuesta multi-régimen, regla confirmada por el usuario):
  el vínculo LOES se calcula de forma independiente al LOSEP porque la
  diferencia no está solo en los días, sino en la JORNADA sobre la que se
  convierte ese derecho: LOSEP usa la jornada administrativa completa fija
  (WORK_MINUTES_PER_DAY=480), LOES usa la dedicación académica registrada en
  su contrato vigente (tbl_Contracts.ContractedHours, ej. 40h/semana=tiempo
  completo, 20h/semana=medio tiempo).

  SUPUESTO EXPLÍCITO PENDIENTE DE CONFIRMAR: se reutiliza el mismo
  VACATION_PER_YEAR (30, hoy global) que LOSEP, porque el usuario no indicó un
  número de días distinto para LOES — solo que la jornada de conversión es
  distinta. Si legalmente LOES acumula un número de días/año diferente al de
  LOSEP, ese valor debe confirmarse aparte y agregarse como parámetro propio.

  2026-07-06 (tarde): agregado @Mode ('MONTHLY' | 'TOTAL'), mismo patrón que
  sp_hr_AccrueVacationBalance. DAILY no se construyó — no se pidió y evita
  anticipar un caso no confirmado.
*/
CREATE OR ALTER PROCEDURE HR.sp_hr_AccrueVacationBalance_LOES
(
    @EmployeeID INT,
    @AsOfDate DATE = NULL,
    @Mode VARCHAR(10) = 'MONTHLY',
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @HireDate DATE,
        @EmployeeName NVARCHAR(50),
        @PersonID INT,
        @VacationPerYear DECIMAL(10,2),
        @UnusedMinutesPerDay INT,
        @ContractedHours DECIMAL(5,2),
        @MinutesPerDayLOES DECIMAL(18,6),
        @Year INT,
        @Month INT,
        @PeriodStart DATE,
        @PeriodEnd DATE,
        @TerminationDate DATE,
        @DaysInMonth INT,
        @TotalEarnedMinutes INT,
        @AlreadyCredited INT,
        @Delta INT,
        @SourceID NVARCHAR(128),
        @StartedTran BIT = 0,
        @SavepointName NVARCHAR(128) = 'sp_AccrueLOES_' + CAST(NEWID() AS NVARCHAR(36)),

        -- Tope de acumulación (2026-07-22)
        @MaxAccumulationPeriods INT,
        @CapMinutes INT,
        @CurrentBalanceForCap INT;

    SET @AsOfDate = ISNULL(@AsOfDate, CAST(GETDATE() AS DATE));
    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran = 1;
            BEGIN TRANSACTION;
        END
        ELSE
        BEGIN
            SAVE TRANSACTION @SavepointName;
        END

        IF UPPER(@Mode) NOT IN ('TOTAL', 'MONTHLY')
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'Modo inválido. Use: TOTAL o MONTHLY';
            GOTO ErrorExit;
        END

        SELECT
            @HireDate = HireDate,
            @EmployeeName = LEFT(FirstName + ' ' + LastName, 50)
        FROM HR.vw_EmployeeDetails ved WITH (UPDLOCK)
        WHERE EmployeeID = @EmployeeID;

        SELECT @PersonID = PersonID FROM HR.tbl_Employees WHERE EmployeeID = @EmployeeID;

        IF @HireDate IS NULL
        BEGIN
            SET @StatusCode = 404;
            SET @Message = LEFT(N'Empleado no existe o inactivo (ID:' + CAST(@EmployeeID AS NVARCHAR(10)) + N')', 500);
            GOTO ErrorExit;
        END

        IF @HireDate > @AsOfDate
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'HireDate posterior a AsOfDate';
            GOTO ErrorExit;
        END

        -- Dedicación académica: contrato LOES vigente en @AsOfDate (mismo
        -- criterio de vigencia que HR.fn_ResolveEmployeeRate).
        SELECT TOP 1 @ContractedHours = c.ContractedHours
        FROM HR.tbl_Contracts c
        LEFT JOIN HR.ref_Types rt ON rt.TypeId = c.Status AND rt.Category = 'CONTRACT_STATUS'
        WHERE c.PersonID = @PersonID
          AND c.LaborRegimeID = 58
          AND c.StartDate <= @AsOfDate
          AND (c.EndDate IS NULL OR c.EndDate >= @AsOfDate)
        ORDER BY CASE WHEN rt.Name = 'ANULADO' THEN 1 ELSE 0 END ASC, c.ContractID DESC;

        IF @ContractedHours IS NULL OR @ContractedHours <= 0
        BEGIN
            SET @StatusCode = 404;
            SET @Message = N'No existe contrato LOES vigente con ContractedHours válido para este empleado en la fecha.';
            GOTO ErrorExit;
        END

        -- Jornada LOES: horas semanales contratadas / 5 días laborables * 60.
        SET @MinutesPerDayLOES = (@ContractedHours / 5.0) * 60.0;

        EXEC HR.sp_hr_GetVacationParams
            @VacationPerYear = @VacationPerYear OUTPUT,
            @MinutesPerDay   = @UnusedMinutesPerDay OUTPUT;

        IF @VacationPerYear IS NULL OR @VacationPerYear <= 0
        BEGIN
            SET @StatusCode = 500;
            SET @Message = N'Error al obtener VACATION_PER_YEAR';
            GOTO ErrorExit;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID = @EmployeeID;

        -- Tope de acumulación institucional (2026-07-22, ver hr.TBL_PARAMETERS
        -- 'VACATION_MAX_ACCUMULATION_PERIODS'): mismo criterio que la versión
        -- LOSEP, calculado una sola vez para los 2 modos (TOTAL/MONTHLY).
        SELECT @MaxAccumulationPeriods = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_MAX_ACCUMULATION_PERIODS' AND IsActive = 1;
        IF @MaxAccumulationPeriods IS NULL OR @MaxAccumulationPeriods <= 0
            SET @MaxAccumulationPeriods = 2;

        SET @CapMinutes = CAST(ROUND(@MaxAccumulationPeriods * @VacationPerYear * @MinutesPerDayLOES, 0) AS INT);

        ------------------------------------------------------------
        -- MODO TOTAL: todo lo acumulado desde HireDate hasta @AsOfDate,
        -- menos lo ya acreditado antes (por TOTAL o MONTHLY), igual criterio
        -- que sp_hr_AccrueVacationBalance.
        ------------------------------------------------------------
        IF UPPER(@Mode) = 'TOTAL'
        BEGIN
            SET @SourceID = 'VAC_LOES_TOTAL|' + CONVERT(VARCHAR(8), @AsOfDate, 112);

            IF EXISTS (
                SELECT 1
                FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
                WHERE EmployeeID = @EmployeeID
                  AND SourceModule = 'VACATION_ACCRUAL_LOES_TOTAL'
                  AND SourceID = @SourceID
            )
            BEGIN
                SET @StatusCode = 409;
                SET @Message = LEFT(N'LOES TOTAL ya ejecutado: ' + CONVERT(NVARCHAR(10), @AsOfDate, 120), 500);
                GOTO ErrorExit;
            END

            SET @TotalEarnedMinutes = CAST(
                ROUND(
                    (DATEDIFF(DAY, @HireDate, @AsOfDate) / 365.25) *
                    @VacationPerYear *
                    @MinutesPerDayLOES,
                    0
                )
                AS INT
            );

            SELECT @AlreadyCredited = ISNULL(SUM(DeltaVacationMin), 0)
            FROM HR.tbl_TimeBalanceMovements
            WHERE EmployeeID = @EmployeeID
              AND SourceModule IN ('VACATION_ACCRUAL_LOES_TOTAL', 'VACATION_ACCRUAL_LOES_MONTHLY');

            SET @Delta = @TotalEarnedMinutes - @AlreadyCredited;

            IF @Delta <= 0
            BEGIN
                SET @StatusCode = 204;
                SET @Message = LEFT(
                    N'LOES TOTAL: Sin delta. Teórico=' + CAST(@TotalEarnedMinutes AS NVARCHAR(50)) +
                    N' YaAcred=' + CAST(@AlreadyCredited AS NVARCHAR(50)),
                    500
                );
                GOTO SuccessExit;
            END

            -- Tope de acumulación: nunca acredita por encima de @CapMinutes.
            SELECT @CurrentBalanceForCap = VacationAvailableMin
            FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 58;
            SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
            IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
                SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

            UPDATE HR.tbl_TimeBalances
            SET VacationAvailableMin = VacationAvailableMin + @Delta,
                LastUpdated = GETDATE()
            WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 58;

            INSERT INTO HR.tbl_TimeBalanceMovements
            (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
             SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
            VALUES
            (@EmployeeID, @Delta, 0, GETDATE(),
             'VACATION_ACCRUAL_LOES_TOTAL', 'CALC', @SourceID, @PerformedByEmpID,
             LEFT(
                 N'[LOES TOTAL] ' + @EmployeeName +
                 N' | ' + CONVERT(NVARCHAR(10), @HireDate, 120) + N'->' + CONVERT(NVARCHAR(10), @AsOfDate, 120) +
                 N' | Dedicación=' + CAST(@ContractedHours AS NVARCHAR(10)) + N'h/sem' +
                 N' Teórico:' + CAST(@TotalEarnedMinutes AS NVARCHAR(50)) +
                 N' YaAcred:' + CAST(@AlreadyCredited AS NVARCHAR(50)) +
                 N' Delta:+' + CAST(@Delta AS NVARCHAR(50)),
                 500
             ),
             58
            );

            SET @StatusCode = 200;
            SET @Message = LEFT(N'✓ LOES TOTAL: +' + CAST(@Delta AS NVARCHAR(50)) + N' min', 500);
            GOTO SuccessExit;
        END

        ------------------------------------------------------------
        -- MODO MONTHLY (default, recomendado)
        ------------------------------------------------------------
        SET @Year = YEAR(@AsOfDate);
        SET @Month = MONTH(@AsOfDate);
        SET @SourceID = 'VAC_LOES_MONTHLY|' + CAST(@Year AS VARCHAR(4)) + RIGHT('0' + CAST(@Month AS VARCHAR(2)), 2);

        IF EXISTS (
            SELECT 1
            FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
            WHERE EmployeeID = @EmployeeID
              AND SourceModule = 'VACATION_ACCRUAL_LOES_MONTHLY'
              AND SourceID = @SourceID
        )
        BEGIN
            SET @StatusCode = 409;
            SET @Message = LEFT(N'LOES MONTHLY ya ejecutado: ' + CAST(@Year AS VARCHAR(4)) + N'-' + CAST(@Month AS VARCHAR(2)), 500);
            GOTO ErrorExit;
        END

        SET @PeriodStart = DATEFROMPARTS(@Year, @Month, 1);
        SET @PeriodEnd = EOMONTH(@AsOfDate);

        -- Fin de vínculo LOES dentro del mes (mismo criterio que la versión
        -- LOSEP: contrato LOES cuyo EndDate cae en el mes, sin adendum
        -- posterior que lo extienda).
        SELECT TOP 1 @TerminationDate = CAST(c.EndDate AS DATE)
        FROM HR.tbl_Contracts c
        WHERE c.PersonID = @PersonID
          AND c.LaborRegimeID = 58
          AND CAST(c.EndDate AS DATE) BETWEEN @PeriodStart AND @PeriodEnd
          AND NOT EXISTS (
              SELECT 1 FROM HR.tbl_Contracts a
              WHERE a.ParentID = c.ContractID AND a.EndDate >= c.EndDate
          )
        ORDER BY c.EndDate DESC;

        IF (@HireDate > @PeriodStart) SET @PeriodStart = @HireDate;
        IF (@TerminationDate IS NOT NULL AND @TerminationDate < @PeriodEnd) SET @PeriodEnd = @TerminationDate;

        -- Freno adicional: si existe una solicitud de renuncia/jubilación viva (no
        -- RECHAZADO ni ANULADO) con fecha de salida propuesta dentro o antes del
        -- período, se detiene la acreditación desde esa fecha — sin esperar a que
        -- se suba el documento firmado ni se cierre el régimen.
        DECLARE @ResignationExitDate DATE = NULL;
        SELECT TOP 1 @ResignationExitDate = rr.ProposedExitDate
        FROM HR.tbl_ResignationRetirementRequests rr
        WHERE rr.EmployeeID = @EmployeeID
          AND rr.Status NOT IN ('RECHAZADO', 'ANULADO')
          AND rr.ProposedExitDate <= @PeriodEnd
        ORDER BY rr.ProposedExitDate ASC;

        IF (@ResignationExitDate IS NOT NULL AND @ResignationExitDate < @PeriodEnd) SET @PeriodEnd = @ResignationExitDate;

        SET @DaysInMonth = DATEDIFF(DAY, @PeriodStart, @PeriodEnd) + 1;
        IF @DaysInMonth < 0 SET @DaysInMonth = 0;

        SET @Delta = CAST(ROUND((@DaysInMonth / 365.25) * @VacationPerYear * @MinutesPerDayLOES, 0) AS INT);

        IF @Delta <= 0
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'LOES MONTHLY: Delta calculado es 0. Revise ContractedHours/parámetros.';
            GOTO ErrorExit;
        END

        -- Tope de acumulación (ver nota en modo TOTAL).
        SELECT @CurrentBalanceForCap = VacationAvailableMin
        FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 58;
        SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
        IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
            SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin + @Delta,
            LastUpdated = GETDATE()
        WHERE EmployeeID = @EmployeeID AND LaborRegimeId = 58;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
         SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
        VALUES
        (@EmployeeID, @Delta, 0, GETDATE(),
         'VACATION_ACCRUAL_LOES_MONTHLY', 'CALC', @SourceID, @PerformedByEmpID,
         LEFT(
             N'[LOES MONTHLY] ' + @EmployeeName +
             N' | ' + LEFT(DATENAME(MONTH, @AsOfDate), 3) + N'-' + CAST(@Year AS NVARCHAR(4)) +
             N' | Dedicación=' + CAST(@ContractedHours AS NVARCHAR(10)) + N'h/sem' +
             N' JornadaMin=' + CAST(CAST(@MinutesPerDayLOES AS DECIMAL(10,2)) AS NVARCHAR(20)) +
             N' Días:' + CAST(@DaysInMonth AS NVARCHAR(10)) +
             N' Acred:+' + CAST(@Delta AS NVARCHAR(50)),
             500
         ),
         58
        );

        SET @StatusCode = 200;
        SET @Message = LEFT(N'✓ LOES MONTHLY: +' + CAST(@Delta AS NVARCHAR(50)) + N' min', 500);
        GOTO SuccessExit;

    ErrorExit:
        IF @StartedTran = 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        END
        ELSE
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION @SavepointName;
        END
        RETURN;

    SuccessExit:
        IF @StartedTran = 1 AND @@TRANCOUNT > 0 COMMIT TRANSACTION;
        RETURN;

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran = 1
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION @SavepointName;
            END
        END
        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_AccrueVacationBalance_CT]

/*
  HR.sp_hr_AccrueVacationBalance_CT
  =====================================
  2026-07-21, corregido 2026-07-22. Acreditación mensual automática para
  régimen Código de Trabajo. Regla institucional (Contrato Colectivo UTA,
  Cláusula Vigésima Sexta — más favorable que el Art. 69 genérico del Código
  del Trabajo y prevalece sobre este para estos trabajadores): 15 días
  calendario base/año (1,25 días/mes proporcional); al cumplir 5 años de
  servicio se otorgan 15 días adicionales COMPLETOS de una sola vez (no
  progresivo por año) — total fijo 30 días/año desde el 5° año en adelante.
  Usa la misma jornada administrativa fija (WORK_MINUTES_PER_DAY) que LOSEP —
  a diferencia de LOES, aquí no hay dedicación académica que convertir.

  Deliberadamente NO se construyó modo TOTAL: la carga inicial/histórica de
  este régimen se hace vía ajuste manual (VacationBalanceAdjustmentService,
  EF Core, pantalla de RRHH) para no reinterpretar saldos reales ya cargados
  a mano (algunos negativos, verificados por RRHH) con un recálculo teórico
  desde HireDate. Este SP solo suma el proporcional del mes hacia adelante.

  El régimen se resuelve por HR.ref_Types.Name ('Código Trabajo'), nunca por
  TypeID literal — el TypeID es IDENTITY y varía entre ambientes.

  Tope de acumulación (2 períodos anuales, hr.TBL_PARAMETERS
  'VACATION_MAX_ACCUMULATION_PERIODS'): implementado 2026-07-22, ver bloque
  antes del UPDATE de saldo más abajo. Liquidación proporcional al término
  de la relación laboral: fuera de alcance de este SP (ver Liquidaciones /
  VacationBalanceAdjustmentService).
*/
CREATE OR ALTER PROCEDURE HR.sp_hr_AccrueVacationBalance_CT
(
    @EmployeeID INT,
    @AsOfDate DATE = NULL,
    @Mode VARCHAR(10) = 'MONTHLY',
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @HireDate DATE,
        @EmployeeName NVARCHAR(50),
        @PersonID INT,
        @LaborRegimeId INT,
        @VacationPerYearCT DECIMAL(10,2),
        @MinutesPerDay INT,
        @SeniorityStartYear INT,
        @MaxAdditionalDays INT,
        @YearsCompleted INT,
        @AdditionalDays INT,
        @EffectiveVacationPerYear DECIMAL(10,2),
        @Year INT,
        @Month INT,
        @PeriodStart DATE,
        @PeriodEnd DATE,
        @TerminationDate DATE,
        @DaysInMonth INT,
        @Delta INT,
        @SourceID NVARCHAR(128),
        @StartedTran BIT = 0,
        @SavepointName NVARCHAR(128) = 'sp_AccrueCT_' + CAST(NEWID() AS NVARCHAR(36));

    SET @AsOfDate = ISNULL(@AsOfDate, CAST(GETDATE() AS DATE));
    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran = 1;
            BEGIN TRANSACTION;
        END
        ELSE
        BEGIN
            SAVE TRANSACTION @SavepointName;
        END

        IF UPPER(@Mode) <> 'MONTHLY'
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'Modo inválido. sp_hr_AccrueVacationBalance_CT solo soporta MONTHLY (la carga inicial/histórica se hace vía ajuste manual).';
            GOTO ErrorExit;
        END

        SELECT @LaborRegimeId = TypeId
        FROM HR.ref_Types
        WHERE Category = 'CONTRACT_TYPE' AND Name = N'Código Trabajo' AND IsActive = 1;

        IF @LaborRegimeId IS NULL
        BEGIN
            SET @StatusCode = 500;
            SET @Message = N'No existe régimen activo "Código Trabajo" en ref_Types (Category=CONTRACT_TYPE).';
            GOTO ErrorExit;
        END

        SELECT
            @HireDate = HireDate,
            @EmployeeName = LEFT(FirstName + ' ' + LastName, 50)
        FROM HR.vw_EmployeeDetails ved WITH (UPDLOCK)
        WHERE EmployeeID = @EmployeeID;

        SELECT @PersonID = PersonID FROM HR.tbl_Employees WHERE EmployeeID = @EmployeeID;

        IF @HireDate IS NULL
        BEGIN
            SET @StatusCode = 404;
            SET @Message = LEFT(N'Empleado no existe o inactivo (ID:' + CAST(@EmployeeID AS NVARCHAR(10)) + N')', 500);
            GOTO ErrorExit;
        END

        IF @HireDate > @AsOfDate
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'HireDate posterior a AsOfDate';
            GOTO ErrorExit;
        END

        IF NOT EXISTS (
            SELECT 1 FROM HR.tbl_EmployeeLaborRegime
            WHERE EmployeeId = @EmployeeID AND LaborRegimeId = @LaborRegimeId AND IsActive = 1
        )
        BEGIN
            SET @StatusCode = 404;
            SET @Message = N'El empleado no tiene régimen "Código Trabajo" activo.';
            GOTO ErrorExit;
        END

        SELECT @VacationPerYearCT = CAST(Pvalues AS DECIMAL(10,2))
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_PER_YEAR_CT' AND IsActive = 1;

        SELECT @MinutesPerDay = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'WORK_MINUTES_PER_DAY' AND IsActive = 1;

        SELECT @SeniorityStartYear = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_SENIORITY_START_YEAR_CT' AND IsActive = 1;

        SELECT @MaxAdditionalDays = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_MAX_ADDITIONAL_DAYS_CT' AND IsActive = 1;

        IF @VacationPerYearCT IS NULL OR @VacationPerYearCT <= 0
           OR @MinutesPerDay IS NULL OR @MinutesPerDay <= 0
           OR @SeniorityStartYear IS NULL OR @MaxAdditionalDays IS NULL
        BEGIN
            SET @StatusCode = 500;
            SET @Message = N'Faltan parámetros VACATION_PER_YEAR_CT / WORK_MINUTES_PER_DAY / VACATION_SENIORITY_START_YEAR_CT / VACATION_MAX_ADDITIONAL_DAYS_CT en hr.TBL_PARAMETERS.';
            GOTO ErrorExit;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID = @EmployeeID;

        SET @Year = YEAR(@AsOfDate);
        SET @Month = MONTH(@AsOfDate);
        SET @SourceID = 'VAC_CT_MONTHLY|' + CAST(@Year AS VARCHAR(4)) + RIGHT('0' + CAST(@Month AS VARCHAR(2)), 2);

        IF EXISTS (
            SELECT 1
            FROM HR.tbl_TimeBalanceMovements WITH (UPDLOCK, HOLDLOCK)
            WHERE EmployeeID = @EmployeeID
              AND SourceModule = 'VACATION_ACCRUAL_MONTHLY_CT'
              AND SourceID = @SourceID
        )
        BEGIN
            SET @StatusCode = 409;
            SET @Message = LEFT(N'CT MONTHLY ya ejecutado: ' + CAST(@Year AS VARCHAR(4)) + N'-' + CAST(@Month AS VARCHAR(2)), 500);
            GOTO ErrorExit;
        END

        SET @PeriodStart = DATEFROMPARTS(@Year, @Month, 1);
        SET @PeriodEnd = EOMONTH(@AsOfDate);

        -- Fin de vínculo CT dentro del mes (mismo criterio que LOSEP/LOES:
        -- contrato/adenda CT cuyo EndDate cae en el mes, sin extensión posterior).
        SELECT TOP 1 @TerminationDate = CAST(c.EndDate AS DATE)
        FROM HR.tbl_Contracts c
        WHERE c.PersonID = @PersonID
          AND c.LaborRegimeID = @LaborRegimeId
          AND CAST(c.EndDate AS DATE) BETWEEN @PeriodStart AND @PeriodEnd
          AND NOT EXISTS (
              SELECT 1 FROM HR.tbl_Contracts a
              WHERE a.ParentID = c.ContractID AND a.EndDate >= c.EndDate
          )
        ORDER BY c.EndDate DESC;

        IF (@HireDate > @PeriodStart) SET @PeriodStart = @HireDate;
        IF (@TerminationDate IS NOT NULL AND @TerminationDate < @PeriodEnd) SET @PeriodEnd = @TerminationDate;

        -- Freno adicional: si existe una solicitud de renuncia/jubilación viva (no
        -- RECHAZADO ni ANULADO) con fecha de salida propuesta dentro o antes del
        -- período, se detiene la acreditación desde esa fecha — sin esperar a que
        -- se suba el documento firmado ni se cierre el régimen.
        DECLARE @ResignationExitDate DATE = NULL;
        SELECT TOP 1 @ResignationExitDate = rr.ProposedExitDate
        FROM HR.tbl_ResignationRetirementRequests rr
        WHERE rr.EmployeeID = @EmployeeID
          AND rr.Status NOT IN ('RECHAZADO', 'ANULADO')
          AND rr.ProposedExitDate <= @PeriodEnd
        ORDER BY rr.ProposedExitDate ASC;

        IF (@ResignationExitDate IS NOT NULL AND @ResignationExitDate < @PeriodEnd) SET @PeriodEnd = @ResignationExitDate;

        SET @DaysInMonth = DATEDIFF(DAY, @PeriodStart, @PeriodEnd) + 1;
        IF @DaysInMonth < 0 SET @DaysInMonth = 0;

        -- Antigüedad completa (años) a la fecha de corte del período.
        SET @YearsCompleted = DATEDIFF(YEAR, @HireDate, @PeriodEnd)
            - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @HireDate, @PeriodEnd), @HireDate) > @PeriodEnd THEN 1 ELSE 0 END;

        -- Regla institucional (Contrato Colectivo UTA, Cláusula Vigésima Sexta — NO es la
        -- progresión genérica del Art. 69 del Código del Trabajo): al cumplir
        -- VACATION_SENIORITY_START_YEAR_CT años de servicio, se otorgan
        -- VACATION_MAX_ADDITIONAL_DAYS_CT días adicionales COMPLETOS de una sola vez
        -- (15 base + 15 al cumplir 5 años = 30 días/año fijo desde ahí), no un día
        -- adicional progresivo por cada año posterior.
        SET @AdditionalDays = CASE
            WHEN @YearsCompleted >= @SeniorityStartYear THEN @MaxAdditionalDays
            ELSE 0
        END;

        SET @EffectiveVacationPerYear = @VacationPerYearCT + @AdditionalDays;

        SET @Delta = CAST(ROUND((@DaysInMonth / 365.25) * @EffectiveVacationPerYear * @MinutesPerDay, 0) AS INT);

        IF @Delta <= 0
        BEGIN
            SET @StatusCode = 400;
            SET @Message = N'CT MONTHLY: Delta calculado es 0. Revise HireDate/parámetros.';
            GOTO ErrorExit;
        END

        -- Tope de acumulación institucional (2026-07-22, ver hr.TBL_PARAMETERS
        -- 'VACATION_MAX_ACCUMULATION_PERIODS'): usa @EffectiveVacationPerYear
        -- (ya incluye el bonus de antigüedad de este empleado), no el genérico.
        DECLARE @MaxAccumulationPeriods INT;
        SELECT @MaxAccumulationPeriods = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS WHERE name = 'VACATION_MAX_ACCUMULATION_PERIODS' AND IsActive = 1;
        IF @MaxAccumulationPeriods IS NULL OR @MaxAccumulationPeriods <= 0
            SET @MaxAccumulationPeriods = 2;

        DECLARE @CapMinutes INT = CAST(ROUND(@MaxAccumulationPeriods * @EffectiveVacationPerYear * @MinutesPerDay, 0) AS INT);

        DECLARE @CurrentBalanceForCap INT;
        SELECT @CurrentBalanceForCap = VacationAvailableMin
        FROM HR.tbl_TimeBalances WHERE EmployeeID = @EmployeeID AND LaborRegimeId = @LaborRegimeId;
        SET @CurrentBalanceForCap = ISNULL(@CurrentBalanceForCap, 0);
        IF (@CurrentBalanceForCap + @Delta) > @CapMinutes
            SET @Delta = CASE WHEN @CapMinutes > @CurrentBalanceForCap THEN @CapMinutes - @CurrentBalanceForCap ELSE 0 END;

        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin + @Delta,
            LastUpdated = GETDATE()
        WHERE EmployeeID = @EmployeeID AND LaborRegimeId = @LaborRegimeId;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (EmployeeID, DeltaVacationMin, DeltaRecoveryMin, MovementAt,
         SourceModule, SourceTable, SourceID, PerformedByEmpID, Note, LaborRegimeId)
        VALUES
        (@EmployeeID, @Delta, 0, GETDATE(),
         'VACATION_ACCRUAL_MONTHLY_CT', 'CALC', @SourceID, @PerformedByEmpID,
         LEFT(
             N'[CT MONTHLY] ' + @EmployeeName +
             N' | ' + LEFT(DATENAME(MONTH, @AsOfDate), 3) + N'-' + CAST(@Year AS NVARCHAR(4)) +
             N' | Antigüedad=' + CAST(@YearsCompleted AS NVARCHAR(10)) + N'a' +
             N' DiasAdicionales=' + CAST(@AdditionalDays AS NVARCHAR(10)) +
             N' DiasEfectivos/año=' + CAST(@EffectiveVacationPerYear AS NVARCHAR(10)) +
             N' Días período:' + CAST(@DaysInMonth AS NVARCHAR(10)) +
             N' Acred:+' + CAST(@Delta AS NVARCHAR(50)),
             500
         ),
         @LaborRegimeId
        );

        SET @StatusCode = 200;
        SET @Message = LEFT(N'✓ CT MONTHLY: +' + CAST(@Delta AS NVARCHAR(50)) + N' min', 500);
        GOTO SuccessExit;

    ErrorExit:
        IF @StartedTran = 1
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        END
        ELSE
        BEGIN
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION @SavepointName;
        END
        RETURN;

    SuccessExit:
        IF @StartedTran = 1 AND @@TRANCOUNT > 0 COMMIT TRANSACTION;
        RETURN;

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran = 1
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION @SavepointName;
            END
        END
        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_hr_ReserveVacationBalance_LOES]

/*
  OBSOLETA / SIN USO REAL (verificado 2026-07-22): HrBalanceRepository.ReserveVacationAsync
  nunca la llamó — siempre llamaba a sp_hr_ReserveVacationBalance (LOSEP=57) sin importar el
  régimen real del empleado. Ya huérfana desde antes de esta migración. El mecanismo
  vigente es IVacationBalanceAdjustmentService.ReserveAsync (EF Core), que sí resuelve el
  régimen real. Se deja sin borrar por trazabilidad histórica.

  HR.sp_hr_ReserveVacationBalance_LOES
  =======================================
  2026-07-06. Espejo de sp_hr_ReserveVacationBalance pero contra el saldo
  LOES (LaborRegimeId=58) — mismo cálculo de días calendario reales del
  período, cobrando contra HR.tbl_Vacations.Status='Planned'. No usa jornada
  distinta aquí porque la vacación ya se solicita en días calendario, no en
  horas de dedicación (esa distinción solo aplica en la ACUMULACIÓN, no en el
  descuento de días tomados).
*/
CREATE OR ALTER PROCEDURE HR.sp_hr_ReserveVacationBalance_LOES
(
    @VacationID INT,
    @PerformedByEmpID INT = NULL,
    @StatusCode INT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @EmployeeID INT,
        @StartDate DATE,
        @EndDate DATE,
        @ChargedDays INT,
        @MinutesPerDay INT,
        @ChargedMinutes INT,
        @Balance INT,
        @SourceID NVARCHAR(128),
        @StartedTran BIT = 0;

    SET @StatusCode = 0;
    SET @Message = N'';

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            SET @StartedTran=1;
            BEGIN TRAN;
        END
        ELSE
        BEGIN
            SAVE TRAN SP_RVB_LOES;
        END

        SELECT @MinutesPerDay = CAST(Pvalues AS INT)
        FROM hr.TBL_PARAMETERS
        WHERE name='WORK_MINUTES_PER_DAY' AND IsActive=1;

        IF @MinutesPerDay IS NULL OR @MinutesPerDay <= 0
            RAISERROR('WORK_MINUTES_PER_DAY invalido', 16, 1);

        SELECT
            @EmployeeID = EmployeeID,
            @StartDate = StartDate,
            @EndDate = EndDate
        FROM HR.tbl_Vacations
        WHERE VacationID=@VacationID
          AND Status='Planned';

        IF @EmployeeID IS NULL
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Vacación no existe o no está Planned';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN SP_RVB_LOES;
            RETURN;
        END

        EXEC HR.sp_hr_EnsureTimeBalanceRow @EmployeeID=@EmployeeID;

        SET @ChargedDays = DATEDIFF(DAY, @StartDate, @EndDate) + 1;
        SET @ChargedMinutes = @ChargedDays * @MinutesPerDay;
        SET @SourceID = 'VAC_LOES_RESERVE|' + CAST(@VacationID AS NVARCHAR(20));

        IF EXISTS (SELECT 1 FROM HR.tbl_TimeBalanceMovements WHERE EmployeeID=@EmployeeID AND SourceID=@SourceID)
        BEGIN
            SET @StatusCode = 1;
            SET @Message = N'Reserva ya existe: ' + @SourceID;
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN SP_RVB_LOES;
            RETURN;
        END

        SELECT @Balance = VacationAvailableMin
        FROM HR.tbl_TimeBalances WITH (UPDLOCK, HOLDLOCK)
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 58;

        IF @Balance < @ChargedMinutes
        BEGIN
            SET @StatusCode = -1;
            SET @Message = N'Saldo LOES insuficiente. Disponible=' + CAST(@Balance AS NVARCHAR(20))
                         + N' min, Requerido=' + CAST(@ChargedMinutes AS NVARCHAR(20)) + N' min';
            IF @StartedTran=1 ROLLBACK TRAN ELSE ROLLBACK TRAN SP_RVB_LOES;
            RETURN;
        END

        UPDATE HR.tbl_TimeBalances
        SET VacationAvailableMin = VacationAvailableMin - @ChargedMinutes,
            LastUpdated = GETDATE()
        WHERE EmployeeID=@EmployeeID AND LaborRegimeId = 58;

        INSERT INTO HR.tbl_TimeBalanceMovements
        (
            EmployeeID, DeltaVacationMin, DeltaRecoveryMin,
            MovementAt, SourceModule, SourceTable, SourceID,
            PerformedByEmpID, Note, LaborRegimeId
        )
        VALUES
        (
            @EmployeeID, -@ChargedMinutes, 0,
            GETDATE(), 'VACATION_RESERVE_LOES', 'tbl_Vacations', @SourceID,
            @PerformedByEmpID,
            N'Reserva vacaciones LOES. Rango=' + CONVERT(VARCHAR(10),@StartDate,120) + N' a ' + CONVERT(VARCHAR(10),@EndDate,120) +
            N' DiasCobrados=' + CAST(@ChargedDays AS NVARCHAR(10)) +
            N' MinCobrados=' + CAST(@ChargedMinutes AS NVARCHAR(20)),
            58
        );

        IF @StartedTran=1 COMMIT TRAN;
        SET @StatusCode = 0;
        SET @Message = N'Reserva vacaciones LOES OK. MinCobrados=' + CAST(@ChargedMinutes AS NVARCHAR(20));
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            IF @StartedTran=1
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN;
            END
            ELSE
            BEGIN
                IF @@TRANCOUNT>0 ROLLBACK TRAN SP_RVB_LOES;
            END
        END

        SET @StatusCode = ERROR_NUMBER();
        SET @Message = ERROR_MESSAGE();
        THROW;
    END CATCH
END

GO

-- [sp_InsertReportAudit]
-- ELIMINADO 2026-07-01: reemplazado por inserción vía Entity Framework Core
-- (ReportAuditRepository.CreateAuditAsync). Respaldo completo del script en
-- Database/hr/99_legacy_sp_backup_20260701.sql

-- CREATE PROCEDURE [HR].[sp_InsertReportAudit]
--     @UserId UNIQUEIDENTIFIER,
--     @UserEmail NVARCHAR(255),
--     @ReportType NVARCHAR(50),
--     @ReportFormat NVARCHAR(10),
--     @FiltersApplied NVARCHAR(MAX) = NULL,
--     @FileSizeBytes BIGINT = NULL,
--     @GenerationTimeMs INT = NULL,
--     @ClientIp NVARCHAR(50) = NULL,
--     @Success BIT = 1,
--     @ErrorMessage NVARCHAR(MAX) = NULL,
--     @FileName NVARCHAR(255) = NULL
-- AS
-- BEGIN
--     SET NOCOUNT ON;
--
--     INSERT INTO [HR].[tbl_ReportAudit] (
--         UserId, UserEmail, ReportType, ReportFormat, FiltersApplied,
--         GeneratedAt, FileSizeBytes, GenerationTimeMs, ClientIp,
--         Success, ErrorMessage, FileName
--     )
--     VALUES (
--         @UserId, @UserEmail, @ReportType, @ReportFormat, @FiltersApplied,
--         GETUTCDATE(), @FileSizeBytes, @GenerationTimeMs, @ClientIp,
--         @Success, @ErrorMessage, @FileName
--     );
--
--     SELECT SCOPE_IDENTITY() AS AuditId;
-- END
--
-- GO

-- [sp_Justifications_Apply]
CREATE   PROCEDURE HR.sp_Justifications_Apply
  @FromDate   DATE,
  @ToDate     DATE,
  @EmployeeID INT = NULL,
  @Debug      BIT = 0
AS
BEGIN
  SET NOCOUNT ON;

  /* =========================
     0) Resolver TypeIDs y Variables
     ========================= */
  DECLARE @Type_Picada INT, @Type_Horas INT, @Type_Dia INT;
  DECLARE @PunchType_Entrada INT, @PunchType_Salida INT, @PunchType_SalidaAlmuerzo INT, @PunchType_RegresoAlmuerzo INT;

  SELECT
    @Type_Horas  = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE 'HORA%'   THEN TypeID END),
    @Type_Picada = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE 'PICADA%' THEN TypeID END),
    @Type_Dia    = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) IN ('DIA','DÍA','DIA COMPLETO','DÍA COMPLETO') THEN TypeID END)
  FROM HR.ref_Types
  WHERE Category = 'JUSTIFICATION';

  SELECT
    @PunchType_Entrada        = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE '%ENTRADA%' THEN TypeID END),
    @PunchType_Salida         = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE '%SALIDA%' AND UPPER(Name COLLATE Latin1_General_CI_AI) NOT LIKE '%ALMUERZO%' THEN TypeID END),
    @PunchType_SalidaAlmuerzo = MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE '%SALIDA ALMUERZO%' THEN TypeID END),
    @PunchType_RegresoAlmuerzo= MAX(CASE WHEN UPPER(Name COLLATE Latin1_General_CI_AI) LIKE '%REGRESO ALMUERZO%' THEN TypeID END)
  FROM HR.ref_Types
  WHERE Category = 'PUNCH_TYPE';

  IF @Debug = 1
  BEGIN
    PRINT '=== HR.sp_Justifications_Apply - VERSION CORREGIDA ===';
    PRINT 'Rango: ' + CONVERT(varchar(10),@FromDate,120) + ' a ' + CONVERT(varchar(10),@ToDate,120);
    PRINT 'EmployeeID: ' + COALESCE(CAST(@EmployeeID AS varchar(12)),'(todos)');
    PRINT 'Tipos Justificación: PICADA=' + COALESCE(CAST(@Type_Picada AS varchar(12)),'NULL') +
          ', HORAS=' + COALESCE(CAST(@Type_Horas AS varchar(12)),'NULL') +
          ', DIA='   + COALESCE(CAST(@Type_Dia   AS varchar(12)),'NULL');
    PRINT 'Tipos Picada: ENTRADA=' + COALESCE(CAST(@PunchType_Entrada AS varchar(12)),'NULL') +
          ', SALIDA=' + COALESCE(CAST(@PunchType_Salida AS varchar(12)),'NULL');
  END;

  /* =========================
     1) Calendario
     ========================= */
  IF OBJECT_ID('tempdb..#Cal') IS NOT NULL DROP TABLE #Cal;
  ;WITH D AS (
    SELECT @FromDate AS D
    UNION ALL
    SELECT DATEADD(DAY,1,D) FROM D WHERE D < @ToDate
  )
  SELECT D INTO #Cal FROM D OPTION (MAXRECURSION 0);

  /* =========================
     2) Explosión de justificaciones CORREGIDA
     ========================= */
  IF OBJECT_ID('tempdb..#J') IS NOT NULL DROP TABLE #J;
  CREATE TABLE #J (
    PunchJustID INT NOT NULL,
    EmployeeID  INT NOT NULL,
    WorkDate    DATE NOT NULL,
    JustTypeID  INT NOT NULL,
    JustMinutes INT NOT NULL,
    PunchTypeID INT NULL,
    StartTime   TIME NULL,
    EndTime     TIME NULL
  );

  -- DÍA COMPLETO
  INSERT INTO #J (PunchJustID, EmployeeID, WorkDate, JustTypeID, JustMinutes)
  SELECT j.PunchJustID, j.EmployeeID, c.D, j.JustificationTypeID, 0
  FROM HR.tbl_PunchJustifications j
  JOIN #Cal c ON c.D BETWEEN CAST(j.StartDate AS DATE) AND CAST(j.EndDate AS DATE)
  WHERE j.Status = 'APPROVED'
    AND j.JustificationTypeID = @Type_Dia
    AND (@EmployeeID IS NULL OR j.EmployeeID = @EmployeeID);

  -- PICADA - CORREGIDO: Calcular minutos reales del rango
  INSERT INTO #J (PunchJustID, EmployeeID, WorkDate, JustTypeID, JustMinutes, PunchTypeID, StartTime, EndTime)
  SELECT 
    j.PunchJustID,
    j.EmployeeID,
    CAST(j.JustificationDate AS DATE) AS WorkDate,
    j.JustificationTypeID,
    -- CORRECCIÓN: Usar el rango de tiempo real
    DATEDIFF(MINUTE, j.StartDate, j.EndDate) AS JustMinutes,
    j.PunchTypeID,
    CAST(j.StartDate AS TIME) AS StartTime,
    CAST(j.EndDate   AS TIME) AS EndTime
  FROM HR.tbl_PunchJustifications j
  WHERE j.Status = 'APPROVED'
    AND j.JustificationTypeID = @Type_Picada
    AND CAST(j.JustificationDate AS DATE) BETWEEN @FromDate AND @ToDate
    AND (@EmployeeID IS NULL OR j.EmployeeID = @EmployeeID)
    AND j.StartDate IS NOT NULL 
    AND j.EndDate IS NOT NULL;

  IF @Debug = 1
  BEGIN
    DECLARE @PicadaCount INT;
    SELECT @PicadaCount = COUNT(*) FROM #J WHERE JustTypeID = @Type_Picada;
    PRINT 'Picadas cargadas: ' + CAST(@PicadaCount AS VARCHAR(10));
  END;

  -- HORAS
  INSERT INTO #J (PunchJustID, EmployeeID, WorkDate, JustTypeID, JustMinutes, StartTime, EndTime)
  SELECT 
    j.PunchJustID,
    j.EmployeeID,
    c.D,
    j.JustificationTypeID,
    DATEDIFF(MINUTE, j.StartDate, j.EndDate) AS JustMinutes,
    CAST(j.StartDate AS TIME) AS StartTime,
    CAST(j.EndDate   AS TIME) AS EndTime
  FROM HR.tbl_PunchJustifications j
  JOIN #Cal c ON c.D BETWEEN CAST(j.StartDate AS DATE) AND CAST(j.EndDate AS DATE)
  WHERE j.Status = 'APPROVED'
    AND j.JustificationTypeID = @Type_Horas
    AND (@EmployeeID IS NULL OR j.EmployeeID = @EmployeeID);

  IF @Debug = 1
  BEGIN
    PRINT '----- JUSTIFICACIONES CARGADAS -----';
    SELECT 
      PunchJustID,
      EmployeeID,
      WorkDate,
      CASE JustTypeID 
        WHEN @Type_Dia THEN 'DIA'
        WHEN @Type_Picada THEN 'PICADA'
        WHEN @Type_Horas THEN 'HORAS'
        ELSE 'OTRO'
      END AS TipoJust,
      JustMinutes,
      CASE PunchTypeID
        WHEN @PunchType_Entrada THEN 'ENTRADA'
        WHEN @PunchType_Salida THEN 'SALIDA'
        ELSE CAST(PunchTypeID AS VARCHAR(10))
      END AS TipoPicada,
      StartTime,
      EndTime
    FROM #J 
    ORDER BY WorkDate, EmployeeID;
  END;

  /* =========================
     3) Agregado por Día
     ========================= */
  IF OBJECT_ID('tempdb..#Agg') IS NOT NULL DROP TABLE #Agg;
  CREATE TABLE #Agg (
    EmployeeID INT NOT NULL,
    WorkDate   DATE NOT NULL,
    DiaFlag    BIT  NOT NULL DEFAULT(0),
    TotalJust  INT  NOT NULL DEFAULT(0),
    PRIMARY KEY(EmployeeID, WorkDate)
  );

  INSERT INTO #Agg (EmployeeID, WorkDate, DiaFlag, TotalJust)
  SELECT
    j.EmployeeID,
    j.WorkDate,
    MAX(CASE WHEN j.JustTypeID = @Type_Dia THEN 1 ELSE 0 END) AS DiaFlag,
    SUM(j.JustMinutes) AS TotalJust
  FROM #J j
  GROUP BY j.EmployeeID, j.WorkDate;

  IF @Debug = 1
  BEGIN
    PRINT '----- AGREGADO POR DÍA -----';
    SELECT 
      EmployeeID,
      WorkDate,
      DiaFlag,
      TotalJust AS MinutosJustificados
    FROM #Agg 
    ORDER BY WorkDate, EmployeeID;
  END;

  /* =========================
     4) Estado base AC
     ========================= */
  IF OBJECT_ID('tempdb..#DayBase') IS NOT NULL DROP TABLE #DayBase;
  CREATE TABLE #DayBase(
    EmployeeID         INT,
    WorkDate           DATE,
    RequiredMinutes    INT,
    TotalWorkedMinutes INT,
    RegularMinutes     INT,
    TardinessMin       INT,
    MinutesLate        INT
  );

  INSERT INTO #DayBase
  SELECT 
    ac.EmployeeID,
    ac.WorkDate,
    ISNULL(ac.RequiredMinutes,0),
    ISNULL(ac.TotalWorkedMinutes,0),
    ISNULL(ac.RegularMinutes,0),
    ISNULL(ac.TardinessMin,0),
    ISNULL(ac.MinutesLate,0)
  FROM HR.tbl_AttendanceCalculations ac
  JOIN #Agg a ON a.EmployeeID = ac.EmployeeID AND a.WorkDate = ac.WorkDate
  WHERE ac.WorkDate BETWEEN @FromDate AND @ToDate
    AND (@EmployeeID IS NULL OR ac.EmployeeID = @EmployeeID);

  /* =========================
     5) Horarios efectivos
     ========================= */
  IF OBJECT_ID('tempdb..#EmployeeSchedules') IS NOT NULL DROP TABLE #EmployeeSchedules;
  CREATE TABLE #EmployeeSchedules (
    EmployeeID INT,
    WorkDate   DATE,
    EntryTime  TIME,
    ExitTime   TIME,
    PRIMARY KEY (EmployeeID, WorkDate)
  );

  ;WITH HorariosEfectivos AS (
    SELECT
        a.EmployeeID,
        a.WorkDate,
        es.ScheduleID,
        ROW_NUMBER() OVER (
            PARTITION BY a.EmployeeID, a.WorkDate
            ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC
        ) AS rn
    FROM #Agg a
    JOIN HR.tbl_EmployeeSchedules es
        ON es.EmployeeID = a.EmployeeID
       AND es.ValidFrom <= a.WorkDate
       AND (es.ValidTo IS NULL OR a.WorkDate <= es.ValidTo)
  )
  INSERT INTO #EmployeeSchedules (EmployeeID, WorkDate, EntryTime, ExitTime)
  SELECT h.EmployeeID, h.WorkDate, s.EntryTime, s.ExitTime
  FROM HorariosEfectivos h
  JOIN HR.tbl_Schedules s ON s.ScheduleID = h.ScheduleID
  WHERE h.rn = 1;

  /* =========================
     6) APLICAR DÍA COMPLETO
     ========================= */
  UPDATE ac
  SET 
    ac.JustificationApply   = 1,
    ac.JustificationMinutes = db.RequiredMinutes,
    ac.TotalWorkedMinutes   = db.RequiredMinutes,
    ac.RegularMinutes       = db.RequiredMinutes,
    ac.TardinessMin         = 0,
    ac.MinutesLate          = 0,
    ac.OvertimeMinutes      = 0,
    ac.HolidayMinutes       = 0
  FROM HR.tbl_AttendanceCalculations ac
  JOIN #DayBase db ON db.EmployeeID = ac.EmployeeID AND db.WorkDate = ac.WorkDate
  JOIN #Agg a ON a.EmployeeID = db.EmployeeID AND a.WorkDate = db.WorkDate
  WHERE a.DiaFlag = 1;

  IF @Debug = 1 PRINT 'Días completos aplicados: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

  /* =========================
     7) APLICAR PICADAS Y HORAS - LÓGICA SIMPLIFICADA
     ========================= */
  IF OBJECT_ID('tempdb..#PHCalc') IS NOT NULL DROP TABLE #PHCalc;
  CREATE TABLE #PHCalc (
    EmployeeID         INT,
    WorkDate           DATE,
    RequiredMinutes    INT,
    TotalWorkedBefore  INT,
    RegularBefore      INT,
    TardinessBefore    INT,
    MinutesLateBefore  INT,
    TotalJust          INT,
    IsEntryJust        BIT,  -- NUEVA: Detecta si hay justificación de ENTRADA
    -- Campos calculados
    MinutosParaCompletar INT,
    MinutosUsadosRegular INT,
    MinutosSobrantes     INT,
    MinutosUsadosTardanza INT,
    TotalJustificado     INT,
    NuevoTotalWorked     INT,
    NuevoRegular         INT,
    NuevoTardiness       INT,
    NuevoMinutesLate     INT
  );

  INSERT INTO #PHCalc (
    EmployeeID, WorkDate, RequiredMinutes, TotalWorkedBefore, RegularBefore, 
    TardinessBefore, MinutesLateBefore, TotalJust, IsEntryJust
  )
  SELECT
    db.EmployeeID,
    db.WorkDate,
    db.RequiredMinutes,
    db.TotalWorkedMinutes,
    db.RegularMinutes,
    db.TardinessMin,
    db.MinutesLate,
    a.TotalJust,
    -- CORRECCIÓN: Detectar si hay PICADA de ENTRADA (sin requerir 30 minutos)
    CASE WHEN EXISTS (
      SELECT 1 
      FROM #J j 
      WHERE j.EmployeeID = db.EmployeeID 
        AND j.WorkDate = db.WorkDate
        AND j.JustTypeID = @Type_Picada
        AND j.PunchTypeID = @PunchType_Entrada
    ) THEN 1 ELSE 0 END
  FROM #DayBase db
  JOIN #Agg a ON a.EmployeeID = db.EmployeeID AND a.WorkDate = db.WorkDate
  WHERE a.DiaFlag = 0;

  -- CALCULAR APLICACIÓN CON LÓGICA CLARA
  UPDATE #PHCalc
  SET 
    -- 1. ¿Cuántos minutos faltan para completar RequiredMinutes?
    MinutosParaCompletar = CASE 
      WHEN RequiredMinutes > TotalWorkedBefore 
      THEN RequiredMinutes - TotalWorkedBefore 
      ELSE 0 
    END,
    
    -- 2. Usar justificación para completar tiempo regular (hasta RequiredMinutes)
    MinutosUsadosRegular = CASE 
      WHEN RequiredMinutes > TotalWorkedBefore THEN
        CASE WHEN TotalJust >= (RequiredMinutes - TotalWorkedBefore)
          THEN (RequiredMinutes - TotalWorkedBefore)
          ELSE TotalJust
        END
      ELSE 0
    END;

  -- 3. Calcular sobrantes y aplicar a tardanza
  UPDATE #PHCalc
  SET 
    MinutosSobrantes = TotalJust - MinutosUsadosRegular,
    
    -- Si es ENTRADA, elimina toda la tardanza
    MinutosUsadosTardanza = CASE 
      WHEN IsEntryJust = 1 THEN TardinessBefore
      WHEN (TotalJust - MinutosUsadosRegular) > 0 THEN
        CASE WHEN (TotalJust - MinutosUsadosRegular) >= TardinessBefore
          THEN TardinessBefore
          ELSE (TotalJust - MinutosUsadosRegular)
        END
      ELSE 0
    END,
    
    TotalJustificado = MinutosUsadosRegular + 
      CASE 
        WHEN IsEntryJust = 1 THEN TardinessBefore
        WHEN (TotalJust - MinutosUsadosRegular) > 0 THEN
          CASE WHEN (TotalJust - MinutosUsadosRegular) >= TardinessBefore
            THEN TardinessBefore
            ELSE (TotalJust - MinutosUsadosRegular)
          END
        ELSE 0
      END;

  -- 4. Calcular valores finales
  UPDATE #PHCalc
  SET 
    -- CORRECCIÓN: TotalWorked SIEMPRE suma los minutos justificados
    NuevoTotalWorked = TotalWorkedBefore + MinutosUsadosRegular,
    
    -- Regular se limita a RequiredMinutes
    NuevoRegular = CASE 
      WHEN (TotalWorkedBefore + MinutosUsadosRegular) > RequiredMinutes
      THEN RequiredMinutes
      ELSE (TotalWorkedBefore + MinutosUsadosRegular)
    END,
    
    NuevoTardiness = CASE 
      WHEN (TardinessBefore - MinutosUsadosTardanza) < 0 
      THEN 0 
      ELSE (TardinessBefore - MinutosUsadosTardanza)
    END,
    
    -- CORRECCIÓN: Si es justificación de ENTRADA, MinutesLate = 0
    NuevoMinutesLate = CASE 
      WHEN IsEntryJust = 1 THEN 0
      WHEN (MinutesLateBefore - MinutosUsadosTardanza) < 0 
      THEN 0 
      ELSE (MinutesLateBefore - MinutosUsadosTardanza)
    END;

  IF @Debug = 1
  BEGIN
    PRINT '----- CÁLCULOS PICADA/HORAS -----';
    SELECT * FROM #PHCalc ORDER BY WorkDate, EmployeeID;
  END;

  -- APLICAR A LA TABLA PRINCIPAL
  UPDATE ac
  SET 
    ac.JustificationApply   = 1,
    ac.JustificationMinutes = ph.TotalJustificado,
    ac.TotalWorkedMinutes   = ph.NuevoTotalWorked,
    ac.RegularMinutes       = ph.NuevoRegular,
    ac.TardinessMin         = ph.NuevoTardiness,
    ac.MinutesLate          = ph.NuevoMinutesLate,
    -- CORRECCIÓN: OvertimeMinutes se calcula sobre el nuevo TotalWorked
    ac.OvertimeMinutes      = CASE 
      WHEN ph.NuevoTotalWorked > ph.RequiredMinutes
      THEN ph.NuevoTotalWorked - ph.RequiredMinutes
      ELSE 0 
    END
  FROM HR.tbl_AttendanceCalculations ac
  JOIN #PHCalc ph ON ph.EmployeeID = ac.EmployeeID AND ph.WorkDate = ac.WorkDate;

  IF @Debug = 1 PRINT 'Picadas/Horas aplicadas: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

  /* =========================
     8) MARCAR JUSTIFICACIONES COMO APPLIED
     ========================= */
  IF COL_LENGTH('HR.tbl_PunchJustifications', 'AppliedAt') IS NOT NULL
  BEGIN
    UPDATE HR.tbl_PunchJustifications
    SET Status = 'APPLIED', AppliedAt = GETDATE()
    WHERE PunchJustID IN (SELECT DISTINCT PunchJustID FROM #J)
      AND Status = 'APPROVED';
  END
  ELSE
  BEGIN
    UPDATE HR.tbl_PunchJustifications
    SET Status = 'APPLIED'
    WHERE PunchJustID IN (SELECT DISTINCT PunchJustID FROM #J)
      AND Status = 'APPROVED';
  END

  PRINT 'Proceso completado. Justificaciones marcadas: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

  -- Limpieza
  DROP TABLE IF EXISTS #PHCalc;
  DROP TABLE IF EXISTS #EmployeeSchedules;
  DROP TABLE IF EXISTS #DayBase;
  DROP TABLE IF EXISTS #Agg;
  DROP TABLE IF EXISTS #J;
  DROP TABLE IF EXISTS #Cal;
END;

GO

-- [sp_Overtime_Calculate]
 CREATE   PROCEDURE HR.sp_Overtime_Calculate
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
  -- 6) Replica a HR.tbl_Overtime (EXECUTED; no pisar APPROVED/PAID)
  ------------------------------------------------------------------
  ;WITH ExecPerDay AS (
    SELECT pe.EmployeeID, x.WorkDate,
           SUM(ISNULL(x.OvertimeMinutes,0)) AS OrdMin,
           SUM(ISNULL(x.NightMinutes,0))    AS NightMin,
           SUM(ISNULL(x.HolidayMinutes,0))  AS HolMin
    FROM HR.tbl_TimePlanningExecution x
    JOIN HR.tbl_TimePlanningEmployees pe ON pe.PlanEmployeeID = x.PlanEmployeeID
    JOIN HR.tbl_TimePlanning p ON p.PlanID = pe.PlanID
    WHERE p.PlanType='Overtime'
      AND x.WorkDate BETWEEN @FromDate AND @ToDate
      AND (@EmployeeID IS NULL OR pe.EmployeeID = @EmployeeID)
    GROUP BY pe.EmployeeID, x.WorkDate
  ),
  Resolved AS (
    SELECT 
      e.EmployeeID,
      e.WorkDate,
      CAST((ISNULL(e.OrdMin,0)+ISNULL(e.NightMin,0)+ISNULL(e.HolMin,0)) AS DECIMAL(10,2))/60.0 AS Hours,
      CASE 
        WHEN ISNULL(e.HolMin,0)   > 0 THEN 'Feriado'
        WHEN ISNULL(e.NightMin,0) > 0 THEN 'Nocturna'
        ELSE 'Ordinaria'
      END AS OvertimeType
    FROM ExecPerDay e
    WHERE (ISNULL(e.OrdMin,0)+ISNULL(e.NightMin,0)+ISNULL(e.HolMin,0)) > 0
  )
  MERGE HR.tbl_Overtime AS T
  USING (
    SELECT r.EmployeeID, r.WorkDate, r.Hours, r.OvertimeType,
           COALESCE(oc.Factor, 1.0) AS Factor
    FROM Resolved r
    LEFT JOIN HR.tbl_OvertimeConfig oc ON oc.OvertimeType = r.OvertimeType
  ) AS S
    ON (T.EmployeeID = S.EmployeeID AND T.WorkDate = S.WorkDate)
  WHEN MATCHED THEN
    UPDATE SET
      T.OvertimeType = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.OvertimeType ELSE S.OvertimeType END,
      T.Hours        = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Hours        ELSE S.Hours        END,
      T.ActualHours  = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.ActualHours  ELSE S.Hours        END,
      T.Factor       = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Factor       ELSE S.Factor       END,
      T.Status       = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Status ELSE 'EXECUTED' END --,
      --T.UpdatedAt    = GETDATE()
  WHEN NOT MATCHED THEN
    INSERT (EmployeeID, WorkDate, OvertimeType, Hours, ActualHours, Factor, Status, CreatedAt)
    VALUES (S.EmployeeID, S.WorkDate, S.OvertimeType, S.Hours, S.Hours, S.Factor, 'EXECUTED', GETDATE());

  IF @Debug=1 PRINT 'Consolidación completada.';
END

GO

-- [fn_ResolveEmployeeRate]

/*
  HR.fn_ResolveEmployeeRate
  ==========================
  2026-07-06 (Fase 2 de la propuesta de tarifa de nómina/multi-régimen).

  Resuelve la tarifa horaria de un empleado vigente en @AsOfDate, considerando
  DOS rutas independientes que nunca se mezclan entre sí:

    - CONTRATO: contrato en HR.tbl_Contracts cuyo rango de fechas cubre
      @AsOfDate (no HOY como hacía el código anterior). Si hay varios
      contratos superpuestos en el mismo régimen (dato inconsistente o
      histórico previo al control de la Fase 1 que anula al padre al firmar
      un adendum), se prioriza el que NO esté ANULADO y, en empate, el más
      reciente por ContractID.
    - NOMBRAMIENTO: HR.tbl_PersonnelActions con Status='FIRMADO_CARGADO'
      (acción ya formalizada, no un borrador) y EffectiveDate <= @AsOfDate,
      usando NewRmu. Es independiente de tbl_Contracts — antes esta fuente
      nunca se consultaba, así que un cambio de sueldo por acción de personal
      sin contrato nuevo asociado quedaba invisible para nómina/horas extra.

  Un empleado puede tener las dos rutas activas a la vez (ej. nombramiento
  LOSEP + contrato de docencia LOES) — son regímenes distintos, no una
  duplicación. Esta función devuelve TODAS las filas candidatas (una por
  régimen/ruta), sin colapsar — cada procedimiento consumidor decide su
  propio criterio:
    - sp_Overtime_Price filtra siempre LaborRegimeID=57 (LOSEP), porque solo
      ese régimen genera horas extra (confirmado 2026-07-06).
    - sp_Payroll_Discounts/sp_Payroll_Subsidies, que todavía no tienen columna
      de régimen en su tabla destino (tbl_PayrollLines no separada para estos
      conceptos), colapsan ellos mismos al régimen IsPrincipal usando la
      columna IsPrincipal que esta función ya expone.

  2026-07-06: @EmployeeID opcional (NULL = todos los empleados, comportamiento
  igual que antes — usado por los procedimientos de nómina que procesan un
  período completo). Con valor, acota el cálculo a un solo empleado desde el
  origen (ContractCandidates/ActionCandidates), en vez de calcular para los
  667 empleados y descartar el resto después — pensado para consultas
  puntuales de un empleado específico.
*/
CREATE OR ALTER FUNCTION HR.fn_ResolveEmployeeRate (@AsOfDate DATE, @EmployeeID INT = NULL)
RETURNS TABLE
AS
RETURN
(
    WITH BaseHours AS (
        -- MAX(...) sin GROUP BY siempre devuelve exactamente 1 fila (NULL si no
        -- hay parámetro), a diferencia de un SELECT plano que devolvería 0 filas
        -- y anularía todo el resultado vía el CROSS JOIN de más abajo.
        SELECT MAX(TRY_CAST(Pvalues AS INT)) AS BaseHoursPerDay
        FROM HR.tbl_Parameters WHERE name = 'BASE_HOURS_PER_DAY'
    ),
    ContractCandidates AS (
        SELECT
            c.PersonID,
            c.ContractID,
            c.LaborRegimeID,
            c.JobID,
            ROW_NUMBER() OVER (
                PARTITION BY c.PersonID, c.LaborRegimeID
                ORDER BY CASE WHEN rt.Name = 'ANULADO' THEN 1 ELSE 0 END ASC, c.ContractID DESC
            ) AS rn
        FROM HR.tbl_Contracts c
        LEFT JOIN HR.ref_Types rt ON rt.TypeId = c.Status AND rt.Category = 'CONTRACT_STATUS'
        WHERE c.StartDate <= @AsOfDate
          AND (c.EndDate IS NULL OR c.EndDate >= @AsOfDate)
          AND (@EmployeeID IS NULL OR EXISTS (
              SELECT 1 FROM HR.tbl_Employees e2
              WHERE e2.PersonID = c.PersonID AND e2.EmployeeID = @EmployeeID
          ))
    ),
    ContractRate AS (
        SELECT
            e.EmployeeID,
            cc.LaborRegimeID,
            og.RMU,
            'CONTRATO' AS SourceType
        FROM ContractCandidates cc
        JOIN HR.tbl_Employees e ON e.PersonID = cc.PersonID
        LEFT JOIN HR.tbl_jobs j ON j.JobID = cc.JobID
        LEFT JOIN HR.tbl_Occupational_Groups og ON og.GroupID = j.GroupID
        WHERE cc.rn = 1 AND og.RMU IS NOT NULL
    ),
    ActionCandidates AS (
        SELECT
            pa.EmployeeID,
            pa.ActionID,
            pa.NewRmu,
            ROW_NUMBER() OVER (
                PARTITION BY pa.EmployeeID
                ORDER BY pa.EffectiveDate DESC, pa.ActionID DESC
            ) AS rn
        FROM HR.tbl_PersonnelActions pa
        -- 2026-07-06: VIGENTE reemplaza a FIRMADO_CARGADO como señal de "esta acción
        -- es la actual" — antes se usaba una heurística de "la más reciente por fecha"
        -- porque no existía un estado explícito de vigencia; ahora sí existe (ver
        -- PersonnelActionService.ReachesVigente). El ROW_NUMBER se conserva como
        -- respaldo defensivo, aunque solo debería haber una VIGENTE por empleado.
        WHERE pa.Status = 'VIGENTE'
          AND pa.EffectiveDate <= @AsOfDate
          AND pa.NewRmu IS NOT NULL
          AND (@EmployeeID IS NULL OR pa.EmployeeID = @EmployeeID)
    ),
    ActionRate AS (
        SELECT
            ac.EmployeeID,
            -- 57=LOSEP: un nombramiento sin enlace explícito a tbl_EmployeeLaborRegime
            -- se asume LOSEP por default (es el uso típico del término en el dominio).
            ISNULL(elr.LaborRegimeId, 57) AS LaborRegimeID,
            ac.NewRmu AS RMU,
            'NOMBRAMIENTO' AS SourceType
        FROM ActionCandidates ac
        LEFT JOIN HR.tbl_EmployeeLaborRegime elr ON elr.SourcePersonnelActionId = ac.ActionID
        WHERE ac.rn = 1
    ),
    Combined AS (
        SELECT * FROM ContractRate
        UNION ALL
        SELECT * FROM ActionRate
    )
    SELECT
        c.EmployeeID,
        c.LaborRegimeID,
        c.RMU,
        -- 2026-07-06: HR.tbl_Parameters nunca tuvo una fila BASE_HOURS_PER_DAY
        -- (confirmado: 0 filas en producción). Antes esto hacía que
        -- @BaseHoursPerDay fuera NULL y el HourRate saliera silenciosamente
        -- NULL para todos; aquí se usa 8 como respaldo temporal explícito
        -- hasta que se confirme/siembre el valor real del parámetro.
        CAST((c.RMU / (ISNULL(bh.BaseHoursPerDay, 8) * 30.0)) AS DECIMAL(12,4)) AS HourRate,
        c.SourceType,
        ISNULL(elr.IsPrincipal, 0) AS IsPrincipal
    FROM Combined c
    CROSS JOIN BaseHours bh
    LEFT JOIN HR.tbl_EmployeeLaborRegime elr
        ON elr.EmployeeId = c.EmployeeID
       AND elr.LaborRegimeId = c.LaborRegimeID
       AND elr.IsActive = 1
);
GO

-- [sp_Overtime_Price]


/*Horas extra: valuación y líneas de pago

	Base: HR.tbl_Overtime (planificadas/ejecutadas con factor en HR.tbl_OvertimeConfig).

	Valor hora: RMU / (BASE_HOURS_PER_DAY * 30) o desde Payroll.
	*/
	CREATE OR ALTER PROCEDURE HR.sp_Overtime_Price
  @Period CHAR(7) -- 'YYYY-MM'
AS
BEGIN
  -- 2026-07-06 (Fase 2): la tarifa se resuelve al último día de @Period, no al
  -- día de hoy — ver HR.fn_ResolveEmployeeRate para el detalle de las 2 rutas
  -- (Contrato/Nombramiento) que reemplazan el TOP 1 + GETDATE() anterior.
  -- 2026-07-06 (Fase 3): solo el régimen LOSEP (57) genera horas extra
  -- (confirmado), así que se filtra explícitamente por régimen y NO por
  -- IsPrincipal — un docente LOES con nombramiento LOSEP secundario igual
  -- debe cobrar horas extra por su parte LOSEP. MAX() colapsa el caso raro
  -- de que existan a la vez un contrato y un nombramiento LOSEP.
  DECLARE @AsOfDate DATE = EOMONTH(CAST(@Period + '-01' AS DATE));

  ;WITH rmu AS (
    SELECT EmployeeID, MAX(HourRate) AS HourRate
    FROM HR.fn_ResolveEmployeeRate(@AsOfDate, DEFAULT)
    WHERE LaborRegimeID = 57
    GROUP BY EmployeeID
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
  -- 2026-07-06: el ON 1=0 original nunca hacía match, así que cada reproceso
  -- del mismo período insertaba líneas duplicadas. Se agrega llave real
  -- (PayrollID+LineType+Concept), respaldada por UQ_PayrollLines_Payroll_Line_Concept.
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
  ) S
    ON T.PayrollID = S.PayrollID
   AND T.LineType  = S.LineType
   AND T.Concept   = S.Concept
  WHEN MATCHED THEN
    UPDATE SET T.Quantity = S.Quantity, T.UnitValue = S.UnitValue, T.LaborRegimeId = 57
  WHEN NOT MATCHED THEN
    -- 2026-07-06 (Fase 3): LaborRegimeId=57 (LOSEP) siempre — ver nota arriba.
    INSERT (PayrollID, LineType, Concept, Quantity, UnitValue, LaborRegimeId)
    VALUES (S.PayrollID, S.LineType, S.Concept, S.Quantity, S.UnitValue, 57);

  DROP TABLE #OTPrice;
END

GO

-- [sp_Payroll_Discounts]


--Descuentos y subsidios (nómina)
--E1) Descuento por atrasos/ausencias
CREATE OR ALTER PROCEDURE HR.sp_Payroll_Discounts
  @Period CHAR(7)
AS
BEGIN
  -- 2026-07-06 (Fase 2): mismo cambio que sp_Overtime_Price — ver comentario ahí.
  -- 2026-07-06 (Fase 3): este descuento SÍ se colapsa al régimen principal
  -- (IsPrincipal), a diferencia de horas extra — tbl_PayrollLines todavía no
  -- separa este concepto por régimen (pendiente, alcance mayor al esperado,
  -- ver conversación de la Fase 3).
  DECLARE @AsOfDate DATE = EOMONTH(CAST(@Period + '-01' AS DATE));
  DECLARE @TardyRate DECIMAL(6,2) = CAST((SELECT Pvalues FROM HR.tbl_Parameters WHERE name='TARDINESS_DISCOUNT_RATE') AS DECIMAL(6,2));

  ;WITH rmu AS (
    SELECT EmployeeID, HourRate FROM HR.fn_ResolveEmployeeRate(@AsOfDate, DEFAULT) WHERE IsPrincipal = 1
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
  -- 2026-07-06: mismo fix de llave real que sp_Overtime_Price (ver comentario ahí).
  MERGE HR.tbl_PayrollLines AS T
  USING (
    SELECT PayrollID,'Deduction' AS LineType,'Descuento por atrasos/ausencias' AS Concept, QtyHours AS Quantity, UnitValue
    FROM #Disc WHERE QtyHours>0
  ) S
    ON T.PayrollID = S.PayrollID
   AND T.LineType  = S.LineType
   AND T.Concept   = S.Concept
  WHEN MATCHED THEN
    UPDATE SET T.Quantity = S.Quantity, T.UnitValue = S.UnitValue
  WHEN NOT MATCHED THEN
    INSERT (PayrollID, LineType, Concept, Quantity, UnitValue)
    VALUES (S.PayrollID, S.LineType, S.Concept, S.Quantity, S.UnitValue);

  DROP TABLE #Disc;
END

GO

-- [sp_Payroll_Subsidies]

--E2)Subsidios/recargos (nocturno/feriado)

--(agrega líneas positivas tipo “Subsidy” si tu política paga recargos)

CREATE OR ALTER PROCEDURE HR.sp_Payroll_Subsidies
  @Period CHAR(7)
AS
BEGIN
  -- 2026-07-06: fix de 3 bugs encontrados en auditoría de subsidios:
  --  1) Faltaba el MERGE hacia tbl_PayrollLines (terminaba en un SELECT
  --     suelto que el C# descartaba con ExecuteNonQueryAsync) — nunca se
  --     guardaba nada. Ahora sigue el mismo patrón que sp_Overtime_Price.
  --  2) El CASE nocturno/feriado era mutuamente excluyente — un empleado
  --     con ambos tipos en el mismo período perdía uno. Ahora se genera
  --     una fila por cada tipo (UNION ALL), igual que sp_Overtime_Price
  --     agrupa por OvertimeType.
  --  3) UnitValue no aplicaba ningún factor de recargo. Ahora reutiliza
  --     los factores ya configurados en HR.tbl_OvertimeConfig (Nocturna,
  --     Feriado) en vez de pagar ambos a la tarifa base.
  --  4) (2026-07-06, Fase 2) tarifa resuelta con GETDATE()/TOP1 en vez del
  --     período — mismo cambio que sp_Overtime_Price, ver comentario ahí.
  --  5) (2026-07-06, Fase 3) colapsa al régimen principal (IsPrincipal),
  --     mismo criterio que sp_Payroll_Discounts — no separado por régimen aún.
  DECLARE @AsOfDate DATE = EOMONTH(CAST(@Period + '-01' AS DATE));

  ;WITH rmu AS (
    SELECT EmployeeID, HourRate FROM HR.fn_ResolveEmployeeRate(@AsOfDate, DEFAULT) WHERE IsPrincipal = 1
  ),
  agg AS (
    SELECT EmployeeID,
      SUM(CASE WHEN CONVERT(CHAR(7),WorkDate,126)=@Period THEN NightMinutes ELSE 0 END)/60.0 AS NightHours,
      SUM(CASE WHEN CONVERT(CHAR(7),WorkDate,126)=@Period THEN HolidayMinutes ELSE 0 END)/60.0 AS HolidayHours
    FROM HR.tbl_AttendanceCalculations
    GROUP BY EmployeeID
  ),
  sub AS (
    SELECT a.EmployeeID,
           'Recargo nocturno' AS Concept,
           a.NightHours AS Quantity,
           r.HourRate * ISNULL(ocN.Factor, 1.0) AS UnitValue
    FROM agg a
    JOIN rmu r ON r.EmployeeID = a.EmployeeID
    LEFT JOIN HR.tbl_OvertimeConfig ocN ON ocN.OvertimeType = 'Nocturna'
    WHERE a.NightHours > 0

    UNION ALL

    SELECT a.EmployeeID,
           'Recargo feriado' AS Concept,
           a.HolidayHours AS Quantity,
           r.HourRate * ISNULL(ocF.Factor, 1.0) AS UnitValue
    FROM agg a
    JOIN rmu r ON r.EmployeeID = a.EmployeeID
    LEFT JOIN HR.tbl_OvertimeConfig ocF ON ocF.OvertimeType = 'Feriado'
    WHERE a.HolidayHours > 0
  )
  -- 2026-07-06: mismo fix de llave real que sp_Overtime_Price (ver comentario ahí).
  MERGE HR.tbl_PayrollLines AS T
  USING (
    SELECT p.PayrollID, s.Concept, s.Quantity, s.UnitValue
    FROM HR.tbl_Payroll p
    JOIN sub s ON s.EmployeeID = p.EmployeeID
    WHERE p.Period = @Period
  ) S
    ON T.PayrollID = S.PayrollID
   AND T.LineType  = 'Subsidy'
   AND T.Concept   = S.Concept
  WHEN MATCHED THEN
    UPDATE SET T.Quantity = S.Quantity, T.UnitValue = S.UnitValue
  WHEN NOT MATCHED THEN
    INSERT (PayrollID, LineType, Concept, Quantity, UnitValue)
    VALUES (S.PayrollID, 'Subsidy', S.Concept, S.Quantity, S.UnitValue);
END

GO

-- [sp_GetConsolidatedRemunerationReport]

/*
  HR.sp_GetConsolidatedRemunerationReport
  =========================================
  2026-07-06 (Fase 4 de la propuesta de tarifa de nómina/multi-régimen).

  Reporte de solo lectura: consolida, por empleado, el total a pagar/descontar
  del período a partir de las líneas YA CALCULADAS en tbl_PayrollLines
  (generadas por sp_Overtime_Price, sp_Payroll_Discounts, sp_Payroll_Subsidies).
  No recalcula nada — es una vista de presentación sobre datos que ya viven
  separados por régimen cuando aplica (hoy solo Overtime, siempre LOSEP=57;
  Deduction/Subsidy siguen sin separar, ver Fase 3).

  Devuelve dos resultsets:
    1) Detalle por empleado + régimen + tipo de línea (para auditar de dónde
       sale cada monto).
    2) Total consolidado por empleado (suma de todas sus líneas, sin importar
       régimen) — el número final a pagar que pediste poder ver consolidado.
*/
CREATE OR ALTER PROCEDURE HR.sp_GetConsolidatedRemunerationReport
(
    @Period     CHAR(7),
    @EmployeeID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Lines AS (
        SELECT
            p.EmployeeID,
            pl.LaborRegimeId,
            rt.Name AS LaborRegimeName,
            pl.LineType,
            pl.Concept,
            pl.Quantity,
            pl.UnitValue,
            CAST(pl.Quantity * pl.UnitValue AS DECIMAL(12,2)) AS Amount
        FROM HR.tbl_PayrollLines pl
        JOIN HR.tbl_Payroll p ON p.PayrollID = pl.PayrollID
        LEFT JOIN HR.ref_Types rt ON rt.TypeId = pl.LaborRegimeId AND rt.Category = 'CONTRACT_TYPE'
        WHERE p.Period = @Period
          AND (@EmployeeID IS NULL OR p.EmployeeID = @EmployeeID)
    )
    SELECT
        l.EmployeeID,
        p.FirstName + ' ' + p.LastName AS EmployeeName,
        ISNULL(l.LaborRegimeName, 'Sin régimen asignado') AS LaborRegimeName,
        l.LineType,
        l.Concept,
        l.Quantity,
        l.UnitValue,
        -- Overtime (Concept 'Overtime') suma; Deduction resta en el total consolidado
        CASE WHEN l.LineType = 'Deduction' THEN -l.Amount ELSE l.Amount END AS SignedAmount
    INTO #Detail
    FROM Lines l
    JOIN HR.tbl_Employees e ON e.EmployeeID = l.EmployeeID
    JOIN HR.tbl_People p ON p.PersonID = e.PersonID;

    -- Resultset 1: detalle por régimen/línea
    SELECT * FROM #Detail
    ORDER BY EmployeeID, LaborRegimeName, LineType, Concept;

    -- Resultset 2: total consolidado por empleado (todas las líneas, todos los régimenes)
    SELECT
        EmployeeID,
        EmployeeName,
        CAST(SUM(SignedAmount) AS DECIMAL(12,2)) AS TotalAPagar
    FROM #Detail
    GROUP BY EmployeeID, EmployeeName
    ORDER BY EmployeeID;

    DROP TABLE #Detail;
END

GO

-- [sp_ProcessAttendanceBaseDay]
/*-----  HR.sp_ProcessAttendanceBaseDay -*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceBaseDay
(
    @EmployeeID INT,
    @WorkDate DATE,
    @GraceMin INT,
    @OTMin INT,
    @NightStart TIME,
    @NightEnd TIME,
    @ContractType NVARCHAR(100) = NULL,
    @IsHoliday BIT = 0,
    @IsWeekend BIT = 0,
    @ScheduleID INT = NULL,
    @EntryTime TIME = NULL,
    @ExitTime TIME = NULL,
    @HasLunch BIT = NULL,
    @LunchStartT TIME = NULL,
    @LunchEndT TIME = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceBaseDay
      DESCRIPCIÓN:
          Calcula la asistencia base de un empleado en una fecha específica.

      RESPONSABILIDADES:
          - Obtener horario vigente del empleado
          - Obtener marcaciones del día
          - Calcular minutos trabajados
          - Calcular minutos dentro y fuera del horario
          - Calcular atrasos bruto/neto
          - Calcular ausencia, salida anticipada, nocturnidad y feriado
          - Persistir snapshot del horario aplicado
          - Crear/actualizar HR.tbl_AttendanceCalculations

      NOTAS:
          - No aplica permisos, vacaciones ni justificaciones.
          - No calcula recovery ni planning.
          - Los minutos de gracia se leen desde HR.tbl_Parameters.
    **********************************************************************/

    /* 
       Si el horario ya fue precargado por HR.sp_ProcessAttendanceRunDate,
       se usa directamente. Si no, se hace fallback a la consulta original.
    */
    IF @ScheduleID IS NULL
    BEGIN
        SELECT TOP 1
            @ScheduleID  = es.ScheduleID,
            @EntryTime   = s.EntryTime,
            @ExitTime    = s.ExitTime,
            @HasLunch    = s.HasLunchBreak,
            @LunchStartT = s.LunchStart,
            @LunchEndT   = s.LunchEnd
        FROM HR.tbl_EmployeeSchedules es
        INNER JOIN HR.tbl_Schedules s
            ON s.ScheduleID = es.ScheduleID
        WHERE es.EmployeeID = @EmployeeID
          AND es.ValidFrom <= @WorkDate
          AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
        ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC;
    END;

    IF @ScheduleID IS NULL
        RETURN;

    DECLARE
        @BaseDate      DATETIME2 = CAST(@WorkDate AS DATETIME2),
        @ShiftStart    DATETIME2,
        @ShiftEnd      DATETIME2,
        @LunchStart    DATETIME2 = NULL,
        @LunchEnd      DATETIME2 = NULL,
        @NightStartDT  DATETIME2 = NULL,
        @NightEndDT    DATETIME2 = NULL,
        @RequiredMin   INT       = 0;

    SET @ShiftStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @EntryTime), @BaseDate);

    IF (@ExitTime <= @EntryTime)
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), DATEADD(DAY, 1, @BaseDate));
    ELSE
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), @BaseDate);

    IF (@HasLunch = 1 AND @LunchStartT IS NOT NULL AND @LunchEndT IS NOT NULL)
    BEGIN
        SET @LunchStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchStartT), @BaseDate);
        SET @LunchEnd   = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchEndT), @BaseDate);

        IF (@LunchEndT <= @LunchStartT)
            SET @LunchEnd = DATEADD(DAY, 1, @LunchEnd);
    END;

    /* ------------------------------------------------------------------
       PUNTO 9 — Partición en 2 jornadas (mañana/tarde).
       Guardia de seguridad: solo se activa si el almuerzo es confiable
       (no NULL, cae dentro del turno y dura 4h o menos). Si los datos
       de almuerzo están mal cargados (ej. LunchEnd<=LunchStart por error
       de captura, que ya se interpretó arriba como cruce de medianoche),
       @SplitJourneys queda en 0 y el cálculo cae al camino de jornada
       única de siempre (comportamiento sin cambios).
    ------------------------------------------------------------------ */
    DECLARE @SplitJourneys BIT = 0;

    IF (@HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL
        AND @LunchStart >= @ShiftStart AND @LunchEnd <= @ShiftEnd
        AND DATEDIFF(MINUTE, @LunchStart, @LunchEnd) BETWEEN 1 AND 240)
        SET @SplitJourneys = 1;

    IF (@NightStart IS NOT NULL AND @NightEnd IS NOT NULL)
    BEGIN
        SET @NightStartDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightStart), @BaseDate);

        IF (@NightEnd > @NightStart)
            SET @NightEndDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd), @BaseDate);
        ELSE
            SET @NightEndDT = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @NightEnd), DATEADD(DAY, 1, @BaseDate));
    END;

    SET @RequiredMin = DATEDIFF(MINUTE, @ShiftStart, @ShiftEnd)
                     - CASE
                           WHEN @HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL
                               THEN DATEDIFF(MINUTE, @LunchStart, @LunchEnd)
                           ELSE 0
                       END;

    IF @RequiredMin < 0 SET @RequiredMin = 0;

    DECLARE
        @RequiredMorningMin   INT = 0,
        @RequiredAfternoonMin INT = 0;

    IF (@SplitJourneys = 1)
    BEGIN
        SET @RequiredMorningMin   = DATEDIFF(MINUTE, @ShiftStart, @LunchStart);
        SET @RequiredAfternoonMin = DATEDIFF(MINUTE, @LunchEnd, @ShiftEnd);
        IF @RequiredMorningMin   < 0 SET @RequiredMorningMin   = 0;
        IF @RequiredAfternoonMin < 0 SET @RequiredAfternoonMin = 0;
    END;

    DECLARE
        @WindowStart DATETIME2 = DATEADD(HOUR, -4, @ShiftStart),
        @WindowEnd   DATETIME2 = DATEADD(HOUR,  4, @ShiftEnd);

    DROP TABLE IF EXISTS #Punches;
    DROP TABLE IF EXISTS #Segments;

    ;WITH PunchesOrdered AS
    (
        SELECT
            ap.PunchTime,
            ap.PunchType,
            ROW_NUMBER() OVER (ORDER BY ap.PunchTime) AS rn
        FROM HR.tbl_AttendancePunches ap
        WHERE ap.EmployeeID = @EmployeeID
          AND ap.PunchTime >= @WindowStart
          AND ap.PunchTime <= @WindowEnd
    )
    SELECT *
    INTO #Punches
    FROM PunchesOrdered;

    IF EXISTS (SELECT 1 FROM #Punches)
    BEGIN
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
                DELETE FROM #Punches WHERE rn = 1;
        END;
    END;

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

    DECLARE
        @FirstIn DATETIME2 = NULL,
        @LastOut DATETIME2 = NULL,
        @TotalWorkedSegments FLOAT = 0,
        @InsideShift FLOAT = 0,
        @InsideWorkBands FLOAT = 0,
        @InsideBlock1Sum FLOAT = 0,
        @InsideBlock2Sum FLOAT = 0,
        @NightMinutes INT = 0,
        @InsideMinutes FLOAT = 0,
        @OutsideMinutes FLOAT = 0,
        @AbsentMinutes INT = 0,
        @MinutesLate INT = 0,
        @TardinessMin INT = 0,
        @EarlyLeaveMinutes INT = 0,
        @OvertimeWithinSchedule FLOAT = 0,
        @OvertimeMinutes INT = 0,
        @RegularMinutes INT = 0,
        @OffScheduleMin INT = 0,
        @TotalWorkedMinutes INT = 0,
        @FoodSubsidy INT = 0,
        @HolidayMinutes INT = 0,
        @FirstInInside DATETIME2 = NULL,
        @MorningHasSegment BIT = 0,
        @AfternoonHasSegment BIT = 0,
        @MorningLateRaw INT = 0,
        @AfternoonLateRaw INT = 0,
        @MorningTardiness INT = 0,
        @AfternoonTardiness INT = 0;

    IF EXISTS (SELECT 1 FROM #Segments)
    BEGIN
        SELECT
            @FirstIn = MIN(StartTime),
            @LastOut = MAX(EndTime)
        FROM #Segments;

        ;WITH SegCalc AS
        (
            SELECT
                StartTime,
                EndTime,
                CAST(DATEDIFF(MINUTE, StartTime, EndTime) AS FLOAT) AS SegmentMinutes,

                CASE
                    WHEN EndTime <= @ShiftStart OR StartTime >= @ShiftEnd THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @ShiftEnd   THEN EndTime   ELSE @ShiftEnd   END
                    ) AS FLOAT)
                END AS InsideShiftMinutes,

                CASE
                    WHEN @HasLunch = 0 OR @LunchStart IS NULL THEN 0
                    WHEN EndTime <= @ShiftStart OR StartTime >= @LunchStart THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @ShiftStart THEN StartTime ELSE @ShiftStart END,
                        CASE WHEN EndTime   < @LunchStart THEN EndTime   ELSE @LunchStart END
                    ) AS FLOAT)
                END AS InsideBlock1,

                CASE
                    WHEN @HasLunch = 0 OR @LunchEnd IS NULL THEN 0
                    WHEN EndTime <= @LunchEnd OR StartTime >= @ShiftEnd THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @LunchEnd THEN StartTime ELSE @LunchEnd END,
                        CASE WHEN EndTime   < @ShiftEnd THEN EndTime   ELSE @ShiftEnd END
                    ) AS FLOAT)
                END AS InsideBlock2,

                CASE
                    WHEN @NightStartDT IS NULL OR @NightEndDT IS NULL THEN 0
                    WHEN EndTime <= @NightStartDT OR StartTime >= @NightEndDT THEN 0
                    ELSE CAST(DATEDIFF(
                        MINUTE,
                        CASE WHEN StartTime > @NightStartDT THEN StartTime ELSE @NightStartDT END,
                        CASE WHEN EndTime   < @NightEndDT   THEN EndTime   ELSE @NightEndDT   END
                    ) AS FLOAT)
                END AS NightMinutes
            FROM #Segments
        )
        SELECT
            @TotalWorkedSegments = ISNULL(SUM(SegmentMinutes), 0),
            @InsideShift         = ISNULL(SUM(InsideShiftMinutes), 0),
            @InsideWorkBands     = ISNULL(SUM(InsideBlock1 + InsideBlock2), 0),
            @InsideBlock1Sum     = ISNULL(SUM(InsideBlock1), 0),
            @InsideBlock2Sum     = ISNULL(SUM(InsideBlock2), 0),
            @NightMinutes        = ISNULL(CAST(SUM(NightMinutes) AS INT), 0)
        FROM SegCalc;

        IF (@HasLunch = 1 AND @LunchStart IS NOT NULL AND @LunchEnd IS NOT NULL)
            SET @InsideMinutes = @InsideWorkBands;
        ELSE
            SET @InsideMinutes = @InsideShift;

        SET @OutsideMinutes = @TotalWorkedSegments - @InsideMinutes;
        IF @OutsideMinutes < 0 SET @OutsideMinutes = 0;

        /* PUNTO 9: qué jornada tiene al menos un segmento de marcación
           (solo se evalúa cuando la partición en 2 jornadas está activa). */
        IF (@SplitJourneys = 1)
        BEGIN
            SET @MorningHasSegment = CASE WHEN EXISTS (
                SELECT 1 FROM #Segments WHERE EndTime > @ShiftStart AND StartTime < @LunchStart
            ) THEN 1 ELSE 0 END;

            SET @AfternoonHasSegment = CASE WHEN EXISTS (
                SELECT 1 FROM #Segments WHERE EndTime > @LunchEnd AND StartTime < @ShiftEnd
            ) THEN 1 ELSE 0 END;
        END;

        /* PUNTO 9 — AUSENCIA: con partición en 2 jornadas, cada jornada
           contribuye a la ausencia de forma independiente (si a una le
           falta cobertura, no se penaliza el día completo). Sin partición
           válida, se mantiene el cálculo combinado de siempre. */
        IF (@SplitJourneys = 1)
        BEGIN
            SET @AbsentMinutes =
                (CASE WHEN @InsideBlock1Sum < @RequiredMorningMin
                      THEN @RequiredMorningMin - CAST(@InsideBlock1Sum AS INT) ELSE 0 END)
              + (CASE WHEN @InsideBlock2Sum < @RequiredAfternoonMin
                      THEN @RequiredAfternoonMin - CAST(@InsideBlock2Sum AS INT) ELSE 0 END);
        END
        ELSE
        BEGIN
            IF (@InsideMinutes < @RequiredMin)
                SET @AbsentMinutes = @RequiredMin - CAST(@InsideMinutes AS INT);
            ELSE
                SET @AbsentMinutes = 0;
        END;

        /* PUNTO 9 — TARDANZA / SALIDA ANTICIPADA: con partición en 2
           jornadas, cada una se evalúa contra su propio inicio/fin, y
           solo si tiene al menos un segmento (si no tiene marcación ya
           se contó como ausencia arriba, no se duplica como atraso). */
        IF (@SplitJourneys = 1)
        BEGIN
            IF (@MorningHasSegment = 1)
            BEGIN
                SELECT TOP 1 @FirstInInside =
                    CASE WHEN s.StartTime <= @ShiftStart AND s.EndTime > @ShiftStart THEN @ShiftStart ELSE s.StartTime END
                FROM #Segments s
                WHERE s.EndTime > @ShiftStart AND s.StartTime < @LunchStart
                ORDER BY s.StartTime;

                IF (@FirstInInside IS NOT NULL)
                BEGIN
                    SET @MorningLateRaw = DATEDIFF(MINUTE, @ShiftStart, @FirstInInside);
                    IF @MorningLateRaw < 0 SET @MorningLateRaw = 0;
                END;
            END;

            SET @FirstInInside = NULL;

            IF (@AfternoonHasSegment = 1)
            BEGIN
                SELECT TOP 1 @FirstInInside =
                    CASE WHEN s.StartTime <= @LunchEnd AND s.EndTime > @LunchEnd THEN @LunchEnd ELSE s.StartTime END
                FROM #Segments s
                WHERE s.EndTime > @LunchEnd AND s.StartTime < @ShiftEnd
                ORDER BY s.StartTime;

                IF (@FirstInInside IS NOT NULL)
                BEGIN
                    SET @AfternoonLateRaw = DATEDIFF(MINUTE, @LunchEnd, @FirstInInside);
                    IF @AfternoonLateRaw < 0 SET @AfternoonLateRaw = 0;
                END;
            END;

            SET @MorningTardiness = @MorningLateRaw - @GraceMin;
            IF @MorningTardiness < 0 SET @MorningTardiness = 0;

            SET @AfternoonTardiness = @AfternoonLateRaw - @GraceMin;
            IF @AfternoonTardiness < 0 SET @AfternoonTardiness = 0;

            SET @MinutesLate  = @MorningLateRaw + @AfternoonLateRaw;
            SET @TardinessMin = @MorningTardiness + @AfternoonTardiness;

            -- Salida anticipada: contra el fin de la tarde si tiene marcación;
            -- si la tarde no marcó pero la mañana sí, contra el fin de la mañana.
            IF (@AfternoonHasSegment = 1)
            BEGIN
                IF (@LastOut IS NOT NULL AND @LastOut < @ShiftEnd)
                    SET @EarlyLeaveMinutes = DATEDIFF(MINUTE, @LastOut, @ShiftEnd);
                ELSE
                    SET @EarlyLeaveMinutes = 0;
            END
            ELSE IF (@MorningHasSegment = 1)
            BEGIN
                IF (@LastOut IS NOT NULL AND @LastOut < @LunchStart)
                    SET @EarlyLeaveMinutes = DATEDIFF(MINUTE, @LastOut, @LunchStart);
                ELSE
                    SET @EarlyLeaveMinutes = 0;
            END
            ELSE
                SET @EarlyLeaveMinutes = 0;
        END
        ELSE
        BEGIN
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
            SELECT @FirstInInside = FirstInInside
            FROM FirstInInsideCTE;

            IF (@FirstInInside IS NOT NULL)
            BEGIN
                SET @MinutesLate = DATEDIFF(MINUTE, @ShiftStart, @FirstInInside);
                IF @MinutesLate < 0 SET @MinutesLate = 0;
            END;

            SET @TardinessMin = @MinutesLate - @GraceMin;
            IF @TardinessMin < 0 SET @TardinessMin = 0;

            IF (@LastOut IS NOT NULL AND @LastOut < @ShiftEnd)
                SET @EarlyLeaveMinutes = DATEDIFF(MINUTE, @LastOut, @ShiftEnd);
            ELSE
                SET @EarlyLeaveMinutes = 0;
        END;

        IF (@InsideMinutes > @RequiredMin)
            SET @OvertimeWithinSchedule = @InsideMinutes - @RequiredMin;
        ELSE
            SET @OvertimeWithinSchedule = 0;

        IF (@OvertimeWithinSchedule < @OTMin)
            SET @OvertimeWithinSchedule = 0;

        SET @OffScheduleMin = CAST(@OutsideMinutes AS INT);
        SET @OvertimeMinutes = CAST(@OvertimeWithinSchedule + @OutsideMinutes AS INT);
        SET @RegularMinutes = CAST(@InsideMinutes - @OvertimeWithinSchedule AS INT);
        IF @RegularMinutes < 0 SET @RegularMinutes = 0;
        SET @TotalWorkedMinutes = CAST(@InsideMinutes + @OutsideMinutes AS INT);
    END
    ELSE
    BEGIN
        SET @AbsentMinutes = @RequiredMin;
    END;

    SET @HolidayMinutes =
        CASE WHEN @IsHoliday = 1 OR @IsWeekend = 1
             THEN @TotalWorkedMinutes
             ELSE 0
        END;

    IF (@ContractType = N'Código Trabajo' AND (@RegularMinutes + @TardinessMin) >= @RequiredMin)
        SET @FoodSubsidy = 1;
    ELSE
        SET @FoodSubsidy = 0;

    MERGE HR.tbl_AttendanceCalculations AS T
    USING (SELECT @EmployeeID AS EmployeeID, @WorkDate AS WorkDate) AS S
       ON T.EmployeeID = S.EmployeeID
      AND T.WorkDate   = S.WorkDate
    WHEN MATCHED THEN
        UPDATE SET
            FirstPunchIn = @FirstIn,
            LastPunchOut = @LastOut,
            TotalWorkedMinutes = @TotalWorkedMinutes,
            RegularMinutes = @RegularMinutes,
            OvertimeMinutes = @OvertimeMinutes,
            -- 2026-07-06: se preserva aquí la detección automática (trabajado
            -- fuera de horario) ANTES de que sp_ProcessTimePlanningForEmployeeDay
            -- sobreescriba OvertimeMinutes más adelante en el pipeline con el
            -- monto verificado/autorizado (el que realmente se paga). Sin este
            -- campo, la detección original se perdía sin dejar rastro.
            DetectedOvertimeMinutes = @OvertimeMinutes,
            NightMinutes = @NightMinutes,
            HolidayMinutes = @HolidayMinutes,
            RequiredMinutes = @RequiredMin,
            ScheduledWorkedMin = CAST(@InsideMinutes AS INT),
            OffScheduleMin = @OffScheduleMin,
            AbsentMinutes = @AbsentMinutes,
            MinutesLate = @MinutesLate,
            TardinessMin = @TardinessMin,
            EarlyLeaveMinutes = @EarlyLeaveMinutes,

            PermissionMinutes = 0,
            VacationMinutes = 0,
            JustificationMinutes = 0,
            MedicalLeaveMinutes = 0,
            PaidLeaveMinutes = 0,
            UnpaidLeaveMinutes = 0,
            VacationDeductedMinutes = 0,
            RecoveredMinutes = 0,

            JustificationApply = 0,
            HasPermission = 0,
            HasVacation = 0,
            HasJustification = 0,
            HasMedicalLeave = 0,
            HasManualAdjustment = 0,

            FoodSubsidy = @FoodSubsidy,

            AppliedScheduleID = @ScheduleID,
            ScheduledEntryTime = @EntryTime,
            ScheduledExitTime = @ExitTime,
            ScheduledLunchStart = @LunchStartT,
            ScheduledLunchEnd = @LunchEndT,
            ScheduledHasLunchBreak = ISNULL(@HasLunch, 0),
            ScheduledMinutes = @RequiredMin,

            Status = 'Approved',
            CalculatedAt = GETDATE(),
            CalculationSource = 'System',
            UpdatedAt = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT
        (
            EmployeeID, WorkDate, FirstPunchIn, LastPunchOut,
            TotalWorkedMinutes, RegularMinutes, OvertimeMinutes, DetectedOvertimeMinutes,
            NightMinutes, HolidayMinutes,
            RequiredMinutes, ScheduledWorkedMin, OffScheduleMin, AbsentMinutes,
            MinutesLate, TardinessMin, EarlyLeaveMinutes,
            PermissionMinutes, VacationMinutes, JustificationMinutes,
            MedicalLeaveMinutes, PaidLeaveMinutes, UnpaidLeaveMinutes,
            VacationDeductedMinutes, RecoveredMinutes,
            JustificationApply, HasPermission, HasVacation,
            HasJustification, HasMedicalLeave, HasManualAdjustment,
            FoodSubsidy,
            AppliedScheduleID, ScheduledEntryTime, ScheduledExitTime,
            ScheduledLunchStart, ScheduledLunchEnd, ScheduledHasLunchBreak, ScheduledMinutes,
            Status, CalculatedAt, CalculationVersion, CalculationSource, CreatedAt
        )
        VALUES
        (
            @EmployeeID, @WorkDate, @FirstIn, @LastOut,
            @TotalWorkedMinutes, @RegularMinutes, @OvertimeMinutes, @OvertimeMinutes,
            @NightMinutes, @HolidayMinutes,
            @RequiredMin, CAST(@InsideMinutes AS INT), @OffScheduleMin, @AbsentMinutes,
            @MinutesLate, @TardinessMin, @EarlyLeaveMinutes,
            0, 0, 0,
            0, 0, 0,
            0, 0,
            0, 0, 0,
            0, 0, 0,
            @FoodSubsidy,
            @ScheduleID, @EntryTime, @ExitTime,
            @LunchStartT, @LunchEndT, ISNULL(@HasLunch, 0), @RequiredMin,
            'Approved', GETDATE(), 1, 'System', GETDATE()
        );

    DROP TABLE IF EXISTS #Segments;
    DROP TABLE IF EXISTS #Punches;
END;

GO

-- [sp_ProcessAttendanceEmployeeDay]

CREATE PROCEDURE HR.sp_ProcessAttendanceEmployeeDay
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

-- [sp_ProcessAttendanceFinalizeDay]

/*-------- HR.sp_ProcessAttendanceFinalizeDay----------*/

CREATE   PROCEDURE HR.sp_ProcessAttendanceFinalizeDay
(
    @EmployeeID INT,
    @WorkDate   DATE,
    @ContractType NVARCHAR(100) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceFinalizeDay
      DESCRIPCIÓN:
          Ejecuta validaciones y normalizaciones finales sobre el consolidado
          diario de asistencia.

      RESPONSABILIDADES:
          - Evitar negativos
          - Recalcular FoodSubsidy con el estado final
          - Sellar metadatos del cálculo
    **********************************************************************/

    DECLARE
        @RegularMinutes INT,
        @TardinessMin INT,
        @RequiredMinutes INT,
        @FoodSubsidy INT = 0;

    SELECT
        @RegularMinutes = RegularMinutes,
        @TardinessMin = TardinessMin,
        @RequiredMinutes = RequiredMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF (@ContractType = N'Código Trabajo' AND ISNULL(@RegularMinutes,0) + ISNULL(@TardinessMin,0) >= ISNULL(@RequiredMinutes,0))
        SET @FoodSubsidy = 1;
    ELSE
        SET @FoodSubsidy = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        TotalWorkedMinutes = CASE WHEN TotalWorkedMinutes < 0 THEN 0 ELSE TotalWorkedMinutes END,
        RegularMinutes = CASE WHEN RegularMinutes < 0 THEN 0 ELSE RegularMinutes END,
        OvertimeMinutes = CASE WHEN OvertimeMinutes < 0 THEN 0 ELSE OvertimeMinutes END,
        NightMinutes = CASE WHEN NightMinutes < 0 THEN 0 ELSE NightMinutes END,
        HolidayMinutes = CASE WHEN HolidayMinutes < 0 THEN 0 ELSE HolidayMinutes END,
        RequiredMinutes = CASE WHEN RequiredMinutes < 0 THEN 0 ELSE RequiredMinutes END,
        ScheduledWorkedMin = CASE WHEN ScheduledWorkedMin < 0 THEN 0 ELSE ScheduledWorkedMin END,
        OffScheduleMin = CASE WHEN OffScheduleMin < 0 THEN 0 ELSE OffScheduleMin END,
        AbsentMinutes = CASE WHEN AbsentMinutes < 0 THEN 0 ELSE AbsentMinutes END,
        MinutesLate = CASE WHEN MinutesLate < 0 THEN 0 ELSE MinutesLate END,
        TardinessMin = CASE WHEN TardinessMin < 0 THEN 0 ELSE TardinessMin END,
        EarlyLeaveMinutes = CASE WHEN EarlyLeaveMinutes < 0 THEN 0 ELSE EarlyLeaveMinutes END,
        PermissionMinutes = CASE WHEN PermissionMinutes < 0 THEN 0 ELSE PermissionMinutes END,
        VacationMinutes = CASE WHEN VacationMinutes < 0 THEN 0 ELSE VacationMinutes END,
        JustificationMinutes = CASE WHEN JustificationMinutes < 0 THEN 0 ELSE JustificationMinutes END,
        MedicalLeaveMinutes = CASE WHEN MedicalLeaveMinutes < 0 THEN 0 ELSE MedicalLeaveMinutes END,
        PaidLeaveMinutes = CASE WHEN PaidLeaveMinutes < 0 THEN 0 ELSE PaidLeaveMinutes END,
        UnpaidLeaveMinutes = CASE WHEN UnpaidLeaveMinutes < 0 THEN 0 ELSE UnpaidLeaveMinutes END,
        VacationDeductedMinutes = CASE WHEN VacationDeductedMinutes < 0 THEN 0 ELSE VacationDeductedMinutes END,
        RecoveredMinutes = CASE WHEN RecoveredMinutes < 0 THEN 0 ELSE RecoveredMinutes END,
        ScheduledMinutes = CASE WHEN ScheduledMinutes < 0 THEN 0 ELSE ScheduledMinutes END,
        FoodSubsidy = @FoodSubsidy,
        Status = 'Approved',
        CalculatedAt = GETDATE(),
        CalculationSource = 'System',
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;

GO

-- [sp_ProcessAttendanceForDate]
CREATE   PROCEDURE HR.sp_ProcessAttendanceForDate
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

-- [sp_ProcessAttendanceJustificationsDay]


/*------ HR.sp_ProcessAttendanceJustificationsDay----------*/

CREATE   PROCEDURE HR.sp_ProcessAttendanceJustificationsDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceJustificationsDay
      DESCRIPCIÓN:
          Aplica justificaciones aprobadas de marcación sobre el consolidado
          diario del empleado.

      RESPONSABILIDADES:
          - Calcular minutos justificados
          - Marcar JustificationApply y HasJustification
          - Reducir tardanza neta
          - Reducir ausencia cuando corresponda
    **********************************************************************/

    DECLARE
        @ScheduledMinutes INT,
        @MinutesLate INT,
        @TardinessMin INT,
        @AbsentMinutes INT;

    SELECT
        @ScheduledMinutes = ScheduledMinutes,
        @MinutesLate = MinutesLate,
        @TardinessMin = TardinessMin,
        @AbsentMinutes = AbsentMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @ScheduledMinutes IS NULL
        RETURN;

    DECLARE @JustificationMinutes INT = 0;

    SELECT
        @JustificationMinutes = ISNULL(SUM(
            CASE
                WHEN j.HoursRequested IS NOT NULL THEN CAST(ROUND(j.HoursRequested * 60.0, 0) AS INT)
                WHEN j.StartDate IS NOT NULL AND j.EndDate IS NOT NULL AND j.EndDate > j.StartDate
                    THEN DATEDIFF(MINUTE, j.StartDate, j.EndDate)
                ELSE 0
            END
        ), 0)
    FROM HR.tbl_PunchJustifications j
    WHERE j.EmployeeID = @EmployeeID
      AND j.Status IN ('APPROVED','APPLIED')
      AND (
            CAST(j.JustificationDate AS DATE) = @WorkDate
         OR CAST(j.StartDate AS DATE) = @WorkDate
         OR CAST(j.EndDate AS DATE) = @WorkDate
          );

    IF @JustificationMinutes < 0 SET @JustificationMinutes = 0;
    IF @JustificationMinutes > @ScheduledMinutes SET @JustificationMinutes = @ScheduledMinutes;

    DECLARE
        @NewTardiness INT = @TardinessMin,
        @NewAbsent INT = @AbsentMinutes,
        @Apply BIT = CASE WHEN @JustificationMinutes > 0 THEN 1 ELSE 0 END,
        @HasJustification BIT = CASE WHEN @JustificationMinutes > 0 THEN 1 ELSE 0 END;

    -- Primero cubrir tardanza neta
    IF @JustificationMinutes > 0
    BEGIN
        DECLARE @Remaining INT = @JustificationMinutes;

        IF @Remaining >= @NewTardiness
        BEGIN
            SET @Remaining = @Remaining - @NewTardiness;
            SET @NewTardiness = 0;
        END
        ELSE
        BEGIN
            SET @NewTardiness = @NewTardiness - @Remaining;
            SET @Remaining = 0;
        END;

        -- Luego cubrir ausencia
        IF @Remaining > 0
        BEGIN
            IF @Remaining >= @NewAbsent
                SET @NewAbsent = 0;
            ELSE
                SET @NewAbsent = @NewAbsent - @Remaining;
        END;
    END;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        JustificationMinutes = @JustificationMinutes,
        JustificationApply = @Apply,
        HasJustification = @HasJustification,
        TardinessMin = @NewTardiness,
        AbsentMinutes = @NewAbsent,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;

GO

-- [sp_ProcessAttendanceLeavesDay]

/*--- HR.sp_ProcessAttendanceLeavesDay -----*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceLeavesDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceLeavesDay
      DESCRIPCIÓN:
          Aplica permisos, vacaciones y licencias médicas al consolidado
          diario de asistencia del empleado.

      RESPONSABILIDADES:
          - Calcular minutos cubiertos por vacaciones
          - Calcular minutos cubiertos por permisos
          - Identificar permisos médicos
          - Identificar minutos que descuentan vacaciones
          - Actualizar flags relacionados
    **********************************************************************/

    DECLARE
        @EntryTime TIME,
        @ExitTime TIME,
        @HasLunch BIT,
        @LunchStartT TIME,
        @LunchEndT TIME,
        @RequiredMinutes INT,
        @AbsentMinutes INT,
        @DayStart DATETIME2,
        @DayEnd DATETIME2,
        @ShiftStart DATETIME2,
        @ShiftEnd DATETIME2,
        @LunchStart DATETIME2 = NULL,
        @LunchEnd DATETIME2 = NULL;

    SELECT
        @EntryTime = ScheduledEntryTime,
        @ExitTime = ScheduledExitTime,
        @HasLunch = ScheduledHasLunchBreak,
        @LunchStartT = ScheduledLunchStart,
        @LunchEndT = ScheduledLunchEnd,
        @RequiredMinutes = ScheduledMinutes,
        @AbsentMinutes = AbsentMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @EntryTime IS NULL OR @ExitTime IS NULL
        RETURN;

    SET @AbsentMinutes = ISNULL(@AbsentMinutes, 0);

    SET @DayStart = CAST(@WorkDate AS DATETIME2);
    SET @DayEnd   = DATEADD(DAY, 1, @DayStart);

    SET @ShiftStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @EntryTime), @DayStart);

    IF (@ExitTime <= @EntryTime)
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), DATEADD(DAY, 1, @DayStart));
    ELSE
        SET @ShiftEnd = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @ExitTime), @DayStart);

    IF (@HasLunch = 1 AND @LunchStartT IS NOT NULL AND @LunchEndT IS NOT NULL)
    BEGIN
        SET @LunchStart = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchStartT), @DayStart);
        SET @LunchEnd   = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00:00' AS TIME), @LunchEndT), @DayStart);

        IF (@LunchEndT <= @LunchStartT)
            SET @LunchEnd = DATEADD(DAY, 1, @LunchEnd);
    END;

    DECLARE
        @VacationMinutes INT = 0,
        @PermissionMinutes INT = 0,
        @MedicalLeaveMinutes INT = 0,
        @VacationDeductedMinutes INT = 0,
        @PaidLeaveMinutes INT = 0,
        @UnpaidLeaveMinutes INT = 0,
        @HasVacation BIT = 0,
        @HasPermission BIT = 0,
        @HasMedicalLeave BIT = 0;

    DECLARE @EmployeeDepartmentId INT;
    SELECT @EmployeeDepartmentId = DepartmentId FROM HR.tbl_Employees WHERE EmployeeID = @EmployeeID;

    ;WITH VacationWindows AS
    (
        SELECT
            OverlapStart = CASE WHEN CAST(v.StartDate AS DATETIME2) > @ShiftStart THEN CAST(v.StartDate AS DATETIME2) ELSE @ShiftStart END,
            OverlapEnd   = CASE WHEN DATEADD(DAY, 1, CAST(v.EndDate AS DATETIME2)) < @ShiftEnd THEN DATEADD(DAY, 1, CAST(v.EndDate AS DATETIME2)) ELSE @ShiftEnd END
        FROM HR.tbl_Vacations v
        WHERE v.EmployeeID = @EmployeeID
          AND v.Status IN ('Planned', 'InProgress', 'Completed')
          AND @WorkDate BETWEEN v.StartDate AND v.EndDate

        UNION ALL

        -- Planificación masiva de vacaciones (cierre institucional/departamental) En
        -- Ejecución o ya Finalizada, salvo que el empleado esté en la exclusión de ese
        -- plan (2026-08-17; estados actualizados 2026-08-18 tras pasar la transición de
        -- estado a automática por fecha vía DailyMassVacationPlanTransitionJob).
        -- No crea filas en tbl_Vacations, se lee directo de acá — aditivo, no afecta a
        -- nadie fuera de un plan que le aplique.
        -- Modo "por horas" (StartTime/EndTime con valor, StartDate=EndDate): la ventana es
        -- solo esa franja del día, no el día completo.
        -- 2026-08-18: Status se resuelve por HR.ref_Types (categoría
        -- MASS_VACATION_PLAN_STATUS, valores IN_PROGRESS/FINISHED), no por string
        -- hardcodeado — el TypeID es IDENTITY y varía entre ambientes.
        SELECT
            OverlapStart = CASE
                WHEN mvp.StartTime IS NOT NULL AND mvp.EndTime IS NOT NULL
                    THEN CASE WHEN DATEADD(MINUTE, DATEDIFF(MINUTE, 0, mvp.StartTime), CAST(mvp.StartDate AS DATETIME2)) > @ShiftStart
                              THEN DATEADD(MINUTE, DATEDIFF(MINUTE, 0, mvp.StartTime), CAST(mvp.StartDate AS DATETIME2))
                              ELSE @ShiftStart END
                ELSE CASE WHEN CAST(mvp.StartDate AS DATETIME2) > @ShiftStart THEN CAST(mvp.StartDate AS DATETIME2) ELSE @ShiftStart END
            END,
            OverlapEnd = CASE
                WHEN mvp.StartTime IS NOT NULL AND mvp.EndTime IS NOT NULL
                    THEN CASE WHEN DATEADD(MINUTE, DATEDIFF(MINUTE, 0, mvp.EndTime), CAST(mvp.StartDate AS DATETIME2)) < @ShiftEnd
                              THEN DATEADD(MINUTE, DATEDIFF(MINUTE, 0, mvp.EndTime), CAST(mvp.StartDate AS DATETIME2))
                              ELSE @ShiftEnd END
                ELSE CASE WHEN DATEADD(DAY, 1, CAST(mvp.EndDate AS DATETIME2)) < @ShiftEnd THEN DATEADD(DAY, 1, CAST(mvp.EndDate AS DATETIME2)) ELSE @ShiftEnd END
            END
        FROM HR.tbl_MassVacationPlan mvp
        WHERE mvp.StatusTypeId IN (
              SELECT rt.TypeID FROM HR.ref_Types rt
              WHERE rt.Category = 'MASS_VACATION_PLAN_STATUS' AND rt.Name IN ('IN_PROGRESS', 'FINISHED')
          )
          AND mvp.IsDeleted = 0
          AND (mvp.DepartmentId IS NULL OR mvp.DepartmentId = @EmployeeDepartmentId)
          AND @WorkDate BETWEEN mvp.StartDate AND mvp.EndDate
          AND NOT EXISTS (
              SELECT 1 FROM HR.tbl_MassVacationPlanExclusion mvpe
              WHERE mvpe.PlanId = mvp.PlanId AND mvpe.EmployeeId = @EmployeeID
          )
    )
    SELECT
        @VacationMinutes = ISNULL(SUM(
            CASE
                WHEN OverlapEnd <= OverlapStart THEN 0
                ELSE DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                     - CASE
                           WHEN @HasLunch = 1
                                AND @LunchStart IS NOT NULL
                                AND @LunchEnd IS NOT NULL
                                AND OverlapStart < @LunchEnd
                                AND OverlapEnd > @LunchStart
                               THEN DATEDIFF(MINUTE,
                                     CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                     CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                           ELSE 0
                       END
            END
        ), 0)
    FROM VacationWindows;

    ;WITH PermissionWindows AS
    (
        SELECT
            pt.IsMedical,
            p.ChargedToVacation,
            OverlapStart = CASE WHEN p.StartDate > @ShiftStart THEN p.StartDate ELSE @ShiftStart END,
            OverlapEnd   = CASE WHEN p.EndDate   < @ShiftEnd   THEN p.EndDate   ELSE @ShiftEnd   END
        FROM HR.tbl_Permissions p
        INNER JOIN HR.tbl_PermissionTypes pt
            ON pt.TypeID = p.PermissionTypeID
        WHERE p.EmployeeID = @EmployeeID
          AND p.Status = 'Approved'
          AND p.StartDate < @ShiftEnd
          AND p.EndDate   > @ShiftStart
    )
    SELECT
        @PermissionMinutes = ISNULL(SUM(
            CASE
                WHEN OverlapEnd <= OverlapStart THEN 0
                ELSE DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                     - CASE
                           WHEN @HasLunch = 1
                                AND @LunchStart IS NOT NULL
                                AND @LunchEnd IS NOT NULL
                                AND OverlapStart < @LunchEnd
                                AND OverlapEnd > @LunchStart
                               THEN DATEDIFF(MINUTE,
                                     CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                     CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                           ELSE 0
                       END
            END
        ), 0),
        @MedicalLeaveMinutes = ISNULL(SUM(
            CASE
                WHEN IsMedical = 1 AND OverlapEnd > OverlapStart
                    THEN DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                         - CASE
                               WHEN @HasLunch = 1
                                    AND @LunchStart IS NOT NULL
                                    AND @LunchEnd IS NOT NULL
                                    AND OverlapStart < @LunchEnd
                                    AND OverlapEnd > @LunchStart
                                   THEN DATEDIFF(MINUTE,
                                         CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                         CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                               ELSE 0
                           END
                ELSE 0
            END
        ), 0),
        @VacationDeductedMinutes = ISNULL(SUM(
            CASE
                WHEN ChargedToVacation = 1 AND OverlapEnd > OverlapStart
                    THEN DATEDIFF(MINUTE, OverlapStart, OverlapEnd)
                         - CASE
                               WHEN @HasLunch = 1
                                    AND @LunchStart IS NOT NULL
                                    AND @LunchEnd IS NOT NULL
                                    AND OverlapStart < @LunchEnd
                                    AND OverlapEnd > @LunchStart
                                   THEN DATEDIFF(MINUTE,
                                         CASE WHEN OverlapStart > @LunchStart THEN OverlapStart ELSE @LunchStart END,
                                         CASE WHEN OverlapEnd   < @LunchEnd   THEN OverlapEnd   ELSE @LunchEnd END)
                               ELSE 0
                           END
                ELSE 0
            END
        ), 0)
    FROM PermissionWindows;

    IF @VacationMinutes < 0 SET @VacationMinutes = 0;
    IF @PermissionMinutes < 0 SET @PermissionMinutes = 0;
    IF @MedicalLeaveMinutes < 0 SET @MedicalLeaveMinutes = 0;
    IF @VacationDeductedMinutes < 0 SET @VacationDeductedMinutes = 0;

    /* BUG ORIGINAL: VacationMinutes/PermissionMinutes/MedicalLeaveMinutes se
       calculaban pero nunca se restaban de AbsentMinutes, causando doble
       penalización (un día de vacación/permiso/licencia aprobada quedaba
       como ausencia completa Y descontaba el saldo correspondiente).
       Mismo patrón de clamp que ya usa sp_ProcessAttendanceJustificationsDay
       para las justificaciones de marcación. */
    SET @AbsentMinutes = CASE
                              WHEN (@VacationMinutes + @PermissionMinutes + @MedicalLeaveMinutes) >= @AbsentMinutes
                                  THEN 0
                              ELSE @AbsentMinutes - (@VacationMinutes + @PermissionMinutes + @MedicalLeaveMinutes)
                          END;

    SET @HasVacation = CASE WHEN @VacationMinutes > 0 THEN 1 ELSE 0 END;
    SET @HasPermission = CASE WHEN @PermissionMinutes > 0 THEN 1 ELSE 0 END;
    SET @HasMedicalLeave = CASE WHEN @MedicalLeaveMinutes > 0 THEN 1 ELSE 0 END;

    -- Regla provisional:
    -- PaidLeaveMinutes = permisos que no descuentan vacaciones
    -- UnpaidLeaveMinutes = 0 (hasta que exista una regla explícita de permisos no remunerados)
    SET @PaidLeaveMinutes = CASE
                                WHEN @PermissionMinutes - @VacationDeductedMinutes > 0
                                    THEN @PermissionMinutes - @VacationDeductedMinutes
                                ELSE 0
                            END;

    SET @UnpaidLeaveMinutes = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        VacationMinutes = @VacationMinutes,
        PermissionMinutes = @PermissionMinutes,
        MedicalLeaveMinutes = @MedicalLeaveMinutes,
        VacationDeductedMinutes = @VacationDeductedMinutes,
        PaidLeaveMinutes = @PaidLeaveMinutes,
        UnpaidLeaveMinutes = @UnpaidLeaveMinutes,
        HasVacation = @HasVacation,
        HasPermission = @HasPermission,
        HasMedicalLeave = @HasMedicalLeave,
        AbsentMinutes = @AbsentMinutes,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;

GO

-- [sp_ProcessAttendancePlanningDay]

/*--------- HR.sp_ProcessAttendancePlanningDay-----------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendancePlanningDay
(
    @EmployeeID        INT,
    @WorkDate          DATE,
    @Debug             BIT = 0,
    -- Fase 4 punto 4.5: forwardeado a sp_ProcessTimePlanningForEmployeeDay
    -- para guardias (horario resuelto vía tbl_GuardShiftPlanning).
    @OverrideEntryTime TIME = NULL,
    @OverrideExitTime  TIME = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendancePlanningDay
      DESCRIPCIÓN:
          Procesa overtime y recovery planificado para el empleado/día.

      RESPONSABILIDADES:
          - Delegar la lógica de planning al procedimiento especializado actual
          - Mantener la nueva familia de nombres homogénea
    **********************************************************************/

    EXEC HR.sp_ProcessTimePlanningForEmployeeDay
         @EmployeeID        = @EmployeeID,
         @WorkDate          = @WorkDate,
         @Debug             = @Debug,
         @OverrideEntryTime = @OverrideEntryTime,
         @OverrideExitTime  = @OverrideExitTime;
END;

GO

-- [sp_ProcessAttendanceRange]
CREATE   PROCEDURE HR.sp_ProcessAttendanceRange
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

-- [sp_ProcessAttendanceRecoveryDay]

/*------ HR.sp_ProcessAttendanceRecoveryDay---------*/
CREATE   PROCEDURE HR.sp_ProcessAttendanceRecoveryDay
(
    @EmployeeID INT,
    @WorkDate   DATE
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRecoveryDay
      DESCRIPCIÓN:
          Aplica minutos recuperados aprobados al consolidado diario.

      RESPONSABILIDADES:
          - Consolidar minutos recuperados desde logs
          - Ajustar ausencia si la regla lo permite
    **********************************************************************/

    DECLARE
        @RecoveredMinutes INT = 0,
        @AbsentMinutes INT = 0;

    SELECT
        @RecoveredMinutes = ISNULL(SUM(trl.MinutesRecovered), 0)
    FROM HR.tbl_TimeRecoveryPlans trp
    INNER JOIN HR.tbl_TimeRecoveryLogs trl
        ON trl.RecoveryPlanID = trp.RecoveryPlanID
    WHERE trp.EmployeeID = @EmployeeID
      AND trl.ExecutedDate = @WorkDate;

    SELECT
        @AbsentMinutes = AbsentMinutes
    FROM HR.tbl_AttendanceCalculations
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;

    IF @RecoveredMinutes < 0 SET @RecoveredMinutes = 0;
    IF @AbsentMinutes IS NULL SET @AbsentMinutes = 0;

    UPDATE HR.tbl_AttendanceCalculations
    SET
        RecoveredMinutes = @RecoveredMinutes,
        AbsentMinutes = CASE
                            WHEN @RecoveredMinutes >= @AbsentMinutes THEN 0
                            ELSE @AbsentMinutes - @RecoveredMinutes
                        END,
        UpdatedAt = GETDATE()
    WHERE EmployeeID = @EmployeeID
      AND WorkDate = @WorkDate;
END;

GO

-- [sp_ProcessAttendanceRunDate]

/*---------HR.sp_ProcessAttendanceRunDate -----------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRunDate
(
    @WorkDate         DATE,
    @Debug            BIT = 0,
    -- 2026-07-03: filtro opcional. NULL = comportamiento actual (todos los
    -- empleados activos con horario vigente ese día). Con valor, acota el
    -- reproceso a un solo empleado. Nombre distinto a la variable interna
    -- @EmployeeID (usada como cursor del loop) para no colisionar con ella.
    @FilterEmployeeID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRunDate

      DESCRIPCIÓN GENERAL:
          Orquesta el procesamiento completo de asistencia para todos los
          empleados activos que tengan un horario vigente en la fecha
          especificada.

      OBJETIVO:
          Ejecutar en orden todas las etapas necesarias para construir y
          consolidar el registro diario de asistencia en
          HR.tbl_AttendanceCalculations.

      FUNCIONALIDAD:
          1. Identifica los empleados activos con horario vigente en la fecha.
          2. Ejecuta el cálculo base de asistencia por empleado.
          3. Aplica novedades administrativas como permisos y vacaciones.
          4. Aplica justificaciones aprobadas que afecten atrasos o ausencias.
          5. Aplica recuperaciones de tiempo aprobadas.
          6. Procesa planificación de horas extra o recuperación.
          7. Finaliza y normaliza el registro diario antes de dejarlo aprobado.

      DETALLE DE CADA ETAPA:
          1. Base
             Ejecuta HR.sp_ProcessAttendanceBaseDay.
             Calcula la asistencia base del día usando:
               - horario asignado
               - marcaciones de entrada/salida
               - minutos trabajados
               - atraso bruto y neto
               - ausencia
               - salida anticipada
               - minutos nocturnos
               - minutos en feriado
             Además guarda el snapshot del horario aplicado.

          2. Leaves
             Ejecuta HR.sp_ProcessAttendanceLeavesDay.
             Aplica al consolidado diario las novedades de:
               - permisos
               - vacaciones
               - licencias médicas
               - minutos que descuentan vacaciones
             También actualiza las banderas relacionadas.

          3. Justifications
             Ejecuta HR.sp_ProcessAttendanceJustificationsDay.
             Aplica las justificaciones aprobadas del empleado para:
               - cubrir tardanzas
               - reducir ausencias
               - registrar minutos justificados
               - marcar que la justificación fue aplicada

          4. Recovery
             Ejecuta HR.sp_ProcessAttendanceRecoveryDay.
             Aplica los minutos recuperados aprobados del empleado en la fecha
             y ajusta la ausencia cuando corresponda.

          5. Planning
             Ejecuta HR.sp_ProcessAttendancePlanningDay.
             Procesa la planificación aprobada del empleado para:
               - horas extra planificadas
               - recuperación planificada
               - ejecución real sobre el día trabajado

          6. Finalize
             Ejecuta HR.sp_ProcessAttendanceFinalizeDay.
             Realiza validaciones y ajustes finales del registro, por ejemplo:
               - normalización de valores
               - recalculo final de subsidio
               - actualización de estado
               - sellado de metadatos del cálculo

      CONSIDERACIONES:
          - Este procedimiento no calcula directamente la lógica de negocio
            detallada, sino que coordina procedimientos especializados.
          - Si ocurre un error en cualquier empleado, se detiene la ejecución
            y se devuelve el detalle del empleado y la fecha procesada.
          - Está diseñado para ser llamado por procesos de reproceso diario o
            por el orquestador de rango.
    **********************************************************************/

    IF @WorkDate IS NULL
        THROW 50001, 'El parametro @WorkDate es obligatorio.', 1;

    /* =========================================================
       1) PARÁMETROS DEL SISTEMA: UNA SOLA VEZ POR FECHA
       ========================================================= */
    DECLARE
        @GraceMin INT = 0,
        @OTMin INT = 0,
        @NightStart TIME = NULL,
        @NightEnd TIME = NULL,
        @IsHoliday BIT = 0,
        @IsWeekend BIT = 0;

    SELECT
        @GraceMin = ISNULL(MAX(CASE WHEN name = 'TARDINESS_GRACE_MIN'
                                    THEN TRY_CAST(Pvalues AS INT) END), 0),
        @OTMin = ISNULL(MAX(CASE WHEN name = 'OT_MIN_THRESHOLD_MIN'
                                 THEN TRY_CAST(Pvalues AS INT) END), 0),
        @NightStart = MAX(CASE WHEN name = 'NIGHT_START'
                               THEN TRY_CAST(Pvalues AS TIME) END),
        @NightEnd = MAX(CASE WHEN name = 'NIGHT_END'
                             THEN TRY_CAST(Pvalues AS TIME) END)
    FROM HR.tbl_Parameters
    WHERE name IN
    (
        'TARDINESS_GRACE_MIN',
        'OT_MIN_THRESHOLD_MIN',
        'NIGHT_START',
        'NIGHT_END'
    );

    IF @NightStart IS NULL SET @NightStart = TRY_CAST('22:00:00' AS TIME);
    IF @NightEnd   IS NULL SET @NightEnd   = TRY_CAST('06:00:00' AS TIME);

    /* =========================================================
       2) CALENDARIO DEL DÍA: UNA SOLA VEZ POR FECHA
       ========================================================= */
    SELECT
        @IsHoliday = ISNULL(IsHoliday, 0),
        @IsWeekend = ISNULL(IsWeekend, 0)
    FROM HR.vw_Calendar
    WHERE D = @WorkDate;

    /* =========================================================
       3) EMPLEADOS A PROCESAR + CONTRACT TYPE + HORARIO VIGENTE
          Excluye empleados con GuardShiftPlanning activo en la fecha
          (esos se procesan en sp_ProcessGuardAttendanceDate).
       ========================================================= */
    DROP TABLE IF EXISTS #EmployeesToProcess;

    ;WITH CurrentSchedule AS
    (
        SELECT
            es.EmployeeID,
            es.ScheduleID,
            s.EntryTime,
            s.ExitTime,
            s.HasLunchBreak,
            s.LunchStart,
            s.LunchEnd,
            ROW_NUMBER() OVER (
                PARTITION BY es.EmployeeID
                ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC
            ) AS rn
        FROM HR.tbl_EmployeeSchedules es
        INNER JOIN HR.tbl_Schedules s
            ON s.ScheduleID = es.ScheduleID
        WHERE es.ValidFrom <= @WorkDate
          AND (es.ValidTo IS NULL OR es.ValidTo >= @WorkDate)
    )
    SELECT
        e.EmployeeID,
        ved.ContractType,
        cs.ScheduleID,
        cs.EntryTime,
        cs.ExitTime,
        cs.HasLunchBreak,
        cs.LunchStart,
        cs.LunchEnd,
        ROW_NUMBER() OVER (ORDER BY e.EmployeeID) AS RowNum
    INTO #EmployeesToProcess
    FROM HR.tbl_Employees e
    INNER JOIN CurrentSchedule cs
        ON cs.EmployeeID = e.EmployeeID
       AND cs.rn = 1
    LEFT JOIN HR.vw_EmployeeDetails ved
        ON ved.EmployeeID = e.EmployeeID
    WHERE e.IsActive = 1
      AND (@FilterEmployeeID IS NULL OR e.EmployeeID = @FilterEmployeeID)
      AND NOT EXISTS (
          SELECT 1
          FROM HR.tbl_GuardShiftPlanning gsp
          WHERE gsp.EmployeeId          = e.EmployeeID
            AND gsp.WorkDate            = @WorkDate
            AND gsp.IsActiveForAssignment = 1
      );

    DECLARE
        @MaxRow INT,
        @RowNum INT = 1,
        @EmployeeID INT,
        @ContractType NVARCHAR(100),
        @ScheduleID INT,
        @EntryTime TIME,
        @ExitTime TIME,
        @HasLunch BIT,
        @LunchStartT TIME,
        @LunchEndT TIME;

    SELECT @MaxRow = MAX(RowNum)
    FROM #EmployeesToProcess;

    IF @MaxRow IS NULL
        SET @MaxRow = 0;

    WHILE @RowNum <= @MaxRow
    BEGIN
        SELECT
            @EmployeeID = EmployeeID,
            @ContractType = ContractType,
            @ScheduleID = ScheduleID,
            @EntryTime = EntryTime,
            @ExitTime = ExitTime,
            @HasLunch = HasLunchBreak,
            @LunchStartT = LunchStart,
            @LunchEndT = LunchEnd
        FROM #EmployeesToProcess
        WHERE RowNum = @RowNum;

        BEGIN TRY
            EXEC HR.sp_ProcessAttendanceBaseDay
                 @EmployeeID   = @EmployeeID,
                 @WorkDate     = @WorkDate,
                 @GraceMin     = @GraceMin,
                 @OTMin        = @OTMin,
                 @NightStart   = @NightStart,
                 @NightEnd     = @NightEnd,
                 @ContractType = @ContractType,
                 @IsHoliday    = @IsHoliday,
                 @IsWeekend    = @IsWeekend,
                 @ScheduleID   = @ScheduleID,
                 @EntryTime    = @EntryTime,
                 @ExitTime     = @ExitTime,
                 @HasLunch     = @HasLunch,
                 @LunchStartT  = @LunchStartT,
                 @LunchEndT    = @LunchEndT;

            EXEC HR.sp_ProcessAttendanceLeavesDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceJustificationsDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendanceRecoveryDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate;

            EXEC HR.sp_ProcessAttendancePlanningDay
                 @EmployeeID = @EmployeeID,
                 @WorkDate   = @WorkDate,
                 @Debug      = @Debug;

            EXEC HR.sp_ProcessAttendanceFinalizeDay
                 @EmployeeID   = @EmployeeID,
                 @WorkDate     = @WorkDate,
                 @ContractType = @ContractType;
        END TRY
        BEGIN CATCH
            DECLARE @ErrMsg NVARCHAR(4000);
            DECLARE @ThrowMsg NVARCHAR(4000);

            SET @ErrMsg = ERROR_MESSAGE();

            SET @ThrowMsg =
                'Error procesando EmployeeID='
                + CAST(ISNULL(@EmployeeID, 0) AS VARCHAR(20))
                + ' Fecha='
                + CONVERT(VARCHAR(10), @WorkDate, 120)
                + ' -> '
                + ISNULL(@ErrMsg, 'Error no determinado.');

            THROW 50010, @ThrowMsg, 1;
        END CATCH;

        SET @RowNum += 1;
    END;

    DROP TABLE IF EXISTS #EmployeesToProcess;

    /* =========================================================
       4) GUARDIAS: procesamiento con horario rotativo
          Se ejecuta al final para que los SPs de novedades
          (permisos, vacaciones, justificaciones) ya estén
          completamente estabilizados en la fecha.
       ========================================================= */
    EXEC HR.sp_ProcessGuardAttendanceDate
         @WorkDate         = @WorkDate,
         @Debug            = @Debug,
         @FilterEmployeeID = @FilterEmployeeID;
END;

GO

-- [sp_ProcessAttendanceRunRange]

/*---------HR.sp_ProcessAttendanceRunRange---------*/
CREATE OR ALTER PROCEDURE HR.sp_ProcessAttendanceRunRange
(
    @FromDate         DATE,
    @ToDate           DATE,
    @Debug            BIT = 0,
    -- 2026-07-03: filtro opcional, forwardeado a sp_ProcessAttendanceRunDate.
    @FilterEmployeeID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    /**********************************************************************
      PROCEDIMIENTO: HR.sp_ProcessAttendanceRunRange
      DESCRIPCIÓN:
          Orquesta el procesamiento completo de asistencia para un rango
          de fechas, ejecutando el flujo diario por cada fecha.

      RESPONSABILIDADES:
          - Validar rango
          - Ejecutar RunDate por cada fecha del período
          - Servir como punto de entrada para jobs y reprocesos masivos
    **********************************************************************/

    IF @FromDate IS NULL OR @ToDate IS NULL
        THROW 50020, 'Los parámetros @FromDate y @ToDate son obligatorios.', 1;

    IF @FromDate > @ToDate
        THROW 50021, 'El rango de fechas es inválido: @FromDate no puede ser mayor que @ToDate.', 1;

    DECLARE @d DATE = @FromDate;

    WHILE @d <= @ToDate
    BEGIN
        EXEC HR.sp_ProcessAttendanceRunDate
             @WorkDate         = @d,
             @Debug            = @Debug,
             @FilterEmployeeID = @FilterEmployeeID;

        SET @d = DATEADD(DAY, 1, @d);
    END;
END;

GO

-- [sp_ProcessTimePlanningForEmployeeDay]
CREATE OR ALTER PROCEDURE HR.sp_ProcessTimePlanningForEmployeeDay
(
    @EmployeeID        INT,
    @WorkDate          DATE,
    @Debug             BIT = 0,
    -- Fase 4 punto 4.5: horario ya resuelto (guardias, vía tbl_GuardShiftPlanning,
    -- que no usan tbl_EmployeeSchedules). Si vienen poblados, se usan directo y
    -- se omite la resolución interna por EmpSched más abajo.
    @OverrideEntryTime TIME = NULL,
    @OverrideExitTime  TIME = NULL
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

    IF @OverrideEntryTime IS NOT NULL AND @OverrideExitTime IS NOT NULL
    BEGIN
        -- Horario ya resuelto por el llamador (ej. guardias vía tbl_GuardShiftPlanning).
        SET @EntryTime = @OverrideEntryTime;
        SET @ExitTime  = @OverrideExitTime;
    END
    ELSE
    BEGIN
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
    END;

    IF @EntryTime IS NULL OR @ExitTime IS NULL
    BEGIN
        -- No se encontró un horario claro para este día
        IF @Debug = 1 
            PRINT 'No se encontró horario (Schedule) para el empleado en la fecha indicada. No se aplicará planificación.';

        -- MOD: Si no hay horario pero sí podría haber quedado HE/Recovery de antes, los ponemos en 0
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            RecoveryExecutedMinutes = 0
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
      -- 2026-07-06: corrección sobre el fix de Fase 4. Se había restringido a
      -- solo 'Aprobado', pero se confirmó que en el flujo real NINGÚN plan
      -- llega nunca a ese estado — los endpoints /submit, /approve, /reject de
      -- TimePlanningsController están comentados (no existen en la API), y el
      -- formulario de creación del frontend manda siempre 'Borrador' de forma
      -- hardcodeada. Restringir a solo 'Aprobado' dejaba CERO planes válidos,
      -- rompiendo el pago de horas extra por completo. Se acepta 'Borrador'
      -- (el único estado alcanzable hoy) pero se mantiene excluido el caso de
      -- estado nulo/no clasificado (fail-open real del bug original).
      AND st.Name IN ('Aprobado', 'Borrador');

    IF NOT EXISTS(SELECT 1 FROM #Plans)
    BEGIN
        IF @Debug = 1 
            PRINT 'No hay planificación Overtime/Recovery aplicable para este empleado en la fecha.';

        -- MOD: Si NO hay planificación, NO se permiten HE/REC de cálculo. Se ponen en 0.
        UPDATE HR.tbl_AttendanceCalculations
        SET OvertimeMinutes  = 0,
            RecoveryExecutedMinutes = 0
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
            RecoveryExecutedMinutes = 0
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
            RecoveryExecutedMinutes  = 0
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
                           END,
            -- Minutos trabajados ANTES de que empezara la ventana planificada
            -- (llegó más temprano de lo planificado).
            BeforeMin    = CASE
                             WHEN @FirstPunchIn < w.PlanStartDT
                                  THEN DATEDIFF(MINUTE, @FirstPunchIn, w.PlanStartDT)
                             ELSE 0
                           END,
            -- Minutos trabajados DESPUÉS de que terminara la ventana planificada
            -- (se quedó más tiempo del planificado).
            AfterMin     = CASE
                             WHEN @LastPunchOut > w.PlanEndDT
                                  THEN DATEDIFF(MINUTE, w.PlanEndDT, @LastPunchOut)
                             ELSE 0
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
            END,
        -- 2026-07-06: minutos realmente trabajados FUERA de la ventana
        -- planificada (antes del inicio o después del fin del plan). Antes se
        -- descartaban en silencio por el recorte MIN/MAX de OverlapStart/End;
        -- ahora quedan visibles en HR.tbl_TimePlanningExecution.ExceededMinutes
        -- en vez de perderse.
        ExceededMinutes = o.BeforeMin + o.AfterMin
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
            RecoveryExecutedMinutes = 0
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

    -- Fase 4 punto 4.2: tipo/factor real de horas extra a consolidar, en vez de
    -- hardcodear 'Ordinaria'/1.0. Si el día tuvo más de un plan de Overtime con
    -- tipos distintos (ej. Ordinaria + Feriado), se toma el de mayor Factor
    -- (Feriado gana sobre Ordinaria) — simplificación deliberada, tbl_Overtime
    -- solo admite una fila por EmployeeID+WorkDate.
    -- 2026-07-06 (punto 6): se captura también el PlanEmployeeID "ganador" para
    -- trazabilidad en tbl_Overtime. Si hubiera más de un plan de Overtime el
    -- mismo día (no se encontró ningún caso real hasta ahora), representa el
    -- plan que ganó el desempate por Factor, NO todos los que contribuyeron
    -- ese día — la fila de tbl_Overtime sigue siendo una sola por EmployeeID+WorkDate.
    DECLARE @OvertimeTypeUsed NVARCHAR(50) = 'Ordinaria',
            @OvertimeFactorUsed DECIMAL(5,2) = 1.0,
            @WinningPlanEmployeeID INT = NULL;

    SELECT TOP 1
        @OvertimeTypeUsed      = ISNULL(OvertimeType, 'Ordinaria'),
        @OvertimeFactorUsed    = ISNULL(Factor, 1.0),
        @WinningPlanEmployeeID = PlanEmployeeID
    FROM #ExecPlans
    WHERE PlanType = 'Overtime'
    ORDER BY ISNULL(Factor, 1.0) DESC, ExecutedMinutes DESC;

    --------------------------------------------------------------------
    -- 8) Actualizar tbl_AttendanceCalculations con minutos verificados
    --------------------------------------------------------------------
    -- 2026-07-06: OvertimeMinutes sigue siendo el monto final autorizado/pagado
    -- (correcto, no cambia). RecoveryExecutedMinutes (antes "recoveredMinutes")
    -- es un campo DISTINTO al RecoveredMinutes que llena sp_ProcessAttendanceRecoveryDay
    -- desde tbl_TimeRecoveryLogs — este representa minutos ejecutados contra un
    -- plan de tbl_TimePlanning (PlanType='Recovery'), que abonan a
    -- tbl_TimeBalances.RecoveryPendingMin (paso 9), NO perdonan la ausencia del
    -- día. Antes de este fix, este UPDATE pisaba por error RecoveredMinutes
    -- (el campo de Recovery Day), perdiendo esa información sin afectar el
    -- cálculo real de AbsentMinutes (que ya se había aplicado antes en el
    -- pipeline). Ver Database/ATTENDANCE_PIPELINE.md para el detalle completo.
    UPDATE HR.tbl_AttendanceCalculations
    SET OvertimeMinutes         = @TotalOvertimeMin,
        RecoveryExecutedMinutes = @TotalRecoveryMin
    WHERE EmployeeID = @EmployeeID
      AND WorkDate   = @WorkDate;

    IF @Debug = 1
        PRINT 'Actualizados OvertimeMinutes y RecoveryExecutedMinutes en HR.tbl_AttendanceCalculations.';

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
            ep.ExceededMinutes,
            ep.PlanType
        FROM #ExecPlans ep
    ) AS S
    ON T.PlanEmployeeID = S.PlanEmployeeID
       AND T.WorkDate   = S.WorkDate
    WHEN MATCHED THEN
        UPDATE SET
            T.TotalMinutes    = S.ExecutedMinutes,
            T.OvertimeMinutes = CASE WHEN S.PlanType = 'Overtime' THEN S.ExecutedMinutes ELSE 0 END,
            T.RegularMinutes  = CASE WHEN S.PlanType = 'Recovery' THEN S.ExecutedMinutes ELSE T.RegularMinutes END,
            T.ExceededMinutes = S.ExceededMinutes
    WHEN NOT MATCHED THEN
        INSERT (PlanEmployeeID, WorkDate, StartTime, EndTime, TotalMinutes, RegularMinutes, OvertimeMinutes, NightMinutes, HolidayMinutes, ExceededMinutes, CreatedAt)
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
            S.ExceededMinutes,
            SYSDATETIME()
        );

    IF @Debug = 1
        PRINT 'Actualizada/insertada ejecución en HR.tbl_TimePlanningExecution.';

    --------------------------------------------------------------------
    -- 10.5) Fase 4 punto 4.3: poblar ActualMinutes/ActualHours en
    --       HR.tbl_TimePlanningEmployees (antes era un campo muerto,
    --       siempre 0). Se RECALCULA desde HR.tbl_TimePlanningExecution
    --       (que ya es idempotente por día vía el MERGE anterior) en vez
    --       de sumar incrementalmente, para que reprocesar el mismo día
    --       varias veces no duplique el acumulado.
    --------------------------------------------------------------------
    UPDATE pe
    SET pe.ActualMinutes = agg.TotalMin,
        pe.ActualHours   = CAST(agg.TotalMin AS DECIMAL(10,2)) / 60.0
    FROM HR.tbl_TimePlanningEmployees pe
    CROSS APPLY (
        SELECT SUM(te.TotalMinutes) AS TotalMin
        FROM HR.tbl_TimePlanningExecution te
        WHERE te.PlanEmployeeID = pe.PlanEmployeeID
    ) agg
    WHERE pe.PlanEmployeeID IN (SELECT DISTINCT PlanEmployeeID FROM #ExecPlans);

    IF @Debug = 1
        PRINT 'Recalculado ActualMinutes/ActualHours en HR.tbl_TimePlanningEmployees.';

    --------------------------------------------------------------------
    -- 11) Consolidar horas extra en HR.tbl_Overtime (solo Overtime)
    --------------------------------------------------------------------
    IF @TotalOvertimeMin > 0
    BEGIN
        -- Fase 4 punto 4.4: tope opcional (NULL = sin tope, sin efecto hoy).
        -- Trunca solo lo que se PAGA (tbl_Overtime); tbl_AttendanceCalculations
        -- y tbl_TimePlanningExecution ya quedaron con el minuto real ejecutado
        -- (pasos 8 y 10), sin recortar — no se oculta el dato real, solo se topa
        -- lo facturable.
        DECLARE @MaxDailyMinutes  INT = NULL,
                @MaxWeeklyMinutes INT = NULL;

        SELECT
            @MaxDailyMinutes  = MaxDailyMinutes,
            @MaxWeeklyMinutes = MaxWeeklyMinutes
        FROM HR.tbl_OvertimeConfig
        WHERE OvertimeType = @OvertimeTypeUsed;

        DECLARE @PayableOvertimeMin INT = @TotalOvertimeMin;

        IF @MaxDailyMinutes IS NOT NULL AND @PayableOvertimeMin > @MaxDailyMinutes
            SET @PayableOvertimeMin = @MaxDailyMinutes;

        IF @MaxWeeklyMinutes IS NOT NULL
        BEGIN
            -- Semana = lunes a domingo (independiente de @@DATEFIRST: el día 0 de
            -- SQL Server, 1900-01-01, fue lunes, así que DATEDIFF(WEEK,0,fecha)
            -- siempre alinea a lunes sin importar la config de sesión).
            DECLARE @WeekStart DATE = DATEADD(WEEK, DATEDIFF(WEEK, 0, @WorkDate), 0);
            DECLARE @WeekEnd   DATE = DATEADD(DAY, 6, @WeekStart);
            DECLARE @AlreadyPaidThisWeekMin INT = 0;

            SELECT @AlreadyPaidThisWeekMin = ISNULL(SUM(Hours * 60), 0)
            FROM HR.tbl_Overtime
            WHERE EmployeeID = @EmployeeID
              AND WorkDate BETWEEN @WeekStart AND @WeekEnd
              AND WorkDate <> @WorkDate;

            DECLARE @RemainingWeeklyMin INT = @MaxWeeklyMinutes - @AlreadyPaidThisWeekMin;
            IF @RemainingWeeklyMin < 0 SET @RemainingWeeklyMin = 0;

            IF @PayableOvertimeMin > @RemainingWeeklyMin
                SET @PayableOvertimeMin = @RemainingWeeklyMin;
        END;

        DECLARE @HoursOT DECIMAL(5,2) = CAST(@PayableOvertimeMin AS DECIMAL(10,2)) / 60.0;

        MERGE HR.tbl_Overtime AS T
        USING (
            SELECT
                @EmployeeID             AS EmployeeID,
                @WorkDate               AS WorkDate,
                @HoursOT                AS Hours,
                @OvertimeTypeUsed       AS OvertimeType,
                @OvertimeFactorUsed     AS Factor,
                @WinningPlanEmployeeID  AS PlanEmployeeID
        ) AS S
        ON T.EmployeeID = S.EmployeeID
           AND T.WorkDate = S.WorkDate
        WHEN MATCHED THEN
            UPDATE SET
                T.Hours          = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Hours          ELSE S.Hours END,
                T.ActualHours    = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.ActualHours    ELSE S.Hours END,
                T.OvertimeType   = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.OvertimeType   ELSE S.OvertimeType END,
                T.Factor         = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Factor         ELSE S.Factor END,
                T.PlanEmployeeID = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.PlanEmployeeID ELSE S.PlanEmployeeID END,
                -- 2026-07-06 (Fase 3): siempre 57=LOSEP, único régimen que genera horas extra.
                T.LaborRegimeId  = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.LaborRegimeId  ELSE 57 END,
                T.Status         = CASE WHEN T.Status IN ('APPROVED','PAID') THEN T.Status         ELSE 'EXECUTED' END
        WHEN NOT MATCHED THEN
            INSERT (EmployeeID, WorkDate, OvertimeType, Hours, Status, Factor, ActualHours, PaymentAmount, PlanEmployeeID, LaborRegimeId, CreatedAt)
            VALUES (S.EmployeeID, S.WorkDate, S.OvertimeType, S.Hours, 'EXECUTED', S.Factor, S.Hours, 0, S.PlanEmployeeID, 57, SYSDATETIME());

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
CREATE   PROCEDURE HR.sp_Recovery_Apply
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

-- [sp_RegisterPersonnelMovement]
CREATE PROCEDURE HR.sp_RegisterPersonnelMovement
    @EmployeeID INT,
    @ContractID INT,
    @DestinationDepartmentID INT,
    @MovementDate DATE,
    @MovementType VARCHAR(30),
    @Reason NVARCHAR(500) = NULL,
    @DocumentLocation NVARCHAR(255) = NULL,
    @CreatedBy INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Obtener departamento actual
        DECLARE @CurrentDepartmentID INT;
        SELECT @CurrentDepartmentID = DepartmentID 
        FROM HR.tbl_Employees 
        WHERE EmployeeID = @EmployeeID;

        -- Validar que el destino sea diferente al origen
        IF @CurrentDepartmentID = @DestinationDepartmentID
        BEGIN
            RAISERROR('El departamento destino debe ser diferente al departamento actual.', 16, 1);
            RETURN;
        END

        -- Registrar movimiento
        INSERT INTO HR.tbl_PersonnelMovements (
            EmployeeID, ContractID, OriginDepartmentID, DestinationDepartmentID,
            MovementDate, MovementType, DocumentLocation, Reason, CreatedBy
        )
        VALUES (
            @EmployeeID, @ContractID, @CurrentDepartmentID, @DestinationDepartmentID,
            @MovementDate, @MovementType, @DocumentLocation, @Reason, @CreatedBy
        );

        -- Actualizar departamento del empleado
        UPDATE HR.tbl_Employees
        SET 
            DepartmentID = @DestinationDepartmentID,
            UpdatedBy = @CreatedBy,
            UpdatedAt = GETDATE()
        WHERE EmployeeID = @EmployeeID;

        DECLARE @MovementID INT = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
        
        SELECT 
            1 AS Success,
            'Movimiento de personal registrado exitosamente' AS Message,
            @MovementID AS MovementID;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        SELECT 
            0 AS Success,
            ERROR_MESSAGE() AS Message,
            NULL AS MovementID;
    END CATCH
END

GO

-- [usp_ExecuteScheduleChangePlans]
CREATE PROCEDURE HR.usp_ExecuteScheduleChangePlans
    @ExecutedByID INT = NULL,
    @DryRun       BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(0) = SYSDATETIME();

    DECLARE @StatusAprobado     INT = (
        SELECT TypeID
        FROM HR.ref_Types
        WHERE Category = 'SCHEDULE_CHANGE_STATUS'
          AND Name = 'Aprobado'
    );

    DECLARE @StatusEjecutado    INT = (
        SELECT TypeID
        FROM HR.ref_Types
        WHERE Category = 'SCHEDULE_CHANGE_STATUS'
          AND Name = 'Ejecutado'
    );

    DECLARE @EmpStatusPendiente INT = (
        SELECT TypeID
        FROM HR.ref_Types
        WHERE Category = 'SCH_CHANGE_EMP_STATUS'
          AND Name = 'Pendiente'
    );

    DECLARE @EmpStatusAplicado  INT = (
        SELECT TypeID
        FROM HR.ref_Types
        WHERE Category = 'SCH_CHANGE_EMP_STATUS'
          AND Name = 'Aplicado'
    );

    DECLARE @EmpStatusOmitido   INT = (
        SELECT TypeID
        FROM HR.ref_Types
        WHERE Category = 'SCH_CHANGE_EMP_STATUS'
          AND Name = 'Omitido'
    );

    DECLARE @ExecLog TABLE
    (
        PlanID                   INT,
        DetailID                 INT,
        EmployeeID               INT,
        Action                   NVARCHAR(50),
        Message                  NVARCHAR(500),
        PreviousScheduleID       INT NULL,
        PreviousEmpScheduleID    INT NULL,
        AppliedEmpScheduleID     INT NULL,
        EffectiveDate            DATE NULL,
        EffectiveApplyDate       DATETIME2(0) NULL,
        ExecutedAt               DATETIME2(0)
    );

    DECLARE
        @PlanID               INT,
        @NewScheduleID        INT,
        @EffectiveDate        DATE,
        @IsPermanent          BIT,
        @TemporalEndDate      DATE;

    DECLARE plan_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            p.PlanID,
            p.NewScheduleID,
            CAST(p.EffectiveDate AS DATE) AS EffectiveDate,
            p.IsPermanent,
            p.TemporalEndDate
        FROM HR.tbl_ScheduleChangePlan p
        WHERE p.StatusTypeID = @StatusAprobado
          AND p.EffectiveDate IS NOT NULL
          AND CAST(p.EffectiveDate AS DATE) <= CAST(@Now AS DATE);

    OPEN plan_cursor;

    FETCH NEXT FROM plan_cursor
    INTO @PlanID, @NewScheduleID, @EffectiveDate, @IsPermanent, @TemporalEndDate;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE
            @DetailID                 INT,
            @EmployeeID               INT,
            @PreviousScheduleID       INT,
            @PreviousEmpScheduleID    INT,
            @CurrentEmpScheduleID     INT,
            @CurrentScheduleID        INT,
            @NewEmpSchedID            INT;

        DECLARE emp_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                d.DetailID,
                d.EmployeeID,
                d.PreviousScheduleID,
                d.PreviousEmpScheduleID
            FROM HR.tbl_ScheduleChangePlanDetail d
            WHERE d.PlanID = @PlanID
              AND d.StatusTypeID = @EmpStatusPendiente;

        OPEN emp_cursor;

        FETCH NEXT FROM emp_cursor
        INTO @DetailID, @EmployeeID, @PreviousScheduleID, @PreviousEmpScheduleID;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @CurrentEmpScheduleID = NULL;
            SET @CurrentScheduleID    = NULL;
            SET @NewEmpSchedID        = NULL;

            BEGIN TRY
                BEGIN TRANSACTION;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM HR.tbl_Employees e
                    WHERE e.EmployeeID = @EmployeeID
                      AND e.IsActive = 1
                )
                BEGIN
                    IF @DryRun = 0
                    BEGIN
                        UPDATE HR.tbl_ScheduleChangePlanDetail
                        SET StatusTypeID   = @EmpStatusOmitido,
                            OmissionReason = N'Empleado inactivo al momento de ejecución',
                            UpdatedAt      = @Now
                        WHERE DetailID = @DetailID;
                    END;

                    INSERT INTO @ExecLog
                    (
                        PlanID, DetailID, EmployeeID, Action, Message,
                        PreviousScheduleID, PreviousEmpScheduleID, AppliedEmpScheduleID,
                        EffectiveDate, EffectiveApplyDate, ExecutedAt
                    )
                    VALUES
                    (
                        @PlanID, @DetailID, @EmployeeID, N'OMITIDO', N'Empleado inactivo al momento de ejecución',
                        @PreviousScheduleID, @PreviousEmpScheduleID, NULL,
                        @EffectiveDate, CASE WHEN @DryRun = 1 THEN NULL ELSE @Now END, @Now
                    );

                    COMMIT TRANSACTION;

                    FETCH NEXT FROM emp_cursor
                    INTO @DetailID, @EmployeeID, @PreviousScheduleID, @PreviousEmpScheduleID;

                    CONTINUE;
                END;

                SELECT TOP (1)
                    @CurrentEmpScheduleID = es.EmpScheduleID,
                    @CurrentScheduleID    = es.ScheduleID
                FROM HR.tbl_EmployeeSchedules es
                WHERE es.EmployeeID = @EmployeeID
                  AND es.ValidTo IS NULL
                ORDER BY es.ValidFrom DESC, es.EmpScheduleID DESC;

                IF EXISTS
                (
                    SELECT 1
                    FROM HR.tbl_EmployeeSchedules es
                    WHERE es.EmployeeID = @EmployeeID
                      AND es.ScheduleID = @NewScheduleID
                      AND es.ValidFrom  = @EffectiveDate
                )
                BEGIN
                    IF @DryRun = 0
                    BEGIN
                        UPDATE HR.tbl_ScheduleChangePlanDetail
                        SET StatusTypeID   = @EmpStatusOmitido,
                            OmissionReason = N'Ya existe el horario destino para la misma fecha efectiva',
                            UpdatedAt      = @Now
                        WHERE DetailID = @DetailID;
                    END;

                    INSERT INTO @ExecLog
                    (
                        PlanID, DetailID, EmployeeID, Action, Message,
                        PreviousScheduleID, PreviousEmpScheduleID, AppliedEmpScheduleID,
                        EffectiveDate, EffectiveApplyDate, ExecutedAt
                    )
                    VALUES
                    (
                        @PlanID, @DetailID, @EmployeeID, N'OMITIDO', N'Ya existe el horario destino para la misma fecha efectiva',
                        @PreviousScheduleID, @PreviousEmpScheduleID, NULL,
                        @EffectiveDate, CASE WHEN @DryRun = 1 THEN NULL ELSE @Now END, @Now
                    );

                    COMMIT TRANSACTION;

                    FETCH NEXT FROM emp_cursor
                    INTO @DetailID, @EmployeeID, @PreviousScheduleID, @PreviousEmpScheduleID;

                    CONTINUE;
                END;

                IF @DryRun = 0
                BEGIN
                    IF @CurrentEmpScheduleID IS NOT NULL
                    BEGIN
                        UPDATE HR.tbl_EmployeeSchedules
                        SET ValidTo   = DATEADD(DAY, -1, @EffectiveDate),
                            UpdatedAt = @Now,
                            UpdatedBy = @ExecutedByID
                        WHERE EmpScheduleID = @CurrentEmpScheduleID;
                    END;

                    INSERT INTO HR.tbl_EmployeeSchedules
                    (
                        EmployeeID,
                        ScheduleID,
                        ValidFrom,
                        ValidTo,
                        CreatedBy,
                        CreatedAt
                    )
                    VALUES
                    (
                        @EmployeeID,
                        @NewScheduleID,
                        @EffectiveDate,
                        CASE
                            WHEN @IsPermanent = 1 THEN NULL
                            ELSE @TemporalEndDate
                        END,
                        @ExecutedByID,
                        @Now
                    );

                    SET @NewEmpSchedID = SCOPE_IDENTITY();

                    UPDATE HR.tbl_ScheduleChangePlanDetail
                    SET StatusTypeID         = @EmpStatusAplicado,
                        AppliedEmpScheduleID = @NewEmpSchedID,
                        AppliedAt            = @Now,
                        UpdatedAt            = @Now
                    WHERE DetailID = @DetailID;
                END;

                INSERT INTO @ExecLog
                (
                    PlanID, DetailID, EmployeeID, Action, Message,
                    PreviousScheduleID, PreviousEmpScheduleID, AppliedEmpScheduleID,
                    EffectiveDate, EffectiveApplyDate, ExecutedAt
                )
                VALUES
                (
                    @PlanID,
                    @DetailID,
                    @EmployeeID,
                    N'APLICADO',
                    CASE
                        WHEN @DryRun = 1 THEN N'DryRun: cambio listo para aplicar'
                        WHEN @PreviousEmpScheduleID IS NOT NULL
                             AND @CurrentEmpScheduleID IS NOT NULL
                             AND @PreviousEmpScheduleID <> @CurrentEmpScheduleID
                             THEN N'Aplicado: el horario vigente al ejecutar difería del capturado al planificar'
                        ELSE N'Horario cambiado correctamente'
                    END,
                    @PreviousScheduleID,
                    @PreviousEmpScheduleID,
                    @NewEmpSchedID,
                    @EffectiveDate,
                    CASE WHEN @DryRun = 1 THEN NULL ELSE @Now END,
                    @Now
                );

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0
                    ROLLBACK TRANSACTION;

                INSERT INTO @ExecLog
                (
                    PlanID, DetailID, EmployeeID, Action, Message,
                    PreviousScheduleID, PreviousEmpScheduleID, AppliedEmpScheduleID,
                    EffectiveDate, EffectiveApplyDate, ExecutedAt
                )
                VALUES
                (
                    @PlanID,
                    @DetailID,
                    @EmployeeID,
                    N'ERROR',
                    ERROR_MESSAGE(),
                    @PreviousScheduleID,
                    @PreviousEmpScheduleID,
                    NULL,
                    @EffectiveDate,
                    NULL,
                    @Now
                );
            END CATCH;

            FETCH NEXT FROM emp_cursor
            INTO @DetailID, @EmployeeID, @PreviousScheduleID, @PreviousEmpScheduleID;
        END;

        CLOSE emp_cursor;
        DEALLOCATE emp_cursor;

        IF @DryRun = 0
        BEGIN
            IF NOT EXISTS
            (
                SELECT 1
                FROM HR.tbl_ScheduleChangePlanDetail d
                WHERE d.PlanID = @PlanID
                  AND d.StatusTypeID = @EmpStatusPendiente
            )
            BEGIN
                UPDATE HR.tbl_ScheduleChangePlan
                SET StatusTypeID       = @StatusEjecutado,
                    EffectiveApplyDate = @Now,
                    AppliedAt          = @Now,
                    AppliedByID        = @ExecutedByID,
                    UpdatedAt          = @Now,
                    UpdatedBy          = @ExecutedByID
                WHERE PlanID = @PlanID;
            END
        END;

        FETCH NEXT FROM plan_cursor
        INTO @PlanID, @NewScheduleID, @EffectiveDate, @IsPermanent, @TemporalEndDate;
    END;

    CLOSE plan_cursor;
    DEALLOCATE plan_cursor;

    SELECT
        PlanID,
        DetailID,
        EmployeeID,
        Action,
        Message,
        PreviousScheduleID,
        PreviousEmpScheduleID,
        AppliedEmpScheduleID,
        EffectiveDate,
        EffectiveApplyDate,
        ExecutedAt
    FROM @ExecLog
    ORDER BY PlanID, EmployeeID, DetailID;
END;

GO
