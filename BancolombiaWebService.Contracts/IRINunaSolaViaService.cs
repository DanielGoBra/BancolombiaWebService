using System.ServiceModel;

namespace BancolombiaWebService.Contracts
{
    /// <summary>
    /// Contrato de servicio principal para Bancolombia.
    /// Define los dos métodos requeridos: VerificarEstadoWebService y RegistrarPagoIFX.
    /// </summary>
    [ServiceContract(Namespace = "http://tempuri.org/")]
    public interface IRINunaSolaViaService
    {
        /// <summary>
        /// Método usado por Bancolombia para verificar periódicamente el estado del servicio.
        /// Debe responder rápidamente para confirmar disponibilidad.
        /// </summary>
        /// <param name="request">Solicitud de verificación de estado</param>
        /// <returns>Respuesta indicando estado operativo</returns>
        [OperationContract(Action = "http://tempuri.org/VerificarEstadoWebService")]
        VerificarEstadoResponse VerificarEstadoWebService(VerificarEstadoRequest request);

        /// <summary>
        /// Método principal para registrar pagos notificados por Bancolombia en tiempo real.
        /// El mensaje IFX viene encapsulado como CDATA dentro del Request.
        /// DEBE responder en máximo 2 segundos (SLA crítico).
        /// </summary>
        /// <param name="request">Solicitud con datos del pago en formato IFX</param>
        /// <returns>Respuesta con resultado del registro en formato IFX</returns>
        [OperationContract(Action = "http://tempuri.org/RegistrarPagoIFX")]
        RegistrarPagoResponse RegistrarPagoIFX(RegistrarPagoRequest request);
    }
}
