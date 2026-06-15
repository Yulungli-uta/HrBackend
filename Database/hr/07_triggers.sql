-- ============================================================
-- TRIGGERS : esquema [HR]
-- NOTA: SQL Server no soporta CREATE OR ALTER TRIGGER.
-- Se usa DROP IF EXISTS + CREATE para permitir re-ejecución.
-- Generado: 2026-05-29
-- ============================================================

SET NOCOUNT ON;
GO

-- [trg_Punch_Validations]
CREATE OR ALTER TRIGGER HR.trg_Punch_Validations
ON HR.tbl_AttendancePunches
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    --------------------------------------------------------------------
    -- 1) VALIDACIÓN: Empleado no puede marcar durante vacaciones
    --------------------------------------------------------------------
    DECLARE @ErrorMsg NVARCHAR(500);

    SELECT TOP 1 @ErrorMsg =
        'ERROR: El empleado ' + CONVERT(VARCHAR(10), v.EmployeeID) +
        ' está de vacaciones del ' + CONVERT(VARCHAR(10), v.StartDate, 103) +
        ' al ' + CONVERT(VARCHAR(10), v.EndDate, 103) +
        '. No se permiten marcaciones durante este período.'
    FROM inserted i
    INNER JOIN HR.tbl_Vacations v ON v.EmployeeID = i.EmployeeID
    WHERE v.Status = 'InProgress'
      AND CAST(i.PunchTime AS DATE) >= v.StartDate
      AND CAST(i.PunchTime AS DATE) <= v.EndDate
      AND CAST(GETDATE() AS DATE) >= v.StartDate
      AND CAST(GETDATE() AS DATE) <= v.EndDate;

    IF @ErrorMsg IS NOT NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, @ErrorMsg, 1;
    END

    --------------------------------------------------------------------
    -- 2) VALIDACIÓN: Diferencia mínima de 5 minutos entre marcaciones
    -- Nota: Como ya insertó, excluimos el mismo PunchID.
    --------------------------------------------------------------------
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_AttendancePunches p
            ON p.EmployeeID = i.EmployeeID
           AND p.PunchID <> i.PunchID
           -- Si quieres solo "marcaciones anteriores", usa p.PunchTime < i.PunchTime
           AND DATEDIFF(MINUTE, p.PunchTime, i.PunchTime) BETWEEN -4 AND 4
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, 'ERROR: La diferencia entre marcaciones debe ser al menos de 5 minutos.', 1;
    END

    --------------------------------------------------------------------
    -- 3) VALIDACIÓN: Empleado debe estar activo
    --------------------------------------------------------------------
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN HR.tbl_Employees e ON e.EmployeeID = i.EmployeeID
        WHERE e.IsActive = 0
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, 'ERROR: No se permiten marcaciones para empleados inactivos.', 1;
    END

    --------------------------------------------------------------------
    -- 4) VALIDACIÓN: Tipo de picada debe ser 'In' o 'Out'
    --------------------------------------------------------------------
    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.PunchType NOT IN ('In', 'Out')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50004, 'ERROR: El tipo de marcación debe ser "In" (Entrada) o "Out" (Salida).', 1;
    END

    --------------------------------------------------------------------
    -- 5) REGLA: >2 marcaciones por día del mismo tipo
    -- Recomendación: si esto debe bloquear => THROW.
    -- Si solo es advertencia, NO uses RAISERROR 10; mejor registra en una tabla log.
    --------------------------------------------------------------------
--    IF EXISTS (
--        SELECT 1
--        FROM inserted i
--        CROSS APPLY (
--            SELECT COUNT(*) AS Cnt
--            FROM HR.tbl_AttendancePunches p
--            WHERE p.EmployeeID = i.EmployeeID
--              AND CAST(p.PunchTime AS DATE) = CAST(i.PunchTime AS DATE)
--              AND p.PunchType = i.PunchType
--        ) x
--        WHERE x.Cnt > 2
--    )
--    BEGIN
--        -- Decide una de estas dos:
--
--        -- (A) Bloquear:
--         ROLLBACK TRANSACTION;
--         THROW 50005, 'ERROR: Se detectaron más de 2 marcaciones del mismo tipo en el día.', 1;
--
--        -- (B) Solo advertir (sin romper EF): registrar en tabla de auditoría
--        -- (si no tienes tabla, omite este bloque)
--        -- INSERT INTO HR.tbl_AttendanceWarnings(EmployeeID, PunchTime, PunchType, Message, CreatedAt)
--        -- SELECT i.EmployeeID, i.PunchTime, i.PunchType,
--        --        'ADVERTENCIA: múltiples marcaciones del mismo tipo en un día.',
--        --        GETDATE()
--        -- FROM inserted i;
--    END
END

GO
