using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiAgentes.Agents;
using MultiAgentes.Configuration;
using MultiAgentes.Models;
using MultiAgentes.Tools;
using MultiAgentes.Workflows;

var builder = Host.CreateApplicationBuilder(args);

// Host.CreateApplicationBuilder so carrega user-secrets no ambiente
// Development. Como este e um console app sem DOTNET_ENVIRONMENT definido,
// o ambiente padrao e Production — por isso carregamos explicitamente.
builder.Configuration.AddUserSecrets(
    System.Reflection.Assembly.GetExecutingAssembly(), optional: true);

// --- Cliente do modelo ------------------------------------------------------
var aiConfig = AIConfiguration.Load(builder.Configuration);

// AddChatClient registra o IChatClient no container.
// UseFunctionInvocation nao e necessario: o ChatClientAgent ja executa as
// tools internamente.
builder.Services.AddChatClient(aiConfig.CreateChatClient(builder.Configuration));

// --- Tools ------------------------------------------------------------------
builder.Services.AddSingleton<CreditTool>();
builder.Services.AddSingleton<ComplianceTool>();

// --- Agents -----------------------------------------------------------------
builder.Services.AddSingleton<PlannerAgent>();
builder.Services.AddSingleton<CreditAgent>();
builder.Services.AddSingleton<ComplianceAgent>();
builder.Services.AddSingleton<ReviewerAgent>();
builder.Services.AddSingleton<ResponseAgent>();

// --- Workflow ---------------------------------------------------------------
builder.Services.AddSingleton<CreditWorkflow>();

var app = builder.Build();

// --- Diagnostico: dotnet run -- --schemas -----------------------------------
// Imprime o JSON Schema de cada tool sem chamar a API (nao consome cota).
// Util quando o provedor devolve 400 INVALID_ARGUMENT e voce precisa ver o
// que foi realmente gerado.
if (args.Contains("--schemas"))
{
    AIFunction[] funcoes =
    [
        ToolFactory.Criar(app.Services.GetRequiredService<CreditTool>().ConsultarSituacaoFinanceira),
        ToolFactory.Criar(app.Services.GetRequiredService<ComplianceTool>().VerificarCompliance)
    ];

    foreach (var f in funcoes)
    {
        Console.WriteLine($"=== {f.Name} ===");
        Console.WriteLine(f.JsonSchema);
        Console.WriteLine();
    }

    return 0;
}

// --- Execucao ---------------------------------------------------------------
// Qualquer argumento que nao seja uma flag vira a solicitacao.
var solicitacao = args.Where(a => !a.StartsWith("--")).ToArray() is { Length: > 0 } texto
    ? string.Join(' ', texto)
    : "Avalie o pedido de credito do cliente Joao, no valor de R$ 30.000 em 24 parcelas.";

var detalhado = args.Contains("--detalhes");
var separador = new string('-', 70);

Console.WriteLine($"Modelo: {aiConfig.ModeloEmUso}");
Console.WriteLine($"Solicitacao: {solicitacao}");
Console.WriteLine(separador);

var progresso = new Progress<string>(msg => Console.WriteLine($"  > {msg}"));
var workflow = app.Services.GetRequiredService<CreditWorkflow>();

try
{
    var resultado = await workflow.RunAsync(new CreditAnalysisRequest(solicitacao), progresso);

    if (detalhado)
    {
        Console.WriteLine($"{separador}\n[PLANO]\n{resultado.Plano}");
        Console.WriteLine($"\n[CREDITO]\n{resultado.AnaliseCredito}");
        Console.WriteLine($"\n[COMPLIANCE]\n{resultado.AnaliseCompliance}");
        Console.WriteLine($"\n[REVISAO]\n{resultado.Revisao}");
    }

    Console.WriteLine(separador);
    Console.WriteLine(resultado.RespostaFinal);
    Console.WriteLine(separador);
    Console.WriteLine($"Concluido em {resultado.Duracao.TotalSeconds:N1}s");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Falha na execucao do workflow: {ex.Message}");
    return 1;
}

return 0;
