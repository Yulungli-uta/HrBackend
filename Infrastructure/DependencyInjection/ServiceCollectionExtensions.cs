using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Email;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Infrastructure.Academic;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Academic;
using WsUtaSystem.Application.Services;
using WsUtaSystem.Application.Services.Academic;
using WsUtaSystem.Filters;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Email;
using WsUtaSystem.Infrastructure.Filters;
using WsUtaSystem.Infrastructure.Interceptors;
using WsUtaSystem.Infrastructure.Jobs;
using WsUtaSystem.Infrastructure.Repositories;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Engine;
using WsUtaSystem.Reports.Renderers;

namespace WsUtaSystem.Infrastructure.DependencyInjection;

/// <summary>
/// Clase estática que centraliza todos los métodos de extensión para el registro
/// de servicios en el contenedor de inyección de dependencias (DI).
///
/// Cada método agrupa servicios por dominio funcional, permitiendo que Program.cs
/// quede limpio y solo orqueste las llamadas de alto nivel.
///
/// Convención: cada método retorna IServiceCollection para permitir encadenamiento fluido.
/// </summary>
public static class ServiceCollectionExtensions
{
    // =========================================================
    // CORS
    // Configura la política de CORS leyendo los valores desde appsettings.json.
    // Soporta orígenes, headers, métodos y credenciales configurables.
    // =========================================================

