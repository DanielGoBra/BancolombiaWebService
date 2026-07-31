// ============================================================================
// IFXModels.cs - Modelos de datos del estandar IFX (Interactive Financial Exchange)
// ============================================================================
// Estos modelos representan la estructura XML que Bancolombia envia y espera
// recibir. Cada clase mapea directamente a un nodo XML del mensaje IFX.
//
// ESTRUCTURA DEL REQUEST (lo que Bancolombia nos envia):
//   <IFX>
//     <SignonRq>           -> SignonRq (datos de sesion: fecha, idioma, app origen)
//     <PaySvcRq>           -> PaySvcRq (contenedor del servicio de pago)
//       <RqUID>            -> Identificador unico de la transaccion (UUID solo digitos)
//       <SPName>           -> Codigo del convenio (15 digitos numericos)
//       <MsgRqHdr>         -> MsgRqHdr (canal, banco, oficina, cajero)
//       <PmtAddRq>         -> PmtAddRq (solicitud de registro de pago)
//         <PmtInfo>        -> PmtInfo (monto, factura, cuenta origen)
//           <CurAmt>       -> Monto y forma de pago (E/C/M/D)
//           <RemitInfo>    -> Datos de la factura (BillId, CustPayeeId, etc.)
//           <DepAcctIdFrom>-> Cuenta origen (vacia en este modelo)
//
// ESTRUCTURA DEL RESPONSE (lo que nosotros respondemos):
//   <IFX>
//     <SignonRs>           -> SignonRs (confirmacion con datos del recaudador)
//     <PaySvcRs>           -> PaySvcRs (resultado del servicio)
//       <PmtAddRs>         -> PmtAddRs (resultado del registro de pago)
//         <Status>         -> Codigo de resultado (0, -001, -002, -005, -006, -099)
//         <PmtInfo>        -> Eco de datos de la factura + PrcDt (fecha contable)
//         <PmtRec>         -> Comprobante de recibo
//           <PmtId>        -> Numero de comprobante (hasta 12 digitos numericos)
//           <PmtInfo>      -> Datos de factura + DueDt (fecha aceptacion pago)
//           <PmtStatus>    -> Estado final: Processed/Cancelled/Failed
// ============================================================================

using System;

namespace BancolombiaWebService.Contracts.IFX
{
    // === SignonRq / SignonRs ===
    // Seccion de autenticacion/sesion presente en TODOS los mensajes IFX.

    /// <summary>
    /// Request de sesion. Bancolombia envia sus datos de identificacion aqui.
    /// ClientDt = fecha/hora de la transaccion, ClientApp = identidad de Bancolombia.
    /// </summary>
    public class SignonRq
    {
        public string ClientDt { get; set; } = string.Empty;       // Fecha y hora real de la transaccion (YYYY-MM-DDTHH:mm:ss)
        public string CustLangPref { get; set; } = string.Empty;   // Valor fijo: "es-CO"
        public ClientApp ClientApp { get; set; } = new ClientApp(); // En request: Bancolombia/RINSV/1.0
    }

    /// <summary>
    /// Response de sesion. Nosotros (el recaudador) respondemos con nuestra identidad.
    /// ServerDt = fecha/hora de nuestro servidor.
    /// </summary>
    public class SignonRs
    {
        public string ClientDt { get; set; } = string.Empty;       // Eco de la fecha del request
        public string CustLangPref { get; set; } = string.Empty;   // Valor fijo: "es-CO"
        public ClientApp ClientApp { get; set; } = new ClientApp(); // En response: Inklusiva/WebService/1.0
        public string ServerDt { get; set; } = string.Empty;       // Fecha y hora de nuestro servidor
        public string Language { get; set; } = string.Empty;       // Valor fijo: "es-CO"
    }

