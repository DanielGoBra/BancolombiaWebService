using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;

namespace BancolombiaWebService.Contracts
{
    [MessageContract(WrapperName = "VerificarEstadoWebService", WrapperNamespace = "http://tempuri.org/")]
    public class VerificarEstadoRequest
    {
        /// <summary>
        /// El request de prueba envía 'infoDummy'
        /// </summary>
        [MessageBodyMember(Name = "infoDummy", Namespace = "http://tempuri.org/", Order = 1)]
        [XmlElement(ElementName = "infoDummy", Namespace = "http://tempuri.org/")]
        public string InfoDummy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de verificación del estado del servicio.
    /// </summary>
    [MessageContract(WrapperName = "VerificarEstadoWebServiceResponse", WrapperNamespace = "http://tempuri.org/")]
    public class VerificarEstadoResponse
    {
        [MessageBodyMember(Name = "VerificarEstadoWebServiceResult", Order = 1)]
        public string ResultadoIFX { get; set; } = string.Empty;
    }
}
