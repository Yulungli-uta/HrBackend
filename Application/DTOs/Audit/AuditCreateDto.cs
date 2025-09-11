namespace WsUtaSystem.Application.DTOs.Audit;
public class AuditCreateDto
{
    //public class Audit { get; set; }
    public long AuditId { get; set; }
    public string TableName { get; set; }
    public string Action { get; set; }
    public string RecordId { get; set; }
    public string UserName { get; set; }
    public DateTime DateTime { get; set; }
    public string Details { get; set; }
}
