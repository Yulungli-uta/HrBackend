using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>Configuración del perfil de investigador (ORCID) y de los IDs por plataforma de publicaciones científicas.</summary>
public sealed class ResearcherProfileConfiguration : IEntityTypeConfiguration<ResearcherProfile>
{
    public void Configure(EntityTypeBuilder<ResearcherProfile> e)
    {
        e.ToTable("tbl_ResearcherProfiles", "HR");
        e.HasKey(x => x.ResearcherProfileId);
        e.Property(x => x.ResearcherProfileId).HasColumnName("ResearcherProfileID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Orcid).HasMaxLength(19).IsUnicode(false);
        // Si quien escribe no informa la fecha (la auditoría la completa al guardar), la BD pone la hora del servidor.
        e.Property(x => x.CreatedAt).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIME()");
        e.Property(x => x.OrcidEnteredAt).HasDefaultValueSql("SYSDATETIME()");
        e.HasIndex(x => x.PersonId).IsUnique();
        e.HasIndex(x => x.Orcid).IsUnique().HasFilter("[Orcid] IS NOT NULL").HasDatabaseName("UX_ResearcherProfiles_Orcid");
        e.HasMany(x => x.ProviderIds).WithOne(x => x.ResearcherProfile)
            .HasForeignKey(x => x.ResearcherProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ResearcherProviderIdConfiguration : IEntityTypeConfiguration<ResearcherProviderId>
{
    public void Configure(EntityTypeBuilder<ResearcherProviderId> e)
    {
        e.ToTable("tbl_ResearcherProviderIds", "HR");
        e.HasKey(x => x.ResearcherProviderIdId);
        e.Property(x => x.ResearcherProviderIdId).HasColumnName("ResearcherProviderIdID");
        e.Property(x => x.ResearcherProfileId).HasColumnName("ResearcherProfileID");
        e.Property(x => x.ProviderTypeId).HasColumnName("ProviderTypeID");
        e.Property(x => x.ExternalId).HasMaxLength(50).IsRequired();
        e.Property(x => x.CreatedAt).ValueGeneratedOnAdd().HasDefaultValueSql("SYSDATETIME()");
        e.Property(x => x.LastSyncStatus).HasMaxLength(12);
        e.Property(x => x.LastSyncSummary).HasMaxLength(500);
    }
}
