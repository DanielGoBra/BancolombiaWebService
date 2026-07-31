# Script de Prueba - Bancolombia WebService
# Conforme a Especificaciones Tecnicas v4.0 Jun 2023
# Compatible con Windows PowerShell 5.1

# 1. Configurar confianza SSL (para localhost dev cert)
if ("TrustAllCertsPolicy" -as [type]) {} else {
  add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
  [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# 2. Configuracion
$baseUrl = "https://localhost:5001/RINunaSolaVia.svc"
$username = "bancolombia_user"
$password = "ChangeMe123!@#"

# ========================================================================
# PRUEBA 1: VerificarEstadoWebService
# Spec: infoDummy contiene IFX XML en CDATA con SignonRq
# ========================================================================
Write-Host "=== PRUEBA 1: Verificar Estado del Servicio ===" -ForegroundColor Cyan

$soapRequest = @"
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:tem="http://tempuri.org/">
   <soapenv:Header>
      <tem:userName>$username</tem:userName>
      <tem:password>$password</tem:password>
   </soapenv:Header>
   <soapenv:Body>
      <tem:VerificarEstadoWebService>
         <tem:infoDummy><![CDATA[<?xml version="1.0" encoding="UTF-8"?><IFX>
 <SignonRq>
  <ClientDt>$(Get-Date -Format "yyyy-MM-ddTHH:mm:ss")</ClientDt>
  <CustLangPref>es-CO</CustLangPref>
  <ClientApp>
   <Org>Bancolombia</Org>
   <Name>RIN</Name>
   <Version>1.0</Version>
  </ClientApp>
 </SignonRq>
</IFX>]]></tem:infoDummy>
      </tem:VerificarEstadoWebService>
   </soapenv:Body>
</soapenv:Envelope>
"@

try {
  $response = Invoke-WebRequest -Uri $baseUrl `
    -Method POST `
    -ContentType "text/xml; charset=utf-8" `
    -Headers @{ "SOAPAction" = "http://tempuri.org/VerificarEstadoWebService" } `
    -Body $soapRequest

  Write-Host "`n[OK] RESPUESTA:" -ForegroundColor Green
  Write-Host $response.Content
}
catch {
  Write-Host "`n[ERROR]" -ForegroundColor Red
  Write-Host $_.Exception.Message
  if ($_.Exception.Response) {
    $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
    $errBody = $reader.ReadToEnd()
    Write-Host $errBody -ForegroundColor Red
  }
}

# ========================================================================
# PRUEBA 2: RegistrarPagoIFX
# Spec: infoPago contiene IFX XML completo en CDATA
# - RqUID: UUID solo digitos
# - SPName: 15 digitos numericos
# - BillId: UUID solo digitos
# - CurCode: E, C, M o D
# - MsgRqHdr con NetworkTrnInfo y PointOfServiceData
# - DepAcctIdFrom con AcctId, AcctType, BankInfo
# ========================================================================
Write-Host "`n`n=== PRUEBA 2: Registrar Pago (IFX Spec-Compliant) ===" -ForegroundColor Cyan

$fechaActual = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
$fechaContable = Get-Date -Format "yyyy-MM-dd"

$ifxPayload = @"
<?xml version="1.0" encoding="UTF-8"?><IFX>
 <SignonRq>
  <ClientDt>$fechaActual</ClientDt>
  <CustLangPref>es-CO</CustLangPref>
  <ClientApp>
   <Org>Bancolombia</Org>
   <Name>RINSV</Name>
   <Version>1.0</Version>
  </ClientApp>
 </SignonRq>
 <PaySvcRq>
  <RqUID>12345678-1234-1234-1234-123456789012</RqUID>
  <MsgRqHdr>
   <NetworkTrnInfo>
    <NetworkOwner>02</NetworkOwner>
    <BankId>0007</BankId>
   </NetworkTrnInfo>
   <PointOfServiceData>
    <Environment></Environment>
    <POSAgent>
     <AgentId>1234</AgentId>
    </POSAgent>
    <POSLocation>00001</POSLocation>
   </PointOfServiceData>
  </MsgRqHdr>
  <SPName>000000014583000</SPName>
  <PmtAddRq>
   <RqUID>12345678-1234-1234-1234-123456789012</RqUID>
   <PmtInfo>
    <CurAmt>
     <Amt>150000.00</Amt>
     <CurCode>E</CurCode>
    </CurAmt>
    <RemitInfo>
     <CustPayeeId>900123456789012</CustPayeeId>
     <BillId>00000000-0001-0001-0001-000000000999</BillId>
     <BillRefInfo>000000000001</BillRefInfo>
     <PmtId>00000000000001</PmtId>
     <CurAmt>
      <Amt>150000.00</Amt>
     </CurAmt>
    </RemitInfo>
    <DepAcctIdFrom>
     <AcctId></AcctId>
     <AcctType>Default</AcctType>
     <BankInfo></BankInfo>
    </DepAcctIdFrom>
    <PrcDt>$fechaContable</PrcDt>
   </PmtInfo>
  </PmtAddRq>
 </PaySvcRq>
</IFX>
"@

$soapRequest2 = @"
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:tem="http://tempuri.org/">
   <soapenv:Header>
      <tem:userName>$username</tem:userName>
      <tem:password>$password</tem:password>
   </soapenv:Header>
   <soapenv:Body>
      <tem:RegistrarPagoIFX>
         <tem:infoPago><![CDATA[$ifxPayload]]></tem:infoPago>
      </tem:RegistrarPagoIFX>
   </soapenv:Body>
</soapenv:Envelope>
"@

try {
  $response = Invoke-WebRequest -Uri $baseUrl `
    -Method POST `
    -ContentType "text/xml; charset=utf-8" `
    -Headers @{ "SOAPAction" = "http://tempuri.org/RegistrarPagoIFX" } `
    -Body $soapRequest2

  Write-Host "`n[OK] RESPUESTA:" -ForegroundColor Green
  Write-Host $response.Content
}
catch {
  Write-Host "`n[ERROR]" -ForegroundColor Red
  Write-Host $_.Exception.Message
  if ($_.Exception.Response) {
    $reader = New-Object System.IO.StreamReader $_.Exception.Response.GetResponseStream()
    $errBody = $reader.ReadToEnd()
    Write-Host $errBody -ForegroundColor Red
  }
}
