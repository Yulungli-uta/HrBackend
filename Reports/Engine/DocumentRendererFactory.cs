using Microsoft.Extensions.DependencyInjection;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Renderers;

namespace WsUtaSystem.Reports.Engine;

public sealed class DocumentRendererFactory : IDocumentRendererFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentRendererFactory> _logger;

    public DocumentRendererFactory(IServiceProvider serviceProvider, ILogger<DocumentRendererFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    public IDocumentRenderer GetRenderer(string? templateType)
    {

        var normalized = (templateType ?? string.Empty).Trim().ToUpperInvariant();

        _logger.LogInformation(
            "DocumentRendererFactory seleccionando renderer. TemplateType={TemplateType}, Normalized={Normalized}",
            templateType,
            normalized);

        //return (templateType ?? string.Empty).Trim().ToUpperInvariant() switch
        //{
        //    "ACCION_PERSONAL" =>
        //        _serviceProvider.GetRequiredService<PersonalActionDocumentRenderer>(),

        //    _ =>
        //        _serviceProvider.GetRequiredService<InstitutionalDocumentRenderer>()
        //};
        return normalized switch
        {
            "ACCION_PERSONAL" =>
                _serviceProvider.GetRequiredService<HtmlDocumentRenderer>(),

            "CONTRATO" =>
                _serviceProvider.GetRequiredService<HtmlDocumentRenderer>(),

            _ =>
                _serviceProvider.GetRequiredService<InstitutionalDocumentRenderer>()
        };
    }
}