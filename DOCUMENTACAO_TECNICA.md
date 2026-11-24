# Processador de NFC-e — Documentação Técnica

Fábrica de Projetos | 4º Termo ADS — UNIMAR

## O que o programa faz

Lê os XMLs da pasta **`C:\XmlNfce`** (fixa, definida no escopo do projeto), recebe do usuário a **série** e a **competência** (MM/AAAA)
e gera o arquivo de envio só com as notas de **consumidor final autorizadas** (cStat 100, sem CPF/CNPJ
do comprador). Cada processamento fica gravado num banco de dados SQLite.

A série é digitada normalmente (ex.: `1`) e o programa completa com zeros à esquerda (`001`),
que é o formato usado na chave de acesso. Aceita de 0 a 999.

## Stack

| Camada | Tecnologia | Onde fica |
|---|---|---|
| Front-end | HTML, CSS e JavaScript puro (sem framework), servidos pelo próprio back-end | `wwwroot/` |
| Back-end | C# com ASP.NET Core Minimal API (.NET 10) | `Program.cs`, `Api/`, `Servicos/`, `Modelos/` |
| Banco de dados | SQLite com o pacote `Microsoft.Data.Sqlite` (SQL escrito à mão, sem ORM) | `Dados/`, arquivo `processador_nfce.db` |

O SQLite foi escolhido porque o banco é um arquivo só, criado automaticamente na primeira execução,
sem precisar instalar servidor de banco na máquina da apresentação.

## Arquitetura

```
Navegador (wwwroot)  --HTTP/JSON-->  API (Api/EndpointsProcessamento.cs)
                                        |-- Servicos/  regras de negócio (leitura, filtros, geração do JSON)
                                        |-- Dados/     gravação e consulta no SQLite
```

| Arquivo | Função |
|---|---|
| `Program.cs` | Sobe o servidor web, cria as tabelas do banco e registra as rotas. |
| `Api/EndpointsProcessamento.cs` | Rotas da API e validação da pasta, série e competência. |
| `Servicos/ValidadorEntrada.cs` | Valida e normaliza série (`1` → `001`) e competência (`MM/AAAA`). |
| `Modelos/ChaveAcesso.cs` | Separa e valida os 44 dígitos da chave de acesso. |
| `Servicos/ProcessadorNotas.cs` | Percorre os XMLs da pasta e aplica o filtro pelo nome do arquivo. |
| `Servicos/LeitorXmlNfce.cs` | Lê cada XML e pega os dados da nota, inclusive o cStat do protocolo. |
| `Servicos/FiltroNfce.cs` | Decide se a nota é aprovada ou descartada (todas as regras ficam aqui). |
| `Servicos/GeradorEnvioJson.cs` | Grava os arquivos de envio (`mmaaaaxxxxx.txt`). |
| `Dados/RepositorioProcessamentos.cs` | Cria as tabelas e grava/consulta o histórico no SQLite. |
| `wwwroot/index.html`, `css/`, `js/` | Tela: série, competência e a lista das notas lidas com a situação de cada uma. |

### Rotas da API

| Método | Rota | O que faz |
|---|---|---|
| GET | `/api/config` | Devolve a pasta dos XMLs (`C:\XmlNfce`). |
| POST | `/api/processamentos` | Recebe `{ serie, competencia }`, processa `C:\XmlNfce`, gera o arquivo e grava no banco. |
| GET | `/api/processamentos` | Lista os últimos 50 processamentos gravados no banco (não é usada pela tela). |
| GET | `/api/processamentos/{id}` | Detalhe de um processamento: cada XML com situação e motivo, e o JSON gerado. |

Erros de entrada (série, competência ou pasta inexistente) voltam com status 400 e a mensagem em `erro`.

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
| mod | 2 | Modelo (65 = NFC-e, 55 = NF-e) |
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

Na tela, clicar numa chave mostra essa composição campo a campo.

**Atenção:** o "65" é o **modelo** (NFC-e), não a série. A série é o campo de 3 dígitos logo depois do modelo.

## Leitura dos XMLs

A pasta é sempre `C:\XmlNfce`: ela não aparece na tela e o back-end não aceita outro caminho.
Antes de tudo, a API confere se a **pasta existe**. Se não existir, nada é processado e a tela mostra o erro.

Depois, cada arquivo `.xml` passa por três etapas, e para na primeira que descartar a nota:

1. **Nome do arquivo:** se o nome tiver uma sequência de 44 dígitos que seja uma chave válida
   (ex.: `35260144470771000118650010000009121060719696-nfe.xml`), o modelo, a série e o mês/ano
   da chave são comparados com o que foi informado. Se não baterem, o arquivo é descartado
   **sem abrir o XML**. Se o nome não tiver chave (ex.: `nota-912.xml`), segue para a leitura normal.
2. **Chave do XML:** atributo `Id` da tag `<infNFe>`, sem o prefixo `NFe`. Passa pelas mesmas
   validações. Se a chave do nome do arquivo for diferente da chave do `Id`, o arquivo vai para erro.
3. **Dados da nota:** só depois que a chave foi aprovada o programa lê:
   - **Status da autorização:** tag `<cStat>` em `<nfeProc><protNFe><infProt>`
   - **Série:** tag `<ide><serie>`
   - **Competência:** mês e ano da tag `<ide><dhEmi>`
   - **Comprador:** tags `<dest><CPF>` e `<dest><CNPJ>`

As tags são buscadas pelo nome local, então funciona com ou sem namespace e com a raiz `<NFe>` ou `<nfeProc>`.
Arquivos corrompidos ou inválidos aparecem como erro e não interrompem o processamento.

