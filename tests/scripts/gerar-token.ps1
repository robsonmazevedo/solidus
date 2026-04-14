param(
    [string]$ComercianteId  = "11111111-1111-1111-1111-111111111111",
    [string]$Secret         = "dev-secret-solidus-mude-em-producao-2026",
    [string]$Issuer         = "solidus",
    [int]   $ExpiracaoHoras = 24
)

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$header  = [System.Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}')
$exp     = [DateTimeOffset]::UtcNow.AddHours($ExpiracaoHoras).ToUnixTimeSeconds()
$payload = [System.Text.Encoding]::UTF8.GetBytes(
    "{`"sub`":`"$ComercianteId`",`"comerciante_id`":`"$ComercianteId`",`"iss`":`"$Issuer`",`"exp`":$exp}"
)

$h        = ConvertTo-Base64Url $header
$p        = ConvertTo-Base64Url $payload
$sigInput = "$h.$p"

$hmac     = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($Secret)
$sig      = ConvertTo-Base64Url ($hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($sigInput)))

$token = "$sigInput.$sig"

Write-Host "Token JWT gerado:"
Write-Host $token
Write-Host ""
Write-Host "comerciante_id : $ComercianteId"
Write-Host "expira em      : $([DateTimeOffset]::UtcNow.AddHours($ExpiracaoHoras).ToString('yyyy-MM-dd HH:mm:ss')) UTC"
Write-Host ""
Write-Host "Exemplo de uso:"
Write-Host "  curl -H `"Authorization: Bearer $token`" `"http://localhost:8081/posicao/diaria?data=$(Get-Date -Format 'yyyy-MM-dd')`""
