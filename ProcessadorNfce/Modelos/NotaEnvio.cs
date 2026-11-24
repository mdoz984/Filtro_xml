using System.Text.Json.Serialization;

namespace ProcessadorNfce.Modelos;

public sealed record NotaEnvio(
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("serie")] string Serie,
    [property: JsonPropertyName("competencia")] string Competencia);
