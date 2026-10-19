using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        /// <summary>Guid del usuario en el sistema de autenticación (claim "sub"/NameIdentifier).</summary>
        Guid? UserId { get; }
        int? EmployeeId { get; }
        string? UserName { get; }
        string? Email { get; }
        int? DepartmentID { get; }
        string? DepartmentName { get; }

        int? BossId { get; }
        string? BossName { get; }
        string? BossEmail { get; }

        Task<CurrentBossInfo?> LoadBossAsync(CancellationToken ct = default);
        Task<VwEmployeeDetails?> LoadMeAsync(CancellationToken ct = default);
        /// <summary>Retorna el EmployeeType (RefTypes.TypeId, Category=CONTRACT_TYPE) del empleado logueado.</summary>
        Task<int?> GetEmployeeTypeAsync(CancellationToken ct = default);
        /// <summary>Resuelve HR.tbl_People.PersonId del empleado autenticado (vía Employees.PersonID). Usado
        /// para validar propiedad de registros de hoja de vida (educación, publicaciones, etc.) antes de
        /// permitir lectura/edición — nunca confiar en el PersonId que envía el cliente.</summary>
        Task<int?> GetPersonIdAsync(CancellationToken ct = default);

        string? GetIp();
        /// <summary>IP de origen que ve el servidor (RemoteIpAddress, ya corregida por UseForwardedHeaders si el proxy es de confianza).
        /// A diferencia de <see cref="GetIp"/>, NO lee X-Forwarded-For a mano: el cliente no puede falsearla. Úsese para decisiones de seguridad.</summary>
        string? GetRemoteIp();
        /// <summary>Valor crudo de X-Forwarded-For (solo informativo para diagnóstico; nunca para decidir accesos).</summary>
        string? GetForwardedForHeader();
        string? GetUserAgent();
        string? GetDeviceInfo();
    }

    public sealed record CurrentBossInfo(int BossId, string FullName, string Email);
}