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
/// SIIES prohíbe mezclar CEDULA y PASAPORTE en un mismo archivo. Por eso, aunque es un solo
/// <see cref="IReportSource"/> (un solo reporte, un solo menú, un solo filtro para el usuario),
/// cada ejecución genera un único archivo correspondiente a UN tipo de identificación a la vez,
/// determinado por <see cref="ReportFilterDto.IdentType"/> ("CEDULA" o "PASAPORTE"; por defecto
/// CEDULA si no se especifica). Nunca se combinan filas de ambos tipos en el mismo
/// <see cref="ReportDefinition"/>.
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
    private const string DefaultIdentType = "CEDULA";

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

        var (codigoIes, codigoMatriz) = await GetInstitutionalParametersAsync(context.RequestAborted);
        var employees = await GetEmployeesAsync(identType, filter, context.RequestAborted);

        _logger.LogInformation(
            "SiiesFuncionariosReportSource: IdentType={IdentType}, {Count} registros.",
            identType, employees.Count);

        var rows = esPasaporte
            ? employees.Select(v =>
                {
                    var row = BuildCommonRow(v, codigoIes, codigoMatriz);
                    var (primerApellido, segundoApellido, nombres) = SplitNamesForPasaporte(v);
                    row["PRIMER_APELLIDO"] = primerApellido;
                    row["SEGUNDO_APELLIDO"] = segundoApellido;
                    row["NOMBRES"] = nombres;
                    row["FECHA_NACIMIENTO"] = v.BirthDate;
                    return (IReadOnlyDictionary<string, object?>)row;
                }).ToList()
            : employees
                .Select(v => (IReadOnlyDictionary<string, object?>)BuildCommonRow(v, codigoIes, codigoMatriz))
                .ToList();

        return new ReportDefinition
        {
            Title = esPasaporte ? "SIIES - Funcionario Pasaporte" : "SIIES - Funcionarios (Cédula)",
            FilePrefix = esPasaporte ? "SIIES_Funcionario_Pasaporte" : "SIIES_Funcionarios_Cedula",
            Subtitle = $"Total registros: {rows.Count}",
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = esPasaporte ? PasaporteColumns : CedulaColumns,
            Rows = rows,
            Orientation = PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    /// <summary>
    /// Normaliza <see cref="ReportFilterDto.IdentType"/> a "CEDULA" o "PASAPORTE".
    /// Cualquier valor nulo, vacío o distinto de "PASAPORTE" cae a CEDULA (matriz por defecto)
    /// — nunca se generan ambos tipos a la vez.
    /// </summary>
    private static string NormalizeIdentType(string? identType) =>
        string.Equals(identType?.Trim(), "PASAPORTE", StringComparison.OrdinalIgnoreCase)
            ? "PASAPORTE"
            : DefaultIdentType;

    private async Task<List<VwSiiesFuncionario>> GetEmployeesAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        var query = _db.vwSiiesFuncionarios
            .AsNoTracking()
            .Where(v => v.IdentTypeName == identTypeName);

        if (filter.IncludeInactive != true)
        {
            query = query.Where(v => v.EmployeeIsActive);

            // Employees.IsActive no siempre se actualiza cuando el régimen laboral ya venció
            // (visto en datos reales: EffectiveTo en el pasado pero IsActive todavía true).
            // RegimeIsActive=false significa que la única fila de régimen resuelta ya no está
            // vigente -> no es realmente un funcionario activo. NULL (sin régimen registrado
            // todavía) no se excluye aquí a propósito, es un hueco de datos aparte.
            query = query.Where(v => v.RegimeIsActive != false);
        }

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        return await query.ToListAsync(ct);
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
        var parts = (v.LastName ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var primerApellido = parts.Length > 0 ? parts[0] : string.Empty;
        var segundoApellido = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
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
