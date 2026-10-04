// Wave2ASetupWizard.cs — Editor-only
// Tạo toàn bộ assets Wave 2A (Weapon Technique + Dodge) và wire vào Player prefab.
// Menu: Game/Wave2A/Run Wave 2A Setup Wizard
// Xóa file này sau khi chạy xong.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Core.Skills;

namespace Game.Editor
{
    public static class Wave2ASetupWizard
    {
        private const string TechFolder  = "Assets/_Game/Data/Techniques";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player.prefab";

        [MenuItem("Game/Wave2A/Run Wave 2A Setup Wizard")]
        public static void RunWizard()
        {
            int step = 0;
            try
            {
                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Tạo folders...", 0.05f);
                EnsureFolder(TechFolder);
                step = 1;

                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Tạo DodgeDefinition...", 0.15f);
                var dodgeDef = CreateOrLoadDodgeDefinition();
                step = 2;

                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Tạo Blade Foundation technique...", 0.30f);
                var bladeCore = CreateOrLoadBladeFoundation();
                step = 3;

                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Tạo Blade Traverse technique...", 0.45f);
                var bladeTraverse = CreateOrLoadBladeTraverse();
                step = 4;

                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Tạo TechniqueDatabase...", 0.60f);
                var db = CreateOrLoadTechniqueDatabase(bladeCore, bladeTraverse);
                step = 5;

                EditorUtility.DisplayProgressBar("Wave 2A Setup", "Wire Player prefab...", 0.80f);
                WirePlayerPrefab(db, dodgeDef);
                step = 6;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Wave2ASetupWizard] Lỗi ở bước {step}: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "Wave 2A Setup — Hoàn tất",
                "Đã tạo:\n" +
                "  • DodgeDefinition.asset\n" +
                "  • Blade_Foundation.asset (technique.blade_foundation)\n" +
                "  • Blade_Traverse.asset   (technique.blade_traverse)\n" +
                "  • TechniqueDatabase.asset\n" +
                "  • TechniqueController + DodgeController → Player prefab\n\n" +
                "Bước còn lại thủ công:\n" +
                "  1. Player prefab → TechniqueController → gán TechniqueDatabase\n" +
                "     (Inspector → Technique Database field)\n" +
                "  2. Player prefab → DodgeController → gán DodgeDefinition\n" +
                "     (Inspector → Definition field)\n" +
                "  3. Gán Animator (nếu có) vào TechniqueController + DodgeController",
                "OK");
        }

        [MenuItem("Game/Wave2A/Run Wave 2A Setup Wizard", true)]
        private static bool Validate() =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded;

        // ── DodgeDefinition ─────────────────────────────────────────────────

