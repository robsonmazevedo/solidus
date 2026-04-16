Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Globalization

# ── Configuração centralizada ─────────────────────────────────────────────────

$Config = [ordered]@{
    Ambiente = [ordered]@{
        BaseUrlRegistros      = "http://localhost:8080"
        BaseUrlPosicao        = "http://localhost:8081"
        ComercianteId         = "11111111-1111-1111-1111-111111111111"
        JwtSecret             = "dev-secret-solidus-mude-em-producao-2026"
        JwtIssuer             = "solidus"
        ResultadosDir         = Join-Path $PSScriptRoot "resultados"
    }
    Calibragem = [ordered]@{
        TaxaAlvoReqPorSegundo = 50
        Duracao               = "1m"
        VUsPreAlocados        = 50
        VUsMaximos            = 200
        TaxaErroMaxima        = 0.05
        RegistrosP95Ms        = 300
        RegistrosP99Ms        = 600
        PosicaoP95Ms          = 200
        PosicaoP99Ms          = 400
    }
    Dados = [ordered]@{
        DataCompetencia       = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
        ValorLancamento       = "100.00"
        DescricaoLancamento   = "carga-k6-solidus"
    }
}

$Cenarios = @(
    [ordered]@{
        Nome      = "registros-carga"
        Script    = Join-Path $PSScriptRoot "registros-carga.js"
        Descricao = "Carga do endpoint de registro"
    },
    [ordered]@{
        Nome      = "posicao-carga"
        Script    = Join-Path $PSScriptRoot "posicao-carga.js"
        Descricao = "Carga do endpoint de posição"
    }
)

function Ensure-K6Installed {
    if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
        throw "k6 não encontrado. Instale com: winget install k6"
    }
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-JwtToken(
    [string]$ComercianteId,
    [string]$Secret,
    [string]$Issuer
) {
    $tokenScript = Join-Path $PSScriptRoot "../scripts/gerar-token.ps1"
    $output = & $tokenScript -ComercianteId $ComercianteId -Secret $Secret -Issuer $Issuer 6>&1
    $token = $output |
        ForEach-Object { $_.ToString() } |
        Where-Object { $_ -match '^ey' } |
        Select-Object -First 1

    if (-not $token) {
        throw "Não foi possível extrair o token JWT para o comerciante $ComercianteId."
    }

    return $token
}

function ConvertTo-K6EnvironmentArguments([hashtable]$EnvironmentMap) {
    $arguments = @()

    foreach ($entry in $EnvironmentMap.GetEnumerator()) {
        $value = $entry.Value

        if ($value -is [IFormattable]) {
            $value = $value.ToString($null, [System.Globalization.CultureInfo]::InvariantCulture)
        }

        $arguments += "-e"
        $arguments += ("{0}={1}" -f $entry.Key, $value)
    }

    return $arguments
}

function ConvertTo-TestDuration([string]$Duration) {
    if ($Duration -match '^(\d+)([smh])$') {
        $value = [int]$Matches[1]
        $unit = $Matches[2]

        switch ($unit) {
            's' { return [TimeSpan]::FromSeconds($value) }
            'm' { return [TimeSpan]::FromMinutes($value) }
            'h' { return [TimeSpan]::FromHours($value) }
        }
    }

    throw "Formato de duração não suportado para acompanhamento de progresso: $Duration"
}

function Get-ScenarioProgress(
    [string]$ScenarioName,
    [datetime]$StartTime,
    [TimeSpan]$ExpectedDuration
) {
    $elapsed = (Get-Date) - $StartTime
    $elapsedSeconds = [Math]::Min($elapsed.TotalSeconds, $ExpectedDuration.TotalSeconds)

    if ($ExpectedDuration.TotalSeconds -le 0) {
        return $null
    }

    $percent = [int][Math]::Min(100, [Math]::Floor(($elapsedSeconds / $ExpectedDuration.TotalSeconds) * 100))
    $remaining = $ExpectedDuration - [TimeSpan]::FromSeconds($elapsedSeconds)

    return [ordered]@{
        ScenarioName = $ScenarioName
        Percent      = $percent
        Elapsed      = $elapsed
        Remaining    = $remaining
    }
}

