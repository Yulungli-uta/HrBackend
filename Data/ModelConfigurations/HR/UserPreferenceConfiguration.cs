using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> e)
    {
        e.ToTable("tbl_UserPreferences", "HR");
        e.HasKey(x => x.UserPreferenceId);
        e.HasIndex(x => x.UserId).IsUnique();
        e.Property(x => x.PreferencesJson).IsRequired();
    }
}
