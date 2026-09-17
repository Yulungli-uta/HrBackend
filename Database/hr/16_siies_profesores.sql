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
-- 6) [2026-09-16] Integración DINARDAP (WsUtaDinardap.Api): corrige la
--    numeración de ACADEMIC_LEVEL (NIVEL_1 tenía "TERCER NIVEL" mal puesto,
--    ver sección 8 al final), migra los 30 registros existentes (fixtures
--    QA, no producción) al nivel correcto, y agrega a tbl_EducationLevels
--    las columnas que hacen falta para guardar todo lo que devuelve
--    DINARDAP (fechas de grado/registro SENESCYT, tipo, nivel original,
--    origen del registro) + índice único filtrado en
--    SenescytRegistrationNumber.
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

-- 5.1) Homologación SiiesRelacionIesTypeId sobre 4 tipos de contrato sin mapear ----------
-- 2026-09-11: huecos de catálogo encontrados al revisar por qué 200 profesores activos
-- salían con RELACION_IES vacía en el reporte SIIES -- sus hermanos (con/sin "(DELEGACIÓN)")
-- ya estaban mapeados, estos 4 quedaron fuera del UPDATE original. Valores confirmados con
-- el usuario 2026-09-11 siguiendo el mismo patrón que sus hermanos ya mapeados.
UPDATE ct
SET ct.[SiiesRelacionIesTypeId] = rt.[TypeID]
FROM [HR].[tbl_contract_type] ct
CROSS JOIN (SELECT [TypeID] FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_RELACION_IES' AND [Name] = N'Contrato con relación de dependencia') rt
WHERE ct.[Name] IN (N'CONTRATO TÉCNICO DOCENTE DCF', N'CONTRATO TÉCNICO DE LABORATORIO 1', N'CONTRATO TÉCNICO DE LABORATORIO1')
  AND ct.[SiiesRelacionIesTypeId] IS NULL;
GO

UPDATE ct
SET ct.[SiiesRelacionIesTypeId] = rt.[TypeID]
FROM [HR].[tbl_contract_type] ct
CROSS JOIN (SELECT [TypeID] FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_RELACION_IES' AND [Name] = N'Contrato sin relación de dependencia') rt
WHERE ct.[Name] = N'CONTRATO POR SERVICIOS OCASIONALES'
  AND ct.[SiiesRelacionIesTypeId] IS NULL;
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
    -- 2026-09-11: cuando no hay TeacherStructure (profesor ocasional, ver mas abajo) se
    -- respalda a "No Aplica" -- los ocasionales no tienen escalafon.
    COALESCE(escal.[SiiesLabel], CASE WHEN ts.[TeacherStructureID] IS NULL AND ocasionalDocente.[ContractID] IS NOT NULL THEN escalNoAplicaDefault.[SiiesLabel] END) AS [TipoEscalafonNombramientoSiiesLabel],
    nivel.[SiiesLabel]             AS [NivelSiiesLabel],
    -- CATEGORIA: prioridad a la columna directa; si está NULL, respaldo desde AcademicLadder;
    -- si tampoco (profesor ocasional sin TeacherStructure), respaldo a "Ocasional".
    COALESCE(catDirecta.[SiiesLabel], la.[SiiesLabel], CASE WHEN ocasionalDocente.[ContractID] IS NOT NULL THEN catOcasionalDefault.[SiiesLabel] END) AS [CategoriaSiiesLabel],
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
    -- 2026-09-11: si el documento resuelto es una Acción de Personal (no Contrato) y su tipo
    -- de acción no tiene RelacionIes mapeada (ej. Promoción/Recategorización/Reintegro -- son
    -- modificaciones administrativas posteriores, no el acto fundacional Nombramiento/
    -- Designación/Nombramiento Provisional, únicos 3 tipos mapeados a propósito), se asume
    -- "Nombramiento" por decisión explícita del usuario: si está vinculado por Acción de
    -- Personal, es porque su relación con la IES es de nombramiento.
    COALESCE(
        ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel],
        -- 2026-09-11: si el contrato resuelto es un ADENDUM/PRORROGA/RENOVACIÓN (modificación,
        -- no tiene RelacionIes propia a propósito), se sube por ParentID hasta el contrato base
        -- (ctRelChainType, definido más abajo) -- decisión explícita del usuario.
        ctRelChainType.[SiiesLabel],
        CASE WHEN COALESCE(elr.[DocumentType],
                CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
                     WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) = 'PERSONNEL_ACTION'
             THEN relIesNombramientoDefault.[SiiesLabel] END
    ) AS [RelacionIesSiiesLabel],
    COALESCE(ctr.[ContractedHours], ctrFallback.[ContractedHours]) AS [ContractedHours],
    e.[IsActive]                    AS [EmployeeIsActive],
    e.[HireDate],
    -- 2026-09-11: período académico más reciente con distributivo real cargado (visible
    -- directo en la vista, sin pasar por fn_SiiesProfesoresHoras). Mismo criterio de
    -- "más reciente" que usa esa función cuando @PeriodCode es NULL.
    latestPeriod.[PeriodCode]       AS [LatestPeriodCode],
    latestPeriod.[PeriodStart]      AS [LatestPeriodStart],
    latestPeriod.[PeriodEnd]        AS [LatestPeriodEnd]
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
    SELECT TOP 1 a.[PeriodCode], a.[PeriodStart], a.[PeriodEnd]
    FROM [HR].[tbl_AcademicHoursDistribution] a
    WHERE a.[IDCard] = p.[IDCard]
    ORDER BY a.[PeriodEnd] DESC
) latestPeriod
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
-- 2026-09-11: profesores OCASIONALES (nunca tienen fila en tbl_TeacherStructure -- esa
-- tabla es solo para Titulares, confirmado con el usuario). Se identifican por tener un
-- contrato de tipo "Profesor/a Ocasional" o "Técnico Docente" (con o sin Delegación) --
-- criterio confirmado explícitamente con el usuario 2026-09-11. "Técnico de Laboratorio"
-- queda fuera a propósito (va al reporte de Funcionarios, no a Profesores).
-- 2026-09-11 (ajuste): un ADENDUM por sí solo no dice de qué contrato es -- se resuelve
-- via tbl_Contracts.ParentID (contrato padre). Un ADENDUM cuenta solo si su padre es
-- Profesor Ocasional/Técnico Docente -- esto excluye correctamente las ~75 adendas cuyo
-- padre es Técnico de Laboratorio (confirmado con datos reales antes de aplicar).
OUTER APPLY (
    SELECT TOP 1 c2.*
    FROM [HR].[tbl_Contracts] c2
    INNER JOIN [HR].[tbl_contract_type] ct2 ON ct2.[ContractTypeID] = c2.[ContractTypeID]
    WHERE c2.[PersonID] = p.[PersonID] AND c2.[IsDeleted] = 0
      AND (
            ct2.[Name] LIKE N'CONTRATO PROFESOR/A OCASIONAL%'
         OR ct2.[Name] LIKE N'CONTRATO TÉCNICO DOCENTE%'
         OR (
              ct2.[Name] LIKE N'ADENDUM%'
              AND EXISTS (
                  SELECT 1
                  FROM [HR].[tbl_Contracts] parentC
                  INNER JOIN [HR].[tbl_contract_type] parentCt ON parentCt.[ContractTypeID] = parentC.[ContractTypeID]
                  WHERE parentC.[ContractID] = c2.[ParentID]
                    AND (
                          parentCt.[Name] LIKE N'CONTRATO PROFESOR/A OCASIONAL%'
                       OR parentCt.[Name] LIKE N'CONTRATO TÉCNICO DOCENTE%'
                        )
              )
            )
          )
    ORDER BY c2.[startdate] DESC
) ocasionalDocente
LEFT JOIN [HR].[ref_Types] catOcasionalDefault
    ON catOcasionalDefault.[Category] = 'SIIES_CATEGORIA_DOCENTE' AND catOcasionalDefault.[Name] = N'Ocasional'
