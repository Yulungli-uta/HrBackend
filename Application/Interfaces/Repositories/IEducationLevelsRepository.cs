using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IEducationLevelsRepository : IRepository<EducationLevels, int>
{
    Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId);

    /// <summary>
    /// Un renglón por docente activo (Titular + Ocasional, mismo criterio que
    /// HR.vw_SiiesProfesores) con el Nivel/Grado de su título de mayor jerarquía y su
    /// departamento — para el gráfico "Docentes activos por grado y nivel" del Dashboard de
    /// Talento Humano, filtrable en el frontend por Nivel/Grado/Departamento o Facultad.
    /// Nivel=null representa docentes activos sin ningún título cargado.
    /// </summary>
    Task<IReadOnlyList<(int EmployeeId, int? DepartmentId, string? Nivel, string? Grado)>> GetActiveProfessorStatsAsync(CancellationToken ct = default);
}
