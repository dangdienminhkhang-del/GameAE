// FloorSetupWizard.cs — Editor-only
// Tạo toàn bộ floor assets + UI trong một bước.
// Dùng menu: Game / Floor / Run Floor Setup Wizard
//
// Wizard làm 3 việc:
//   1. Tạo FloorDefinition asset (Floor 1) và FloorDatabase asset
//   2. Tạo FloorSelectRow prefab (khung trống — thêm TMP labels thủ công hoặc qua Prefab Editor)
//   3. Tạo Canvas + FloorSelectPanel trong scene đang mở
//   4. Tạo FloorSpawnRegistrar GameObject trong scene

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Game.Core.Floor;
using Game.UI.Floor;
using Game.PvE.Floor;

namespace Game.Editor
{
    public static class FloorSetupWizard
    {
        // ── Đường dẫn asset ──────────────────────────────────────────────────
        private const string DatabaseFolder = "Assets/_Game/Data/Databases";
        private const string DatabasePath   = DatabaseFolder + "/FloorDatabase.asset";
        private const string FloorFolder    = "Assets/_Game/Content/Floors/Floor01";
        private const string FloorDefPath   = FloorFolder + "/FloorDefinition_Floor01.asset";
        private const string PrefabFolder   = "Assets/_Game/Prefabs/UI";
        private const string RowPrefabPath  = PrefabFolder + "/FloorSelectRow.prefab";

        // ── Menu entry ───────────────────────────────────────────────────────

        [MenuItem("Game/Floor/Run Floor Setup Wizard")]
        public static void RunWizard()
        {
            int step = 0;
            try
            {
                EditorUtility.DisplayProgressBar("Floor Setup Wizard", "Tạo FloorDefinition Floor 1...", 0.1f);
                var floorDef = CreateOrLoadFloorDefinition();
                step = 1;

                EditorUtility.DisplayProgressBar("Floor Setup Wizard", "Tạo FloorDatabase...", 0.3f);
                var db = CreateOrLoadFloorDatabase(floorDef);
                step = 2;

                EditorUtility.DisplayProgressBar("Floor Setup Wizard", "Tạo FloorSelectRow prefab...", 0.5f);
                CreateOrLoadRowPrefab();
                step = 3;

                EditorUtility.DisplayProgressBar("Floor Setup Wizard", "Tạo Canvas + FloorSelectPanel trong scene...", 0.7f);
                SetupSceneUI(db);
                step = 4;

                EditorUtility.DisplayProgressBar("Floor Setup Wizard", "Tạo FloorSpawnRegistrar trong scene...", 0.9f);
                SetupSpawnRegistrar();
                step = 5;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FloorSetupWizard] Lỗi ở bước {step}: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "Floor Setup Wizard — Hoàn tất",
                "Assets đã tạo:\n" +
                "  • FloorDatabase.asset\n" +
                "  • FloorDefinition_Floor01.asset\n" +
                "  • Prefabs/UI/FloorSelectRow.prefab (khung trống)\n" +
                "  • FloorSelectCanvas (scene)\n" +
                "  • FloorSpawnRegistrar (scene)\n\n" +
                "Việc còn lại:\n" +
                "1. PvEBootstrap → gán FloorDatabase vào 'Floor Database'\n" +
                "2. FloorSpawnRegistrar → gán EnemySpawner\n" +
                "3. FloorSelectRow prefab → thêm TMP labels (xem hướng dẫn)\n" +
                "4. Build Settings: thêm scene PvE_Gameplay nếu chưa có",
                "OK");
        }

        [MenuItem("Game/Floor/Run Floor Setup Wizard", true)]
        private static bool ValidateWizard()
        {
            return SceneManager.GetActiveScene().isLoaded;
        }

        // ── Bước 1: FloorDefinition ──────────────────────────────────────────

