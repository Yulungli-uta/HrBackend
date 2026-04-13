using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Abstractions;

/// <summary>
/// Define el contrato que debe implementar cada origen de datos de reporte.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: cada implementación tiene una única responsabilidad —
/// consultar los datos de su dominio y construir la <see cref="ReportDefinition"/>
/// correspondiente. No sabe nada de PDF ni Excel.
/// </para>
/// <para>
/// Principio OCP: para agregar un nuevo reporte al sistema basta con crear
/// una nueva clase que implemente esta interfaz y registrarla en el contenedor DI.
/// No se modifica ningún código existente.
/// </para>
/// <para>
/// Principio LSP: cualquier implementación puede sustituirse por otra sin
/// que el <c>IReportServiceV2</c> cambie su comportamiento.
/// </para>
/// <para>
/// Ejemplo de implementaciones:
/// <list type="bullet">
///   <item><description><c>EmployeesReportSource</c></description></item>
///   <item><description><c>AttendanceReportSource</c></description></item>
///   <item><description><c>DepartmentsReportSource</c></description></item>
///   <item><description><c>AttendanceSummaryReportSource</c></description></item>
/// </list>
/// </para>
/// </remarks>
public interface IReportSource
{
    /// <summary>
    /// Identifica el tipo de reporte que esta fuente puede construir.
    /// Debe ser único entre todas las implementaciones registradas en DI.
    /// </summary>
    ReportType ReportType { get; }

    /// <summary>
    /// Consulta los datos necesarios y construye la <see cref="ReportDefinition"/>
    /// lista para ser renderizada por cualquier <see cref="IReportRenderer"/>.
    /// </summary>
    /// <param name="filter">Filtros aplicados por el usuario (fechas, departamento, etc.).</param>
    /// <param name="context">
    /// Contexto HTTP actual. Se usa para obtener la identidad del usuario
    /// que solicita el reporte (auditoría).
    /// </param>
    /// <returns>
    /// Una <see cref="ReportDefinition"/> completamente construida con
    /// título, columnas y filas de datos.
    /// </returns>
    Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context);
}
