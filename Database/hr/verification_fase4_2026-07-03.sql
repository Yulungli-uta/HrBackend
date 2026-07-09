-- ============================================================
-- Fase 4 — Consultas de verificación (reemplazar valores marcados)
-- Referenciado desde Database/DEPLOYMENT_CHECKLIST_2026-07-03.md
-- ============================================================

-- 1) Estados válidos de autorización (4.1): confirmar que un plan
--    'En Progreso'/'Borrador' ya NO genera fila en tbl_Overtime.
EXEC HR.sp_ProcessAttendanceRunDate @WorkDate = '2026-07-02', @Debug = 1;

SELECT o.*
FROM HR.tbl_Overtime o
WHERE o.EmployeeID = @EmployeeID        -- reemplazar: empleado con plan NO aprobado ese día
  AND o.WorkDate   = '2026-07-02';
-- Esperado: 0 filas (o la fila previa sin actualizar si ya existía de antes del fix).

-- 2) Factor real de feriado (4.2)
SELECT o.EmployeeID, o.WorkDate, o.OvertimeType, o.Factor, o.Hours, o.Status
FROM HR.tbl_Overtime o
WHERE o.WorkDate = '2026-07-02'         -- reemplazar: día feriado con plan tipo 'Feriado'
ORDER BY o.EmployeeID;
-- Esperado: OvertimeType='Feriado', Factor=2.00 (o el factor real configurado),
-- no 'Ordinaria'/1.00.

-- 3) ActualMinutes poblado (4.3)
SELECT pe.PlanEmployeeID, pe.PlanID, pe.EmployeeID, pe.AssignedMinutes, pe.ActualMinutes, pe.ActualHours
FROM HR.tbl_TimePlanningEmployees pe
WHERE pe.EmployeeID = @EmployeeID;      -- reemplazar
-- Esperado: ActualMinutes/ActualHours reflejan minutos reales ejecutados, no 0.

-- 4) Tope de horas extra (4.4) — confirmar que sigue INACTIVO por defecto
SELECT OvertimeType, Factor, MaxDailyMinutes, MaxWeeklyMinutes
FROM HR.tbl_OvertimeConfig;
-- Esperado: MaxDailyMinutes/MaxWeeklyMinutes en NULL para todos los tipos existentes.
-- Cuando HR quiera activar un tope:
--   UPDATE HR.tbl_OvertimeConfig SET MaxDailyMinutes = 240 WHERE OvertimeType = 'Ordinaria';

-- 5) Guardias: horas extra ejecutadas llegando a tbl_Overtime (4.5)
EXEC HR.sp_ProcessGuardAttendanceDate @WorkDate = '2026-07-02', @Debug = 1;

SELECT o.EmployeeID, o.WorkDate, o.OvertimeType, o.Factor, o.Hours, o.Status
FROM HR.tbl_Overtime o
INNER JOIN HR.tbl_GuardShiftPlanning gsp
    ON gsp.EmployeeID = o.EmployeeID AND gsp.WorkDate = o.WorkDate
WHERE o.WorkDate = '2026-07-02'
ORDER BY o.EmployeeID;
-- Esperado: aparecen filas para guardias con plan de horas extra aprobado y
-- marcación real ese día (antes del fix, esta consulta no devolvía nada nunca).
