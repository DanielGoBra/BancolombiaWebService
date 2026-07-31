using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace BancolombiaWebService.Security
{
    /// <summary>
    /// Middleware para autenticación básica HTTP (opcional).
    /// Valida usuario y contraseña en el header Authorization.
    /// La contraseña debe cumplir con requisitos mínimos de seguridad:
    /// - Mínimo 12 caracteres
    /// - Incluir mayúsculas, minúsculas, números y caracteres especiales
    /// </summary>
    public class BasicAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BasicAuthenticationMiddleware> _logger;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enabled;

        public BasicAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<BasicAuthenticationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;

            _enabled = configuration.GetValue<bool>("Security:BasicAuth:Enabled");
            _username = configuration.GetValue<string>("Security:BasicAuth:Username") ?? "";
            _password = configuration.GetValue<string>("Security:BasicAuth:Password") ?? "";

            if (_enabled)
            {
                ValidatePasswordComplexity(_password);
                _logger.LogInformation("BasicAuthenticationMiddleware habilitado para usuario: {Username}", _username);
            }
            else
            {
                _logger.LogInformation("BasicAuthenticationMiddleware deshabilitado");
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Si no está habilitado, continuar
            if (!_enabled)
            {
                await _next(context);
                return;
            }

            // Verificar header de autorización
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("Solicitud rechazada: Header Authorization no presente");
                await ReturnUnauthorized(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            
            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Solicitud rechazada: Esquema de autenticación inválido");
                await ReturnUnauthorized(context);
                return;
            }

            try
            {
                // Decodificar credenciales
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length != 2)
                {
                    _logger.LogWarning("Solicitud rechazada: Formato de credenciales inválido");
                    await ReturnUnauthorized(context);
                    return;
                }

                var username = credentials[0];
                var password = credentials[1];

                // Validar credenciales
                if (username != _username || password != _password)
                {
                    _logger.LogWarning("Solicitud rechazada: Credenciales inválidas para usuario: {Username}", username);
                    await ReturnUnauthorized(context);
                    return;
                }

                _logger.LogDebug("Autenticación exitosa para usuario: {Username}", username);
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando autenticación básica");
                await ReturnUnauthorized(context);
            }
        }

        private async Task ReturnUnauthorized(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Bancolombia WebService\"");
            await context.Response.WriteAsync("Acceso denegado: Credenciales inválidas o ausentes");
        }

        private void ValidatePasswordComplexity(string password)
        {
            if (password.Length < 12)
                throw new InvalidOperationException("La contraseña debe tener al menos 12 caracteres");

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                throw new InvalidOperationException(
                    "La contraseña debe incluir mayúsculas, minúsculas, números y caracteres especiales");
            }
        }
    }

    /// <summary>
    /// Extension method para registrar el middleware
    /// </summary>
    public static class BasicAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseBasicAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BasicAuthenticationMiddleware>();
        }
    }
}
