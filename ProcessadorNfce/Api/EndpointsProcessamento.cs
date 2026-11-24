using Microsoft.Data.Sqlite;
using ProcessadorNfce.Dados;
using ProcessadorNfce.Modelos;
using ProcessadorNfce.Servicos;

namespace ProcessadorNfce.Api;

public static class EndpointsProcessamento
{
    public static void MapProcessamentos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/config", () => Results.Ok(new { pastaXml = ValidadorEntrada.PastaXml }));

        app.MapPost("/api/processamentos", Processar);

        app.MapGet("/api/processamentos", (RepositorioProcessamentos repositorio) =>
            Results.Ok(repositorio.Listar()));

        app.MapGet("/api/processamentos/{id:long}", (long id, RepositorioProcessamentos repositorio) =>
            repositorio.Obter(id) is DetalheProcessamento detalhe
                ? Results.Ok(detalhe)
                : Results.NotFound(new { erro = $"Processamento {id} não encontrado." }));
    }

    private static IResult Processar(RequisicaoProcessamento requisicao, RepositorioProcessamentos repositorio)
    {
        const string pasta = ValidadorEntrada.PastaXml;

        string? serie = ValidadorEntrada.NormalizarSerie(requisicao.Serie);
        if (serie is null)
            return Erro("Série inválida. Digite um número de 0 a 999.");

        string? competencia = ValidadorEntrada.NormalizarCompetencia(requisicao.Competencia);
        if (competencia is null)
            return Erro("Competência inválida. Use MM/AAAA, por exemplo 01/2026.");

        if (!Directory.Exists(pasta))
            return Erro($"A pasta {pasta} não existe. Crie a pasta e coloque os XMLs das notas nela.");

        try
        {
            ResultadoProcessamento resultado = new ProcessadorNotas(pasta).Processar(new FiltroNfce(serie, competencia));
            List<ArquivoGerado> arquivos = GeradorEnvioJson.Gerar(resultado.Aprovadas, competencia, pasta);

            long id = repositorio.Salvar(pasta, serie, competencia, resultado, arquivos);
            return Results.Ok(repositorio.Obter(id));
        }
        catch (UnauthorizedAccessException)
        {
            return Erro($"Sem permissão para ler os XMLs ou gravar o arquivo de envio em {pasta}.");
        }
        catch (IOException ex)
        {
            return Erro($"Erro de acesso ao disco: {ex.Message}");
        }
        catch (SqliteException ex)
        {
            return Results.Problem(
                title: "Erro ao gravar no banco de dados",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static IResult Erro(string mensagem) => Results.BadRequest(new { erro = mensagem });
}
