// Controle de alertas e alternância de Tema Claro / Escuro (Voz do Povo)

document.addEventListener('DOMContentLoaded', function () {
    // Fechar alertas
    document.querySelectorAll('.close-alert').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const alertBox = this.closest('.alert');
            if (alertBox) {
                alertBox.style.display = 'none';
            }
        });
    });

    // Atualizar rótulo/ícone do botão de tema
    atualizarBotaoTema();
});

function alternarTemaVozDoPovo() {
    const html = document.documentElement;
    const temaAtual = html.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    const novoTema = temaAtual === 'dark' ? 'light' : 'dark';

    html.setAttribute('data-theme', novoTema);
    localStorage.setItem('vp-theme', novoTema);
    atualizarBotaoTema();
}

function atualizarBotaoTema() {
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const label = document.getElementById('vp-theme-label');
    const iconSun = document.getElementById('vp-icon-sun');
    const iconMoon = document.getElementById('vp-icon-moon');

    if (label) {
        label.textContent = isDark ? 'Tema Claro' : 'Tema Escuro';
    }
    if (iconSun && iconMoon) {
        iconSun.style.display = isDark ? 'inline-block' : 'none';
        iconMoon.style.display = isDark ? 'none' : 'inline-block';
    }
}
