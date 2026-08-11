-- ============================================================
-- Extensión de esquema: reporte SIIES Funcionarios / Funcionario Pasaporte
-- (matrices 5.7 y 5.8, Instructivo Carga Masiva CACES v2S, mayo 2026)
-- Generado: 2026-08-03
--
-- 1) HR.ref_Types.SiiesLabel: denominación exacta SIIES para valores de
--    catálogo ya existentes (GENDER_TYPE, SEX_TYPE, ETHNICITY, DISABILITY_TYPE).
-- 2) Categorías ref_Types nuevas: SIIES_RELACION_IES, SIIES_TIPO_FUNCIONARIO,
--    SIIES_TIPO_DOCENTE_LOES, SIIES_CATEGORIA_DOCENTE_LOES,
--    SIIES_INDIGENOUS_NATIONALITY.
-- 3) HR.tbl_contract_type.SiiesRelacionIesTypeId y
--    HR.tbl_personnel_action_type.SiiesRelacionIesTypeId: ambos apuntan a la
--    misma categoría SIIES_RELACION_IES (decisión: una sola lista compartida).
-- 4) HR.tbl_EmployeeLaborRegime.IngresoPorConcurso: NULL = sin clasificar
--    (673 registros existentes no tenían este dato); se llena hacia adelante.
-- 5) HR.tbl_jobs.SiiesTipoFuncionarioTypeId y PuestoJerarquicoSuperior:
--    clasificación por cargo, no por empleado.
-- 6) HR.tbl_Employees.TipoDocenteLoesTypeId / CategoriaDocenteLoesTypeId:
--    NULL/NO APLICA salvo que el cargo del empleado sea DOCENTE LOES.
-- 7) HR.tbl_People.IndigenousNationalityTypeId: solo aplica si Etnia=INDIGENA.
-- 8) HR.TBL_PARAMETERS: CODIGO_IES y CODIGO_MATRIZ_EXTENSION (reutiliza tabla
--    de parámetros existente, no se crea tabla nueva).
--
-- Solo aditivo / idempotente — seguro de re-ejecutar. Ninguna columna
-- existente se elimina, renombra ni cambia de tipo.
--
-- PENDIENTE DE DECISIÓN INSTITUCIONAL (no bloquea este script, ya documentado
-- en el análisis Fase 1): homologación exacta de Discapacidad = 'Otra' (sin
-- equivalente en el catálogo SIIES), y de Sexo/Género = 'Prefiero no decir'.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) ref_Types.SiiesLabel ----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[ref_Types]') AND name = 'SiiesLabel')
    ALTER TABLE [HR].[ref_Types] ADD [SiiesLabel] NVARCHAR(100) NULL;
GO

-- Homologación Género (Tabla 0 del instructivo)
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'NO DISPONE'   WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'Prefiero no decir';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'MASCULINO'    WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'Hombre';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'FEMENINO/A'   WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'Mujer';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'NO BINARIO'   WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'No Binario';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'TRANSMASCULINO' WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'Hombre Trans';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'TRANSFEMENINA'  WHERE [Category] = 'GENDER_TYPE' AND [Name] = N'Mujer Trans';
-- 'Otros' y cualquier valor con bug de codificación quedan sin SiiesLabel a propósito: requieren decisión institucional (ver Tabla 0, no hay equivalente directo).
GO

-- Homologación Sexo (Tabla 1)
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'HOMBRE' WHERE [Category] = 'SEX_TYPE' AND [Name] = N'Masculino';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'MUJER'  WHERE [Category] = 'SEX_TYPE' AND [Name] = N'Femenino';
-- 'Prefiero no decir' queda sin SiiesLabel: catálogo SIIES es binario, requiere decisión institucional.
GO

-- Homologación Etnia (Tabla 3)
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'MESTIZO/A'        WHERE [Category] = 'ETHNICITY' AND [Name] = N'Mestizo';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'AFROECUATORIANO/A' WHERE [Category] = 'ETHNICITY' AND [Name] = N'Afroecuatoriano';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'INDIGENA'         WHERE [Category] = 'ETHNICITY' AND [Name] = N'Indígena';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'MONTUBIO/A'       WHERE [Category] = 'ETHNICITY' AND [Name] = N'Montubio';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'BLANCO/A'         WHERE [Category] = 'ETHNICITY' AND [Name] = N'Blanco';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'OTRO'             WHERE [Category] = 'ETHNICITY' AND [Name] = N'Otro';
-- Faltan en el catálogo local: Negro/a, Mulato/a, No Registra. Se agregan como nuevas filas si no existen:
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ETHNICITY' AND [Name] = N'Negro/a')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('ETHNICITY', N'Negro/a', N'NEGRO/A', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ETHNICITY' AND [Name] = N'Mulato/a')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('ETHNICITY', N'Mulato/a', N'MULATO/A', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'ETHNICITY' AND [Name] = N'No Registra')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('ETHNICITY', N'No Registra', N'NO REGISTRA', 1, GETDATE());
GO

