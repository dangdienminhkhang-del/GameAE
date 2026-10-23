using Game.Core.Presentation;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Đồng hồ của <see cref="PlaySpeedState"/>: mỗi frame đếm lùi dừng hình bằng thời gian THẬT.
    /// Đặt MỘT object trong scene gameplay. Khi bị tắt/hủy (đổi scene, thoát Play Mode) tự trả tốc độ về 1.
    /// Cũng đăng ký <see cref="CombatFeelConfig"/> làm cấu hình đang dùng cho mọi hiệu ứng chiến đấu.
    /// </summary>
    public sealed class PlaySpeedTicker : MonoBehaviour
    {
        [Tooltip("Asset CombatFeelConfig của game. Để trống = dùng số mặc định trong code.")]
        [SerializeField] private CombatFeelConfig combatFeelConfig;

        private void OnEnable()
        {
            if (combatFeelConfig != null) CombatFeelConfig.SetActive(combatFeelConfig);
        }

        private void Update()
        {
            PlaySpeedState.Shared.Tick(UnityEngine.Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            CombatFeelConfig.ClearActive(combatFeelConfig);
            PlaySpeedState.Shared.ForceReset();
        }
    }
}
