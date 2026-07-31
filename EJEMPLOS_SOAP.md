# Ejemplo de Request SOAP para VerificarEstadoWebService

```xml
POST /RINunaSolaVia.svc HTTP/1.1
Host: tu-servidor.com
Content-Type: text/xml; charset=utf-8
SOAPAction: "http://www.bancolombia.com.co/RINunaSolaVia/V1/VerificarEstadoWebService"

<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Header/>
  <soap:Body>
    <v1:VerificarEstadoWebService>
      <v1:request>
        <v1:TransaccionId>VER-12345-2025</v1:TransaccionId>
        <v1:FechaHora>2025-10-23T10:30:00</v1:FechaHora>
      </v1:request>
    </v1:VerificarEstadoWebService>
  </soap:Body>
</soap:Envelope>
```

## Respuesta Esperada

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:VerificarEstadoWebServiceResponse>
      <v1:VerificarEstadoWebServiceResult>
        <v1:Estado>OK</v1:Estado>
        <v1:Mensaje>Servicio operativo</v1:Mensaje>
        <v1:FechaHora>2025-10-23T10:30:01</v1:FechaHora>
        <v1:Version>1.0.0</v1:Version>
      </v1:VerificarEstadoWebServiceResult>
    </v1:VerificarEstadoWebServiceResponse>
  </soap:Body>
</soap:Envelope>
```

---

# Ejemplo de Request SOAP para RegistrarPagoIFX

```xml
POST /RINunaSolaVia.svc HTTP/1.1
Host: tu-servidor.com
Content-Type: text/xml; charset=utf-8
SOAPAction: "http://www.bancolombia.com.co/RINunaSolaVia/V1/RegistrarPagoIFX"

<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Header/>
  <soap:Body>
    <v1:RegistrarPagoIFX>
      <v1:request>
        <v1:MensajeIFX><![CDATA[<?xml version="1.0" encoding="utf-8"?>
<IFX xmlns="http://www.ifxforum.org/standards">
  <SignonRq>
    <DtClient>2025-10-23T10:30:00</DtClient>
    <ClientApp>
      <Org>Bancolombia</Org>
      <Name>RecaudosBancolombia</Name>
      <Version>1.0</Version>
    </ClientApp>
  </SignonRq>
  <BillPmtRq>
    <MsgRqHdr>
      <TrnUID>TRN-20251023-103000-12345</TrnUID>
      <MsgId>MSG-103000-67890</MsgId>
    </MsgRqHdr>
    <BillPmtInfo>
      <BillerAcctId>CONV-001</BillerAcctId>
      <RefInfo>REF-2025-12345</RefInfo>
      <EffDt>2025-10-23</EffDt>
      <CurAmt>
        <Amt>150000</Amt>
        <CurCode>COP</CurCode>
      </CurAmt>
      <PayerInfo>
        <PersonInfo>
          <PersonName>
            <FullName>JUAN CARLOS PEREZ GOMEZ</FullName>
          </PersonName>
          <ContactInfo>
            <PhoneNum>3001234567</PhoneNum>
            <EmailAddr>juan.perez@example.com</EmailAddr>
          </ContactInfo>
        </PersonInfo>
        <TaxId>
          <TaxIdType>CC</TaxIdType>
          <TaxIdValue>1234567890</TaxIdValue>
        </TaxId>
      </PayerInfo>
      <PaymentMethod>
        <DeliveryMethod>Internet</DeliveryMethod>
        <PmtType>Credit</PmtType>
      </PaymentMethod>
      <AdditionalData>
        <Field1>Dato adicional 1</Field1>
        <Field2>Dato adicional 2</Field2>
      </AdditionalData>
    </BillPmtInfo>
  </BillPmtRq>
</IFX>]]></v1:MensajeIFX>
        <v1:TransaccionId>TRN-20251023-103000-12345</v1:TransaccionId>
        <v1:FechaHora>2025-10-23T10:30:00</v1:FechaHora>
      </v1:request>
    </v1:RegistrarPagoIFX>
  </soap:Body>
