# ============================================================
# iniciar-pruebas.ps1
# Inicio RAPIDO del servicio en modo Testing
# Para correr desde el codigo fuente (sin publicar)
# ============================================================
# Uso: .\iniciar-pruebas.ps1
# ============================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  BancolombiaWebService - Modo PRUEBAS (Testing)"            -ForegroundColor Cyan  
Write-Host "============================================================" -ForegroundColor Cyan

# Verificar que existe el certificado
$pfxFile = "C:\certs\bancolombia-test.pfx"
if (-not (Test-Path $pfxFile)) {
    Write-Host ""
    Write-Host "  CERTIFICADO NO ENCONTRADO: $pfxFile" -ForegroundColor Red
    Write-Host "  Ejecuta primero: .\setup-testing.ps1 (como Administrador)" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# Mostrar info del servicio
$serverIP = (Get-NetIPAddress -AddressFamily IPv4 | 
    Where-Object { $_.InterfaceAlias -notlike "*Loopback*" -and $_.IPAddress -notlike "169.*" } | 
    Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "  Certificado: OK ($pfxFile)" -ForegroundColor Green
Write-Host ""
Write-Host "  URLS DEL SERVICIO:" -ForegroundColor Yellow
Write-Host "  -> HTTPS : https://$serverIP/RINunaSolaVia.svc" -ForegroundColor White
Write-Host "  -> HTTP  : http://$serverIP`:8080/RINunaSolaVia.svc" -ForegroundColor White  
Write-Host "  -> WSDL  : https://$serverIP/RINunaSolaVia.svc?wsdl" -ForegroundColor White
Write-Host "  -> Local : https://localhost/RINunaSolaVia.svc" -ForegroundColor Gray
Write-Host ""
Write-Host "  CREDENCIALES SOAP HEADER:" -ForegroundColor Yellow
Write-Host "  -> Usuario  : inklusiva_erp" -ForegroundColor White
Write-Host "  -> Password : InklusivaTesting2026!@#" -ForegroundColor White
Write-Host ""
Write-Host "  SOAPAction para VerificarEstado : http://tempuri.org/VerificarEstadoWebService" -ForegroundColor Gray
Write-Host "  SOAPAction para RegistrarPago   : http://tempuri.org/RegistrarPagoIFX" -ForegroundColor Gray
Write-Host ""
Write-Host "  Presiona Ctrl+C para detener el servicio" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Ir al directorio del proyecto principal
$scriptDir = $PSScriptRoot
$projectDir = Join-Path $scriptDir "BancolombiaWebService\BancolombiaWebService"

Set-Location $projectDir

# Iniciar con ambiente Testing
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet run --configuration Release
