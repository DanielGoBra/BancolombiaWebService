namespace BancolombiaWebService.Contracts.IFX
{
    public static class IFXStatusCodes
    {
        public const string Success = "0";
        public const string FacturaNoExiste = "-001";
        public const string FacturaYaPagada = "-002";
        public const string PagoNoRealizado = "-005";
        public const string TransaccionDuplicada = "-006";
        public const string ErrorTecnico = "-099";
    }

    public static class IFXStatusDescriptions
    {
        public const string Success = "Transaccion exitosa";
        public const string FacturaNoExiste = "Factura no existe";
        public const string FacturaYaPagada = "Factura ya fue pagada";
        public const string PagoNoRealizado = "Pago no realizado";
        public const string TransaccionDuplicada = "Transaccion duplicada";
        public const string ErrorTecnico = "Error tecnico";
    }

    public static class IFXSeverity
    {
        public const string Info = "Info";
        public const string Error = "Error";
    }

    // Spec lineas 618-626: Valores string, NO numericos
    // Processed = Exitosa (StatusCode 0)
    // Cancelled = Error tecnico (StatusCode -099)
    // Failed = Errores de negocio (-001, -002, -005, -006)
    public static class IFXPmtStatusCodes
    {
        public const string Processed = "Processed";
        public const string Cancelled = "Cancelled";
        public const string Failed = "Failed";

        public static string FromStatusCode(string statusCode)
        {
            return statusCode switch
            {
                "0" => Processed,
                "-099" => Cancelled,
                _ => Failed // -001, -002, -005, -006
            };
        }
    }

    public static class IFXConstants
    {
        public const string DefaultLanguage = "es-CO";
        public const string BancolombiaOrg = "Bancolombia";
        public const string RINName = "RINSV";
        public const string RINVersion = "1.0";
    }

    public static class CurCodeValues
    {
        public const string Efectivo = "E";
        public const string Cheque = "C";
        public const string Mixto = "M";
        public const string DebitoACuenta = "D";

        public static bool IsValid(string curCode)
        {
            return curCode == Efectivo || curCode == Cheque || curCode == Mixto || curCode == DebitoACuenta;
        }
    }

    // Spec lineas 313-326: Codigos de canal validos
    public static class ChannelCodes
    {
        public const string Caja = "02";
        public const string SucursalVirtual = "03";
        public const string SucursalTelefonica = "04";
        public const string FacturaNet = "05";
        public const string PACElectronico = "07";
        public const string BancolombiaALaMano = "09";

        private static readonly string[] ValidCodes = { Caja, SucursalVirtual, SucursalTelefonica, FacturaNet, PACElectronico, BancolombiaALaMano };

        public static bool IsValid(string code)
        {
            return Array.Exists(ValidCodes, c => c == code);
        }
    }
}
