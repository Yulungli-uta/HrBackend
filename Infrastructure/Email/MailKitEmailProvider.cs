using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Email;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace WsUtaSystem.Infrastructure.Email
{
    public sealed class MailKitEmailProvider : IEmailProvider
    {
        private readonly EmailSettings _settings;
        private readonly IEnvironmentCredentialProvider _credentials;
        private readonly ILogger<MailKitEmailProvider> _logger;

        public MailKitEmailProvider(
            IOptions<EmailSettings> settings,
            IEnvironmentCredentialProvider credentials,
            ILogger<MailKitEmailProvider> logger)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAsync(MimeMessage message, CancellationToken ct)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var user = _credentials.GetSmtpUser();
            var pass = _credentials.GetSmtpPassword();

            using var client = new SmtpClient
            {
                Timeout = 15000
            };

            try
            {
                var secureOpt = ResolveSocketOptions(_settings);

                _logger.LogInformation("[SMTP] Connect {Host}:{Port} opt={Opt}", _settings.Host, _settings.Port, secureOpt);

                await client.ConnectAsync(_settings.Host, _settings.Port, secureOpt, ct);

                if (!string.IsNullOrWhiteSpace(user))
                {
                    _logger.LogInformation("[SMTP] Authenticate user={User}", user);
                    await client.AuthenticateAsync(user, pass, ct);
                }

                _logger.LogInformation("[SMTP] Send to={To} subject={Subject}", string.Join(",", message.To), message.Subject);

                await client.SendAsync(message, ct);
            }
            finally
            {
                try
                {
                    if (client.IsConnected)
                        await client.DisconnectAsync(true, ct);
                }
                catch
                {
                    // evita que un disconnect falle y esconda el error real
                }
            }
        }

        private static SecureSocketOptions ResolveSocketOptions(EmailSettings s)
        {
            if (s.UseStartTls) return SecureSocketOptions.StartTls;
            if (s.UseSsl) return SecureSocketOptions.SslOnConnect;
            return SecureSocketOptions.Auto;
        }
    }
}