        private static FloorDefinition CreateOrLoadFloorDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FloorDefPath);
            if (existing != null)
            {
                Debug.Log("[FloorSetupWizard] FloorDefinition Floor 1 đã tồn tại.");
                return existing;
            }

            EnsureFolder(FloorFolder);

            var def = ScriptableObject.CreateInstance<FloorDefinition>();
            def.id                  = "floor.01";
            def.floorNumber         = 1;
            def.displayName         = "Tầng 1";
            def.sceneKey            = "PvE_Gameplay";
            def.recommendedLevelMin = 1;
            def.recommendedLevelMax = 5;
            def.itemTierId          = "tier.common";

            AssetDatabase.CreateAsset(def, FloorDefPath);
            Debug.Log($"[FloorSetupWizard] Tạo FloorDefinition: {FloorDefPath}");
            return def;
        }

        // ── Bước 2: FloorDatabase ────────────────────────────────────────────

        private static FloorDatabase CreateOrLoadFloorDatabase(FloorDefinition floorDef)
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloorDatabase>(DatabasePath);
            if (existing != null)
            {
                bool alreadyHas = existing.floors.Exists(f => f != null && f.id == "floor.01");
                if (!alreadyHas && floorDef != null)
                {
                    existing.floors.Add(floorDef);
                    EditorUtility.SetDirty(existing);
                    Debug.Log("[FloorSetupWizard] Đã thêm floor.01 vào FloorDatabase.");
                }
                return existing;
            }

            EnsureFolder(DatabaseFolder);
            var db = ScriptableObject.CreateInstance<FloorDatabase>();
            if (floorDef != null) db.floors.Add(floorDef);
            AssetDatabase.CreateAsset(db, DatabasePath);
            Debug.Log($"[FloorSetupWizard] Tạo FloorDatabase: {DatabasePath}");
            return db;
        }

        // ── Bước 3: FloorSelectRow prefab (khung trống, KHÔNG dùng TMP) ─────
        // Tạo prefab với Button + LockedOverlay. Người phát triển thêm TMP labels
        // qua Prefab Editor sau (hoặc duplicate từ Hierarchy).

        private static void CreateOrLoadRowPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            if (existing != null && existing.GetComponent<FloorSelectRow>() != null)
            {
                Debug.Log("[FloorSetupWizard] FloorSelectRow prefab đã tồn tại.");
                return;
            }

            EnsureFolder(PrefabFolder);

            // Root
            var root     = new GameObject("FloorSelectRow");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(600f, 80f);
            root.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding              = new RectOffset(16, 16, 8, 8);
            layout.spacing              = 8f;
            layout.childAlignment       = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = true;

            // FloorName placeholder (Text legacy — không cần TMPro)
            var floorNameGO = CreateLegacyLabel(root, "FloorNameLabel", 200f, 60f, 18);

            // LevelRange placeholder
            var levelRangeGO = CreateLegacyLabel(root, "LevelRangeLabel", 140f, 60f, 14);

            // Status placeholder
            var statusGO = CreateLegacyLabel(root, "StatusLabel", 140f, 60f, 14);

            // SelectButton
            var btnGO   = new GameObject("SelectButton");
            btnGO.transform.SetParent(root.transform, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(100f, 56f);
            btnGO.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f);
            btnGO.AddComponent<Button>();
            var btnLabel = CreateLegacyLabel(btnGO, "ButtonLabel", 90f, 50f, 14);
            btnLabel.GetComponent<Text>().text = "Chọn";

            // LockedOverlay
            var lockedGO   = new GameObject("LockedOverlay");
            lockedGO.transform.SetParent(root.transform, false);
            var lockedRect = lockedGO.AddComponent<RectTransform>();
            lockedRect.anchorMin = Vector2.zero;
            lockedRect.anchorMax = Vector2.one;
            lockedRect.offsetMin = Vector2.zero;
            lockedRect.offsetMax = Vector2.zero;
            lockedGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            lockedGO.SetActive(false);

            // Gắn script
            root.AddComponent<FloorSelectRow>();

            // Lưu prefab
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            Object.DestroyImmediate(root);

            // NOTE: FloorSelectRow cần TMP_Text refs — để trống ở đây (legacy Text không tương thích).
            // Người phát triển mở prefab → thêm/thay bằng TMP_Text và gán vào serialized fields.

            Debug.Log($"[FloorSetupWizard] Tạo FloorSelectRow prefab (khung): {RowPrefabPath}");
        }

        // ── Bước 4: Canvas + FloorSelectPanel ───────────────────────────────
        // FloorSelectPanel chỉ cần: panelRoot, listContainer, loadingIndicator, errorPanel
        // Không truyền rowPrefab ở đây vì prefab dùng TMP — gán thủ công sau.

        private static void SetupSceneUI(FloorDatabase db)
        {
            var existing = Object.FindFirstObjectByType<FloorSelectPanel>();
            if (existing != null)
            {
                Debug.Log("[FloorSetupWizard] FloorSelectPanel đã có trong scene.");
                return;
            }

            // ── Canvas ──
            var canvasGO = new GameObject("FloorSelectCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── PanelRoot ──
            var panelRoot     = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(canvasGO.transform, false);
            var panelRect     = panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.1f);
            panelRect.anchorMax = new Vector2(0.95f, 0.9f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRoot.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

            var vLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            vLayout.padding              = new RectOffset(20, 20, 20, 20);
            vLayout.spacing              = 12f;
            vLayout.childAlignment       = TextAnchor.UpperCenter;
            vLayout.childControlWidth    = true;
            vLayout.childControlHeight   = false;
            vLayout.childForceExpandWidth  = true;
            vLayout.childForceExpandHeight = false;

            // ── Header ──
            var header = CreateLegacyLabel(panelRoot, "Header", 0f, 60f, 24);
            header.GetComponent<Text>().text = "Chọn Tầng";
            AddLayoutElement(header, 60f);

            // ── LoadingIndicator ──
            var loadingGO = new GameObject("LoadingIndicator");
            loadingGO.transform.SetParent(panelRoot.transform, false);
            loadingGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);
            loadingGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var loadingText = CreateLegacyLabel(loadingGO, "LoadingText", 0f, 40f, 16);
            loadingText.GetComponent<Text>().text = "Đang tải...";
            AddLayoutElement(loadingGO, 40f);
            loadingGO.SetActive(false);

            // ── ErrorPanel ──
            var errorGO = new GameObject("ErrorPanel");
            errorGO.transform.SetParent(panelRoot.transform, false);
            errorGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 60f);
            errorGO.AddComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.9f);
            var errorLabelGO = CreateLegacyLabel(errorGO, "ErrorLabel", 0f, 50f, 14);
            var errorText = errorLabelGO.GetComponent<Text>();
            errorText.text  = "";
            errorText.color = Color.white;
            AddLayoutElement(errorGO, 60f);
            errorGO.SetActive(false);

            // ── ListContainer ──
            var listGO   = new GameObject("ListContainer");
            listGO.transform.SetParent(panelRoot.transform, false);
            listGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 400f);
            var listLayout = listGO.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing              = 8f;
            listLayout.childAlignment       = TextAnchor.UpperCenter;
            listLayout.childControlWidth    = true;
            listLayout.childControlHeight   = false;
            listLayout.childForceExpandWidth  = true;
            listLayout.childForceExpandHeight = false;
            listGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddLayoutElement(listGO, 400f);

            // ── Gắn FloorSelectPanel script + wire fields ──
            var panelScript = canvasGO.AddComponent<FloorSelectPanel>();
            using (var so = new SerializedObject(panelScript))
            {
                so.FindProperty("panelRoot").objectReferenceValue        = panelRoot;
                so.FindProperty("listContainer").objectReferenceValue    = listGO.transform;
                // rowPrefab: để trống — gán thủ công sau khi thêm TMP labels vào prefab
                so.FindProperty("loadingIndicator").objectReferenceValue = loadingGO;
                so.FindProperty("errorPanel").objectReferenceValue       = errorGO;
                // errorLabel (TMP_Text): để trống — gán thủ công
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FloorSetupWizard] Tạo FloorSelectCanvas trong scene.");
        }

        // ── Bước 5: FloorSpawnRegistrar ──────────────────────────────────────

        private static void SetupSpawnRegistrar()
        {
            var existing = Object.FindFirstObjectByType<FloorSpawnRegistrar>();
            if (existing != null)
            {
                Debug.Log("[FloorSetupWizard] FloorSpawnRegistrar đã có trong scene.");
                return;
            }

            var go = new GameObject("FloorSpawnRegistrar");
            go.AddComponent<FloorSpawnRegistrar>();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FloorSetupWizard] Tạo FloorSpawnRegistrar trong scene. Nhớ gán EnemySpawner.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void EnsureFolder(string path)
        {
            var parts   = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// Tạo GameObject với UnityEngine.UI.Text (legacy) — không phụ thuộc TMPro.
        private static GameObject CreateLegacyLabel(
            GameObject parent, string name, float width, float height, int fontSize)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            var txt  = go.AddComponent<Text>();
            txt.fontSize  = fontSize;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text      = name;
            txt.color     = Color.white;
            return go;
        }

        private static void AddLayoutElement(GameObject go, float preferredHeight)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.flexibleWidth   = 1f;
        }
    }
}
