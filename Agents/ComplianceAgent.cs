using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MultiAgentes.Tools;

namespace MultiAgentes.Agents;

/// <summary>
/// Verifica listas restritivas e politicas internas.
/// </summary>
public sealed class ComplianceAgent(ComplianceTool tool)
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: """
            Voce e o analista de compliance.

            Use SEMPRE as ferramentas disponiveis para (a) consultar listas
            restritivas e (b) obter as politicas internas vigentes.

            Confronte o caso concreto com cada politica aplicavel e diga,
            para cada uma, se foi atendida, violada ou se nao ha dado suficiente.

            Responda em portugues, em no maximo 8 linhas. Termine com uma linha
            unica: "Parecer de compliance: APROVADO" ou
            "Parecer de compliance: BLOQUEADO" ou
            "Parecer de compliance: PENDENTE DE INFORMACAO".
            """,
            name: "ComplianceAgent",
            description: "Verifica listas restritivas e politicas internas de credito.",
            // Ver comentario em CreditAgent sobre ToolFactory.
            tools:
            [
                ToolFactory.Criar(tool.VerificarCompliance)
            ]);
}
