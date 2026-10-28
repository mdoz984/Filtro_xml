using System.Text;
using System.Text.Json;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public static class GeradorEnvioJson
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    public static List<ArquivoGerado> Gerar(IEnumerable<NotaFiscal> notas, string competencia, string pastaSaida)
    {
        var arquivos = new List<ArquivoGerado>();

        // o nome usa só os 5 primeiros dígitos do CNPJ, então matriz e filial com o mesmo início caem no mesmo arquivo
        foreach (IGrouping<string, NotaFiscal> grupo in notas.GroupBy(nota => nota.Chave.Cnpj[..5]).OrderBy(g => g.Key))
        {
            List<NotaEnvio> envio = grupo
                .OrderBy(nota => nota.Chave.NumeroNota, StringComparer.Ordinal)
                .ThenBy(nota => nota.Chave.Digitos, StringComparer.Ordinal)
                .Select((nota, indice) => new NotaEnvio(indice + 1, nota.Chave.Digitos, nota.Competencia))
                .ToList();

            string nomeArquivo = $"{competencia.Replace("/", "")}{grupo.Key}.txt";
            string caminhoSaida = Path.Combine(pastaSaida, nomeArquivo);
            string json = JsonSerializer.Serialize(envio, Opcoes);

            File.WriteAllText(caminhoSaida, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            arquivos.Add(new ArquivoGerado(caminhoSaida, envio.Count));
        }

        return arquivos;
    }
}
