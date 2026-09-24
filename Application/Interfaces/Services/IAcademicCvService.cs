using WsUtaSystem.Application.DTOs.AcademicCv;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Arma los datos ya resueltos (catálogos incluidos) para la Hoja de Vida Académica.</summary>
public interface IAcademicCvService
{
    /// <summary>Retorna null si la persona no existe.</summary>
    Task<AcademicCvDto?> BuildAsync(int personId, CancellationToken ct);
}
