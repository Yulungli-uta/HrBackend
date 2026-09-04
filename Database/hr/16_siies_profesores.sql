-- ============================================================
-- Extensión de esquema: reporte SIIES Profesores
-- (matrices 5.2/5.3 Contratos, 5.4 Distribución Horas, 5.5 Formación
-- Profesional Terminado. Matriz 5.6 -Becas- queda FUERA de alcance
-- por decisión explícita del usuario. Instructivo CACES v2S, mayo 2026.)
-- Generado: 2026-08-03
--
-- Alcance de este script:
-- 1) HR.ref_Types: 4 categorías nuevas (SIIES_TIPO_ESCALAFON_NOMBRAMIENTO,
--    SIIES_NIVEL, SIIES_GRADO, SIIES_CATEGORIA_DOCENTE) + SiiesLabel sobre
--    HR.tbl_AcademicLadder y sobre la categoría ACADEMIC_LEVEL existente.
-- 2) HR.tbl_TeacherStructure: 3 columnas nuevas (TipoEscalafonNombramiento,
--    Nivel, Categoria) — todas NULL hasta que se complete la carga masiva
--    que el usuario va a ejecutar por separado.
-- 3) HR.tbl_EducationLevels: 1 columna nueva (SiiesGradoTypeId).
-- 4) HR.tbl_KnowledgeArea: columna SiiesCode agregada pero SIN poblar —
--    requiere mapeo manual fila por fila contra el anexo del instructivo,
--    marcado como pendiente (ver decisión institucional #3 del análisis).
-- 5) HR.vw_SiiesProfesores: vista de solo lectura, mismo patrón que
--    vw_SiiesFuncionarios.
--
-- Decisión de datos ya acordada (no requiere código adicional, solo
-- documentar): HORAS_CLASE_TERCER_NIVEL = Contracts.ContractedHours del
-- contrato vigente, resto de columnas de horas = 0 (no existe distributivo
-- real en el sistema). CODIGO_IES_ESTUDIO queda vacío (decisión pendiente,
-- diferida). Matriz 5.6 (becas) fuera de alcance.
--
-- Solo aditivo / idempotente — seguro de re-ejecutar. Ninguna columna
-- existente se elimina, renombra ni cambia de tipo.
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) Categorías nuevas de ref_Types ------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_TIPO_ESCALAFON_NOMBRAMIENTO')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_TIPO_ESCALAFON_NOMBRAMIENTO', N'Laboral Previo', N'LABORAL PREVIO', 1, GETDATE()),
        ('SIIES_TIPO_ESCALAFON_NOMBRAMIENTO', N'Laboral Actual', N'LABORAL ACTUAL', 1, GETDATE()),
        ('SIIES_TIPO_ESCALAFON_NOMBRAMIENTO', N'No Aplica', N'NO APLICA', 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_NIVEL')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_NIVEL', N'Tercer Nivel', N'TERCER NIVEL', 1, GETDATE()),
        ('SIIES_NIVEL', N'Cuarto Nivel', N'CUARTO NIVEL', 1, GETDATE()),
        ('SIIES_NIVEL', N'Tercer/Cuarto Nivel', N'TERCER/CUARTO NIVEL', 1, GETDATE());
END
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_GRADO')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_GRADO', N'Doctor (Ph.D)', N'DOCTOR (Ph.D)', 1, GETDATE()),
        ('SIIES_GRADO', N'Maestría o Equivalente', N'MAESTRÍA O EQUIVALENTE', 1, GETDATE()),
        ('SIIES_GRADO', N'Diploma Superior', N'DIPLOMA SUPERIOR', 1, GETDATE()),
        ('SIIES_GRADO', N'Doctor en Filosofía o Jurisprudencia', N'DOCTOR EN FILOSOFIA O JURISPRUDENCIA', 1, GETDATE()),
        ('SIIES_GRADO', N'Especialista', N'ESPECIALISTA', 1, GETDATE()),
        ('SIIES_GRADO', N'Especialista Área Salud', N'ESPECIALISTA AREA SALUD', 1, GETDATE());
END
GO

