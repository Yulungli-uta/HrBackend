using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Views;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de vistas de base de datos del módulo HR y sistema de autenticación.
/// Las vistas son de solo lectura y no tienen clave primaria.
/// </summary>
public sealed class VwEmployeeCompleteConfiguration : IEntityTypeConfiguration<VwEmployeeComplete>
{
    public void Configure(EntityTypeBuilder<VwEmployeeComplete> e)
    {
        e.HasNoKey();
        e.ToView("vw_EmployeeComplete", "HR");
    }
}

public sealed class VwEmployeeDetailsConfiguration : IEntityTypeConfiguration<VwEmployeeDetails>
{
    public void Configure(EntityTypeBuilder<VwEmployeeDetails> e)
    {
        e.ToView("vw_EmployeeDetails", "HR");
        e.HasNoKey();
        e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
        e.Property(x => x.FirstName).HasColumnName("FirstName");
        e.Property(x => x.LastName).HasColumnName("LastName");
        e.Property(x => x.IDCard).HasColumnName("IDCard");
        e.Property(x => x.Email).HasColumnName("Email");
        e.Property(x => x.PersonnelEmail).HasColumnName("PersonnelEmail");
        e.Property(x => x.EmployeeType).HasColumnName("EmployeeType");
        e.Property(x => x.ContractType).HasColumnName("ContractType");
        e.Property(x => x.ImmediateBossID).HasColumnName("ImmediateBossID");
        e.Property(x => x.ScheduleID).HasColumnName("ScheduleID");
        e.Property(x => x.Schedule).HasColumnName("Schedule");
        e.Property(x => x.JobId).HasColumnName("JobID");
        e.Property(x => x.DepartmentID).HasColumnName("DepartmentID");        
        e.Property(x => x.Department).HasColumnName("Department");
        e.Property(x => x.BaseSalary).HasColumnName("BaseSalary");
        e.Property(x => x.HireDate).HasColumnName("HireDate");
    }
}

public sealed class VwEmployeeScheduleAtDateConfiguration : IEntityTypeConfiguration<VwEmployeeScheduleAtDate>
{
    public void Configure(EntityTypeBuilder<VwEmployeeScheduleAtDate> e)
    {
        e.HasNoKey();
        e.ToView("vw_EmployeeScheduleAtDate", "HR");
    }
}

public sealed class VwPunchDayConfiguration : IEntityTypeConfiguration<VwPunchDay>
{
    public void Configure(EntityTypeBuilder<VwPunchDay> e)
    {
        e.HasNoKey();
        e.ToView("vw_PunchDay", "HR");
    }
}

public sealed class VwLeaveWindowsConfiguration : IEntityTypeConfiguration<VwLeaveWindows>
{
    public void Configure(EntityTypeBuilder<VwLeaveWindows> e)
    {
        e.HasNoKey();
        e.ToView("vw_LeaveWindows", "HR");
    }
}

public sealed class VwAttendanceDayConfiguration : IEntityTypeConfiguration<VwAttendanceDay>
{
    public void Configure(EntityTypeBuilder<VwAttendanceDay> e)
    {
        e.HasNoKey();
        e.ToView("vw_AttendanceDay", "HR");
    }
}

public sealed class VwUserRoleConfiguration : IEntityTypeConfiguration<VwUserRole>
{
    public void Configure(EntityTypeBuilder<VwUserRole> e)
    {
        e.HasNoKey();
        // La vista vive en el esquema auth (junto con tbl_Roles/tbl_MenuItems/tbl_RoleMenuItems),
        // no en dbo -- confirmado contra INFORMATION_SCHEMA.VIEWS.
        e.ToView("vw_UserRoles", "auth");
    }
}

public sealed class VwRoleMenuItemConfiguration : IEntityTypeConfiguration<VwRoleMenuItem>
{
    public void Configure(EntityTypeBuilder<VwRoleMenuItem> e)
    {
        e.HasNoKey();
        e.ToView("vw_RoleMenuItems", "auth");
    }
}

public sealed class VwDepartmentWithTypeConfiguration : IEntityTypeConfiguration<VwDepartmentWithType>
{
    public void Configure(EntityTypeBuilder<VwDepartmentWithType> e)
    {
        e.HasNoKey();
        e.ToView("vw_Departments_WithType", "HR");
    }
}

public sealed class VwJobWithDegreeAndGroupConfiguration : IEntityTypeConfiguration<VwJobWithDegreeAndGroup>
{
    public void Configure(EntityTypeBuilder<VwJobWithDegreeAndGroup> e)
    {
        e.HasNoKey();
        e.ToView("vw_Jobs_WithDegreeAndGroup", "HR");
    }
}

public sealed class VwJobActivityConfiguration : IEntityTypeConfiguration<VwJobActivity>
{
    public void Configure(EntityTypeBuilder<VwJobActivity> e)
    {
        e.HasNoKey();
        e.ToView("vw_Job_Activities", "HR");
    }
}

public sealed class VwAuthorityConfiguration : IEntityTypeConfiguration<VwAuthority>
{
    public void Configure(EntityTypeBuilder<VwAuthority> e)
    {
        e.HasNoKey();
        e.ToView("vw_Authority", "HR");
    }
}

public sealed class VwEmployeeCurrentScheduleConfiguration : IEntityTypeConfiguration<VwEmployeeCurrentSchedule>
{
    public void Configure(EntityTypeBuilder<VwEmployeeCurrentSchedule> builder)
    {
        builder.ToView("Vw_EmployeeCurrentSchedule", "HR");
        builder.HasNoKey();

        builder.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        builder.Property(x => x.PersonId).HasColumnName("PersonID");
        builder.Property(x => x.EmployeeType).HasColumnName("EmployeeType");
        builder.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        builder.Property(x => x.ImmediateBossId).HasColumnName("ImmediateBossID");
        builder.Property(x => x.HireDate).HasColumnType("date");
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.IsActive);

        builder.Property(x => x.EmpScheduleId).HasColumnName("EmpScheduleID");
        builder.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        builder.Property(x => x.ValidFrom).HasColumnType("date");
        builder.Property(x => x.ValidTo).HasColumnType("date");
        builder.Property(x => x.ScheduleAssignedAt).HasColumnName("ScheduleAssignedAt");
        builder.Property(x => x.ScheduleAssignedBy).HasColumnName("ScheduleAssignedBy");

        builder.Property(x => x.ScheduleDescription).HasColumnName("ScheduleDescription").HasMaxLength(120);
        builder.Property(x => x.EntryTime).HasColumnType("time");
        builder.Property(x => x.ExitTime).HasColumnType("time");
        builder.Property(x => x.WorkingDays).HasMaxLength(20);
        builder.Property(x => x.RequiredHoursPerDay).HasColumnType("decimal(5,2)");
        builder.Property(x => x.HasLunchBreak);
        builder.Property(x => x.LunchStart).HasColumnType("time");
        builder.Property(x => x.LunchEnd).HasColumnType("time");
        builder.Property(x => x.IsRotating);
        builder.Property(x => x.RotationPattern).HasMaxLength(120);
    }
}
