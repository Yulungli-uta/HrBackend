using WsUtaSystem.Application.DTOs.Dinardap;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Cliente HTTP hacia WsUtaDinardap.Api (proyecto DatosDINARDAP), usado por HrBackend para
/// auto-rellenar Personas/Cargas Familiares y sincronizar Formación Académica. Se autentica
/// con el mismo JWT de cuenta de servicio ya usado para RepositoryUta
/// (<see cref="IEmployeeProvisioningClient.GetServiceTokenAsync"/>) — requiere que esa cuenta
/// tenga el permiso DINARDAP_HR.READ.
/// </summary>
public interface IHrDinardapClient
{
    /// <summary>
    /// Retorna null SOLO si no se pudo consultar el servicio (red, timeout, permiso, DINARDAP
    /// caído) — WsUtaDinardap.Api siempre responde un objeto (aunque con propiedades en null)
    /// cuando la persona simplemente no tiene datos, nunca un cuerpo vacío.
    /// </summary>
    Task<DinardapRegistroCivilDto?> ConsultarRegistroCivilAsync(string identificacion, CancellationToken ct = default);

    /// <summary>Mismo criterio de null que <see cref="ConsultarRegistroCivilAsync"/>.</summary>
    Task<DinardapTceDto?> ConsultarTceAsync(string identificacion, CancellationToken ct = default);

    /// <summary>Nunca null — lista vacía si la persona no tiene títulos o el servicio falla.</summary>
    Task<IReadOnlyList<DinardapTituloDto>> ConsultarTitulosAsync(string identificacion, CancellationToken ct = default);
}
