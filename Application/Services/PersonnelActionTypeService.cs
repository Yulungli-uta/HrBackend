using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.PersonnelActionType;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class PersonnelActionTypeService
    : Service<PersonnelActionType, int>, IPersonnelActionTypeService
{
    private readonly IPersonnelActionTypeRepository _repo;

    public PersonnelActionTypeService(IPersonnelActionTypeRepository repo) : base(repo)
        => _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default)
        => _repo.GetAllActiveAsync(ct);

    public async Task<NextDocumentNumberDto> GetNextNumberAsync(
        int personnelActionTypeId,
        CancellationToken ct = default)
    {
        var year = DateTime.Now.Year;
        var (docNumber, y, seq) = await _repo.ConsumeNextNumberAsync(personnelActionTypeId, year, ct);
        return new NextDocumentNumberDto(docNumber, docNumber.Split('-')[0], y, seq);
    }
}
