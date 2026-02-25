import * as THREE from 'three';
import { createScene, createGrid, resizeRenderer } from './scene.js';
import { preloadAll, registerCustomModel } from './modelLoader.js';
import { TRACK_PIECES } from './trackPieces.js';
import { GridSystem } from './gridSystem.js';
import { PlacementController } from './placement.js';
import { History } from './history.js';
import { exportTrackJSON, importTrackJSON } from './exportImport.js';
import { buildPalette, bindToolbar, setStatus, hideLoading, updateLoadingProgress } from './ui.js';

function createCheckpointModels() {
    // Start/Finish: green+yellow checkered pole
    const startGroup = new THREE.Group();
    const poleGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.8, 8);
    const poleMat = new THREE.MeshStandardMaterial({ color: 0x333333 });
    const pole = new THREE.Mesh(poleGeo, poleMat);
    pole.position.y = 0.4;
    pole.castShadow = true;
    startGroup.add(pole);

    const flagGeo = new THREE.PlaneGeometry(0.35, 0.25);
    const flagMat = new THREE.MeshStandardMaterial({
        color: 0x00cc44, emissive: 0x00cc44, emissiveIntensity: 0.3, side: THREE.DoubleSide,
    });
    const flag = new THREE.Mesh(flagGeo, flagMat);
    flag.position.set(0.18, 0.7, 0);
    flag.castShadow = true;
    startGroup.add(flag);

    const baseGeo = new THREE.CylinderGeometry(0.15, 0.18, 0.05, 12);
    const baseMat = new THREE.MeshStandardMaterial({ color: 0x00cc44, emissive: 0x00aa33, emissiveIntensity: 0.2 });
    const base = new THREE.Mesh(baseGeo, baseMat);
    base.position.y = 0.025;
    base.castShadow = true;
    startGroup.add(base);

    registerCustomModel('_checkpointStart', startGroup);

    // Waypoint: blue marker cone
    const wpGroup = new THREE.Group();
    const coneGeo = new THREE.ConeGeometry(0.12, 0.5, 8);
    const coneMat = new THREE.MeshStandardMaterial({
        color: 0x4488ff, emissive: 0x4488ff, emissiveIntensity: 0.3,
    });
    const cone = new THREE.Mesh(coneGeo, coneMat);
    cone.position.y = 0.25;
    cone.castShadow = true;
    wpGroup.add(cone);

    const wpBase = new THREE.Mesh(baseGeo.clone(), new THREE.MeshStandardMaterial({
        color: 0x4488ff, emissive: 0x3366cc, emissiveIntensity: 0.2,
    }));
    wpBase.position.y = 0.025;
    wpBase.castShadow = true;
    wpGroup.add(wpBase);

    registerCustomModel('_checkpointWaypoint', wpGroup);
}

async function init() {
    const canvas = document.getElementById('three-canvas');
    const viewport = document.getElementById('viewport');

    const { renderer, scene, camera, controls } = createScene(canvas);
    const gridSize = 30;
    createGrid(scene, gridSize);

    const gridSystem = new GridSystem(gridSize);

    // Preload GLB models
    await preloadAll(TRACK_PIECES, (loaded, total) => {
        updateLoadingProgress(loaded, total);
    });

    // Create programmatic checkpoint models
    createCheckpointModels();

    hideLoading();
    setStatus('Select a piece from the sidebar to start building');

    const placement = new PlacementController(scene, camera, canvas, gridSystem, controls);
    const history = new History(gridSystem, placement);

    // Wire placement actions into history
    placement.onAction = (action) => {
        history.push(action);
    };

    // Build sidebar with thumbnails
    buildPalette(TRACK_PIECES, (pieceDef) => {
        placement.selectPieceForPlacement(pieceDef);
        setStatus(`Placing: ${pieceDef.name} (${pieceDef.gridW}x${pieceDef.gridD}) - Click to place, R to rotate`);
    });

    bindToolbar({
        onNew: () => {
            placement.clearAll();
            history.clear();
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
                history.clear();
                setStatus(`Imported "${result.trackName}": ${result.placed} placed, ${result.skipped} skipped`);
            } catch (err) {
                setStatus(`Import failed: ${err.message}`);
            }
        },
        onRotateCW: () => placement.rotate(90),
        onRotateCCW: () => placement.rotate(-90),
        onDelete: () => {
            if (placement.selectedPlacedId !== null) {
                const deleted = placement.deletePlaced(placement.selectedPlacedId);
                if (deleted) history.push({ type: 'delete', placed: { ...deleted } });
                setStatus('Piece deleted');
            }
        },
        onUndo: () => {
            history.undo();
            setStatus('Undone');
        },
        onRedo: () => {
            history.redo();
            setStatus('Redone');
        },
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', (e) => {
        if (e.target.tagName === 'INPUT') return;

        // Undo: Cmd/Ctrl+Z (without Shift)
        if ((e.metaKey || e.ctrlKey) && e.key === 'z' && !e.shiftKey) {
            e.preventDefault();
            history.undo();
            setStatus('Undone');
            return;
        }

        // Redo: Cmd/Ctrl+Shift+Z
        if ((e.metaKey || e.ctrlKey) && e.key === 'z' && e.shiftKey) {
            e.preventDefault();
            history.redo();
            setStatus('Redone');
            return;
        }
        if ((e.metaKey || e.ctrlKey) && e.key === 'Z') {
            e.preventDefault();
            history.redo();
            setStatus('Redone');
            return;
        }

        // Export: Cmd/Ctrl+S
        if ((e.metaKey || e.ctrlKey) && e.key === 's') {
            e.preventDefault();
            const name = document.getElementById('track-name').value;
            const layout = exportTrackJSON(gridSystem, name);
            setStatus(`Exported "${layout.trackName}" with ${layout.metadata.totalPieces} pieces`);
            return;
        }

        switch (e.key) {
            case 'r':
                if (!e.shiftKey && !e.ctrlKey && !e.metaKey) placement.rotate(90);
                break;
            case 'R':
                if (e.shiftKey) placement.rotate(-90);
                break;
            case 'Delete':
            case 'Backspace':
                if (placement.selectedPlacedId !== null) {
                    const deleted = placement.deletePlaced(placement.selectedPlacedId);
                    if (deleted) history.push({ type: 'delete', placed: { ...deleted } });
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
