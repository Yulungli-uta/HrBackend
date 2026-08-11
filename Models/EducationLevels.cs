
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class EducationLevels : IAuditable{
  public int EducationId{get;set;}
  public int PersonId{get;set;}
  public int EducationLevelTypeId{get;set;}
  public int InstitutionId{get;set;}
  public string Title{get;set;}=null!;
  public string? Specialty{get;set;}
  public DateOnly? StartDate{get;set;}
  public DateOnly? EndDate{get;set;}
  public string? Grade{get;set;}
  public string? Location{get;set;}
  public decimal? Score{get;set;}
  public string? SenescytRegistrationNumber { get; set; }

  /// <summary>FK -> ref_Types (Category='SIIES_GRADO'). Solo aplica cuando el nivel del título es CUARTO NIVEL.</summary>
  public int? SiiesGradoTypeId { get; set; }

  /// <summary>FK -> tbl_KnowledgeArea. Campo detallado UNESCO del título (SIIES CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO).</summary>
  public int? KnowledgeAreaId { get; set; }

  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}
