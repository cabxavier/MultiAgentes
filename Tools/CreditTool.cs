using System.ComponentModel;
using System.Text;

namespace MultiAgentes.Tools;

/// <summary>
/// Fonte de dados financeiros. Em producao isso bateria num bureau de credito
/// ou num banco de dados interno; aqui os dados sao simulados.
///
/// As consultas estao consolidadas numa unica funcao por escolha de projeto,
/// nao por limitacao: o Gemini aceita varias function declarations sem
/// problema. Uma chamada que ja devolve tudo economiza round-trips com o
/// modelo — relevante porque a cota do free tier e contada por requisicao.
///
/// Tipos: os parametros expostos ao modelo usam double. Converta para decimal
/// dentro do metodo quando precisar de precisao monetaria.
/// </summary>
public sealed class CreditTool
{
    private static readonly Dictionary<string, (int Score, double Renda, bool Inadimplente)> Base =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Joao"] = (820, 12_500d, false),
            ["Joao Silva"] = (820, 12_500d, false),
            ["Maria"] = (540, 3_200d, true),
            ["Pedro"] = (690, 7_800d, false),
        };

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
    {
        if (!Base.TryGetValue(cliente.Trim(), out var d))
            return $"Cliente '{cliente}' nao encontrado na base de credito.";

        var sb = new StringBuilder();
        var situacao = d.Inadimplente ? "COM registros de inadimplencia" : "SEM registros de inadimplencia";

        sb.AppendLine($"Cliente: {cliente}");
        sb.AppendLine($"Score: {d.Score}");
        sb.AppendLine($"Renda mensal declarada: R$ {d.Renda:N2}");
        sb.AppendLine($"Inadimplencia: {situacao}");

        if (parcelaMensal > 0 && d.Renda > 0)
        {
            var percentual = (decimal)parcelaMensal / (decimal)d.Renda * 100m;
            sb.AppendLine($"Parcela proposta: R$ {parcelaMensal:N2}");
            sb.AppendLine($"Comprometimento de renda: {percentual:N1}%");
        }
        else if (parcelaMensal > 0)
        {
            sb.AppendLine("Comprometimento de renda: nao calculavel, renda nao informada.");
        }

        return sb.ToString();
    }
}
