using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catalog of every road piece from the Racing Kit, labeled with grid dimensions
/// matching the website track builder (js/trackPieces.js).
/// Pieces used for procedural generation also carry connection metadata.
/// </summary>
public static class TrackPieceDatabase
{
    public enum Category
    {
        Straight,
        CornerSmall,
        CornerLarge,
        CornerLarger,
        Bridge,
        Split,
        StartEnd,
        PitLane,
        Special,
    }

    /// <summary>
    /// Shape determines how the procedural generator treats a piece for connections.
    /// </summary>
    public enum ConnShape
    {
        None,          // Not used in procedural generation
        Straight1x1,   // 1-cell straight
        Straight1x2,   // 2-cell straight (along depth)
        Corner1x1,     // 90° turn in 1 cell
        Corner2x2,     // 90° turn in 2x2 cells
        Corner3x3,     // 90° turn in 3x3 cells
    }

    public readonly struct PieceDef
    {
        public readonly string modelId;
        public readonly string displayName;
        public readonly Category category;
        public readonly int gridW;
        public readonly int gridD;
        public readonly ConnShape connShape;

        public PieceDef(string modelId, string displayName, Category cat, int w, int d, ConnShape conn = ConnShape.None)
        {
            this.modelId = modelId;
            this.displayName = displayName;
            category = cat;
            gridW = w;
            gridD = d;
            connShape = conn;
        }
    }

