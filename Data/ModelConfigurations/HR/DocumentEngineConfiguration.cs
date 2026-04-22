using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuraciones EF Core para el motor documental institucional.
/// Mapea las 5 entidades nuevas a sus tablas en el esquema HR.
/// </summary>

// ── DocumentTemplate ────────────────────────────────────────────────────────────
public sealed class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> e)
    {
        e.ToTable("tbl_DocumentTemplates", "HR");
        e.HasKey(x => x.TemplateId);

        e.Property(x => x.TemplateId).HasColumnName("TemplateID");
        e.Property(x => x.TemplateCode).HasMaxLength(50).IsRequired();
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.TemplateType).HasMaxLength(50).IsRequired();
        e.Property(x => x.Version).HasMaxLength(10).HasDefaultValue("1.0");
        e.Property(x => x.HtmlContent).HasColumnType("NVARCHAR(MAX)").IsRequired();
        e.Property(x => x.CssStyles).HasColumnType("NVARCHAR(MAX)");
        e.Property(x => x.MetaJson).HasColumnType("NVARCHAR(MAX)");

        // Enum → string en BD (columna VARCHAR(20))
        e.Property(x => x.LayoutType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant().Replace("TEXT", "_TEXT"),
                v => Enum.Parse<LayoutType>(v.Replace("_TEXT", "TEXT"), true))
            .HasMaxLength(20)
            .HasDefaultValue(LayoutType.FlowText);

        e.Property(x => x.Status)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<DocumentTemplateStatus>(v, true))
            .HasMaxLength(20)
            .HasDefaultValue(DocumentTemplateStatus.Draft);

        e.HasIndex(x => x.TemplateCode).IsUnique();
        e.HasIndex(x => new { x.TemplateType, x.Status });

        // Relaciones
        e.HasMany(x => x.Fields)
            .WithOne(f => f.Template)
            .HasForeignKey(f => f.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.GeneratedDocuments)
            .WithOne(d => d.Template)
            .HasForeignKey(d => d.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ── DocumentTemplateField ────────────────────────────────────────────────────────
public sealed class DocumentTemplateFieldConfiguration : IEntityTypeConfiguration<DocumentTemplateField>
{
    public void Configure(EntityTypeBuilder<DocumentTemplateField> e)
    {
        e.ToTable("tbl_DocumentTemplateFields", "HR");
        e.HasKey(x => x.FieldId);

        e.Property(x => x.FieldId).HasColumnName("FieldID");
        e.Property(x => x.TemplateId).HasColumnName("TemplateID");
        e.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        e.Property(x => x.Label).HasMaxLength(150).IsRequired();
        e.Property(x => x.SourceProperty).HasMaxLength(200);
        e.Property(x => x.DataType).HasMaxLength(20).HasDefaultValue("TEXT");
        e.Property(x => x.FormatPattern).HasMaxLength(50);
        e.Property(x => x.DefaultValue).HasMaxLength(500);
        e.Property(x => x.HelpText).HasMaxLength(300);

        e.Property(x => x.SourceType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<FieldSourceType>(v, true))
            .HasMaxLength(20)
            .HasDefaultValue(FieldSourceType.System);

        e.HasIndex(x => new { x.TemplateId, x.FieldName }).IsUnique();
    }
}

// ── GeneratedDocument ────────────────────────────────────────────────────────────
public sealed class GeneratedDocumentConfiguration : IEntityTypeConfiguration<GeneratedDocument>
{
    public void Configure(EntityTypeBuilder<GeneratedDocument> e)
    {
        e.ToTable("tbl_GeneratedDocuments", "HR");
        e.HasKey(x => x.DocumentId);

        e.Property(x => x.DocumentId).HasColumnName("DocumentID");
        e.Property(x => x.TemplateId).HasColumnName("TemplateID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.StoredFileId).HasColumnName("StoredFileID");
        e.Property(x => x.DocumentNumber).HasMaxLength(50);
        e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT");
        e.Property(x => x.Notes).HasMaxLength(1000);

        e.Property(x => x.EntityType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<DocumentEntityType>(v, true))
            .HasMaxLength(30);

        e.HasIndex(x => new { x.EmployeeId, x.EntityType, x.EntityId });
        e.HasIndex(x => x.Status);

        // Relaciones
        e.HasMany(x => x.Fields)
            .WithOne(f => f.Document)
            .HasForeignKey(f => f.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ── GeneratedDocumentField ───────────────────────────────────────────────────────
public sealed class GeneratedDocumentFieldConfiguration : IEntityTypeConfiguration<GeneratedDocumentField>
{
    public void Configure(EntityTypeBuilder<GeneratedDocumentField> e)
    {
        e.ToTable("tbl_GeneratedDocumentFields", "HR");
        e.HasKey(x => x.DocumentFieldId);

        e.Property(x => x.DocumentFieldId).HasColumnName("DocumentFieldID");
        e.Property(x => x.DocumentId).HasColumnName("DocumentID");
        e.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
        e.Property(x => x.FieldValue).HasColumnType("NVARCHAR(MAX)");
        e.Property(x => x.SourceType).HasMaxLength(20).HasDefaultValue("SYSTEM");

        e.HasIndex(x => new { x.DocumentId, x.FieldName });
    }
}

// ── PersonnelAction ──────────────────────────────────────────────────────────────
public sealed class PersonnelActionConfiguration : IEntityTypeConfiguration<PersonnelAction>
{
    public void Configure(EntityTypeBuilder<PersonnelAction> e)
    {
        e.ToTable("tbl_PersonnelActions", "HR");
        e.HasKey(x => x.ActionId);

        e.Property(x => x.ActionId).HasColumnName("ActionID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.ActionTypeId).HasColumnName("ActionTypeID");
        e.Property(x => x.GeneratedDocumentId).HasColumnName("GeneratedDocumentID");
        e.Property(x => x.ContractId).HasColumnName("ContractID");
        e.Property(x => x.MovementId).HasColumnName("MovementID");
        e.Property(x => x.ActionNumber).HasMaxLength(50);
        e.Property(x => x.OriginBudgetCode).HasMaxLength(50);
        e.Property(x => x.DestinationBudgetCode).HasMaxLength(50);
        e.Property(x => x.LegalBasis).HasMaxLength(500);
        e.Property(x => x.Reason).HasMaxLength(1000);
        e.Property(x => x.Observations).HasMaxLength(1000);
        e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT");
        e.Property(x => x.PreviousRmu).HasColumnType("DECIMAL(10,2)");
        e.Property(x => x.NewRmu).HasColumnType("DECIMAL(10,2)");

        e.HasIndex(x => new { x.EmployeeId, x.ActionDate });
        e.HasIndex(x => x.Status);
        e.HasIndex(x => x.ActionNumber);

        // Relación con GeneratedDocument
        e.HasOne(x => x.GeneratedDocument)
            .WithMany()
            .HasForeignKey(x => x.GeneratedDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
