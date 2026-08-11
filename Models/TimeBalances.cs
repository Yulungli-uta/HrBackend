namespace WsUtaSystem.Models
{
    /// <summary>
    /// Saldo de tiempo de un empleado, separado por régimen laboral.
    /// Clave compuesta (EmployeeID, LaborRegimeId) — un empleado puede tener
    /// una fila por cada régimen activo (LOSEP/LOES/Código Trabajo).
    /// Mapea HR.tbl_TimeBalances.
    /// </summary>
    public class TimeBalances
    {
        public int EmployeeID { get; set; }

        /// <summary>FK -> HR.ref_Types (Category='CONTRACT_TYPE'). Parte de la clave compuesta.</summary>
        public int LaborRegimeId { get; set; }

        public int VacationAvailableMin { get; set; }
        public int RecoveryPendingMin { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>Token de concurrencia optimista (columna SQL Server rowversion/timestamp).</summary>
        public byte[]? RowVersion { get; set; }
    }
}