    /// <summary>
    /// Identificacion de la aplicacion. En el Request identifica a Bancolombia,
    /// en el Response identifica al recaudador (Inklusiva).
    /// </summary>
    public class ClientApp
    {
        public string Org { get; set; } = string.Empty;     // Organizacion (ej: "Bancolombia" o "Inklusiva")
        public string Name { get; set; } = string.Empty;    // Nombre app (ej: "RINSV" o "WebService")
        public string Version { get; set; } = string.Empty; // Version (ej: "1.0")
    }

    // === VerificarEstado ===
    // Usado para el metodo de health-check. Solo contiene SignonRq/Rs.

    public class VerificarEstadoIFXRequest
    {
        public SignonRq SignonRq { get; set; } = new SignonRq();
    }

    public class VerificarEstadoIFXResponse
    {
        public SignonRs SignonRs { get; set; } = new SignonRs();
    }

    // === PaySvcRq / PaySvcRs ===
    // Contenedores del servicio de pago (solo en RegistrarPagoIFX).

    /// <summary>
    /// Solicitud del servicio de pago enviada por Bancolombia.
    /// Contiene el identificador de transaccion, codigo de convenio,
    /// datos del canal (MsgRqHdr) y la solicitud de pago (PmtAddRq).
    /// </summary>
    public class PaySvcRq
    {
        public string RqUID { get; set; } = string.Empty;            // Campo #6: UUID solo digitos, 36 caracteres
        public string SPName { get; set; } = string.Empty;           // Campo #7: Codigo del convenio, 15 digitos numericos
        public MsgRqHdr MsgRqHdr { get; set; } = new MsgRqHdr();    // Encabezado con datos del canal
        public PmtAddRq PmtAddRq { get; set; } = new PmtAddRq();    // Solicitud de registro de pago
    }

    /// <summary>
    /// Respuesta del servicio de pago que enviamos a Bancolombia.
    /// Contiene el eco del RqUID, SPName y el resultado del registro (PmtAddRs).
    /// </summary>
    public class PaySvcRs
    {
        public string RqUID { get; set; } = string.Empty;            // Eco del RqUID del request
        public string SPName { get; set; } = string.Empty;           // Eco del SPName del request
        public PmtAddRs PmtAddRs { get; set; } = new PmtAddRs();    // Resultado del pago
    }

    // === MsgRqHdr ===
    // Encabezado del request que indica POR DONDE se hizo el pago.
    // IMPORTANTE: NetworkTrnInfo y PointOfServiceData son hijos DIRECTOS de MsgRqHdr.

    public class MsgRqHdr
    {
        public NetworkTrnInfo NetworkTrnInfo { get; set; } = new NetworkTrnInfo();         // Canal y banco
        public PointOfServiceData PointOfServiceData { get; set; } = new PointOfServiceData(); // Oficina y cajero
    }

    /// <summary>
    /// Informacion de la red/canal.
    /// NetworkOwner indica POR CUAL CANAL se hizo el pago:
    ///   02=Caja, 03=Sucursal Virtual, 04=Sucursal Telefonica,
    ///   05=FacturaNet, 07=PAC Electronico, 09=Bancolombia a la Mano
    /// </summary>
    public class NetworkTrnInfo
    {
        public string NetworkOwner { get; set; } = string.Empty; // Campo #8: Codigo del canal (02, 03, 04, 05, 07, 09)
        public string BankId { get; set; } = string.Empty;       // Campo #9: Codigo entidad bancaria, 4 digitos
    }

    /// <summary>
    /// Datos del punto de servicio (oficina/cajero).
    /// Para canales electronicos estos campos pueden venir vacios.
    /// </summary>
    public class PointOfServiceData
    {
        public string Environment { get; set; } = string.Empty;        // Campo #10: Siempre vacio
        public POSAgent POSAgent { get; set; } = new POSAgent();       // Nodo hijo con AgentId
        public string POSLocation { get; set; } = string.Empty;        // Campo #11: Codigo oficina, 5 digitos
    }

    public class POSAgent
    {
        public string AgentId { get; set; } = string.Empty; // Campo #12: Codigo del cajero, 4 digitos
    }

    // === PmtAddRq / PmtAddRs ===
    // Solicitud y respuesta del registro de pago.

