using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class ContractsService : Service<Contracts, int>, IContractsService
{
    private readonly IContractsRepository _repository;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly IRefTypesService _refTypes;
    private readonly ILogger<ContractsService> _logger;

    public ContractsService(
        IContractsRepository repo,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        IRefTypesService refTypes,
        ILogger<ContractsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------
    // Compatibilidad con el uso antiguo (entity)
    // -------------------------------------------------------
    public new Task<Contracts> CreateAsync(Contracts entity, CancellationToken ct)
        => CreateAndNotifyAsync(entity, ct);

    public new Task UpdateAsync(int id, Contracts entity, CancellationToken ct)
        => UpdateAndNotifyAsync(id, entity, ct);

    // -------------------------------------------------------
    // NUEVO: Update desde DTO (controller delgado)
    // -------------------------------------------------------
    public async Task UpdateAsync(int id, ContractsUpdateDto dto, CancellationToken ct)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.ContractID != 0 && dto.ContractID != id)
            throw new ArgumentException("ContractID del body no coincide con el id de la ruta.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? updated = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Contracts con id={id} no existe.");

            // ✅ Update campo por campo (en Service, buena práctica)
            ApplyDto(dto, current);

            ValidateDates(current);

            // Si manejas RowVersion en tu arquitectura, aquí va el OriginalValue:
            // _db.Entry(current).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct);

            await tx.CommitAsync(ct);
        });

        if (updated is not null)
            await NotifyOnUpdateAsync(updated, ct);
    }

    // -------------------------------------------------------
    // Métodos de negocio existentes (entity)
    // -------------------------------------------------------
    public async Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            created = await base.CreateAsync(entity, ct);

            await tx.CommitAsync(ct);
        });

        if (created is not null)
            await NotifyOnCreateAsync(created, ct);

        return created!;
    }

    public async Task UpdateAndNotifyAsync(int id, Contracts entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? updated = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Contracts con id={id} no existe.");

            CopyUpdatableFields(source: entity, target: current);

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct);

            await tx.CommitAsync(ct);
        });

        if (updated is not null)
            await NotifyOnUpdateAsync(updated, ct);
    }

    public async Task<IReadOnlyList<int>> GetAllowedNextStatusesAsync(int currentStatusTypeId, CancellationToken ct)
    {
        var next = await _db.ContractStatusTransitions
            .AsNoTracking()
            .Where(x => x.IsActive && x.FromStatusTypeID == currentStatusTypeId)
            .Select(x => x.ToStatusTypeID)
            .Distinct()
            .ToListAsync(ct);

        return next;
    }

    public async Task ChangeStatusAsync(int contractId, int toStatusTypeId, string? comment, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var contract = await _db.Set<Contracts>().FirstOrDefaultAsync(x => x.ContractID == contractId, ct);
            if (contract is null) throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

            var fromStatus = contract.Status; // asumiendo int
            if (fromStatus == toStatusTypeId)
                return; // NoOp

            var allowed = await _db.ContractStatusTransitions
                .AsNoTracking()
                .AnyAsync(x => x.IsActive && x.FromStatusTypeID == fromStatus && x.ToStatusTypeID == toStatusTypeId, ct);

            if (!allowed)
                throw new InvalidOperationException($"Transición no permitida: {fromStatus} -> {toStatusTypeId}");

            // Actualiza estado
            contract.Status = toStatusTypeId;

            // Histórico (auditoría por JWT)
            var userId = _currentUser.EmployeeId; // según tu implementación
            _db.ContractStatusHistories.Add(new ContractStatusHistory
            {
                ContractID = contractId,
                StatusTypeID = toStatusTypeId,
                Comment = comment,
                ChangedBy = userId > 0 ? userId : null,
                ChangedAt = DateTime.Now
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<ContractStatusHistoryDto>> GetStatusHistoryAsync(int contractId, CancellationToken ct)
    {
        // RefTypes por categoría (no hardcode de IDs)
        var refTypes = await _refTypes.GetByCategoryAsync("CONTRACT_STATUS", ct);
        var map = refTypes.ToDictionary(x => x.TypeId, x => x.Name);

        var items = await _db.ContractStatusHistories
            .AsNoTracking()
            .Where(x => x.ContractID == contractId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync(ct);

        return items.Select(h => new ContractStatusHistoryDto
        {
            HistoryID = h.HistoryID,
            ContractID = h.ContractID,
            StatusTypeID = h.StatusTypeID,
            StatusName = map.TryGetValue(h.StatusTypeID, out var name) ? name : null,
            Comment = h.Comment,
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy
        }).ToList();
    }

    public async Task<IReadOnlyList<Contracts>> GetAddendumsAsync(int contractId, CancellationToken ct)
    {
        return await _db.Set<Contracts>()
            .AsNoTracking()
            .Where(x => x.ParentID == contractId) // requiere campo ParentID
            .OrderByDescending(x => x.ContractID)
            .ToListAsync(ct);
    }



    // -------------------------------------------------------
    // Notificaciones
    // -------------------------------------------------------
    private async Task NotifyOnCreateAsync(Contracts created, CancellationToken ct)
    {
        try
        {
            await _currentUser.LoadBossAsync(ct);
            var toBoss = _currentUser.BossEmail?.Trim();

            if (!string.IsNullOrWhiteSpace(toBoss))
            {
                var body = BuildCreateEmailBody(created);
                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Contrato creado: {created.ContractCode}",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CREATE contract => fallo notificando. ContractID={ContractID}", created?.ContractID);
        }
    }

    private async Task NotifyOnUpdateAsync(Contracts updated, CancellationToken ct)
    {
        try
        {
            await _currentUser.LoadBossAsync(ct);
            var toBoss = _currentUser.BossEmail?.Trim();

            if (!string.IsNullOrWhiteSpace(toBoss))
            {
                var body = BuildUpdateEmailBody(updated);
                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Contrato actualizado: {updated.ContractCode}",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UPDATE contract => fallo notificando. ContractID={ContractID}", updated?.ContractID);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private static void ValidateDates(Contracts entity)
    {
        if (entity.EndDate < entity.StartDate)
            throw new Exception("EndDate no puede ser menor que StartDate.");
    }

    private static void ApplyDto(ContractsUpdateDto dto, Contracts target)
    {
        target.CertificationID = dto.CertificationID;
        target.ParentID = dto.ParentID;
        target.ContractCode = dto.ContractCode?.Trim() ?? target.ContractCode;

        target.PersonID = dto.PersonID;
        target.ContractTypeID = dto.ContractTypeID;
        target.JobID = dto.JobID;

        target.StartDate = dto.StartDate;
        target.EndDate = dto.EndDate;

        target.ContractFileName = dto.ContractFileName;
        target.ContractFilepath = dto.ContractFilepath;

        target.Status = dto.Status;
        target.ContractDescription = dto.ContractDescription;

        target.DepartmentID = dto.DepartmentID;
        target.AuthorizationDate = dto.AuthorizationDate;

        target.ResignationFileName = dto.ResignationFileName;
        target.ResignationFilepath = dto.ResignationFilepath;
        target.ResignationCode = dto.ResignationCode;
        target.RegResignationDate = dto.RegResignationDate;
        target.ResignationDate = dto.ResignationDate;

        target.CancelReason = dto.CancelReason;
        target.CancelFilename = dto.CancelFilename;
        target.CancelFilepath = dto.CancelFilepath;
        target.CancelCode = dto.CancelCode;
        target.RegistrationDateAnulCon = dto.RegistrationDateAnulCon;

        target.Nationality = dto.Nationality;
        target.Visa = dto.Visa;
        target.Consulate = dto.Consulate;
        target.WorkOf = dto.WorkOf;

        target.InicialContent = dto.InicialContent;
        target.ResolucionContent = dto.ResolucionContent;

        target.RelationshipType = dto.RelationshipType;
        target.Relationship = dto.Relationship;

        target.Competition = dto.Competition;
        target.CompetitionDate = dto.CompetitionDate;
    }

    private static void CopyUpdatableFields(Contracts source, Contracts target)
    {
        target.CertificationID = source.CertificationID;
        target.ParentID = source.ParentID;
        target.ContractCode = source.ContractCode?.Trim() ?? target.ContractCode;

        target.PersonID = source.PersonID;
        target.ContractTypeID = source.ContractTypeID;
        target.JobID = source.JobID;

        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;

        target.ContractFileName = source.ContractFileName;
        target.ContractFilepath = source.ContractFilepath;

        target.Status = source.Status;
        target.ContractDescription = source.ContractDescription;

        target.DepartmentID = source.DepartmentID;
        target.AuthorizationDate = source.AuthorizationDate;

        target.ResignationFileName = source.ResignationFileName;
        target.ResignationFilepath = source.ResignationFilepath;
        target.ResignationCode = source.ResignationCode;
        target.RegResignationDate = source.RegResignationDate;
        target.ResignationDate = source.ResignationDate;

        target.CancelReason = source.CancelReason;
        target.CancelFilename = source.CancelFilename;
        target.CancelFilepath = source.CancelFilepath;
        target.CancelCode = source.CancelCode;
        target.RegistrationDateAnulCon = source.RegistrationDateAnulCon;

        target.Nationality = source.Nationality;
        target.Visa = source.Visa;
        target.Consulate = source.Consulate;
        target.WorkOf = source.WorkOf;

        target.InicialContent = source.InicialContent;
        target.ResolucionContent = source.ResolucionContent;

        target.RelationshipType = source.RelationshipType;
        target.Relationship = source.Relationship;

        target.Competition = source.Competition;
        target.CompetitionDate = source.CompetitionDate;
    }

    private static string BuildCreateEmailBody(Contracts c) => $@"
        <h3>Nuevo contrato creado</h3>
        <ul>
          <li><b>Contrato:</b> {c.ContractCode}</li>
          <li><b>PersonID:</b> {c.PersonID}</li>
          <li><b>Tipo:</b> {c.ContractTypeID}</li>
          <li><b>Departamento:</b> {c.DepartmentID}</li>
          <li><b>Inicio:</b> {c.StartDate:yyyy-MM-dd}</li>
          <li><b>Fin:</b> {c.EndDate:yyyy-MM-dd}</li>
          <li><b>Estado:</b> {c.Status}</li>
        </ul>";

    private static string BuildUpdateEmailBody(Contracts c) => $@"
        <h3>Contrato actualizado</h3>
        <ul>
          <li><b>Contrato:</b> {c.ContractCode}</li>
          <li><b>PersonID:</b> {c.PersonID}</li>
          <li><b>Tipo:</b> {c.ContractTypeID}</li>
          <li><b>Departamento:</b> {c.DepartmentID}</li>
          <li><b>Inicio:</b> {c.StartDate:yyyy-MM-dd}</li>
          <li><b>Fin:</b> {c.EndDate:yyyy-MM-dd}</li>
          <li><b>Estado:</b> {c.Status}</li>
        </ul>";
}
