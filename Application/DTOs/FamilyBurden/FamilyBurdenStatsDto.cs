namespace WsUtaSystem.Application.DTOs.FamilyBurden;

/// <summary>Contadores agregados para las tarjetas de resumen de la pantalla de validación.</summary>
public class FamilyBurdenStatsDto
{
    public int TotalCount { get; set; }
    public int RegisteredCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int DisabilityCount { get; set; }
}
