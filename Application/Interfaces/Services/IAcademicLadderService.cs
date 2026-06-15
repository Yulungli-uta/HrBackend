using WsUtaSystem.Application.DTOs.AcademicLadder;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IAcademicLadderService
{
    /// <summary>Lista todos los escalafones ordenados por secuencia.</summary>
    Task<List<AcademicLadderDto>> GetAllAsync(CancellationToken ct);

    /// <summary>Devuelve un escalafón por su Id o null si no existe.</summary>
    Task<AcademicLadderDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Devuelve el escalafón al que puede postular el empleado desde el actual.</summary>
    Task<AcademicLadderDto?> GetNextAsync(int currentLadderId, CancellationToken ct);

    /// <summary>Crea un nuevo escalafón.</summary>
    Task<AcademicLadderDto> CreateAsync(AcademicLadderCreateDto dto, CancellationToken ct);

    /// <summary>Actualiza un escalafón existente.</summary>
    Task<AcademicLadderDto> UpdateAsync(int id, AcademicLadderUpdateDto dto, CancellationToken ct);
}
