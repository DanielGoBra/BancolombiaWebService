// ============================================================================
// TransactionLogger.cs - Auditoría de transacciones y Procesador de Pagos
// ============================================================================
// Contiene dos componentes clave:
//
// 1. TransactionLogger: Registra auditoría de cada operación (verificación/pago).
//    Bancolombia REQUIERE log transaccional completo (spec sec. 2.2.1 punto 7).
//
// 2. PagoProcessor: LOGICA DE NEGOCIO para procesar pagos.
//    *** PUNTO DE INTEGRACION CON EL ERP ***
//    Aquí es donde se debe:
//    - Consultar la BD para verificar si la factura (BillId) existe
//    - Verificar si la factura ya fue pagada (StatusCode -002)
//    - Registrar el pago en la BD
//    - Generar el comprobante de recibo (ReferenciaInterna / PmtId)
//    - Retornar el StatusCode apropiado (0=éxito, -001=no existe, etc.)
//
// CODIGOS DE RESPUESTA (spec sec. 3.3):
//   0     -> Exitoso: pago registrado correctamente
//   -001  -> Factura no existe en el sistema
//   -002  -> Factura ya fue pagada anteriormente
//   -005  -> Pago no realizado (error al registrar)
//   -006  -> Transacción duplicada (RqUID ya procesado)
//   -099  -> Error técnico (fallo inesperado)
// ============================================================================

using BancolombiaWebService.Contracts;
using BancolombiaWebService.Contracts.IFX;

namespace BancolombiaWebService.Services
{
    public interface ITransactionLogger
    {
        void LogVerificacion(string? transactionId, string estado, object? data = null);
        void LogPago(string transactionId, string estado, object request, string? detalle = null);
    }

    public class TransactionLogger : ITransactionLogger
    {
        private readonly ILogger<TransactionLogger> _logger;

        public TransactionLogger(ILogger<TransactionLogger> logger)
        {
            _logger = logger;
        }

        public void LogVerificacion(string? transactionId, string estado, object? data = null)
        {
            _logger.LogInformation(
                "[AUDITORIA] Verificacion | TransaccionId: {TransaccionId} | Estado: {Estado} | FechaHora: {FechaHora} | Data: {@Data}",
                transactionId ?? "N/A",
                estado,
                DateTime.Now,
                data);
        }

        public void LogPago(string transactionId, string estado, object request, string? detalle = null)
        {
            _logger.LogInformation(
                "[AUDITORIA] RegistroPago | TransaccionId: {TransaccionId} | Estado: {Estado} | FechaHora: {FechaHora} | Detalle: {Detalle} | Request: {@Request}",
                transactionId,
                estado,
                DateTime.Now,
                detalle ?? "N/A",
                request);
        }
    }

    public interface IPagoProcessor
    {
        PagoProcessingResult ProcesarPago(RegistrarPagoIFXRequest request, string transactionId);
    }

    public class PagoProcessingResult
    {
        public string StatusCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ReferenciaInterna { get; set; }
    }

    /// <summary>
    /// Procesador de pagos - PUNTO PRINCIPAL DE INTEGRACION CON EL ERP/BD.
    /// Actualmente tiene lógica básica de validación. Para producción, aquí se debe:
    /// 1. Inyectar el DbContext o servicio del ERP
    /// 2. Buscar la factura por BillId en la base de datos
    /// 3. Validar que no esté ya pagada
    /// 4. Registrar el pago y generar comprobante
    /// 5. Retornar el código de respuesta apropiado
    /// </summary>
    public class PagoProcessor : IPagoProcessor
    {
        private readonly ILogger<PagoProcessor> _logger;

        public PagoProcessor(ILogger<PagoProcessor> logger)
        {
            _logger = logger;
        }

        public PagoProcessingResult ProcesarPago(RegistrarPagoIFXRequest request, string transactionId)
        {
            try
            {
                var pmtInfo = request.PaySvcRq.PmtAddRq.PmtInfo;
                var remitInfo = pmtInfo.RemitInfo;

                _logger.LogInformation(
                    "Procesando pago - TxnId: {TxnId}, BillId: {BillId}, Monto: {Monto}, CustPayeeId: {CustPayeeId}",
                    transactionId,
                    remitInfo.BillId,
                    pmtInfo.CurAmt.Amt,
                    remitInfo.CustPayeeId);

                if (string.IsNullOrWhiteSpace(remitInfo.BillId))
                {
                    return new PagoProcessingResult
                    {
                        StatusCode = IFXStatusCodes.FacturaNoExiste,
                        Message = IFXStatusDescriptions.FacturaNoExiste
                    };
                }

                if (decimal.TryParse(pmtInfo.CurAmt.Amt, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt <= 0)
                {
                    return new PagoProcessingResult
                    {
                        StatusCode = IFXStatusCodes.ErrorTecnico,
                        Message = "Monto invalido"
                    };
                }

                // Referencia interna numerica (spec: PmtId [0-9]{12})
                var referenciaInterna = DateTime.Now.ToString("yyyyMMddHHmm");

                _logger.LogInformation("Pago procesado exitosamente. Referencia: {Ref}", referenciaInterna);

                return new PagoProcessingResult
                {
                    StatusCode = IFXStatusCodes.Success,
                    Message = IFXStatusDescriptions.Success,
                    ReferenciaInterna = referenciaInterna
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando pago");
                return new PagoProcessingResult
                {
                    StatusCode = IFXStatusCodes.ErrorTecnico,
                    Message = $"Error interno: {ex.Message}"
                };
            }
        }
    }
}
