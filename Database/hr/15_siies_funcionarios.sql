-- ============================================================
-- Reporte SIIES Funcionarios / Funcionario Pasaporte (matrices 5.7/5.8, Instructivo
-- CACES v2S). Homologa SiiesLabel sobre catálogos existentes (Género/Sexo/Etnia/
-- Discapacidad) y agrega catálogos/columnas SIIES nuevos (RELACION_IES compartida
-- entre Contratos y Acciones, TIPO_FUNCIONARIO, TIPO/CATEGORIA_DOCENTE_LOES,
-- NACIONALIDAD_INDIGENA). Solo aditivo / idempotente.
-- Pendiente de decisión institucional (no bloquea): homologación de Discapacidad
-- 'Otra' y Sexo/Género 'Prefiero no decir' (sin equivalente en el catálogo SIIES).
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
-- Un renglón por empleado: homologa catálogos (SiiesLabel) y expone el régimen
-- laboral vigente/principal (EmployeeLaborRegimeService). Reglas condicionales
-- SIIES (nacionalidad, tipo docente LOES, formato de fecha, nombres pasaporte)
-- se resuelven en el IReportSource (C#), no aquí.
--
-- IMPORTANTE: esta vista solo refleja "quien está vigente HOY". El filtro de rango
-- de fechas del reporte usa en cambio SiiesFuncionariosReportSource.
-- GetEmployeesVigentesEnRangoAsync, que replica la MISMA cascada evaluada contra
-- el rango en vez de GETDATE(). Si tocas la cascada, replica el cambio ahí también.
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
    co.[CountryName],
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
    -- TIPO_FUNCIONARIO se homologa por Autoridad/Régimen laboral (no por tbl_jobs.
    -- SiiesTipoFuncionarioTypeId, que salía siempre vacío): Autoridad activa -> DIRECTIVO;
    -- si no, según régimen principal: LOSEP->ADMINISTRATIVO, Trabajo->TRABAJADOR, LOES->DOCENTE LOES.
    CASE
        WHEN EXISTS (
            SELECT 1 FROM [HR].[tbl_DepartmentAuthorities] da
            WHERE da.[EmployeeId] = e.[EmployeeID] AND da.[IsActive] = 1
              AND (da.[EndDate] IS NULL OR da.[EndDate] >= CAST(GETDATE() AS DATE))
        ) THEN N'DIRECTIVO'
        WHEN lr.[Name] = N'LOSEP' THEN N'ADMINISTRATIVO'
        WHEN lr.[Name] = N'Código Trabajo' THEN N'TRABAJADOR'
        WHEN lr.[Name] = N'LOES' THEN N'DOCENTE LOES'
        ELSE NULL
    END                             AS [TipoFuncionarioSiiesLabel],
    tdl.[SiiesLabel]                AS [TipoDocenteLoesSiiesLabel],
    cdl.[SiiesLabel]                AS [CategoriaDocenteLoesSiiesLabel],
    -- Fallback a ContractCode/ActionNumber (DocumentNumber a veces no se copió al crear el
    -- régimen) y luego a ctrFallback/paFallback para empleados sin fila en EmployeeLaborRegime.
    COALESCE(apt.[ActionNumber], elr.[DocumentNumber], ctr.[ContractCode], pa.[ActionNumber], ctrFallback.[ContractCode], paFallback.[ActionNumber]) AS [DocumentNumber],
    -- CAST a DATE obligatorio: mezclar date (EmployeeLaborRegime/PersonnelActions) con datetime2
    -- (tbl_Contracts) en un COALESCE promueve todo a datetime2 y rompe el mapeo EF Core DateOnly?.
    -- 2026-10-14: FECHA_INICIO = inicio del contrato/accion que respalda el regimen (ctr/pa); el
    -- EffectiveFrom del regimen queda solo como respaldo (se cargo en bloque con 2025-01-01 en 549
    -- filas) y HireDate como ultimo recurso. CAST a DATE obligatorio por el mismo motivo de arriba.
    COALESCE(CAST(apt.[ActionDate] AS DATE), CAST(ctr.[startdate] AS DATE), CAST(pa.[EffectiveDate] AS DATE), elr.[EffectiveFrom],
             CAST(ctrFallback.[startdate] AS DATE), paFallback.[EffectiveDate], e.[HireDate])   AS [EffectiveFrom],
    COALESCE(elr.[EffectiveTo], CAST(ctrFallback.[enddate] AS DATE), paFallback.[EndDate])             AS [EffectiveTo],
    elr.[IsActive]                  AS [RegimeIsActive],
    elr.[IngresoPorConcurso],
    CASE WHEN apt.[ActionId] IS NOT NULL THEN 'PERSONNEL_ACTION' ELSE COALESCE(elr.[DocumentType],
        CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
             WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) END        AS [RegimeDocumentType],
    lr.[Name]                       AS [LaborRegimeName],
    COALESCE(aptRel.[SiiesLabel], ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel]) AS [RelacionIesSiiesLabel],
    COALESCE(ctr.[ContractedHours], ctrFallback.[ContractedHours]) AS [ContractedHours],
    e.[IsActive]                    AS [EmployeeIsActive],
    e.[HireDate]
