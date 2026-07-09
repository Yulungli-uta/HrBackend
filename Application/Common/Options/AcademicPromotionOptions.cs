namespace WsUtaSystem.Application.Common.Options;

/// <summary>
/// Configuración del módulo de perfil académico docente (academic-promotion).
/// Sección appsettings.json: "AcademicPromotion".
/// </summary>
public class AcademicPromotionOptions
{
    /// <summary>
    /// Si es true, el endpoint responde con datos de prueba generados en código
    /// (IAcademicPromotionMockProvider) en vez de consultar la base de datos real.
    /// </summary>
    public bool UseMockData { get; set; }
}
