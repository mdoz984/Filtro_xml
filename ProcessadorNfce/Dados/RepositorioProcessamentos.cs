using System.Globalization;
using Microsoft.Data.Sqlite;
using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Dados;

public sealed class RepositorioProcessamentos(string caminhoBanco)
{
    public const string ScriptCriacao = """
        CREATE TABLE IF NOT EXISTS Processamento (
            Id           INTEGER PRIMARY KEY AUTOINCREMENT,
            DataHora     TEXT    NOT NULL,
            Pasta        TEXT    NOT NULL,
            Serie        TEXT    NOT NULL,
            Competencia  TEXT    NOT NULL,
            Enviadas     INTEGER NOT NULL,
            Descartadas  INTEGER NOT NULL,
            Erros        INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS NotaProcessada (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            ProcessamentoId  INTEGER NOT NULL REFERENCES Processamento(Id) ON DELETE CASCADE,
            Arquivo          TEXT    NOT NULL,
            Chave            TEXT    NULL,
            Situacao         TEXT    NOT NULL CHECK (Situacao IN ('ENVIADA', 'DESCARTADA', 'ERRO')),
            Motivo           TEXT    NULL
        );

        CREATE TABLE IF NOT EXISTS ArquivoEnvio (
            Id               INTEGER PRIMARY KEY AUTOINCREMENT,
            ProcessamentoId  INTEGER NOT NULL REFERENCES Processamento(Id) ON DELETE CASCADE,
            NomeArquivo      TEXT    NOT NULL,
            Caminho          TEXT    NOT NULL,
            QuantidadeNotas  INTEGER NOT NULL,
            Conteudo         TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_NotaProcessada_Processamento ON NotaProcessada (ProcessamentoId);
        CREATE INDEX IF NOT EXISTS IX_NotaProcessada_Chave ON NotaProcessada (Chave);
        CREATE INDEX IF NOT EXISTS IX_ArquivoEnvio_Processamento ON ArquivoEnvio (ProcessamentoId);
        """;

    private readonly string _stringConexao = new SqliteConnectionStringBuilder
    {
        DataSource = caminhoBanco,
        ForeignKeys = true,
    }.ToString();

    public string CaminhoBanco { get; } = caminhoBanco;

    public void CriarTabelas()
    {
        using SqliteConnection conexao = Abrir();
        using SqliteCommand comando = conexao.CreateCommand();
        comando.CommandText = ScriptCriacao;
        comando.ExecuteNonQuery();
    }

