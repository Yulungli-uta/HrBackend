using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Endpoints para disparar manualmente el aprovisionamiento AD/O365
/// de un empleado a partir de un contrato existente.
///
/// Útil cuando:
///   - El trigger automático (carga de documento firmado) falló
///   - El administrador desea re-intentar el aprovisionamiento desde el panel
///
/// Delega toda la lógica al <see cref="IEmployeeProvisioningOrchestrator"/>,
/// que centraliza: EnsureEmployee + RepositoryUta + UpdateEmail + SendEmail.
/// </summary>
[ApiController, Route("contracts")]
public class ContractProvisioningController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmployeeProvisioningOrchestrator _orchestrator;
    private readonly ILogger<ContractProvisioningController> _logger;

    public ContractProvisioningController(
        AppDbContext db,
        IEmployeeProvisioningOrchestrator orchestrator,
        ILogger<ContractProvisioningController> logger)
    {
        _db          = db;
        _orchestrator = orchestrator;
        _logger      = logger;
    }

    /// <summary>
    /// Dispara manualmente el aprovisionamiento AD/O365 para el empleado
    /// asociado al contrato indicado.
    /// </summary>
    /// <param name="contractId">ID del contrato. Debe existir y tener PersonID válido.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("{contractId:int}/provision")]
    public async Task<IActionResult> TriggerProvisioning(int contractId, CancellationToken ct)
    {
        // Cargar contrato
        var contract = await _db.Set<Contracts>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct);

        if (contract is null)
            return NotFound(new { success = false, message = $"Contrato {contractId} no encontrado" });

        // Verificar que la persona tiene correo personal (validación anticipada)
        var person = await _db.People
            .AsNoTracking()
            .Where(p => p.PersonId == contract.PersonID)
            .Select(p => new { p.Email })
            .FirstOrDefaultAsync(ct);

        if (person is null || string.IsNullOrWhiteSpace(person.Email))
            return BadRequest(new
            {
                success = false,
                message = "La persona no tiene correo personal registrado. " +
                          "El aprovisionamiento requiere correo personal para notificar credenciales."
            });

        // Leer tipo de empleado (si ya existe el registro)
        var empType = await _db.Employees
            .AsNoTracking()
            .Where(e => e.PersonID == contract.PersonID)
            .Select(e => (int?)e.EmployeeType)
            .FirstOrDefaultAsync(ct) ?? 0;

        // Leer nombre del departamento
        string? deptName = null;
        if (contract.DepartmentID > 0)
            deptName = await _db.Departments
                .AsNoTracking()
                .Where(d => d.DepartmentId == contract.DepartmentID)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct);

        var token = Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;

        _logger.LogInformation(
            "[CONTRACT-MANUAL] Iniciando aprovisionamiento manual. ContractId={ContractId} | PersonId={PersonId}",
            contractId, contract.PersonID);

        // Delegar al orquestador: EnsureEmployee + RepositoryUta + UpdateEmail + SendEmail
        var result = await _orchestrator.ExecuteAsync(new ProvisioningOrchestrationRequest(
            PersonId:        contract.PersonID,
            EmployeeType:    empType,
            DepartmentId:    contract.DepartmentID > 0 ? contract.DepartmentID : null,
            DepartmentName:  deptName,
            HireDate:        null,
            JobId:           null,
            UpdatedBy:       0,
            BearerToken:     token,
            SourceReference: $"Contract:{contractId}",
            Source:          ProvisioningSource.Manual
        ), ct);

        if (result.AlreadyExists)
            return Conflict(new
            {
                success = false,
                alreadyExists = true,
                message = "La cuenta institucional ya existe para este empleado.",
                email = result.InstitutionalEmail
            });

        if (!result.Success)
            return StatusCode(502, new
            {
                success = false,
                message = result.ErrorMessage ?? "Error en el aprovisionamiento",
                provisioningStatus = result.ProvisioningStatusName
            });

        return Ok(new
        {
            success            = true,
            employeeId         = result.EmployeeId,
            institutionalEmail = result.InstitutionalEmail,
            provisioningStatus = result.ProvisioningStatusName,
            // warning es null cuando todo fue limpio; presente cuando el CN se ajustó
            warning            = result.Warning,
            message            = result.Warning is null
                ? "Cuenta institucional creada y correo de bienvenida enviado."
                : $"Cuenta institucional creada con aviso: {result.Warning}"
        });
    }
}
