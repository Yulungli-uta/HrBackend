namespace WsUtaSystem.Application.DTOs.Provisioning;

/// <summary>Solicitud para aprovisionar un empleado HR en RepositoryUta (AD Local → Entra → O365).</summary>
public record HrProvisionEmployeeRequest(
    int HrEmployeeId,
    string? Email,
    string DisplayName,
    string GivenName,
    string Surname,
    string InitialPassword,
    int EmployeeTypeId,
    string? EmployeeTypeName = null,
    int? DepartmentId = null,
    string? DepartmentName = null,
    string? JobTitle = null,
    string? SourceReference = null,
    bool ForcePasswordChange = true,
    string? PersonalEmail = null,
    /// <summary>Cédula del empleado. Se persiste como atributo employeeID en AD Local.</summary>
    string? IdCard = null
);

/// <summary>Resultado de deshabilitar la cuenta institucional devuelto por RepositoryUta.</summary>
public record HrDisableEmployeeResult(
    bool Success,
    int HrEmployeeId,
    string? Email,
    string? ErrorMessage
);

/// <summary>Resultado resumido del aprovisionamiento devuelto por RepositoryUta.</summary>
public record HrProvisioningResult(
    Guid Id,
    int HrEmployeeId,
    string Email,
    int ProvisioningStatusId,
    string? ProvisioningStatusName,
    string? ErrorMessage,
    bool AlreadyExists = false,
    /// <summary>
    /// Aviso no bloqueante devuelto por RepositoryUta.
    /// Ejemplo: "CN 'María Lozano' ya existía en AD, se usó 'María Lozano 1'."
    /// Null = sin avisos.
    /// </summary>
    string? Warning = null
);
