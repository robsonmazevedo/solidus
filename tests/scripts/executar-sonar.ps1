# Executa analise SonarQube local contra o container do docker-compose.tools.yml
# Uso: .\tests\scripts\executar-sonar.ps1 -Token SEU_TOKEN
# O token pode ser definido na variavel de ambiente SONAR_TOKEN antes de executar o script.

param(
    [string]$Token      = $env:SONAR_TOKEN,
    [string]$ProjectKey = "solidus",
    [string]$HostUrl    = "http://localhost:9000"
)

$ErrorActionPreference = "Stop"

$rootDir = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

Write-Host ""
Write-Host "=== Analise SonarQube - Solidus ===" -ForegroundColor Cyan
Write-Host ""

if (-not $Token) {
    Write-Host "Token nao informado." -ForegroundColor Red
    Write-Host "Defina a variavel de ambiente SONAR_TOKEN ou passe -Token SEU_TOKEN." -ForegroundColor Red
    Write-Host ""
    Write-Host "Como gerar o token:" -ForegroundColor Yellow
    Write-Host "  1. Acesse $HostUrl"
    Write-Host "  2. Login admin / admin  (altere a senha no primeiro acesso)"
    Write-Host "  3. Meu perfil: Security - Generate Token"
    exit 1
}

# Verifica se o scanner esta instalado
if (-not (Get-Command "dotnet-sonarscanner" -ErrorAction SilentlyContinue)) {
    Write-Host "[!] dotnet-sonarscanner nao encontrado. Instalando..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
    Write-Host "    OK" -ForegroundColor Green
    Write-Host ""
}

# Verifica se o SonarQube esta acessivel
Write-Host "[1/4] Verificando SonarQube em $HostUrl..." -ForegroundColor Yellow
try {
    $resp   = Invoke-WebRequest -Uri "$HostUrl/api/system/status" -UseBasicParsing -TimeoutSec 5
    $status = ($resp.Content | ConvertFrom-Json).status
    if ($status -ne "UP") {
        Write-Host "      SonarQube respondeu com status '$status'. Aguarde o boot completo e tente novamente." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "      SonarQube nao acessivel. Suba o container antes de executar este script:" -ForegroundColor Red
    Write-Host "      docker compose -f docker-compose.tools.yml up sonarqube -d" -ForegroundColor Red
    exit 1
}
Write-Host "      OK" -ForegroundColor Green

# Begin
Write-Host ""
Write-Host "[2/4] Iniciando analise (begin)..." -ForegroundColor Yellow
Push-Location $rootDir
$scannerArgs = @(
    "begin",
    "/k:$ProjectKey",
    "/d:sonar.host.url=$HostUrl",
    "/d:sonar.token=$Token",
    "/d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml",
    "/d:sonar.exclusions=**/Migrations/**"
)
dotnet sonarscanner @scannerArgs
Write-Host "      OK" -ForegroundColor Green

# Build
Write-Host ""
Write-Host "[3/4] Build..." -ForegroundColor Yellow
dotnet build Solidus.slnx -c Release --no-incremental
Write-Host "      OK" -ForegroundColor Green

# End
Write-Host ""
Write-Host "[4/4] Enviando resultados (end)..." -ForegroundColor Yellow
dotnet sonarscanner end /d:sonar.token="$Token"
Pop-Location
Write-Host "      OK" -ForegroundColor Green

Write-Host ""
Write-Host "Analise concluida." -ForegroundColor Cyan
Write-Host ('Dashboard: ' + $HostUrl + '/dashboard?id=' + $ProjectKey) -ForegroundColor Cyan
Write-Host ""