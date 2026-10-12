using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Floor
{
    /// <summary>
    /// Định nghĩa một tầng (docs/03 §3, §4, contract 13 §4.2).
    /// Asset nằm ở Content/Floors/FloorNN/; class này thuộc Core/Floor.
    /// ID phải theo regex ^floor\.\d{2,}$ (ví dụ "floor.01").
    /// </summary>
    [CreateAssetMenu(fileName = "FloorDefinition", menuName = "Game/Floor/Floor Definition")]
    public class FloorDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ID ổn định dạng floor.NN — không đổi sau khi dùng (docs/03 §2).")]
        public string id;

        [Tooltip("Số thứ tự tầng dùng để sort (không sinh từ ID string).")]
        public int floorNumber;

        [Tooltip("Tên hiển thị cho người chơi.")]
        public string displayName;

        [Header("Scene")]
        [Tooltip("Tên scene cần load additive (Build Settings phải có scene này).")]
        public string sceneKey;

        [Header("Balance")]
        [Tooltip("Level khuyến nghị — là hướng dẫn balance, KHÔNG phải điều kiện khóa (docs/07 §1).")]
        public int recommendedLevelMin = 1;
        public int recommendedLevelMax = 5;

        [Tooltip("CHƯA DÙNG ở Mốc 1: EnemyController luôn lấy hệ số tầng từ FloorScaling. Để 0.")]
        public FloorScalingOverride scalingOverride;

        [Tooltip("Item tier ID dùng để roll drop (docs/07 §7, ví dụ 'tier.common').")]
        public string itemTierId = "tier.common";

        [Header("Content refs (IDs only — dữ liệu thật trong Content/Floors/FloorNN)")]
        [Tooltip("Danh sách ID EnemyDefinition sẽ spawn — Spawner đọc danh sách này.")]
        public List<string> spawnEnemyIds = new List<string>();

        [Tooltip("Danh sách ID NPCDefinition có mặt ở tầng này.")]
        public List<string> npcIds = new List<string>();

        [Tooltip("Danh sách ID QuestDefinition có thể nhận ở tầng này.")]
        public List<string> questIds = new List<string>();

        [Tooltip("ID boss của tầng (dạng boss.floor_NN). Để trống nếu tầng không có boss.")]
        public string bossId;

        [Header("Safe Zone")]
        [Tooltip("Danh sách ID SafeZoneConfig của tầng — trigger đặt trong scene (docs/09 §1.2).")]
        public List<string> safeZoneConfigIds = new List<string>();

        [Header("Unlock Rule")]
        [Tooltip("ID World State boss phải DEFEATED trước khi tầng này thành Available. " +
                 "Để trống nếu tầng mở mặc định (floor.01).")]
        public string requiredBossDefeatedId;

        // ── Validation helper ─────────────────────────────────────────────
        /// <summary>Trả true nếu ID hợp lệ theo regex ^floor\.\d{2,}$</summary>
        public bool HasValidId()
        {
            if (string.IsNullOrEmpty(id)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(id, @"^floor\.\d{2,}$");
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(id) && !HasValidId())
                Debug.LogWarning($"[FloorDefinition] ID '{id}' không hợp lệ. Phải theo dạng floor.NN (floor.01, floor.02...).", this);

            if (floorNumber < 1)
                Debug.LogWarning($"[FloorDefinition] floorNumber phải >= 1.", this);
        }
    }

    /// <summary>
    /// Ghi đè hệ số scaling cho tầng cụ thể (thay cho giá trị mặc định trong FloorScaling).
    /// Dùng trong FloorDefinition.scalingOverride.
    /// </summary>
    [System.Serializable]
    public class FloorScalingOverride
    {
        [Tooltip("Override enemyPowerMultiplier — nếu <= 0 thì dùng FloorScaling tính.")]
        public float enemyPowerMultiplier = 0f;

        [Tooltip("Override expMultiplier — nếu <= 0 thì dùng FloorScaling tính.")]
        public float expMultiplier = 0f;

        public float ResolveEnemyPower(int floorNumber) =>
            enemyPowerMultiplier > 0 ? enemyPowerMultiplier : FloorScaling.EnemyPowerMultiplier(floorNumber);

        public float ResolveExp(int floorNumber) =>
            expMultiplier > 0 ? expMultiplier : FloorScaling.ExpMultiplier(floorNumber);
    }
}
