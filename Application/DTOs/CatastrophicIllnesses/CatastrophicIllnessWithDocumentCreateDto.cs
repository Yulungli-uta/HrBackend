using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.CatastrophicIllnesses;

/// <summary>
/// DTO multipart para crear un registro de Enfermedad Catastrófica junto con su certificado
/// médico de respaldo en una sola llamada, con garantía transaccional entre el INSERT del
/// registro y el INSERT de la metadata del archivo.
/// </summary>
public class CatastrophicIllnessWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public string Illness { get; set; } = null!;
    public string? IESSNumber { get; set; }
    public string? SubstituteName { get; set; }
    public int IllnessTypeId { get; set; }
    public string CertificateNumber { get; set; } = null!;

    /// <summary>Archivo opcional (certificado médico). Si se omite, se crea el registro sin adjunto.</summary>
    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}
