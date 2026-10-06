namespace WsUtaSystem.Application.DTOs.RefTypes;
public class RefTypesUpdateDto
{
    //public class RefTypes { get; set; }
    public int TypeId { get; set; }
    public string Category { get; set; }
    public string Name { get; set; }
    // Nullable a propósito: [ApiController] infiere [Required] en reference types no-nulos, y
    // la UI anuncia "Descripción opcional" -- sin el '?' el backend rechazaba con 400 cualquier
    // guardado con descripción vacía (hallazgo real QA UTA-DITIC-PS-030-2026, obs. 42-43,
    // 2026-10-06).
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SiiesLabel { get; set; }
    public int SortOrder { get; set; }
}
