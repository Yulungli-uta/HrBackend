namespace WsUtaSystem.Application.Common.Options;

/// <summary>
/// Configuración de visibilidad de los datos de prueba (empleados con <c>IsTestData = 1</c>).
/// Sección appsettings.json: "TestData".
/// </summary>
public class TestDataOptions
{
    /// <summary>
    /// Si es true, los empleados de prueba aparecen en reportes y listados. Por defecto false;
    /// se activa solo en el appsettings del entorno de desarrollo.
    /// </summary>
    public bool Visible { get; set; }
}
