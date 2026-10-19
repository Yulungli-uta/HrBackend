namespace WsUtaSystem.Application.Interfaces.Guards;

/// <summary>Consulta si un empleado es guardia (planifica turnos rotativos).</summary>
public interface IGuardMembershipRepository
{
    /// <summary>
    /// Es guardia quien tiene el cargo de guardia de seguridad o es miembro activo de un grupo de
    /// rotación en la fecha indicada.
    /// </summary>
    Task<bool> IsGuardAsync(int employeeId, DateOnly onDate, CancellationToken ct = default);
}