LEFT JOIN [HR].[ref_Types] escalNoAplicaDefault
    ON escalNoAplicaDefault.[Category] = 'SIIES_TIPO_ESCALAFON_NOMBRAMIENTO' AND escalNoAplicaDefault.[Name] = N'No Aplica'
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
-- 2026-09-11: cadena ParentID para RELACION_IES cuando el contrato resuelto (ctr o
-- ctrFallback) es un ADENDUM/PRORROGA/RENOVACIÓN -- sube hasta 2 niveles (cadena real
-- observada en datos: adenda -> adenda -> contrato base) buscando el primer ancestro cuyo
-- tipo de contrato sí tenga SiiesRelacionIesTypeId mapeado. Decisión explícita del usuario.
OUTER APPLY (
    SELECT TOP 1 COALESCE(ctSelf.[SiiesRelacionIesTypeId], ctParent.[SiiesRelacionIesTypeId], ctGrandparent.[SiiesRelacionIesTypeId]) AS [SiiesRelacionIesTypeId]
    FROM [HR].[tbl_Contracts] cSelf
    LEFT JOIN [HR].[tbl_contract_type] ctSelf        ON ctSelf.[ContractTypeID] = cSelf.[ContractTypeID]
    LEFT JOIN [HR].[tbl_Contracts] cParent           ON cParent.[ContractID] = cSelf.[ParentID]
    LEFT JOIN [HR].[tbl_contract_type] ctParent      ON ctParent.[ContractTypeID] = cParent.[ContractTypeID]
    LEFT JOIN [HR].[tbl_Contracts] cGrandparent      ON cGrandparent.[ContractID] = cParent.[ParentID]
    LEFT JOIN [HR].[tbl_contract_type] ctGrandparent ON ctGrandparent.[ContractTypeID] = cGrandparent.[ContractTypeID]
    WHERE cSelf.[ContractID] = COALESCE(ctr.[ContractID], ctrFallback.[ContractID])
) ctRelChain
LEFT JOIN [HR].[ref_Types] ctRelChainType ON ctRelChainType.[TypeID] = ctRelChain.[SiiesRelacionIesTypeId]
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
-- 2026-09-11: default de RELACION_IES = "Nombramiento" cuando el documento resuelto es Acción
-- de Personal y su tipo no tiene mapeo propio (ver comentario junto a RelacionIesSiiesLabel).
LEFT JOIN [HR].[ref_Types] relIesNombramientoDefault
    ON relIesNombramientoDefault.[Category] = 'SIIES_RELACION_IES' AND relIesNombramientoDefault.[Name] = N'Nombramiento'
