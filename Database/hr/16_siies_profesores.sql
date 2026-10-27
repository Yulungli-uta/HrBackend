-- ============================================================
-- Reporte SIIES Profesores (matrices 5.2-5.5, Instructivo CACES v2S).
-- Matriz 5.6 (Becas) fuera de alcance. Catálogos SIIES nuevos en ref_Types,
-- 3 columnas nuevas en tbl_TeacherStructure (Tipo Escalafón/Nivel/Categoría,
-- se llenan con la carga masiva aparte), SiiesGradoTypeId en EducationLevels,
-- SiiesCode en KnowledgeArea (sin poblar, requiere mapeo manual).
-- Horas por actividad: ver sección 6.1 (fn_SiiesProfesoresHoras). Integración
-- DINARDAP: ver sección 8.
-- Solo aditivo / idempotente.
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

-- Tabla 9 del instructivo (CATEGORIA, Profesores). Fuente: tbl_TeacherStructure.SiiesCategoriaTypeId;
-- si queda NULL, respaldo desde AcademicLadder.SiiesLabel (ver vw_SiiesProfesores).
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

-- ACADEMIC_LEVEL: NIVEL_1 (único valor en uso real) homologado a TERCER NIVEL.
UPDATE [HR].[ref_Types] SET [SiiesLabel] = N'TERCER NIVEL' WHERE [Category] = 'ACADEMIC_LEVEL' AND [Name] = N'NIVEL_1' AND [SiiesLabel] IS NULL;
GO

-- tbl_AcademicLadder.SiiesLabel: mapeo best-effort por patrón de Code; deja NULL lo no mapeable con certeza.
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

-- Fuente directa de CATEGORIA; sin ella, categorías sin escalafón (Ocasional/Honorario/Invitado) quedan sin clasificar.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_TeacherStructure]') AND name = 'SiiesCategoriaTypeId')
    ALTER TABLE [HR].[tbl_TeacherStructure] ADD [SiiesCategoriaTypeId] INT NULL;
GO

-- 4) HR.tbl_EducationLevels: SiiesGradoTypeId + KnowledgeAreaId --------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'SiiesGradoTypeId')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [SiiesGradoTypeId] INT NULL;
GO

-- Vínculo título -> tbl_KnowledgeArea (UNESCO); queda NULL hasta que exista pantalla/carga que lo asigne.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'KnowledgeAreaId')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [KnowledgeAreaId] INT NULL;
GO

-- 5) HR.tbl_KnowledgeArea.SiiesCode -------------------------------------------
-- Sin poblar a propósito: requiere mapeo manual contra el anexo CINE del instructivo (78 filas).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_KnowledgeArea]') AND name = 'SiiesCode')
    ALTER TABLE [HR].[tbl_KnowledgeArea] ADD [SiiesCode] NVARCHAR(20) NULL;
GO

