using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural closed-loop track generator using grid-based placement.
/// Mirrors the website builder grid system (js/gridSystem.js):
///   • Each cell = cellSize × cellSize world units
///   • Pieces occupy gridW × gridD cells
///   • Occupancy grid prevents overlapping
///
/// Algorithm: turtle-graphics walk on a grid.
///   1. Place start piece at grid center
///   2. Advance cursor, pick random straights and corners
///   3. After enough pieces, steer back to start and close the loop
///   4. Backtrack if stuck
///
/// Each placed piece automatically receives a Checkpoint trigger
/// for ML-Agents training rewards.
/// </summary>
public class TrackGenerator : MonoBehaviour
{
    // ─── Direction helpers ──────────────────────────────────────
    // 0=North(+Z), 1=East(+X), 2=South(-Z), 3=West(-X)
    public enum Dir { North = 0, East = 1, South = 2, West = 3 }

    static Dir RotateCW(Dir d) => (Dir)(((int)d + 1) % 4);
    static Dir RotateCCW(Dir d) => (Dir)(((int)d + 3) % 4);
    static Dir Opposite(Dir d) => (Dir)(((int)d + 2) % 4);

    /// <summary>Rotate a grid-relative offset (dx,dz) by heading.</summary>
    static Vector2Int RotateOffset(Vector2Int v, Dir heading)
    {
        return heading switch
        {
            Dir.North => v,                           // identity
            Dir.East  => new Vector2Int(v.y, -v.x),   // 90° CW
            Dir.South => new Vector2Int(-v.x, -v.y),  // 180°
            Dir.West  => new Vector2Int(-v.y, v.x),   // 270° CW
            _ => v,
        };
    }

    static Vector3 DirToWorldForward(Dir d)
    {
        return d switch
        {
            Dir.North => Vector3.forward,  // +Z
            Dir.East  => Vector3.right,    // +X
            Dir.South => Vector3.back,     // -Z
            Dir.West  => Vector3.left,     // -X
            _ => Vector3.forward,
        };
    }

    static float DirToYRotation(Dir d)
    {
        return d switch
        {
            Dir.North => 0f,
            Dir.East  => 90f,
            Dir.South => 180f,
            Dir.West  => 270f,
            _ => 0f,
        };
    }

    // ─── Generation piece templates ─────────────────────────────
    // Defines the shape of each connectable piece in local space (heading = North).
    // localCells: cells occupied relative to entry cell (0,0)
    // exitOffset: where the cursor moves to AFTER the piece
    // exitDir:    new cursor heading after the piece

    enum GenPiece
    {
        Straight1x1,
        Straight1x2,
        CornerSmallRight,
        CornerSmallLeft,
        CornerLargeRight,
        CornerLargeLeft,
        CornerLargerRight,
        CornerLargerLeft,
    }

    readonly struct GenPieceDef
    {
        public readonly GenPiece piece;
        public readonly Vector2Int[] localCells;   // relative to entry at (0,0), heading North
        public readonly Vector2Int exitOffset;      // cursor position after piece (heading North)
        public readonly int turnSteps;              // 0=straight, +1=right90°, -1=left90°
        public readonly TrackPieceDatabase.ConnShape connShape;

        public GenPieceDef(GenPiece piece, Vector2Int[] cells, Vector2Int exit, int turn, TrackPieceDatabase.ConnShape conn)
        {
            this.piece = piece;
            localCells = cells;
            exitOffset = exit;
            turnSteps = turn;
            connShape = conn;
        }
    }

