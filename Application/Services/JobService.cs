using AutoMapper;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class JobService : Service<Job, int>, IJobService
    {
        private readonly IJobRepository _repository;
        private readonly IMapper _mapper;

        public JobService(IJobRepository repo) : base(repo)
        {
            _repository = repo;
        }
        public async Task<IEnumerable<Job>> GetActiveJobsAsync(CancellationToken ct)
        {
            return await _repository.GetActiveJobsAsync(ct);
        }

        public async Task<IEnumerable<Job>> SearchJobsByTitleAsync(string title, CancellationToken ct)
        {
            return await _repository.SearchJobsByTitleAsync(title,ct);
        }

        /// <inheritdoc/>
        public override async Task<Job> CreateAsync(Job entity, CancellationToken ct)
        {
            await ValidateAsync(entity, null, ct);
            return await base.CreateAsync(entity, ct);
        }

        /// <inheritdoc/>
        public override async Task UpdateAsync(int id, Job entity, CancellationToken ct)
        {
            var current = await base.GetByIdAsync(id, ct);
            await ValidateAsync(entity, current, ct);
            await base.UpdateAsync(id, entity, ct);
        }

        /// <summary>
        /// Reglas del cargo: la descripción no se repite entre cargos activos y, en LOSEP y Código de Trabajo, la escala
        /// (Grupo x Grado) es obligatoria. Un cargo existente que RRHH todavía no clasificó puede seguir editándose sin escala,
        /// pero no se le puede quitar una que ya tiene.
        /// </summary>
        private async Task ValidateAsync(Job entity, Job? current, CancellationToken ct)
        {
            // decimal(10,2): hasta 99.999.999,99; el sueldo de referencia no puede ser negativo
            if (entity.ReferenceSalary is < 0 or > 99_999_999.99m)
                throw new BusinessRuleException("El sueldo de referencia debe estar entre 0 y 99.999.999,99.");

            if (entity.IsActive && !string.IsNullOrWhiteSpace(entity.Description)
                && await _repository.ActiveDescriptionInUseAsync(entity.Description, current?.JobID, ct))
                throw new BusinessRuleException("Ya existe otro cargo activo con esa descripción.");

            // Una escala inactiva no se puede asignar (si el cargo ya la tenía, se respeta para poder seguir editándolo)
            if (entity.GroupId is int scaleId && current?.GroupId != scaleId && !await _repository.IsScaleActiveAsync(scaleId, ct))
                throw new BusinessRuleException("La escala seleccionada está inactiva o no existe. Elija una escala vigente.");

            if (!entity.IsActive || entity.GroupId.HasValue || entity.JobTypeId is not int typeId) return;
            if (current is not null && !current.GroupId.HasValue) return;

            var typeName = await _repository.GetJobTypeNameAsync(typeId, ct);
            var requiresScale = typeName is not null
                && (typeName.Contains("LOSEP", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Código", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Codigo", StringComparison.OrdinalIgnoreCase));
            if (requiresScale)
                throw new BusinessRuleException("Los cargos LOSEP y de Código de Trabajo deben tener grupo y grado (escala).");
        }

    }
}
