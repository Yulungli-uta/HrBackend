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

    public async Task<ContractTypeWithTemplateDto?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default)
    {
        var ct2 = await _repo.GetWithDefaultTemplateAsync(contractTypeId, ct);
        if (ct2 is null) return null;

        string? templateName    = null;
        string? templateCode    = null;
        string? templateVersion = null;

        if (ct2.DefaultTemplateId.HasValue)
        {
            var tpl = await _db.Set<DocumentTemplate>()
                .AsNoTracking()
                .Where(t => t.TemplateId == ct2.DefaultTemplateId.Value)
                .Select(t => new { t.Name, t.TemplateCode, t.Version })
                .FirstOrDefaultAsync(ct);

            templateName    = tpl?.Name;
            templateCode    = tpl?.TemplateCode;
            templateVersion = tpl?.Version;
        }

        return new ContractTypeWithTemplateDto(
            ct2.ContractTypeId,
            ct2.Name,
            ct2.Description,
            ct2.Status,
            ct2.ContractCode,
            ct2.DocumentTemplateTypeId,
            ct2.DefaultTemplateId,
            templateName,
            templateCode,
            templateVersion,
            ct2.NumberingPrefix,
            ct2.NumberingYear,
            ct2.NumberingLastSequence
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
