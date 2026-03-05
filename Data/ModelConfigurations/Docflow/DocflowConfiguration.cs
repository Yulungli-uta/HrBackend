using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Data.ModelConfigurations.Docflow;

/// <summary>
/// Configuración de todas las entidades del módulo Docflow.
/// Mapea las tablas del schema docflow en SQL Server.
/// </summary>
public sealed class DocflowProcessHierarchyConfiguration : IEntityTypeConfiguration<DocflowProcessHierarchy>
{
    public void Configure(EntityTypeBuilder<DocflowProcessHierarchy> e)
    {
        e.ToTable("tbl_ProcessHierarchy", "docflow");
        e.HasKey(x => x.ProcessId);
        e.Property(x => x.ProcessCode).HasMaxLength(50).IsRequired();
        e.Property(x => x.ProcessName).HasMaxLength(200).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => x.ProcessCode).IsUnique();
    }
}

public sealed class DocflowProcessTransitionConfiguration : IEntityTypeConfiguration<DocflowProcessTransition>
{
    public void Configure(EntityTypeBuilder<DocflowProcessTransition> e)
    {
        e.ToTable("tbl_ProcessTransitions", "docflow");
        e.HasKey(x => x.TransitionId);
        e.Property(x => x.IsDefault).HasDefaultValue(true);
        e.Property(x => x.AllowReturn).HasDefaultValue(true);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => new { x.FromProcessId, x.ToProcessId }).IsUnique();
        e.HasIndex(x => new { x.FromProcessId, x.IsDefault });
    }
}

public sealed class DocflowDocumentRuleConfiguration : IEntityTypeConfiguration<DocflowDocumentRule>
{
    public void Configure(EntityTypeBuilder<DocflowDocumentRule> e)
    {
        e.ToTable("tbl_DocumentRules", "docflow");
        e.HasKey(x => x.RuleId);
        e.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
        e.Property(x => x.IsRequired).HasDefaultValue(true);
        e.Property(x => x.DefaultVisibility).HasDefaultValue((byte)1);
        e.Property(x => x.AllowVisibilityOverride).HasDefaultValue(false);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => x.ProcessId);
        e.HasIndex(x => new { x.ProcessId, x.IsRequired });
    }
}

public sealed class DocflowWorkflowInstanceConfiguration : IEntityTypeConfiguration<DocflowWorkflowInstance>
{
    public void Configure(EntityTypeBuilder<DocflowWorkflowInstance> e)
    {
        e.ToTable("tbl_WorkflowInstances", "docflow");
        e.HasKey(x => x.InstanceId);
        e.Property(x => x.CurrentStatus).HasMaxLength(50).IsRequired();
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => new { x.CurrentDepartmentId, x.CurrentStatus, x.CreatedAt });
        e.HasIndex(x => x.ProcessId);
    }
}

public sealed class DocflowDocumentConfiguration : IEntityTypeConfiguration<DocflowDocument>
{
    public void Configure(EntityTypeBuilder<DocflowDocument> e)
    {
        e.ToTable("tbl_Documents", "docflow");
        e.HasKey(x => x.DocumentId);
        e.Property(x => x.DocumentName).HasMaxLength(255).IsRequired();
        e.Property(x => x.Visibility).HasDefaultValue((byte)1);
        e.Property(x => x.CurrentVersion).HasDefaultValue(0);
        e.Property(x => x.IsDeleted).HasDefaultValue(false);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => x.RuleId);
        e.HasIndex(x => new { x.InstanceId, x.Visibility, x.CreatedByDepartmentId })
            .HasFilter("[IsDeleted] = 0");
    }
}

public sealed class DocflowFileVersionConfiguration : IEntityTypeConfiguration<DocflowFileVersion>
{
    public void Configure(EntityTypeBuilder<DocflowFileVersion> e)
    {
        e.ToTable("tbl_FileVersions", "docflow");
        e.HasKey(x => x.VersionId);
        e.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
    }
}

public sealed class DocflowWorkflowMovementConfiguration : IEntityTypeConfiguration<DocflowWorkflowMovement>
{
    public void Configure(EntityTypeBuilder<DocflowWorkflowMovement> e)
    {
        e.ToTable("tbl_WorkflowMovements", "docflow");
        e.HasKey(x => x.MovementId);
        e.Property(x => x.MovementType).HasMaxLength(10).IsRequired();
        e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
        e.HasIndex(x => new { x.InstanceId, x.CreatedAt });
    }
}
