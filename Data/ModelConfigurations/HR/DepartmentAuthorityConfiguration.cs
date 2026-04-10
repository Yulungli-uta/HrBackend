using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de EF Core para la entidad <see cref="DepartmentAuthority"/>.
/// Mapea la tabla HR.tbl_DepartmentAuthorities con todas sus restricciones,
/// índices y valores por defecto definidos en el script SQL original.
/// </summary>
public sealed class DepartmentAuthorityConfiguration : IEntityTypeConfiguration<DepartmentAuthority>
{
    public void Configure(EntityTypeBuilder<DepartmentAuthority> e)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Tabla y esquema
        // ─────────────────────────────────────────────────────────────────────
        e.ToTable("tbl_DepartmentAuthorities", "HR");

        // ─────────────────────────────────────────────────────────────────────
        // Clave primaria
        // ─────────────────────────────────────────────────────────────────────
        e.HasKey(x => x.AuthorityId);
        e.Property(x => x.AuthorityId)
            .HasColumnName("AuthorityID")
            .UseIdentityColumn();

        // ─────────────────────────────────────────────────────────────────────
        // Propiedades escalares
        // ─────────────────────────────────────────────────────────────────────
        e.Property(x => x.DepartmentId)
            .HasColumnName("DepartmentID")
            .IsRequired();

        e.Property(x => x.EmployeeId)
            .HasColumnName("EmployeeID")
            .IsRequired();

        e.Property(x => x.AuthorityTypeId)
            .HasColumnName("AuthorityTypeID")
            .IsRequired();

        e.Property(x => x.JobId)
            .HasColumnName("JobID");

        e.Property(x => x.Denomination)
            .HasColumnName("Denomination")
            .HasMaxLength(200);

        e.Property(x => x.StartDate)
            .HasColumnName("StartDate")
            .HasColumnType("date")
            .IsRequired();

        e.Property(x => x.EndDate)
            .HasColumnName("EndDate")
            .HasColumnType("date");

        e.Property(x => x.ResolutionCode)
            .HasColumnName("ResolutionCode")
            .HasMaxLength(100);

        e.Property(x => x.Notes)
            .HasColumnName("Notes")
            .HasMaxLength(500);

        e.Property(x => x.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        // ─────────────────────────────────────────────────────────────────────
        // Auditoría
        // ─────────────────────────────────────────────────────────────────────
        e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");

        e.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("GETDATE()");

        e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");

        e.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasColumnType("datetime2(3)");

        // ─────────────────────────────────────────────────────────────────────
        // Control de concurrencia optimista (TIMESTAMP / ROWVERSION)
        // ─────────────────────────────────────────────────────────────────────
        e.Property(x => x.RowVersion)
            .HasColumnName("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken();

        // ─────────────────────────────────────────────────────────────────────
        // Relaciones (FK)
        // ─────────────────────────────────────────────────────────────────────
        e.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("FK_DeptAuth_Department")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_DeptAuth_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.AuthorityType)
            .WithMany()
            .HasForeignKey(x => x.AuthorityTypeId)
            .HasConstraintName("FK_DeptAuth_AuthType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Job)
            .WithMany()
            .HasForeignKey(x => x.JobId)
            .HasConstraintName("FK_DeptAuth_Job")
            .OnDelete(DeleteBehavior.SetNull);

        // ─────────────────────────────────────────────────────────────────────
        // Índices (reflejan los índices del script SQL)
        // ─────────────────────────────────────────────────────────────────────

        // IX_DeptAuth_DateRange
        e.HasIndex(x => new { x.StartDate, x.EndDate })
            .HasDatabaseName("IX_DeptAuth_DateRange");

        // IX_DeptAuth_Dept_Active (índice filtrado: EndDate IS NULL AND IsActive = 1)
        e.HasIndex(x => new { x.DepartmentId, x.AuthorityTypeId, x.IsActive })
            .HasDatabaseName("IX_DeptAuth_Dept_Active")
            .HasFilter("[EndDate] IS NULL AND [IsActive] = (1)");

        // IX_DeptAuth_Employee
        e.HasIndex(x => new { x.EmployeeId, x.IsActive })
            .HasDatabaseName("IX_DeptAuth_Employee");
    }
}
