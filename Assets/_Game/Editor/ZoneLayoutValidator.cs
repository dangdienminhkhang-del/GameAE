using System.Collections.Generic;
using System.Text;
using Game.Core.Floor;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using Game.PvE.SafeZone;
using Game.PvE.Zones;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Công cụ kiểm bố cục vùng của tầng (Phase 9.7a): Tools/Game2D/Validate Floor Zones.
    /// Chạy trên scene đang mở và CHỈ ĐỌC, không ghi gì vào scene. Báo kết quả ở Console và một hộp thoại.
    /// Lưu ý: kiểm bằng Collider2D nên object đang TẮT (inactive) không có vùng va chạm để hỏi; công cụ sẽ cảnh báo thay vì kết luận sai.
    /// </summary>
    public static class ZoneLayoutValidator
    {
        private const string MenuPath = "Tools/Game2D/Validate Floor Zones";

        [MenuItem(MenuPath)]
        public static void ValidateOpenScene()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            Validate(errors, warnings);
            Report(errors, warnings);
        }

        /// <summary>Gom lỗi và cảnh báo (public để test hoặc công cụ khác gọi lại).</summary>
        public static void Validate(List<string> errors, List<string> warnings)
        {
            SafeZoneTrigger[] safeZones = Object.FindObjectsByType<SafeZoneTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            FloorSceneBinder[] binders = Object.FindObjectsByType<FloorSceneBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (binders.Length == 0)
            {
                warnings.Add("Scene đang mở không có FloorSceneBinder nào, không có tầng để kiểm.");
            }

            foreach (FloorSceneBinder binder in binders)
            {
                ValidateFloor(binder, safeZones, errors, warnings);
            }

            CheckDuplicateZoneIds(errors);
        }

        private static void ValidateFloor(FloorSceneBinder binder, SafeZoneTrigger[] safeZones,
            List<string> errors, List<string> warnings)
        {
            string floorId = string.IsNullOrEmpty(binder.FloorId) ? "(chưa có floorId)" : binder.FloorId;
            string label = $"[{floorId}]";

            if (binder.ContentRoot == null)
            {
                errors.Add($"{label} FloorSceneBinder chưa gán contentRoot.");
                return;
            }

            ZoneVolume[] zones = binder.ContentRoot.GetComponentsInChildren<ZoneVolume>(true);
            EnemySpawner[] spawners = binder.ContentRoot.GetComponentsInChildren<EnemySpawner>(true);

            // 1) Đúng một vùng Town, nằm trong một Safe Zone.
            ZoneVolume town = null;
            int townCount = 0;
            foreach (ZoneVolume zone in zones)
            {
                if (zone.Kind != ZoneKind.Town) continue;
                townCount++;
                town = zone;
            }

            if (townCount != 1)
            {
                errors.Add($"{label} cần ĐÚNG một vùng Town, hiện có {townCount}.");
            }
            else if (!IsInsideAnySafeZone(ZoneCenter(town), safeZones, warnings, label))
            {
                errors.Add($"{label} vùng Town '{town.ZoneId}' không nằm trong Safe Zone nào (tâm vùng nằm ngoài mọi Safe Zone).");
            }

            // 2) entryPoint nằm trong Safe Zone.
            if (binder.EntryPoint == null)
            {
                errors.Add($"{label} FloorSceneBinder chưa gán entryPoint.");
            }
            else if (!IsInsideAnySafeZone(binder.EntryPoint.position, safeZones, warnings, label))
            {
                errors.Add($"{label} entryPoint '{binder.EntryPoint.name}' không nằm trong Safe Zone (docs/02 §19).");
            }

            // 3) Mọi spawner có spawnCenter ngoài Safe Zone.
            foreach (EnemySpawner spawner in spawners)
            {
                if (IsInsideAnySafeZone(spawner.SpawnCenterPosition, safeZones, warnings, label))
                {
                    errors.Add($"{label} spawner '{spawner.name}' có tâm spawn NẰM TRONG Safe Zone; hãy dời tâm ra ngoài.");
                }
            }

            // 4) BossRoom khi và chỉ khi tầng có boss.
            bool hasBossRoom = false;
            foreach (ZoneVolume zone in zones)
            {
                if (zone.Kind == ZoneKind.BossRoom) hasBossRoom = true;
            }

            string bossId = FindFloorDefinition(binder.FloorId)?.bossId;
            if (FindFloorDefinition(binder.FloorId) == null)
            {
                warnings.Add($"{label} không tìm thấy FloorDefinition cùng id, bỏ qua kiểm BossRoom.");
            }
            else
            {
                bool hasBoss = !string.IsNullOrEmpty(bossId);
                if (hasBoss && !hasBossRoom) errors.Add($"{label} tầng có boss ('{bossId}') nhưng chưa có vùng BossRoom.");
                if (!hasBoss && hasBossRoom) errors.Add($"{label} tầng KHÔNG có boss nhưng lại có vùng BossRoom.");
            }

            // 5) Mỗi vùng Farm có ít nhất một spawner.
            foreach (ZoneVolume zone in zones)
            {
                if (zone.Kind != ZoneKind.Farm) continue;
                if (!HasSpawnerFor(zone, spawners))
                {
                    errors.Add($"{label} vùng Farm '{zone.ZoneId}' chưa có spawner nào (gán spawnZone hoặc đặt tâm spawn trong vùng).");
                }
            }
        }

        private static bool HasSpawnerFor(ZoneVolume zone, EnemySpawner[] spawners)
        {
            foreach (EnemySpawner spawner in spawners)
            {
                if (spawner.SpawnZone == zone) return true;
                if (spawner.SpawnZone == null && zone.Contains(spawner.SpawnCenterPosition)) return true;
            }
            return false;
        }

        private static void CheckDuplicateZoneIds(List<string> errors)
        {
            ZoneVolume[] all = Object.FindObjectsByType<ZoneVolume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var seen = new Dictionary<string, ZoneVolume>();
            foreach (ZoneVolume zone in all)
            {
                if (string.IsNullOrEmpty(zone.ZoneId))
                {
                    errors.Add($"Vùng '{zone.name}' có zoneId rỗng.");
                    continue;
                }
                if (seen.TryGetValue(zone.ZoneId, out ZoneVolume first))
                {
                    errors.Add($"zoneId '{zone.ZoneId}' bị trùng giữa '{first.name}' và '{zone.name}'.");
                }
                else
                {
                    seen[zone.ZoneId] = zone;
                }
            }
        }

        private static bool IsInsideAnySafeZone(Vector2 point, SafeZoneTrigger[] safeZones, List<string> warnings, string label)
        {
            foreach (SafeZoneTrigger trigger in safeZones)
            {
                if (trigger == null) continue;
                Collider2D collider = trigger.GetComponent<Collider2D>();
                if (collider == null) continue;

                BoxCollider2D box = collider as BoxCollider2D;
                if (box != null)
                {
                    // Hộp chữ nhật tính bằng hình học (không phụ thuộc object đang bật hay tắt; content tầng 2 được lưu ở trạng thái tắt).
                    if (WorldRect(box).Contains(point)) return true;
                    continue;
                }

                if (!trigger.gameObject.activeInHierarchy || !collider.enabled)
                {
                    string message = $"{label} Safe Zone '{trigger.name}' đang tắt nên không kiểm được vị trí; hãy bật tạm rồi chạy lại.";
                    if (!warnings.Contains(message)) warnings.Add(message);
                    continue;
                }

                if (collider.OverlapPoint(point)) return true;
            }
            return false;
        }

        private static Rect WorldRect(BoxCollider2D box)
        {
            Transform t = box.transform;
            Vector3 center = t.TransformPoint(box.offset);
            Vector3 s = t.lossyScale;
            Vector2 half = new Vector2(Mathf.Abs(box.size.x * s.x), Mathf.Abs(box.size.y * s.y)) * 0.5f;
            return new Rect(center.x - half.x, center.y - half.y, half.x * 2f, half.y * 2f);
        }

        private static FloorDefinition FindFloorDefinition(string floorId)
        {
            if (string.IsNullOrEmpty(floorId)) return null;
            foreach (string guid in AssetDatabase.FindAssets("t:FloorDefinition"))
            {
                var def = AssetDatabase.LoadAssetAtPath<FloorDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && def.id == floorId) return def;
            }
            return null;
        }

        private static Vector2 ZoneCenter(ZoneVolume zone)
        {
            Collider2D collider = zone.GetComponent<Collider2D>();
            return collider != null ? (Vector2)collider.bounds.center : (Vector2)zone.transform.position;
        }

        private static void Report(List<string> errors, List<string> warnings)
        {
            var sb = new StringBuilder();
            sb.AppendLine(errors.Count == 0 ? "Bố cục vùng: KHÔNG có lỗi." : $"Bố cục vùng: {errors.Count} lỗi.");
            foreach (string e in errors) sb.AppendLine("  LỖI: " + e);
            foreach (string w in warnings) sb.AppendLine("  Cảnh báo: " + w);

            string text = sb.ToString();
            if (errors.Count > 0) Debug.LogError("[ZoneLayoutValidator] " + text);
            else if (warnings.Count > 0) Debug.LogWarning("[ZoneLayoutValidator] " + text);
            else Debug.Log("[ZoneLayoutValidator] " + text);

            EditorUtility.DisplayDialog("Validate Floor Zones", text, "OK");
        }
    }
}