FROM [HR].[tbl_Employees] e
JOIN [HR].[tbl_People] p            ON p.[PersonID] = e.[PersonID]
LEFT JOIN [HR].[ref_Types] it        ON it.[TypeID] = p.[IdentType]
LEFT JOIN [HR].[ref_Types] sx        ON sx.[TypeID] = p.[Sex]
LEFT JOIN [HR].[ref_Types] gn        ON gn.[TypeID] = p.[Gender]
LEFT JOIN [HR].[tbl_Countries] co    ON co.[CountryID] = p.[CountryId]
LEFT JOIN [HR].[ref_Types] et        ON et.[TypeID] = p.[EthnicityTypeID]
LEFT JOIN [HR].[ref_Types] indig     ON indig.[TypeID] = p.[IndigenousNationalityTypeId]
-- Se une por p.Disability (texto libre, coincide con ref_Types.Name de DISABILITY_TYPE);
-- p.SpecialNeedsTypeID es otro catálogo (SPECIAL_NEEDS), nunca apunta a DISABILITY_TYPE.
LEFT JOIN [HR].[ref_Types] sn        ON sn.[Category] = 'DISABILITY_TYPE' AND sn.[Name] = p.[Disability]
LEFT JOIN [HR].[tbl_Departments] d   ON d.[DepartmentID] = e.[DepartmentID]
LEFT JOIN [HR].[tbl_jobs] j          ON j.[JobID] = e.[JobID]
LEFT JOIN [HR].[ref_Types] tdl       ON tdl.[TypeID] = e.[TipoDocenteLoesTypeId]
LEFT JOIN [HR].[ref_Types] cdl       ON cdl.[TypeID] = e.[CategoriaDocenteLoesTypeId]
OUTER APPLY (
    SELECT TOP 1 r.*
    FROM [HR].[tbl_EmployeeLaborRegime] r
    WHERE r.[EmployeeId] = e.[EmployeeID]
    ORDER BY CASE WHEN r.[IsPrincipal] = 1 AND r.[IsActive] = 1 THEN 0 ELSE 1 END, r.[EffectiveFrom] DESC
) elr
LEFT JOIN [HR].[ref_Types] lr                ON lr.[TypeID] = elr.[LaborRegimeId]
LEFT JOIN [HR].[tbl_Contracts] ctr           ON elr.[DocumentType] = 'CONTRACT' AND ctr.[ContractID] = elr.[SourceContractId]
LEFT JOIN [HR].[tbl_contract_type] ct        ON ct.[ContractTypeID] = ctr.[ContractTypeID]
LEFT JOIN [HR].[ref_Types] ctRel             ON ctRel.[TypeID] = ct.[SiiesRelacionIesTypeId]
LEFT JOIN [HR].[tbl_PersonnelActions] pa     ON elr.[DocumentType] = 'PERSONNEL_ACTION' AND pa.[ActionID] = elr.[SourcePersonnelActionId]
LEFT JOIN [HR].[tbl_personnel_action_type] pat ON pat.[PersonnelActionTypeId] = pa.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] patRel            ON patRel.[TypeID] = pat.[SiiesRelacionIesTypeId]
-- Fallback directo cuando elr no resolvió ningún documento; nunca reemplaza uno ya resuelto.
OUTER APPLY (
    SELECT TOP 1 c.*
    FROM [HR].[tbl_Contracts] c
    WHERE c.[PersonID] = p.[PersonID] AND c.[IsDeleted] = 0
      AND c.[Status] IN (274, 276) -- VIGENTE, VENCIDO (ref_Types CONTRACT_STATUS) — excluye ANULADO/BORRADOR/GENERADO/PENDIENTE_FIRMAS
      AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
    ORDER BY CASE WHEN c.[Status] = 274 THEN 0 ELSE 1 END, c.[startdate] DESC
) ctrFallback
LEFT JOIN [HR].[tbl_contract_type] ctFallback ON ctFallback.[ContractTypeID] = ctrFallback.[ContractTypeID]
LEFT JOIN [HR].[ref_Types] ctRelFallback      ON ctRelFallback.[TypeID] = ctFallback.[SiiesRelacionIesTypeId]
OUTER APPLY (
    SELECT TOP 1 pa2.*
    FROM [HR].[tbl_PersonnelActions] pa2
    WHERE pa2.[EmployeeID] = e.[EmployeeID] AND pa2.[IsDeleted] = 0
      AND pa2.[Status] IN ('VIGENTE', 'FINALIZADO') -- excluye ANULADO/BORRADOR/GENERADO
      AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
      AND ctrFallback.[ContractID] IS NULL -- prioriza el fallback de contrato si ya encontró uno
    ORDER BY CASE WHEN pa2.[Status] = 'VIGENTE' THEN 0 ELSE 1 END, pa2.[ActionDate] DESC
) paFallback
LEFT JOIN [HR].[tbl_personnel_action_type] patFallback ON patFallback.[PersonnelActionTypeId] = paFallback.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] patRelFallback               ON patRelFallback.[TypeID] = patFallback.[SiiesRelacionIesTypeId]
-- 2026-10-15: NUMERO_DOCUMENTO / FECHA_INICIO / RELACION_IES = documento con el que el empleado GANO su
-- nombramiento (HR.vw_EmployeeAppointment, regla de cadena: un permanente nuevo reinicia solo si cambia el cargo o hay
-- recategorizacion). Si no tiene nombramiento, o tiene una salida posterior (HasLaterExit), queda la cascada anterior.
-- Si el nombramiento actual es de fecha futura, se usa el primer permanente ya vigente.
OUTER APPLY (
    SELECT TOP 1 x.[ActionId], x.[ActionNumber], x.[ActionDate]
    FROM (
        SELECT ap.[AppointmentActionId] AS [ActionId], ap.[AppointmentActionNumber] AS [ActionNumber], ap.[AppointmentDate] AS [ActionDate], 1 AS [Pri]
        FROM [HR].[vw_EmployeeAppointment] ap
        WHERE ap.[EmployeeID] = e.[EmployeeID] AND ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] <= CAST(GETDATE() AS DATE)
        UNION ALL
        SELECT ap.[FirstPermanentActionId], ap.[FirstPermanentActionNumber], ap.[FirstPermanentDate], 2
        FROM [HR].[vw_EmployeeAppointment] ap
        WHERE ap.[EmployeeID] = e.[EmployeeID] AND ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] > CAST(GETDATE() AS DATE)
          AND ap.[FirstPermanentDate] <= CAST(GETDATE() AS DATE)
    ) x
    ORDER BY x.[Pri]
) apt
LEFT JOIN [HR].[tbl_PersonnelActions] aptPa           ON aptPa.[ActionID] = apt.[ActionId]
LEFT JOIN [HR].[tbl_personnel_action_type] aptPat     ON aptPat.[PersonnelActionTypeId] = aptPa.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] aptRel                     ON aptRel.[TypeID] = aptPat.[SiiesRelacionIesTypeId]
-- Excluye escalafón docente titular (va a HR.vw_SiiesProfesores) por patrón de cargo y por
-- tbl_TeacherStructure (casos con el mismo título de cargo pero sin carga horaria activa).
WHERE e.[IsDeleted] = 0
  AND ISNULL(j.[Description], '') NOT LIKE 'PROFESOR TITULAR%'
  AND NOT EXISTS (SELECT 1 FROM [HR].[tbl_TeacherStructure] ts WHERE ts.[EmployeeID] = e.[EmployeeID])
  -- Excluye profesores ocasionales/técnicos docentes (van a vw_SiiesProfesores) con el mismo
  -- criterio exacto, para que los dos reportes queden mutuamente excluyentes.
  AND NOT EXISTS (SELECT 1 FROM [HR].[fn_SiiesDocenteOcasionalContract](p.[PersonID]));
