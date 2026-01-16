namespace WsUtaSystem.Application.DTOs.Documents
{
    // (2) DIFERENTES TIPOS PARA CADA ARCHIVO(mapped batch)
    public  class DocumentUploadMappedRequestDto
    {
        public string DirectoryCode { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string? RelativePath { get; set; }

        public List<DocumentUploadMappedItemDto> Items { get; set; } = new();
    }

    public  class DocumentUploadMappedItemDto
    {
        public int? DocumentTypeId { get; set; }

        // IMPORTANTE: el binder soporta Items[0].File, Items[1].File, etc.
        public IFormFile File { get; set; } = default!;
    }
}
