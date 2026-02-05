using Microsoft.AspNetCore.Hosting.Server;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Security
{
    /// <summary>
    /// Lee credenciales SMTP desde variables de entorno (ideal para IIS).
    /// Recomendado:
    ///   SMTP_USER = cuenta@dominio
    ///   SMTP_PASS = password/app-password
    /// </summary>
    public sealed class EnvironmentCredentialProvider : IEnvironmentCredentialProvider
    {
        private const string UserVar = "SMTP_USER";
        private const string PassVar = "SMTP_PASS";

        private const string SecretsUserKey = "EmailSecrets:User";
        private const string SecretsPassKey = "EmailSecrets:Pass";

        private readonly IConfiguration _config;

        public EnvironmentCredentialProvider(IConfiguration config)
        {
            _config = config;
        }

        public string GetSmtpUser()
        {
            var v = ReadEnv(UserVar) ?? _config[SecretsUserKey];
            if (string.IsNullOrWhiteSpace(v))
                throw new InvalidOperationException(
                    $"Credencial SMTP no configurada. Defina {UserVar} (env) o {SecretsUserKey} (UserSecrets).");

            return v.Trim();
        }

        public string GetSmtpPassword()
        {
            var v = ReadEnv(PassVar) ?? _config[SecretsPassKey];
            if (string.IsNullOrWhiteSpace(v))
                throw new InvalidOperationException(
                    $"Credencial SMTP no configurada. Defina {PassVar} (env) o {SecretsPassKey} (UserSecrets).");

            return v;
        }

        private static string? ReadEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine)
                ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
        }
    }
}
