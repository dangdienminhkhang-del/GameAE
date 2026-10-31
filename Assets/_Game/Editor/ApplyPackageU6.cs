using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU6
    {
        private const string PathButtonSprite = "Assets/_Game/Art/UI/UI_Button.png";
        private const string PathPanelSprite = "Assets/_Game/Art/UI/UI_Panel.png";
        private const string PathFrameSprite = "Assets/_Game/Art/UI/UI_BarFrame.png";
        private const string PathKnobSprite = "Assets/_Game/Art/UI/UI_Knob.png";
        private const string PathBarFillSprite = "Assets/_Game/Art/UI/UI_BarFill.png";

        [MenuItem("Game/UI/Apply Package U6")]
        public static void Execute()
        {
            var btnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathButtonSprite);
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathPanelSprite);
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathFrameSprite);
            var knobSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathKnobSprite);
            var barFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathBarFillSprite);

            ApplyToPvEGameplay(btnSprite, panelSprite, frameSprite, knobSprite, barFillSprite);
            ApplyToMainMenu(btnSprite, panelSprite, frameSprite);
            ApplyToPrefabRow(btnSprite, frameSprite);

            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU6] Successfully applied Package U6 (Sprites & 9-Slice Styling)!");
        }

        private static void ApplySliced(Image img, Sprite sprite, float artPixelMultiplier = 0.5f)
        {
            if (img == null || sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = artPixelMultiplier;
            EditorUtility.SetDirty(img);
        }

        public static void ApplyToPvEGameplay(Sprite btnSprite, Sprite panelSprite, Sprite frameSprite, Sprite knobSprite, Sprite barFillSprite)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            // 1. Joystick
            var joyPanel = GameObject.Find("JoystickPanel");
            if (joyPanel != null)
            {
                var img = joyPanel.GetComponent<Image>();
                if (img != null)
                {
                    ApplySliced(img, frameSprite, 0.33f);
                    img.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
                }

                var handle = joyPanel.transform.Find("Handle");
                if (handle != null)
                {
                    var hImg = handle.GetComponent<Image>();
                    if (hImg != null)
                    {
                        if (knobSprite != null)
                        {
                            hImg.sprite = knobSprite;
                            hImg.type = Image.Type.Simple;
                        }
                        else
                        {
                            ApplySliced(hImg, btnSprite, 0.5f);
                        }
                        hImg.color = new Color(0.9f, 0.95f, 1f, 0.85f);
                        EditorUtility.SetDirty(hImg);
                    }
                }
            }

            // 2. Buttons in scene
            var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allButtons)
            {
                var img = b.GetComponent<Image>();
                if (img == null) img = b.targetGraphic as Image;
                if (img != null)
                {
                    ApplySliced(img, btnSprite, 0.5f);
                    // Keep hue if colored, or ensure nice contrast
                    string name = b.gameObject.name.ToLowerInvariant();
                    if (name.Contains("attack"))
                    {
                        img.color = new Color(0.85f, 0.22f, 0.22f, 0.95f);
                    }
                    else if (name.Contains("dodge"))
                    {
                        img.color = new Color(0.2f, 0.55f, 0.85f, 0.95f);
                    }
                    else if (name.Contains("technique"))
                    {
                        img.color = new Color(0.6f, 0.25f, 0.85f, 0.95f);
                    }
                    else if (name.Contains("potion"))
                    {
                        img.color = new Color(0.2f, 0.75f, 0.35f, 0.95f);
                    }
                    else if (name.Contains("close") || name.Contains("btnclose") || name == "x")
                    {
                        img.color = new Color(0.8f, 0.2f, 0.2f, 0.95f);
                    }
                    else if (img.color.a < 0.1f || (img.color.r > 0.95f && img.color.g > 0.95f && img.color.b > 0.95f))
                    {
                        img.color = new Color(0.3f, 0.35f, 0.45f, 0.95f);
                    }
                }
            }

            // 3. Panels & Boxes
            string[] panelNames = { "SettingsPanel", "PausePanel", "QuestPanel", "PanelRoot" };
            foreach (var pName in panelNames)
            {
                var go = GameObject.Find(pName);
                if (go != null)
                {
                    Transform box = go.transform.Find("Box") ?? go.transform;
                    var img = box.GetComponent<Image>();
                    if (img != null)
                    {
                        ApplySliced(img, panelSprite, 0.33f);
                        img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU6] Applied sprites to PvE_Gameplay.");
        }

        public static void ApplyToMainMenu(Sprite btnSprite, Sprite panelSprite, Sprite frameSprite)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in allButtons)
            {
                var img = b.GetComponent<Image>();
                if (img == null) img = b.targetGraphic as Image;
                if (img != null)
                {
                    ApplySliced(img, btnSprite, 0.5f);
                    string name = b.gameObject.name.ToLowerInvariant();
                    if (name.Contains("playpve"))
                    {
                        img.color = new Color(0.18f, 0.6f, 0.35f, 0.95f);
                    }
                    else if (name.Contains("playpvp"))
                    {
                        img.color = new Color(0.2f, 0.45f, 0.75f, 0.95f);
                    }
                    else if (name.Contains("settings"))
                    {
                        img.color = new Color(0.35f, 0.4f, 0.48f, 0.95f);
                    }
                    else if (name.Contains("quit"))
                    {
                        img.color = new Color(0.55f, 0.22f, 0.22f, 0.95f);
                    }
                    else if (name.Contains("resetguest"))
                    {
                        img.color = new Color(0.42f, 0.42f, 0.48f, 0.92f);
                    }
                    else if (name.Contains("close") || name == "x")
                    {
                        img.color = new Color(0.8f, 0.2f, 0.2f, 0.95f);
                    }
                }
            }

            var settingsPanel = GameObject.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                Transform box = settingsPanel.transform.Find("Box") ?? settingsPanel.transform;
                var img = box.GetComponent<Image>();
                if (img != null)
                {
                    ApplySliced(img, panelSprite, 0.33f);
                    img.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU6] Applied sprites to MainMenu.");
        }

        public static void ApplyToPrefabRow(Sprite btnSprite, Sprite frameSprite)
        {
            string prefabPath = "Assets/_Game/Prefabs/UI/FloorSelectRow.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var buttons = root.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    var img = b.GetComponent<Image>();
                    if (img != null)
                    {
                        ApplySliced(img, btnSprite, 0.5f);
                        img.color = new Color(0.2f, 0.55f, 0.85f, 0.95f);
                    }
                }

                var rowImg = root.GetComponent<Image>();
                if (rowImg != null)
                {
                    ApplySliced(rowImg, frameSprite, 0.33f);
                    rowImg.color = new Color(0.15f, 0.18f, 0.24f, 0.9f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[ApplyPackageU6] Applied sprites to FloorSelectRow.prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