-- 2026-09-11: antes exigia TeacherStructure (solo Titulares). Ahora tambien entran
-- los profesores ocasionales identificados por tipo de contrato (ver OUTER APPLY
-- ocasionalDocente arriba).
WHERE e.[IsDeleted] = 0
  AND (
        ts.[TeacherStructureID] IS NOT NULL
     OR ocasionalDocente.[ContractID] IS NOT NULL
      );
GO

-- 6.1) Función de tabla HR.fn_SiiesProfesoresHoras (matriz 5.4, Distribución de Horas)
-- --------------------------------------------------------------------------------
-- 2026-09-10: reemplaza el placeholder de horas en 0 (ver vw_SiiesProfesores original,
-- que nunca tuvo columnas de horas por actividad) con datos reales de
-- HR.tbl_AcademicHoursDistribution (cargados desde el sistema "UTA Mático",
-- servidor 10.102.12.3, ajeno a HrBackend — carga por lotes, ver Database/hr/
-- 22_academic_hours_distribution.sql).
--
-- Se usa una FUNCIÓN DE TABLA (no una vista plana) porque una vista no acepta
-- parámetros y las horas dependen del período académico (un profesor puede
-- tener filas para el período 47 y para el 48, con horas distintas en cada
-- una) — vw_SiiesProfesores debe seguir devolviendo una sola fila por
-- profesor, así que el período no puede resolverse dentro de esa vista sin
-- arriesgar filas duplicadas.
--
-- @PeriodCode = NULL (por defecto): todos los profesores, cada uno con su
-- período más reciente disponible (mismo comportamiento que tenía el
-- placeholder: siempre devuelve algo). @PeriodCode = '47'/'48'/...: filtra
-- exacto a ese período Y RESTRINGE la lista a quienes tuvieron distributivo
-- cargado ese período específico — mismo criterio de "filtrar de verdad" ya
-- usado en HR.fn_SiiesFormacionProfesional, confirmado con el usuario
-- 2026-09-11 (la primera versión solo cambiaba las columnas de horas sin
-- reducir la lista de profesores, lo cual generó confusión).
CREATE OR ALTER FUNCTION [HR].[fn_SiiesProfesoresHoras] (@PeriodCode VARCHAR(10) = NULL)
RETURNS TABLE
AS
RETURN
(
    SELECT
        v.*,
        ahd.[PeriodCode]           AS [HoursPeriodCode],
        ahd.[PeriodStart]          AS [HoursPeriodStart],
        ahd.[PeriodEnd]            AS [HoursPeriodEnd],
        ahd.[TotalHours],
        ahd.[ClassHours],
        ahd.[ManagementHours],
        ahd.[ResearchHours],
        ahd.[OtherActivitiesHours],
        ahd.[TutoringHours],
        ahd.[OutreachHours]
    FROM [HR].[vw_SiiesProfesores] v
    OUTER APPLY (
        SELECT TOP 1 a.*
        FROM [HR].[tbl_AcademicHoursDistribution] a
        WHERE a.[IDCard] = v.[IDCard]
          AND (@PeriodCode IS NULL OR a.[PeriodCode] = @PeriodCode)
        ORDER BY a.[PeriodEnd] DESC
    ) ahd
    WHERE @PeriodCode IS NULL
       OR EXISTS (
            SELECT 1
            FROM [HR].[tbl_AcademicHoursDistribution] a2
            WHERE a2.[IDCard] = v.[IDCard] AND a2.[PeriodCode] = @PeriodCode
          )
);
GO

