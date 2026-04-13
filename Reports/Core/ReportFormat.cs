namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Define los formatos de exportación soportados por el sistema de reportes.
/// </summary>
/// <remarks>
/// Principio OCP: agregar un nuevo formato (p.ej. <c>Csv</c>) solo requiere
/// añadir el valor aquí y crear su <c>IReportRenderer</c> correspondiente.
/// </remarks>
public enum ReportFormat
{
    /// <summary>Formato PDF generado con QuestPDF.</summary>
    Pdf = 1,

    /// <summary>Formato Excel (.xlsx) generado con ClosedXML u OpenXML.</summary>
    Excel = 2
}
