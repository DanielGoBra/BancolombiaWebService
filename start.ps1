# Script de inicio rápido - Bancolombia WebService
# PowerShell

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Bancolombia WebService - Inicio Rápido" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Función para verificar si .NET está instalado
function Test-DotNetInstalled {
    try {
        $version = dotnet --version
        Write-Host ".NET SDK instalado: $version" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host ".NET SDK no encontrado" -ForegroundColor Red
        Write-Host "  Descarga desde: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
}

# Función para restaurar paquetes
function Restore-Packages {
    Write-Host ""
    Write-Host "Restaurando paquetes NuGet..." -ForegroundColor Yellow
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host " Paquetes restaurados exitosamente" -ForegroundColor Green
    } else {
        Write-Host " Error restaurando paquetes" -ForegroundColor Red
        exit 1
    }
}

# Función para compilar
function Build-Solution {
    Write-Host ""
    Write-Host "Compilando solución..." -ForegroundColor Yellow
    dotnet build --configuration Debug
    if ($LASTEXITCODE -eq 0) {
        Write-Host " Compilación exitosa" -ForegroundColor Green
    } else {
        Write-Host " Error en la compilación" -ForegroundColor Red
        exit 1
    }
}

# Función para ejecutar
function Start-Service {
    param (
        [string]$Environment = "Development"
    )
    
    Write-Host ""
    Write-Host "Iniciando servicio en modo: $Environment" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "URLs del servicio:" -ForegroundColor Yellow
    Write-Host "  SOAP Endpoint: https://localhost:5001/RINunaSolaVia.svc" -ForegroundColor White
    Write-Host "  WSDL:          https://localhost:5001/RINunaSolaVia.svc?wsdl" -ForegroundColor White
    Write-Host "  Home:          https://localhost:5001/" -ForegroundColor White
    Write-Host ""
    Write-Host "Presiona Ctrl+C para detener el servicio" -ForegroundColor Gray
    Write-Host ""
    
    Set-Location -Path ".\BancolombiaWebService"
    dotnet run --environment $Environment
}

# Menú principal
function Show-Menu {
    Clear-Host
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Bancolombia WebService - Menú Principal" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Restaurar paquetes NuGet" -ForegroundColor White
    Write-Host "2. Compilar solución" -ForegroundColor White
    Write-Host "3. Ejecutar en modo Development" -ForegroundColor White
    Write-Host "4. Ejecutar en modo Testing" -ForegroundColor White
    Write-Host "5. Ejecutar en modo Production" -ForegroundColor White
    Write-Host "6. Todo (Restaurar + Compilar + Ejecutar)" -ForegroundColor Yellow
    Write-Host "7. Ver logs" -ForegroundColor White
    Write-Host "8. Limpiar build" -ForegroundColor White
    Write-Host "9. Salir" -ForegroundColor Red
    Write-Host ""
}

# Verificar .NET
if (-not (Test-DotNetInstalled)) {
    exit 1
}

# Loop del menú
do {
    Show-Menu
    $selection = Read-Host "Selecciona una opción"
    
    switch ($selection) {
        '1' {
            Restore-Packages
            Read-Host "Presiona Enter para continuar"
        }
        '2' {
            Build-Solution
            Read-Host "Presiona Enter para continuar"
        }
        '3' {
            Start-Service -Environment "Development"
        }
        '4' {
            Start-Service -Environment "Testing"
        }
        '5' {
            Start-Service -Environment "Production"
        }
        '6' {
            Restore-Packages
            Build-Solution
            Start-Service -Environment "Development"
        }
        '7' {
            Write-Host ""
            Write-Host "Últimas 50 líneas del log:" -ForegroundColor Yellow
            Write-Host ""
            Get-ChildItem -Path ".\BancolombiaWebService\logs" -Filter "*.log" | 
                Sort-Object LastWriteTime -Descending | 
                Select-Object -First 1 | 
                Get-Content -Tail 50
            Write-Host ""
            Read-Host "Presiona Enter para continuar"
        }
        '8' {
            Write-Host ""
            Write-Host "Limpiando archivos de build..." -ForegroundColor Yellow
            dotnet clean
            Remove-Item -Path ".\BancolombiaWebService\bin" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path ".\BancolombiaWebService\obj" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path ".\BancolombiaWebService.Contracts\bin" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path ".\BancolombiaWebService.Contracts\obj" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path ".\BancolombiaWebService.Security\bin" -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -Path ".\BancolombiaWebService.Security\obj" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host " Limpieza completada" -ForegroundColor Green
            Read-Host "Presiona Enter para continuar"
        }
        '9' {
            Write-Host ""
            Write-Host "¡Hasta pronto!" -ForegroundColor Cyan
            return
        }
    }
} while ($true)
