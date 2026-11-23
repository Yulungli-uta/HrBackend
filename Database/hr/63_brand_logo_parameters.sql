-- Logos parametrizables en HR.TBL_PARAMETERS (convención: parámetros centralizados, sin tablas por módulo).
-- Pvalues vacío = se usa el logo oficial incluido en la aplicación. Con valor = data URI base64 (data:image/png;base64,...), máx. 1 MB.
-- Se administran desde Parámetros del Sistema > Identidad visual; el API no devuelve su contenido en el listado de parámetros.
SET NOCOUNT ON;
MERGE INTO HR.TBL_PARAMETERS AS tgt
USING (VALUES
    (N'BRAND_LOGO_LOGIN_LIGHT', N'Logo del inicio de sesión (modo claro). Vacío = logo oficial.'),
    (N'BRAND_LOGO_LOGIN_DARK',  N'Logo del inicio de sesión (modo oscuro). Vacío = logo oficial.'),
    (N'BRAND_LOGO_REPORT',      N'Logo de reportes y documentos PDF. Vacío = logo oficial.')
) AS src (Name, Description)
ON tgt.Name = src.Name
WHEN NOT MATCHED THEN
    INSERT (Name, DataType, Description, Pvalues, IsActive, CreatedAt, CreatedBy)
    VALUES (src.Name, N'TEXTO', src.Description, NULL, 1, SYSDATETIME(), NULL);
SELECT Name, DataType, IsActive, CASE WHEN Pvalues IS NULL THEN 0 ELSE 1 END AS TieneValor FROM HR.TBL_PARAMETERS WHERE Name LIKE 'BRAND_LOGO_%' ORDER BY Name;
