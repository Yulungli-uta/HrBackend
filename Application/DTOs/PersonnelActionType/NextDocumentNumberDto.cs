namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

/// <summary>Respuesta al solicitar el siguiente número de documento.</summary>
public sealed record NextDocumentNumberDto(
    string DocumentNumber,
    string Prefix,
    int Year,
    int Sequence
);
