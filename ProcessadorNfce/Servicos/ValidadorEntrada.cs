using System.Globalization;

namespace ProcessadorNfce.Servicos;

public static class ValidadorEntrada
{
    // pasta fixa, definida no escopo do projeto
    public const string PastaXml = @"C:\XmlNfce";

    // "1" -> "001": na chave a série tem 3 dígitos
    public static string? NormalizarSerie(string? entrada)
    {
        if (entrada is null)
            return null;

        if (int.TryParse(entrada.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int numero) && numero <= 999)
            return numero.ToString("D3", CultureInfo.InvariantCulture);

        return null;
    }

    public static string? NormalizarCompetencia(string? entrada)
    {
        if (entrada is null)
            return null;

        if (DateTime.TryParseExact(entrada.Trim(), "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
            return data.ToString("MM/yyyy", CultureInfo.InvariantCulture);

        return null;
    }
}
