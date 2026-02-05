using MimeKit;

namespace WsUtaSystem.Application.Interfaces.Email
{
    public interface IEmailProvider
    {
        Task SendAsync(MimeMessage message, CancellationToken ct);
    }
}
