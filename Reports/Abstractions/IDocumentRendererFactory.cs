using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Abstractions
{
    public interface IDocumentRendererFactory
    {
        IDocumentRenderer GetRenderer(string? templateType);
    }
}
