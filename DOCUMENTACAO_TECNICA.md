# Processador de NFC-e — Documentação Técnica

**Fábrica de Projetos | 4º Termo ADS — UNIMAR**
Desafio técnico: extração de NFC-e (modelo 65) emitidas para consumidor final.

---

## 1. Objetivo

Ler os XMLs das NFC-e armazenados em `C:\XmlNfce`, selecionar apenas as notas
**sem identificação do comprador** (sem CPF e sem CNPJ) de uma **série** e **competência**
informadas pelo usuário, e gerar o arquivo `envio.json` que será enviado à API externa.

**Tecnologia:** C# / .NET 10, aplicativo de console, sem bibliotecas externas
(somente `System.Xml.Linq` e `System.Text.Json`, que já vêm no .NET).

**Fora do escopo desta sprint:** a aplicação **não chama a API externa** (apenas gera o
arquivo que será enviado) e **não usa banco de dados** — a entrada são os arquivos XML
e a saída é o arquivo JSON.

---

## 2. Arquitetura

A solução é dividida em camadas pequenas, cada uma com uma única responsabilidade:

```
Program.cs  (interface com o usuário: pede os parâmetros e mostra o relatório)
   │
   ├─► ProcessadorNotas   percorre a pasta e coordena o processamento de cada arquivo
   │      ├─► LeitorXmlNfce   lê o XML e extrai os campos (chave, série, dhEmi, dest, vNF)
   │      └─► FiltroNfce      decide se a nota atende aos critérios (ou por que foi descartada)
   │
   └─► GeradorEnvioJson   grava o envio.json com as notas aprovadas
```

| Arquivo | Responsabilidade |
|---|---|
| `Program.cs` | Solicita série e competência, valida as entradas, chama o processamento, grava a saída e exibe o relatório. Trata erros da pasta (inexistente, sem permissão). |
| `Servicos/ProcessadorNotas.cs` | Lista os `.xml` de `C:\XmlNfce`, processa um a um e separa em **aprovadas**, **descartadas** e **com erro**. Um arquivo com problema não interrompe os demais. |
| `Servicos/LeitorXmlNfce.cs` | Carrega o XML (tratando codificação) e extrai os dados para um objeto `NotaFiscal`. |
| `Servicos/FiltroNfce.cs` | Aplica os critérios de série, competência e consumidor final. |
| `Servicos/GeradorEnvioJson.cs` | Converte as notas aprovadas para o formato de envio e grava o JSON. |
| `Modelos/NotaFiscal.cs` | Dados extraídos de uma NFC-e. |
| `Modelos/NotaEnvio.cs` | Item do `envio.json` (`chave`, `competencia`, `valor`). |
| `Modelos/ResultadoProcessamento.cs` | Resultado da execução (listas de aprovadas, descartadas e erros). |

### Fluxo de execução

1. O usuário informa a **série** (0 a 999) e a **competência** (`AAAA-MM`). Entradas inválidas são recusadas e perguntadas de novo.
2. O programa lista os arquivos `.xml` que estão diretamente em `C:\XmlNfce`.
3. Para cada arquivo: lê o XML → aplica o filtro → classifica o arquivo como aprovado, descartado (com motivo) ou erro (com motivo).
4. Grava `C:\XmlNfce\envio.json` com as notas aprovadas.
5. Exibe um relatório com o destino de cada arquivo.

---

## 3. Lógica de leitura dos XMLs

A leitura usa **LINQ to XML** (`XDocument`). As tags são localizadas pelo **nome local**,
ignorando o namespace `http://www.portalfiscal.inf.br/nfe`. Assim o programa funciona tanto
com o XML só da nota (`<NFe>`) quanto com o XML autorizado (`<nfeProc>`), que envolve a
nota junto com o protocolo da SEFAZ.

| Campo | Caminho no XML | Tratamento |
|---|---|---|
| Chave | atributo `Id` de `<infNFe>` | Remove o prefixo `NFe` e valida que restam **44 dígitos**. |
| Modelo | `<infNFe><ide><mod>` | `65` = NFC-e, `55` = NF-e. |
| Série | `<infNFe><ide><serie>` | Texto obrigatório. |
| Data de emissão | `<infNFe><ide><dhEmi>` | Convertida para `DateTimeOffset`. A competência (`AAAA-MM`) usa o ano e o mês **do próprio XML**, sem converter para o fuso do computador. |
| CPF / CNPJ do comprador | `<infNFe><dest><CPF>` e `<infNFe><dest><CNPJ>` | Opcionais. Tag ausente ou vazia = não identificado. O CNPJ do **emitente** (`<emit><CNPJ>`) não é considerado. |
| Valor | `<infNFe><total><ICMSTot><vNF>` | Convertido para `decimal` usando **ponto** como separador (padrão SEFAZ). |

### Composição da chave de acesso (44 dígitos)

