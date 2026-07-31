# Bancolombia WebService - RINunaSolaVia
## Servicio SOAP para Recepción de Pagos en Tiempo Real

###  Descripción General

Este servicio web SOAP permitirá recibir notificaciones de pagos de Bancolombia en tiempo real, cumpliendo con todas las especificaciones técnicas del Anexo 3 - Información técnica en el Web Service Bancolombia 1 vías.

**Funcionalidades principales:**
- **VerificarEstadoWebService**: Monitoreo de disponibilidad del servicio
- **RegistrarPagoIFX**: Registro de pagos en formato IFX (SLA: 2 segundos máximo)

---

## Arquitectura y Tecnologías

### Stack Tecnológico
- **.NET 8.0**: Framework principal
- **SoapCore**: Implementación de servicios SOAP
- **Serilog**: Logging estructurado y auditoría transaccional
- **ASP.NET Core**: Host del servicio web

### Estructura del Proyecto

```
BancolombiaWebService/
├── BancolombiaWebService/              # Servicio principal
│   ├── Program.cs                      # Configuración y startup
│   ├── Services/
│   │   ├── RINunaSolaViaService.cs    # Implementación SOAP
│   │   └── TransactionLogger.cs        # Logger transaccional
│   ├── appsettings.Development.json    # Config desarrollo
│   ├── appsettings.Testing.json        # Config pruebas
│   └── appsettings.Production.json     # Config producción
│
├── BancolombiaWebService.Contracts/    # Contratos del servicio
│   ├── IRINunaSolaViaService.cs       # Interfaz del servicio
│   ├── VerificarEstadoModels.cs       # Modelos verificación
│   ├── RegistrarPagoModels.cs         # Modelos de pago
│   └── IFX/
│       ├── IFXMessageProcessor.cs      # Procesador mensajes IFX
│       └── IFXModels.cs                # Modelos IFX
│
└── BancolombiaWebService.Security/     # Seguridad
    ├── IPWhitelistMiddleware.cs        # Validación de IPs
    └── BasicAuthenticationMiddleware.cs # Autenticación básica
```

---

## Seguridad Implementada

