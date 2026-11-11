namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

public sealed class PersonnelActionTypeCreateDto
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
    public string? ActionCategory { get; set; }
    public int? SiiesRelacionIesTypeId { get; set; }

    /// <summary>Tipo padre (un solo nivel). Null = tipo raíz.</summary>
    public int? ParentTypeId { get; set; }

    /// <summary>Formato del número con tokens {PREFIX}, {SEQ}, {YEAR}. Null = formato por defecto.</summary>
    public string? NumberingFormat { get; set; }

    /// <summary>true = la secuencia se reinicia cada año; false = acumulada.</summary>
    public bool NumberingResetsYearly { get; set; } = true;
}
