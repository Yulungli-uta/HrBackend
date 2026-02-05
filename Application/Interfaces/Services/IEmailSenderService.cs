using WsUtaSystem.Application.DTOs.Email;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmailSenderService
    {
        Task<EmailSendResponseDto> SendAsync(EmailSendRequestDto request, CancellationToken ct);
    }
}
