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
            .Select(a => new { a.PeriodCode, a.PeriodEnd })
            .Distinct()
            .OrderByDescending(a => a.PeriodEnd)
            .Select(a => a.PeriodCode)
            .ToListAsync(ct);
    }
}
