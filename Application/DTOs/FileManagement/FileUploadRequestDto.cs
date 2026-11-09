using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.FileManagement
{
    public class FileUploadRequestDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string RelativePath { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public IFormFile File { get; set; } = default!;

        /// <summary>
        /// Opcional. Carpeta relativa EXACTA dentro del directorio (por ejemplo "2021/anexos/FCS-DTH-076-2021"). Si viene junto con
        /// <see cref="TargetFileName"/>, no se agrega el año de la subida ni se genera un nombre con GUID.
        /// </summary>
        public string? TargetRelativeFolder { get; set; }

        /// <summary>Opcional. Nombre del archivo indicado por la política de ubicación. Nunca sobrescribe: si existe, agrega _2, _3…</summary>
        public string? TargetFileName { get; set; }

        /// <summary>Opcional. Año que se informa como <c>UploadYear</c> cuando se usa <see cref="TargetRelativeFolder"/> (año del documento).</summary>
        public int? TargetYear { get; set; }
    }
}

