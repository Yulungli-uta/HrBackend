namespace WsUtaSystem.Application.Common.Email
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.office365.com";
        public int Port { get; set; } = 587;

        public bool UseSsl { get; set; } = false;
        public bool UseStartTls { get; set; } = true;

        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "RRHH";
    }
}
