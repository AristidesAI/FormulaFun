import { getPieceDef } from './trackPieces.js';

export class History {
    constructor(gridSystem, placement) {
        this.grid = gridSystem;
        this.placement = placement;
        this.undoStack = [];
        this.redoStack = [];
    }

    push(action) {
        this.undoStack.push(action);
        this.redoStack.length = 0; // clear redo on new action
    }

    canUndo() { return this.undoStack.length > 0; }
    canRedo() { return this.redoStack.length > 0; }

    undo() {
        if (!this.canUndo()) return;
        const action = this.undoStack.pop();

        if (action.type === 'place') {
            // Undo a placement = delete the piece
            const placed = action.placed;
            this.placement.deselectPlaced();
            this.placement.deletePlaced(placed.id);
            this.redoStack.push(action);
        } else if (action.type === 'delete') {
            // Undo a deletion = re-place the piece
            const data = action.placed;
            const def = getPieceDef(data.modelId);
            if (def) {
                const restored = this.grid.place(def, data.anchorX, data.anchorZ, data.rotation);
                if (restored) {
                    this.placement.instantiatePiece(restored);
                    // Update the action's id so redo can find it
                    this.redoStack.push({ type: 'delete', placed: { ...restored } });
                }
            }
        }
    }

    redo() {
        if (!this.canRedo()) return;
        const action = this.redoStack.pop();

        if (action.type === 'place') {
            // Redo a placement = re-place
            const data = action.placed;
            const def = getPieceDef(data.modelId);
            if (def) {
                const restored = this.grid.place(def, data.anchorX, data.anchorZ, data.rotation);
                if (restored) {
                    this.placement.instantiatePiece(restored);
                    this.undoStack.push({ type: 'place', placed: { ...restored } });
                }
            }
        } else if (action.type === 'delete') {
            // Redo a deletion = delete again
            const data = action.placed;
            this.placement.deselectPlaced();
            this.placement.deletePlaced(data.id);
            this.undoStack.push({ type: 'delete', placed: data });
        }
    }

    clear() {
        this.undoStack.length = 0;
        this.redoStack.length = 0;
    }
}
