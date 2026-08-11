using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class GuardServiceLocationConfiguration : IEntityTypeConfiguration<GuardServiceLocation>
{
    public void Configure(EntityTypeBuilder<GuardServiceLocation> e)
    {
        e.ToTable("tbl_GuardServiceLocations", "HR");
        e.HasKey(x => x.LocationId);
        e.Property(x => x.LocationId).HasColumnName("LocationID").UseIdentityColumn();
        e.Property(x => x.ParentLocationId).HasColumnName("ParentLocationID");
        e.Property(x => x.RootLocationId).HasColumnName("RootLocationID");
        e.Property(x => x.LocationTypeId).HasColumnName("LocationTypeID");
        e.Property(x => x.LocationCode).HasMaxLength(30);
        e.Property(x => x.LocationName).HasMaxLength(200).IsRequired();
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.LocationPath).HasMaxLength(900);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(x => x.ParentLocationId)
            .HasConstraintName("FK_GuardServiceLocations_Parent")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Root)
            .WithMany()
            .HasForeignKey(x => x.RootLocationId)
            .HasConstraintName("FK_GuardServiceLocations_Root")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardRotationGroupConfiguration : IEntityTypeConfiguration<GuardRotationGroup>
{
    public void Configure(EntityTypeBuilder<GuardRotationGroup> e)
    {
        e.ToTable("tbl_GuardRotationGroups", "HR");
        e.HasKey(x => x.GroupId);
        e.Property(x => x.GroupId).HasColumnName("GroupID").UseIdentityColumn();
        e.Property(x => x.GroupCode).HasMaxLength(30);
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.ParentGroupId).HasColumnName("ParentGroupId");
        e.Property(x => x.GroupLevelTypeId).HasColumnName("GroupLevelTypeId");
        e.Property(x => x.ColorCode).HasMaxLength(20);
        e.Property(x => x.IsSpecial).HasDefaultValue(false);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.ParentGroup)
            .WithMany(g => g.Subgroups)
            .HasForeignKey(x => x.ParentGroupId)
            .HasConstraintName("FK_GuardRotationGroups_Parent")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.GroupLevelType)
            .WithMany()
            .HasForeignKey(x => x.GroupLevelTypeId)
            .HasConstraintName("FK_GuardRotationGroups_LevelType")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardRotationGroupEmployeeConfiguration : IEntityTypeConfiguration<GuardRotationGroupEmployee>
{
    public void Configure(EntityTypeBuilder<GuardRotationGroupEmployee> e)
    {
        e.ToTable("tbl_GuardRotationGroupEmployees", "HR");
        e.HasKey(x => x.GroupEmployeeId);
        e.Property(x => x.GroupEmployeeId).HasColumnName("GroupEmployeeID").UseIdentityColumn();
        e.Property(x => x.GroupId).HasColumnName("GroupID");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Group)
            .WithMany(g => g.Employees)
            .HasForeignKey(x => x.GroupId)
            .HasConstraintName("FK_GuardRotGroupEmp_Group")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_GuardRotGroupEmp_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            // Employees tiene soft-delete (filtro global IsDeleted) — esta relación se marca
            // opcional a nivel de EF (la columna sigue NOT NULL en la BD) para que, si el
            // empleado queda soft-deleted, el registro siga siendo consultable en vez de que
            // EF advierta/arriesgue resultados inconsistentes en la navegación "requerida".
            .IsRequired(false);
    }
}

