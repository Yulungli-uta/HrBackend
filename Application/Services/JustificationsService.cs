using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class JustificationsService : Service<PunchJustifications, int>, IJustificationsService
{
    private readonly AppDbContext _db;
    private readonly IPunchJustificationsRepository _repository;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly ILogger<JustificationsService> _logger;

    private static readonly JsonSerializerOptions LogJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JustificationsService(
        IPunchJustificationsRepository repo,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        ILogger<JustificationsService> logger
    ) : base(repo)
    {
        _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<PunchJustifications>> GetByBossEmployeeId(int bossEmployeeId, CancellationToken ct)
        => _repository.GetByBossEmployeeId(bossEmployeeId, ct);

    public Task<IEnumerable<PunchJustifications>> GetByEmployeeId(int employeeId, CancellationToken ct)
        => _repository.GetByEmployeeId(employeeId, ct);

    public async Task ApplyJustificationsAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "HR.sp_Justifications_Apply";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@FromDate", fromDate.Date));
        command.Parameters.Add(new SqlParameter("@ToDate", toDate.Date));
        command.Parameters.Add(new SqlParameter("@EmployeeID", (object?)employeeId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(ct);

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<PunchJustifications> CreateWithNotifyAsync(PunchJustifications entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        var strategy = _db.Database.CreateExecutionStrategy();
        PunchJustifications? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            entity.Status = NormalizeStatus(entity.Status);
            entity.Approved = entity.Status == "APPROVED";
            entity.CreatedAt ??= DateTime.Now;

            created = await base.CreateAsync(entity, ct);

            await tx.CommitAsync(ct);
        });

        if (created is not null)
        {
            _logger.LogInformation("JUST CREATE => PunchJustId={Id} EmpId={EmpId} BossEmpId={BossEmpId} Status={Status}",
                created.PunchJustId, created.EmployeeId, created.BossEmployeeId, created.Status);

            await NotifyBossOnCreateIfPendingAsync(created, ct);
        }

        return created!;
    }

    public async Task<PunchJustifications> UpdateWithNotifyAsync(int id, PunchJustifications patch, CancellationToken ct)
    {
        if (patch is null) throw new ArgumentNullException(nameof(patch));

        // 1) Snapshot REAL desde BD (NoTracking) => evita el bug del ChangeTracker
        var dbSnapshot = await _db.Set<PunchJustifications>()
            .AsNoTracking()
            .Where(x => x.PunchJustId == id)
            .Select(x => new
            {
                x.PunchJustId,
                x.Status,
                x.ApprovedAt,
                x.EmployeeId,
                x.BossEmployeeId
            })
            .SingleOrDefaultAsync(ct);

        if (dbSnapshot is null)
            throw new KeyNotFoundException($"PunchJustifications con id={id} no existe.");

        var oldStatus = NormalizeStatus(dbSnapshot.Status);
        var oldApprovedAt = dbSnapshot.ApprovedAt;

        _logger.LogInformation("JUST PUT SNAPSHOT => {json}",
            JsonSerializer.Serialize(new { id, dbSnapshot.Status, oldStatus, oldApprovedAt }, LogJsonOptions));

        // 2) Log request (patch)
        _logger.LogInformation("JUST PUT REQUEST => {json}",
            JsonSerializer.Serialize(new
            {
                id,
                patch.Status,
                patch.ApprovedAt,
                patch.JustificationTypeId,
                patch.PunchTypeId,
                patch.StartDate,
                patch.EndDate,
                patch.JustificationDate,
                patch.HoursRequested
            }, LogJsonOptions));

        var strategy = _db.Database.CreateExecutionStrategy();
        PunchJustifications? updated = null;

        string newStatus = oldStatus;
        bool incomingHasStatus = false;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // 3) Cargar tracked SOLO para aplicar cambios
            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"PunchJustifications con id={id} no existe.");

            incomingHasStatus = !string.IsNullOrWhiteSpace(patch.Status);
            newStatus = incomingHasStatus ? NormalizeStatus(patch.Status) : oldStatus;

            // 4) Aplicación parcial (no pisar null/default)
            if (incomingHasStatus)
                current.Status = newStatus;

            if (patch.JustificationTypeId > 0)
                current.JustificationTypeId = patch.JustificationTypeId;

            if (patch.PunchTypeId.HasValue)
                current.PunchTypeId = patch.PunchTypeId;

            if (patch.StartDate.HasValue)
                current.StartDate = patch.StartDate;

            if (patch.EndDate.HasValue)
                current.EndDate = patch.EndDate;

            if (patch.JustificationDate.HasValue)
                current.JustificationDate = patch.JustificationDate;

            if (!string.IsNullOrWhiteSpace(patch.Reason))
                current.Reason = patch.Reason;

            if (patch.HoursRequested.HasValue)
                current.HoursRequested = patch.HoursRequested;

            if (patch.ApprovedAt.HasValue)
                current.ApprovedAt = patch.ApprovedAt;

            // Consistencia Approved
            current.Approved = NormalizeStatus(current.Status) == "APPROVED";

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Error al recargar la justificación actualizada.");

            await tx.CommitAsync(ct);
        });

        // 5) Decisión de envío
        var statusChanged = updated is not null &&
            !string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase);

        var approvedAtJustSet = updated is not null &&
            string.Equals(newStatus, "APPROVED", StringComparison.OrdinalIgnoreCase) &&
            oldApprovedAt is null &&
            updated.ApprovedAt is not null;

        _logger.LogInformation("JUST PUT SEND CHECK => {json}",
            JsonSerializer.Serialize(new
            {
                id,
                oldStatus,
                newStatus,
                statusChanged,
                oldApprovedAt,
                newApprovedAt = updated?.ApprovedAt,
                approvedAtJustSet
            }, LogJsonOptions));

        // Recomendación: enviar si cambia estado o si se aprobó por primera vez (ApprovedAt set)
        if (updated is not null && (statusChanged || approvedAtJustSet))
        {
            await NotifyOnStatusChangedAsync(updated, oldStatus, newStatus, ct);
        }

        return updated!;
    }

    // -----------------------------
    // Notificaciones
    // -----------------------------
    private async Task NotifyBossOnCreateIfPendingAsync(PunchJustifications created, CancellationToken ct)
    {
        try
        {
            var status = NormalizeStatus(created.Status);
            if (status != "PENDING") return;

            var bossEmail = await GetBossEmailAsync(created.BossEmployeeId, ct);
            if (string.IsNullOrWhiteSpace(bossEmail))
            {
                _logger.LogWarning("JUST CREATE => BossEmail vacío. PunchJustId={Id}", created.PunchJustId);
                return;
            }

            var body = GenerateEmailBodyToApproveSafe(created);
            if (string.IsNullOrWhiteSpace(body)) return;

            await _emailBuilder.TryNotifyAsync(
                EmailTemplateKey.AttendancePunch,
                $"Justificación #{created.PunchJustId} para aprobación",
                body,
                to: bossEmail,
                timeoutSeconds: 15,
                ct: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JUST CREATE => fallo notificando al jefe. PunchJustId={Id}", created?.PunchJustId);
        }
    }

    private async Task NotifyOnStatusChangedAsync(PunchJustifications updated, string oldStatus, string newStatus, CancellationToken ct)
    {
        try
        {
            oldStatus = NormalizeStatus(oldStatus);
            newStatus = NormalizeStatus(newStatus);

            if (newStatus == "PENDING")
            {
                var bossEmail = await GetBossEmailAsync(updated.BossEmployeeId, ct);
                if (string.IsNullOrWhiteSpace(bossEmail)) return;

                var body = GenerateEmailBodyToApproveSafe(updated);
                if (string.IsNullOrWhiteSpace(body)) return;

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Justificación #{updated.PunchJustId} para aprobación",
                    body,
                    to: bossEmail,
                    timeoutSeconds: 15,
                    ct: ct
                );
                return;
            }

            if (newStatus is "APPROVED" or "REJECTED" or "APPLIED")
            {
                var owner = await _employeeDetails.GetEmployeeDetailsAsync(updated.EmployeeId, ct);
                var toEmployee = owner?.Email?.Trim();
                if (string.IsNullOrWhiteSpace(toEmployee)) return;

                var body = GenerateEmailBodyChangeStatusSafe(updated, oldStatus, newStatus);
                if (string.IsNullOrWhiteSpace(body)) return;

                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Estado de justificación #{updated.PunchJustId}: {newStatus}",
                    body,
                    to: toEmployee,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JUST STATUS => fallo notificando. PunchJustId={Id} Old={Old} New={New}",
                updated?.PunchJustId, oldStatus, newStatus);
        }
    }

    private async Task<string?> GetBossEmailAsync(int bossEmployeeId, CancellationToken ct)
    {
        if (bossEmployeeId > 0)
        {
            var boss = await _employeeDetails.GetEmployeeDetailsAsync(bossEmployeeId, ct);
            var email = boss?.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email)) return email;
        }

        await _currentUser.LoadBossAsync(ct);
        return _currentUser.BossEmail?.Trim();
    }

    // -----------------------------
    // Helpers
    // -----------------------------
    // BD: REJECTED | APPROVED | PENDING | APPLIED
    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "PENDING";
        var v = status.Trim().ToUpperInvariant();

        if (v.Contains("APPROV")) return "APPROVED";
        if (v.Contains("REJECT")) return "REJECTED";
        if (v.Contains("APPL")) return "APPLIED";
        if (v.Contains("PEND")) return "PENDING";

        return "PENDING";
    }

    private string GenerateEmailBodyToApproveSafe(PunchJustifications just)
    {
        try { return GenerateEmailBodyToApprove(just) ?? string.Empty; }
        catch { return string.Empty; }
    }

    private string GenerateEmailBodyChangeStatusSafe(PunchJustifications just, string oldStatus, string newStatus)
    {
        try { return GenerateEmailBodyChangeStatus(just, oldStatus, newStatus) ?? string.Empty; }
        catch { return string.Empty; }
    }

    private string GenerateEmailBodyToApprove(PunchJustifications just)
    {
        var requesterName = string.IsNullOrWhiteSpace(_currentUser.UserName)
            ? $"Empleado #{just.EmployeeId}"
            : _currentUser.UserName;

        var date = just.JustificationDate ?? just.StartDate ?? just.EndDate;

        return
            $"<p>Registro de Justificación.</p>" +
            $"<ul>" +
            $"<p>Se ha registrado una justificación para su aprobación.</p>" +
            $"<li><b>Empleado:</b> {requesterName}</li>" +
            (date.HasValue ? $"<li><b>Fecha:</b> {date:yyyy-MM-dd}</li>" : "") +
            (just.StartDate.HasValue ? $"<li><b>Desde:</b> {just.StartDate:yyyy-MM-dd HH:mm}</li>" : "") +
            (just.EndDate.HasValue ? $"<li><b>Hasta:</b> {just.EndDate:yyyy-MM-dd HH:mm}</li>" : "") +
            (just.HoursRequested.HasValue ? $"<li><b>Horas solicitadas:</b> {just.HoursRequested:0.##}</li>" : "") +
            (!string.IsNullOrWhiteSpace(just.Reason) ? $"<li><b>Motivo:</b> {just.Reason}</li>" : "") +
            (!string.IsNullOrWhiteSpace(just.Comments) ? $"<li><b>Comentarios:</b> {just.Comments}</li>" : "") +
            $"</ul>";
    }

    private static string GenerateEmailBodyChangeStatus(PunchJustifications just, string oldStatus, string newStatus)
    {
        var date = just.JustificationDate ?? just.StartDate ?? just.EndDate;

        return
            $"<p>Estado de Justificación.</p>" +
            $"<ul>" +
            $"<p>El estado de la justificación #{just.PunchJustId} cambió de <b>{oldStatus}</b> a <b>{newStatus}</b>.</p>" +
            (date.HasValue ? $"<li><b>Fecha:</b> {date:yyyy-MM-dd}</li>" : "") +
            (just.StartDate.HasValue ? $"<li><b>Desde:</b> {just.StartDate:yyyy-MM-dd HH:mm}</li>" : "") +
            (just.EndDate.HasValue ? $"<li><b>Hasta:</b> {just.EndDate:yyyy-MM-dd HH:mm}</li>" : "") +
            (just.HoursRequested.HasValue ? $"<li><b>Horas:</b> {just.HoursRequested:0.##}</li>" : "") +
            (!string.IsNullOrWhiteSpace(just.Reason) ? $"<li><b>Motivo:</b> {just.Reason}</li>" : "") +
            (!string.IsNullOrWhiteSpace(just.Comments) ? $"<li><b>Comentarios:</b> {just.Comments}</li>" : "") +
            $"</ul>";
    }
}