-- 5.1) Homologación SiiesRelacionIesTypeId sobre tipos de contrato sin mapear ----------
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
    -- Sin TeacherStructure: "Laboral Actual" si es autoridad vigente (Decano/Director/etc,
    -- tiene prioridad), si no "No Aplica" (ocasional sin escalafón).
    COALESCE(
        escal.[SiiesLabel],
        CASE
            WHEN ts.[TeacherStructureID] IS NULL AND authority.[EmployeeId] IS NOT NULL
                THEN escalLaboralActualDefault.[SiiesLabel]
            WHEN ts.[TeacherStructureID] IS NULL AND ocasionalDocente.[ContractID] IS NOT NULL
                THEN escalNoAplicaDefault.[SiiesLabel]
        END
    ) AS [TipoEscalafonNombramientoSiiesLabel],
    nivel.[SiiesLabel]             AS [NivelSiiesLabel],
    -- CATEGORIA: prioridad a la columna directa; si está NULL, respaldo desde AcademicLadder;
    -- si tampoco (profesor ocasional sin TeacherStructure), respaldo a "Ocasional".
    COALESCE(catDirecta.[SiiesLabel], la.[SiiesLabel], CASE WHEN ocasionalDocente.[ContractID] IS NOT NULL THEN catOcasionalDefault.[SiiesLabel] END) AS [CategoriaSiiesLabel],
    ded.[SiiesLabel]                AS [TiempoDedicacionSiiesLabel],
    -- Fallback en cascada a ContractCode/ActionNumber cuando no hay EmployeeLaborRegime
    -- (o DocumentNumber no se copió al crearlo) -- mismo criterio que vw_SiiesFuncionarios.
    COALESCE(apt.[ActionNumber], elr.[DocumentNumber], ctr.[ContractCode], pa.[ActionNumber], ctrFallback.[ContractCode], paFallback.[ActionNumber]) AS [DocumentNumber],
    CASE WHEN apt.[ActionId] IS NOT NULL THEN 'PERSONNEL_ACTION' ELSE COALESCE(elr.[DocumentType],
        CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
             WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) END        AS [RegimeDocumentType],
    -- CAST a DATE obligatorio: mezclar date con datetime2 (tbl_Contracts) en un COALESCE
    -- rompe el mapeo EF Core DateOnly? con InvalidCastException.
    -- 2026-10-14: FECHA_INICIO = inicio del contrato/accion que respalda el regimen (ctr/pa); el
    -- EffectiveFrom del regimen queda solo como respaldo (se cargo en bloque con 2025-01-01 en 549
    -- filas) y HireDate como ultimo recurso. CAST a DATE obligatorio por el mismo motivo de arriba.
    COALESCE(CAST(apt.[ActionDate] AS DATE), CAST(ctr.[startdate] AS DATE), CAST(pa.[EffectiveDate] AS DATE), elr.[EffectiveFrom],
             CAST(ctrFallback.[startdate] AS DATE), paFallback.[EffectiveDate], e.[HireDate])   AS [EffectiveFrom],
    COALESCE(elr.[EffectiveTo], CAST(ctrFallback.[enddate] AS DATE), paFallback.[EndDate])             AS [EffectiveTo],
    elr.[IsActive]                  AS [RegimeIsActive],
    elr.[IngresoPorConcurso],
    -- Si no hay RelacionIes mapeada y el documento es Acción de Personal, se asume "Nombramiento"
    -- (solo Nombramiento/Designación/Nombramiento Provisional tienen mapeo propio a propósito).
    COALESCE(
        aptRel.[SiiesLabel], ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel],
        -- Si el contrato es un ADENDUM/PRORROGA/RENOVACIÓN sin RelacionIes propia, sube por ParentID.
        ctRelChainType.[SiiesLabel],
        CASE WHEN COALESCE(elr.[DocumentType],
                CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
                     WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) = 'PERSONNEL_ACTION'
             THEN relIesNombramientoDefault.[SiiesLabel] END
    ) AS [RelacionIesSiiesLabel],
    COALESCE(ctr.[ContractedHours], ctrFallback.[ContractedHours]) AS [ContractedHours],
    e.[IsActive]                    AS [EmployeeIsActive],
    e.[HireDate],
    -- Período académico más reciente con distributivo real (mismo criterio que fn_SiiesProfesoresHoras sin @PeriodCode).
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
-- Se une por p.Disability (texto libre) — ver comentario en vw_SiiesFuncionarios.
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
-- Profesores ocasionales (sin fila en tbl_TeacherStructure, esa tabla es solo para
-- Titulares): identificados por HR.fn_SiiesDocenteOcasionalContract, que revisa
-- HR.tbl_contract_type.IsDocenteOcasional en el contrato mismo o hasta 2 niveles de
-- padre (adendas). Mismo criterio exacto en vw_SiiesFuncionarios (exclusión).
OUTER APPLY [HR].[fn_SiiesDocenteOcasionalContract](p.[PersonID]) AS ocasionalDocente
LEFT JOIN [HR].[ref_Types] catOcasionalDefault
    ON catOcasionalDefault.[Category] = 'SIIES_CATEGORIA_DOCENTE' AND catOcasionalDefault.[Name] = N'Ocasional'
LEFT JOIN [HR].[ref_Types] escalNoAplicaDefault
    ON escalNoAplicaDefault.[Category] = 'SIIES_TIPO_ESCALAFON_NOMBRAMIENTO' AND escalNoAplicaDefault.[Name] = N'No Aplica'
