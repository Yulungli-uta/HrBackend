using WsUtaSystem.Application.DTOs.StoredFile;

namespace WsUtaSystem.Application.DTOs.Books;

public class BookWithDocumentResultDto
{
    public BooksDto Book { get; set; } = null!;
    public StoredFileDto? StoredFile { get; set; }
}
