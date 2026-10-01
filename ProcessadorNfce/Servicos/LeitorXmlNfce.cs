using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;


public sealed class NotaInvalidaException(string mensagem) : Exception(mensagem);

/// <summary>
/// Lê um arquivo XML de NFC-e e extrai os campos usados no processamento.
///
/// Caminho das tags dentro do XML (layout 4.00 da SEFAZ):
///   infNFe (atributo Id)          -> chave de acesso
///   infNFe/ide/mod                -> modelo (65 = NFC-e, 55 = NF-e)
///   infNFe/ide/serie              -> série
///   infNFe/ide/dhEmi              -> data de emissão (competência)
///   infNFe/dest/CPF | dest/CNPJ   -> identificação do comprador
/// </summary>
public static class LeitorXmlNfce
{
    /// <summary>
    /// Lê o XML e devolve os dados da nota.
    /// Lança <see cref="XmlException"/> se o arquivo estiver corrompido e
    /// <see cref="NotaInvalidaException"/> se não for uma NFC-e válida.
    /// </summary>
    public static NotaFiscal Ler(string caminhoArquivo)
    {
        XDocument documento = CarregarXml(caminhoArquivo);

        // As tags da NFC-e ficam no namespace "http://www.portalfiscal.inf.br/nfe".
        // Buscamos pelo nome local (LocalName) para funcionar tanto com o XML só da nota (<NFe>)
        // quanto com o XML autorizado (<nfeProc>), que envolve a nota junto com o protocolo da SEFAZ.
        XElement infNFe = documento.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe")
            ?? throw new NotaInvalidaException("tag <infNFe> não encontrada, o arquivo não é uma NFC-e");

        XElement? ide = Filho(infNFe, "ide");
        XElement? dest = Filho(infNFe, "dest"); 

        return new NotaFiscal
        {
            Arquivo = Path.GetFileName(caminhoArquivo),
            Chave = LerChave(infNFe),
            Modelo = TextoObrigatorio(Filho(ide, "mod"), "<ide><mod>"),
            Serie = TextoObrigatorio(Filho(ide, "serie"), "<ide><serie>"),
            DataEmissao = LerDataEmissao(ide),
            CpfDestinatario = Texto(Filho(dest, "CPF")),
            CnpjDestinatario = Texto(Filho(dest, "CNPJ")),
        };
    }

    private static XDocument CarregarXml(string caminhoArquivo)
    {
        try
        {
            return XDocument.Load(caminhoArquivo);
        }
        catch (XmlException erroOriginal)
        {

            try
            {
                string conteudo = File.ReadAllText(caminhoArquivo, Encoding.Latin1);
                return XDocument.Parse(conteudo);
            }
            catch (XmlException)
            {
                throw erroOriginal;
            }
        }
    }

    private static string LerChave(XElement infNFe)
    {
        string id = infNFe.Attribute("Id")?.Value.Trim()
            ?? throw new NotaInvalidaException("atributo Id da tag <infNFe> não encontrado");

        string chave = id.StartsWith("NFe", StringComparison.OrdinalIgnoreCase) ? id[3..] : id;

        if (chave.Length != 44 || !chave.All(char.IsAsciiDigit))
            throw new NotaInvalidaException($"chave de acesso inválida no atributo Id (\"{id}\"), esperado 44 dígitos");

        return chave;
    }


    private static DateTimeOffset LerDataEmissao(XElement? ide)
    {
        string texto = TextoObrigatorio(Filho(ide, "dhEmi"), "<ide><dhEmi>");

        if (!DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset data))
            throw new NotaInvalidaException($"data de emissão inválida em <dhEmi> (\"{texto}\")");

        return data;
    }

    private static XElement? Filho(XElement? pai, string nome) =>
        pai?.Elements().FirstOrDefault(e => e.Name.LocalName == nome);

    private static string? Texto(XElement? elemento) =>
        string.IsNullOrWhiteSpace(elemento?.Value) ? null : elemento.Value.Trim();
    private static string TextoObrigatorio(XElement? elemento, string descricaoTag) =>
        Texto(elemento) ?? throw new NotaInvalidaException($"tag {descricaoTag} não encontrada ou vazia");
}
