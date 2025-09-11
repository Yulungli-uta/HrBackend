namespace WsUtaSystem.Application.DTOs.PayrollLines;
public class PayrollLinesCreateDto
{
    //public class PayrollLines { get; set; }
    public int PayrollLineId { get; set; }
    public int PayrollId { get; set; }
    public string LineType { get; set; }
    public string Concept { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public string Notes { get; set; }
}
