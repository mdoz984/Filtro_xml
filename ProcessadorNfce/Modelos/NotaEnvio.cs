using System.Text.Json.Serialization;

namespace ProcessadorNfce.Modelos;

public sealed record NotaEnvio(
    [property: JsonPropertyName("sequencia")] int Sequencia,
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("competencia")] string Competencia);
    