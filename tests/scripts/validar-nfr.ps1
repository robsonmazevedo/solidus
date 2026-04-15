# Validação do RNF-001: Registros deve permanecer disponível independente de Posição
# Execute a partir da raiz do repositório: .\tests\scripts\validar-nfr.ps1

param(
    [string]$RegistrosUrl  = "http://localhost:8080",
    [string]$PosicaoUrl    = "http://localhost:8081",
    [string]$ComercianteId = "11111111-1111-1111-1111-111111111111",
    [string]$Secret        = "dev-secret-solidus-mude-em-producao-2026",
    [string]$Issuer        = "solidus"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$rootDir      = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$composeEnv   = Join-Path $rootDir "config/.env"
$composeApp   = Join-Path $rootDir "config/docker-compose.app.yml"
$composeArgs  = @("compose", "--project-directory", $rootDir, "--env-file", $composeEnv, "-f", $composeApp)

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-JwtToken {
    $header  = [System.Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}')
    $exp     = [DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
    $payload = [System.Text.Encoding]::UTF8.GetBytes(
        "{`"sub`":`"$ComercianteId`",`"comerciante_id`":`"$ComercianteId`",`"iss`":`"$Issuer`",`"exp`":$exp}"
    )
    $h          = ConvertTo-Base64Url $header
    $p          = ConvertTo-Base64Url $payload
    $sigInput   = "$h.$p"
    $hmac       = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key   = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $sig        = ConvertTo-Base64Url ($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($sigInput)))
    return "$sigInput.$sig"
}

Write-Host ""
Write-Host "=== Validacao RNF-001: Registros independente de Posicao ===" -ForegroundColor Cyan
Write-Host ""

$token   = New-JwtToken
$headers = @{ Authorization = "Bearer $token" }
$data    = Get-Date -Format "yyyy-MM-dd"
$chave   = "nfr-001-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"

# Passo 1: para os serviços de Posição
Write-Host "[1/5] Parando posicao-api e posicao-processor..." -ForegroundColor Yellow
docker @composeArgs stop posicao-api posicao-processor
Write-Host "      OK" -ForegroundColor Green

# Passo 2: registra lançamento com Posição indisponível
Write-Host ""
Write-Host "[2/5] POST /lancamentos com Posicao indisponivel..." -ForegroundColor Yellow

$body = @{
    tipo              = "CREDITO"
    valor             = 150.00
    dataCompetencia   = $data
    descricao         = "Teste RNF-001"
    chaveIdempotencia = $chave
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest `
        -Uri             "$RegistrosUrl/lancamentos" `
        -Method          POST `
        -Headers         $headers `
        -ContentType     "application/json" `
        -Body            $body `
        -UseBasicParsing

    if ($response.StatusCode -eq 201) {
        Write-Host "      PASSOU: 201 Created com Posicao indisponivel" -ForegroundColor Green
    } else {
        Write-Host "      FALHOU: status $($response.StatusCode) inesperado" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "      FALHOU: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Passo 3: reinicia os serviços de Posição
Write-Host ""
Write-Host "[3/5] Reiniciando posicao-api e posicao-processor..." -ForegroundColor Yellow
docker @composeArgs start posicao-api posicao-processor
Write-Host "      Aguardando inicializacao (20s)..."
Start-Sleep -Seconds 20
Write-Host "      OK" -ForegroundColor Green

# Passo 4: aguarda propagação do evento (RN-013: até 60s)
Write-Host ""
Write-Host "[4/5] Aguardando propagacao do evento (RN-013: ate 60s)..." -ForegroundColor Yellow

$refletido  = $false
$tentativas = 0
$posicao    = $null

do {
    Start-Sleep -Seconds 10
    $tentativas++

    try {
        $posicao = Invoke-RestMethod `
            -Uri     "$PosicaoUrl/posicao/diaria?data=$data" `
            -Headers $headers

        if ($posicao.totalCreditos -ge 150.00) {
            $refletido = $true
            Write-Host "      Propagado em $($tentativas * 10)s" -ForegroundColor Green
        } else {
            Write-Host "      totalCreditos=$($posicao.totalCreditos) - aguardando... ($($tentativas * 10)s / 60s)"
        }
    } catch {
        Write-Host "      Servico ainda inicializando... ($($tentativas * 10)s / 60s)"
    }
} while (-not $refletido -and $tentativas -lt 6)

# Passo 5: resultado final
Write-Host ""
Write-Host "[5/5] Resultado:" -ForegroundColor Cyan

if ($refletido) {
    Write-Host "      PASSOU: consolidado reflete lancamento feito com Posicao indisponivel" -ForegroundColor Green
    Write-Host "      data            : $data"
    Write-Host "      totalCreditos   : $($posicao.totalCreditos)"
    Write-Host "      totalDebitos    : $($posicao.totalDebitos)"
    Write-Host "      saldo           : $($posicao.saldo)"
    Write-Host ""
    Write-Host "      RNF-001 validado com sucesso." -ForegroundColor Green
} else {
    Write-Host "      FALHOU: consolidado nao refletiu o lancamento dentro de 60s" -ForegroundColor Red
    exit 1
}
