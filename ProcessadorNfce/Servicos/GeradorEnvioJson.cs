using System.Text;
using System.Text.Json;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

/// <summary>Gera o arquivo envio.json com as notas aprovadas.</summary>
public static class GeradorEnvioJson
{
    // JSON indentado para facilitar a conferência humana do arquivo.
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    /// <summary>
    /// Converte as notas para o formato de envio e grava o arquivo.
    /// Se nenhuma nota for aprovada, grava uma lista vazia ([]).
    /// </summary>
    public static void Gerar(IEnumerable<NotaFiscal> notas, string caminhoSaida)
    {
        List<NotaEnvio> envio = notas
            .Select(nota => new NotaEnvio(nota.Chave, nota.Competencia, nota.Valor))
            .ToList();

        string json = JsonSerializer.Serialize(envio, Opcoes);

        // UTF-8 sem BOM, que é o que APIs normalmente esperam.
        File.WriteAllText(caminhoSaida, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