    // tudo numa transação: ou grava o processamento inteiro, ou nada
    public long Salvar(string pasta, string serie, string competencia, ResultadoProcessamento resultado,
        List<ArquivoGerado> arquivos)
    {
        using SqliteConnection conexao = Abrir();
        using SqliteTransaction transacao = conexao.BeginTransaction();

        long id;
        using (SqliteCommand comando = conexao.CreateCommand())
        {
            comando.Transaction = transacao;
            comando.CommandText = """
                INSERT INTO Processamento (DataHora, Pasta, Serie, Competencia, Enviadas, Descartadas, Erros)
                VALUES ($dataHora, $pasta, $serie, $competencia, $enviadas, $descartadas, $erros);
                SELECT last_insert_rowid();
                """;
            comando.Parameters.AddWithValue("$dataHora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            comando.Parameters.AddWithValue("$pasta", pasta);
            comando.Parameters.AddWithValue("$serie", serie);
            comando.Parameters.AddWithValue("$competencia", competencia);
            comando.Parameters.AddWithValue("$enviadas", resultado.Aprovadas.Count);
            comando.Parameters.AddWithValue("$descartadas", resultado.Descartadas.Count);
            comando.Parameters.AddWithValue("$erros", resultado.Erros.Count);
            id = Convert.ToInt64(comando.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        foreach (NotaFiscal nota in resultado.Aprovadas)
            InserirNota(conexao, transacao, id, nota.Arquivo, nota.Chave.Digitos, "ENVIADA", null);

        foreach (ArquivoIgnorado item in resultado.Descartadas)
            InserirNota(conexao, transacao, id, item.Arquivo, item.Chave, "DESCARTADA", item.Motivo);

        foreach (ArquivoIgnorado item in resultado.Erros)
            InserirNota(conexao, transacao, id, item.Arquivo, item.Chave, "ERRO", item.Motivo);

        foreach (ArquivoGerado arquivo in arquivos)
        {
            using SqliteCommand comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = """
                INSERT INTO ArquivoEnvio (ProcessamentoId, NomeArquivo, Caminho, QuantidadeNotas, Conteudo)
                VALUES ($processamentoId, $nomeArquivo, $caminho, $quantidade, $conteudo);
                """;
            comando.Parameters.AddWithValue("$processamentoId", id);
            comando.Parameters.AddWithValue("$nomeArquivo", arquivo.NomeArquivo);
            comando.Parameters.AddWithValue("$caminho", arquivo.Caminho);
            comando.Parameters.AddWithValue("$quantidade", arquivo.QuantidadeNotas);
            comando.Parameters.AddWithValue("$conteudo", arquivo.Conteudo);
            comando.ExecuteNonQuery();
        }

        transacao.Commit();
        return id;
    }

    public List<ResumoProcessamento> Listar(int limite = 50)
    {
        using SqliteConnection conexao = Abrir();
        using SqliteCommand comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT Id, DataHora, Pasta, Serie, Competencia, Enviadas + Descartadas + Erros, Enviadas, Descartadas, Erros
            FROM Processamento
            ORDER BY Id DESC
            LIMIT $limite;
            """;
        comando.Parameters.AddWithValue("$limite", limite);

        var lista = new List<ResumoProcessamento>();
        using SqliteDataReader leitor = comando.ExecuteReader();
        while (leitor.Read())
            lista.Add(LerResumo(leitor));

        return lista;
    }

    public DetalheProcessamento? Obter(long id)
    {
        using SqliteConnection conexao = Abrir();

        ResumoProcessamento? resumo;
        using (SqliteCommand comando = conexao.CreateCommand())
        {
            comando.CommandText = """
                SELECT Id, DataHora, Pasta, Serie, Competencia, Enviadas + Descartadas + Erros, Enviadas, Descartadas, Erros
                FROM Processamento
                WHERE Id = $id;
                """;
            comando.Parameters.AddWithValue("$id", id);
            using SqliteDataReader leitor = comando.ExecuteReader();
            resumo = leitor.Read() ? LerResumo(leitor) : null;
        }

        if (resumo is null)
            return null;

        var itens = new List<ItemProcessado>();
        using (SqliteCommand comando = conexao.CreateCommand())
        {
            comando.CommandText = """
                SELECT Arquivo, Chave, Situacao, Motivo
                FROM NotaProcessada
                WHERE ProcessamentoId = $id
                ORDER BY CASE Situacao WHEN 'ENVIADA' THEN 1 WHEN 'DESCARTADA' THEN 2 ELSE 3 END, Arquivo;
                """;
            comando.Parameters.AddWithValue("$id", id);
            using SqliteDataReader leitor = comando.ExecuteReader();
            while (leitor.Read())
                itens.Add(new ItemProcessado(leitor.GetString(0), TextoOuNulo(leitor, 1), leitor.GetString(2), TextoOuNulo(leitor, 3)));
        }

        var arquivos = new List<ArquivoGerado>();
        using (SqliteCommand comando = conexao.CreateCommand())
        {
            comando.CommandText = """
                SELECT NomeArquivo, Caminho, QuantidadeNotas, Conteudo
                FROM ArquivoEnvio
                WHERE ProcessamentoId = $id
                ORDER BY NomeArquivo;
                """;
            comando.Parameters.AddWithValue("$id", id);
            using SqliteDataReader leitor = comando.ExecuteReader();
            while (leitor.Read())
                arquivos.Add(new ArquivoGerado(leitor.GetString(0), leitor.GetString(1), leitor.GetInt32(2), leitor.GetString(3)));
        }

        return new DetalheProcessamento(resumo, itens, arquivos);
    }

    private SqliteConnection Abrir()
    {
        var conexao = new SqliteConnection(_stringConexao);
        conexao.Open();
        return conexao;
    }

    private static void InserirNota(SqliteConnection conexao, SqliteTransaction transacao, long processamentoId,
        string arquivo, string? chave, string situacao, string? motivo)
    {
        using SqliteCommand comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = """
            INSERT INTO NotaProcessada (ProcessamentoId, Arquivo, Chave, Situacao, Motivo)
            VALUES ($processamentoId, $arquivo, $chave, $situacao, $motivo);
            """;
        comando.Parameters.AddWithValue("$processamentoId", processamentoId);
        comando.Parameters.AddWithValue("$arquivo", arquivo);
        comando.Parameters.AddWithValue("$chave", (object?)chave ?? DBNull.Value);
        comando.Parameters.AddWithValue("$situacao", situacao);
        comando.Parameters.AddWithValue("$motivo", (object?)motivo ?? DBNull.Value);
        comando.ExecuteNonQuery();
    }

    private static ResumoProcessamento LerResumo(SqliteDataReader leitor) => new(
        leitor.GetInt64(0),
        leitor.GetString(1),
        leitor.GetString(2),
        leitor.GetString(3),
        leitor.GetString(4),
        leitor.GetInt32(5),
        leitor.GetInt32(6),
        leitor.GetInt32(7),
        leitor.GetInt32(8));

    private static string? TextoOuNulo(SqliteDataReader leitor, int coluna) =>
        leitor.IsDBNull(coluna) ? null : leitor.GetString(coluna);
}
