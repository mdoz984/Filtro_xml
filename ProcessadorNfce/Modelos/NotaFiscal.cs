using System.Globalization;

namespace ProcessadorNfce.Modelos;

/// <summary>
/// Dados de uma NFC-e (modelo 65) extraídos do XML.
/// Guarda apenas as informações necessárias para aplicar os filtros e montar o envio.json.
/// </summary>
public sealed class NotaFiscal
{
    /// <summary>Nome do arquivo XML de origem (usado nas mensagens do relatório).</summary>
    public required string Arquivo { get; init; }

    /// <summary>Chave de acesso com 44 dígitos (atributo Id de &lt;infNFe&gt; sem o prefixo "NFe").</summary>
    public required string Chave { get; init; }

    /// <summary>Modelo do documento, tag &lt;ide&gt;&lt;mod&gt;: "65" = NFC-e, "55" = NF-e.</summary>
    public required string Modelo { get; init; }

    /// <summary>Conteúdo da tag &lt;ide&gt;&lt;serie&gt;.</summary>
    public required string Serie { get; init; }

    /// <summary>Data/hora de emissão da tag &lt;ide&gt;&lt;dhEmi&gt;, com o fuso horário informado no XML.</summary>
    public required DateTimeOffset DataEmissao { get; init; }

    /// <summary>CPF do comprador (&lt;dest&gt;&lt;CPF&gt;), ou null quando não informado.</summary>
    public string? CpfDestinatario { get; init; }

    /// <summary>CNPJ do comprador (&lt;dest&gt;&lt;CNPJ&gt;), ou null quando não informado.</summary>
    public string? CnpjDestinatario { get; init; }

    /// <summary>
    /// Competência no formato AAAA-MM, calculada a partir da data de emissão.
    /// Usa o ano/mês do próprio XML (sem converter para o fuso do computador),
    /// assim uma nota emitida em 30/09 às 23h50 continua sendo de setembro.
    /// </summary>
    public string Competencia => DataEmissao.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}
