using System.Diagnostics;
using Microsoft.Extensions.AI;
using MultiAgentes.Agents;
using MultiAgentes.Models;

namespace MultiAgentes.Workflows;

/// <summary>
/// Orquestra os cinco agentes:
///
///              Planner
///                 |
///        +--------+--------+
///        |                 |
///     Credit          Compliance     (em paralelo)
///        +--------+--------+
///                 |
///              Reviewer
///                 |
///              Response
///
/// A orquestracao e feita em C# puro, e nao com o WorkflowBuilder do MAF.
/// E deliberadamente explicito: cada agente recebe apenas o contexto de que
/// precisa, o resultado de cada etapa fica visivel para auditoria, e os erros
/// apontam qual agente falhou. O passo paralelo dispara as duas chamadas antes
/// do primeiro await, entao elas rodam concorrentemente sem precisar do grafo.
/// </summary>
public sealed class CreditWorkflow(
    IChatClient chatClient,
    PlannerAgent planner,
    CreditAgent credit,
    ComplianceAgent compliance,
    ReviewerAgent reviewer,
    ResponseAgent response)
{
    public async Task<CreditAnalysisResult> RunAsync(
        CreditAnalysisRequest request,
        IProgress<string>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();

        var agentePlanner    = planner.Create(chatClient);
        var agenteCredito    = credit.Create(chatClient);
        var agenteCompliance = compliance.Create(chatClient);
        var agenteRevisor    = reviewer.Create(chatClient);
        var agenteResposta   = response.Create(chatClient);

        // --- Etapa 1: planejamento ------------------------------------------
        progresso?.Report("Planner: interpretando a solicitacao...");
        var plano = (await agentePlanner.RunAsync(
            request.Solicitacao,
            cancellationToken: cancellationToken)).Text;

        // --- Etapa 2: credito e compliance em paralelo ----------------------
        progresso?.Report("Credit + Compliance: executando em paralelo...");

        var contexto = $"""
            Solicitacao original do usuario:
            {request.Solicitacao}

            Plano de verificacao definido pelo Planner:
            {plano}
            """;

        // As duas chamadas sao disparadas antes de qualquer await,
        // entao rodam concorrentemente.
        var tarefaCredito = agenteCredito.RunAsync(contexto, cancellationToken: cancellationToken);
        var tarefaCompliance = agenteCompliance.RunAsync(contexto, cancellationToken: cancellationToken);

        string analiseCredito;
        string analiseCompliance;

        try
        {
            analiseCredito = (await tarefaCredito).Text;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"CreditAgent falhou: {ex.Message}", ex);
        }

        try
        {
            analiseCompliance = (await tarefaCompliance).Text;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"ComplianceAgent falhou: {ex.Message}", ex);
        }

        // --- Etapa 3: revisao ------------------------------------------------
        progresso?.Report("Reviewer: consolidando pareceres...");
        var entradaRevisor = $"""
            Solicitacao original:
            {request.Solicitacao}

            Plano:
            {plano}

            Parecer do CreditAgent:
            {analiseCredito}

            Parecer do ComplianceAgent:
            {analiseCompliance}
            """;

        var revisao = (await agenteRevisor.RunAsync(
            entradaRevisor,
            cancellationToken: cancellationToken)).Text;

        // --- Etapa 4: resposta final ----------------------------------------
        progresso?.Report("Response: redigindo resposta ao usuario...");
        var entradaResposta = $"""
            Solicitacao original:
            {request.Solicitacao}

            Decisao consolidada do revisor:
            {revisao}

            Fatos apurados pelo CreditAgent:
            {analiseCredito}

            Fatos apurados pelo ComplianceAgent:
            {analiseCompliance}
            """;

        var respostaFinal = (await agenteResposta.RunAsync(
            entradaResposta,
            cancellationToken: cancellationToken)).Text;

        cronometro.Stop();

        return new CreditAnalysisResult
        {
            Plano = plano,
            AnaliseCredito = analiseCredito,
            AnaliseCompliance = analiseCompliance,
            Revisao = revisao,
            RespostaFinal = respostaFinal,
            Duracao = cronometro.Elapsed
        };
    }

    /// <summary>Atalho para quem so quer o texto final.</summary>
    public async Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default)
        => (await RunAsync(new CreditAnalysisRequest(prompt), null, cancellationToken)).RespostaFinal;
}
