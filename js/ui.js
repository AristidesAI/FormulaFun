import { CATEGORIES } from './trackPieces.js';

export function buildPalette(pieces, onSelect) {
    const container = document.getElementById('piece-categories');
    container.innerHTML = '';

    // Group pieces by category
    const grouped = {};
    for (const piece of pieces) {
        if (!grouped[piece.category]) grouped[piece.category] = [];
        grouped[piece.category].push(piece);
    }

    // Build category accordions in CATEGORIES order
    for (const [catKey, catName] of Object.entries(CATEGORIES)) {
        const catPieces = grouped[catKey];
        if (!catPieces || catPieces.length === 0) continue;

        const header = document.createElement('div');
        header.className = 'category-header';
        header.innerHTML = `<span>${catName} (${catPieces.length})</span><span class="arrow">&#9654;</span>`;

        const body = document.createElement('div');
        body.className = 'category-body';

        header.addEventListener('click', () => {
            header.classList.toggle('open');
            body.classList.toggle('open');
        });

        for (const piece of catPieces) {
            const item = document.createElement('div');
            item.className = 'piece-item';
            item.dataset.modelId = piece.modelId;
            item.dataset.name = piece.name.toLowerCase();

            const nameSpan = document.createElement('span');
            nameSpan.textContent = piece.name;

            const sizeSpan = document.createElement('span');
            sizeSpan.className = 'piece-size';
            sizeSpan.textContent = `${piece.gridW}x${piece.gridD}`;

            item.appendChild(nameSpan);
            item.appendChild(sizeSpan);

            item.addEventListener('click', (e) => {
                e.stopPropagation();
                document.querySelectorAll('.piece-item.selected').forEach(el => el.classList.remove('selected'));
                item.classList.add('selected');
                onSelect(piece);
            });

            body.appendChild(item);
        }

        container.appendChild(header);
        container.appendChild(body);
    }

    // Search filter
    const searchInput = document.getElementById('piece-search');
    searchInput.addEventListener('input', () => {
        const query = searchInput.value.toLowerCase().trim();
        document.querySelectorAll('.piece-item').forEach(item => {
            const matches = !query || item.dataset.name.includes(query) || item.dataset.modelId.toLowerCase().includes(query);
            item.style.display = matches ? '' : 'none';
        });
        // Auto-expand categories with visible items when searching
        if (query) {
            document.querySelectorAll('.category-body').forEach(body => {
                const hasVisible = body.querySelector('.piece-item:not([style*="display: none"])');
                if (hasVisible) {
                    body.classList.add('open');
                    body.previousElementSibling.classList.add('open');
                }
            });
        }
    });
}

export function bindToolbar({ onNew, onExport, onImport, onRotateCW, onRotateCCW, onDelete }) {
    document.getElementById('btn-new').addEventListener('click', () => {
        if (confirm('Clear all placed pieces and start a new track?')) {
            onNew();
        }
    });

    document.getElementById('btn-save').addEventListener('click', onExport);

    const fileInput = document.getElementById('file-input');
    document.getElementById('btn-load').addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            onImport(e.target.files[0]);
            fileInput.value = '';
        }
    });

    document.getElementById('btn-rotate-cw').addEventListener('click', onRotateCW);
    document.getElementById('btn-rotate-ccw').addEventListener('click', onRotateCCW);
    document.getElementById('btn-delete').addEventListener('click', onDelete);
}

export function setStatus(text) {
    document.getElementById('status-bar').textContent = text;
}

export function hideLoading() {
    document.getElementById('loading-overlay').classList.add('hidden');
}

export function updateLoadingProgress(loaded, total) {
    document.getElementById('loading-progress').textContent = `${loaded} / ${total}`;
}
