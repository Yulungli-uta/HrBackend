using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WsUtaSystem.Infrastructure.Security;

/// <summary>
/// Clase para ejecutar operaciones con credenciales de Windows específicas
/// Usa LogonUser API de Windows para impersonation
/// </summary>
public class WindowsImpersonation : IDisposable
{
    private WindowsImpersonationContext? _impersonationContext;
    private IntPtr _token = IntPtr.Zero;
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

    // Importar CloseHandle de kernel32.dll
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool CloseHandle(IntPtr handle);

    /// <summary>
    /// Inicia impersonation con las credenciales proporcionadas
    /// </summary>
    /// <param name="username">Nombre de usuario</param>
    /// <param name="password">Contraseña</param>
    /// <param name="domain">Dominio (opcional)</param>
    /// <returns>True si el impersonation fue exitoso</returns>
    public bool Impersonate(string username, string password, string? domain = null)
    {
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
            out _token);

        if (!loggedOn)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"LogonUser failed with error code: {errorCode}");
        }

        // Crear identidad de Windows con el token
        var identity = new WindowsIdentity(_token);
        
        // Iniciar impersonation
        _impersonationContext = identity.Impersonate();

        return true;
    }

    /// <summary>
    /// Finaliza el impersonation y libera recursos
    /// </summary>
    public void Undo()
    {
        if (_impersonationContext != null)
        {
            _impersonationContext.Undo();
            _impersonationContext.Dispose();
            _impersonationContext = null;
        }

        if (_token != IntPtr.Zero)
        {
            CloseHandle(_token);
            _token = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Undo();
            }
            _disposed = true;
        }
    }

    ~WindowsImpersonation()
    {
        Dispose(false);
    }
}

