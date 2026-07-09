using WsUtaSystem.Application.DTOs.AcademicPromotion;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Arma el perfil académico completo de un docente, alternando entre datos mock
/// y datos reales de base de datos según <see cref="Common.Options.AcademicPromotionOptions"/>.
/// </summary>
public interface IAcademicPromotionService
{
    /// <summary>
    /// Busca al docente por identificación (cédula) y arma su perfil académico.
    /// Devuelve null si no existe una persona/empleado con esa identificación.
    /// </summary>
    Task<TeacherAcademicProfileDto?> GetProfileByIdentificationAsync(string identification, CancellationToken ct = default);

    /// <summary>Indica si el usuario autenticado tiene un rol autorizado para consultar este perfil.</summary>
    Task<bool> IsCurrentUserAuthorizedAsync(CancellationToken ct = default);
}
