namespace WsUtaSystem.Application.Interfaces.Services;

public interface IAttendanceCalculationService
{
    // Nuevo flujo oficial
    Task ProcessAttendanceRunRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);

    Task ProcessAttendanceRunDateAsync(
        DateTime workDate,
        CancellationToken ct = default);

    // Compatibilidad temporal con endpoints / jobs anteriores
    [Obsolete("Use ProcessAttendanceRunRangeAsync instead.")]
    Task ProcessAttendanceRange(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);

    [Obsolete("Legacy method. Use the attendance pipeline instead.")]
    Task CalculateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default);

    [Obsolete("Legacy method. Night minutes are now processed inside the attendance pipeline.")]
    Task CalculateNightMinutesAsync(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default);

    [Obsolete("Legacy method. Justifications are now processed inside the attendance pipeline.")]
    Task ProcessApplyJustification(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default);

    [Obsolete("Legacy method. Overtime and recovery are now processed inside the attendance pipeline.")]
    Task ProcessApplyOvertimeRecovery(
        DateTime fromDate,
        DateTime toDate,
        int? employeeId = null,
        CancellationToken ct = default);
}