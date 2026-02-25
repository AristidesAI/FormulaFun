using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DemoSceneSetup : EditorWindow
{
    [MenuItem("FormulaFun/Setup Demo Scene")]
    static void SetupScene()
    {
        // Open DemoScene
        string scenePath = "Assets/Scenes/DemoScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // --- CLEAN UP previous setup objects so we never get duplicates ---
        string[] managedNames = { "GameManager", "PlayerCar", "GameCanvas", "EventSystem" };
        foreach (var name in managedNames)
        {
            GameObject existing;
            while ((existing = GameObject.Find(name)) != null)
                Object.DestroyImmediate(existing);
        }

        // --- GAME MANAGER ---
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<GameManager>();

        // --- PLAYER CAR ---
        // Parent GameObject holds physics + controller; FBX model is a child rotated 180°
        // because the Racing Kit car models face -Z (backwards in Unity)
        var carGO = new GameObject("PlayerCar");
        carGO.transform.rotation = Quaternion.identity;

        string carModelPath = "Assets/Racing Kit/Models/FBX format/raceCarRed.fbx";
        var carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(carModelPath);
        if (carPrefab != null)
        {
            var carModel = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            carModel.name = "CarModel";
            carModel.transform.SetParent(carGO.transform, false);
            // Rotate 180° so model faces +Z (Unity forward)
            carModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            carModel.transform.localPosition = Vector3.zero;
        }
        else
        {
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = "CarModel";
            placeholder.transform.SetParent(carGO.transform, false);
            placeholder.transform.localScale = new Vector3(1f, 0.5f, 2f);
            Debug.LogWarning("Could not load raceCarRed.fbx, using placeholder cube");
        }

        // Add physics to parent
        var rb = carGO.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Compute a tight BoxCollider from the combined renderer bounds
        var renderers = carGO.GetComponentsInChildren<Renderer>();
        Bounds combinedBounds = new Bounds(carGO.transform.position, Vector3.zero);
        foreach (var r in renderers)
            combinedBounds.Encapsulate(r.bounds);

        var box = carGO.AddComponent<BoxCollider>();
        box.center = carGO.transform.InverseTransformPoint(combinedBounds.center);
        box.size = combinedBounds.size;

        // Position the car so the bottom of the collider sits at groundOffset above Y=0
        float bottomY = combinedBounds.center.y - combinedBounds.extents.y;
        float groundOffset = 0.0001f;
        carGO.transform.position = new Vector3(0f, -bottomY + groundOffset, 0f);

        var carController = carGO.AddComponent<CarController>();

        // --- CAMERA ---
        var camGO = GameObject.Find("Main Camera");
        if (camGO == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        var isoCam = camGO.GetComponent<IsometricCameraController>();
        if (isoCam == null)
            isoCam = camGO.AddComponent<IsometricCameraController>();

        // Set camera target via serialized field
        var camSO = new SerializedObject(isoCam);
        camSO.FindProperty("target").objectReferenceValue = carGO.transform;
        camSO.ApplyModifiedProperties();

        // Position will be set by IsometricCameraController at runtime;
        // set a reasonable initial position matching the isometric defaults
        camGO.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
        camGO.transform.position = carGO.transform.position + camGO.transform.rotation * new Vector3(0f, 0f, -25f);

        // --- CANVAS (UI) ---
        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Portrait
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // --- EVENT SYSTEM (required for all UI interaction) ---
        var eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // --- HUD PANEL ---
        var hudGO = CreatePanel(canvasGO.transform, "HUD", stretchFill: true);

        // Timer text (top center)
        var timerGO = CreateTMPText(hudGO.transform, "TimerText", "00:00.000",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), 36);

        // Speed text (top left)
        var speedGO = CreateTMPText(hudGO.transform, "SpeedText", "0 km/h",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -40f), 28);

        // Pause button (top right)
        var pauseBtnGO = CreateButton(hudGO.transform, "PauseButton", "II",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(80f, 80f));

        // --- CONTROLS PANEL (bottom) ---
        var controlsGO = CreatePanel(canvasGO.transform, "Controls", stretchFill: false);
        var controlsRT = controlsGO.GetComponent<RectTransform>();
        controlsRT.anchorMin = new Vector2(0f, 0f);
        controlsRT.anchorMax = new Vector2(1f, 0.25f);
        controlsRT.offsetMin = Vector2.zero;
        controlsRT.offsetMax = Vector2.zero;

        // Left button
        var leftBtnGO = CreateButton(controlsGO.transform, "LeftButton", "<",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(150f, 150f));

        // Brake button (between knob and right turn)
        var brakeBtnGO = CreateButton(controlsGO.transform, "BrakeButton", "BRAKE",
            new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0f, 0f), new Vector2(120f, 120f));
        var brakeBtnImage = brakeBtnGO.GetComponent<Image>();
        brakeBtnImage.color = new Color(0.8f, 0.1f, 0.1f, 0.9f);

        // Right button
        var rightBtnGO = CreateButton(controlsGO.transform, "RightButton", ">",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-100f, 0f), new Vector2(150f, 150f));

        // Speed knob area (center)
        var knobAreaGO = CreatePanel(controlsGO.transform, "SpeedKnobArea", stretchFill: false);
        var knobAreaRT = knobAreaGO.GetComponent<RectTransform>();
        knobAreaRT.anchorMin = new Vector2(0.5f, 0f);
        knobAreaRT.anchorMax = new Vector2(0.5f, 1f);
        knobAreaRT.sizeDelta = new Vector2(120f, 0f);
        knobAreaRT.anchoredPosition = Vector2.zero;
        var knobAreaImage = knobAreaGO.GetComponent<Image>();
        knobAreaImage.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // Knob track (the sliding area)
        var knobTrackGO = CreatePanel(knobAreaGO.transform, "KnobTrack", stretchFill: false);
        var knobTrackRT = knobTrackGO.GetComponent<RectTransform>();
        knobTrackRT.anchorMin = new Vector2(0.5f, 0.1f);
        knobTrackRT.anchorMax = new Vector2(0.5f, 0.9f);
        knobTrackRT.sizeDelta = new Vector2(60f, 0f);
        knobTrackRT.anchoredPosition = Vector2.zero;
        var trackImage = knobTrackGO.GetComponent<Image>();
        trackImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        // Knob handle
        var knobHandleGO = CreatePanel(knobTrackGO.transform, "KnobHandle", stretchFill: false);
        var knobHandleRT = knobHandleGO.GetComponent<RectTransform>();
        knobHandleRT.anchorMin = new Vector2(0.5f, 0.5f);
        knobHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
        knobHandleRT.sizeDelta = new Vector2(80f, 40f);
        knobHandleRT.anchoredPosition = new Vector2(0f, -knobTrackRT.rect.height * 0.5f);
        var handleImage = knobHandleGO.GetComponent<Image>();
        handleImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);

        // Gear label on knob
        var gearLabelGO = CreateTMPText(knobHandleGO.transform, "GearLabel", "N",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 24);

        // Add SpeedKnobUI component to knob area
        var speedKnob = knobAreaGO.AddComponent<SpeedKnobUI>();
        var knobSO = new SerializedObject(speedKnob);
        knobSO.FindProperty("carController").objectReferenceValue = carController;
        knobSO.FindProperty("knobHandle").objectReferenceValue = knobHandleRT;
        knobSO.FindProperty("knobTrack").objectReferenceValue = knobTrackRT;
        knobSO.FindProperty("gearLabel").objectReferenceValue = gearLabelGO.GetComponent<TMP_Text>();
        knobSO.ApplyModifiedProperties();

        // --- PAUSE PANEL (hidden by default) ---
        var pausePanelGO = CreatePanel(canvasGO.transform, "PausePanel", stretchFill: true);
        var pauseImage = pausePanelGO.GetComponent<Image>();
        pauseImage.color = new Color(0f, 0f, 0f, 0.7f);
        pausePanelGO.SetActive(false);

        var pauseTitleGO = CreateTMPText(pausePanelGO.transform, "PauseTitle", "PAUSED",
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, 48);

        var resumeBtnGO = CreateButton(pausePanelGO.transform, "ResumeButton", "RESUME",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(300f, 80f));

        var restartBtnGO = CreateButton(pausePanelGO.transform, "RestartButton", "RESTART",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(300f, 80f));

        var menuBtnGO = CreateButton(pausePanelGO.transform, "MainMenuButton", "MAIN MENU",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(300f, 80f));

        // --- WIRE UP COMPONENTS ---

        // RaceHUD
        var raceHUD = hudGO.AddComponent<RaceHUD>();
        var hudSO = new SerializedObject(raceHUD);
        hudSO.FindProperty("speedText").objectReferenceValue = speedGO.GetComponent<TMP_Text>();
        hudSO.FindProperty("pauseButton").objectReferenceValue = pauseBtnGO.GetComponent<Button>();
        hudSO.FindProperty("pausePanel").objectReferenceValue = pausePanelGO;
        hudSO.FindProperty("resumeButton").objectReferenceValue = resumeBtnGO.GetComponent<Button>();
        hudSO.FindProperty("restartButton").objectReferenceValue = restartBtnGO.GetComponent<Button>();
        hudSO.FindProperty("mainMenuButton").objectReferenceValue = menuBtnGO.GetComponent<Button>();
        hudSO.FindProperty("carController").objectReferenceValue = carController;
        hudSO.ApplyModifiedProperties();

        // RaceTimer
        var raceTimer = hudGO.AddComponent<RaceTimer>();
        var timerSO = new SerializedObject(raceTimer);
        timerSO.FindProperty("timerText").objectReferenceValue = timerGO.GetComponent<TMP_Text>();
        timerSO.ApplyModifiedProperties();

        // TouchInputManager
        var touchMgr = canvasGO.AddComponent<TouchInputManager>();
        var touchSO = new SerializedObject(touchMgr);
        touchSO.FindProperty("carController").objectReferenceValue = carController;
        touchSO.FindProperty("leftButton").objectReferenceValue = leftBtnGO.GetComponent<RectTransform>();
        touchSO.FindProperty("rightButton").objectReferenceValue = rightBtnGO.GetComponent<RectTransform>();
        touchSO.FindProperty("brakeButton").objectReferenceValue = brakeBtnGO.GetComponent<RectTransform>();
        touchSO.FindProperty("speedKnob").objectReferenceValue = speedKnob;
        touchSO.ApplyModifiedProperties();

        // --- AUTO-START RACE ---
        // Add a simple auto-start component
        var autoStart = gmGO.AddComponent<DemoAutoStart>();

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("DemoScene setup complete!");
    }

    static void CreateDemoTrack()
    {
        var trackParent = new GameObject("Track");
        trackParent.transform.position = Vector3.zero;

        // Try to load road pieces
        string basePath = "Assets/Racing Kit/Models/FBX format/";
        var straightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "roadStraight.fbx");
        var cornerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "roadCornerLarge.fbx");
        var startPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "roadStart.fbx");

        if (straightPrefab == null || cornerPrefab == null)
        {
            Debug.LogWarning("Could not load road FBX models. Creating placeholder track.");
            // Create a simple rectangular plane track
            var trackPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            trackPlane.name = "TrackSurface";
            trackPlane.transform.SetParent(trackParent.transform);
            trackPlane.transform.localScale = new Vector3(5f, 1f, 10f);
            trackPlane.transform.position = new Vector3(0f, 0.01f, 20f);
            return;
        }

        // Build a simple oval: start, straights, and 4 corners
        float straightLength = 4f; // approx length of one road piece
        int straightCount = 6;

        Vector3 pos = Vector3.zero;
        float angle = 0f;

        // Place start
        PlaceRoadPiece(startPrefab, trackParent.transform, pos, angle);
        pos += Quaternion.Euler(0, angle, 0) * Vector3.forward * straightLength;

        // Straight section 1
        for (int i = 0; i < straightCount; i++)
        {
            PlaceRoadPiece(straightPrefab, trackParent.transform, pos, angle);
            pos += Quaternion.Euler(0, angle, 0) * Vector3.forward * straightLength;
        }

        // Corner 1 (turn right 90)
        PlaceRoadPiece(cornerPrefab, trackParent.transform, pos, angle);
        angle += 90f;
        pos += Quaternion.Euler(0, angle - 45f, 0) * Vector3.forward * straightLength;

        // Straight section 2
        for (int i = 0; i < straightCount / 2; i++)
        {
            PlaceRoadPiece(straightPrefab, trackParent.transform, pos, angle);
            pos += Quaternion.Euler(0, angle, 0) * Vector3.forward * straightLength;
        }

        // Corner 2
        PlaceRoadPiece(cornerPrefab, trackParent.transform, pos, angle);
        angle += 90f;
        pos += Quaternion.Euler(0, angle - 45f, 0) * Vector3.forward * straightLength;

        // Straight section 3
        for (int i = 0; i < straightCount; i++)
        {
            PlaceRoadPiece(straightPrefab, trackParent.transform, pos, angle);
            pos += Quaternion.Euler(0, angle, 0) * Vector3.forward * straightLength;
        }

        // Corner 3
        PlaceRoadPiece(cornerPrefab, trackParent.transform, pos, angle);
        angle += 90f;
        pos += Quaternion.Euler(0, angle - 45f, 0) * Vector3.forward * straightLength;

        // Straight section 4
        for (int i = 0; i < straightCount / 2; i++)
        {
            PlaceRoadPiece(straightPrefab, trackParent.transform, pos, angle);
            pos += Quaternion.Euler(0, angle, 0) * Vector3.forward * straightLength;
        }

        // Corner 4
        PlaceRoadPiece(cornerPrefab, trackParent.transform, pos, angle);
    }

    static void PlaceRoadPiece(GameObject prefab, Transform parent, Vector3 position, float yAngle)
    {
        var piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        piece.transform.SetParent(parent);
        piece.transform.position = position;
        piece.transform.rotation = Quaternion.Euler(0f, yAngle, 0f);

        // Add MeshCollider to all child meshes if not present
        foreach (var mf in piece.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<Collider>() == null)
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }
    }

    // --- UI HELPER METHODS ---

    static GameObject CreatePanel(Transform parent, string name, bool stretchFill)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // transparent by default

        if (stretchFill)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return go;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 60f);
        rt.anchoredPosition = anchoredPos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // Button label
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }
}
