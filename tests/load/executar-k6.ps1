param(
    [string]$BaseUrlRegistros = "http://localhost:8080",
    [string]$BaseUrlPosicao   = "http://localhost:8081",
    [string]$ComercianteId    = "11111111-1111-1111-1111-111111111111"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Pré-requisitos ────────────────────────────────────────────────────────────

if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
    Write-Error "k6 não encontrado. Instale com: winget install k6"
    exit 1
}

# ── Token JWT ─────────────────────────────────────────────────────────────────

Write-Host "Gerando token JWT..." -ForegroundColor Cyan

$tokenScript = Join-Path $PSScriptRoot "../scripts/gerar-token.ps1"
$output      = & $tokenScript -ComercianteId $ComercianteId 6>&1
$TOKEN       = ($output | ForEach-Object { $_.ToString() } | Where-Object { $_ -match '^ey' } | Select-Object -First 1)

if (-not $TOKEN) {
    Write-Error "Não foi possível extrair o token JWT."
    exit 1
}

Write-Host "Token gerado com sucesso." -ForegroundColor Green

# ── Diretório de resultados ───────────────────────────────────────────────────

$resultados = Join-Path $PSScriptRoot "resultados"
if (-not (Test-Path $resultados)) {
    New-Item -ItemType Directory -Path $resultados | Out-Null
}

$ts = Get-Date -Format "yyyyMMdd-HHmmss"

# ── Testes ────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Executando teste de carga: registros (POST /lancamentos)..." -ForegroundColor Cyan
k6 run `
    -e TOKEN=$TOKEN `
    -e BASE_URL=$BaseUrlRegistros `
    --out "json=$(Join-Path $resultados "registros-$ts.json")" `
    (Join-Path $PSScriptRoot "registros.js")
$registrosOk = $LASTEXITCODE -eq 0

Write-Host ""
Write-Host "Executando teste de carga: posicao (GET /posicao/diaria)..." -ForegroundColor Cyan
k6 run `
    -e TOKEN=$TOKEN `
    -e BASE_URL=$BaseUrlPosicao `
    --out "json=$(Join-Path $resultados "posicao-$ts.json")" `
    (Join-Path $PSScriptRoot "posicao.js")
$posicaoOk = $LASTEXITCODE -eq 0

# ── Resumo ────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "-----------------------------------------" -ForegroundColor DarkGray
 Write-Host " Resumo dos testes de carga" -ForegroundColor Cyan
Write-Host "-----------------------------------------" -ForegroundColor DarkGray

if ($registrosOk) {
    Write-Host "  registros  PASSOU" -ForegroundColor Green
} else {
    Write-Host "  registros  FALHOU" -ForegroundColor Red
}

if ($posicaoOk) {
    Write-Host "  posicao    PASSOU" -ForegroundColor Green
} else {
    Write-Host "  posicao    FALHOU" -ForegroundColor Red
}

Write-Host "-----------------------------------------" -ForegroundColor DarkGray
Write-Host " Resultados salvos em: tests/load/resultados/" -ForegroundColor DarkGray
Write-Host ""

if (-not $registrosOk -or -not $posicaoOk) { exit 1 }
