using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyFiveTasks
    {
        [MenuItem("Game/UI/Apply 5 Tasks")]
        public static void Execute()
        {
            // 1. Configure TextureImporters in Resources/UISkin
            ConfigureUISkinSprites();

            // 2. Wire PvE_Gameplay scene
            WirePvEGameplayScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Run Layout Check
            UiLayoutChecker.RunCheckMenuItem();
            Debug.Log("[ApplyFiveTasks] Đã thực hiện xong 5 việc và kiểm tra bố cục.");
        }

        private static void ConfigureUISkinSprites()
        {
            SetBorder("Assets/_Game/Resources/UISkin/UI_Slot.png", new Vector4(5, 5, 5, 5));
            SetBorder("Assets/_Game/Resources/UISkin/UI_Row.png", new Vector4(8, 4, 8, 4));
            SetBorder("Assets/_Game/Resources/UISkin/UI_Header.png", new Vector4(16, 6, 16, 6));
            SetBorder("Assets/_Game/Resources/UISkin/UI_Toast.png", new Vector4(8, 6, 8, 6));
            SetIcon("Assets/_Game/Resources/UISkin/Icon_Coin.png");
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

        private static void SetIcon(string path)
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

        private static void WirePvEGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;

            // ── VIỆC 1: CharacterButton bỏ nhãn chữ "Nhân vật", chỉ còn Icon ────────
            var charBtn = safeAreaRoot.Find("CharacterButton");
            if (charBtn != null)
            {
                for (int i = charBtn.childCount - 1; i >= 0; i--)
                {
                    var child = charBtn.GetChild(i);
                    if (child.name == "Label" || child.name == "Text" || child.GetComponent<Text>() != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
                var iconTf = charBtn.Find("Icon");
                if (iconTf != null)
                {
                    iconTf.gameObject.SetActive(true);
                    var img = iconTf.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Character.png");
                        img.preserveAspect = true;
                    }
                }
                EditorUtility.SetDirty(charBtn.gameObject);
            }

            // ── VIỆC 2: Góc trên-trái: nền mờ UI_Toast + bóng chữ cho Col và Giờ ────
            var toastSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_Toast.png");
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Coin.png");
            var boldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/UI/Fonts/BeVietnamPro-Bold.ttf")
                        ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/UI/Fonts/BeVietnamPro-SemiBold.ttf");

            Color plateColor = new Color(0.06f, 0.09f, 0.12f, 0.85f);
            Color shadowColor = new Color(0f, 0f, 0f, 0.9f);
            Vector2 shadowOffset = new Vector2(2f, -2f);

            // Dòng tiền Col: WalletHud đặt tại y = -145 (cách thanh HP tại y = -124 một khoảng thở 21px)
            var walletHud = safeAreaRoot.Find("WalletHud");
            if (walletHud != null)
            {
                var wrt = walletHud.GetComponent<RectTransform>();
                wrt.anchorMin = wrt.anchorMax = wrt.pivot = new Vector2(0f, 1f);
                wrt.anchoredPosition = new Vector2(24f, -145f);
                wrt.sizeDelta = new Vector2(260f, 44f);

                // Plate
                var plateTf = walletHud.Find("Plate");
                if (plateTf == null)
                {
                    var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
                    plateGo.transform.SetParent(walletHud, false);
                    plateTf = plateGo.transform;
                }
                var prt = (RectTransform)plateTf;
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
                var pImg = plateTf.GetComponent<Image>();
                pImg.sprite = toastSprite;
                pImg.type = Image.Type.Sliced;
                pImg.color = plateColor;
                pImg.raycastTarget = false;

                // CoinIcon
                var coinTf = plateTf.Find("CoinIcon");
                if (coinTf == null)
                {
                    var coinGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
                    coinGo.transform.SetParent(plateTf, false);
                    coinTf = coinGo.transform;
                }
                var crt = (RectTransform)coinTf;
                crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(24f, 0f);
                crt.sizeDelta = new Vector2(28f, 28f);
                var cImg = coinTf.GetComponent<Image>();
                cImg.sprite = coinSprite;
                cImg.preserveAspect = true;
                cImg.raycastTarget = false;

                // ColLabel
                var labelTf = plateTf.Find("ColLabel");
                if (labelTf == null)
                {
                    var labelGo = new GameObject("ColLabel", typeof(RectTransform), typeof(Text), typeof(Shadow));
                    labelGo.transform.SetParent(plateTf, false);
                    labelTf = labelGo.transform;
                }
                var lrt = (RectTransform)labelTf;
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(46f, 0f);
                lrt.offsetMax = new Vector2(-10f, 0f);
                var lblTxt = labelTf.GetComponent<Text>();
                lblTxt.font = boldFont;
                lblTxt.fontSize = 26;
                lblTxt.color = new Color(0.95f, 0.76f, 0.31f);
                lblTxt.alignment = TextAnchor.MiddleLeft;
                lblTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                lblTxt.raycastTarget = false;
                var sh = labelTf.GetComponent<Shadow>() ?? labelTf.gameObject.AddComponent<Shadow>();
                sh.effectColor = shadowColor;
                sh.effectDistance = shadowOffset;

                EditorUtility.SetDirty(walletHud.gameObject);
            }

            // Dòng giờ Ngày/Đêm: TimeLabel đặt tại y = -200 (dưới Col, cách Col một khoảng 11px)
            var timeLabel = safeAreaRoot.Find("TimeLabel");
            if (timeLabel != null)
            {
                var trt = timeLabel.GetComponent<RectTransform>();
                trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
                trt.anchoredPosition = new Vector2(24f, -200f);
                trt.sizeDelta = new Vector2(330f, 44f);

                // Plate (background)
                var plateTf = timeLabel.Find("Plate");
                if (plateTf == null)
                {
                    var plateGo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
                    plateGo.transform.SetParent(timeLabel, false);
                    plateGo.transform.SetAsFirstSibling();
                    plateTf = plateGo.transform;
                }
                var prt = (RectTransform)plateTf;
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
                var pImg = plateTf.GetComponent<Image>();
                pImg.sprite = toastSprite;
                pImg.type = Image.Type.Sliced;
                pImg.color = plateColor;
                pImg.raycastTarget = false;

                // PhaseIcon
                var phaseTf = timeLabel.Find("PhaseIcon");
                if (phaseTf != null)
                {
                    var prtIcon = (RectTransform)phaseTf;
                    prtIcon.anchorMin = prtIcon.anchorMax = new Vector2(0f, 0.5f);
                    prtIcon.pivot = new Vector2(0.5f, 0.5f);
                    prtIcon.anchoredPosition = new Vector2(24f, 0f);
                    prtIcon.sizeDelta = new Vector2(28f, 28f);
                }

                // Text
                var txt = timeLabel.GetComponent<Text>();
                if (txt != null)
                {
                    txt.font = boldFont;
                    txt.fontSize = 24;
                    txt.alignment = TextAnchor.MiddleLeft;
                    var sh = timeLabel.GetComponent<Shadow>() ?? timeLabel.gameObject.AddComponent<Shadow>();
                    sh.effectColor = shadowColor;
                    sh.effectDistance = shadowOffset;
                }

                EditorUtility.SetDirty(timeLabel.gameObject);
            }

            // ── VIỆC 5: Đổi cỡ nút Đánh 190 -> 192 và Né/Kỹ năng 140 -> 144 ────────
            ResizeButton(safeAreaRoot, "AttackButton", 192f);
            ResizeButton(safeAreaRoot, "DodgeButton", 144f);
            ResizeButton(safeAreaRoot, "TechniqueButton", 144f);
            ResizeButton(safeAreaRoot, "TechniqueButton (1)", 144f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyFiveTasks] Đã lưu scene PvE_Gameplay.");
        }

        private static void ResizeButton(Transform root, string btnName, float size)
        {
            var btnTf = root.Find(btnName);
            if (btnTf != null)
            {
                var rt = btnTf.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(size, size);
                EditorUtility.SetDirty(btnTf.gameObject);
            }
        }
    }
}