-- 7) Vista HR.vw_SiiesFormacionProfesional (matriz 5.5) ------------------------
-- 2026-09-11: cambiada de INNER JOIN sobre tbl_EducationLevels (tabla ancla) a LEFT JOIN
-- desde HR.vw_SiiesProfesores. Antes, si el profesor no tenía ningún título cargado en
-- tbl_EducationLevels (hoy: 0 de los 224 Titulares), ni su identificación aparecía y el
-- reporte quedaba completamente vacío. Ahora aparecen TODOS los profesores (mismo criterio
-- que vw_SiiesProfesores: Titulares + Ocasionales), con los campos de título en NULL cuando
-- no los tienen cargados — visible para que RRHH sepa a quién le falta esa información.
--
-- [2026-09-16, reescrita] Dos problemas encontrados al verificar el reporte tras la
-- sincronización DINARDAP (ver sección 8): (1) traía UNA FILA POR TÍTULO de
-- tbl_EducationLevels, duplicando profesores con más de un título cargado — se agrega
-- ROW_NUMBER() para quedarse solo con el título de MAYOR nivel/grado por profesor
-- (Doctorado > Maestría > Especialista(Salud) > Diplomado > Tercer Nivel > sin
-- clasificar > sin título), una sola fila por EmployeeID, decisión explícita del
-- usuario. (2) FECHA_OBTUVO_TITULO leía [EndDate] (fecha fin de estudios, 0/3409
-- registros la tienen) en vez de [SenescytGraduationDate] (fecha de grado real que
-- llegó de DINARDAP, 2333/3409 la tienen) — corregido con COALESCE, [EndDate] queda
-- de respaldo para registros manuales antiguos. NOMBRES_IES ahora cae a
-- [InstitutionNameOriginal] cuando no hay [InstitutionID] catalogado (caso DINARDAP:
-- institución no obligatoria, ver 8.3b) — antes quedaba en blanco aunque el título sí
-- tuviera institución. PAIS_ESTUDIO sigue en blanco sin institución catalogada:
-- DINARDAP no informa país en el detalle del título, no hay fuente real de la que
-- tomarlo (mismo criterio que CODIGO_IES_ESTUDIO, no se inventa el dato).
CREATE OR ALTER VIEW [HR].[vw_SiiesFormacionProfesional] AS
WITH [Titulos] AS (
    SELECT
        v.[EmployeeID], v.[IDCard], v.[IdentTypeName],
        v.[LatestPeriodCode], v.[LatestPeriodStart], v.[LatestPeriodEnd],
        el.[EducationID],
        inst.[CountryID]                                     AS [InstitutionCountryId],
        COALESCE(inst.[Name], el.[InstitutionNameOriginal])  AS [InstitutionName],
        nivelCat.[Name]                                      AS [NivelName],
        nivelCat.[SiiesLabel]                                AS [NivelSiiesLabel],
        grado.[Name]                                         AS [GradoName],
        grado.[SiiesLabel]                                   AS [GradoSiiesLabel],
        el.[Title]                                           AS [NombreTitulo],
        ka.[SiiesCode]                                        AS [CampoDetalladoSiiesCode],
        el.[SenescytRegistrationNumber],
        COALESCE(el.[SenescytGraduationDate], el.[SenescytRegistrationDate], el.[EndDate])  AS [FechaObtuvoTitulo]
    FROM [HR].[vw_SiiesProfesores] v
    LEFT JOIN [HR].[tbl_EducationLevels] el  ON el.[PersonID] = v.[PersonID]
    LEFT JOIN [HR].[tbl_Institutions] inst   ON inst.[InstitutionID] = el.[InstitutionID]
    LEFT JOIN [HR].[ref_Types] nivelCat      ON nivelCat.[TypeID] = el.[EducationLevelTypeID]
    LEFT JOIN [HR].[ref_Types] grado         ON grado.[TypeID] = el.[SiiesGradoTypeId]
    LEFT JOIN [HR].[tbl_KnowledgeArea] ka    ON ka.[id] = el.[KnowledgeAreaId]
),
[Ranked] AS (
    SELECT *,
        ROW_NUMBER() OVER (
            PARTITION BY [EmployeeID]
            ORDER BY
                CASE WHEN [NivelName] IS NULL THEN 99
                     WHEN [NivelName] = 'NIVEL_4' THEN 1
                     WHEN [NivelName] = 'NIVEL_3' THEN 2
                     ELSE 3 END,
                CASE [GradoName]
                     WHEN 'Doctor (Ph.D)' THEN 1
                     WHEN 'Maestría o Equivalente' THEN 2
                     WHEN 'Especialista Área Salud' THEN 3
                     WHEN 'Especialista' THEN 3
                     WHEN 'Diploma Superior' THEN 4
                     ELSE 5 END,
                [FechaObtuvoTitulo] DESC,
                [EducationID] DESC
        ) AS [Rn]
    FROM [Titulos]
)
SELECT
    [EmployeeID], [IDCard], [IdentTypeName],
    [InstitutionCountryId], [InstitutionName],
    [NivelSiiesLabel], [GradoSiiesLabel], [NombreTitulo],
    [CampoDetalladoSiiesCode], [SenescytRegistrationNumber], [FechaObtuvoTitulo],
    [LatestPeriodCode], [LatestPeriodStart], [LatestPeriodEnd]
