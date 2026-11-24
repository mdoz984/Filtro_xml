using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public sealed class NotaInvalidaException(string mensagem) : Exception(mensagem);

public static class LeitorXmlNfce
{
    public static XElement LerInfNFe(string caminhoArquivo)
    {
        XDocument documento = CarregarXml(caminhoArquivo);

        // busca pelo nome local para ignorar o namespace (funciona com <NFe> e com <nfeProc>)
        return documento.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe")
            ?? throw new NotaInvalidaException("tag <infNFe> não encontrada, o arquivo não é uma NFC-e");
    }

    public static ChaveAcesso LerChave(XElement infNFe)
    {
        string id = infNFe.Attribute("Id")?.Value.Trim()
            ?? throw new NotaInvalidaException("atributo Id da tag <infNFe> não encontrado");

        string digitos = id.StartsWith("NFe", StringComparison.OrdinalIgnoreCase) ? id[3..] : id;

        string? motivo = ChaveAcesso.MotivoInvalida(digitos);
        if (motivo is not null)
            throw new NotaInvalidaException($"chave de acesso inválida no atributo Id (\"{id}\"), {motivo}");

        return new ChaveAcesso(digitos);
    }

    public static NotaFiscal LerNota(XElement infNFe, ChaveAcesso chave, string arquivo)
    {
        XElement? ide = Filho(infNFe, "ide");
        XElement? dest = Filho(infNFe, "dest");

        return new NotaFiscal
        {
            Arquivo = arquivo,
            Chave = chave,
            Serie = TextoObrigatorio(Filho(ide, "serie"), "<ide><serie>").PadLeft(3, '0'),
            DataEmissao = LerDataEmissao(ide),
            CStat = LerCStat(infNFe),
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
        catch (XmlException)
        {
            // tem emissor que grava em ISO-8859-1 mas declara UTF-8, então tenta de novo como Latin-1
            XDocument? documentoLatin1 = TentarCarregarLatin1(caminhoArquivo);
            if (documentoLatin1 is not null)
                return documentoLatin1;

            throw;
        }
    }

    private static XDocument? TentarCarregarLatin1(string caminhoArquivo)
    {
        try
        {
            return XDocument.Parse(File.ReadAllText(caminhoArquivo, Encoding.Latin1));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    // o cStat fica fora do <infNFe>, no protocolo: <nfeProc><protNFe><infProt><cStat>
    private static string? LerCStat(XElement infNFe)
    {
        XElement? infProt = infNFe.Document?.Descendants().FirstOrDefault(e => e.Name.LocalName == "infProt");
        return Texto(Filho(infProt, "cStat"));
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