function Invoke-K6Scenario(
    [hashtable]$Scenario,
    [hashtable]$EnvironmentMap,
    [string]$Timestamp,
    [string]$OutputDirectory
) {
    if (-not (Test-Path $Scenario.Script)) {
        throw "Script do cenário não encontrado: $($Scenario.Script)"
    }

    Write-Host ""
    Write-Host ("Executando cenário: {0} ({1})..." -f $Scenario.Nome, $Scenario.Descricao) -ForegroundColor Cyan

    $outputFile = Join-Path $OutputDirectory ("{0}-{1}.json" -f $Scenario.Nome, $Timestamp)
    $arguments = @("run")
    $arguments += ConvertTo-K6EnvironmentArguments $EnvironmentMap
    $arguments += @("--out", "json=$outputFile", $Scenario.Script)

    $expectedDuration = ConvertTo-TestDuration $EnvironmentMap.DURACAO_TESTE
    $startedAt = Get-Date
    $process = Start-Process -FilePath "k6" -ArgumentList $arguments -NoNewWindow -PassThru
    $lastReportedPercent = -1

    while (-not $process.HasExited) {
        $progress = Get-ScenarioProgress -ScenarioName $Scenario.Nome -StartTime $startedAt -ExpectedDuration $expectedDuration

        if ($null -ne $progress) {
            $reportBucket = [int]([Math]::Floor($progress.Percent / 10) * 10)

            if ($reportBucket -gt $lastReportedPercent) {
                Write-Host (
                    "[{0}] {1,3}% | decorrido {2:mm\:ss} | restante estimado {3:mm\:ss}" -f
                    $progress.ScenarioName,
                    $progress.Percent,
                    $progress.Elapsed,
                    $progress.Remaining
                ) -ForegroundColor DarkGray
                $lastReportedPercent = $reportBucket
            }
        }

        Start-Sleep -Seconds 1
        $process.Refresh()
    }

    Write-Host ("[{0}] 100% | concluido" -f $Scenario.Nome) -ForegroundColor DarkGray

    $success = $process.ExitCode -eq 0

    return [ordered]@{
        Nome    = $Scenario.Nome
        Sucesso = $success
    }
}

Ensure-K6Installed
Ensure-Directory $Config.Ambiente.ResultadosDir

$token = Get-JwtToken $Config.Ambiente.ComercianteId $Config.Ambiente.JwtSecret $Config.Ambiente.JwtIssuer
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

$environmentMap = [ordered]@{
    BASE_URL_REGISTROS   = $Config.Ambiente.BaseUrlRegistros
    BASE_URL_POSICAO     = $Config.Ambiente.BaseUrlPosicao
    TOKEN                = $token
    TAXA_ALVO_RPS        = $Config.Calibragem.TaxaAlvoReqPorSegundo
    DURACAO_TESTE        = $Config.Calibragem.Duracao
    VUS_PRE_ALOCADOS     = $Config.Calibragem.VUsPreAlocados
    VUS_MAXIMOS          = $Config.Calibragem.VUsMaximos
    TAXA_ERRO_MAXIMA     = $Config.Calibragem.TaxaErroMaxima
    REGISTROS_P95_MS     = $Config.Calibragem.RegistrosP95Ms
    REGISTROS_P99_MS     = $Config.Calibragem.RegistrosP99Ms
    POSICAO_P95_MS       = $Config.Calibragem.PosicaoP95Ms
    POSICAO_P99_MS       = $Config.Calibragem.PosicaoP99Ms
    DATA_COMPETENCIA     = $Config.Dados.DataCompetencia
    VALOR_LANCAMENTO     = $Config.Dados.ValorLancamento
    DESCRICAO_LANCAMENTO = $Config.Dados.DescricaoLancamento
}

$resultados = @()

foreach ($cenario in $Cenarios) {
    $resultados += Invoke-K6Scenario $cenario $environmentMap $timestamp $Config.Ambiente.ResultadosDir
}

Write-Host ""
Write-Host "-----------------------------------------" -ForegroundColor DarkGray
Write-Host " Resumo dos testes de carga" -ForegroundColor Cyan
Write-Host "-----------------------------------------" -ForegroundColor DarkGray

foreach ($resultado in $resultados) {
    if ($resultado.Sucesso) {
        Write-Host ("  {0,-24} PASSOU" -f $resultado.Nome) -ForegroundColor Green
    }
    else {
        Write-Host ("  {0,-24} FALHOU" -f $resultado.Nome) -ForegroundColor Red
    }
}

Write-Host "-----------------------------------------" -ForegroundColor DarkGray
Write-Host (" Resultados salvos em: {0}" -f $Config.Ambiente.ResultadosDir) -ForegroundColor DarkGray
Write-Host ""

if ($resultados.Where({ -not $_.Sucesso }).Count -gt 0) {
    exit 1
}