using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de la entidad <see cref="People"/> para EF Core.
/// Mapea la tabla tbl_People del schema HR.
/// </summary>
public sealed class PeopleConfiguration : IEntityTypeConfiguration<People>
{
    public void Configure(EntityTypeBuilder<People> e)
    {
        e.ToTable("tbl_People", "HR");
        e.HasKey(x => x.PersonId);
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        e.Property(x => x.IdCard).HasMaxLength(20).IsRequired().HasColumnName("IDCard");
        e.Property(x => x.Email).HasMaxLength(150).IsRequired();
        e.Property(x => x.Phone).HasMaxLength(30);
        e.Property(x => x.Sex).HasMaxLength(50);
        e.Property(x => x.Gender).HasMaxLength(50);
        e.Property(x => x.Disability).HasMaxLength(200);
        e.Property(x => x.Address).HasMaxLength(255);
        e.Property(x => x.ConadisCard).HasMaxLength(50);
        e.Property(x => x.CountryId).HasColumnName("CountryID");
        e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
        e.Property(x => x.CantonId).HasColumnName("CantonID");
    }
}
