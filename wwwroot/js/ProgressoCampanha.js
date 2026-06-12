const CORES = ['#c62828', '#991c8f', '#ffd937'];

let graficoProgresso;

async function carregarProgresso(campanhaId) {
    try {
        const res = await fetch(`/Home/ObterProgressoCampanha?id=${campanhaId}`);
        const dados = await res.json();

        renderizarGrafico(dados.porcentagemGeral, dados.categorias);
        renderizarCategorias(dados.categorias);
    } catch (err) {
        console.error("Erro ao carregar progresso:", err);
    }
}

function renderizarGrafico(porcentagem, categorias) {
    const canvas = document.getElementById('graficoCampanha');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    document.getElementById('total-porcentagem').innerText = porcentagem + "%";

    if (graficoProgresso) graficoProgresso.destroy();

    const dados = categorias.map(c => c.porcentagem);
    const pendente = Math.max(0, 100 - dados.reduce((a, b) => a + b, 0));
    const cores = categorias.map((_, i) => CORES[i]);

    graficoProgresso = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: [...categorias.map(c => c.nome), 'Pendente'],
            datasets: [{
                data: [...dados, pendente],
                backgroundColor: [...cores, '#f3ffd0'],
                borderWidth: 0,
                hoverOffset: 0
            }]
        },
        options: {
            cutout: '70%',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: { enabled: false },
                legend: { display: false }
            }
        }
    });
}

function renderizarCategorias(categorias) {
    const lista = document.getElementById('listaCategorias');
    if (!lista) return;

    lista.innerHTML = categorias.map((cat, i) => `
        <div class="categoria-item" style="margin-bottom: 12px;">
            <div class="categoria-info" style="display: flex; justify-content: space-between; margin-bottom: 4px; padding: 12px;">
                <span style="font-size: 13px; font-weight: 500; display: flex; align-items: center; gap: 6px;">
                    <span style="width: 8px; height: 8px; border-radius: 50%; background: ${CORES[i]}; display: inline-block;"></span>
                    ${cat.nome}
                </span>
                <span style="font-size: 12px; color: #778899;">${cat.atual}/${cat.meta} ${cat.unidade} · ${cat.porcentagem}%</span>
            </div>
            <div style="background: #f3ffd0; border-radius: 4px; height: 8px; width: 100%; overflow: hidden;">
                <div style="height: 100%; border-radius: 4px; background: ${CORES[i]}; width: ${Math.min(cat.porcentagem, 100)}%;"></div>
            </div>
        </div>
    `).join('');
}

document.addEventListener('DOMContentLoaded', function () {
    const select = document.getElementById('selectCampanha');

    select?.addEventListener('change', function () {
        const id = this.value;
        if (id) {
            carregarProgresso(id);
        } else {
            renderizarGrafico(0, []);
            document.getElementById('listaCategorias').innerHTML = '';
        }
    });
});




async function carregarHistoricoDoacoes() {
    const lista = document.getElementById('listaDoacoes');
    if (!lista) return;

    try {
        const res = await fetch('/Home/ObterHistoricoDoacoes');
        const doacoes = await res.json();

        if (!doacoes.length) {
            lista.innerHTML = '<p style="text-align:center;color:#999;padding:20px;">Nenhuma doação recebida ainda.</p>';
            return;
        }

        lista.innerHTML = doacoes.map(d => {
            const unidadeTexto = d.unidade ? ` ${d.unidade}` : '';
            const itemTexto = d.item ? ` ${d.item}` : '';
            const linhaCampanha = d.campanha ? `<strong>Campanha: </strong> ${d.campanha}` : `Doação avulsa`;

            return `
        <div class="doacao">
            <div class="doacao-id"><strong>CPF:  </strong>   ${d.documentoDoador}</div>
            <div class="doacao-info">
                <strong>QTD:</strong> ${d.quantidade}${unidadeTexto}<br><strong>Categoria: </strong> ${itemTexto}<br>
                ${linhaCampanha}
            </div>
        </div>
    `;
        }).join('');
    } catch (err) {
        console.error("Erro ao carregar histórico de doações:", err);
        lista.innerHTML = '<p style="text-align:center;color:#c01d36;padding:20px;">Erro ao carregar histórico.</p>';
    }
}

document.addEventListener('DOMContentLoaded', carregarHistoricoDoacoes);