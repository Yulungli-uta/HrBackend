using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.EmployeePositions;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

public class VwEmployeePositionService : IVwEmployeePositionService
{
    private readonly IVwEmployeePositionRepository _repo;

    public VwEmployeePositionService(IVwEmployeePositionRepository repo) => _repo = repo;

    /// <inheritdoc/>
    public async Task<List<EmployeePositionDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default) =>
        Order(await _repo.GetByEmployeeAsync(employeeId, ct));

    /// <inheritdoc/>
    public async Task<List<EmployeePositionDto>> GetByEmployeesAsync(IReadOnlyCollection<int> employeeIds, CancellationToken ct = default)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        if (ids.Count > IVwEmployeePositionService.MaxEmployees)
            throw new BusinessRuleException($"Solo se pueden consultar hasta {IVwEmployeePositionService.MaxEmployees} empleados por vez.");
        return Order(await _repo.GetByEmployeesAsync(ids, ct));
    }

    /// <summary>La principal primero; luego segundo régimen, autoridad y temporal.</summary>
    private static List<EmployeePositionDto> Order(IEnumerable<VwEmployeePosition> rows) =>
        rows.OrderBy(p => p.EmployeeID)
            .ThenBy(p => p.PositionType switch { "Principal" => 0, "Segundo régimen" => 1, "Autoridad" => 2, _ => 3 })
            .Select(p => new EmployeePositionDto(p.EmployeeID, p.PersonID, p.IDCard, p.FullName, p.PositionType, p.LaborRegimeID, p.LaborRegimeName,
                p.JobID, p.JobName, p.JobBaseName, p.JobSource, p.DepartmentID, p.DepartmentName, p.DedicationCode, p.DedicationName,
                p.DedicationSource, p.ModalityName, p.ContractedHours, p.DocumentNumber, p.StartDate, p.EndDate))
            .ToList();
}
