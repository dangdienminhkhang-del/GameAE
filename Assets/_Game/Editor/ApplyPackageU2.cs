using System.IO;
using Game.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class ApplyPackageU2
    {
        [MenuItem("Game/UI/Apply Package U2")]
        public static void Execute()
        {
            ApplyToPvEGameplay();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);
            Debug.Log("[ApplyPackageU2] Successfully applied Package U2 to PvE_Gameplay!");
        }

        public static void ApplyToPvEGameplay()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/PvE_Gameplay.unity", OpenSceneMode.Single);

            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[ApplyPackageU2] Canvas not found!");
                return;
            }

            var safeAreaRootTf = canvasObj.transform.Find("SafeAreaRoot");
            if (safeAreaRootTf == null)
            {
                Debug.LogError("[ApplyPackageU2] SafeAreaRoot not found! Run U1 first.");
                return;
            }

            // 1. CỤM NGÓN PHẢI (Góc dưới-phải: anchorMin = anchorMax = (1, 0), pivot = (0.5, 0.5))
            // Đánh (190x190) tại (-125, 125)
            SetupElement(safeAreaRootTf, "AttackButton", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-125f, 125f), new Vector2(190f, 190f), true);

            // Né (140x140) tại (-310, 105)
            SetupElement(safeAreaRootTf, "DodgeButton", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-310f, 105f), new Vector2(140f, 140f), true);

            // Kỹ năng 0 / Core (140x140) tại (-135, 315)
            SetupElement(safeAreaRootTf, "TechniqueButton", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-135f, 315f), new Vector2(140f, 140f), true);

            // Kỹ năng 1 / Traverse (140x140) tại (-305, 280)
            SetupElement(safeAreaRootTf, "TechniqueButton (1)", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-305f, 280f), new Vector2(140f, 140f), true);

            // Kỹ năng 2 & 3: chưa dùng trong Mốc 1 -> ẩn đi
            Transform tech2 = safeAreaRootTf.Find("TechniqueButton (2)");
            if (tech2 != null) tech2.gameObject.SetActive(false);
            Transform tech3 = safeAreaRootTf.Find("TechniqueButton (3)");
            if (tech3 != null) tech3.gameObject.SetActive(false);

            // Bình thuốc (120x120) tại (-480, 110)
            SetupElement(safeAreaRootTf, "PotionButton", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-480f, 110f), new Vector2(120f, 120f), true);

            // Nhãn trạng thái bình thuốc (180x40) tại (-480, 185)
            SetupElement(safeAreaRootTf, "PotionStatusLabel", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-480f, 185f), new Vector2(180f, 40f), true);

            // Nút Nói chuyện / Hành động (120x120) tại (-135, 470)
            SetupElement(safeAreaRootTf, "TalkButton", new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-135f, 470f), new Vector2(120f, 120f), true);

            // 2. CỤM NGÓN TRÁI: JoystickPanel (200x200) tại (150, 150)
            SetupElement(safeAreaRootTf, "JoystickPanel", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), new Vector2(200f, 200f), true);

            // 3. MENU GÓC TRÊN-PHẢI (2 hàng gọn gàng, nút 120x120, cách nhau 16px, lề 24px)
            // Hàng 1 (y = -24): [Tạm dừng], [Tầng], [Sổ quest]
            SetupElement(safeAreaRootTf, "PauseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(120f, 120f), true);
            SetupElement(safeAreaRootTf, "FloorButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -24f), new Vector2(120f, 120f), true);
            SetupElement(safeAreaRootTf, "QuestButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-296f, -24f), new Vector2(120f, 120f), true);

            // Hàng 2 (y = -160): [Túi], [Nhân vật]
            SetupElement(safeAreaRootTf, "InventoryButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -160f), new Vector2(120f, 120f), true);
            SetupElement(safeAreaRootTf, "CharacterButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -160f), new Vector2(120f, 120f), true);

            // Cấu hình nhãn và sự kiện cho CharacterButton nếu chưa có
            Transform charBtnTf = safeAreaRootTf.Find("CharacterButton");
            if (charBtnTf != null)
            {
                Button btn = charBtnTf.GetComponent<Button>();
                Transform labelTf = charBtnTf.Find("Text");
                if (labelTf == null)
                {
                    var labelObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    labelObj.transform.SetParent(charBtnTf, false);
                    var rt = labelObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    var txt = labelObj.GetComponent<Text>();
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    txt.fontSize = 28;
                    txt.fontStyle = FontStyle.Bold;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.white;
                    txt.text = "Nhân vật";
                }

                // Nối openButton của CharacterPanel nếu chưa có
                Transform charPanelTf = canvasObj.transform.Find("CharacterPanel");
                if (charPanelTf != null)
                {
                    var charPanel = charPanelTf.GetComponent<Game.UI.Character.CharacterPanel>();
                    if (charPanel != null)
                    {
                        var so = new SerializedObject(charPanel);
                        var sp = so.FindProperty("openButton");
                        if (sp != null && sp.objectReferenceValue == null)
                        {
                            sp.objectReferenceValue = btn;
                            so.ApplyModifiedProperties();
                        }
                    }
                }
            }

            // 4. GÓC TRÊN-TRÁI
            // PlayerHUD (420x100) tại (24, -24)
            SetupElement(safeAreaRootTf, "PlayerHUD", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 100f), true);
            // TimeLabel (360x40) tại (24, -185)
            SetupElement(safeAreaRootTf, "TimeLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -185f), new Vector2(360f, 40f), true);

            // 5. TRÊN-GIỮA
            // BossHealthBar (500x40) tại (0, -24)
            SetupElement(safeAreaRootTf, "BossHealthBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 40f), true);
            // ZoneBanner (500x60) tại (0, -75)
            SetupElement(safeAreaRootTf, "ZoneBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -75f), new Vector2(500f, 60f), true);
            // PotionToastLabel (500x50) tại (0, -145)
            SetupElement(safeAreaRootTf, "PotionToastLabel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(500f, 50f), true);

            // Cập nhật cỡ chữ trên các nút menu (Gói U5 chuẩn hóa)
            UpdateButtonFontSize(safeAreaRootTf, "PauseButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "FloorButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "QuestButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "InventoryButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "CharacterButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "TalkButton", 28);
            UpdateButtonFontSize(safeAreaRootTf, "PotionButton", 28);

            EditorUtility.SetDirty(safeAreaRootTf.gameObject);
            EditorUtility.SetDirty(canvasObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ApplyPackageU2] Applied button and HUD layout successfully!");
        }

        private static void SetupElement(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, bool active)
        {
            Transform t = parent.Find(name);
            if (t == null) return;

            t.gameObject.SetActive(active);
            var rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = pivot;
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;
                EditorUtility.SetDirty(rt);
            }
        }

        private static void UpdateButtonFontSize(Transform parent, string name, int fontSize)
        {
            Transform t = parent.Find(name);
            if (t == null) return;
            foreach (var txt in t.GetComponentsInChildren<Text>(true))
            {
                txt.fontSize = fontSize;
                EditorUtility.SetDirty(txt);
            }
        }
    }
}
