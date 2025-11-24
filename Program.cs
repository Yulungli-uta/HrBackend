using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Services;
using WsUtaSystem.Filters;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Infrastructure.DependencyInjection;
using WsUtaSystem.Infrastructure.Filters;
using WsUtaSystem.Infrastructure.Jobs;
using WsUtaSystem.Infrastructure.Repositories;
using WsUtaSystem.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configurar el puerto 5000
//builder.WebHost.UseUrls("http://localhost:5000");

// ===== CORS (appsettings.json) =====
var cors = builder.Configuration.GetSection("Cors");
var corsName = cors["PolicyName"] ?? "Frontend";
var origins = cors.GetSection("Origins").Get<string[]>() ?? Array.Empty<string>();
var allowCred = bool.TryParse(cors["AllowCredentials"], out var ac) && ac;
var headers = cors.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "content-type", "authorization" };
var methods = cors.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };

builder.Services.AddCors(opt => opt.AddPolicy(corsName, p =>
{
    if (origins.Length > 0) p.WithOrigins(origins); else p.AllowAnyOrigin();
    p.WithHeaders(headers).WithMethods(methods);
    if (allowCred) p.AllowCredentials();
}));

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// ===== Controllers + JSON + Filtro de modelo =====
builder.Services.AddControllers(o => o.Filters.Add<ValidateModelFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.WriteIndented = true;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();

// ===== Configuración avanzada de Swagger =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WsUtaSystem API",
        Version = "v1",
        Description = "Sistema de Gestión de Recursos Humanos - UTA",
        Contact = new OpenApiContact
        {
            Name = "Equipo de Desarrollo",
            Email = "desarrollo@uta.edu.ec"
        }
    });

    // Configuración para manejar controladores base y herencia
    c.UseAllOfToExtendReferenceSchemas();
    c.UseAllOfForInheritance();
    c.UseOneOfForPolymorphism();

    // Resolver conflictos de acciones
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Personalizar OperationIds para evitar conflictos
    c.CustomOperationIds(apiDesc =>
    {
        return apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
            ? $"{methodInfo.DeclaringType?.Name}_{methodInfo.Name}"
            : null;
    });

    // Configurar tags para agrupamiento
    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName };
    });

    // AGREGAR ESTA LÍNEA: Filtro para mostrar el prefijo de ruta
    c.DocumentFilter<PathPrefixDocumentFilter>("/api/v1/rh");

    // Incluir comentarios XML
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    }
    catch
    {
        // Ignorar errores de XML comments
    }
});

// ===== AutoMapper & Validadores =====
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddValidatorsFromAssemblyContaining<WsUtaSystem.Application.Mapping.EntityToDtoProfile>();

// ===== DB =====
var cs = builder.Configuration.GetConnectionString("SqlServerConn")
        ?? builder.Configuration.GetConnectionString("Sql")
        ?? builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No se encontró cadena de conexión.");
if (!cs.Contains("Integrated Security", StringComparison.OrdinalIgnoreCase) &&
    !cs.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
    cs += ";Integrated Security=False;Authentication=SqlPassword;";

builder.Services.AddDbContext<WsUtaSystem.Data.AppDbContext>(o =>
    o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));

// ===== DI genérica =====
builder.Services.AddScoped(typeof(IRepository<,>), typeof(ServiceAwareEfRepository<,>));
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>));

