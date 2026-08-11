using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Interceptors
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider; // Cambiado para evitar circularidad
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;

        public AuditSaveChangesInterceptor(
            IServiceProvider serviceProvider,
            ILogger<AuditSaveChangesInterceptor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAuditFields(DbContext? context)
        {
            if (context == null) return;

            var timestamp = DateTime.Now;
            int? employeeId = null;
            string? actorName = null;

            try
            {
                // Resolvemos el servicio de usuario de forma perezosa (Lazy)
                using var scope = _serviceProvider.CreateScope();
                var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();
                employeeId = currentUser?.EmployeeId;
                actorName = currentUser?.UserName ?? currentUser?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AUDIT] No se pudo obtener el EmployeeId del usuario actual");
            }

            ProcessCreationAudit(context, timestamp, employeeId);
            ProcessModificationAudit(context, timestamp, employeeId);
            ProcessDeletionAudit(context, timestamp, employeeId, actorName);
        }

        private void ProcessCreationAudit(DbContext context, DateTime timestamp, int? employeeId)
        {
            var creationEntries = context.ChangeTracker
                .Entries<ICreationAuditable>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in creationEntries)
            {
                entry.Entity.CreatedAt = timestamp;
                if (employeeId.HasValue) entry.Entity.CreatedBy = employeeId.Value;
            }
        }

        private void ProcessModificationAudit(DbContext context, DateTime timestamp, int? employeeId)
        {
            var modificationEntries = context.ChangeTracker
                .Entries<IModificationAuditable>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modificationEntries)
            {
                entry.Entity.UpdatedAt = timestamp;
                if (employeeId.HasValue) entry.Entity.UpdatedBy = employeeId.Value;

                if (entry.Entity is ICreationAuditable)
                {
                    entry.Property(nameof(ICreationAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(ICreationAuditable.CreatedBy)).IsModified = false;
                }
            }
        }

        // La fila desaparece al borrarse, así que no se puede "estampar" un campo en ella
        // como en Create/Update — se deja constancia aparte en HR.Audit con una foto de sus
        // valores justo antes del delete (EF todavía los tiene disponibles en este punto).
        private void ProcessDeletionAudit(DbContext context, DateTime timestamp, int? employeeId, string? actorName)
        {
            var deletionEntries = context.ChangeTracker
                .Entries<IAuditable>()
                .Where(e => e.State == EntityState.Deleted)
                .ToList();

            if (deletionEntries.Count == 0) return;

            var actor = actorName ?? employeeId?.ToString() ?? "unknown";

            foreach (var entry in deletionEntries)
            {
                var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
                var recordId = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())
                    ?.CurrentValue?.ToString() ?? "unknown";

                var snapshot = entry.Properties
                    .Where(p => !p.Metadata.IsPrimaryKey())
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

                context.Set<Audit>().Add(new Audit
                {
                    TableName = tableName,
                    Action = "DELETE",
                    RecordId = recordId,
                    UserName = actor,
                    DateTime = timestamp,
                    Details = System.Text.Json.JsonSerializer.Serialize(snapshot),
                });
            }
        }
    }
}