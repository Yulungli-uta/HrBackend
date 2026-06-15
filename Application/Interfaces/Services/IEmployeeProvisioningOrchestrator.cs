using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

// ══════════════════════════════════════════════════════════════════════════════
// IEmployeeProvisioningOrchestrator
// ══════════════════════════════════════════════════════════════════════════════
//
// Centraliza el flujo completo de aprovisionamiento de cuenta institucional
// para empleados nuevos. Sustituye la lógica duplicada que existía en:
//   - ContractsService.TriggerProvisioningAsync
//   - PersonnelActionService.TriggerActionProvisioningAsync
//   - ContractProvisioningController.TriggerProvisioning
//
// FLUJO INTERNO:
//   [1] EnsureEmployee  → garantiza registro en hr.tbl_Employees (crea si no existe)
//   [2] ValidarPerson   → lee correo personal y nombres desde hr.tbl_People
//   [3] RepositoryUta   → crea cuenta AD Local → Entra ID → O365
//                         (RepositoryUta también crea: auth.tbl_Users,
//                          auth.tbl_UserEmployees, auth.tbl_UserRoles, grupo AD)
//   [4] UpdateEmail     → actualiza hr.tbl_Employees.Email institucional
//   [5] SendEmail       → envía correo de bienvenida al correo personal
//
// CUÁNDO USAR:
//   ✓ Carga de documento firmado en CONTRATO con RequiresAdUserCreation = true
//   ✓ Carga de documento firmado en ACCIÓN DE PERSONAL con RequiresAdUserCreation = true
//   ✓ Trigger manual desde panel de administración (ContractProvisioningController)
//
// CUÁNDO NO USAR:
//   ✗ Renovaciones o adendas de contratos (el usuario ya existe)
//   ✗ Cambios de departamento / movimientos sin creación de cuenta
//   ✗ Re-aprovisionamiento de cuentas ya existentes (use el retry de RepositoryUta)
//
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Orquestador reutilizable del flujo de aprovisionamiento de cuenta institucional.
/// Ver comentario de archivo para descripción completa del flujo y casos de uso.
/// </summary>
public interface IEmployeeProvisioningOrchestrator
{
    /// <summary>
    /// Ejecuta el flujo completo de aprovisionamiento de cuenta institucional
    /// para un empleado nuevo.
    /// </summary>
    /// <param name="request">Datos del empleado y contexto del trigger.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>
    /// <see cref="OrchestratorResult"/> con el email institucional generado,
    /// estado final y bandera de éxito.
    /// </returns>
    Task<OrchestratorResult> ExecuteAsync(
        ProvisioningOrchestrationRequest request,
        CancellationToken ct = default);
}

// ── DTOs del orquestador ──────────────────────────────────────────────────────

/// <summary>
/// Origen que disparó el aprovisionamiento.
/// Determina la plantilla de correo a usar desde HR.TBL_PARAMETERS.
/// </summary>
public enum ProvisioningSource
{
    /// <summary>
    /// Carga de documento firmado en un contrato.
    /// Plantilla: EMAIL_TEMPLATE_ACCOUNT_CREATED_CONTRACT
    /// </summary>
    Contract,

    /// <summary>
    /// Carga de documento firmado en una acción de personal.
    /// Plantilla: EMAIL_TEMPLATE_ACCOUNT_CREATED_ACTION
    /// </summary>
    PersonnelAction,

    /// <summary>
    /// Trigger manual desde el panel de administración.
    /// Usa la misma plantilla que Contract.
    /// </summary>
    Manual
}

/// <summary>
/// Datos de entrada para el orquestador de aprovisionamiento.
/// </summary>
/// <param name="PersonId">
/// ID de la persona en <c>hr.tbl_People</c>. El orquestador busca aquí
/// el correo personal y los nombres del empleado.
/// </param>
/// <param name="EmployeeType">
/// TypeId del tipo de empleado (hr.ref_Types). Requerido para crear
/// el registro en <c>hr.tbl_Employees</c> si no existe.
/// </param>
/// <param name="DepartmentId">ID del departamento. Null si no aplica.</param>
/// <param name="DepartmentName">Nombre del departamento para RepositoryUta.</param>
/// <param name="HireDate">
/// Fecha de contratación para crear <c>hr.tbl_Employees</c>.
/// Null = usa la fecha del día actual.
/// </param>
/// <param name="JobId">ID del cargo en <c>hr.tbl_Jobs</c>. Null si no aplica.</param>
/// <param name="ImmediateBossId">
/// EmployeeId del jefe inmediato, resuelto desde <c>hr.tbl_DepartmentAuthorities</c>
/// (Director TypeId=237 o Decano TypeId=235). Null si no se encontró autoridad activa.
/// </param>
/// <param name="UpdatedBy">EmployeeId del usuario que dispara la acción (auditoría).</param>
/// <param name="BearerToken">
/// Token JWT del HTTP request actual. Se reenvía a RepositoryUta para autenticar
/// la llamada de aprovisionamiento.
/// </param>
/// <param name="SourceReference">
/// Referencia trazable al origen, ej: <c>"Contract:123"</c>, <c>"PersonnelAction:456"</c>.
/// </param>
/// <param name="Source">
/// Fuente del trigger. Determina la plantilla de correo que se usará.
/// </param>
public record ProvisioningOrchestrationRequest(
    int PersonId,
    int EmployeeType,
    int? DepartmentId,
    string? DepartmentName,
    DateOnly? HireDate,
    int? JobId,
    int UpdatedBy,
    string BearerToken,
    string SourceReference,
    ProvisioningSource Source,
    int? ImmediateBossId = null
);

/// <summary>
/// Resultado del flujo de aprovisionamiento orquestado.
/// </summary>
/// <param name="Success">
/// True si la cuenta fue creada exitosamente en AD Local (status 2002–2006).
/// False si hubo error o la cuenta ya existía.
/// </param>
/// <param name="AlreadyExists">
/// True si la cuenta ya existía en RepositoryUta (HTTP 409).
/// En este caso el proceso no reintenta la creación.
/// </param>
/// <param name="InstitutionalEmail">
/// Email institucional generado (ej: <c>m.lozano@uta.edu.ec</c>).
/// Null si el aprovisionamiento falló.
/// </param>
/// <param name="EmployeeId">
/// ID en <c>hr.tbl_Employees</c> (nuevo o existente). Null si falló antes de EnsureEmployee.
/// </param>
/// <param name="ProvisioningStatusName">Estado final devuelto por RepositoryUta.</param>
/// <param name="ErrorMessage">Descripción del error si <c>Success = false</c>.</param>
/// <param name="Warning">
/// Aviso no bloqueante cuando <c>Success = true</c> pero ocurrió algo que
/// el administrador debe conocer. Ejemplo: el CN fue ajustado automáticamente
/// porque ya existía un usuario con el mismo nombre en AD Local.
/// Null = aprovisionamiento limpio sin advertencias.
/// </param>
public record OrchestratorResult(
    bool Success,
    bool AlreadyExists,
    string? InstitutionalEmail,
    int? EmployeeId,
    string? ProvisioningStatusName,
    string? ErrorMessage,
    string? Warning = null
);
