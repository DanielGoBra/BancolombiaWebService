using System;
using System.Xml.Linq;

namespace BancolombiaWebService.Contracts.IFX
{
    public static class IFXMessageProcessor
    {
        // ===================== PARSING (Request -> Objetos) =====================

        public static VerificarEstadoIFXRequest ParseVerificarEstadoRequest(string mensajeIFX)
        {
            try
            {
                var doc = XDocument.Parse(mensajeIFX);
                var ifxRoot = doc.Root ?? throw new InvalidOperationException("Mensaje IFX vacio");

                return new VerificarEstadoIFXRequest
                {
                    SignonRq = ParseSignonRq(ifxRoot.Element("SignonRq"))
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parseando VerificarEstadoRequest: {ex.Message}", ex);
            }
        }

        public static RegistrarPagoIFXRequest ParseRegistrarPagoRequest(string mensajeIFX)
        {
            try
            {
                var doc = XDocument.Parse(mensajeIFX);
                var ifxRoot = doc.Root ?? throw new InvalidOperationException("Mensaje IFX vacio");

                return new RegistrarPagoIFXRequest
                {
                    SignonRq = ParseSignonRq(ifxRoot.Element("SignonRq")),
                    PaySvcRq = ParsePaySvcRq(ifxRoot.Element("PaySvcRq"))
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parseando RegistrarPagoRequest: {ex.Message}", ex);
            }
        }

        // ===================== GENERACION DE RESPONSE =====================

        public static string GenerateVerificarEstadoResponse(VerificarEstadoIFXResponse response)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("IFX",
                    CreateSignonRsElement(response.SignonRs)
                )
            );

            return doc.ToString(SaveOptions.None);
        }

        public static string GenerateRegistrarPagoResponse(RegistrarPagoIFXResponse response)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("IFX",
                    CreateSignonRsElement(response.SignonRs),
                    CreatePaySvcRsElement(response.PaySvcRs)
                )
            );

