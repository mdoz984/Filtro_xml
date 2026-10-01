using System.Globalization;
using System.Text;
using ProcessadorNfce.Modelos;
using ProcessadorNfce.Servicos;

const string PastaXml = @"C:\XmlNfce";

const string NomeArquivoSaida = "envio.json";

Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

Console.WriteLine("==================================================");
Console.WriteLine("  PROCESSADOR DE NFC-e  -  Envio consumidor final");
Console.WriteLine("==================================================");
Console.WriteLine($"Pasta dos XMLs: {PastaXml}");
Console.WriteLine();

string? serie = PerguntarSerie();
string? competencia = serie is null ? null : PerguntarCompetencia();

if (serie is null || competencia is null)
{
    Console.WriteLine("Entrada encerrada. Nada foi processado.");
    return 1;
}

int codigoSaida;
try
{
    var processador = new ProcessadorNotas(PastaXml);
    ResultadoProcessamento resultado = processador.Processar(new FiltroNfce(serie, competencia));

    string caminhoSaida = Path.Combine(PastaXml, NomeArquivoSaida);
    GeradorEnvioJson.Gerar(resultado.Aprovadas, caminhoSaida);

    MostrarRelatorio(resultado, caminhoSaida);
    codigoSaida = 0;
}

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



static string? PerguntarSerie()
{
    while (true)
    {
        Console.Write("Informe a SÉRIE da NFC-e (ex.: 1): ");
        string? entrada = Console.ReadLine();
        if (entrada is null) return null;

        
        if (int.TryParse(entrada.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int numero) && numero <= 999)
            return numero.ToString(CultureInfo.InvariantCulture);

        Console.WriteLine("  Série inválida. Digite um número de 0 a 999.");
    }
}


static string? PerguntarCompetencia()
{
    while (true)
    {
        Console.Write("Informe a COMPETÊNCIA no formato AAAA-MM (ex.: 2026-09): ");
        string? entrada = Console.ReadLine();
        if (entrada is null) return null;

        
        if (DateTime.TryParseExact(entrada.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
            return data.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        Console.WriteLine("  Competência inválida. Use AAAA-MM, por exemplo 2026-09.");
    }
}


static void MostrarRelatorio(ResultadoProcessamento resultado, string caminhoSaida)
{
    int total = resultado.Aprovadas.Count + resultado.Descartadas.Count + resultado.Erros.Count;

    Console.WriteLine();
    Console.WriteLine("-------------------- RESULTADO --------------------");
    Console.WriteLine($"XMLs encontrados:     {total}");
    Console.WriteLine($"Enviados (aprovados): {resultado.Aprovadas.Count}");
    Console.WriteLine($"Descartados (filtro): {resultado.Descartadas.Count}");
    Console.WriteLine($"Com erro de leitura:  {resultado.Erros.Count}");
    Console.WriteLine();

    foreach (NotaFiscal nota in resultado.Aprovadas)
        Escrever(ConsoleColor.Green, "[ENVIADO]    ", nota.Arquivo);

    foreach (ArquivoIgnorado item in resultado.Descartadas)
        Escrever(ConsoleColor.Yellow, "[DESCARTADO] ", $"{item.Arquivo} - {item.Motivo}");

    foreach (ArquivoIgnorado item in resultado.Erros)
        Escrever(ConsoleColor.Red, "[ERRO]       ", $"{item.Arquivo} - {item.Motivo}");

    Console.WriteLine();
    if (total == 0)
        Console.WriteLine("Nenhum arquivo .xml encontrado na pasta.");

    Console.WriteLine($"Arquivo gerado: {caminhoSaida} ({resultado.Aprovadas.Count} nota(s))");
}


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

static void AguardarEnter()
{
    Console.WriteLine();
    Console.Write("Pressione ENTER para sair...");
    Console.ReadLine();
}
