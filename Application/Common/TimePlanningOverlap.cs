namespace WsUtaSystem.Application.Common
{
    /// <summary>
    /// Regla de cruce entre planificaciones (horas extra / recuperación) de un mismo empleado.
    /// Cada plan es una ventana horaria diaria repetida entre su fecha inicio y fin; una hora fin
    /// igual o menor a la hora inicio (ej. 00:00) se interpreta como el día siguiente.
    /// </summary>
    public static class TimePlanningOverlap
    {
        private const int MaxDaysPerPlan = 400;

        /// <summary>Indica si alguna ventana diaria del plan A se cruza con alguna del plan B.</summary>
        public static bool Overlaps(
            DateTime startDateA, DateTime endDateA, TimeSpan startTimeA, TimeSpan endTimeA,
            DateTime startDateB, DateTime endDateB, TimeSpan startTimeB, TimeSpan endTimeB)
        {
            var windowsB = BuildWindows(startDateB, endDateB, startTimeB, endTimeB);
            foreach (var a in BuildWindows(startDateA, endDateA, startTimeA, endTimeA))
            {
                foreach (var b in windowsB)
                {
                    if (a.Start < b.End && a.End > b.Start)
                        return true;
                }
            }
            return false;
        }

        private static List<(DateTime Start, DateTime End)> BuildWindows(
            DateTime startDate, DateTime endDate, TimeSpan startTime, TimeSpan endTime)
        {
            var crossesMidnight = endTime <= startTime;
            var windows = new List<(DateTime, DateTime)>();
            var days = Math.Min((endDate.Date - startDate.Date).Days, MaxDaysPerPlan);

            for (var i = 0; i <= days; i++)
            {
                var day = startDate.Date.AddDays(i);
                var start = day + startTime;
                var end = (crossesMidnight ? day.AddDays(1) : day) + endTime;
                windows.Add((start, end));
            }
            return windows;
        }
    }
}