    static readonly GenPieceDef[] GenPieces = new GenPieceDef[]
    {
        // Straight 1×1: occupies (0,0), exits at (0,1)
        new(GenPiece.Straight1x1,
            new[] { V(0,0) }, V(0,1), 0,
            TrackPieceDatabase.ConnShape.Straight1x1),

        // Straight 1×2: occupies (0,0),(0,1), exits at (0,2)
        new(GenPiece.Straight1x2,
            new[] { V(0,0), V(0,1) }, V(0,2), 0,
            TrackPieceDatabase.ConnShape.Straight1x2),

        // Corner Small Right: occupies (0,0), exits at (1,0) heading East
        new(GenPiece.CornerSmallRight,
            new[] { V(0,0) }, V(1,0), 1,
            TrackPieceDatabase.ConnShape.Corner1x1),

        // Corner Small Left: occupies (0,0), exits at (-1,0) heading West
        new(GenPiece.CornerSmallLeft,
            new[] { V(0,0) }, V(-1,0), -1,
            TrackPieceDatabase.ConnShape.Corner1x1),

        // Corner Large Right: 2×2, exits at (2,1) heading East
        new(GenPiece.CornerLargeRight,
            new[] { V(0,0), V(1,0), V(0,1), V(1,1) }, V(2,1), 1,
            TrackPieceDatabase.ConnShape.Corner2x2),

        // Corner Large Left: 2×2, exits at (-2,1) heading West
        new(GenPiece.CornerLargeLeft,
            new[] { V(0,0), V(-1,0), V(0,1), V(-1,1) }, V(-2,1), -1,
            TrackPieceDatabase.ConnShape.Corner2x2),

        // Corner Larger Right: 3×3, exits at (3,2) heading East
        new(GenPiece.CornerLargerRight,
            new[] { V(0,0),V(1,0),V(2,0), V(0,1),V(1,1),V(2,1), V(0,2),V(1,2),V(2,2) },
            V(3,2), 1,
            TrackPieceDatabase.ConnShape.Corner3x3),

        // Corner Larger Left: 3×3, exits at (-3,2) heading West
        new(GenPiece.CornerLargerLeft,
            new[] { V(0,0),V(-1,0),V(-2,0), V(0,1),V(-1,1),V(-2,1), V(0,2),V(-1,2),V(-2,2) },
            V(-3,2), -1,
            TrackPieceDatabase.ConnShape.Corner3x3),
    };

    static Vector2Int V(int x, int y) => new(x, y);

    // ─── Inspector ──────────────────────────────────────────────

    [Header("Grid Settings")]
    [SerializeField] int gridSize = 40;
    [SerializeField] float cellSize = 4f;

    [Header("Generation")]
    [SerializeField] int minPieces = 12;
    [SerializeField] int maxPieces = 24;
    [SerializeField] int seed = -1;
    [SerializeField] int maxRetries = 50;

    [Header("Piece Weights (higher = more likely)")]
    [SerializeField, Range(0f, 5f)] float weightStraight1x1 = 3f;
    [SerializeField, Range(0f, 5f)] float weightStraight1x2 = 2f;
    [SerializeField, Range(0f, 5f)] float weightCornerSmall = 2f;
    [SerializeField, Range(0f, 5f)] float weightCornerLarge = 1.5f;
    [SerializeField, Range(0f, 5f)] float weightCornerLarger = 0.5f;

    [Header("Checkpoint")]
    [SerializeField] float checkpointWidth = 8f;
    [SerializeField] float checkpointHeight = 4f;

    [Header("Prefab Loading")]
    [Tooltip("Path relative to Assets/ where FBX models live")]
    [SerializeField] string modelBasePath = "Racing Kit/Models/FBX format/";

    Transform trackParent;
    CheckpointManager checkpointManager;
    List<Transform> spawnPoints = new();
    int[,] occupancy; // -1 = empty, else piece index

    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
    public CheckpointManager CheckpointMgr => checkpointManager;

    // ─── Placed piece record ────────────────────────────────────
    struct PlacedPiece
    {
        public GenPieceDef def;
        public Vector2Int gridPos;   // entry cell in world grid
        public Dir heading;          // cursor heading when entering this piece
        public string modelId;
        public List<Vector2Int> occupiedCells;
    }

    // ─── Public API ─────────────────────────────────────────────

