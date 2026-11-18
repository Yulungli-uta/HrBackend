using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.StoredFileVersions;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc cref="IStoredFileVersionService"/>
public sealed class StoredFileVersionService : IStoredFileVersionService
{
    private const byte StoredFileActive = 1;
    private const int ReasonMaxLength = 500;

    private readonly IStoredFileVersionRepository _repository;
    private readonly ILogger<StoredFileVersionService> _logger;

    public StoredFileVersionService(IStoredFileVersionRepository repository, ILogger<StoredFileVersionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<IReadOnlyList<StoredFileVersionDto>> GetVersionsAsync(string entityType, string entityId, string slotCode, CancellationToken ct)
        => _repository.GetVersionsAsync(entityType, entityId, slotCode, ct);

    public Task<bool> IsCurrentVersionFileAsync(int storedFileId, CancellationToken ct)
        => _repository.IsCurrentVersionFileAsync(storedFileId, ct);

    public async Task<int> AppendVersionAsync(
        string entityType,
        string entityId,
        string slotCode,
        int storedFileId,
        int? userId,
        string? reason,
        bool requireOwnership,
        Func<CancellationToken, Task>? sameTransactionWork,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("El tipo de registro es obligatorio.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("El registro es obligatorio.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(slotCode)) throw new ArgumentException("La ranura es obligatoria.", nameof(slotCode));
        if (storedFileId <= 0) throw new ArgumentException("El archivo es obligatorio.", nameof(storedFileId));
        if (reason is not null && reason.Trim().Length > ReasonMaxLength)
            throw new ArgumentException($"El motivo no puede superar {ReasonMaxLength} caracteres.", nameof(reason));

        var file = await _repository.GetStoredFileAsync(storedFileId, ct)
            ?? throw new KeyNotFoundException($"Archivo {storedFileId} no encontrado.");

        if (file.Status != StoredFileActive)
            throw new InvalidOperationException("El archivo indicado no está activo.");

        // Evita enlazar el archivo de otro registro como firmado de este.
        if (requireOwnership
            && (!string.Equals(file.EntityType, entityType, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(file.EntityId, entityId, StringComparison.Ordinal)))
            throw new InvalidOperationException("El archivo no pertenece al registro indicado.");

        var existing = await _repository.GetVersionsAsync(entityType, entityId, slotCode, ct);
        if (existing.Any(v => v.IsCurrent) && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debe ingresar el motivo del reemplazo.", nameof(reason));

        var versionNo = await _repository.AppendVersionAsync(
            entityType, entityId, slotCode, storedFileId, userId, reason, sameTransactionWork, ct);

        _logger.LogInformation(
            "StoredFileVersionService: {EntityType} {EntityId} ranura {Slot} → versión {Version} (StoredFileId={FileId}) por EmployeeId={UserId}.",
            entityType, entityId, slotCode, versionNo, storedFileId, userId);

        return versionNo;
    }
}
