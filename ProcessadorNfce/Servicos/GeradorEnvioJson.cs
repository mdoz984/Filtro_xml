using System.Text;
using System.Text.Json;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public static class GeradorEnvioJson
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };


    public static void Gerar(IEnumerable<NotaFiscal> notas, string caminhoSaida)
    {
        List<NotaEnvio> envio = notas
            .Select(nota => new NotaEnvio(nota.Chave, nota.Competencia))
            .ToList();

        string json = JsonSerializer.Serialize(envio, Opcoes);


        File.WriteAllText(caminhoSaida, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
