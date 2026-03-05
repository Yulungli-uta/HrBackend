using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data.ModelConfigurations.HR;

/// <summary>
/// Configuración de entidades de hoja de vida y datos biográficos del módulo HR:
/// Addresses, Institutions, EducationLevels, EmergencyContacts,
/// CatastrophicIllnesses, FamilyBurden, Trainings, WorkExperiences,
/// BankAccounts, Publications, Books, KnowledgeArea.
/// </summary>
public sealed class AddressesConfiguration : IEntityTypeConfiguration<Addresses>
{
    public void Configure(EntityTypeBuilder<Addresses> e)
    {
        e.ToTable("tbl_Addresses", "HR");
        e.HasKey(x => x.AddressId);
        e.Property(x => x.AddressId).HasColumnName("AddressID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.AddressTypeId).HasColumnName("AddressTypeID");
        e.Property(x => x.CountryId).HasColumnName("CountryID");
        e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
        e.Property(x => x.CantonId).HasColumnName("CantonID");
        e.Property(x => x.Parish).HasMaxLength(100);
        e.Property(x => x.Neighborhood).HasMaxLength(100);
        e.Property(x => x.MainStreet).HasMaxLength(100).IsRequired();
        e.Property(x => x.SecondaryStreet).HasMaxLength(100);
        e.Property(x => x.HouseNumber).HasMaxLength(20);
        e.Property(x => x.Reference).HasMaxLength(255);
    }
}

public sealed class InstitutionsConfiguration : IEntityTypeConfiguration<Institutions>
{
    public void Configure(EntityTypeBuilder<Institutions> e)
    {
        e.ToTable("tbl_Institutions", "HR");
        e.HasKey(x => x.InstitutionId);
        e.Property(x => x.InstitutionId).HasColumnName("InstitutionID");
        e.Property(x => x.InstitutionTypeId).HasColumnName("InstitutionTypeID");
        e.Property(x => x.CountryId).HasColumnName("CountryID");
        e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
        e.Property(x => x.CantonId).HasColumnName("CantonID");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public sealed class EducationLevelsConfiguration : IEntityTypeConfiguration<EducationLevels>
{
    public void Configure(EntityTypeBuilder<EducationLevels> e)
    {
        e.ToTable("tbl_EducationLevels", "HR");
        e.HasKey(x => x.EducationId);
        e.Property(x => x.EducationId).HasColumnName("EducationID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.EducationLevelTypeId).HasColumnName("EducationLevelTypeID");
        e.Property(x => x.InstitutionId).HasColumnName("InstitutionID");
        e.Property(x => x.Title).HasMaxLength(150).IsRequired();
        e.Property(x => x.Specialty).HasMaxLength(100);
        e.Property(x => x.Grade).HasMaxLength(50);
        e.Property(x => x.Location).HasMaxLength(100);
    }
}

public sealed class EmergencyContactsConfiguration : IEntityTypeConfiguration<EmergencyContacts>
{
    public void Configure(EntityTypeBuilder<EmergencyContacts> e)
    {
        e.ToTable("tbl_EmergencyContacts", "HR");
        e.HasKey(x => x.ContactId);
        e.Property(x => x.ContactId).HasColumnName("ContactID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Identification).HasMaxLength(20);
        e.Property(x => x.FirstName).HasMaxLength(100);
        e.Property(x => x.LastName).HasMaxLength(100);
        e.Property(x => x.RelationshipTypeId).HasColumnName("RelationshipTypeID");
        e.Property(x => x.Address).HasMaxLength(255);
        e.Property(x => x.Phone).HasMaxLength(30);
        e.Property(x => x.Mobile).HasMaxLength(30);
    }
}

public sealed class CatastrophicIllnessesConfiguration : IEntityTypeConfiguration<CatastrophicIllnesses>
{
    public void Configure(EntityTypeBuilder<CatastrophicIllnesses> e)
    {
        e.ToTable("tbl_CatastrophicIllnesses", "HR");
        e.HasKey(x => x.IllnessId);
        e.Property(x => x.IllnessId).HasColumnName("IllnessID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Illness).HasMaxLength(150);
        e.Property(x => x.IESSNumber).HasMaxLength(50);
        e.Property(x => x.SubstituteName).HasMaxLength(100);
        e.Property(x => x.IllnessTypeId).HasColumnName("IllnessTypeID");
        e.Property(x => x.CertificateNumber).HasMaxLength(50);
    }
}

public sealed class FamilyBurdenConfiguration : IEntityTypeConfiguration<FamilyBurden>
{
    public void Configure(EntityTypeBuilder<FamilyBurden> e)
    {
        e.ToTable("tbl_FamilyBurden", "HR");
        e.HasKey(x => x.BurdenId);
        e.Property(x => x.BurdenId).HasColumnName("BurdenID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.DependentId).HasMaxLength(20);
        e.Property(x => x.IdentificationTypeId).HasColumnName("IdentificationTypeID");
        e.Property(x => x.FirstName).HasMaxLength(100);
        e.Property(x => x.LastName).HasMaxLength(100);
    }
}

public sealed class TrainingsConfiguration : IEntityTypeConfiguration<Trainings>
{
    public void Configure(EntityTypeBuilder<Trainings> e)
    {
        e.ToTable("tbl_Trainings", "HR");
        e.HasKey(x => x.TrainingId);
        e.Property(x => x.TrainingId).HasColumnName("TrainingID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Location).HasMaxLength(100);
        e.Property(x => x.Title).HasMaxLength(200);
        e.Property(x => x.Institution).HasMaxLength(150);
        e.Property(x => x.KnowledgeAreaTypeId).HasColumnName("KnowledgeAreaTypeID");
        e.Property(x => x.EventTypeId).HasColumnName("EventTypeID");
        e.Property(x => x.CertifiedBy).HasMaxLength(150);
        e.Property(x => x.CertificateTypeId).HasColumnName("CertificateTypeID");
        e.Property(x => x.ApprovalTypeId).HasColumnName("ApprovalTypeID");
    }
}

public sealed class WorkExperiencesConfiguration : IEntityTypeConfiguration<WorkExperiences>
{
    public void Configure(EntityTypeBuilder<WorkExperiences> e)
    {
        e.ToTable("tbl_WorkExperiences", "HR");
        e.HasKey(x => x.WorkExpId);
        e.Property(x => x.WorkExpId).HasColumnName("WorkExpID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Company).HasMaxLength(150);
        e.Property(x => x.Position).HasMaxLength(120);
        e.Property(x => x.EntryReason).HasMaxLength(200);
        e.Property(x => x.ExitReason).HasMaxLength(200);
        e.Property(x => x.InstitutionAddress).HasMaxLength(255);
    }
}

public sealed class BankAccountsConfiguration : IEntityTypeConfiguration<BankAccounts>
{
    public void Configure(EntityTypeBuilder<BankAccounts> e)
    {
        e.ToTable("tbl_BankAccounts", "HR");
        e.HasKey(x => x.AccountId);
        e.Property(x => x.AccountId).HasColumnName("AccountID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.FinancialInstitution).HasMaxLength(150);
        e.Property(x => x.AccountTypeId).HasColumnName("AccountTypeID");
        e.Property(x => x.AccountNumber).HasMaxLength(50);
    }
}

public sealed class PublicationsConfiguration : IEntityTypeConfiguration<Publications>
{
    public void Configure(EntityTypeBuilder<Publications> e)
    {
        e.ToTable("tbl_Publications", "HR");
        e.HasKey(x => x.PublicationId);
        e.Property(x => x.PublicationId).HasColumnName("PublicationID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.Location).HasMaxLength(100);
        e.Property(x => x.Issn_Isbn).HasColumnName("ISSN_ISBN").HasMaxLength(20);
        e.Property(x => x.JournalName).HasMaxLength(200);
        e.Property(x => x.JournalNumber).HasMaxLength(50);
        e.Property(x => x.Volume).HasMaxLength(50);
        e.Property(x => x.Pages).HasMaxLength(20);
        e.Property(x => x.Title).HasMaxLength(300).IsRequired();
        e.Property(x => x.OrganizedBy).HasMaxLength(150);
        e.Property(x => x.EventName).HasMaxLength(200);
        e.Property(x => x.EventEdition).HasMaxLength(50);
    }
}

public sealed class BooksConfiguration : IEntityTypeConfiguration<Books>
{
    public void Configure(EntityTypeBuilder<Books> e)
    {
        e.ToTable("tbl_Books", "HR");
        e.HasKey(x => x.BookId);
        e.Property(x => x.BookId).HasColumnName("BookID");
        e.Property(x => x.PersonId).HasColumnName("PersonID");
        e.Property(x => x.BookTypeId).HasColumnName("BookTypeID");
        e.Property(x => x.ParticipationTypeId).HasColumnName("ParticipationTypeID");
        e.Property(x => x.Title).HasMaxLength(300);
        e.Property(x => x.ISBN).HasMaxLength(20);
        e.Property(x => x.Publisher).HasMaxLength(200);
        e.Property(x => x.City).HasMaxLength(100);
    }
}

public sealed class KnowledgeAreaConfiguration : IEntityTypeConfiguration<KnowledgeArea>
{
    public void Configure(EntityTypeBuilder<KnowledgeArea> e)
    {
        e.ToTable("tbl_KnowledgeArea", "HR");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.ParentId).HasColumnName("parent_id");
    }
}
