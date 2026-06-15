using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Security
{
    /// <summary>
    /// Proveedor de credenciales SMTP con dos modos de operación, controlados por
    /// la clave <c>Smtp:UseAppSettings</c> en appsettings.json:
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <term>true (default)</term>
    ///     <description>
    ///       Lee <c>Smtp:User</c> y <c>Smtp:Password</c> directamente de appsettings.json.
    ///       Recomendado para desarrollo local.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>false</term>
    ///     <description>
    ///       Lee las credenciales desde variables de entorno (<c>SMTP_USER</c> / <c>SMTP_PASS</c>)
    ///       con fallback a User Secrets (<c>EmailSecrets:User</c> / <c>EmailSecrets:Pass</c>).
    ///       Recomendado para producción (IIS, Docker, CI/CD).
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// Para cambiar el modo basta con editar <c>appsettings.json</c>:
    /// <code>
    /// "Smtp": {
    ///   "UseAppSettings": true   // ← cambiar a false para usar variables de entorno
    /// }
    /// </code>
    /// </summary>
    public sealed class EnvironmentCredentialProvider : IEnvironmentCredentialProvider
    {
        // ── Claves de variables de entorno ──────────────────────────────────────
        private const string EnvUser = "SMTP_USER";
        private const string EnvPass = "SMTP_PASS";

        // ── Claves de User Secrets (fallback cuando UseAppSettings = false) ─────
        private const string SecretsUserKey = "EmailSecrets:User";
        private const string SecretsPassKey = "EmailSecrets:Pass";

        // ── Claves de appsettings (cuando UseAppSettings = true) ────────────────
        private const string AppSettingsUserKey = "Smtp:User";
        private const string AppSettingsPassKey = "Smtp:Password";
        private const string UseAppSettingsKey  = "Smtp:UseAppSettings";

        private readonly IConfiguration _config;

        public EnvironmentCredentialProvider(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Retorna el usuario SMTP según el modo configurado en <c>Smtp:UseAppSettings</c>.
        /// </summary>
        public string GetSmtpUser()
        {
            var value = UseAppSettings()
                ? _config[AppSettingsUserKey]                    // appsettings.json
                : ReadEnv(EnvUser) ?? _config[SecretsUserKey];  // env var o User Secrets

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    UseAppSettings()
                        ? $"Credencial SMTP no configurada. Defina '{AppSettingsUserKey}' en appsettings.json."
                        : $"Credencial SMTP no configurada. Defina '{EnvUser}' (variable de entorno) " +
                          $"o '{SecretsUserKey}' (User Secrets).");

            return value.Trim();
        }

        /// <summary>
        /// Retorna la contraseña SMTP según el modo configurado en <c>Smtp:UseAppSettings</c>.
        /// </summary>
        public string GetSmtpPassword()
        {
            var value = UseAppSettings()
                ? _config[AppSettingsPassKey]                    // appsettings.json
                : ReadEnv(EnvPass) ?? _config[SecretsPassKey];  // env var o User Secrets

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    UseAppSettings()
                        ? $"Credencial SMTP no configurada. Defina '{AppSettingsPassKey}' en appsettings.json."
                        : $"Credencial SMTP no configurada. Defina '{EnvPass}' (variable de entorno) " +
                          $"o '{SecretsPassKey}' (User Secrets).");

            return value;
        }

        // ── Helpers privados ────────────────────────────────────────────────────

        /// <summary>
        /// Retorna true si se deben usar las credenciales de appsettings.json.
        /// Por defecto es true si la clave no está definida.
        /// </summary>
        private bool UseAppSettings()
        {
            var raw = _config[UseAppSettingsKey];
            // Si no está definida la clave, se asume true (modo appsettings)
            if (string.IsNullOrWhiteSpace(raw)) return true;
            return !raw.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Lee una variable de entorno buscando en Process → Machine → User.
        /// Retorna null si no está definida en ninguno.
        /// </summary>
        private static string? ReadEnv(string key)
            => Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process)
            ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine)
            ?? Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);
    }
}
