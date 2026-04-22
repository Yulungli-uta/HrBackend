using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using WsUtaSystem.Endpoints;
using WsUtaSystem.Infrastructure.DependencyInjection;
using WsUtaSystem.Infrastructure.Jobs;
using WsUtaSystem.Middleware;

// =========================================================
// BOOTSTRAP DE LA APLICACIÓN
//
// Este archivo únicamente orquesta:
//   1. El registro de servicios (delegado a ServiceCollectionExtensions)
//   2. La construcción del pipeline HTTP en el orden correcto
//
// Regla: NO debe haber lógica de negocio ni configuración inline aquí.
// Toda la configuración de servicios vive en DependencyInjection.cs
// =========================================================

var builder = WebApplication.CreateBuilder(args);

// ── 1. Logging estructurado ──────────────────────────────────────────────────
// Debe configurarse primero para capturar logs del resto del proceso de arranque
builder.AddSerilogConfiguration();

// ── 2. CORS ───────────────────────────────────────────────────────────────────
// El nombre de la política se necesita más adelante en el pipeline HTTP
var corsPolicy = builder.Services.AddCorsConfiguration(builder.Configuration);

// ── 3. Controllers + JSON ─────────────────────────────────────────────────────
builder.Services.AddControllersConfiguration();

// ── 4. Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddSwaggerConfiguration();

// ── 5. AutoMapper + FluentValidation ─────────────────────────────────────────
builder.Services.AddMappingAndValidation();

// ── 6. Repositorios y servicios genéricos ────────────────────────────────────
builder.Services.AddGenericRepositories();

// ── 7. Dominio RH: todos los repositorios y servicios por entidad ─────────────
builder.Services.AddDomainServices();

// ── 8. Módulo Docflow ─────────────────────────────────────────────────────────
builder.Services.AddDocflowServices();

// ── 9. Sistema de Email ───────────────────────────────────────────────────────
builder.Services.AddEmailServices(builder.Configuration);

// ── 10. Reportes ──────────────────────────────────────────────────────────────
builder.Services.AddReportServices(builder.Configuration);

// ── 11. Motor Documental Institucional ───────────────────────────────────────
builder.Services.AddDocumentEngineServices();
// ── 12. Permisos de usuario ───────────────────────────────────────────────────
builder.Services.AddUserPermissionServices();

// ── 12. Autenticación JWT + usuario actual ────────────────────────────────────
builder.Services.AddAuthServices();

// ── 13. Base de datos + interceptores EF Core ────────────────────────────────
// Debe ir después de AddAuthServices porque el interceptor de auditoría
// depende de ICurrentUserService
builder.Services.AddDatabaseServices(builder.Configuration, builder.Environment);

// ── 14. Jobs programados (Quartz) ─────────────────────────────────────────────
builder.Services.AddQuartzJobs();

// =========================================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// =========================================================
var app = builder.Build();

// ── Pipeline: Headers de proxy inverso ───────────────────────────────────────
// Debe ser el primero para que los middlewares posteriores lean la IP/protocolo real
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ── Pipeline: Manejo global de errores ───────────────────────────────────────
// Envuelve todo el pipeline para capturar excepciones no controladas
app.UseMiddleware<ErrorHandlingMiddleware>();

// ── Pipeline: Archivos estáticos ─────────────────────────────────────────────
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Pipeline: Logging de requests HTTP ───────────────────────────────────────
app.UseSerilogRequestLogging(opts =>
{
    opts.IncludeQueryInRequestPath = true;
});

// ── Pipeline: Routing explícito ──────────────────────────────────────────────
// DEBE ir antes de CORS y de los middlewares de autenticación
app.UseRouting();

// ── Pipeline: CORS ───────────────────────────────────────────────────────────
// DEBE ir entre UseRouting y UseEndpoints
app.UseCors(corsPolicy);

// ── Pipeline: Autenticación JWT personalizada ────────────────────────────────
// Se ejecuta después de CORS para que las preflight requests no requieran token
app.UseMiddleware<WsUtaSystem.Middleware.JwtAuthenticationMiddleware>();

// ── Pipeline: Swagger UI (solo en Development) ────────────────────────────────
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
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

// ── Pipeline: Endpoints ───────────────────────────────────────────────────────
app.UseEndpoints(endpoints =>
{
    // Grupo base para todos los endpoints del dominio RH
    //var apiGroup = endpoints.MapGroup("/api/v1/rh");
    //apiGroup.RequireCors(corsPolicy);

    //// Controladores MVC
    //apiGroup.MapControllers();

    //// Minimal APIs de reportes
    //apiGroup.MapReportEndpoints();

    endpoints.MapControllers().RequireCors(corsPolicy);

    // 2. MINIMAL APIs (Si no son controladores, hay que mapearlas manualmente)
    // Para reportes de RH
    endpoints.MapGroup("/api/v1/rh/reports")
            .RequireCors(corsPolicy)
            .MapReportEndpoints();

    // Endpoints de permisos de usuario (fuera del grupo /rh para acceso global)
    endpoints.MapUserPermissionEndpoints();

    // Redirección raíz → Swagger
    endpoints.MapGet("/", () => Results.Redirect("/swagger"));

    // Health check
    endpoints.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.Now }));
});

app.Run();