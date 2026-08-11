using System.Text.Json.Serialization;

namespace WsUtaSystem.Application.DTOs.TimeBalances;

/// <summary>
/// Incrementar/Descontar aplica ValueMinutes como delta sobre el saldo actual.
/// Establecer fija el saldo resultante exactamente en ValueMinutes (el delta real
/// se calcula internamente para que el movimiento auditado siempre quede como delta).
/// </summary>
/// <remarks>
/// [JsonConverter(JsonStringEnumConverter)] aquí porque AddControllersConfiguration
/// (Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs) no registra un
/// convertidor de enums a string global — sin esto, System.Text.Json exige que el
/// frontend mande 0/1 en vez de "Increment"/"Set", y el body falla en deserializar.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VacationBalanceAdjustmentMode
{
    Increment,
    Set
}

/// <summary>Bolsa afectada por el ajuste — son independientes, un ajuste nunca toca ambas a la vez.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeBalanceField
{
    /// <summary>HR.tbl_TimeBalances.VacationAvailableMin.</summary>
    Vacation,
    /// <summary>HR.tbl_TimeBalances.RecoveryPendingMin (incluye recuperación de horas por pandemia).</summary>
    Recovery
}

public class VacationBalanceAdjustmentDto
{
    public int EmployeeId { get; set; }

    /// <summary>Nombre exacto en HR.ref_Types (Category='CONTRACT_TYPE'): "LOSEP" | "LOES" | "Código Trabajo".</summary>
    public string LaborRegimeName { get; set; } = null!;

    public TimeBalanceField BalanceField { get; set; } = TimeBalanceField.Vacation;
    public VacationBalanceAdjustmentMode Mode { get; set; }
    public int ValueMinutes { get; set; }

    /// <summary>Obligatorio siempre, no solo cuando el resultado queda negativo.</summary>
    public string Reason { get; set; } = null!;

    public bool AllowNegativeResult { get; set; }
}

public class VacationBalanceAdjustmentResultDto
{
    public int EmployeeId { get; set; }
    public int LaborRegimeId { get; set; }
    public int PreviousBalanceMin { get; set; }
    public int NewBalanceMin { get; set; }
    public int DeltaAppliedMin { get; set; }
}

/// <summary>Una fila de la carga masiva (ej. el listado de Código de Trabajo). Identifica al empleado por cédula, no por EmployeeId.</summary>
public class VacationBalanceBulkAdjustmentItemDto
{
    public string Cedula { get; set; } = null!;
    public string LaborRegimeName { get; set; } = null!;
    public TimeBalanceField BalanceField { get; set; } = TimeBalanceField.Vacation;
    public VacationBalanceAdjustmentMode Mode { get; set; } = VacationBalanceAdjustmentMode.Set;
    public int ValueMinutes { get; set; }
    public string Reason { get; set; } = null!;
    public bool AllowNegativeResult { get; set; }
}

public class VacationBalanceBulkAdjustmentRequestDto
{
    /// <summary>Identifica el lote en el movimiento auditado, ej. "CT_2026". SourceModule queda como BULK_LOAD_{BatchTag}.</summary>
    public string BatchTag { get; set; } = null!;
    public List<VacationBalanceBulkAdjustmentItemDto> Items { get; set; } = [];
}

public class VacationBalanceBulkAdjustmentRowResultDto
{
    public string Cedula { get; set; } = null!;
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int? PreviousBalanceMin { get; set; }
    public int? NewBalanceMin { get; set; }
}

/// <summary>Saldo actual de un empleado en un régimen específico — para precargar la pantalla de ajuste antes de aplicar nada.</summary>
public class CurrentTimeBalanceDto
{
    public int EmployeeId { get; set; }
    public int LaborRegimeId { get; set; }
    public string LaborRegimeName { get; set; } = null!;
    public int VacationAvailableMin { get; set; }
    public int RecoveryPendingMin { get; set; }
}

/// <summary>Una fila del buzón de liquidaciones pendientes: régimen cerrado (contrato terminado) con saldo todavía sin liquidar (vacaciones y/o recuperación de horas).</summary>
public class PendingVacationSettlementDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public int LaborRegimeId { get; set; }
    public string LaborRegimeName { get; set; } = null!;
    public DateOnly? RegimeEffectiveTo { get; set; }
    public int CurrentBalanceMin { get; set; }

    /// <summary>HR.tbl_TimeBalances.RecoveryPendingMin del régimen cerrado — puede quedar a favor (positivo) o en contra (negativo) del empleado.</summary>
    public int CurrentRecoveryBalanceMin { get; set; }

    /// <summary>Motivo del cierre del régimen: Renuncia, Jubilación, Fin de Contrato, Acción de personal, o Cierre manual.</summary>
    public string TriggerReason { get; set; } = "Cierre manual";
}

public class VacationSettlementRequestDto
{
    public int EmployeeId { get; set; }
    public string LaborRegimeName { get; set; } = null!;
    public string Reason { get; set; } = null!;
}

/// <summary>Resultado de liquidar un régimen cerrado — ambas bolsas (vacaciones y recuperación) quedan en 0 en la misma transacción.</summary>
public class VacationSettlementResultDto
{
    public int EmployeeId { get; set; }
    public int LaborRegimeId { get; set; }
    public int PreviousVacationBalanceMin { get; set; }
    public int NewVacationBalanceMin { get; set; }
    public int PreviousRecoveryBalanceMin { get; set; }
    public int NewRecoveryBalanceMin { get; set; }
}
