namespace WsUtaSystem.Application.DTOs.EmployeeLaborRegime;

public class EmployeeLaborRegimeDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeEmail { get; set; }

    public int LaborRegimeId { get; set; }
    public string? LaborRegimeName { get; set; }

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public int? JobId { get; set; }
    public string? JobName { get; set; }

    public bool IsIndefinite { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public int? SourceContractId { get; set; }
    public int? SourcePersonnelActionId { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsPrincipal { get; set; }

    /// <summary>SIIES INGRESO_POR_CONCURSO. Null = sin clasificar todavía.</summary>
    public bool? IngresoPorConcurso { get; set; }

    /// <summary>SIIES RELACION_IES manual (ref_Types SIIES_RELACION_IES). Null = se deriva del documento que respalda el régimen.</summary>
    public int? SiiesRelacionIesTypeId { get; set; }

    /// <summary>Nombre de la relación manual (para mostrar), si existe.</summary>
    public string? SiiesRelacionIesName { get; set; }
}

public class EmployeeLaborRegimeCreateDto
{
    public int EmployeeId { get; set; }
    public int LaborRegimeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? JobId { get; set; }
    public bool IsIndefinite { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public int? SourceContractId { get; set; }
    public int? SourcePersonnelActionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>SIIES INGRESO_POR_CONCURSO. Opcional al crear; puede completarse después con SetIngresoPorConcursoAsync.</summary>
    public bool? IngresoPorConcurso { get; set; }

    /// <summary>SIIES RELACION_IES manual. Opcional al crear; puede completarse después con SetSiiesRelacionIesAsync.</summary>
    public int? SiiesRelacionIesTypeId { get; set; }
}

/// <summary>Cierra un régimen activo (renuncia, vencimiento, cambio de asignación).</summary>
public class EmployeeLaborRegimeCloseDto
{
    public DateOnly EffectiveTo { get; set; }
}

/// <summary>Clasifica (o corrige) el ingreso por concurso de un régimen ya existente.</summary>
public class EmployeeLaborRegimeIngresoPorConcursoDto
{
    public bool IngresoPorConcurso { get; set; }
}

/// <summary>Fija (o quita, con null) la relación IES manual de un régimen ya existente.</summary>
public class EmployeeLaborRegimeSiiesRelacionIesDto
{
    /// <summary>TypeId de ref_Types (Category='SIIES_RELACION_IES'); null quita el valor manual y vuelve a derivarse del documento.</summary>
    public int? SiiesRelacionIesTypeId { get; set; }
}
