using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

/// <summary>
/// Aplica os critérios de filtragem do desafio sobre uma nota já lida.
///
/// O critério 1 (localização em C:\XmlNfce) é garantido pelo <see cref="ProcessadorNotas"/>,
/// que só lê arquivos dessa pasta. Aqui ficam os outros três, precedidos de uma verificação
/// de que o documento é mesmo uma NFC-e (modelo 65), já que a pasta pode conter NF-e (modelo 55):
///   2. Série igual à informada pelo usuário;
///   3. Competência (AAAA-MM da tag dhEmi) igual à informada;
///   4. Consumidor final: sem CPF e sem CNPJ preenchidos em &lt;dest&gt;.
/// </summary>
/// <param name="serie">Série informada pelo usuário, já sem zeros à esquerda (ex.: "1").</param>
/// <param name="competencia">Competência informada pelo usuário no formato AAAA-MM.</param>
public sealed class FiltroNfce(string serie, string competencia)
{
    /// <summary>Código do modelo da NFC-e na tag &lt;mod&gt;.</summary>
    private const string ModeloNfce = "65";

    public string Serie { get; } = serie;
    public string Competencia { get; } = competencia;

    /// <summary>
    /// Verifica a nota contra os critérios.
    /// Retorna null quando a nota atende a TODOS eles (deve ir para o envio)
    /// ou o motivo do descarte quando algum critério falha.
    /// </summary>
    public string? MotivoDescarte(NotaFiscal nota)
    {
        // Pré-requisito - Modelo: o processador trata apenas NFC-e (modelo 65).
        // A NF-e (modelo 55) usa o mesmo layout de XML, mas é outro documento fiscal.
        if (nota.Modelo != ModeloNfce)
            return nota.Modelo == "55"
                ? "modelo 55 (NF-e), não é NFC-e (modelo 65)"
                : $"modelo {nota.Modelo}, não é NFC-e (modelo 65)";

        // Critério 2 - Série: deve corresponder exatamente à série informada.
        if (nota.Serie != Serie)
            return $"série {nota.Serie} diferente da informada ({Serie})";

        // Critério 3 - Competência: ano e mês da emissão devem ser os informados.
        if (nota.Competencia != Competencia)
            return $"competência {nota.Competencia} diferente da informada ({Competencia})";

        // Critério 4 - Consumidor final: o comprador não pode estar identificado.
        if (nota.CpfDestinatario is not null)
            return "comprador identificado por CPF em <dest><CPF> (não é consumidor final)";

        if (nota.CnpjDestinatario is not null)
            return "comprador identificado por CNPJ em <dest><CNPJ> (não é consumidor final)";

        return null; // passou em todos os critérios
    }
}
