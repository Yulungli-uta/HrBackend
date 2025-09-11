namespace WsUtaSystem.Application.DTOs.PunchJustifications;
public class PunchJustificationsUpdateDto
{
    //public class PunchJustifications { get; set; }
    public int PunchJustID { get; set; }
    public bool Approved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public required string Status { get; set; }
}
