using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/files")]
public class FileManagementController : ControllerBase
{
    private readonly IFileManagementService _fileService;

    public FileManagementController(IFileManagementService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// Sube un archivo al servidor con gestión automática de carpetas por año.
    /// </summary>
    /// <param name="directoryCode">Código del directorio configurado en DirectoryParameters</param>
    /// <param name="relativePath">Ruta relativa base (ej: /contracts/)</param>
    /// <param name="fileName">Nombre del archivo a guardar</param>
    /// <param name="file">Archivo a subir</param>
    /// <returns>Información del archivo subido incluyendo la ruta completa</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(
        [FromForm] string directoryCode,
        [FromForm] string relativePath,
        [FromForm] string fileName,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file uploaded." });
        }

        var request = new FileUploadRequestDto
        {
            DirectoryCode = directoryCode,
            RelativePath = relativePath,
            FileName = fileName,
            File = file
        };

        var result = await _fileService.UploadFileAsync(request, ct);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Descarga un archivo del servidor.
    /// </summary>
    /// <param name="directoryCode">Código del directorio configurado en DirectoryParameters</param>
    /// <param name="filePath">Ruta relativa completa del archivo (ej: /contracts/2025/contrato_001.pdf)</param>
    /// <returns>Archivo como stream para descarga</returns>
    [HttpGet("download/{directoryCode}")]
    public async Task<IActionResult> DownloadFile(
        [FromRoute] string directoryCode,
        [FromQuery] string filePath,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { success = false, message = "File path is required." });
        }

        var result = await _fileService.DownloadFileAsync(directoryCode, filePath, ct);

        if (result == null)
        {
            return NotFound(new { success = false, message = "File not found or directory not configured." });
        }

        return File(result.Value.fileBytes, result.Value.contentType, result.Value.fileName);
    }

    /// <summary>
    /// Verifica si un archivo existe en el servidor.
    /// </summary>
    /// <param name="directoryCode">Código del directorio</param>
    /// <param name="filePath">Ruta relativa del archivo</param>
    /// <returns>True si el archivo existe, False en caso contrario</returns>
    [HttpGet("exists/{directoryCode}")]
    public async Task<IActionResult> FileExists(
        [FromRoute] string directoryCode,
        [FromQuery] string filePath,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return BadRequest(new { success = false, message = "File path is required." });
        }

        var result = await _fileService.DownloadFileAsync(directoryCode, filePath, ct);

        return Ok(new { exists = result != null });
    }
}