-- Tabla 9 del instructivo: 12 valores posibles de CATEGORIA (Profesores).
-- Fuente directa: HR.tbl_TeacherStructure.SiiesCategoriaTypeId (columna nueva,
-- punto 2 de este script). Se llena junto con la carga masiva planeada por
-- el usuario; si queda NULL, el reporte intenta derivar desde
-- AcademicLadder.SiiesLabel como respaldo (ver vw_SiiesProfesores).
IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_CATEGORIA_DOCENTE')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_CATEGORIA_DOCENTE', N'Titular Principal', N'TITULAR PRINCIPAL', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Titular Agregado', N'TITULAR AGREGADO', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Titular Auxiliar', N'TITULAR AUXILIAR', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Titular No Escalafonado', N'TITULAR NO ESCALAFONADO', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Principal 1', N'PRINCIPAL1', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Principal 2', N'PRINCIPAL2', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Principal 3', N'PRINCIPAL3', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Agregado 1', N'AGREGADO1', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Agregado 2', N'AGREGADO2', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Agregado 3', N'AGREGADO3', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Auxiliar 1', N'AUXILIAR1', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Auxiliar 2', N'AUXILIAR2', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Ocasional II', N'OCASIONALII', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Honorario', N'HONORARIO', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Ocasional', N'OCASIONAL', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'Invitado', N'INVITADO', 1, GETDATE()),
        ('SIIES_CATEGORIA_DOCENTE', N'No Titular', N'NO TITULAR', 1, GETDATE());
END
GO

-- 2) Homologación SiiesLabel sobre catálogos existentes ----------------------

-- ACADEMIC_LEVEL (usado por tbl_EducationLevels.EducationLevelTypeID).
-- Hoy solo existe el valor NIVEL_1 en uso real (30 registros). Se homologa
-- como TERCER NIVEL por ser el valor por defecto más común; si en el futuro
-- se usan NIVEL_2+ para posgrado, requieren revisión manual (no se asume).
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'TERCER NIVEL' WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_1' AND [SiiesLabel] IS NULL;
GO

-- HR.tbl_AcademicLadder.SiiesLabel: mapeo best-effort por patrón de Code/Name.
-- Deja NULL lo que no se pueda mapear con certeza (requiere confirmación
-- institucional antes de usarse en el reporte, mismo criterio que Género
-- 'Otros'/Discapacidad 'Otra' en el reporte Funcionarios).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_AcademicLadder]') AND name = 'SiiesLabel')
    ALTER TABLE [HR].[tbl_AcademicLadder] ADD [SiiesLabel] NVARCHAR(50) NULL;
GO

UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'PRINCIPAL1' WHERE [Code] LIKE '%PRINCIPAL%1%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'PRINCIPAL2' WHERE [Code] LIKE '%PRINCIPAL%2%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'PRINCIPAL3' WHERE [Code] LIKE '%PRINCIPAL%3%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AGREGADO1' WHERE [Code] LIKE '%AGREGADO%1%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AGREGADO2' WHERE [Code] LIKE '%AGREGADO%2%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AGREGADO3' WHERE [Code] LIKE '%AGREGADO%3%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AUXILIAR1' WHERE [Code] LIKE '%AUXILIAR%1%' AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AUXILIAR2' WHERE [Code] LIKE '%AUXILIAR%2%' AND [SiiesLabel] IS NULL;
-- Filas de auxiliar/agregado/principal sin sufijo numérico -> variante base "1".
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AUXILIAR1'  WHERE [Code] LIKE '%AUXILIAR%'  AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'AGREGADO1'  WHERE [Code] LIKE '%AGREGADO%'  AND [SiiesLabel] IS NULL;
UPDATE [HR].[tbl_AcademicLadder] SET [SiiesLabel] = N'PRINCIPAL1' WHERE [Code] LIKE '%PRINCIPAL%' AND [SiiesLabel] IS NULL;
GO

