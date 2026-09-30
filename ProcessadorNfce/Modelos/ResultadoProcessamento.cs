namespace ProcessadorNfce.Modelos;

/// <summary>Arquivo que não entrou no envio, junto com o motivo.</summary>
public sealed record ArquivoIgnorado(string Arquivo, string Motivo);

/// <summary>
/// Resultado da leitura da pasta: o que foi aprovado, o que o filtro descartou
/// e o que não pôde nem ser lido (arquivo corrompido, sem permissão etc.).
/// </summary>
public sealed class ResultadoProcessamento
{
    /// <summary>Notas que atendem aos 4 critérios e vão para o envio.json.</summary>
    public List<NotaFiscal> Aprovadas { get; } = [];

    /// <summary>Notas lidas com sucesso, mas descartadas por algum critério do filtro.</summary>
    public List<ArquivoIgnorado> Descartadas { get; } = [];

    /// <summary>Arquivos que não puderam ser lidos ou não são uma NFC-e válida.</summary>
    public List<ArquivoIgnorado> Erros { get; } = [];
}
