namespace WsUtaSystem.Application.DTOs.Contracts
{
    public class ContractChangeStatusDto
    {
        /// <summary>
        /// TypeID del estado destino (RefType).
        /// Ej: APROBADO, FIRMADO, ANULADO, etc.
        /// </summary>
        public int ToStatusTypeID { get; set; }

        /// <summary>
        /// Comentario opcional para el histórico del cambio de estado.
        /// </summary>
        public string? Comment { get; set; }
    }
}
