using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Data;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte SIIES Formación Profesional (matriz 5.5, Formación
/// Profesional Terminado) — una fila por título académico de un docente. No segrega
/// CEDULA/PASAPORTE (esa distinción no aplica a esta matriz). Instructivo Carga Masiva
/// CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// Requiere que el empleado tenga registro en HR.tbl_TeacherStructure (INNER JOIN en la
/// vista HR.vw_SiiesFormacionProfesional) — vacío hasta que se complete la carga masiva
/// planeada por separado; hasta entonces este reporte no devuelve filas.
/// </para>
/// <para>
/// CODIGO_IES_ESTUDIO queda siempre vacío (decisión institucional diferida — no hay campo
/// de código SIIES para instituciones externas en tbl_Institutions todavía). NOMBRES_IES se
/// llena siempre con el nombre de la institución (simplificación: el instructivo solo lo pide
/// para IES internacionales, pero al no resolver aún CODIGO_IES_ESTUDIO para las nacionales,
/// se prefiere mostrar el nombre en ambos casos antes que dejarlo vacío sin ninguna referencia).
/// </para>
/// <para>
/// CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO sale de tbl_KnowledgeArea.SiiesCode, columna
/// agregada pero sin poblar (pendiente de mapeo manual contra el anexo del instructivo) — vendrá
/// vacío en casi todos los casos hasta que se complete esa homologación.
/// </para>
/// </remarks>
public sealed class SiiesFormacionProfesionalReportSource : IReportSource
{
    private readonly AppDbContext _db;
    private readonly ILogger<SiiesFormacionProfesionalReportSource> _logger;

    private const string CuartoNivelLabel = "CUARTO NIVEL";

    public ReportType ReportType => ReportType.SiiesFormacionProfesional;

    public SiiesFormacionProfesionalReportSource(AppDbContext db, ILogger<SiiesFormacionProfesionalReportSource> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var codigoIes = await _db.Parameters.AsNoTracking()
            .Where(p => p.Name == "CODIGO_IES" && p.IsActive)
            .Select(p => p.Pvalues)
            .FirstOrDefaultAsync(context.RequestAborted) ?? string.Empty;

        var query = _db.vwSiiesFormacionProfesional.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            query = query.Where(v => v.IDCard == identification);
        }

        var titulos = await query.ToListAsync(context.RequestAborted);

        _logger.LogInformation("SiiesFormacionProfesionalReportSource: {Count} registros.", titulos.Count);

        var rows = titulos.Select(v =>
        {
            var esCuartoNivel = string.Equals(v.NivelSiiesLabel, CuartoNivelLabel, StringComparison.OrdinalIgnoreCase);

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["CODIGO_IES"] = codigoIes,
                ["TIPO_IDENTIFICACIÓN"] = v.IdentTypeName ?? string.Empty,
                ["NUMERO_IDENTIFICACION"] = v.IDCard,
                ["PAIS_ESTUDIO"] = v.InstitutionCountryId ?? string.Empty,
                ["CODIGO_IES_ESTUDIO"] = string.Empty,
                ["NOMBRES_IES"] = v.InstitutionName ?? string.Empty,
                ["NIVEL"] = v.NivelSiiesLabel ?? string.Empty,
                ["GRADO"] = esCuartoNivel ? (v.GradoSiiesLabel ?? string.Empty) : string.Empty,
                ["NOMBRE_TITULO"] = v.NombreTitulo,
                ["CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO"] = v.CampoDetalladoSiiesCode ?? string.Empty,
                ["NUMERO_REGISTRO_SENESCYT"] = v.SenescytRegistrationNumber ?? string.Empty,
                ["FECHA_OBTUVO_TITULO"] = v.FechaObtuvoTitulo,
            };
        }).ToList();

        return new ReportDefinition
        {
            Title = "SIIES - Formación Profesional (Terminado)",
            FilePrefix = "SIIES_Formacion_Profesional",
            Subtitle = $"Total registros: {rows.Count}",
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns =
            [
                new("CODIGO_IES", "CODIGO_IES"),
                new("TIPO_IDENTIFICACIÓN", "TIPO_IDENTIFICACIÓN"),
                new("NUMERO_IDENTIFICACION", "NUMERO_IDENTIFICACION"),
                new("PAIS_ESTUDIO", "PAIS_ESTUDIO"),
                new("CODIGO_IES_ESTUDIO", "CODIGO_IES_ESTUDIO"),
                new("NOMBRES_IES", "NOMBRES_IES"),
                new("NIVEL", "NIVEL"),
                new("GRADO", "GRADO"),
                new("NOMBRE_TITULO", "NOMBRE_TITULO"),
                new("CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO", "CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO"),
                new("NUMERO_REGISTRO_SENESCYT", "NUMERO_REGISTRO_SENESCYT"),
                new("FECHA_OBTUVO_TITULO", "FECHA_OBTUVO_TITULO"),
            ],
            Rows = rows,
            Orientation = PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }
}
