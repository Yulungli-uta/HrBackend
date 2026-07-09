using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class EmployeeLaborRegimeConfiguration : IEntityTypeConfiguration<EmployeeLaborRegime>
{
    public void Configure(EntityTypeBuilder<EmployeeLaborRegime> e)
    {
        e.ToTable("tbl_EmployeeLaborRegime", "HR");
        e.HasKey(x => x.Id);

        e.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
        e.Property(x => x.DocumentNumber).HasMaxLength(50);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.IsIndefinite).HasDefaultValue(false);
        e.Property(x => x.IsPrincipal).HasDefaultValue(false);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_EmployeeLaborRegime_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.LaborRegime)
            .WithMany()
            .HasForeignKey(x => x.LaborRegimeId)
            .HasConstraintName("FK_EmployeeLaborRegime_RefTypes")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_EmployeeLaborRegime_Department")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .HasConstraintName("FK_EmployeeLaborRegime_Job")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.EmployeeId, x.IsActive })
            .HasDatabaseName("IX_EmployeeLaborRegime_Employee_Active");

        e.HasIndex(x => new { x.EmployeeId, x.LaborRegimeId })
            .HasDatabaseName("IX_EmployeeLaborRegime_Employee_Regime_Active")
            .HasFilter("[IsActive] = (1)")
            .IsUnique();
    }
}