    /// <summary>
    /// Solicitud de registro de pago dentro del request IFX.
    /// Tiene su propio RqUID (campo #13) ademas del RqUID de PaySvcRq.
    /// </summary>
    public class PmtAddRq
    {
        public string RqUID { get; set; } = string.Empty;         // Campo #13: UUID propio de PmtAddRq
        public PmtInfo PmtInfo { get; set; } = new PmtInfo();     // Datos del pago
    }

    /// <summary>
    /// Respuesta al registro de pago. Contiene:
    /// - Status: resultado de la operacion (0=exito, -001=no existe, etc.)
    /// - PmtInfo: eco de datos de la factura con PrcDt (fecha contable)
    /// - PmtRec: comprobante de recibo con PmtId y PmtStatus
    /// </summary>
    public class PmtAddRs
    {
        public Status Status { get; set; } = new Status();         // Resultado de la operacion
        public string RqUID { get; set; } = string.Empty;          // Eco del RqUID
        public string AsyncRqUID { get; set; } = string.Empty;     // Campo #11: Identificador alternativo
        public PmtInfo PmtInfo { get; set; } = new PmtInfo();      // Primera seccion PmtInfo (con PrcDt)
        public PmtRec PmtRec { get; set; } = new PmtRec();        // Comprobante de recibo
    }

    // === PmtInfo ===
    // Informacion del pago: monto, factura, cuenta origen, fechas.
    // Se usa tanto en el request como en DOS secciones del response.

    /// <summary>
    /// Informacion del pago. Campos clave para el ERP:
    /// - CurAmt: monto pagado y forma de pago
    /// - RemitInfo: datos de la factura (BillId es la referencia principal)
    /// - PrcDt: fecha contable (en PmtAddRs/PmtInfo)
    /// - DueDt: fecha de aceptacion del pago (en PmtRec/PmtInfo)
    /// </summary>
    public class PmtInfo
    {
        public CurAmt CurAmt { get; set; } = new CurAmt();                 // Monto y forma de pago
        public RemitInfo RemitInfo { get; set; } = new RemitInfo();         // Datos de la factura
        public DepAcctIdFrom DepAcctIdFrom { get; set; } = new DepAcctIdFrom(); // Cuenta origen (vacia)
        public string PrcDt { get; set; } = string.Empty;                  // Fecha contable (YYYY-MM-DD)
        public string DueDt { get; set; } = string.Empty;                  // Fecha aceptacion pago (YYYY-MM-DD)
    }

    /// <summary>
    /// Monto de la transaccion.
    /// Amt: valor con 2 decimales, separador punto. Ej: "150000.00"
    /// CurCode: forma de pago - E(Efectivo), C(Cheque), M(Mixto), D(Debito a Cuenta)
    /// </summary>
    public class CurAmt
    {
        public string Amt { get; set; } = string.Empty;     // Valor pagado con 2 decimales
        public string CurCode { get; set; } = string.Empty; // E=Efectivo, C=Cheque, M=Mixto, D=Debito
    }

    /// <summary>
    /// Informacion de remision/factura. CAMPOS CLAVE PARA EL ERP:
    /// - CustPayeeId: NIT del pagador (15 digitos)
    /// - BillId: Numero de factura en formato UUID solo digitos (REFERENCIA PRINCIPAL)
    /// - BillRefInfo: Referencia 2 (12 digitos)
    /// - PmtId: Identificador del grupo/comprobante (14 digitos)
    /// - CurAmt: Monto pagado (solo Amt, sin CurCode en RemitInfo)
    /// </summary>
    public class RemitInfo
    {
        public string CustPayeeId { get; set; } = string.Empty; // Campo #17: NIT del pagador, 15 digitos
        public string BillId { get; set; } = string.Empty;       // Campo #18: Numero factura, UUID solo digitos
        public string BillRefInfo { get; set; } = string.Empty;  // Campo #19: Referencia 2, 12 digitos
        public string PmtId { get; set; } = string.Empty;        // Campo #20: ID del grupo, 14 digitos
        public CurAmt CurAmt { get; set; } = new CurAmt();       // Campo #21: Valor pagado
    }

