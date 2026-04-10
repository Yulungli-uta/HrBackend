using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades organizacionales del módulo HR:
/// Employees, Faculties, Departments, Jobs, OccupationalGroup, Degree.
/// </summary>
public sealed class EmployeesConfiguration : IEntityTypeConfiguration<Employees>
{
    public void Configure(EntityTypeBuilder<Employees> e)
    {
        e.ToTable("tbl_Employees", "HR");
        e.HasKey(x => x.EmployeeId);
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.PersonID).HasColumnName("PersonID");
        e.Property(x => x.EmployeeType).HasColumnName("EmployeeType");
        e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        e.Property(x => x.ImmediateBossId).HasColumnName("ImmediateBossID");

        // Relación con People: un empleado tiene una persona asociada
        e.HasOne(x => x.People)
            .WithMany()
            .HasForeignKey(x => x.PersonID)
            .HasPrincipalKey(p => p.PersonId)
            .HasConstraintName("FK_Employees_People")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacultiesConfiguration : IEntityTypeConfiguration<Faculties>
{
    public void Configure(EntityTypeBuilder<Faculties> e)
    {
        e.ToTable("tbl_Faculties", "HR");
        e.HasKey(x => x.FacultyId);
        e.Property(x => x.FacultyId).HasColumnName("FacultyID");
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.Property(x => x.DeanEmployeeId).HasColumnName("DeanEmployeeID");
    }
}

public sealed class DepartmentsConfiguration : IEntityTypeConfiguration<Departments>
{
    public void Configure(EntityTypeBuilder<Departments> e)
    {
        e.ToTable("tbl_Departments", "HR");
        e.HasKey(x => x.DepartmentId);
        e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.Ignore(x => x.RowVersion);
    }
}

public sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> e)
    {
        e.ToTable("tbl_Jobs", "HR");
        e.HasKey(x => x.JobID);
        e.Property(x => x.JobID).HasColumnName("JobID");
    }
}

public sealed class OccupationalGroupConfiguration : IEntityTypeConfiguration<OccupationalGroup>
{
    public void Configure(EntityTypeBuilder<OccupationalGroup> e)
    {
        e.ToTable("tbl_Occupational_Groups", "HR");
        e.HasKey(x => x.GroupId);
        e.Property(x => x.GroupId).HasColumnName("GroupID");
    }
}

public sealed class DegreeConfiguration : IEntityTypeConfiguration<Degree>
{
    public void Configure(EntityTypeBuilder<Degree> e)
    {
        e.ToTable("tbl_Degrees", "HR");
        e.HasKey(x => x.DegreeId);
        e.Property(x => x.DegreeId).HasColumnName("DegreeID");
    }
}