FROM [Ranked]
WHERE [Rn] = 1;
GO

-- 7.1) Función de tabla HR.fn_SiiesFormacionProfesional (filtro de período académico)
-- --------------------------------------------------------------------------------
-- 2026-09-11: mismo patrón que HR.fn_SiiesProfesoresHoras — una VIEW no acepta
-- parámetros. @PeriodCode = NULL (por defecto) devuelve todos los profesores igual
-- que vw_SiiesFormacionProfesional sin filtrar. @PeriodCode = '47'/'48' restringe
-- la lista a quienes tuvieron actividad real ese período en
-- HR.tbl_AcademicHoursDistribution (mismo criterio de "período" que el resto del
-- reporte SIIES Profesores).
--
-- [2026-09-16] Con @PeriodCode también se filtra por FECHA — regla explícita del
-- usuario: "si en el periodo todavía no ha adquirido el título no debería aparecer".
-- Se calcula la fecha de corte del período (MAX([PeriodEnd]) de
-- tbl_AcademicHoursDistribution para ese código) y solo cuentan títulos con
-- COALESCE([SenescytGraduationDate],[EndDate]) <= esa fecha; títulos con fecha
-- desconocida (ambas columnas NULL) quedan fuera del filtro por período específico
-- porque no se puede confirmar que ya existían — no se inventa el dato. Sin
-- @PeriodCode (NULL) no hay corte de fecha, igual que antes. La deduplicación
-- (mayor nivel por profesor, ver vista) se recalcula completa DENTRO del período,
-- porque el título más alto vigente hoy puede no haber existido todavía en un
-- período histórico, y en ese caso debe ganar el siguiente más alto que sí existía.
CREATE OR ALTER FUNCTION [HR].[fn_SiiesFormacionProfesional] (@PeriodCode VARCHAR(10) = NULL)
RETURNS TABLE
AS
RETURN
(
    SELECT * FROM [HR].[vw_SiiesFormacionProfesional] WHERE @PeriodCode IS NULL

    UNION ALL

    SELECT
        [EmployeeID], [IDCard], [IdentTypeName],
        [InstitutionCountryId], [InstitutionName],
        [NivelSiiesLabel], [GradoSiiesLabel], [NombreTitulo],
        [CampoDetalladoSiiesCode], [SenescytRegistrationNumber], [FechaObtuvoTitulo],
        [LatestPeriodCode], [LatestPeriodStart], [LatestPeriodEnd]
    FROM (
        SELECT *,
            ROW_NUMBER() OVER (
                PARTITION BY [EmployeeID]
                ORDER BY
                    CASE WHEN [NivelName] IS NULL THEN 99
                         WHEN [NivelName] = 'NIVEL_4' THEN 1
                         WHEN [NivelName] = 'NIVEL_3' THEN 2
                         ELSE 3 END,
                    CASE [GradoName]
                         WHEN 'Doctor (Ph.D)' THEN 1
                         WHEN 'Maestría o Equivalente' THEN 2
                         WHEN 'Especialista Área Salud' THEN 3
                         WHEN 'Especialista' THEN 3
                         WHEN 'Diploma Superior' THEN 4
                         ELSE 5 END,
                    [FechaObtuvoTitulo] DESC,
                    [EducationID] DESC
            ) AS [Rn]
        FROM (
            SELECT
                v.[EmployeeID], v.[IDCard], v.[IdentTypeName],
                v.[LatestPeriodCode], v.[LatestPeriodStart], v.[LatestPeriodEnd],
                el.[EducationID],
                inst.[CountryID]                                     AS [InstitutionCountryId],
                COALESCE(inst.[Name], el.[InstitutionNameOriginal])  AS [InstitutionName],
                nivelCat.[Name]                                      AS [NivelName],
                nivelCat.[SiiesLabel]                                AS [NivelSiiesLabel],
                grado.[Name]                                         AS [GradoName],
                grado.[SiiesLabel]                                   AS [GradoSiiesLabel],
                el.[Title]                                           AS [NombreTitulo],
                ka.[SiiesCode]                                        AS [CampoDetalladoSiiesCode],
                el.[SenescytRegistrationNumber],
                COALESCE(el.[SenescytGraduationDate], el.[SenescytRegistrationDate], el.[EndDate])  AS [FechaObtuvoTitulo]
            FROM [HR].[vw_SiiesProfesores] v
            LEFT JOIN [HR].[tbl_EducationLevels] el
                ON el.[PersonID] = v.[PersonID]
               AND COALESCE(el.[SenescytGraduationDate], el.[SenescytRegistrationDate], el.[EndDate]) <= (
                        SELECT MAX(a.[PeriodEnd])
                        FROM [HR].[tbl_AcademicHoursDistribution] a
                        WHERE a.[PeriodCode] = @PeriodCode
                   )
            LEFT JOIN [HR].[tbl_Institutions] inst   ON inst.[InstitutionID] = el.[InstitutionID]
            LEFT JOIN [HR].[ref_Types] nivelCat      ON nivelCat.[TypeID] = el.[EducationLevelTypeID]
            LEFT JOIN [HR].[ref_Types] grado         ON grado.[TypeID] = el.[SiiesGradoTypeId]
            LEFT JOIN [HR].[tbl_KnowledgeArea] ka    ON ka.[id] = el.[KnowledgeAreaId]
            WHERE @PeriodCode IS NOT NULL
              AND EXISTS (
                    SELECT 1 FROM [HR].[tbl_AcademicHoursDistribution] a2
                    WHERE a2.[IDCard] = v.[IDCard] AND a2.[PeriodCode] = @PeriodCode
                  )
        ) [x]
    ) [y]
    WHERE [Rn] = 1
);
GO