    public void GenerateTrack()
    {
        ClearTrack();

        Random.State savedState = Random.state;
        if (seed >= 0)
            Random.InitState(seed);
        else
            Random.InitState(System.Environment.TickCount);

        trackParent = new GameObject("GeneratedTrack").transform;
        trackParent.SetParent(transform);

        checkpointManager = trackParent.gameObject.AddComponent<CheckpointManager>();

        occupancy = new int[gridSize, gridSize];
        for (int x = 0; x < gridSize; x++)
            for (int z = 0; z < gridSize; z++)
                occupancy[x, z] = -1;

        int targetPieces = Random.Range(minPieces, maxPieces + 1);
        List<PlacedPiece> placed = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            placed = TryGenerateLoop(targetPieces);
            if (placed != null) break;
            ClearOccupancy();
        }

        if (placed == null)
        {
            Debug.LogWarning("TrackGenerator: Failed to generate a closed loop. Using fallback oval.");
            ClearOccupancy();
            placed = GenerateFallbackOval();
        }

        // Instantiate all pieces
        InstantiateTrack(placed);

        Random.state = savedState;
    }

    public void ClearTrack()
    {
        if (trackParent != null)
            Destroy(trackParent.gameObject);
        spawnPoints.Clear();
        checkpointManager = null;
    }

    // ─── Core generation: random walk with backtracking ─────────

    List<PlacedPiece> TryGenerateLoop(int targetPieces)
    {
        ClearOccupancy();

        Vector2Int startPos = new(gridSize / 2, gridSize / 2);
        Dir startHeading = Dir.North;

        var placed = new List<PlacedPiece>();
        Vector2Int cursor = startPos;
        Dir heading = startHeading;

        // Build the weights array
        float[] weights = BuildWeightArray();

        for (int i = 0; i < targetPieces; i++)
        {
            bool isLast = (i >= targetPieces - 4); // last few: try to close

            GenPieceDef? chosen = null;

            if (isLast)
            {
                // Try to steer back toward start
                chosen = TryPickClosingPiece(cursor, heading, startPos, startHeading, placed.Count, targetPieces);
            }

            if (chosen == null)
            {
                chosen = PickRandomPiece(cursor, heading, weights);
            }

            if (chosen == null)
            {
                // Dead end — backtrack
                if (placed.Count > 0)
                {
                    var last = placed[placed.Count - 1];
                    RemovePieceFromGrid(last);
                    placed.RemoveAt(placed.Count - 1);
                    // Recalculate cursor
                    if (placed.Count > 0)
                    {
                        RecalcCursor(placed, startPos, startHeading, out cursor, out heading);
                    }
                    else
                    {
                        cursor = startPos;
                        heading = startHeading;
                    }
                    i -= 2; // retry from before
                    if (i < -1) return null; // too many backtracks
                    continue;
                }
                return null;
            }

            var pp = PlacePiece(chosen.Value, cursor, heading, placed.Count);
            placed.Add(pp);

            // Advance cursor
            var exitOff = RotateOffset(chosen.Value.exitOffset, heading);
            cursor = pp.gridPos + exitOff;
            heading = ApplyTurn(heading, chosen.Value.turnSteps);
        }

        // Check if we closed the loop: cursor should be back at startPos heading startHeading
        // Allow some tolerance: cursor within 1 cell of start and heading matches
        if (cursor == startPos && heading == startHeading)
            return placed;

        // Try adding up to 4 more straight pieces to reach start
        for (int extra = 0; extra < 6; extra++)
        {
            if (cursor == startPos && heading == startHeading)
                return placed;

            // Try a corner to redirect, or a straight to advance
            var closePiece = TryPickClosingPiece(cursor, heading, startPos, startHeading, placed.Count, placed.Count + 6);
            if (closePiece == null)
                closePiece = PickRandomPiece(cursor, heading, weights);
            if (closePiece == null) break;

            var pp = PlacePiece(closePiece.Value, cursor, heading, placed.Count);
            placed.Add(pp);

            var exitOff = RotateOffset(closePiece.Value.exitOffset, heading);
            cursor = pp.gridPos + exitOff;
            heading = ApplyTurn(heading, closePiece.Value.turnSteps);
        }

        if (cursor == startPos && heading == startHeading)
            return placed;

        return null; // didn't close
    }

    GenPieceDef? TryPickClosingPiece(Vector2Int cursor, Dir heading, Vector2Int target, Dir targetHeading, int currentCount, int targetCount)
    {
        // Calculate vector from cursor to target in grid space
        Vector2Int delta = target - cursor;

        // What direction do we need to go?
        // Try pieces that steer us toward the target
        var candidates = new List<(GenPieceDef def, float priority)>();

        foreach (var gp in GenPieces)
        {
            if (!CanPlacePiece(gp, cursor, heading)) continue;

            var exitOff = RotateOffset(gp.exitOffset, heading);
            Vector2Int newCursor = cursor + exitOff;
            Dir newHeading = ApplyTurn(heading, gp.turnSteps);

            // How close does this get us to the target?
            float distBefore = Vector2Int.Distance(cursor, target);
            float distAfter = Vector2Int.Distance(newCursor, target);

            // Bonus for getting closer, bonus for correct heading
            float priority = (distBefore - distAfter) * 10f;
            if (newHeading == targetHeading) priority += 5f;
            if (newCursor == target && newHeading == targetHeading) priority += 100f;

            candidates.Add((gp, priority));
        }

        if (candidates.Count == 0) return null;

        // Sort by priority descending, pick best
        candidates.Sort((a, b) => b.priority.CompareTo(a.priority));

        // Pick from top candidates with some randomness
        int topN = Mathf.Min(3, candidates.Count);
        return candidates[Random.Range(0, topN)].def;
    }

    GenPieceDef? PickRandomPiece(Vector2Int cursor, Dir heading, float[] weights)
    {
        // Shuffle indices weighted
        var candidates = new List<(GenPieceDef def, float w)>();
        for (int i = 0; i < GenPieces.Length; i++)
        {
            if (weights[i] <= 0f) continue;
            if (!CanPlacePiece(GenPieces[i], cursor, heading)) continue;
            candidates.Add((GenPieces[i], weights[i]));
        }

        if (candidates.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var c in candidates) totalWeight += c.w;

        float r = Random.Range(0f, totalWeight);
        float acc = 0f;
        foreach (var c in candidates)
        {
            acc += c.w;
            if (r <= acc) return c.def;
        }

        return candidates[candidates.Count - 1].def;
    }

    float[] BuildWeightArray()
    {
        float[] w = new float[GenPieces.Length];
        for (int i = 0; i < GenPieces.Length; i++)
        {
            w[i] = GenPieces[i].piece switch
            {
                GenPiece.Straight1x1     => weightStraight1x1,
                GenPiece.Straight1x2     => weightStraight1x2,
                GenPiece.CornerSmallRight => weightCornerSmall,
                GenPiece.CornerSmallLeft  => weightCornerSmall,
                GenPiece.CornerLargeRight => weightCornerLarge,
                GenPiece.CornerLargeLeft  => weightCornerLarge,
                GenPiece.CornerLargerRight=> weightCornerLarger,
                GenPiece.CornerLargerLeft => weightCornerLarger,
                _ => 1f,
            };
        }
        return w;
    }

    // ─── Grid occupancy ─────────────────────────────────────────

    bool CanPlacePiece(GenPieceDef def, Vector2Int entryPos, Dir heading)
    {
        foreach (var localCell in def.localCells)
        {
            var worldCell = entryPos + RotateOffset(localCell, heading);
            if (worldCell.x < 0 || worldCell.x >= gridSize ||
                worldCell.y < 0 || worldCell.y >= gridSize)
                return false;
            if (occupancy[worldCell.x, worldCell.y] != -1)
                return false;
        }
        return true;
    }

    PlacedPiece PlacePiece(GenPieceDef def, Vector2Int entryPos, Dir heading, int pieceIndex)
    {
        var cells = new List<Vector2Int>(def.localCells.Length);
        foreach (var localCell in def.localCells)
        {
            var worldCell = entryPos + RotateOffset(localCell, heading);
            occupancy[worldCell.x, worldCell.y] = pieceIndex;
            cells.Add(worldCell);
        }

        // Pick a random model that matches the ConnShape
        string modelId = PickModelForShape(def.connShape);

        return new PlacedPiece
        {
            def = def,
            gridPos = entryPos,
            heading = heading,
            modelId = modelId,
            occupiedCells = cells,
        };
    }

    void RemovePieceFromGrid(PlacedPiece pp)
    {
        foreach (var cell in pp.occupiedCells)
        {
            if (cell.x >= 0 && cell.x < gridSize && cell.y >= 0 && cell.y < gridSize)
                occupancy[cell.x, cell.y] = -1;
        }
    }

    void ClearOccupancy()
    {
        if (occupancy == null) return;
        for (int x = 0; x < gridSize; x++)
            for (int z = 0; z < gridSize; z++)
                occupancy[x, z] = -1;
    }

    void RecalcCursor(List<PlacedPiece> placed, Vector2Int startPos, Dir startHeading,
        out Vector2Int cursor, out Dir heading)
    {
        cursor = startPos;
        heading = startHeading;
        foreach (var pp in placed)
        {
            var exitOff = RotateOffset(pp.def.exitOffset, pp.heading);
            cursor = pp.gridPos + exitOff;
            heading = ApplyTurn(pp.heading, pp.def.turnSteps);
        }
    }

    static Dir ApplyTurn(Dir heading, int turnSteps)
    {
        int h = ((int)heading + turnSteps + 4) % 4;
        return (Dir)h;
    }

    // ─── Model selection ────────────────────────────────────────

    string PickModelForShape(TrackPieceDatabase.ConnShape shape)
    {
        var genPieces = TrackPieceDatabase.GetGenerationPieces();
        var matching = new List<TrackPieceDatabase.PieceDef>();
        foreach (var p in genPieces)
        {
            if (p.connShape == shape)
                matching.Add(p);
        }

        if (matching.Count == 0)
        {
            // Fallback
            return shape switch
            {
                TrackPieceDatabase.ConnShape.Straight1x1 => "roadStraight",
                TrackPieceDatabase.ConnShape.Straight1x2 => "roadStraightLong",
                TrackPieceDatabase.ConnShape.Corner1x1 => "roadCornerSmall",
                TrackPieceDatabase.ConnShape.Corner2x2 => "roadCornerLarge",
                TrackPieceDatabase.ConnShape.Corner3x3 => "roadCornerLarger",
                _ => "roadStraight",
            };
        }

        return matching[Random.Range(0, matching.Count)].modelId;
    }

    // ─── Instantiation ──────────────────────────────────────────

    void InstantiateTrack(List<PlacedPiece> pieces)
    {
        var checkpoints = new List<Checkpoint>();

        for (int i = 0; i < pieces.Count; i++)
        {
            var pp = pieces[i];

            // World position: center of occupied cells
            Vector3 worldPos = GridToWorld(pp.gridPos);

            // Model rotation: combine heading rotation with corner-specific flip
            float yRot = CalculateModelRotation(pp);

            // Load and instantiate the FBX model
            GameObject pieceGO = InstantiateModel(pp.modelId, worldPos, yRot);
            if (pieceGO == null)
            {
                // Fallback: create a placeholder plane
                pieceGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                pieceGO.transform.position = worldPos;
                pieceGO.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
                pieceGO.transform.localScale = new Vector3(cellSize * 0.1f, 1f, cellSize * 0.1f);
            }

            pieceGO.name = $"track_{i}_{pp.modelId}";
            pieceGO.transform.SetParent(trackParent);

            // Add mesh colliders
            foreach (var mf in pieceGO.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() == null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }

            // Add checkpoint trigger at piece center
            var cpGO = new GameObject($"checkpoint_{i}");
            cpGO.transform.SetParent(trackParent);
            cpGO.transform.position = worldPos + Vector3.up * 0.5f;
            cpGO.transform.rotation = Quaternion.Euler(0f, DirToYRotation(pp.heading), 0f);

            var cpCollider = cpGO.AddComponent<BoxCollider>();
            cpCollider.isTrigger = true;
            cpCollider.size = new Vector3(checkpointWidth, checkpointHeight, 1f);

            var cp = cpGO.AddComponent<Checkpoint>();
            checkpoints.Add(cp);

            // First piece: create spawn points
            if (i == 0)
                CreateSpawnPoints(worldPos, Quaternion.Euler(0f, DirToYRotation(pp.heading), 0f));
        }

        // Wire up checkpoint manager
        if (checkpointManager != null)
        {
            var cmSO = checkpointManager;
            // Manager auto-discovers via GetComponentsInChildren in its Awake
        }
    }

    float CalculateModelRotation(PlacedPiece pp)
    {
        // Base rotation from heading
        float baseRot = DirToYRotation(pp.heading);

        // For left-turn corners, the FBX model is a right-turn model
        // that we mirror by adding 90° rotation
        bool isLeftTurn = pp.def.turnSteps < 0;
        if (isLeftTurn)
        {
            // Left turn = right-turn model rotated -90° relative to heading
            baseRot -= 90f;
        }

        return baseRot;
    }

    GameObject InstantiateModel(string modelId, Vector3 position, float yRotation)
    {
        // Try loading from Resources first (runtime)
        string resourcePath = modelBasePath + modelId;
        var prefab = Resources.Load<GameObject>(resourcePath);

        #if UNITY_EDITOR
        if (prefab == null)
        {
            // Editor fallback: load from AssetDatabase
            string assetPath = $"Assets/{modelBasePath}{modelId}.fbx";
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }
        #endif

        if (prefab == null)
        {
            Debug.LogWarning($"TrackGenerator: Could not load model '{modelId}'");
            return null;
        }

        #if UNITY_EDITOR
        GameObject instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            instance = Instantiate(prefab);
        #else
        GameObject instance = Instantiate(prefab);
        #endif

        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        return instance;
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        float halfGrid = gridSize * 0.5f;
        return new Vector3(
            (gridPos.x - halfGrid) * cellSize,
            0f,
            (gridPos.y - halfGrid) * cellSize
        );
    }

    void CreateSpawnPoints(Vector3 pos, Quaternion rot)
    {
        spawnPoints.Clear();
        float[] laneOffsets = { -1.2f, 0f, 1.2f };

        for (int row = 0; row < 2; row++)
        {
            for (int lane = 0; lane < laneOffsets.Length; lane++)
            {
                if (spawnPoints.Count >= 6) break;

                Vector3 offset = new Vector3(laneOffsets[lane], 0f, -row * 3f);
                Vector3 spawnPos = pos + rot * offset;
                var sp = new GameObject($"SpawnPoint_{spawnPoints.Count}").transform;
                sp.SetPositionAndRotation(spawnPos, rot);
                sp.SetParent(trackParent);
                spawnPoints.Add(sp);
            }
        }
    }

    // ─── Fallback oval ──────────────────────────────────────────

    List<PlacedPiece> GenerateFallbackOval()
    {
        ClearOccupancy();

        var placed = new List<PlacedPiece>();
        Vector2Int cursor = new(gridSize / 2, gridSize / 2);
        Dir heading = Dir.North;

        int straightsPerSide = Mathf.Max(2, (minPieces - 4) / 4);

        for (int side = 0; side < 4; side++)
        {
            // Straights
            for (int s = 0; s < straightsPerSide; s++)
            {
                var def = GenPieces[0]; // Straight1x1
                if (CanPlacePiece(def, cursor, heading))
                {
                    var pp = PlacePiece(def, cursor, heading, placed.Count);
                    placed.Add(pp);
                    var exitOff = RotateOffset(def.exitOffset, heading);
                    cursor = pp.gridPos + exitOff;
                }
            }

            // Corner (right turn)
            var cornerDef = GenPieces[2]; // CornerSmallRight
            if (CanPlacePiece(cornerDef, cursor, heading))
            {
                var pp = PlacePiece(cornerDef, cursor, heading, placed.Count);
                placed.Add(pp);
                var exitOff = RotateOffset(cornerDef.exitOffset, heading);
                cursor = pp.gridPos + exitOff;
                heading = ApplyTurn(heading, cornerDef.turnSteps);
            }
        }

        return placed;
    }
}
