-- ============================================================
-- Unificación de HR.tbl_SalaryHistory como libro histórico único
-- de sueldos: cada fila queda ligada a UN documento fuente
-- (Contrato o Acción de Personal), nunca a "cualquier registro"
-- del empleado. Aprobado por el usuario 2026-08-14.
--
-- Contexto (ver Database/MULTI_REGIME_EMPLOYEES.md y
-- 17_matriz_carga_masiva_schema.sql para el historial previo):
-- - Código de Trabajo: el documento fuente es el Contrato
--   (HR.tbl_Contracts.BaseSalary).
-- - LOSEP/LOES: el documento fuente es la Acción de Personal
--   económica (HR.tbl_PersonnelActions.NewRmu/PreviousRmu).
-- - HR.tbl_SalaryHistory pasa a ser el libro consolidado que
--   leen reportes/vistas para "último sueldo" — antes solo
--   aceptaba ContractID (NOT NULL), lo que impedía registrar
--   cambios originados en una Acción de Personal.
--
-- Incluye también el backfill único (idempotente) que completa los
-- datos existentes bajo el esquema nuevo:
-- 6) Contracts.BaseSalary de los 244 contratos de Código de Trabajo
--    (ya estaba correcto en SalaryHistory, faltaba en Contracts).
-- 7) EmployeeID en las filas existentes de SalaryHistory (columna
--    nueva del punto 3, agregada NULL).
-- 8) SalaryHistory para las acciones económicas LOSEP/LOES que
--    tienen NewRmu pero nunca tuvieron fila (ligadas por ActionID,
--    no por ContractID — confirmado que
--    tbl_PersonnelActions.ContractID está en NULL en el 100% de
--    los casos).
-- 9) SalaryHistory para los contratos ya firmados/vigentes con
--    BaseSalary pero sin fila histórica aún.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar. Ninguna
-- columna existente se elimina, renombra ni cambia de tipo.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) ContractID pasa a ser opcional: una fila puede originarse en
--    una Acción de Personal en vez de un Contrato.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]') AND name = 'ContractID' AND is_nullable = 0
)
    ALTER TABLE [HR].[tbl_SalaryHistory] ALTER COLUMN [ContractID] INT NULL;
GO

-- 2) ActionID: documento fuente cuando la fila viene de una Acción
--    de Personal económica en vez de un Contrato.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]') AND name = 'ActionID')
    ALTER TABLE [HR].[tbl_SalaryHistory] ADD [ActionID] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryHistory_Action')
    ALTER TABLE [HR].[tbl_SalaryHistory]
        ADD CONSTRAINT [FK_SalaryHistory_Action]
        FOREIGN KEY ([ActionID]) REFERENCES [HR].[tbl_PersonnelActions]([ActionID]);
GO

-- 3) EmployeeID directo: evita el doble salto (ContractID->Contracts->PersonID
--    o ActionID->PersonnelActions->EmployeeID) en cada consulta/reporte que
--    necesite "último sueldo por empleado".
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]') AND name = 'EmployeeID')
    ALTER TABLE [HR].[tbl_SalaryHistory] ADD [EmployeeID] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SalaryHistory_Employee')
    ALTER TABLE [HR].[tbl_SalaryHistory]
        ADD CONSTRAINT [FK_SalaryHistory_Employee]
        FOREIGN KEY ([EmployeeID]) REFERENCES [HR].[tbl_Employees]([EmployeeID]);
GO

-- 4) Cada fila debe poder justificarse con al menos un documento fuente
--    (Contrato o Acción) — nunca queda huérfana.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SalaryHistory_HasSource')
    ALTER TABLE [HR].[tbl_SalaryHistory]
        ADD CONSTRAINT [CK_SalaryHistory_HasSource]
        CHECK ([ContractID] IS NOT NULL OR [ActionID] IS NOT NULL);
GO

