
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class EducationLevels : IAuditable{
  public int EducationId{get;set;}
  public int PersonId{get;set;}
  public int EducationLevelTypeId{get;set;}

  /// <summary>
  /// Nullable desde 2026-09-16: un título sincronizado desde DINARDAP puede no tener una
  /// institución catalogada (HR.tbl_Institutions exige País/Provincia/Cantón, dato que
  /// DINARDAP no manda para universidades extranjeras) — en ese caso queda null y el nombre
  /// real se guarda igual en <see cref="InstitutionNameOriginal"/>.
  /// </summary>
  public int? InstitutionId{get;set;}
  public string Title{get;set;}=null!;
  public string? Specialty{get;set;}
  public DateOnly? StartDate{get;set;}
  public DateOnly? EndDate{get;set;}
  public string? Grade{get;set;}
  public decimal? Score{get;set;}
  public string? SenescytRegistrationNumber { get; set; }

  /// <summary>FK -> ref_Types (Category='SIIES_GRADO'). Solo aplica cuando el nivel del título es CUARTO NIVEL.</summary>
  public int? SiiesGradoTypeId { get; set; }

  /// <summary>FK -> tbl_KnowledgeArea. Área de conocimiento interna (selector de Publicaciones) - NO es la subárea SIIES, ver UnescoSubareaTypeId.</summary>
  public int? KnowledgeAreaId { get; set; }

  /// <summary>FK -> tbl_Countries. País donde se obtuvo el título (SIIES PAIS_ESTUDIO). DINARDAP no lo informa - solo carga manual.</summary>
  public string? CountryOfStudyId { get; set; }

  /// <summary>FK -> ref_Types (Category='SIIES_UNESCO_SUBAREA'). Campo detallado UNESCO/ISCED-F del título (SIIES CODIGO_SUBAREA_CONOCIMIENTO_ESPECIFICO_UNESCO). DINARDAP no lo informa - solo carga manual.</summary>
  public int? UnescoSubareaTypeId { get; set; }

  // ---- Integración DINARDAP (2026-09-16) ----------------------------------

  /// <summary>fechaGrado de DINARDAP - distinto de StartDate/EndDate (fechas de estudio, no de grado).</summary>
  public DateOnly? SenescytGraduationDate { get; set; }

  /// <summary>fechaRegistro de DINARDAP (fecha en que SENESCYT registró el título) - distinto de SenescytRegistrationNumber.</summary>
  public DateOnly? SenescytRegistrationDate { get; set; }

  /// <summary>"NACIONALES" / "EXTRANJEROS" tal como lo manda DINARDAP.</summary>
  public string? SenescytType { get; set; }

  /// <summary>Texto crudo del nivel tal como lo manda DINARDAP (ej. "Tercer Nivel Técnico Superior") - trazabilidad si se corrige el clasificador.</summary>
  public string? SenescytNivelNombreOriginal { get; set; }

  /// <summary>Nombre libre de la institución tal como lo manda DINARDAP - ver InstitutionId.</summary>
  public string? InstitutionNameOriginal { get; set; }

  /// <summary>"Manual" (default) o "Dinardap" - gobierna el bloqueo por campo en HrFrontend.</summary>
  public string Source { get; set; } = "Manual";

  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}
