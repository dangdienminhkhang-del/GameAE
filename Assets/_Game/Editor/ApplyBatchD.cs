using System.IO;
using Game.UI.Death;
using Game.UI.Time;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyBatchD
    {
        private static readonly string[] SpritesD = {
            "Icon_Menu_Bag",
            "Icon_Menu_Talk",
            "Icon_Menu_Quest",
            "Icon_Menu_Floor",
            "Icon_Menu_Pause",
            "Icon_Menu_Character",
            "Icon_Menu_Settings",
            "Icon_Close",
            "Icon_Time_Dawn",
            "Icon_Time_Morning",
            "Icon_Time_Noon",
            "Icon_Time_Afternoon",
            "Icon_Time_Evening",
            "Icon_Time_Night",
            "Icon_Seal"
        };

        [MenuItem("Game/UI/Apply Batch D")]
        public static void Execute()
        {
            // 1. Configure all TextureImporters
            ConfigureImporters();

            // 2. Wire PvE_Gameplay scene
            WirePvEScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ApplyBatchD] Hoàn thành cấu hình và nối toàn bộ sprite Lô D vào PvE_Gameplay!");
        }

        public static void ConfigureImporters()
        {
            foreach (var name in SpritesD)
            {
                SetIconImporter($"Assets/_Game/Art/UI/{name}.png");
                SetIconImporter($"Assets/_Game/Resources/UISkin/{name}.png");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ApplyBatchD] Đã cấu hình TextureImporter cho toàn bộ sprite Lô D.");
        }

        private static void SetIconImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spriteBorder = Vector4.zero;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        public static void WirePvEScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[ApplyBatchD] Không tìm thấy Canvas!");
                return;
            }

            var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;

            // 1. Menu Buttons
            WireMenuButton(safeAreaRoot, "PauseButton", "Icon_Menu_Pause");
            WireMenuButton(safeAreaRoot, "FloorButton", "Icon_Menu_Floor");
            WireMenuButton(safeAreaRoot, "QuestButton", "Icon_Menu_Quest");
            WireMenuButton(safeAreaRoot, "InventoryButton", "Icon_Menu_Bag");
            WireMenuButton(safeAreaRoot, "CharacterButton", "Icon_Menu_Character");
            WireMenuButton(safeAreaRoot, "TalkButton", "Icon_Menu_Talk");

            // Close button in QuestPanel
            var questPanel = canvas.transform.Find("QuestPanel");
            if (questPanel != null)
            {
                WireMenuButton(questPanel, "CloseButton", "Icon_Close");
            }

            // 2. TimeOfDayHUD
            var timeTf = safeAreaRoot.Find("TimeLabel") ?? canvas.transform.Find("TimeLabel");
            if (timeTf != null)
            {
                var trt = timeTf.GetComponent<RectTransform>();
                trt.anchoredPosition = new Vector2(70f, -185f);

                var iconChild = timeTf.Find("PhaseIcon");
                if (iconChild != null && iconChild.GetComponent<RectTransform>() == null)
                {
                    Object.DestroyImmediate(iconChild.gameObject);
                    iconChild = null;
                }
                if (iconChild == null)
                {
                    var go = new GameObject("PhaseIcon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(timeTf, false);
                    iconChild = go.transform;
                }

                var irt = iconChild.GetComponent<RectTransform>() ?? iconChild.gameObject.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(36f, 36f);
                irt.anchoredPosition = new Vector2(-24f, 0f);

                var img = iconChild.GetComponent<Image>() ?? iconChild.gameObject.AddComponent<Image>();
                var morningSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Morning.png");
                img.sprite = morningSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                EditorUtility.SetDirty(img);

                var hud = timeTf.GetComponent<TimeOfDayHUD>();
                if (hud != null)
                {
                    var so = new SerializedObject(hud);
                    so.FindProperty("phaseIcon").objectReferenceValue = img;
                    so.FindProperty("dawnIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Dawn.png");
                    so.FindProperty("morningIcon").objectReferenceValue = morningSprite;
                    so.FindProperty("noonIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Noon.png");
                    so.FindProperty("afternoonIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Afternoon.png");
                    so.FindProperty("eveningIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Evening.png");
                    so.FindProperty("nightIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Night.png");
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(hud);
                }
            }

            // 3. DeathPanel Seal Emblem
            var deathPanel = canvas.transform.Find("DeathPanel");
            if (deathPanel != null)
            {
                var sealChild = deathPanel.Find("SealEmblem");
                if (sealChild != null && sealChild.GetComponent<RectTransform>() == null)
                {
                    Object.DestroyImmediate(sealChild.gameObject);
                    sealChild = null;
                }
                if (sealChild == null)
                {
                    var go = new GameObject("SealEmblem", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(deathPanel, false);
                    sealChild = go.transform;
                }

                var srt = sealChild.GetComponent<RectTransform>() ?? sealChild.gameObject.AddComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(144f, 144f);
                srt.anchoredPosition = new Vector2(0f, 260f);

                var sealImg = sealChild.GetComponent<Image>() ?? sealChild.gameObject.AddComponent<Image>();
                var sealSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Seal.png");
                sealImg.sprite = sealSprite;
                sealImg.preserveAspect = true;
                sealImg.raycastTarget = false;
                EditorUtility.SetDirty(sealImg);

                // Wire on DeathScreenController if present on Canvas
                var deathCtrl = canvas.GetComponent<DeathScreenController>();
                if (deathCtrl != null)
                {
                    var so = new SerializedObject(deathCtrl);
                    var prop = so.FindProperty("sealEmblem");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = sealImg;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(deathCtrl);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyBatchD] Đã lưu scene PvE_Gameplay.");
        }

        private static void WireMenuButton(Transform root, string btnName, string iconSpriteName)
        {
            var btnTf = root.Find(btnName);
            if (btnTf == null) return;

            // Background UI_Button
            var btnImg = btnTf.GetComponent<Image>();
            if (btnImg != null)
            {
                var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_Button.png");
                if (bgSprite != null) btnImg.sprite = bgSprite;
                btnImg.type = Image.Type.Sliced;
                EditorUtility.SetDirty(btnImg);
            }

            // Hide text label
            var txt = btnTf.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.gameObject.SetActive(false);
                EditorUtility.SetDirty(txt.gameObject);
            }

            // Create or configure child Icon
            var iconTf = btnTf.Find("Icon");
            if (iconTf != null && iconTf.GetComponent<RectTransform>() == null)
            {
                Object.DestroyImmediate(iconTf.gameObject);
                iconTf = null;
            }
            if (iconTf == null)
            {
                var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnTf, false);
                iconTf = iconObj.transform;
            }

            var irt = iconTf.GetComponent<RectTransform>() ?? iconTf.gameObject.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(72f, 72f);
            irt.anchoredPosition = Vector2.zero;

            var img = iconTf.GetComponent<Image>() ?? iconTf.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Game/Art/UI/{iconSpriteName}.png");
            img.preserveAspect = true;
            img.raycastTarget = false;
            EditorUtility.SetDirty(img);
            EditorUtility.SetDirty(iconTf.gameObject);
        }
    }
}