public sealed class RotationPatternConfiguration : IEntityTypeConfiguration<RotationPattern>
{
    public void Configure(EntityTypeBuilder<RotationPattern> e)
    {
        e.ToTable("tbl_RotationPatterns", "HR");
        e.HasKey(x => x.PatternId);
        e.Property(x => x.PatternId).HasColumnName("PatternID").UseIdentityColumn();
        e.Property(x => x.PatternCode).HasMaxLength(30);
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.PatternTypeId).HasColumnName("PatternTypeID");
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.PatternType)
            .WithMany()
            .HasForeignKey(x => x.PatternTypeId)
            .HasConstraintName("FK_RotationPatterns_Type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RotationPatternDetailConfiguration : IEntityTypeConfiguration<RotationPatternDetail>
{
    public void Configure(EntityTypeBuilder<RotationPatternDetail> e)
    {
        e.ToTable("tbl_RotationPatternDetails", "HR");
        e.HasKey(x => x.PatternDetailId);
        e.Property(x => x.PatternDetailId).HasColumnName("PatternDetailID").UseIdentityColumn();
        e.Property(x => x.PatternId).HasColumnName("PatternID");
        e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        e.Property(x => x.Notes).HasMaxLength(300);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Pattern)
            .WithMany(p => p.Details)
            .HasForeignKey(x => x.PatternId)
            .HasConstraintName("FK_RotationPatternDetails_Pattern")
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Schedule)
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .HasConstraintName("FK_RotationPatternDetails_Schedule")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardGroupRotationPatternConfiguration : IEntityTypeConfiguration<GuardGroupRotationPattern>
{
    public void Configure(EntityTypeBuilder<GuardGroupRotationPattern> e)
    {
        e.ToTable("tbl_GuardGroupRotationPatterns", "HR");
        e.HasKey(x => x.GroupPatternId);
        e.Property(x => x.GroupPatternId).HasColumnName("GroupPatternID").UseIdentityColumn();
        e.Property(x => x.GroupId).HasColumnName("GroupID");
        e.Property(x => x.PatternId).HasColumnName("PatternID");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Group)
            .WithMany(g => g.Patterns)
            .HasForeignKey(x => x.GroupId)
            .HasConstraintName("FK_GuardGroupRotPat_Group")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Pattern)
            .WithMany()
            .HasForeignKey(x => x.PatternId)
            .HasConstraintName("FK_GuardGroupRotPat_Pattern")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardShiftCoverageRequirementConfiguration : IEntityTypeConfiguration<GuardShiftCoverageRequirement>
{
    public void Configure(EntityTypeBuilder<GuardShiftCoverageRequirement> e)
    {
        e.ToTable("tbl_GuardShiftCoverageRequirements", "HR");
        e.HasKey(x => x.RequirementId);
        e.Property(x => x.RequirementId).HasColumnName("RequirementID").UseIdentityColumn();
        e.Property(x => x.LocationId).HasColumnName("LocationID");
        e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        e.Property(x => x.DayOfWeek).HasColumnName("DayOfWeek").HasColumnType("tinyint");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("FK_GuardShiftCovReq_Location")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Schedule)
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .HasConstraintName("FK_GuardShiftCovReq_Schedule")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardShiftPlanningConfiguration : IEntityTypeConfiguration<GuardShiftPlanning>
{
    public void Configure(EntityTypeBuilder<GuardShiftPlanning> e)
    {
        e.ToTable("tbl_GuardShiftPlanning", "HR");
        e.HasKey(x => x.PlanningId);
        e.Property(x => x.PlanningId).HasColumnName("PlanningID").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.GroupId).HasColumnName("GroupID");
        e.Property(x => x.LocationId).HasColumnName("LocationID");
        e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        e.Property(x => x.PlanningSourceTypeId).HasColumnName("PlanningSourceTypeID");
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeID");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.AllowDoubleShift).HasDefaultValue(false);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_GuardShiftPlanning_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .HasConstraintName("FK_GuardShiftPlanning_Group")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("FK_GuardShiftPlanning_Location")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Schedule)
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .HasConstraintName("FK_GuardShiftPlanning_Schedule")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.PlanningSourceType)
            .WithMany()
            .HasForeignKey(x => x.PlanningSourceTypeId)
            .HasConstraintName("FK_GuardShiftPlanning_SourceType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_GuardShiftPlanning_StatusType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .IsUnique()
            .HasFilter("IsActiveForAssignment = 1 AND AllowDoubleShift = 0")
            .HasDatabaseName("UX_GuardShiftPlanning_NoDoubleActiveShift");
    }
}

public sealed class GuardShiftChangeConfiguration : IEntityTypeConfiguration<GuardShiftChange>
{
    public void Configure(EntityTypeBuilder<GuardShiftChange> e)
    {
        e.ToTable("tbl_GuardShiftChanges", "HR");
        e.HasKey(x => x.ShiftChangeId);
        e.Property(x => x.ShiftChangeId).HasColumnName("ShiftChangeID").UseIdentityColumn();
        e.Property(x => x.PlanningId).HasColumnName("PlanningID");
        e.Property(x => x.OriginalEmployeeId).HasColumnName("OriginalEmployeeID");
        e.Property(x => x.ReplacementEmployeeId).HasColumnName("ReplacementEmployeeID");
        e.Property(x => x.OriginalScheduleId).HasColumnName("OriginalScheduleID");
        e.Property(x => x.NewScheduleId).HasColumnName("NewScheduleID");
        e.Property(x => x.NewWorkDate).HasColumnName("NewWorkDate");
        e.Property(x => x.NewLocationId).HasColumnName("NewLocationID");
        e.Property(x => x.ChangeTypeId).HasColumnName("ChangeTypeID");
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeID");
        e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        e.Property(x => x.RequestedBy).HasColumnName("RequestedBy");
        e.Property(x => x.ApprovedBy).HasColumnName("ApprovedBy");
        e.Property(x => x.RejectionReason).HasMaxLength(500);
        e.Property(x => x.RequestedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Planning)
            .WithMany(p => p.Changes)
            .HasForeignKey(x => x.PlanningId)
            .HasConstraintName("FK_GuardShiftChanges_Planning")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.OriginalEmployee)
            .WithMany()
            .HasForeignKey(x => x.OriginalEmployeeId)
            .HasConstraintName("FK_GuardShiftChanges_OrigEmp")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.ReplacementEmployee)
            .WithMany()
            .HasForeignKey(x => x.ReplacementEmployeeId)
            .HasConstraintName("FK_GuardShiftChanges_ReplEmp")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.OriginalSchedule)
            .WithMany()
            .HasForeignKey(x => x.OriginalScheduleId)
            .HasConstraintName("FK_GuardShiftChanges_OrigSched")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.NewSchedule)
            .WithMany()
            .HasForeignKey(x => x.NewScheduleId)
            .HasConstraintName("FK_GuardShiftChanges_NewSched")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.NewLocation)
            .WithMany()
            .HasForeignKey(x => x.NewLocationId)
            .HasConstraintName("FK_GuardShiftChanges_NewLocation")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ChangeType)
            .WithMany()
            .HasForeignKey(x => x.ChangeTypeId)
            .HasConstraintName("FK_GuardShiftChanges_ChangeType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_GuardShiftChanges_StatusType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.RequesterEmployee)
            .WithMany()
            .HasForeignKey(x => x.RequestedBy)
            .HasConstraintName("FK_GuardShiftChanges_RequestedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ApproverEmployee)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .HasConstraintName("FK_GuardShiftChanges_ApprovedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.PlanningId)
            .IsUnique()
            .HasFilter("IsActiveForAttendance = 1")
            .HasDatabaseName("UX_GuardShiftChanges_OneActiveAtt");
    }
}