### 1. HTTPS Obligatorio (Mandatorio)
- **Certificado SSL**: Debe ser emitido por una CA reconocida (VeriSign, Certicámaras, Let's Encrypt)
- **Protocolo**: TLS 1.2 o superior
- **Configuración**: Ver sección "Configuración de Certificados"

### 2. Validación de IP (Whitelist)
- Solo las IPs de Bancolombia pueden consumir el servicio
- Configurar en `appsettings.json`:

```json
"Security": {
  "AllowedIPs": [
    "200.xxx.xxx.xxx",  // IP 1 de Bancolombia
    "201.xxx.xxx.xxx"   // IP 2 de Bancolombia
  ]
}
```

  
```json
"Security": {
  "BasicAuth": {
    "Enabled": true,
    "Username": "usuario",
    "Password": "CAMBIAR"
  }
}
```

---

##  Instalación y Configuración

### Prerrequisitos
- .NET 8.0 SDK o superior
- Certificado SSL válido
- IPs públicas de Bancolombia
- Firewall configurado para permitir tráfico HTTPS

### Paso 1: Clonar y Compilar

```powershell
# Navegar al directorio del proyecto


# Restaurar paquetes NuGet
dotnet restore

# Compilar el proyecto
dotnet build --configuration Release
```

### Paso 2: Configurar Certificados SSL

#### Opción A: Certificado de Producción
1. Obtener certificado SSL de una CA reconocida
2. Convertir a formato PFX si es necesario
3. Colocar en la carpeta configurada

```powershell
# Ejemplo de conversión de certificado
openssl pkcs12 -export -out bancolombia-service.pfx -inkey private.key -in certificate.crt -certfile ca-bundle.crt
```

#### Opción B: Certificado de Desarrollo (Solo para pruebas locales)
```powershell
# Generar certificado auto-firmado
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\bancolombia-dev.pfx -p "YourPassword"
dotnet dev-certs https --trust
```

### Paso 3: Configurar IPs Permitidas

Editar `appsettings.Production.json`:

```json
"Security": {
  "AllowedIPs": [
    "IP_BANCOLOMBIA_1",
    "IP_BANCOLOMBIA_2"
  ]
}
```

 **IMPORTANTE**: Solicitar a Bancolombia las IPs estáticas desde las cuales consumirán el servicio.

### Paso 4: Configurar Autenticación (Opcional)

Si decides usar autenticación básica:

```json
"Security": {
  "BasicAuth": {
    "Enabled": true,
    "Username": "usuario",
    "Password": "CAMBIAR"
  }
}
```

Compartir estas credenciales de forma segura con Bancolombia.

---

##  Despliegue

### Desarrollo Local

```powershell
# Ejecutar en modo desarrollo
dotnet run --project BancolombiaWebService --environment Development

# El servicio estará disponible en:
# https://localhost:5001/RINunaSolaVia.svc
# WSDL: https://localhost:5001/RINunaSolaVia.svc?wsdl
```

### Ambiente de Pruebas

```powershell
# Publicar aplicación
dotnet publish -c Release -o ./publish

# Ejecutar con configuración de pruebas
cd publish
dotnet BancolombiaWebService.dll --environment Testing
```

### Producción (Windows Server con IIS)

1. **Publicar la aplicación**:
```powershell
dotnet publish -c Release -o C:\inetpub\BancolombiaWebService
```

2. **Configurar IIS**:
   - Crear nuevo Application Pool (.NET Core)
   - Crear nuevo sitio web
   - Apuntar al directorio publicado
   - Configurar binding HTTPS con el certificado SSL
   - Habilitar protocolo HTTPS en puerto 443

3. **Configurar Firewall**:
```powershell
# Abrir puerto HTTPS
New-NetFirewallRule -DisplayName "Bancolombia HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow

# Restricción de IPs (ejemplo)
New-NetFirewallRule -DisplayName "Bancolombia IP Whitelist" -Direction Inbound -RemoteAddress "IP_BANCOLOMBIA_1","IP_BANCOLOMBIA_2" -Protocol TCP -LocalPort 443 -Action Allow
```

### Producción (Linux con Nginx como Reverse Proxy)

1. **Publicar aplicación**:
```bash
dotnet publish -c Release -o /var/www/bancolombia-webservice
```

2. **Crear servicio systemd** (`/etc/systemd/system/bancolombia-webservice.service`):
```ini
[Unit]
Description=Bancolombia WebService

[Service]
WorkingDirectory=/var/www/bancolombia-webservice
ExecStart=/usr/bin/dotnet /var/www/bancolombia-webservice/BancolombiaWebService.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

3. **Configurar Nginx** (`/etc/nginx/sites-available/bancolombia`):
```nginx
server {
    listen 443 ssl;
    server_name tu-dominio.com;

    ssl_certificate /etc/ssl/certs/bancolombia-service.crt;
    ssl_certificate_key /etc/ssl/private/bancolombia-service.key;
    ssl_protocols TLSv1.2 TLSv1.3;

    # Restricción de IPs
    allow IP_BANCOLOMBIA_1;
    allow IP_BANCOLOMBIA_2;
    deny all;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

4. **Iniciar servicios**:
```bash
systemctl enable bancolombia-webservice
systemctl start bancolombia-webservice
systemctl restart nginx
```

---

##  Pruebas del Servicio

### 1. Verificar WSDL

Abrir en navegador:
```
https://tu-servidor.com/RINunaSolaVia.svc?wsdl
```

Deberías ver el XML del WSDL con los métodos `VerificarEstadoWebService` y `RegistrarPagoIFX`.

### 2. Probar con SoapUI

1. Descargar e instalar [SoapUI](https://www.soapui.org/)
2. Crear nuevo proyecto SOAP con el WSDL
3. Probar `VerificarEstadoWebService`:

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
   <soapenv:Header/>
   <soapenv:Body>
      <v1:VerificarEstadoWebService>
         <v1:request>
            <v1:TransaccionId>TEST-001</v1:TransaccionId>
            <v1:FechaHora>2025-10-23T10:00:00</v1:FechaHora>
         </v1:request>
      </v1:VerificarEstadoWebService>
   </soapenv:Body>
</soapenv:Envelope>
```

4. Probar `RegistrarPagoIFX`:

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                  xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
   <soapenv:Header/>
   <soapenv:Body>
      <v1:RegistrarPagoIFX>
         <v1:request>
            <v1:MensajeIFX><![CDATA[
               <IFX>
                  <MsgId>MSG-001</MsgId>
                  <TrnId>TRN-12345</TrnId>
                  <EffDt>2025-10-23</EffDt>
                  <CurAmt>
                     <Amt>100000</Amt>
                     <CurCode>COP</CurCode>
                  </CurAmt>
                  <RefInfo>REF-12345</RefInfo>
                  <PayerName>Juan Pérez</PayerName>
               </IFX>
            ]]></v1:MensajeIFX>
            <v1:TransaccionId>TEST-002</v1:TransaccionId>
         </v1:request>
      </v1:RegistrarPagoIFX>
   </soapenv:Body>
</soapenv:Envelope>
```

### 3. Probar con PowerShell

```powershell
# Crear solicitud SOAP
$soapRequest = @"
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:VerificarEstadoWebService>
      <v1:request>
        <v1:TransaccionId>PS-TEST-001</v1:TransaccionId>
      </v1:request>
    </v1:VerificarEstadoWebService>
  </soap:Body>
</soap:Envelope>
"@

# Enviar solicitud
$response = Invoke-WebRequest -Uri "https://localhost:5001/RINunaSolaVia.svc" `
    -Method POST `
    -ContentType "text/xml" `
    -Body $soapRequest `
    -SkipCertificateCheck

# Mostrar respuesta
$response.Content
```

---

##  Monitoreo y Logs

### Logs de Auditoría

Los logs se generan automáticamente en la carpeta `logs/`:

```
logs/
├── bancolombia-service-2025-10-23.log
├── bancolombia-service-2025-10-24.log
└── ...
```

### Formato de Log de Auditoría

```
2025-10-23 10:15:30.123 [INF] [AUDITORIA] RegistroPago | TransaccionId: TRN-12345 | Estado: EXITOSO | FechaHora: 2025-10-23T10:15:30 | Detalle: N/A | Request: {...}
```

### Monitoreo de Performance

**CRÍTICO**: El servicio DEBE responder en menos de 2 segundos. El log incluye el tiempo de respuesta:

```
[INF] RegistrarPagoIFX - Pago procesado exitosamente. TransaccionId: TRN-12345, Tiempo: 1234ms
[WRN] RegistrarPagoIFX - ADVERTENCIA: Tiempo de respuesta cercano al límite SLA: 1850ms
```

### Alertas Recomendadas

Configurar alertas para:
-  Respuestas > 1800ms (advertencia)
-  Respuestas > 2000ms (crítico)
-  Errores de autenticación (posible ataque)
-  Servicio no disponible

---

##  Personalización

### Implementar Lógica de Negocio

El procesamiento de pagos está en `Services/TransactionLogger.cs`, clase `PagoProcessor`.

**Reemplaza el código de ejemplo** en `ProcesarPago()` con tu lógica:

```csharp
public PagoProcessingResult ProcesarPago(IFXPagoRequest pago)
{
    try
    {
        // 1. Validar referencia contra tu base de datos
        var factura = _facturaRepository.GetByReferencia(pago.NumeroReferencia);
        if (factura == null)
        {
            return new PagoProcessingResult
            {
                Exitoso = false,
                CodigoError = IFXErrorCodes.InvalidReference,
                MensajeError = "Referencia no encontrada"
            };
        }

        // 2. Verificar duplicados
        if (_pagoRepository.ExistePago(pago.TrnId))
        {
            return new PagoProcessingResult
            {
                Exitoso = false,
                CodigoError = IFXErrorCodes.DuplicateTransaction,
                MensajeError = "Pago duplicado"
            };
        }

        // 3. Registrar pago en base de datos
        var pagoRegistrado = _pagoRepository.RegistrarPago(new Pago
        {
            TransaccionId = pago.TrnId,
            Monto = pago.Monto,
            Referencia = pago.NumeroReferencia,
            FechaPago = pago.FechaPago ?? DateTime.Now,
            // ... otros campos
        });

        // 4. Actualizar estado de factura
        _facturaRepository.MarcarComoPagada(factura.Id, pagoRegistrado.Id);

        // 5. Retornar éxito
        return new PagoProcessingResult
        {
            Exitoso = true,
            ReferenciaInterna = pagoRegistrado.Id.ToString()
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error procesando pago");
        return new PagoProcessingResult
        {
            Exitoso = false,
            CodigoError = IFXErrorCodes.InternalError,
            MensajeError = "Error interno"
        };
    }
}
```

---

##  Coordinación con Bancolombia

### Información a Proporcionar

1. **URLs de Servicio**:
   - **Pruebas**: `https://test.tu-dominio.com/RINunaSolaVia.svc`
   - **Producción**: `https://prod.tu-dominio.com/RINunaSolaVia.svc`

2. **WSDL**:
   - Agregar `?wsdl` a las URLs anteriores

3. **Certificado SSL**:
   - Compartir el certificado público (.crt o .cer)
   - Informar CA emisora
   - Notificar fecha de vencimiento

4. **Controles de Seguridad Implementados**:
   -  HTTPS (Mandatorio)
   -  Validación de IP (Whitelist)
   -  Autenticación con Certificado Cliente 

5. **Horarios de Disponibilidad**:
   - Indicar si hay ventanas de mantenimiento


##  Renovación de Certificados

 **MUY IMPORTANTE**: Los certificados SSL vencen anualmente.

### Proceso de Renovación

1. **Notificar a Bancolombia** con al menos **1 mes de antelación**
2. Obtener nuevo certificado
3. Probar en ambiente de testing
4. Coordinar ventana de cambio con Bancolombia
5. Actualizar certificado en producción
6. Verificar funcionamiento
7. Confirmar a Bancolombia

### Recordatorios Automáticos

Configurar alertas 60, 30 y 15 días antes del vencimiento:

```bash
# Verificar fecha de vencimiento del certificado
openssl x509 -in bancolombia-service.crt -noout -enddate
```

---


## Troubleshooting

### Problema: "Certificado no válido o no confiable"

**Solución**:
```powershell
# Verificar certificado
openssl x509 -in certificate.crt -text -noout

# Verificar cadena de certificados
openssl verify -CAfile ca-bundle.crt certificate.crt
```

### Problema: "IP no autorizada"

**Solución**: Verificar configuración en `appsettings.json` y logs para ver la IP real de origen.

### Problema: "Timeout - Respuesta > 2 segundos"

**Soluciones**:
1. Optimizar consultas a base de datos
2. Implementar caché
3. Usar procesamiento asíncrono donde sea posible
4. Revisar logs para identificar cuello de botella

### Problema: "Error parseando mensaje IFX"

**Solución**: Verificar que el XML IFX esté dentro de CDATA y revisar logs detallados.

---

## Licencia y Soporte

Este servicio fue desarrollado para cumplir con las especificaciones de Bancolombia.

**Soporte Técnico**:
- Email: contacto@inklusiva.com
- Teléfono: 321 5568104
- Horario: L-V 8:00 AM - 5:30 PM

---

## Referencias

- Anexo 3 - Información técnica en el Web Service Bancolombia 1 vías (Versión 4.0)
- Afiliación Sin terceros Web Service
- [SoapCore Documentation](https://github.com/DigDes/SoapCore)
- [IFX Standard](https://www.ifxforum.org/)

---

**Versión**: 1.0.0  
**Fecha**: Octubre 2025  
**Autor**: Daniel Felipe Gómez Bravo - Inklusiva S.A.S.
