namespace WsUtaSystem.Application.Common;

/// <summary>
/// Roles de un archivo dentro de un contrato o una acción de personal. Deciden la carpeta y el nombre
/// (ver <c>IEntityDocumentLayoutPolicy</c>).
/// </summary>
public static class DocumentRoles
{
    /// <summary>Documento firmado: el contrato o la acción de personal en sí.</summary>
    public const string Signed = "signed";

    /// <summary>Anexo (certificados, memorandos, resoluciones, etc.). Es el rol por defecto.</summary>
    public const string Attachment = "attachment";

    /// <summary>Oficio o documento de anulación.</summary>
    public const string Cancellation = "cancellation";

    /// <summary>Documento de renuncia.</summary>
    public const string Resignation = "resignation";

    /// <summary>Normaliza un rol recibido del cliente; devuelve null si no es uno de los válidos.</summary>
    public static string? Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        var r = role.Trim().ToLowerInvariant();
        return r is Signed or Attachment or Cancellation or Resignation ? r : null;
    }
}
