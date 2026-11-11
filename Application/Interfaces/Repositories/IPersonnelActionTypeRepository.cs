using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IPersonnelActionTypeRepository : IRepository<PersonnelActionType, int>
{
    /// <summary>Obtiene todos los tipos activos ordenados por nombre.</summary>
    Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Consume el siguiente número de secuencia para el año dado.
    /// Usa una transacción serializable para evitar duplicados concurrentes.
    /// </summary>
    Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int personnelActionTypeId,
        int year,
        CancellationToken ct = default);

    /// <summary>Actualiza únicamente la plantilla predeterminada de un tipo de acción, sin afectar el resto de campos.</summary>
    Task SetDefaultTemplateAsync(int personnelActionTypeId, int? templateId, CancellationToken ct = default);

    /// <summary>Indica si el tipo tiene tipos hijos (es un grupo, ej. "Licencia").</summary>
    Task<bool> HasChildrenAsync(int personnelActionTypeId, CancellationToken ct = default);

    /// <summary>
    /// Id de la raíz de la familia del tipo (su padre si lo tiene, o él mismo). La jerarquía es de un solo nivel.
    /// Devuelve <c>null</c> si el tipo no existe.
    /// </summary>
    Task<int?> GetRootTypeIdAsync(int personnelActionTypeId, CancellationToken ct = default);

    /// <summary>
    /// Aplica cambios sobre un tipo existente modificando solo las propiedades que <paramref name="apply"/> toca.
    /// A diferencia de <c>UpdateAsync</c> genérico (que copia todas las propiedades), no pisa el contador de
    /// numeración (<c>NumberingYear</c>/<c>NumberingLastSequence</c>) que otro usuario pudo avanzar al mismo tiempo.
    /// </summary>
    Task UpdateSettingsAsync(int personnelActionTypeId, Action<PersonnelActionType> apply, CancellationToken ct = default);

    /// <summary>
    /// Datos del tipo y del motivo de licencia necesarios para validar una acción: ver
    /// <see cref="WsUtaSystem.Application.DTOs.PersonnelActionType.LeaveActionContext"/>.
    /// </summary>
    Task<WsUtaSystem.Application.DTOs.PersonnelActionType.LeaveActionContext> GetLeaveContextAsync(
        int personnelActionTypeId, int? leaveReasonId, CancellationToken ct = default);
}
