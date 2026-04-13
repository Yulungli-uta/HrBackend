using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Helpers;

/// <summary>
/// Centraliza la construcción de la respuesta de vista previa (preview) de reportes.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — convertir bytes de PDF
/// en la respuesta <see cref="PreviewResponseDto"/> estandarizada que el frontend espera.
/// </para>
/// <para>
/// Antes de esta clase, cada endpoint construía manualmente el base64, el nombre del archivo
/// y el tipo MIME. Este helper elimina esa duplicación.
/// </para>
/// </remarks>
public static class ReportPreviewResponseBuilder
{
    /// <summary>
    /// Construye una respuesta de preview exitosa a partir de los bytes del PDF generado.
    /// </summary>
    /// <param name="pdfBytes">Bytes del PDF generado por el renderer.</param>
    /// <param name="definition">
    /// Definición del reporte usada para construir el nombre del archivo.
    /// </param>
    /// <returns>
    /// <see cref="PreviewResponseDto"/> con <c>Status = "success"</c> y los datos base64.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="pdfBytes"/> o <paramref name="definition"/> son nulos.
    /// </exception>
    public static PreviewResponseDto BuildSuccess(byte[] pdfBytes, ReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(definition);

        var fileName  = ReportFileNameBuilder.Build(definition, ReportFormat.Pdf);
        var mimeType  = ReportFileNameBuilder.GetMimeType(ReportFormat.Pdf);
        var base64    = Convert.ToBase64String(pdfBytes);

        return new PreviewResponseDto(
            Status: "success",
            Data: new PdfPreviewDataDto(
                Base64Data: base64,
                FileName:   fileName,
                MimeType:   mimeType
            ),
            Error: null
        );
    }

    /// <summary>
    /// Construye una respuesta de preview de error estandarizada.
    /// </summary>
    /// <param name="message">Mensaje de error descriptivo para el cliente.</param>
    /// <returns>
    /// <see cref="PreviewResponseDto"/> con <c>Status = "error"</c> y el mensaje de error.
    /// </returns>
    public static PreviewResponseDto BuildError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new PreviewResponseDto(
            Status: "error",
            Data:   null,
            Error:  new PreviewErrorDto(message)
        );
    }
}
