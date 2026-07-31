using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BancolombiaWebService.Contracts.IFX
{
    public static class IFXValidator
    {
        // Spec: UUID solo digitos [0-9], no hexadecimal
        private const string UuidPattern = @"^[0-9]{8}(-[0-9]{4}){3}-[0-9]{12}$";

        public static ValidationResult ValidateVerificarEstadoRequest(VerificarEstadoIFXRequest request)
        {
            var result = new ValidationResult();

            if (request == null)
            {
                result.AddError("Request no puede ser nulo");
                return result;
            }

            ValidateSignonRq(request.SignonRq, result);

            return result;
        }

        public static ValidationResult ValidateRegistrarPagoRequest(RegistrarPagoIFXRequest request)
        {
            var result = new ValidationResult();

            if (request == null)
            {
                result.AddError("Request no puede ser nulo");
                return result;
            }

            ValidateSignonRq(request.SignonRq, result);
            ValidatePaySvcRq(request.PaySvcRq, result);

            return result;
        }

        private static void ValidateSignonRq(SignonRq signonRq, ValidationResult result)
        {
            if (signonRq == null)
            {
                result.AddError("SignonRq no puede ser nulo");
                return;
            }

            if (string.IsNullOrWhiteSpace(signonRq.ClientDt))
                result.AddError("ClientDt es requerido");

            if (string.IsNullOrWhiteSpace(signonRq.CustLangPref))
                result.AddError("CustLangPref es requerido");
        }

        private static void ValidatePaySvcRq(PaySvcRq paySvcRq, ValidationResult result)
        {
            if (paySvcRq == null)
            {
                result.AddError("PaySvcRq no puede ser nulo");
                return;
            }

            // RqUID: UUID solo digitos, 36 caracteres
            if (string.IsNullOrWhiteSpace(paySvcRq.RqUID))
                result.AddError("RqUID es requerido");
            else if (!Regex.IsMatch(paySvcRq.RqUID, UuidPattern))
                result.AddError("RqUID debe ser UUID de solo digitos (36 caracteres, formato: 00000000-0000-0000-0000-000000000000)");

            // SPName: 15 digitos numericos
            if (string.IsNullOrWhiteSpace(paySvcRq.SPName))
                result.AddError("SPName es requerido");
            else if (!Regex.IsMatch(paySvcRq.SPName, @"^[0-9]{15}$"))
                result.AddError("SPName debe ser un codigo numerico de 15 digitos");

            ValidateMsgRqHdr(paySvcRq.MsgRqHdr, result);
            ValidatePmtAddRq(paySvcRq.PmtAddRq, result);
        }

        private static void ValidateMsgRqHdr(MsgRqHdr msgRqHdr, ValidationResult result)
        {
            if (msgRqHdr == null)
                return;

            // NetworkOwner: codigo de canal valido
            if (!string.IsNullOrWhiteSpace(msgRqHdr.NetworkTrnInfo?.NetworkOwner) &&
                !ChannelCodes.IsValid(msgRqHdr.NetworkTrnInfo.NetworkOwner))
                result.AddError($"NetworkOwner '{msgRqHdr.NetworkTrnInfo.NetworkOwner}' no es un codigo de canal valido (02, 03, 04, 05, 07, 09)");

            // BankId: 4 digitos
            if (!string.IsNullOrWhiteSpace(msgRqHdr.NetworkTrnInfo?.BankId) &&
                !Regex.IsMatch(msgRqHdr.NetworkTrnInfo.BankId, @"^[0-9]{4}$"))
                result.AddError("BankId debe ser un codigo numerico de 4 digitos");
        }

        private static void ValidatePmtAddRq(PmtAddRq pmtAddRq, ValidationResult result)
        {
            if (pmtAddRq == null)
            {
                result.AddError("PmtAddRq no puede ser nulo");
                return;
            }

            // PmtAddRq tiene su propio RqUID (campo #13)
            if (!string.IsNullOrWhiteSpace(pmtAddRq.RqUID) && !Regex.IsMatch(pmtAddRq.RqUID, UuidPattern))
                result.AddError("PmtAddRq.RqUID debe ser UUID de solo digitos");

            ValidatePmtInfo(pmtAddRq.PmtInfo, result);
        }

        private static void ValidatePmtInfo(PmtInfo? pmtInfo, ValidationResult result)
        {
            if (pmtInfo == null)
            {
                result.AddError("PmtInfo no puede ser nulo");
                return;
            }

            if (string.IsNullOrWhiteSpace(pmtInfo.PrcDt))
                result.AddError("PrcDt es requerido");

            if (pmtInfo.CurAmt == null)
            {
                result.AddError("CurAmt no puede ser nulo");
            }
            else
            {
                ValidateAmt(pmtInfo.CurAmt.Amt, "CurAmt.Amt", result);

                if (string.IsNullOrWhiteSpace(pmtInfo.CurAmt.CurCode))
                    result.AddError("CurCode es requerido");
                else if (!CurCodeValues.IsValid(pmtInfo.CurAmt.CurCode))
                    result.AddError("CurCode debe ser E (Efectivo), C (Cheque), M (Mixto) o D (Debito a Cuenta)");
            }

            ValidateRemitInfo(pmtInfo.RemitInfo, result);
        }

        private static void ValidateAmt(string amt, string fieldName, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(amt))
            {
                result.AddError($"{fieldName} es requerido");
                return;
            }

            // Debe ser numerico con separador punto, maximo 15 digitos
            if (!decimal.TryParse(amt, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                result.AddError($"{fieldName} debe ser un valor decimal valido con separador punto");
                return;
            }

            if (value <= 0)
                result.AddError($"{fieldName} debe ser mayor que cero");
        }

        private static void ValidateRemitInfo(RemitInfo remitInfo, ValidationResult result)
        {
            if (remitInfo == null)
            {
                result.AddError("RemitInfo no puede ser nulo");
                return;
            }

            if (string.IsNullOrWhiteSpace(remitInfo.BillId))
                result.AddError("BillId es requerido");
        }
    }

    public class ValidationResult
    {
        private readonly System.Collections.Generic.List<string> _errors = new();

        public bool IsValid => _errors.Count == 0;
        public System.Collections.Generic.IReadOnlyList<string> Errors => _errors;

        public void AddError(string error)
        {
            _errors.Add(error);
        }

        public string GetErrorMessage()
        {
            return string.Join("; ", _errors);
        }
    }
}
