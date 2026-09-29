-- ============================================================================
-- Catálogo BANK_INSTITUTION (ref_Types)
-- Generado: 2026-09-28
-- ----------------------------------------------------------------------------
-- Hallazgo informe UTA-DITIC-PS-027-2026, observación 42: "Entidad financiera"
-- en Cuentas Bancarias era texto libre; se pide listar los bancos existentes
-- en Ecuador + opción "Otro" para texto libre.
--
-- Fuente: catálogo oficial de códigos de instituciones financieras (Banco
-- Central del Ecuador / Superintendencia de Bancos), 25 bancos activos
-- verificados. Para cooperativas de ahorro y crédito Segmento 1 (SEPS,
-- activos > $80M) solo se cargan las 17 que se pudieron verificar por nombre
-- completo contra el mismo catálogo oficial; la lista completa de Segmento 1
-- (~43 entidades) requiere el boletín SEPS vigente para no cargar nombres
-- desactualizados o inexactos — pendiente de que RRHH lo proporcione.
--
-- Administración posterior de este catálogo (agregar/desactivar bancos o
-- cooperativas): pantalla "Parámetros del Sistema" (HrParametersPage.tsx),
-- categoría ya registrada en HR_PARAMETER_DOMAINS (features/constants.ts).
-- No requiere script SQL para altas futuras.
-- ============================================================================

-- --- Bancos (25) -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Pichincha')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Pichincha', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco de Guayaquil')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco de Guayaquil', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Citibank')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Citibank', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Machala')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Machala', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco de Loja')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco de Loja', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco del Pacífico')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco del Pacífico', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Internacional')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Internacional', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Amazonas')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Amazonas', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco del Austro')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco del Austro', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Produbanco / Promerica')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Produbanco / Promerica', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Bolivariano')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Bolivariano', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Comercial de Manabí')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Comercial de Manabí', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco General Rumiñahui')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco General Rumiñahui', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco del Litoral')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco del Litoral', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Solidario')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Solidario', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco ProCredit')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco ProCredit', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Capital')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Capital', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco de Desarrollo de los Pueblos (Codesarrollo)')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco de Desarrollo de los Pueblos (Codesarrollo)', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'BanEcuador')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'BanEcuador', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Delbank')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Delbank', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Ecuatoriano de la Vivienda')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Ecuatoriano de la Vivienda', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco para la Asistencia Comunitaria FINCA')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco para la Asistencia Comunitaria FINCA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco del Instituto Ecuatoriano de Seguridad Social (BIESS)')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco del Instituto Ecuatoriano de Seguridad Social (BIESS)', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco D-Miro')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco D-Miro', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Banco Coopnacional')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Banco Coopnacional', 1, GETDATE());

-- --- Cooperativas de ahorro y crédito, Segmento 1 (17 verificadas) -----------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Juventud Ecuatoriana Progresista (JEP)')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Juventud Ecuatoriana Progresista (JEP)', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Jardín Azuayo')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Jardín Azuayo', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Cooprogreso')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Cooprogreso', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Riobamba')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Riobamba', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa 29 de Octubre')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa 29 de Octubre', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa San Francisco')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa San Francisco', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Chone')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Chone', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Chibuleo')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Chibuleo', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Pablo Muñoz Vega')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Pablo Muñoz Vega', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Erco')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Erco', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Virgen del Cisne')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Virgen del Cisne', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'CACPE Loja')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'CACPE Loja', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'CACPE Cotopaxi (CACEC)')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'CACPE Cotopaxi (CACEC)', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'CACPE Pastaza')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'CACPE Pastaza', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'CACPE Gualaquiza')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'CACPE Gualaquiza', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa Alfonso Jaramillo León')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa Alfonso Jaramillo León', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Cooperativa de Servidores Públicos del Ministerio de Educación (CME)')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Cooperativa de Servidores Públicos del Ministerio de Educación (CME)', 1, GETDATE());

-- --- Fallback ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'BANK_INSTITUTION' AND [Name] = N'Otro')
    INSERT INTO [HR].[ref_Types] (Category, Name, IsActive, CreatedAt) VALUES ('BANK_INSTITUTION', N'Otro', 1, GETDATE());
GO
