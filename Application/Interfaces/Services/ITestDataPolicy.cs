namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Decide si los datos de prueba (empleados con <c>IsTestData = 1</c>) son visibles en reportes y listados.
/// </summary>
public interface ITestDataPolicy
{
    /// <summary>
    /// Refleja <c>TestData:Visible</c> (appsettings; false por defecto).
    /// </summary>
    bool IncludeTestData { get; }

    /// <summary>
    /// Empleado del usuario autenticado. Un empleado de prueba siempre puede ver su propio
    /// registro (login, perfil), aunque <see cref="IncludeTestData"/> sea false.
    /// </summary>
    int? CurrentEmployeeId { get; }
}
