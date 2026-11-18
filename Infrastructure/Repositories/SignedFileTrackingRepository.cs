using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.SignedFileTracking;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Lectura de <c>HR.vw_PersonnelActionSignedFile</c> y <c>HR.vw_ContractSignedFile</c>.
/// Las vistas ya solo traen registros con archivo firmado activo; aquí se filtra y se pagina.
/// </summary>
public sealed class SignedFileTrackingRepository : ISignedFileTrackingRepository
{
    private readonly AppDbContext _db;

    public SignedFileTrackingRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<PersonnelActionSignedFileDto>> GetActionsPagedAsync(SignedFileTrackingFilter filter, CancellationToken ct)
    {
        var query = _db.Set<VwPersonnelActionSignedFile>().AsNoTracking().AsQueryable();

        if (filter.AllowedDepartmentIds is { } allowed)
        {
            var ids = allowed.ToList();
            query = query.Where(x => x.DepartmentId != null && ids.Contains(x.DepartmentId.Value));
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => (x.PersonIdCard != null && x.PersonIdCard.Contains(term))
                                  || (x.PersonName != null && x.PersonName.Contains(term))
                                  || (x.ActionNumber != null && x.ActionNumber.Contains(term)));
        }
        if (filter.TypeId is { } typeId) query = query.Where(x => x.ActionTypeId == typeId);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
        if (filter.DepartmentId is { } deptId) query = query.Where(x => x.DepartmentId == deptId);
        if (filter.StartDate is { } from) query = query.Where(x => x.EffectiveDate != null && x.EffectiveDate >= from);
        if (filter.EndDate is { } to) query = query.Where(x => x.EffectiveDate != null && x.EffectiveDate <= to);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.ActionId)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new PersonnelActionSignedFileDto(
                x.ActionId, x.ActionNumber, x.PersonIdCard, x.PersonName, x.DepartmentName, x.ActionTypeName, x.Status,
                x.EffectiveDate, x.EndDate, x.ElaboratorIdCard, x.ElaboratorName,
                x.SignedFileName, x.SignedFileSizeBytes, x.SignedFileUploadedAt))
            .ToListAsync(ct);

        return new PagedResult<PersonnelActionSignedFileDto> { Items = items, Page = filter.Page, PageSize = filter.PageSize, TotalCount = total };
    }

    public async Task<PagedResult<ContractSignedFileDto>> GetContractsPagedAsync(SignedFileTrackingFilter filter, CancellationToken ct)
    {
        var query = _db.Set<VwContractSignedFile>().AsNoTracking().AsQueryable();

        if (filter.AllowedDepartmentIds is { } allowed)
        {
            var ids = allowed.ToList();
            query = query.Where(x => ids.Contains(x.DepartmentId));
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => (x.PersonIdCard != null && x.PersonIdCard.Contains(term))
                                  || (x.PersonName != null && x.PersonName.Contains(term))
                                  || (x.ContractCode != null && x.ContractCode.Contains(term)));
        }
        if (filter.TypeId is { } typeId) query = query.Where(x => x.ContractTypeId == typeId);
        if (filter.StatusTypeId is { } statusTypeId) query = query.Where(x => x.StatusTypeId == statusTypeId);
        if (filter.DepartmentId is { } deptId) query = query.Where(x => x.DepartmentId == deptId);
        if (filter.StartDate is { } from)
        {
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.StartDate >= fromDt);
        }
        if (filter.EndDate is { } to)
        {
            var toDt = to.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.StartDate <= toDt);
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.StartDate).ThenByDescending(x => x.ContractID)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new ContractSignedFileDto(
                x.ContractID, x.ContractCode, x.PersonIdCard, x.PersonName, x.DepartmentName, x.ContractTypeName, x.StatusName,
                x.StartDate, x.EndDate, x.CreatedByIdCard, x.CreatedByName,
                x.SignedFileName, x.SignedFileSizeBytes, x.SignedFileUploadedAt))
            .ToListAsync(ct);

        return new PagedResult<ContractSignedFileDto> { Items = items, Page = filter.Page, PageSize = filter.PageSize, TotalCount = total };
    }

    public async Task<Guid?> GetActionFileGuidAsync(int actionId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct)
    {
        var query = _db.Set<VwPersonnelActionSignedFile>().AsNoTracking().Where(x => x.ActionId == actionId);
        if (allowedDepartmentIds is { } allowed)
        {
            var ids = allowed.ToList();
            query = query.Where(x => x.DepartmentId != null && ids.Contains(x.DepartmentId.Value));
        }
        return await query.Select(x => (Guid?)x.SignedFileGuid).FirstOrDefaultAsync(ct);
    }

    public async Task<Guid?> GetContractFileGuidAsync(int contractId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct)
    {
        var query = _db.Set<VwContractSignedFile>().AsNoTracking().Where(x => x.ContractID == contractId);
        if (allowedDepartmentIds is { } allowed)
        {
            var ids = allowed.ToList();
            query = query.Where(x => ids.Contains(x.DepartmentId));
        }
        return await query.Select(x => (Guid?)x.SignedFileGuid).FirstOrDefaultAsync(ct);
    }
}
