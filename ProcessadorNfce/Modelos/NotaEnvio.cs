using System.Text.Json.Serialization;

namespace ProcessadorNfce.Modelos;

/// <summary>
/// Um item do arquivo envio.json, no formato pedido pela API externa:
/// { "chave": "...", "competencia": "AAAA-MM" }
/// </summary>
public sealed record NotaEnvio(
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("competencia")] string Competencia);
