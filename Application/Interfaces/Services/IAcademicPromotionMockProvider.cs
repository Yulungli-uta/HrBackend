using WsUtaSystem.Application.DTOs.AcademicPromotion;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Genera un perfil académico docente completo y realista (datos sintéticos,
/// nunca persistidos), usado cuando <see cref="Common.Options.AcademicPromotionOptions.UseMockData"/> es true.
/// </summary>
public interface IAcademicPromotionMockProvider
{
    /// <summary>Arma el perfil mock para el escenario Principal 2 → Principal 3 (cumple todos los requisitos).</summary>
    TeacherAcademicProfileDto GetProfile(string identification);
}
