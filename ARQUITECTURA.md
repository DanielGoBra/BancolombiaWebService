
Arquitectura del Sistema - Bancolombia WebService

## Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────────────┐
│                          BANCOLOMBIA                                 │
│                     (Cliente del Servicio)                          │
│                                                                      │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────────┐     │
│  │   Oficina    │      │   Internet   │      │   Cajero     │     │
│  │   Física     │      │   Banking    │      │   Automático │     │
│  └──────┬───────┘      └──────┬───────┘      └──────┬───────┘     │
│         │                     │                      │              │
│         └─────────────────────┴──────────────────────┘              │
│                               │                                      │
│                    ┌──────────▼──────────┐                         │
│                    │  Sistema Central    │                         │
│                    │    Bancolombia      │                         │
│                    └──────────┬──────────┘                         │
└───────────────────────────────┼──────────────────────────────────┘
                                │
                                │ HTTPS (TLS 1.2+)
                                │ Mensaje SOAP/IFX
                                │
                ┌───────────────▼───────────────┐
                │    FIREWALL / NAT              │
                │  ✓ Validación IP Bancolombia   │
                │  ✓ Puerto 443 (HTTPS)          │
                └───────────────┬────────────────┘
                                │
                                │
┌───────────────────────────────▼─────────────────────────────────────┐
│                     NOSOTROS INFRAESTRUCTURA                               │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │              Load Balancer (Opcional)                        │  │
│  │              - Alta Disponibilidad                           │  │
│  │              - Distribución de Carga                         │  │
│  └───────────────┬─────────────────────┬───────────────────────┘  │
│                  │                      │                           │
│        ┌─────────▼─────────┐  ┌────────▼────────┐                 │
│        │   Servidor 1      │  │   Servidor 2    │                 │
│        │   (Principal)     │  │   (Respaldo)    │                 │
│        └─────────┬─────────┘  └────────┬────────┘                 │
│                  │                      │                           │
│                  └──────────┬───────────┘                           │
│                             │                                       │
│  ┌──────────────────────────▼───────────────────────────────────┐ │
│  │           IIS / Nginx (Reverse Proxy)                         │ │
│  │           - Terminación SSL                                   │ │
│  │           - Certificado SSL Válido                            │ │
│  └──────────────────────────┬───────────────────────────────────┘ │
│                              │                                      │
│  ┌──────────────────────────▼───────────────────────────────────┐ │
│  │        ASP.NET Core Application (Kestrel)                     │ │
│  │                                                                │ │
│  │  ┌──────────────────────────────────────────────────────┐   │ │
│  │  │             Security Middleware                       │   │ │
│  │  │  ┌─────────────────────────────────────────────┐     │   │ │
│  │  │  │  1. HTTPS Redirection (Mandatorio)           │     │   │ │
│  │  │  └─────────────────────────────────────────────┘     │   │ │
│  │  │  ┌─────────────────────────────────────────────┐     │   │ │
│  │  │  │  2. IP Whitelist Middleware                  │     │   │ │
│  │  │  │     - Verifica IP origen                     │     │   │ │
│  │  │  │     - Rechaza IPs no autorizadas             │     │   │ │
│  │  │  └─────────────────────────────────────────────┘     │   │ │
│  │  │  ┌─────────────────────────────────────────────┐     │   │ │
│  │  │  │  3. Basic Authentication (Opcional)          │     │   │ │
│  │  │  │     - Valida usuario/contraseña              │     │   │ │
│  │  │  │     - Password: 12+ chars, compleja          │     │   │ │
│  │  │  └─────────────────────────────────────────────┘     │   │ │
│  │  └──────────────────────────────────────────────────────┘   │ │
│  │                              │                                │ │
│  │  ┌──────────────────────────▼───────────────────────────┐   │ │
│  │  │         SOAP Endpoint                                 │   │ │
│  │  │         /RINunaSolaVia.svc                           │   │ │
│  │  │                                                       │   │ │
│  │  │  ┌─────────────────────────────────────────────┐    │   │ │
│  │  │  │  RINunaSolaViaService                        │    │   │ │
│  │  │  │  (Implementación SOAP)                       │    │   │ │
│  │  │  │                                              │    │   │ │
│  │  │  │  ┌────────────────────────────────────┐    │    │   │ │
│  │  │  │  │ VerificarEstadoWebService()        │    │    │   │ │
│  │  │  │  │  - Verificación de disponibilidad  │    │    │   │ │
│  │  │  │  │  - Respuesta rápida                │    │    │   │ │
│  │  │  │  └────────────────────────────────────┘    │    │   │ │
│  │  │  │                                              │    │   │ │
│  │  │  │  ┌────────────────────────────────────┐    │    │   │ │
│  │  │  │  │ RegistrarPagoIFX()                 │    │    │   │ │
│  │  │  │  │  - Parse mensaje IFX                │    │    │   │ │
│  │  │  │  │  - Validaciones                    │    │    │   │ │
│  │  │  │  │  - Procesamiento de pago           │    │    │   │ │
│  │  │  │  │  - Respuesta IFX                   │    │    │   │ │
│  │  │  │  │  - SLA: < 2 segundos               │    │    │   │ │
│  │  │  │  └────────────────────────────────────┘    │    │   │ │
│  │  │  └─────────────────────────────────────────────┘    │   │ │
│  │  └──────────────────────────────────────────────────────┘   │ │
│  │                              │                                │ │
│  │  ┌──────────────────────────▼───────────────────────────┐   │ │
│  │  │         Business Logic Layer                          │   │ │
│  │  │                                                       │   │ │
│  │  │  ┌─────────────────────────────────────────────┐    │   │ │
│  │  │  │  IFXMessageProcessor                         │    │   │ │
│  │  │  │  - Parse XML IFX                             │    │   │ │
│  │  │  │  - Generar respuestas IFX                    │    │   │ │
│  │  │  └─────────────────────────────────────────────┘    │   │ │
│  │  │                                                       │   │ │
│  │  │  ┌─────────────────────────────────────────────┐    │   │ │
│  │  │  │  PagoProcessor                               │    │   │ │
│  │  │  │  - Validar referencia                        │    │   │ │
│  │  │  │  - Detectar duplicados                       │    │   │ │
│  │  │  │  - Procesar lógica de negocio                │    │   │ │
│  │  │  │  - Registrar pago                            │    │   │ │
│  │  │  └─────────────────────────────────────────────┘    │   │ │
│  │  │                                                       │   │ │
│  │  │  ┌─────────────────────────────────────────────┐    │   │ │
│  │  │  │  TransactionLogger                           │    │   │ │
│  │  │  │  - Logging estructurado (Serilog)            │    │   │ │
│  │  │  │  - Auditoría completa                        │    │   │ │
│  │  │  │  - Trazabilidad por TransaccionId            │    │   │ │
│  │  │  └─────────────────────────────────────────────┘    │   │ │
│  │  └──────────────────────────────────────────────────────┘   │ │
│  │                              │                                │ │
│  └──────────────────────────────┼────────────────────────────────┘ │
│                                 │                                   │
│  ┌──────────────────────────────▼────────────────────────────────┐ │
│  │                   Data Layer                                   │ │
│  │                                                                │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │ │
│  │  │  SQL Server  │  │   PostgreSQL │  │    MongoDB   │       │ │
│  │  │  (Principal) │  │   (Opción)   │  │   (Opción)   │       │ │
│  │  │              │  │              │  │              │       │ │
│  │  │  - Pagos     │  └──────────────┘  └──────────────┘       │ │
│  │  │  - Facturas  │                                            │ │
│  │  │  - Auditoría │                                            │ │
│  │  └──────────────┘                                            │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │                   Logging & Monitoring                         │ │
│  │                                                                │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │ │
│  │  │  Serilog     │  │  Log Files   │  │  Monitoring  │       │ │
│  │  │  (Logger)    │  │  (Rotación)  │  │  (Alertas)   │       │ │
│  │  │              │  │              │  │              │       │ │
│  │  │  - Info      │  │  - Diarios   │  │  - Performance│      │ │
│  │  │  - Warning   │  │  - 30 días   │  │  - Errores   │       │ │
│  │  │  - Error     │  │  - Auditoría │  │  - Seguridad │       │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘       │ │
│  └───────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

