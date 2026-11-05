using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Academic;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Infrastructure.ExternalServices.Common;
using WsUtaSystem.Infrastructure.ExternalServices.Deadv;
using WsUtaSystem.Infrastructure.ExternalServices.Dinardap;
using WsUtaSystem.Infrastructure.ExternalServices.Provisioning;
using WsUtaSystem.Infrastructure.ExternalServices.Orcid;
using WsUtaSystem.Infrastructure.ExternalServices.Scopus;

namespace WsUtaSystem.Infrastructure.ExternalServices;

/// <summary>
/// Punto único de registro de los clientes HTTP hacia servicios externos. Cada uno pasa por
/// <see cref="ExternalCallLoggingHandler"/>, que registra servicio, host, estado y duración de cada llamada.
/// </summary>
public static class ExternalServicesExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ExternalServiceUrlResolver>();

        // RepositoryUta (autenticación y aprovisionamiento). La URL vive en AuthService:Url (configuración):
        // debe estar disponible aunque la base de datos falle.
        var authUrl = configuration["AuthService:Url"] ?? string.Empty;
        services.AddHttpClient<IEmployeeProvisioningClient, EmployeeProvisioningClient>(client => ConfigureBase(client, authUrl))
            .AddHttpMessageHandler(sp => LoggingHandler("RepositoryUta", sp));
        services.AddHttpClient<IStudentProvisioningClient, StudentProvisioningClient>(client => ConfigureBase(client, authUrl))
            .AddHttpMessageHandler(sp => LoggingHandler("RepositoryUta", sp));

        // DINARDAP (WsUtaDinardap.Api). La URL vive en el parámetro DINARDAP_API_URL (HR.tbl_Parameters). Envía el JWT de la
        // cuenta de servicio, así que no se siguen redirecciones y el host se valida como institucional (anti-SSRF).
        services.AddHttpClient<IHrDinardapClient, HrDinardapClient>(client => client.Timeout = DefaultTimeout)
            .AddHttpMessageHandler(sp => LoggingHandler("DINARDAP", sp))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        // DEaDV (certificados de capacitación). La URL vive en el parámetro DEADV_API_URL. Sin redirecciones: el
        // descargador de PDF valida el host de la URL y un redirect lo saltaría.
        services.AddHttpClient<IDeadvClient, DeadvClient>(client => client.Timeout = DefaultTimeout)
            .AddHttpMessageHandler(sp => LoggingHandler("DEaDV", sp))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        // Elsevier (Scopus) para sincronizar publicaciones. URL y credenciales en Parámetros del Sistema (PUBSYNC_SCOPUS_*); el
        // host se fija en el adaptador. Sin redirecciones: la credencial solo se envía al host validado.
        services.AddHttpClient<ScopusPublicationProvider>(client => client.Timeout = TimeSpan.FromSeconds(40))
            .AddHttpMessageHandler(sp => LoggingHandler("Scopus", sp))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddTransient<IPublicationSourceProvider>(sp => sp.GetRequiredService<ScopusPublicationProvider>());

        // ORCID (API pública, solo lectura). Servicio gratuito de un tercero con host fijo: sin credenciales ni parámetros. No se
        // envía ninguna credencial, pero igualmente no se siguen redirecciones para no salir del host.
        services.AddHttpClient<OrcidPublicationProvider>(ConfigureOrcid)
            .AddHttpMessageHandler(sp => LoggingHandler("ORCID", sp))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddTransient<IPublicationSourceProvider>(sp => sp.GetRequiredService<OrcidPublicationProvider>());
        services.AddHttpClient<IOrcidDirectory, OrcidDirectoryClient>(ConfigureOrcid)
            .AddHttpMessageHandler(sp => LoggingHandler("ORCID", sp))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        return services;
    }

    private static void ConfigureOrcid(HttpClient client)
    {
        client.BaseAddress = new Uri($"https://{OrcidPublicationProvider.Host}/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WsUtaSystem-HR/1.0 (Universidad Tecnica de Ambato)");
    }

    private static void ConfigureBase(HttpClient client, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl))
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");

        client.Timeout = DefaultTimeout;
    }

    private static ExternalCallLoggingHandler LoggingHandler(string service, IServiceProvider sp) =>
        new(service, sp.GetRequiredService<ILoggerFactory>());
}
