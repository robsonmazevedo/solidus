# Popular os dois dashboards do Grafana com dados reais
#
# O que este script faz:
#   [1/5] Verifica saude de registros-api e posicao-api
#   [2/5] Envia lancamentos validos  -> Dashboard 1: throughput, latencia, outbox, relay
#   [3/5] Envia lancamentos invalidos -> Dashboard 1: taxa de erro
#   [4/5] Aguarda OutboxRelay -> RabbitMQ -> Processor
#   [5/5] Consulta posicao diaria    -> Dashboard 2: throughput, latencia, cache hit/miss
#
# Pre-requisito: docker compose up -d
# Execute da raiz do repositorio: .\tests\scripts\popular-dashboards.ps1

param(
    [string]$RegistrosUrl         = "http://localhost:8080",
    [string]$PosicaoUrl           = "http://localhost:8081",
    [string]$GrafanaUrl           = "http://localhost:3000",
    [string]$ComercianteId        = "11111111-1111-1111-1111-111111111111",
    [string]$Secret               = "dev-secret-solidus-mude-em-producao-2026",
    [string]$Issuer               = "solidus",
    [int]   $LancamentosValidos   = 20,
    [int]   $LancamentosInvalidos = 5,
    [int]   $ConsultasPosicao     = 15
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-JwtToken {
    $header  = [System.Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}')
    $exp     = [DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
    $payload = [System.Text.Encoding]::UTF8.GetBytes(
        "{`"sub`":`"$ComercianteId`",`"comerciante_id`":`"$ComercianteId`",`"iss`":`"$Issuer`",`"exp`":$exp}"
    )
    $h        = ConvertTo-Base64Url $header
    $p        = ConvertTo-Base64Url $payload
    $sigInput = "$h.$p"
    $hmac     = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
    $sig      = ConvertTo-Base64Url ($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($sigInput)))
    return "$sigInput.$sig"
}

Write-Host ""
Write-Host "=== Popular Dashboards do Grafana ===" -ForegroundColor Cyan
Write-Host "    registros-api : $RegistrosUrl"     -ForegroundColor DarkCyan
Write-Host "    posicao-api   : $PosicaoUrl"       -ForegroundColor DarkCyan
Write-Host "    grafana       : $GrafanaUrl"       -ForegroundColor DarkCyan
Write-Host ""

$token   = New-JwtToken
$headers = @{ Authorization = "Bearer $token" }
$hoje    = Get-Date -Format "yyyy-MM-dd"
$ontem   = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
$ts      = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()

# --- Passo 1: saude dos servicos ---

Write-Host "[1/5] Verificando saude dos servicos..." -ForegroundColor Yellow

try {
    $r = Invoke-WebRequest -Uri "$RegistrosUrl/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "      registros-api: OK ($($r.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "      registros-api: FALHOU - $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "      Verifique se o ambiente esta rodando: docker compose up -d" -ForegroundColor Red
    exit 1
}

try {
    $r = Invoke-WebRequest -Uri "$PosicaoUrl/health" -UseBasicParsing -TimeoutSec 5
    Write-Host "      posicao-api  : OK ($($r.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "      posicao-api  : FALHOU - $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "      Verifique se o ambiente esta rodando: docker compose up -d" -ForegroundColor Red
    exit 1
}

# --- Passo 2: lancamentos validos ---

Write-Host ""
Write-Host "[2/5] Enviando $LancamentosValidos lancamentos validos..." -ForegroundColor Yellow
Write-Host "      Popula: Throughput, Latencia p99, Outbox pendentes, Relay (Dashboard 1)" -ForegroundColor DarkGray

$tipos     = @("CREDITO", "DEBITO")
$valores   = @(120.00, 45.50, 300.00, 89.90, 250.00, 15.00, 500.00, 33.33, 175.00, 60.00)
$okValidos = 0

for ($i = 1; $i -le $LancamentosValidos; $i++) {
    $tipo  = $tipos[($i - 1) % $tipos.Length]
    $valor = $valores[($i - 1) % $valores.Length]
    $body  = @{
        tipo              = $tipo
        valor             = $valor
        dataCompetencia   = $hoje
        descricao         = "Popular dashboard $i"
        chaveIdempotencia = "dash-valido-$ts-$i"
    } | ConvertTo-Json

    try {
        $r = Invoke-WebRequest `
            -Uri         "$RegistrosUrl/lancamentos" `
            -Method      POST `
            -Headers     $headers `
            -ContentType "application/json" `
            -Body        $body `
            -UseBasicParsing

        Write-Host ("      [{0,2}/{1}] {2,-7} R$ {3,6} -> {4}" -f $i, $LancamentosValidos, $tipo, $valor, $r.StatusCode) -ForegroundColor Green
        $okValidos++
    } catch {
        Write-Host "      [$i/$LancamentosValidos] FALHOU: $($_.Exception.Message)" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 400
}

Write-Host "      OK: $okValidos/$LancamentosValidos registrados" -ForegroundColor Green

# --- Passo 3: lancamentos invalidos (valor = 0 -> 422) ---

Write-Host ""
Write-Host "[3/5] Enviando $LancamentosInvalidos lancamentos invalidos (valor=0)..." -ForegroundColor Yellow
Write-Host "      Popula: Taxa de erro (Dashboard 1)" -ForegroundColor DarkGray

$okErros = 0

for ($i = 1; $i -le $LancamentosInvalidos; $i++) {
    $body = @{
        tipo              = "CREDITO"
        valor             = 0
        dataCompetencia   = $hoje
        descricao         = "Lancamento invalido $i"
        chaveIdempotencia = "dash-invalido-$ts-$i"
    } | ConvertTo-Json

    try {
        Invoke-WebRequest `
            -Uri         "$RegistrosUrl/lancamentos" `
            -Method      POST `
            -Headers     $headers `
            -ContentType "application/json" `
            -Body        $body `
            -UseBasicParsing | Out-Null

        Write-Host ("      [{0}/{1}] inesperado: 2xx (esperava 422)" -f $i, $LancamentosInvalidos) -ForegroundColor Red
    } catch {
        $status = [int]$_.Exception.Response.StatusCode
        if ($status -eq 422) {
            Write-Host ("      [{0}/{1}] 422 Unprocessable (esperado)" -f $i, $LancamentosInvalidos) -ForegroundColor Green
            $okErros++
        } else {
            Write-Host ("      [{0}/{1}] status inesperado: $status" -f $i, $LancamentosInvalidos) -ForegroundColor Red
        }
    }

    Start-Sleep -Milliseconds 300
}

Write-Host "      OK: $okErros/$LancamentosInvalidos erros gerados" -ForegroundColor Green

# --- Passo 4: propagacao outbox -> RabbitMQ -> Processor ---

Write-Host ""
Write-Host "[4/5] Aguardando OutboxRelay -> RabbitMQ -> Processor (10s)..." -ForegroundColor Yellow
Write-Host "      Popula: Outbox pendentes, Relay, Fila RabbitMQ, Processor (Dashboards 1 e 2)" -ForegroundColor DarkGray
Start-Sleep -Seconds 10
Write-Host "      OK" -ForegroundColor Green

# --- Passo 5: consultas de posicao diaria ---

Write-Host ""
Write-Host "[5/5] Consultando posicao diaria ($ConsultasPosicao requisicoes)..." -ForegroundColor Yellow
Write-Host "      Popula: Throughput, Latencia p99, Cache hit/miss (Dashboard 2)" -ForegroundColor DarkGray

# Sequencia: hoje (miss) -> hoje x4 (hit) -> ontem (miss) -> ontem x2 (hit) -> hoje x5 (hit) -> ontem x2 (hit)
$sequencia = @(
    $hoje,  $hoje,  $hoje,  $hoje,  $hoje,
    $ontem, $ontem, $ontem,
    $hoje,  $hoje,  $hoje,  $hoje,  $hoje,
    $ontem, $ontem
)

$okConsultas = 0
$limite      = [Math]::Min($ConsultasPosicao, $sequencia.Length)

for ($i = 0; $i -lt $limite; $i++) {
    $data     = $sequencia[$i]
    $esperado = if ($i -eq 0 -or $i -eq 5) { "cache miss" } else { "cache hit " }

    try {
        $r = Invoke-WebRequest `
            -Uri     "$PosicaoUrl/posicao/diaria?data=$data" `
            -Headers $headers `
            -UseBasicParsing

        Write-Host ("      [{0,2}/{1}] data={2}  {3}  -> {4}" -f ($i + 1), $limite, $data, $esperado, $r.StatusCode) -ForegroundColor Green
        $okConsultas++
    } catch {
        Write-Host "      [$($i+1)/$limite] FALHOU: $($_.Exception.Message)" -ForegroundColor Red
    }

    Start-Sleep -Milliseconds 500
}

Write-Host "      OK: $okConsultas/$limite consultas realizadas" -ForegroundColor Green

# --- Resumo ---

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Dashboards prontos para visualizacao     " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Abra o Grafana e defina o intervalo para 'Last 15 minutes':" -ForegroundColor White
Write-Host ""
Write-Host "  Dashboard 1 - Registros:" -ForegroundColor Yellow
Write-Host "    $GrafanaUrl/d/solidus-registros" -ForegroundColor White
Write-Host "    Paineis: Throughput, Taxa de erro, Latencia p99, Outbox, Relay" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Dashboard 2 - Posicao:" -ForegroundColor Yellow
Write-Host "    $GrafanaUrl/d/solidus-posicao" -ForegroundColor White
Write-Host "    Paineis: Throughput, Latencia p99, Cache hit rate, Fila RabbitMQ, Processor" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Login Grafana: admin / (senha configurada no .env)" -ForegroundColor DarkGray
Write-Host ""
