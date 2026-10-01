using System.Xml;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public sealed class ProcessadorNotas(string pastaXml)
{
    public ResultadoProcessamento Processar(FiltroNfce filtro)
    {
        var resultado = new ResultadoProcessamento();
        var chavesAprovadas = new HashSet<string>(); 

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
                    resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, "nota duplicada"));
                else
                    resultado.Aprovadas.Add(nota);
            }
            
            catch (XmlException ex)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, $"XML corrompido (linha {ex.LineNumber})"));
            }
            catch (NotaInvalidaException ex)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, "sem permissão de leitura"));
            }
            catch (IOException)
            {
                
                resultado.Erros.Add(new ArquivoIgnorado(nomeArquivo, "erro ao ler o arquivo"));
            }
        }

        return resultado;
    }
}