    /// <summary>
    /// Registra y configura la política CORS desde la sección "Cors" de appsettings.json.
    /// Retorna el nombre de la política para ser usada en el pipeline HTTP.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="policyName">Nombre resultante de la política CORS (out).</param>
    public static string AddCorsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cors = configuration.GetSection("Cors");
        var policy = cors["PolicyName"] ?? "Frontend";
        var origins = cors.GetSection("Origins").Get<string[]>() ?? Array.Empty<string>();
        var allowCred = bool.TryParse(cors["AllowCredentials"], out var ac) && ac;
        var headers = cors.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "content-type", "authorization" };
        var methods = cors.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };

        // Seguridad: no permitir AllowAnyOrigin en ningún entorno.
        // Si no hay orígenes configurados, el sistema no debe arrancar.
        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                "[CORS] No se han configurado orígenes permitidos en la sección 'Cors:Origins' de appsettings.json. " +
                "Agregue al menos un origen válido (ej: http://localhost:5173) para garantizar la seguridad de la API.");
        }

        services.AddCors(opt =>
        {
            opt.AddPolicy(policy, p =>
            {
                p.WithOrigins(origins)
                 .WithHeaders(headers)
                 .WithMethods(methods);

                // AllowCredentials solo es válido con orígenes concretos (nunca con AllowAnyOrigin)
                if (allowCred)
                    p.AllowCredentials();
            });
        });

        return policy;
    }

    // =========================================================
    // SERILOG
    // Configura el proveedor de logging estructurado con Serilog.
    // Escribe a consola y a archivo rotativo diario.
    // Suprime logs de ruido de Microsoft y EF Core (nivel Warning).
    // =========================================================

    /// <summary>
    /// Configura Serilog como proveedor de logging principal.
    /// Limpia los proveedores por defecto de .NET y registra Serilog en el host.
    /// </summary>
    /// <param name="builder">WebApplicationBuilder para acceder a Logging y Host.</param>
    public static WebApplicationBuilder AddSerilogConfiguration(this WebApplicationBuilder builder)
    {
        // Elimina los proveedores de logging nativos (Console, Debug, EventSource, etc.)
        builder.Logging.ClearProviders();

        // Ruta del archivo de log: absoluta en Production (appsettings.Production.json),
        // relativa al working directory en el resto de entornos.
        var logFilePath = builder.Configuration["Serilog:FilePath"] ?? "logs/log-.txt";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            // Reducir ruido de logs internos de ASP.NET Core y EF Core
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            // Salida a consola para observabilidad en tiempo real
            .WriteTo.Console()
            // Archivo rotativo diario
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Integra Serilog con el host de ASP.NET Core
        builder.Host.UseSerilog();

        return builder;
    }

    // =========================================================
    // CONTROLLERS + JSON
    // Configura los controladores MVC con filtro de validación de modelo
    // y opciones de serialización JSON estandarizadas (camelCase, sin ciclos).
    // =========================================================

    /// <summary>
    /// Registra los controladores con filtro de validación automática de modelos
    /// y configura las opciones de serialización JSON.
    /// </summary>
    public static IServiceCollection AddControllersConfiguration(this IServiceCollection services)
    {
        services
            .AddControllers(options =>
            {
                // Aplica validación de ModelState en todos los endpoints automáticamente
                options.Filters.Add<ValidateModelFilter>();

                options.Conventions.Add(new ApiPrefixByNamespaceConvention("api/v1", new Dictionary<string, string>
                {
                    { "WsUtaSystem.Controllers.HR", "rh" },
                    { "WsUtaSystem.Controllers.Docflow", "docflow" },
                    { "WsUtaSystem.Controllers.Documents", "documents" }
                }));
            })
            .AddJsonOptions(options =>
            {
                // Serialización de propiedades en camelCase (convención REST)
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                // JSON indentado para legibilidad en Swagger y depuración
                options.JsonSerializerOptions.WriteIndented = true;
                // Evita errores en grafos de objetos con referencias circulares
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        return services;
    }

    // =========================================================
    // SWAGGER
    // Configura la documentación OpenAPI con soporte para polimorfismo,
    // operationIds personalizados, prefijo de ruta y comentarios XML.
    // =========================================================

    /// <summary>
    /// Registra y configura SwaggerGen con metadatos del proyecto,
    /// soporte para herencia/polimorfismo y comentarios XML opcionales.
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
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

            // Esto soluciona el problema de los TimeSpan
            c.MapType<TimeSpan>(() => new OpenApiSchema
            {
                Type = "string",
                Example = new OpenApiString("00:00:00")
            });

            // Botón "Authorize" en la UI de Swagger: pega el token JWT (sin el prefijo
            // "Bearer ") y todas las peticiones "Try it out" lo incluyen automáticamente
            // en el header Authorization.
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Pega el token JWT (sin el prefijo 'Bearer '): eyJhbGciOi..."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            // Soporte para herencia y tipos polimórficos en el esquema OpenAPI
            c.UseAllOfToExtendReferenceSchemas();
            c.UseAllOfForInheritance();
            c.UseOneOfForPolymorphism();

            // En caso de rutas duplicadas, toma la primera definición encontrada
            c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            // Evita colisión de schemaIds para tipos internos del sistema con nombres duplicados
            // causada por TIME_ZONE_INFORMATION y TIME_DYNAMIC_ZONE_INFORMATION en .NET 9
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "_") ?? type.Name);

            // OperationId único por controlador + método para generación de clientes
            c.CustomOperationIds(apiDesc =>
                apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
                    ? $"{methodInfo.DeclaringType?.Name}_{methodInfo.Name}"
                    : null);

            // Agrupa endpoints por nombre de controlador en la UI de Swagger
            c.TagActionsBy(api =>
            {
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName };
            });

            // Prefija todas las rutas con /api/v1/rh en la documentación generada
            //c.DocumentFilter<PathPrefixDocumentFilter>("/api/v1/rh");

            // Incluye comentarios XML si el archivo existe (generado por el compilador)
            try
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
            catch
            {
                // Si el XML no existe o no es accesible, Swagger sigue funcionando sin comentarios
            }
        });

        return services;
    }

    // =========================================================
    // AUTOMAPPER + FLUENT VALIDATION
    // Registra AutoMapper escaneando todos los ensamblados cargados
    // y los validadores de FluentValidation desde el ensamblado de Application.
    // =========================================================

    /// <summary>
    /// Registra AutoMapper (escaneo de todos los perfiles en todos los ensamblados)
    /// y los validadores de FluentValidation del ensamblado de Application.
    /// </summary>
    public static IServiceCollection AddMappingAndValidation(this IServiceCollection services)
    {
        // Escanea y registra todos los perfiles AutoMapper disponibles
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Registra todos los IValidator<T> encontrados en el ensamblado que contiene EntityToDtoProfile
        services.AddValidatorsFromAssemblyContaining<WsUtaSystem.Application.Mapping.EntityToDtoProfile>();

        return services;
    }

    // =========================================================
    // SERVICIOS DE REPORTES
    // Registra la configuración tipada de reportes y los repositorios/servicios
    // necesarios para la generación de reportes y su auditoría.
    // =========================================================

    /// <summary>
    /// Registra los servicios de generación de reportes y auditoría de reportes.
    /// La configuración se lee desde la sección "Reports" de appsettings.json.
    /// </summary>
    public static IServiceCollection AddReportServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración tipada de reportes como Singleton (no cambia en runtime)
        services.AddSingleton(sp =>
        {
            var config = new WsUtaSystem.Application.Services.Reports.Configuration.ReportConfiguration();
            configuration.GetSection("Reports").Bind(config);
            return config;
        });

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Reports.IReportRepository,
            WsUtaSystem.Infrastructure.Repositories.Reports.ReportRepository>();

        services.AddScoped<WsUtaSystem.Infrastructure.Repositories.Reports.ReportAuditRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Reports.IReportAuditService,
            WsUtaSystem.Application.Services.Reports.ReportAuditService>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Reports.IReportService,
            WsUtaSystem.Application.Services.Reports.ReportService>();

        // ── Reportes v2: arquitectura genérica (OCP / DIP) ────────────────────────
        // IReportSource: cada implementación sabe cómo construir un ReportDefinition.
        // IReportRenderer: cada implementación sabe cómo renderizar en un formato.
        // IReportServiceV2: orquestador que une source + renderer.
        // Para agregar un nuevo reporte: solo registrar un nuevo IReportSource aquí.

        // ── Migración v1 → v2 ────────────────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.EmployeesReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.AttendanceReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.DepartmentsReportSource>();

        // ── Existentes ───────────────────────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.AttendanceSummaryReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.EmployeesByDepartmentReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.DepartmentContractSummaryReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.ScheduleContractSummaryReportSource>();

        // ── Nuevos reportes de gestión RH ────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.ContractsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.ActiveContractsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.PersonnelActionsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.ActivePersonnelActionsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.EmployeeHistoryReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.GrantedPermissionsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.ContractRequestsReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.CertificationsReportSource>();

        // ── Reportes v2: AttendanceCalculations (Atrasos, Horas Extras, Cruzado) ──────────
        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.IAttendanceCalculationsReportRepository,
            WsUtaSystem.Infrastructure.Repositories.AttendanceCalculationsReportRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.IAttendanceCalculationsReportService,
            WsUtaSystem.Application.Services.AttendanceCalculationsReportService>();

        // ── Reportes v2: Dependencias/Departamentos ───────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.IDepartmentsReportRepository,
            WsUtaSystem.Infrastructure.Repositories.DepartmentsReportRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.IDepartmentsReportService,
            WsUtaSystem.Application.Services.DepartmentsReportService>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.LatenessReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.OvertimeReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.AttendanceCrossReportSource>();

        // ── Reportes módulo Guardias ──────────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.Guards.GuardShiftPlanningReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.Guards.GuardLocationCoverageReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.Guards.GuardShiftChangesReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.Guards.GuardGroupRosterReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.Guards.GuardScheduleMatrixReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.SiiesFuncionariosReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.SiiesProfesoresReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportSource,
            WsUtaSystem.Reports.Sources.SiiesFormacionProfesionalReportSource>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportRenderer,
            WsUtaSystem.Reports.Renderers.PdfReportRenderer>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportRenderer,
            WsUtaSystem.Reports.Renderers.ExcelReportRenderer>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportRenderer,
            WsUtaSystem.Reports.Renderers.CsvReportRenderer>();

        services.AddScoped<
            WsUtaSystem.Reports.Abstractions.IReportServiceV2,
            WsUtaSystem.Reports.Services.ReportServiceV2>();

        services.AddScoped<HtmlDocumentRenderer>();

        return services;
    }

    // =========================================================
    // PERMISOS DE USUARIO
    // Repositorio y servicio para gestión de permisos por usuario.
    // =========================================================

    /// <summary>
    /// Registra el repositorio y servicio de permisos de usuario.
    /// </summary>
    public static IServiceCollection AddUserPermissionServices(this IServiceCollection services)
    {
        services.AddScoped<
            global::Application.Interfaces.Repositories.IUserPermissionRepository,
            global::Infrastructure.Repositories.UserPermissionRepository>();

        services.AddScoped<
            global::Application.Interfaces.Services.IUserPermissionService,
            global::Application.Services.UserPermissionService>();

        return services;
    }

    // =========================================================
    // REPOSITORIOS Y SERVICIOS GENÉRICOS
    // Registro genérico open-type para IRepository<,> e IService<,>
    // que permite resolver cualquier entidad sin registro explícito.
    // =========================================================

    /// <summary>
    /// Registra los tipos genéricos abiertos para repositorio y servicio base.
    /// Permite resolver IRepository&lt;TEntity, TKey&gt; e IService&lt;TEntity, TKey&gt;
    /// automáticamente para cualquier entidad.
    /// </summary>
    public static IServiceCollection AddGenericRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(ServiceAwareEfRepository<,>));
        services.AddScoped(typeof(IService<,>), typeof(Service<,>));

        return services;
    }

    // =========================================================
    // SERVICIOS POR ENTIDAD (DOMINIO RH)
    // Registro explícito de todos los repositorios y servicios del dominio
    // de Recursos Humanos. Se agrupan lógicamente por módulo.
    // =========================================================

    /// <summary>
    /// Registra todos los repositorios y servicios del dominio de Recursos Humanos.
    /// Se listan en orden alfabético por entidad para facilitar su mantenimiento.
    /// </summary>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // ── Módulo: Direcciones ──────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAddressesRepository, WsUtaSystem.Infrastructure.Repositories.AddressesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAddressesService, WsUtaSystem.Application.Services.AddressesService>();

        // ── Módulo: Asistencia y Marcaciones ─────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAttendanceCalculationsRepository, WsUtaSystem.Infrastructure.Repositories.AttendanceCalculationsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAttendanceCalculationsService, WsUtaSystem.Application.Services.AttendanceCalculationsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAttendancePunchesRepository, WsUtaSystem.Infrastructure.Repositories.AttendancePunchesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAttendancePunchesService, WsUtaSystem.Application.Services.AttendancePunchesService>();

        // ── Módulo: Auditoría ─────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAuditRepository, WsUtaSystem.Infrastructure.Repositories.AuditRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAuditService, WsUtaSystem.Application.Services.AuditService>();

        // ── Módulo: Cuentas Bancarias ─────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IBankAccountsRepository, WsUtaSystem.Infrastructure.Repositories.BankAccountsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IBankAccountsService, WsUtaSystem.Application.Services.BankAccountsService>();

        // ── Módulo: Libros / Publicaciones ────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IBooksRepository, WsUtaSystem.Infrastructure.Repositories.BooksRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IBooksService, WsUtaSystem.Application.Services.BooksService>();

        // ── Módulo: Ubicación Geográfica ──────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICantonsRepository, WsUtaSystem.Infrastructure.Repositories.CantonsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICantonsService, WsUtaSystem.Application.Services.CantonsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICountriesRepository, WsUtaSystem.Infrastructure.Repositories.CountriesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICountriesService, WsUtaSystem.Application.Services.CountriesService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IProvincesRepository, WsUtaSystem.Infrastructure.Repositories.ProvincesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IProvincesService, WsUtaSystem.Application.Services.ProvincesService>();

        // ── Módulo: Enfermedades Catastróficas ────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ICatastrophicIllnessesRepository, WsUtaSystem.Infrastructure.Repositories.CatastrophicIllnessesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ICatastrophicIllnessesService, WsUtaSystem.Application.Services.CatastrophicIllnessesService>();

        // ── Módulo: Contratos ─────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractsRepository, WsUtaSystem.Infrastructure.Repositories.ContractsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractsService, WsUtaSystem.Application.Services.ContractsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractExpirationService, WsUtaSystem.Application.Services.ContractExpirationService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractTypeRepository, WsUtaSystem.Infrastructure.Repositories.ContractTypeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractTypeService, WsUtaSystem.Application.Services.ContractTypeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractRequestRepository, WsUtaSystem.Infrastructure.Repositories.ContractRequestRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractRequestService, WsUtaSystem.Application.Services.ContractRequestService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPersonnelActionTypeRepository, WsUtaSystem.Infrastructure.Repositories.PersonnelActionTypeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPersonnelActionTypeService, WsUtaSystem.Application.Services.PersonnelActionTypeService>();        // ── Módulo: Departamentos / Facultades ────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDepartmentsRepository, WsUtaSystem.Infrastructure.Repositories.DepartmentsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDepartmentsService, WsUtaSystem.Application.Services.DepartmentsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFacultiesRepository, WsUtaSystem.Infrastructure.Repositories.FacultiesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFacultiesService, WsUtaSystem.Application.Services.FacultiesService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDepartmentAuthorityRepository, WsUtaSystem.Infrastructure.Repositories.DepartmentAuthorityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDepartmentAuthorityService, WsUtaSystem.Application.Services.DepartmentAuthorityService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITramiteRequirementsRepository, WsUtaSystem.Infrastructure.Repositories.TramiteRequirementsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITramiteRequirementsService, WsUtaSystem.Application.Services.TramiteRequirementsService>();
        // ── Módulo: Educación ─────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEducationLevelsRepository, WsUtaSystem.Infrastructure.Repositories.EducationLevelsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEducationLevelsService, WsUtaSystem.Application.Services.EducationLevelsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDegreeRepository, WsUtaSystem.Infrastructure.Repositories.DegreeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDegreeService, WsUtaSystem.Application.Services.DegreeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IKnowledgeAreaRepository, WsUtaSystem.Infrastructure.Repositories.KnowledgeAreaRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IKnowledgeAreaService, WsUtaSystem.Application.Services.KnowledgeAreaService>();

        // ── Módulo: Contactos de Emergencia ───────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmergencyContactsRepository, WsUtaSystem.Infrastructure.Repositories.EmergencyContactsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmergencyContactsService, WsUtaSystem.Application.Services.EmergencyContactsService>();

        // ── Módulo: Empleados ─────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeesRepository, WsUtaSystem.Infrastructure.Repositories.EmployeesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeesService, WsUtaSystem.Application.Services.EmployeesService>();

        // Orquestador de aprovisionamiento — centraliza el flujo post-firma de documento
        // (EnsureEmployee → RepositoryUta → UpdateEmail → SendEmail)
        // Usado por: ContractsService, PersonnelActionService, ContractProvisioningController
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeProvisioningOrchestrator, WsUtaSystem.Application.Services.EmployeeProvisioningOrchestrator>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeSchedulesRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeSchedulesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeSchedulesService, WsUtaSystem.Application.Services.EmployeeSchedulesService>();

        // ── Módulo: Cargas Familiares ─────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFamilyBurdenRepository, WsUtaSystem.Infrastructure.Repositories.FamilyBurdenRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFamilyBurdenService, WsUtaSystem.Application.Services.FamilyBurdenService>();

        // ── Módulo: Certificación Financiera ──────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IFinancialCertificationRepository, WsUtaSystem.Infrastructure.Repositories.FinancialCertificationRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFinancialCertificationService, WsUtaSystem.Application.Services.FinancialCertificationService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IContractRequestPersonRepository, WsUtaSystem.Infrastructure.Repositories.ContractRequestPersonRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IContractRequestPersonService, WsUtaSystem.Application.Services.ContractRequestPersonService>();

        // ── Módulo: Feriados ──────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IHolidayRepository, WsUtaSystem.Infrastructure.Repositories.HolidayRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IHolidayService, WsUtaSystem.Application.Services.HolidayService>();

        // ── Módulo: Saldos de Horas ───────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IHrBalanceRepository, WsUtaSystem.Infrastructure.Repositories.HrBalanceRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IHrBalanceService, WsUtaSystem.Application.Services.HrBalanceService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVacationBalanceAdjustmentService, WsUtaSystem.Application.Services.VacationBalanceAdjustmentService>();

        // ── Módulo: Instituciones ─────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IInstitutionsRepository, WsUtaSystem.Infrastructure.Repositories.InstitutionsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IInstitutionsService, WsUtaSystem.Application.Services.InstitutionsService>();

        // ── Módulo: Cargos / Puestos ──────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IJobRepository, WsUtaSystem.Infrastructure.Repositories.JobRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJobService, WsUtaSystem.Application.Services.JobService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IJobActivityRepository, WsUtaSystem.Infrastructure.Repositories.JobActivityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJobActivityService, WsUtaSystem.Application.Services.JobActivityService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOccupationalGroupRepository, WsUtaSystem.Infrastructure.Repositories.OccupationalGroupRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOccupationalGroupService, WsUtaSystem.Application.Services.OccupationalGroupService>();

        // ── Módulo: Vistas de solo lectura ───────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVwDepartmentWithTypeRepository, WsUtaSystem.Infrastructure.Repositories.VwDepartmentWithTypeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVwDepartmentWithTypeService, WsUtaSystem.Application.Services.VwDepartmentWithTypeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVwJobWithDegreeAndGroupRepository, WsUtaSystem.Infrastructure.Repositories.VwJobWithDegreeAndGroupRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVwJobWithDegreeAndGroupService, WsUtaSystem.Application.Services.VwJobWithDegreeAndGroupService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVwJobActivityRepository, WsUtaSystem.Infrastructure.Repositories.VwJobActivityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVwJobActivityService, WsUtaSystem.Application.Services.VwJobActivityService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVwAuthorityRepository, WsUtaSystem.Infrastructure.Repositories.VwAuthorityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVwAuthorityService, WsUtaSystem.Application.Services.VwAuthorityService>();

        // ── Módulo: Actividades ───────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IActivityRepository, WsUtaSystem.Infrastructure.Repositories.ActivityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IActivityService, WsUtaSystem.Application.Services.ActivityService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAdditionalActivityRepository, WsUtaSystem.Infrastructure.Repositories.AdditionalActivityRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAdditionalActivityService, WsUtaSystem.Application.Services.AdditionalActivityService>();

        // ── Módulo: Horas Extra ───────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOvertimeRepository, WsUtaSystem.Infrastructure.Repositories.OvertimeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOvertimeService, WsUtaSystem.Application.Services.OvertimeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IOvertimeConfigRepository, WsUtaSystem.Infrastructure.Repositories.OvertimeConfigRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IOvertimeConfigService, WsUtaSystem.Application.Services.OvertimeConfigService>();

        // ── Módulo: Parámetros del Sistema ────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IParametersRepository, WsUtaSystem.Infrastructure.Repositories.ParametersRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IParametersService, WsUtaSystem.Application.Services.ParametersService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IDirectoryParametersRepository, WsUtaSystem.Infrastructure.Repositories.DirectoryParametersRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDirectoryParametersService, WsUtaSystem.Application.Services.DirectoryParametersService>();

        // ── Módulo: Nómina ────────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPayrollRepository, WsUtaSystem.Infrastructure.Repositories.PayrollRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPayrollService, WsUtaSystem.Application.Services.PayrollService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPayrollLinesRepository, WsUtaSystem.Infrastructure.Repositories.PayrollLinesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPayrollLinesService, WsUtaSystem.Application.Services.PayrollLinesService>();

        // ── Módulo: Personas ──────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPeopleRepository, WsUtaSystem.Infrastructure.Repositories.PeopleRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPeopleService, WsUtaSystem.Application.Services.PeopleService>();

        // ── Módulo: Alcance de acceso por departamento (UserAccessScope) ────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IUserAccessScopeRepository, WsUtaSystem.Infrastructure.Repositories.UserAccessScopeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IUserAccessScopeService, WsUtaSystem.Application.Services.UserAccessScopeService>();
        services.AddScoped<WsUtaSystem.Application.Common.Interfaces.IRecordAccessGuard, WsUtaSystem.Application.Services.RecordAccessGuard>();

        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IResignationRetirementRepository, WsUtaSystem.Infrastructure.Repositories.ResignationRetirementRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IResignationRetirementService, WsUtaSystem.Application.Services.ResignationRetirementService>();

        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeCertificateRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeCertificateRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeCertificateService, WsUtaSystem.Application.Services.EmployeeCertificateService>();

        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeInternalRequestRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeInternalRequestRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeInternalRequestService, WsUtaSystem.Application.Services.EmployeeInternalRequestService>();

        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeSelfServiceService, WsUtaSystem.Application.Services.EmployeeSelfServiceService>();

        // ── Módulo: Régimen laboral por empleado (EmployeeLaborRegime) ──────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeLaborRegimeRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeLaborRegimeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeLaborRegimeService, WsUtaSystem.Application.Services.EmployeeLaborRegimeService>();

        // ── Módulo: Permisos ──────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPermissionTypesRepository, WsUtaSystem.Infrastructure.Repositories.PermissionTypesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPermissionTypesService, WsUtaSystem.Application.Services.PermissionTypesService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPermissionsRepository, WsUtaSystem.Infrastructure.Repositories.PermissionsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPermissionsService, WsUtaSystem.Application.Services.PermissionsService>();

        // ── Módulo: Movimientos de Personal ───────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPersonnelMovementsRepository, WsUtaSystem.Infrastructure.Repositories.PersonnelMovementsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPersonnelMovementsService, WsUtaSystem.Application.Services.PersonnelMovementsService>();

        // ── Módulo: Publicaciones ─────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPublicationsRepository, WsUtaSystem.Infrastructure.Repositories.PublicationsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPublicationsService, WsUtaSystem.Application.Services.PublicationsService>();

        // ── Módulo: Justificaciones de Marcación ──────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IPunchJustificationsRepository, WsUtaSystem.Infrastructure.Repositories.PunchJustificationsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IPunchJustificationsService, WsUtaSystem.Application.Services.PunchJustificationsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IJustificationsService, WsUtaSystem.Application.Services.JustificationsService>();

        // ── Módulo: Tipos de Referencia ───────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IRefTypesRepository, WsUtaSystem.Infrastructure.Repositories.RefTypesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IRefTypesService, WsUtaSystem.Application.Services.RefTypesService>();

        // ── Módulo: Historial Salarial ────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISalaryHistoryRepository, WsUtaSystem.Infrastructure.Repositories.SalaryHistoryRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISalaryHistoryService, WsUtaSystem.Application.Services.SalaryHistoryService>();

        // ── Módulo: Horarios ──────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISchedulesRepository, WsUtaSystem.Infrastructure.Repositories.SchedulesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISchedulesService, WsUtaSystem.Application.Services.SchedulesService>();

        // ── Módulo: Subrogaciones ─────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ISubrogationsRepository, WsUtaSystem.Infrastructure.Repositories.SubrogationsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ISubrogationsService, WsUtaSystem.Application.Services.SubrogationsService>();

        // ── Módulo: Recuperación de Tiempo ────────────────────────────────────
        // OBSOLETO/SIN USO REAL (verificado 2026-07-22) — se mantiene registrado solo para
        // que el DI no rompa mientras el controller siga presente. Ver TimeRecoveryPlansController/
        // TimeRecoveryLogsController para el detalle de por qué no se elimina todavía.
