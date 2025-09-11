namespace WsUtaSystem.Models; public class Countries
{
    public string CountryId { get; set; } = null!;           // Cambiado a string (VARCHAR)
    public string CountryName { get; set; } = null!;
    public string? Nationality { get; set; }                 // Nuevo campo
    public string? NationalityCode { get; set; }             // Nuevo campo
    public string? AuxSIITH { get; set; }                    // Nuevo campo
    public string? AuxCEAACES { get; set; }                  // Nuevo campo
    public DateTime CreatedAt { get; set; }
}