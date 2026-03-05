using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Views;
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
        e.Property(x => x.EmployeeType).HasColumnName("EmployeeType");
        e.Property(x => x.ContractType).HasColumnName("ContractType");
        e.Property(x => x.ImmediateBossID).HasColumnName("ImmediateBossID");
        e.Property(x => x.ScheduleID).HasColumnName("ScheduleID");
        e.Property(x => x.Schedule).HasColumnName("Schedule");
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
        e.ToView("vw_UserRoles", "dbo");
    }
}

public sealed class VwRoleMenuItemConfiguration : IEntityTypeConfiguration<VwRoleMenuItem>
{
    public void Configure(EntityTypeBuilder<VwRoleMenuItem> e)
    {
        e.HasNoKey();
        e.ToView("vw_RoleMenuItems", "dbo");
    }
}
