namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Ubicación física decidida para un archivo: carpeta relativa al directorio, nombre y año del documento.</summary>
/// <param name="RelativeFolder">Carpeta relativa al directorio parametrizado, sin barras al inicio ni al final (por ejemplo "2021/anexos/FCS-DTH-076-2021").</param>
/// <param name="FileName">Nombre del archivo, ya limpio de caracteres que Windows no acepta.</param>
/// <param name="Year">Año del documento (carpeta de año y <c>UploadYear</c>).</param>
public sealed record DocumentLayout(string RelativeFolder, string FileName, int Year);

/// <summary>
/// Política de ubicación de los archivos de contratos y acciones de personal. Decide, según el rol del archivo,
/// en qué carpeta y con qué nombre se guarda:
/// <list type="bullet">
/// <item><c>signed</c>: <c>{año}/{número}.pdf</c></item>
/// <item><c>attachment</c>: <c>{año}/anexos/{número}/{TIPO}.pdf</c></item>
/// <item><c>cancellation</c>: <c>{año}/anulacion/{número}.pdf</c></item>
/// <item><c>resignation</c>: <c>{año}/renuncia/{número}.pdf</c></item>
/// </list>
/// El año sale de la fecha del documento (inicio del contrato o fecha de la acción); si esa fecha difiere más de un año del año del
/// número se usa el año del número.
/// </summary>
public interface IEntityDocumentLayoutPolicy
{
    /// <summary>
    /// Resuelve la ubicación para un archivo de <c>HRCONTRACT</c> o <c>PersonnelAction</c>. Devuelve null cuando la política no aplica
    /// (otro tipo de entidad, id inválido o registro inexistente): quien llama conserva el comportamiento anterior.
    /// </summary>
    Task<DocumentLayout?> ResolveAsync(
        string entityType,
        string entityId,
        string? documentRole,
        int? documentTypeId,
        string originalFileName,
        CancellationToken ct = default);
}
