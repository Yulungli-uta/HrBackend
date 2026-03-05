using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades de asistencia y tiempo del módulo HR:
/// Schedules, EmployeeSchedules, AttendancePunches, PunchJustifications,
/// AttendanceCalculations, Overtime, OvertimeConfig, TimeRecoveryPlans,
/// TimeRecoveryLogs, TimePlanning, TimePlanningEmployee, TimePlanningExecution,
/// TimeBalances, Holiday, Activity, AdditionalActivity, JobActivity.
/// </summary>
public sealed class SchedulesConfiguration : IEntityTypeConfiguration<Schedules>
{
    public void Configure(EntityTypeBuilder<Schedules> e)
    {
        e.ToTable("tbl_Schedules", "HR");
        e.HasKey(x => x.ScheduleId);
        e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        e.Property(x => x.Description).HasMaxLength(120).IsRequired();
        e.Property(x => x.WorkingDays).HasMaxLength(20).IsRequired();
        e.Property(x => x.RotationPattern).HasMaxLength(120);
    }
}

public sealed class EmployeeSchedulesConfiguration : IEntityTypeConfiguration<EmployeeSchedules>
{
    public void Configure(EntityTypeBuilder<EmployeeSchedules> e)
    {
        e.ToTable("tbl_EmployeeSchedules", "HR");
        e.HasKey(x => x.EmpScheduleId);
        e.Property(x => x.EmpScheduleId).HasColumnName("EmpScheduleID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
    }
}

public sealed class AttendancePunchesConfiguration : IEntityTypeConfiguration<AttendancePunches>
{
    public void Configure(EntityTypeBuilder<AttendancePunches> e)
    {
        e.ToTable("tbl_AttendancePunches", "HR", tb =>
        {
            tb.HasTrigger("trg_Punch_Validations");
        });
        e.HasKey(x => x.PunchId);
        e.Property(x => x.PunchId).HasColumnName("PunchID").ValueGeneratedOnAdd().UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.PunchTime).HasColumnType("datetime2");
        e.Property(x => x.PunchType).HasMaxLength(10);
        e.Property(x => x.DeviceId).HasMaxLength(60);
        e.Property(x => x.IpAddress).HasColumnName("IpAddress");
        e.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.Property(x => x.RowVersion)
            .IsRowVersion()
            .HasColumnName("RowVersion")
            .IsConcurrencyToken();
    }
}

public sealed class PunchJustificationsConfiguration : IEntityTypeConfiguration<PunchJustifications>
{
    public void Configure(EntityTypeBuilder<PunchJustifications> e)
    {
        e.ToTable("tbl_PunchJustifications", "HR");
        e.HasKey(x => x.PunchJustId);
        e.Property(x => x.PunchJustId).HasColumnName("PunchJustID").ValueGeneratedOnAdd();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID").IsRequired();
        e.Property(x => x.BossEmployeeId).HasColumnName("BossEmployeeID").IsRequired();
        e.Property(x => x.JustificationTypeId).HasColumnName("JustificationTypeID").IsRequired();
        e.Property(x => x.PunchTypeId).HasColumnName("PunchTypeID");
    }
}