-- ============================================================
-- 8) [2026-09-16] Integración DINARDAP — historial académico real vía
--    HR.tbl_EducationLevels (analizado y aprobado por el usuario en la
--    misma sesión que el proyecto DatosDINARDAP/WsUtaDinardap.Api).
--
-- 8.1) ACADEMIC_LEVEL: corrige la incoherencia de numeración detectada en
--      vivo — NIVEL_1 tenía el SiiesLabel "TERCER NIVEL" mientras
--      NIVEL_3/NIVEL_4 estaban vacíos, sin un solo registro real usándolos
--      (confirmado: los 30 registros existentes de tbl_EducationLevels son
--      fixtures de QA, no datos de producción). Se mueve "TERCER NIVEL" a
--      NIVEL_3 y se agrega "CUARTO NIVEL" a NIVEL_4 — coherente con
--      SIIES_NIVEL (mismos nombres) y con el nivel numérico 2/3/4 que ya
--      calcula WsUtaDinardap.Api.TitulosPackageHandler.ClasificarNivel.
--      NIVEL_1 queda sin SiiesLabel (huérfano, NO se borra ni desactiva).
--      NIVEL_2 queda sin usar por ahora.
-- 8.2) Migra los 30 registros existentes (fixtures QA) a NIVEL_3/NIVEL_4
--      según su Title real, coherente con 8.1. Bajo riesgo: datos de
--      prueba, no producción real.
-- 8.3) tbl_EducationLevels: 5 columnas nuevas para capturar todo lo que
--      devuelve DINARDAP y que hoy no tiene dónde guardarse:
--      - SenescytGraduationDate / SenescytRegistrationDate: fechaGrado /
--        fechaRegistro de DINARDAP. Conceptos DISTINTOS de StartDate/
--        EndDate (fechas de inicio/fin de ESTUDIO) — confirmado en vivo
--        que pueden diferir por meses/años del fin de estudios.
--      - SenescytType: Nacional/Extranjero (campo "tipo" de DINARDAP).
--      - SenescytNivelNombreOriginal: texto crudo del nivel tal como lo
--        manda DINARDAP (ej. "Tercer Nivel Técnico Superior"), para no
--        perder trazabilidad si el clasificador se corrige después.
--      - Source: 'Manual' (default) o 'Dinardap' — gobierna el bloqueo
--        por campo en el formulario de HrFrontend.
--      - InstitutionNameOriginal: nombre libre de la institución tal como
--        lo manda DINARDAP (ej. "ARIZONA STATE UNIVERSITY") — el catálogo
--        HR.tbl_Institutions hoy solo tiene 12 filas (casi todas fixtures
--        QA, todas de Ecuador) y exige Tipo+País+Provincia+Cantón NOT
--        NULL, dato que DINARDAP no manda para universidades extranjeras.
--        Decisión del usuario 2026-09-16: InstitutionID deja de ser
--        obligatorio (8.3b) y el nombre siempre se guarda como texto,
--        se resuelva o no contra el catálogo.
-- 8.3b) InstitutionID pasa a NULLABLE (antes NOT NULL) — un título
--       sincronizado sin institución catalogada igual se guarda completo,
--       con InstitutionID en null e InstitutionNameOriginal con el
--       nombre real. La FK (FK_EducationLevels_Institution) no se toca,
--       ya admite NULL sola.
-- 8.4) Índice único FILTRADO en SenescytRegistrationNumber (solo WHERE NOT
--      NULL, porque no todos los registros manuales lo tienen) para que
--      sincronizar dos veces nunca duplique el mismo título.
--
-- Solo aditivo / idempotente. Ninguna columna ni fila existente se
-- elimina; StartDate/EndDate no se tocan.
-- ============================================================

