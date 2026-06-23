document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.querySelector('.bc-navbar-toggle');
    const nav = document.getElementById('nav');
    if (toggle && nav) {
        toggle.addEventListener('click', () => nav.classList.toggle('open'));
    }
    document.querySelectorAll('[data-dialog]').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.getAttribute('data-dialog');
            const dlg = document.getElementById(id);
            if (dlg) dlg.showModal ? dlg.showModal() : dlg.setAttribute('open', '');
        });
    });
    document.querySelectorAll('.bc-dialog-close').forEach(b => {
        b.addEventListener('click', e => {
            const dlg = e.target.closest('dialog');
            if (dlg) dlg.close ? dlg.close() : dlg.removeAttribute('open');
        });
    });
});