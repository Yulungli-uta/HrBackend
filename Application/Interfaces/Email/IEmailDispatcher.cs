using WsUtaSystem.Application.DTOs.Email;

namespace WsUtaSystem.Application.Interfaces.Email
{
    public interface IEmailDispatcher
    {
        ValueTask QueueAsync(EmailSendRequestDto request, CancellationToken ct = default);
    }
}
