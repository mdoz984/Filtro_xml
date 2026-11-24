namespace ProcessadorNfce.Modelos;

// Chave é null quando não deu para identificar (ex.: XML corrompido sem chave no nome)
public sealed record ArquivoIgnorado(string Arquivo, string Motivo, string? Chave = null);

public sealed record ArquivoGerado(string NomeArquivo, string Caminho, int QuantidadeNotas, string Conteudo);

public sealed class ResultadoProcessamento
{
    public List<NotaFiscal> Aprovadas { get; } = [];

    public List<ArquivoIgnorado> Descartadas { get; } = [];

    public List<ArquivoIgnorado> Erros { get; } = [];
}
