using ProcessadorNfce.Modelos;

namespace ProcessadorNfce.Servicos;

public sealed class FiltroNfce(string serie, string competencia)
{
    
    private const string ModeloNfce = "65";

    public string Serie { get; } = serie;
    public string Competencia { get; } = competencia;

    public string? MotivoDescarte(NotaFiscal nota)
    {
       
        if (nota.Modelo != ModeloNfce)
            return $"modelo {nota.Modelo}";

        
        if (nota.Serie != Serie)
            return $"série {nota.Serie}";

        
        if (nota.Competencia != Competencia)
            return $"competência {nota.Competencia}";

        
        if (nota.CpfDestinatario is not null)
            return "tem CPF";

        if (nota.CnpjDestinatario is not null)
            return "tem CNPJ";

        return null; 
    }
}
