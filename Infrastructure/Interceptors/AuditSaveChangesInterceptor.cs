using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Auditable;

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

            try
            {
                // Resolvemos el servicio de usuario de forma perezosa (Lazy)
                using var scope = _serviceProvider.CreateScope();
                var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();
                employeeId = currentUser?.EmployeeId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AUDIT] No se pudo obtener el EmployeeId del usuario actual");
            }

            ProcessCreationAudit(context, timestamp, employeeId);
            ProcessModificationAudit(context, timestamp, employeeId);
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
    }
}