using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public sealed class DepartmentsReportService : IDepartmentsReportService
{
    private readonly IDepartmentsReportRepository _repository;
    private readonly ILogger<DepartmentsReportService> _logger;

    public DepartmentsReportService(IDepartmentsReportRepository repository, ILogger<DepartmentsReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<DepartmentReportDto>> GetDepartmentsDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var data = await _repository.GetDepartmentsDataAsync(filter, ct);

        _logger.LogInformation("Reporte de dependencias generado. Total registros: {Count}", data.Count);

        return data;
    }
}