public sealed class EmployeeAvailabilityBlockConfiguration : IEntityTypeConfiguration<EmployeeAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<EmployeeAvailabilityBlock> e)
    {
        e.ToTable("tbl_EmployeeAvailabilityBlocks", "HR");
        e.HasKey(x => x.BlockId);
        e.Property(x => x.BlockId).HasColumnName("BlockID").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.SourceTypeId).HasColumnName("SourceTypeID");
        e.Property(x => x.SourceTable).HasColumnName("SourceTable").HasMaxLength(128);
        e.Property(x => x.SourceId).HasColumnName("SourceID").HasMaxLength(128);
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeID");
        e.Property(x => x.Reason).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_EmpAvailBlocks_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.SourceType)
            .WithMany()
            .HasForeignKey(x => x.SourceTypeId)
            .HasConstraintName("FK_EmpAvailBlocks_SourceType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_EmpAvailBlocks_StatusType")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardAssignmentValidationConfiguration : IEntityTypeConfiguration<GuardAssignmentValidation>
{
    public void Configure(EntityTypeBuilder<GuardAssignmentValidation> e)
    {
        e.ToTable("tbl_GuardAssignmentValidations", "HR");
        e.HasKey(x => x.ValidationId);
        e.Property(x => x.ValidationId).HasColumnName("ValidationID").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
        e.Property(x => x.PlanningId).HasColumnName("PlanningID");
        e.Property(x => x.ShiftChangeId).HasColumnName("ShiftChangeID");
        e.Property(x => x.ValidationTypeId).HasColumnName("ValidationTypeID");
        e.Property(x => x.ResultTypeId).HasColumnName("ResultTypeID");
        e.Property(x => x.SeverityTypeId).HasColumnName("SeverityTypeID");
        e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        e.Property(x => x.Details).HasColumnType("nvarchar(max)");
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.ValidationDate)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_GuardAssignValids_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.Planning)
            .WithMany(p => p.Validations)
            .HasForeignKey(x => x.PlanningId)
            .HasConstraintName("FK_GuardAssignValids_Planning")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ShiftChange)
            .WithMany()
            .HasForeignKey(x => x.ShiftChangeId)
            .HasConstraintName("FK_GuardAssignValids_ShiftChange")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ValidationType)
            .WithMany()
            .HasForeignKey(x => x.ValidationTypeId)
            .HasConstraintName("FK_GuardAssignValids_ValidationType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.ResultType)
            .WithMany()
            .HasForeignKey(x => x.ResultTypeId)
            .HasConstraintName("FK_GuardAssignValids_ResultType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.SeverityType)
            .WithMany()
            .HasForeignKey(x => x.SeverityTypeId)
            .HasConstraintName("FK_GuardAssignValids_SeverityType")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// ─── Entidades nuevas ────────────────────────────────────────────────────────

public sealed class GuardSettingConfiguration : IEntityTypeConfiguration<GuardSetting>
{
    public void Configure(EntityTypeBuilder<GuardSetting> e)
    {
        e.ToTable("tbl_GuardSettings", "HR");
        e.HasKey(x => x.SettingKey);
        e.Property(x => x.SettingKey).HasMaxLength(100);
        e.Property(x => x.SettingValue).HasMaxLength(500).IsRequired();
        e.Property(x => x.Description).HasMaxLength(500);
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
    }
}

public sealed class GuardLocationRotationPeriodConfiguration : IEntityTypeConfiguration<GuardLocationRotationPeriod>
{
    public void Configure(EntityTypeBuilder<GuardLocationRotationPeriod> e)
    {
        e.ToTable("tbl_GuardLocationRotationPeriods", "HR");
        e.HasKey(x => x.LocationRotationPeriodId);
        e.Property(x => x.LocationRotationPeriodId).HasColumnName("LocationRotationPeriodId").UseIdentityColumn();
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

public sealed class GuardLocationRotationAssignmentConfiguration : IEntityTypeConfiguration<GuardLocationRotationAssignment>
{
    public void Configure(EntityTypeBuilder<GuardLocationRotationAssignment> e)
    {
        e.ToTable("tbl_GuardLocationRotationAssignments", "HR");
        e.HasKey(x => x.LocationRotationAssignmentId);
        e.Property(x => x.LocationRotationAssignmentId).HasColumnName("LocationRotationAssignmentId").UseIdentityColumn();
        e.Property(x => x.LocationRotationPeriodId).HasColumnName("LocationRotationPeriodId");
        e.Property(x => x.GroupId).HasColumnName("GroupId");
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
        e.Property(x => x.LocationId).HasColumnName("LocationId");
        e.Property(x => x.PriorityTypeId).HasColumnName("PriorityTypeId");
        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Period)
            .WithMany(p => p.Assignments)
            .HasForeignKey(x => x.LocationRotationPeriodId)
            .HasConstraintName("FK_LocationAssign_Period")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .HasConstraintName("FK_LocationAssign_Group")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_LocationAssign_Employee")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("FK_LocationAssign_Location")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.PriorityType)
            .WithMany()
            .HasForeignKey(x => x.PriorityTypeId)
            .HasConstraintName("FK_LocationAssign_Priority")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardEmployeeSpecialRuleConfiguration : IEntityTypeConfiguration<GuardEmployeeSpecialRule>
{
    public void Configure(EntityTypeBuilder<GuardEmployeeSpecialRule> e)
    {
        e.ToTable("tbl_GuardEmployeeSpecialRules", "HR");
        e.HasKey(x => x.SpecialRuleId);
        e.Property(x => x.SpecialRuleId).HasColumnName("SpecialRuleId").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
        e.Property(x => x.FixedLocationId).HasColumnName("FixedLocationId");
        e.Property(x => x.FixedScheduleId).HasColumnName("FixedScheduleId");
        e.Property(x => x.Reason).HasMaxLength(500);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_SpecialRules_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.FixedLocation)
            .WithMany()
            .HasForeignKey(x => x.FixedLocationId)
            .HasConstraintName("FK_SpecialRules_Location")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.FixedSchedule)
            .WithMany()
            .HasForeignKey(x => x.FixedScheduleId)
            .HasConstraintName("FK_SpecialRules_Schedule")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => new { x.EmployeeId, x.IsActive })
            .HasDatabaseName("IX_SpecialRules_Employee");
    }
}

public sealed class GuardVacationPlanConfiguration : IEntityTypeConfiguration<GuardVacationPlan>
{
    public void Configure(EntityTypeBuilder<GuardVacationPlan> e)
    {
        e.ToTable("tbl_GuardVacationPlans", "HR");
        e.HasKey(x => x.GuardVacationPlanId);
        e.Property(x => x.GuardVacationPlanId).HasColumnName("GuardVacationPlanId").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeId");
        e.Property(x => x.DirectionApprovedBy).HasColumnName("DirectionApprovedBy");
        e.Property(x => x.SubmittedToDirectionBy).HasColumnName("SubmittedToDirectionBy");
        e.Property(x => x.SubmittedToDirectionAt).HasColumnName("SubmittedToDirectionAt");
        e.Property(x => x.Notes).HasMaxLength(1000);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_VacPlan_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_VacPlan_Status")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.DirectionApprover)
            .WithMany()
            .HasForeignKey(x => x.DirectionApprovedBy)
            .HasConstraintName("FK_VacPlan_ApprovedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.SubmittedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.SubmittedToDirectionBy)
            .HasConstraintName("FK_VacPlan_SubmittedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuardVacationRequestConfiguration : IEntityTypeConfiguration<GuardVacationRequest>
{
    public void Configure(EntityTypeBuilder<GuardVacationRequest> e)
    {
        e.ToTable("tbl_GuardVacationRequests", "HR");
        e.HasKey(x => x.GuardVacationRequestId);
        e.Property(x => x.GuardVacationRequestId).HasColumnName("GuardVacationRequestId").UseIdentityColumn();
        e.Property(x => x.EmployeeId).HasColumnName("EmployeeId");
        e.Property(x => x.GuardVacationPlanId).HasColumnName("GuardVacationPlanId");
        e.Property(x => x.VacationId).HasColumnName("VacationId");
        e.Property(x => x.RequestTypeId).HasColumnName("RequestTypeId");
        e.Property(x => x.StatusTypeId).HasColumnName("StatusTypeId");
        e.Property(x => x.RequestedBy).HasColumnName("RequestedBy");
        e.Property(x => x.DirectionApprovedBy).HasColumnName("DirectionApprovedBy");
        e.Property(x => x.SubmittedToDirectionBy).HasColumnName("SubmittedToDirectionBy");
        e.Property(x => x.SubmittedToDirectionAt).HasColumnName("SubmittedToDirectionAt");
        e.Property(x => x.RejectedBy).HasColumnName("RejectedBy");
        e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        e.Property(x => x.RejectionReason).HasMaxLength(500);
        e.Property(x => x.RequestedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.Property(x => x.RowVersion).IsRowVersion().HasColumnName("RowVersion").IsConcurrencyToken();
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .HasConstraintName("FK_VacReq_Employee")
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // opcional a nivel EF por el soft-delete de Employees (ver arriba)

        e.HasOne(x => x.Plan)
            .WithMany(p => p.Requests)
            .HasForeignKey(x => x.GuardVacationPlanId)
            .HasConstraintName("FK_VacReq_Plan")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.RequestType)
            .WithMany()
            .HasForeignKey(x => x.RequestTypeId)
            .HasConstraintName("FK_VacReq_RequestType")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.StatusType)
            .WithMany()
            .HasForeignKey(x => x.StatusTypeId)
            .HasConstraintName("FK_VacReq_Status")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Requester)
            .WithMany()
            .HasForeignKey(x => x.RequestedBy)
            .HasConstraintName("FK_VacReq_RequestedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.DirectionApprover)
            .WithMany()
            .HasForeignKey(x => x.DirectionApprovedBy)
            .HasConstraintName("FK_VacReq_ApprovedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.SubmittedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.SubmittedToDirectionBy)
            .HasConstraintName("FK_VacReq_SubmittedBy")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Rejector)
            .WithMany()
            .HasForeignKey(x => x.RejectedBy)
            .HasConstraintName("FK_VacReq_RejectedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