    /// <summary>
    /// Cuenta de origen del pago. Bancolombia envia estos campos vacios.
    /// AcctType siempre tiene valor "Default".
    /// </summary>
    public class DepAcctIdFrom
    {
        public string AcctId { get; set; } = string.Empty;       // Campo #22: Siempre vacio
        public string AcctType { get; set; } = "Default";        // Campo #23: Valor fijo "Default"
        public string BankInfo { get; set; } = string.Empty;     // Campo #24: Siempre vacio
    }

    // === Status ===
    // Resultado de la operacion en el response.

    /// <summary>
    /// Estado del resultado. StatusCode determina como Bancolombia procesa la respuesta:
    ///   "0"    -> Exitoso: Bancolombia marca pago como aplicado
    ///   "-001" -> Factura no existe: Bancolombia marca como aplicado (EX)
    ///   "-002" -> Ya pagada: Bancolombia marca como aplicado (EX)
    ///   "-005" -> No realizado: Bancolombia REINTENTA durante el dia (PE)
    ///   "-006" -> Duplicada: Bancolombia marca como aplicado (EX)
    ///   "-099" -> Error tecnico: Bancolombia REINTENTA durante el dia (PE)
    /// </summary>
    public class Status
    {
        public string StatusCode { get; set; } = string.Empty;  // Codigo de resultado
        public string Severity { get; set; } = string.Empty;    // "Info" para 0 y -001, "Error" para el resto
        public string StatusDesc { get; set; } = string.Empty;  // Descripcion del resultado (opcional)
    }

    // === PmtRec ===
    // Comprobante de recibo en el response. Contiene el numero de comprobante,
    // una segunda seccion PmtInfo con DueDt, y el estado final del pago.

    /// <summary>
    /// Registro de comprobante de pago en el response.
    /// PmtId es el numero de comprobante que genera el recaudador.
    /// </summary>
    public class PmtRec
    {
        public string PmtId { get; set; } = string.Empty;              // Campo #23: Comprobante, hasta 12 digitos
        public PmtInfo PmtInfo { get; set; } = new PmtInfo();          // Segunda PmtInfo (con DueDt)
        public PmtStatus PmtStatus { get; set; } = new PmtStatus();    // Estado final del pago
    }

    /// <summary>
    /// Estado final del procesamiento del pago.
    /// PmtStatusCode indica el resultado en texto:
    ///   "Processed" -> Exitoso (cuando StatusCode = 0)
    ///   "Cancelled" -> Operacion fallida sin razon de negocio (StatusCode = -099)
    ///   "Failed"    -> Error de negocio (StatusCode = -001, -002, -005, -006)
    /// </summary>
    public class PmtStatus
    {
        public string PmtStatusCode { get; set; } = string.Empty; // Campo #32: Processed/Cancelled/Failed
        public string EffDt { get; set; } = string.Empty;          // Campo #33: Fecha y hora (YYYY-MM-DDTHH:mm:ss)
    }

    // === Wrappers de nivel superior ===
    // Estos son los objetos raiz que representan el mensaje IFX completo.

    /// <summary>
    /// Mensaje IFX completo del request de RegistrarPago.
    /// Se obtiene parseando el XML CDATA que viene dentro del SOAP.
    /// </summary>
    public class RegistrarPagoIFXRequest
    {
        public SignonRq SignonRq { get; set; } = new SignonRq();
        public PaySvcRq PaySvcRq { get; set; } = new PaySvcRq();
    }

    /// <summary>
    /// Mensaje IFX completo del response de RegistrarPago.
    /// Se serializa a XML y se envia dentro de CDATA en el SOAP response.
    /// </summary>
    public class RegistrarPagoIFXResponse
    {
        public SignonRs SignonRs { get; set; } = new SignonRs();
        public PaySvcRs PaySvcRs { get; set; } = new PaySvcRs();
    }
}
