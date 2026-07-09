using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class ResignationRetirementRequestConfiguration : IEntityTypeConfiguration<ResignationRetirementRequest>
{
    public void Configure(EntityTypeBuilder<ResignationRetirementRequest> e)
    {
        e.ToTable("tbl_ResignationRetirementRequests", "HR");
        e.HasKey(x => x.RequestId);

        e.Property(x => x.RequestType).HasMaxLength(20).IsRequired();
        e.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE");
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.Property(x => x.AdditionalNotes).HasMaxLength(1000);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_ResignationRetirementRequests_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Request)
            .HasForeignKey(x => x.RequestId)
            .HasConstraintName("FK_ResignationRetirementStatusHistory_Request")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_ResignationRetirementRequests_Employee");
        e.HasIndex(x => x.Status).HasDatabaseName("IX_ResignationRetirementRequests_Status");
        e.HasIndex(x => x.RequestType).HasDatabaseName("IX_ResignationRetirementRequests_RequestType");
        e.HasIndex(x => x.RequestDate).HasDatabaseName("IX_ResignationRetirementRequests_RequestDate");
    }
}

public sealed class ResignationRetirementStatusHistoryConfiguration : IEntityTypeConfiguration<ResignationRetirementStatusHistory>
{
    public void Configure(EntityTypeBuilder<ResignationRetirementStatusHistory> e)
    {
        e.ToTable("tbl_ResignationRetirementStatusHistory", "HR");
        e.HasKey(x => x.HistoryId);

        e.Property(x => x.PreviousStatus).HasMaxLength(20);
        e.Property(x => x.NewStatus).HasMaxLength(20).IsRequired();
        e.Property(x => x.Action).HasMaxLength(20).IsRequired();
        e.Property(x => x.Observation).HasMaxLength(1000);

        e.HasIndex(x => new { x.RequestId, x.CreatedAt })
            .HasDatabaseName("IX_ResignationRetirementStatusHistory_Request");
    }
}
