const elementos = {
  form: document.getElementById("form"),
  serie: document.getElementById("serie"),
  competencia: document.getElementById("competencia"),
  botaoProcessar: document.getElementById("botao-processar"),
  mensagemErro: document.getElementById("mensagem-erro"),
  resultado: document.getElementById("resultado"),
};

const estado = { detalhe: null };

const CAMPOS_CHAVE = [
  { nome: "cUF", descricao: "UF", inicio: 0, fim: 2 },
  { nome: "AAMM", descricao: "ano e mês", inicio: 2, fim: 6, destaque: true },
  { nome: "CNPJ", descricao: "emitente", inicio: 6, fim: 20 },
  { nome: "mod", descricao: "modelo", inicio: 20, fim: 22, destaque: true },
  { nome: "serie", descricao: "série", inicio: 22, fim: 25, destaque: true },
  { nome: "nNF", descricao: "número", inicio: 25, fim: 34 },
  { nome: "tpEmis", descricao: "tipo emissão", inicio: 34, fim: 35 },
  { nome: "cNF", descricao: "código", inicio: 35, fim: 43 },
  { nome: "cDV", descricao: "dígito verif.", inicio: 43, fim: 44 },
];

const SITUACOES = {
  ENVIADA: { classe: "enviada", texto: "Enviada" },
  DESCARTADA: { classe: "descartada", texto: "Descartada" },
  ERRO: { classe: "erro", texto: "Erro" },
};

function escapar(texto) {
  return String(texto ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function formatarDataHora(texto) {
  const [data, hora = ""] = String(texto).split(" ");
  const [ano, mes, dia] = data.split("-");
  return `${dia}/${mes}/${ano} às ${hora.slice(0, 5)}`;
}

// grupos de 4 dígitos, como no DANFE da NFC-e
function agruparChave(chave) {
  return chave.replace(/(\d{4})(?=\d)/g, "$1 ");
}

async function chamarApi(url, opcoes) {
  let resposta;
  try {
    resposta = await fetch(url, opcoes);
  } catch {
    throw new Error("Não foi possível falar com o servidor. Confira se o programa ainda está rodando.");
  }

  let corpo = null;
  try {
    corpo = await resposta.json();
  } catch {
    corpo = null;
  }

  if (!resposta.ok) {
    throw new Error(corpo?.erro ?? corpo?.detail ?? `O servidor respondeu com erro ${resposta.status}.`);
  }
  return corpo;
}

function mostrarErro(mensagem) {
  elementos.mensagemErro.textContent = mensagem;
  elementos.mensagemErro.hidden = false;
}

function esconderErro() {
  elementos.mensagemErro.hidden = true;
  elementos.mensagemErro.textContent = "";
}

elementos.competencia.addEventListener("input", () => {
  const digitos = elementos.competencia.value.replace(/\D/g, "").slice(0, 6);
  elementos.competencia.value = digitos.length > 2 ? `${digitos.slice(0, 2)}/${digitos.slice(2)}` : digitos;
});

elementos.serie.addEventListener("input", () => {
  elementos.serie.value = elementos.serie.value.replace(/\D/g, "").slice(0, 3);
});

elementos.form.addEventListener("submit", async (evento) => {
  evento.preventDefault();
  esconderErro();

  const serie = elementos.serie.value.trim();
  const competencia = elementos.competencia.value.trim();

  if (!serie) {
    mostrarErro("Informe a série da NFC-e (ex.: 1).");
    elementos.serie.focus();
    return;
  }
  if (!/^(0[1-9]|1[0-2])\/\d{4}$/.test(competencia)) {
    mostrarErro("Competência inválida. Use MM/AAAA, por exemplo 01/2026.");
    elementos.competencia.focus();
    return;
  }

  elementos.botaoProcessar.disabled = true;
  elementos.botaoProcessar.textContent = "Processando…";

  try {
    const detalhe = await chamarApi("/api/processamentos", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ serie, competencia }),
    });
    mostrarDetalhe(detalhe);
  } catch (erro) {
    mostrarErro(erro.message);
  } finally {
    elementos.botaoProcessar.disabled = false;
    elementos.botaoProcessar.textContent = "Processar notas";
  }
});

function mostrarDetalhe(detalhe) {
  estado.detalhe = detalhe;

  const r = detalhe.resumo;

  const linhas = detalhe.itens.map((item, indice) => linhaItem(item, indice)).join("");

  elementos.resultado.innerHTML = `
    <p class="resumo__titulo">Processado em ${escapar(formatarDataHora(r.dataHora))}</p>
    <p class="resumo__meta">Série ${escapar(r.serie)}, competência ${escapar(r.competencia)}</p>

    <ul class="legenda">
      <li><strong>${r.total}</strong> ${r.total === 1 ? "XML lido" : "XMLs lidos"}</li>
      <li class="enviada"><strong>${r.enviadas}</strong> ${r.enviadas === 1 ? "enviada" : "enviadas"}</li>
      <li class="descartada"><strong>${r.descartadas}</strong> ${r.descartadas === 1 ? "descartada" : "descartadas"}</li>
      <li class="erro"><strong>${r.erros}</strong> com erro</li>
    </ul>

    ${r.total === 0
      ? `<p class="vazio">Nenhum arquivo .xml foi encontrado na pasta ${escapar(r.pasta)}.</p>`
      : `<div class="tabela-rolagem">
          <table>
            <thead>
              <tr><th>Situação</th><th>Arquivo</th><th>Chave de acesso</th><th>Motivo</th></tr>
            </thead>
            <tbody>${linhas}</tbody>
          </table>
        </div>`}
  `;
}

function linhaItem(item, indice) {
  const situacao = SITUACOES[item.situacao] ?? SITUACOES.ERRO;
  const chave = item.chave
    ? `<button type="button" class="chave-botao" data-indice="${indice}" aria-expanded="false"
         title="Ver a composição da chave">${escapar(agruparChave(item.chave))}</button>`
    : `<span class="sem-chave">não identificada</span>`;

  return `
    <tr>
      <td><span class="etiqueta ${situacao.classe}">${situacao.texto}</span></td>
      <td class="arquivo-nome">${escapar(item.arquivo)}</td>
      <td>${chave}</td>
      <td>${escapar(item.motivo ?? "—")}</td>
    </tr>`;
}

elementos.resultado.addEventListener("click", (evento) => {
  const botao = evento.target.closest(".chave-botao");
  if (!botao) return;

  const linha = botao.closest("tr");
  const aberta = linha.nextElementSibling?.classList.contains("composicao");

  if (aberta) {
    linha.nextElementSibling.remove();
    botao.setAttribute("aria-expanded", "false");
    return;
  }

  const item = estado.detalhe.itens[Number(botao.dataset.indice)];
  const campos = CAMPOS_CHAVE.map((campo) => `
      <li class="${campo.destaque ? "destaque" : ""}">
        <span class="valor">${escapar(item.chave.slice(campo.inicio, campo.fim))}</span>
        <span class="nome">${campo.nome}, ${campo.descricao}</span>
      </li>`).join("");

  const nova = document.createElement("tr");
  nova.className = "composicao";
  nova.innerHTML = `<td colspan="4"><ul class="composicao__campos">${campos}</ul></td>`;
  linha.after(nova);
  botao.setAttribute("aria-expanded", "true");
});
