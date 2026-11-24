using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public sealed class FiltroNfce(string serie, string competencia)
{
    private const string ModeloNfce = "65";
    private const string CStatAutorizada = "100";

    public string Serie { get; } = serie;
    public string Competencia { get; } = competencia;

    public string? MotivoDescarte(ChaveAcesso chave)
    {
        if (chave.Modelo != ModeloNfce)
            return $"modelo {chave.Modelo}, não é NFC-e";

        if (chave.Serie != Serie)
            return $"série {chave.Serie}";

        if (chave.Competencia != Competencia)
            return $"competência {chave.Competencia}";

        return null;
    }

    public string? MotivoDescarte(NotaFiscal nota)
    {
        if (nota.CStat is null)
            return "sem protocolo de autorização (tag <cStat> não encontrada)";

        if (nota.CStat != CStatAutorizada)
            return $"cStat {nota.CStat}, nota não autorizada";

        if (nota.Serie != Serie)
            return $"série {nota.Serie}";

        if (nota.Competencia != Competencia)
            return $"competência {nota.Competencia}";

        if (nota.CpfDestinatario is not null)
            return "tem CPF do comprador";

        if (nota.CnpjDestinatario is not null)
            return "tem CNPJ do comprador";

        return null;
    }
}
