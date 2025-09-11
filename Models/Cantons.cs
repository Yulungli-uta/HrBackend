namespace WsUtaSystem.Models; 
public class Cantons {
    public string CantonId { get; set; } = null!;            // Cambiado a string (VARCHAR)
    public string ProvinceId { get; set; } = null!;          // Cambiado a string (VARCHAR)
    public string CantonName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}