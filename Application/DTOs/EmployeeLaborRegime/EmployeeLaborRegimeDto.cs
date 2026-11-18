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

    /// <summary>Fin del documento (contrato con fecha de fin). Null = abierto. Lo usa <c>RegisterDocumentAsync</c>.</summary>
    public DateOnly? EffectiveTo { get; set; }

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

/// <summary>Qué hizo la sincronización de la posición (departamento/cargo) de una acción de personal con su régimen laboral.</summary>
public enum LaborRegimeSyncOutcome
{
    /// <summary>La acción no aplica (estado distinto de VIGENTE/FINALIZADO, sin empleado o sin departamento/cargo de destino).</summary>
    NotApplicable,
    /// <summary>El régimen ya tenía el departamento y el cargo de destino, o no hay cambio de posición que registrar.</summary>
    NoChange,
    /// <summary>Se actualizó el departamento/cargo del régimen que nació de la acción.</summary>
    Updated,
    /// <summary>La acción no tenía régimen y se creó uno por cambio de departamento o cargo.</summary>
    Created,
    /// <summary>Se omitió: el empleado tiene una acción posterior con otro departamento o cargo.</summary>
    SkippedLaterMovement,
    /// <summary>Se omitió la creación: la acción no indica el tipo de empleado/régimen.</summary>
    SkippedNoRegimeType,
    /// <summary>Se omitió la creación: el empleado ya tiene ese régimen activo respaldado por otro documento.</summary>
    SkippedActiveRegimeExists,
}

/// <summary>Resultado de <c>SyncPositionFromPersonnelActionAsync</c>.</summary>
/// <param name="Outcome">Qué ocurrió.</param>
/// <param name="RegimeId">Régimen actualizado o creado (null si no se tocó ninguno).</param>
/// <param name="Message">Texto para bitácora/historial.</param>
public sealed record LaborRegimeSyncResult(LaborRegimeSyncOutcome Outcome, int? RegimeId, string Message);
