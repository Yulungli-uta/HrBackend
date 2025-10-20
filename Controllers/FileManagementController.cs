using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("files")]
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
    /// <remarks>
    /// Enviar como <c>multipart/form-data</c> con los campos:
    /// <list type="bullet">
    /// <item><description><c>DirectoryCode</c> (obligatorio)</description></item>
    /// <item><description><c>RelativePath</c> (opcional)</description></item>
    /// <item><description><c>FileName</c> (opcional)</description></item>
    /// <item><description><c>File</c> (obligatorio, <see cref="IFormFile"/>)</description></item>
    /// </list>
    /// </remarks>
    /// <returns>Información del archivo subido incluyendo la ruta completa</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        [FromForm] FileUploadRequestDto form,
        CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file uploaded." });
        }

        var request = new FileUploadRequestDto
        {
            DirectoryCode = form.DirectoryCode,
            RelativePath = form.RelativePath ?? string.Empty,
            FileName = string.IsNullOrWhiteSpace(form.FileName) ? form.File.FileName : form.FileName!,
            File = form.File
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        [FromRoute] string directoryCode,
        [FromQuery] string filePath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath))
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
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FileExists(
        [FromRoute] string directoryCode,
        [FromQuery] string filePath,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest(new { success = false, message = "File path is required." });
        }

        var result = await _fileService.DownloadFileAsync(directoryCode, filePath, ct);

        return Ok(new { exists = result != null });
    }
}
