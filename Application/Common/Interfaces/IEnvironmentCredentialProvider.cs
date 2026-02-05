namespace WsUtaSystem.Application.Common.Interfaces
{
    public interface IEnvironmentCredentialProvider
    {
        string? GetSmtpUser();
        string? GetSmtpPassword();
    }
}
