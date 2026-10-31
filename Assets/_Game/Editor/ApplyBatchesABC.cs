using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyBatchesABC
    {
        [MenuItem("Game/UI/Apply Batches A B C")]
        public static void Execute()
        {
            // 1. Configure all TextureImporter settings & 9-slice borders
            ConfigureAllTextureImporters();

            // 2. Wire MainMenu
            WireMainMenu();

            // 3. Wire PvE_Gameplay
            WirePvEGameplay();

            // Return to PvE_Gameplay as active scene
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyBatchesABC] Successfully configured 9-slice borders and wired all assets for Batches A, B, C!");
        }

        public static void ConfigureAllTextureImporters()
        {
            // Batch A
            SetBorder("Assets/_Game/Art/UI/UI_Panel.png", new Vector4(8, 8, 8, 8));
            SetBorder("Assets/_Game/Art/UI/UI_Button.png", new Vector4(6, 6, 6, 6));
            SetBorder("Assets/_Game/Art/UI/UI_BarFrame.png", new Vector4(4, 4, 4, 4));
            SetBorder("Assets/_Game/Art/UI/UI_PanelInset.png", new Vector4(6, 6, 6, 6));
            SetBorder("Assets/_Game/Art/UI/UI_Header.png", new Vector4(16, 6, 16, 6));
            SetBorder("Assets/_Game/Art/UI/UI_Slot.png", new Vector4(5, 5, 5, 5));
            SetBorder("Assets/_Game/Art/UI/UI_Row.png", new Vector4(8, 4, 8, 4));
            SetBorder("Assets/_Game/Art/UI/UI_Toast.png", new Vector4(8, 6, 8, 6));
            SetBorder("Assets/_Game/Art/UI/UI_ScrollTrack.png", new Vector4(2, 4, 2, 4));
            SetBorder("Assets/_Game/Art/UI/UI_ScrollHandle.png", new Vector4(2, 4, 2, 4));
            SetBorder("Assets/_Game/Art/UI/UI_Divider.png", new Vector4(4, 1, 4, 1));

            // Batch B
            SetBorder("Assets/_Game/Art/UI/UI_TitlePlate.png", new Vector4(20, 7, 20, 7));
            SetBorder("Assets/_Game/Art/UI/UI_ButtonWide.png", new Vector4(12, 6, 12, 6));

            // Resources UISkin
            SetBorder("Assets/_Game/Resources/UISkin/UI_Panel.png", new Vector4(8, 8, 8, 8));
            SetBorder("Assets/_Game/Resources/UISkin/UI_Button.png", new Vector4(6, 6, 6, 6));
            SetBorder("Assets/_Game/Resources/UISkin/UI_BarFrame.png", new Vector4(4, 4, 4, 4));
            SetBorder("Assets/_Game/Resources/UISkin/UI_PanelInset.png", new Vector4(6, 6, 6, 6));
            SetBorder("Assets/_Game/Resources/UISkin/UI_ButtonWide.png", new Vector4(12, 6, 12, 6));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ApplyBatchesABC] Configured all TextureImporter 9-slice borders.");
        }

        private static void SetBorder(string path, Vector4 border)
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
                importer.spriteBorder = border;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        public static void WireMainMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/MainMenu.unity", OpenSceneMode.Single);

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            // 1. Background
            var bgObj = canvas.transform.Find("Background");
            if (bgObj != null)
            {
                bgObj.gameObject.SetActive(true);
                var img = bgObj.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Bg_MainMenu.png");
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                }

                var arf = bgObj.GetComponent<AspectRatioFitter>();
                if (arf != null)
                {
                    arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    arf.aspectRatio = 800f / 360f;
                    EditorUtility.SetDirty(arf);
                }
            }

            // 2. LogoEmblem
            var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;
            var emblemObj = safeAreaRoot.Find("LogoEmblem");
            if (emblemObj != null)
            {
                emblemObj.gameObject.SetActive(true);
                var img = emblemObj.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Logo_Emblem.png");
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                }
            }

            // 3. TitlePlate
            var plateObj = safeAreaRoot.Find("TitlePlate");
            if (plateObj != null)
            {
                plateObj.gameObject.SetActive(true);
                var img = plateObj.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_TitlePlate.png");
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                }
            }

            // 4. Menu Buttons: PlayPvE, PlayPvP, Settings, Quit, ResetGuestButton
            var btnWideSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ButtonWide.png");
            string[] btnNames = { "PlayPvE", "PlayPvP", "Settings", "Quit", "ResetGuestButton" };
            foreach (var bName in btnNames)
            {
                var bTf = safeAreaRoot.Find(bName);
                if (bTf != null)
                {
                    var img = bTf.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = btnWideSprite;
                        img.type = Image.Type.Sliced;
                        EditorUtility.SetDirty(img);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyBatchesABC] Wired MainMenu scene.");
        }

        public static void WirePvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;

            // 1. Joystick
            var joyPanel = safeAreaRoot.Find("JoystickPanel") ?? canvas.transform.Find("JoystickPanel");
            if (joyPanel != null)
            {
                var img = joyPanel.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_Joystick_Base.png");
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                }

                var handle = joyPanel.Find("Handle");
                if (handle != null)
                {
                    var hImg = handle.GetComponent<Image>();
                    if (hImg != null)
                    {
                        hImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_Joystick_Knob.png");
                        hImg.type = Image.Type.Simple;
                        hImg.color = Color.white;
                        EditorUtility.SetDirty(hImg);
                    }
                }
            }

            // 2. Combat Buttons & Icons
            var ringL = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_L.png");
            var ringM = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_M.png");
            var ringS = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_S.png");

            var iconAtk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Attack.png");
            var iconDdg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Dodge.png");
            var iconCore = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Core.png");
            var iconTrv = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Traverse.png");
            var iconPotion = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/Item_Consumable_HealthPotion.png");

            // AttackButton
            WireCombatButton(safeAreaRoot, "AttackButton", ringL, iconAtk, 120f);

            // DodgeButton
            WireCombatButton(safeAreaRoot, "DodgeButton", ringM, iconDdg, 96f);

            // TechniqueButton (Core - Slot 1)
            WireCombatButton(safeAreaRoot, "TechniqueButton", ringM, iconCore, 96f);

            // TechniqueButton (1) (Traverse - Slot 2)
            WireCombatButton(safeAreaRoot, "TechniqueButton (1)", ringM, iconTrv, 96f);

            // PotionButton
            WireCombatButton(safeAreaRoot, "PotionButton", ringS, iconPotion, 86f);

            // 3. WalletHud Coin Icon
            var walletHud = safeAreaRoot.Find("WalletHud");
            if (walletHud != null)
            {
                var coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Coin.png");
                var coinTf = walletHud.Find("CoinIcon") ?? walletHud.Find("Icon");
                if (coinTf != null)
                {
                    var img = coinTf.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = coinIconSprite;
                        img.type = Image.Type.Simple;
                        EditorUtility.SetDirty(img);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyBatchesABC] Wired PvE_Gameplay scene.");
        }

        private static void WireCombatButton(Transform root, string btnName, Sprite ringSprite, Sprite iconSprite, float iconSize)
        {
            var btnTf = root.Find(btnName);
            if (btnTf == null) return;

            // Set ring background
            var btnImg = btnTf.GetComponent<Image>();
            if (btnImg != null && ringSprite != null)
            {
                btnImg.sprite = ringSprite;
                btnImg.type = Image.Type.Simple;
                btnImg.color = Color.white;
                EditorUtility.SetDirty(btnImg);
            }

            // Hide/empty label text
            var txt = btnTf.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = "";
                txt.gameObject.SetActive(false);
                EditorUtility.SetDirty(txt);
            }

            // Create/update Icon child
            var iconTf = btnTf.Find("Icon");
            GameObject iconObj;
            if (iconTf == null)
            {
                iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(btnTf, false);
            }
            else
            {
                iconObj = iconTf.gameObject;
            }

            iconObj.SetActive(true);
            var rt = iconObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            var iconImg = iconObj.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.type = Image.Type.Simple;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                EditorUtility.SetDirty(iconImg);
            }
        }
    }
}
