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
/// Origen de datos para el reporte SIIES Profesores — fusiona las matrices 5.2/5.3
/// (Profesores – Contratos IES, cédula/pasaporte) y 5.4 (Distribución Horas Periodo
/// Académico) en un solo reporte, igual que el archivo manual actual que la universidad
/// ya genera con estas 3 matrices combinadas en una sola hoja. Instructivo Carga Masiva
/// CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// SIIES prohíbe mezclar CEDULA y PASAPORTE en un mismo archivo. Igual que
/// <see cref="SiiesFuncionariosReportSource"/>, es un solo <see cref="IReportSource"/> que
/// segrega por <see cref="ReportFilterDto.IdentType"/> ("CEDULA" por defecto).
/// </para>
/// <para>
/// Requiere que el empleado tenga un registro en HR.tbl_TeacherStructure (la vista
/// HR.vw_SiiesProfesores ya filtra por esto). Esa tabla está vacía hasta que se complete
/// una carga masiva planeada por separado — hasta entonces este reporte no devuelve filas,
/// comportamiento esperado, no es un error.
/// </para>
/// <para>
/// Regla de HORAS (actualizada 2026-09-10): se consulta vía
/// <c>HR.fn_SiiesProfesoresHoras(@PeriodCode)</c> — no <c>HR.vw_SiiesProfesores</c> directo —
/// que agrega el distributivo real de horas académicas (<c>HR.tbl_AcademicHoursDistribution</c>,
/// cargado desde el sistema "UTA Mático", servidor 10.102.12.3, ajeno a HrBackend) por período
/// académico. <see cref="ReportFilterDto.PeriodCode"/> selecciona el período; null = el más
/// reciente disponible por profesor. Si el profesor no tiene distributivo cargado para ese
/// período (hoy: todos los "Profesor Titular", el distributivo cargado hasta ahora es de
/// personal con horas de docencia ocasional), se mantiene el fallback anterior: horas
/// contratadas del contrato vigente como aproximación de HORAS_CLASE/HORAS_CLASE_TERCER_NIVEL.
/// El resto de columnas de horas sin fuente real (nivel técnico, cuarto nivel) siguen en 0.
/// </para>
/// <para>
/// TIPO_DOCUMENTO: el sistema solo distingue internamente <c>DocumentType</c> = "CONTRACT" o
/// "PERSONNEL_ACTION" (no existe un catálogo más fino de Memorando/Convenio/Adendum/etc.).
/// Se homologa CONTRACT→CONTRATO, PERSONNEL_ACTION→ACCION PERSONAL como mejor aproximación
/// posible con la granularidad actual del dato.
/// </para>
/// </remarks>
public abstract class SiiesProfesoresReportSourceBase
{
    protected readonly AppDbContext _db;
    protected readonly ILogger _logger;

    private const string IndigenaLabel = "INDIGENA";
    private const string CuartoNivelLabel = "CUARTO NIVEL";

