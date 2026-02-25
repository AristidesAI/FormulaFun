using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utilities to wire up scene objects and add track generation.
/// Use via FormulaFun menu.
/// </summary>
public static class SceneWireUp
{
    [MenuItem("FormulaFun/Wire Up Brake Button")]
    static void WireBrakeButton()
    {
        // Find TouchInputManager in scene
        var touchMgr = Object.FindAnyObjectByType<TouchInputManager>();
        if (touchMgr == null)
        {
            Debug.LogError("SceneWireUp: No TouchInputManager found in scene.");
            return;
        }

        // Find the BrakeButton by name
        var brakeGO = GameObject.Find("BrakeButton");
        if (brakeGO == null)
        {
            // Try searching all children of canvases
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                var found = canvas.transform.Find("BrakeButton");
                if (found == null)
                    found = FindChildRecursive(canvas.transform, "BrakeButton");
                if (found != null)
                {
                    brakeGO = found.gameObject;
                    break;
                }
            }
        }

        if (brakeGO == null)
        {
            Debug.LogError("SceneWireUp: No GameObject named 'BrakeButton' found in scene.");
            return;
        }

        var brakeRT = brakeGO.GetComponent<RectTransform>();
        if (brakeRT == null)
        {
            Debug.LogError("SceneWireUp: BrakeButton has no RectTransform.");
            return;
        }

        // Wire it up via SerializedObject
        var so = new SerializedObject(touchMgr);
        so.FindProperty("brakeButton").objectReferenceValue = brakeRT;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(touchMgr);
        MarkSceneDirty();

        Debug.Log($"SceneWireUp: BrakeButton wired to TouchInputManager successfully. ({brakeGO.transform.GetSiblingIndex()})");
    }

    [MenuItem("FormulaFun/Setup Track Generation")]
    static void SetupTrackGeneration()
    {
        // Find or create TrackGenerator
        var existingGen = Object.FindAnyObjectByType<TrackGenerator>();
        if (existingGen != null)
        {
            Debug.Log("SceneWireUp: TrackGenerator already exists. Generating new track...");
            existingGen.GenerateTrack();
            MarkSceneDirty();
            return;
        }

        // Create TrackGenerator GameObject
        var genGO = new GameObject("TrackGenerator");
        var gen = genGO.AddComponent<TrackGenerator>();

        // Wire up via SerializedObject to set default values
        var so = new SerializedObject(gen);
        so.FindProperty("gridSize").intValue = 40;
        so.FindProperty("cellSize").floatValue = 4f;
        so.FindProperty("minPieces").intValue = 14;
        so.FindProperty("maxPieces").intValue = 22;
        so.FindProperty("seed").intValue = -1; // random
        so.FindProperty("maxRetries").intValue = 50;
        so.FindProperty("checkpointWidth").floatValue = 8f;
        so.FindProperty("checkpointHeight").floatValue = 4f;
        so.FindProperty("modelBasePath").stringValue = "Racing Kit/Models/FBX format/";
        so.ApplyModifiedProperties();

        // Generate an initial track
        gen.GenerateTrack();

        Undo.RegisterCreatedObjectUndo(genGO, "Setup Track Generation");
        Selection.activeGameObject = genGO;
        MarkSceneDirty();

        Debug.Log("SceneWireUp: TrackGenerator created and initial track generated.");
    }

    [MenuItem("FormulaFun/Wire Up Everything")]
    static void WireUpEverything()
    {
        WireBrakeButton();
        SetupTrackGeneration();
        Debug.Log("SceneWireUp: All wiring complete.");
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static void MarkSceneDirty()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
