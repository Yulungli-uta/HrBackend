using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.Academic;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Academic;

namespace WsUtaSystem.Application.Services.Academic;

/// <summary>
/// Orquesta la sincronización entre el origen de datos académico y el aprovisionamiento AD.
/// Flujo: busca o crea People → Students → llama RepositoryUta (AD) → persiste estado en tbl_StudentProvisioning.
/// </summary>
public class StudentEnrollmentSyncService : IStudentEnrollmentSyncService
{
    private readonly AppDbContext _db;
    private readonly IStudentEnrollmentSource _source;
    private readonly IStudentProvisioningClient _provisioningClient;
    private readonly ILogger<StudentEnrollmentSyncService> _logger;

    private const string DefaultInitialPassword = "Uta2024*Estudiante!";
    private const int DefaultStudentTypeId = 0;

    public StudentEnrollmentSyncService(
        AppDbContext db,
        IStudentEnrollmentSource source,
        IStudentProvisioningClient provisioningClient,
        ILogger<StudentEnrollmentSyncService> logger)
    {
        _db                 = db;
        _source             = source;
        _provisioningClient = provisioningClient;
        _logger             = logger;
    }

    // ── Sincronizar matrículas ────────────────────────────────────────────────

    public async Task<int> SyncPeriodAsync(string periodCode, string serviceToken, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ENROLLMENT-SYNC] Iniciando sincronización de período {PeriodCode}", periodCode);

        var enrolled = await _source.GetEnrolledAsync(periodCode, ct);
        _logger.LogInformation(
            "[ENROLLMENT-SYNC] Estudiantes matriculados encontrados: {Count}", enrolled.Count);

        int provisioned = 0;

