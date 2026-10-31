using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU5
    {
        [MenuItem("Game/UI/Apply Package U5")]
        public static void Execute()
        {
            ApplyToPvEGameplay();
            ApplyToMainMenu();
            ApplyToPrefabRow();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU5] Successfully applied Package U5 (Typography, Outlines, Touch Targets)!");
        }

        public static void ApplyToPvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            ProcessAllCanvasesInActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU5] Processed typography and touch targets for PvE_Gameplay.");
        }

        public static void ApplyToMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);
            ProcessAllCanvasesInActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU5] Processed typography and touch targets for MainMenu.");
        }

        public static void ApplyToPrefabRow()
        {
            string prefabPath = "Assets/_Game/Prefabs/UI/FloorSelectRow.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ProcessGameObjectTextsAndButtons(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[ApplyPackageU5] Processed FloorSelectRow.prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ProcessAllCanvasesInActiveScene()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                ProcessGameObjectTextsAndButtons(c.gameObject);
            }
        }

        public static void ProcessGameObjectTextsAndButtons(GameObject root)
        {
            // 1. Process all Text components
            var texts = root.GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                txt.resizeTextForBestFit = false; // Không dùng Best Fit

                bool isButtonChild = txt.GetComponentInParent<Button>(true) != null;
                string lowerName = txt.gameObject.name.ToLowerInvariant();
                string parentName = txt.transform.parent != null ? txt.transform.parent.name.ToLowerInvariant() : "";

                if (lowerName.Contains("title") || lowerName.Contains("banner") || parentName.Contains("title") || lowerName.Contains("header"))
                {
                    txt.fontSize = Mathf.Max(44, txt.fontSize);
                    txt.fontStyle = FontStyle.Bold;
                }
                else if (isButtonChild || lowerName.Contains("btn") || lowerName.Contains("button"))
                {
                    txt.fontSize = Mathf.Max(30, txt.fontSize);
                }
                else if (lowerName.Contains("hp") || lowerName.Contains("level") || lowerName.Contains("exp") || lowerName.Contains("time") || lowerName.Contains("col"))
                {
                    txt.fontSize = Mathf.Max(28, txt.fontSize);
                }
                else if (lowerName.Contains("body") || lowerName.Contains("desc") || lowerName.Contains("status") || lowerName.Contains("message") || lowerName.Contains("details"))
                {
                    txt.fontSize = Mathf.Max(28, txt.fontSize);
                }
                else
                {
                    txt.fontSize = Mathf.Max(24, txt.fontSize);
                }

                // Đảm bảo có Shadow hoặc Outline để đọc được cả ngày lẫn đêm
                var shadow = txt.GetComponent<Shadow>();
                var outline = txt.GetComponent<Outline>();
                if (shadow == null && outline == null)
                {
                    var sh = txt.gameObject.AddComponent<Shadow>();
                    sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    sh.effectDistance = new Vector2(2f, -2f);
                    sh.useGraphicAlpha = true;
                }

                EditorUtility.SetDirty(txt);
            }

            // 2. Process all Button touch targets
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var rt = b.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 size = rt.rect.size;
                    float padX = Mathf.Max(0f, (UiLayoutConstants.MinButtonSize - size.x) / 2f);
                    float padY = Mathf.Max(0f, (UiLayoutConstants.MinButtonSize - size.y) / 2f);
                    if (padX > 0.5f || padY > 0.5f)
                    {
                        var g = b.targetGraphic;
                        if (g != null)
                        {
                            g.raycastPadding = new Vector4(-padX, -padY, -padX, -padY);
                            EditorUtility.SetDirty(g);
                        }
                    }
                }
            }
        }
    }
}
