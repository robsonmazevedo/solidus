# Exporta issues abertas do SonarQube para arquivos locais em sonar-export/
# Uso: .\tests\scripts\exportar-sonar.ps1 -Token SEU_TOKEN

param(
    [string]$Token      = $env:SONAR_TOKEN,
    [string]$ProjectKey = "Solidus",
    [string]$HostUrl    = "http://localhost:9000",
    [string]$OutputDir  = "sonar-export"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$rootDir       = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$resolvedOutDir = Join-Path $rootDir $OutputDir

function New-SonarHeaders {
    param([string]$AuthToken)

    $basicToken = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("${AuthToken}:"))
    return @{ Authorization = "Basic $basicToken" }
}

function Get-PagedIssues {
    param(
        [string]$BaseUrl,
        [string]$ComponentKey,
        [hashtable]$Headers
    )

    $pageSize = 500
    $page = 1
    $allIssues = @()

    do {
        $url = "$BaseUrl/api/issues/search?componentKeys=$([Uri]::EscapeDataString($ComponentKey))&resolved=false&ps=$pageSize&p=$page&additionalFields=_all"
        $response = Invoke-RestMethod -Uri $url -Headers $Headers -Method Get
        $allIssues += @($response.issues)
        $total = [int]$response.total
        $page++
    } while ($allIssues.Count -lt $total)

    return $allIssues
}

function Get-Hotspots {
    param(
        [string]$BaseUrl,
        [string]$ProjectKey,
        [hashtable]$Headers
    )

    $pageSize = 500
    $page = 1
    $allHotspots = @()

    do {
        $url = "$BaseUrl/api/hotspots/search?projectKey=$([Uri]::EscapeDataString($ProjectKey))&ps=$pageSize&p=$page"
        $response = Invoke-RestMethod -Uri $url -Headers $Headers -Method Get
        $allHotspots += @($response.hotspots)
        $paging = $response.paging
        $total = [int]$paging.total
        $page++
    } while ($allHotspots.Count -lt $total)

    return $allHotspots
}

function New-IssuesSummary {
    param(
        [object[]]$Issues,
        [object[]]$Hotspots,
        [string]$ProjectKey,
        [string]$HostUrl
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Sonar export - $ProjectKey")
    $lines.Add("")
    $lines.Add("- Gerado em: $([DateTimeOffset]::Now.ToString('yyyy-MM-dd HH:mm:ss zzz'))")
    $lines.Add("- Projeto: $ProjectKey")
    $lines.Add("- Dashboard: $HostUrl/dashboard?id=$ProjectKey")
    $lines.Add("- Issues abertas: $($Issues.Count)")
    $lines.Add("- Security hotspots: $($Hotspots.Count)")
    $lines.Add("")

    $bySeverity = $Issues |
        Group-Object severity |
        Sort-Object Name

    if ($bySeverity.Count -gt 0) {
        $lines.Add("## Issues por severidade")
        $lines.Add("")
        foreach ($group in $bySeverity) {
            $lines.Add("- $($group.Name): $($group.Count)")
        }
        $lines.Add("")
    }

    $byType = $Issues |
        Group-Object type |
        Sort-Object Name

    if ($byType.Count -gt 0) {
        $lines.Add("## Issues por tipo")
        $lines.Add("")
        foreach ($group in $byType) {
            $lines.Add("- $($group.Name): $($group.Count)")
        }
        $lines.Add("")
    }

    $topFiles = $Issues |
        Group-Object component |
        Sort-Object Count -Descending |
        Select-Object -First 20

    if ($topFiles.Count -gt 0) {
        $lines.Add("## Arquivos com mais issues")
        $lines.Add("")
        foreach ($group in $topFiles) {
            $lines.Add("- $($group.Name): $($group.Count)")
        }
        $lines.Add("")
    }

    $lines.Add("## Primeiras issues")
    $lines.Add("")

    foreach ($issue in ($Issues | Select-Object -First 50)) {
        $message = ($issue.message -replace "\r?\n", " ").Trim()
        $line = if ($issue.line) { $issue.line } else { "-" }
        $lines.Add("- [$($issue.severity)/$($issue.type)] $($issue.component):$line - $message")
    }

    if ($Issues.Count -eq 0) {
        $lines.Add("- Nenhuma issue aberta encontrada.")
    }

    return ($lines -join [Environment]::NewLine)
}

Write-Host ""
Write-Host "=== Exportacao SonarQube - Solidus ===" -ForegroundColor Cyan
Write-Host ""

if (-not $Token) {
    Write-Host "Token nao informado." -ForegroundColor Red
    Write-Host "Defina a variavel de ambiente SONAR_TOKEN ou passe -Token SEU_TOKEN." -ForegroundColor Red
    exit 1
}

$headers = New-SonarHeaders -AuthToken $Token

Write-Host "[1/4] Verificando SonarQube em $HostUrl..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-RestMethod -Uri "$HostUrl/api/system/status" -Headers $headers -Method Get
    if ($statusResponse.status -ne "UP") {
        throw "SonarQube respondeu com status '$($statusResponse.status)'."
    }
    Write-Host "      OK" -ForegroundColor Green
} catch {
    Write-Host "      Nao foi possivel acessar o SonarQube: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[2/4] Coletando issues abertas..." -ForegroundColor Yellow
$issues = Get-PagedIssues -BaseUrl $HostUrl -ComponentKey $ProjectKey -Headers $headers
Write-Host "      $($issues.Count) issue(s) coletada(s)" -ForegroundColor Green

Write-Host ""
Write-Host "[3/4] Coletando security hotspots..." -ForegroundColor Yellow
try {
    $hotspots = Get-Hotspots -BaseUrl $HostUrl -ProjectKey $ProjectKey -Headers $headers
    Write-Host "      $($hotspots.Count) hotspot(s) coletado(s)" -ForegroundColor Green
} catch {
    $hotspots = @()
    Write-Host "      Hotspots nao exportados: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[4/4] Gravando arquivos em $resolvedOutDir..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $resolvedOutDir -Force | Out-Null

$issuesPath = Join-Path $resolvedOutDir "issues.json"
$hotspotsPath = Join-Path $resolvedOutDir "hotspots.json"
$summaryPath = Join-Path $resolvedOutDir "summary.md"

$issuesPayload = [ordered]@{
    generatedAt = [DateTimeOffset]::Now.ToString("o")
    projectKey  = $ProjectKey
    hostUrl     = $HostUrl
    total       = $issues.Count
    issues      = @($issues)
}

$hotspotsPayload = [ordered]@{
    generatedAt = [DateTimeOffset]::Now.ToString("o")
    projectKey  = $ProjectKey
    hostUrl     = $HostUrl
    total       = $hotspots.Count
    hotspots    = @($hotspots)
}

$issuesPayload | ConvertTo-Json -Depth 20 | Set-Content -Path $issuesPath -Encoding UTF8
$hotspotsPayload | ConvertTo-Json -Depth 20 | Set-Content -Path $hotspotsPath -Encoding UTF8
New-IssuesSummary -Issues $issues -Hotspots $hotspots -ProjectKey $ProjectKey -HostUrl $HostUrl | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host "      OK" -ForegroundColor Green
Write-Host ""
Write-Host "Arquivos gerados:" -ForegroundColor Cyan
Write-Host "- $issuesPath"
Write-Host "- $hotspotsPath"
Write-Host "- $summaryPath"
Write-Host ""