-- Homologación Discapacidad (Tabla 2)
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'FISICA MOTORA'     WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Física';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'MENTAL PSICOSOCIAL' WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Mental';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'AUDITIVA'          WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Auditiva';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'VISUAL'            WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Visual';
-- 'Otra' queda sin SiiesLabel a propósito: SIIES no tiene un valor "otra" para discapacidad, requiere decisión institucional.
-- Faltan en el catálogo local: Ninguna, Intelectual, Lenguaje. Se agregan como nuevas filas si no existen:
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Ninguna')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('DISABILITY_TYPE', N'Ninguna', N'NINGUNA', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Intelectual')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('DISABILITY_TYPE', N'Intelectual', N'INTELECTUAL', 1, GETDATE());
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'DISABILITY_TYPE' AND [Name] = N'Lenguaje')
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES ('DISABILITY_TYPE', N'Lenguaje', N'LENGUAJE', 1, GETDATE());
GO

-- 2) Categoría nueva: SIIES_RELACION_IES (Tabla 7) ---------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_RELACION_IES')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_RELACION_IES', N'Nombramiento', N'NOMBRAMIENTO', 1, GETDATE()),
        ('SIIES_RELACION_IES', N'Contrato con relación de dependencia', N'CONTRATO CON RELACION DE DEPENDENCIA', 1, GETDATE()),
        ('SIIES_RELACION_IES', N'Contrato sin relación de dependencia', N'CONTRATO SIN RELACION DE DEPENDENCIA', 1, GETDATE()),
        ('SIIES_RELACION_IES', N'Prometeo', N'PROMETEO', 1, GETDATE());
END
GO

-- 3) Categoría nueva: SIIES_TIPO_FUNCIONARIO (Tabla 15) ----------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_TIPO_FUNCIONARIO')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_TIPO_FUNCIONARIO', N'Trabajador', N'TRABAJADOR', 1, GETDATE()),
        ('SIIES_TIPO_FUNCIONARIO', N'Administrativo', N'ADMINISTRATIVO', 1, GETDATE()),
        ('SIIES_TIPO_FUNCIONARIO', N'Directivo', N'DIRECTIVO', 1, GETDATE()),
        ('SIIES_TIPO_FUNCIONARIO', N'Docente LOES', N'DOCENTE LOES', 1, GETDATE());
END
GO

-- 4) Categorías nuevas: SIIES_TIPO_DOCENTE_LOES y SIIES_CATEGORIA_DOCENTE_LOES (Tablas 27/28)
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_TIPO_DOCENTE_LOES')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_TIPO_DOCENTE_LOES', N'Técnico Docente', N'TECNICO DOCENTE', 1, GETDATE()),
        ('SIIES_TIPO_DOCENTE_LOES', N'Técnico Laboratorio', N'TECNICO LABORATORIO', 1, GETDATE()),
        ('SIIES_TIPO_DOCENTE_LOES', N'Técnico Investigación', N'TECNICO INVESTIGACION', 1, GETDATE()),
        ('SIIES_TIPO_DOCENTE_LOES', N'Técnico Artes', N'TECNICO ARTES', 1, GETDATE()),
        ('SIIES_TIPO_DOCENTE_LOES', N'No Aplica', N'NO APLICA', 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_CATEGORIA_DOCENTE_LOES')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'Categoría 1', N'CATEGORIA1', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'Categoría 2', N'CATEGORIA2', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'Categoría 3', N'CATEGORIA3', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'Categoría 4', N'CATEGORIA4', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'Categoría 5', N'CATEGORIA5', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE_LOES', N'No Aplica', N'NO APLICA', 1, GETDATE());
END
GO

-- 5) Categoría nueva: SIIES_INDIGENOUS_NATIONALITY (Tabla 4, completa) -------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_INDIGENOUS_NATIONALITY')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_INDIGENOUS_NATIONALITY', N'No Aplica', N'NO APLICA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Achuar', N'ACHUAR', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Awa', N'AWA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'A''i Cofán', N'AL COFAN', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Chachi', N'CHACHI', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Epera', N'EPERA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Waorani', N'WAORANI', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Quichua', N'QUICHUA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Secoya', N'SECOYA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Shuar', N'SHUAR', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Siona', N'SIONA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Tsáchila', N'TSACHILA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Shiwiar', N'SHIWIAR', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Zápara', N'ZAPARA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Andoa', N'ANDOA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Kichwa', N'KICHWA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Pastos', N'PASTOS', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Natabuela', N'NATABUELA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Otavalo', N'OTAVALO', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Karanki', N'KARANKI', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Kayambi', N'KAYAMBI', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Kitukara', N'KITUKARA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Panzaleo', N'PANZALEO', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Chibuleo', N'CHIBULEO', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Salasaka', N'SALASAKA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Kisapincha', N'KISAPINCHA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Tomabela', N'TOMABELA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Waranka', N'WARANKA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Puruhá', N'PURUHA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Cañari', N'KAÑARI', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Saraguro', N'SARAGURO', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Paltas', N'PALTAS', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Manta', N'MANTA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'Huancavilca', N'HUANCAVILCA', 1, GETDATE()),
        ('SIIES_INDIGENOUS_NATIONALITY', N'No Registra', N'NO REGISTRA', 1, GETDATE());
