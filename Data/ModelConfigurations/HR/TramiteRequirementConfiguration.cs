using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de EF Core para la entidad <see cref="TramiteRequirement"/>.
/// Mapea la tabla HR.tbl_TramiteRequirements.
/// </summary>
public sealed class TramiteRequirementConfiguration : IEntityTypeConfiguration<TramiteRequirement>
{
    public void Configure(EntityTypeBuilder<TramiteRequirement> e)
    {
        e.ToTable("tbl_TramiteRequirements", "HR");

        e.HasKey(x => x.RequirementId);
        e.Property(x => x.RequirementId)
            .HasColumnName("RequirementID")
            .UseIdentityColumn();

        e.Property(x => x.ModuleTypeId)
            .HasColumnName("ModuleTypeID")
            .IsRequired();

        e.Property(x => x.SpecificTypeId)
            .HasColumnName("SpecificTypeID");

        e.Property(x => x.DocumentTypeId)
            .HasColumnName("DocumentTypeID")
            .IsRequired();

        e.Property(x => x.IsRequired)
            .HasColumnName("IsRequired")
            .IsRequired()
            .HasDefaultValue(false);

        e.Property(x => x.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        e.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETDATE()");
        e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");
        e.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasColumnType("datetime2");

        e.HasOne(x => x.ModuleType)
            .WithMany()
            .HasForeignKey(x => x.ModuleTypeId)
            .HasConstraintName("FK_TramiteRequirements_ModuleType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.DocumentType)
            .WithMany()
            .HasForeignKey(x => x.DocumentTypeId)
            .HasConstraintName("FK_TramiteRequirements_DocumentType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.ModuleTypeId, x.SpecificTypeId })
            .HasDatabaseName("IX_TramiteRequirements_Module_Specific");
    }
}