// ===== DI por entidad =====
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAddressesRepository, WsUtaSystem.Infrastructure.Repositories.AddressesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAddressesService, WsUtaSystem.Application.Services.AddressesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAttendanceCalculationsRepository, WsUtaSystem.Infrastructure.Repositories.AttendanceCalculationsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAttendanceCalculationsService, WsUtaSystem.Application.Services.AttendanceCalculationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAttendancePunchesRepository, WsUtaSystem.Infrastructure.Repositories.AttendancePunchesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAttendancePunchesService, WsUtaSystem.Application.Services.AttendancePunchesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAuditRepository, WsUtaSystem.Infrastructure.Repositories.AuditRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAuditService, WsUtaSystem.Application.Services.AuditService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IBankAccountsRepository, WsUtaSystem.Infrastructure.Repositories.BankAccountsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IBankAccountsService, WsUtaSystem.Application.Services.BankAccountsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IBooksRepository, WsUtaSystem.Infrastructure.Repositories.BooksRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IBooksService, WsUtaSystem.Application.Services.BooksService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICantonsRepository, WsUtaSystem.Infrastructure.Repositories.CantonsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICantonsService, WsUtaSystem.Application.Services.CantonsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICatastrophicIllnessesRepository, WsUtaSystem.Infrastructure.Repositories.CatastrophicIllnessesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICatastrophicIllnessesService, WsUtaSystem.Application.Services.CatastrophicIllnessesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractsRepository, WsUtaSystem.Infrastructure.Repositories.ContractsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractsService, WsUtaSystem.Application.Services.ContractsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICountriesRepository, WsUtaSystem.Infrastructure.Repositories.CountriesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICountriesService, WsUtaSystem.Application.Services.CountriesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDepartmentsRepository, WsUtaSystem.Infrastructure.Repositories.DepartmentsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDepartmentsService, WsUtaSystem.Application.Services.DepartmentsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEducationLevelsRepository, WsUtaSystem.Infrastructure.Repositories.EducationLevelsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEducationLevelsService, WsUtaSystem.Application.Services.EducationLevelsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmergencyContactsRepository, WsUtaSystem.Infrastructure.Repositories.EmergencyContactsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmergencyContactsService, WsUtaSystem.Application.Services.EmergencyContactsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeSchedulesRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeSchedulesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeSchedulesService, WsUtaSystem.Application.Services.EmployeeSchedulesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeesRepository, WsUtaSystem.Infrastructure.Repositories.EmployeesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeesService, WsUtaSystem.Application.Services.EmployeesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFacultiesRepository, WsUtaSystem.Infrastructure.Repositories.FacultiesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFacultiesService, WsUtaSystem.Application.Services.FacultiesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFamilyBurdenRepository, WsUtaSystem.Infrastructure.Repositories.FamilyBurdenRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFamilyBurdenService, WsUtaSystem.Application.Services.FamilyBurdenService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IInstitutionsRepository, WsUtaSystem.Infrastructure.Repositories.InstitutionsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IInstitutionsService, WsUtaSystem.Application.Services.InstitutionsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOvertimeRepository, WsUtaSystem.Infrastructure.Repositories.OvertimeRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOvertimeService, WsUtaSystem.Application.Services.OvertimeService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOvertimeConfigRepository, WsUtaSystem.Infrastructure.Repositories.OvertimeConfigRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOvertimeConfigService, WsUtaSystem.Application.Services.OvertimeConfigService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPayrollRepository, WsUtaSystem.Infrastructure.Repositories.PayrollRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPayrollService, WsUtaSystem.Application.Services.PayrollService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPayrollLinesRepository, WsUtaSystem.Infrastructure.Repositories.PayrollLinesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPayrollLinesService, WsUtaSystem.Application.Services.PayrollLinesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPeopleRepository, WsUtaSystem.Infrastructure.Repositories.PeopleRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPeopleService, WsUtaSystem.Application.Services.PeopleService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPermissionTypesRepository, WsUtaSystem.Infrastructure.Repositories.PermissionTypesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPermissionTypesService, WsUtaSystem.Application.Services.PermissionTypesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPermissionsRepository, WsUtaSystem.Infrastructure.Repositories.PermissionsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPermissionsService, WsUtaSystem.Application.Services.PermissionsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPersonnelMovementsRepository, WsUtaSystem.Infrastructure.Repositories.PersonnelMovementsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPersonnelMovementsService, WsUtaSystem.Application.Services.PersonnelMovementsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IProvincesRepository, WsUtaSystem.Infrastructure.Repositories.ProvincesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IProvincesService, WsUtaSystem.Application.Services.ProvincesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPublicationsRepository, WsUtaSystem.Infrastructure.Repositories.PublicationsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPublicationsService, WsUtaSystem.Application.Services.PublicationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPunchJustificationsRepository, WsUtaSystem.Infrastructure.Repositories.PunchJustificationsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPunchJustificationsService, WsUtaSystem.Application.Services.PunchJustificationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJustificationsService, WsUtaSystem.Application.Services.JustificationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IRefTypesRepository, WsUtaSystem.Infrastructure.Repositories.RefTypesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IRefTypesService, WsUtaSystem.Application.Services.RefTypesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISalaryHistoryRepository, WsUtaSystem.Infrastructure.Repositories.SalaryHistoryRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISalaryHistoryService, WsUtaSystem.Application.Services.SalaryHistoryService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISchedulesRepository, WsUtaSystem.Infrastructure.Repositories.SchedulesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISchedulesService, WsUtaSystem.Application.Services.SchedulesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISubrogationsRepository, WsUtaSystem.Infrastructure.Repositories.SubrogationsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISubrogationsService, WsUtaSystem.Application.Services.SubrogationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimeRecoveryLogsRepository, WsUtaSystem.Infrastructure.Repositories.TimeRecoveryLogsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeRecoveryLogsService, WsUtaSystem.Application.Services.TimeRecoveryLogsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimeRecoveryPlansRepository, WsUtaSystem.Infrastructure.Repositories.TimeRecoveryPlansRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeRecoveryPlansService, WsUtaSystem.Application.Services.TimeRecoveryPlansService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITrainingsRepository, WsUtaSystem.Infrastructure.Repositories.TrainingsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITrainingsService, WsUtaSystem.Application.Services.TrainingsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVacationsRepository, WsUtaSystem.Infrastructure.Repositories.VacationsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVacationsService, WsUtaSystem.Application.Services.VacationsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IWorkExperiencesRepository, WsUtaSystem.Infrastructure.Repositories.WorkExperiencesRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IWorkExperiencesService, WsUtaSystem.Application.Services.WorkExperiencesService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IvwEmployeeCompleteRepository, WsUtaSystem.Infrastructure.Repositories.VwEmployeeCompleteRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IvwEmployeeCompleteService, WsUtaSystem.Application.Services.VwEmployeeCompleteService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IJobRepository, WsUtaSystem.Infrastructure.Repositories.JobRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJobService, WsUtaSystem.Application.Services.JobService>();
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IvwEmployeeDetailsRepository, WsUtaSystem.Infrastructure.Repositories.VwEmployeeDetailsRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IvwEmployeeDetailsService, WsUtaSystem.Application.Services.VwEmployeeDetailsService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IHolidayRepository, WsUtaSystem.Infrastructure.Repositories.HolidayRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningEmployeeRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningEmployeeRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningExecutionRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningExecutionRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IHolidayService, WsUtaSystem.Application.Services.HolidayService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningService, WsUtaSystem.Application.Services.TimePlanningService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningEmployeeService, WsUtaSystem.Application.Services.TimePlanningEmployeeService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningExecutionService, WsUtaSystem.Application.Services.TimePlanningExecutionService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IActivityService, WsUtaSystem.Application.Services.ActivityService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAdditionalActivityService, WsUtaSystem.Application.Services.AdditionalActivityService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractTypeService, WsUtaSystem.Application.Services.ContractTypeService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDegreeService, WsUtaSystem.Application.Services.DegreeService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOccupationalGroupService, WsUtaSystem.Application.Services.OccupationalGroupService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJobActivityService, WsUtaSystem.Application.Services.JobActivityService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IActivityRepository, WsUtaSystem.Infrastructure.Repositories.ActivityRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAdditionalActivityRepository, WsUtaSystem.Infrastructure.Repositories.AdditionalActivityRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractTypeRepository, WsUtaSystem.Infrastructure.Repositories.ContractTypeRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDegreeRepository, WsUtaSystem.Infrastructure.Repositories.DegreeRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOccupationalGroupRepository, WsUtaSystem.Infrastructure.Repositories.OccupationalGroupRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IJobActivityRepository, WsUtaSystem.Infrastructure.Repositories.JobActivityRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractRequestService, WsUtaSystem.Application.Services.ContractRequestService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFinancialCertificationService, WsUtaSystem.Application.Services.FinancialCertificationService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractRequestRepository, WsUtaSystem.Infrastructure.Repositories.ContractRequestRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFinancialCertificationRepository, WsUtaSystem.Infrastructure.Repositories.FinancialCertificationRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IParametersRepository, WsUtaSystem.Infrastructure.Repositories.ParametersRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDirectoryParametersRepository, WsUtaSystem.Infrastructure.Repositories.DirectoryParametersRepository>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IParametersService, WsUtaSystem.Application.Services.ParametersService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDirectoryParametersService, WsUtaSystem.Application.Services.DirectoryParametersService>();
builder.Services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFileManagementService, WsUtaSystem.Application.Services.FileManagementService>();
// Vistas
builder.Services.AddScoped<IVwEmployeeScheduleAtDateRepository, VwEmployeeScheduleAtDateRepository>();
builder.Services.AddScoped<IVwEmployeeScheduleAtDateService, VwEmployeeScheduleAtDateService>();
builder.Services.AddScoped<IVwPunchDayRepository, VwPunchDayRepository>();
builder.Services.AddScoped<IVwPunchDayService, VwPunchDayService>();
builder.Services.AddScoped<IVwLeaveWindowsRepository, VwLeaveWindowsRepository>();
builder.Services.AddScoped<IVwLeaveWindowsService, VwLeaveWindowsService>();
builder.Services.AddScoped<IVwAttendanceDayRepository, VwAttendanceDayRepository>();
builder.Services.AddScoped<IVwAttendanceDayService, VwAttendanceDayService>();

