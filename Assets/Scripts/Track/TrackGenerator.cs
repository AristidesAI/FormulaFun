using System.Collections.Generic;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    public enum PieceType
    {
        Straight,
        StraightLong,
        CornerSmall,   // 90-degree tight
        CornerLarge,   // 90-degree wide
        CornerLarger,  // 90-degree wider
    }

    [System.Serializable]
    public class TrackPiecePrefab
    {
        public PieceType type;
        public GameObject prefab;
        [Tooltip("How far forward this piece extends (in world units)")]
        public float forwardLength;
        [Tooltip("For corners: the lateral offset after this piece")]
        public float lateralOffset;
        [Tooltip("For corners: rotation delta in degrees (positive = right turn)")]
        public float rotationDelta;
    }

    [Header("Prefab Library")]
    [SerializeField] List<TrackPiecePrefab> piecePrefabs = new();

    [Header("Wall Prefab")]
    [SerializeField] GameObject wallPrefab;
    [SerializeField] float wallHeight = 1.5f;
    [SerializeField] float trackHalfWidth = 2.5f;

    [Header("Generation Settings")]
    [SerializeField] int minPieces = 12;
    [SerializeField] int maxPieces = 20;
    [SerializeField] int seed = -1; // -1 = random

    [Header("Checkpoint Settings")]
    [SerializeField] GameObject checkpointPrefab;
    [SerializeField] int checkpointEveryNPieces = 2;

    Transform trackParent;
    CheckpointManager checkpointManager;
    List<Transform> spawnPoints = new();

    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
    public CheckpointManager CheckpointMgr => checkpointManager;

    public void GenerateTrack()
    {
        ClearTrack();

        if (seed >= 0)
            Random.InitState(seed);
        else
            Random.InitState(System.Environment.TickCount);

        trackParent = new GameObject("GeneratedTrack").transform;
        trackParent.SetParent(transform);

        // Add checkpoint manager
        checkpointManager = trackParent.gameObject.AddComponent<CheckpointManager>();

        int pieceCount = Random.Range(minPieces, maxPieces + 1);
        List<PieceType> sequence = GenerateClosedLoopSequence(pieceCount);

        Vector3 currentPos = Vector3.zero;
        Quaternion currentRot = Quaternion.identity;
        List<Checkpoint> checkpoints = new();
        int pieceIndex = 0;

        foreach (PieceType type in sequence)
        {
            TrackPiecePrefab pieceDef = GetPieceDef(type);
            if (pieceDef == null || pieceDef.prefab == null) continue;

            // Place the piece
            GameObject piece = Instantiate(pieceDef.prefab, currentPos, currentRot, trackParent);
            piece.name = $"track_{pieceIndex}_{type}";

            // Place walls along this piece
            PlaceWalls(currentPos, currentRot, pieceDef);

            // Place checkpoint
            if (pieceIndex % checkpointEveryNPieces == 0 && checkpointPrefab != null)
            {
                Vector3 cpPos = currentPos + currentRot * (Vector3.forward * pieceDef.forwardLength * 0.5f);
                cpPos.y += 0.5f;
                GameObject cpObj = Instantiate(checkpointPrefab, cpPos, currentRot, trackParent);
                cpObj.name = $"checkpoint_{checkpoints.Count}";
                var cp = cpObj.GetComponent<Checkpoint>();
                if (cp == null) cp = cpObj.AddComponent<Checkpoint>();
                checkpoints.Add(cp);
            }

            // First piece: create spawn points
            if (pieceIndex == 0)
            {
                CreateSpawnPoints(currentPos, currentRot);
            }

            // Advance cursor
            if (pieceDef.rotationDelta != 0f)
            {
                // Corner piece
                currentPos += currentRot * (Vector3.forward * pieceDef.forwardLength);
                currentRot *= Quaternion.Euler(0f, pieceDef.rotationDelta, 0f);
            }
            else
            {
                // Straight piece
                currentPos += currentRot * (Vector3.forward * pieceDef.forwardLength);
            }

            pieceIndex++;
        }
    }

    List<PieceType> GenerateClosedLoopSequence(int targetPieces)
    {
        // Simple approach: generate a loop with 4 right-angle turns
        // Fill straights between corners to reach target count
        var sequence = new List<PieceType>();

        int straightsPerSide = Mathf.Max(1, (targetPieces - 4) / 4);
        PieceType[] cornerTypes = { PieceType.CornerSmall, PieceType.CornerLarge, PieceType.CornerLarger };

        for (int side = 0; side < 4; side++)
        {
            // Straights
            for (int s = 0; s < straightsPerSide; s++)
            {
                sequence.Add(Random.value > 0.4f ? PieceType.Straight : PieceType.StraightLong);
            }
            // Corner (right turn to form a loop)
            sequence.Add(cornerTypes[Random.Range(0, cornerTypes.Length)]);
        }

        return sequence;
    }

    void PlaceWalls(Vector3 pos, Quaternion rot, TrackPiecePrefab pieceDef)
    {
        if (wallPrefab == null) return;

        // Left wall
        Vector3 leftWallPos = pos + rot * (Vector3.left * trackHalfWidth + Vector3.forward * pieceDef.forwardLength * 0.5f);
        leftWallPos.y += wallHeight * 0.5f;
        GameObject leftWall = Instantiate(wallPrefab, leftWallPos, rot, trackParent);
        leftWall.transform.localScale = new Vector3(0.2f, wallHeight, pieceDef.forwardLength);
        leftWall.name = "wall_L";
        leftWall.layer = LayerMask.NameToLayer("Wall");

        // Right wall
        Vector3 rightWallPos = pos + rot * (Vector3.right * trackHalfWidth + Vector3.forward * pieceDef.forwardLength * 0.5f);
        rightWallPos.y += wallHeight * 0.5f;
        GameObject rightWall = Instantiate(wallPrefab, rightWallPos, rot, trackParent);
        rightWall.transform.localScale = new Vector3(0.2f, wallHeight, pieceDef.forwardLength);
        rightWall.name = "wall_R";
        rightWall.layer = LayerMask.NameToLayer("Wall");
    }

    void CreateSpawnPoints(Vector3 pos, Quaternion rot)
    {
        spawnPoints.Clear();
        float[] laneOffsets = { -1.2f, 0f, 1.2f };

        for (int row = 0; row < 4; row++)
        {
            for (int lane = 0; lane < laneOffsets.Length; lane++)
            {
                if (spawnPoints.Count >= 6) break; // max 6 cars

                Vector3 offset = new Vector3(laneOffsets[lane], 0f, -row * 3f);
                Vector3 spawnPos = pos + rot * offset;
                var sp = new GameObject($"SpawnPoint_{spawnPoints.Count}").transform;
                sp.SetPositionAndRotation(spawnPos, rot);
                sp.SetParent(trackParent);
                spawnPoints.Add(sp);
            }
        }
    }

    TrackPiecePrefab GetPieceDef(PieceType type)
    {
        foreach (var p in piecePrefabs)
        {
            if (p.type == type) return p;
        }
        // Return a fallback straight
        return piecePrefabs.Count > 0 ? piecePrefabs[0] : null;
    }

    public void ClearTrack()
    {
        if (trackParent != null)
        {
            Destroy(trackParent.gameObject);
        }
        spawnPoints.Clear();
        checkpointManager = null;
    }
}
