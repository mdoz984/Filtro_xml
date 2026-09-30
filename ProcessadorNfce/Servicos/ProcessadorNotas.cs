using System.Xml;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

/// <summary>
/// Percorre a pasta de XMLs, lê cada arquivo, aplica o filtro e separa o resultado.
/// Um arquivo com problema nunca interrompe o processamento dos demais.
/// </summary>
/// <param name="pastaXml">Pasta onde estão os XMLs das notas (C:\XmlNfce).</param>
public sealed class ProcessadorNotas(string pastaXml)
{
    /// <summary>
    /// Processa todos os .xml da pasta.
    /// Lança <see cref="DirectoryNotFoundException"/> ou <see cref="UnauthorizedAccessException"/>
    /// se a própria pasta não existir ou não puder ser lida (tratado no Program).
    /// </summary>
    public ResultadoProcessamento Processar(FiltroNfce filtro)
    {
        var resultado = new ResultadoProcessamento();
        var chavesAprovadas = new HashSet<string>(); // evita mandar a mesma nota duas vezes

        // Critério 1 - Localização: apenas arquivos .xml que estão diretamente na pasta
        // (subpastas não são lidas). A comparação da extensão ignora maiúsculas/minúsculas.
        List<string> arquivos = Directory.GetFiles(pastaXml, "*", SearchOption.TopDirectoryOnly)
            .Where(caminho => Path.GetExtension(caminho).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (string caminho in arquivos)
        {
            string nomeArquivo = Path.GetFileName(caminho);

            try
            {
                NotaFiscal nota = LeitorXmlNfce.Ler(caminho);
                string? motivo = filtro.MotivoDescarte(nota);

                if (motivo is not null)
                    resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, motivo));
                else if (!chavesAprovadas.Add(nota.Chave))
                    resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, "nota duplicada (mesma chave de outro arquivo)"));
                else
                    resultado.Aprovadas.Add(nota);
            }
            // Cada tipo de problema vira uma mensagem clara no relatório, e seguimos para o próximo arquivo.
            catch (XmlException ex)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo,
                    $"XML corrompido ou mal formado (linha {ex.LineNumber}, posição {ex.LinePosition})"));
            }
            catch (NotaInvalidaException ex)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, "sem permissão de leitura no arquivo"));
            }
            catch (IOException ex)
            {
                // Ex.: arquivo aberto/bloqueado por outro programa.
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, $"erro ao ler o arquivo: {ex.Message}"));
            }
        }

        return resultado;
    }
}
