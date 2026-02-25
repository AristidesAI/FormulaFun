import { CATEGORIES } from './trackPieces.js';
import { renderThumbnail } from './modelLoader.js';

export function buildPalette(pieces, onSelect) {
    const container = document.getElementById('piece-categories');
    container.innerHTML = '';

    const grouped = {};
    for (const piece of pieces) {
        if (!grouped[piece.category]) grouped[piece.category] = [];
        grouped[piece.category].push(piece);
    }

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

            // Thumbnail image
            const thumb = document.createElement('div');
            thumb.className = 'piece-thumb';
            const dataUrl = renderThumbnail(piece.modelId);
            if (dataUrl) {
                thumb.style.backgroundImage = `url(${dataUrl})`;
                thumb.style.backgroundSize = 'contain';
                thumb.style.backgroundRepeat = 'no-repeat';
                thumb.style.backgroundPosition = 'center';
            } else if (piece.modelId === '_checkpointStart') {
                thumb.style.background = 'linear-gradient(135deg, #00cc44, #cccc00)';
                thumb.textContent = 'S/F';
                thumb.style.display = 'flex';
                thumb.style.alignItems = 'center';
                thumb.style.justifyContent = 'center';
                thumb.style.fontSize = '10px';
                thumb.style.fontWeight = '700';
                thumb.style.color = '#fff';
            } else if (piece.modelId === '_checkpointWaypoint') {
                thumb.style.background = 'linear-gradient(135deg, #4488ff, #44ccff)';
                thumb.textContent = 'WP';
                thumb.style.display = 'flex';
                thumb.style.alignItems = 'center';
                thumb.style.justifyContent = 'center';
                thumb.style.fontSize = '10px';
                thumb.style.fontWeight = '700';
                thumb.style.color = '#fff';
            } else {
                thumb.style.background = '#222';
            }
            item.appendChild(thumb);

            const info = document.createElement('div');
            info.className = 'piece-info';

            const nameSpan = document.createElement('span');
            nameSpan.className = 'piece-name';
            nameSpan.textContent = piece.name;

            const sizeSpan = document.createElement('span');
            sizeSpan.className = 'piece-size';
            sizeSpan.textContent = `${piece.gridW}x${piece.gridD}`;

            info.appendChild(nameSpan);
            info.appendChild(sizeSpan);
            item.appendChild(info);

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

    // Search
    const searchInput = document.getElementById('piece-search');
    searchInput.addEventListener('input', () => {
        const query = searchInput.value.toLowerCase().trim();
        document.querySelectorAll('.piece-item').forEach(item => {
            const matches = !query || item.dataset.name.includes(query) || item.dataset.modelId.toLowerCase().includes(query);
            item.style.display = matches ? '' : 'none';
        });
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

export function bindToolbar({ onNew, onExport, onImport, onRotateCW, onRotateCCW, onDelete, onUndo, onRedo }) {
    document.getElementById('btn-new').addEventListener('click', () => {
        if (confirm('Clear all placed pieces and start a new track?')) onNew();
    });
    document.getElementById('btn-save').addEventListener('click', onExport);
    const fileInput = document.getElementById('file-input');
    document.getElementById('btn-load').addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) { onImport(e.target.files[0]); fileInput.value = ''; }
    });
    document.getElementById('btn-rotate-cw').addEventListener('click', onRotateCW);
    document.getElementById('btn-rotate-ccw').addEventListener('click', onRotateCCW);
    document.getElementById('btn-delete').addEventListener('click', onDelete);
    document.getElementById('btn-undo').addEventListener('click', onUndo);
    document.getElementById('btn-redo').addEventListener('click', onRedo);
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
