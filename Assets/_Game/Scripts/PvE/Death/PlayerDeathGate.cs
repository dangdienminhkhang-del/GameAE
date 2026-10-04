using System;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.World;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Death
{
    /// <summary>
    /// Cổng permadeath (docs/09 §1, docs/03 §12), FAIL-CLOSED:
    /// - Vào PvE: khóa điều khiển NGAY, chỉ mở khóa khi đã xác minh tài khoản CÒN SỐNG.
    /// - Đã chết (isDead = true) → giữ khóa + mở Death Screen. Nhờ vậy "chết là hết" đúng cả khi tắt app vào lại.
    /// - Không đọc được trạng thái sau nhiều lần thử → giữ khóa + báo lỗi (không "mặc định là còn sống").
    /// - Chết ngay trong phiên → khóa điều khiển ngay (trước đây chỉ khóa khi nạp lại).
    /// </summary>
    public class PlayerDeathGate : MonoBehaviour
    {
        private const int MaxVerifyAttempts = 3;
        private const float RetryDelaySeconds = 1f;

        [SerializeField] private Player.PlayerController playerController;
        [SerializeField] private Player.PlayerAttack playerAttack;

        private bool _isDead;

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            if (playerAttack == null) TryGetComponent(out playerAttack);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private async void Start()
        {
            // Khóa trước, xác minh sau: nếu không đọc được thì người chơi cũng chưa kịp đi/đánh.
            SetControlsEnabled(false);

            try
            {
                await VerifyAliveAsync();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Death] Lỗi khi xác minh trạng thái tài khoản: {error}", this);
                BlockAsUnverified();
            }
        }

        private async Task VerifyAliveAsync()
        {
            IDeathService deathService = ServiceProvider.CreateDeathService();

            for (int attempt = 1; attempt <= MaxVerifyAttempts; attempt++)
            {
                try
                {
                    DeathInfo info = await deathService.LoadDeathInfoAsync();
                    if (this == null) return; // scene đã bị hủy trong lúc chờ

                    if (info != null && info.isDead)
                    {
                        _isDead = true;
                        SetControlsEnabled(false);
                        GameEvents.RaisePlayerDeathRecorded(this, info);
                        return;
                    }

                    // Đã xác minh còn sống → mở khóa (trừ khi vừa chết trong lúc đang chờ).
                    if (!_isDead) SetControlsEnabled(true);
                    return;
                }
                catch (SaveUnavailableException error)
                {
                    Debug.LogWarning($"[Death] Chưa xác minh được tài khoản (lần {attempt}/{MaxVerifyAttempts}): {error.Message}", this);
                    if (attempt < MaxVerifyAttempts) await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds));
                    if (this == null) return;
                }
            }

            BlockAsUnverified();
        }

        private void BlockAsUnverified()
        {
            if (this == null) return;
            SetControlsEnabled(false);
            GameEvents.RaiseDeathStateUnavailable(this);
        }

        private void HandlePlayerDied(object sender, DamageInfo lastHit)
        {
            _isDead = true;
            SetControlsEnabled(false);
        }

        private void SetControlsEnabled(bool enabledState)
        {
            if (playerController != null) playerController.enabled = enabledState;
            if (playerAttack != null) playerAttack.enabled = enabledState;

            if (!enabledState)
            {
                var body = GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
            }
        }
    }
}