// Procedimientos Almacenados
builder.Services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
builder.Services.AddScoped<IJustificationsService, JustificationsService>();
builder.Services.AddScoped<IRecoveryService, RecoveryService>();
builder.Services.AddScoped<IOvertimePriceService, OvertimePriceService>();
builder.Services.AddScoped<IPayrollDiscountsService, PayrollDiscountsService>();
builder.Services.AddScoped<IPayrollSubsidiesService, PayrollSubsidiesService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// Agregar Quartz.NET Jobs
//DailyAttendanceCalculationJob 2:00 AM Calcula asistencia del día anterior HR.sp_Attendance_CalculateRange
//DailyNightMinutesCalculationJob 3:00 AM  Calcula minutos nocturnos del día anterior HR.sp_Attendance_CalcNightMinutes
//DailyJustificationsJob 4:00 AM Aplica justificaciones aprobadas HR.sp_Justifications_Apply 
//DailyRecoveryJob 5:00 AM Aplica recuperaciones de tiempo HR.sp_Recovery_Apply

builder.Services.AddQuartzJobs();

// ===== Configuración de Autenticación JWT =====
builder.Services.AddMemoryCache(); // Para caching de tokens validados
builder.Services.AddHttpClient(); // Para llamadas HTTP al servicio de autenticación
builder.Services.AddScoped<WsUtaSystem.Infrastructure.Services.ITokenValidationService, WsUtaSystem.Infrastructure.Services.TokenValidationService>();

var app = builder.Build();

app.UseCors(corsName);
app.UseMiddleware<WsUtaSystem.Middleware.JwtAuthenticationMiddleware>(); // Validación JWT
app.UseMiddleware<ErrorHandlingMiddleware>();

// Agrupar todos los controladores bajo el prefijo "api/v1/rh/"
var apiGroup = app.MapGroup("api/v1/rh");
apiGroup.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WsUtaSystem RH API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "WsUtaSystem RH API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.EnableDeepLinking();
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));
app.Run();