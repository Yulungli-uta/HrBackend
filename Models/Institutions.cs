
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Institutions : IAuditable{
  public int InstitutionId{get;set;}
  public string Name{get;set;}=null!;
  public int InstitutionTypeId{get;set;}
  public string CountryId{get;set;}=null!;

  /// <summary>Nullable desde 2026-09-19: las 378 instituciones del catálogo oficial CACES
  /// (ver Database/hr/16_siies_profesores.sql sección 10) solo traen código+nombre, sin
  /// provincia/cantón - la BD ya lo permite NULL, este campo tenía que reflejarlo.</summary>
  public string? ProvinceId{get;set;}

  /// <summary>Nullable desde 2026-09-19, mismo motivo que ProvinceId.</summary>
  public string? CantonId{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}
