using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades de referencia, geográficas, auditoría,
/// parámetros y correo del módulo HR.
/// </summary>
public sealed class RefTypesConfiguration : IEntityTypeConfiguration<RefTypes>
{
    public void Configure(EntityTypeBuilder<RefTypes> e)
    {
        e.ToTable("ref_Types", "HR");
        e.HasKey(x => x.TypeId);
        e.Property(x => x.TypeId).HasColumnName("TypeID");
        e.Property(x => x.Category).HasMaxLength(50).IsRequired();
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.Description).HasMaxLength(255);
    }
}

public sealed class CountriesConfiguration : IEntityTypeConfiguration<Countries>
{
    public void Configure(EntityTypeBuilder<Countries> e)
    {
        e.ToTable("tbl_Countries", "HR");
        e.HasKey(x => x.CountryId);
        e.Property(x => x.CountryId).HasColumnName("CountryID");
        e.Property(x => x.CountryName).HasMaxLength(100);
    }
}

public sealed class ProvincesConfiguration : IEntityTypeConfiguration<Provinces>
{
    public void Configure(EntityTypeBuilder<Provinces> e)
    {
        e.ToTable("tbl_Provinces", "HR");
        e.HasKey(x => x.ProvinceId);
        e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
        e.Property(x => x.CountryId).HasColumnName("CountryID");
        e.Property(x => x.ProvinceName).HasMaxLength(100);
    }
}

public sealed class CantonsConfiguration : IEntityTypeConfiguration<Cantons>
{
    public void Configure(EntityTypeBuilder<Cantons> e)
    {
        e.ToTable("tbl_Cantons", "HR");
        e.HasKey(x => x.CantonId);
        e.Property(x => x.CantonId).HasColumnName("CantonID");
        e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
        e.Property(x => x.CantonName).HasMaxLength(100);
    }
}

public sealed class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> e)
    {
        e.ToTable("tbl_Audit", "HR");
        e.HasKey(x => x.AuditId);
        e.Property(x => x.AuditId).HasColumnName("AuditID");
        e.Property(x => x.TableName).HasMaxLength(128);
        e.Property(x => x.Action).HasMaxLength(20);
        e.Property(x => x.RecordId).HasColumnName("RecordID");
    }
}

public sealed class ParametersConfiguration : IEntityTypeConfiguration<Parameters>
{
    public void Configure(EntityTypeBuilder<Parameters> e)
    {
        e.ToTable("TBL_PARAMETERS", "HR");
        e.HasKey(x => x.ParameterId);
        e.Property(x => x.ParameterId).HasColumnName("ParameterID");
    }
}

public sealed class DirectoryParametersConfiguration : IEntityTypeConfiguration<DirectoryParameters>
{
    public void Configure(EntityTypeBuilder<DirectoryParameters> e)
    {
        e.ToTable("TBL_DirectoryParameters", "HR");
        e.HasKey(x => x.DirectoryId);
        e.Property(x => x.DirectoryId).HasColumnName("DirectoryID");
    }
}

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> e)
    {
        e.ToTable("TBL_StoredFile", "HR");
        e.HasKey(x => x.FileId);
        e.Property(x => x.FileId).ValueGeneratedOnAdd();
        e.Property(x => x.FileGuid).HasDefaultValueSql("newid()");
        e.Property(x => x.DirectoryCode).HasMaxLength(50).IsRequired();
        e.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        e.Property(x => x.UploadYear).HasColumnType("smallint").HasConversion<short>().IsRequired();
        e.Property(x => x.RelativeFolder).HasMaxLength(600).IsRequired();
        e.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
        e.Property(x => x.OriginalFileName).HasMaxLength(260);
        e.Property(x => x.Extension).HasMaxLength(20);
        e.Property(x => x.ContentType).HasMaxLength(100);
        e.Property(x => x.SizeBytes).IsRequired();
        e.Property(x => x.Sha256).HasColumnType("binary(32)");
        e.Property(x => x.Status)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.Property(x => x.FilePathHash)
            .HasColumnType("binary(32)")
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        e.HasOne<DirectoryParameters>()
            .WithMany()
            .HasForeignKey(x => x.DirectoryCode)
            .HasPrincipalKey(p => p.Code);
    }
}

public sealed class EmailLayoutConfiguration : IEntityTypeConfiguration<EmailLayout>
{
    public void Configure(EntityTypeBuilder<EmailLayout> e)
    {
        e.ToTable("tbl_EmailLayouts", "HR");
        e.HasKey(x => x.EmailLayoutID);
        e.Property(x => x.EmailLayoutID).HasColumnName("EmailLayoutID").ValueGeneratedOnAdd();
        e.Property(x => x.Slug).HasMaxLength(150).IsRequired();
        e.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_tbl_EmailLayouts_Slug");
        e.Property(x => x.HeaderHtml).HasColumnType("nvarchar(max)");
        e.Property(x => x.FooterHtml).HasColumnType("nvarchar(max)");
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
    }
}

public sealed class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> e)
    {
        e.ToTable("tbl_EmailLogs", "HR");
        e.HasKey(x => x.EmailLogID);
        e.Property(x => x.EmailLogID).HasColumnName("EmailLogID").ValueGeneratedOnAdd();
        e.Property(x => x.Recipient).HasMaxLength(320).IsRequired();
        e.Property(x => x.Subject).HasMaxLength(255).IsRequired();
        e.Property(x => x.BodyRendered).HasColumnType("nvarchar(max)").IsRequired();
        e.Property(x => x.Status).HasMaxLength(20).IsRequired();
        e.Property(x => x.SentAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        e.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        e.HasIndex(x => x.SentAt).HasDatabaseName("IX_tbl_EmailLogs_SentAt");
        e.HasIndex(x => new { x.Recipient, x.SentAt }).HasDatabaseName("IX_tbl_EmailLogs_Recipient_SentAt");
        e.HasMany(x => x.Attachments)
            .WithOne(a => a.EmailLog!)
            .HasForeignKey(a => a.EmailLogID)
            .HasConstraintName("FK_tbl_EmailLogAttachments_tbl_EmailLogs")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EmailLogAttachmentConfiguration : IEntityTypeConfiguration<EmailLogAttachment>
{
    public void Configure(EntityTypeBuilder<EmailLogAttachment> e)
    {
        e.ToTable("tbl_EmailLogAttachments", "HR");
        e.HasKey(x => x.EmailLogAttachmentID);
        e.Property(x => x.EmailLogAttachmentID).HasColumnName("EmailLogAttachmentID").ValueGeneratedOnAdd();
        e.Property(x => x.EmailLogID).IsRequired();
        e.Property(x => x.StoredFileGuid).IsRequired();
        e.Property(x => x.FileName).HasMaxLength(260);
        e.Property(x => x.ContentType).HasMaxLength(100);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
        e.HasIndex(x => new { x.EmailLogID, x.StoredFileGuid })
            .IsUnique()
            .HasDatabaseName("UX_tbl_EmailLogAttachments_EmailLogID_StoredFileGuid");
        e.HasIndex(x => x.EmailLogID).HasDatabaseName("IX_tbl_EmailLogAttachments_EmailLogID");
        e.HasOne(a => a.StoredFile)
            .WithMany()
            .HasForeignKey(a => a.StoredFileGuid)
            .HasPrincipalKey(sf => sf.FileGuid)
            .HasConstraintName("FK_tbl_EmailLogAttachments_TBL_StoredFile_FileGuid")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
