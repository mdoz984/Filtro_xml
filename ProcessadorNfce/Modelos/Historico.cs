namespace ProcessadorNfce.Modelos;

public sealed record RequisicaoProcessamento(string? Serie, string? Competencia);

public sealed record ResumoProcessamento(
    long Id,
    string DataHora,
    string Pasta,
    string Serie,
    string Competencia,
    int Total,
    int Enviadas,
    int Descartadas,
    int Erros);

public sealed record ItemProcessado(string Arquivo, string? Chave, string Situacao, string? Motivo);

public sealed record DetalheProcessamento(
    ResumoProcessamento Resumo,
    List<ItemProcessado> Itens,
    List<ArquivoGerado> Arquivos);
