// ============================================================================
// RINunaSolaViaService.cs - Implementación principal del servicio SOAP
// ============================================================================
// Este archivo contiene la lógica de los 2 métodos del contrato:
//   1. VerificarEstadoWebService: Bancolombia lo llama periódicamente para
//      verificar que el servicio está arriba. Responde con SignonRs.
//   2. RegistrarPagoIFX: Bancolombia lo llama cada vez que un cliente paga.
//      Recibe el XML IFX con datos del pago, lo procesa, y responde con
//      estructura IFX completa (éxito o error). SLA: máximo 2 segundos.
//
// IMPORTANTE PARA EL ERP:
// - El método ProcesarPago en PagoProcessor es donde se debe implementar la
//   lógica de negocio real: validar factura, registrar pago en BD, etc.
// - Las respuestas SIEMPRE deben tener la estructura IFX completa, tanto
//   en éxito como en error (BuildFullResponse lo garantiza).
// ============================================================================

using BancolombiaWebService.Contracts;
using BancolombiaWebService.Contracts.IFX;
using System.Diagnostics;

namespace BancolombiaWebService.Services
{
    public class RINunaSolaViaService : IRINunaSolaViaService
    {
        private readonly ILogger<RINunaSolaViaService> _logger;
        private readonly ITransactionLogger _transactionLogger;
        private readonly IPagoProcessor _pagoProcessor;

        // Identidad del cliente recaudador en las respuestas IFX.
        // Spec sec. 3.1.1.1.2 campos #5-7: "Nombre del cliente", "Aplicación del cliente", "Versión".
        // IMPORTANTE: En el Response, ClientApp identifica al RECAUDADOR (Inklusiva), NO a Bancolombia.
        // Bancolombia se identifica en el Request; nosotros nos identificamos en el Response.
        private const string RecaudadorOrg = "Inklusiva";
        private const string RecaudadorApp = "WebService";
        private const string RecaudadorVersion = "1.0";

        public RINunaSolaViaService(
            ILogger<RINunaSolaViaService> logger,
            ITransactionLogger transactionLogger,
            IPagoProcessor pagoProcessor)
        {
            _logger = logger;
            _transactionLogger = transactionLogger;
            _pagoProcessor = pagoProcessor;
        }

