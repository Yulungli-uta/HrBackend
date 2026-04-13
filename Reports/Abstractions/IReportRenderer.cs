using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Abstractions;

/// <summary>
/// Define el contrato para los renderizadores de reportes.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: cada implementación tiene una única responsabilidad —
/// convertir una <see cref="ReportDefinition"/> en bytes del formato correspondiente.
/// No sabe nada de empleados, asistencia ni departamentos.
/// </para>
/// <para>
/// Principio DIP: el <c>IReportServiceV2</c> depende de esta abstracción,
/// no de <c>PdfReportRenderer</c> ni <c>ExcelReportRenderer</c> directamente.
/// </para>
/// <para>
/// Implementaciones previstas:
/// <list type="bullet">
///   <item><description><c>PdfReportRenderer</c> — usa QuestPDF.</description></item>
///   <item><description><c>ExcelReportRenderer</c> — usa ClosedXML u OpenXML.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IReportRenderer
{
    /// <summary>
    /// Formato de exportación que este renderer produce.
    /// Usado por el <c>IReportServiceV2</c> para seleccionar el renderer correcto.
    /// </summary>
    ReportFormat Format { get; }

    /// <summary>
    /// Convierte la <see cref="ReportDefinition"/> en un arreglo de bytes
    /// del formato correspondiente.
    /// </summary>
    /// <param name="definition">
    /// Definición completa del reporte con título, columnas y filas.
    /// </param>
    /// <returns>
    /// Arreglo de bytes listo para ser enviado como respuesta HTTP
    /// o almacenado en disco.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la definición no contiene columnas o filas válidas.
    /// </exception>
    Task<byte[]> RenderAsync(ReportDefinition definition);
}