GO

-- 13.1) tbl_contract_type.IsDocenteOcasional + HR.fn_SiiesDocenteOcasionalContract ---------
-- Reemplaza el filtro por texto duplicado entre vw_SiiesFuncionarios (exclusión) y
-- vw_SiiesProfesores (inclusión). Verificado contra datos reales: mismo resultado exacto
-- que el filtro por texto (1,712 personas).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_contract_type]') AND name = 'IsDocenteOcasional')
    ALTER TABLE [HR].[tbl_contract_type] ADD [IsDocenteOcasional] BIT NOT NULL CONSTRAINT [DF_ContractType_IsDocenteOcasional] DEFAULT ((0));
GO

UPDATE [HR].[tbl_contract_type] SET [IsDocenteOcasional] = 1
WHERE [ContractTypeID] IN (17, 23, 41, 51, 52) AND [IsDocenteOcasional] = 0;
GO

CREATE OR ALTER FUNCTION [HR].[fn_SiiesDocenteOcasionalContract] (@PersonID INT)
RETURNS TABLE
AS
RETURN
(
    SELECT TOP 1 b.*
    FROM [HR].[tbl_Contracts] b
    LEFT JOIN [HR].[tbl_Contracts] p1 ON p1.[ContractID] = b.[ParentID]
    LEFT JOIN [HR].[tbl_Contracts] p2 ON p2.[ContractID] = p1.[ParentID]
    LEFT JOIN [HR].[tbl_contract_type] ctB  ON ctB.[ContractTypeID]  = b.[ContractTypeID]
    LEFT JOIN [HR].[tbl_contract_type] ctP1 ON ctP1.[ContractTypeID] = p1.[ContractTypeID]
    LEFT JOIN [HR].[tbl_contract_type] ctP2 ON ctP2.[ContractTypeID] = p2.[ContractTypeID]
    WHERE b.[PersonID] = @PersonID AND b.[IsDeleted] = 0
      AND (ISNULL(ctB.[IsDocenteOcasional],0) = 1 OR ISNULL(ctP1.[IsDocenteOcasional],0) = 1 OR ISNULL(ctP2.[IsDocenteOcasional],0) = 1)
    ORDER BY b.[startdate] DESC
);
GO

