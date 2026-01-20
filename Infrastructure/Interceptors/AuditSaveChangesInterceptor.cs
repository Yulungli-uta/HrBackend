using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Infrastructure.Interceptors
{
    /// <summary>
    /// Interceptor que asigna automáticamente campos de auditoría 
    /// a entidades que implementan ICreationAuditable y/o IModificationAuditable
    /// </summary>
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;

        public AuditSaveChangesInterceptor(
            ICurrentUserService currentUser,
            ILogger<AuditSaveChangesInterceptor> logger)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (context is null) return;

            // Validación de usuario autenticado
            if (!_currentUser.IsAuthenticated || !_currentUser.EmployeeId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "No se pudo resolver EmployeeId desde el token para la auditoría.");
            }

            var now = DateTime.Now;
            var employeeId = _currentUser.EmployeeId.Value;

            //_logger.LogInformation(
            //    "Aplicando auditoría para EmployeeId {EmployeeId} a las {Timestamp}",
            //    employeeId,
            //    now);

            // Procesar entidades con auditoría de CREACIÓN
            ProcessCreationAudit(context, now, employeeId);

            // Procesar entidades con auditoría de MODIFICACIÓN
            ProcessModificationAudit(context, now, employeeId);
        }

        /// <summary>
        /// Aplica auditoría de creación a entidades nuevas
        /// </summary>
        private void ProcessCreationAudit(DbContext context, DateTime timestamp, int employeeId)
        {
            var creationEntries = context.ChangeTracker
                .Entries<ICreationAuditable>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in creationEntries)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.CreatedBy = employeeId;

                //_logger.LogDebug(
                //    "Auditoría de creación aplicada: {EntityType} - CreatedBy: {EmployeeId}",
                //    entry.Entity.GetType().Name,
                //    employeeId);
            }
        }

        /// <summary>
        /// Aplica auditoría de modificación a entidades actualizadas
        /// </summary>
        private void ProcessModificationAudit(DbContext context, DateTime timestamp, int employeeId)
        {
            var modificationEntries = context.ChangeTracker
                .Entries<IModificationAuditable>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modificationEntries)
            {
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = employeeId;

                // Si la entidad también implementa ICreationAuditable,
                // proteger los campos de creación contra modificación
                if (entry.Entity is ICreationAuditable)
                {
                    entry.Property(nameof(ICreationAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(ICreationAuditable.CreatedBy)).IsModified = false;
                }

                //_logger.LogDebug(
                //    "Auditoría de modificación aplicada: {EntityType} - UpdatedBy: {EmployeeId}",
                //    entry.Entity.GetType().Name,
                //    employeeId);
            }
        }
    }
}
