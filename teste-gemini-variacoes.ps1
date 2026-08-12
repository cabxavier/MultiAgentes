# Isola qual elemento de uma requisicao faz o Gemini responder 400.
#
# Adiciona, um de cada vez, elementos que um cliente .NET pode enviar a mais, e
# reporta quais passam e quais falham. Foi o que identificou o "$schema" como
# causa do 400 INVALID_ARGUMENT (cenario 6 e o unico que falha).
#
# Util sempre que trocar de modelo ou de biblioteca cliente.
#
# Uso:  .\teste-gemini-variacoes.ps1

param(
    [string]$Modelo = "gemini-3.5-flash-lite"
)

$ErrorActionPreference = "Continue"

# --- Chave -----------------------------------------------------------------
$userSecretsId = "4accd307-eff3-4ef9-987a-b02ac2dc7e9a"
$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$userSecretsId\secrets.json"

$apiKey = $null
if (Test-Path $secretsPath) {
    $apiKey = (Get-Content $secretsPath -Raw | ConvertFrom-Json)."Gemini:ApiKey"
}
if (-not $apiKey) { $apiKey = $env:GEMINI_API_KEY }
if (-not $apiKey) { throw "Chave nao encontrada." }

$url = "https://generativelanguage.googleapis.com/v1beta/models/${Modelo}:generateContent"

# --- Blocos reutilizaveis --------------------------------------------------
function NovoConteudo {
    @(@{ role = "user"; parts = @(@{ text = "Qual a situacao financeira do cliente Joao?" }) })
}

function NovaFuncao([string]$nome, $props, $obrigatorios) {
    @{
        name        = $nome
        description = "Consulta dados de um cliente."
        parameters  = @{
            type       = "object"
            properties = $props
            required   = $obrigatorios
        }
    }
}

$propSimples = @{ cliente = @{ type = "string"; description = "Nome do cliente." } }

# --- Cenarios --------------------------------------------------------------
$cenarios = [ordered]@{}

# 1. Controle: sabemos que passa.
$cenarios["1. baseline (1 funcao)"] = @{
    contents = NovoConteudo
    tools    = @(@{ functionDeclarations = @((NovaFuncao "ConsultarCliente" $propSimples @("cliente"))) })
}

# 2. Instrucoes de sistema, como o ChatClientAgent envia.
$cenarios["2. + systemInstruction"] = @{
    contents          = NovoConteudo
    systemInstruction = @{ parts = @(@{ text = "Voce e um analista de credito. Use sempre as ferramentas." }) }
    tools             = @(@{ functionDeclarations = @((NovaFuncao "ConsultarCliente" $propSimples @("cliente"))) })
}

# 3. Duas funcoes dentro do MESMO objeto tools.
$cenarios["3. 2 funcoes, 1 objeto tools"] = @{
    contents = NovoConteudo
    tools    = @(@{ functionDeclarations = @(
        (NovaFuncao "ConsultarCliente" $propSimples @("cliente")),
        (NovaFuncao "VerificarRestricao" $propSimples @("cliente"))
    ) })
}

# 4. Duas funcoes em objetos tools SEPARADOS.
$cenarios["4. 2 objetos tools separados"] = @{
    contents = NovoConteudo
    tools    = @(
        @{ functionDeclarations = @((NovaFuncao "ConsultarCliente" $propSimples @("cliente"))) },
        @{ functionDeclarations = @((NovaFuncao "VerificarRestricao" $propSimples @("cliente"))) }
    )
}

# 5. Parametro opcional com "default" no schema.
$propComDefault = @{
    cliente       = @{ type = "string"; description = "Nome do cliente." }
    parcelaMensal = @{ type = "number"; description = "Parcela mensal."; default = 0 }
}
$cenarios["5. schema com 'default'"] = @{
    contents = NovoConteudo
    tools    = @(@{ functionDeclarations = @((NovaFuncao "ConsultarCliente" $propComDefault @("cliente"))) })
}

# 6. Keyword "$schema" dentro do parameters.
$funcComSchema = NovaFuncao "ConsultarCliente" $propSimples @("cliente")
$funcComSchema.parameters['$schema'] = "https://json-schema.org/draft/2020-12/schema"
$cenarios['6. schema com $schema'] = @{
    contents = NovoConteudo
    tools    = @(@{ functionDeclarations = @($funcComSchema) })
}

# --- Execucao --------------------------------------------------------------
Write-Host "Modelo: $Modelo`n" -ForegroundColor Cyan

foreach ($nome in $cenarios.Keys) {
    $corpo = $cenarios[$nome] | ConvertTo-Json -Depth 15
    try {
        $null = Invoke-RestMethod -Uri $url -Method Post `
            -Headers @{ "x-goog-api-key" = $apiKey } `
            -ContentType "application/json" -Body $corpo
        Write-Host ("{0,-32} OK" -f $nome) -ForegroundColor Green
    }
    catch {
        $detalhe = ""
        if ($_.ErrorDetails.Message) {
            try { $detalhe = ($_.ErrorDetails.Message | ConvertFrom-Json).error.message }
            catch { $detalhe = $_.ErrorDetails.Message }
        }
        Write-Host ("{0,-32} FALHOU  {1}" -f $nome, $detalhe) -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 1500
}
