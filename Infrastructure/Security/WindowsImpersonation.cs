using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace WsUtaSystem.Infrastructure.Security;

/// <summary>
/// Clase para ejecutar operaciones con credenciales de Windows específicas
/// Usa LogonUser API de Windows con SafeAccessTokenHandle (compatible con .NET 9)
/// </summary>
public sealed class WindowsImpersonation : IDisposable
{
    private SafeAccessTokenHandle? _tokenHandle;
    private bool _disposed = false;

    // Constantes de LogonUser API
    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    // Importar LogonUser de advapi32.dll
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string? lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    /// <summary>
    /// Obtiene un token de acceso para las credenciales proporcionadas
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <returns>SafeAccessTokenHandle para usar con WindowsIdentity.RunImpersonated</returns>
    /// <exception cref="PlatformNotSupportedException">Si no se ejecuta en Windows</exception>
    /// <exception cref="InvalidOperationException">Si la autenticación falla</exception>
    public SafeAccessTokenHandle GetToken(string username, string password, string? domain = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Solo funciona en Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("WindowsImpersonation only works on Windows platform");
        }

        // Intentar autenticación
        bool loggedOn = LogonUser(
            username,
            domain,
            password,
            LOGON32_LOGON_NEW_CREDENTIALS,
            LOGON32_PROVIDER_DEFAULT,
            out IntPtr token);

        if (!loggedOn)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"LogonUser failed with error code: {errorCode}. Verify username, password and domain are correct.");
        }

        // Crear SafeAccessTokenHandle
        _tokenHandle = new SafeAccessTokenHandle(token);
        
        return _tokenHandle;
    }

    /// <summary>
    /// Ejecuta una acción con las credenciales especificadas usando impersonation
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <param name="action">Acción a ejecutar con las credenciales</param>
    public void RunImpersonated(string username, string password, string? domain, Action action)
    {
        var token = GetToken(username, password, domain);
        WindowsIdentity.RunImpersonated(token, action);
    }

    /// <summary>
    /// Ejecuta una función con las credenciales especificadas usando impersonation
    /// </summary>
    /// <typeparam name="T">Tipo de retorno</typeparam>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <param name="func">Función a ejecutar con las credenciales</param>
    /// <returns>Resultado de la función</returns>
    public T RunImpersonated<T>(string username, string password, string? domain, Func<T> func)
    {
        var token = GetToken(username, password, domain);
        return WindowsIdentity.RunImpersonated(token, func);
    }

    /// <summary>
    /// Ejecuta una tarea asíncrona con las credenciales especificadas usando impersonation
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <param name="func">Función asíncrona a ejecutar</param>
    public async Task RunImpersonatedAsync(string username, string password, string? domain, Func<Task> func)
    {
        var token = GetToken(username, password, domain);
        await WindowsIdentity.RunImpersonatedAsync(token, func);
    }

    /// <summary>
    /// Ejecuta una tarea asíncrona con resultado usando impersonation
    /// </summary>
    /// <typeparam name="T">Tipo de retorno</typeparam>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <param name="func">Función asíncrona a ejecutar</param>
    /// <returns>Resultado de la función</returns>
    public async Task<T> RunImpersonatedAsync<T>(string username, string password, string? domain, Func<Task<T>> func)
    {
        var token = GetToken(username, password, domain);
        return await WindowsIdentity.RunImpersonatedAsync(token, func);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tokenHandle?.Dispose();
            _tokenHandle = null;
            _disposed = true;
        }
    }
}

