namespace WsUtaSystem.Application.DTOs.Docflow
{
    /// <summary>
    /// DTO de respuesta para una instancia de expediente.
    /// </summary>
    public sealed record InstanceDto(
        Guid InstanceId,
        int ProcessId,
        int? RootProcessId,
        string? InstanceName,
        string CurrentStatus,
        int CurrentDepartmentId,
        int? AssignedToUserId,
        string? DynamicMetadata,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
