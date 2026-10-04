// FloorSetupApply.cs — Temporary helper to verify and save PvE_Gameplay scene after manual YAML edits.
// Run via: Tools → Floor → Apply Floor Setup
// Delete this file after confirming the setup is correct in Unity Editor.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class FloorSetupApply
    {
        [MenuItem("Tools/Floor/Apply Floor Setup (Then Delete This Script)")]
        private static void ApplyAndSave()
        {
            // Open PvE_Gameplay if not already open
            const string scenePath = "Assets/_Game/Scenes/PvE_Gameplay.unity";
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            bool hasErrors = false;

            // Verify PvEBootstrap.floorDatabase
            var bootstrapGO = GameObject.Find("PvEBootstrap");
            if (bootstrapGO == null) { Debug.LogError("[FloorSetupApply] PvEBootstrap not found!"); hasErrors = true; }
            else
            {
                var bootstrap = bootstrapGO.GetComponent<Game.PvE.Bootstrap.PvEBootstrap>();
                if (bootstrap == null) { Debug.LogError("[FloorSetupApply] PvEBootstrap component not found!"); hasErrors = true; }
                else
                {
                    var so = new SerializedObject(bootstrap);
                    var prop = so.FindProperty("floorDatabase");
                    if (prop?.objectReferenceValue == null) { Debug.LogError("[FloorSetupApply] floorDatabase is still null!"); hasErrors = true; }
                    else Debug.Log($"[FloorSetupApply] ✓ floorDatabase = {prop.objectReferenceValue.name}");
                }
            }

            // Verify FloorSelectPanel.rowPrefab & errorLabel
            var canvasGO = GameObject.Find("FloorSelectCanvas");
            if (canvasGO == null) { Debug.LogError("[FloorSetupApply] FloorSelectCanvas not found!"); hasErrors = true; }
            else
            {
                var panel = canvasGO.GetComponent<Game.UI.Floor.FloorSelectPanel>();
                if (panel == null) { Debug.LogError("[FloorSetupApply] FloorSelectPanel component not found!"); hasErrors = true; }
                else
                {
                    var so = new SerializedObject(panel);
                    var rowProp = so.FindProperty("rowPrefab");
                    var errProp = so.FindProperty("errorLabel");
                    if (rowProp?.objectReferenceValue == null) { Debug.LogError("[FloorSetupApply] rowPrefab is still null!"); hasErrors = true; }
                    else Debug.Log($"[FloorSetupApply] ✓ rowPrefab = {rowProp.objectReferenceValue.name}");
                    if (errProp?.objectReferenceValue == null) { Debug.LogError("[FloorSetupApply] errorLabel is still null!"); hasErrors = true; }
                    else Debug.Log($"[FloorSetupApply] ✓ errorLabel = {errProp.objectReferenceValue.name}");
                }
            }

            if (!hasErrors)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[FloorSetupApply] ✓ Scene saved successfully. You can delete FloorSetupApply.cs now.");
                EditorUtility.DisplayDialog("Floor Setup", "All fields verified and scene saved!", "OK");
            }
            else
            {
                Debug.LogError("[FloorSetupApply] Some fields are still unassigned. Check the Console.");
                EditorUtility.DisplayDialog("Floor Setup Error", "Some fields failed verification. Check Console.", "OK");
            }
        }
    }
}
#endif
