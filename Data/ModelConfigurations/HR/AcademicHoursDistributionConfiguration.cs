using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class AcademicHoursDistributionConfiguration : IEntityTypeConfiguration<AcademicHoursDistribution>
{
    public void Configure(EntityTypeBuilder<AcademicHoursDistribution> e)
    {
        e.ToTable("tbl_AcademicHoursDistribution", "HR");
        e.HasKey(x => x.AcademicHoursDistributionId);
        e.Property(x => x.AcademicHoursDistributionId).HasColumnName("AcademicHoursDistributionId");
        e.Property(x => x.IDCard).HasColumnName("IDCard").HasMaxLength(20);
        e.Property(x => x.PeriodCode).HasColumnName("PeriodCode").HasMaxLength(10);
        e.Property(x => x.ContractCode).HasMaxLength(100);
        e.HasIndex(x => x.IDCard);
        e.HasIndex(x => new { x.IDCard, x.PeriodCode }).IsUnique();
    }
}