## Flujo de Procesamiento de Pago

```
1. BANCOLOMBIA
   │
   ├─► Genera mensaje IFX con datos del pago
   │   - Transacción ID
   │   - Monto
   │   - Referencia
   │   - Datos del pagador
   │
   └─► Envía mensaje SOAP HTTPS
       │
       ▼
2. FIREWALL / NAT
   │
   ├─► Valida IP origen (Whitelist)
   │   ✓ IP autorizada → Continúa
   │   ✗ IP no autorizada → 403 Forbidden
   │
   └─► Redirige a servidor
       │
       ▼
3. REVERSE PROXY (IIS/Nginx)
   │
   ├─► Terminación SSL
   │   - Valida certificado
   │   - Desencripta HTTPS
   │
   └─► Redirige a aplicación
       │
       ▼
4. SECURITY MIDDLEWARE
   │
   ├─► HTTPS Redirection
   │   - Fuerza HTTPS
   │
   ├─► IP Whitelist
   │   - Segunda validación de IP
   │   ✓ Autorizada → Continúa
   │   ✗ No autorizada → 403
   │
   ├─► Basic Authentication (si está habilitada)
   │   - Valida usuario/password
   │   ✓ Válido → Continúa
   │   ✗ Inválido → 401
   │
   └─► Continúa al endpoint
       │
       ▼
5. SOAP ENDPOINT
   │
   └─► RINunaSolaViaService.RegistrarPagoIFX()
       │
       ▼
6. VALIDACIONES
   │
   ├─► Mensaje IFX no vacío
   ├─► Parse mensaje IFX (XML)
   ├─► Validar formato IFX
   │
   └─► Log auditoría: INICIADO
       │
       ▼
7. PROCESAMIENTO
   │
   ├─► IFXMessageProcessor.ParsePagoRequest()
   │   - Extrae datos del XML IFX
   │   - Crea objeto IFXPagoRequest
   │
   ├─► PagoProcessor.ProcesarPago()
   │   │
   │   ├─► Validar referencia en BD
   │   │   ✗ No existe → Error 200
   │   │
   │   ├─► Verificar duplicados
   │   │   ✗ Duplicado → Error 400
   │   │
   │   ├─► Validar monto
   │   │   ✗ Inválido → Error 300
   │   │
   │   ├─► Registrar pago en BD
   │   │
   │   ├─► Actualizar estado factura
   │   │
   │   └─► Generar referencia interna
   │
   └─► Generar respuesta IFX
       │
       ▼
8. RESPUESTA
   │
   ├─► Éxito:
   │   - IFXMessageProcessor.GenerateSuccessResponse()
   │   - Código: 0
   │   - Mensaje: "Pago registrado exitosamente"
   │   - Referencia interna
   │
   ├─► Error:
   │   - IFXMessageProcessor.GenerateErrorResponse()
   │   - Código de error apropiado
   │   - Mensaje descriptivo
   │
   └─► Log auditoría: EXITOSO / ERROR
       │
       ▼
9. LOGGING
   │
   ├─► TransactionLogger
   │   - [AUDITORIA] RegistroPago
   │   - TransaccionId
   │   - Estado
   │   - FechaHora
   │   - Detalle completo
   │
   ├─► Performance Log
   │   - Tiempo de respuesta
   │   - Advertencia si > 1800ms
   │
   └─► Archivo de log diario
       │
       ▼
10. RESPUESTA A BANCOLOMBIA
    │
    └─► Mensaje SOAP con IFX encapsulado
        - Estado OK/ERROR
        - Mensaje IFX en CDATA
        - Tiempo total: < 2 segundos
```

