using Microsoft.Extensions.AI;

namespace MultiAgentes.Tools;

/// <summary>
/// Fabrica de tools compativel com o Gemini.
///
/// O AIFunctionFactory, por padrao, emite o keyword "$schema"
/// ("https://json-schema.org/draft/2020-12/schema") no schema da funcao.
/// O Gemini aceita apenas um subconjunto de OpenAPI e recusa a requisicao
/// inteira com 400 INVALID_ARGUMENT quando encontra keywords desconhecidos.
///
/// Use sempre <see cref="Criar"/> no lugar de AIFunctionFactory.Create
/// direto, para que todos os agentes gerem schemas aceitos.
/// </summary>
public static class ToolFactory
{
    private static readonly AIFunctionFactoryOptions Opcoes = new()
    {
        JsonSchemaCreateOptions = new AIJsonSchemaCreateOptions
        {
            IncludeSchemaKeyword = false
        }
    };

    public static AIFunction Criar(Delegate metodo) => AIFunctionFactory.Create(metodo, Opcoes);
}
