# Filtro XML de NFC-e

Programa de console em C# que lê os XMLs de NFC-e de uma pasta e gera um `envio.json` só com as notas de consumidor final, ou seja, as que não têm CPF nem CNPJ do comprador.

Projeto da disciplina Fábrica de Projetos, 4º termo de ADS na UNIMAR.

## Como funciona

Quando abre, o programa pede a série da NFC-e e a competência (mês e ano, no formato `AAAA-MM`). Depois lê todos os `.xml` da pasta `C:\XmlNfce` e só aprova a nota que:

- é modelo 65 (NFC-e);
- é da série informada;
- foi emitida no mês informado (ele usa o ano e o mês do `<dhEmi>`);
- não tem `<CPF>` nem `<CNPJ>` dentro de `<dest>`.

Se a mesma chave aparecer em dois arquivos, só o primeiro entra e o outro fica como duplicado.

As notas aprovadas vão para `C:\XmlNfce\envio.json`. No final aparece um resumo na tela: em verde o que foi enviado, em amarelo o que foi descartado (com o motivo) e em vermelho os arquivos que não deu pra ler, como XML quebrado, chave com tamanho errado ou falta de série ou data. Um arquivo com problema não para o programa, ele só anota o erro e segue para o próximo.

Alguns emissores gravam o XML em ISO-8859-1 mas declaram UTF-8 no cabeçalho. Quando isso acontece, o programa tenta ler de novo como Latin-1 antes de considerar o arquivo corrompido.

## Requisitos

- Windows, porque a pasta `C:\XmlNfce` está fixa no código
- [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

Não tem pacote NuGet pra instalar. Tudo o que o programa usa já vem no .NET: `System.Xml.Linq` para ler os XMLs e `System.Text.Json` para gravar o JSON.

## Como rodar

Crie a pasta `C:\XmlNfce` e coloque os XMLs dentro dela (subpastas não são lidas). Depois, na raiz do repositório:

```
dotnet run --project ProcessadorNfce
```

Ou abra o `ProcessadorNfce.slnx` no Visual Studio e rode com F5.

Exemplo com dois XMLs na pasta, um sem comprador e outro com CPF:

```
==================================================
  PROCESSADOR DE NFC-e  -  Envio consumidor final
==================================================
Pasta dos XMLs: C:\XmlNfce

Informe a SÉRIE da NFC-e (ex.: 1): 1
Informe a COMPETÊNCIA no formato AAAA-MM (ex.: 2026-09): 2026-01

-------------------- RESULTADO --------------------
XMLs encontrados:     2
Enviados (aprovados): 1
Descartados (filtro): 1
Com erro de leitura:  0

[ENVIADO]    nota-912.xml
[DESCARTADO] nota-921.xml - tem CPF

Arquivo gerado: C:\XmlNfce\envio.json (1 nota(s))

Pressione ENTER para sair...
```

Para usar outra pasta, troque a constante `PastaXml` no começo do `Program.cs`.

## O envio.json

Uma lista com a chave de acesso (os 44 dígitos, sem o prefixo `NFe`) e a competência de cada nota aprovada:

```json
[
  {
    "chave": "35260144470771000118650010000009121060719696",
    "competencia": "2026-01"
  }
]
```

O arquivo é recriado toda vez que o programa roda. Se nenhuma nota passar no filtro, ele fica só com `[]`.

## Estrutura

```
ProcessadorNfce/
├── Program.cs                     pede série e competência, mostra o resultado
├── Modelos/
│   ├── NotaFiscal.cs              dados lidos de cada XML
│   ├── NotaEnvio.cs               formato de cada item do envio.json
│   └── ResultadoProcessamento.cs  listas de aprovadas, descartadas e erros
└── Servicos/
    ├── ProcessadorNotas.cs        percorre a pasta e separa os resultados
    ├── LeitorXmlNfce.cs           lê um XML e monta a NotaFiscal
    ├── FiltroNfce.cs              regras de aprovação
    └── GeradorEnvioJson.cs        grava o envio.json
```

De onde sai cada campo do XML e os casos de teste estão na [documentação técnica](DOCUMENTACAO_TECNICA.md).

## Limitações

- Não envia nada para a API, só gera o arquivo.
- Não confere se a nota foi autorizada pela SEFAZ (`cStat`), se é de homologação (`tpAmb` 2) ou se foi cancelada depois.
- A checagem de duplicadas só vale dentro da mesma execução.
