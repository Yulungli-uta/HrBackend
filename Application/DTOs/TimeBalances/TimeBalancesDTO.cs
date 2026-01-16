namespace WsUtaSystem.Application.DTOs.TimeBalances.TimeBalancesDTO
{
    //public class TimeBalancesDTO
    //{
        public class TimeBalancesCreateDTO
        {
            public int EmployeeID { get; set; }
            public int VacationAvailableMin { get; set; }
            public int RecoveryPendingMin { get; set; }
        }

        public class TimeBalancesUpdateDTO
        {
            public int EmployeeID { get; set; }
            public int VacationAvailableMin { get; set; }
            public int RecoveryPendingMin { get; set; }
            public DateTime LastUpdated { get; set; }
        }
        public class TimeBalancesResponseDTO
        {
            public int EmployeeID { get; set; }
            public int VacationAvailableMin { get; set; }
            public int RecoveryPendingMin { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    //}
}
