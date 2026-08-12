using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgentes.Tools;

namespace MultiAgentes.Agents;

/// <summary>
/// Analisa a saude financeira do cliente. Recebe as tools de credito.
/// </summary>
public sealed class CreditAgent(CreditTool tool)
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: """
            Voce e o analista de credito.

            Use SEMPRE as ferramentas disponiveis para obter score, renda e
            situacao de inadimplencia. Nunca invente numeros: se a ferramenta
            nao retornar o dado, diga explicitamente que o dado esta indisponivel.

            Se um valor de parcela for mencionado, calcule o comprometimento de renda.

            Responda em portugues, em no maximo 8 linhas, cobrindo:
            - Score e o que ele indica
            - Renda e comprometimento (se aplicavel)
            - Inadimplencia
            - Sua recomendacao do ponto de vista financeiro
            """,
            name: "CreditAgent",
            description: "Consulta historico financeiro, score e inadimplencia do cliente.",
            // Sempre ToolFactory.Criar, nunca AIFunctionFactory.Create direto:
            // o Gemini recusa o keyword "$schema" que vem no schema padrao.
            tools:
            [
                ToolFactory.Criar(tool.ConsultarSituacaoFinanceira)
            ]);
}