        foreach (var enrollment in enrolled)
        {
            try
            {
                var student = await EnsureStudentAsync(enrollment, periodCode, ct);

                // Idempotencia: saltar si ya tiene cuenta AD activa
                var existingProv = await _db.StudentProvisionings
                    .Where(p => p.StudentId == student.StudentId
                             && p.ProvisioningStatusId == (int)StudentProvisioningStatus.CreatedInAd)
                    .FirstOrDefaultAsync(ct);

                if (existingProv is not null)
                {
                    _logger.LogInformation(
                        "[ENROLLMENT-SYNC] Cuenta ya aprovisionada. StudentId={Id}", student.StudentId);
                    continue;
                }

                var req = new CreateStudentAdAccountRequest(
                    HrStudentId:         student.StudentId,
                    DisplayName:         $"{enrollment.FirstName} {enrollment.LastName}",
                    GivenName:           enrollment.FirstName,
                    Surname:             enrollment.LastName,
                    InitialPassword:     DefaultInitialPassword,
                    IdCard:              enrollment.IdCard,
                    SourceReference:     $"Enrollment:{periodCode}"
                );

                // Registrar la solicitud antes de llamar a RepositoryUta
                var provRecord = new StudentProvisioning
                {
                    StudentId              = student.StudentId,
                    DisplayName            = req.DisplayName,
                    GivenName              = req.GivenName,
                    Surname                = req.Surname,
                    Email                  = string.Empty, // lo retorna RepositoryUta
                    ProvisioningStatusId   = (int)StudentProvisioningStatus.Requested,
                    ProvisioningStatusName = nameof(StudentProvisioningStatus.Requested),
                    SourceReference        = req.SourceReference,
                    CreatedAt              = DateTime.UtcNow,
                };
                _db.StudentProvisionings.Add(provRecord);
                await _db.SaveChangesAsync(ct);

                var result = await _provisioningClient.CreateAdAccountAsync(req, serviceToken, ct);

                if (result is null)
                {
                    provRecord.ProvisioningStatusId   = (int)StudentProvisioningStatus.AdFailed;
                    provRecord.ProvisioningStatusName = nameof(StudentProvisioningStatus.AdFailed);
                    provRecord.ErrorMessage           = "Sin respuesta de RepositoryUta";
                }
                else if (result.Success)
                {
                    provRecord.ProvisioningStatusId   = (int)StudentProvisioningStatus.CreatedInAd;
                    provRecord.ProvisioningStatusName = nameof(StudentProvisioningStatus.CreatedInAd);
                    provRecord.Email                  = result.Email ?? string.Empty;
                    provRecord.AdObjectId             = result.AdObjectId;
                    provRecord.ProvisionedAt          = DateTime.UtcNow;
                    provisioned++;

                    _logger.LogInformation(
                        "[ENROLLMENT-SYNC] Cuenta creada. StudentId={Id} | Email={Email}",
                        student.StudentId, result.Email);
                }
                else
                {
                    provRecord.ProvisioningStatusId   = (int)StudentProvisioningStatus.AdFailed;
                    provRecord.ProvisioningStatusName = nameof(StudentProvisioningStatus.AdFailed);
                    provRecord.Email                  = result.Email ?? string.Empty;
                    provRecord.ErrorMessage           = result.ErrorMessage;

                    _logger.LogWarning(
                        "[ENROLLMENT-SYNC] Fallo en AD. StudentId={Id} | Error={Err}",
                        student.StudentId, result.ErrorMessage);
                }

                provRecord.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ENROLLMENT-SYNC] Error procesando estudiante IdCard={IdCard}", enrollment.IdCard);
            }
        }

        _logger.LogInformation(
            "[ENROLLMENT-SYNC] Período {PeriodCode} finalizado: {Count} cuenta(s) aprovisionada(s).",
            periodCode, provisioned);

        return provisioned;
    }

    // ── Deshabilitar no re-matriculados ───────────────────────────────────────

    public async Task<int> DisableNonReEnrolledAsync(
        string currentPeriod,
        string previousPeriod,
        string serviceToken,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[ENROLLMENT-SYNC] Deshabilitando no re-matriculados. CurrentPeriod={Current} PreviousPeriod={Previous}",
            currentPeriod, previousPeriod);

        var nonReEnrolled = await _source.GetNonReEnrolledAsync(currentPeriod, previousPeriod, ct);
        _logger.LogInformation(
            "[ENROLLMENT-SYNC] No re-matriculados encontrados: {Count}", nonReEnrolled.Count);

        int disabled = 0;

        foreach (var record in nonReEnrolled)
        {
            try
            {
                var person = await _db.People
                    .FirstOrDefaultAsync(p => p.IdCard == record.IdCard, ct);

                if (person is null)
                {
                    _logger.LogWarning(
                        "[ENROLLMENT-SYNC] Persona no encontrada para IdCard={IdCard}", record.IdCard);
                    continue;
                }

                var student = await _db.Students
                    .FirstOrDefaultAsync(s => s.PersonID == person.PersonId, ct);

                if (student is null)
                {
                    _logger.LogWarning(
                        "[ENROLLMENT-SYNC] Registro Students no encontrado para PersonID={Id}", person.PersonId);
                    continue;
                }

                // Buscar el registro de aprovisionamiento activo (necesitamos el AdObjectId)
                var provRecord = await _db.StudentProvisionings
                    .Where(p => p.StudentId == student.StudentId
                             && p.ProvisioningStatusId == (int)StudentProvisioningStatus.CreatedInAd
                             && p.AdObjectId != null)
                    .OrderByDescending(p => p.ProvisionedAt)
                    .FirstOrDefaultAsync(ct);

                if (provRecord is null)
                {
                    _logger.LogWarning(
                        "[ENROLLMENT-SYNC] Sin registro AD activo para StudentId={Id}", student.StudentId);
                    continue;
                }

                var adResult = await _provisioningClient.DisableAdAccountAsync(
                    provRecord.AdObjectId!, serviceToken, ct);

                if (adResult?.Success == true)
                {
                    provRecord.ProvisioningStatusId   = (int)StudentProvisioningStatus.Disabled;
                    provRecord.ProvisioningStatusName = nameof(StudentProvisioningStatus.Disabled);
                    provRecord.DisabledAt             = DateTime.UtcNow;
                    provRecord.UpdatedAt              = DateTime.UtcNow;

                    student.IsActive = false;
                    disabled++;

                    _logger.LogInformation(
                        "[ENROLLMENT-SYNC] Cuenta deshabilitada. StudentId={Id}", student.StudentId);
                }
                else
                {
                    _logger.LogWarning(
                        "[ENROLLMENT-SYNC] Fallo al deshabilitar AD. StudentId={Id} | Error={Err}",
                        student.StudentId, adResult?.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ENROLLMENT-SYNC] Error deshabilitando IdCard={IdCard}", record.IdCard);
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[ENROLLMENT-SYNC] Deshabilitar finalizado: {Count} cuenta(s) deshabilitada(s).", disabled);

        return disabled;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Students> EnsureStudentAsync(
        Infrastructure.Academic.StudentEnrollmentRecord enrollment,
        string periodCode,
        CancellationToken ct)
    {
        var person = await _db.People
            .FirstOrDefaultAsync(p => p.IdCard == enrollment.IdCard, ct);

        if (person is null)
        {
            person = new People
            {
                IdCard    = enrollment.IdCard,
                FirstName = enrollment.FirstName,
                LastName  = enrollment.LastName,
                Email     = enrollment.Email ?? $"{enrollment.IdCard}@estudiante.uta.edu.ec",
            };
            _db.People.Add(person);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[ENROLLMENT-SYNC] People creado. PersonId={Id} | IdCard={IdCard}",
                person.PersonId, enrollment.IdCard);
        }

        var student = await _db.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.PersonID == person.PersonId, ct);

        if (student is null)
        {
            student = new Students
            {
                PersonID            = person.PersonId,
                StudentTypeId       = DefaultStudentTypeId,
                ExternalStudentCode = enrollment.ExternalStudentCode,
                IsActive            = true,
                CreatedAt           = DateTime.UtcNow,
            };
            _db.Students.Add(student);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[ENROLLMENT-SYNC] Students creado. StudentId={Id} | PersonId={PersonId}",
                student.StudentId, person.PersonId);
        }

        if (!student.Enrollments.Any(e => e.PeriodCode == periodCode))
        {
            _db.StudentEnrollments.Add(new StudentEnrollments
            {
                StudentId      = student.StudentId,
                PeriodCode     = periodCode,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status         = "Activo",
                Program        = enrollment.Program,
                Faculty        = enrollment.Faculty,
                CreatedAt      = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }

        return student;
    }
}
