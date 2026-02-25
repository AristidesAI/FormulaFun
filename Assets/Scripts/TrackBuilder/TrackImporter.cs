#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.IO;

namespace FormulaFun.TrackBuilder
{
    public class TrackImporter : EditorWindow
    {
        private string jsonPath = "";
        private const string MODEL_BASE = "Assets/Racing Kit/Models/FBX format/";

        [MenuItem("FormulaFun/Import Track Layout")]
        static void ShowWindow()
        {
            GetWindow<TrackImporter>("Track Importer");
        }

        void OnGUI()
        {
            GUILayout.Label("Import Track from JSON", EditorStyles.boldLabel);
            GUILayout.Space(8);

            if (GUILayout.Button("Select JSON File"))
            {
                jsonPath = EditorUtility.OpenFilePanel("Select Track JSON", "", "json");
            }

            if (!string.IsNullOrEmpty(jsonPath))
            {
                GUILayout.Label($"File: {Path.GetFileName(jsonPath)}", EditorStyles.miniLabel);
                GUILayout.Space(8);

                if (GUILayout.Button("Import Track"))
                {
                    ImportTrack(jsonPath);
                }
            }
        }

        void ImportTrack(string path)
        {
            string json = File.ReadAllText(path);
            TrackLayout layout = JsonConvert.DeserializeObject<TrackLayout>(json);

            if (layout == null || layout.Pieces == null)
            {
                Debug.LogError("Failed to parse track layout JSON");
                return;
            }

            // Create parent GameObject
            string rootName = $"Track_{layout.TrackName}";
            GameObject trackRoot = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(trackRoot, $"Import Track {layout.TrackName}");

            int placed = 0;
            int skipped = 0;

            foreach (TrackPiece piece in layout.Pieces)
            {
                string assetPath = $"{MODEL_BASE}{piece.ModelId}.fbx";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    Debug.LogWarning($"Model not found at: {assetPath}");
                    skipped++;
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(trackRoot.transform);
                instance.transform.localPosition = new Vector3(piece.GridX, 0, piece.GridZ);
                instance.transform.localRotation = Quaternion.Euler(0, -piece.RotationY, 0);
                instance.name = $"{piece.ModelId}_{piece.Id}";
                placed++;
            }

            Selection.activeGameObject = trackRoot;
            Debug.Log($"Imported track \"{layout.TrackName}\": {placed} pieces placed, {skipped} skipped");
        }
    }
}
#endif