    // Master catalog — grid sizes match js/trackPieces.js exactly
    // Ramps excluded per project requirement (flat tracks only)
    public static readonly PieceDef[] All = new PieceDef[]
    {
        // ── Straights ──────────────────────────────────────────────
        new("roadStraight",              "Straight",                Category.Straight,     1, 1, ConnShape.Straight1x1),
        new("roadStraightLong",          "Straight Long",           Category.Straight,     1, 2, ConnShape.Straight1x2),
        new("roadStraightArrow",         "Straight Arrow",          Category.Straight,     1, 1, ConnShape.Straight1x1),
        new("roadStraightSkew",          "Straight Skew",           Category.Straight,     2, 2, ConnShape.None),
        new("roadStraightLongMid",       "Straight Long Mid",       Category.Straight,     1, 2, ConnShape.Straight1x2),
        new("roadStraightLongBump",      "Straight Long Bump",      Category.Straight,     1, 2, ConnShape.Straight1x2),
        new("roadStraightLongBumpRound", "Straight Long Bump Rnd",  Category.Straight,     1, 2, ConnShape.Straight1x2),
        new("roadBump",                  "Bump",                    Category.Straight,     1, 2, ConnShape.Straight1x2),

        // ── Corners Small (1×1) ────────────────────────────────────
        new("roadCornerSmall",           "Corner Small",            Category.CornerSmall,  1, 1, ConnShape.Corner1x1),
        new("roadCornerSmallBorder",     "Corner Small Border",     Category.CornerSmall,  1, 1, ConnShape.Corner1x1),
        new("roadCornerSmallSand",       "Corner Small Sand",       Category.CornerSmall,  1, 1, ConnShape.Corner1x1),
        new("roadCornerSmallSquare",     "Corner Small Square",     Category.CornerSmall,  1, 1, ConnShape.Corner1x1),
        new("roadCornerSmallWall",       "Corner Small Wall",       Category.CornerSmall,  1, 1, ConnShape.Corner1x1),

        // ── Corners Large (2×2) ────────────────────────────────────
        new("roadCornerLarge",           "Corner Large",            Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeBorder",     "Corner Large Border",     Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeBorderInner","Corner Large Brd Inner",  Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeSand",       "Corner Large Sand",       Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeSandInner",  "Corner Large Sand Inner", Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeWall",       "Corner Large Wall",       Category.CornerLarge,  2, 2, ConnShape.Corner2x2),
        new("roadCornerLargeWallInner",  "Corner Large Wall Inner", Category.CornerLarge,  2, 2, ConnShape.Corner2x2),

        // ── Corners Larger (3×3) ───────────────────────────────────
        new("roadCornerLarger",           "Corner Larger",           Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerBorder",     "Corner Larger Border",    Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerBorderInner","Corner Larger Brd Inner", Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerSand",       "Corner Larger Sand",      Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerSandInner",  "Corner Larger Sand Innr", Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerWall",       "Corner Larger Wall",      Category.CornerLarger, 3, 3, ConnShape.Corner3x3),
        new("roadCornerLargerWallInner",  "Corner Larger Wall Inner",Category.CornerLarger, 3, 3, ConnShape.Corner3x3),

        // ── Bridges (flat, no elevation change) ────────────────────
        new("roadStraightBridge",        "Bridge Straight",         Category.Bridge,       1, 1, ConnShape.Straight1x1),
        new("roadStraightBridgeStart",   "Bridge Start",            Category.Bridge,       1, 1, ConnShape.Straight1x1),
        new("roadStraightBridgeMid",     "Bridge Mid",              Category.Bridge,       1, 1, ConnShape.Straight1x1),
        new("roadCornerBridgeSmall",     "Bridge Corner Small",     Category.Bridge,       1, 1, ConnShape.Corner1x1),
        new("roadCornerBridgeLarge",     "Bridge Corner Large",     Category.Bridge,       2, 2, ConnShape.Corner2x2),
        new("roadCornerBridgeLarger",    "Bridge Corner Larger",    Category.Bridge,       3, 3, ConnShape.Corner3x3),

        // ── Splits ─────────────────────────────────────────────────
        new("roadSplitSmall",            "Split Small",             Category.Split,        1, 1, ConnShape.None),
        new("roadSplit",                 "Split",                   Category.Split,        3, 2, ConnShape.None),
        new("roadSplitLarge",            "Split Large",             Category.Split,        2, 2, ConnShape.None),
        new("roadSplitLarger",           "Split Larger",            Category.Split,        3, 3, ConnShape.None),
        new("roadSplitRound",            "Split Round",             Category.Split,        3, 2, ConnShape.None),
        new("roadSplitRoundLarge",       "Split Round Large",       Category.Split,        5, 3, ConnShape.None),

        // ── Start / End ────────────────────────────────────────────
        new("roadStart",                 "Start Gate",              Category.StartEnd,     2, 2, ConnShape.None),
        new("roadStartPositions",        "Start Positions",         Category.StartEnd,     1, 2, ConnShape.Straight1x2),
        new("roadEnd",                   "Finish Line",             Category.StartEnd,     1, 2, ConnShape.Straight1x2),

        // ── Pit Lane ───────────────────────────────────────────────
        new("roadPitEntry",              "Pit Entry",               Category.PitLane,      2, 2, ConnShape.None),
        new("roadPitStraight",           "Pit Straight",            Category.PitLane,      1, 1, ConnShape.None),
        new("roadPitStraightLong",       "Pit Straight Long",       Category.PitLane,      1, 2, ConnShape.None),
        new("roadPitGarage",             "Pit Garage",              Category.PitLane,      1, 1, ConnShape.None),

        // ── Special Road ───────────────────────────────────────────
        new("roadCrossing",              "Crossing",                Category.Special,      2, 2, ConnShape.None),
        new("roadCurved",                "Curved",                  Category.Special,      2, 2, ConnShape.None),
        new("roadCurvedSplit",           "Curved Split",            Category.Special,      2, 2, ConnShape.None),
        new("roadSide",                  "Side",                    Category.Special,      2, 2, ConnShape.None),
    };

    static Dictionary<string, PieceDef> _lookup;

    public static PieceDef? GetByModelId(string modelId)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, PieceDef>(All.Length);
            foreach (var p in All)
                _lookup[p.modelId] = p;
        }
        return _lookup.TryGetValue(modelId, out var def) ? def : null;
    }

    /// <summary>
    /// Returns all pieces that are usable in procedural generation (have a ConnShape).
    /// </summary>
    public static List<PieceDef> GetGenerationPieces()
    {
        var list = new List<PieceDef>();
        foreach (var p in All)
        {
            if (p.connShape != ConnShape.None)
                list.Add(p);
        }
        return list;
    }
}