-- 3) HR.tbl_TeacherStructure: 3 columnas nuevas -------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_TeacherStructure]') AND name = 'SiiesTipoEscalafonNombramientoTypeId')
    ALTER TABLE [HR].[tbl_TeacherStructure] ADD [SiiesTipoEscalafonNombramientoTypeId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_TeacherStructure]') AND name = 'SiiesNivelTypeId')
    ALTER TABLE [HR].[tbl_TeacherStructure] ADD [SiiesNivelTypeId] INT NULL;
GO

-- Fuente directa de CATEGORIA. Si queda NULL, la vista cae de respaldo a
-- AcademicLadder.SiiesLabel (cubre solo la familia Titular/Principal/
-- Agregado/Auxiliar; las categorías Ocasional/Honorario/Invitado/No Titular
-- no tienen escalafón y requieren esta columna llena explícitamente).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_TeacherStructure]') AND name = 'SiiesCategoriaTypeId')
    ALTER TABLE [HR].[tbl_TeacherStructure] ADD [SiiesCategoriaTypeId] INT NULL;
GO

-- 4) HR.tbl_EducationLevels: SiiesGradoTypeId + KnowledgeAreaId --------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SiiesGradoTypeId')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SiiesGradoTypeId] INT NULL;
GO

-- No existía ningún vínculo entre un título (EducationLevels) y el árbol de
-- CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO (tbl_KnowledgeArea). Se agrega
-- para que a futuro se pueda capturar; queda NULL hasta que exista pantalla o
-- carga que lo asigne (ver decisión pendiente #3 del análisis).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'KnowledgeAreaId')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [KnowledgeAreaId] INT NULL;
GO

-- 5) HR.tbl_KnowledgeArea.SiiesCode -------------------------------------------
-- Columna agregada pero SIN poblar: requiere mapeo manual fila por fila
-- contra el Anexo Clasificación Internacional Normalizada de la Educación
-- del instructivo (78 filas de nivel de detalle). No se homologa automático
-- para evitar códigos incorrectos en un reporte hacia un ente gubernamental.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_KnowledgeArea]') AND name = 'SiiesCode')
    ALTER TABLE [HR].[tbl_KnowledgeArea] ADD [SiiesCode] NVARCHAR(20) NULL;
GO

-- 6) Vista HR.vw_SiiesProfesores -----------------------------------------------
CREATE OR ALTER VIEW [HR].[vw_SiiesProfesores] AS
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
    et.[SiiesLabel]                AS [EthnicitySiiesLabel],
    indig.[SiiesLabel]             AS [IndigenousNationalitySiiesLabel],
    sn.[SiiesLabel]                AS [DisabilitySiiesLabel],
    p.[DisabilityPercentage],
    p.[CONADISCard],
    e.[Email]                      AS [InstitutionalEmail],
    d.[Name]                       AS [DepartmentName],
    ts.[TeacherStructureID],
    ts.[WeeklyClassHours],
    escal.[SiiesLabel]             AS [TipoEscalafonNombramientoSiiesLabel],
    nivel.[SiiesLabel]             AS [NivelSiiesLabel],
    -- CATEGORIA: prioridad a la columna directa; si está NULL, respaldo desde AcademicLadder.
    COALESCE(catDirecta.[SiiesLabel], la.[SiiesLabel]) AS [CategoriaSiiesLabel],
    ded.[SiiesLabel]                AS [TiempoDedicacionSiiesLabel],
    -- 2026-08-27: fallback a ContractCode/ActionNumber cuando elr.DocumentNumber nunca se
    -- copió al crear el régimen — mismo criterio que vw_SiiesFuncionarios.
    -- 2026-09-02: segundo fallback (ctrFallback/paFallback) para profesores sin ninguna fila en
    -- tbl_EmployeeLaborRegime (210 de 213 profesores activos con NUMERO_DOCUMENTO vacío
    -- verificados en esa condición) — mismo criterio que vw_SiiesFuncionarios.
    COALESCE(elr.[DocumentNumber], ctr.[ContractCode], pa.[ActionNumber], ctrFallback.[ContractCode], paFallback.[ActionNumber]) AS [DocumentNumber],
    COALESCE(elr.[DocumentType],
        CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
             WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END)          AS [RegimeDocumentType],
    -- CAST a DATE obligatorio: ver comentario equivalente en vw_SiiesFuncionarios — mezclar
    -- date (EmployeeLaborRegime/PersonnelActions) con datetime2 (tbl_Contracts.startdate/enddate)
    -- en un COALESCE rompe el mapeo EF Core DateOnly? con InvalidCastException.
    COALESCE(elr.[EffectiveFrom], CAST(ctrFallback.[startdate] AS DATE), paFallback.[EffectiveDate])   AS [EffectiveFrom],
    COALESCE(elr.[EffectiveTo], CAST(ctrFallback.[enddate] AS DATE), paFallback.[EndDate])             AS [EffectiveTo],
    elr.[IsActive]                  AS [RegimeIsActive],
    elr.[IngresoPorConcurso],
    COALESCE(ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel]) AS [RelacionIesSiiesLabel],
    COALESCE(ctr.[ContractedHours], ctrFallback.[ContractedHours]) AS [ContractedHours],
    e.[IsActive]                    AS [EmployeeIsActive],
    e.[HireDate]
