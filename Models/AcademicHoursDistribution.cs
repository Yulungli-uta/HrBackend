namespace WsUtaSystem.Models;

/// <summary>
/// Distributivo real de horas académicas por profesor y período académico
/// (HR.tbl_AcademicHoursDistribution). Origen: sistema "UTA Mático" (servidor
/// 10.102.12.3, ajeno a HrBackend) — se carga por lotes manuales, no hay
/// sincronización en vivo. Usado por el reporte SIIES Profesores (matriz 5.4
/// Distribución de Horas) para reemplazar el placeholder de horas en 0.
/// </summary>
public class AcademicHoursDistribution
{
    public int AcademicHoursDistributionId { get; set; }
    public string IDCard { get; set; } = null!;
    public string PeriodCode { get; set; } = null!;
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public string? ContractCode { get; set; }
    public string? ContractsInPeriod { get; set; }
    public int NumContractsInPeriod { get; set; }
    public int TotalHours { get; set; }
    public int ClassHours { get; set; }
    public int ManagementHours { get; set; }
    public int ResearchHours { get; set; }
    public int OtherActivitiesHours { get; set; }
    public int TutoringHours { get; set; }
    public int OutreachHours { get; set; }
    public int NullHours { get; set; }
    public DateTime LoadedAt { get; set; }
}