        private static DodgeDefinition CreateOrLoadDodgeDefinition()
        {
            const string path = TechFolder + "/DodgeDefinition.asset";
            var ex = AssetDatabase.LoadAssetAtPath<DodgeDefinition>(path);
            if (ex != null) { Debug.Log("[Wave2A] DodgeDefinition đã tồn tại."); return ex; }

            var def = ScriptableObject.CreateInstance<DodgeDefinition>();
            // Dùng Reset() via SerializedObject để apply [SerializeField] defaults
            using (var so = new SerializedObject(def))
            {
                so.FindProperty("dodgeDurationSeconds").floatValue = 0.18f;
                so.FindProperty("cooldownSeconds").floatValue = 1.2f;
                so.FindProperty("recoverySeconds").floatValue = 0.20f;
                so.FindProperty("dashDistance").floatValue = 2.0f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[Wave2A] Tạo DodgeDefinition: {path}");
            return def;
        }

        // ── Blade Foundation (technique.blade_foundation) ───────────────────
        // Mốc 1 — Blade Core slot, masteryRequired=0, 2 hit windows (docs/07 §4)

        private static WeaponTechniqueDefinition CreateOrLoadBladeFoundation()
        {
            const string path = TechFolder + "/Blade_Foundation.asset";
            var ex = AssetDatabase.LoadAssetAtPath<WeaponTechniqueDefinition>(path);
            if (ex != null) { Debug.Log("[Wave2A] Blade_Foundation đã tồn tại."); return ex; }

            var def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            using (var so = new SerializedObject(def))
            {
                so.FindProperty("id").stringValue          = "technique.blade_foundation";
                so.FindProperty("displayName").stringValue = "Blade Foundation";
                so.FindProperty("weaponDiscipline").enumValueIndex = (int)WeaponDiscipline.Blade;
                so.FindProperty("inputSlot").enumValueIndex        = (int)TechniqueInputSlot.Core;
                so.FindProperty("masteryRequired").intValue        = 0;
                so.FindProperty("cooldownSeconds").floatValue      = 2.5f;
                so.FindProperty("windUpSeconds").floatValue        = 0.10f;
                so.FindProperty("recoverySeconds").floatValue      = 0.25f;
                so.FindProperty("totalMultiplier").doubleValue     = 1.3;
                so.FindProperty("hitboxRadius").floatValue         = 0.9f;
                so.FindProperty("criticalChanceOverride").doubleValue = -1.0;
                so.FindProperty("criticalDamageOverride").doubleValue = -1.0;

                // 2 hit windows: swing 1 (0.4) + swing 2 (0.9)
                var windows = so.FindProperty("activeWindows");
                windows.arraySize = 2;
                SetHitWindow(windows.GetArrayElementAtIndex(0), 0.00f, 0.12f, 0.6, (int)MovementPolicy.None,     0f);
                SetHitWindow(windows.GetArrayElementAtIndex(1), 0.15f, 0.12f, 0.7, (int)MovementPolicy.Step,  0.4f);

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[Wave2A] Tạo Blade_Foundation: {path}");
            return def;
        }

        // ── Blade Traverse (technique.blade_traverse) ───────────────────────
        // Mốc 1 — Blade Traverse slot, masteryRequired=25, 3 hit windows, dash forward

        private static WeaponTechniqueDefinition CreateOrLoadBladeTraverse()
        {
            const string path = TechFolder + "/Blade_Traverse.asset";
            var ex = AssetDatabase.LoadAssetAtPath<WeaponTechniqueDefinition>(path);
            if (ex != null) { Debug.Log("[Wave2A] Blade_Traverse đã tồn tại."); return ex; }

            var def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            using (var so = new SerializedObject(def))
            {
                so.FindProperty("id").stringValue          = "technique.blade_traverse";
                so.FindProperty("displayName").stringValue = "Blade Traverse";
                so.FindProperty("weaponDiscipline").enumValueIndex = (int)WeaponDiscipline.Blade;
                so.FindProperty("inputSlot").enumValueIndex        = (int)TechniqueInputSlot.Traverse;
                so.FindProperty("masteryRequired").intValue        = 25;
                so.FindProperty("cooldownSeconds").floatValue      = 3.5f;
                so.FindProperty("windUpSeconds").floatValue        = 0.08f;
                so.FindProperty("recoverySeconds").floatValue      = 0.30f;
                so.FindProperty("totalMultiplier").doubleValue     = 1.8;
                so.FindProperty("hitboxRadius").floatValue         = 1.0f;
                so.FindProperty("criticalChanceOverride").doubleValue = -1.0;
                so.FindProperty("criticalDamageOverride").doubleValue = -1.0;

                // 3 hit windows: lướt qua + 2 slash
                var windows = so.FindProperty("activeWindows");
                windows.arraySize = 3;
                SetHitWindow(windows.GetArrayElementAtIndex(0), 0.00f, 0.10f, 0.5, (int)MovementPolicy.Dash, 1.8f);
                SetHitWindow(windows.GetArrayElementAtIndex(1), 0.12f, 0.10f, 0.6, (int)MovementPolicy.None, 0f);
                SetHitWindow(windows.GetArrayElementAtIndex(2), 0.26f, 0.12f, 0.7, (int)MovementPolicy.None, 0f);

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[Wave2A] Tạo Blade_Traverse: {path}");
            return def;
        }

        // ── TechniqueDatabase ───────────────────────────────────────────────

        private static TechniqueDatabase CreateOrLoadTechniqueDatabase(
            WeaponTechniqueDefinition core, WeaponTechniqueDefinition traverse)
        {
            const string path = TechFolder + "/TechniqueDatabase.asset";
            var ex = AssetDatabase.LoadAssetAtPath<TechniqueDatabase>(path);
            if (ex != null)
            {
                // Đảm bảo cả 2 technique đều có trong database
                bool dirty = false;
                using (var so = new SerializedObject(ex))
                {
                    var list = so.FindProperty("techniques");
                    bool hasCore = false, hasTraverse = false;
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        var el = list.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (el == core) hasCore = true;
                        if (el == traverse) hasTraverse = true;
                    }
                    if (!hasCore)
                    {
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = core;
                        dirty = true;
                    }
                    if (!hasTraverse)
                    {
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = traverse;
                        dirty = true;
                    }
                    if (dirty) so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (dirty) EditorUtility.SetDirty(ex);
                Debug.Log("[Wave2A] TechniqueDatabase đã tồn tại — kiểm tra entries xong.");
                return ex;
            }

            var db = ScriptableObject.CreateInstance<TechniqueDatabase>();
            using (var so = new SerializedObject(db))
            {
                var list = so.FindProperty("techniques");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(0).objectReferenceValue = core;
                list.GetArrayElementAtIndex(1).objectReferenceValue = traverse;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(db, path);
            Debug.Log($"[Wave2A] Tạo TechniqueDatabase: {path}");
            return db;
        }

        // ── Wire Player prefab ──────────────────────────────────────────────

        private static void WirePlayerPrefab(TechniqueDatabase db, DodgeDefinition dodgeDef)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Wave2A] Không tìm thấy Player prefab tại: {PlayerPrefabPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PlayerPrefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Thêm TechniqueController nếu chưa có
                var techCtrl = root.GetComponent<Game.PvE.Skills.TechniqueController>();
                if (techCtrl == null)
                {
                    techCtrl = root.AddComponent<Game.PvE.Skills.TechniqueController>();
                    Debug.Log("[Wave2A] Thêm TechniqueController vào Player prefab.");
                }

                // Gán TechniqueDatabase
                using (var so = new SerializedObject(techCtrl))
                {
                    so.FindProperty("techniqueDatabase").objectReferenceValue = db;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Thêm DodgeController nếu chưa có
                var dodgeCtrl = root.GetComponent<Game.PvE.Dodge.DodgeController>();
                if (dodgeCtrl == null)
                {
                    dodgeCtrl = root.AddComponent<Game.PvE.Dodge.DodgeController>();
                    Debug.Log("[Wave2A] Thêm DodgeController vào Player prefab.");
                }

                // Gán DodgeDefinition
                using (var so = new SerializedObject(dodgeCtrl))
                {
                    so.FindProperty("definition").objectReferenceValue = dodgeDef;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[Wave2A] Đã wire Player prefab xong.");
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// SerializedProperty cho TechniqueHitWindow (startTimeOffset, duration, hitMultiplier, movementPolicy, moveDistance)
        private static void SetHitWindow(SerializedProperty wp, float start, float dur,
            double hitMult, int movePolicy, float moveDist)
        {
            wp.FindPropertyRelative("startTimeOffset").floatValue  = start;
            wp.FindPropertyRelative("duration").floatValue         = dur;
            wp.FindPropertyRelative("hitMultiplier").doubleValue   = hitMult;
            wp.FindPropertyRelative("movementPolicy").enumValueIndex = movePolicy;
            wp.FindPropertyRelative("moveDistance").floatValue     = moveDist;
        }
    }
}
