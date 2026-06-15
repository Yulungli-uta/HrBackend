using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface ITeacherStructureRepository : IRepository<TeacherStructure, int>
{
    /// <summary>Devuelve todas las estructuras activas de un empleado.</summary>
    Task<List<TeacherStructure>> GetByEmployeeAsync(int employeeId, CancellationToken ct);

    /// <summary>Verifica si existe un registro activo solapado en el mismo período para el empleado.</summary>
    Task<bool> HasOverlapAsync(int employeeId, DateOnly startDate, DateOnly? endDate, int? excludeId, CancellationToken ct);
}
