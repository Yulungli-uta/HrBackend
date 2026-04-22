using Microsoft.EntityFrameworkCore;
using Models.Views;
using WsUtaSystem.Data.ModelConfigurations.HR;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Docflow;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Data;

/// <summary>
/// Contexto principal de Entity Framework Core para el sistema WsUta.
/// Las configuraciones de entidades están separadas en clases <see cref="IEntityTypeConfiguration{TEntity}"/>
/// organizadas por módulo en el directorio Data/ModelConfigurations/.
/// Esto sigue el principio de Responsabilidad Única (SRP) y facilita el mantenimiento.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─────────────────────────────────────────────────────────────────────────
    // Convenciones globales de tipos
    // ─────────────────────────────────────────────────────────────────────────

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
    {
        cfg.Properties<decimal>().HavePrecision(18, 2);
        cfg.Properties<DateTime>().HaveColumnType("datetime2");
        cfg.Properties<TimeSpan>().HaveColumnType("time(0)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Personas y Empleados
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<People> People => Set<People>();
    public DbSet<Employees> Employees => Set<Employees>();
    public DbSet<Faculties> Faculties => Set<Faculties>();
    public DbSet<Departments> Departments => Set<Departments>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<OccupationalGroup> OccupationalGroup => Set<OccupationalGroup>();
    public DbSet<Degree> Degree => Set<Degree>();
    public DbSet<DepartmentAuthority> DepartmentAuthorities => Set<DepartmentAuthority>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Contratos y Nómina
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<Contracts> Contracts => Set<Contracts>();
    public DbSet<SalaryHistory> SalaryHistory => Set<SalaryHistory>();
    public DbSet<ContractType> ContractType => Set<ContractType>();
    public DbSet<ContractRequest> ContractRequest => Set<ContractRequest>();
    public DbSet<FinancialCertification> FinancialCertification => Set<FinancialCertification>();
    public DbSet<ContractStatusTransition> ContractStatusTransitions => Set<ContractStatusTransition>();
    public DbSet<ContractStatusHistory> ContractStatusHistories => Set<ContractStatusHistory>();
    public DbSet<Payroll> Payroll => Set<Payroll>();
    public DbSet<PayrollLines> PayrollLines => Set<PayrollLines>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Asistencia y Tiempo
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<Schedules> Schedules => Set<Schedules>();
    public DbSet<EmployeeSchedules> EmployeeSchedules => Set<EmployeeSchedules>();
    public DbSet<AttendancePunches> AttendancePunches => Set<AttendancePunches>();
    public DbSet<PunchJustifications> PunchJustifications => Set<PunchJustifications>();
    public DbSet<AttendanceCalculations> AttendanceCalculations => Set<AttendanceCalculations>();
    public DbSet<OvertimeConfig> OvertimeConfig => Set<OvertimeConfig>();
    public DbSet<Overtime> Overtime => Set<Overtime>();
    public DbSet<TimeRecoveryPlans> TimeRecoveryPlans => Set<TimeRecoveryPlans>();
    public DbSet<TimeRecoveryLogs> TimeRecoveryLogs => Set<TimeRecoveryLogs>();
    public DbSet<TimePlanning> TimePlanning => Set<TimePlanning>();
    public DbSet<TimePlanningEmployee> TimePlanningEmployee => Set<TimePlanningEmployee>();
    public DbSet<TimePlanningExecution> TimePlanningExecution => Set<TimePlanningExecution>();
    public DbSet<TimeBalances> TimeBalances => Set<TimeBalances>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Activity> Activity => Set<Activity>();
    public DbSet<AdditionalActivity> AdditionalActivity => Set<AdditionalActivity>();
    public DbSet<JobActivity> JobActivity => Set<JobActivity>();
    public DbSet<ScheduleChangePlan> ScheduleChangePlan => Set<ScheduleChangePlan>();
    public DbSet<ScheduleChangePlanDetail> ScheduleChangePlanDetail => Set<ScheduleChangePlanDetail>();


    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Permisos y Movimientos
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<PermissionTypes> PermissionTypes => Set<PermissionTypes>();
    public DbSet<Vacations> Vacations => Set<Vacations>();
    public DbSet<Permissions> Permissions => Set<Permissions>();
    public DbSet<Subrogations> Subrogations => Set<Subrogations>();
    public DbSet<PersonnelMovements> PersonnelMovements => Set<PersonnelMovements>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Hoja de Vida
    // ─────────────────────────────────────────────────────────────────────────

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
    public DbSet<KnowledgeArea> KnowledgeAreas => Set<KnowledgeArea>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Referencia, Parámetros y Archivos
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<RefTypes> RefTypes => Set<RefTypes>();
    public DbSet<Countries> Countries => Set<Countries>();
    public DbSet<Provinces> Provinces => Set<Provinces>();
    public DbSet<Cantons> Cantons => Set<Cantons>();
    public DbSet<Audit> Audit => Set<Audit>();
    public DbSet<Parameters> Parameters => Set<Parameters>();
    public DbSet<DirectoryParameters> DirectoryParameters => Set<DirectoryParameters>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo HR: Email
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<EmailLayout> EmailLayouts => Set<EmailLayout>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailLogAttachment> EmailLogAttachments => Set<EmailLogAttachment>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Módulo Docflow
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<DocflowProcessHierarchy> DocflowProcesses => Set<DocflowProcessHierarchy>();
    public DbSet<DocflowProcessTransition> DocflowTransitions => Set<DocflowProcessTransition>();
    public DbSet<DocflowDocumentRule> DocflowDocumentRules => Set<DocflowDocumentRule>();
    public DbSet<DocflowWorkflowInstance> DocflowInstances => Set<DocflowWorkflowInstance>();
    public DbSet<DocflowDocument> DocflowDocuments => Set<DocflowDocument>();
    public DbSet<DocflowFileVersion> DocflowFileVersions => Set<DocflowFileVersion>();
    public DbSet<DocflowWorkflowMovement> DocflowMovements => Set<DocflowWorkflowMovement>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Motor Documental Institucional
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<DocumentTemplateField> DocumentTemplateFields => Set<DocumentTemplateField>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
    public DbSet<GeneratedDocumentField> GeneratedDocumentFields => Set<GeneratedDocumentField>();
    public DbSet<PersonnelAction> PersonnelActions => Set<PersonnelAction>();

    // ─────────────────────────────────────────────────────────────────────────
    // DbSets - Vistas (sin clave primaria)
    // ─────────────────────────────────────────────────────────────────────────

    public DbSet<VwEmployeeComplete> vwEmployeeComplete { get; set; }
    public DbSet<VwEmployeeDetails> vwEmployeeDetails { get; set; }
    public DbSet<VwEmployeeScheduleAtDate> VwEmployeeScheduleAtDate { get; set; }
    public DbSet<VwPunchDay> VwPunchDay { get; set; }
    public DbSet<VwLeaveWindows> VwLeaveWindows { get; set; }
    public DbSet<VwAttendanceDay> VwAttendanceDay { get; set; }
    public DbSet<VwUserRole> VwUserRoles { get; set; }
    public DbSet<VwRoleMenuItem> VwRoleMenuItems { get; set; }
    public DbSet<VwEmployeeCurrentSchedule> VwEmployeeCurrentSchedules => Set<VwEmployeeCurrentSchedule>();

    // ─────────────────────────────────────────────────────────────────────────
    // Configuración del modelo
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica todas las configuraciones de entidades definidas en clases
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> del ensamblado actual.
    /// EF Core descubre automáticamente todas las clases de configuración
    /// en el directorio Data/ModelConfigurations/.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
