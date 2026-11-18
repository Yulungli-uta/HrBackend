using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Data;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// [2026-09-21] Traduce un rango de fechas (StartDate/EndDate del filtro) a los codigos de
/// periodo academico que se solapan con ese rango, para los reportes SIIES Profesores y
/// Formacion Profesional. Se prefiere esto a reescribir el cascade completo de
/// HR.vw_SiiesProfesores (mucho mas grande y complejo que el de vw_SiiesFuncionarios -
/// escalafon docente, profesores ocasionales, cadena de ParentID, horas por periodo) en SQL
/// crudo: HR.fn_SiiesProfesoresHoras(@PeriodCode)/HR.fn_SiiesFormacionProfesional(@PeriodCode)
/// YA implementan correctamente "quien trabajo en este periodo" (elegibilidad por distributivo
/// de horas cargado, NO por estado actual del contrato - confirmado leyendo ambas funciones),
/// asi que solo hace falta encontrar que periodo(s) aplican y llamarlas, en vez de duplicar
/// esa logica.
/// </summary>
internal static class SiiesAcademicPeriodHelper
{
    /// <summary>
    /// [2026-11-17] Timeout (segundos) para las consultas de HR.fn_SiiesProfesoresHoras / fn_SiiesFormacionProfesional:
    /// cada periodo tarda ~19 s con todas las columnas y con la base ocupada superaba los 30 s por defecto (TaskCanceledException / HTTP 500).
    /// </summary>
    public const int ReportCommandTimeoutSeconds = 300;

    /// <summary>
    /// [2026-11-17] SELECT * de una función de tabla con el código de período como literal (validado), no como parámetro.
    /// HR.fn_SiiesProfesoresHoras usa "@PeriodCode IS NULL OR ..."; con el período como parámetro SQL Server reutiliza un plan que tarda ~19 s
    /// (con literal ~2 s, 0,7 s sin filtros). El código solo admite letras, dígitos, guion y guion bajo: no hay inyección posible.
    /// </summary>
    public static string BuildTableFunctionSql(string functionName, string? periodCode)
    {
        if (periodCode is null) return $"SELECT * FROM {functionName}(NULL)";
        if (!System.Text.RegularExpressions.Regex.IsMatch(periodCode, "^[A-Za-z0-9_-]{1,10}$"))
            throw new ArgumentException("Código de período académico inválido.", nameof(periodCode));
        return $"SELECT * FROM {functionName}('{periodCode}')";
    }

    /// <summary>
    /// Periodos de HR.tbl_AcademicHoursDistribution cuyo [PeriodStart, PeriodEnd] se solapa con
    /// [desde, hasta], ordenados del mas reciente al mas antiguo (PeriodEnd DESC) - el orden
    /// importa para que el llamador pueda quedarse con "el periodo mas reciente que califica"
    /// simplemente tomando la primera aparicion de cada empleado/titulo al iterar en este orden.
    /// Periodos con PeriodStart/PeriodEnd NULL (dato incompleto) se excluyen: no se puede
    /// determinar solape sin fechas.
    /// </summary>
    public static async Task<List<string>> GetOverlappingPeriodCodesAsync(AppDbContext db, DateTime start, DateTime end, CancellationToken ct)
    {
        var fechaDesde = DateOnly.FromDateTime(start.Date);
        var fechaHasta = DateOnly.FromDateTime(end.Date);

        return await db.AcademicHoursDistributions
            .AsNoTracking()
            .Where(a => a.PeriodStart != null && a.PeriodEnd != null
                     && a.PeriodStart <= fechaHasta && a.PeriodEnd >= fechaDesde)
            .GroupBy(a => a.PeriodCode)
            .OrderByDescending(g => g.Max(a => a.PeriodEnd))
            .Select(g => g.Key)
            .ToListAsync(ct);
    }
}
