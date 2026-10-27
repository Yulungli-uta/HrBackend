using System.Data;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos único para el reporte SIIES Funcionarios — cubre tanto la matriz 5.7
/// (Funcionarios, TIPO_IDENTIFICACION=CEDULA) como la 5.8 (Funcionario Pasaporte,
/// TIPO_IDENTIFICACION=PASAPORTE) del Instructivo Carga Masiva CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// SIIES prohíbe mezclar CEDULA y PASAPORTE en un mismo archivo de carga. Por eso
/// <see cref="ReportFilterDto.IdentType"/> "CEDULA" o "PASAPORTE" genera un único archivo de ese tipo
/// (la matriz oficial 5.7 o 5.8). "TODOS", que es el valor por defecto cuando no se especifica,
/// junta ambos en un solo archivo con las columnas de la matriz 5.8 (las columnas propias del
/// pasaporte quedan vacías en las filas de cédula): sirve para revisión interna, no para cargar en SIIES.
/// </para>
/// <para>
/// Consulta HR.vw_SiiesFuncionarios (joins y homologación de catálogos ya resueltos en la vista)
/// y aplica aquí las reglas condicionales SIIES que no corresponden a un simple join:
/// NACIONALIDAD según ETNIA, TIPO/CATEGORIA_DOCENTE_LOSEP según TIPO_FUNCIONARIO,
/// NUMERO_CONADIS vacío si DISCAPACIDAD=NINGUNA, y separación de nombres para pasaporte.
/// </para>
/// <para>
/// Decisión documentada (actualizada 2026-08-27): INGRESO_POR_CONCURSO es obligatorio en el
/// archivo SIIES pero en BD puede estar NULL (sin clasificar todavía). Se exporta como "SI"
/// cuando no se ha clasificado (antes era "NO"); solo un false explícito exporta "NO".
/// </para>
/// </remarks>
public sealed class SiiesFuncionariosReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<SiiesFuncionariosReportSource> _logger;

    private const string DocenteLoesLabel = "DOCENTE LOES";
    private const string IndigenaLabel = "INDIGENA";
    private const string AllIdentTypes = "TODOS";
    // Sin tipo especificado se trae cédula y pasaporte juntos; cada matriz por separado se pide explícitamente
    private const string DefaultIdentType = AllIdentTypes;

    public ReportType ReportType => ReportType.SiiesFuncionarios;

    public SiiesFuncionariosReportSource(AppDbContext db, ILogger<SiiesFuncionariosReportSource> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var identType = NormalizeIdentType(filter.IdentType);
        var esPasaporte = identType == "PASAPORTE";
        var esTodos = identType == AllIdentTypes;

        var (codigoIes, codigoMatriz) = await GetInstitutionalParametersAsync(context.RequestAborted);
        var employees = await GetEmployeesAsync(identType, filter, context.RequestAborted);

        _logger.LogInformation(
            "SiiesFuncionariosReportSource: IdentType={IdentType}, {Count} registros.",
            identType, employees.Count);

        // "TODOS": un solo archivo con cédula y pasaporte y las columnas de la matriz de cédula: el nombre completo basta
        // para identificar a cada persona. PRIMER_APELLIDO, SEGUNDO_APELLIDO, NOMBRES y FECHA_NACIMIENTO solo salen en
        // PASAPORTE, que es lo que SIIES exige para cargar esa matriz. Es para revisión: SIIES pide cargarlos por separado.
        var rows = employees
            .Select(v => esPasaporte
                ? BuildPasaporteRow(v, codigoIes, codigoMatriz)
                : BuildCedulaRow(v, codigoIes, codigoMatriz))
            .ToList();

        return new ReportDefinition
        {
            Title = esTodos ? "SIIES - Funcionarios (Cédula y Pasaporte)"
                : esPasaporte ? "SIIES - Funcionario Pasaporte" : "SIIES - Funcionarios (Cédula)",
            FilePrefix = esTodos ? "SIIES_Funcionarios_Todos"
                : esPasaporte ? "SIIES_Funcionario_Pasaporte" : "SIIES_Funcionarios_Cedula",
            Subtitle = $"Total registros: {rows.Count}",
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = esPasaporte ? PasaporteColumns : CedulaColumns,
            PdfPagination = PdfPagination.Expand,
            Rows = rows,
            Orientation = PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    /// <summary>
    /// Normaliza <see cref="ReportFilterDto.IdentType"/> a "CEDULA", "PASAPORTE" o "TODOS".
    /// Cualquier valor nulo, vacío o desconocido cae a TODOS (cédula y pasaporte en un mismo archivo,
    /// para revisión: SIIES exige cargar cada matriz por separado, para eso se pide CEDULA o PASAPORTE).
    /// </summary>
    private static string NormalizeIdentType(string? identType)
    {
        var value = identType?.Trim();
        if (string.Equals(value, "PASAPORTE", StringComparison.OrdinalIgnoreCase)) return "PASAPORTE";
        if (string.Equals(value, "CEDULA", StringComparison.OrdinalIgnoreCase)) return "CEDULA";
        return DefaultIdentType;
    }

    private IReadOnlyDictionary<string, object?> BuildPasaporteRow(VwSiiesFuncionario v, string codigoIes, string codigoMatriz)
    {
        var row = BuildCommonRow(v, codigoIes, codigoMatriz);
        var (primerApellido, segundoApellido, nombres) = SplitNamesForPasaporte(v);
        row["PRIMER_APELLIDO"] = primerApellido;
        row["SEGUNDO_APELLIDO"] = segundoApellido;
        row["NOMBRES"] = nombres;
        row["FECHA_NACIMIENTO"] = v.BirthDate;
        return row;
    }

    private IReadOnlyDictionary<string, object?> BuildCedulaRow(VwSiiesFuncionario v, string codigoIes, string codigoMatriz) =>
        BuildCommonRow(v, codigoIes, codigoMatriz);

    private async Task<List<VwSiiesFuncionario>> GetEmployeesAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        // [2026-09-21] Con rango de fechas: busqueda historica real (quien estuvo vigente
        // -via regimen, contrato o accion de personal- en algun momento del rango, sin
        // importar si hoy sigue activo). HR.vw_SiiesFuncionarios no sirve para esto (ver
        // GetEmployeesVigentesEnRangoAsync) - se consulta aparte, vía SQL parametrizado.
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            return await GetEmployeesVigentesEnRangoAsync(identTypeName, filter, ct);

        // 2026-10-08: el reporte SIIES debe traer a TODOS los que laboraron en el período, estén
        // o no activos hoy -- se quitó el filtro de activo/inactivo por defecto (antes excluía
        // inactivos salvo que se marque IncludeInactive). Sin StartDate/EndDate no hay período
        // explícito, así que se trae el universo completo de la vista sin recorte por estado.
        var query = _db.vwSiiesFuncionarios.AsNoTracking();
        query = identTypeName == AllIdentTypes
            ? query.Where(v => v.IdentTypeName == "CEDULA" || v.IdentTypeName == "PASAPORTE")
                   .OrderBy(v => v.IdentTypeName).ThenBy(v => v.LastName)
            : query.Where(v => v.IdentTypeName == identTypeName);

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// [2026-09-21] Busqueda historica real para el filtro de fecha desde/hasta: encuentra
    /// empleados cuyo regimen laboral, contrato o accion de personal estuvo vigente en ALGUN
    /// momento dentro de [FechaDesde, FechaHasta], sin importar si hoy siguen activos.
    ///
    /// HR.vw_SiiesFuncionarios (usada por GetEmployeesAsync sin fechas) NO sirve para esto:
    /// su cascada (OUTER APPLY con TOP 1 ... ORDER BY prioridad) ya elige "la fila vigente de
    /// HOY" antes de que cualquier filtro externo pueda actuar - alguien inactivo hoy nunca
    /// aparecería ahí aunque haya estado vigente en el rango pedido. Por decision explicita
    /// del usuario (analisis 2026-09-21), esta consulta vive aqui como SQL parametrizado
    /// (Dapper) en vez de como un objeto nuevo en la base de datos: replica la MISMA cascada
    /// de la vista (Database/hr/15_siies_funcionarios.sql, regimen -> contrato -> accion) pero
    /// evaluando vigencia contra el rango en vez de contra GETDATE(). Un solo renglon por
    /// empleado (el mas relevante: regimen principal > mas reciente).
    ///
    /// IMPORTANTE: si se cambia la cascada de HR.vw_SiiesFuncionarios (joins, exclusiones de
    /// Status, calculo de TIPO_FUNCIONARIO), replicar el mismo cambio aqui - son dos copias
    /// de la misma logica de negocio a proposito (ver Opcion C del analisis: evita crear un
    /// objeto SQL nuevo, a costa de mantener la cascada en dos lugares).
    /// </summary>
    private async Task<List<VwSiiesFuncionario>> GetEmployeesVigentesEnRangoAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        // DateOnly como parametro de Dapper falla en runtime ("cannot be used as a parameter
        // value" - esta version de Dapper no lo soporta sin un ITypeHandler registrado, que
        // no existe en el proyecto). Se usa DateTime (solo la parte de fecha): SQL Server
        // compara DATE contra DATETIME sin problema, la comparacion sigue siendo por dia.
        var fechaDesde = filter.StartDate!.Value.Date;
        var fechaHasta = filter.EndDate!.Value.Date;
        var identification = string.IsNullOrWhiteSpace(filter.Identification) ? null : filter.Identification.Trim();

        const string sql = """
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
                -- Igual que la vista, pero DepartmentAuthorities se evalua contra el rango
                -- (StartDate/EndDate), no contra GETDATE().
                CASE
                    WHEN EXISTS (
                        SELECT 1 FROM [HR].[tbl_DepartmentAuthorities] da
                        WHERE da.[EmployeeId] = e.[EmployeeID] AND da.[IsActive] = 1
                          AND da.[StartDate] <= @FechaHasta
                          AND (da.[EndDate] IS NULL OR da.[EndDate] >= @FechaDesde)
                    ) THEN N'DIRECTIVO'
                    WHEN lrEff.[Name] = N'LOSEP' THEN N'ADMINISTRATIVO'
                    WHEN lrEff.[Name] = N'Código Trabajo' THEN N'TRABAJADOR'
                    WHEN lrEff.[Name] = N'LOES' THEN N'DOCENTE LOES'
                    ELSE NULL
                END                             AS [TipoFuncionarioSiiesLabel],
                tdl.[SiiesLabel]                AS [TipoDocenteLoesSiiesLabel],
                cdl.[SiiesLabel]                AS [CategoriaDocenteLoesSiiesLabel],
                COALESCE(apt.[ActionNumber], elr.[DocumentNumber], ctr.[ContractCode], pa.[ActionNumber], ctrFallback.[ContractCode], paFallback.[ActionNumber]) AS [DocumentNumber],
                -- 2026-10-14: igual que la vista — FECHA_INICIO = inicio del contrato/accion que respalda el
                -- regimen (ctr/pa); EffectiveFrom del regimen solo como respaldo y HireDate como ultimo recurso.
                COALESCE(CAST(apt.[ActionDate] AS DATE), CAST(ctr.[startdate] AS DATE), CAST(pa.[EffectiveDate] AS DATE), elr.[EffectiveFrom],
                         CAST(ctrFallback.[startdate] AS DATE), paFallback.[EffectiveDate], e.[HireDate])   AS [EffectiveFrom],
                COALESCE(elr.[EffectiveTo], CAST(ctrFallback.[enddate] AS DATE), paFallback.[EndDate])             AS [EffectiveTo],
                elr.[IsActive]                  AS [RegimeIsActive],
                elr.[IngresoPorConcurso],
                CASE WHEN apt.[ActionId] IS NOT NULL THEN 'PERSONNEL_ACTION' ELSE COALESCE(elr.[DocumentType],
                    CASE WHEN ctrFallback.[ContractID] IS NOT NULL THEN 'CONTRACT'
                         WHEN paFallback.[ActionID] IS NOT NULL THEN 'PERSONNEL_ACTION' END) END        AS [RegimeDocumentType],
                lrEff.[Name]                    AS [LaborRegimeName],
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
            LEFT JOIN [HR].[ref_Types] sn        ON sn.[Category] = 'DISABILITY_TYPE' AND sn.[Name] = p.[Disability]
            LEFT JOIN [HR].[tbl_Departments] d   ON d.[DepartmentID] = e.[DepartmentID]
            LEFT JOIN [HR].[tbl_jobs] j          ON j.[JobID] = e.[JobID]
            LEFT JOIN [HR].[ref_Types] tdl       ON tdl.[TypeID] = e.[TipoDocenteLoesTypeId]
            LEFT JOIN [HR].[ref_Types] cdl       ON cdl.[TypeID] = e.[CategoriaDocenteLoesTypeId]
            -- Regimen laboral: en vez de "el mas reciente activo/principal" (vista), el que se
            -- solapa con el rango, priorizando el principal y luego el mas reciente dentro de el.
            OUTER APPLY (
                SELECT TOP 1 r.*
                FROM [HR].[tbl_EmployeeLaborRegime] r
                WHERE r.[EmployeeId] = e.[EmployeeID]
                  AND r.[EffectiveFrom] <= @FechaHasta
                  AND (r.[EffectiveTo] IS NULL OR r.[EffectiveTo] >= @FechaDesde)
                ORDER BY CASE WHEN r.[IsPrincipal] = 1 THEN 0 ELSE 1 END, r.[EffectiveFrom] DESC
            ) elr
            LEFT JOIN [HR].[tbl_Contracts] ctr           ON elr.[DocumentType] = 'CONTRACT' AND ctr.[ContractID] = elr.[SourceContractId]
            LEFT JOIN [HR].[tbl_contract_type] ct        ON ct.[ContractTypeID] = ctr.[ContractTypeID]
            LEFT JOIN [HR].[ref_Types] ctRel             ON ctRel.[TypeID] = ct.[SiiesRelacionIesTypeId]
            LEFT JOIN [HR].[tbl_PersonnelActions] pa     ON elr.[DocumentType] = 'PERSONNEL_ACTION' AND pa.[ActionID] = elr.[SourcePersonnelActionId]
            LEFT JOIN [HR].[tbl_personnel_action_type] pat ON pat.[PersonnelActionTypeId] = pa.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] patRel            ON patRel.[TypeID] = pat.[SiiesRelacionIesTypeId]
            -- Contrato (fallback): igual que la vista (excluye ANULADO/BORRADOR/etc via
            -- Status), mas la condicion de solape de fechas con el rango.
            OUTER APPLY (
                SELECT TOP 1 c.*
                FROM [HR].[tbl_Contracts] c
                WHERE c.[PersonID] = p.[PersonID] AND c.[IsDeleted] = 0
                  AND c.[Status] IN (274, 276) -- VIGENTE, VENCIDO (ref_Types CONTRACT_STATUS)
                  AND c.[startdate] <= @FechaHasta AND c.[enddate] >= @FechaDesde
                  AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
                ORDER BY CASE WHEN c.[Status] = 274 THEN 0 ELSE 1 END, c.[startdate] DESC
            ) ctrFallback
            LEFT JOIN [HR].[tbl_contract_type] ctFallback ON ctFallback.[ContractTypeID] = ctrFallback.[ContractTypeID]
            LEFT JOIN [HR].[ref_Types] ctRelFallback      ON ctRelFallback.[TypeID] = ctFallback.[SiiesRelacionIesTypeId]
            -- Accion de personal (fallback): igual criterio, con solape de fechas.
            OUTER APPLY (
                SELECT TOP 1 pa2.*
                FROM [HR].[tbl_PersonnelActions] pa2
                WHERE pa2.[EmployeeID] = e.[EmployeeID] AND pa2.[IsDeleted] = 0
                  AND pa2.[Status] IN ('VIGENTE', 'FINALIZADO')
                  AND pa2.[EffectiveDate] <= @FechaHasta AND (pa2.[EndDate] IS NULL OR pa2.[EndDate] >= @FechaDesde)
                  AND elr.[DocumentNumber] IS NULL AND ctr.[ContractCode] IS NULL AND pa.[ActionNumber] IS NULL
                  AND ctrFallback.[ContractID] IS NULL
                ORDER BY CASE WHEN pa2.[Status] = 'VIGENTE' THEN 0 ELSE 1 END, pa2.[ActionDate] DESC
            ) paFallback
            LEFT JOIN [HR].[tbl_personnel_action_type] patFallback ON patFallback.[PersonnelActionTypeId] = paFallback.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] patRelFallback               ON patRelFallback.[TypeID] = patFallback.[SiiesRelacionIesTypeId]
            -- 2026-10-15: igual que la vista — documento con el que GANO el nombramiento (HR.vw_EmployeeAppointment), evaluado
            -- contra el rango: si el nombramiento actual es posterior al fin del rango se usa el primer permanente ya vigente
            -- en esa fecha (aproximacion: exacta cuando hubo a lo mas un reinicio de cadena). Sin nombramiento o con salida
            -- posterior queda la cascada anterior.
            OUTER APPLY (
                SELECT TOP 1 x.[ActionId], x.[ActionNumber], x.[ActionDate]
                FROM (
                    SELECT ap.[AppointmentActionId] AS [ActionId], ap.[AppointmentActionNumber] AS [ActionNumber], ap.[AppointmentDate] AS [ActionDate], 1 AS [Pri]
                    FROM [HR].[vw_EmployeeAppointment] ap
                    WHERE ap.[EmployeeID] = e.[EmployeeID] AND ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] <= @FechaHasta
                    UNION ALL
                    SELECT ap.[FirstPermanentActionId], ap.[FirstPermanentActionNumber], ap.[FirstPermanentDate], 2
                    FROM [HR].[vw_EmployeeAppointment] ap
                    WHERE ap.[EmployeeID] = e.[EmployeeID] AND ap.[HasLaterExit] = 0 AND ap.[AppointmentDate] > @FechaHasta
                      AND ap.[FirstPermanentDate] <= @FechaHasta
                ) x
                ORDER BY x.[Pri]
            ) apt
            LEFT JOIN [HR].[tbl_PersonnelActions] aptPa           ON aptPa.[ActionID] = apt.[ActionId]
            LEFT JOIN [HR].[tbl_personnel_action_type] aptPat     ON aptPat.[PersonnelActionTypeId] = aptPa.[ActionTypeID]
            LEFT JOIN [HR].[ref_Types] aptRel                     ON aptRel.[TypeID] = aptPat.[SiiesRelacionIesTypeId]
            -- 2026-10-14: Regimen EFECTIVO (TIPO_FUNCIONARIO / LaborRegimeName), el primero que exista:
            --   1) regimen que solapa el rango (elr)
            --   2) regimen del TIPO del contrato de respaldo (tbl_contract_type.PersonalContractTypeID: 57/58/59)
            --   3) regimen propio de la accion de respaldo (EmployeeTypeId; el tipo de accion NO se usa: el mismo
            --      tipo sale LOES y LOSEP)
            --   4) regimen ACTUAL del empleado (principal activo, o el mas reciente; acierta 98-99 % donde se puede
            --      comparar con el regimen real del periodo)
            --   5) NULL: sin documento ni regimen, no se adivina.
            -- Antes solo existia el paso 1, y tbl_EmployeeLaborRegime guarda el regimen vigente, asi que en rangos
            -- pasados TIPO_FUNCIONARIO salia vacio (617 filas en 2024).
            OUTER APPLY (
                SELECT TOP 1 x.[LaborRegimeId]
                FROM [HR].[tbl_EmployeeLaborRegime] x
                WHERE x.[EmployeeId] = e.[EmployeeID]
                ORDER BY CASE WHEN x.[IsPrincipal] = 1 AND x.[IsActive] = 1 THEN 0 ELSE 1 END, x.[EffectiveFrom] DESC
            ) curReg
            LEFT JOIN [HR].[ref_Types] lrEff ON lrEff.[TypeID] = COALESCE(
                elr.[LaborRegimeId],
                CASE WHEN ctFallback.[PersonalContractTypeID] IN (57, 58, 59) THEN ctFallback.[PersonalContractTypeID] END,
                CASE WHEN paFallback.[EmployeeTypeId] IN (57, 58, 59) THEN paFallback.[EmployeeTypeId] END,
                curReg.[LaborRegimeId])
            WHERE e.[IsDeleted] = 0
              AND ISNULL(j.[Description], '') NOT LIKE 'PROFESOR TITULAR%'
              AND NOT EXISTS (SELECT 1 FROM [HR].[tbl_TeacherStructure] ts WHERE ts.[EmployeeID] = e.[EmployeeID])
              -- 2026-10-14: igual que la vista — los profesores ocasionales/tecnicos docentes van a
              -- vw_SiiesProfesores; sin esta exclusion se colaban aqui (557-727 filas segun el rango).
              AND NOT EXISTS (SELECT 1 FROM [HR].[fn_SiiesDocenteOcasionalContract](p.[PersonID]))
              AND (it.[Name] = @IdentTypeName OR (@IdentTypeName = N'TODOS' AND it.[Name] IN (N'CEDULA', N'PASAPORTE')))
              AND (@Identification IS NULL OR p.[IDCard] = @Identification)
              -- A diferencia de la vista (que siempre devuelve 1 fila por empleado activo,
              -- vigente o no), aqui se excluye a quien no tuvo NINGUN origen de vigencia
              -- (regimen/contrato/accion) solapado con el rango - sin esto quedarian filas
              -- "vacias" (nadie estuvo vigente en esas fechas).
              -- 2026-09-21: BUG corregido - antes comparaba elr.[DocumentNumber] IS NOT NULL,
              -- pero DocumentNumber es un campo descriptivo que puede venir NULL aunque SI
              -- exista una fila de regimen que matcheo (excluia gente valida sin querer).
              -- Se usa elr.[Id] (PK real de tbl_EmployeeLaborRegime), igual criterio que ya
              -- se usaba correctamente para Contract/PersonnelAction (sus propias PK).
              AND (elr.[Id] IS NOT NULL OR ctrFallback.[ContractID] IS NOT NULL OR paFallback.[ActionID] IS NOT NULL)
            """;

        var parameters = new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            IdentTypeName = identTypeName,
            Identification = identification
        };

        var connection = _db.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(ct);
        try
        {
            // Dapper no puede mapear columnas DATE directo a propiedades DateOnly/DateOnly?
            // (InvalidCastException) - el proyecto no registra un TypeHandler para esto (ver
            // EmployeeSelfServiceSummaryRawData.cs), se mapea a un DTO con DateTime y se
            // convierte a mano, mismo criterio ya usado en el resto del codigo.
            var rawRows = await connection.QueryAsync<RawVigenteEnRangoRow>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));

            return rawRows.Select(r => new VwSiiesFuncionario
            {
                EmployeeID = r.EmployeeID,
                PersonID = r.PersonID,
                IdentType = r.IdentType,
                IdentTypeName = r.IdentTypeName,
                IDCard = r.IDCard,
                FirstName = r.FirstName,
                LastName = r.LastName,
                BirthDate = ToDateOnly(r.BirthDate),
                SexSiiesLabel = r.SexSiiesLabel,
                GenderSiiesLabel = r.GenderSiiesLabel,
                CountryId = r.CountryId,
                CountryName = r.CountryName,
                EthnicityName = r.EthnicityName,
                EthnicitySiiesLabel = r.EthnicitySiiesLabel,
                IndigenousNationalitySiiesLabel = r.IndigenousNationalitySiiesLabel,
                DisabilitySiiesLabel = r.DisabilitySiiesLabel,
                DisabilityPercentage = r.DisabilityPercentage,
                CONADISCard = r.CONADISCard,
                InstitutionalEmail = r.InstitutionalEmail,
                DepartmentName = r.DepartmentName,
                JobDescription = r.JobDescription,
                PuestoJerarquicoSuperior = r.PuestoJerarquicoSuperior,
                TipoFuncionarioSiiesLabel = r.TipoFuncionarioSiiesLabel,
                TipoDocenteLoesSiiesLabel = r.TipoDocenteLoesSiiesLabel,
                CategoriaDocenteLoesSiiesLabel = r.CategoriaDocenteLoesSiiesLabel,
                DocumentNumber = r.DocumentNumber,
                EffectiveFrom = ToDateOnly(r.EffectiveFrom),
                EffectiveTo = ToDateOnly(r.EffectiveTo),
                RegimeIsActive = r.RegimeIsActive,
                IngresoPorConcurso = r.IngresoPorConcurso,
                RegimeDocumentType = r.RegimeDocumentType,
                LaborRegimeName = r.LaborRegimeName,
                RelacionIesSiiesLabel = r.RelacionIesSiiesLabel,
                ContractedHours = r.ContractedHours,
                EmployeeIsActive = r.EmployeeIsActive,
                HireDate = ToDateOnly(r.HireDate) ?? default,
            }).ToList();
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    /// <summary>
    /// Fila cruda de <see cref="GetEmployeesVigentesEnRangoAsync"/>: mismas columnas que
    /// <see cref="VwSiiesFuncionario"/>, pero con <see cref="DateTime"/> en vez de
    /// <see cref="DateOnly"/> para que Dapper pueda mapear las columnas DATE sin lanzar
    /// InvalidCastException (ver comentario en GetEmployeesVigentesEnRangoAsync).
    /// </summary>
    private sealed class RawVigenteEnRangoRow
    {
        public int EmployeeID { get; set; }
        public int PersonID { get; set; }
        public int? IdentType { get; set; }
        public string? IdentTypeName { get; set; }
        public string IDCard { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime? BirthDate { get; set; }
        public string? SexSiiesLabel { get; set; }
        public string? GenderSiiesLabel { get; set; }
        public string? CountryId { get; set; }
        public string? CountryName { get; set; }
        public string? EthnicityName { get; set; }
        public string? EthnicitySiiesLabel { get; set; }
        public string? IndigenousNationalitySiiesLabel { get; set; }
        public string? DisabilitySiiesLabel { get; set; }
        public decimal? DisabilityPercentage { get; set; }
        public string? CONADISCard { get; set; }
        public string? InstitutionalEmail { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobDescription { get; set; }
        public bool PuestoJerarquicoSuperior { get; set; }
        public string? TipoFuncionarioSiiesLabel { get; set; }
        public string? TipoDocenteLoesSiiesLabel { get; set; }
        public string? CategoriaDocenteLoesSiiesLabel { get; set; }
        public string? DocumentNumber { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool? RegimeIsActive { get; set; }
        public bool? IngresoPorConcurso { get; set; }
        public string? RegimeDocumentType { get; set; }
        public string? LaborRegimeName { get; set; }
        public string? RelacionIesSiiesLabel { get; set; }
        public decimal? ContractedHours { get; set; }
        public bool EmployeeIsActive { get; set; }
        public DateTime HireDate { get; set; }
    }

    /// <summary>Construye las 25 columnas comunes a ambas matrices (mismo orden oficial que 5.7 Funcionarios).</summary>
    private static Dictionary<string, object?> BuildCommonRow(VwSiiesFuncionario v, string codigoIes, string codigoMatrizExtension)
    {
        var esDocenteLoes = string.Equals(v.TipoFuncionarioSiiesLabel, DocenteLoesLabel, StringComparison.OrdinalIgnoreCase);
        var esIndigena = string.Equals(v.EthnicitySiiesLabel, IndigenaLabel, StringComparison.OrdinalIgnoreCase);
        var sinDiscapacidad = string.IsNullOrEmpty(v.DisabilitySiiesLabel) || string.Equals(v.DisabilitySiiesLabel, "NINGUNA", StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, object?>
        {
            ["CODIGO_IES"] = codigoIes,
            ["CODIGO_MATRIZ_EXTENSION"] = codigoMatrizExtension,
            ["TIPO_IDENTIFICACION"] = v.IdentTypeName,
            ["IDENTIFICACION"] = v.IDCard,
            ["GENERO"] = v.GenderSiiesLabel ?? "NO DISPONE",
            ["SEXO"] = v.SexSiiesLabel ?? string.Empty,
            ["PAIS_ORIGEN"] = v.CountryName ?? v.CountryId ?? string.Empty,
            // 2026-09-02: p.Disability queda NULL cuando la persona no tiene discapacidad (nunca
            // se escribió "Ninguna" literal) — el archivo real entregado a CACES siempre trae
            // "NINGUNA" en vez de vacío para ese caso (verificado: 328 de 331 casos comparados).
            ["DISCAPACIDAD"] = v.DisabilitySiiesLabel ?? "NINGUNA",
            ["NUMERO_CONADIS"] = sinDiscapacidad ? string.Empty : (v.CONADISCard ?? "NO REGISTRA"),
            ["PORCENTAJE_DISCAPACIDAD"] = v.DisabilityPercentage ?? 0,
            ["ETNIA"] = v.EthnicitySiiesLabel ?? "NO REGISTRA",
            ["NACIONALIDAD"] = esIndigena ? (v.IndigenousNationalitySiiesLabel ?? "NO REGISTRA") : "NO APLICA",
            ["EMAIL_INSTITUCIONAL"] = v.InstitutionalEmail ?? string.Empty,
            ["NUMERO_DOCUMENTO"] = v.DocumentNumber ?? string.Empty,
            ["RELACION_IES"] = v.RelacionIesSiiesLabel ?? string.Empty,
            ["FECHA_INICIO"] = v.EffectiveFrom,
            ["FECHA_FIN"] = v.EffectiveTo,
            // Decisión de exportación 2026-08-27: NULL (sin clasificar) se exporta como "SI" —
            // solo false explícito exporta "NO". Antes era al revés; ver remarks de la clase.
            ["INGRESO_POR_CONCURSO"] = v.IngresoPorConcurso == false ? "NO" : "SI",
            ["TIPO_FUNCIONARIO"] = v.TipoFuncionarioSiiesLabel ?? string.Empty,
            ["CARGO"] = v.JobDescription ?? string.Empty,
            ["TIPO_DOCENTE_LOSEP(LOES)"] = esDocenteLoes ? (v.TipoDocenteLoesSiiesLabel ?? "NO APLICA") : "NO APLICA",
            ["CATEGORIA_DOCENTE_LOSEP(LOES)"] = esDocenteLoes ? (v.CategoriaDocenteLoesSiiesLabel ?? "NO APLICA") : "NO APLICA",
            ["UNIDAD_ACADEMICA"] = v.DepartmentName ?? string.Empty,
            ["PUESTO_JERARQUICO_SUPERIOR"] = v.PuestoJerarquicoSuperior ? "SI" : "NO",
            ["HORAS_LABORABLES_SEMANA"] = v.ContractedHours ?? 0,
            // Columnas adicionales fuera del esquema oficial CACES — se agregan al final para
            // no alterar el orden/cantidad de las columnas oficiales (uso interno/verificación,
            // no para la carga masiva al SIIES).
            ["NOMBRE_COMPLETO"] = $"{v.LastName} {v.FirstName}".Trim(),
            ["REGIMEN_LABORAL"] = v.LaborRegimeName ?? string.Empty,
        };
    }

    /// <summary>
    /// Separa Apellidos/Nombres a nivel de reporte (no se toca tbl_People). Heurística:
    /// LastName = "primer segundo" (primeras dos palabras); si solo trae una palabra,
    /// SEGUNDO_APELLIDO queda vacío. NOMBRES = FirstName completo, sin dividir.
    /// </summary>
    private static (string primerApellido, string segundoApellido, string nombres) SplitNamesForPasaporte(VwSiiesFuncionario v)
    {
        var (primerApellido, segundoApellido) = SiiesNameSplitter.Split(v.LastName);
        return (primerApellido, segundoApellido, v.FirstName ?? string.Empty);
    }

    private async Task<(string codigoIes, string codigoMatrizExtension)> GetInstitutionalParametersAsync(CancellationToken ct)
    {
        var codigoIes = await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_IES" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var codigoMatriz = await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_MATRIZ_EXTENSION" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return (codigoIes, codigoMatriz ?? string.Empty);
    }

    private static readonly IReadOnlyList<ReportColumn> CommonColumns =
    [
        new("CODIGO_IES", "CODIGO_IES"),
        new("CODIGO_MATRIZ_EXTENSION", "CODIGO_MATRIZ_EXTENSION"),
        new("TIPO_IDENTIFICACION", "TIPO_IDENTIFICACION"),
        new("IDENTIFICACION", "IDENTIFICACION"),
    ];

    private static readonly IReadOnlyList<ReportColumn> PasaporteNameColumns =
    [
        new("PRIMER_APELLIDO", "PRIMER_APELLIDO"),
        new("SEGUNDO_APELLIDO", "SEGUNDO_APELLIDO"),
        new("NOMBRES", "NOMBRES"),
    ];

    private static readonly IReadOnlyList<ReportColumn> RestOfCedulaColumns =
    [
        new("GENERO", "GENERO"),
        new("SEXO", "SEXO"),
    ];

    private static readonly IReadOnlyList<ReportColumn> FechaNacimientoColumn =
    [
        new("FECHA_NACIMIENTO", "FECHA_NACIMIENTO"),
    ];

    private static readonly IReadOnlyList<ReportColumn> TailColumns =
    [
        new("PAIS_ORIGEN", "PAIS_ORIGEN"),
        new("DISCAPACIDAD", "DISCAPACIDAD"),
        new("NUMERO_CONADIS", "NUMERO_CONADIS"),
        new("PORCENTAJE_DISCAPACIDAD", "PORCENTAJE_DISCAPACIDAD"),
        new("ETNIA", "ETNIA"),
        new("NACIONALIDAD", "NACIONALIDAD"),
        new("EMAIL_INSTITUCIONAL", "EMAIL_INSTITUCIONAL"),
        new("NUMERO_DOCUMENTO", "NUMERO_DOCUMENTO"),
        new("RELACION_IES", "RELACION_IES"),
        new("FECHA_INICIO", "FECHA_INICIO"),
        new("FECHA_FIN", "FECHA_FIN"),
        new("INGRESO_POR_CONCURSO", "INGRESO_POR_CONCURSO"),
        new("TIPO_FUNCIONARIO", "TIPO_FUNCIONARIO"),
        new("CARGO", "CARGO"),
        new("TIPO_DOCENTE_LOSEP(LOES)", "TIPO_DOCENTE_LOSEP(LOES)"),
        new("CATEGORIA_DOCENTE_LOSEP(LOES)", "CATEGORIA_DOCENTE_LOSEP(LOES)"),
        new("UNIDAD_ACADEMICA", "UNIDAD_ACADEMICA"),
        new("PUESTO_JERARQUICO_SUPERIOR", "PUESTO_JERARQUICO_SUPERIOR"),
        new("HORAS_LABORABLES_SEMANA", "HORAS_LABORABLES_SEMANA"),
        // Fuera del esquema oficial CACES — ver comentario en BuildCommonRow.
        new("NOMBRE_COMPLETO", "NOMBRE_COMPLETO"),
        new("REGIMEN_LABORAL", "REGIMEN_LABORAL"),
    ];

    /// <summary>Columnas oficiales de la matriz 5.7 Funcionarios (cédula), en el orden exacto del instructivo.</summary>
    private static IReadOnlyList<ReportColumn> CedulaColumns =>
        [.. CommonColumns, .. RestOfCedulaColumns, .. TailColumns];

    /// <summary>Columnas oficiales de la matriz 5.8 Funcionario Pasaporte, en el orden exacto del instructivo.</summary>
    private static IReadOnlyList<ReportColumn> PasaporteColumns =>
        [.. CommonColumns, .. PasaporteNameColumns, .. RestOfCedulaColumns, .. FechaNacimientoColumn, .. TailColumns];
}
