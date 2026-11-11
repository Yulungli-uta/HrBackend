namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed class PersonnelActionTypeUpdateDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public string NumberingPrefix { get; set; } = null!;
    public int? DefaultTemplateId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresAdUserCreation { get; set; } = false;
    public bool RequiresAdUserDisable { get; set; } = false;
    public bool RequiresAdGroupAssignment { get; set; } = false;
    /// <summary>Categoría funcional. Null = conservar lo guardado; cadena vacía = borrar.</summary>
    public string? ActionCategory { get; set; }

    /// <summary>Homologación SIIES. Null = conservar lo guardado; 0 = borrar.</summary>
    public int? SiiesRelacionIesTypeId { get; set; }

    /// <summary>Tipo padre. Null = conservar lo guardado; 0 = dejar como tipo raíz (evita que un cliente anterior desligue los hijos).</summary>
    public int? ParentTypeId { get; set; }

    /// <summary>Formato del número. Null = conservar; cadena vacía = volver al formato por defecto.</summary>
    public string? NumberingFormat { get; set; }

    /// <summary>Reinicio anual de la secuencia. Null = conservar lo guardado.</summary>
    public bool? NumberingResetsYearly { get; set; }
}