END
GO

-- 6) tbl_contract_type.SiiesRelacionIesTypeId --------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_contract_type]') AND name = 'SiiesRelacionIesTypeId')
    ALTER TABLE [HR].[tbl_contract_type] ADD [SiiesRelacionIesTypeId] INT NULL;
GO

-- 7) tbl_personnel_action_type.SiiesRelacionIesTypeId (misma categoría que 6) -
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_personnel_action_type]') AND name = 'SiiesRelacionIesTypeId')
    ALTER TABLE [HR].[tbl_personnel_action_type] ADD [SiiesRelacionIesTypeId] INT NULL;
GO

-- 8) tbl_EmployeeLaborRegime.IngresoPorConcurso ------------------------------
-- NULL a propósito (no NOT NULL DEFAULT 0): distingue "sin clasificar" de "NO".
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EmployeeLaborRegime]') AND name = 'IngresoPorConcurso')
    ALTER TABLE [HR].[tbl_EmployeeLaborRegime] ADD [IngresoPorConcurso] BIT NULL;
GO

-- 9) tbl_jobs.SiiesTipoFuncionarioTypeId y PuestoJerarquicoSuperior ----------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_jobs]') AND name = 'SiiesTipoFuncionarioTypeId')
    ALTER TABLE [HR].[tbl_jobs] ADD [SiiesTipoFuncionarioTypeId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_jobs]') AND name = 'PuestoJerarquicoSuperior')
    ALTER TABLE [HR].[tbl_jobs]
        ADD [PuestoJerarquicoSuperior] BIT NOT NULL CONSTRAINT [DF_tbl_jobs_PuestoJerarquicoSuperior] DEFAULT ((0));
GO

-- 10) tbl_Employees.TipoDocenteLoesTypeId / CategoriaDocenteLoesTypeId -------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_Employees]') AND name = 'TipoDocenteLoesTypeId')
    ALTER TABLE [HR].[tbl_Employees] ADD [TipoDocenteLoesTypeId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_Employees]') AND name = 'CategoriaDocenteLoesTypeId')
    ALTER TABLE [HR].[tbl_Employees] ADD [CategoriaDocenteLoesTypeId] INT NULL;
GO

-- 11) tbl_People.IndigenousNationalityTypeId ---------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_People]') AND name = 'IndigenousNationalityTypeId')
    ALTER TABLE [HR].[tbl_People] ADD [IndigenousNationalityTypeId] INT NULL;
GO

-- 12) TBL_PARAMETERS: CODIGO_IES / CODIGO_MATRIZ_EXTENSION -------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[TBL_PARAMETERS] WHERE [Name] = N'CODIGO_IES')
    INSERT INTO [HR].[TBL_PARAMETERS] (Name, Pvalues, Description, DataType, IsActive)
    VALUES (N'CODIGO_IES', N'1010', N'Código de la IES asignado por el MINEDEC/CACES para reportes SIIES.', N'TEXTO', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[TBL_PARAMETERS] WHERE [Name] = N'CODIGO_MATRIZ_EXTENSION')
    INSERT INTO [HR].[TBL_PARAMETERS] (Name, Pvalues, Description, DataType, IsActive)
    VALUES (N'CODIGO_MATRIZ_EXTENSION', N'1010-MAT-01', N'Código de Matriz/Extensión obtenido en el sistema SIIES (Menú Matriz/Extensión).', N'TEXTO', 1);
GO

