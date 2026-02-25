export const CATEGORIES = {
    ROAD_STRAIGHT: 'Road Straights',
    ROAD_CORNER_SMALL: 'Corners (Small)',
    ROAD_CORNER_LARGE: 'Corners (Large)',
    ROAD_CORNER_LARGER: 'Corners (Larger)',
    ROAD_BRIDGE: 'Bridges',
    ROAD_RAMP: 'Ramps',
    ROAD_SPLIT: 'Splits',
    ROAD_START_END: 'Start / End',
    ROAD_PIT: 'Pit Lane',
    ROAD_SPECIAL: 'Special Road',
    BARRIER: 'Barriers & Rails',
    DECORATION: 'Decorations',
    CAR: 'Cars',
};

// Each piece: modelId, display name, category key, grid width (X), grid depth (Z)
export const TRACK_PIECES = [
    // Road Straights
    { modelId: 'roadStraight',               name: 'Straight',                category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 1 },
    { modelId: 'roadStraightLong',            name: 'Straight Long',           category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 2 },
    { modelId: 'roadStraightArrow',           name: 'Straight Arrow',          category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 1 },
    { modelId: 'roadStraightSkew',            name: 'Straight Skew',           category: 'ROAD_STRAIGHT',      gridW: 2, gridD: 2 },
    { modelId: 'roadStraightLongMid',         name: 'Straight Long Mid',       category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 2 },
    { modelId: 'roadStraightLongBump',        name: 'Straight Long Bump',      category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 2 },
    { modelId: 'roadStraightLongBumpRound',   name: 'Straight Long Bump Rnd',  category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 2 },
    { modelId: 'roadBump',                    name: 'Bump',                    category: 'ROAD_STRAIGHT',      gridW: 1, gridD: 2 },

    // Corners Small (1x1)
    { modelId: 'roadCornerSmall',             name: 'Corner Small',            category: 'ROAD_CORNER_SMALL',  gridW: 1, gridD: 1 },
    { modelId: 'roadCornerSmallBorder',       name: 'Corner Small Border',     category: 'ROAD_CORNER_SMALL',  gridW: 1, gridD: 1 },
    { modelId: 'roadCornerSmallSand',         name: 'Corner Small Sand',       category: 'ROAD_CORNER_SMALL',  gridW: 1, gridD: 1 },
    { modelId: 'roadCornerSmallSquare',       name: 'Corner Small Square',     category: 'ROAD_CORNER_SMALL',  gridW: 1, gridD: 1 },
    { modelId: 'roadCornerSmallWall',         name: 'Corner Small Wall',       category: 'ROAD_CORNER_SMALL',  gridW: 1, gridD: 1 },

    // Corners Large (2x2)
    { modelId: 'roadCornerLarge',             name: 'Corner Large',            category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeBorder',       name: 'Corner Large Border',     category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeBorderInner',  name: 'Corner Large Brd Inner',  category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeSand',         name: 'Corner Large Sand',       category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeSandInner',    name: 'Corner Large Sand Inner', category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeWall',         name: 'Corner Large Wall',       category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },
    { modelId: 'roadCornerLargeWallInner',    name: 'Corner Large Wall Inner', category: 'ROAD_CORNER_LARGE',  gridW: 2, gridD: 2 },

    // Corners Larger (3x3)
    { modelId: 'roadCornerLarger',            name: 'Corner Larger',           category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerBorder',      name: 'Corner Larger Border',    category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerBorderInner', name: 'Corner Larger Brd Inner', category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerSand',        name: 'Corner Larger Sand',      category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerSandInner',   name: 'Corner Larger Sand Innr', category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerWall',        name: 'Corner Larger Wall',      category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },
    { modelId: 'roadCornerLargerWallInner',   name: 'Corner Larger Wall Inner',category: 'ROAD_CORNER_LARGER', gridW: 3, gridD: 3 },

    // Bridges
    { modelId: 'roadStraightBridge',          name: 'Bridge Straight',         category: 'ROAD_BRIDGE',        gridW: 1, gridD: 1 },
    { modelId: 'roadStraightBridgeStart',     name: 'Bridge Start',            category: 'ROAD_BRIDGE',        gridW: 1, gridD: 1 },
    { modelId: 'roadStraightBridgeMid',       name: 'Bridge Mid',              category: 'ROAD_BRIDGE',        gridW: 1, gridD: 1 },
    { modelId: 'roadCornerBridgeSmall',       name: 'Bridge Corner Small',     category: 'ROAD_BRIDGE',        gridW: 1, gridD: 1 },
    { modelId: 'roadCornerBridgeLarge',       name: 'Bridge Corner Large',     category: 'ROAD_BRIDGE',        gridW: 2, gridD: 2 },
    { modelId: 'roadCornerBridgeLarger',      name: 'Bridge Corner Larger',    category: 'ROAD_BRIDGE',        gridW: 3, gridD: 3 },

    // Ramps
    { modelId: 'roadRamp',                    name: 'Ramp',                    category: 'ROAD_RAMP',          gridW: 1, gridD: 1 },
    { modelId: 'roadRampWall',                name: 'Ramp Wall',               category: 'ROAD_RAMP',          gridW: 1, gridD: 1 },
    { modelId: 'roadRampLong',                name: 'Ramp Long',               category: 'ROAD_RAMP',          gridW: 1, gridD: 2 },
    { modelId: 'roadRampLongWall',            name: 'Ramp Long Wall',          category: 'ROAD_RAMP',          gridW: 1, gridD: 2 },
    { modelId: 'roadRampLongCurved',          name: 'Ramp Long Curved',        category: 'ROAD_RAMP',          gridW: 1, gridD: 2 },
    { modelId: 'roadRampLongCurvedWall',      name: 'Ramp Long Curved Wall',   category: 'ROAD_RAMP',          gridW: 1, gridD: 2 },
    { modelId: 'ramp',                        name: 'Ramp (Deco)',             category: 'ROAD_RAMP',          gridW: 1, gridD: 1 },

    // Splits
    { modelId: 'roadSplitSmall',              name: 'Split Small',             category: 'ROAD_SPLIT',         gridW: 1, gridD: 1 },
    { modelId: 'roadSplit',                   name: 'Split',                   category: 'ROAD_SPLIT',         gridW: 3, gridD: 2 },
    { modelId: 'roadSplitLarge',              name: 'Split Large',             category: 'ROAD_SPLIT',         gridW: 2, gridD: 2 },
    { modelId: 'roadSplitLarger',             name: 'Split Larger',            category: 'ROAD_SPLIT',         gridW: 3, gridD: 3 },
    { modelId: 'roadSplitRound',              name: 'Split Round',             category: 'ROAD_SPLIT',         gridW: 3, gridD: 2 },
    { modelId: 'roadSplitRoundLarge',         name: 'Split Round Large',       category: 'ROAD_SPLIT',         gridW: 5, gridD: 3 },

    // Start / End
    { modelId: 'roadStart',                   name: 'Start Gate',              category: 'ROAD_START_END',     gridW: 2, gridD: 2 },
    { modelId: 'roadStartPositions',          name: 'Start Positions',         category: 'ROAD_START_END',     gridW: 1, gridD: 2 },
    { modelId: 'roadEnd',                     name: 'Finish Line',             category: 'ROAD_START_END',     gridW: 1, gridD: 2 },

    // Pit Lane
    { modelId: 'roadPitEntry',                name: 'Pit Entry',               category: 'ROAD_PIT',           gridW: 2, gridD: 2 },
    { modelId: 'roadPitStraight',             name: 'Pit Straight',            category: 'ROAD_PIT',           gridW: 1, gridD: 1 },
    { modelId: 'roadPitStraightLong',         name: 'Pit Straight Long',       category: 'ROAD_PIT',           gridW: 1, gridD: 2 },
    { modelId: 'roadPitGarage',               name: 'Pit Garage',              category: 'ROAD_PIT',           gridW: 1, gridD: 1 },

    // Special Road
    { modelId: 'roadCrossing',                name: 'Crossing',                category: 'ROAD_SPECIAL',       gridW: 2, gridD: 2 },
    { modelId: 'roadCurved',                  name: 'Curved',                  category: 'ROAD_SPECIAL',       gridW: 2, gridD: 2 },
    { modelId: 'roadCurvedSplit',             name: 'Curved Split',            category: 'ROAD_SPECIAL',       gridW: 2, gridD: 2 },
    { modelId: 'roadSide',                    name: 'Side',                    category: 'ROAD_SPECIAL',       gridW: 2, gridD: 2 },

    // Barriers & Rails
    { modelId: 'barrierRed',                  name: 'Barrier Red',             category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'barrierWhite',                name: 'Barrier White',           category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'barrierWall',                 name: 'Barrier Wall',            category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'rail',                        name: 'Rail',                    category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'railDouble',                  name: 'Rail Double',             category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'fenceStraight',               name: 'Fence Straight',          category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'fenceCurved',                 name: 'Fence Curved',            category: 'BARRIER',            gridW: 1, gridD: 1 },
    { modelId: 'pylon',                       name: 'Pylon',                   category: 'BARRIER',            gridW: 1, gridD: 1 },

    // Decorations
    { modelId: 'grandStand',                  name: 'Grand Stand',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'grandStandAwning',            name: 'Grand Stand Awning',      category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'grandStandCovered',           name: 'Grand Stand Covered',     category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'grandStandCoveredRound',      name: 'Grand Stand Cvrd Round',  category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'grandStandRound',             name: 'Grand Stand Round',       category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'billboard',                   name: 'Billboard',               category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'billboardDouble_exclusive',   name: 'Billboard Double',        category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'billboardLow',                name: 'Billboard Low',           category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'billboardLower',              name: 'Billboard Lower',         category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightColored',                name: 'Light Colored',           category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightPostLarge',              name: 'Light Post Large',        category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightPostModern',             name: 'Light Post Modern',       category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightPost_exclusive',         name: 'Light Post',              category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightRed',                    name: 'Light Red',               category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'lightRedDouble',              name: 'Light Red Double',        category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'overhead',                    name: 'Overhead',                category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'overheadLights',              name: 'Overhead Lights',         category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'overheadRound',               name: 'Overhead Round',          category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'overheadRoundColored',        name: 'Overhead Round Colored',  category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'flagCheckers',                name: 'Flag Checkers',           category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'flagCheckersSmall',           name: 'Flag Checkers Small',     category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'flagGreen',                   name: 'Flag Green',              category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'flagRed',                     name: 'Flag Red',                category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'flagTankco',                  name: 'Flag Tankco',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'bannerTowerGreen',            name: 'Banner Tower Green',      category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'bannerTowerRed',              name: 'Banner Tower Red',        category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'tent',                        name: 'Tent',                    category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'tentClosed',                  name: 'Tent Closed',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'tentClosedLong',              name: 'Tent Closed Long',        category: 'DECORATION',         gridW: 1, gridD: 2 },
    { modelId: 'tentLong',                    name: 'Tent Long',               category: 'DECORATION',         gridW: 1, gridD: 2 },
    { modelId: 'tentRoof',                    name: 'Tent Roof',               category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'tentRoofDouble',              name: 'Tent Roof Double',        category: 'DECORATION',         gridW: 1, gridD: 2 },
    { modelId: 'treeLarge',                   name: 'Tree Large',              category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'treeSmall',                   name: 'Tree Small',              category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'grass',                       name: 'Grass Patch',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'camera_exclusive',            name: 'Camera',                  category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'radarEquipment',              name: 'Radar Equipment',         category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsGarage',                  name: 'Pits Garage',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsGarageClosed',            name: 'Pits Garage Closed',      category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsGarageCorner',            name: 'Pits Garage Corner',      category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsOffice',                  name: 'Pits Office',             category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsOfficeCorner',            name: 'Pits Office Corner',      category: 'DECORATION',         gridW: 1, gridD: 1 },
    { modelId: 'pitsOfficeRoof',              name: 'Pits Office Roof',        category: 'DECORATION',         gridW: 1, gridD: 1 },

    // Cars
    { modelId: 'raceCarGreen',                name: 'Race Car Green',          category: 'CAR',                gridW: 1, gridD: 1 },
    { modelId: 'raceCarOrange',               name: 'Race Car Orange',         category: 'CAR',                gridW: 1, gridD: 1 },
    { modelId: 'raceCarRed',                  name: 'Race Car Red',            category: 'CAR',                gridW: 1, gridD: 1 },
    { modelId: 'raceCarWhite',                name: 'Race Car White',          category: 'CAR',                gridW: 1, gridD: 1 },
];

export function getPieceDef(modelId) {
    return TRACK_PIECES.find(p => p.modelId === modelId);
}
