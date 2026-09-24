using WsUtaSystem.Application.DTOs.AcademicCv;

namespace WsUtaSystem.Reports.Abstractions;

/// <summary>
/// Compone el PDF de la Hoja de Vida Académica. No es un <see cref="IReportRenderer"/>
/// genérico a propósito: la forma de un CV (varias secciones heterogéneas por persona)
/// no encaja en el modelo tabular de <c>ReportDefinition.Columns/Rows</c> que usan los
/// demás reportes — ver análisis previo a la implementación.
/// </summary>
public interface IAcademicCvPdfComposer
{
    byte[] Compose(AcademicCvDto cv);
}
