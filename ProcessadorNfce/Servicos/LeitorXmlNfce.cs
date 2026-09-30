using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

/// <summary>
/// O arquivo é um XML bem formado, mas não tem a estrutura esperada de uma NFC-e
/// (falta uma tag obrigatória, a chave é inválida, o valor não é numérico etc.).
/// </summary>
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
///   infNFe/total/ICMSTot/vNF      -> valor total da nota
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
        XElement? dest = Filho(infNFe, "dest"); // na venda para consumidor final essa tag costuma nem existir

        return new NotaFiscal
        {
            Arquivo = Path.GetFileName(caminhoArquivo),
            Chave = LerChave(infNFe),
            Modelo = TextoObrigatorio(Filho(ide, "mod"), "<ide><mod>"),
            Serie =TextoObrigatorio(Filho(ide, "serie"), "<ide><serie>"),
            DataEmissao = LerDataEmissao(ide),
            Valor = LerValor(infNFe),
            CpfDestinatario = Texto(Filho(dest, "CPF")),
            CnpjDestinatario = Texto(Filho(dest, "CNPJ")),
        };
    }

    /// <summary>
    /// Carrega o XML do disco tratando as variações de codificação mais comuns.
    /// </summary>
    private static XDocument CarregarXml(string caminhoArquivo)
    {
        try
        {
            // XDocument.Load detecta a codificação sozinho, pelo BOM ou pela
            // declaração <?xml ... encoding="UTF-8"?> / encoding="ISO-8859-1".
            return XDocument.Load(caminhoArquivo);
        }
        catch (XmlException erroOriginal)
        {
            // Alguns sistemas gravam o arquivo em ISO-8859-1 (Latin-1), mas declaram UTF-8.
            // Aí os acentos (ex.: "SÃO JOSÉ") quebram a leitura. Tentamos mais uma vez
            // interpretando os bytes como Latin-1 antes de considerar o arquivo corrompido.
            try
            {
                string conteudo = File.ReadAllText(caminhoArquivo, Encoding.Latin1);
                return XDocument.Parse(conteudo);
            }
            catch (XmlException)
            {
                // Continua inválido: o arquivo está mesmo corrompido. Repassamos o erro original.
                throw erroOriginal;
            }
        }
    }

    /// <summary>
    /// Chave de acesso: atributo Id de &lt;infNFe&gt;, que vem como "NFe" + 44 dígitos.
    /// Composição dos 44 dígitos: UF(2) + AAMM(4) + CNPJ emitente(14) + modelo(2) +
    /// série(3) + número(9) + tipo de emissão(1) + código numérico(8) + dígito verificador(1).
    /// </summary>
    private static string LerChave(XElement infNFe)
    {
        string id = infNFe.Attribute("Id")?.Value.Trim()
            ?? throw new NotaInvalidaException("atributo Id da tag <infNFe> não encontrado");

        // Remove o prefixo "NFe", caso venha incorporado.
        string chave = id.StartsWith("NFe", StringComparison.OrdinalIgnoreCase) ? id[3..] : id;

        if (chave.Length != 44 || !chave.All(char.IsAsciiDigit))
            throw new NotaInvalidaException($"chave de acesso inválida no atributo Id (\"{id}\"), esperado 44 dígitos");

        return chave;
    }

    /// <summary>Data de emissão da tag &lt;dhEmi&gt;, ex.: "2026-09-15T10:30:00-03:00".</summary>
    private static DateTimeOffset LerDataEmissao(XElement? ide)
    {
        string texto = TextoObrigatorio(Filho(ide, "dhEmi"), "<ide><dhEmi>");

        if (!DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset data))
            throw new NotaInvalidaException($"data de emissão inválida em <dhEmi> (\"{texto}\")");

        return data;
    }

    /// <summary>Valor total da nota da tag &lt;total&gt;&lt;ICMSTot&gt;&lt;vNF&gt;, ex.: "150.45".</summary>
    private static decimal LerValor(XElement infNFe)
    {
        XElement? vNF = Filho(Filho(Filho(infNFe, "total"), "ICMSTot"), "vNF");
        string texto = TextoObrigatorio(vNF, "<total><ICMSTot><vNF>");

        // O padrão da SEFAZ sempre usa ponto como separador decimal, por isso InvariantCulture
        // (com a cultura pt-BR, "150.45" seria lido como 15045).
        if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal valor))
            throw new NotaInvalidaException($"valor inválido em <vNF> (\"{texto}\")");

        return valor;
    }

    // ---------- Funções auxiliares ----------

    /// <summary>Primeiro filho direto com o nome informado (ignorando o namespace), ou null.</summary>
    private static XElement? Filho(XElement? pai, string nome) =>
        pai?.Elements().FirstOrDefault(e => e.Name.LocalName == nome);

    /// <summary>Texto da tag sem espaços, ou null se a tag não existir ou estiver vazia.</summary>
    private static string? Texto(XElement? elemento) =>
        string.IsNullOrWhiteSpace(elemento?.Value) ? null : elemento.Value.Trim();

    /// <summary>Texto da tag; lança erro se ela não existir ou estiver vazia.</summary>
    private static string TextoObrigatorio(XElement? elemento, string descricaoTag) =>
        Texto(elemento) ?? throw new NotaInvalidaException($"tag {descricaoTag} não encontrada ou vazia");
}
