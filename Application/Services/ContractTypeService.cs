using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.ContractType;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class ContractTypeService : Service<ContractType, int>, IContractTypeService
{
    private readonly IContractTypeRepository _repo;
    private readonly AppDbContext _db;

    public ContractTypeService(IContractTypeRepository repo, AppDbContext db) : base(repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _db   = db   ?? throw new ArgumentNullException(nameof(db));
    }

    public Task SetDefaultTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default)
        => _repo.SetDefaultTemplateAsync(contractTypeId, templateId, ct);

    public Task SetDelegationTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default)
        => _repo.SetDelegationTemplateAsync(contractTypeId, templateId, ct);

    public async Task<ContractTypeWithTemplateDto?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default)
    {
        var ct2 = await _repo.GetWithDefaultTemplateAsync(contractTypeId, ct);
        if (ct2 is null) return null;

        async Task<(string? Name, string? Code, string? Version)> LoadTemplateAsync(int? templateId)
        {
            if (!templateId.HasValue) return (null, null, null);

            var tpl = await _db.Set<DocumentTemplate>()
                .AsNoTracking()
                .Where(t => t.TemplateId == templateId.Value)
                .Select(t => new { t.Name, t.TemplateCode, t.Version })
                .FirstOrDefaultAsync(ct);

            return (tpl?.Name, tpl?.TemplateCode, tpl?.Version);
        }

        var (defaultName, defaultCode, defaultVersion) = await LoadTemplateAsync(ct2.DefaultTemplateId);
        var (delegationName, delegationCode, delegationVersion) = await LoadTemplateAsync(ct2.DelegationTemplateId);

        return new ContractTypeWithTemplateDto(
            ct2.ContractTypeId,
            ct2.Name,
            ct2.Description,
            ct2.Status,
            ct2.ContractCode,
            ct2.DocumentTemplateTypeId,
            ct2.DefaultTemplateId,
            defaultName,
            defaultCode,
            defaultVersion,
            ct2.DelegationTemplateId,
            delegationName,
            delegationCode,
            delegationVersion,
            ct2.NumberingPrefix,
            ct2.NumberingYear,
            ct2.NumberingLastSequence,
            ct2.RequiresAdUserCreation,
            ct2.RequiresAdUserDisable,
            ct2.RequiresAdGroupAssignment
        );
    }

    public async Task<ContractNextNumberDto> GetNextNumberAsync(int contractTypeId, CancellationToken ct = default)
    {
        var year = DateTime.Now.Year;
        var (docNumber, y, seq) = await _repo.ConsumeNextNumberAsync(contractTypeId, year, ct);
        // Extraer el prefijo completo quitando los dos últimos segmentos (-año-seq)
        var lastDash       = docNumber.LastIndexOf('-');
        var secondLastDash = lastDash > 0 ? docNumber.LastIndexOf('-', lastDash - 1) : -1;
        var prefix         = secondLastDash > 0 ? docNumber[..secondLastDash] : docNumber;
        return new ContractNextNumberDto(docNumber, prefix, y, seq);
    }
}