-- ¿Tiene autoridad vigente (Decano/Director/Subdecano/etc.)? Ver TipoEscalafonNombramientoSiiesLabel.
OUTER APPLY (
    SELECT TOP 1 da.[EmployeeId]
    FROM [HR].[tbl_DepartmentAuthorities] da
    WHERE da.[EmployeeId] = e.[EmployeeID] AND da.[IsActive] = 1
      AND (da.[EndDate] IS NULL OR da.[EndDate] >= CAST(GETDATE() AS DATE))
) authority
LEFT JOIN [HR].[ref_Types] escalLaboralActualDefault
    ON escalLaboralActualDefault.[Category] = 'SIIES_TIPO_ESCALAFON_NOMBRAMIENTO' AND escalLaboralActualDefault.[Name] = N'Laboral Actual'
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
-- Fallback directo — mismo criterio y filtros de Status que vw_SiiesFuncionarios.
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
-- Cadena ParentID para RELACION_IES (hasta 2 niveles: adenda -> adenda -> contrato base),
-- busca el primer ancestro con SiiesRelacionIesTypeId mapeado.
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
-- 2026-10-15: SOLO TITULARES (con fila en tbl_TeacherStructure y sin contrato de docente ocasional) toman
-- NUMERO_DOCUMENTO / FECHA_INICIO / RELACION_IES del documento con el que ganaron el nombramiento
-- (HR.vw_EmployeeAppointment, regla de cadena). Los ocasionales conservan su contrato: un ocasional puede tener un
-- nombramiento antiguo de otro puesto que no es su relacion actual. Con salida posterior (HasLaterExit) tambien se
-- conserva la cascada anterior. Si el nombramiento actual es de fecha futura, se usa el primer permanente vigente.
-- 2026-10-26: join por empleado calculado UNA vez. Antes era un OUTER APPLY correlacionado que reevaluaba
-- HR.vw_EmployeeAppointment por cada fila (~2000 veces, ~20 s). Equivale al TOP 1 anterior porque esa vista
-- tiene una sola fila por empleado y las dos ramas (Pri 1 / Pri 2) son excluyentes por la fecha de hoy.
-- Si vw_EmployeeAppointment dejara de ser una fila por empleado, volver a un TOP 1 por empleado.
LEFT JOIN (
    SELECT ap.[EmployeeID], ap.[AppointmentActionId] AS [ActionId], ap.[AppointmentActionNumber] AS [ActionNumber], ap.[AppointmentDate] AS [ActionDate], 1 AS [Pri]
    FROM [HR].[vw_EmployeeAppointment] ap
    WHERE ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] <= CAST(GETDATE() AS DATE)
    UNION ALL
    SELECT ap.[EmployeeID], ap.[FirstPermanentActionId], ap.[FirstPermanentActionNumber], ap.[FirstPermanentDate], 2
    FROM [HR].[vw_EmployeeAppointment] ap
    WHERE ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] > CAST(GETDATE() AS DATE)
      AND ap.[FirstPermanentDate] <= CAST(GETDATE() AS DATE)
) apt ON apt.[EmployeeID] = e.[EmployeeID] AND ts.[TeacherStructureID] IS NOT NULL AND ocasionalDocente.[ContractID] IS NULL
LEFT JOIN [HR].[tbl_PersonnelActions] aptPa           ON aptPa.[ActionID] = apt.[ActionId]
LEFT JOIN [HR].[tbl_personnel_action_type] aptPat     ON aptPat.[PersonnelActionTypeId] = aptPa.[ActionTypeID]
LEFT JOIN [HR].[ref_Types] aptRel                     ON aptRel.[TypeID] = aptPat.[SiiesRelacionIesTypeId]
-- Default "Nombramiento" para Acción de Personal sin mapeo propio (ver RelacionIesSiiesLabel).
LEFT JOIN [HR].[ref_Types] relIesNombramientoDefault
    ON relIesNombramientoDefault.[Category] = 'SIIES_RELACION_IES' AND relIesNombramientoDefault.[Name] = N'Nombramiento'
WHERE e.[IsDeleted] = 0
  AND (
        ts.[TeacherStructureID] IS NOT NULL
     OR ocasionalDocente.[ContractID] IS NOT NULL
      );
GO

