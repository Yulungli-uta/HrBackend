-- ============================================================================
-- 40_personnel_action_possession.sql
-- "Incluye posesión del puesto" en la Acción de Personal (mismo patrón que "Incluye declaración juramentada").
--
-- Cuando la opción está activa, el documento imprime en el bloque POSESIÓN DEL PUESTO el nombre y el número de
-- identificación del servidor de la propia acción (no se vuelven a ingresar). Si no está activa, las dos líneas quedan en
-- blanco para llenarlas a mano. Antes la identificación salía siempre con la cédula del empleado y el nombre siempre en blanco.
--
-- 1) HR.tbl_PersonnelActions.PossessionEnabled BIT NOT NULL DEFAULT 0 (las acciones existentes quedan sin la opción).
-- 2) Plantilla 20: las líneas del bloque usan {{POSSESSION_NAME}} y {{POSSESSION_IDCARD}} (campos MANUAL que el backend
--    completa solo cuando la opción está activa).
-- 3) Se elimina el campo "Medio" de la comunicación electrónica: sale la línea "MEDIO:" de la plantilla 20, el campo
--    NOTIFICATION_MEDIUM de la plantilla y la columna HR.tbl_PersonnelActions.NotificationMedium (ninguna acción tenía valor;
--    se recrea el CHECK CK_PersonnelActions_NotificationOff sin esa columna).
-- Idempotente. Reversión: DROP de PossessionEnabled (y su DF), restaurar el HtmlContent de la plantilla 20 desde el respaldo
-- y, si hiciera falta, volver a agregar NotificationMedium NVARCHAR(200) NULL con su CHECK.
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1) Columna ----------------------------------------------------------------------
IF COL_LENGTH('HR.tbl_PersonnelActions', 'PossessionEnabled') IS NULL
    ALTER TABLE HR.tbl_PersonnelActions ADD PossessionEnabled BIT NOT NULL
        CONSTRAINT DF_PersonnelActions_PossessionEnabled DEFAULT (0);
GO

-- 2) Plantilla 20: nombre e identificación de la posesión del puesto -----------------
DECLARE @old NVARCHAR(MAX) =
    N'<div class="mt1">YO, <span style="display:inline-block;width:55mm;border-bottom:1px solid #000">&nbsp;</span> CON NÚMERO DE DOCUMENTO DE IDENTIFICACIÓN <span style="display:inline-block;width:28mm;border-bottom:1px solid #000;text-align:center">{{EMPLOYEE_IDCARD}}</span>,</div>';
DECLARE @new NVARCHAR(MAX) =
    N'<div class="mt1">YO, <span style="display:inline-block;width:55mm;border-bottom:1px solid #000">{{POSSESSION_NAME}}&nbsp;</span> CON NÚMERO DE DOCUMENTO DE IDENTIFICACIÓN <span style="display:inline-block;width:28mm;border-bottom:1px solid #000;text-align:center">{{POSSESSION_IDCARD}}&nbsp;</span>,</div>';
DECLARE @h NVARCHAR(MAX) = (SELECT HtmlContent FROM HR.tbl_DocumentTemplates WHERE TemplateID = 20);

IF @h IS NOT NULL AND @h NOT LIKE N'%POSSESSION_NAME%'
BEGIN
    IF CHARINDEX(@old, @h) = 0 THROW 50040, 'No se encontró el bloque POSESIÓN DEL PUESTO esperado en la plantilla 20.', 1;
    UPDATE HR.tbl_DocumentTemplates
       SET HtmlContent = REPLACE(@h, @old, @new),
           Version = CAST(ISNULL(TRY_CAST(Version AS INT), 1) + 1 AS NVARCHAR(20)),
           UpdatedAt = SYSDATETIME()
     WHERE TemplateID = 20;
END
GO

INSERT INTO HR.tbl_DocumentTemplateFields
    (TemplateID, FieldName, Label, SourceType, SourceProperty, DataType, FormatPattern, DefaultValue, IsRequired, IsEditable, SortOrder, HelpText, CreatedAt)
SELECT 20, v.FieldName, v.Label, N'MANUAL', NULL, N'TEXT', NULL, NULL, 0, 0, v.SortOrder,
       N'Se completa solo cuando la acción incluye posesión del puesto (nombre e identificación del servidor).', SYSDATETIME()
FROM (VALUES
    (N'POSSESSION_NAME',   N'Nombre en la posesión del puesto',          675),
    (N'POSSESSION_IDCARD', N'Identificación en la posesión del puesto',  676)
) v (FieldName, Label, SortOrder)
WHERE EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplates WHERE TemplateID = 20)
  AND NOT EXISTS (SELECT 1 FROM HR.tbl_DocumentTemplateFields f WHERE f.TemplateID = 20 AND f.FieldName = v.FieldName);
GO

-- 3) Quitar la línea "MEDIO:" de la plantilla 20 (se conserva el espacio para la firma) ------
DECLARE @oldMedio NVARCHAR(MAX) = N'<p style="margin-bottom:12mm">* * MEDIO: {{NOTIFICATION_MEDIUM}}</p>';
DECLARE @newMedio NVARCHAR(MAX) = N'<div style="height:12mm"></div>';
DECLARE @h2 NVARCHAR(MAX) = (SELECT HtmlContent FROM HR.tbl_DocumentTemplates WHERE TemplateID = 20);
IF @h2 IS NOT NULL AND CHARINDEX(@oldMedio, @h2) > 0
    UPDATE HR.tbl_DocumentTemplates
       SET HtmlContent = REPLACE(@h2, @oldMedio, @newMedio),
           Version = CAST(ISNULL(TRY_CAST(Version AS INT), 1) + 1 AS NVARCHAR(20)),
           UpdatedAt = SYSDATETIME()
     WHERE TemplateID = 20;

DELETE FROM HR.tbl_DocumentTemplateFields WHERE TemplateID = 20 AND FieldName = N'NOTIFICATION_MEDIUM';
GO

-- 4) Quitar la columna NotificationMedium (el CHECK que la menciona se recrea sin ella) ------
IF COL_LENGTH('HR.tbl_PersonnelActions', 'NotificationMedium') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM HR.tbl_PersonnelActions WHERE NotificationMedium IS NOT NULL)
        THROW 50041, 'Hay acciones con NotificationMedium guardado: no se elimina la columna.', 1;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PersonnelActions_NotificationOff')
        ALTER TABLE HR.tbl_PersonnelActions DROP CONSTRAINT CK_PersonnelActions_NotificationOff;

    ALTER TABLE HR.tbl_PersonnelActions DROP COLUMN NotificationMedium;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PersonnelActions_NotificationOff')
    ALTER TABLE HR.tbl_PersonnelActions ADD CONSTRAINT CK_PersonnelActions_NotificationOff
        CHECK (NotificationEnabled = 1 OR (NotificationDate IS NULL AND NotificationTime IS NULL
                                           AND EmployeeSignatureDate IS NULL AND EmployeeSignatureTime IS NULL));
GO
