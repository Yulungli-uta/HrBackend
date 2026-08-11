using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IEducationLevelsService : IService<EducationLevels, int>
{
    Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId);

    /// <summary>
    /// Crea el registro y (opcionalmente) su documento de respaldo en una sola transacción SQL.
    /// Si el archivo se sube físicamente pero la transacción falla, revierte (borra) el archivo físico.
    /// </summary>
    Task<(EducationLevels entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        EducationLevels entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct);
}
