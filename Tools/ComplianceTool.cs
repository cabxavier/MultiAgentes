using System.ComponentModel;
using System.Text;

namespace MultiAgentes.Tools;

/// <summary>
/// Consultas de compliance: listas restritivas e politicas internas.
/// Dados simulados.
///
/// Consolidada numa unica funcao pelo mesmo motivo descrito em CreditTool:
/// menos round-trips com o modelo, e portanto menos consumo de cota.
/// </summary>
public sealed class ComplianceTool
{
    private static readonly HashSet<string> ListaRestritiva =
        new(StringComparer.OrdinalIgnoreCase) { "Maria" };

    private const string Politicas =
        """
        Politicas internas de credito vigentes:
        1. Score minimo de 600 para credito pessoal sem garantia.
        2. Comprometimento de renda maximo de 30%.
        3. Clientes em lista restritiva nao podem receber credito.
        4. Operacoes acima de R$ 50.000 exigem aprovacao do comite.
        """;

    [Description("""
        Verifica a situacao de compliance de um cliente: consulta listas
        restritivas (PEP, sancoes, negativacao interna) e retorna as politicas
        internas de concessao de credito vigentes para confronto.
        """)]
    public string VerificarCompliance(
        [Description("Nome do cliente a verificar.")]
        string cliente)
    {
        var nome = cliente.Trim();
        var sb = new StringBuilder();

        sb.AppendLine(ListaRestritiva.Contains(nome)
            ? $"ATENCAO: cliente {nome} consta em lista restritiva interna. Operacao bloqueada por politica."
            : $"Nenhuma restricao encontrada para o cliente {nome}.");

        sb.AppendLine();
        sb.AppendLine(Politicas);

        return sb.ToString();
    }
}
