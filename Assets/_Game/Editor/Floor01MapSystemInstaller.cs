using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Game.PvE.Exploration;
using Game.PvE.SafeZone;
using Game.PvE.Zones;
using Game.UI.Map;

namespace Game.EditorTools
{
    /// <summary>
    /// Gắn Sổ Hải Đồ vào Tầng 1 (scene host PvE_Gameplay). Chạy lại được và TỰ ĐO lại giới hạn bản đồ mỗi lần,
    /// nên sau khi mở rộng map Tầng 1 (docs/17) chỉ cần bấm lại menu.
    ///
    /// Tầng 1 vẽ đất bằng sprite (World_Ground) và tilemap chỉ chứa khối chặn → bật <c>groundIsOpen</c>:
    /// ô không có khối chặn = đi được. Mọi cột đèn Safe Zone trong Content tự thành Đèn Hiệu; tên lấy từ Zone chứa nó.
    /// id tầng đọc từ FloorSceneBinder (không hardcode).
    /// </summary>
    public static class Floor01MapSystemInstaller
    {
        [MenuItem("Game/Floor 01/Install Map System")]
        public static void InstallMenu()
        {
            Debug.Log(InstallInOpenScene());
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.ToLowerInvariant()) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString().Trim('_');
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        public static string InstallInOpenScene()
        {
            Scene scene = SceneManager.GetSceneByName("PvE_Gameplay");
            if (!scene.isLoaded) return "Cần mở scene PvE_Gameplay.";

            GameObject floorRoot = null, gridRoot = null, groundRoot = null;
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "Floor01") floorRoot = r;
                else if (r.name == "Grid") gridRoot = r;
                else if (r.name == "World_Ground") groundRoot = r;
            }
            if (floorRoot == null) return "Không thấy root Floor01.";

            var binder = floorRoot.GetComponentInChildren<Game.PvE.Floor.FloorSceneBinder>(true);
            if (binder == null) return "Floor01 chưa có FloorSceneBinder.";
            var bso = new SerializedObject(binder);
            string floorId = bso.FindProperty("floorId").stringValue;
            var content = bso.FindProperty("contentRoot").objectReferenceValue as Transform
                          ?? (bso.FindProperty("contentRoot").objectReferenceValue as GameObject)?.transform;
            if (string.IsNullOrEmpty(floorId) || content == null) return "FloorSceneBinder thiếu floorId/contentRoot.";

            // 1. Giới hạn thế giới: hợp của ảnh đất và tilemap khối chặn, làm tròn ra ô nguyên.
            Tilemap blockers = gridRoot != null ? gridRoot.GetComponentInChildren<Tilemap>(true) : null;
            bool has = false; Bounds b = new Bounds();
            if (groundRoot != null)
            {
                foreach (var r in groundRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
                }
            }
            if (blockers != null)
            {
                blockers.CompressBounds();
                var cb = blockers.cellBounds;
                var tb = new Bounds(new Vector3((cb.xMin + cb.xMax) * 0.5f, (cb.yMin + cb.yMax) * 0.5f, 0f), new Vector3(cb.size.x, cb.size.y, 0f));
                if (!has) { b = tb; has = true; } else b.Encapsulate(tb);
            }
            if (!has) return "Không đo được giới hạn Tầng 1 (không có World_Ground hay tilemap).";
            int x0 = Mathf.FloorToInt(b.min.x), y0 = Mathf.FloorToInt(b.min.y);
            int x1 = Mathf.CeilToInt(b.max.x), y1 = Mathf.CeilToInt(b.max.y);
            var worldBounds = new Rect(x0, y0, x1 - x0, y1 - y0);

            // 2. Hệ thống bản đồ trong Content
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
            var hud = GetOrAdd<MapHudController>(sys);

            var iso = new SerializedObject(info);
            iso.FindProperty("floorId").stringValue = floorId;
            iso.FindProperty("worldBounds").rectValue = worldBounds;
            iso.FindProperty("floorTilemap").objectReferenceValue = null;
            iso.FindProperty("wallsTilemap").objectReferenceValue = blockers;
            iso.FindProperty("groundIsOpen").boolValue = true;
            iso.ApplyModifiedPropertiesWithoutUndo();

            var hso = new SerializedObject(hud);
            hso.FindProperty("minimapOffset").vector2Value = new Vector2(24f, -300f);
            hso.ApplyModifiedPropertiesWithoutUndo();

            // 3. Đèn Hiệu: mọi Safe Zone trong Content
            var zones = new List<ZoneVolume>(content.GetComponentsInChildren<ZoneVolume>(true));
            var sb = new StringBuilder();
            int count = 0;
            foreach (var safe in content.GetComponentsInChildren<SafeZoneTrigger>(true))
            {
                ZoneVolume best = null; int bestPrio = int.MinValue; int bestKind = -1; string bestName = null;
                foreach (var z in zones)
                {
                    var bc = z.GetComponent<BoxCollider2D>();
                    if (bc == null || !bc.bounds.Contains(new Vector3(safe.transform.position.x, safe.transform.position.y, bc.bounds.center.z))) continue;
                    var zso = new SerializedObject(z);
                    int prio = zso.FindProperty("priority").intValue;
                    if (prio > bestPrio) { best = z; bestPrio = prio; bestKind = zso.FindProperty("kind").intValue; bestName = zso.FindProperty("displayName").stringValue; }
                }
                string key = best != null && bestKind == 0 ? "town" : Sanitize(safe.name.Replace("SafeZone_", ""));
                string display = !string.IsNullOrEmpty(bestName) ? bestName : safe.name;

                var wp = GetOrAdd<BeaconWaypoint>(safe.gameObject);
                var wso = new SerializedObject(wp);
                wso.FindProperty("beaconId").stringValue = "beacon." + floorId.Replace('.', '_') + "." + key;
                wso.FindProperty("displayName").stringValue = display;
                wso.FindProperty("floorId").stringValue = floorId;
                wso.ApplyModifiedPropertiesWithoutUndo();
                sb.Append(" [" + key + " → " + display + "]");
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Sổ Hải Đồ Tầng 1 (" + floorId + "): bounds=" + worldBounds + ", Đèn Hiệu=" + count + sb;
        }
    }
}
