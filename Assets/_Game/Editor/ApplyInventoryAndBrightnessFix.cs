using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyInventoryAndBrightnessFix
    {
        [MenuItem("Game/UI/Apply Inventory And Brightness Fix")]
        public static void Execute()
        {
            Debug.Log("[BrightnessFix] Starting execution...");

            // 1. PvE_Gameplay
            ApplyToPvEGameplay();

            // 2. MainMenu
            ApplyToMainMenu();

            // Re-open PvE_Gameplay
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            // 3. Run UI Layout Check
            UiLayoutChecker.RunCheckMenuItem();
            Debug.Log("[BrightnessFix] Finished successfully!");
        }

        private static GameObject FindInScene(string name)
        {
            var allTfs = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTfs)
            {
                if (t.gameObject.name == name) return t.gameObject;
            }
            return null;
        }

        public static void ApplyToPvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            // 1. PausePanel
            var pauseGo = FindInScene("PausePanel");
            if (pauseGo != null)
            {
                var rootImg = pauseGo.GetComponent<Image>();
                if (rootImg != null)
                {
                    // Dark semi-transparent scrim, not pitch-black
                    rootImg.color = new Color(0f, 0f, 0f, 0.55f);
                    EditorUtility.SetDirty(rootImg);
                }

                Transform box = pauseGo.transform.Find("Box") ?? pauseGo.transform;
                var boxImg = box.GetComponent<Image>();
                if (boxImg != null)
                {
                    boxImg.color = Color.white;
                    EditorUtility.SetDirty(boxImg);
                }

                // Brighten Pause buttons
                var buttons = pauseGo.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    var img = btn.GetComponent<Image>();
                    var txt = btn.GetComponentInChildren<Text>(true);
                    if (txt != null)
                    {
                        txt.color = Color.white;
                        txt.fontSize = Mathf.Max(32, txt.fontSize);
                        txt.fontStyle = FontStyle.Bold;
                        EditorUtility.SetDirty(txt);
                    }

                    if (img != null)
                    {
                        string bName = btn.gameObject.name.ToLowerInvariant();
                        if (bName.Contains("resume"))
                        {
                            img.color = new Color(0.20f, 0.58f, 0.75f, 1f); // Oceanic teal
                        }
                        else if (bName.Contains("settings"))
                        {
                            img.color = new Color(0.32f, 0.48f, 0.65f, 1f); // Slate steel
                        }
                        else if (bName.Contains("mainmenu") || bName.Contains("menu") || bName.Contains("quit"))
                        {
                            img.color = new Color(0.80f, 0.30f, 0.30f, 1f); // Rust red
                        }
                        else
                        {
                            img.color = new Color(0.25f, 0.48f, 0.70f, 1f);
                        }
                        EditorUtility.SetDirty(img);
                    }
                }
                Debug.Log($"[BrightnessFix] Updated PausePanel with {buttons.Length} buttons.");
            }

            // 2. SettingsPanel
            var settingsGo = FindInScene("SettingsPanel");
            if (settingsGo != null)
            {
                Transform box = settingsGo.transform.Find("Root/Box") ?? settingsGo.transform.Find("Box") ?? settingsGo.transform;
                var boxImg = box.GetComponent<Image>();
                if (boxImg != null)
                {
                    boxImg.color = Color.white;
                    EditorUtility.SetDirty(boxImg);
                }
                Debug.Log("[BrightnessFix] Updated SettingsPanel box color.");
            }

            // 3. QuestPanel
            var questGo = FindInScene("QuestPanel");
            if (questGo != null)
            {
                Transform box = questGo.transform.Find("Box") ?? questGo.transform;
                var boxImg = box.GetComponent<Image>();
                if (boxImg != null)
                {
                    boxImg.color = Color.white;
                    EditorUtility.SetDirty(boxImg);
                }
                Debug.Log("[BrightnessFix] Updated QuestPanel box color.");
            }

            // 4. FloorSelectCanvas -> PanelRoot
            var floorCanvas = FindInScene("FloorSelectCanvas");
            if (floorCanvas != null)
            {
                var panelRoot = floorCanvas.transform.Find("PanelRoot");
                if (panelRoot != null)
                {
                    var prImg = panelRoot.GetComponent<Image>();
                    if (prImg != null)
                    {
                        prImg.color = Color.white;
                        EditorUtility.SetDirty(prImg);
                    }
                }
                Debug.Log("[BrightnessFix] Updated FloorSelectCanvas panelRoot color.");
            }

            // 5. InventoryPanel
            var invGo = FindInScene("InventoryPanel");
            if (invGo != null)
            {
                var invImg = invGo.GetComponent<Image>();
                if (invImg != null)
                {
                    invImg.color = Color.white;
                    EditorUtility.SetDirty(invImg);
                }
                Debug.Log("[BrightnessFix] Updated InventoryPanel color.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BrightnessFix] Saved PvE_Gameplay scene.");
        }

        public static void ApplyToMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var settingsGo = FindInScene("SettingsPanel");
            if (settingsGo != null)
            {
                Transform box = settingsGo.transform.Find("Root/Box") ?? settingsGo.transform.Find("Box") ?? settingsGo.transform;
                var boxImg = box.GetComponent<Image>();
                if (boxImg != null)
                {
                    boxImg.color = Color.white;
                    EditorUtility.SetDirty(boxImg);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BrightnessFix] Saved MainMenu scene.");
        }
    }
}