public sealed class AttendanceCalculationsConfiguration : IEntityTypeConfiguration<AttendanceCalculations>
{
    public void Configure(EntityTypeBuilder<AttendanceCalculations> e)
    {
        e.ToTable("tbl_AttendanceCalculations", "HR");
        e.HasKey(x => x.CalculationId);
        e.Property(x => x.CalculationId).HasColumnName("CalculationID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.Status).HasMaxLength(12);
    }
}

public sealed class OvertimeConfigConfiguration : IEntityTypeConfiguration<OvertimeConfig>
{
    public void Configure(EntityTypeBuilder<OvertimeConfig> e)
    {
        e.ToTable("tbl_OvertimeConfig", "HR");
        e.HasKey(x => x.OvertimeType);
        e.Property(x => x.OvertimeType).HasMaxLength(50);
        e.Property(x => x.Description).HasMaxLength(200);
    }
}

public sealed class OvertimeConfiguration : IEntityTypeConfiguration<Overtime>
{
    public void Configure(EntityTypeBuilder<Overtime> e)
    {
        e.ToTable("tbl_Overtime", "HR");
        e.HasKey(x => x.OvertimeId);
        e.Property(x => x.OvertimeId).HasColumnName("OvertimeID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.OvertimeType).HasMaxLength(50);
        e.Property(x => x.Status).HasMaxLength(20);
    }
}

public sealed class TimeRecoveryPlansConfiguration : IEntityTypeConfiguration<TimeRecoveryPlans>
{
    public void Configure(EntityTypeBuilder<TimeRecoveryPlans> e)
    {
        e.ToTable("tbl_TimeRecoveryPlans", "HR");
        e.HasKey(x => x.RecoveryPlanId);
        e.Property(x => x.RecoveryPlanId).HasColumnName("RecoveryPlanID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.Reason).HasMaxLength(300);
    }
}

public sealed class TimeRecoveryLogsConfiguration : IEntityTypeConfiguration<TimeRecoveryLogs>
{
    public void Configure(EntityTypeBuilder<TimeRecoveryLogs> e)
    {
        e.ToTable("tbl_TimeRecoveryLogs", "HR");
        e.HasKey(x => x.RecoveryLogId);
        e.Property(x => x.RecoveryLogId).HasColumnName("RecoveryLogID");
        e.Property(x => x.RecoveryPlanId).HasColumnName("RecoveryPlanID");
    }
}

public sealed class TimePlanningConfiguration : IEntityTypeConfiguration<TimePlanning>
{
    public void Configure(EntityTypeBuilder<TimePlanning> e)
    {
        e.ToTable("tbl_TimePlanning", "HR");
        e.HasKey(x => x.PlanID);
        e.Property(x => x.PlanID).HasColumnName("PlanID");
        e.Property(x => x.RowVersion).IsRowVersion();
    }
}

public sealed class TimePlanningEmployeeConfiguration : IEntityTypeConfiguration<TimePlanningEmployee>
{
    public void Configure(EntityTypeBuilder<TimePlanningEmployee> e)
    {
        e.ToTable("tbl_TimePlanningEmployees", "HR");
        e.HasKey(x => x.PlanEmployeeID);
        e.Property(x => x.PlanEmployeeID).HasColumnName("PlanEmployeeID");
        e.Property(x => x.PlanID).HasColumnName("PlanID");
        e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
        e.HasOne(x => x.TimePlanning)
            .WithMany()
            .HasForeignKey(x => x.PlanID)
            .HasConstraintName("FK_TimePlanningEmployees_Plan")
            .OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Employees)
            .WithMany()
            .HasForeignKey(x => x.EmployeeID)
            .HasConstraintName("FK_TimePlanningEmployees_Employee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TimePlanningExecutionConfiguration : IEntityTypeConfiguration<TimePlanningExecution>
{
    public void Configure(EntityTypeBuilder<TimePlanningExecution> e)
    {
        e.ToTable("tbl_TimePlanningExecution", "HR");
        e.HasKey(x => x.ExecutionID);
        e.Property(x => x.ExecutionID).HasColumnName("ExecutionID");
        e.Property(x => x.PlanEmployeeID).HasColumnName("PlanEmployeeID");
    }
}

public sealed class TimeBalancesConfiguration : IEntityTypeConfiguration<TimeBalances>
{
    public void Configure(EntityTypeBuilder<TimeBalances> e)
    {
        e.ToTable("tbl_TimeBalances", "HR");
        e.HasKey(x => x.EmployeeID);
        e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
    }
}

public sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> e)
    {
        e.ToTable("tbl_Holidays", "HR");
        e.HasKey(x => x.HolidayID);
        e.Property(x => x.HolidayID).HasColumnName("HolidayID");
    }
}

public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> e)
    {
        e.ToTable("tbl_Activities", "HR");
        e.HasKey(x => x.ActivitiesId);
        e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
    }
}

public sealed class AdditionalActivityConfiguration : IEntityTypeConfiguration<AdditionalActivity>
{
    public void Configure(EntityTypeBuilder<AdditionalActivity> e)
    {
        e.ToTable("tbl_AdditionalActivities", "HR");
        e.HasKey(x => new { x.ActivitiesId, x.ContractId });
        e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
        e.Property(x => x.ContractId).HasColumnName("ContractID");
    }
}

public sealed class JobActivityConfiguration : IEntityTypeConfiguration<JobActivity>
{
    public void Configure(EntityTypeBuilder<JobActivity> e)
    {
        e.ToTable("tbl_JobActivities", "HR");
        e.HasKey(x => new { x.ActivitiesId, x.JobID });
        e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
        e.Property(x => x.JobID).HasColumnName("JobID");
    }
}