-- 5) Índices de apoyo para los lookups de upsert (por documento) y de
--    lectura (último sueldo por empleado).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryHistory_ContractID' AND object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]'))
    CREATE INDEX [IX_SalaryHistory_ContractID] ON [HR].[tbl_SalaryHistory] ([ContractID]) WHERE [ContractID] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryHistory_ActionID' AND object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]'))
    CREATE INDEX [IX_SalaryHistory_ActionID] ON [HR].[tbl_SalaryHistory] ([ActionID]) WHERE [ActionID] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalaryHistory_EmployeeID_ChangedAt' AND object_id = OBJECT_ID('[HR].[tbl_SalaryHistory]'))
    CREATE INDEX [IX_SalaryHistory_EmployeeID_ChangedAt] ON [HR].[tbl_SalaryHistory] ([EmployeeID], [ChangedAt] DESC);
GO

-- 6) Contracts.BaseSalary <- SalaryHistory (Código de Trabajo) ---------------
UPDATE c
SET c.BaseSalary = sh.NewSalary
FROM HR.tbl_Contracts c
JOIN HR.tbl_SalaryHistory sh ON sh.ContractID = c.ContractID
WHERE sh.Reason = N'Carga histórica adendums Código de Trabajo 2025'
  AND c.BaseSalary IS NULL;
GO

-- 7) EmployeeID en filas existentes de SalaryHistory (todas vía ContractID) --
UPDATE sh
SET sh.EmployeeID = e.EmployeeID
FROM HR.tbl_SalaryHistory sh
JOIN HR.tbl_Contracts c ON c.ContractID = sh.ContractID
JOIN HR.tbl_Employees e ON e.PersonID = c.PersonID
WHERE sh.EmployeeID IS NULL AND sh.ContractID IS NOT NULL;
GO

-- 8) SalaryHistory para acciones económicas LOSEP/LOES sin fila aún ----------
INSERT INTO HR.tbl_SalaryHistory (ContractID, ActionID, EmployeeID, OldSalary, NewSalary, ChangedBy, ChangedAt, Reason)
SELECT
    NULL,
    pa.ActionID,
    pa.EmployeeID,
    ISNULL(pa.PreviousRmu, pa.NewRmu),
    pa.NewRmu,
    N'CARGA_MASIVA_ACCIONES_2026',
    CAST(ISNULL(pa.EffectiveDate, pa.ActionDate) AS DATETIME2),
    N'Carga histórica de acciones de personal LOSEP/LOES 2026'
FROM HR.tbl_PersonnelActions pa
WHERE pa.NewRmu IS NOT NULL
  AND pa.EmployeeID IS NOT NULL
  -- Solo acciones que ya llegaron a FIRMADO_CARGADO/VIGENTE/FINALIZADO (documento
  -- firmado) — igual que el disparo automático en PersonnelActionService. Un
  -- BORRADOR/GENERADO nunca fue firmado y no debe alimentar SalaryHistory.
  -- (Bug encontrado y corregido 2026-08-15: la primera corrida de este backfill
  -- no tenía este filtro y coló 2 acciones sin firmar — ver
  -- hrbackend-salary-history-unification-2026-08-14.md.)
  AND pa.Status IN ('FIRMADO_CARGADO', 'VIGENTE', 'FINALIZADO')
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_SalaryHistory sh WHERE sh.ActionID = pa.ActionID);
GO

-- 9) SalaryHistory para contratos ya firmados/vigentes con BaseSalary sin fila
INSERT INTO HR.tbl_SalaryHistory (ContractID, ActionID, EmployeeID, OldSalary, NewSalary, ChangedBy, ChangedAt, Reason)
SELECT
    c.ContractID,
    NULL,
    e.EmployeeID,
    c.BaseSalary,
    c.BaseSalary,
    N'CARGA_MASIVA_CONTRATOS_2026',
    CAST(ISNULL(c.authorizationdate, c.startdate) AS DATETIME2),
    N'Carga histórica de contratos ya firmados/vigentes 2026'
FROM HR.tbl_Contracts c
JOIN HR.tbl_Employees e ON e.PersonID = c.PersonID
WHERE c.BaseSalary IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_SalaryHistory sh WHERE sh.ContractID = c.ContractID);
GO
