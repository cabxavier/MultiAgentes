using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MultiAgentes.Agents;

/// <summary>
/// Traduz a decisao tecnica em uma resposta para o usuario final.
/// </summary>
public sealed class ResponseAgent
{
    public ChatClientAgent Create(IChatClient chatClient) =>
        new(chatClient,
            instructions: """
            Voce redige a resposta final ao solicitante.

            Recebe a decisao consolidada do revisor. Transforme-a em uma
            resposta clara, em portugues, com esta estrutura:

            **Decisao:** <uma linha>
            **Por que:** <2 a 4 bullets com os fatos que sustentam a decisao>
            **Proximos passos:** <1 a 3 bullets>

            Nao invente fatos que nao estejam nos pareceres recebidos.
            Nao use jargao interno. Seja direto e cordial.
            """,
            name: "ResponseAgent",
            description: "Produz a resposta final ao usuario a partir da decisao consolidada.");
}
