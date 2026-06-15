using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models.Academic;

namespace WsUtaSystem.Data.ModelConfigurations.Academic;

public class StudentsConfiguration : IEntityTypeConfiguration<Students>
{
    public void Configure(EntityTypeBuilder<Students> b)
    {
        b.ToTable("tbl_Students", "HR");
        b.HasKey(x => x.StudentId);

        b.Property(x => x.InstitutionalEmail).HasMaxLength(320);
        b.Property(x => x.ExternalStudentCode).HasMaxLength(64);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasOne(x => x.People)
         .WithMany()
         .HasForeignKey(x => x.PersonID)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Enrollments)
         .WithOne(x => x.Student)
         .HasForeignKey(x => x.StudentId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.PersonID).IsUnique();
        b.HasIndex(x => x.StudentTypeId);
    }
}

public class StudentEnrollmentsConfiguration : IEntityTypeConfiguration<StudentEnrollments>
{
    public void Configure(EntityTypeBuilder<StudentEnrollments> b)
    {
        b.ToTable("tbl_StudentEnrollments", "HR");
        b.HasKey(x => x.EnrollmentId);

        b.Property(x => x.PeriodCode).HasMaxLength(16).IsRequired();
        b.Property(x => x.Status).HasMaxLength(32).HasDefaultValue("Activo");
        b.Property(x => x.Program).HasMaxLength(256);
        b.Property(x => x.Faculty).HasMaxLength(256);
        b.Property(x => x.Notes).HasMaxLength(512);

        b.HasIndex(x => new { x.StudentId, x.PeriodCode }).IsUnique();
        b.HasIndex(x => x.PeriodCode);
    }
}

public class StudentProvisioningConfiguration : IEntityTypeConfiguration<StudentProvisioning>
{
    public void Configure(EntityTypeBuilder<StudentProvisioning> b)
    {
        b.ToTable("tbl_StudentProvisioning", "HR");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWID()");

        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        b.Property(x => x.GivenName).HasMaxLength(128);
        b.Property(x => x.Surname).HasMaxLength(128);
        b.Property(x => x.ProvisioningStatusName).HasMaxLength(64);
        b.Property(x => x.AdObjectId).HasMaxLength(256);
        b.Property(x => x.SourceReference).HasMaxLength(256);
        b.Property(x => x.RequestedBy).HasMaxLength(320);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        b.HasOne(x => x.Student)
         .WithMany()
         .HasForeignKey(x => x.StudentId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.StudentId);
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.ProvisioningStatusId);
        b.HasIndex(x => x.AdObjectId);
    }
}
