using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.FamilyBurden;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IFamilyBurdenService : IService<FamilyBurden, int>
{
    Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId);

    Task<(FamilyBurden entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        FamilyBurden entity,
        IFormFile? file,
        int? documentTypeId,
        IFormFile? disabilityFile,
        int? disabilityDocumentTypeId,
        CancellationToken ct);

    /// <summary>Listado para la pantalla de validación (nombre de empleado + catálogos resueltos).</summary>
    Task<PagedResult<FamilyBurdenValidationListItemDto>> GetForValidationAsync(
        int? statusTypeId, int page, int pageSize, CancellationToken ct);

    /// <summary>Contadores agregados para las tarjetas de resumen (dato gerencial).</summary>
    Task<FamilyBurdenStatsDto> GetStatsAsync(CancellationToken ct);

    /// <summary>Aprueba una carga familiar registrada. Lanza <see cref="KeyNotFoundException"/> si no existe.</summary>
    Task ApproveAsync(int burdenId, int approvedByEmployeeId, CancellationToken ct);

    /// <summary>Rechaza una carga familiar registrada con motivo obligatorio.</summary>
    Task RejectAsync(int burdenId, int rejectedByEmployeeId, string reason, CancellationToken ct);
}
