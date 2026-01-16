using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Models
{
    public sealed record SpResult(int StatusCode, string Message)
    {
    public bool Ok => StatusCode == 0;
    public bool NoOp => StatusCode == 1;
}
}
