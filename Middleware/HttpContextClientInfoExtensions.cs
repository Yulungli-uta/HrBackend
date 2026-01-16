namespace WsUtaSystem.Middleware
{
    public static class HttpContextClientInfoExtensions
    {
        public static string? GetClientIp(this HttpContext ctx)
        {
            // 1) X-Forwarded-For (proxies / load balancers)
            var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(xff))
            {
                // "ip1, ip2, ip3" -> ip1
                return xff.Split(',')[0].Trim();
            }

            // 2) X-Real-IP (nginx / algunos proxies)
            var xReal = ctx.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(xReal))
            {
                return xReal.Trim();
            }

            // 3) RemoteIpAddress (sin proxy o ya procesado por forwarded headers)
            var ip = ctx.Connection.RemoteIpAddress?.ToString();

            // Normaliza loopback IPv6
            if (ip == "::1") return "127.0.0.1";

            return ip;
        }

        public static string? GetUserAgent(this HttpContext ctx)
        {
            var ua = ctx.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(ua) ? null : ua;
        }

        public static string? GetDeviceInfo(this HttpContext ctx)
        {
            // si tu front envía este header
            var device = ctx.Request.Headers["X-Device-Info"].ToString();
            return string.IsNullOrWhiteSpace(device) ? null : device;
        }
    }
}