using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class AllowedPunchNetworkConfiguration : IEntityTypeConfiguration<AllowedPunchNetwork>
{
    public void Configure(EntityTypeBuilder<AllowedPunchNetwork> e)
    {
        e.ToTable("tbl_AllowedPunchNetworks", "HR");
        e.HasKey(x => x.NetworkId);
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.Property(x => x.IpOrCidr).HasMaxLength(64).IsUnicode(false).IsRequired();
        e.Property(x => x.Notes).HasMaxLength(300);
        e.HasIndex(x => x.IpOrCidr).IsUnique();
    }
}

public sealed class EmployeePunchPolicyConfiguration : IEntityTypeConfiguration<EmployeePunchPolicy>
{
    public void Configure(EntityTypeBuilder<EmployeePunchPolicy> e)
    {
        e.ToTable("tbl_EmployeePunchPolicies", "HR");
        e.HasKey(x => x.PolicyId);
        e.Property(x => x.Reason).HasMaxLength(300);
        e.Property(x => x.ValidFrom).HasColumnType("date");
        e.Property(x => x.ValidTo).HasColumnType("date");
        e.HasIndex(x => x.EmployeeId).IsUnique();
    }
}

public sealed class PunchAccessLogConfiguration : IEntityTypeConfiguration<PunchAccessLog>
{
    public void Configure(EntityTypeBuilder<PunchAccessLog> e)
    {
        e.ToTable("tbl_PunchAccessLog", "HR");
        e.HasKey(x => x.LogId);
        e.Property(x => x.IpAddress).HasMaxLength(64).IsUnicode(false);
        e.Property(x => x.ForwardedFor).HasMaxLength(200).IsUnicode(false);
        e.Property(x => x.Result).HasMaxLength(30).IsUnicode(false).IsRequired();
        e.Property(x => x.Mode).HasMaxLength(10).IsUnicode(false).IsRequired();
        e.Property(x => x.Detail).HasMaxLength(200);
    }
}
