namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Datos base del empleado/docente resueltos a partir de su identificación.</summary>
public sealed record AcademicPromotionEmployeeLookup(
    int PersonId,
    int EmployeeId,
    string IdCard,
    string FullName,
    int? DepartmentId
);

/// <summary>Departamento resuelto para el bloque "dependency" del perfil académico.</summary>
public sealed record AcademicPromotionDependency(
    int DepartmentId,
    string Name
);

/// <summary>
/// Consultas de solo lectura, específicas del módulo de promoción académica docente,
/// que no tienen un repositorio genérico existente (resolución de persona/empleado por
/// identificación, dependencia/facultad, y nombres de ref_Types por lote).
/// </summary>
public interface IAcademicPromotionRepository
{
    Task<AcademicPromotionEmployeeLookup?> FindEmployeeByIdentificationAsync(string identification, CancellationToken ct = default);

    /// <summary>
    /// Sube por la jerarquía de departamentos (ParentID) desde <paramref name="departmentId"/>
    /// hasta encontrar el primero cuyo DepartmentType corresponda a 'FACULTAD'. Si el propio
    /// departamento ya es de tipo FACULTAD, se devuelve él mismo. Null si no hay departamento
    /// o no se encuentra ninguno de tipo FACULTAD en la cadena.
    /// </summary>
    Task<AcademicPromotionDependency?> FindFacultyDependencyAsync(int? departmentId, CancellationToken ct = default);

    /// <summary>Resuelve en lote (una sola consulta) el Name de ref_Types para un conjunto de TypeIds.</summary>
    Task<IReadOnlyDictionary<int, string>> GetRefTypeNamesAsync(IEnumerable<int?> typeIds, CancellationToken ct = default);

    /// <summary>Indica si el usuario (vw_UserRoles.UserId) tiene alguno de los roles indicados.</summary>
    Task<bool> UserHasAnyRoleAsync(string? userId, IReadOnlyCollection<string> roleNames, CancellationToken ct = default);
}
