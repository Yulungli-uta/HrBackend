namespace WsUtaSystem.Application.Interfaces.Services.Academic;

/// <summary>
/// Typed HttpClient hacia RepositoryUta para operaciones AD de estudiantes.
/// RepositoryUta crea/deshabilita la cuenta; HrBackend persiste el estado en tbl_StudentProvisioning.
/// </summary>
public interface IStudentProvisioningClient
{
    /// <summary>
    /// Crea la cuenta AD en OU=Activos,OU=ESTUDIANTES y la añade a EActivos.
    /// Retorna el AdObjectId y email generados por RepositoryUta.
    /// </summary>
    Task<CreateStudentAdAccountResult?> CreateAdAccountAsync(
        CreateStudentAdAccountRequest req,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>
    /// Deshabilita la cuenta AD: desactiva, quita de EActivos, mueve a OU=Inactivos,OU=ESTUDIANTES.
    /// Requiere el AdObjectId almacenado en tbl_StudentProvisioning.
    /// </summary>
    Task<DisableStudentAdAccountResult?> DisableAdAccountAsync(
        string adObjectId,
        string bearerToken,
        CancellationToken ct = default);
}

// ─── DTOs de transferencia HrBackend → RepositoryUta ─────────────────────────

public record CreateStudentAdAccountRequest(
    int HrStudentId,
    string DisplayName,
    string GivenName,
    string Surname,
    string InitialPassword,
    string? IdCard = null,
    string? SourceReference = null,
    bool ForcePasswordChange = true
);

public record CreateStudentAdAccountResult(
    bool Success,
    string? AdObjectId,
    string? Email,
    string? ErrorMessage
);

public record DisableStudentAdAccountResult(
    bool Success,
    string AdObjectId,
    string? ErrorMessage
);