    protected SiiesProfesoresReportSourceBase(AppDbContext db, ILogger logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected async Task<List<VwSiiesProfesor>> GetTeachersAsync(string identTypeName, ReportFilterDto filter, CancellationToken ct)
    {
        // 2026-09-10: HR.fn_SiiesProfesoresHoras(@PeriodCode) en vez de la vista directa —
        // agrega el distributivo real de horas (HR.tbl_AcademicHoursDistribution) por período
        // académico. Toda la lógica de unión/período vive en la función SQL; aquí solo se
        // reenvía el parámetro que llega del filtro del reporte. NULL = período más reciente
        // disponible por profesor (mismo comportamiento por defecto de siempre).
        var periodCode = string.IsNullOrWhiteSpace(filter.PeriodCode) ? null : filter.PeriodCode.Trim();
        var query = _db.vwSiiesProfesores
            .FromSqlInterpolated($"SELECT * FROM HR.fn_SiiesProfesoresHoras({periodCode})")
            .AsNoTracking()
            .Where(v => v.IdentTypeName == identTypeName);

        // 2026-09-11: el filtro de "activo hoy" solo tiene sentido cuando se pide el listado
        // general (sin período). Si se pidió un período específico, lo relevante es si el
        // profesor estuvo activo EN ESE PERÍODO — y eso ya lo garantiza
        // fn_SiiesProfesoresHoras(@PeriodCode) (solo trae a quien tiene distributivo real
        // cargado ese período). Aplicar además "empleado activo hoy" excluía en silencio a
        // profesores cuyo contrato ya venció pero que sí dieron clases en el período
        // consultado — un reporte histórico no debe filtrar por el estado actual.
        if (filter.IncludeInactive != true && periodCode is null)
        {
            query = query.Where(v => v.EmployeeIsActive);

            // Ver comentario equivalente en SiiesFuncionariosReportSource: Employees.IsActive
            // no siempre refleja que el régimen laboral ya venció. NULL (sin régimen todavía)
            // no se excluye a propósito.
            query = query.Where(v => v.RegimeIsActive != false);
        }

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        return await query.ToListAsync(ct);
    }

    private static string HomologateTipoDocumento(string? regimeDocumentType) => regimeDocumentType switch
    {
        "CONTRACT" => "CONTRATO",
        "PERSONNEL_ACTION" => "ACCION PERSONAL",
        _ => string.Empty
    };

    /// <summary>Construye las columnas comunes a ambas matrices (mismo orden oficial que 5.2 Profesores – Contratos IES).</summary>
    private static Dictionary<string, object?> BuildCommonRow(VwSiiesProfesor v, string codigoIes)
    {
        var esIndigena = string.Equals(v.EthnicitySiiesLabel, IndigenaLabel, StringComparison.OrdinalIgnoreCase);

        // 2026-09-10: HORAS ahora vienen del distributivo real (HR.tbl_AcademicHoursDistribution,
        // vía HR.fn_SiiesProfesoresHoras) cuando el profesor tiene esa información cargada. Si no
        // la tiene (hoy es el caso de todos los "Profesor Titular" — el distributivo cargado hasta
        // ahora es de personal con horas de docencia ocasional, ver hallazgo de esta sesión), se
        // mantiene el fallback anterior: horas contratadas como aproximación de HORAS_CLASE.
        var horasContratadas = v.ContractedHours ?? 0;
        var horasClase = v.ClassHours ?? (int)horasContratadas;

        return new Dictionary<string, object?>
        {
            ["CODIGO_IES"] = codigoIes,
            ["TIPO_IDENTIFICACION"] = v.IdentTypeName,
            ["IDENTIFICACION"] = v.IDCard,
            ["GENERO"] = v.GenderSiiesLabel ?? "NO DISPONE",
            ["SEXO"] = v.SexSiiesLabel ?? string.Empty,
            ["PAIS_ORIGEN"] = v.CountryName ?? v.CountryId ?? string.Empty,
            // 2026-09-03: mismo criterio que SiiesFuncionariosReportSource — p.Disability queda NULL
            // cuando no hay discapacidad (nunca se escribió "Ninguna" literal), pero el archivo real
            // entregado a CACES siempre trae "NINGUNA" en vez de vacío para ese caso.
            ["DISCAPACIDAD"] = v.DisabilitySiiesLabel ?? "NINGUNA",
            ["PORCENTAJE_DISCAPACIDAD"] = v.DisabilityPercentage ?? 0,
            ["NUMERO_CONADIS"] = string.Equals(v.DisabilitySiiesLabel, "NINGUNA", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(v.DisabilitySiiesLabel)
                ? string.Empty
                : (v.CONADISCard ?? "NO REGISTRA"),
            ["ETNIA"] = v.EthnicitySiiesLabel ?? "NO REGISTRA",
            ["NACIONALIDAD"] = esIndigena ? (v.IndigenousNationalitySiiesLabel ?? "NO REGISTRA") : "NO APLICA",
            ["EMAIL_INSTITUCIONAL"] = v.InstitutionalEmail ?? string.Empty,
            ["TIPO_DOCUMENTO"] = HomologateTipoDocumento(v.RegimeDocumentType),
            ["NUMERO_DOCUMENTO"] = v.DocumentNumber ?? string.Empty,
            ["CONTRATO_RELACIONADO"] = string.Empty,
            // Decisión de exportación 2026-08-27: NULL (sin clasificar) se exporta como "SI" —
            // solo false explícito exporta "NO" (mismo criterio que SiiesFuncionariosReportSource).
            ["INGRESO_POR_CONCURSO"] = v.IngresoPorConcurso == false ? "NO" : "SI",
            ["RELACION_IES"] = v.RelacionIesSiiesLabel ?? string.Empty,
            ["TIPO_ESCALAFON_NOMBRAMIENTO"] = v.TipoEscalafonNombramientoSiiesLabel ?? "NO APLICA",
            ["CATEGORIA"] = v.CategoriaSiiesLabel ?? string.Empty,
            ["TIEMPO_DEDICACION"] = v.TiempoDedicacionSiiesLabel ?? string.Empty,
            ["FECHA_INGRESO_IES"] = v.HireDate,
            ["FECHA_INICIO"] = v.EffectiveFrom,
            ["FECHA_FIN"] = v.EffectiveTo,
            ["NIVEL"] = v.NivelSiiesLabel ?? string.Empty,
            ["UNIDAD_ACADEMICA"] = v.DepartmentName ?? string.Empty,
            ["HORAS_CLASE"] = horasClase,
            ["HORAS_TUTORIA"] = v.TutoringHours ?? 0,
            ["HORAS_ADMINISTRATIVAS"] = v.ManagementHours ?? 0,
            ["HORAS_INVESTIGACION"] = v.ResearchHours ?? 0,
            ["HORAS_VINCULACION"] = v.OutreachHours ?? 0,
            ["HORAS_OTRAS_ACTIVIDADES"] = v.OtherActivitiesHours ?? 0,
            ["HORAS_CLASE_NIVEL_TECNICO"] = 0,
            ["HORAS_CLASE_TERCER_NIVEL"] = horasClase,
            ["HORAS_CLASE_CUARTO_NIVEL"] = 0,
            // Fuera del esquema oficial CACES — se agrega al final para no alterar el orden/
            // cantidad de las columnas oficiales (uso interno/verificación, no para la carga
            // masiva al SIIES).
            ["NOMBRE_COMPLETO"] = $"{v.LastName} {v.FirstName}".Trim(),
        };
    }

    /// <summary>Separa Apellidos/Nombres a nivel de reporte (no se toca tbl_People), mismo criterio que Funcionarios.</summary>
    private static (string primerApellido, string segundoApellido, string nombres) SplitNamesForPasaporte(VwSiiesProfesor v)
    {
        var parts = (v.LastName ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var primerApellido = parts.Length > 0 ? parts[0] : string.Empty;
        var segundoApellido = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;
        return (primerApellido, segundoApellido, v.FirstName ?? string.Empty);
    }

    protected async Task<string> GetCodigoIesAsync(CancellationToken ct) =>
        await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_IES" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

    private static readonly IReadOnlyList<ReportColumn> HeadColumns =
    [
        new("CODIGO_IES", "CODIGO_IES"),
        new("TIPO_IDENTIFICACION", "TIPO_IDENTIFICACION"),
        new("IDENTIFICACION", "IDENTIFICACION"),
    ];

    private static readonly IReadOnlyList<ReportColumn> PasaporteNameColumns =
    [
        new("PRIMER_APELLIDO", "PRIMER_APELLIDO"),
        new("SEGUNDO_APELLIDO", "SEGUNDO_APELLIDO"),
        new("NOMBRES", "NOMBRES"),
    ];

    private static readonly IReadOnlyList<ReportColumn> GeneroSexoColumns =
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
        new("PORCENTAJE_DISCAPACIDAD", "PORCENTAJE_DISCAPACIDAD"),
        new("NUMERO_CONADIS", "NUMERO_CONADIS"),
        new("ETNIA", "ETNIA"),
        new("NACIONALIDAD", "NACIONALIDAD"),
        new("EMAIL_INSTITUCIONAL", "EMAIL_INSTITUCIONAL"),
        new("TIPO_DOCUMENTO", "TIPO_DOCUMENTO"),
        new("NUMERO_DOCUMENTO", "NUMERO_DOCUMENTO"),
        new("CONTRATO_RELACIONADO", "CONTRATO_RELACIONADO"),
        new("INGRESO_POR_CONCURSO", "INGRESO_POR_CONCURSO"),
        new("RELACION_IES", "RELACION_IES"),
        new("TIPO_ESCALAFON_NOMBRAMIENTO", "TIPO_ESCALAFON_NOMBRAMIENTO"),
        new("CATEGORIA", "CATEGORIA"),
        new("TIEMPO_DEDICACION", "TIEMPO_DEDICACION"),
        new("FECHA_INGRESO_IES", "FECHA_INGRESO_IES"),
        new("FECHA_INICIO", "FECHA_INICIO"),
        new("FECHA_FIN", "FECHA_FIN"),
        new("NIVEL", "NIVEL"),
        new("UNIDAD_ACADEMICA", "UNIDAD_ACADEMICA"),
        new("HORAS_CLASE", "HORAS_CLASE"),
        new("HORAS_TUTORIA", "HORAS_TUTORIA"),
        new("HORAS_ADMINISTRATIVAS", "HORAS_ADMINISTRATIVAS"),
        new("HORAS_INVESTIGACION", "HORAS_INVESTIGACION"),
        new("HORAS_VINCULACION", "HORAS_VINCULACION"),
        new("HORAS_OTRAS_ACTIVIDADES", "HORAS_OTRAS_ACTIVIDADES"),
        new("HORAS_CLASE_NIVEL_TECNICO", "HORAS_CLASE_NIVEL_TECNICO"),
        new("HORAS_CLASE_TERCER_NIVEL", "HORAS_CLASE_TERCER_NIVEL"),
        new("HORAS_CLASE_CUARTO_NIVEL", "HORAS_CLASE_CUARTO_NIVEL"),
        // Fuera del esquema oficial CACES — ver comentario en BuildCommonRow.
        new("NOMBRE_COMPLETO", "NOMBRE_COMPLETO"),
    ];

    /// <summary>Columnas oficiales de la matriz 5.2 Profesores – Contratos IES (cédula) + 5.4 fusionadas.</summary>
    protected static IReadOnlyList<ReportColumn> CedulaColumns =>
        [.. HeadColumns, .. GeneroSexoColumns, .. TailColumns];

    /// <summary>Columnas oficiales de la matriz 5.3 Profesores – Contratos IES – Pasaporte + 5.4 fusionadas.</summary>
    protected static IReadOnlyList<ReportColumn> PasaporteColumns =>
        [.. HeadColumns, .. PasaporteNameColumns, .. GeneroSexoColumns, .. FechaNacimientoColumn, .. TailColumns];

    protected static IReadOnlyDictionary<string, object?> BuildRowCedula(VwSiiesProfesor v, string codigoIes) =>
        BuildCommonRow(v, codigoIes);

    protected static IReadOnlyDictionary<string, object?> BuildRowPasaporte(VwSiiesProfesor v, string codigoIes)
    {
        var row = BuildCommonRow(v, codigoIes);
        var (primerApellido, segundoApellido, nombres) = SplitNamesForPasaporte(v);
        row["PRIMER_APELLIDO"] = primerApellido;
        row["SEGUNDO_APELLIDO"] = segundoApellido;
        row["NOMBRES"] = nombres;
        row["FECHA_NACIMIENTO"] = v.BirthDate;
        return row;
    }
}

/// <summary>Reporte SIIES Profesores — fusiona matrices 5.2/5.3 (Contratos) y 5.4 (Horas).</summary>
public sealed class SiiesProfesoresReportSource : SiiesProfesoresReportSourceBase, IReportSource
{
    public ReportType ReportType => ReportType.SiiesProfesores;

    public SiiesProfesoresReportSource(AppDbContext db, ILogger<SiiesProfesoresReportSource> logger)
        : base(db, logger) { }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var identType = string.Equals(filter.IdentType?.Trim(), "PASAPORTE", StringComparison.OrdinalIgnoreCase)
            ? "PASAPORTE"
            : "CEDULA";
        var esPasaporte = identType == "PASAPORTE";

        var codigoIes = await GetCodigoIesAsync(context.RequestAborted);
        var teachers = await GetTeachersAsync(identType, filter, context.RequestAborted);

        _logger.LogInformation("SiiesProfesoresReportSource: IdentType={IdentType}, {Count} registros.", identType, teachers.Count);

        var rows = teachers
            .Select(v => esPasaporte ? BuildRowPasaporte(v, codigoIes) : BuildRowCedula(v, codigoIes))
            .ToList();

        return new ReportDefinition
        {
            Title = esPasaporte ? "SIIES - Profesores Pasaporte" : "SIIES - Profesores (Cédula)",
            FilePrefix = esPasaporte ? "SIIES_Profesores_Pasaporte" : "SIIES_Profesores_Cedula",
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
}
