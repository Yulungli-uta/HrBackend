using WsUtaSystem.Application.DTOs.Trainings;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Cliente HTTP hacia DEaDV (Dirección de Desarrollo Académico), origen de los certificados de
/// capacitación. La URL se lee del parámetro DEADV_API_URL (HR.tbl_Parameters). Nunca lanza al llamador: ante cualquier falla
/// registra una advertencia (sin cédula) y retorna null.
/// </summary>
public interface IDeadvClient
{
    /// <summary>
    /// Certificados de la persona. null SOLO si no se pudo consultar el servicio; lista vacía si
    /// la persona simplemente no tiene certificados.
    /// </summary>
    Task<IReadOnlyList<DeadvCertificateDto>?> GetCertificatesAsync(string idCard, CancellationToken ct = default);

    /// <summary>
    /// Descarga el PDF de un certificado. Solo acepta URLs HTTPS de hosts uta.edu.ec
    /// (anti-SSRF), no sigue redirecciones, exige application/pdf y limita el tamaño. Retorna null
    /// si la URL no es válida o la descarga falla.
    /// </summary>
    Task<byte[]?> DownloadCertificatePdfAsync(string certificateUrl, CancellationToken ct = default);
}
