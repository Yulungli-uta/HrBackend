using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class MassVacationPlanConfiguration : IEntityTypeConfiguration<MassVacationPlan>
{
    public void Configure(EntityTypeBuilder<MassVacationPlan> e)
    {
        e.ToTable("tbl_MassVacationPlan", "HR");
        e.HasKey(x => x.PlanId);

        e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_MassVacationPlan_Department")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_MassVacationPlan_StatusType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.Exclusions)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .HasConstraintName("FK_MassVacationPlanExclusion_Plan")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MassVacationPlanExclusionConfiguration : IEntityTypeConfiguration<MassVacationPlanExclusion>
{
    public void Configure(EntityTypeBuilder<MassVacationPlanExclusion> e)
    {
        e.ToTable("tbl_MassVacationPlanExclusion", "HR");
        e.HasKey(x => x.ExclusionId);
        e.Property(x => x.Reason).HasMaxLength(500);
        e.HasIndex(x => new { x.PlanId, x.EmployeeId }).IsUnique().HasDatabaseName("UQ_MassVacationPlanExclusion");

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_MassVacationPlanExclusion_Employee")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