</soap:Envelope>
```

## Respuesta Exitosa

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:RegistrarPagoIFXResponse>
      <v1:RegistrarPagoIFXResult>
        <v1:MensajeIFX><![CDATA[<?xml version="1.0" encoding="utf-8"?>
<IFX>
  <SignonRs>
    <Status>
      <StatusCode>0</StatusCode>
      <StatusDesc>Pago registrado exitosamente</StatusDesc>
    </Status>
    <DtServer>2025-10-23T10:30:01</DtServer>
  </SignonRs>
  <BillPmtRs>
    <MsgRqHdr>
      <TrnUID>TRN-20251023-103000-12345</TrnUID>
    </MsgRqHdr>
    <Status>
      <StatusCode>0</StatusCode>
      <Severity>Info</Severity>
      <StatusDesc>Pago registrado exitosamente</StatusDesc>
    </Status>
    <BillPmtInfo>
      <ConfirmationNum>INT-20251023103001-A1B2C3D4</ConfirmationNum>
      <EffDt>2025-10-23</EffDt>
    </BillPmtInfo>
  </BillPmtRs>
</IFX>]]></v1:MensajeIFX>
        <v1:Estado>OK</v1:Estado>
        <v1:Mensaje>Pago registrado exitosamente</v1:Mensaje>
        <v1:FechaHora>2025-10-23T10:30:01</v1:FechaHora>
        <v1:TransaccionId>TRN-20251023-103000-12345</v1:TransaccionId>
      </v1:RegistrarPagoIFXResult>
    </v1:RegistrarPagoIFXResponse>
  </soap:Body>
</soap:Envelope>
```

## Respuesta de Error

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:RegistrarPagoIFXResponse>
      <v1:RegistrarPagoIFXResult>
        <v1:MensajeIFX><![CDATA[<?xml version="1.0" encoding="utf-8"?>
<IFX>
  <SignonRs>
    <Status>
      <StatusCode>200</StatusCode>
      <StatusDesc>Referencia no encontrada</StatusDesc>
    </Status>
    <DtServer>2025-10-23T10:30:01</DtServer>
  </SignonRs>
  <BillPmtRs>
    <MsgRqHdr>
      <TrnUID>TRN-20251023-103000-12345</TrnUID>
    </MsgRqHdr>
    <Status>
      <StatusCode>200</StatusCode>
      <Severity>Error</Severity>
      <StatusDesc>Referencia no encontrada</StatusDesc>
    </Status>
  </BillPmtRs>
</IFX>]]></v1:MensajeIFX>
        <v1:Estado>ERROR</v1:Estado>
        <v1:Mensaje>Referencia no encontrada</v1:Mensaje>
        <v1:FechaHora>2025-10-23T10:30:01</v1:FechaHora>
        <v1:TransaccionId>TRN-20251023-103000-12345</v1:TransaccionId>
      </v1:RegistrarPagoIFXResult>
    </v1:RegistrarPagoIFXResponse>
  </soap:Body>
</soap:Envelope>
```

---

# Códigos de Error IFX

| Código | Descripción | Uso |
|--------|-------------|-----|
| 0 | Éxito | Transacción procesada correctamente |
| 100 | Formato Inválido | Mensaje IFX mal formado |
| 200 | Referencia Inválida | Número de referencia no encontrado |
| 300 | Monto Inválido | Monto es 0, negativo o no coincide |
| 400 | Transacción Duplicada | Ya se procesó este pago |
| 500 | Error Interno | Error del servidor |
| 600 | Timeout | No se procesó en el tiempo límite |
| 700 | Servicio No Disponible | Servicio en mantenimiento |

---

# Prueba con cURL

## VerificarEstadoWebService
```bash
curl -k -X POST https://localhost:5001/RINunaSolaVia.svc \
  -H "Content-Type: text/xml" \
  -H "SOAPAction: http://www.bancolombia.com.co/RINunaSolaVia/V1/VerificarEstadoWebService" \
  -d '<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:VerificarEstadoWebService>
      <v1:request>
        <v1:TransaccionId>CURL-TEST-001</v1:TransaccionId>
      </v1:request>
    </v1:VerificarEstadoWebService>
  </soap:Body>
</soap:Envelope>'
```

## RegistrarPagoIFX
```bash
curl -k -X POST https://localhost:5001/RINunaSolaVia.svc \
  -H "Content-Type: text/xml" \
  -H "SOAPAction: http://www.bancolombia.com.co/RINunaSolaVia/V1/RegistrarPagoIFX" \
  -d '<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:v1="http://www.bancolombia.com.co/RINunaSolaVia/V1">
  <soap:Body>
    <v1:RegistrarPagoIFX>
      <v1:request>
        <v1:MensajeIFX><![CDATA[<IFX><MsgId>MSG-001</MsgId><TrnId>TRN-12345</TrnId><EffDt>2025-10-23</EffDt><CurAmt><Amt>100000</Amt><CurCode>COP</CurCode></CurAmt><RefInfo>REF-12345</RefInfo><PayerName>Juan Perez</PayerName></IFX>]]></v1:MensajeIFX>
        <v1:TransaccionId>CURL-TEST-002</v1:TransaccionId>
      </v1:request>
    </v1:RegistrarPagoIFX>
  </soap:Body>
</soap:Envelope>'
```