#pragma warning disable CS0618
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimeRecoveryLogsRepository, WsUtaSystem.Infrastructure.Repositories.TimeRecoveryLogsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeRecoveryLogsService, WsUtaSystem.Application.Services.TimeRecoveryLogsService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimeRecoveryPlansRepository, WsUtaSystem.Infrastructure.Repositories.TimeRecoveryPlansRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeRecoveryPlansService, WsUtaSystem.Application.Services.TimeRecoveryPlansService>();
#pragma warning restore CS0618

        // ── Módulo: Saldos de Tiempo ──────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimeBalancesRepository, WsUtaSystem.Infrastructure.Repositories.TimeBalancesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeBalancesService, WsUtaSystem.Application.Services.TimeBalancesService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimeService, WsUtaSystem.Application.Services.TimeService>();

        // ── Módulo: Planificación de Tiempo ───────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningEmployeeRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningEmployeeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITimePlanningExecutionRepository, WsUtaSystem.Infrastructure.Repositories.TimePlanningExecutionRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningService, WsUtaSystem.Application.Services.TimePlanningService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningEmployeeService, WsUtaSystem.Application.Services.TimePlanningEmployeeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITimePlanningExecutionService, WsUtaSystem.Application.Services.TimePlanningExecutionService>();

        // ── Módulo: Capacitaciones ────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITrainingsRepository, WsUtaSystem.Infrastructure.Repositories.TrainingsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITrainingsService, WsUtaSystem.Application.Services.TrainingsService>();

        // ── Módulo: Idiomas ───────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ILanguagesRepository, WsUtaSystem.Infrastructure.Repositories.LanguagesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ILanguagesService, WsUtaSystem.Application.Services.LanguagesService>();

        // ── Módulo: Vacaciones ────────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IVacationsRepository, WsUtaSystem.Infrastructure.Repositories.VacationsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IVacationsService, WsUtaSystem.Application.Services.VacationsService>();

        // ── Módulo: Escalafón y Estructura Docente ────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAcademicLadderRepository, WsUtaSystem.Infrastructure.Repositories.AcademicLadderRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAcademicLadderService, WsUtaSystem.Application.Services.AcademicLadderService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.ITeacherStructureRepository, WsUtaSystem.Infrastructure.Repositories.TeacherStructureRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.ITeacherStructureService, WsUtaSystem.Application.Services.TeacherStructureService>();

        // ── Módulo: Experiencia Laboral ───────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IWorkExperiencesRepository, WsUtaSystem.Infrastructure.Repositories.WorkExperiencesRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IWorkExperiencesService, WsUtaSystem.Application.Services.WorkExperiencesService>();

        // ── Módulo: Vistas (Views) ────────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IvwEmployeeCompleteRepository, WsUtaSystem.Infrastructure.Repositories.VwEmployeeCompleteRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IvwEmployeeCompleteService, WsUtaSystem.Application.Services.VwEmployeeCompleteService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IvwEmployeeDetailsRepository, WsUtaSystem.Infrastructure.Repositories.VwEmployeeDetailsRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IvwEmployeeDetailsService, WsUtaSystem.Application.Services.VwEmployeeDetailsService>();
        services.AddScoped<IVwEmployeeScheduleAtDateRepository, VwEmployeeScheduleAtDateRepository>();
        services.AddScoped<IVwEmployeeScheduleAtDateService, VwEmployeeScheduleAtDateService>();
        services.AddScoped<IVwPunchDayRepository, VwPunchDayRepository>();
        services.AddScoped<IVwPunchDayService, VwPunchDayService>();
        services.AddScoped<IVwLeaveWindowsRepository, VwLeaveWindowsRepository>();
        services.AddScoped<IVwLeaveWindowsService, VwLeaveWindowsService>();
        services.AddScoped<IVwAttendanceDayRepository, VwAttendanceDayRepository>();
        services.AddScoped<IVwAttendanceDayService, VwAttendanceDayService>();

        // ── Módulo: Archivos / Documentos ─────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IStoredFileRepository, WsUtaSystem.Infrastructure.Repositories.StoredFileRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IStoredFileService, WsUtaSystem.Application.Services.StoredFileService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IFileManagementService, WsUtaSystem.Application.Services.FileManagementService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IDocumentOrchestratorService, WsUtaSystem.Application.Services.DocumentOrchestratorService>();
        services.AddScoped<WsUtaSystem.Infrastructure.Common.TransactionalDocumentRepository>();

        // ── Módulo: Procedimientos Almacenados ────────────────────────────────
        services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
        services.AddScoped<IJustificationsService, JustificationsService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddScoped<IOvertimePriceService, OvertimePriceService>();
        services.AddScoped<IPayrollDiscountsService, PayrollDiscountsService>();
        services.AddScoped<IPayrollSubsidiesService, PayrollSubsidiesService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IJobExecutionLogService, WsUtaSystem.Infrastructure.Services.JobExecutionLogService>();

        // ── Módulo: Planificacion de cambio horario ────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IScheduleChangePlanRepository, WsUtaSystem.Infrastructure.Repositories.ScheduleChangePlanRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IScheduleChangePlanService, WsUtaSystem.Application.Services.ScheduleChangePlanService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IEmployeeCurrentScheduleRepository, WsUtaSystem.Infrastructure.Repositories.EmployeeCurrentScheduleRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IEmployeeCurrentScheduleService, WsUtaSystem.Application.Services.EmployeeCurrentScheduleService>();

        // ── Módulo: Guardias Rotativos ────────────────────────────────────────
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardServiceLocationRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardServiceLocationRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardRotationGroupRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardRotationGroupRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IRotationPatternRepository, WsUtaSystem.Infrastructure.Repositories.Guards.RotationPatternRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftCoverageRequirementRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardShiftCoverageRequirementRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftPlanningRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardShiftPlanningRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftChangeRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardShiftChangeRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IEmployeeAvailabilityBlockRepository, WsUtaSystem.Infrastructure.Repositories.Guards.EmployeeAvailabilityBlockRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardAssignmentValidationRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardAssignmentValidationRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardLocationRotationPeriodRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardLocationRotationPeriodRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardLocationRotationAssignmentRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardLocationRotationAssignmentRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardEmployeeSpecialRuleRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardEmployeeSpecialRuleRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardVacationPlanRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardVacationPlanRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardVacationRequestRepository, WsUtaSystem.Infrastructure.Repositories.Guards.GuardVacationRequestRepository>();

        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardServiceLocationService, WsUtaSystem.Application.Services.Guards.GuardServiceLocationService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardRotationGroupService, WsUtaSystem.Application.Services.Guards.GuardRotationGroupService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IRotationPatternService, WsUtaSystem.Application.Services.Guards.RotationPatternService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftCoverageRequirementService, WsUtaSystem.Application.Services.Guards.GuardShiftCoverageRequirementService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IEmployeeAvailabilityService, WsUtaSystem.Application.Services.Guards.EmployeeAvailabilityService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardAssignmentValidationService, WsUtaSystem.Application.Services.Guards.GuardAssignmentValidationService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftPlanningService, WsUtaSystem.Application.Services.Guards.GuardShiftPlanningService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardShiftChangeService, WsUtaSystem.Application.Services.Guards.GuardShiftChangeService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardLocationRotationService, WsUtaSystem.Application.Services.Guards.GuardLocationRotationService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardEmployeeSpecialRuleService, WsUtaSystem.Application.Services.Guards.GuardEmployeeSpecialRuleService>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Guards.IGuardVacationService, WsUtaSystem.Application.Services.Guards.GuardVacationService>();

        return services;
    }

    // =========================================================
    // ACADEMIC PROMOTION
    // Perfil académico docente (experiencia, publicaciones, capacitaciones,
    // investigación, tesis, idiomas, evaluación) con switch mock/BD.
    // =========================================================

    /// <summary>
    /// Registra el módulo de perfil académico docente (academic-promotion):
    /// opciones tipadas (AcademicPromotion en appsettings.json), repositorio
    /// especializado, servicio orquestador y proveedor de datos mock.
    /// </summary>
    public static IServiceCollection AddAcademicPromotionServices(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<WsUtaSystem.Application.Common.Options.AcademicPromotionOptions>(
            configuration.GetSection("AcademicPromotion"));

        services.AddScoped<WsUtaSystem.Application.Interfaces.Repositories.IAcademicPromotionRepository, WsUtaSystem.Infrastructure.Repositories.AcademicPromotionRepository>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAcademicPromotionMockProvider, WsUtaSystem.Application.Services.AcademicPromotionMockProvider>();
        services.AddScoped<WsUtaSystem.Application.Interfaces.Services.IAcademicPromotionService, WsUtaSystem.Application.Services.AcademicPromotionService>();

        return services;
    }

    // =========================================================
    // DOCFLOW
    // Repositorios y servicio del módulo de flujo documental.
    // =========================================================

    /// <summary>
    /// Registra los repositorios y el servicio del módulo Docflow
    /// para gestión de procesos, instancias, documentos y movimientos.
    /// </summary>
    public static IServiceCollection AddDocflowServices(this IServiceCollection services)
    {
        // Repositorios Docflow        
        services.AddScoped<IDocflowProcessRepository, DocflowProcessRepository>();
        services.AddScoped<IDocflowInstanceRepository, DocflowInstanceRepository>();
        services.AddScoped<IDocflowDocumentRepository, DocflowDocumentRepository>();
        services.AddScoped<IDocflowMovementRepository, DocflowMovementRepository>();
        services.AddScoped<IDocFlowDocumentRuleRepository, DocFlowDocumentRuleRepository>();

        // Servicio de orquestación Docflow
        services.AddScoped<IDocflowService, DocflowService>();
        services.AddScoped<IDocflowDirectoryService, DocflowDirectoryService>();

        return services;
    }

    // =========================================================
    // EMAIL
    // Cola de fondo, proveedor SMTP, repositorios de layouts/logs
    // y servicios de construcción y envío de emails.
    // =========================================================

    /// <summary>
    /// Registra todos los componentes del sistema de email:
    /// repositorios, servicios, proveedor SMTP, cola en background y worker.
    /// La configuración se lee desde "EmailSettings" y "EmailTemplates" en appsettings.json.
    /// </summary>
    public static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuraciones tipadas de email leídas desde appsettings.json
        services.Configure<EmailSettings>(
            configuration.GetSection("EmailSettings"));
        services.Configure<WsUtaSystem.Application.Common.Email.EmailTemplatesOptions>(
            configuration.GetSection("EmailTemplates"));

        // Repositorios de persistencia de email
        services.AddScoped<IEmailLayoutsRepository, EmailLayoutsRepository>();
        services.AddScoped<IEmailLogsRepository, EmailLogsRepository>();
        services.AddScoped<IEmailLogAttachmentsRepository, EmailLogAttachmentsRepository>();

        // Servicios de negocio de email
        services.AddScoped<IEmailLayoutsService, EmailLayoutsService>();
        services.AddScoped<IEmailLogsService, EmailLogsService>();
        services.AddScoped<IEmailLogAttachmentsService, EmailLogAttachmentsService>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();

        // Builder fluent para construcción de emails (Transient: nueva instancia cada uso)
        services.AddTransient<
            WsUtaSystem.Application.Interfaces.Services.IEmailBuilder,
            WsUtaSystem.Application.Services.EmailBuilder>();

        // Proveedor de credenciales desde variables de entorno (Singleton: sin estado mutable)
        services.AddSingleton<IEnvironmentCredentialProvider, EnvironmentCredentialProvider>();

        // Proveedor de envío SMTP via MailKit
        services.AddScoped<IEmailProvider, MailKitEmailProvider>();

        // Cola de fondo con capacidad máxima y timeout configurados
        services.AddSingleton<IBackgroundTaskQueue<WsUtaSystem.Application.DTOs.Email.EmailSendRequestDto>>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BackgroundTaskQueue<WsUtaSystem.Application.DTOs.Email.EmailSendRequestDto>>>();
            return new BackgroundTaskQueue<WsUtaSystem.Application.DTOs.Email.EmailSendRequestDto>(
                capacity: 1000,                           // Máximo de emails en cola simultáneos
                enqueueTimeout: TimeSpan.FromSeconds(2),        // Timeout si la cola está llena
                logger: logger);
        });

        // Dispatcher y worker del sistema de cola de emails
        services.AddSingleton<IEmailDispatcher, EmailDispatcher>();
        services.AddHostedService<EmailQueueWorker>();

        return services;
    }

    // =========================================================
    // APROVISIONAMIENTO EMPLEADOS (HrBackend → RepositoryUta)
    // Typed HttpClient para llamar al endpoint de provisioning en RepositoryUta.
    // La URL base se lee de la sección "AuthService:Url" de appsettings.json.
    // =========================================================

    /// <summary>
    /// Registra el cliente HTTP tipado para el servicio de aprovisionamiento de empleados.
    /// Requiere la sección "AuthService:Url" en appsettings.json.
    /// </summary>
    public static IServiceCollection AddProvisioningServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["AuthService:Url"] ?? string.Empty;

        services.AddHttpClient<IEmployeeProvisioningClient, EmployeeProvisioningClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IStudentProvisioningClient, StudentProvisioningClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Origen de datos de matrícula: cambiar a ApiStudentEnrollmentSource cuando la API esté lista
        services.AddScoped<IStudentEnrollmentSource, DbStudentEnrollmentSource>();
        services.AddScoped<IStudentEnrollmentSyncService, StudentEnrollmentSyncService>();

        return services;
    }

    // =========================================================
    // AUTENTICACIÓN JWT + USUARIO ACTUAL
    // Caché de tokens, cliente HTTP, validación de tokens custom
    // y acceso al usuario autenticado por request.
    // =========================================================

    /// <summary>
    /// Registra los servicios de autenticación JWT personalizada:
    /// caché en memoria para tokens, cliente HTTP para validación externa,
    /// servicio de validación de token y acceso al contexto del usuario actual.
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        // Caché en memoria para almacenar tokens validados y reducir llamadas externas
        services.AddMemoryCache();

        // HttpClient para llamadas al servicio externo de autenticación/validación
        services.AddHttpClient();

        // Servicio de validación de tokens JWT: "Remote" (default, valida contra RepositoryUta
        // por request) o "Local" (valida la firma RS256 localmente usando JWKS cacheado).
        services.AddScoped<WsUtaSystem.Infrastructure.Services.ITokenValidationService>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var mode = cfg["AuthService:ValidationMode"] ?? "Remote";

            return mode.Equals("Local", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<WsUtaSystem.Infrastructure.Services.LocalJwtValidationService>(sp)
                : ActivatorUtilities.CreateInstance<WsUtaSystem.Infrastructure.Services.TokenValidationService>(sp);
        });

        // Permite acceder a IHttpContextAccessor para leer headers y claims del request actual
        services.AddHttpContextAccessor();

        // Servicio que expone el usuario autenticado actual durante el request
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Resuelve permisos de acción (RolePermission) contra RepositoryUta para [RequirePermission]
        services.AddScoped<WsUtaSystem.Application.Common.Interfaces.IUserActionPermissionService,
            WsUtaSystem.Infrastructure.Services.UserActionPermissionService>();

        return services;
    }

    // =========================================================
    // RATE LIMITING
    // Limitador de tasa global leído desde la sección "RateLimiting"
    // de appsettings.json. Protege la API y la BD ante ráfagas de
    // requests (bugs de frontend en loop, clientes abusivos).
    // =========================================================

    /// <summary>
    /// Registra el rate limiter global con ventana fija, particionado por IP de origen.
    /// Lee "RateLimiting:PermitLimit", "RateLimiting:WindowSeconds" y "RateLimiting:QueueLimit"
    /// de la configuración, con valores seguros por defecto (200 requests / 60 segundos / sin cola).
    /// Los preflights OPTIONS, /health y /swagger están exentos para no interferir
    /// con CORS, monitoreo ni desarrollo.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var permitLimit = int.TryParse(section["PermitLimit"], out var pl) && pl > 0 ? pl : 200;
        var windowSeconds = int.TryParse(section["WindowSeconds"], out var ws) && ws > 0 ? ws : 60;
        var queueLimit = int.TryParse(section["QueueLimit"], out var ql) && ql >= 0 ? ql : 0;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Exentos: preflights CORS, health checks y Swagger no consumen cuota
                if (HttpMethods.IsOptions(context.Request.Method) ||
                    context.Request.Path.StartsWithSegments("/health") ||
                    context.Request.Path.StartsWithSegments("/swagger"))
                {
                    return RateLimitPartition.GetNoLimiter("exempt");
                }

                // Partición por IP real: UseForwardedHeaders corre al inicio del pipeline,
                // por lo que RemoteIpAddress ya refleja la IP del cliente detrás del proxy.
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
            });

            options.OnRejected = async (rejectionContext, cancellationToken) =>
            {
                // Retry-After indica al cliente cuándo reintentar (en segundos)
                rejectionContext.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();

                await rejectionContext.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Demasiadas solicitudes",
                    message = $"Se superó el límite de {permitLimit} solicitudes por {windowSeconds} segundos. Intente nuevamente más tarde."
                }, cancellationToken);
            };
        });

        return services;
    }

    // =========================================================
    // BASE DE DATOS + INTERCEPTORES
    // Interceptores de auditoría y logging SQL deben registrarse
    // ANTES del DbContext porque este los consume via GetRequiredService.
    // =========================================================

    /// <summary>
    /// Registra los interceptores de EF Core y el DbContext principal.
    /// El DbContext se configura con retry on failure y, en desarrollo,
    /// con logging detallado de queries y datos sensibles.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="environment">Entorno de ejecución para flags de desarrollo.</param>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Obtiene la cadena de conexión probando múltiples nombres de clave
        var connectionString =
            configuration.GetConnectionString("SqlServerConn") ??
            configuration.GetConnectionString("Sql") ??
            configuration.GetConnectionString("SqlServer") ??
            throw new InvalidOperationException(
                "No se encontró cadena de conexión. Verifique 'SqlServerConn', 'Sql' o 'SqlServer' en appsettings.json.");

        // Garantiza que se use autenticación SQL cuando no se usa Integrated Security
        if (!connectionString.Contains("Integrated Security", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("Authentication", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Integrated Security=False;Authentication=SqlPassword;";
        }

        // Los interceptores deben registrarse ANTES del DbContext para poder resolverlos
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<SqlErrorLoggingInterceptor>();

        services.AddDbContext<WsUtaSystem.Data.AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
                // Reintenta hasta 5 veces con 5 segundos entre intentos ante errores transitorios
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null));

            // Agrega los interceptores resolvidos desde el contenedor
            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<SqlErrorLoggingInterceptor>());

            // En desarrollo, habilita logging detallado para facilitar depuración
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    // =========================================================
    // MOTOR DOCUMENTAL INSTITUCIONAL
    // Plantillas HTML, resolución de campos, renderizado PDF
    // y gestión de acciones de personal.
    // =========================================================
    /// <summary>
    /// Registra todos los componentes del Motor Documental Institucional:
    /// repositorios de plantillas y documentos generados, motor de sustitución,
    /// renderizador PDF, resolver de campos y servicios de orquestación.
    /// </summary>
    public static IServiceCollection AddDocumentEngineServices(this IServiceCollection services)
    {
        // ── Repositorios ─────────────────────────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.Documents.IDocumentTemplateRepository,
            WsUtaSystem.Infrastructure.Repositories.Documents.DocumentTemplateRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.Documents.IDocumentTemplateFieldRepository,
            WsUtaSystem.Infrastructure.Repositories.Documents.DocumentTemplateFieldRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.Documents.IGeneratedDocumentRepository,
            WsUtaSystem.Infrastructure.Repositories.Documents.GeneratedDocumentRepository>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Repositories.Documents.IPersonnelActionRepository,
            PersonnelActionRepository>();

        // ── Motor de plantillas (sustitución de tokens {{CAMPO}}) ─────────────────
        services.AddSingleton<IDocumentTemplateEngine, DocumentTemplateEngine>();

        // ── Logo institucional: fuente única usada por reportes (QuestPDF) y por el
        //    resolver de campos de plantillas (HTML/Puppeteer). Singleton porque el
        //    archivo no cambia en tiempo de ejecución.
        services.AddSingleton<IInstitutionalLogoService, WsUtaSystem.Infrastructure.Services.InstitutionalLogoService>();

        // ── Resolver de campos (Employee, Contract, Movement, System, Manual) ─────
        services.AddScoped<IDocumentFieldResolver, DocumentFieldResolver>();

        // ── Renderizador PDF institucional ────────────────────────────────────────
        //services.AddScoped<IDocumentRenderer, InstitutionalDocumentRenderer>();
        services.AddScoped<InstitutionalDocumentRenderer>();
        services.AddScoped<PersonalActionDocumentRenderer>();
        services.AddScoped<IDocumentRendererFactory, DocumentRendererFactory>();

        // ── Servicios de aplicación ───────────────────────────────────────────────
        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.Documents.IDocumentTemplateService,
            WsUtaSystem.Application.Services.Documents.DocumentTemplateService>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.Documents.IDocumentTemplateFieldService,
            WsUtaSystem.Application.Services.Documents.DocumentTemplateFieldService>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.Documents.IDocumentGenerationService,
            WsUtaSystem.Application.Services.Documents.DocumentGenerationService>();

        services.AddScoped<
            WsUtaSystem.Application.Interfaces.Services.Documents.IPersonnelActionService,
            PersonnelActionService>();

        return services;
    }
}