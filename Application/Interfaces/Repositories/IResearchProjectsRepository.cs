using WsUtaSystem.Application.DTOs.ResearchProjects;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Documento de resolución al que pertenece un archivo del proyecto.</summary>
public enum ResearchDocumentKind { Approval, Extension, Completion }

public sealed record ResearchProjectListRow(int Id, string Name, DateOnly StartDate, DateOnly EndDate, bool HasExtension, int ParticipantsCount);

public sealed record ResearchRefTypeLite(int Id, string Category, string Name, string? Description);

public sealed record ResearchPersonInfo(int PersonId, string IdCard, string FullName);

/// <summary>Acceso a datos de proyectos de investigación y sus participaciones.</summary>
public interface IResearchProjectsRepository
{
    /// <summary>
    /// Lista paginada. Filtros opcionales: nombre o número de resolución de aprobación, y rango de fechas
    /// (proyectos cuyo período —inicio a fin— se cruza con el rango <paramref name="from"/>–<paramref name="to"/>).
    /// </summary>
    Task<(IReadOnlyList<ResearchProjectListRow> Items, long Total)> SearchAsync(
        string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Proyecto con sus participaciones no eliminadas. Con <paramref name="track"/> queda rastreado para actualizar.</summary>
    Task<ResearchProject?> GetByIdAsync(int id, bool track, CancellationToken ct = default);

    /// <summary>Proyectos (no eliminados) donde participa la persona, con todas sus participaciones no eliminadas.</summary>
    Task<IReadOnlyList<ResearchProject>> GetByPersonAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Guarda el proyecto y los archivos nuevos en UNA transacción: inserta los StoredFile, asigna sus ids al
    /// proyecto según el tipo de documento, y marca como eliminados los archivos que se reemplazan.
    /// </summary>
    Task SaveAsync(ResearchProject project, bool isNew, IReadOnlyDictionary<ResearchDocumentKind, StoredFile> newFiles, CancellationToken ct = default);

    Task SoftDeleteAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, StoredFile>> GetStoredFilesAsync(IEnumerable<int> fileIds, CancellationToken ct = default);

    Task<StoredFile?> GetStoredFileByGuidAsync(Guid fileGuid, CancellationToken ct = default);

    /// <summary>true si la persona participa (internamente) en algún proyecto que usa ese archivo.</summary>
    Task<bool> IsParticipantOfFileAsync(Guid fileGuid, int personId, CancellationToken ct = default);

    Task<IReadOnlyList<ResearchRefTypeLite>> GetRefTypesAsync(IEnumerable<string> categories, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, ResearchPersonInfo>> GetPeopleAsync(IEnumerable<int> personIds, CancellationToken ct = default);

    Task<IReadOnlyList<ResearchPersonLookupDto>> SearchPeopleAsync(string term, int take, CancellationToken ct = default);
}
