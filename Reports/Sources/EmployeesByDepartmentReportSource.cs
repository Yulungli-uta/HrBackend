using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte detallado de empleados filtrado por dependencia
/// y opcionalmente por tipo de empleado.
/// </summary>
    public sealed class EmployeesByDepartmentReportSource : IReportSource
    {
        private readonly IvwEmployeeDetailsService _employeeDetailsService;
        private readonly ILogger<EmployeesByDepartmentReportSource> _logger;

        public ReportType ReportType => ReportType.EmployeesByDepartment;

        private const string ColEmployeeId = "employee_id";
        private const string ColIdCard = "id_card";
        private const string ColFullName = "full_name";
        private const string ColEmail = "email";
        private const string ColEmployeeType = "employee_type";
        private const string ColDepartment = "department";
        private const string ColContractType = "contract_type";
        private const string ColSchedule = "schedule";

        private static readonly IReadOnlyList<ReportColumn> _columns =
        [
            new(ColEmployeeId,   "ID",               Width: 1.0f, Alignment: ColumnAlignment.Right),
            new(ColIdCard,       "Cédula",           Width: 1.4f),
            new(ColFullName,     "Nombre Completo",  Width: 2.8f),
            new(ColEmail,        "Correo",           Width: 2.3f),
            new(ColEmployeeType, "Tipo Empleado",    Width: 1.5f),
            new(ColDepartment,   "Dependencia",      Width: 1.8f),
            new(ColContractType, "Tipo Contrato",    Width: 1.6f),
            new(ColSchedule,     "Horario",          Width: 1.4f)
        ];

        public EmployeesByDepartmentReportSource(
            IvwEmployeeDetailsService employeeDetailsService,
            ILogger<EmployeesByDepartmentReportSource> logger)
        {
            _employeeDetailsService = employeeDetailsService ?? throw new ArgumentNullException(nameof(employeeDetailsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(context);

            _logger.LogInformation(
                "Building EmployeesByDepartment report. DepartmentId={DepartmentId}, EmployeeTypeId={EmployeeTypeId}",
                filter.DepartmentId,
                filter.EmployeeTypeId);

            var employees = (await _employeeDetailsService.GetByFiltersAsync(
                filter.DepartmentId,
                filter.EmployeeTypeId,
                filter.LaborRegimeId,
                CancellationToken.None)).ToList();

            _logger.LogInformation(
                "EmployeesByDepartment report: {Count} records retrieved.",
                employees.Count);

            var subtitle = BuildSubtitle(filter, employees);

            return new ReportDefinition
            {
                Title = "Reporte de Empleados por Dependencia",
                FilePrefix = "Reporte_Empleados_Dependencia",
                Subtitle = subtitle,
                GeneratedBy = context.User.Identity?.Name ?? "anonymous",
                GeneratedAt = DateTime.Now,
                Columns = _columns,
                Rows = BuildRows(employees),
                Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
                VerticalHeaders = filter.VerticalHeaders ?? false,
                RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
            };
        }

        private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
            IReadOnlyList<VwEmployeeDetails> employees)
        {
            var rows = new List<IReadOnlyDictionary<string, object?>>(employees.Count);

            foreach (var emp in employees)
            {
                rows.Add(new Dictionary<string, object?>
                {
                    [ColEmployeeId] = emp.EmployeeID,
                    [ColIdCard] = emp.IDCard,
                    [ColFullName] = emp.FullName,
                    [ColEmail] = emp.Email,
                    [ColEmployeeType] = string.IsNullOrWhiteSpace(emp.ContractType) ? "Sin tipo" : emp.ContractType,
                    [ColDepartment] = string.IsNullOrWhiteSpace(emp.Department) ? "Sin dependencia" : emp.Department,
                    [ColContractType] = string.IsNullOrWhiteSpace(emp.ContractType) ? "Sin contrato" : emp.ContractType,
                    [ColSchedule] = string.IsNullOrWhiteSpace(emp.Schedule) ? "Sin horario" : emp.Schedule
                });
            }

            return rows;
        }

        private static string BuildSubtitle(
            ReportFilterDto filter,
            IReadOnlyList<VwEmployeeDetails> employees)
        {
            var parts = new List<string>();

            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            {
                var departmentName = employees
                    .Select(x => x.Department)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                parts.Add($"Dependencia: {departmentName ?? $"ID {filter.DepartmentId.Value}"}");
            }
            else
            {
                parts.Add("Dependencia: Todas");
            }

            if (filter.EmployeeTypeId.HasValue && filter.EmployeeTypeId.Value > 0)
            {
                // Resuelve el nombre desde el propio resultado (ya trae ContractType
                // correctamente unido a ref_Types) en vez de un mapeo hardcodeado que
                // nunca coincidía con los valores reales del catálogo (bug encontrado
                // 2026-08-11, mostraba el código crudo en vez de la descripción).
                var typeName = employees
                    .Where(x => x.EmployeeType == filter.EmployeeTypeId.Value)
                    .Select(x => x.ContractType)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                parts.Add($"Tipo Empleado: {typeName ?? filter.EmployeeTypeId.Value.ToString()}");
            }
            else
                parts.Add("Tipo Empleado: Todos");

            parts.Add($"Total registros: {employees.Count}");

            return string.Join(" | ", parts);
        }
    }

