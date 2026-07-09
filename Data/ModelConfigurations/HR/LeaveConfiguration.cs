using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades de permisos, vacaciones, subrogaciones y
/// movimientos de personal del módulo HR.
/// </summary>
public sealed class PermissionTypesConfiguration : IEntityTypeConfiguration<PermissionTypes>
{
    public void Configure(EntityTypeBuilder<PermissionTypes> e)
    {
        e.ToTable("tbl_PermissionTypes", "HR");
        e.HasKey(x => x.TypeId);
        e.Property(x => x.TypeId).HasColumnName("TypeID");
        e.Property(x => x.Name).HasMaxLength(80);
        e.Property(x => x.ContractTypeId).HasColumnName("ContractTypeID");

        e.HasOne<WsUtaSystem.Models.RefTypes>()
            .WithMany()
            .HasForeignKey(x => x.ContractTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VacationsConfiguration : IEntityTypeConfiguration<Vacations>
{
    public void Configure(EntityTypeBuilder<Vacations> e)
    {
        e.ToTable("tbl_Vacations", "HR");
        e.HasKey(x => x.VacationId);
        e.Property(x => x.VacationId).HasColumnName("VacationID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.Status).HasMaxLength(20);
    }
}

public sealed class PermissionsConfiguration : IEntityTypeConfiguration<Permissions>
{
    public void Configure(EntityTypeBuilder<Permissions> e)
    {
        e.ToTable("tbl_Permissions", "HR");
        e.HasKey(x => x.PermissionId);
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.PermissionTypeId).HasColumnName("PermissionTypeID");
        e.Property(x => x.HourTaken).HasColumnName("HourTaken");
        e.Property(x => x.Status).HasMaxLength(20);
        e.Property(x => x.VacationId).HasColumnName("VacationID");

        // Índices para reportes: filtra por rango de fechas, estado y empleado
        e.HasIndex(x => new { x.EmployeeId, x.StartDate });
        e.HasIndex(x => new { x.StartDate, x.Status });
    }
}

public sealed class SubrogationsConfiguration : IEntityTypeConfiguration<Subrogations>
{
    public void Configure(EntityTypeBuilder<Subrogations> e)
    {
        e.ToTable("tbl_Subrogations", "HR");
        e.HasKey(x => x.SubrogationId);
        e.Property(x => x.SubrogationId).HasColumnName("SubrogationID");
        e.Property(x => x.SubrogatedEmployeeId).HasColumnName("SubrogatedEmployeeID");
        e.Property(x => x.SubrogatingEmployeeId).HasColumnName("SubrogatingEmployeeID");
        e.Property(x => x.PermissionId).HasColumnName("PermissionID");
        e.Property(x => x.VacationId).HasColumnName("VacationID");
        e.Property(x => x.Reason).HasMaxLength(300);
    }
}

public sealed class PersonnelMovementsConfiguration : IEntityTypeConfiguration<PersonnelMovements>
{
    public void Configure(EntityTypeBuilder<PersonnelMovements> e)
    {
        e.ToTable("tbl_PersonnelMovements", "HR");
        e.HasKey(x => x.MovementId);
        e.Property(x => x.MovementId).HasColumnName("MovementID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.ContractId).HasColumnName("ContractID");
        e.Property(x => x.JobId).HasColumnName("JobID");
        e.Property(x => x.OriginDepartmentId).HasColumnName("OriginDepartmentID");
        e.Property(x => x.DestinationDepartmentId).HasColumnName("DestinationDepartmentID");
        e.Property(x => x.DocumentLocation).HasMaxLength(255);
        e.Property(x => x.Reason).HasMaxLength(500);
        e.Property(x => x.PersonnelActionId).HasColumnName("PersonnelActionID");

        e.HasOne(x => x.MovementType)
            .WithMany()
            .HasForeignKey(x => x.MovementTypeId)
            .HasConstraintName("FK_PersonnelMovements_MovementType")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
