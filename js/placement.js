import * as THREE from 'three';
import { getClone } from './modelLoader.js';
import { getPieceDef } from './trackPieces.js';

export class PlacementController {
    constructor(scene, camera, canvas, gridSystem, controls) {
        this.scene = scene;
        this.camera = camera;
        this.canvas = canvas;
        this.grid = gridSystem;
        this.controls = controls;

        // Placement state
        this.selectedPieceDef = null;
        this.currentRotation = 0;
        this.ghostMesh = null;
        this.ghostValid = false;
        this.ghostGridX = -1;
        this.ghostGridZ = -1;

        // Placed pieces
        this.placedMeshes = new Map();
        this.selectedPlacedId = null;

        // Action callback for undo/redo (set by main.js)
        this.onAction = null;

        // Raycasting
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        this.groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);

        // Grid cell hover highlights and selection highlights
        this.hoverHighlights = [];
        this.selectionHighlights = [];

        // Pointer tracking: distinguish click from drag
        this._pointerDownPos = null;
        this._pointerDownTime = 0;

        this.canvas.addEventListener('pointermove', (e) => this._onPointerMove(e));
        this.canvas.addEventListener('pointerdown', (e) => this._onPointerDown(e));
        this.canvas.addEventListener('pointerup', (e) => this._onPointerUp(e));
        this.canvas.addEventListener('contextmenu', (e) => e.preventDefault());
    }

    // --- Public API ---

    selectPieceForPlacement(pieceDef) {
        this.deselectPlaced();
        this.selectedPieceDef = pieceDef;
        this.currentRotation = 0;
        this._createGhost();
    }

    rotate(delta) {
        this.currentRotation = ((this.currentRotation + delta) % 360 + 360) % 360;

        if (this.ghostMesh) {
            this.ghostMesh.rotation.y = THREE.MathUtils.degToRad(-this.currentRotation);
            this._updateGhostPosition();
            this._updateHover();
        }

        if (this.selectedPlacedId !== null) {
            this._rotatePlaced(this.selectedPlacedId, delta);
        }
    }

    deletePlaced(pieceId) {
        const placed = this.grid.pieces.get(pieceId);
        const mesh = this.placedMeshes.get(pieceId);
        if (mesh) {
            this.scene.remove(mesh);
            this.placedMeshes.delete(pieceId);
        }
        this.grid.remove(pieceId);
        if (this.selectedPlacedId === pieceId) {
            this.selectedPlacedId = null;
            this._clearHighlights(this.selectionHighlights);
        }
        return placed; // for undo
    }

    cancel() {
        if (this.selectedPieceDef) {
            this.selectedPieceDef = null;
            this._removeGhost();
            this._clearHighlights(this.hoverHighlights);
            document.querySelectorAll('.piece-item.selected').forEach(el => el.classList.remove('selected'));
        } else {
            this.deselectPlaced();
        }
    }

    clearAll() {
        for (const [id, mesh] of this.placedMeshes) {
            this.scene.remove(mesh);
        }
        this.placedMeshes.clear();
        this.grid.clear();
        this.selectedPlacedId = null;
        this._removeGhost();
        this._clearHighlights(this.hoverHighlights);
        this._clearHighlights(this.selectionHighlights);
    }

    selectPlaced(pieceId) {
        this.deselectPlaced();
        this.selectedPlacedId = pieceId;
        const mesh = this.placedMeshes.get(pieceId);
        if (mesh) {
            mesh.traverse(child => {
                if (child.isMesh) {
                    child.userData._origEmissive = child.material.emissive ? child.material.emissive.clone() : new THREE.Color(0);
                    child.userData._origEmissiveIntensity = child.material.emissiveIntensity || 0;
                    child.material = child.material.clone();
                    child.material.emissive = new THREE.Color(0x4488ff);
                    child.material.emissiveIntensity = 0.4;
                }
            });
        }
        const placed = this.grid.pieces.get(pieceId);
        if (placed) {
            this._showCellHighlights(placed.cells, 0x4488ff, this.selectionHighlights);
            this.currentRotation = placed.rotation;
        }
    }

    deselectPlaced() {
        if (this.selectedPlacedId !== null) {
            const mesh = this.placedMeshes.get(this.selectedPlacedId);
            if (mesh) {
                mesh.traverse(child => {
                    if (child.isMesh && child.userData._origEmissive) {
                        child.material.emissive = child.userData._origEmissive;
                        child.material.emissiveIntensity = child.userData._origEmissiveIntensity;
                    }
                });
            }
            this.selectedPlacedId = null;
            this._clearHighlights(this.selectionHighlights);
        }
    }

    instantiatePiece(placed) {
        const model = getClone(placed.modelId);
        if (!model) return;
        const def = getPieceDef(placed.modelId);
        if (!def) return;
        // Position model at the center of its occupied cells
        const center = this.grid.getCellCenter(def, placed.anchorX, placed.anchorZ, placed.rotation);
        model.position.set(center.x, 0, center.z);
        model.rotation.y = THREE.MathUtils.degToRad(-placed.rotation);
        model.userData.pieceId = placed.id;
        model.traverse(child => { child.userData.pieceId = placed.id; });
        this.scene.add(model);
        this.placedMeshes.set(placed.id, model);
    }

    // --- Private: pointer handling ---

    _onPointerMove(event) {
        const pos = this._getGridPos(event);
        if (!pos) return;

        if (this.selectedPieceDef) {
            this.ghostGridX = pos.x;
            this.ghostGridZ = pos.z;
            this._updateGhostPosition();
            this._updateHover();
        }
    }

    _onPointerDown(event) {
        if (event.button !== 0) return;
        this._pointerDownPos = { x: event.clientX, y: event.clientY };
        this._pointerDownTime = Date.now();
    }

    _onPointerUp(event) {
        if (event.button !== 0 || !this._pointerDownPos) return;
        const dx = event.clientX - this._pointerDownPos.x;
        const dy = event.clientY - this._pointerDownPos.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        const duration = Date.now() - this._pointerDownTime;
        this._pointerDownPos = null;

        // Only treat as a click if pointer barely moved and was quick
        if (dist > 5 || duration > 500) return;

        this._handleClick(event);
    }

    _handleClick(event) {
        if (this.selectedPieceDef) {
            // Placement mode
            if (!this.ghostValid) return;

            const placed = this.grid.place(
                this.selectedPieceDef, this.ghostGridX, this.ghostGridZ, this.currentRotation
            );
            if (placed) {
                this.instantiatePiece(placed);
                if (this.onAction) {
                    this.onAction({ type: 'place', placed: { ...placed } });
                }
            }
            return;
        }

        // Selection mode: raycast against placed meshes
        const rect = this.canvas.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        this.raycaster.setFromCamera(this.mouse, this.camera);

        const allMeshes = [];
        for (const group of this.placedMeshes.values()) {
            group.traverse(child => { if (child.isMesh) allMeshes.push(child); });
        }

        const hits = this.raycaster.intersectObjects(allMeshes, false);
        if (hits.length > 0) {
            const pieceId = hits[0].object.userData.pieceId;
            if (pieceId !== undefined) {
                this.selectPlaced(pieceId);
                return;
            }
        }
        this.deselectPlaced();
    }

    _getGridPos(event) {
        const rect = this.canvas.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const pt = new THREE.Vector3();
        const hit = this.raycaster.ray.intersectPlane(this.groundPlane, pt);
        if (!hit) return null;
        // Use Math.floor to get the correct cell index
        // Cell (x, z) spans from world (x, z) to (x+1, z+1)
        return { x: Math.floor(pt.x), z: Math.floor(pt.z) };
    }

    // --- Private: ghost + hover highlights ---

    _updateGhostPosition() {
        if (!this.ghostMesh || !this.selectedPieceDef) return;
        // Position ghost at the center of the prospective occupied cells
        const center = this.grid.getCellCenter(
            this.selectedPieceDef, this.ghostGridX, this.ghostGridZ, this.currentRotation
        );
        this.ghostMesh.position.set(center.x, 0, center.z);
    }

    _createGhost() {
        this._removeGhost();
        if (!this.selectedPieceDef) return;

        const model = getClone(this.selectedPieceDef.modelId);
        if (!model) return;

        model.traverse(child => {
            if (child.isMesh) {
                child.material = child.material.clone();
                child.material.transparent = true;
                child.material.opacity = 0.6;
                child.material.depthWrite = false;
            }
        });
        model.rotation.y = THREE.MathUtils.degToRad(-this.currentRotation);
        model.renderOrder = 999;
        model.visible = false; // hidden until mouse enters grid
        this.ghostMesh = model;
        this.scene.add(model);
    }

    _removeGhost() {
        if (this.ghostMesh) {
            this.scene.remove(this.ghostMesh);
            this.ghostMesh = null;
        }
    }

    _updateHover() {
        if (!this.selectedPieceDef) {
            this._clearHighlights(this.hoverHighlights);
            return;
        }

        const gx = this.ghostGridX;
        const gz = this.ghostGridZ;
        const valid = this.grid.canPlace(this.selectedPieceDef, gx, gz, this.currentRotation);
        this.ghostValid = valid;

        // Update ghost appearance
        if (this.ghostMesh) {
            this.ghostMesh.visible = true;
            const color = valid ? new THREE.Color(0x00ff00) : new THREE.Color(0xff4444);
            this.ghostMesh.traverse(child => {
                if (child.isMesh) {
                    child.material.emissive = color;
                    child.material.emissiveIntensity = 0.3;
                }
            });
        }

        // Show grid cell highlights under the piece
        const cells = this.grid.getOccupiedCells(this.selectedPieceDef, gx, gz, this.currentRotation);
        const highlightColor = valid ? 0xffffff : 0xff4444;
        this._showCellHighlights(cells, highlightColor, this.hoverHighlights);
    }

    _rotatePlaced(pieceId, delta) {
        const placed = this.grid.pieces.get(pieceId);
        if (!placed) return;
        const def = getPieceDef(placed.modelId);
        if (!def) return;

        const newRotation = ((placed.rotation + delta) % 360 + 360) % 360;
        this.grid.remove(pieceId);

        if (this.grid.canPlace(def, placed.anchorX, placed.anchorZ, newRotation)) {
            const newPlaced = this.grid.place(def, placed.anchorX, placed.anchorZ, newRotation);
            if (newPlaced) {
                const mesh = this.placedMeshes.get(pieceId);
                if (mesh) { this.scene.remove(mesh); this.placedMeshes.delete(pieceId); }
                this.instantiatePiece(newPlaced);
                this.selectedPlacedId = newPlaced.id;
                this.selectPlaced(newPlaced.id);
                this.currentRotation = newRotation;
            }
        } else {
            this.grid.place(def, placed.anchorX, placed.anchorZ, placed.rotation);
        }
    }

    // --- Private: cell highlights ---

    _showCellHighlights(cells, color, arr) {
        this._clearHighlights(arr);
        const fillMat = new THREE.MeshBasicMaterial({
            color, transparent: true, opacity: 0.18, depthWrite: false, side: THREE.DoubleSide,
        });
        const edgeMat = new THREE.LineBasicMaterial({ color, transparent: true, opacity: 0.5 });

        for (const [x, z] of cells) {
            // Center highlights at the cell center (x+0.5, z+0.5)
            const cx = x + 0.5;
            const cz = z + 0.5;

            const quad = new THREE.Mesh(new THREE.PlaneGeometry(0.96, 0.96), fillMat);
            quad.rotation.x = -Math.PI / 2;
            quad.position.set(cx, 0.015, cz);
            this.scene.add(quad);
            arr.push(quad);

            const pts = [
                new THREE.Vector3(-0.48, 0, -0.48), new THREE.Vector3(0.48, 0, -0.48),
                new THREE.Vector3(0.48, 0, 0.48), new THREE.Vector3(-0.48, 0, 0.48),
                new THREE.Vector3(-0.48, 0, -0.48),
            ];
            const edge = new THREE.Line(new THREE.BufferGeometry().setFromPoints(pts), edgeMat);
            edge.position.set(cx, 0.025, cz);
            this.scene.add(edge);
            arr.push(edge);
        }
    }

    _clearHighlights(arr) {
        for (const m of arr) this.scene.remove(m);
        arr.length = 0;
    }
}
