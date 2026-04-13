using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Abstractions;

/// <summary>
/// Servicio central genérico de generación de reportes (v2).
/// </summary>
/// <remarks>
/// <para>
/// Reemplaza al <c>IReportService</c> original que tenía un método por cada
/// combinación de tipo de reporte y formato (8 métodos para 4 reportes × 2 formatos).
/// </para>
/// <para>
/// Principio OCP: agregar un nuevo reporte no requiere modificar esta interfaz.
/// Solo se necesita un nuevo <see cref="IReportSource"/> y registrarlo en DI.
/// </para>
/// <para>
/// Principio ISP: esta interfaz expone únicamente los métodos que los endpoints
/// necesitan. La lógica de selección de source y renderer queda encapsulada
/// en la implementación <c>ReportServiceV2</c>.
/// </para>
/// </remarks>
public interface IReportServiceV2
{
    /// <summary>
    /// Genera el reporte en formato PDF y devuelve los bytes resultantes.
    /// </summary>
    /// <param name="reportType">Tipo de reporte a generar.</param>
    /// <param name="filter">Filtros aplicados por el usuario.</param>
    /// <param name="context">Contexto HTTP para auditoría e identidad del usuario.</param>
    /// <returns>Bytes del PDF generado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si no existe un <see cref="IReportSource"/> registrado para el tipo indicado.
    /// </exception>
    Task<byte[]> GeneratePdfAsync(ReportType reportType, ReportFilterDto filter, HttpContext context);

    /// <summary>
    /// Genera el reporte en formato Excel (.xlsx) y devuelve los bytes resultantes.
    /// </summary>
    /// <param name="reportType">Tipo de reporte a generar.</param>
    /// <param name="filter">Filtros aplicados por el usuario.</param>
    /// <param name="context">Contexto HTTP para auditoría e identidad del usuario.</param>
    /// <returns>Bytes del archivo Excel generado.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si no existe un <see cref="IReportSource"/> registrado para el tipo indicado.
    /// </exception>
    Task<byte[]> GenerateExcelAsync(ReportType reportType, ReportFilterDto filter, HttpContext context);

    /// <summary>
    /// Genera el reporte en el formato especificado y devuelve los bytes resultantes.
    /// Método unificado que delega internamente a <see cref="GeneratePdfAsync"/> o
    /// <see cref="GenerateExcelAsync"/> según el formato indicado.
    /// </summary>
    /// <param name="reportType">Tipo de reporte a generar.</param>
    /// <param name="format">Formato de exportación.</param>
    /// <param name="filter">Filtros aplicados por el usuario.</param>
    /// <param name="context">Contexto HTTP para auditoría e identidad del usuario.</param>
    /// <returns>Bytes del archivo generado.</returns>
    Task<byte[]> GenerateAsync(ReportType reportType, ReportFormat format, ReportFilterDto filter, HttpContext context);
}