## Componentes y Responsabilidades

### Capa de Seguridad
| Componente | Responsabilidad | Tipo |
|------------|-----------------|------|
| HTTPS | Cifrado de comunicación | Mandatorio |
| IP Whitelist | Control de acceso por IP | Recomendado |
| Basic Auth | Autenticación adicional | Opcional |
| Firewall | Filtrado de tráfico | Recomendado |

### Capa de Aplicación
| Componente | Responsabilidad | Ubicación |
|------------|-----------------|-----------|
| IRINunaSolaViaService | Contrato SOAP | Contracts |
| RINunaSolaViaService | Implementación SOAP | Services |
| IFXMessageProcessor | Parsing/generación IFX | Contracts/IFX |
| PagoProcessor | Lógica de negocio | Services |
| TransactionLogger | Auditoría | Services |

### Capa de Datos
| Componente | Responsabilidad | Tipo |
|------------|-----------------|------|
| Base de Datos | Persistencia | SQL/NoSQL |
| Logs | Auditoría/Debug | Archivos |
| Cache | Performance (opcional) | Redis/Memory |

## Capas de Seguridad

```
┌────────────────────────────────────────┐
│  Capa 1: Red (Firewall + NAT)         │  ◄── IP Whitelisting
└────────────────┬───────────────────────┘
                 │
┌────────────────▼───────────────────────┐
│  Capa 2: Transporte (HTTPS/TLS)       │  ◄── Cifrado, Certificado
└────────────────┬───────────────────────┘
                 │
┌────────────────▼───────────────────────┐
│  Capa 3: Aplicación (Middleware)      │  ◄── IP Whitelist + Basic Auth
└────────────────┬───────────────────────┘
                 │
┌────────────────▼───────────────────────┐
│  Capa 4: Lógica (Validaciones)        │  ◄── Validación de datos
└────────────────────────────────────────┘
```

## Monitoreo y Métricas

### Métricas Clave
- **Performance**: Tiempo de respuesta por transacción
- **Disponibilidad**: Uptime del servicio
- **Errores**: Tasa de error por tipo
- **Seguridad**: Intentos de acceso no autorizados
- **Volumen**: Número de pagos procesados

### Alertas Críticas
| Alerta | Umbral | Acción |
|--------|--------|--------|
| Response Time | > 1800ms | Warning |
| Response Time | > 2000ms | Critical |
| Error Rate | > 5% | Warning |
| Error Rate | > 10% | Critical |
| Unauthorized Access | > 10/hora | Warning |
| Service Down | > 1 min | Critical |

---

**Nota**: Este diagrama representa la arquitectura completa. Para desarrollo local, solo se requieren los componentes de la aplicación (ASP.NET Core + Base de datos).
