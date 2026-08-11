namespace WsUtaSystem.Models
{
    /// <summary>
    /// Movimiento auditado de saldo de tiempo (acreditación, reserva, consumo, liberación,
    /// ajuste manual). Mapea HR.tbl_TimeBalanceMovements. Ledger de solo inserción — nunca
    /// se actualiza ni se borra un movimiento ya escrito.
    /// </summary>
    public class TimeBalanceMovements
    {
        public int MovementID { get; set; }
        public int EmployeeID { get; set; }
        public int DeltaVacationMin { get; set; }
        public int DeltaRecoveryMin { get; set; }
        public DateTime MovementAt { get; set; }

        /// <summary>Ej. 'VACATION_ACCRUAL_MONTHLY_CT', 'MANUAL_ADJUSTMENT', 'BULK_LOAD_CT_2026'.</summary>
        public string? SourceModule { get; set; }
        public string? SourceTable { get; set; }
        public string? SourceID { get; set; }
        public int? PerformedByEmpID { get; set; }
        public string? Note { get; set; }

        /// <summary>FK -> HR.ref_Types (Category='CONTRACT_TYPE'). Régimen al que aplica este movimiento.</summary>
        public int? LaborRegimeId { get; set; }
    }
}
