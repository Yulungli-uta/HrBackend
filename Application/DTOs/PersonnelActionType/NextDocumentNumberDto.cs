namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

/// <summary>Respuesta al solicitar el siguiente número de documento.</summary>
public sealed record NextDocumentNumberDto(
    string DocumentNumber,
    string Prefix,
    int Year,
    int Sequence
);

/// <summary>Solicitud para vincular una plantilla predeterminada a un tipo de acción de personal.</summary>
public sealed record SetDefaultTemplateRequest(int? TemplateId);
