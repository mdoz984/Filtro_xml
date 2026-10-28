namespace ProcessadorNfce.Modelos;

public sealed class ChaveAcesso(string digitos)
{
    private static readonly HashSet<string> CodigosUf =
    [
        "11", "12", "13", "14", "15", "16", "17",
        "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "31", "32", "33", "35",
        "41", "42", "43",
        "50", "51", "52", "53",
    ];

    public string Digitos { get; } = digitos;

    public string Uf => Digitos[..2];
    public string AnoMes => Digitos[2..6];
    public string Cnpj => Digitos[6..20];
    public string Modelo => Digitos[20..22];
    public string Serie => Digitos[22..25];
    public string NumeroNota => Digitos[25..34];
    public string TipoEmissao => Digitos[34..35];
    public string CodigoNumerico => Digitos[35..43];
    public string DigitoVerificador => Digitos[43..];

    public string Competencia => $"{AnoMes[2..]}/20{AnoMes[..2]}";

    public static string? MotivoInvalida(string digitos)
    {
        if (digitos.Length != 44 || !digitos.All(char.IsAsciiDigit))
            return "esperado 44 dígitos";

        if (!CodigosUf.Contains(digitos[..2]))
            return $"UF {digitos[..2]} inexistente";

        if (int.Parse(digitos[4..6]) is < 1 or > 12)
            return $"mês {digitos[4..6]} inválido";

        if (CalcularDigito(digitos[..43]) != digitos[43] - '0')
            return "dígito verificador inválido";

        return null;
    }

    private static int CalcularDigito(string digitos)
    {
        int soma = 0;
        int peso = 2;

        for (int i = digitos.Length - 1; i >= 0; i--)
        {
            soma += (digitos[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
