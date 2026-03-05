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
