using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmailLogsService : IService<EmailLog, int>
    {
        Task<IEnumerable<EmailLog>> GetByRecipientAsync(string recipient, CancellationToken ct);

        /// <summary>
        /// Inserta log + attachments (si existen) como operación compuesta.
        /// Devuelve EmailLogID.
        /// </summary>
        Task<int> InsertLogWithAttachmentsAsync(
            EmailLog log,
            IEnumerable<EmailLogAttachment> attachments,
            CancellationToken ct);
    }
}
