using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WsUtaSystem.Infrastructure.Filters
{
    public class OpenApiVersionFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // No existe la propiedad 'Openapi' en OpenApiDocument.
            // Si necesitas establecer la versión, hazlo en swaggerDoc.Info.Version.
            //swaggerDoc.OpenApi = "3.0.1";
        }
    }
}