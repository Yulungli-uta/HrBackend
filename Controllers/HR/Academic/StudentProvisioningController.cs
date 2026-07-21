using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Academic;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models.Academic;

namespace WsUtaSystem.Controllers.HR.Academic;

/// <summary>
/// Gestiona el seguimiento del aprovisionamiento AD de estudiantes.
/// El estado vive en HrBackend (tbl_StudentProvisioning); RepositoryUta ejecuta las ops AD.
/// </summary>
[ApiController]
[Route("student-provisioning")]
public class StudentProvisioningController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStudentProvisioningClient _adClient;
    private readonly IEmployeeProvisioningClient _authClient;
    private readonly ILogger<StudentProvisioningController> _logger;

    public StudentProvisioningController(
        AppDbContext db,
        IStudentProvisioningClient adClient,
        IEmployeeProvisioningClient authClient,
        ILogger<StudentProvisioningController> logger)
    {
        _db         = db;
        _adClient   = adClient;
        _authClient = authClient;
        _logger     = logger;
    }

    /// <summary>Lista registros de aprovisionamiento con paginación y filtro por estado.</summary>
    [HttpGet]
    [RequirePermission("STUDENT_PROVISIONING.READ")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? statusId = null,
        CancellationToken ct = default)
    {
        var query = _db.StudentProvisionings.AsQueryable();
        if (statusId.HasValue)
            query = query.Where(p => p.ProvisioningStatusId == statusId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.StudentId,
                p.Email,
                p.DisplayName,
                p.GivenName,
                p.Surname,
                p.ProvisioningStatusId,
                p.ProvisioningStatusName,
                p.AdObjectId,
                p.SourceReference,
                p.ErrorMessage,
                p.RequestedBy,
                p.ProvisionedAt,
                p.DisabledAt,
                p.CreatedAt,
                p.UpdatedAt,
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = new { items, total, page, pageSize }
        });
    }

    /// <summary>Retorna un registro de aprovisionamiento por Id.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("STUDENT_PROVISIONING.READ")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var record = await _db.StudentProvisionings
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id, p.StudentId, p.Email, p.DisplayName,
                p.GivenName, p.Surname,
                p.ProvisioningStatusId, p.ProvisioningStatusName,
                p.AdObjectId, p.SourceReference,
                p.ErrorMessage, p.RequestedBy,
                p.ProvisionedAt, p.DisabledAt,
                p.CreatedAt, p.UpdatedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (record is null) return NotFound();
        return Ok(new { data = record });
    }

    /// <summary>
    /// Deshabilita manualmente la cuenta AD de un estudiante.
    /// Busca el AdObjectId en tbl_StudentProvisioning y delega la operación a RepositoryUta.
    /// </summary>
    [HttpPost("{studentId:int}/disable")]
    [RequirePermission("STUDENT_PROVISIONING.MANAGE")]
    public async Task<IActionResult> Disable(int studentId, CancellationToken ct)
    {
        _logger.LogInformation(
            "POST student-provisioning/{Id}/disable. Usuario={User}",
            studentId, User.Identity?.Name ?? "desconocido");

        var provRecord = await _db.StudentProvisionings
            .Where(p => p.StudentId == studentId
                     && p.ProvisioningStatusId == (int)StudentProvisioningStatus.CreatedInAd
                     && p.AdObjectId != null)
            .OrderByDescending(p => p.ProvisionedAt)
            .FirstOrDefaultAsync(ct);

        if (provRecord is null)
            return NotFound(new { success = false, message = "No se encontró cuenta AD activa para este estudiante." });

        var serviceToken = await _authClient.GetServiceTokenAsync(ct) ?? string.Empty;

        var result = await _adClient.DisableAdAccountAsync(provRecord.AdObjectId!, serviceToken, ct);

        if (result?.Success == true)
        {
            provRecord.ProvisioningStatusId   = (int)StudentProvisioningStatus.Disabled;
            provRecord.ProvisioningStatusName = nameof(StudentProvisioningStatus.Disabled);
            provRecord.DisabledAt             = DateTime.UtcNow;
            provRecord.UpdatedAt              = DateTime.UtcNow;

            var student = await _db.Students.FindAsync([studentId], ct);
            if (student is not null)
                student.IsActive = false;

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                success    = true,
                studentId,
                email      = provRecord.Email,
                disabledAt = provRecord.DisabledAt
            });
        }

        return BadRequest(new
        {
            success      = false,
            studentId,
            errorMessage = result?.ErrorMessage ?? "Sin respuesta de RepositoryUta"
        });
    }
}
