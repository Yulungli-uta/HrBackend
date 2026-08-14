namespace WsUtaSystem.Application.DTOs.FamilyBurden;

/// <summary>Motivo obligatorio al rechazar una carga familiar en la validación.</summary>
public class FamilyBurdenRejectDto
{
    public string Reason { get; set; } = null!;
}
