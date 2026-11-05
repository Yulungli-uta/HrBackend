using WsUtaSystem.Application.DTOs.PublicationSync;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Sincronización masiva de publicaciones: recorre a los docentes con empleado activo que ya registraron su ORCID y su ID en el
/// proveedor. Importa lo nuevo (queda SIN_REVISION para la Dirección de Investigación) y completa vacíos; los casos
/// ambiguos se omiten y se informan (se resuelven cuando el docente sincroniza individualmente). Se detiene si el proveedor
/// rechaza las credenciales, no está configurado o se agota el cupo.
/// </summary>
public interface IPublicationMassSyncService
{
    Task<PublicationMassSyncResultDto> RunAsync(CancellationToken ct);
}
