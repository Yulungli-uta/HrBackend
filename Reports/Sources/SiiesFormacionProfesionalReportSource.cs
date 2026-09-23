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
/// Origen de datos para el reporte SIIES Formación Profesional (matriz 5.5, Formación
/// Profesional Terminado) — una fila por título académico de un docente. No segrega
/// CEDULA/PASAPORTE (esa distinción no aplica a esta matriz). Instructivo Carga Masiva
/// CACES v2S, mayo 2026.
/// </summary>
/// <remarks>
/// <para>
/// 2026-09-11: se consulta vía HR.fn_SiiesFormacionProfesional(@PeriodCode) — no la vista
/// directa — misma <see cref="ReportFilterDto.PeriodCode"/> que SiiesProfesoresReportSource;
/// null = todos los profesores (Titulares + Ocasionales, mismo criterio que
/// HR.vw_SiiesProfesores) sin importar el período. Antes exigía HR.tbl_TeacherStructure vía
/// INNER JOIN sobre tbl_EducationLevels, lo que dejaba el reporte completamente vacío (0
/// títulos cargados hoy) — ahora siempre aparece la identificación del profesor, con los
/// campos de título en blanco cuando no los tiene cargados.
/// </para>
/// <para>
/// [2026-09-18] CODIGO_IES_ESTUDIO/NOMBRES_IES/PAIS_ESTUDIO/CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO
/// ya se resuelven de verdad, siguiendo la regla textual del instructivo: CODIGO_IES_ESTUDIO
/// aplica solo para IES nacionales (viene de HR.tbl_Institutions, sembrada con el catálogo
/// oficial CACES de 378 instituciones); NOMBRES_IES aplica solo para IES internacionales
/// (antes se llenaba siempre como simplificación, ya no hace falta esa simplificación).
/// PAIS_ESTUDIO prioriza el país de estudio real (nuevo CountryOfStudyId) y cae al país de la
/// institución catalogada si no hay dato directo. CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO
/// sale de ref_Types.SIIES_UNESCO_SUBAREA (catálogo propio, 175 códigos ISCED-F del anexo del
/// instructivo) vía UnescoSubareaTypeId — nunca de tbl_KnowledgeArea (esa tabla ya se usa para
/// el selector de Publicaciones con otra codificación, mezclarlas sería un error).
/// </para>
/// <para>
/// Nota de alcance: estos 4 campos solo llegan poblados para títulos cargados manualmente con
/// esos datos (el servicio DINARDAP en vivo no informa país/código IES de estudio/subárea
/// UNESCO, solo nombre de institución y nivel) — para títulos sincronizados automáticamente
/// seguirán vacíos hasta que se carguen a mano o aparezca una fuente real para esos 3 campos.
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

        // [2026-09-21] Fecha desde/hasta: busqueda historica real (mismo requisito que
        // SiiesFuncionariosReportSource/SiiesProfesoresReportSource) - se traduce el rango a
        // los periodos academicos que se solapan y se reutiliza fn_SiiesFormacionProfesional
        // una vez por periodo, en vez de reescribir su logica de corte por fecha.
        List<VwSiiesFormacionProfesional> titulos;
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            titulos = await GetTitulosVigentesEnRangoAsync(filter, context.RequestAborted);
        }
        else
        {
            // 2026-09-11: HR.fn_SiiesFormacionProfesional(@PeriodCode) en vez de la vista directa
            // -- filtra la lista a quienes tuvieron actividad real ese período (misma lógica de
            // período que SiiesProfesoresReportSource). NULL = todos, sin filtrar.
            var periodCode = string.IsNullOrWhiteSpace(filter.PeriodCode) ? null : filter.PeriodCode.Trim();
            var query = _db.vwSiiesFormacionProfesional
                .FromSqlInterpolated($"SELECT * FROM HR.fn_SiiesFormacionProfesional({periodCode})")
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Identification))
            {
                var identification = filter.Identification.Trim();
                query = query.Where(v => v.IDCard == identification);
            }

            titulos = await query.ToListAsync(context.RequestAborted);
        }

        _logger.LogInformation("SiiesFormacionProfesionalReportSource: {Count} registros.", titulos.Count);

        var rows = titulos.Select(v =>
        {
            var esCuartoNivel = string.Equals(v.NivelSiiesLabel, CuartoNivelLabel, StringComparison.OrdinalIgnoreCase);

            return (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["CODIGO_IES"] = codigoIes,
                ["TIPO_IDENTIFICACIÓN"] = v.IdentTypeName ?? string.Empty,
                ["NUMERO_IDENTIFICACION"] = v.IDCard,
                ["PAIS_ESTUDIO"] = v.PaisEstudio ?? string.Empty,
                ["CODIGO_IES_ESTUDIO"] = v.InstitutionSiiesCode?.ToString() ?? string.Empty,
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

    /// <summary>
    /// [2026-09-21] Traduce el rango de fechas a los períodos académicos que se solapan
    /// (<see cref="SiiesAcademicPeriodHelper"/>) y reutiliza HR.fn_SiiesFormacionProfesional
    /// una vez por período que califique, en vez de reescribir su lógica de corte por fecha
    /// (hoy usa fin de período; aquí seguimos alimentándola con el período real que corresponde
    /// al rango, no con GETDATE()). Un mismo título puede aparecer en más de un período que se
    /// solapa (sigue existiendo) - se deduplica por empleado+título+fecha de obtención, no se
    /// pierden títulos distintos de la misma persona.
    /// </summary>
    private async Task<List<VwSiiesFormacionProfesional>> GetTitulosVigentesEnRangoAsync(ReportFilterDto filter, CancellationToken ct)
    {
        var periodCodes = await SiiesAcademicPeriodHelper.GetOverlappingPeriodCodesAsync(_db, filter.StartDate!.Value, filter.EndDate!.Value, ct);

        var seen = new HashSet<(int EmployeeID, string? NombreTitulo, DateOnly? FechaObtuvoTitulo)>();
        var result = new List<VwSiiesFormacionProfesional>();

        foreach (var periodCode in periodCodes)
        {
            var rows = await _db.vwSiiesFormacionProfesional
                .FromSqlInterpolated($"SELECT * FROM HR.fn_SiiesFormacionProfesional({periodCode})")
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                if (seen.Add((row.EmployeeID, row.NombreTitulo, row.FechaObtuvoTitulo)))
                    result.Add(row);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Identification))
        {
            var identification = filter.Identification.Trim();
            result = result.Where(v => v.IDCard == identification).ToList();
        }

        return result;
    }
}
