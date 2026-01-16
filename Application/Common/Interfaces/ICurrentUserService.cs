namespace WsUtaSystem.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        int? EmployeeId { get; }
        string? UserName { get; }
        string? Email { get; }
        string? GetIp();
        string? GetUserAgent();
        string? GetDeviceInfo();
    }
}
