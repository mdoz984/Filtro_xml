using System.Text.Json.Serialization;

namespace ProcessadorNfce.Modelos;

/// <summary>
/// Um item do arquivo envio.json, exatamente no formato pedido pela API externa:
/// { "chave": "...", "competencia": "AAAA-MM", "valor": 150.45 }
/// </summary>
public sealed record NotaEnvio(
    [property: JsonPropertyName("chave")] string Chave,
    [property: JsonPropertyName("competencia")] string Competencia,
    [property: JsonPropertyName("valor")] decimal Valor);
