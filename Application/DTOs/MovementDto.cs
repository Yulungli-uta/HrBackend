namespace WsUtaSystem.Application.DTOs
{
    public class MovementDto
    {
        public long MovementID { get; set; }

        public DateTime MovementAt { get; set; }

        public string SourceModule { get; set; } = string.Empty;

        public string SourceID { get; set; } = string.Empty;

        // Cambios de saldo
        public int DeltaVacationMin { get; set; }

        public int DeltaRecoveryMin { get; set; }

        // Detalle / auditoría
        public string Note { get; set; } = string.Empty;

        public int? PerformedByEmpID { get; set; }
    }
}
