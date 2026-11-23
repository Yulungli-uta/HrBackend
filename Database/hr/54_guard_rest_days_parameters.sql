-- Guardias: mínimo de días libres por semana en los patrones de rotación (QA PS-030, obs. 21).
-- Un patrón de más de 5 días debe incluir al menos MINIMUM_REST_DAYS_PER_WEEK días de descanso por cada 7 días de ciclo.
-- La regla solo bloquea cuando MINIMUM_REST_DAYS_SEVERITY = 'BLOCKING' (igual que MINIMUM_REST_SEVERITY).
SET XACT_ABORT ON;
BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM HR.tbl_Parameters WHERE name = N'MINIMUM_REST_DAYS_PER_WEEK')
    INSERT INTO HR.tbl_Parameters (name, Pvalues, Description, DataType, IsActive, CreatedAt, CreatedBy)
    VALUES (N'MINIMUM_REST_DAYS_PER_WEEK', N'2',
            N'Días libres mínimos por cada 7 días de ciclo en un patrón de rotación de más de 5 días', N'NUMERO', 1, SYSDATETIME(), 4);

IF NOT EXISTS (SELECT 1 FROM HR.tbl_Parameters WHERE name = N'MINIMUM_REST_DAYS_SEVERITY')
    INSERT INTO HR.tbl_Parameters (name, Pvalues, Description, DataType, IsActive, CreatedAt, CreatedBy)
    VALUES (N'MINIMUM_REST_DAYS_SEVERITY', N'BLOCKING',
            N'BLOCKING = impide guardar el patrón si no cumple los días libres mínimos; WARNING = no bloquea', N'TEXTO', 1, SYSDATETIME(), 4);

COMMIT;
SELECT name, Pvalues FROM HR.tbl_Parameters WHERE name LIKE N'MINIMUM_REST_DAYS%';
