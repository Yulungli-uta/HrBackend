/* =====================================================================
   67_siies_profesores_doc_01_view.sql
   HR.vw_SiiesProfesores: el documento de un docente sale de su fila de régimen LOES (no de la LOSEP principal).
   Idempotente (CREATE OR ALTER). Respaldo: 67_siies_profesores_doc_99_rollback.sql (definición anterior).
   ===================================================================== */
SET NOCOUNT ON;
GO
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
        elrRel.[SiiesLabel], aptRel.[SiiesLabel], ctRel.[SiiesLabel], patRel.[SiiesLabel], ctRelFallback.[SiiesLabel], patRelFallback.[SiiesLabel],
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
    e.[IsTestData],
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
    -- 2026-11-26: este reporte (Profesores) lleva solo documentos del régimen LOES. Un docente con otro régimen principal (p. ej. un
    -- cargo administrativo LOSEP y un contrato ocasional LOES) salía con el número de documento de la fila LOSEP, el mismo que ya sale en
    -- Funcionarios. Ahora se prefiere la fila LOES (la activa y vigente primero); si no tiene ninguna, queda el criterio anterior.
    SELECT TOP 1 r.*
    FROM [HR].[tbl_EmployeeLaborRegime] r
    WHERE r.[EmployeeId] = e.[EmployeeID]
    ORDER BY CASE WHEN r.[LaborRegimeId] = 58 THEN 0 ELSE 1 END,
             CASE WHEN r.[LaborRegimeId] = 58 THEN CASE WHEN r.[IsActive] = 1 THEN 0 ELSE 1 END ELSE 0 END,
             CASE WHEN r.[LaborRegimeId] = 58 AND r.[EffectiveFrom] <= CAST(GETDATE() AS DATE) AND (r.[EffectiveTo] IS NULL OR r.[EffectiveTo] >= CAST(GETDATE() AS DATE)) THEN 0 ELSE 1 END,
             CASE WHEN r.[IsPrincipal] = 1 AND r.[IsActive] = 1 THEN 0 ELSE 1 END,
             r.[EffectiveFrom] DESC, r.[Id] DESC
) elr
-- RELACION_IES manual por régimen (tbl_EmployeeLaborRegime.SiiesRelacionIesTypeId, 35_laborregime_siies_relacion.sql): manda sobre la derivación.
LEFT JOIN [HR].[ref_Types] elrRel             ON elrRel.[TypeID] = elr.[SiiesRelacionIesTypeId]
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
