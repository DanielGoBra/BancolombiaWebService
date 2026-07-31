# ============================================================
# setup-testing.ps1
# Script de preparacion completa para ambiente de PRUEBAS
# BancolombiaWebService - Inklusiva s.a.s
# ============================================================
# Ejecutar como Administrador en el servidor de pruebas
# ============================================================

param(
    [string]$CertPassword = "InklusivaCert2026!",
    [string]$CertPath     = "C:\certs",
    [string]$CertName     = "bancolombia-test",
    [string]$PublishPath  = "C:\servicios\BancolombiaWebService",
    [string]$Puerto       = "443"
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host ">>> $msg" -ForegroundColor Cyan
}

function Write-OK([string]$msg) {
    Write-Host "    OK: $msg" -ForegroundColor Green
}

function Write-Info([string]$msg) {
    Write-Host "    $msg" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  BancolombiaWebService - Setup Ambiente de Pruebas"          -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================
# PASO 1 - Verificar .NET 8
# ============================================================
Write-Step "Verificando .NET 8..."
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -notmatch "^8\.") {
        Write-Host "    ADVERTENCIA: Se detecto .NET $dotnetVersion. Se requiere .NET 8." -ForegroundColor Yellow
        Write-Host "    Descarga: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    } else {
        Write-OK ".NET $dotnetVersion instalado"
    }
} catch {
    Write-Host "    ERROR: .NET no esta instalado." -ForegroundColor Red
    Write-Host "    Descarga: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}

# ============================================================
# PASO 2 - Generar certificado autofirmado (Self-Signed)
# ============================================================
Write-Step "Generando certificado SSL autofirmado..."

# Crear carpeta de certificados
if (-not (Test-Path $CertPath)) {
    New-Item -ItemType Directory -Path $CertPath -Force | Out-Null
    Write-OK "Carpeta creada: $CertPath"
}

$pfxFile  = Join-Path $CertPath "$CertName.pfx"
$cerFile  = Join-Path $CertPath "$CertName.cer"

# Verificar si ya existe
if (Test-Path $pfxFile) {
    Write-Info "El certificado ya existe en $pfxFile"
    Write-Info "Para regenerarlo, elimina el archivo y vuelve a ejecutar este script."
} else {
    # Generar certificado autofirmado con PowerShell (viene incluido en Windows)
    $cert = New-SelfSignedCertificate `
        -DnsName "inklusiva-bancolombia-test", "localhost", $env:COMPUTERNAME `
        -CertStoreLocation "cert:\LocalMachine\My" `
        -NotAfter (Get-Date).AddYears(2) `
        -KeySpec KeyExchange `
        -KeyUsage KeyEncipherment, DataEncipherment `
        -FriendlyName "Inklusiva BancolombiaWebService - Testing"

    # Exportar como .pfx (con clave privada) para Kestrel
    $securePassword = ConvertTo-SecureString -String $CertPassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxFile -Password $securePassword | Out-Null

    # Exportar como .cer (solo clave publica) para que el ERP lo importe y confie en el
    Export-Certificate -Cert $cert -FilePath $cerFile | Out-Null

    Write-OK "Certificado generado:"
    Write-Info "  PFX (servidor):  $pfxFile"
    Write-Info "  CER (proveedor): $cerFile"
    Write-Info "  Valido hasta:    $((Get-Date).AddYears(2).ToString('yyyy-MM-dd'))"
    Write-Info "  Password PFX:    $CertPassword"
    Write-Host ""
    Write-Host "  IMPORTANTE: Envia el archivo .CER al proveedor ERP para" -ForegroundColor Yellow
    Write-Host "  que lo importe en su almacen de certificados de confianza." -ForegroundColor Yellow
}

# ============================================================
# PASO 3 - Compilar y publicar el servicio
# ============================================================
Write-Step "Compilando y publicando el servicio..."

# Ruta de la solucion (relativa a donde esta este script)
$scriptDir  = $PSScriptRoot
$solutionDir = Join-Path $scriptDir "BancolombiaWebService"
$projectDir  = Join-Path $solutionDir "BancolombiaWebService"

Write-Info "Directorio del proyecto: $projectDir"

if (-not (Test-Path $projectDir)) {
    Write-Host "    ERROR: No se encontro el proyecto en $projectDir" -ForegroundColor Red
    Write-Host "    Ejecuta este script desde la carpeta BancolombiaWebService (la raiz con el .sln)" -ForegroundColor Red
    exit 1
}

# Restaurar paquetes
Write-Info "Restaurando paquetes NuGet..."
dotnet restore $solutionDir
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR en restore" -ForegroundColor Red; exit 1 }

# Publicar en modo Release con ambiente Testing
Write-Info "Publicando en $PublishPath ..."
if (-not (Test-Path $PublishPath)) {
    New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
}

dotnet publish $projectDir `
    -c Release `
    -o $PublishPath `
    --self-contained false

if ($LASTEXITCODE -ne 0) { Write-Host "ERROR en publish" -ForegroundColor Red; exit 1 }

Write-OK "Servicio publicado en: $PublishPath"

# ============================================================
# PASO 4 - Configurar Windows Firewall para el puerto HTTPS
# ============================================================
Write-Step "Configurando regla de Firewall para puerto $Puerto..."

$ruleName = "BancolombiaWebService-HTTPS-$Puerto"
$existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-Info "Regla de firewall ya existe: $ruleName"
} else {
    New-NetFirewallRule `
        -DisplayName $ruleName `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $Puerto `
        -Action Allow `
        -Profile Any | Out-Null
    Write-OK "Regla de firewall creada: puerto $Puerto abierto para conexiones entrantes"
}

# Puerto HTTP 8080 tambien (para pruebas sin SSL)
$ruleName8080 = "BancolombiaWebService-HTTP-8080"
if (-not (Get-NetFirewallRule -DisplayName $ruleName8080 -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule `
        -DisplayName $ruleName8080 `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort 8080 `
        -Action Allow `
        -Profile Any | Out-Null
    Write-OK "Regla de firewall creada: puerto 8080 abierto (HTTP para pruebas)"
}

# ============================================================
# PASO 5 - Resumen final e instrucciones
# ============================================================
$serverIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  SETUP COMPLETADO" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "CERTIFICADO:" -ForegroundColor Cyan
Write-Host "  Archivo PFX: $pfxFile" -ForegroundColor White
Write-Host "  Archivo CER: $cerFile  <-- Envia este al ERP" -ForegroundColor Yellow
Write-Host "  Password:    $CertPassword" -ForegroundColor White
Write-Host ""
Write-Host "CREDENCIALES SOAP HEADER (para el proveedor ERP):" -ForegroundColor Cyan
Write-Host "  <tem:userName>inklusiva_erp</tem:userName>" -ForegroundColor White
Write-Host "  <tem:password>InklusivaTesting2026!@#</tem:password>" -ForegroundColor White
Write-Host ""
Write-Host "SERVICIO PUBLICADO EN:" -ForegroundColor Cyan
Write-Host "  $PublishPath" -ForegroundColor White
Write-Host ""
Write-Host "COMO INICIAR EL SERVICIO:" -ForegroundColor Cyan
Write-Host "  cd $PublishPath" -ForegroundColor White
Write-Host '  $env:ASPNETCORE_ENVIRONMENT = "Testing"' -ForegroundColor White
Write-Host "  dotnet BancolombiaWebService.dll" -ForegroundColor White
Write-Host ""
Write-Host "URLS DEL SERVICIO (compartir con el ERP):" -ForegroundColor Cyan
Write-Host "  HTTPS: https://$serverIP/RINunaSolaVia.svc" -ForegroundColor White
Write-Host "  HTTP:  http://$serverIP`:8080/RINunaSolaVia.svc  (sin SSL - solo pruebas)" -ForegroundColor White
Write-Host "  WSDL:  https://$serverIP/RINunaSolaVia.svc?wsdl" -ForegroundColor White
Write-Host ""
Write-Host "SOAP ACTION a usar en las peticiones:" -ForegroundColor Cyan
Write-Host "  VerificarEstado: http://tempuri.org/VerificarEstadoWebService" -ForegroundColor White
Write-Host "  RegistrarPago:   http://tempuri.org/RegistrarPagoIFX" -ForegroundColor White
Write-Host ""
Write-Host "NOTA: El certificado es autofirmado. El ERP debe:" -ForegroundColor Yellow
Write-Host "  1. Importar el archivo .CER en su almacen de certificados de confianza, O" -ForegroundColor Yellow
Write-Host "  2. Configurar su cliente HTTP para ignorar errores de SSL (solo en pruebas)" -ForegroundColor Yellow
Write-Host ""
