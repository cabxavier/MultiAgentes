using GeminiDotnet;
using GeminiDotnet.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace MultiAgentes.Configuration;

/// <summary>
/// Monta o <see cref="IChatClient"/> usado por todos os agentes.
///
/// O modelo vem de "AI:Gemini:Model" no appsettings.json. A chave NUNCA fica
/// no appsettings — e resolvida, nesta ordem:
///   1) user-secrets / configuracao ("Gemini:ApiKey")  -> desenvolvimento
///   2) variavel de ambiente GEMINI_API_KEY            -> CI, container, prod
///
/// Historico das alternativas descartadas, para nao repetir a investigacao:
///
/// - GitHub Models: retirado em 30/07/2026, responde HTTP 410.
///
/// - Endpoint OpenAI-compatible do Gemini
///   (generativelanguage.googleapis.com/v1beta/openai/): nao funciona com o
///   SDK OpenAI do .NET. O Gemini devolve role "model" e o SDK valida contra
///   uma lista fixa de cinco roles, quebrando na desserializacao.
///   Ver https://github.com/openai/openai-dotnet/issues/289
///
/// - Mscc.GenerativeAI.Microsoft: emite o keyword "$schema" nas function
///   declarations, porque regera o schema com JsonSchema.Net.Generation em vez
///   de usar o AIFunction.JsonSchema ja produzido pelo MEAI. O Gemini recusa
///   com 400 INVALID_ARGUMENT, sem indicar a causa. Comprovado por
///   teste-gemini-variacoes.ps1: todos os cenarios passam, exceto "$schema".
///
/// GeminiDotnet.Extensions.AI e escrito sobre as abstracoes do
/// Microsoft.Extensions.AI e repassa o schema como esta, sem regerar.
/// </summary>
public sealed class AIConfiguration
{
    public const string SectionName = "AI";

    public GeminiOptions Gemini { get; set; } = new();

    public sealed class GeminiOptions
    {
        /// <summary>
        /// Cota do free tier e por modelo, por dia. O gemini-3.6-flash tem
        /// apenas 20 requisicoes/dia; flash-lite tem cota bem maior e da conta
        /// deste workflow, que gasta 5 ou mais chamadas por execucao.
        /// </summary>
        public string Model { get; set; } = "gemini-3.5-flash-lite";
    }

    public static AIConfiguration Load(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<AIConfiguration>() ?? new AIConfiguration();

    public string ModeloEmUso => Gemini.Model;

    public IChatClient CreateChatClient(IConfiguration configuration)
    {
        // O modelo e aplicado via ConfigureOptions em vez do construtor: assim
        // uma chamada especifica pode sobrescrever o ModelId se precisar.
        return new GeminiChatClient(new GeminiClientOptions { ApiKey = ResolveApiKey(configuration) })
            .AsBuilder()
            .ConfigureOptions(o => o.ModelId ??= Gemini.Model)
            .Build();
    }

    private static string ResolveApiKey(IConfiguration configuration)
    {
        var valor = configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(valor))
            valor = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException("""
                Chave da API do Gemini nao encontrada.

                Obtenha em: https://aistudio.google.com/apikey

                Depois configure de uma destas formas:
                  dotnet user-secrets set "Gemini:ApiKey" "<chave>"
                  $env:GEMINI_API_KEY = "<chave>"
                """);

        return valor;
    }
}