## Filtros

A nota só vai para o arquivo de envio se:

1. for NFC-e (modelo 65 na chave);
2. a série for igual à informada (na chave e na tag `<serie>`);
3. a competência for igual à informada (na chave e na tag `<dhEmi>`);
4. tiver protocolo de autorização com **cStat = 100** (autorizado o uso). Nota cancelada (101),
   denegada (110, 301, 302...) ou XML sem `<protNFe>` é descartada;
5. não tiver CPF nem CNPJ do comprador (consumidor final);
6. a mesma chave não tiver aparecido antes no mesmo processamento (duplicada).

## Saída (mmaaaaxxxxx.txt)

O conteúdo é JSON, mas o arquivo é salvo como `.txt` na própria pasta dos XMLs, com o nome no formato
`mmaaaaxxxxx.txt`: mês (2 dígitos), ano (4 dígitos) e os 5 primeiros dígitos do CNPJ do emitente.
Exemplo: competência 01/2026 e CNPJ 44470771000118 → `01202644470.txt`.

Cada nota tem só a **chave**, a **série** e o **ano/mês** (MM/AAAA), ordenadas pelo número da nota (nNF).
Se houver notas de emitentes com início de CNPJ diferente, é gerado um arquivo para cada um.
Se nenhuma nota for aprovada, nenhum arquivo é gerado. O arquivo não aparece na tela: para conferir, abra a pasta `C:\XmlNfce`.

```json
[
  {
    "chave": "35260144470771000118650010000009121060719696",
    "serie": "001",
    "competencia": "01/2026"
  }
]
```

## Banco de dados

Arquivo `processador_nfce.db`, criado na pasta do projeto na primeira execução. As tabelas são criadas
com `CREATE TABLE IF NOT EXISTS`, e cada processamento é gravado numa única transação.

```sql
CREATE TABLE Processamento (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    DataHora     TEXT    NOT NULL,
    Pasta        TEXT    NOT NULL,
    Serie        TEXT    NOT NULL,
    Competencia  TEXT    NOT NULL,
    Enviadas     INTEGER NOT NULL,
    Descartadas  INTEGER NOT NULL,
    Erros        INTEGER NOT NULL
);

CREATE TABLE NotaProcessada (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessamentoId  INTEGER NOT NULL REFERENCES Processamento(Id) ON DELETE CASCADE,
    Arquivo          TEXT    NOT NULL,
    Chave            TEXT    NULL,
    Situacao         TEXT    NOT NULL CHECK (Situacao IN ('ENVIADA', 'DESCARTADA', 'ERRO')),
    Motivo           TEXT    NULL
);

CREATE TABLE ArquivoEnvio (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessamentoId  INTEGER NOT NULL REFERENCES Processamento(Id) ON DELETE CASCADE,
    NomeArquivo      TEXT    NOT NULL,
    Caminho          TEXT    NOT NULL,
    QuantidadeNotas  INTEGER NOT NULL,
    Conteudo         TEXT    NOT NULL
);
```

- **Processamento:** uma linha por clique em "Processar notas" (parâmetros e totais).
- **NotaProcessada:** uma linha por XML lido, com a situação e o motivo do descarte ou erro.
- **ArquivoEnvio:** os arquivos `.txt` gerados, com o JSON completo.

Para abrir o banco e mostrar as tabelas, dá para usar o DB Browser for SQLite (gratuito).

## Como rodar

1. Abra `ProcessadorNfce.slnx` no Visual Studio (ou rode `dotnet run` dentro da pasta `ProcessadorNfce`).
2. Na primeira vez, o Visual Studio baixa o pacote `Microsoft.Data.Sqlite` (precisa de internet só nessa hora).
3. Aperte F5. O navegador abre em `http://localhost:5080`.
4. Coloque os XMLs em `C:\XmlNfce`, informe série e competência e clique em **Processar notas**.

## Casos de teste

XMLs fornecidos pelo professor, copiados para `C:\XmlNfce`. Teste feito com série `1` e competência `01/2026`.

**XML 1 — válido (nota 912):** é NFC-e, série 1, emitida em janeiro de 2026, autorizada (cStat 100)
e não tem CPF/CNPJ do comprador. **Resultado: enviada.**

**XML 2 — inválido (nota 921):** é NFC-e, série 1, de janeiro de 2026, mas tem CPF do comprador.
**Resultado: descartada ("tem CPF do comprador").**

**XMLs de modelo 55 (notas 2574 e 2576):** a chave no nome do arquivo já mostra que não são NFC-e.
**Resultado: descartadas sem abrir o XML.**

**Nota sem chave no nome (ex.: `nota-912.xml`):** o arquivo não é descartado pelo nome e segue
para a leitura normal do XML. **Resultado: depende do conteúdo da nota.**

**Nota não autorizada (cStat diferente de 100, ex.: 101 cancelada):** copie o XML 1, troque o
`<cStat>100</cStat>` por `<cStat>101</cStat>` e renomeie. **Resultado: descartada ("cStat 101, nota não autorizada").**

**XML sem protocolo:** copie o XML 1 e apague o bloco `<protNFe>...</protNFe>`.
**Resultado: descartada ("sem protocolo de autorização").**

**Pasta inexistente:** renomeie a pasta `C:\XmlNfce` (ex.: para `C:\XmlNfce_`) e clique em Processar notas.
Depois volte o nome original.
**Resultado: mensagem de erro na tela, nada é processado nem gravado.**
