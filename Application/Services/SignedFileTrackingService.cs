using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.SignedFileTracking;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc cref="ISignedFileTrackingService"/>
public sealed class SignedFileTrackingService : ISignedFileTrackingService
{
    private const string ActionsScopeModule = "PERSONNEL_ACTIONS";
    private const string ContractsScopeModule = "CONTRACTS";
    private const int MaxPageSize = 100;

    private readonly ISignedFileTrackingRepository _repository;
    private readonly IUserAccessScopeService _accessScope;
    private readonly IDocumentOrchestratorService _documents;
    private readonly ILogger<SignedFileTrackingService> _logger;

    public SignedFileTrackingService(
        ISignedFileTrackingRepository repository,
        IUserAccessScopeService accessScope,
        IDocumentOrchestratorService documents,
        ILogger<SignedFileTrackingService> logger)
    {
        _repository = repository;
        _accessScope = accessScope;
        _documents = documents;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PersonnelActionSignedFileDto>> GetActionsAsync(SignedFileTrackingFilter filter, int? employeeId, CancellationToken ct)
    {
        var allowed = await ResolveScopeAsync(employeeId, ActionsScopeModule, ct);
        return await _repository.GetActionsPagedAsync(Normalize(filter, allowed), ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<ContractSignedFileDto>> GetContractsAsync(SignedFileTrackingFilter filter, int? employeeId, CancellationToken ct)
    {
        var allowed = await ResolveScopeAsync(employeeId, ContractsScopeModule, ct);
        return await _repository.GetContractsPagedAsync(Normalize(filter, allowed), ct);
    }

    /// <inheritdoc />
    public async Task<SignedFilePreviewResult> PreviewActionFileAsync(int actionId, int? employeeId, CancellationToken ct)
    {
        var allowed = await ResolveScopeAsync(employeeId, ActionsScopeModule, ct);
        var guid = await _repository.GetActionFileGuidAsync(actionId, allowed, ct);
        return await ReadFileAsync("PersonnelAction", actionId, employeeId, guid, ct);
    }

    /// <inheritdoc />
    public async Task<SignedFilePreviewResult> PreviewContractFileAsync(int contractId, int? employeeId, CancellationToken ct)
    {
        var allowed = await ResolveScopeAsync(employeeId, ContractsScopeModule, ct);
        var guid = await _repository.GetContractFileGuidAsync(contractId, allowed, ct);
        return await ReadFileAsync("Contract", contractId, employeeId, guid, ct);
    }

    // null = sin restricción de departamento (GLOBAL, sin scopes asignados o sin empleado asociado).
    private async Task<IReadOnlyList<int>?> ResolveScopeAsync(int? employeeId, string moduleCode, CancellationToken ct) =>
        employeeId.HasValue ? await _accessScope.GetAllowedDepartmentIdsAsync(employeeId.Value, moduleCode, ct) : null;

    private static SignedFileTrackingFilter Normalize(SignedFileTrackingFilter filter, IReadOnlyList<int>? allowed) =>
        filter with
        {
            Page = filter.Page < 1 ? 1 : filter.Page,
            PageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, MaxPageSize),
            AllowedDepartmentIds = allowed
        };

    private async Task<SignedFilePreviewResult> ReadFileAsync(string entity, int entityId, int? employeeId, Guid? fileGuid, CancellationToken ct)
    {
        if (fileGuid is null)
            return new SignedFilePreviewResult(SignedFilePreviewStatus.RecordNotFound);

        try
        {
            var file = await _documents.DownloadByGuidAsync(fileGuid.Value, ct);
            if (file is null)
            {
                _logger.LogWarning("Signed file missing on storage. Entity={Entity} Id={Id} FileGuid={FileGuid}", entity, entityId, fileGuid);
                return new SignedFilePreviewResult(SignedFilePreviewStatus.FileMissing);
            }

            // Trazabilidad de acceso a documentos de RRHH.
            _logger.LogInformation("Signed file previewed. Entity={Entity} Id={Id} FileGuid={FileGuid} EmployeeId={EmployeeId}",
                entity, entityId, fileGuid, employeeId);
            return new SignedFilePreviewResult(SignedFilePreviewStatus.Ok, file.Value.fileBytes, file.Value.contentType, file.Value.fileName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TimeoutException)
        {
            _logger.LogError(ex, "File store unavailable reading signed file. Entity={Entity} Id={Id} FileGuid={FileGuid}", entity, entityId, fileGuid);
            return new SignedFilePreviewResult(SignedFilePreviewStatus.StoreUnavailable);
        }
    }
}