-- 14) Clasificación SiiesRelacionIesTypeId en tbl_contract_type / tbl_personnel_action_type.
-- Solo se clasifican los tipos con uso real en tbl_Contracts/tbl_PersonnelActions; el resto
-- queda NULL a propósito. Aditivo/idempotente — condicionado a SiiesRelacionIesTypeId IS NULL.

-- Contratos con relación de dependencia (2132): docentes ocasionales, técnicos y adendums de
-- Código de Trabajo. ContractTypeID 22 ("OCASIONALES (DELEGACIÓN)") incluido aquí: verificado
-- contra "Funcionario-v3 2526.xlsx" que es personal con vínculo laboral, no contrato civil.
UPDATE [HR].[tbl_contract_type] SET [SiiesRelacionIesTypeId] = 2132
WHERE [ContractTypeID] IN (46, 17, 47, 51, 23, 52, 22);

-- Contratos sin relación de dependencia (2133): "Prestación de Servicios" es un contrato
-- civil, no laboral. Sin evidencia real disponible todavía para 18/30 (nadie los usa en el
-- archivo real comparado) — queda la suposición original hasta que haya un caso para verificar.
UPDATE [HR].[tbl_contract_type] SET [SiiesRelacionIesTypeId] = 2133
WHERE [ContractTypeID] IN (18, 30) AND [SiiesRelacionIesTypeId] IS NULL;

-- Acciones de personal tipo ENTRY que establecen una relación de Nombramiento (2131).
-- 'Nombramiento Provisional' ya estaba clasificado, no se toca de nuevo.
UPDATE [HR].[tbl_personnel_action_type] SET [SiiesRelacionIesTypeId] = 2131
WHERE [PersonnelActionTypeId] IN (3, 17) AND [SiiesRelacionIesTypeId] IS NULL;
GO
