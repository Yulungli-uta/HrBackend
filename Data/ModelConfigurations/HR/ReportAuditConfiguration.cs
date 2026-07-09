using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class ReportAuditConfiguration : IEntityTypeConfiguration<ReportAudit>
{
    public void Configure(EntityTypeBuilder<ReportAudit> e)
    {
        e.ToTable("tbl_ReportAudit", "HR");
        e.HasKey(x => x.Id);

        e.Property(x => x.UserEmail).HasMaxLength(255).IsRequired();
        e.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
        e.Property(x => x.ReportFormat).HasMaxLength(10).IsRequired();
        e.Property(x => x.ClientIp).HasMaxLength(50);
        e.Property(x => x.FileName).HasMaxLength(255);
        e.Property(x => x.Success).HasDefaultValue(true);
        e.Property(x => x.GeneratedAt).HasDefaultValueSql("(getdate())");
    }
}
