using System.Globalization;

namespace ProcessadorNfce.Modelos;


public sealed class NotaFiscal
{
    
    public required string Arquivo { get; init; }

    
    public required string Chave { get; init; }

   
    public required string Modelo { get; init; }

    public required string Serie { get; init; }

   
    public required DateTimeOffset DataEmissao { get; init; }

    
    public string? CpfDestinatario { get; init; }

    public string? CnpjDestinatario { get; init; }

    public string Competencia => DataEmissao.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}
