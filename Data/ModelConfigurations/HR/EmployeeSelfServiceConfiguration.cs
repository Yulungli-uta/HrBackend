using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class EmployeeCertificateRequestConfiguration : IEntityTypeConfiguration<EmployeeCertificateRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificateRequest> e)
    {
        e.ToTable("tbl_EmployeeCertificateRequests", "HR");
        e.HasKey(x => x.RequestId);

        e.Property(x => x.CertificateType).HasMaxLength(30).IsRequired().HasDefaultValue("LABORAL");
        e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE");
        e.Property(x => x.Purpose).HasMaxLength(300);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_EmployeeCertificateRequests_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees

        e.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Request)
            .HasForeignKey(x => x.RequestId)
            .HasConstraintName("FK_EmployeeCertificateStatusHistory_Request")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.EmployeeId, x.CreatedAt }).HasDatabaseName("IX_EmployeeCertificateRequests_Employee");
    }
}

public sealed class EmployeeCertificateStatusHistoryConfiguration : IEntityTypeConfiguration<EmployeeCertificateStatusHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificateStatusHistory> e)
    {
        e.ToTable("tbl_EmployeeCertificateStatusHistory", "HR");
        e.HasKey(x => x.HistoryId);

        e.Property(x => x.PreviousStatus).HasMaxLength(20);
        e.Property(x => x.NewStatus).HasMaxLength(20).IsRequired();
        e.Property(x => x.Action).HasMaxLength(20).IsRequired();
        e.Property(x => x.Observation).HasMaxLength(1000);
    }
}

public sealed class EmployeeInternalRequestConfiguration : IEntityTypeConfiguration<EmployeeInternalRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeInternalRequest> e)
    {
        e.ToTable("tbl_EmployeeInternalRequests", "HR");
        e.HasKey(x => x.RequestId);

        e.Property(x => x.RequestType).HasMaxLength(30).IsRequired();
        e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        e.Property(x => x.Description).HasMaxLength(1500);
        e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE");
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_EmployeeInternalRequests_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees

        e.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Request)
            .HasForeignKey(x => x.RequestId)
            .HasConstraintName("FK_EmployeeInternalRequestStatusHistory_Request")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.EmployeeId, x.CreatedAt }).HasDatabaseName("IX_EmployeeInternalRequests_Employee");
        e.HasIndex(x => x.Status).HasDatabaseName("IX_EmployeeInternalRequests_Status");
    }
}

public sealed class EmployeeInternalRequestStatusHistoryConfiguration : IEntityTypeConfiguration<EmployeeInternalRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeInternalRequestStatusHistory> e)
    {
        e.ToTable("tbl_EmployeeInternalRequestStatusHistory", "HR");
        e.HasKey(x => x.HistoryId);

        e.Property(x => x.PreviousStatus).HasMaxLength(20);
        e.Property(x => x.NewStatus).HasMaxLength(20).IsRequired();
        e.Property(x => x.Action).HasMaxLength(20).IsRequired();
        e.Property(x => x.Observation).HasMaxLength(1000);
    }
}
