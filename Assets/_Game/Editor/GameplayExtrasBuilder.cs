using System.IO;
using Game.PvE.Items;
using Game.PvE.Player;
using Game.UI.Character;
using Game.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.EditorTools
{
    /// <summary>
    /// Nối 3 tính năng mới vào scene PvE_Gameplay: viền cảnh báo máu thấp, nút + bảng Nhân vật, rương ở vịnh.
    /// Chạy lại được nhiều lần (idempotent). Sprite rương là PLACEHOLDER (32px, PPU 32) — Art Track có thể ghi đè PNG cùng tên.
    /// </summary>
    public static class GameplayExtrasBuilder
    {
        private const string ArtDir = "Assets/_Game/Art/Environment/Floor01";

        // Bảng màu docs/14 (tools/art_gen/pixelkit.py).
        private static readonly int[] Wood2 = { 0x2a1f16, 0x43321f, 0x5f482f, 0x7a5e3f, 0x99794f };
        private static readonly int[] Nail = { 0x15110e, 0x2a2622, 0x5a554e, 0x8a8478, 0xb8b2a4 };
        private static readonly int[] Iron = { 0x14181f, 0x252b35, 0x3a4250, 0x566073, 0x7c889d };

        [MenuItem("Game/Extras/Build Low-HP Warning, Character Panel, Chest")]
        private static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ExtrasBuilder] Thoát Play Mode trước khi dựng.");
                return;
            }

            var canvas = GameObject.Find("/Canvas");
            var hudGo = GameObject.Find("/Canvas/PlayerHUD");
            var content = GameObject.Find("/Floor01/Content");
            var potion = GameObject.Find("/Floor01/Content/Pickup_Potion");
            var progress = Object.FindFirstObjectByType<PlayerProgress>();
            if (canvas == null || hudGo == null || content == null || potion == null || progress == null)
            {
                Debug.LogError("[ExtrasBuilder] Thiếu object (cần mở PvE_Gameplay): Canvas, Canvas/PlayerHUD, Floor01/Content, Pickup_Potion, PlayerProgress.");
                return;
            }

            var invBtn = canvas.transform.Find("InventoryButton") as RectTransform;
            var pauseBtn = canvas.transform.Find("PauseButton") as RectTransform;
            var hud = hudGo.GetComponent<PlayerHUD>();
            var potionPickup = potion.GetComponent<ItemPickup>();
            if (invBtn == null || pauseBtn == null || hud == null || potionPickup == null)
            {
                Debug.LogError("[ExtrasBuilder] Thiếu InventoryButton / PauseButton / PlayerHUD / ItemPickup trên Pickup_Potion.");
                return;
            }

            // 1) Viền cảnh báo máu thấp: con đầu tiên của Canvas để vẽ DƯỚI các nút HUD.
            var vT = canvas.transform.Find("LowHealthVignette");
            GameObject vGo = vT != null
                ? vT.gameObject
                : new GameObject("LowHealthVignette", typeof(RectTransform), typeof(RawImage), typeof(LowHealthWarning));
            vGo.transform.SetParent(canvas.transform, false);
            vGo.transform.SetAsFirstSibling();
            Stretch((RectTransform)vGo.transform);
            var raw = vGo.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = new Color(1f, 1f, 1f, 0f);

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("lowHealthWarning").objectReferenceValue = vGo.GetComponent<LowHealthWarning>();
            hudSo.ApplyModifiedProperties();

            // 2) Nút Nhân vật (nhân bản nút Túi cho đồng bộ giao diện) + bảng Nhân vật
            var btnT = canvas.transform.Find("CharacterButton") as RectTransform;
            if (btnT == null)
            {
                var clone = Object.Instantiate(invBtn.gameObject, canvas.transform);
                clone.name = "CharacterButton";
                var toggle = clone.GetComponent("InventoryToggleButton"); // tên kiểu, không phụ thuộc namespace
                if (toggle != null) Object.DestroyImmediate(toggle);
                btnT = (RectTransform)clone.transform;
            }
            var btn = btnT.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            var label = btnT.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Nhân vật";
            // Nút Pause và nút Túi dùng neo (anchor) KHÁC nhau nên không được so anchoredPosition trực tiếp.
            // Đặt cùng neo/pivot với Pause, nằm sát bên TRÁI Pause với khoảng cách 24.
            btnT.anchorMin = pauseBtn.anchorMin;
            btnT.anchorMax = pauseBtn.anchorMax;
            btnT.pivot = pauseBtn.pivot;
            btnT.anchoredPosition = new Vector2(
                pauseBtn.anchoredPosition.x - pauseBtn.sizeDelta.x - 24f,
                pauseBtn.anchoredPosition.y);

            var pT = canvas.transform.Find("CharacterPanel");
            GameObject pGo = pT != null ? pT.gameObject : new GameObject("CharacterPanel", typeof(RectTransform), typeof(CharacterPanel));
            pGo.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)pGo.transform);
            var invPanel = canvas.transform.Find("InventoryPanel");
            if (invPanel != null) pGo.transform.SetSiblingIndex(invPanel.GetSiblingIndex() + 1);

            var invLabel = invBtn.GetComponentInChildren<Text>(true);
            var panelSo = new SerializedObject(pGo.GetComponent<CharacterPanel>());
            panelSo.FindProperty("playerProgress").objectReferenceValue = progress;
            panelSo.FindProperty("openButton").objectReferenceValue = btn;
            panelSo.FindProperty("fontSource").objectReferenceValue = invLabel;
            panelSo.ApplyModifiedProperties();

            // 3) Rương ở vịnh: sprite placeholder + phần thưởng lấy từ vật phẩm của Pickup_Potion có sẵn.
            Sprite closed = WriteSprite("Props_Floor01_Chest", ChestPixels(false));
            Sprite open = WriteSprite("Props_Floor01_ChestOpen", ChestPixels(true));

            var cT = content.transform.Find("Chest_Cove");
            GameObject cGo = cT != null
                ? cT.gameObject
                : new GameObject("Chest_Cove", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(TreasureChest));
            cGo.transform.SetParent(content.transform, false);
            cGo.transform.position = new Vector3(20.5f, -14.5f, 0f);

            var sr = cGo.GetComponent<SpriteRenderer>();
            sr.sprite = closed;
            sr.sortingOrder = -1;
            var col = cGo.GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f);

            var potionItem = new SerializedObject(potionPickup).FindProperty("item").objectReferenceValue;
            if (potionItem == null) Debug.LogWarning("[ExtrasBuilder] Pickup_Potion chưa có item — rương sẽ chỉ có Col.");

            var cso = new SerializedObject(cGo.GetComponent<TreasureChest>());
            cso.FindProperty("chestId").stringValue = "floor1.cove";
            var rewards = cso.FindProperty("rewards");
            rewards.arraySize = potionItem != null ? 1 : 0;
            if (potionItem != null)
            {
                var r0 = rewards.GetArrayElementAtIndex(0);
                r0.FindPropertyRelative("item").objectReferenceValue = potionItem;
                r0.FindPropertyRelative("quantity").intValue = 2;
            }
            cso.FindProperty("colReward").intValue = 60;
            cso.FindProperty("spriteRenderer").objectReferenceValue = sr;
            cso.FindProperty("closedSprite").objectReferenceValue = closed;
            cso.FindProperty("openSprite").objectReferenceValue = open;
            cso.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            var scene = canvas.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ExtrasBuilder] Xong: viền máu thấp, nút+bảng Nhân vật, rương Chest_Cove. Đã lưu scene.");
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Sprite rương placeholder ──────────────────────────────────────────

        private static Color32 C(int[] ramp, int i)
        {
            int v = ramp[i];
            return new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        }

        private static void Fill(Color32[] p, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    p[y * 32 + x] = c;
        }

        private static Color32[] ChestPixels(bool open)
        {
            var p = new Color32[32 * 32]; // mặc định trong suốt
            // Thân rương
            Fill(p, 4, 3, 27, 18, C(Wood2, 0));
            Fill(p, 5, 4, 26, 17, C(Wood2, 2));
            Fill(p, 8, 4, 10, 17, C(Iron, 2));
            Fill(p, 21, 4, 23, 17, C(Iron, 2));

            if (!open)
            {
                // Nắp đóng + khóa
                Fill(p, 4, 17, 27, 23, C(Wood2, 0));
                Fill(p, 5, 18, 26, 22, C(Wood2, 3));
                Fill(p, 8, 17, 10, 23, C(Iron, 3));
                Fill(p, 21, 17, 23, 23, C(Iron, 3));
                Fill(p, 14, 14, 17, 19, C(Nail, 4));
                Fill(p, 15, 15, 16, 17, C(Wood2, 0));
            }
            else
            {
                // Nắp mở ngửa ra sau, lòng rương tối, vài điểm sáng của đồ bên trong
                Fill(p, 4, 22, 27, 28, C(Wood2, 0));
                Fill(p, 5, 23, 26, 27, C(Wood2, 3));
                Fill(p, 5, 16, 26, 18, C(Wood2, 0));
                p[19 * 32 + 12] = C(Nail, 4);
                p[20 * 32 + 16] = C(Nail, 4);
                p[19 * 32 + 20] = C(Nail, 4);
                p[18 * 32 + 14] = C(Nail, 3);
                p[18 * 32 + 18] = C(Nail, 3);
            }
            return p;
        }

        private static Sprite WriteSprite(string fileName, Color32[] pixels)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string assetPath = ArtDir + "/" + fileName + ".png";
            string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            File.WriteAllBytes(abs, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
