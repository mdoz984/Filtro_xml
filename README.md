# Processador de NFC-e

Aplicação de console em C# (.NET 10) que lê os XMLs de NFC-e em `C:\XmlNfce`, filtra as notas
de **consumidor final** (sem CPF/CNPJ do comprador) pela **série** e **competência** informadas
e gera o arquivo `envio.json`.

Detalhes de arquitetura, lógica e casos de teste: [DOCUMENTACAO_TECNICA.md](DOCUMENTACAO_TECNICA.md).

## Estrutura

```
ProcessadorNfce.slnx          solução (abrir no Visual Studio 2026)
ProcessadorNfce/              código-fonte
  Program.cs                  entrada do usuário e relatório
  Modelos/                    NotaFiscal, NotaEnvio, ResultadoProcessamento
  Servicos/                   LeitorXmlNfce, FiltroNfce, ProcessadorNotas, GeradorEnvioJson
MassaDeTestes/Professor/      os 6 XMLs fornecidos para a atividade (casos de teste oficiais)
MassaDeTestes/Extras/         XMLs fictícios para casos de borda (corrompido, ISO-8859-1 etc.)
DOCUMENTACAO_TECNICA.md       documentação técnica
```

## Como executar

1. Crie a pasta `C:\XmlNfce` (se ainda não existir) e coloque os XMLs das notas nela.
   Para testar, copie os arquivos de `MassaDeTestes/Professor`.
2. Abra `ProcessadorNfce.slnx` no Visual Studio e aperte **F5**
   (ou, no terminal: `dotnet run --project ProcessadorNfce`).
3. Informe a série e a competência. Para os XMLs do professor: série `1`, competência `2026-01`.
4. O resultado é gravado em `C:\XmlNfce\envio.json` e o resumo aparece na tela.
