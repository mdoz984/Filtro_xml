# Processador de NFC-e — Documentação Técnica

Fábrica de Projetos | 4º Termo ADS — UNIMAR

## O que o programa faz

Lê os XMLs da pasta `C:\XmlNfce`, pede ao usuário a **série** e a **competência** (MM/AAAA)
e gera os arquivos de envio só com as notas de **consumidor final** (sem CPF/CNPJ do comprador).

A série é digitada normalmente (ex.: `1`) e o programa completa com zeros à esquerda (`001`),
que é o formato usado na chave de acesso. Aceita de 0 a 999.

Feito em C# (.NET 10), aplicativo de console. Não usa banco de dados nem chama a API.

## Arquitetura

| Arquivo | Função |
|---|---|
| `Program.cs` | Pede série e competência e mostra o resultado na tela. |
| `ChaveAcesso.cs` | Separa e valida os 44 dígitos da chave de acesso. |
| `ProcessadorNotas.cs` | Percorre os XMLs da pasta e aplica o filtro pelo nome do arquivo. |
| `LeitorXmlNfce.cs` | Lê cada XML e pega os dados da nota. |
| `FiltroNfce.cs` | Decide se a nota é aprovada ou descartada. |
| `GeradorEnvioJson.cs` | Grava os arquivos de envio (`mmaaaaxxxxx.txt`). |

## Chave de acesso

A chave tem 44 dígitos e segue a máscara:

```
## #### ############## ## ### ######### # ######## #
```

| Campo | Dígitos | Conteúdo |
|---|---|---|
| cUF | 2 | Código da UF do emitente (tabela do IBGE) |
| AAMM | 4 | Ano e mês da emissão |
| CNPJ | 14 | CNPJ do emitente |
| mod | 2 | Modelo (65 = NFC-e) |
| serie | 3 | Série |
| nNF | 9 | Número da nota |
| tpEmis | 1 | Tipo de emissão (1 normal, 9 contingência) |
| cNF | 8 | Código numérico |
| cDV | 1 | Dígito verificador |

A chave só é considerada válida se tiver 44 dígitos, a UF existir na tabela do IBGE, o mês estiver
entre 01 e 12 e o dígito verificador bater pelo **Módulo 11** (pesos de 2 a 9 da direita para a
esquerda sobre os 43 primeiros dígitos; resto 0 ou 1 dá DV 0, senão 11 - resto).

Exemplo: `35260144470771000118650010000009121060719696` → UF 35, emissão 01/2026,
CNPJ 44470771000118, modelo 65, série 001, nota 000000912, emissão normal, cNF 06071969, DV 6.

## Leitura dos XMLs

Cada arquivo passa por três etapas, e para na primeira que descartar a nota:

1. **Nome do arquivo:** se o nome tiver uma sequência de 44 dígitos que seja uma chave válida
   (ex.: `35260144470771000118650010000009121060719696-nfe.xml`), o modelo, a série e o mês/ano
   da chave são comparados com o que foi informado. Se não baterem, o arquivo é descartado
   **sem abrir o XML**. Se o nome não tiver chave (ex.: `nota-912.xml`), segue para a leitura normal.
2. **Chave do XML:** atributo `Id` da tag `<infNFe>`, sem o prefixo `NFe`. Passa pelas mesmas
   validações. Se a chave do nome do arquivo for diferente da chave do `Id`, o arquivo vai para erro.
3. **Dados da nota:** só depois que a chave foi aprovada o programa lê:
   - **Série:** tag `<serie>`
   - **Competência:** mês e ano da tag `<dhEmi>`
   - **Comprador:** tags `<dest><CPF>` e `<dest><CNPJ>`

Arquivos corrompidos ou inválidos aparecem como erro na tela e não interrompem o programa.

## Filtros

A nota só vai para o arquivo de envio se:

1. for NFC-e (modelo 65 na chave);
2. a série for igual à informada (na chave e na tag `<serie>`);
3. a competência for igual à informada (na chave e na tag `<dhEmi>`);
4. não tiver CPF nem CNPJ do comprador.

## Saída (mmaaaaxxxxx.txt)

O conteúdo é JSON, mas o arquivo é salvo como `.txt` em `C:\XmlNfce`, com o nome no formato
`mmaaaaxxxxx.txt`: mês (2 dígitos), ano (4 dígitos) e os 5 primeiros dígitos do CNPJ do emitente.
Exemplo: competência 01/2026 e CNPJ 44470771000118 → `01202644470.txt`.

As notas ficam ordenadas pelo número (nNF) e recebem um campo `sequencia` (1, 2, 3...).
Se houver notas de mais de um emitente, é gerado um arquivo para cada CNPJ, e a sequência
recomeça em cada arquivo. Se nenhuma nota for aprovada, nenhum arquivo é gerado.

```json
[
  {
    "sequencia": 1,
    "chave": "35260144470771000118650010000009121060719696",
    "competencia": "01/2026"
  }
]
```

## Casos de teste

XMLs fornecidos pelo professor, copiados para `C:\XmlNfce`. Teste feito com série `1` e competência `01/2026`.

**XML 1 — válido (nota 912):** é NFC-e, série 1, emitida em janeiro de 2026 e não tem CPF/CNPJ
do comprador. **Resultado: enviada.**

**XML 2 — inválido (nota 921):** é NFC-e, série 1, de janeiro de 2026, mas tem CPF do comprador.
**Resultado: descartada.**

**XMLs de modelo 55 (notas 2574 e 2576):** a chave no nome do arquivo já mostra que não são NFC-e.
**Resultado: descartadas sem abrir o XML.**

**Nota sem chave no nome (ex.: `nota-912.xml`):** o arquivo não é descartado pelo nome e segue
para a leitura normal do XML. **Resultado: depende do conteúdo da nota.**
