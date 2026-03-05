using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        int? EmployeeId { get; }
        string? UserName { get; }
        string? Email { get; }
        int? DepartmentID { get; }
        string? DepartmentName { get; }

        int? BossId { get; }
        string? BossName { get; }
        string? BossEmail { get; }

        Task<CurrentBossInfo?> LoadBossAsync(CancellationToken ct = default);
        Task<VwEmployeeDetails?> LoadMeAsync(CancellationToken ct = default);  // NUEVO

        string? GetIp();
        string? GetUserAgent();
        string? GetDeviceInfo();
    }

    public sealed record CurrentBossInfo(int BossId, string FullName, string Email);
}