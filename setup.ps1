# Restaura os pacotes e orienta a configuracao da chave.
#
# Uso, a partir da pasta do projeto:
#   .\setup.ps1

$ErrorActionPreference = "Stop"

$proj = Join-Path $PSScriptRoot "MultiAgentes.csproj"
if (-not (Test-Path $proj)) {
    throw "MultiAgentes.csproj nao encontrado em $PSScriptRoot"
}

Write-Host "Restaurando pacotes..." -ForegroundColor Cyan
dotnet restore $proj

Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build $proj --no-restore

# --- Verifica se a chave ja esta configurada -------------------------------
$userSecretsId = ([xml](Get-Content $proj)).Project.PropertyGroup.UserSecretsId | Where-Object { $_ }
$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$userSecretsId\secrets.json"

$temChave = $false
if (Test-Path $secretsPath) {
    $temChave = [bool] (Get-Content $secretsPath -Raw | ConvertFrom-Json)."Gemini:ApiKey"
}
if (-not $temChave) { $temChave = [bool] $env:GEMINI_API_KEY }

Write-Host ""
if ($temChave) {
    Write-Host "Chave do Gemini configurada. Rode: dotnet run" -ForegroundColor Green
}
else {
    Write-Host "Falta configurar a chave do Gemini." -ForegroundColor Yellow
    Write-Host "  1. Obtenha em https://aistudio.google.com/apikey"
    Write-Host '  2. dotnet user-secrets set "Gemini:ApiKey" "<chave>"'
}
