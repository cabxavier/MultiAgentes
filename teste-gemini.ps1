# Teste HTTP puro contra a API do Gemini, sem passar por SDK nenhum.
#
# Serve para separar problema de API de problema de biblioteca: se este script
# retorna 200 mas a aplicacao retorna erro, a causa esta no cliente .NET, nao
# no Gemini. Foi assim que descobrimos que o pacote Mscc.GenerativeAI.Microsoft
# injetava "$schema" nas function declarations.
#
# Tambem serve para validar se um modelo novo aceita function calling antes de
# troca-lo no appsettings.json.
#
# Uso:  .\teste-gemini.ps1
#       .\teste-gemini.ps1 -Modelo "gemini-3.6-flash"

param(
    [string]$Modelo = "gemini-3.5-flash-lite"
)

$ErrorActionPreference = "Stop"

# --- Resolve a chave: user-secrets primeiro, depois variavel de ambiente ---
$userSecretsId = "4accd307-eff3-4ef9-987a-b02ac2dc7e9a"
$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$userSecretsId\secrets.json"

$apiKey = $null
if (Test-Path $secretsPath) {
    $secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json
    $apiKey = $secrets."Gemini:ApiKey"
}
if (-not $apiKey) { $apiKey = $env:GEMINI_API_KEY }
if (-not $apiKey) { throw "Chave nao encontrada em user-secrets nem em GEMINI_API_KEY." }

# --- Requisicao minima: um prompt + UMA function declaration ---------------
$corpo = @{
    contents = @(
        @{
            role  = "user"
            parts = @(@{ text = "Qual a situacao financeira do cliente Joao?" })
        }
    )
    tools    = @(
        @{
            functionDeclarations = @(
                @{
                    name        = "ConsultarSituacaoFinanceira"
                    description = "Consulta score de credito e renda mensal de um cliente."
                    parameters  = @{
                        type       = "object"
                        properties = @{
                            cliente = @{
                                type        = "string"
                                description = "Nome do cliente."
                            }
                        }
                        required   = @("cliente")
                    }
                }
            )
        }
    )
} | ConvertTo-Json -Depth 12

$url = "https://generativelanguage.googleapis.com/v1beta/models/${Modelo}:generateContent"

Write-Host "Modelo: $Modelo" -ForegroundColor Cyan
Write-Host "--- Corpo enviado ---" -ForegroundColor DarkGray
Write-Host $corpo
Write-Host "--- Resposta ---" -ForegroundColor DarkGray

try {
    $resposta = Invoke-RestMethod -Uri $url -Method Post `
        -Headers @{ "x-goog-api-key" = $apiKey } `
        -ContentType "application/json" `
        -Body $corpo

    Write-Host "SUCESSO (200)" -ForegroundColor Green
    $resposta | ConvertTo-Json -Depth 12
}
catch {
    Write-Host "FALHOU" -ForegroundColor Red
    $_.Exception.Message
    if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message }
}
