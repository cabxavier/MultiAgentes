<div align="center">

# MultiAgentes

**Sistema multiagente de análise de crédito em .NET, construído com o Microsoft Agent Framework**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Microsoft Agent Framework](https://img.shields.io/badge/Agent_Framework-1.17.0-0078D4?style=flat-square&logo=microsoft&logoColor=white)](https://learn.microsoft.com/agent-framework/)
[![Microsoft.Extensions.AI](https://img.shields.io/badge/Extensions.AI-10.9.0-5C2D91?style=flat-square&logo=nuget&logoColor=white)](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
[![Gemini](https://img.shields.io/badge/Google_Gemini-3.5_Flash_Lite-4285F4?style=flat-square&logo=googlegemini&logoColor=white)](https://ai.google.dev/)
[![Visual Studio](https://img.shields.io/badge/Visual_Studio-2026-5C2D91?style=flat-square&logo=visualstudio&logoColor=white)](https://visualstudio.microsoft.com/)

</div>

---

## Sobre

Cinco agentes de IA especializados colaboram para decidir sobre um pedido de crédito. Cada um tem uma responsabilidade estreita, acesso apenas às ferramentas de que precisa, e produz um parecer auditável. Um revisor consolida os pareceres e um redator traduz a decisão técnica em linguagem para o solicitante.

O objetivo é didático: mostrar como os componentes do **Microsoft Agent Framework** se encaixam — agentes, tool calling, orquestração e injeção de dependência — num caso de uso reconhecível, sem a complexidade de um sistema de produção.

Além do código, este repositório documenta **o que não funciona** no ecossistema .NET de IA hoje. Boa parte do tempo de construção foi gasta descobrindo incompatibilidades que as mensagens de erro não revelam. Essas descobertas estão em [Armadilhas do Gemini](#armadilhas-do-gemini-em-net) e [Caminhos sem saída](#caminhos-sem-saída).

> [!NOTE]
> Os dados de crédito e compliance são simulados. Este projeto é uma referência de arquitetura, não um sistema de decisão de crédito.

---

## Arquitetura

```
                        ┌─────────────┐
   Solicitação  ──────► │   Planner   │  interpreta o pedido
                        └──────┬──────┘  define o que verificar
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
          ┌──────▼──────┐            ┌───────▼───────┐
          │   Credit    │            │  Compliance   │   em paralelo
          │             │            │               │
          │ 🔧 tool     │            │ 🔧 tool       │
          └──────┬──────┘            └───────┬───────┘
                 │                           │
                 └─────────────┬─────────────┘
                               │
                        ┌──────▼──────┐
                        │  Reviewer   │  consolida e decide
                        └──────┬──────┘
                               │
                        ┌──────▼──────┐
                        │  Response   │  redige ao usuário
                        └──────┬──────┘
                               │
                        Resposta final
```

### Os agentes

| Agente | Responsabilidade | Ferramenta | Regra que carrega |
|---|---|---|---|
| **Planner** | Extrai cliente, valor e prazo; lista as verificações necessárias | — | Não emite juízo sobre aprovar ou negar |
| **Credit** | Score, renda, inadimplência, comprometimento de renda | `ConsultarSituacaoFinanceira` | Nunca inventa números; declara dado indisponível |
| **Compliance** | Listas restritivas e confronto com políticas internas | `VerificarCompliance` | Emite parecer explícito: aprovado, bloqueado ou pendente |
| **Reviewer** | Confere completude e aponta contradições entre pareceres | — | Bloqueio de compliance prevalece sobre parecer financeiro favorável |
| **Response** | Traduz a decisão em resposta estruturada ao solicitante | — | Não introduz fatos ausentes dos pareceres |

### Por que orquestração em C# e não `WorkflowBuilder`

O MAF oferece o `Microsoft.Agents.AI.Workflows` para montar grafos de agentes. Aqui a orquestração é código C# explícito, por três motivos:

1. **Erros identificáveis.** Quando uma etapa falha, a exceção diz qual agente quebrou. Num grafo, o erro vem do runtime.
2. **Contexto controlado.** Cada agente recebe exatamente o texto que precisa, montado à mão. Isso é visível e ajustável.
3. **Paralelismo trivial.** As chamadas de Credit e Compliance são disparadas antes do primeiro `await`, então rodam concorrentemente sem precisar de fan-out/fan-in.

O resultado de cada etapa fica no `CreditAnalysisResult`, não só a resposta final — o fluxo inteiro é auditável.

---

## Início rápido

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (`dotnet --version` deve mostrar `10.x`)
- Uma chave da API do Gemini — gratuita, sem cartão de crédito

### 1. Obtenha a chave

Acesse [aistudio.google.com/apikey](https://aistudio.google.com/apikey), faça login com uma conta Google e clique em **Create API key**. Copie o valor na hora.

### 2. Configure o projeto

```powershell
git clone <url-do-repositorio>
cd MultiAgentes

.\setup.ps1                                          # restaura, compila e verifica a chave
dotnet user-secrets set "Gemini:ApiKey" "<sua-chave>"
```

O `setup.ps1` avisa se a chave ainda não está configurada.

### 3. Execute

```powershell
dotnet run
```

```
Modelo: gemini-3.5-flash-lite
Solicitacao: Avalie o pedido de credito do cliente Joao, no valor de R$ 30.000 em 24 parcelas.
----------------------------------------------------------------------
  > Planner: interpretando a solicitacao...
  > Credit + Compliance: executando em paralelo...
  > Reviewer: consolidando pareceres...
  > Response: redigindo resposta ao usuario...
----------------------------------------------------------------------
**Decisao:** Crédito aprovado.

**Por que:**
- Score de 820, bem acima do mínimo de 600 exigido pela política interna
- Parcela de R$ 1.250 representa 10% da renda declarada, dentro do teto de 30%
- Sem registros de inadimplência e sem restrições em listas internas

**Proximos passos:**
- Formalizar a proposta com o cliente
- Registrar a operação no sistema de contratos
----------------------------------------------------------------------
Concluido em 6.4s
```

No Visual Studio 2026, abra `MultiAgentes.sln` e pressione F5.

---

## Uso

```powershell
dotnet run                                    # caso padrão: cliente Joao
dotnet run -- "Posso conceder R$ 8.000 em 12x para a cliente Maria?"
dotnet run -- --detalhes                      # imprime a saída de cada agente
dotnet run -- --schemas                       # imprime o schema das tools, sem chamar a API
```

### Cenários da base simulada

A base foi montada para exercitar caminhos de decisão diferentes:

| Cliente | Score | Renda | Situação | Desfecho esperado |
|---|---|---|---|---|
| **Joao** | 820 | R$ 12.500 | Regular | Aprovação |
| **Maria** | 540 | R$ 3.200 | Inadimplente e em lista restritiva | Bloqueio por compliance |
| **Pedro** | 690 | R$ 7.800 | Regular | Depende do comprometimento de renda |

O caso da Maria é o mais interessante: o parecer financeiro é ruim **e** há bloqueio de compliance. Serve para verificar se o `ReviewerAgent` aplica a regra de precedência corretamente.

### Configuração

O modelo fica em `appsettings.json`:

```json
{
  "AI": {
    "Gemini": {
      "Model": "gemini-3.5-flash-lite"
    }
  }
}
```

A credencial **nunca** vai para o `appsettings.json`. É resolvida nesta ordem:

| Origem | Chave | Uso |
|---|---|---|
| user-secrets | `Gemini:ApiKey` | Desenvolvimento local |
| Variável de ambiente | `GEMINI_API_KEY` | CI, containers, produção |

Em GitHub Actions, registre o segredo em `Settings → Secrets and variables → Actions` e exponha no workflow:

```yaml
- run: dotnet run
  env:
    GEMINI_API_KEY: ${{ secrets.GEMINI_API_KEY }}
```

> [!IMPORTANT]
> `Host.CreateApplicationBuilder` só carrega user-secrets no ambiente `Development`. Um console app sem `DOTNET_ENVIRONMENT` definido roda como `Production`, então o `Program.cs` chama `AddUserSecrets` explicitamente. Sem isso, o segredo é silenciosamente ignorado.

---

## Estrutura do projeto

```
MultiAgentes/
├── Agents/                      Um arquivo por agente, cada um é uma factory
│   ├── PlannerAgent.cs
│   ├── CreditAgent.cs
│   ├── ComplianceAgent.cs
│   ├── ReviewerAgent.cs
│   └── ResponseAgent.cs
├── Tools/
│   ├── CreditTool.cs            Dados financeiros simulados
│   ├── ComplianceTool.cs        Listas restritivas e políticas
│   └── ToolFactory.cs           Cria AIFunction com schema aceito pelo Gemini
├── Models/
│   └── CreditAnalysis.cs        Request e Result (com saída de cada etapa)
├── Workflows/
│   └── CreditWorkflow.cs        Orquestração dos cinco agentes
├── Configuration/
│   └── AIConfiguration.cs       Monta o IChatClient e resolve a credencial
├── Program.cs                   Composition root e CLI
├── appsettings.json
├── setup.ps1                    Restaura, compila e verifica a configuração
├── teste-gemini.ps1             Diagnóstico: requisição HTTP mínima
└── teste-gemini-variacoes.ps1   Diagnóstico: isola qual campo causa erro
```

### Pacotes

| Pacote | Papel |
|---|---|
| [`Microsoft.Agents.AI`](https://www.nuget.org/packages/Microsoft.Agents.AI) | `ChatClientAgent`, `AIAgent` — a abstração de agente |
| [`Microsoft.Extensions.AI`](https://www.nuget.org/packages/Microsoft.Extensions.AI) | `IChatClient`, `AddChatClient`, `AIFunctionFactory` — a camada neutra de provedor |
| [`GeminiDotnet.Extensions.AI`](https://www.nuget.org/packages/GeminiDotnet.Extensions.AI) | `IChatClient` sobre a API nativa do Gemini |
| [`Microsoft.Extensions.Hosting`](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) | `Host.CreateApplicationBuilder`, DI e configuração |

A separação importa: os agentes conhecem apenas `IChatClient`. Trocar de provedor mexe em um arquivo, `AIConfiguration.cs`, e em nenhum agente.

---

## Como funciona

### Um agente

Cada agente é uma classe com um método `Create` que devolve um `ChatClientAgent` configurado:

```csharp
public sealed class CreditAgent(CreditTool tool)
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: """
            Voce e o analista de credito.

            Use SEMPRE as ferramentas disponiveis para obter score, renda e
            situacao de inadimplencia. Nunca invente numeros: se a ferramenta
            nao retornar o dado, diga explicitamente que o dado esta indisponivel.
            ...
            """,
            name: "CreditAgent",
            description: "Consulta historico financeiro, score e inadimplencia do cliente.",
            tools: [ToolFactory.Criar(tool.ConsultarSituacaoFinanceira)]);
}
```

O `name` importa se você migrar para workflows com handoff — é por ele que os outros agentes referenciam este.

O `ChatClientAgent` executa as ferramentas internamente. Não é necessário encadear `.UseFunctionInvocation()` no `IChatClient`, ao contrário do que acontece quando se usa `IChatClient` puro.

### Uma ferramenta

Métodos comuns, anotados com `[Description]`. É essa descrição que o modelo lê para decidir quando chamar a função e o que passar em cada parâmetro:

```csharp
[Description("""
    Consulta a situacao financeira completa de um cliente: score de credito,
    renda mensal e inadimplencia. Se um valor de parcela mensal for informado,
    calcula tambem o comprometimento de renda.
    """)]
public string ConsultarSituacaoFinanceira(
    [Description("Nome do cliente a consultar, por exemplo 'Joao'.")]
    string cliente,
    [Description("Valor da parcela mensal proposta, em reais. Use 0 se nao houver valor definido.")]
    double parcelaMensal = 0)
```

Descrições vagas produzem chamadas erradas com mais frequência do que instruções vagas no agente. Vale investir nelas.

### O workflow

```csharp
// Etapa 1 — planejamento
var plano = (await agentePlanner.RunAsync(request.Solicitacao, ct)).Text;

// Etapa 2 — as duas chamadas são disparadas antes de qualquer await,
// então rodam concorrentemente
var tarefaCredito    = agenteCredito.RunAsync(contexto, ct);
var tarefaCompliance = agenteCompliance.RunAsync(contexto, ct);

var analiseCredito    = (await tarefaCredito).Text;
var analiseCompliance = (await tarefaCompliance).Text;

// Etapas 3 e 4 — revisão e redação
```

Cada `await` está envolvido em um `try/catch` que renomeia a exceção com o agente responsável, para que o erro diga `CreditAgent falhou: ...` em vez de uma stack trace anônima.

---

## Armadilhas do Gemini em .NET

Três decisões neste código parecem estilo, mas são obrigatórias. Nenhuma delas aparece nas mensagens de erro da API.

### 1. Nunca chame `AIFunctionFactory.Create` diretamente

Ele emite `"$schema": "https://json-schema.org/draft/2020-12/schema"` no schema da função. O Gemini aceita apenas um subconjunto de OpenAPI e **recusa a requisição inteira** com `400 INVALID_ARGUMENT` ao encontrar um keyword que não conhece — sem indicar qual campo é o problema.

Use sempre `ToolFactory.Criar`:

```csharp
private static readonly AIFunctionFactoryOptions Opcoes = new()
{
    JsonSchemaCreateOptions = new AIJsonSchemaCreateOptions
    {
        IncludeSchemaKeyword = false
    }
};

public static AIFunction Criar(Delegate metodo) => AIFunctionFactory.Create(metodo, Opcoes);
```

Este foi o **único** cenário reprovado entre seis testados. Múltiplas function declarations, `systemInstruction` e `default` no schema passam sem problema — se você encontrar afirmações contrárias, teste antes de reestruturar o código.

### 2. A cota do free tier é por modelo, por dia

O `gemini-3.6-flash` tem apenas **20 requisições/dia**, e cada execução do workflow consome cinco ou mais — function calling adiciona round-trips. Por isso o padrão aqui é `gemini-3.5-flash-lite`, com cota substancialmente maior.

Ao estourar, o erro é `429 RESOURCE_EXHAUSTED` e o campo `retryDelay` informa quanto esperar. Como a contagem é por modelo, trocar o valor em `appsettings.json` libera uma cota nova.

### 3. Modelos são descontinuados sem aviso no código

O `gemini-2.5-flash` responde `404 no longer available to new users` — mesmo aparecendo na listagem de `ListModels`. Antes de fixar um modelo, valide com o script de diagnóstico.

---

## Diagnóstico

Erros da API do Gemini raramente indicam a causa. Três ferramentas ajudam:

```powershell
.\teste-gemini.ps1                  # requisição HTTP mínima, sem SDK no meio
.\teste-gemini-variacoes.ps1        # seis variações, mostra exatamente qual falha
dotnet run -- --schemas             # o schema que o MEAI gerou, sem consumir cota
```

**`teste-gemini.ps1`** separa problema de API de problema de biblioteca. Se ele retorna `200` e a aplicação falha, a causa está no cliente .NET. Foi assim que o `$schema` foi identificado.

**`teste-gemini-variacoes.ps1`** adiciona, um de cada vez, elementos que um cliente .NET pode enviar a mais, e reporta quais passam:

```
Modelo: gemini-3.5-flash-lite

1. baseline (1 funcao)           OK
2. + systemInstruction           OK
3. 2 funcoes, 1 objeto tools     OK
4. 2 objetos tools separados     OK
5. schema com 'default'          OK
6. schema com $schema            FALHOU
```

Ambos leem a chave do user-secrets automaticamente. Use-os ao trocar de modelo ou de biblioteca cliente.

---

## Caminhos sem saída

Documentado para que a investigação não precise ser repetida.

| Abordagem | O que acontece | Causa |
|---|---|---|
| **GitHub Models** | `HTTP 410 github_models_retirement_brownout` | Serviço retirado em 30/07/2026. Tutoriais que recomendam `models.github.ai/inference` estão desatualizados |
| **Endpoint OpenAI-compatible do Gemini** | Quebra na desserialização | O Gemini devolve `role: "model"` e o SDK OpenAI do .NET valida contra uma lista fixa de cinco roles ([openai-dotnet#289](https://github.com/openai/openai-dotnet/issues/289)). Funciona em Python e TypeScript, não em .NET |
| **`Mscc.GenerativeAI.Microsoft`** | `400 INVALID_ARGUMENT` em qualquer agente com tools | Regera o JSON Schema com `JsonSchema.Net.Generation` em vez de usar o `AIFunction.JsonSchema` que o MEAI já produziu — e o schema regerado inclui `$schema`. Não há como configurar por fora |
| **`gemini-2.5-flash`** | `404 no longer available to new users` | Fechado para contas novas |

O `GeminiDotnet.Extensions.AI` é construído sobre as abstrações do `Microsoft.Extensions.AI` e repassa o schema como está, sem regerar — por isso funciona.

---

## Diferenças em relação ao artigo de referência

Este projeto partiu do artigo [Construindo Sistemas Multiagentes com o MAF](https://macoratti.net/26/08/net_creatingmultiagentswithmaf1.htm), de José Carlos Macoratti. O código publicado lá não compila. As correções:

**1. `OpenAIChatClient` não existe.** O construtor `new OpenAIChatClient(model, apiKey)` foi removido nas versões atuais do `Microsoft.Extensions.AI.OpenAI`.

**2. Tools precisam virar `AIFunction`.** O parâmetro `tools:` do `ChatClientAgent` espera `IList<AITool>`, não a sua classe — `tools: [_tool]` não compila. Os métodos também precisam de `[Description]`.

**3. `usings` ausentes.** Faltavam `Microsoft.Extensions.Hosting` (para `Host`), `Microsoft.Extensions.AI` e `Microsoft.Agents.AI`. Este último é a causa do erro `CS0246: ChatClientAgent could not be found`.

**4. `apiKey` nunca era declarada** no `Program.cs`.

**5. O `WorkflowBuilder` estava incompleto.** Além dos nomes de método não corresponderem à API real — o correto é `AddFanOutEdge` / `AddFanInEdge` / `WithOutputFrom` — o grafo tinha um buraco: nenhuma aresta chegava ao `reviewer`. E `workflow.RunAsync(prompt)` não devolve `string`.

---

## Estendendo o projeto

<details>
<summary><b>Adicionar um agente</b></summary>

Crie a classe em `Agents/` seguindo o padrão de factory, registre no DI e insira no workflow:

```csharp
// Agents/RiskAgent.cs
public sealed class RiskAgent
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: "Calcule o risco da operacao...",
            name: "RiskAgent",
            description: "Calcula o risco da operacao de credito.");
}

// Program.cs
builder.Services.AddSingleton<RiskAgent>();

// Workflows/CreditWorkflow.cs — injete no construtor primário e adicione
// ao bloco paralelo junto de Credit e Compliance
```

</details>

<details>
<summary><b>Adicionar uma ferramenta</b></summary>

Métodos públicos com `[Description]` em parâmetros e retorno. Registre no agente via `ToolFactory.Criar`:

```csharp
tools:
[
    ToolFactory.Criar(tool.ConsultarSituacaoFinanceira),
    ToolFactory.Criar(tool.ConsultarHistoricoPagamentos)
]
```

Prefira uma função que devolve o conjunto a várias funções granulares: cada chamada de ferramenta é um round-trip com o modelo, e a cota do free tier é contada por requisição.

</details>

<details>
<summary><b>Trocar de provedor de LLM</b></summary>

Toda a lógica de provedor está em `Configuration/AIConfiguration.cs`. Nenhum agente precisa mudar, porque todos dependem apenas de `IChatClient`.

Para **OpenAI**:

```csharp
new OpenAIClient(new ApiKeyCredential(chave))
    .GetChatClient("gpt-4.1")
    .AsIChatClient()
```

Para **Ollama** local, sem cota e sem chave:

```powershell
ollama pull qwen3:8b
dotnet add package OllamaSharp
```

```csharp
new OllamaApiClient(new Uri("http://localhost:11434"), "qwen3:8b")
```

Neste caso o `.UseFunctionInvocation()` é necessário, e o modelo precisa suportar tools (`qwen3`, `llama3.1`, `gpt-oss`).

</details>

<details>
<summary><b>Migrar para o WorkflowBuilder do MAF</b></summary>

```powershell
dotnet add package Microsoft.Agents.AI.Workflows
```

```csharp
var workflow = new WorkflowBuilder(startExecutor)
    .AddFanOutEdge(startExecutor, [credit, compliance])
    .AddFanInEdge(aggregator, [credit, compliance])
    .WithOutputFrom(aggregator)
    .Build();
```

Exige executors customizados para o início e a agregação. Há issues abertas sobre fan-in com `ChatClientAgent` — verifique o estado antes de migrar.

</details>

---

## Referências

- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) — documentação oficial
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) — a camada de abstração
- [Gemini API](https://ai.google.dev/gemini-api/docs) — referência da API e limites de cota
- [Macoratti — Construindo Sistemas Multiagentes com o MAF](https://macoratti.net/26/08/net_creatingmultiagentswithmaf1.htm) — artigo que originou o projeto
