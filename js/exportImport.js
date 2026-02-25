import { TRACK_PIECES, getPieceDef } from './trackPieces.js';

export function exportTrackJSON(gridSystem, trackName) {
    const pieces = [];
    for (const [id, placed] of gridSystem.pieces) {
        const def = getPieceDef(placed.modelId);
        pieces.push({
            id: placed.id,
            modelId: placed.modelId,
            category: def ? def.category : 'UNKNOWN',
            gridX: placed.anchorX,
            gridZ: placed.anchorZ,
            rotationY: placed.rotation,
            gridWidth: def ? def.gridW : 1,
            gridDepth: def ? def.gridD : 1,
        });
    }

    const layout = {
        formatVersion: 1,
        trackName: trackName || 'Untitled Track',
        author: 'FormulaFun Track Builder',
        createdAt: new Date().toISOString(),
        gridSize: gridSystem.size,
        pieces,
        metadata: {
            totalPieces: pieces.length,
            hasStartLine: pieces.some(p => p.modelId === 'roadStart' || p.modelId === 'roadStartPositions' || p.modelId === '_checkpointStart'),
            hasCheckpoints: pieces.some(p => p.modelId === '_checkpointStart'),
            waypointCount: pieces.filter(p => p.modelId === '_checkpointWaypoint').length,
        },
    };

    const json = JSON.stringify(layout, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${(trackName || 'track').replace(/\s+/g, '_')}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    return layout;
}

export async function importTrackJSON(file, gridSystem, placement) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const layout = JSON.parse(e.target.result);

                // Clear existing
                placement.clearAll();

                // Set track name
                if (layout.trackName) {
                    document.getElementById('track-name').value = layout.trackName;
                }

                // Place each piece
                let placed = 0;
                let skipped = 0;
                for (const piece of layout.pieces) {
                    const def = getPieceDef(piece.modelId);
                    if (!def) {
                        console.warn(`Unknown piece: ${piece.modelId}, skipping`);
                        skipped++;
                        continue;
                    }

                    const result = gridSystem.place(def, piece.gridX, piece.gridZ, piece.rotationY);
                    if (result) {
                        placement.instantiatePiece(result);
                        placed++;
                    } else {
                        console.warn(`Could not place ${piece.modelId} at (${piece.gridX}, ${piece.gridZ}), skipping`);
                        skipped++;
                    }
                }

                resolve({ placed, skipped, trackName: layout.trackName });
            } catch (err) {
                reject(err);
            }
        };
        reader.onerror = () => reject(new Error('Failed to read file'));
        reader.readAsText(file);
    });
}
