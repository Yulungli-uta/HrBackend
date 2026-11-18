-- ============================================================================
-- 52_merge_canton_puyo_into_pastaza.sql
-- "PUYO" (0011603) no es un cantón: es la ciudad capital del cantón PASTAZA (0011601), provincia Pastaza (00116).
-- Cantones reales de Pastaza: Pastaza, Mera, Santa Clara, Arajuno.
--   1) Pasa a PASTAZA las personas, direcciones, instituciones y registros de staging que apuntaban a PUYO.
--   2) Borra el cantón PUYO (las 3 FK a tbl_Cantons impedían borrarlo mientras hubiera referencias).
-- La ciudad no se pierde: el campo Parroquia de las direcciones ya dice "PUYO".
-- Con guarda: si algún conteo no coincide con lo esperado, aborta y deshace todo. Idempotente.
-- ============================================================================
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @puyo CHAR(7) = '0011603', @pastaza CHAR(7) = '0011601';

IF NOT EXISTS (SELECT 1 FROM HR.tbl_Cantons WHERE CantonID = @puyo)
BEGIN
    PRINT 'El cantón PUYO ya no existe; no hay nada que hacer.';
    COMMIT; RETURN;
END
IF NOT EXISTS (SELECT 1 FROM HR.tbl_Cantons WHERE CantonID = @pastaza AND CantonName = 'PASTAZA' AND ProvinceID = '00116')
    THROW 50080, 'No se encontró el cantón PASTAZA (0011601) de la provincia 00116.', 1;
IF NOT EXISTS (SELECT 1 FROM HR.tbl_Cantons WHERE CantonID = @puyo AND CantonName = 'PUYO' AND ProvinceID = '00116')
    THROW 50081, 'El cantón 0011603 no es PUYO de la provincia 00116.', 1;

DECLARE @pp INT = (SELECT COUNT(*) FROM HR.tbl_People    WHERE CantonID = @puyo),
        @pa INT = (SELECT COUNT(*) FROM HR.tbl_Addresses WHERE CantonID = @puyo),
        @pi INT = (SELECT COUNT(*) FROM HR.tbl_Institutions WHERE CantonID = @puyo),
        @ps INT = (SELECT COUNT(*) FROM HR.stg_People    WHERE CantonID = @puyo);
PRINT CONCAT('Antes -> personas: ', @pp, ', direcciones: ', @pa, ', instituciones: ', @pi, ', staging: ', @ps);

UPDATE HR.tbl_People       SET CantonID = @pastaza WHERE CantonID = @puyo;
UPDATE HR.tbl_Addresses    SET CantonID = @pastaza WHERE CantonID = @puyo;
UPDATE HR.tbl_Institutions SET CantonID = @pastaza WHERE CantonID = @puyo;
UPDATE HR.stg_People       SET CantonID = @pastaza WHERE CantonID = @puyo;

IF EXISTS (SELECT 1 FROM HR.tbl_People WHERE CantonID = @puyo)
   OR EXISTS (SELECT 1 FROM HR.tbl_Addresses WHERE CantonID = @puyo)
   OR EXISTS (SELECT 1 FROM HR.tbl_Institutions WHERE CantonID = @puyo)
    THROW 50082, 'Quedaron referencias a PUYO; se deshace todo.', 1;

DELETE FROM HR.tbl_Cantons WHERE CantonID = @puyo;
COMMIT;
PRINT 'Cantón PUYO eliminado; referencias pasadas a PASTAZA.';
GO

SELECT CantonID, CantonName FROM HR.tbl_Cantons WHERE ProvinceID = '00116' ORDER BY CantonID;
SELECT (SELECT COUNT(*) FROM HR.tbl_People WHERE CantonID = '0011601') AS personas_pastaza,
       (SELECT COUNT(*) FROM HR.tbl_Addresses WHERE CantonID = '0011601') AS direcciones_pastaza;
