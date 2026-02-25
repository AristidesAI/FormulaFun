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

        // State
        this.selectedPieceDef = null;   // piece definition for placement mode
        this.currentRotation = 0;       // 0, 90, 180, 270
        this.ghostMesh = null;          // transparent preview
        this.ghostValid = false;
        this.ghostGridX = 0;
        this.ghostGridZ = 0;

        this.placedMeshes = new Map();  // pieceId -> THREE.Group
        this.selectedPlacedId = null;   // id of selected placed piece

        // Raycasting
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        this.groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);

        // Highlight indicator
        this.highlightMeshes = [];

        this._onMouseMove = this._onMouseMove.bind(this);
        this._onClick = this._onClick.bind(this);
        this._onContextMenu = this._onContextMenu.bind(this);

        canvas.addEventListener('mousemove', this._onMouseMove);
        canvas.addEventListener('click', this._onClick);
        canvas.addEventListener('contextmenu', this._onContextMenu);
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
            this._updateGhostValidity();
        }

        if (this.selectedPlacedId !== null) {
            this._rotatePlaced(this.selectedPlacedId, delta);
        }
    }

    deletePlaced(pieceId) {
        const mesh = this.placedMeshes.get(pieceId);
        if (mesh) {
            this.scene.remove(mesh);
            this.placedMeshes.delete(pieceId);
        }
        this.grid.remove(pieceId);
        if (this.selectedPlacedId === pieceId) {
            this.selectedPlacedId = null;
            this._clearHighlights();
        }
    }

    cancel() {
        if (this.selectedPieceDef) {
            this.selectedPieceDef = null;
            this._removeGhost();
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
        this._clearHighlights();
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
            this._showCellHighlights(placed.cells, 0x4488ff);
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
            this._clearHighlights();
        }
    }

    instantiatePiece(placed) {
        const model = getClone(placed.modelId);
        if (!model) return;
        model.position.set(placed.anchorX, 0, placed.anchorZ);
        model.rotation.y = THREE.MathUtils.degToRad(-placed.rotation);
        model.userData.pieceId = placed.id;
        model.traverse(child => {
            child.userData.pieceId = placed.id;
        });
        this.scene.add(model);
        this.placedMeshes.set(placed.id, model);
    }

    // --- Private ---

    _createGhost() {
        this._removeGhost();
        if (!this.selectedPieceDef) return;

        const model = getClone(this.selectedPieceDef.modelId);
        if (!model) return;

        model.traverse(child => {
            if (child.isMesh) {
                child.material = child.material.clone();
                child.material.transparent = true;
                child.material.opacity = 0.5;
                child.material.depthWrite = false;
            }
        });
        model.rotation.y = THREE.MathUtils.degToRad(-this.currentRotation);
        model.renderOrder = 999;
        this.ghostMesh = model;
        this.scene.add(model);
    }

    _removeGhost() {
        if (this.ghostMesh) {
            this.scene.remove(this.ghostMesh);
            this.ghostMesh = null;
        }
    }

    _setGhostColor(valid) {
        this.ghostValid = valid;
        if (!this.ghostMesh) return;
        const color = valid ? new THREE.Color(0x00ff00) : new THREE.Color(0xff4444);
        this.ghostMesh.traverse(child => {
            if (child.isMesh) {
                child.material.emissive = color;
                child.material.emissiveIntensity = 0.3;
            }
        });
    }

    _updateGhostValidity() {
        if (!this.ghostMesh || !this.selectedPieceDef) return;
        const valid = this.grid.canPlace(
            this.selectedPieceDef, this.ghostGridX, this.ghostGridZ, this.currentRotation
        );
        this._setGhostColor(valid);
    }

    _onMouseMove(event) {
        if (!this.ghostMesh || !this.selectedPieceDef) return;

        const rect = this.canvas.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersection = new THREE.Vector3();
        const hit = this.raycaster.ray.intersectPlane(this.groundPlane, intersection);
        if (!hit) return;

        const gx = Math.round(intersection.x);
        const gz = Math.round(intersection.z);
        this.ghostGridX = gx;
        this.ghostGridZ = gz;

        this.ghostMesh.position.set(gx, 0, gz);
        this._updateGhostValidity();
    }

    _onClick(event) {
        if (event.button !== 0) return;

        if (this.selectedPieceDef && this.ghostMesh) {
            // Placement mode
            if (!this.ghostValid) return;

            const placed = this.grid.place(
                this.selectedPieceDef, this.ghostGridX, this.ghostGridZ, this.currentRotation
            );
            if (placed) {
                this.instantiatePiece(placed);
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
            group.traverse(child => {
                if (child.isMesh) allMeshes.push(child);
            });
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

    _onContextMenu(event) {
        event.preventDefault();
    }

    _rotatePlaced(pieceId, delta) {
        const placed = this.grid.pieces.get(pieceId);
        if (!placed) return;

        const def = getPieceDef(placed.modelId);
        if (!def) return;

        const newRotation = ((placed.rotation + delta) % 360 + 360) % 360;

        // Remove from grid temporarily
        this.grid.remove(pieceId);

        if (this.grid.canPlace(def, placed.anchorX, placed.anchorZ, newRotation)) {
            const newPlaced = this.grid.place(def, placed.anchorX, placed.anchorZ, newRotation);
            if (newPlaced) {
                // Remove old mesh
                const mesh = this.placedMeshes.get(pieceId);
                if (mesh) {
                    this.scene.remove(mesh);
                    this.placedMeshes.delete(pieceId);
                }
                // Create new mesh
                this.instantiatePiece(newPlaced);
                this.selectedPlacedId = newPlaced.id;
                // Highlight with selection
                this.selectPlaced(newPlaced.id);
                this.currentRotation = newRotation;
            }
        } else {
            // Can't rotate, restore original placement
            const restored = this.grid.place(def, placed.anchorX, placed.anchorZ, placed.rotation);
            if (restored) {
                // Remap mesh to the restored id if it changed
                const mesh = this.placedMeshes.get(pieceId);
                if (mesh && restored.id !== pieceId) {
                    this.placedMeshes.delete(pieceId);
                    mesh.userData.pieceId = restored.id;
                    mesh.traverse(child => { child.userData.pieceId = restored.id; });
                    this.placedMeshes.set(restored.id, mesh);
                    this.selectedPlacedId = restored.id;
                }
            }
        }
    }

    _showCellHighlights(cells, color) {
        this._clearHighlights();
        const mat = new THREE.MeshBasicMaterial({
            color,
            transparent: true,
            opacity: 0.15,
            depthWrite: false,
        });
        for (const [x, z] of cells) {
            const geo = new THREE.PlaneGeometry(1, 1);
            const mesh = new THREE.Mesh(geo, mat);
            mesh.rotation.x = -Math.PI / 2;
            mesh.position.set(x, 0.02, z);
            this.scene.add(mesh);
            this.highlightMeshes.push(mesh);
        }
    }

    _clearHighlights() {
        for (const m of this.highlightMeshes) {
            this.scene.remove(m);
        }
        this.highlightMeshes = [];
    }
}