-- 8.1) Corrección de ACADEMIC_LEVEL --------------------------------------
UPDATE [HR].[ref_Types] SET [SiiesLabel] = NULL WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_1' AND [SiiesLabel] IS NOT NULL;
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'TERCER NIVEL' WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_3';
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'CUARTO NIVEL' WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_4';
GO

-- 8.2) Migración de los 30 registros existentes (fixtures QA) ------------
DECLARE @Nivel3 INT = (SELECT [TypeID] FROM [HR].[ref_Types] WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_3');
DECLARE @Nivel4 INT = (SELECT [TypeID] FROM [HR].[ref_Types] WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_4');

UPDATE [HR].[tbl_EducationLevels]
    SET [EducationLevelTypeID] = @Nivel4
    WHERE ([Title] LIKE N'Doctorado%' OR [Title] LIKE N'Maestria%')
      AND [EducationLevelTypeID] <> @Nivel4;

UPDATE [HR].[tbl_EducationLevels]
    SET [EducationLevelTypeID] = @Nivel3
    WHERE [Title] LIKE N'Ingeniero%'
      AND [EducationLevelTypeID] <> @Nivel3;
GO

-- 8.3) Columnas nuevas en tbl_EducationLevels -----------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SenescytGraduationDate')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SenescytGraduationDate] DATE NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SenescytRegistrationDate')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SenescytRegistrationDate] DATE NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SenescytType')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SenescytType] NVARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SenescytNivelNombreOriginal')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SenescytNivelNombreOriginal] NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'Source')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [Source] NVARCHAR(20) NOT NULL CONSTRAINT [DF_EducationLevels_Source] DEFAULT ('Manual');
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_EducationLevels_Source')
    ALTER TABLE [HR].[tbl_EducationLevels]
        ADD CONSTRAINT [CK_EducationLevels_Source] CHECK ([Source] IN ('Manual', 'Dinardap'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'InstitutionNameOriginal')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [InstitutionNameOriginal] NVARCHAR(200) NULL;
GO

-- 8.3b) InstitutionID pasa a nullable (ver nota arriba) ------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'InstitutionID' AND is_nullable = 0
)
    ALTER TABLE [HR].[tbl_EducationLevels] ALTER COLUMN [InstitutionID] INT NULL;
