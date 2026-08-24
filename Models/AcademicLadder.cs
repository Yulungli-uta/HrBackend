using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

public class AcademicLadder : IAuditable
{
    public int LadderId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? CategoryTypeId { get; set; }
    public int? LevelTypeId { get; set; }
    public int Sequence { get; set; }
    public int? NextLadderId { get; set; }
    public int? MinYearsService { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    /// <summary>Tipo de dedicación: TC / MT / TP / Exclusiva. Nulo en los 7 escalones base (sin dedicación explícita).</summary>
    public int? DedicationTypeId { get; set; }

    /// <summary>Sueldo base de referencia para esta combinación Categoría × Dedicación.</summary>
    public decimal? BaseRmu { get; set; }

    /// <summary>Piso real observado de RMU para este escalón (matriz de personal). Complementa
    /// a BaseRmu, que es un valor único de referencia — en la práctica el sueldo varía dentro
    /// de un rango para el mismo escalón (antigüedad, negociación puntual, etc.).</summary>
    public decimal? RmuMin { get; set; }

    /// <summary>Techo real observado de RMU para este escalón. Ver RmuMin.</summary>
    public decimal? RmuMax { get; set; }

    /// <summary>Denominación exacta exigida por el catálogo SIIES (Tabla 9) para este escalón, cuando aplica.</summary>
    public string? SiiesLabel { get; set; }

    /// <summary>Agrupación institucional (ref_Types.Category=ACADEMIC_GROUP_TYPE, ej. "Personal
    /// Académico Agregado") tal como aparece en la matriz de personal — misma categoría que
    /// CategoryTypeId, solo que con la nomenclatura usada en ese documento.</summary>
    public int? AcademicGroupTypeId { get; set; }

    public virtual RefTypes? CategoryType { get; set; }
    public virtual RefTypes? LevelType { get; set; }
    public virtual RefTypes? DedicationType { get; set; }
    public virtual RefTypes? AcademicGroupType { get; set; }
    public virtual AcademicLadder? NextLadder { get; set; }
}
