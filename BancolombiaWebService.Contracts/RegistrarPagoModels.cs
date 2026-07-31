using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace BancolombiaWebService.Contracts
{
    /// <summary>
    /// Solicitud para registrar un pago notificado por Bancolombia.
    /// El contenido IFX debe viajar dentro de una sección CDATA en el XML.
    /// </summary>
    [MessageContract(WrapperName = "RegistrarPagoIFX", WrapperNamespace = "http://tempuri.org/")]
    public class RegistrarPagoRequest
    {
        [MessageBodyMember(Name = "infoPago", Namespace = "http://tempuri.org/", Order = 1)]
        [XmlElement(ElementName = "infoPago", Namespace = "http://tempuri.org/")]
        public string InfoPago { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta al registro de pago.
    /// La respuesta también debe contener un mensaje IFX en formato CDATA.
    /// </summary>
    [MessageContract(WrapperName = "RegistrarPagoResponse", WrapperNamespace = "http://tempuri.org/")]
    public class RegistrarPagoResponse
    {
        [MessageBodyMember(Name = "RegistrarPagoResult", Order = 1)]
        [XmlElement(ElementName = "RegistrarPagoResult", Namespace = "http://tempuri.org/")]
        public string ResultadoIFX { get; set; } = string.Empty;
    }
}