```
35 2609 12345678000195 65 001 000000001 1 10000001 6
│  │    │              │  │   │         │ │        └ dígito verificador (módulo 11)
│  │    │              │  │   │         │ └ código numérico
│  │    │              │  │   │         └ tipo de emissão
│  │    │              │  │   └ número da nota
│  │    │              │  └ série
│  │    │              └ modelo (65 = NFC-e, 55 = NF-e)
│  │    └ CNPJ do emitente
│  └ ano/mês de emissão (AAMM)
└ UF (35 = SP)
```

### Codificação (UTF-8 e variações)

- `XDocument.Load` detecta sozinho a codificação pelo **BOM** ou pela declaração `<?xml encoding="..."?>` (UTF-8, UTF-16, ISO-8859-1).
- Se o arquivo foi **gravado em ISO-8859-1 mas declara UTF-8** (erro comum de alguns emissores), os acentos quebram a leitura. Nesse caso o programa tenta de novo interpretando os bytes como Latin-1 e só considera o arquivo corrompido se a segunda tentativa também falhar.
- O provedor `CodePagesEncodingProvider` é registrado para aceitar XMLs declarados em `windows-1252`.

---

## 4. Aplicação dos filtros

Uma nota só vai para o `envio.json` se for uma **NFC-e** e atender **simultaneamente** aos 4 critérios:

| # | Critério | Onde é aplicado | Regra |
|---|---|---|---|
| — | Pré-requisito: modelo | `FiltroNfce` | `<mod>` deve ser `65` (NFC-e). A NF-e (modelo 55) usa o mesmo layout de XML, mas é outro documento fiscal e é descartada. |
| 1 | Localização | `ProcessadorNotas` | Somente arquivos com extensão `.xml` (maiúsculas ou minúsculas) diretamente em `C:\XmlNfce`. Subpastas e outros tipos de arquivo são ignorados. |
| 2 | Série | `FiltroNfce` | `<serie>` igual à série informada. A série digitada é normalizada (`001` → `1`), porque no XML ela nunca tem zeros à esquerda. |
| 3 | Competência | `FiltroNfce` | `AAAA-MM` de `<dhEmi>` igual à competência informada. |
| 4 | Consumidor final | `FiltroNfce` | `<dest><CPF>` e `<dest><CNPJ>` ausentes ou vazios. |

Regra adicional: se duas notas tiverem a **mesma chave** (arquivo copiado em duplicidade),
apenas a primeira é enviada.

---

## 5. Tratamento de exceções

| Situação | Comportamento |
|---|---|
| Pasta `C:\XmlNfce` não existe | Mensagem de erro e encerramento (código 1). |
| Sem permissão de leitura/escrita na pasta | Mensagem de erro e encerramento (código 1). |
| Arquivo XML corrompido/mal formado | Arquivo listado como **ERRO** (com linha e posição), os demais continuam. |
| XML válido mas não é NFC-e ou falta tag obrigatória | Arquivo listado como **ERRO** com a tag ausente. |
| Chave, data ou valor em formato inválido | Arquivo listado como **ERRO** com o valor encontrado. |
| Arquivo sem permissão de leitura ou bloqueado | Arquivo listado como **ERRO**, os demais continuam. |
| NF-e (modelo 55), outra série ou outra competência | Arquivo listado como **DESCARTADO** com o motivo. |
| Codificação ISO-8859-1 / declaração incorreta | Tratada automaticamente (seção 3). |
| Nenhuma nota aprovada | `envio.json` é gerado com lista vazia `[]`. |

---

## 6. Formato de saída

Arquivo `C:\XmlNfce\envio.json`, em UTF-8:

```json
[
  {
    "chave": "35260912345678000195650010000000011100000016",
    "competencia": "2026-09",
    "valor": 150.45
  }
]
```

---

## 7. Casos de teste (massa de dados)

A massa de testes fica na pasta `MassaDeTestes`, dividida em duas partes:

- `MassaDeTestes/Professor` — os 6 XMLs reais fornecidos para a atividade (emitente Comercial Garcia Ltda, Marília/SP, janeiro de 2026). **São os casos de teste oficiais.**
- `MassaDeTestes/Extras` — XMLs fictícios criados pela equipe para cobrir situações que não aparecem nos arquivos fornecidos (arquivo corrompido, codificação ISO-8859-1, outra série etc.).

Para executar, copie os XMLs para `C:\XmlNfce`.

### 7.1 Arquivos fornecidos — série `1`, competência `2026-01`

