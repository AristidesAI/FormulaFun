export class GridSystem {
    constructor(size = 30) {
        this.size = size;
        // occupancy[x][z] = pieceId or null
        this.occupancy = Array.from({ length: size }, () => Array(size).fill(null));
        this.pieces = new Map(); // pieceId -> PlacedPiece
        this.nextId = 1;
    }

    /**
     * Compute which grid cells a piece occupies given anchor position and rotation.
     * Kenney models anchor at (X=0, Z=0) and extend into -X and +Z.
     * At rotation 0: dx in [-(W-1)..0], dz in [0..(D-1)]
     */
    getOccupiedCells(pieceDef, anchorX, anchorZ, rotation) {
        const w = pieceDef.gridW;
        const d = pieceDef.gridD;
        const cells = [];

        let dxMin, dxMax, dzMin, dzMax;
        const rot = ((rotation % 360) + 360) % 360;

        switch (rot) {
            case 0:
                dxMin = -(w - 1); dxMax = 0;
                dzMin = 0;        dzMax = d - 1;
                break;
            case 90:
                dxMin = 0;        dxMax = d - 1;
                dzMin = 0;        dzMax = w - 1;
                break;
            case 180:
                dxMin = 0;        dxMax = w - 1;
                dzMin = -(d - 1); dzMax = 0;
                break;
            case 270:
                dxMin = -(d - 1); dxMax = 0;
                dzMin = -(w - 1); dzMax = 0;
                break;
            default:
                dxMin = -(w - 1); dxMax = 0;
                dzMin = 0;        dzMax = d - 1;
        }

        for (let dx = dxMin; dx <= dxMax; dx++) {
            for (let dz = dzMin; dz <= dzMax; dz++) {
                cells.push([anchorX + dx, anchorZ + dz]);
            }
        }
        return cells;
    }

    canPlace(pieceDef, anchorX, anchorZ, rotation) {
        const cells = this.getOccupiedCells(pieceDef, anchorX, anchorZ, rotation);
        for (const [x, z] of cells) {
            if (x < 0 || x >= this.size || z < 0 || z >= this.size) return false;
            if (this.occupancy[x][z] !== null) return false;
        }
        return true;
    }

    place(pieceDef, anchorX, anchorZ, rotation) {
        if (!this.canPlace(pieceDef, anchorX, anchorZ, rotation)) return null;

        const id = this.nextId++;
        const cells = this.getOccupiedCells(pieceDef, anchorX, anchorZ, rotation);

        for (const [x, z] of cells) {
            this.occupancy[x][z] = id;
        }

        const placed = {
            id,
            modelId: pieceDef.modelId,
            anchorX,
            anchorZ,
            rotation: ((rotation % 360) + 360) % 360,
            cells,
        };
        this.pieces.set(id, placed);
        return placed;
    }

    remove(pieceId) {
        const piece = this.pieces.get(pieceId);
        if (!piece) return false;
        for (const [x, z] of piece.cells) {
            if (x >= 0 && x < this.size && z >= 0 && z < this.size) {
                this.occupancy[x][z] = null;
            }
        }
        this.pieces.delete(pieceId);
        return true;
    }

    getPieceAt(x, z) {
        if (x < 0 || x >= this.size || z < 0 || z >= this.size) return null;
        const id = this.occupancy[x][z];
        return id !== null ? this.pieces.get(id) : null;
    }

    clear() {
        for (let x = 0; x < this.size; x++) {
            for (let z = 0; z < this.size; z++) {
                this.occupancy[x][z] = null;
            }
        }
        this.pieces.clear();
        this.nextId = 1;
    }
}
