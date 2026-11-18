using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>Configuración de <c>HR.tbl_StoredFileVersions</c> (versionado genérico de archivos).</summary>
public sealed class StoredFileVersionConfiguration : IEntityTypeConfiguration<StoredFileVersion>
{
    public void Configure(EntityTypeBuilder<StoredFileVersion> e)
    {
        e.ToTable("tbl_StoredFileVersions", "HR");
        e.HasKey(x => x.VersionId);
        e.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        e.Property(x => x.EntityId).HasMaxLength(50).IsRequired();
        e.Property(x => x.SlotCode).HasMaxLength(30).IsUnicode(false).IsRequired();
        e.Property(x => x.Reason).HasMaxLength(500);
        e.Property(x => x.UploadedAt).HasColumnType("datetime2(0)");
        e.Property(x => x.ReplacedAt).HasColumnType("datetime2(0)");
    }
}
