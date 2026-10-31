using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Time;
using Game.UI.Controls;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class ApplyBatchFRunner
    {
        static ApplyBatchFRunner()
        {
            EditorApplication.delayCall += RunAuto;
        }

        private static void RunAuto()
        {
            if (!File.Exists("Screenshots/PvE_Gameplay_1920x1080.png") || !File.Exists("Screenshots/PvE_Gameplay_2400x1080.png"))
            {
                ApplyBatchF.Execute();
                ApplyBatchF.CaptureScreenshots();
            }
        }
    }

    public static class ApplyBatchF
    {
        [MenuItem("Game/UI/Apply Batch F")]
        public static void Execute()
        {
            // 1. Configure TextureImporters in Art/UI and Resources/UISkin
            ConfigureUISkinSprites();

            // 2. Wire PvE_Gameplay scene
            WirePvEGameplayScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Run Layout Check
            UiLayoutChecker.RunCheckMenuItem();
            Debug.Log("[ApplyBatchF] Đã thực hiện xong Lô F và kiểm tra bố cục.");
        }

        [MenuItem("Game/UI/Capture Batch F Screenshots")]
        public static void CaptureScreenshots()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                Debug.LogError("[ApplyBatchF] Không tìm thấy Canvas để chụp ảnh!");
                return;
            }
            var canvas = canvasGo.GetComponent<Canvas>();
            var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("[ApplyBatchF] Không tìm thấy Camera!");
                return;
            }

            Directory.CreateDirectory("Screenshots");

            CaptureAtResolution(canvas, cam, 1920, 1080, "Screenshots/PvE_Gameplay_1920x1080.png");
            CaptureAtResolution(canvas, cam, 2400, 1080, "Screenshots/PvE_Gameplay_2400x1080.png");
        }

        private static void CaptureAtResolution(Canvas canvas, Camera cam, int width, int height, string filePath)
        {
            var origRenderMode = canvas.renderMode;
            var origCam = canvas.worldCamera;
            var origPlaneDist = canvas.planeDistance;
            var prevActive = RenderTexture.active;
            var prevTarget = cam.targetTexture;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.Create();

            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;

                cam.targetTexture = rt;
                RenderTexture.active = rt;

                Canvas.ForceUpdateCanvases();
                cam.Render();

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = ImageConversion.EncodeToPNG(tex);
                File.WriteAllBytes(filePath, bytes);
                Object.DestroyImmediate(tex);

                Debug.Log($"[ApplyBatchF] Đã chụp ảnh màn hình: {filePath} ({width}x{height})");
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                rt.Release();
                Object.DestroyImmediate(rt);

                canvas.renderMode = origRenderMode;
                canvas.worldCamera = origCam;
                canvas.planeDistance = origPlaneDist;
            }
        }

        public static void ConfigureUISkinSprites()
        {
            // 9-slice IconButton (viền 5, 5, 5, 5)
            SetBorder("Assets/_Game/Art/UI/UI_IconButton.png", new Vector4(5, 5, 5, 5));
            SetBorder("Assets/_Game/Resources/UISkin/UI_IconButton.png", new Vector4(5, 5, 5, 5));

            // Circular action rings (không 9-slice)
            SetActionRing("Assets/_Game/Art/UI/UI_ActionRing_L.png");
            SetActionRing("Assets/_Game/Art/UI/UI_ActionRing_M.png");
            SetActionRing("Assets/_Game/Art/UI/UI_ActionRing_S.png");
            SetActionRing("Assets/_Game/Resources/UISkin/UI_ActionRing_L.png");
            SetActionRing("Assets/_Game/Resources/UISkin/UI_ActionRing_M.png");
            SetActionRing("Assets/_Game/Resources/UISkin/UI_ActionRing_S.png");

            // Menu icons
            string[] menuIcons = {
                "Icon_Menu_Bag", "Icon_Menu_Character", "Icon_Menu_Floor",
                "Icon_Menu_Pause", "Icon_Menu_Quest", "Icon_Menu_Settings",
                "Icon_Menu_Talk", "Icon_Close"
            };
            foreach (var name in menuIcons)
            {
                SetIcon($"Assets/_Game/Art/UI/{name}.png");
                SetIcon($"Assets/_Game/Resources/UISkin/{name}.png");
            }

            // Combat icons
            string[] combatIcons = {
                "Icon_Combat_Attack", "Icon_Combat_Core", "Icon_Combat_Dodge", "Icon_Combat_Traverse",
                "Icon_Ultimate", "Icon_UltimateRing"
            };
            foreach (var name in combatIcons)
            {
                SetIcon($"Assets/_Game/Art/UI/{name}.png");
                SetIcon($"Assets/_Game/Resources/UISkin/{name}.png");
            }

            // Time icons
            string[] timeIcons = {
                "Icon_Time_Dawn", "Icon_Time_Morning", "Icon_Time_Noon",
                "Icon_Time_Afternoon", "Icon_Time_Evening", "Icon_Time_Night"
            };
            foreach (var name in timeIcons)
            {
                SetIcon($"Assets/_Game/Art/UI/{name}.png");
                SetIcon($"Assets/_Game/Resources/UISkin/{name}.png");
            }

            // Consumable potion
            SetIcon("Assets/_Game/Art/Items/Item_Consumable_HealthPotion.png");
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

        private static void SetActionRing(string path)
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

        public static void WirePvEGameplayScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            var safeAreaRoot = canvas.transform.Find("SafeAreaRoot") ?? canvas.transform;

            // Sprites
            var uiIconButton = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_IconButton.png");
            var actionRingL = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_L.png");
            var actionRingM = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_M.png");
            var actionRingS = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_ActionRing_S.png");
            var uiCooldown = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/UI_Cooldown.png");

            var iconBag = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Bag.png");
            var iconFloor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Floor.png");
            var iconPause = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Pause.png");
            var iconTalk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Talk.png");
            var iconQuest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Quest.png");
            var iconChar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Menu_Character.png");

            var iconAttack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Attack.png");
            var iconDodge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Dodge.png");
            var iconCore = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Core.png");
            var iconTraverse = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Combat_Traverse.png");
            var iconPotion = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/Item_Consumable_HealthPotion.png");

            var timeDawn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Dawn.png");
            var timeMorning = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Morning.png");
            var timeNoon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Noon.png");
            var timeAfternoon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Afternoon.png");
            var timeEvening = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Evening.png");
            var timeNight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Icon_Time_Night.png");

            var boldFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/UI/Fonts/BeVietnamPro-Bold.ttf")
                        ?? AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Art/UI/Fonts/BeVietnamPro-SemiBold.ttf");

            // ── VIỆC 1: Nút Menu (UI_IconButton, không nhuộm màu, icon 84x84, nút 120x120) ──
            var menuButtons = new (string name, Sprite icon)[]
            {
                ("InventoryButton", iconBag),
                ("FloorButton", iconFloor),
                ("PauseButton", iconPause),
                ("TalkButton", iconTalk),
                ("QuestButton", iconQuest),
                ("CharacterButton", iconChar)
            };

            foreach (var (btnName, iconSp) in menuButtons)
            {
                var btnTf = safeAreaRoot.Find(btnName);
                if (btnTf == null) continue;

                var rt = btnTf.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(120f, 120f);

                // Background Image: UI_IconButton (Sliced, Color.white, multiplier 1/3)
                var bgImg = btnTf.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = uiIconButton;
                    bgImg.type = Image.Type.Sliced;
                    bgImg.pixelsPerUnitMultiplier = 1f / 3f;
                    bgImg.color = Color.white;
                }

                // Button colors: pressed x 0.85, disabled x 0.5 (alpha >= 0.7)
                var btn = btnTf.GetComponent<Button>();
                if (btn != null)
                {
                    var colors = btn.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
                    colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                    colors.selectedColor = Color.white;
                    colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                    btn.colors = colors;
                }

                // HudButtonFeedback
                var feedback = btnTf.GetComponent<HudButtonFeedback>();
                if (feedback != null)
                {
                    var soF = new SerializedObject(feedback);
                    var pTint = soF.FindProperty("disabledIconTint");
                    if (pTint != null) pTint.colorValue = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                    var pScale = soF.FindProperty("pressedScale");
                    if (pScale != null) pScale.floatValue = 0.85f;
                    soF.ApplyModifiedProperties();
                }

                // Xóa mọi nhãn chữ con
                for (int i = btnTf.childCount - 1; i >= 0; i--)
                {
                    var child = btnTf.GetChild(i);
                    if (child.name == "Label" || child.name == "Text" || child.name == "Text (Legacy)" || child.GetComponent<Text>() != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                // Icon con 84x84 ở giữa nút
                var iconTf = btnTf.Find("Icon");
                if (iconTf == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(btnTf, false);
                    iconTf = iconGo.transform;
                }
                var iconRt = (RectTransform)iconTf;
                iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(84f, 84f);
                var iconImg = iconTf.GetComponent<Image>();
                iconImg.sprite = iconSp;
                iconImg.type = Image.Type.Simple;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconTf.gameObject.SetActive(true);

                EditorUtility.SetDirty(btnTf.gameObject);
            }

            // ── VIỆC 2 & 3: Nút Chiến Đấu ──────────────────────────────────────
            // 2.1 AttackButton: UI_ActionRing_L (192) + Icon (120)
            var attackTf = safeAreaRoot.Find("AttackButton");
            if (attackTf != null)
            {
                var rt = (RectTransform)attackTf;
                rt.sizeDelta = new Vector2(192f, 192f);

                var bgImg = attackTf.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = actionRingL;
                    bgImg.type = Image.Type.Simple;
                    bgImg.color = Color.white;
                    bgImg.preserveAspect = true;
                }

                ApplyButtonColorsAndFeedback(attackTf);

                var iconTf = attackTf.Find("Icon");
                if (iconTf == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(attackTf, false);
                    iconTf = iconGo.transform;
                }
                var iconRt = (RectTransform)iconTf;
                iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(120f, 120f);
                var iconImg = iconTf.GetComponent<Image>();
                iconImg.sprite = iconAttack;
                iconImg.type = Image.Type.Simple;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                EditorUtility.SetDirty(attackTf.gameObject);
            }

            // 2.2 DodgeButton: UI_ActionRing_M (144) + Icon (96) + CooldownFill (96)
            WireActionRingMButton(safeAreaRoot, "DodgeButton", actionRingM, iconDodge, uiCooldown);

            // 2.3 TechniqueButton: UI_ActionRing_M (144) + Icon (96) + CooldownFill (96)
            WireActionRingMButton(safeAreaRoot, "TechniqueButton", actionRingM, iconCore, uiCooldown);

            // 2.4 TechniqueButton (1): UI_ActionRing_M (144) + Icon (96) + CooldownFill (96)
            WireActionRingMButton(safeAreaRoot, "TechniqueButton (1)", actionRingM, iconTraverse, uiCooldown);

            // 2.5 PotionButton: UI_ActionRing_S (120) + Item_Consumable_HealthPotion (84)
            var potionTf = safeAreaRoot.Find("PotionButton");
            if (potionTf != null)
            {
                var rt = (RectTransform)potionTf;
                rt.sizeDelta = new Vector2(120f, 120f);

                var bgImg = potionTf.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = actionRingS;
                    bgImg.type = Image.Type.Simple;
                    bgImg.color = Color.white;
                    bgImg.preserveAspect = true;
                }

                ApplyButtonColorsAndFeedback(potionTf);

                var iconTf = potionTf.Find("Icon");
                if (iconTf == null)
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(potionTf, false);
                    iconTf = iconGo.transform;
                }
                var iconRt = (RectTransform)iconTf;
                iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(84f, 84f);
                var iconImg = iconTf.GetComponent<Image>();
                iconImg.sprite = iconPotion;
                iconImg.type = Image.Type.Simple;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                EditorUtility.SetDirty(potionTf.gameObject);
            }

            // ── VIỆC 4: Đồng hồ Ngày/Đêm (TimeOfDayHUD, PhaseIcon 72x72, 6 pha) ──
            var timeLabel = safeAreaRoot.Find("TimeLabel");
            if (timeLabel != null && timeLabel.Find("Capsule") == null) // bỏ qua nếu TimeLabel đã dùng bố cục SAO mới
            {
                var trt = timeLabel.GetComponent<RectTransform>();
                trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
                trt.anchoredPosition = new Vector2(24f, -205f);
                trt.sizeDelta = new Vector2(380f, 52f);

                // Plate
                var plateTf = timeLabel.Find("Plate");
                if (plateTf != null)
                {
                    var prt = (RectTransform)plateTf;
                    prt.anchorMin = Vector2.zero;
                    prt.anchorMax = Vector2.one;
                    prt.offsetMin = Vector2.zero;
                    prt.offsetMax = Vector2.zero;
                }

                // PhaseIcon: icon 72
                var phaseTf = timeLabel.Find("PhaseIcon");
                if (phaseTf == null)
                {
                    var phaseGo = new GameObject("PhaseIcon", typeof(RectTransform), typeof(Image));
                    phaseGo.transform.SetParent(timeLabel, false);
                    phaseTf = phaseGo.transform;
                }
                var prtIcon = (RectTransform)phaseTf;
                prtIcon.anchorMin = prtIcon.anchorMax = new Vector2(0f, 0.5f);
                prtIcon.pivot = new Vector2(0.5f, 0.5f);
                prtIcon.anchoredPosition = new Vector2(40f, 0f);
                prtIcon.sizeDelta = new Vector2(72f, 72f);
                var pImg = phaseTf.GetComponent<Image>();
                pImg.sprite = timeMorning;
                pImg.preserveAspect = true;
                pImg.raycastTarget = false;
                phaseTf.gameObject.SetActive(true);

                // Text
                var txt = timeLabel.GetComponent<Text>();
                if (txt != null)
                {
                    txt.font = boldFont;
                    txt.fontSize = 24;
                    txt.alignment = TextAnchor.MiddleLeft;
                }

                // Cài đặt 6 pha vào TimeOfDayHUD
                var hud = timeLabel.GetComponent<TimeOfDayHUD>();
                if (hud != null)
                {
                    var soHud = new SerializedObject(hud);
                    soHud.FindProperty("dawnIcon").objectReferenceValue = timeDawn;
                    soHud.FindProperty("morningIcon").objectReferenceValue = timeMorning;
                    soHud.FindProperty("noonIcon").objectReferenceValue = timeNoon;
                    soHud.FindProperty("afternoonIcon").objectReferenceValue = timeAfternoon;
                    soHud.FindProperty("eveningIcon").objectReferenceValue = timeEvening;
                    soHud.FindProperty("nightIcon").objectReferenceValue = timeNight;
                    soHud.FindProperty("phaseIcon").objectReferenceValue = pImg;
                    soHud.FindProperty("bodyText").objectReferenceValue = txt;
                    soHud.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(timeLabel.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyBatchF] Đã lưu scene PvE_Gameplay.");
        }

        private static void WireActionRingMButton(Transform root, string btnName, Sprite actionRingM, Sprite iconSp, Sprite uiCooldown)
        {
            var btnTf = root.Find(btnName);
            if (btnTf == null) return;

            var rt = (RectTransform)btnTf;
            rt.sizeDelta = new Vector2(144f, 144f);

            var bgImg = btnTf.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = actionRingM;
                bgImg.type = Image.Type.Simple;
                bgImg.color = Color.white;
                bgImg.preserveAspect = true;
            }

            ApplyButtonColorsAndFeedback(btnTf);

            // Icon con 96x96
            var iconTf = btnTf.Find("Icon");
            if (iconTf == null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(btnTf, false);
                iconTf = iconGo.transform;
            }
            var iconRt = (RectTransform)iconTf;
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(96f, 96f);
            var iconImg = iconTf.GetComponent<Image>();
            iconImg.sprite = iconSp;
            iconImg.type = Image.Type.Simple;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconTf.gameObject.SetActive(true);

            // CooldownFill: 96x96, phủ Radial360 trên icon
            var cdTf = btnTf.Find("CooldownFill");
            if (cdTf != null)
            {
                var cdRt = (RectTransform)cdTf;
                cdRt.anchorMin = cdRt.anchorMax = cdRt.pivot = new Vector2(0.5f, 0.5f);
                cdRt.anchoredPosition = Vector2.zero;
                cdRt.sizeDelta = new Vector2(96f, 96f);
                var cdImg = cdTf.GetComponent<Image>();
                cdImg.sprite = uiCooldown;
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = false;
                cdImg.raycastTarget = false;

                // Đảm bảo CooldownFill nằm TRÊN Icon (sibling index sau Icon)
                cdTf.SetSiblingIndex(iconTf.GetSiblingIndex() + 1);
            }

            EditorUtility.SetDirty(btnTf.gameObject);
        }

        private static void ApplyButtonColorsAndFeedback(Transform btnTf)
        {
            var btn = btnTf.GetComponent<Button>();
            if (btn != null)
            {
                var colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                btn.colors = colors;
            }

            var feedback = btnTf.GetComponent<HudButtonFeedback>();
            if (feedback != null)
            {
                var soF = new SerializedObject(feedback);
                var pTint = soF.FindProperty("disabledIconTint");
                if (pTint != null) pTint.colorValue = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                var pScale = soF.FindProperty("pressedScale");
                if (pScale != null) pScale.floatValue = 0.85f;
                soF.ApplyModifiedProperties();
            }
        }
    }
}
