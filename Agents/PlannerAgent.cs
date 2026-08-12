using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MultiAgentes.Agents;

/// <summary>
/// Interpreta a solicitacao do usuario e decide quais verificacoes rodar.
/// Nao tem tools: e um agente puramente de raciocinio.
/// </summary>
public sealed class PlannerAgent
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: """
            Voce e o agente planejador de um sistema de analise de credito.

            A partir da solicitacao do usuario:
            1. Identifique o nome do cliente envolvido.
            2. Identifique o valor e o prazo, se informados.
            3. Liste, de forma objetiva, quais verificacoes sao necessarias
               (analise financeira, compliance, ou ambas).

            Responda em no maximo 6 linhas, em portugues, no formato:
            Cliente: <nome>
            Operacao: <valor e prazo, ou "nao informado">
            Verificacoes: <lista curta>

            Nao faca julgamento sobre aprovar ou negar. Isso nao e seu papel.
            """,
            name: "PlannerAgent",
            description: "Interpreta o pedido e define quais verificacoes serao necessarias.");
}
