using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BancolombiaWebService.Security
{
    /// <summary>
    /// Middleware para autenticación basada en Header SOAP.
    /// Intercepta el request, lee el Envelope y busca los tags <tem:userName> y <tem:password>.
    /// </summary>
    public class SoapHeaderAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SoapHeaderAuthenticationMiddleware> _logger;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enabled;

        public SoapHeaderAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<SoapHeaderAuthenticationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _enabled = configuration.GetValue<bool>("Security:SoapAuth:Enabled", true);
            _username = configuration.GetValue<string>("Security:BasicAuth:Username") ?? "Usuario"; // Reutilizamos config por ahora
            _password = configuration.GetValue<string>("Security:BasicAuth:Password") ?? "Contraseña";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_enabled || !context.Request.Path.Value.EndsWith(".svc", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            try
            {
                context.Request.EnableBuffering(); // Importante para poder leer el body y luego rebobinar

                // Leer el body
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0; // Rebobinar para el siguiente middleware (SoapCore)

                if (string.IsNullOrWhiteSpace(body))
                {
                   // Si el body está vacío, dejar pasar (quizás sea GET wsdl)
                   await _next(context);
                   return;
                }
                
                // Intento simple de parsing para encontrar credenciales
                // Nota: Esto es una validación básica. Para prod robusto usaríamos XmlDocument con Namespaces.
                if (!ValidateCredentials(body))
                {
                    _logger.LogWarning("Autenticación SOAP Header fallida.");
                    await ReturnSoapFault(context, "Autenticación fallida: usuario o contraseña incorrectos.");
                    return;
                }

                _logger.LogDebug("Autenticación SOAP Header exitosa.");
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando autenticación SOAP");
                // Resetear stream por si acaso
                if(context.Request.Body.CanSeek) context.Request.Body.Position = 0;
                await _next(context); // Dejar que SoapCore maneje el error de formato si no pudimos leer
            }
        }

        private bool ValidateCredentials(string soapEnvelope)
        {
            // Buscamos los patrones del texto requerimiento:
            // <tem:userName>Usuario</tem:userName>
            // <tem:password>Contraseña</tem:password>
            
            // Usamos XDocument para ser un poco más seguros que string contains
            try 
            {
               // Limpiamos un poco el XML si tiene caracteres raros al inicio
               int index = soapEnvelope.IndexOf("<");
               if (index > 0) soapEnvelope = soapEnvelope.Substring(index);
               
               var doc = XDocument.Parse(soapEnvelope);
               
               // Buscar con el namespace 'tempuri.org' o sin namespace si es necesario
               XNamespace tem = "http://tempuri.org/";
               
               var header = doc.Descendants(tem + "userName").FirstOrDefault()?.Value 
                            ?? doc.Descendants("userName").FirstOrDefault()?.Value; // fallback
                            
               var pass = doc.Descendants(tem + "password").FirstOrDefault()?.Value
                          ?? doc.Descendants("password").FirstOrDefault()?.Value; // fallback

               if (header == _username && pass == _password)
               {
                   return true;
               }
               
               return false;
            }
            catch
            {
                return false; // XML inválido
            }
        }

        private async Task ReturnSoapFault(HttpContext context, string message)
        {
            context.Response.ContentType = "text/xml; charset=utf-8";
            context.Response.StatusCode = 200; // SOAP faults son 200 o 500, pero deben ser XML válido

            var fault = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                            <soapenv:Body>
                                <soapenv:Fault>
                                    <faultcode>soapenv:Client</faultcode>
                                    <faultstring>{message}</faultstring>
                                </soapenv:Fault>
                            </soapenv:Body>
                           </soapenv:Envelope>";
            
            await context.Response.WriteAsync(fault);
        }
    }

    public static class SoapHeaderAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSoapHeaderAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SoapHeaderAuthenticationMiddleware>();
        }
    }
}
