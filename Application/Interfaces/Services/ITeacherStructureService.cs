using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface ITeacherStructureService
{
    /// <summary>Devuelve el listado paginado según filtros.</summary>
    Task<PagedResult<TeacherStructureDto>> GetPagedAsync(TeacherStructureFilterDto filter, CancellationToken ct);

    /// <summary>Devuelve todas las estructuras docentes de un empleado.</summary>
    Task<List<TeacherStructureDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct);

    /// <summary>Devuelve una estructura docente por su Id o null si no existe.</summary>
    Task<TeacherStructureDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Crea una nueva estructura docente.</summary>
    Task<TeacherStructureDto> CreateAsync(TeacherStructureCreateDto dto, CancellationToken ct);

    /// <summary>Actualiza una estructura docente existente.</summary>
    Task<TeacherStructureDto> UpdateAsync(int id, TeacherStructureUpdateDto dto, CancellationToken ct);

    /// <summary>Inactiva (soft-delete) una estructura docente.</summary>
    Task DeactivateAsync(int id, CancellationToken ct);
}
