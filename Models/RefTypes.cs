
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class RefTypes : IAuditable{
  public int TypeId { get; set; }
  public string Category { get; set; } = null!;
  public string Name { get; set; } = null!;
  public string? Description { get; set; }
  public string? Metadata { get; set; }
  /// <summary>Denominación exacta exigida por el catálogo SIIES (CACES) para este valor, cuando aplica.</summary>
  public string? SiiesLabel { get; set; }

  /// <summary>Orden/jerarquía dentro de la categoría (mayor = más alto/preferido). Usado por
  /// reportes que deben elegir "el valor más alto" de un conjunto (ej. HR.vw_SiiesFormacionProfesional
  /// eligiendo el título de mayor Nivel/Grado por profesor) sin hardcodear la jerarquía en SQL -
  /// ver Database/hr/16_siies_profesores.sql sección 11. Default 0 = sin definir, queda de último.</summary>
  public int SortOrder { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }

}
