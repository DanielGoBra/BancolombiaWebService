using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BancolombiaWebService.Security
{
    /// <summary>
    /// Middleware para validar que las solicitudes provengan únicamente de IPs autorizadas (whitelist).
    /// Control de seguridad recomendado por Bancolombia para autenticación.
    /// </summary>
    public class IPWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IPWhitelistMiddleware> _logger;
        private readonly HashSet<string> _allowedIPs;

        public IPWhitelistMiddleware(
            RequestDelegate next,
            ILogger<IPWhitelistMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;

            // Cargar IPs permitidas desde configuración
            var ips = configuration.GetSection("Security:AllowedIPs").Get<string[]>() ?? Array.Empty<string>();
            _allowedIPs = new HashSet<string>(ips, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("IPWhitelistMiddleware inicializado con {Count} IPs permitidas", _allowedIPs.Count);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(remoteIp))
            {
                _logger.LogWarning("Solicitud rechazada: No se pudo determinar IP remota");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Acceso denegado: IP no identificada");
                return;
            }

            // Validar si la IP está en la whitelist
            if (!_allowedIPs.Contains(remoteIp) && !_allowedIPs.Contains("*"))
            {
                _logger.LogWarning("Solicitud rechazada desde IP no autorizada: {IP}", remoteIp);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Acceso denegado: IP no autorizada");
                return;
            }

            _logger.LogDebug("Solicitud permitida desde IP autorizada: {IP}", remoteIp);
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware
    /// </summary>
    public static class IPWhitelistMiddlewareExtensions
    {
        public static IApplicationBuilder UseIPWhitelist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IPWhitelistMiddleware>();
        }
    }
}