| Nota (nº) | Modelo | Emissão | Comprador | Valor | Resultado | Motivo |
|---|---|---|---|---|---|---|
| 912 | 65 | 03/01/2026 | não identificado | 75,00 | **Enviado** | Atende a todos os critérios. |
| 921 | 65 | 05/01/2026 | CPF | 240,00 | Descartado | Comprador identificado por CPF. |
| 930 | 65 | 06/01/2026 | CPF | 108,00 | Descartado | Comprador identificado por CPF. |
| 937 | 65 | 07/01/2026 | não identificado | 195,00 | **Enviado** | Atende a todos os critérios. |
| 2574 | **55** | 09/01/2026 | CNPJ | 432,00 | Descartado | É NF-e (modelo 55), não NFC-e. |
| 2576 | **55** | 09/01/2026 | CNPJ | 210,00 | Descartado | É NF-e (modelo 55), não NFC-e. |

#### XML 1 — válido: `35260144470771000118650010000009121060719696-nfe.xml`

**Resultado: ENVIADO.**
Justificativa: está em `C:\XmlNfce`; `<mod>65</mod>` indica que é uma NFC-e;
`<serie>1</serie>` é igual à série informada; `<dhEmi>2026-01-03T09:30:34-03:00</dhEmi>`
pertence à competência 2026-01; e **não possui a tag `<dest>`**, ou seja, o comprador não
foi identificado (consumidor final).
Gera no JSON: chave `35260144470771000118650010000009121060719696`, competência `2026-01`, valor `75.00`.

#### XML 2 — inválido: `35260144470771000118650010000009211060719695-nfe.xml`

**Resultado: DESCARTADO.**
Justificativa: é uma NFC-e (modelo 65) da série 1 e da competência 2026-01, porém possui
`<dest><CPF>` preenchido. O comprador está identificado por CPF, portanto **não é consumidor
final** e a nota não deve ser enviada.

#### Observação: NF-e (modelo 55) na pasta

As notas 2574 e 2576 são **NF-e (modelo 55)**, identificável pela tag `<mod>55</mod>` e pelos
dígitos 21–22 da chave (`...0118`**`55`**`001...`). Elas usam o mesmo layout de XML da NFC-e
(inclusive a 2574 vem só com `<NFe>`, sem o envelope `<nfeProc>`), mas não fazem parte do
escopo e são descartadas pela verificação de modelo.

#### Resultado da execução

```
XMLs encontrados:     6
Enviados (aprovados): 2
Descartados (filtro): 4
Com erro de leitura:  0

[ENVIADO]    35260144470771000118650010000009121060719696-nfe.xml - R$ 75,00
[ENVIADO]    35260144470771000118650010000009371060719695-nfe.xml - R$ 195,00
[DESCARTADO] 35260144470771000118550010000025741060719693-nfe.xml - modelo 55 (NF-e), não é NFC-e (modelo 65)
[DESCARTADO] 35260144470771000118550010000025761060719698-nfe.xml - modelo 55 (NF-e), não é NFC-e (modelo 65)
[DESCARTADO] 35260144470771000118650010000009211060719695-nfe.xml - comprador identificado por CPF em <dest><CPF> (não é consumidor final)
[DESCARTADO] 35260144470771000118650010000009301060719694-nfe.xml - comprador identificado por CPF em <dest><CPF> (não é consumidor final)

Arquivo gerado: C:\XmlNfce\envio.json (2 nota(s), total R$ 270,00)
```

`envio.json` gerado:

```json
[
  {
    "chave": "35260144470771000118650010000009121060719696",
    "competencia": "2026-01",
    "valor": 75.00
  },
  {
    "chave": "35260144470771000118650010000009371060719695",
    "competencia": "2026-01",
    "valor": 195.00
  }
]
```

Com a competência `2026-02` nenhuma nota é aprovada e o `envio.json` é gerado com `[]`.

### 7.2 Casos extras (fictícios) — série `1`, competência `2026-09`

| Arquivo | Resultado | Motivo |
|---|---|---|
| `01_valido_consumidor_final.xml` | Enviado | Série 1, emitida em 2026-09, sem `<dest>`. |
| `02_invalido_com_cpf.xml` | Descartado | Comprador identificado por `<dest><CPF>`. |
| `03_invalido_serie_diferente.xml` | Descartado | `<serie>2</serie>`, diferente da série informada (1). |
| `04_invalido_competencia_diferente.xml` | Descartado | Emitida em `2026-08-31`, fora da competência 2026-09. |
| `05_invalido_com_cnpj.xml` | Descartado | Comprador identificado por `<dest><CNPJ>`. |
| `06_valido_iso8859_fim_do_mes.xml` | Enviado | Arquivo em ISO-8859-1 com acentos ("PADARIA SÃO JOSÉ"), emitido em 30/09 às 23h50 (continua em 2026-09) e com `<dest><CPF></CPF></dest>` **vazio** (não há valor preenchido). |
| `07_erro_arquivo_corrompido.xml` | Erro | XML cortado no meio (mal formado). O processamento continua nos outros arquivos. |

> Os dados dos casos extras são fictícios (CNPJ/CPF de exemplo, ambiente de homologação `tpAmb = 2`).
