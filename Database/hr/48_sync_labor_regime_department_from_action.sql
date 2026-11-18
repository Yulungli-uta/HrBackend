-- Alinea el departamento del régimen laboral con el destino de la acción de personal que lo originó.
-- Solo regímenes activos con OTRO departamento (no los vacíos) y solo si el empleado no tiene una acción posterior con
-- departamento o cargo. Por qué: SIIES Funcionarios prioriza el departamento del régimen sobre el del empleado, y
-- CorrectAsync no lo actualizaba. Idempotente. Revisar el SELECT antes del UPDATE.
-- ATENCIÓN: en la mayoría de casos el departamento de la acción difiere del del empleado (HR.tbl_Employees);
-- el reporte pasará a mostrar el de la acción.

;WITH x AS (
    SELECT r.Id, a.DestinationDepartmentId AS NuevoDepto
    FROM HR.tbl_EmployeeLaborRegime r
    JOIN HR.tbl_PersonnelActions a ON a.ActionId = r.SourcePersonnelActionId AND a.IsDeleted = 0 AND a.Status IN ('VIGENTE', 'FINALIZADO')
    LEFT JOIN HR.tbl_personnel_action_type ta ON ta.PersonnelActionTypeId = a.ActionTypeId
    WHERE r.IsActive = 1
      -- licencias, dedicación, vacaciones, disciplinarias y salidas arrastran un departamento pero no cambian el puesto
      AND (ta.ActionCategory IS NULL OR ta.ActionCategory NOT IN ('LEAVE', 'SCHEDULE', 'VACATION', 'DISCIPLINARY', 'EXIT')) AND a.DestinationDepartmentId IS NOT NULL
      AND r.DepartmentId IS NOT NULL AND r.DepartmentId <> a.DestinationDepartmentId
      AND NOT EXISTS (
            SELECT 1 FROM HR.tbl_PersonnelActions b
            WHERE b.EmployeeId = a.EmployeeId AND b.IsDeleted = 0 AND b.ActionId <> a.ActionId AND b.Status IN ('VIGENTE', 'FINALIZADO')
              AND (b.DestinationDepartmentId IS NOT NULL OR b.DestinationJobId IS NOT NULL)
              AND NOT EXISTS (SELECT 1 FROM HR.tbl_personnel_action_type tb WHERE tb.PersonnelActionTypeId = b.ActionTypeId
                              AND tb.ActionCategory IN ('LEAVE', 'SCHEDULE', 'VACATION', 'DISCIPLINARY', 'EXIT'))
              AND (COALESCE(b.EffectiveDate, b.ActionDate) > COALESCE(a.EffectiveDate, a.ActionDate)
                   OR (COALESCE(b.EffectiveDate, b.ActionDate) = COALESCE(a.EffectiveDate, a.ActionDate) AND b.ActionId > a.ActionId)))
)
SELECT r.Id, r.EmployeeId, r.DepartmentId AS DeptoActual, x.NuevoDepto INTO #deptos
FROM HR.tbl_EmployeeLaborRegime r JOIN x ON x.Id = r.Id;

SELECT * FROM #deptos ORDER BY Id;   -- revisar

UPDATE r SET r.DepartmentId = d.NuevoDepto, r.UpdatedAt = SYSDATETIME()
  FROM HR.tbl_EmployeeLaborRegime r JOIN #deptos d ON d.Id = r.Id
 WHERE r.DepartmentId <> d.NuevoDepto;

DROP TABLE #deptos;
