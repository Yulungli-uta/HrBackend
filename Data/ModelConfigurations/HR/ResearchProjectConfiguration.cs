using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class ResearchProjectConfiguration : IEntityTypeConfiguration<ResearchProject>
{
    public void Configure(EntityTypeBuilder<ResearchProject> e)
    {
        e.ToTable("tbl_ResearchProjects", "HR");
        e.HasKey(x => x.ResearchProjectId);
        e.Property(x => x.ResearchProjectId).HasColumnName("ResearchProjectID").UseIdentityColumn();
        e.Property(x => x.Name).HasMaxLength(500).IsRequired();
        e.Property(x => x.ApprovalResolutionNumber).HasMaxLength(100).IsRequired();
        e.Property(x => x.ExtensionResolutionNumber).HasMaxLength(100);
        e.Property(x => x.CompletionResolutionNumber).HasMaxLength(100).IsRequired();
        e.Property(x => x.CreatedAt).HasColumnType("datetime2");
        e.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        e.HasMany(x => x.Participations)
            .WithOne(p => p.ResearchProject)
            .HasForeignKey(p => p.ResearchProjectId)
            .HasConstraintName("FK_ResearchPart_Project")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ResearchProjectParticipationConfiguration : IEntityTypeConfiguration<ResearchProjectParticipation>
{
    public void Configure(EntityTypeBuilder<ResearchProjectParticipation> e)
    {
        e.ToTable("tbl_ResearchProjectParticipations", "HR");
        e.HasKey(x => x.ParticipationId);
        e.Property(x => x.ParticipationId).HasColumnName("ParticipationID").UseIdentityColumn();
        e.Property(x => x.ResearchProjectId).HasColumnName("ResearchProjectID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.ExternalFullName).HasMaxLength(300);
        e.Property(x => x.ExternalIdentification).HasMaxLength(30);
        e.Property(x => x.ExternalInstitution).HasMaxLength(300);
        e.Property(x => x.RoleTypeId).HasColumnName("RoleTypeID");
        e.Property(x => x.CreatedAt).HasColumnType("datetime2");
        e.Property(x => x.UpdatedAt).HasColumnType("datetime2");
    }
}
