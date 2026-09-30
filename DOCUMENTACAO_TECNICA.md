# Processador de NFC-e — Documentação Técnica

Fábrica de Projetos | 4º Termo ADS — UNIMAR

## O que o programa faz

Lê os XMLs da pasta `C:\XmlNfce`, pede ao usuário a **série** e a **competência** (AAAA-MM)
e gera o arquivo `envio.json` só com as notas de **consumidor final** (sem CPF/CNPJ do comprador).

Feito em C# (.NET 10), aplicativo de console. Não usa banco de dados nem chama a API.

## Arquitetura

| Arquivo | Função |
|---|---|
| `Program.cs` | Pede série e competência e mostra o resultado na tela. |
| `ProcessadorNotas.cs` | Percorre os XMLs da pasta. |
| `LeitorXmlNfce.cs` | Lê cada XML e pega os dados da nota. |
| `FiltroNfce.cs` | Decide se a nota é aprovada ou descartada. |
| `GeradorEnvioJson.cs` | Grava o `envio.json`. |

## Leitura dos XMLs

De cada nota o programa pega:

- **Chave:** atributo `Id` da tag `<infNFe>`, sem o prefixo `NFe`
- **Modelo:** tag `<mod>`
- **Série:** tag `<serie>`
- **Competência:** ano e mês da tag `<dhEmi>`
- **Comprador:** tags `<dest><CPF>` e `<dest><CNPJ>`

Arquivos corrompidos ou inválidos aparecem como erro na tela e não interrompem o programa.

## Filtros

A nota só vai para o `envio.json` se:

1. for NFC-e (modelo 65);
2. a série for igual à informada;
3. a competência for igual à informada;
4. não tiver CPF nem CNPJ do comprador.

## Saída (envio.json)

```json
[
  {
    "chave": "35260144470771000118650010000009121060719696",
    "competencia": "2026-01"
  }
]
```

## Casos de teste

XMLs fornecidos pelo professor, copiados para `C:\XmlNfce`. Teste feito com série `1` e competência `2026-01`.

**XML 1 — válido (nota 912):** é NFC-e, série 1, emitida em janeiro de 2026 e não tem CPF/CNPJ
do comprador. **Resultado: enviada.**

**XML 2 — inválido (nota 921):** é NFC-e, série 1, de janeiro de 2026, mas tem CPF do comprador.
**Resultado: descartada.**
