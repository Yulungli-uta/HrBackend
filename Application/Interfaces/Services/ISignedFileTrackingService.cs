using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.SignedFileTracking;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Consulta de acciones y contratos con archivo firmado y vista previa del archivo.</summary>
public interface ISignedFileTrackingService
{
    /// <summary>Lista acciones de personal con archivo firmado, aplicando el alcance por departamento del usuario.</summary>
    /// <param name="filter">Filtros y paginación (la restricción de departamento la calcula el servicio).</param>
    /// <param name="employeeId">Empleado autenticado, para resolver su alcance; null = sin restricción.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<PersonnelActionSignedFileDto>> GetActionsAsync(SignedFileTrackingFilter filter, int? employeeId, CancellationToken ct);

    /// <summary>Lista contratos con archivo firmado, aplicando el alcance por departamento del usuario.</summary>
    /// <param name="filter">Filtros y paginación (la restricción de departamento la calcula el servicio).</param>
    /// <param name="employeeId">Empleado autenticado, para resolver su alcance; null = sin restricción.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<ContractSignedFileDto>> GetContractsAsync(SignedFileTrackingFilter filter, int? employeeId, CancellationToken ct);

    /// <summary>Lee el archivo firmado de una acción de personal para previsualizarlo.</summary>
    /// <param name="actionId">Identificador de la acción.</param>
    /// <param name="employeeId">Empleado autenticado, para validar el alcance; null = sin restricción.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<SignedFilePreviewResult> PreviewActionFileAsync(int actionId, int? employeeId, CancellationToken ct);

    /// <summary>Lee el archivo firmado de un contrato para previsualizarlo.</summary>
    /// <param name="contractId">Identificador del contrato.</param>
    /// <param name="employeeId">Empleado autenticado, para validar el alcance; null = sin restricción.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<SignedFilePreviewResult> PreviewContractFileAsync(int contractId, int? employeeId, CancellationToken ct);
}
