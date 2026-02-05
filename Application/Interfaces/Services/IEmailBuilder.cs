using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Email;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmailBuilder
    {
        IEmailBuilder To(string email);
        IEmailBuilder Subject(string subject);
        IEmailBuilder WithLayout(string? slug);
        IEmailBuilder WithBody(string html);
        IEmailBuilder CreatedBy(int? createdBy);

        IEmailBuilder AddStoredAttachment(Guid storedFileGuid);

        IEmailBuilder AddNewAttachment(
            IFormFile file,
            string directoryCode,
            string entityType,
            string entityId,
            int? documentTypeId,
            string? relativePath = null);

        // NUEVO
        EmailSendRequestDto Build();

        // NUEVO
        Task QueueAsync(CancellationToken ct = default);

        Task<EmailSendResponseDto> SendAsync(CancellationToken ct = default);

        Task TryNotifyAsync(
            EmailTemplateKey templateKey,
            string subject,
            string htmlBody,
            string? to = null,
            int timeoutSeconds = 15,
            CancellationToken ct = default);
    }
}
