# Processador de NFC-e

Aplicação de console em C# (.NET 10) que lê os XMLs de NFC-e em `C:\XmlNfce`, filtra as notas
de **consumidor final** (sem CPF/CNPJ do comprador) pela **série** e **competência** (MM/AAAA) informadas
e gera um arquivo de envio por emitente, no formato `mmaaaaxxxxx.txt` (ex.: `01202644470.txt`).

Detalhes de arquitetura, lógica e casos de teste: [DOCUMENTACAO_TECNICA.md](DOCUMENTACAO_TECNICA.md).

## Estrutura

```
ProcessadorNfce.slnx          solução (abrir no Visual Studio 2026)
ProcessadorNfce/              código-fonte
  Program.cs                  entrada do usuário e relatório
  Modelos/                    ChaveAcesso, NotaFiscal, NotaEnvio, ResultadoProcessamento
  Servicos/                   LeitorXmlNfce, FiltroNfce, ProcessadorNotas, GeradorEnvioJson
DOCUMENTACAO_TECNICA.md       documentação técnica
```

## Como executar

1. Crie a pasta `C:\XmlNfce` (se ainda não existir) e coloque os XMLs das notas nela.
2. Abra `ProcessadorNfce.slnx` no Visual Studio e aperte **F5**
   (ou, no terminal: `dotnet run --project ProcessadorNfce`).
3. Informe a série e a competência. Para os XMLs do professor: série `1`, competência `01/2026`.
4. O resultado é gravado em `C:\XmlNfce\01202644470.txt` (um arquivo por emitente) e o resumo aparece na tela.

## Exemplo de execução

```
==================================================
  PROCESSADOR DE NFC-e  -  Envio consumidor final
==================================================
Pasta dos XMLs: C:\XmlNfce

Informe a SÉRIE da NFC-e (ex.: 1): 1
Informe a COMPETÊNCIA no formato MM/AAAA (ex.: 01/2026): 01/2026

-------------------- RESULTADO --------------------
XMLs encontrados:     6
Enviados (aprovados): 2
Descartados (filtro): 4
Com erro de leitura:  0

[ENVIADO]    35260144470771000118650010000009121060719696-nfe.xml
[ENVIADO]    35260144470771000118650010000009371060719695-nfe.xml
[DESCARTADO] 35260144470771000118550010000025741060719693-nfe.xml - modelo 55 (chave no nome do arquivo)
[DESCARTADO] 35260144470771000118550010000025761060719698-nfe.xml - modelo 55 (chave no nome do arquivo)
[DESCARTADO] 35260144470771000118650010000009211060719695-nfe.xml - tem CPF
[DESCARTADO] 35260144470771000118650010000009301060719694-nfe.xml - tem CPF

Arquivo gerado: C:\XmlNfce\01202644470.txt (2 nota(s))

Pressione ENTER para sair...
```
