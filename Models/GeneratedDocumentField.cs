namespace WsUtaSystem.Models;

/// <summary>
/// Snapshot inmutable de los valores de los campos al momento de generar el documento.
/// Permite reconstruir el documento exactamente como fue generado originalmente.
/// </summary>
/// <remarks>
/// Esta tabla no implementa <c>IAuditable</c> porque es un registro de detalle
/// inmutable — nunca se actualiza, solo se inserta junto con el documento padre.
/// </remarks>
public sealed class GeneratedDocumentField
{
    /// <summary>Clave primaria autoincremental.</summary>
    public int DocumentFieldId { get; set; }

    /// <summary>FK al documento generado padre.</summary>
    public int DocumentId { get; set; }

    /// <summary>Nombre del placeholder (sin llaves). Ej: <c>NOMBRE_EMPLEADO</c>.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Valor resuelto que fue sustituido en el placeholder.</summary>
    public string? FieldValue { get; set; }

    /// <summary>Origen del valor (SYSTEM, EMPLOYEE, CONTRACT, MOVEMENT, MANUAL).</summary>
    public string SourceType { get; set; } = "SYSTEM";

    /// <summary>Indica si el valor fue sobreescrito manualmente por el usuario.</summary>
    public bool WasOverridden { get; set; } = false;

    // ── Navegación ──────────────────────────────────────────────────────────────
    /// <summary>Documento al que pertenece este campo.</summary>
    public GeneratedDocument? Document { get; set; }
}
