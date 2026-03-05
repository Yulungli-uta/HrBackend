
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Models.Views;
using System.Reflection.Emit;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Views;

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

    public DbSet<Activity> Activity  => Set<Activity>();
    public DbSet<AdditionalActivity> AdditionalActivity => Set<AdditionalActivity>();
    public DbSet<ContractType> ContractType => Set<ContractType>();
    public DbSet<Degree> Degree => Set<Degree>();
    public DbSet<OccupationalGroup> OccupationalGroup => Set<OccupationalGroup>();
    public DbSet<JobActivity> JobActivity => Set<JobActivity>();
    public DbSet<ContractRequest> ContractRequest => Set<ContractRequest>();    
    public DbSet<FinancialCertification> FinancialCertification => Set<FinancialCertification>();
    public DbSet<Parameters> Parameters => Set<Parameters>();
    public DbSet<DirectoryParameters> DirectoryParameters => Set<DirectoryParameters>();

    public DbSet<VwEmployeeScheduleAtDate> VwEmployeeScheduleAtDate { get; set; }
    public DbSet<VwPunchDay> VwPunchDay { get; set; }
    public DbSet<VwLeaveWindows> VwLeaveWindows { get; set; }
    public DbSet<VwAttendanceDay> VwAttendanceDay { get; set; }
    public DbSet<KnowledgeArea> KnowledgeAreas => Set<KnowledgeArea>();
    public DbSet<TimeBalances> TimeBalances => Set<TimeBalances>();
    public DbSet<EmailLayout> EmailLayouts => Set<EmailLayout>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailLogAttachment> EmailLogAttachments => Set<EmailLogAttachment>();
    public DbSet<ContractStatusTransition> ContractStatusTransitions => Set<ContractStatusTransition>();
    public DbSet<ContractStatusHistory> ContractStatusHistories => Set<ContractStatusHistory>();

    // Docflow
    public DbSet<WsUtaSystem.Models.Docflow.DocflowProcessHierarchy> DocflowProcesses => Set<WsUtaSystem.Models.Docflow.DocflowProcessHierarchy>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowProcessTransition> DocflowTransitions => Set<WsUtaSystem.Models.Docflow.DocflowProcessTransition>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowDocumentRule> DocflowDocumentRules => Set<WsUtaSystem.Models.Docflow.DocflowDocumentRule>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowWorkflowInstance> DocflowInstances => Set<WsUtaSystem.Models.Docflow.DocflowWorkflowInstance>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowDocument> DocflowDocuments => Set<WsUtaSystem.Models.Docflow.DocflowDocument>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowFileVersion> DocflowFileVersions => Set<WsUtaSystem.Models.Docflow.DocflowFileVersion>();
    public DbSet<WsUtaSystem.Models.Docflow.DocflowWorkflowMovement> DocflowMovements => Set<WsUtaSystem.Models.Docflow.DocflowWorkflowMovement>();

    // Vistas de Permisos y Menús
    public DbSet<VwUserRole> VwUserRoles { get; set; }
    public DbSet<VwRoleMenuItem> VwRoleMenuItems { get; set; }

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
            e.Property(x => x.EmployeeType).HasColumnName("EmployeeType");
            e.Property(x => x.DepartmentId).HasColumnName("DepartmentID");
            e.Property(x => x.ImmediateBossId).HasColumnName("ImmediateBossID");
        });

        // Faculties
        m.Entity<Faculties>(e =>
        {
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
            e.Ignore(x => x.RowVersion);
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
            e.HasKey(x => x.ContractID);
            e.Property(x => x.ContractID).HasColumnName("ContractID");
            e.Property(x => x.PersonID).HasColumnName("PersonID");
            e.Property(x => x.JobID).HasColumnName("JobID");
            e.Property(x => x.RegistrationDateAnulCon).HasColumnName("registrationdate_anul_con");
            e.Property(x => x.WorkOf).HasColumnName("work_of");
            
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
            e.Property(x => x.HourTaken).HasColumnName("HourTaken");
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
            e.Property(x => x.PunchId).HasColumnName("PunchID").ValueGeneratedOnAdd().UseIdentityColumn();
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID");
            e.Property(x => x.PunchTime).HasColumnType("datetime2");            
            e.Property(x => x.PunchType).HasMaxLength(10);
            e.Property(x => x.DeviceId).HasMaxLength(60);            
            e.Property(x => x.IpAddress).HasColumnName("IpAddress");
            //e.Property(x => x.CreatedAt).HasColumnType("datetime2");
            e.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAdd()
                .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
            
            // ✅ RowVersion: control de concurrencia optimista
            e.Property(x => x.RowVersion)
                .IsRowVersion()
                .HasColumnName("RowVersion")
                .IsConcurrencyToken();
        });

        m.Entity<PunchJustifications>(e => {
            e.ToTable("tbl_PunchJustifications", HR);
            e.HasKey(x => x.PunchJustId);
            e.Property(x => x.PunchJustId).HasColumnName("PunchJustID").ValueGeneratedOnAdd();
            e.Property(x => x.EmployeeId).HasColumnName("EmployeeID").IsRequired();
            e.Property(x => x.BossEmployeeId).HasColumnName("BossEmployeeID").IsRequired();
            e.Property(x => x.JustificationTypeId).HasColumnName("JustificationTypeID").IsRequired();
            e.Property(x => x.PunchTypeId).HasColumnName("PunchTypeID");
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
            e.Property(x => x.BookTypeId).HasColumnName("BookTypeID");
            e.Property(x => x.ParticipationTypeId).HasColumnName("ParticipationTypeID");
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
            e.ToView("vw_EmployeeDetails", HR);
            e.HasNoKey(); // Por ser vista
            e.Property(e => e.EmployeeID).HasColumnName("EmployeeID");
            e.Property(e => e.FirstName).HasColumnName("FirstName");
            e.Property(e => e.LastName).HasColumnName("LastName");
            e.Property(e => e.IDCard).HasColumnName("IDCard");
            e.Property(e => e.Email).HasColumnName("Email");
            e.Property(e => e.EmployeeType).HasColumnName("EmployeeType");
            e.Property(e => e.ContractType).HasColumnName("ContractType");
            e.Property(e => e.ImmediateBossID).HasColumnName("ImmediateBossID");
            e.Property(e => e.ScheduleID).HasColumnName("ScheduleID");
            e.Property(e => e.Schedule).HasColumnName("Schedule");
            e.Property(e => e.Department).HasColumnName("Department");
            e.Property(e => e.BaseSalary).HasColumnName("BaseSalary");
            e.Property(e => e.HireDate).HasColumnName("HireDate");
            
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
            e.Property(x => x.PlanID).HasColumnName("PlanID");
            e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
           /* e.HasOne(e => e.TimePlanning)
               .WithMany() // Ajusta según tu modelo
               .HasForeignKey(e => e.PlanID)
               .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación con Employees
            e.HasOne(e => e.Employees)
                   .WithMany() // Ajusta según tu modelo
                   .HasForeignKey(e => e.EmployeeID)
                   .OnDelete(DeleteBehavior.Restrict);*/
           e.HasOne(e => e.TimePlanning)
               .WithMany()
               .HasForeignKey(e => e.PlanID)
               .HasConstraintName("FK_TimePlanningEmployees_Plan")
               .OnDelete(DeleteBehavior.Cascade);

        // Relación con Employees
           e.HasOne(e => e.Employees)
               .WithMany()
               .HasForeignKey(e => e.EmployeeID)
               .HasConstraintName("FK_TimePlanningEmployees_Employee")
               .OnDelete(DeleteBehavior.Restrict);

        });
        m.Entity<TimePlanningExecution>(e => {
            e.ToTable("tbl_TimePlanningExecution", HR);
            e.HasKey(x => x.ExecutionID);
            e.Property(x => x.ExecutionID).HasColumnName("ExecutionID");
            e.Property(x => x.PlanEmployeeID).HasColumnName("PlanEmployeeID"); ;
        });
        m.Entity<Activity>(e => {
            e.ToTable("tbl_Activities", HR);
            e.HasKey(x => x.ActivitiesId);
            e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
        });
        m.Entity<AdditionalActivity>(e => {
            e.ToTable("tbl_AdditionalActivities", HR);
            e.HasKey(x => new { x.ActivitiesId, x.ContractId });
            e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
            e.Property(x => x.ContractId).HasColumnName("ContractID");
        });
        m.Entity<JobActivity>(e => {
            e.ToTable("tbl_JobActivities", HR);
            e.HasKey(x => new { x.ActivitiesId, x.JobID }); 
            e.Property(x => x.ActivitiesId).HasColumnName("ActivitiesID");
            e.Property(x => x.JobID).HasColumnName("JobID");
        });
        m.Entity<ContractType>(e => {
            e.ToTable("tbl_contract_type", HR);
            e.HasKey(x => x.ContractTypeId);
            e.Property(x => x.ContractTypeId).HasColumnName("ContractTypeID");
            
        });
        m.Entity<Degree>(e => {
            e.ToTable("tbl_Degrees", HR);
            e.HasKey(x => x.DegreeId);
            e.Property(x => x.DegreeId).HasColumnName("DegreeID");
        });
        m.Entity<OccupationalGroup>(e => {
            e.ToTable("tbl_Occupational_Groups", HR);
            e.HasKey(x => x.GroupId);
            e.Property(x => x.GroupId).HasColumnName("GroupID");
        });
        m.Entity<ContractRequest>(e => {
            e.ToTable("tbl_contractRequest", HR);
            e.HasKey(x => x.RequestId);
            e.Property(x => x.RequestId).HasColumnName("RequestID");
            e.Property(x => x.CreatedBy).HasColumnName("CreatedBy");
            e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            e.Property(x => x.UpdatedBy).HasColumnName("UpdatedBy");
        });
        m.Entity<FinancialCertification>(e => {
            e.ToTable("tbl_FinancialCertification", HR);
            e.HasKey(x => x.CertificationId);
            e.Property(x => x.CertificationId).HasColumnName("CertificationID");
            e.Property(x => x.RmuCon).HasColumnName("rmu_con");
            e.Property(x => x.RmuHour).HasColumnName("rmu_hour");
            e.Property(x => x.RequestId).HasColumnName("RequestID");
            
        });
        m.Entity<Parameters>(e => {
            e.ToTable("TBL_PARAMETERS", HR);
            e.HasKey(x => x.ParameterId);
            e.Property(x => x.ParameterId).HasColumnName("ParameterID");
        });
        m.Entity<DirectoryParameters>(e => {
            e.ToTable("TBL_DirectoryParameters", HR);
            e.HasKey(x => x.DirectoryId);
            e.Property(x => x.DirectoryId).HasColumnName("DirectoryID");
        });
        m.Entity<StoredFile>(e =>
        {
            e.ToTable("TBL_StoredFile", "HR");

            e.HasKey(x => x.FileId);

            e.Property(x => x.FileId)
                .ValueGeneratedOnAdd();

            e.Property(x => x.FileGuid)
                .HasDefaultValueSql("newid()");

            e.Property(x => x.DirectoryCode)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(x => x.EntityType)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(x => x.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.UploadYear)
                .HasColumnType("smallint")
                .HasConversion<short>()
                .IsRequired();

            e.Property(x => x.RelativeFolder)
                .HasMaxLength(600)
                .IsRequired();

            e.Property(x => x.StoredFileName)
                .HasMaxLength(260)
                .IsRequired();

            e.Property(x => x.OriginalFileName)
                .HasMaxLength(260);

            e.Property(x => x.Extension)
                .HasMaxLength(20);

            e.Property(x => x.ContentType)
                .HasMaxLength(100);

            e.Property(x => x.SizeBytes)
                .IsRequired();

            e.Property(x => x.Sha256)
                .HasColumnType("binary(32)");

            e.Property(x => x.Status)
                .HasConversion<byte>()      // tu propiedad es int, la guardas/lees como tinyint
                .HasColumnType("tinyint")
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Computed column
            e.Property(x => x.FilePathHash)
                .HasColumnType("binary(32)")
                .ValueGeneratedOnAddOrUpdate()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore); ;

            // FK -> DirectoryParameters(Code)
            e.HasOne<DirectoryParameters>() // ver clase abajo
                .WithMany()
                .HasForeignKey(x => x.DirectoryCode)
                .HasPrincipalKey(p => p.Code);            

            // Índices (opcional en EF, ya existen en DB)
            //e.HasIndex(x => new { x.DirectoryCode, x.EntityType, x.EntityId, x.UploadYear })
            //    .HasDatabaseName("IX_TBL_StoredFile_ByEntity");

            //e.HasIndex(x => new { x.DirectoryCode, x.UploadYear })
            //    .HasDatabaseName("IX_TBL_StoredFile_ByDirectoryYear");

            //e.HasIndex(x => x.FileGuid)
            //    .IsUnique()
            //    .HasDatabaseName("UX_TBL_StoredFile_FileGuid");
        });

        m.Entity<KnowledgeArea>(e => {
            e.ToTable("tbl_KnowledgeArea", HR);
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ParentId).HasColumnName("parent_id");            
        });

        m.Entity<EmailLayout>(e =>
        {
            e.ToTable("tbl_EmailLayouts", HR);

            e.HasKey(x => x.EmailLayoutID);

            e.Property(x => x.EmailLayoutID)
                .HasColumnName("EmailLayoutID")
                .ValueGeneratedOnAdd();

            e.Property(x => x.Slug)
                .HasMaxLength(150)
                .IsRequired();

            e.HasIndex(x => x.Slug)
                .IsUnique()
                .HasDatabaseName("UX_tbl_EmailLayouts_Slug");

            e.Property(x => x.HeaderHtml).HasColumnType("nvarchar(max)");
            e.Property(x => x.FooterHtml).HasColumnType("nvarchar(max)");

            e.Property(x => x.IsActive)
                .HasDefaultValue(true);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAdd();

            e.Property(x => x.CreatedBy);

            e.Property(x => x.UpdatedAt);
            e.Property(x => x.UpdatedBy);
        });

        // EmailLogs
        m.Entity<EmailLog>(e =>
        {
            e.ToTable("tbl_EmailLogs", HR);

            e.HasKey(x => x.EmailLogID);

            e.Property(x => x.EmailLogID)
                .HasColumnName("EmailLogID")
                .ValueGeneratedOnAdd();

            e.Property(x => x.Recipient)
                .HasMaxLength(320)
                .IsRequired();

            e.Property(x => x.Subject)
                .HasMaxLength(255)
                .IsRequired();

            e.Property(x => x.BodyRendered)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.SentAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAdd();

            e.Property(x => x.ErrorMessage)
                .HasColumnType("nvarchar(max)");

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAdd();

            e.Property(x => x.CreatedBy);

            e.HasIndex(x => x.SentAt)
                .HasDatabaseName("IX_tbl_EmailLogs_SentAt");

            e.HasIndex(x => new { x.Recipient, x.SentAt })
                .HasDatabaseName("IX_tbl_EmailLogs_Recipient_SentAt");

            e.HasMany(x => x.Attachments)
                .WithOne(a => a.EmailLog!)
                .HasForeignKey(a => a.EmailLogID)
                .HasConstraintName("FK_tbl_EmailLogAttachments_tbl_EmailLogs")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailLogAttachments
        m.Entity<EmailLogAttachment>(e =>
        {
            e.ToTable("tbl_EmailLogAttachments", HR);

            e.HasKey(x => x.EmailLogAttachmentID);

            e.Property(x => x.EmailLogAttachmentID)
                .HasColumnName("EmailLogAttachmentID")
                .ValueGeneratedOnAdd();

            e.Property(x => x.EmailLogID)
                .IsRequired();

            e.Property(x => x.StoredFileGuid)
                .IsRequired();

            e.Property(x => x.FileName)
                .HasMaxLength(260);

            e.Property(x => x.ContentType)
                .HasMaxLength(100);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .ValueGeneratedOnAdd();

            e.Property(x => x.CreatedBy);

            e.HasIndex(x => new { x.EmailLogID, x.StoredFileGuid })
                .IsUnique()
                .HasDatabaseName("UX_tbl_EmailLogAttachments_EmailLogID_StoredFileGuid");

            e.HasIndex(x => x.EmailLogID)
                .HasDatabaseName("IX_tbl_EmailLogAttachments_EmailLogID");

            // FK a HR.TBL_StoredFile(FileGuid) (tu tabla existente)
            e.HasOne(a => a.StoredFile)
                .WithMany() // no necesitas navegación inversa
                .HasForeignKey(a => a.StoredFileGuid)
                .HasPrincipalKey(sf => sf.FileGuid)
                .HasConstraintName("FK_tbl_EmailLogAttachments_TBL_StoredFile_FileGuid")
                .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<VwEmployeeScheduleAtDate>().HasNoKey().ToView("vw_EmployeeScheduleAtDate", "HR");
        m.Entity<VwPunchDay>().HasNoKey().ToView("vw_PunchDay", "HR");
        m.Entity<VwLeaveWindows>().HasNoKey().ToView("vw_LeaveWindows", "HR");
        m.Entity<VwAttendanceDay>().HasNoKey().ToView("vw_AttendanceDay", "HR");

        // Vistas de Permisos y Menús (del sistema de autenticación)
        m.Entity<VwUserRole>().HasNoKey().ToView("vw_UserRoles", "dbo");
        m.Entity<VwRoleMenuItem>().HasNoKey().ToView("vw_RoleMenuItems", "dbo");
        m.Entity<TimeBalances>(e => {
            e.ToTable("tbl_TimeBalances", HR);
            e.HasKey(x => x.EmployeeID);
            e.Property(x => x.EmployeeID).HasColumnName("EmployeeID");
        });
        m.Entity<ContractStatusTransition>(e => {
            e.ToTable("tbl_contract_status_transitions", HR);
            e.HasKey(x => x.TransitionID);
            e.HasIndex(x => new { x.FromStatusTypeID, x.ToStatusTypeID }).IsUnique();
            });

        m.Entity<ContractStatusHistory>(e => {
            e.ToTable("tbl_contract_status_history", HR);
            e.HasKey(x => x.HistoryID);
            e.HasIndex(x => new { x.ContractID, x.ChangedAt }); 
        });

        /*DOCFLOW*/
        const string DOCFLOW = "docflow";

        m.Entity<WsUtaSystem.Models.Docflow.DocflowProcessHierarchy>(e =>
        {
            e.ToTable("tbl_ProcessHierarchy", DOCFLOW);
            e.HasKey(x => x.ProcessId);
            e.Property(x => x.ProcessCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ProcessName).HasMaxLength(200).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.ProcessCode).IsUnique();
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowProcessTransition>(e =>
        {
            e.ToTable("tbl_ProcessTransitions", DOCFLOW);
            e.HasKey(x => x.TransitionId);
            e.Property(x => x.IsDefault).HasDefaultValue(true);
            e.Property(x => x.AllowReturn).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.FromProcessId, x.ToProcessId }).IsUnique();
            e.HasIndex(x => new { x.FromProcessId, x.IsDefault });
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowDocumentRule>(e =>
        {
            e.ToTable("tbl_DocumentRules", DOCFLOW);
            e.HasKey(x => x.RuleId);
            e.Property(x => x.DocumentType).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsRequired).HasDefaultValue(true);
            e.Property(x => x.DefaultVisibility).HasDefaultValue((byte)1);
            e.Property(x => x.AllowVisibilityOverride).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.ProcessId);
            e.HasIndex(x => new { x.ProcessId, x.IsRequired });
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowWorkflowInstance>(e =>
        {
            e.ToTable("tbl_WorkflowInstances", DOCFLOW);
            e.HasKey(x => x.InstanceId);
            e.Property(x => x.CurrentStatus).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.CurrentDepartmentId, x.CurrentStatus, x.CreatedAt });
            e.HasIndex(x => x.ProcessId);
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowDocument>(e =>
        {
            e.ToTable("tbl_Documents", DOCFLOW);
            e.HasKey(x => x.DocumentId);
            e.Property(x => x.DocumentName).HasMaxLength(255).IsRequired();
            e.Property(x => x.Visibility).HasDefaultValue((byte)1);
            e.Property(x => x.CurrentVersion).HasDefaultValue(0);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => x.RuleId);
            e.HasIndex(x => new { x.InstanceId, x.Visibility, x.CreatedByDepartmentId })
                .HasFilter("[IsDeleted] = 0");
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowFileVersion>(e =>
        {
            e.ToTable("tbl_FileVersions", DOCFLOW);
            e.HasKey(x => x.VersionId);
            e.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        });

        m.Entity<WsUtaSystem.Models.Docflow.DocflowWorkflowMovement>(e =>
        {
            e.ToTable("tbl_WorkflowMovements", DOCFLOW);
            e.HasKey(x => x.MovementId);
            e.Property(x => x.MovementType).HasMaxLength(10).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.HasIndex(x => new { x.InstanceId, x.CreatedAt });
        });

    }
}
