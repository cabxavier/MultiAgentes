namespace MultiAgentes.Models;

/// <summary>Pedido de analise submetido ao workflow.</summary>
public sealed record CreditAnalysisRequest(string Solicitacao);

/// <summary>
/// Resultado consolidado. Guarda a saida de cada etapa para que o
/// fluxo seja auditavel, e nao apenas a resposta final.
/// </summary>
public sealed record CreditAnalysisResult
{
    public required string Plano { get; init; }
    public required string AnaliseCredito { get; init; }
    public required string AnaliseCompliance { get; init; }
    public required string Revisao { get; init; }
    public required string RespostaFinal { get; init; }
    public TimeSpan Duracao { get; init; }

    public override string ToString() => RespostaFinal;
}
