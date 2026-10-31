using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Game.PvE.Exploration;
using Game.UI.Map;

namespace Game.EditorTools
{
    /// <summary>
    /// Gắn hệ thống Sổ Hải Đồ vào Content Tầng 2: FloorMapInfo (giới hạn + tilemap), MapExplorer (vẽ bản đồ),
    /// FastTravelController (dịch chuyển), MapHudController (UI) và biến mọi cột đèn Vùng An Toàn thành Đèn Hiệu.
    /// Chạy lại được; <see cref="Floor02WorldBuilder"/> tự gọi cuối mỗi lần dựng.
    /// </summary>
    public static class Floor02MapSystemInstaller
    {
        private static readonly (string objectName, string key, string display)[] Beacons =
        {
            ("SafeZone_Lamp", "town", "Trạm Van"),
            ("SafeZone_R1_Plaza", "plaza", "Ngã Tư Áp Lực"),
            ("SafeZone_R2", "r2_pipes", "Trạm Nghỉ Đường Ống"),
            ("SafeZone_R3", "r3_forge", "Trạm Nghỉ Xưởng Rèn"),
            ("SafeZone_R4", "r4_scrap", "Trạm Phế Liệu"),
            ("SafeZone_R5", "r5_boiler", "Trạm Nghỉ Lò Hơi"),
        };

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        [MenuItem("Game/Floor 02/Install Map System")]
        public static void InstallMenu()
        {
            Scene sc = default;
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).name == "Floor02") sc = SceneManager.GetSceneAt(i);
            Transform content = null;
            foreach (var r in sc.GetRootGameObjects()) if (r.name == "Floor02") content = r.transform.Find("Content");
            if (content == null) { Debug.LogError("Cần mở scene Floor02."); return; }
            var floor = FindDeep(content, "Floor").GetComponent<Tilemap>();
            floor.CompressBounds();
            Debug.Log(Install(content, new Rect(875f, -100f, 350f, 196f)));
        }

        public static string Install(Transform content, Rect worldBounds)
        {
            var sysT = content.Find("MapSystem");
            if (sysT == null)
            {
                var go = new GameObject("MapSystem");
                go.transform.SetParent(content, false);
                sysT = go.transform;
            }
            var sys = sysT.gameObject;
            var info = GetOrAdd<FloorMapInfo>(sys);
            GetOrAdd<MapExplorer>(sys);
            GetOrAdd<FastTravelController>(sys);
            GetOrAdd<MapHudController>(sys);

            var so = new SerializedObject(info);
            so.FindProperty("floorId").stringValue = "floor.02";
            so.FindProperty("worldBounds").rectValue = worldBounds;
            so.FindProperty("floorTilemap").objectReferenceValue = FindDeep(content, "Floor").GetComponent<Tilemap>();
            so.FindProperty("wallsTilemap").objectReferenceValue = FindDeep(content, "Walls").GetComponent<Tilemap>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var hud = sys.GetComponent<MapHudController>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("minimapOffset").vector2Value = new Vector2(24f, -300f);
            hso.ApplyModifiedPropertiesWithoutUndo();

            int count = 0;
            var missing = new List<string>();
            foreach (var b in Beacons)
            {
                var t = FindDeep(content, b.objectName);
                if (t == null) { missing.Add(b.objectName); continue; }
                var wp = GetOrAdd<BeaconWaypoint>(t.gameObject);
                var bso = new SerializedObject(wp);
                bso.FindProperty("beaconId").stringValue = "beacon.floor_02." + b.key;
                bso.FindProperty("displayName").stringValue = b.display;
                bso.FindProperty("floorId").stringValue = "floor.02";
                bso.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }
            return "Sổ Hải Đồ: bounds=" + worldBounds + ", Đèn Hiệu=" + count + (missing.Count > 0 ? " (thiếu: " + string.Join(",", missing) + ")" : "");
        }
    }
}
