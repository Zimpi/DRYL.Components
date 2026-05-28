// Demo-ToC: vollständig JS-gesteuert via MutationObserver.
// Kein Blazor-Lifecycle-Timing nötig — reagiert direkt auf DOM-Änderungen.
window.drylToc = (() => {
    let _nav = null;
    let _iObserver = null;   // IntersectionObserver für aktive Section
    let _mObserver = null;   // MutationObserver für Navigation
    let _debounce = null;
    let _visible = new Set();

    const HEADING_SEL = '.main section > h3';

    function slugify(text) {
        return text.trim().toLowerCase()
            .replace(/[<>&"']/g, '')
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-+|-+$/g, '') || 'section';
    }

    function assignIds(headings) {
        headings.forEach(h => {
            if (h.id) return;
            let base = slugify(h.textContent);
            let id = base, n = 2;
            while (document.getElementById(id) && document.getElementById(id) !== h) {
                id = base + '-' + n++;
            }
            h.id = id;
        });
    }

    function setActive(id) {
        if (!_nav) return;
        _nav.querySelectorAll('.demo-toc-link').forEach(a => {
            a.classList.toggle('is-active', a.dataset.target === id);
        });
    }

    function setupIntersection(headings) {
        if (_iObserver) { _iObserver.disconnect(); _iObserver = null; }
        _visible.clear();
        if (!headings.length) return;

        _iObserver = new IntersectionObserver(entries => {
            entries.forEach(e => {
                if (e.isIntersecting) _visible.add(e.target.id);
                else _visible.delete(e.target.id);
            });
            // Topmost sichtbare Heading aktivieren
            for (const h of headings) {
                if (_visible.has(h.id)) { setActive(h.id); return; }
            }
        }, { rootMargin: '0px 0px -60% 0px', threshold: 0 });

        headings.forEach(h => _iObserver.observe(h));
    }

    function rebuild() {
        if (!_nav) return;

        // MutationObserver pausieren, damit eigene DOM-Änderungen keinen Loop auslösen
        if (_mObserver) _mObserver.disconnect();

        const headings = Array.from(document.querySelectorAll(HEADING_SEL));
        assignIds(headings);

        // ToC-Inhalt neu aufbauen
        _nav.innerHTML = '';
        _nav.classList.toggle('has-content', headings.length > 0);

        if (headings.length) {
            const label = document.createElement('p');
            label.className = 'demo-toc-label';
            label.textContent = 'Auf dieser Seite';
            _nav.appendChild(label);

            const ul = document.createElement('ul');
            headings.forEach((h, i) => {
                const li = document.createElement('li');
                const a = document.createElement('a');
                a.href = '#' + h.id;
                a.className = 'demo-toc-link' + (i === 0 ? ' is-active' : '');
                a.dataset.target = h.id;
                a.textContent = h.textContent.trim();
                a.addEventListener('click', e => {
                    e.preventDefault();
                    h.scrollIntoView({ behavior: 'smooth', block: 'start' });
                });
                li.appendChild(a);
                ul.appendChild(li);
            });
            _nav.appendChild(ul);
        }

        setupIntersection(headings);
        startMutationObserver(); // MutationObserver wieder starten
    }

    function startMutationObserver() {
        // Nur .demo-page-content beobachten, nicht .demo-toc selbst
        const target = document.querySelector('.demo-page-content') || document.querySelector('.main');
        if (!target) return;

        _mObserver = new MutationObserver(() => {
            clearTimeout(_debounce);
            // Kurz warten bis Blazor alle DOM-Änderungen einer Navigation abgeschlossen hat
            _debounce = setTimeout(rebuild, 100);
        });
        _mObserver.observe(target, { childList: true, subtree: true });
    }

    return {
        init(navElement) {
            _nav = navElement;
            rebuild();
            // MutationObserver wird am Ende von rebuild() gestartet
        },

        destroy() {
            if (_iObserver) { _iObserver.disconnect(); _iObserver = null; }
            if (_mObserver) { _mObserver.disconnect(); _mObserver = null; }
            clearTimeout(_debounce);
            _visible.clear();
            _nav = null;
        }
    };
})();
