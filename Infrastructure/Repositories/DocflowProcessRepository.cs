using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación del repositorio de procesos Docflow.
    /// Hereda operaciones CRUD básicas de ServiceAwareEfRepository.
    /// </summary>
    public sealed class DocflowProcessRepository : ServiceAwareEfRepository<DocflowProcessHierarchy, int>, IDocflowProcessRepository
    {
        private readonly AppDbContext _db;

        public DocflowProcessRepository(AppDbContext db) : base(db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Obtiene todos los procesos activos ordenados por ProcessId.
        /// </summary>
        public Task<List<DocflowProcessHierarchy>> GetAllActiveAsync(CancellationToken ct) =>
            _set.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProcessId)
                .ToListAsync(ct);

        /// <summary>
        /// Obtiene las reglas de documento asociadas a un proceso específico.
        /// </summary>
        public Task<List<DocflowDocumentRule>> GetRulesByProcessAsync(int processId, CancellationToken ct) =>
            _db.DocflowDocumentRules
                .AsNoTracking()
                .Where(x => x.ProcessId == processId)
                .OrderBy(x => x.RuleId)
                .ToListAsync(ct);

        /// <summary>
        /// Obtiene la transición por defecto desde un proceso.
        /// </summary>
        public Task<DocflowProcessTransition?> GetDefaultTransitionAsync(int fromProcessId, CancellationToken ct) =>
            _db.DocflowTransitions
                .AsNoTracking()
                .Where(x => x.FromProcessId == fromProcessId)
                .OrderByDescending(x => x.IsDefault)
                .FirstOrDefaultAsync(ct);

        /// <summary>
        /// Obtiene la metadata de campos dinámicos de un proceso en formato JSON.
        /// </summary>
        public async Task<string?> GetDynamicFieldMetadataJsonAsync(int processId, CancellationToken ct)
        {
            return await _set.AsNoTracking()
                .Where(x => x.ProcessId == processId)
                .Select(x => x.DynamicFieldMetadata)
                .FirstOrDefaultAsync(ct);
        }

        /// <summary>
        /// Actualiza la metadata de campos dinámicos de un proceso.
        /// </summary>
        public async Task UpdateDynamicFieldMetadataJsonAsync(int processId, string? json, CancellationToken ct)
        {
            var proc = await _set.FirstOrDefaultAsync(x => x.ProcessId == processId, ct)
                ?? throw new KeyNotFoundException($"Proceso con ID {processId} no encontrado.");

            proc.DynamicFieldMetadata = json;
            proc.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Obtiene procesos por departamento responsable.
        /// </summary>
        public Task<List<DocflowProcessHierarchy>> GetByDepartmentAsync(int departmentId, CancellationToken ct) =>
            _set.AsNoTracking()
                .Where(x => x.ResponsibleDepartmentId == departmentId && x.IsActive)
                .OrderBy(x => x.ProcessName)
                .ToListAsync(ct);

        /// <summary>
        /// Obtiene procesos hijo de un proceso padre (sub-procesos).
        /// </summary>
        public Task<List<DocflowProcessHierarchy>> GetChildrenAsync(int parentProcessId, CancellationToken ct) =>
            _set.AsNoTracking()
                .Where(x => x.ParentId == parentProcessId && x.IsActive)
                .OrderBy(x => x.ProcessName)
                .ToListAsync(ct);

        /// <summary>
        /// Crea una nueva regla de documento para un proceso.
        /// </summary>
        public async Task<DocflowDocumentRule> CreateRuleAsync(DocflowDocumentRule rule, CancellationToken ct)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            // Validar que el proceso existe
            var processExists = await _set.AnyAsync(x => x.ProcessId == rule.ProcessId, ct);
            if (!processExists)
                throw new KeyNotFoundException($"Proceso con ID {rule.ProcessId} no encontrado.");

            _db.DocflowDocumentRules.Add(rule);
            await _db.SaveChangesAsync(ct);

            return rule;
        }

        /// <summary>
        /// Actualiza una regla de documento existente.
        /// </summary>
        public async Task UpdateRuleAsync(int ruleId, DocflowDocumentRule rule, CancellationToken ct)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            var existingRule = await _db.DocflowDocumentRules.FirstOrDefaultAsync(x => x.RuleId == ruleId, ct)
                ?? throw new KeyNotFoundException($"Regla con ID {ruleId} no encontrada.");

            // Asegurar que el ID no cambie
            rule.RuleId = ruleId;

            var entry = _db.Entry(existingRule);
            entry.CurrentValues.SetValues(rule);

            await _db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Elimina una regla de documento.
        /// </summary>
        public async Task DeleteRuleAsync(int ruleId, CancellationToken ct)
        {
            var rule = await _db.DocflowDocumentRules.FirstOrDefaultAsync(x => x.RuleId == ruleId, ct)
                ?? throw new KeyNotFoundException($"Regla con ID {ruleId} no encontrada.");

            _db.DocflowDocumentRules.Remove(rule);
            await _db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Obtiene una regla específica por su ID.
        /// </summary>
        public Task<DocflowDocumentRule?> GetRuleByIdAsync(int ruleId, CancellationToken ct) =>
            _db.DocflowDocumentRules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.RuleId == ruleId, ct);
    }
}
