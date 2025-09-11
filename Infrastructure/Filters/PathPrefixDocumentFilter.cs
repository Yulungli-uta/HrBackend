using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WsUtaSystem.Infrastructure.Filters
{
    public class PathPrefixDocumentFilter : IDocumentFilter
    {
        private readonly string _pathPrefix;

        public PathPrefixDocumentFilter(string pathPrefix)
        {
            _pathPrefix = pathPrefix;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = new OpenApiPaths();
            foreach (var path in swaggerDoc.Paths)
            {
                paths.Add($"{_pathPrefix}{path.Key}", path.Value);
            }
            swaggerDoc.Paths = paths;
        }
    }
}
