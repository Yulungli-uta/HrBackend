using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class TeacherStructureConfiguration : IEntityTypeConfiguration<TeacherStructure>
{
    public void Configure(EntityTypeBuilder<TeacherStructure> e)
    {
        e.ToTable("tbl_TeacherStructure", "HR");
        e.HasKey(x => x.TeacherStructureId);
        e.Property(x => x.TeacherStructureId).HasColumnName("TeacherStructureID").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.LadderId).HasColumnName("LadderID");
        e.Property(x => x.DedicationTypeId).HasColumnName("DedicationTypeID");
        e.Property(x => x.Rmu).HasColumnName("RMU").HasPrecision(10, 2);
        e.Property(x => x.WeeklyClassHours).HasPrecision(5, 2);
        e.Property(x => x.HourValue).HasPrecision(10, 4);
        e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_TeacherStr_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees

        e.HasOne(x => x.Ladder)
            .WithMany()
            .HasForeignKey(x => x.LadderId)
            .HasConstraintName("FK_TeacherStr_Ladder")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.DedicationType)
            .WithMany()
            .HasForeignKey(x => x.DedicationTypeId)
            .HasConstraintName("FK_TeacherStr_Dedication")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_TeacherStr_Department")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
