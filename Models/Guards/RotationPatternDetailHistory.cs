namespace WsUtaSystem.Models.Guards;

// Archivo de los días de un patrón justo antes de reemplazarlos al editar
// ("Configurar días") — no afecta RotationPatternDetail ni cómo la lee la
// generación de turnos, solo conserva la versión anterior para auditoría.
public class RotationPatternDetailHistory
{
    public int HistoryId { get; set; }
    public int PatternId { get; set; }
    public int PatternDetailId { get; set; }
    public int DayOrder { get; set; }
    public int? ScheduleId { get; set; }
    public bool IsRestDay { get; set; }
    public string? Notes { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int? ArchivedBy { get; set; }

    public virtual RotationPattern? Pattern { get; set; }
}