FROM [HR].[tbl_Employees] e
JOIN [HR].[tbl_People] p              ON p.[PersonID] = e.[PersonID]
LEFT JOIN [HR].[ref_Types] it          ON it.[TypeID] = p.[IdentType]
LEFT JOIN [HR].[ref_Types] sx          ON sx.[TypeID] = p.[Sex]
LEFT JOIN [HR].[ref_Types] gn          ON gn.[TypeID] = p.[Gender]
LEFT JOIN [HR].[tbl_Countries] co      ON co.[CountryID] = p.[CountryId]
LEFT JOIN [HR].[ref_Types] et          ON et.[TypeID] = p.[EthnicityTypeID]
LEFT JOIN [HR].[ref_Types] indig       ON indig.[TypeID] = p.[IndigenousNationalityTypeId]
-- 2026-08-27: se une por p.Disability (texto libre) — ver comentario en vw_SiiesFuncionarios.
LEFT JOIN [HR].[ref_Types] sn          ON sn.[Category] = 'DISABILITY_TYPE' AND sn.[Name] = p.[Disability]
LEFT JOIN [HR].[tbl_Departments] d     ON d.[DepartmentID] = e.[DepartmentID]
OUTER APPLY (
    SELECT TOP 1 t.*
    FROM [HR].[tbl_TeacherStructure] t
    WHERE t.[EmployeeID] = e.[EmployeeID]
    ORDER BY CASE WHEN t.[IsActive] = 1 THEN 0 ELSE 1 END, t.[StartDate] DESC
) ts
LEFT JOIN [HR].[ref_Types] escal       ON escal.[TypeID] = ts.[SiiesTipoEscalafonNombramientoTypeId]
LEFT JOIN [HR].[ref_Types] nivel       ON nivel.[TypeID] = ts.[SiiesNivelTypeId]
LEFT JOIN [HR].[ref_Types] catDirecta  ON catDirecta.[TypeID] = ts.[SiiesCategoriaTypeId]
LEFT JOIN [HR].[tbl_AcademicLadder] la ON la.[LadderID] = ts.[LadderID]
LEFT JOIN [HR].[ref_Types] ded         ON ded.[TypeID] = ts.[DedicationTypeID]
OUTER APPLY (
    SELECT TOP 1 r.*
    FROM [HR].[tbl_EmployeeLaborRegime] r
    WHERE r.[EmployeeId] = e.[EmployeeID]
    ORDER BY CASE WHEN r.[IsPrincipal] = 1 AND r.[IsActive] = 1 THEN 0 ELSE 1 END, r.[EffectiveFrom] DESC
) elr
LEFT JOIN [HR].[tbl_Contracts] ctr            ON elr.[DocumentType] = 'CONTRACT' AND ctr.[ContractID] = elr.[SourceContractId]
LEFT JOIN [HR].[tbl_contract_type] ct         ON ct.[ContractTypeID] = ctr.[ContractTypeID]
LEFT JOIN [HR].[ref_Types] ctRel              ON ctRel.[TypeID] = ct.[SiiesRelacionIesTypeId]
LEFT JOIN [HR].[tbl_PersonnelActions] pa      ON elr.[DocumentType] = 'PERSONNEL_ACTION' AND pa.[ActionID] = elr.[SourcePersonnelActionId]
LEFT JOIN [HR].[tbl_personnel_action_type] pat ON pat.[PersonnelActionTypeId] = pa.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] patRel             ON patRel.[TypeID] = pat.[SiiesRelacionIesTypeId]
-- 2026-09-02: fallback directo — mismo criterio y mismos filtros de Status que vw_SiiesFuncionarios.
OUTER APPLY (
    SELECT TOP 1 c.*
    FROM [HR].[tbl_Contracts] c
    WHERE c.[PersonID] = p.[PersonID] AND c.[IsDeleted] = 0
      AND c.[Status] IN (274, 276) -- VIGENTE, VENCIDO
      AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
    ORDER BY CASE WHEN c.[Status] = 274 THEN 0 ELSE 1 END, c.[startdate] DESC
) ctrFallback
LEFT JOIN [HR].[tbl_contract_type] ctFallback ON ctFallback.[ContractTypeID] = ctrFallback.[ContractTypeID]
LEFT JOIN [HR].[ref_Types] ctRelFallback      ON ctRelFallback.[TypeID] = ctFallback.[SiiesRelacionIesTypeId]
OUTER APPLY (
    SELECT TOP 1 pa2.*
    FROM [HR].[tbl_PersonnelActions] pa2
    WHERE pa2.[EmployeeID] = e.[EmployeeID] AND pa2.[IsDeleted] = 0
      AND pa2.[Status] IN ('VIGENTE', 'FINALIZADO')
      AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
      AND ctrFallback.[ContractID] IS NULL
    ORDER BY CASE WHEN pa2.[Status] = 'VIGENTE' THEN 0 ELSE 1 END, pa2.[ActionDate] DESC
) paFallback
LEFT JOIN [HR].[tbl_personnel_action_type] patFallback ON patFallback.[PersonnelActionTypeId] = paFallback.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] patRelFallback               ON patRelFallback.[TypeID] = patFallback.[SiiesRelacionIesTypeId]
WHERE e.[IsDeleted] = 0 AND ts.[TeacherStructureID] IS NOT NULL;
GO

