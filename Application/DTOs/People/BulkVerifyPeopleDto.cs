using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.People;

public class BulkVerifyPeopleRequestDto
{
    [Required, MinLength(1, ErrorMessage = "La lista de identificaciones no puede estar vacía.")]
    public List<string> Identifications { get; set; } = [];
}

public class BulkVerifyResultItemDto
{
    public string Identification { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public PeopleDto? Person { get; set; }
}