-- 13) Vista HR.vw_SiiesFuncionarios -------------------------------------------
-- Un renglón por empleado (funcionario), resolviendo la homologación de catálogos
-- (SiiesLabel) y el régimen laboral vigente/principal ya calculado por
-- EmployeeLaborRegimeService (IsPrincipal/IsActive). Reglas condicionales SIIES
-- (NACIONALIDAD según Etnia, TIPO_DOCENTE_LOES según TIPO_FUNCIONARIO, formato de
-- fecha, separación de nombres para pasaporte) se resuelven en el IReportSource
-- (C#), no aquí — la vista solo joinea y expone los valores crudos/homologados.
CREATE OR ALTER VIEW [HR].[vw_SiiesFuncionarios] AS
SELECT
    e.[EmployeeID],
    p.[PersonID],
    p.[IdentType],
    it.[Name]                      AS [IdentTypeName],
    p.[IDCard],
    p.[FirstName],
    p.[LastName],
    p.[BirthDate],
    sx.[SiiesLabel]                AS [SexSiiesLabel],
    gn.[SiiesLabel]                AS [GenderSiiesLabel],
    p.[CountryId],
    et.[Name]                      AS [EthnicityName],
    et.[SiiesLabel]                AS [EthnicitySiiesLabel],
    indig.[SiiesLabel]             AS [IndigenousNationalitySiiesLabel],
    sn.[SiiesLabel]                AS [DisabilitySiiesLabel],
    p.[DisabilityPercentage],
    p.[CONADISCard],
    e.[Email]                      AS [InstitutionalEmail],
    d.[Name]                       AS [DepartmentName],
    j.[Description]                AS [JobDescription],
    ISNULL(j.[PuestoJerarquicoSuperior], 0) AS [PuestoJerarquicoSuperior],
    tf.[SiiesLabel]                AS [TipoFuncionarioSiiesLabel],
    tdl.[SiiesLabel]                AS [TipoDocenteLoesSiiesLabel],
    cdl.[SiiesLabel]                AS [CategoriaDocenteLoesSiiesLabel],
    elr.[DocumentNumber],
    elr.[EffectiveFrom],
    elr.[EffectiveTo],
    elr.[IsActive]                  AS [RegimeIsActive],
    elr.[IngresoPorConcurso],
    elr.[DocumentType]              AS [RegimeDocumentType],
    COALESCE(ctRel.[SiiesLabel], patRel.[SiiesLabel]) AS [RelacionIesSiiesLabel],
    ctr.[ContractedHours],
    e.[IsActive]                    AS [EmployeeIsActive],
    e.[HireDate]
FROM [HR].[tbl_Employees] e
JOIN [HR].[tbl_People] p            ON p.[PersonID] = e.[PersonID]
LEFT JOIN [HR].[ref_Types] it        ON it.[TypeID] = p.[IdentType]
LEFT JOIN [HR].[ref_Types] sx        ON sx.[TypeID] = p.[Sex]
LEFT JOIN [HR].[ref_Types] gn        ON gn.[TypeID] = p.[Gender]
LEFT JOIN [HR].[ref_Types] et        ON et.[TypeID] = p.[EthnicityTypeID]
LEFT JOIN [HR].[ref_Types] indig     ON indig.[TypeID] = p.[IndigenousNationalityTypeId]
LEFT JOIN [HR].[ref_Types] sn        ON sn.[TypeID] = p.[SpecialNeedsTypeID]
LEFT JOIN [HR].[tbl_Departments] d   ON d.[DepartmentID] = e.[DepartmentID]
LEFT JOIN [HR].[tbl_jobs] j          ON j.[JobID] = e.[JobID]
LEFT JOIN [HR].[ref_Types] tf        ON tf.[TypeID] = j.[SiiesTipoFuncionarioTypeId]
LEFT JOIN [HR].[ref_Types] tdl       ON tdl.[TypeID] = e.[TipoDocenteLoesTypeId]
LEFT JOIN [HR].[ref_Types] cdl       ON cdl.[TypeID] = e.[CategoriaDocenteLoesTypeId]
OUTER APPLY (
    SELECT TOP 1 r.*
    FROM [HR].[tbl_EmployeeLaborRegime] r
    WHERE r.[EmployeeId] = e.[EmployeeID]
    ORDER BY CASE WHEN r.[IsPrincipal] = 1 AND r.[IsActive] = 1 THEN 0 ELSE 1 END, r.[EffectiveFrom] DESC
) elr
LEFT JOIN [HR].[tbl_Contracts] ctr           ON elr.[DocumentType] = 'CONTRACT' AND ctr.[ContractID] = elr.[SourceContractId]
LEFT JOIN [HR].[tbl_contract_type] ct        ON ct.[ContractTypeID] = ctr.[ContractTypeID]
LEFT JOIN [HR].[ref_Types] ctRel             ON ctRel.[TypeID] = ct.[SiiesRelacionIesTypeId]
LEFT JOIN [HR].[tbl_PersonnelActions] pa     ON elr.[DocumentType] = 'PERSONNEL_ACTION' AND pa.[ActionID] = elr.[SourcePersonnelActionId]
LEFT JOIN [HR].[tbl_personnel_action_type] pat ON pat.[PersonnelActionTypeId] = pa.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] patRel            ON patRel.[TypeID] = pat.[SiiesRelacionIesTypeId]
WHERE e.[IsDeleted] = 0;
GO
