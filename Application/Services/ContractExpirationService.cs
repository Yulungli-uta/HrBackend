using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class ContractExpirationService : IContractExpirationService
{
    private const string ContractStatusCategory = "CONTRACT_STATUS";

    private readonly AppDbContext _db;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly ILogger<ContractExpirationService> _logger;

    public ContractExpirationService(
        AppDbContext db,
        IEmployeeProvisioningClient provisioningClient,
        ILogger<ContractExpirationService> logger)
    {
        _db                = db                ?? throw new ArgumentNullException(nameof(db));
        _provisioningClient = provisioningClient ?? throw new ArgumentNullException(nameof(provisioningClient));
        _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ProcessExpiredContractsAsync(string serviceToken, CancellationToken ct = default)
    {
        var today = DateTime.Today;

        var vigentStatusId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == ContractStatusCategory && r.Name == "VIGENTE" && r.IsActive)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (vigentStatusId == 0)
        {
            _logger.LogWarning("Estado VIGENTE no encontrado en ref_Types. Proceso de contratos vencidos omitido.");
            return 0;
        }

        var vencidoStatusId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == ContractStatusCategory && r.Name == "VENCIDO" && r.IsActive)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        // Contratos VIGENTE con EndDate < hoy y sin addendum vigente con EndDate >= hoy
        var expired = await _db.Set<Contracts>()
            .AsNoTracking()
            .Where(c => c.Status == vigentStatusId && c.EndDate < today)
            .Where(c => !_db.Set<Contracts>()
                .Any(a => a.ParentID == c.ContractID && a.Status == vigentStatusId && a.EndDate >= today))
            .Select(c => new { c.ContractID, c.PersonID, c.ContractTypeID })
            .ToListAsync(ct);

        if (expired.Count == 0)
        {
            _logger.LogInformation("Sin contratos VIGENTES vencidos al {Date:yyyy-MM-dd}.", today);
            return 0;
        }

        _logger.LogInformation(
            "Contratos VIGENTES vencidos encontrados: {Count} al {Date:yyyy-MM-dd}.",
            expired.Count, today);

        // Cargar RequiresAdUserDisable para los tipos de contrato involucrados
        var typeIds = expired.Select(c => c.ContractTypeID).Distinct().ToList();
        var contractTypes = await _db.Set<ContractType>()
            .AsNoTracking()
            .Where(t => typeIds.Contains(t.ContractTypeId))
            .ToDictionaryAsync(t => t.ContractTypeId, t => t.RequiresAdUserDisable, ct);

        // Cargar mapa PersonID → EmployeeId
        var personIds = expired.Select(c => c.PersonID).Distinct().ToList();
        var employeeMap = await _db.Set<Employees>()
            .AsNoTracking()
            .Where(e => personIds.Contains(e.PersonID))
            .ToDictionaryAsync(e => e.PersonID, e => e.EmployeeId, ct);

        int processed = 0;
        foreach (var contract in expired)
        {
            try
            {
                if (vencidoStatusId > 0)
                {
                    await _db.Set<Contracts>()
                        .Where(c => c.ContractID == contract.ContractID)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(c => c.Status, vencidoStatusId), ct);

                    _logger.LogInformation(
                        "ContractID={ContractID} marcado VENCIDO.", contract.ContractID);
                }

                if (contractTypes.TryGetValue(contract.ContractTypeID, out var requiresDisable)
                    && requiresDisable)
                {
                    if (employeeMap.TryGetValue(contract.PersonID, out var employeeId) && employeeId > 0)
                    {
                        var result = await _provisioningClient.DisableAsync(employeeId, serviceToken, ct);
                        if (result?.Success == true)
                            _logger.LogInformation(
                                "Cuenta AD deshabilitada: EmployeeId={EmployeeId} (ContractID={ContractID}).",
                                employeeId, contract.ContractID);
                        else
                            _logger.LogWarning(
                                "Error al deshabilitar AD: EmployeeId={EmployeeId} (ContractID={ContractID}): {Error}",
                                employeeId, contract.ContractID,
                                result?.ErrorMessage ?? "sin respuesta de RepositoryUta");
                    }
                    else
                    {
                        _logger.LogWarning(
                            "EmployeeId no encontrado para PersonID={PersonID} (ContractID={ContractID}). Disable omitido.",
                            contract.PersonID, contract.ContractID);
                    }
                }

                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error procesando contrato vencido ContractID={ContractID}.", contract.ContractID);
            }
        }

        _logger.LogInformation(
            "Proceso de contratos vencidos finalizado: {Processed}/{Total}.", processed, expired.Count);

        return processed;
    }
}
