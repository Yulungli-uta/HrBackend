namespace WsUtaSystem.Application.DTOs.Reports.Common
{
  
        // Datos del PDF convertido a base64
        public record PdfPreviewDataDto(
            string Base64Data,
            string FileName,
            string MimeType
        );

        // Error estándar para preview
        public record PreviewErrorDto(
            string Message
        );

        // Respuesta genérica de preview
        public record PreviewResponseDto(
            string Status,               // "success" | "error"
            PdfPreviewDataDto? Data,
            PreviewErrorDto? Error
        );
    
}
