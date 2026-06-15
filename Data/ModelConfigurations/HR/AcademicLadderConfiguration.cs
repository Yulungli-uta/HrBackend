using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

public sealed class AcademicLadderConfiguration : IEntityTypeConfiguration<AcademicLadder>
{
    public void Configure(EntityTypeBuilder<AcademicLadder> e)
    {
        e.ToTable("tbl_AcademicLadder", "HR");
        e.HasKey(x => x.LadderId);
        e.Property(x => x.LadderId).HasColumnName("LadderID").UseIdentityColumn();
        e.Property(x => x.Code).HasMaxLength(30).IsRequired();
        e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        e.Property(x => x.CategoryTypeId).HasColumnName("CategoryTypeID");
        e.Property(x => x.LevelTypeId).HasColumnName("LevelTypeID");
        e.Property(x => x.NextLadderId).HasColumnName("NextLadderID");
        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("GETDATE()")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UQ_AcadLadder_Code");

        e.Property(x => x.DedicationTypeId).HasColumnName("DedicationTypeID");
        e.Property(x => x.BaseRmu).HasColumnName("BaseRMU").HasColumnType("DECIMAL(10,2)");

        e.HasOne(x => x.CategoryType)
            .WithMany()
            .HasForeignKey(x => x.CategoryTypeId)
            .HasConstraintName("FK_AcadLadder_Category")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.LevelType)
            .WithMany()
            .HasForeignKey(x => x.LevelTypeId)
            .HasConstraintName("FK_AcadLadder_Level")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.DedicationType)
            .WithMany()
            .HasForeignKey(x => x.DedicationTypeId)
            .HasConstraintName("FK_AcadLadder_Dedication")
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.NextLadder)
            .WithMany()
            .HasForeignKey(x => x.NextLadderId)
            .HasConstraintName("FK_AcadLadder_Next")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
