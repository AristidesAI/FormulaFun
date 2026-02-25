import { createScene, createGrid, resizeRenderer } from './scene.js';
import { preloadAll } from './modelLoader.js';
import { TRACK_PIECES } from './trackPieces.js';
import { GridSystem } from './gridSystem.js';
import { PlacementController } from './placement.js';
import { exportTrackJSON, importTrackJSON } from './exportImport.js';
import { buildPalette, bindToolbar, setStatus, hideLoading, updateLoadingProgress } from './ui.js';

async function init() {
    const canvas = document.getElementById('three-canvas');
    const viewport = document.getElementById('viewport');

    // Scene setup
    const { renderer, scene, camera, controls } = createScene(canvas);
    const gridSize = 30;
    createGrid(scene, gridSize);

    // Grid system
    const gridSystem = new GridSystem(gridSize);

    // Preload all models
    await preloadAll(TRACK_PIECES, (loaded, total) => {
        updateLoadingProgress(loaded, total);
    });
    hideLoading();
    setStatus('Select a piece from the sidebar to start building');

    // Placement controller
    const placement = new PlacementController(scene, camera, canvas, gridSystem, controls);

    // Build sidebar palette
    buildPalette(TRACK_PIECES, (pieceDef) => {
        placement.selectPieceForPlacement(pieceDef);
        setStatus(`Placing: ${pieceDef.name} (${pieceDef.gridW}x${pieceDef.gridD}) - Click to place, R to rotate`);
    });

    // Toolbar bindings
    bindToolbar({
        onNew: () => {
            placement.clearAll();
            setStatus('Cleared. Select a piece to start building');
        },
        onExport: () => {
            const name = document.getElementById('track-name').value;
            const layout = exportTrackJSON(gridSystem, name);
            setStatus(`Exported "${layout.trackName}" with ${layout.metadata.totalPieces} pieces`);
        },
        onImport: async (file) => {
            try {
                const result = await importTrackJSON(file, gridSystem, placement);
                setStatus(`Imported "${result.trackName}": ${result.placed} pieces placed, ${result.skipped} skipped`);
            } catch (err) {
                setStatus(`Import failed: ${err.message}`);
                console.error('Import error:', err);
            }
        },
        onRotateCW: () => placement.rotate(90),
        onRotateCCW: () => placement.rotate(-90),
        onDelete: () => {
            if (placement.selectedPlacedId !== null) {
                placement.deletePlaced(placement.selectedPlacedId);
                setStatus('Piece deleted');
            }
        },
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        // Don't capture shortcuts when typing in input fields
        if (e.target.tagName === 'INPUT') return;

        switch (e.key) {
            case 'r':
                if (!e.shiftKey && !e.ctrlKey && !e.metaKey) {
                    placement.rotate(90);
                }
                break;
            case 'R':
                if (e.shiftKey) {
                    placement.rotate(-90);
                }
                break;
            case 'Delete':
            case 'Backspace':
                if (placement.selectedPlacedId !== null) {
                    placement.deletePlaced(placement.selectedPlacedId);
                    setStatus('Piece deleted');
                }
                break;
            case 'Escape':
                placement.cancel();
                setStatus('Select a piece from the sidebar to start building');
                break;
            case 'g':
                if (!e.ctrlKey && !e.metaKey) {
                    const grid = scene.getObjectByName('gridHelper');
                    if (grid) grid.visible = !grid.visible;
                }
                break;
        }

        // Ctrl/Cmd+S to export
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            const name = document.getElementById('track-name').value;
            const layout = exportTrackJSON(gridSystem, name);
            setStatus(`Exported "${layout.trackName}" with ${layout.metadata.totalPieces} pieces`);
        }
    });

    // Render loop
    function animate() {
        requestAnimationFrame(animate);
        controls.update();
        resizeRenderer(renderer, camera, viewport);
        renderer.render(scene, camera);
    }
    animate();
}

init().catch(err => {
    console.error('Initialization failed:', err);
    document.getElementById('loading-text').textContent = 'Failed to initialize';
    document.getElementById('loading-progress').textContent = err.message;
});
