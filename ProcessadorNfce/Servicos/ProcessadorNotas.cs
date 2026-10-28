using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public sealed class ProcessadorNotas(string pastaXml)
{
    private static readonly Regex ChaveNoNome = new(@"(?<!\d)\d{44}(?!\d)");

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
            Match chaveNome = ChaveNoNome.Match(nomeArquivo);

            if (chaveNome.Success && ChaveAcesso.MotivoInvalida(chaveNome.Value) is null
                && filtro.MotivoDescarte(new ChaveAcesso(chaveNome.Value)) is string motivoNome)
            {
                resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, $"{motivoNome} (chave no nome do arquivo)"));
                continue;
            }

            try
            {
                XElement infNFe = LeitorXmlNfce.LerInfNFe(caminho);
                ChaveAcesso chave = LeitorXmlNfce.LerChave(infNFe);

                if (chaveNome.Success && chaveNome.Value != chave.Digitos)
                    throw new NotaInvalidaException("chave do nome do arquivo diferente da chave do Id");

                string? motivoChave = filtro.MotivoDescarte(chave);
                if (motivoChave is not null)
                {
                    resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, motivoChave));
                    continue;
                }

                NotaFiscal nota = LeitorXmlNfce.LerNota(infNFe, chave, nomeArquivo);
                string? motivo = filtro.MotivoDescarte(nota);

                if (motivo is not null)
                    resultado.Descartadas.Add(new ArquivoIgnorado(nomeArquivo, motivo));
                else if (!chavesAprovadas.Add(chave.Digitos))
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
