// ============================================================================
// Program.cs - Punto de entrada principal del Web Service de Recaudo Bancolombia
// ============================================================================
// Este servicio implementa el contrato SOAP "RIN una Sola Vía" (Recaudo Integrado
// al Negocio) conforme a las especificaciones técnicas de Bancolombia v4.0 Jun 2023.
//
// Bancolombia CONSUME este servicio para:
//   1. Verificar que el servicio está operativo (VerificarEstadoWebService)
//   2. Notificar pagos recibidos en tiempo real (RegistrarPagoIFX)
//
// La mensajería usa formato IFX (Interactive Financial Exchange) encapsulada
// dentro de secciones CDATA en el XML SOAP.
//
// FLUJO DE UNA PETICION:
//   Request HTTP -> HTTPS Redirect -> IP Whitelist -> Auth SOAP Header -> SoapCore -> Servicio
// ============================================================================

using BancolombiaWebService.Contracts;
using BancolombiaWebService.Security;
using BancolombiaWebService.Services;
using SoapCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING ---
// Serilog proporciona logging estructurado con rotación diaria de archivos.
// Los logs son CRITICOS para la auditoría de transacciones (requerimiento Bancolombia sec. 2.2.1 punto 7).
// Se conservan 30 días de logs para trazabilidad.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/bancolombia-service-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// --- INYECCION DE DEPENDENCIAS ---
// IRINunaSolaViaService: Contrato SOAP principal con los 2 métodos del servicio
// ITransactionLogger: Logger de auditoría para cada transacción procesada
// IPagoProcessor: Lógica de negocio para procesar pagos (aquí se conectará la BD/ERP)
// SoapCore: Librería que habilita endpoints SOAP en ASP.NET Core
builder.Services.AddSingleton<IRINunaSolaViaService, RINunaSolaViaService>();
builder.Services.AddSingleton<ITransactionLogger, TransactionLogger>();
builder.Services.AddSingleton<IPagoProcessor, PagoProcessor>();
builder.Services.AddSoapCore();

// --- HTTPS ---
// Bancolombia REQUIERE comunicación HTTPS (spec sec. 2.1.2 y 2.2).
// En desarrollo se usa el certificado auto-firmado de .NET.
// En Testing/Producción se carga el certificado desde appsettings.{Env}.json
// (ruta configurada en Kestrel:Endpoints:Https:Certificate).
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            // El certificado se carga automáticamente desde appsettings.json en Testing/Production
        });
    });
}

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARE (el orden es CRITICO) ---

// 1. Forzar redirección HTTP -> HTTPS (todo tráfico debe ser cifrado)
app.UseHttpsRedirection();

// 2. Validación de IP: Solo permite IPs de Bancolombia (spec sec. 2.2.3)
//    Las IPs permitidas se configuran en appsettings.{Env}.json -> Security:AllowedIPs
app.UseIPWhitelist();

// 3. Autenticación SOAP Header: Valida usuario/contraseña en el Header SOAP (spec sec. 2.2.4)
//    Bancolombia envía <tem:userName> y <tem:password> en el Header del Envelope SOAP
app.UseSoapHeaderAuthentication();

// 4. Habilitar enrutamiento
app.UseRouting();

// 5. Endpoint SOAP - Expone el servicio en la ruta /RINunaSolaVia.svc
//    El WSDL queda disponible en /RINunaSolaVia.svc?wsdl
//    SoapSerializer.XmlSerializer es necesario porque usamos [MessageContract] en los modelos
app.UseEndpoints(endpoints =>
{
    endpoints.UseSoapEndpoint<IRINunaSolaViaService>(
        path: "/RINunaSolaVia.svc",               // URL del endpoint SOAP
        encoder: new SoapEncoderOptions(),          // Encoding SOAP 1.1 por defecto
        serializer: SoapSerializer.XmlSerializer);  // Serialización XML para MessageContract
});

// Página de información básica (solo para verificación manual, no es parte del contrato)
app.MapGet("/", () =>
{
    return Results.Content(@"
        <html>
            <body>
                <h1>Bancolombia Web Service - RINunaSolaVia</h1>
                <p>Servicio SOAP operativo</p>
                <p><strong>WSDL:</strong> <a href='/RINunaSolaVia.svc?wsdl'>/RINunaSolaVia.svc?wsdl</a></p>
                <p><strong>Versión:</strong> 1.0.0</p>
                <p><strong>Estado:</strong> Activo</p>
            </body>
        </html>", "text/html");
});

Log.Information("Bancolombia WebService iniciando...");
Log.Information("Ambiente: {Environment}", builder.Environment.EnvironmentName);
Log.Information("SOAP Endpoint: /RINunaSolaVia.svc");

app.Run();
