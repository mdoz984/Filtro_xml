namespace ProcessadorNfce.Modelos;

public sealed record ArquivoIgnorado(string Arquivo, string Motivo);

public sealed class ResultadoProcessamento
{
    public List<NotaFiscal> Aprovadas { get; } = [];
    public List<ArquivoIgnorado> Descartadas { get; } = [];

    public List<ArquivoIgnorado> Erros { get; } = [];
}