        public VerificarEstadoResponse VerificarEstadoWebService(VerificarEstadoRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("O1. Verificacion de estado recibida");

            try
            {
                var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                // Spec: ClientApp del response es la identidad del RECAUDADOR, no de Bancolombia
                var ifxResponse = new VerificarEstadoIFXResponse
                {
                    SignonRs = new SignonRs
                    {
                        ClientDt = now,
                        CustLangPref = IFXConstants.DefaultLanguage,
                        ClientApp = new ClientApp
                        {
                            Org = RecaudadorOrg,
                            Name = RecaudadorApp,
                            Version = RecaudadorVersion
                        },
                        ServerDt = now,
                        Language = IFXConstants.DefaultLanguage
                    }
                };

                stopwatch.Stop();
                _logger.LogInformation("Verificacion completada en {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new VerificarEstadoResponse
                {
                    ResultadoIFX = IFXMessageProcessor.GenerateVerificarEstadoResponse(ifxResponse)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando respuesta de verificacion");
                return new VerificarEstadoResponse { ResultadoIFX = "<Error>Error interno</Error>" };
            }
        }

        public RegistrarPagoResponse RegistrarPagoIFX(RegistrarPagoRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var tempId = Guid.NewGuid().ToString();
            _logger.LogInformation("02. Recepcion de pago iniciada. Request is null? {IsNull}", request == null);

            string transactionId = tempId;

            if (request == null)
            {
                _logger.LogError("Request object is null");
                return BuildFullResponse(tempId, string.Empty, IFXStatusCodes.ErrorTecnico, "Request vacio", null);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(request.InfoPago))
                {
                    _logger.LogWarning("Recibido InfoPago vacio");
                    return BuildFullResponse(transactionId, string.Empty, IFXStatusCodes.ErrorTecnico, "Mensaje vacio", null);
                }

                RegistrarPagoIFXRequest ifxRequest;
                try
                {
                    ifxRequest = IFXMessageProcessor.ParseRegistrarPagoRequest(request.InfoPago);
                    transactionId = ifxRequest.PaySvcRq?.RqUID ?? tempId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parseando XML IFX");
                    return BuildFullResponse(transactionId, string.Empty, IFXStatusCodes.ErrorTecnico, "XML Malformado", null);
                }

                _logger.LogInformation("Procesando Pago RqUID: {RqUID}", transactionId);

                var processingResult = _pagoProcessor.ProcesarPago(ifxRequest, transactionId);

                stopwatch.Stop();
                _logger.LogInformation("Pago procesado en {ElapsedMilliseconds}ms. Resultado: {StatusCode}",
                    stopwatch.ElapsedMilliseconds, processingResult.StatusCode);

                return BuildFullResponse(
                    ifxRequest.PaySvcRq?.RqUID ?? transactionId,
                    ifxRequest.PaySvcRq?.SPName ?? string.Empty,
                    processingResult.StatusCode,
                    processingResult.Message,
                    ifxRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado procesando pago");
                return BuildFullResponse(transactionId, string.Empty, IFXStatusCodes.ErrorTecnico, "Error tecnico interno", null);
            }
        }

        /// <summary>
        /// Construye una respuesta IFX completa conforme a la spec de Bancolombia.
        /// Tanto exito como error usan la misma estructura completa.
        /// </summary>
        private RegistrarPagoResponse BuildFullResponse(
            string rqUid,
            string spName,
            string statusCode,
            string statusDesc,
            RegistrarPagoIFXRequest? originalRequest)
        {
            var now = DateTime.Now;
            var nowStr = now.ToString("yyyy-MM-ddTHH:mm:ss");
            var todayStr = now.ToString("yyyy-MM-dd");

            // Datos del request original si disponibles
            var clientDt = originalRequest?.SignonRq?.ClientDt ?? nowStr;
            var remitInfo = originalRequest?.PaySvcRq?.PmtAddRq?.PmtInfo?.RemitInfo ?? new RemitInfo();
            var curAmt = originalRequest?.PaySvcRq?.PmtAddRq?.PmtInfo?.CurAmt ?? new CurAmt();
            var prcDt = originalRequest?.PaySvcRq?.PmtAddRq?.PmtInfo?.PrcDt ?? todayStr;

            // Severity segun spec: 0 y -001 = Info, el resto = Error
            var severity = (statusCode == IFXStatusCodes.Success || statusCode == IFXStatusCodes.FacturaNoExiste)
                ? IFXSeverity.Info
                : IFXSeverity.Error;

            // PmtStatusCode segun spec: Processed/Cancelled/Failed
            var pmtStatusCode = IFXPmtStatusCodes.FromStatusCode(statusCode);

            // PmtId: numerico de hasta 12 digitos (spec: [0-9]{12})
            var pmtId = now.ToString("yyyyMMddHHmm");

            // RemitInfo para response (solo campos requeridos por spec)
            var responseRemitInfo = new RemitInfo
            {
                CustPayeeId = remitInfo.CustPayeeId,
                BillId = remitInfo.BillId,
                BillRefInfo = remitInfo.BillRefInfo,
                CurAmt = new CurAmt { Amt = curAmt.Amt }
            };

            var ifxResponse = new RegistrarPagoIFXResponse
            {
                SignonRs = new SignonRs
                {
                    ClientDt = clientDt,
                    CustLangPref = IFXConstants.DefaultLanguage,
                    ClientApp = new ClientApp
                    {
                        Org = RecaudadorOrg,
                        Name = RecaudadorApp,
                        Version = RecaudadorVersion
                    },
                    ServerDt = nowStr,
                    Language = IFXConstants.DefaultLanguage
                },
                PaySvcRs = new PaySvcRs
                {
                    RqUID = rqUid,
                    SPName = spName,
                    PmtAddRs = new PmtAddRs
                    {
                        Status = new Status
                        {
                            StatusCode = statusCode,
                            Severity = severity,
                            StatusDesc = statusDesc
                        },
                        RqUID = rqUid,
                        AsyncRqUID = rqUid,
                        // PmtAddRs/PmtInfo (primera seccion)
                        PmtInfo = new PmtInfo
                        {
                            RemitInfo = responseRemitInfo,
                            DepAcctIdFrom = new DepAcctIdFrom(),
                            PrcDt = prcDt
                        },
                        // PmtAddRs/PmtRec (segunda seccion con PmtInfo/DueDt)
                        PmtRec = new PmtRec
                        {
                            PmtId = pmtId,
                            PmtInfo = new PmtInfo
                            {
                                RemitInfo = responseRemitInfo,
                                DepAcctIdFrom = new DepAcctIdFrom(),
                                DueDt = todayStr
                            },
                            PmtStatus = new PmtStatus
                            {
                                PmtStatusCode = pmtStatusCode,
                                EffDt = nowStr
                            }
                        }
                    }
                }
            };

            return new RegistrarPagoResponse
            {
                ResultadoIFX = IFXMessageProcessor.GenerateRegistrarPagoResponse(ifxResponse)
            };
        }
    }
}
