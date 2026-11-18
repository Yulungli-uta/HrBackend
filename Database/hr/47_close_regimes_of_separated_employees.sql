-- Cierra los regímenes laborales que siguen abiertos (sin fecha de fin) de personas ya separadas.
-- Criterio: empleado inactivo con una acción VIGENTE/FINALIZADA de renuncia, insubsistencia o separación definitiva
-- (tipos 11, 1148, 1149, 1145) con fecha efectiva <= hoy, régimen iniciado antes de esa fecha y sin acciones de
-- movimiento posteriores (posible reingreso). La fecha de fin es la fecha efectiva de la separación.
-- Por qué: SIIES Funcionarios incluye a quien tiene un régimen solapado con el rango; un régimen sin fecha de fin
-- se solapa con todo rango posterior, y la persona sigue saliendo después de haberse ido.
-- Idempotente (solo toca regímenes aún abiertos). Revisar el SELECT antes de ejecutar el UPDATE.
-- Respaldo previo sugerido: SELECT * INTO HR.tbl_EmployeeLaborRegime_bak_20261116 FROM HR.tbl_EmployeeLaborRegime;

;WITH sep AS (
    SELECT a.EmployeeId, MAX(COALESCE(a.EffectiveDate, a.ActionDate)) AS SepDate
    FROM HR.tbl_PersonnelActions a
    WHERE a.IsDeleted = 0 AND a.EmployeeId IS NOT NULL
      AND a.ActionTypeId IN (11, 1148, 1149, 1145)
      AND a.Status IN ('VIGENTE', 'FINALIZADO')
      AND COALESCE(a.EffectiveDate, a.ActionDate) <= CAST(SYSDATETIME() AS date)
    GROUP BY a.EmployeeId
)
SELECT r.Id, r.EmployeeId, r.DocumentNumber, r.EffectiveFrom, s.SepDate AS NuevaFechaFin
INTO #cierres
FROM HR.tbl_EmployeeLaborRegime r
JOIN HR.tbl_Employees e ON e.EmployeeID = r.EmployeeId AND e.IsDeleted = 0 AND e.IsActive = 0
JOIN sep s ON s.EmployeeId = r.EmployeeId
WHERE r.IsActive = 1 AND r.EffectiveTo IS NULL AND r.EffectiveFrom <= s.SepDate
  AND r.LaborRegimeId <> 59   -- Código de Trabajo se deja como está (decisión de RRHH)
  AND NOT EXISTS (
        SELECT 1 FROM HR.tbl_PersonnelActions b
        WHERE b.EmployeeId = r.EmployeeId AND b.IsDeleted = 0 AND b.Status IN ('VIGENTE', 'FINALIZADO')
          AND b.ActionTypeId NOT IN (11, 1148, 1149, 1145)
          AND (b.DestinationDepartmentId IS NOT NULL OR b.DestinationJobId IS NOT NULL)
          AND COALESCE(b.EffectiveDate, b.ActionDate) > s.SepDate);

SELECT * FROM #cierres ORDER BY NuevaFechaFin, Id;   -- revisar

UPDATE r
   SET r.IsActive = 0, r.IsPrincipal = 0, r.EffectiveTo = c.NuevaFechaFin, r.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_EmployeeLaborRegime r
  JOIN #cierres c ON c.Id = r.Id
 WHERE r.IsActive = 1 AND r.EffectiveTo IS NULL;

DROP TABLE #cierres;
