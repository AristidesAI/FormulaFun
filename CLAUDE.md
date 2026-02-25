# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FormulaFun is a mobile iOS portrait-mode Formula One racing game built with Unity 6 LTS (6000.3.8f1). The player races against AI opponents trained with Unity ML-Agents. Tracks are procedurally generated from the Racing Kit asset library.

**Target platform:** iOS (portrait orientation)
**Render pipeline:** Universal Render Pipeline (URP) with Mobile and PC quality tiers
**Input:** Unity Input System (new) — left/right steering buttons + gearbox-style speed knob
**AI:** Unity ML-Agents 4.0.2 (com.unity.ml-agents) for opponent car behavior

## Build & Run Commands

### Unity (Editor / iOS Build)
Open the project in Unity 6 LTS (6000.3.8f1). The solution file is `FormulaFun.slnx` with two assemblies: `Assembly-CSharp` (runtime) and `Assembly-CSharp-Editor` (editor scripts).

Build for iOS via **File > Build Settings > iOS > Build**. The active scene is `Assets/Scenes/SampleScene.unity`.

### ML-Agents Training (Python)
```bash
# Activate the Python 3.10 venv (already set up)
source venv/bin/activate

# Run training (from project root, adjust config path as needed)
mlagents-learn <config.yaml> --run-id=<run_name>

# Resume interrupted training
mlagents-learn <config.yaml> --run-id=<run_name> --resume

# Monitor training with TensorBoard
tensorboard --logdir results
```

The venv uses Python 3.10 (homebrew) with mlagents 1.1.0, PyTorch 2.10.0, and ONNX 1.15.0 installed. Training produces `.onnx` model files that get imported into Unity as NNModel assets for the ML-Agents inference runtime.

### Unity Tests
```bash
# Run EditMode tests from CLI (requires Unity installed)
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -runTests -batchmode -projectPath . \
  -testPlatform EditMode -testResults results.xml

# Run PlayMode tests
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -runTests -batchmode -projectPath . \
  -testPlatform PlayMode -testResults results.xml
```

## Architecture

### Game Design
- **Portrait mode** with touch controls: left/right steering buttons flanking a central gearbox speed knob
- Player competes against ML-Agents-trained AI drivers on procedurally generated tracks
- Tracks are assembled from modular Racing Kit pieces (road segments, barriers, decorations)

### Key Directories
- `Assets/Racing Kit/` — Third-party modular racing asset pack (112 FBX models). Road segments, barriers, grandstands, pit buildings, race cars, and decorations. No ramps/jumps (removed from game).
- `Assets/Scripts/Car/` — CarController (Rigidbody arcade physics, brake/drift)
- `Assets/Scripts/Camera/` — IsometricCameraController (fixed-rotation follow cam)
- `Assets/Scripts/Input/` — TouchInputManager (Enhanced Touch left/right/brake), SpeedKnobUI (gear knob)
- `Assets/Scripts/Track/` — TrackPieceDatabase (piece catalog with grid sizes), TrackGenerator (grid-based procedural gen), Checkpoint/CheckpointManager (ML-Agents rewards)
- `Assets/Scripts/TrackBuilder/` — TrackLayout (JSON deserialization), TrackImporter (editor import from website builder JSON)
- `Assets/Scripts/Agent/` — CarAgent (ML-Agents Agent subclass with raycasts + checkpoint rewards)
- `Assets/Scripts/Game/` — GameManager (state machine singleton), RaceTimer, DemoAutoStart
- `Assets/Scripts/UI/` — RaceHUD, MainMenuUI, GameSettingsUI
- `Assets/Scripts/Editor/` — DemoSceneSetup (builds entire DemoScene programmatically)
- `Assets/Scenes/DemoScene.unity` — Playable demo scene
- `Assets/Settings/` — URP render pipeline assets (Mobile_RPAsset, PC_RPAsset)
- `Assets/InputSystem_Actions.inputactions` — Input action definitions
- `js/` — Website track builder (JavaScript): gridSystem.js, trackPieces.js, placement.js, etc.

### Racing Kit Track Pieces (grid sizes from js/trackPieces.js)
Road segments follow `road{Type}{Variant}`. Grid sizes (W×D):
- **Straights 1×1:** `roadStraight`, `roadStraightArrow`
- **Straights 1×2:** `roadStraightLong`, `roadStraightLongMid`, `roadStraightLongBump(Round)`, `roadBump`
- **Straights 2×2:** `roadStraightSkew`
- **Corners Small 1×1:** `roadCornerSmall(Border|Sand|Square|Wall)`
- **Corners Large 2×2:** `roadCornerLarge(Border|BorderInner|Sand|SandInner|Wall|WallInner)`
- **Corners Larger 3×3:** `roadCornerLarger(Border|BorderInner|Sand|SandInner|Wall|WallInner)`
- **Start/End:** `roadStart` (2×2), `roadStartPositions` (1×2), `roadEnd` (1×2)
- **No ramps** — removed from game (flat tracks only)
- Decorations/props: `barrier*`, `fence*`, `grandStand*`, `light*`, `pylon`, `flag*`, `billboard*` (all 1×1)
- Cars: `raceCarGreen`, `raceCarOrange`, `raceCarRed`, `raceCarWhite` (all 1×1)

### Track Generation System
The procedural TrackGenerator uses a grid-based turtle-walk algorithm:
1. Places pieces on a 2D occupancy grid (matching js/gridSystem.js logic)
2. Cursor tracks position + heading; straights advance, corners turn 90°
3. Backtracking resolves dead ends; closing logic steers back to start
4. Each piece gets a Checkpoint trigger for ML-Agents reward signals
5. FBX model variants are randomly selected per ConnShape (visual variety)

### ML-Agents Integration
The training pipeline follows the standard Unity ML-Agents workflow:
1. C# `Agent` subclass in Unity defines observations, actions, and rewards
2. Python `mlagents-learn` trains the model externally, communicating over gRPC
3. Trained `.onnx` model is imported into Unity and assigned to a `Behavior Parameters` component for inference

The project uses ML-Agents package 4.0.2 (Unity side) paired with mlagents Python package 1.1.0.

## C# Conventions
- Language version: C# 9.0 (LangVersion 9.0)
- Target framework: .NET Standard 2.1
- No custom assembly definitions yet — all runtime scripts compile into `Assembly-CSharp`
- URP shaders and materials only (no built-in pipeline)

## Unity Project Settings
- `defaultScreenOrientation: 4` (AutoRotation) — lock to portrait for the game
- Quality settings: "Mobile" (default) and "PC" tiers
- Navigation: AI Navigation package installed (com.unity.ai.navigation 2.0.10)
- No git repo initialized yet — set up `.gitignore` for Unity before first commit
