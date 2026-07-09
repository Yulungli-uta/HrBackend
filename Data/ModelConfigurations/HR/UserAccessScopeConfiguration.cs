using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class UserAccessScopeConfiguration : IEntityTypeConfiguration<UserAccessScope>
{
    public void Configure(EntityTypeBuilder<UserAccessScope> e)
    {
        e.ToTable("tbl_UserAccessScopes", "HR");
        e.HasKey(x => x.Id);

        e.Property(x => x.AssignedBy).HasMaxLength(320);
        e.Property(x => x.Reason).HasMaxLength(300);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.ModuleType)
            .WithMany()
            .HasForeignKey(x => x.ModuleTypeId)
            .HasConstraintName("FK_UserAccessScopes_ModuleType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ScopeType)
            .WithMany()
            .HasForeignKey(x => x.ScopeTypeId)
            .HasConstraintName("FK_UserAccessScopes_ScopeType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_UserAccessScopes_Department")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.EmployeeId, x.ModuleTypeId, x.IsActive })
            .HasDatabaseName("IX_UserAccessScopes_Employee_Module_Active");
    }
}

public sealed class UserAccessScopeHistoryConfiguration : IEntityTypeConfiguration<UserAccessScopeHistory>
{
    public void Configure(EntityTypeBuilder<UserAccessScopeHistory> e)
    {
        e.ToTable("tbl_UserAccessScopeHistory", "HR");
        e.HasKey(x => x.Id);

        e.Property(x => x.ChangeType).HasMaxLength(20).IsRequired();
        e.Property(x => x.ChangedBy).HasMaxLength(320).IsRequired();
        e.Property(x => x.ChangeReason).HasMaxLength(300);

        e.HasIndex(x => new { x.EmployeeId, x.ChangeDateTime })
            .HasDatabaseName("IX_UserAccessScopeHistory_Employee");
    }
}
