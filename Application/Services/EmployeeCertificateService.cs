using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Certificados laborales del empleado autenticado. Reutiliza el motor documental existente
/// (plantilla CERTIFICADO_LABORAL + <see cref="IDocumentGenerationService"/>) — no genera PDFs
/// por su cuenta. La emisión es automática al solicitar: un certificado laboral es un documento
/// administrativo de bajo riesgo (confirma hechos ya registrados en el sistema), no requiere
/// aprobación manual de RRHH para este alcance.
/// </summary>
public sealed class EmployeeCertificateService : IEmployeeCertificateService
{
    private const string TemplateCodeLaboral = "CERTIFICADO_LABORAL";
    private const string TemplateCodeHistorialLaboral = "CERTIFICADO_HISTORIAL_LABORAL";

    private readonly IEmployeeCertificateRepository _repository;
    private readonly IDocumentGenerationService _documentGenerationService;

    public EmployeeCertificateService(
        IEmployeeCertificateRepository repository,
        IDocumentGenerationService documentGenerationService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _documentGenerationService = documentGenerationService ?? throw new ArgumentNullException(nameof(documentGenerationService));
    }

    /// <inheritdoc/>
    public async Task<EmployeeCertificateDetailDto> CreateAsync(int employeeId, CreateEmployeeCertificateRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (employeeId <= 0)
            throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        var certificateType = string.IsNullOrWhiteSpace(request.CertificateType)
            ? EmployeeCertificateType.Laboral
            : request.CertificateType;

        var entity = new EmployeeCertificateRequest
        {
            EmployeeId = employeeId,
            CertificateType = certificateType,
            Purpose = request.Purpose,
            Status = EmployeeCertificateStatus.Pendiente
        };

        await _repository.AddAsync(entity, ct);
        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeCertificateStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = null,
            NewStatus = EmployeeCertificateStatus.Pendiente,
            Action = "CREATED",
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);

        // ── Emisión automática: genera el PDF reutilizando el motor documental existente ──
        var templateCode = certificateType == EmployeeCertificateType.HistorialLaboral
            ? TemplateCodeHistorialLaboral
            : TemplateCodeLaboral;

        var templateId = await _repository.GetPublishedTemplateIdAsync(templateCode, ct)
            ?? throw new InvalidOperationException($"No existe una plantilla publicada '{templateCode}'.");

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (certificateType == EmployeeCertificateType.HistorialLaboral)
        {
            var history = await _repository.GetEmploymentHistoryAsync(employeeId, ct);
            overrides["HISTORY_TABLE_HTML"] = BuildHistoryTableHtml(history);
        }
        else
        {
            var (jobDescription, departmentName, _) = await _repository.GetCurrentPositionAsync(employeeId, ct);
            overrides["JOB_DESCRIPTION"] = jobDescription ?? string.Empty;
            overrides["DEPARTMENT_NAME"] = departmentName ?? string.Empty;
        }

        var generated = await _documentGenerationService.GenerateAsync(
            new GenerateDocumentRequest(
                TemplateId: templateId,
                EmployeeId: employeeId,
                EntityType: DocumentEntityType.Certificate,
                EntityId: entity.RequestId,
                DocumentNumber: null,
                Notes: request.Purpose,
                ManualOverrides: overrides),
            employeeId,
            ct);

        entity.GeneratedDocumentId = generated.DocumentId;
        entity.Status = EmployeeCertificateStatus.Emitido;
        entity.IssuedAt = DateTime.Now;
        entity.IssuedBy = employeeId;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeCertificateStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = EmployeeCertificateStatus.Pendiente,
            NewStatus = EmployeeCertificateStatus.Emitido,
            Action = "ISSUED",
            Observation = "Emitido automáticamente al solicitar.",
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(entity.RequestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar el certificado recién creado.");
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeCertificateResult> GetMyRequestsAsync(int employeeId, EmployeeCertificateQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var ownFilter = filter with { EmployeeId = employeeId, AllowedDepartmentIds = null };
        return await _repository.GetPagedAsync(ownFilter, ct);
    }

    /// <inheritdoc/>
    public async Task<EmployeeCertificateDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var detail = await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe el certificado {requestId}.");
        if (detail.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("El certificado no pertenece al usuario autenticado.");
        return detail;
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeCertificateResult> GetPagedAsync(EmployeeCertificateQueryFilter filter, CancellationToken ct = default)
        => await _repository.GetPagedAsync(filter, ct);

    /// <inheritdoc/>
    public async Task<EmployeeCertificateDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
        => await _repository.GetDetailByIdAsync(requestId, ct)
           ?? throw new KeyNotFoundException($"No existe el certificado {requestId}.");

    /// <inheritdoc/>
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMyDocumentAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var detail = await GetMyRequestDetailAsync(requestId, employeeId, ct);
        return await DownloadResolvedAsync(detail, ct);
    }

    /// <inheritdoc/>
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(int requestId, CancellationToken ct = default)
    {
        var detail = await GetDetailByIdAsync(requestId, ct);
        return await DownloadResolvedAsync(detail, ct);
    }

    /// <summary>
    /// Construye la tabla HTML del historial laboral. Se pasa como override del campo
    /// HISTORY_TABLE_HTML (sufijo _HTML = contenido de confianza, no se HTML-encodea en
    /// DocumentTemplateEngine — ver comentario en ese archivo). Las CELDAS sí se encodean
    /// manualmente aquí para no confiar ciegamente en datos de cargo/dependencia.
    /// </summary>
    private static string BuildHistoryTableHtml(IReadOnlyList<Application.DTOs.EmployeeCertificate.EmploymentHistoryEntry> history)
    {
        if (history.Count == 0)
            return "<p>No se encontraron registros de historial laboral.</p>";

        static string Enc(string? s) => System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(s) ? "—" : s);

        var sb = new System.Text.StringBuilder();
        sb.Append("<table class=\"history\"><tr>")
          .Append("<th>Tipo</th><th>Documento</th><th>Fecha inicio</th><th>Fecha fin</th><th>Cargo</th><th>Dependencia</th><th>Estado</th>")
          .Append("</tr>");

        foreach (var entry in history)
        {
            var typeLabel = entry.SourceType == "CONTRACT" ? "Contrato" : "Acción de Personal";
            var start = entry.StartDate?.ToString("dd/MM/yyyy") ?? "—";
            var end = entry.EndDate?.ToString("dd/MM/yyyy") ?? "Indefinido";

            sb.Append("<tr>")
              .Append($"<td>{Enc(typeLabel)}</td>")
              .Append($"<td>{Enc(entry.DocumentNumber)}</td>")
              .Append($"<td>{Enc(start)}</td>")
              .Append($"<td>{Enc(end)}</td>")
              .Append($"<td>{Enc(entry.JobTitle)}</td>")
              .Append($"<td>{Enc(entry.DepartmentName)}</td>")
              .Append($"<td>{Enc(entry.StatusLabel)}</td>")
              .Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadResolvedAsync(EmployeeCertificateDetailDto detail, CancellationToken ct)
    {
        if (detail.GeneratedDocumentId is null)
            throw new InvalidOperationException("Este certificado todavía no tiene un documento generado.");

        return await _documentGenerationService.DownloadAsync(detail.GeneratedDocumentId.Value, ct);
    }
}
