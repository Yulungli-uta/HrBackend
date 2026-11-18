namespace WsUtaSystem.Application.Common;

/// <summary>
/// Ranuras lógicas de archivos versionados dentro de un registro (HR.tbl_StoredFileVersions.SlotCode).
/// Agregar una nueva ranura aquí no requiere cambios de esquema.
/// </summary>
public static class StoredFileSlots
{
    /// <summary>Documento firmado final de una acción de personal o de un contrato.</summary>
    public const string Signed = "SIGNED";

    /// <summary><c>EntityType</c> de <c>StoredFile</c> y de las versiones para acciones de personal.</summary>
    public const string EntityTypePersonnelAction = "PersonnelAction";

    /// <summary><c>EntityType</c> de <c>StoredFile</c> y de las versiones para contratos.</summary>
    public const string EntityTypeContract = "HRCONTRACT";
}