-- 7) Vista HR.vw_SiiesFormacionProfesional (matriz 5.5, depende de empleados con TeacherStructure) --
CREATE OR ALTER VIEW [HR].[vw_SiiesFormacionProfesional] AS
SELECT
    e.[EmployeeID],
    p.[IDCard],
    it.[Name]           AS [IdentTypeName],
    inst.[CountryID]     AS [InstitutionCountryId],
    inst.[Name]          AS [InstitutionName],
    nivelCat.[SiiesLabel] AS [NivelSiiesLabel],
    grado.[SiiesLabel]    AS [GradoSiiesLabel],
    el.[Title]            AS [NombreTitulo],
    ka.[SiiesCode]        AS [CampoDetalladoSiiesCode],
    el.[SenescytRegistrationNumber],
    el.[EndDate]          AS [FechaObtuvoTitulo]
FROM [HR].[tbl_EducationLevels] el
JOIN [HR].[tbl_People] p                ON p.[PersonID] = el.[PersonID]
JOIN [HR].[tbl_Employees] e             ON e.[PersonID] = p.[PersonID] AND e.[IsDeleted] = 0
JOIN [HR].[tbl_TeacherStructure] ts     ON ts.[EmployeeID] = e.[EmployeeID]
LEFT JOIN [HR].[ref_Types] it            ON it.[TypeID] = p.[IdentType]
LEFT JOIN [HR].[tbl_Institutions] inst   ON inst.[InstitutionID] = el.[InstitutionID]
LEFT JOIN [HR].[ref_Types] nivelCat      ON nivelCat.[TypeID] = el.[EducationLevelTypeID]
LEFT JOIN [HR].[ref_Types] grado         ON grado.[TypeID] = el.[SiiesGradoTypeId]
LEFT JOIN [HR].[tbl_KnowledgeArea] ka    ON ka.[id] = el.[KnowledgeAreaId];
GO
