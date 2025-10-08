
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using WsUtaSystem.Models;

namespace WsUtaSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
    {
        cfg.Properties<decimal>().HavePrecision(18, 2);      // salarios, montos, etc.
        cfg.Properties<DateTime>().HaveColumnType("datetime2");
        cfg.Properties<TimeSpan>().HaveColumnType("time(0)");
        // Si usas TimeOnly/DateOnly:
        //cfg.Properties<TimeOnly>().HaveConversion<TimeOnlyConverter>().HaveColumnType("time(0)");
        //cfg.Properties<DateOnly>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
    }

    // DbSets
    public DbSet<People> People => Set<People>();
    public DbSet<Employees> Employees => Set<Employees>();
    public DbSet<Faculties> Faculties => Set<Faculties>();
    public DbSet<Departments> Departments => Set<Departments>();
    public DbSet<Schedules> Schedules => Set<Schedules>();
    public DbSet<EmployeeSchedules> EmployeeSchedules => Set<EmployeeSchedules>();
    public DbSet<Contracts> Contracts => Set<Contracts>();
    public DbSet<SalaryHistory> SalaryHistory => Set<SalaryHistory>();
    public DbSet<PermissionTypes> PermissionTypes => Set<PermissionTypes>();
    public DbSet<Vacations> Vacations => Set<Vacations>();
    public DbSet<Permissions> Permissions => Set<Permissions>();
    public DbSet<AttendancePunches> AttendancePunches => Set<AttendancePunches>();
    public DbSet<PunchJustifications> PunchJustifications => Set<PunchJustifications>();
    public DbSet<AttendanceCalculations> AttendanceCalculations => Set<AttendanceCalculations>();
    public DbSet<OvertimeConfig> OvertimeConfig => Set<OvertimeConfig>();
    public DbSet<Overtime> Overtime => Set<Overtime>();
    public DbSet<TimeRecoveryPlans> TimeRecoveryPlans => Set<TimeRecoveryPlans>();
    public DbSet<TimeRecoveryLogs> TimeRecoveryLogs => Set<TimeRecoveryLogs>();
    public DbSet<Subrogations> Subrogations => Set<Subrogations>();
    public DbSet<PersonnelMovements> PersonnelMovements => Set<PersonnelMovements>();
    public DbSet<Payroll> Payroll => Set<Payroll>();
    public DbSet<PayrollLines> PayrollLines => Set<PayrollLines>();
    public DbSet<Audit> Audit => Set<Audit>();
    // Geo/Ref/CV
    public DbSet<RefTypes> RefTypes => Set<RefTypes>();
    public DbSet<Countries> Countries => Set<Countries>();
    public DbSet<Provinces> Provinces => Set<Provinces>();
    public DbSet<Cantons> Cantons => Set<Cantons>();
    public DbSet<Addresses> Addresses => Set<Addresses>();
    public DbSet<Institutions> Institutions => Set<Institutions>();
    public DbSet<EducationLevels> EducationLevels => Set<EducationLevels>();
    public DbSet<EmergencyContacts> EmergencyContacts => Set<EmergencyContacts>();
    public DbSet<CatastrophicIllnesses> CatastrophicIllnesses => Set<CatastrophicIllnesses>();
    public DbSet<FamilyBurden> FamilyBurden => Set<FamilyBurden>();
    public DbSet<Trainings> Trainings => Set<Trainings>();
    public DbSet<WorkExperiences> WorkExperiences => Set<WorkExperiences>();
    public DbSet<BankAccounts> BankAccounts => Set<BankAccounts>();
    public DbSet<Publications> Publications => Set<Publications>();
    public DbSet<Books> Books => Set<Books>();

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<VwEmployeeComplete> vwEmployeeComplete { get; set; }

    public DbSet<VwEmployeeDetails> vwEmployeeDetails { get; set; }  

    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<TimePlanning> TimePlanning => Set<TimePlanning>();

    public DbSet<TimePlanningEmployee> TimePlanningEmployee => Set<TimePlanningEmployee>();

    public DbSet<TimePlanningExecution> TimePlanningExecution => Set<TimePlanningExecution>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        const string HR = "HR";
        
        // People
        m.Entity<People>(e => {
            e.ToTable("tbl_People", HR);
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
        });

        // Employees
        m.Entity<Employees>(e => {
            e.ToTable("tbl_Employees", HR);
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.Type).HasColumnName("EmployeeType");
            e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
            e.Property(x => x.ImmediateBossId).HasColumnName("ImmediateBossID");
        });

        // Faculties
        m.Entity<Faculties>(e => {
            e.ToTable("tbl_Faculties", HR);
            e.HasKey(x => x.FacultyId);
            e.Property(x => x.FacultyId).HasColumnName("FacultyID");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.DeanEmployeeId).HasColumnName("DeanEmployeeID");
        });

        // Departments
        m.Entity<Departments>(e => {
            e.ToTable("tbl_Departments", HR);
            e.HasKey(x => x.DepartmentId);
            e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
            //e.Property(x => x.FacultyId).HasColumnName("FacultyID");
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
        });

        // Schedules
        m.Entity<Schedules>(e => {
            e.ToTable("tbl_Schedules", HR);
            e.HasKey(x => x.ScheduleId);
            e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
            e.Property(x => x.Description).HasMaxLength(120).IsRequired();
            e.Property(x => x.WorkingDays).HasMaxLength(20).IsRequired();
            e.Property(x => x.RotationPattern).HasMaxLength(120);
        });

        m.Entity<EmployeeSchedules>(e => {
            e.ToTable("tbl_EmployeeSchedules", HR);
            e.HasKey(x => x.EmpScheduleId);
            e.Property(x => x.EmpScheduleId).HasColumnName("EmpScheduleID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.ScheduleId).HasColumnName("ScheduleID");
        });

        m.Entity<Contracts>(e => {
            e.ToTable("tbl_Contracts", HR);
            e.HasKey(x => x.ContractId);
            e.Property(x => x.ContractId).HasColumnName("ContractID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.JobId).HasColumnName("JobID");
            //e.Property(x => x.ContractType).HasMaxLength(50);
        });

        m.Entity<SalaryHistory>(e => {
            e.ToTable("tbl_SalaryHistory", HR);
            e.HasKey(x => x.SalaryHistoryId);
            e.Property(x => x.SalaryHistoryId).HasColumnName("SalaryHistoryID");
            e.Property(x => x.ContractId).HasColumnName("ContractID");
            e.Property(x => x.Reason).HasMaxLength(300);
        });

        m.Entity<PermissionTypes>(e => {
            e.ToTable("tbl_PermissionTypes", HR);
            e.HasKey(x => x.TypeId);
            e.Property(x => x.TypeId).HasColumnName("TypeID");
            e.Property(x => x.Name).HasMaxLength(80);
        });

        m.Entity<Vacations>(e => {
            e.ToTable("tbl_Vacations", HR);
            e.HasKey(x => x.VacationId);
            e.Property(x => x.VacationId).HasColumnName("VacationID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.Status).HasMaxLength(20);
        });

        m.Entity<Permissions>(e => {
            e.ToTable("tbl_Permissions", HR);
            e.HasKey(x => x.PermissionId);
            //e.Property(x => x.PermissionId).HasColumnName("PermissionID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.PermissionTypeId).HasColumnName("PermissionTypeID");
            e.Property(x => x.Justification);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.VacationId).HasColumnName("VacationID");
        });

     
        m.Entity<AttendancePunches>(e => {
            e.ToTable("tbl_AttendancePunches", "HR", tb =>
            {
                // Declara los triggers que existen en SQL Server (puedes listar varios)
                tb.HasTrigger("trg_Punch_Validations");
                // tb.HasTrigger("trg_Punch_Otros"); // si tienes más
            });
            e.HasKey(x => x.PunchId);
            e.Property(x => x.PunchId).UseIdentityColumn();
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.PunchTime).HasColumnType("datetime2");            
            e.Property(x => x.PunchType).HasMaxLength(10);
            e.Property(x => x.DeviceId).HasMaxLength(60);
            e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            
        });

        m.Entity<PunchJustifications>(e => {
            e.ToTable("tbl_PunchJustifications", HR);
            e.HasKey(x => x.PunchJustId);
            e.Property(x => x.PunchJustId).HasColumnName("PunchJustID").ValueGeneratedOnAdd();
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID").IsRequired();
            e.Property(x => x.BossEmployeeId).HasColumnName("BossEmployeeID").IsRequired();
            e.Property(x => x.JustificationTypeId).HasColumnName("JustificationTypeID").IsRequired();
        });

        m.Entity<AttendanceCalculations>(e => {
            e.ToTable("tbl_AttendanceCalculations", HR);
            e.HasKey(x => x.CalculationId);
            e.Property(x => x.CalculationId).HasColumnName("CalculationID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.Status).HasMaxLength(12);
        });

        m.Entity<OvertimeConfig>(e => {
            e.ToTable("tbl_OvertimeConfig", HR);
            e.HasKey(x => x.OvertimeType);
            e.Property(x => x.OvertimeType).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(200);
        });

        m.Entity<Overtime>(e => {
            e.ToTable("tbl_Overtime", HR);
            e.HasKey(x => x.OvertimeId);
            e.Property(x => x.OvertimeId).HasColumnName("OvertimeID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.OvertimeType).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        m.Entity<TimeRecoveryPlans>(e => {
            e.ToTable("tbl_TimeRecoveryPlans", HR);
            e.HasKey(x => x.RecoveryPlanId);
            e.Property(x => x.RecoveryPlanId).HasColumnName("RecoveryPlanID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.Reason).HasMaxLength(300);
        });

        m.Entity<TimeRecoveryLogs>(e => {
            e.ToTable("tbl_TimeRecoveryLogs", HR);
            e.HasKey(x => x.RecoveryLogId);
            e.Property(x => x.RecoveryLogId).HasColumnName("RecoveryLogID");
            e.Property(x => x.RecoveryPlanId).HasColumnName("RecoveryPlanID");
        });

        m.Entity<Subrogations>(e => {
            e.ToTable("tbl_Subrogations", HR);
            e.HasKey(x => x.SubrogationId);
            e.Property(x => x.SubrogationId).HasColumnName("SubrogationID");
            e.Property(x => x.SubrogatedEmployeeId).HasColumnName("SubrogatedEmployeeID");
            e.Property(x => x.SubrogatingEmployeeId).HasColumnName("SubrogatingEmployeeID");
            e.Property(x => x.PermissionId).HasColumnName("PermissionID");
            e.Property(x => x.VacationId).HasColumnName("VacationID");
            e.Property(x => x.Reason).HasMaxLength(300);
        });

        m.Entity<PersonnelMovements>(e => {
            e.ToTable("tbl_PersonnelMovements", HR);
            e.HasKey(x => x.MovementId);
            e.Property(x => x.MovementId).HasColumnName("MovementID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.ContractId).HasColumnName("ContractID");
            e.Property(x => x.JobId).HasColumnName("JobID");
            e.Property(x => x.OriginDepartmentId).HasColumnName("OriginDepartmentID");
            e.Property(x => x.DestinationDepartmentId).HasColumnName("DestinationDepartmentID");
            e.Property(x => x.MovementType).HasMaxLength(30);
            e.Property(x => x.DocumentLocation).HasMaxLength(255);
            e.Property(x => x.Reason).HasMaxLength(500);
        });

        m.Entity<Payroll>(e => {
            e.ToTable("tbl_Payroll", HR);
            e.HasKey(x => x.PayrollId);
            e.Property(x => x.PayrollId).HasColumnName("PayrollID");
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.Period).HasMaxLength(7);
            e.Property(x => x.Status).HasMaxLength(15);
            e.Property(x => x.BankAccount).HasMaxLength(50);
        });

        m.Entity<PayrollLines>(e => {
            e.ToTable("tbl_PayrollLines", HR);
            e.HasKey(x => x.PayrollLineId);
            e.Property(x => x.PayrollLineId).HasColumnName("PayrollLineID");
            e.Property(x => x.PayrollId).HasColumnName("PayrollID");
            e.Property(x => x.LineType).HasMaxLength(20);
            e.Property(x => x.Concept).HasMaxLength(120);
            e.Property(x => x.Notes).HasMaxLength(300);
        });

        m.Entity<Audit>(e => {
            e.ToTable("tbl_Audit", HR);
            e.HasKey(x => x.AuditId);
            e.Property(x => x.AuditId).HasColumnName("AuditID");
            e.Property(x => x.TableName).HasMaxLength(128);
            e.Property(x => x.Action).HasMaxLength(20);
            e.Property(x => x.RecordId).HasColumnName("RecordID");
        });

        // REF / GEO / CV
        m.Entity<RefTypes>(e => {
            e.ToTable("ref_Types", HR);
            e.HasKey(x => x.TypeId);
            e.Property(x => x.TypeId).HasColumnName("TypeID");
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(255);
        });

        m.Entity<Countries>(e => {
            e.ToTable("tbl_Countries", HR);
            e.HasKey(x => x.CountryId);
            e.Property(x => x.CountryId).HasColumnName("CountryID");
            //e.Property(x => x.CountryCode).HasMaxLength(5);
            e.Property(x => x.CountryName).HasMaxLength(100);
        });

        m.Entity<Provinces>(e => {
            e.ToTable("tbl_Provinces", HR);
            e.HasKey(x => x.ProvinceId);
            e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
            e.Property(x => x.CountryId).HasColumnName("CountryID");
            //e.Property(x => x.ProvinceCode).HasMaxLength(5);
            e.Property(x => x.ProvinceName).HasMaxLength(100);
        });

        m.Entity<Cantons>(e => {
            e.ToTable("tbl_Cantons", HR);
            e.HasKey(x => x.CantonId);
            e.Property(x => x.CantonId).HasColumnName("CantonID");
            e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
            //e.Property(x => x.CantonCode).HasMaxLength(5);
            e.Property(x => x.CantonName).HasMaxLength(100);
        });

        m.Entity<Addresses>(e => {
            e.ToTable("tbl_Addresses", HR);
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
        });

        m.Entity<Institutions>(e => {
            e.ToTable("tbl_Institutions", HR);
            e.HasKey(x => x.InstitutionId);
            e.Property(x => x.InstitutionId).HasColumnName("InstitutionID");
            e.Property(x => x.InstitutionTypeId).HasColumnName("InstitutionTypeID");
            e.Property(x => x.CountryId).HasColumnName("CountryID");
            e.Property(x => x.ProvinceId).HasColumnName("ProvinceID");
            e.Property(x => x.CantonId).HasColumnName("CantonID");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        m.Entity<EducationLevels>(e => {
            e.ToTable("tbl_EducationLevels", HR);
            e.HasKey(x => x.EducationId);
            e.Property(x => x.EducationId).HasColumnName("EducationID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.EducationLevelTypeId).HasColumnName("EducationLevelTypeID");
            e.Property(x => x.InstitutionId).HasColumnName("InstitutionID");
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.Property(x => x.Specialty).HasMaxLength(100);
            e.Property(x => x.Grade).HasMaxLength(50);
            e.Property(x => x.Location).HasMaxLength(100);
        });

        m.Entity<EmergencyContacts>(e => {
            e.ToTable("tbl_EmergencyContacts", HR);
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
        });

        m.Entity<CatastrophicIllnesses>(e => {
            e.ToTable("tbl_CatastrophicIllnesses", HR);
            e.HasKey(x => x.IllnessId);
            e.Property(x => x.IllnessId).HasColumnName("IllnessID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.Illness).HasMaxLength(150);
            e.Property(x => x.IESSNumber).HasMaxLength(50);
            e.Property(x => x.SubstituteName).HasMaxLength(100);
            e.Property(x => x.IllnessTypeId).HasColumnName("IllnessTypeID");
            e.Property(x => x.CertificateNumber).HasMaxLength(50);
        });

        m.Entity<FamilyBurden>(e => {
            e.ToTable("tbl_FamilyBurden", HR);
            e.HasKey(x => x.BurdenId);
            e.Property(x => x.BurdenId).HasColumnName("BurdenID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.DependentId).HasMaxLength(20);
            e.Property(x => x.IdentificationTypeId).HasColumnName("IdentificationTypeID");
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
        });

        m.Entity<Trainings>(e => {
            e.ToTable("tbl_Trainings", HR);
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
        });

        m.Entity<WorkExperiences>(e => {
            e.ToTable("tbl_WorkExperiences", HR);
            e.HasKey(x => x.WorkExpId);
            e.Property(x => x.WorkExpId).HasColumnName("WorkExpID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.Company).HasMaxLength(150);
            e.Property(x => x.Position).HasMaxLength(120);
            e.Property(x => x.EntryReason).HasMaxLength(200);
            e.Property(x => x.ExitReason).HasMaxLength(200);
            e.Property(x => x.InstitutionAddress).HasMaxLength(255);
        });

        m.Entity<BankAccounts>(e => {
            e.ToTable("tbl_BankAccounts", HR);
            e.HasKey(x => x.AccountId);
            e.Property(x => x.AccountId).HasColumnName("AccountID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.FinancialInstitution).HasMaxLength(150);
            e.Property(x => x.AccountTypeId).HasColumnName("AccountTypeID");
            e.Property(x => x.AccountNumber).HasMaxLength(50);
        });

        m.Entity<Publications>(e => {
            e.ToTable("tbl_Publications", HR);
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
        });

        m.Entity<Books>(e => {
            e.ToTable("tbl_Books", HR);
            e.HasKey(x => x.BookId);
            e.Property(x => x.BookId).HasColumnName("BookID");
            e.Property(x => x.PersonId).HasColumnName("PersonID");
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.ISBN).HasMaxLength(20);
            e.Property(x => x.Publisher).HasMaxLength(200);
            e.Property(x => x.City).HasMaxLength(100);
        });

        m.Entity<VwEmployeeComplete>(e =>
         {
             e.HasNoKey(); // Por ser vista
             e.ToView("vw_EmployeeComplete", "HR");             
         });

        m.Entity<VwEmployeeDetails>(e =>
        {
            e.HasNoKey(); // Por ser vista
            e.ToView("vw_EmployeeDetails", "HR");
        });

        m.Entity<Job>(e => {
            e.ToTable("tbl_Jobs", HR);
            e.HasKey(x => x.JobID);
            e.Property(x => x.JobID).HasColumnName("JobID");            
        });
        m.Entity<Holiday>(e => {
            e.ToTable("tbl_Holidays", HR);
            e.HasKey(x => x.HolidayID);
            e.Property(x => x.HolidayID).HasColumnName("HolidayID");
        });
        m.Entity<TimePlanning>(e => {
            e.ToTable("tbl_TimePlanning", HR);
            e.HasKey(x => x.PlanID);
            e.Property(x => x.PlanID).HasColumnName("PlanID");
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        m.Entity<TimePlanningEmployee>(e => {
            e.ToTable("tbl_TimePlanningEmployees", HR);
            e.HasKey(x => x.PlanEmployeeID);
            e.Property(x => x.PlanEmployeeID).HasColumnName("PlanEmployeeID");
        });
        m.Entity<TimePlanningExecution>(e => {
            e.ToTable("tbl_TimePlanningExecution", HR);
            e.HasKey(x => x.ExecutionID);
            e.Property(x => x.ExecutionID).HasColumnName("ExecutionID");
        });
    }
}
