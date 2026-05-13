using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class ContractRequestPerson : IAuditable
    {
        public int RequestPersonId { get; set; }
        public int RequestId { get; set; }
        public int? PersonId { get; set; }
        public int JobId { get; set; }

        /// <summary>FK → HR.ref_Types, Category=JOB_TYPE (ADMINISTRATIVO | DOCENTE).</summary>
        public int RequestPersonTypeId { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>Solo DOCENTE: horas clase semanales.</summary>
        public decimal? WeeklyClassHours { get; set; }

        /// <summary>Solo DOCENTE: valor por hora.</summary>
        public decimal? HourValue { get; set; }

        /// <summary>Meses calculados del período (EndDate - StartDate / 30).</summary>
        public decimal? MonthsPeriod { get; set; }

        /// <summary>RMU mensual. ADMIN = RMU del cargo; DOCENTE = WeeklyClassHours × HourValue × 4.</summary>
        public decimal? Rmu { get; set; }

        /// <summary>RMU total del período (Rmu × MonthsPeriod).</summary>
        public decimal? RmuPeriod { get; set; }

        /// <summary>FK → HR.ref_Types, Category=CONTRACT_REQUEST_PERSON_SOURCE.</summary>
        public int? EntrySourceId { get; set; }

        public bool IsHired { get; set; } = false;
        public int? ContractId { get; set; }

        /// <summary>FK → HR.ref_Types, Category=CONTRACT_REQUEST_PERSON_STATUS (PENDIENTE | CONTRATADO | INACTIVO).</summary>
        public int? StatusId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public ContractRequest? Request { get; set; }
        public People? Person { get; set; }
        public Job? Job { get; set; }
        public Contracts? Contract { get; set; }
    }
}
