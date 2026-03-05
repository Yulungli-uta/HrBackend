// File: Infrastructure/Controller/ApiPrefixByNamespaceConvention.cs
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace WsUtaSystem.Infrastructure.Controller
{
    public sealed class ApiPrefixByNamespaceConvention : IApplicationModelConvention
    {
        private readonly string _apiRoot; // ej: "api/v1"
        private readonly IReadOnlyDictionary<string, string> _namespaceToSegment;

        public ApiPrefixByNamespaceConvention(string apiRoot, IReadOnlyDictionary<string, string> namespaceToSegment)
        {
            _apiRoot = apiRoot.Trim('/'); // "api/v1"
            _namespaceToSegment = namespaceToSegment;
        }

        public void Apply(ApplicationModel application)
        {
            // Ordenamos por namespace más específico primero (más largo)
            var mappings = _namespaceToSegment
                .OrderByDescending(kv => kv.Key.Length)
                .ToList();

            foreach (var controller in application.Controllers)
            {
                var ns = controller.ControllerType.Namespace ?? string.Empty;

                var matched = mappings.FirstOrDefault(m => ns.StartsWith(m.Key, StringComparison.Ordinal));
                if (string.IsNullOrWhiteSpace(matched.Key))
                    continue;

                var segment = matched.Value.Trim('/'); // "rh" o "docflow"
                var prefix = $"{_apiRoot}/{segment}";

                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel is null)
                        continue;

                    // Combina prefijo con ruta existente del controller
                    selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(
                        new AttributeRouteModel(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix)),
                        selector.AttributeRouteModel
                    );
                }
            }
        }
    }
}