-- 6.1) Función de tabla HR.fn_SiiesProfesoresHoras (matriz 5.4, Distribución de Horas) -------
-- Horas reales de HR.tbl_AcademicHoursDistribution (cargadas desde "UTA Mático", ver
-- Database/hr/22_academic_hours_distribution.sql). Función de tabla (no vista) porque las
-- horas varían por período académico y una vista no acepta parámetros.
-- @PeriodCode NULL: todos los profesores con su período más reciente. Con valor: filtra Y
-- restringe la lista a quienes tuvieron distributivo cargado ese período específico.
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
-- LEFT JOIN desde vw_SiiesProfesores (no INNER sobre EducationLevels): aparecen TODOS los
-- profesores aunque no tengan título cargado, con esos campos en NULL (visible para RRHH).
-- Una sola fila por profesor vía ROW_NUMBER(), quedándose con el título de mayor nivel/grado
-- (ver CTE Ranked). FECHA_OBTUVO_TITULO prioriza SenescytGraduationDate (DINARDAP) sobre
-- EndDate (respaldo para registros manuales).
-- CODIGO_IES_ESTUDIO: tbl_Institutions.SiiesInstitutionCode (código oficial CACES, distinto
-- de InstitutionID que es solo la PK interna). NOMBRES_IES: en blanco para IES nacionales
-- (van solo por código), lleno para extranjeras. PAIS_ESTUDIO: nombre del país, nunca el
-- código interno. CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO: ref_Types.SIIES_UNESCO_SUBAREA
-- (catálogo propio, no tbl_KnowledgeArea que usa otra codificación).
CREATE OR ALTER VIEW [HR].[vw_SiiesFormacionProfesional] AS
WITH [Titulos] AS (
    SELECT
        v.[EmployeeID], v.[IDCard], v.[IdentTypeName],
        v.[FirstName], v.[LastName], v.[DepartmentName],
        v.[LatestPeriodCode], v.[LatestPeriodStart], v.[LatestPeriodEnd],
        el.[EducationID],
        inst.[SiiesInstitutionCode]                                AS [InstitutionSiiesCode],
        COALESCE(paisEstudio.[CountryName], paisInstitucion.[CountryName]) AS [PaisEstudio],
        -- El nombre siempre aparece (con código, viene de tbl_Institutions; sin código, nombre libre).
        COALESCE(inst.[Name], el.[InstitutionNameOriginal]) AS [InstitutionName],
        nivelCat.[Name]                                            AS [NivelName],
        nivelCat.[SiiesLabel]                                      AS [NivelSiiesLabel],
        nivelCat.[SortOrder]                                       AS [NivelSortOrder],
        grado.[Name]                                               AS [GradoName],
        grado.[SiiesLabel]                                         AS [GradoSiiesLabel],
        grado.[SortOrder]                                          AS [GradoSortOrder],
        el.[Title]                                                 AS [NombreTitulo],
        unesco.[Name]                                              AS [CampoDetalladoSiiesCode],
        el.[SenescytRegistrationNumber],
        COALESCE(el.[SenescytGraduationDate], el.[SenescytRegistrationDate], el.[EndDate])  AS [FechaObtuvoTitulo]
    FROM [HR].[vw_SiiesProfesores] v
    LEFT JOIN [HR].[tbl_EducationLevels] el  ON el.[PersonID] = v.[PersonID]
    LEFT JOIN [HR].[tbl_Institutions] inst   ON inst.[InstitutionID] = el.[InstitutionID]
    LEFT JOIN [HR].[tbl_Countries] paisEstudio ON paisEstudio.[CountryID] = el.[CountryOfStudyId]
    LEFT JOIN [HR].[tbl_Countries] paisInstitucion ON paisInstitucion.[CountryID] = inst.[CountryID]
    LEFT JOIN [HR].[ref_Types] nivelCat      ON nivelCat.[TypeID] = el.[EducationLevelTypeID]
    LEFT JOIN [HR].[ref_Types] grado         ON grado.[TypeID] = el.[SiiesGradoTypeId]
    LEFT JOIN [HR].[ref_Types] unesco        ON unesco.[TypeID] = el.[UnescoSubareaTypeId]
),
[Ranked] AS (
    -- Jerarquía vía ref_Types.SortOrder (mayor = más alto); NULL queda al final por diseño de ORDER BY DESC.
    SELECT *,
        ROW_NUMBER() OVER (
            PARTITION BY [EmployeeID]
            ORDER BY
                [NivelSortOrder] DESC,
                [GradoSortOrder] DESC,
                [FechaObtuvoTitulo] DESC,
                [EducationID] DESC
        ) AS [Rn]
    FROM [Titulos]
)
SELECT
    [EmployeeID], [IDCard], [IdentTypeName],
    [FirstName], [LastName], [DepartmentName],
    [InstitutionSiiesCode], [PaisEstudio], [InstitutionName],
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
        [FirstName], [LastName], [DepartmentName],
        [InstitutionSiiesCode], [PaisEstudio], [InstitutionName],
        [NivelSiiesLabel], [GradoSiiesLabel], [NombreTitulo],
        [CampoDetalladoSiiesCode], [SenescytRegistrationNumber], [FechaObtuvoTitulo],
        [LatestPeriodCode], [LatestPeriodStart], [LatestPeriodEnd]
    FROM (
        SELECT *,
            ROW_NUMBER() OVER (
                PARTITION BY [EmployeeID]
                ORDER BY
                    [NivelSortOrder] DESC,
                    [GradoSortOrder] DESC,
                    [FechaObtuvoTitulo] DESC,
                    [EducationID] DESC
            ) AS [Rn]
        FROM (
            SELECT
                v.[EmployeeID], v.[IDCard], v.[IdentTypeName],
                v.[FirstName], v.[LastName], v.[DepartmentName],
                v.[LatestPeriodCode], v.[LatestPeriodStart], v.[LatestPeriodEnd],
                el.[EducationID],
                inst.[SiiesInstitutionCode]                                AS [InstitutionSiiesCode],
                COALESCE(paisEstudio.[CountryName], paisInstitucion.[CountryName]) AS [PaisEstudio],
                -- 2026-09-21: mismo cambio que en vw_SiiesFormacionProfesional (ver ese
                -- comentario) - el nombre debe aparecer siempre, con o sin codigo.
                COALESCE(inst.[Name], el.[InstitutionNameOriginal]) AS [InstitutionName],
                nivelCat.[Name]                                            AS [NivelName],
                nivelCat.[SiiesLabel]                                      AS [NivelSiiesLabel],
                nivelCat.[SortOrder]                                       AS [NivelSortOrder],
                grado.[Name]                                               AS [GradoName],
                grado.[SiiesLabel]                                         AS [GradoSiiesLabel],
                grado.[SortOrder]                                          AS [GradoSortOrder],
                el.[Title]                                                 AS [NombreTitulo],
                unesco.[Name]                                              AS [CampoDetalladoSiiesCode],
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
            LEFT JOIN [HR].[tbl_Countries] paisEstudio ON paisEstudio.[CountryID] = el.[CountryOfStudyId]
            LEFT JOIN [HR].[tbl_Countries] paisInstitucion ON paisInstitucion.[CountryID] = inst.[CountryID]
            LEFT JOIN [HR].[ref_Types] nivelCat      ON nivelCat.[TypeID] = el.[EducationLevelTypeID]
            LEFT JOIN [HR].[ref_Types] grado         ON grado.[TypeID] = el.[SiiesGradoTypeId]
            LEFT JOIN [HR].[ref_Types] unesco        ON unesco.[TypeID] = el.[UnescoSubareaTypeId]
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
-- 8) Integración DINARDAP — historial académico real vía HR.tbl_EducationLevels.
-- 8.1/8.2: homologa ACADEMIC_LEVEL (NIVEL_3=TERCER NIVEL, NIVEL_4=CUARTO NIVEL,
--   coherente con WsUtaDinardap.Api.TitulosPackageHandler.ClasificarNivel) y migra
--   los 30 registros existentes (fixtures QA) al nivel correcto.
-- 8.3: columnas nuevas para lo que DINARDAP devuelve y no tenía dónde guardarse
--   (fechas de grado/registro, tipo, nivel original crudo, Source Manual/Dinardap,
--   nombre de institución en texto libre). 8.3b: InstitutionID pasa a nullable (un
--   título sin institución catalogada igual se guarda completo).
-- 8.4: índice único FILTRADO por persona en SenescytRegistrationNumber (no global:
--   DINARDAP devolvió el mismo número para ~510 personas distintas, un valor de
--   relleno) — evita duplicar el mismo título sin bloquear sincronizaciones repetidas.
-- Solo aditivo / idempotente.
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
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EducationLevels_SenescytRegistrationNumber' AND object_id = OBJECT_ID('[HR].[tbl_EducationLevels]'))
    DROP INDEX [UQ_EducationLevels_SenescytRegistrationNumber] ON [HR].[tbl_EducationLevels];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_EducationLevels_Person_SenescytRegistrationNumber' AND object_id = OBJECT_ID('[HR].[tbl_EducationLevels]'))
    CREATE UNIQUE INDEX [UQ_EducationLevels_Person_SenescytRegistrationNumber]
        ON [HR].[tbl_EducationLevels] ([PersonID], [SenescytRegistrationNumber])
        WHERE [SenescytRegistrationNumber] IS NOT NULL;
GO

-- 8.5) Title ampliado a 500 (150 truncaba títulos largos reales, ej. maestrías con nombre compuesto).
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'Title' AND max_length < 1000
)
    ALTER TABLE [HR].[tbl_EducationLevels] ALTER COLUMN [Title] NVARCHAR(500) NOT NULL;