            return doc.ToString(SaveOptions.None);
        }

        // ===================== PARSING PRIVADO =====================

        private static SignonRq ParseSignonRq(XElement? signonRqElement)
        {
            if (signonRqElement == null)
                throw new InvalidOperationException("Elemento SignonRq no encontrado");

            return new SignonRq
            {
                ClientDt = signonRqElement.Element("ClientDt")?.Value ?? string.Empty,
                CustLangPref = signonRqElement.Element("CustLangPref")?.Value ?? string.Empty,
                ClientApp = ParseClientApp(signonRqElement.Element("ClientApp"))
            };
        }

        private static ClientApp ParseClientApp(XElement? clientAppElement)
        {
            if (clientAppElement == null)
                return new ClientApp();

            return new ClientApp
            {
                Org = clientAppElement.Element("Org")?.Value ?? string.Empty,
                Name = clientAppElement.Element("Name")?.Value ?? string.Empty,
                Version = clientAppElement.Element("Version")?.Value ?? string.Empty
            };
        }

        private static PaySvcRq ParsePaySvcRq(XElement? paySvcRqElement)
        {
            if (paySvcRqElement == null)
                throw new InvalidOperationException("Elemento PaySvcRq no encontrado");

            return new PaySvcRq
            {
                RqUID = paySvcRqElement.Element("RqUID")?.Value ?? string.Empty,
                SPName = paySvcRqElement.Element("SPName")?.Value ?? string.Empty,
                MsgRqHdr = ParseMsgRqHdr(paySvcRqElement.Element("MsgRqHdr")),
                PmtAddRq = ParsePmtAddRq(paySvcRqElement.Element("PmtAddRq"))
            };
        }

        // Spec: MsgRqHdr contiene NetworkTrnInfo y PointOfServiceData como hijos directos
        private static MsgRqHdr ParseMsgRqHdr(XElement? msgRqHdrElement)
        {
            if (msgRqHdrElement == null)
                return new MsgRqHdr();

            return new MsgRqHdr
            {
                NetworkTrnInfo = ParseNetworkTrnInfo(msgRqHdrElement.Element("NetworkTrnInfo")),
                PointOfServiceData = ParsePointOfServiceData(msgRqHdrElement.Element("PointOfServiceData"))
            };
        }

        // Spec campos #8-9: NetworkOwner y BankId
        private static NetworkTrnInfo ParseNetworkTrnInfo(XElement? networkElement)
        {
            if (networkElement == null)
                return new NetworkTrnInfo();

            return new NetworkTrnInfo
            {
                NetworkOwner = networkElement.Element("NetworkOwner")?.Value ?? string.Empty,
                BankId = networkElement.Element("BankId")?.Value ?? string.Empty
            };
        }

        // Spec campos #10-12: Environment, POSAgent/AgentId, POSLocation
        private static PointOfServiceData ParsePointOfServiceData(XElement? posElement)
        {
            if (posElement == null)
                return new PointOfServiceData();

            return new PointOfServiceData
            {
                Environment = posElement.Element("Environment")?.Value ?? string.Empty,
                POSAgent = ParsePOSAgent(posElement.Element("POSAgent")),
                POSLocation = posElement.Element("POSLocation")?.Value ?? string.Empty
            };
        }

        private static POSAgent ParsePOSAgent(XElement? posAgentElement)
        {
            if (posAgentElement == null)
                return new POSAgent();

            return new POSAgent
            {
                AgentId = posAgentElement.Element("AgentId")?.Value ?? string.Empty
            };
        }

        // Spec campo #13: PmtAddRq tiene su propio RqUID
        private static PmtAddRq ParsePmtAddRq(XElement? pmtAddRqElement)
        {
            if (pmtAddRqElement == null)
                return new PmtAddRq();

            return new PmtAddRq
            {
                RqUID = pmtAddRqElement.Element("RqUID")?.Value ?? string.Empty,
                PmtInfo = ParsePmtInfo(pmtAddRqElement.Element("PmtInfo"))
            };
        }

        private static PmtInfo ParsePmtInfo(XElement? pmtInfoElement)
        {
            if (pmtInfoElement == null)
                return new PmtInfo();

            return new PmtInfo
            {
                CurAmt = ParseCurAmt(pmtInfoElement.Element("CurAmt")),
                RemitInfo = ParseRemitInfo(pmtInfoElement.Element("RemitInfo")),
                DepAcctIdFrom = ParseDepAcctIdFrom(pmtInfoElement.Element("DepAcctIdFrom")),
                PrcDt = pmtInfoElement.Element("PrcDt")?.Value ?? string.Empty
            };
        }

        private static CurAmt ParseCurAmt(XElement? curAmtElement)
        {
            if (curAmtElement == null)
                return new CurAmt();

            return new CurAmt
            {
                Amt = curAmtElement.Element("Amt")?.Value ?? string.Empty,
                CurCode = curAmtElement.Element("CurCode")?.Value ?? string.Empty
            };
        }

        // Spec campos #17-21: CustPayeeId, BillId, BillRefInfo, PmtId, CurAmt
        private static RemitInfo ParseRemitInfo(XElement? remitInfoElement)
        {
            if (remitInfoElement == null)
                return new RemitInfo();

            return new RemitInfo
            {
                CustPayeeId = remitInfoElement.Element("CustPayeeId")?.Value ?? string.Empty,
                BillId = remitInfoElement.Element("BillId")?.Value ?? string.Empty,
                BillRefInfo = remitInfoElement.Element("BillRefInfo")?.Value ?? string.Empty,
                PmtId = remitInfoElement.Element("PmtId")?.Value ?? string.Empty,
                CurAmt = ParseCurAmt(remitInfoElement.Element("CurAmt"))
            };
        }

        // Spec campos #22-24: AcctId, AcctType, BankInfo
        private static DepAcctIdFrom ParseDepAcctIdFrom(XElement? depAcctElement)
        {
            if (depAcctElement == null)
                return new DepAcctIdFrom();

            return new DepAcctIdFrom
            {
                AcctId = depAcctElement.Element("AcctId")?.Value ?? string.Empty,
                AcctType = depAcctElement.Element("AcctType")?.Value ?? "Default",
                BankInfo = depAcctElement.Element("BankInfo")?.Value ?? string.Empty
            };
        }

        // ===================== GENERACION DE ELEMENTOS XML =====================

        // Spec orden: ClientDt, CustLangPref, ClientApp, ServerDt, Language
        private static XElement CreateSignonRsElement(SignonRs signonRs)
        {
            return new XElement("SignonRs",
                new XElement("ClientDt", signonRs.ClientDt),
                new XElement("CustLangPref", signonRs.CustLangPref),
                CreateClientAppElement(signonRs.ClientApp),
                new XElement("ServerDt", signonRs.ServerDt),
                new XElement("Language", signonRs.Language)
            );
        }

        private static XElement CreateClientAppElement(ClientApp clientApp)
        {
            return new XElement("ClientApp",
                new XElement("Org", clientApp.Org),
                new XElement("Name", clientApp.Name),
                new XElement("Version", clientApp.Version)
            );
        }

        // Spec: PaySvcRs contiene RqUID, SPName, PmtAddRs (NO MsgRsHdr)
        private static XElement CreatePaySvcRsElement(PaySvcRs paySvcRs)
        {
            return new XElement("PaySvcRs",
                new XElement("RqUID", paySvcRs.RqUID),
                new XElement("SPName", paySvcRs.SPName),
                CreatePmtAddRsElement(paySvcRs.PmtAddRs)
            );
        }

        // Spec: PmtAddRs contiene Status, RqUID, AsyncRqUID, PmtInfo, PmtRec
        private static XElement CreatePmtAddRsElement(PmtAddRs pmtAddRs)
        {
            return new XElement("PmtAddRs",
                CreateStatusElement(pmtAddRs.Status),
                new XElement("RqUID", pmtAddRs.RqUID),
                new XElement("AsyncRqUID", pmtAddRs.AsyncRqUID),
                CreateResponsePmtInfoElement(pmtAddRs.PmtInfo),
                CreatePmtRecElement(pmtAddRs.PmtRec)
            );
        }

        private static XElement CreateStatusElement(Status status)
        {
            var element = new XElement("Status",
                new XElement("StatusCode", status.StatusCode),
                new XElement("Severity", status.Severity)
            );

            if (!string.IsNullOrEmpty(status.StatusDesc))
                element.Add(new XElement("StatusDesc", status.StatusDesc));

            return element;
        }

        // PmtInfo del response (PmtAddRs/PmtInfo): RemitInfo, DepAcctIdFrom, PrcDt
        private static XElement CreateResponsePmtInfoElement(PmtInfo pmtInfo)
        {
            return new XElement("PmtInfo",
                CreateResponseRemitInfoElement(pmtInfo.RemitInfo),
                CreateDepAcctIdFromElement(pmtInfo.DepAcctIdFrom),
                new XElement("PrcDt", pmtInfo.PrcDt)
            );
        }

        // PmtRec spec: PmtId, PmtInfo (con DueDt), PmtStatus
        private static XElement CreatePmtRecElement(PmtRec pmtRec)
        {
            return new XElement("PmtRec",
                new XElement("PmtId", pmtRec.PmtId),
                CreatePmtRecPmtInfoElement(pmtRec.PmtInfo),
                CreatePmtStatusElement(pmtRec.PmtStatus)
            );
        }

        // PmtInfo dentro de PmtRec: RemitInfo, DepAcctIdFrom, DueDt
        private static XElement CreatePmtRecPmtInfoElement(PmtInfo pmtInfo)
        {
            return new XElement("PmtInfo",
                CreateResponseRemitInfoElement(pmtInfo.RemitInfo),
                CreateDepAcctIdFromElement(pmtInfo.DepAcctIdFrom),
                new XElement("DueDt", pmtInfo.DueDt)
            );
        }

        // Spec: PmtStatus con PmtStatusCode y EffDt
        private static XElement CreatePmtStatusElement(PmtStatus pmtStatus)
        {
            return new XElement("PmtStatus",
                new XElement("PmtStatusCode", pmtStatus.PmtStatusCode),
                new XElement("EffDt", pmtStatus.EffDt)
            );
        }

        // Response RemitInfo: solo CustPayeeId, BillId, BillRefInfo, CurAmt/Amt
        private static XElement CreateResponseRemitInfoElement(RemitInfo remitInfo)
        {
            return new XElement("RemitInfo",
                new XElement("CustPayeeId", remitInfo.CustPayeeId),
                new XElement("BillId", remitInfo.BillId),
                new XElement("BillRefInfo", remitInfo.BillRefInfo),
                new XElement("CurAmt",
                    new XElement("Amt", remitInfo.CurAmt.Amt)
                )
            );
        }

        // Spec campos #22-24 / #20-22 / #29-31
        private static XElement CreateDepAcctIdFromElement(DepAcctIdFrom depAcct)
        {
            return new XElement("DepAcctIdFrom",
                new XElement("AcctId", depAcct.AcctId),
                new XElement("AcctType", depAcct.AcctType),
                new XElement("BankInfo", depAcct.BankInfo)
            );
        }
    }
}
