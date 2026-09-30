// =====================================================================
//  PROCESSADOR DE NFC-e (modelo 65)
//  Fábrica de Projetos | 4º Termo ADS — UNIMAR
//
//  Lê os XMLs de C:\XmlNfce, seleciona as notas de CONSUMIDOR FINAL
//  (sem CPF/CNPJ do comprador) da série e competência informadas
//  e gera o arquivo envio.json para a API externa.
// =====================================================================

using System.Globalization;
using System.Text;
using ProcessadorNfce.Modelos;
using ProcessadorNfce.Servicos;

// Pasta onde a empresa armazena obrigatoriamente os XMLs (definida no enunciado).
const string PastaXml = @"C:\XmlNfce";
// O envio.json é gravado na mesma pasta dos XMLs.
const string NomeArquivoSaida = "envio.json";

// Mostra acentos corretamente no console e habilita codificações antigas
// (ex.: windows-1252) caso algum XML venha declarado assim.
Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Console.WriteLine("==================================================");
Console.WriteLine("  PROCESSADOR DE NFC-e  -  Envio consumidor final");
Console.WriteLine("==================================================");
Console.WriteLine($"Pasta dos XMLs: {PastaXml}");
Console.WriteLine();

// ---------- 1. Parâmetros de entrada ----------
string? serie = PerguntarSerie();
string? competencia = serie is null ? null : PerguntarCompetencia();

if (serie is null || competencia is null)
{
    Console.WriteLine("Entrada encerrada. Nada foi processado.");
    return 1;
}

// ---------- 2. Processamento ----------
int codigoSaida;
try
{
    var processador = new ProcessadorNotas(PastaXml);
    ResultadoProcessamento resultado = processador.Processar(new FiltroNfce(serie, competencia));

    // ---------- 3. Saída ----------
    string caminhoSaida = Path.Combine(PastaXml, NomeArquivoSaida);
    GeradorEnvioJson.Gerar(resultado.Aprovadas, caminhoSaida);

    MostrarRelatorio(resultado, caminhoSaida);
    codigoSaida = 0;
}
// Problemas com a pasta em si (os problemas de cada arquivo são tratados no ProcessadorNotas).
catch (DirectoryNotFoundException)
{
    MostrarErro($"A pasta {PastaXml} não existe. Crie a pasta e coloque os XMLs das notas nela.");
    codigoSaida = 1;
}
catch (UnauthorizedAccessException)
{
    MostrarErro($"Sem permissão para ler os XMLs ou gravar o {NomeArquivoSaida} em {PastaXml}.");
    codigoSaida = 1;
}
catch (IOException ex)
{
    MostrarErro($"Erro de acesso ao disco: {ex.Message}");
    codigoSaida = 1;
}

AguardarEnter();
return codigoSaida;


// =====================================================================
//  Funções auxiliares do console
// =====================================================================

// Pede a série até o usuário digitar um valor válido. Retorna null se a entrada acabar.
static string? PerguntarSerie()
{
    while (true)
    {
        Console.Write("Informe a SÉRIE da NFC-e (ex.: 1): ");
        string? entrada = Console.ReadLine();
        if (entrada is null) return null;

        // A série da NFC-e vai de 0 a 999 e, no XML, é gravada sem zeros à esquerda ("1", nunca "001").
        // Por isso aceitamos "001" e normalizamos para "1" antes de comparar com a tag <serie>.
        if (int.TryParse(entrada.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int numero) && numero <= 999)
            return numero.ToString(CultureInfo.InvariantCulture);

        Console.WriteLine("  Série inválida. Digite um número de 0 a 999.");
    }
}

// Pede a competência (AAAA-MM) até ser válida. Retorna null se a entrada acabar.
static string? PerguntarCompetencia()
{
    while (true)
    {
        Console.Write("Informe a COMPETÊNCIA no formato AAAA-MM (ex.: 2026-09): ");
        string? entrada = Console.ReadLine();
        if (entrada is null) return null;

        // TryParseExact valida o formato e o mês (01 a 12) de uma só vez.
        if (DateTime.TryParseExact(entrada.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
            return data.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        Console.WriteLine("  Competência inválida. Use AAAA-MM, por exemplo 2026-09.");
    }
}

// Mostra o resumo do processamento e o destino de cada arquivo.
static void MostrarRelatorio(ResultadoProcessamento resultado, string caminhoSaida)
{
    var ptBr = CultureInfo.GetCultureInfo("pt-BR");
    int total = resultado.Aprovadas.Count + resultado.Descartadas.Count + resultado.Erros.Count;

    Console.WriteLine();
    Console.WriteLine("-------------------- RESULTADO --------------------");
    Console.WriteLine($"XMLs encontrados:     {total}");
    Console.WriteLine($"Enviados (aprovados): {resultado.Aprovadas.Count}");
    Console.WriteLine($"Descartados (filtro): {resultado.Descartadas.Count}");
    Console.WriteLine($"Com erro de leitura:  {resultado.Erros.Count}");
    Console.WriteLine();

    foreach (NotaFiscal nota in resultado.Aprovadas)
        Escrever(ConsoleColor.Green, "[ENVIADO]    ", $"{nota.Arquivo} - chave {nota.Chave} - R$ {nota.Valor.ToString("N2", ptBr)}");

    foreach (ArquivoIgnorado item in resultado.Descartadas)
        Escrever(ConsoleColor.Yellow, "[DESCARTADO] ", $"{item.Arquivo} - {item.Motivo}");

    foreach (ArquivoIgnorado item in resultado.Erros)
        Escrever(ConsoleColor.Red, "[ERRO]       ", $"{item.Arquivo} - {item.Motivo}");

    Console.WriteLine();
    if (total == 0)
        Console.WriteLine("Nenhum arquivo .xml encontrado na pasta.");

    decimal soma = resultado.Aprovadas.Sum(n => n.Valor);
    Console.WriteLine($"Arquivo gerado: {caminhoSaida} ({resultado.Aprovadas.Count} nota(s), total R$ {soma.ToString("N2", ptBr)})");
}

// Escreve uma linha com um rótulo colorido.
static void Escrever(ConsoleColor cor, string rotulo, string texto)
{
    Console.ForegroundColor = cor;
    Console.Write(rotulo);
    Console.ResetColor();
    Console.WriteLine(texto);
}

static void MostrarErro(string mensagem)
{
    Console.WriteLine();
    Escrever(ConsoleColor.Red, "[ERRO] ", mensagem);
}

// Mantém a janela aberta quando o programa é aberto com duplo clique no .exe.
static void AguardarEnter()
{
    if (Console.IsInputRedirected) return;
    Console.WriteLine();
    Console.Write("Pressione ENTER para sair...");
    Console.ReadLine();
}
