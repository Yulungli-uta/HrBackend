using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades de contratos y nómina del módulo HR:
/// Contracts, SalaryHistory, ContractType, ContractRequest,
/// FinancialCertification, ContractStatusTransition, ContractStatusHistory.
/// </summary>
public sealed class ContractsConfiguration : IEntityTypeConfiguration<Contracts>
{
    public void Configure(EntityTypeBuilder<Contracts> e)
    {
        e.ToTable("tbl_Contracts", "HR");
        e.HasKey(x => x.ContractID);
        e.Property(x => x.ContractID).HasColumnName("ContractID");
        e.Property(x => x.PersonID).HasColumnName("PersonID");
        e.Property(x => x.JobID).HasColumnName("JobID");
        e.Property(x => x.RegistrationDateAnulCon).HasColumnName("registrationdate_anul_con");
        e.Property(x => x.WorkOf).HasColumnName("work_of");
        e.Property(x => x.RowVersion)
            .HasColumnName("RowVersion")
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
        e.Property(x => x.GeneratedDocumentId).HasColumnName("GeneratedDocumentID");
        e.Property(x => x.TemplateVersionUsed).HasColumnName("TemplateVersionUsed");
        e.Property(x => x.IsDocumentFrozen).HasColumnName("IsDocumentFrozen").HasDefaultValue(false);

        e.HasOne(x => x.Parent)
            .WithMany(x => x.Addendums)
            .HasForeignKey(x => x.ParentID)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Certification)
            .WithMany()
            .HasForeignKey(x => x.CertificationID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalaryHistoryConfiguration : IEntityTypeConfiguration<SalaryHistory>
{
    public void Configure(EntityTypeBuilder<SalaryHistory> e)
    {
        e.ToTable("tbl_SalaryHistory", "HR");
        e.HasKey(x => x.SalaryHistoryId);
        e.Property(x => x.SalaryHistoryId).HasColumnName("SalaryHistoryID");
        e.Property(x => x.ContractId).HasColumnName("ContractID");
        e.Property(x => x.Reason).HasMaxLength(300);
    }
}

public sealed class ContractTypeConfiguration : IEntityTypeConfiguration<ContractType>
{
    public void Configure(EntityTypeBuilder<ContractType> e)
    {
        e.ToTable("tbl_contract_type", "HR");
        e.HasKey(x => x.ContractTypeId);
        e.Property(x => x.ContractTypeId).HasColumnName("ContractTypeID");
        e.Property(x => x.DocumentTemplateTypeId).HasColumnName("DocumentTemplateTypeID");
        e.Property(x => x.DefaultTemplateId).HasColumnName("DefaultTemplateID");
        e.Property(x => x.NumberingPrefix).HasMaxLength(30);
        e.Property(x => x.NumberingYear).HasDefaultValue(DateTime.Now.Year);
        e.Property(x => x.NumberingLastSequence).HasDefaultValue(0);
        e.HasOne(x => x.DefaultTemplate)
            .WithMany()
            .HasForeignKey(x => x.DefaultTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PersonnelActionTypeConfiguration : IEntityTypeConfiguration<PersonnelActionType>
{
    public void Configure(EntityTypeBuilder<PersonnelActionType> e)
    {
        e.ToTable("tbl_Personnel_Action_Type", "HR");
        e.HasKey(x => x.PersonnelActionTypeId);
        e.Property(x => x.PersonnelActionTypeId).HasColumnName("PersonnelActionTypeID");
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.Code).HasMaxLength(50).IsRequired();
        e.Property(x => x.Description).HasMaxLength(300);
        e.Property(x => x.NumberingPrefix).HasMaxLength(30).IsRequired();
        e.Property(x => x.NumberingYear).HasDefaultValue(DateTime.Now.Year);
        e.Property(x => x.NumberingLastSequence).HasDefaultValue(0);
        e.Property(x => x.TemplateCode).HasMaxLength(100);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class ContractRequestConfiguration : IEntityTypeConfiguration<ContractRequest>
{
    public void Configure(EntityTypeBuilder<ContractRequest> e)
    {
        e.ToTable("tbl_contractRequest", "HR");
        e.HasKey(x => x.RequestId);
        e.Property(x => x.RequestId).HasColumnName("RequestID");
        e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
        e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
        e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");
        e.Ignore(x => x.PendingCount);

        e.HasMany(x => x.FinancialCertifications)
            .WithOne(f => f.Request)
            .HasForeignKey(f => f.RequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinancialCertificationConfiguration : IEntityTypeConfiguration<FinancialCertification>
{
    public void Configure(EntityTypeBuilder<FinancialCertification> e)
    {
        e.ToTable("tbl_FinancialCertification", "HR");
        e.HasKey(x => x.CertificationId);
        e.Property(x => x.CertificationId).HasColumnName("CertificationID");
        e.Property(x => x.RmuCon).HasColumnName("rmu_con");
        e.Property(x => x.RmuHour).HasColumnName("rmu_hour");
        e.Property(x => x.RequestId).HasColumnName("RequestID");
        e.Property(x => x.FileName).HasColumnName("filename").HasMaxLength(150);
        e.Property(x => x.FilePath).HasColumnName("filepath");
    }
}

public sealed class ContractStatusTransitionConfiguration : IEntityTypeConfiguration<ContractStatusTransition>
{
    public void Configure(EntityTypeBuilder<ContractStatusTransition> e)
    {
        e.ToTable("tbl_contract_status_transitions", "HR");
        e.HasKey(x => x.TransitionID);
        e.HasIndex(x => new { x.FromStatusTypeID, x.ToStatusTypeID }).IsUnique();
    }
}

public sealed class ContractStatusHistoryConfiguration : IEntityTypeConfiguration<ContractStatusHistory>
{
    public void Configure(EntityTypeBuilder<ContractStatusHistory> e)
    {
        e.ToTable("tbl_contract_status_history", "HR");
        e.HasKey(x => x.HistoryID);
        e.HasIndex(x => new { x.ContractID, x.ChangedAt });
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
        e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("BORRADOR");
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeID");
        e.Property(x => x.SignedDocumentStoredFileId).HasColumnName("SignedDocumentStoredFileID");
        e.Property(x => x.PreviousRmu).HasColumnType("DECIMAL(10,2)");
        e.Property(x => x.NewRmu).HasColumnType("DECIMAL(10,2)");
        e.Property(x => x.DthDirectorId).HasColumnName("DthDirectorID");
        e.Property(x => x.AuthorityNominatorId).HasColumnName("AuthorityNominatorID");
        e.Property(x => x.ElaboratorId).HasColumnName("ElaboratorID");
        e.Property(x => x.ReviewerId).HasColumnName("ReviewerID");
        e.Property(x => x.RegistrarId).HasColumnName("RegistrarID");

        e.HasIndex(x => new { x.EmployeeId, x.ActionDate });
        e.HasIndex(x => x.Status);
        e.HasIndex(x => x.ActionNumber);

        // Relación con GeneratedDocument
        e.HasOne(x => x.GeneratedDocument)
            .WithMany()
            .HasForeignKey(x => x.GeneratedDocumentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Estado desde catálogo ref_Types
        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Historial de estados
        e.HasMany(x => x.StatusHistory)
            .WithOne(h => h.Action)
            .HasForeignKey(h => h.ActionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ── PersonnelActionStatusHistory ─────────────────────────────────────────────────
public sealed class PersonnelActionStatusHistoryConfiguration : IEntityTypeConfiguration<PersonnelActionStatusHistory>
{
    public void Configure(EntityTypeBuilder<PersonnelActionStatusHistory> e)
    {
        e.ToTable("tbl_PersonnelActionStatusHistory", "HR");
        e.HasKey(x => x.HistoryId);

        e.Property(x => x.HistoryId).HasColumnName("HistoryID");
        e.Property(x => x.ActionId).HasColumnName("ActionID");
        e.Ignore(x => x.StatusTypeId);
        e.Ignore(x => x.StatusType);
        e.Property(x => x.FromStatus).HasColumnName("FromStatus").HasMaxLength(30);
        e.Property(x => x.StatusCode).HasColumnName("ToStatus").HasMaxLength(30).IsRequired();
        e.Property(x => x.Comment).HasColumnName("Notes").HasMaxLength(500);
        e.Property(x => x.ChangedAt).IsRequired();

        e.HasIndex(x => x.ActionId);
        e.HasIndex(x => x.ChangedAt);
    }
}


public sealed class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> e)
    {
        e.ToTable("tbl_Payroll", "HR");
        e.HasKey(x => x.PayrollId);
        e.Property(x => x.PayrollId).HasColumnName("PayrollID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.Period).HasMaxLength(7);
        e.Property(x => x.Status).HasMaxLength(15);
        e.Property(x => x.BankAccount).HasMaxLength(50);
    }
}

public sealed class PayrollLinesConfiguration : IEntityTypeConfiguration<PayrollLines>
{
    public void Configure(EntityTypeBuilder<PayrollLines> e)
    {
        e.ToTable("tbl_PayrollLines", "HR");
        e.HasKey(x => x.PayrollLineId);
        e.Property(x => x.PayrollLineId).HasColumnName("PayrollLineID");
        e.Property(x => x.PayrollId).HasColumnName("PayrollID");
        e.Property(x => x.LineType).HasMaxLength(20);
        e.Property(x => x.Concept).HasMaxLength(120);
        e.Property(x => x.Notes).HasMaxLength(300);
    }
}
