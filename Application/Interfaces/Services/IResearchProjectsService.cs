using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.AcademicPromotion;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.ResearchProjects;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Resultado de pedir un documento de resolución: separa "no existe" de "no autorizado".</summary>
public sealed record ResearchDocumentDownload(bool Found, bool Allowed, byte[]? Bytes, string? ContentType, string? FileName);

/// <summary>Registro y consulta de proyectos de investigación y la participación de las personas en ellos.</summary>
public interface IResearchProjectsService
{
    /// <summary>
    /// Lista paginada de proyectos. Filtros opcionales: nombre o resolución de aprobación, y rango de fechas
    /// (proyectos cuyo período se cruza con el rango).
    /// </summary>
    Task<PagedResult<ResearchProjectListItemDto>> SearchAsync(
        string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Proyecto completo con todos sus participantes; null si no existe.</summary>
    Task<ResearchProjectDto?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Crea (id nulo) o edita un proyecto. <paramref name="dataJson"/> trae los datos y las participaciones;
    /// los archivos PDF de las resoluciones son opcionales (el número de cada resolución sí es obligatorio); un archivo nuevo reemplaza al anterior.
    /// </summary>
    Task<ResearchProjectDto> SaveAsync(
        int? id, string? dataJson, IFormFile? approvalFile, IFormFile? extensionFile, IFormFile? completionFile,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Búsqueda mínima de personas (cédula o nombre) para elegir participantes internos.</summary>
    Task<IReadOnlyList<ResearchPersonLookupDto>> SearchPeopleAsync(string? term, CancellationToken ct = default);

    /// <summary>Proyectos donde participa la persona, con SOLO sus participaciones (hoja de vida).</summary>
    Task<IReadOnlyList<ResearchProjectDto>> GetByPersonAsync(int personId, CancellationToken ct = default);

    /// <summary>Bloque researchProjectParticipations del servicio de escalafón docente.</summary>
    Task<IReadOnlyList<ResearchProjectParticipationBlockDto>> GetAcademicBlockAsync(int personId, CancellationToken ct = default);

    /// <summary>Descarga un documento de resolución si el usuario es privilegiado o participa en el proyecto.</summary>
    Task<ResearchDocumentDownload> GetDocumentAsync(Guid fileGuid, bool privileged, int? myPersonId, CancellationToken ct = default);
}