GO

-- ============================================================
-- 9) Corrección de vw_SiiesFormacionProfesional/fn_SiiesFormacionProfesional ya
--    aplicada arriba (secciones 7/7.1) tras agregar las columnas DINARDAP (sección 8).
-- ============================================================

-- ============================================================
-- 10) Backfill de Formación Académica para 31 profesores sin fila en tbl_Employees
--     (solo existen en tbl_People) -- DinardapSenescytSyncJob solo sincroniza
--     empleados activos, así que nunca los procesa. Cargados a mano desde el archivo
--     oficial, Source='Manual'.
-- ============================================================
-- Columnas nuevas que el archivo trae y tbl_EducationLevels no tenía dónde guardar:
--   - CountryOfStudy: país de estudio, texto crudo (mismo patrón que InstitutionNameOriginal).
--   - UnescoSubareaCodeOriginal: código SIIES exclusivo (no tiene relación con
--     tbl_KnowledgeArea, que usa otra codificación tipo ISCED).
--   - InstitutionSiiesCodeTypeId: resuelto contra ref_Types 'SIIES_INSTITUTION_CODE'
--     (Name=código, SiiesLabel=nombre IES); solo aplica a instituciones ecuatorianas.
--
-- Verificado end-to-end: 31 filas insertadas, EducationLevelTypeID NIVEL_4, SiiesGradoTypeId
-- resuelto por texto de GRADO (2192 Maestría / 2191 Doctor Ph.D / 2196 Especialista
-- Área Salud), InstitutionSiiesCodeTypeId resuelto en 18/31 (instituciones
-- ecuatorianas), NULL en las 13 extranjeras restantes -- consistente con que el
-- propio archivo oficial tampoco trae ese código para instituciones extranjeras.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'CountryOfStudy')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [CountryOfStudy] NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'UnescoSubareaCodeOriginal')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [UnescoSubareaCodeOriginal] NVARCHAR(20) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[HR].[tbl_EducationLevels]') AND name = 'InstitutionSiiesCodeTypeId')
    ALTER TABLE [HR].[tbl_EducationLevels] ADD [InstitutionSiiesCodeTypeId] INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM [HR].[ref_Types] WHERE [Category] = 'SIIES_INSTITUTION_CODE')