GO

-- 8.4) Índice único filtrado en (PersonID, SenescytRegistrationNumber) ---
-- [2026-09-16, corregido tras la sincronización masiva real] Originalmente el índice era
-- solo sobre SenescytRegistrationNumber (global) - asumía que ese número es único en TODO
-- el sistema. Falso: DINARDAP devolvió el mismo número "858192970" (formato distinto al
-- típico "1010-13-1222798", parece un valor de relleno) para ~510 personas reales distintas,
-- bloqueando el INSERT de todas menos la primera con "Cannot insert duplicate key". Lo único
-- que en realidad había que evitar es duplicar el MISMO título para la MISMA persona - el
-- índice compuesto por persona es la unicidad correcta.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EducationLevels_SenescytRegistrationNumber' AND object_id = OBJECT_ID('[HR].[tbl_EducationLevels]'))
    DROP INDEX [UQ_EducationLevels_SenescytRegistrationNumber] ON [HR].[tbl_EducationLevels];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EducationLevels_Person_SenescytRegistrationNumber' AND object_id = OBJECT_ID('[HR].[tbl_EducationLevels]'))
    CREATE UNIQUE INDEX [UQ_EducationLevels_Person_SenescytRegistrationNumber]
        ON [HR].[tbl_EducationLevels] ([PersonID], [SenescytRegistrationNumber])
        WHERE [SenescytRegistrationNumber] IS NOT NULL;
GO

-- 8.5) [2026-09-16, encontrado en vivo durante la sincronización masiva real]
-- Title NVARCHAR(150) truncaba títulos reales largos (ej. maestrías con nombre
-- compuesto de más de 150 caracteres: "MASTER UNIVERSITARIO EN SISTEMAS
-- INTEGRADOS DE GESTION DE LA PREVENCION DE RIESGOS LABORALES, LA CALIDAD Y EL
-- MEDIO AMBIENTE"), tumbando el INSERT para esas personas. Se amplía a 500.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'Title' AND max_length < 1000
)
    ALTER TABLE [HR].[tbl_EducationLevels] ALTER COLUMN [Title] NVARCHAR(500) NOT NULL;
GO

-- ============================================================
-- 9) [2026-09-16] Corrección HR.vw_SiiesFormacionProfesional / fn_SiiesFormacionProfesional
--    (matriz 5.5) — verificación pedida por el usuario tras la sincronización masiva.
-- ============================================================
-- Verificado contra datos reales (HR.fn_SiiesFormacionProfesional(NULL), 3421 filas
-- antes de este fix): FECHA_OBTUVO_TITULO 100% vacío ([EndDate] nunca se llena,
-- 0/3409), NOMBRES_IES/PAIS_ESTUDIO vacíos en 3379/3409 registros sin InstitutionID
-- catalogado, y filas duplicadas por profesor (una por cada título en
-- tbl_EducationLevels) — la vista/función se habían escrito el 2026-09-11, antes de
-- las columnas DINARDAP agregadas en la sección 8 de este mismo archivo (2026-09-16),
-- y nunca se actualizaron. Ambos objetos quedaron redefinidos más arriba (secciones 7
-- y 7.1): una sola fila por profesor con el título de mayor nivel/grado, fechas
-- correctas con COALESCE(SenescytGraduationDate, EndDate), fallback de nombre de
-- institución a InstitutionNameOriginal, y corte por fecha de período cuando se pide
-- @PeriodCode explícito. PAIS_ESTUDIO y CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO
-- siguen vacíos a propósito (sin fuente real todavía, ver comentario en
-- SiiesFormacionProfesionalReportSource.cs) — no se inventa el dato.
-- No requiere cambio en SiiesFormacionProfesionalReportSource.cs: sigue llamando
-- HR.fn_SiiesFormacionProfesional(@PeriodCode) igual que antes, solo cambió el SQL.