BEGIN
    INSERT INTO [HR].[ref_Types] (Category, Name, SiiesLabel, IsActive, CreatedAt) VALUES
        ('SIIES_INSTITUTION_CODE', N'1010', N'UNIVERSIDAD TECNICA DE AMBATO', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1042', N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1051', N'UNIVERSIDAD TECNOLOGICA ISRAEL', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1031', N'UNIVERSIDAD TECNICA PARTICULAR DE LOJA', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1027', N'PONTIFICIA UNIVERSIDAD CATOLICA DEL ECUADOR', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1005', N'UNIVERSIDAD CENTRAL DEL ECUADOR', 1, GETDATE()),
        ('SIIES_INSTITUTION_CODE', N'1006', N'UNIVERSIDAD DE GUAYAQUIL', 1, GETDATE());
END
GO

-- Las 31 filas, resueltas por cédula contra el archivo oficial (PersonID literal,
-- no se resuelve por cédula en este script porque son personas sin fila de
-- Employee -- no hay forma segura de re-derivar el PersonID por otro criterio):
INSERT INTO [HR].[tbl_EducationLevels]
  ([PersonID],[EducationLevelTypeID],[Title],[SenescytRegistrationNumber],[SiiesGradoTypeId],
   [SenescytGraduationDate],[InstitutionNameOriginal],[CountryOfStudy],[UnescoSubareaCodeOriginal],
   [InstitutionSiiesCodeTypeId],[Source],[CreatedAt])
VALUES
(185127, 2022, N'MAGISTER EN REDES Y TELECOMUNICACIONES', N'1010-09-695656', 2192, '2008-12-11', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'81-16A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(231005, 2022, N'MAGISTER EJECUTIVO EN DIRECCION DE EMPRESAS CON ENFASIS EN GERENCIA ESTRATEGICA', N'1042-07-662878', 2192, '2007-05-18', N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES', N'ECUADOR', N'3-14A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1042'), N'Manual', GETDATE()),
(184253, 2022, N'MAGISTER EN GESTION DE LA PRODUCCION AGROINDUSTRIAL', N'1010-14-86048569', 2192, '2014-05-27', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'3-14A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(179870, 2022, N'MAGISTER EN SISTEMAS INFORMATICOS EDUCATIVOS', N'1051-10-704455', 2192, '2009-07-25', N'UNIVERSIDAD TECNOLOGICA ISRAEL', N'ECUADOR', N'2-12A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1051'), N'Manual', GETDATE()),
(171585, 2022, N'MAGISTER EN DOCENCIA Y CURRICULO PARA LA EDUCACION SUPERIOR', N'1010-10-706227', 2192, '2009-12-10', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'2-12A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(166382, 2022, N'MAGISTER EN TECNOLOGIA DE LA INFORMACION Y MULTIMEDIA EDUCATIVA', N'1010-06-655698', 2192, '2006-03-17', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'1-16A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(233493, 2022, N'MAGISTER EN DOCENCIA Y CURRICULO PARA LA EDUCACION SUPERIOR', N'1010-11-723146', 2192, '2010-06-03', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'1-11A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(182458, 2022, N'DOCTORA PROGRAMA DE FORMACION INICIAL Y PERMANENTE DE PROFESIONALES DE LA EDUCACION E INNOVACION EDUCATIVA', N'7201 R-15-24837', 2191, '2015-05-27', N'UNIVERSIDAD COMPLUTENSE DE MADRID', N'ESPAÑA', N'2-11A', NULL, N'Manual', GETDATE()),
(191811, 2022, N'DOCTORA DENTRO DEL PROGRAMA DE FORMACION INICIAL Y PERMANENTE DE PROFESIONALES DE LA EDUCACION E INNOVACION EDUCATIVA', N'72419601', 2191, '2016-04-01', N'UNIVERSIDAD COMPLUTENSE DE MADRID', N'ESPAÑA', N'2-11A', NULL, N'Manual', GETDATE()),
(208812, 2022, N'DOCTOR EN HUMANIDADES Y ARTES CON MENCION EN CIENCIAS DE LA EDUCACION', N'0321238388', 2191, '2024-08-06', N'UNIVERSIDAD NACIONAL DE ROSARIO', N'ARGENTINA', N'1-11A', NULL, N'Manual', GETDATE()),
(205300, 2022, N'ESPECIALISTA DE PRIMER GRADO EN INMUNOLOGIA', N'CU-12-2922', 2196, '2012-07-23', N'INSTITUTO SUPERIOR DE CIENCIAS MEDICAS DE LA HABANA', N'CUBA', N'1-9A', NULL, N'Manual', GETDATE()),
(235528, 2022, N'MAGISTER EN GERENCIA DE SALUD PARA EL DESARROLLO LOCAL', N'1031-14-86047031', 2192, '2014-03-26', N'UNIVERSIDAD TECNICA PARTICULAR DE LOJA', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1031'), N'Manual', GETDATE()),
(227453, 2022, N'ESPECIALISTA DE PRIMER GRADO EN HISTOLOGIA', N'CU-13-4793', 2196, '2013-06-14', N'INSTITUTO SUPERIOR DE CIENCIAS MEDICAS DE LA HABANA', N'CUBA', N'1-9A', NULL, N'Manual', GETDATE()),
(184225, 2022, N'MAGISTER EN SALUD PUBLICA', N'1042-10-719356', 2192, '2010-10-20', N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1042'), N'Manual', GETDATE()),
(197142, 2022, N'MASTER UNIVERSITARIO EN PSICOLOGIA DE LA SALUD', N'7683R-13-10766', 2192, '2013-07-04', N'UNIVERSIDAD DE MALAGA', N'ESPAÑA', N'1-9A', NULL, N'Manual', GETDATE()),
(224309, 2022, N'MAGISTER EN GESTION DE LOS SERVICIOS HOSPITALARIOS', N'1042-10-710230', 2192, '2010-03-24', N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1042'), N'Manual', GETDATE()),
(235873, 2022, N'MAGISTER EN GERENCIA DE INSTITUCIONES DE SALUD', N'1010-15-86062632', 2192, '2015-06-02', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(224220, 2022, N'MAGISTER EN GESTION DE LOS SERVICIOS HOSPITALARIOS', N'1042-10-710237', 2192, '2010-03-24', N'UNIVERSIDAD REGIONAL AUTONOMA DE LOS ANDES', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1042'), N'Manual', GETDATE()),
(189280, 2022, N'DOCTOR DENTRO DEL PROGRAMA DE DOCTORADO EN TECNOLOGIAS DE LA INFORMACION Y LAS COMUNICACIÓN', N'7241187989', 2191, '2021-12-01', N'UNIVERSIDAD REY JUAN CARLOS ', N'ESPAÑA', N'1-6A', NULL, N'Manual', GETDATE()),
(197237, 2022, N'MAGISTER EN BIOETICA', N'1010-2024-3009416', 2192, '2024-11-17', N'UNIVERSIDAD TECNICA DE AMBATO', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1010'), N'Manual', GETDATE()),
(161867, 2022, N'MAGISTER EN TERAPIA MANUAL ORTOPEDICA', N'4202-15-44915', 2192, '2014-09-09', N'UNIVERSIDAD ANDRES BELLO', N'CHILE', N'1-9A', NULL, N'Manual', GETDATE()),
(165961, 2022, N'MAGISTER EN PREVENCION Y ASISTENCIA DE LAS DROGADEPENDENCIAS', N'1005R-09-4650', 2192, '2007-05-17', N'UNIVERSIDAD DEL SALVADOR BUENOS AIRES', N'ARGENTINA', N'1-9A', NULL, N'Manual', GETDATE()),
(216863, 2022, N'DOCTORA DENTRO DEL PROGRAMA DE DOCTORADO EN BIOMEDICINA', N'7241214161', 2191, '2023-06-23', N'UNIVERSIDAD DE BARCELONA ', N'ESPAÑA', N'1-9A', NULL, N'Manual', GETDATE()),
(192095, 2022, N'DOCTOR EN CIENCIAS DE LA SALUD', N'8622248978', 2191, '2025-03-24', N'UNIVERSIDAD DE ZULIA ', N'VENEZUELA', N'1-9A', NULL, N'Manual', GETDATE()),
(207198, 2022, N'MAGISTER EN GERENCIA DE SALUD PARA EL DESARROLLO LOCAL', N'1031-13-86034830', 2192, '2013-03-22', N'UNIVERSIDAD TECNICA PARTICULAR DE LOJA', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1031'), N'Manual', GETDATE()),
(161868, 2022, N'MAGISTER EN PSICOLOGIA CLINICA MENCIÓN EN PSICOTERAPIA INFANTIL Y DE ADOLESCENTES', N'1027-2021-2392352', 2192, '2021-10-07', N'PONTIFICIA UNIVERSIDAD CATOLICA DEL ECUADOR', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1027'), N'Manual', GETDATE()),
(211578, 2022, N'MASTER EN ENDOCRINOLOGIA AVANZADA', N'10455 R-15-53035', 2192, '2015-09-22', N'UNIVERSIDAD DE ALCALA', N'ESPAÑA', N'1-9A', NULL, N'Manual', GETDATE()),
(197416, 2022, N'ESPECIALISTA EN PEDIATRIA', N'1005-04-551335', 2196, '2004-12-10', N'UNIVERSIDAD CENTRAL DEL ECUADOR', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1005'), N'Manual', GETDATE()),
(207860, 2022, N'MASTER UNIVERSITARIO EN MICROBIOLOGIA AVANZADA ESPECIALIDAD EN MICROBIOLOGIA SANITARIA', N'724184849', 2192, '2016-07-25', N'UNIVERSITAT DE BARCELONA', N'ESPAÑA', N'1-9A', NULL, N'Manual', GETDATE()),
(217121, 2022, N'ESPECIALISTA EN MEDICINA INTERNA', N'1006-14-5001', 2196, '2014-06-22', N'UNIVERSIDAD DE GUAYAQUIL', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1006'), N'Manual', GETDATE()),
(186002, 2022, N'ESPECIALISTA EN CIRUGIA GENERAL', N'1005-08-678557', 2196, '2008-07-21', N'UNIVERSIDAD CENTRAL DEL ECUADOR', N'ECUADOR', N'1-9A', (SELECT TypeID FROM [HR].[ref_Types] WHERE Category='SIIES_INSTITUTION_CODE' AND Name=N'1005'), N'Manual', GETDATE());
GO
