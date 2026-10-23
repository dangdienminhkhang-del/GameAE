using System;
using Game.Core.Events;
using Game.PvE.Boss;
using Game.PvE.Enemy;
using UnityEngine;

namespace Game.PvE.Items
{
    /// <summary>
    /// Ví tiền "Col" của player. Kiếm Col khi hạ quái; tiêu ở cửa hàng NPC.
    /// Lưu/tải do PlayerInventory đảm nhiệm (nơi duy nhất gọi save).
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        [Tooltip("Col khởi điểm cho nhân vật mới (chưa có save).")]
        [SerializeField] private int startingCol = 150;
        [Tooltip("Col nhận được = EXP của quái ÷ số này (tối thiểu 1).")]
        [SerializeField] private int expPerColDivisor = 3;
        [SerializeField] private int bossReward = 300;

        private int _col;
        private bool _initialized;

        public int Col { get { EnsureInit(); return _col; } }
        public event Action Changed;
        /// <summary>Vừa nhận Col do hạ quái (cho hiệu ứng/HUD).</summary>
        public event Action<int> Earned;

        private void EnsureInit()
        {
            if (_initialized) return;
            _initialized = true;
            _col = Mathf.Max(0, startingCol);
        }

        private void OnEnable()
        {
            EnsureInit();
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
        }

        private void HandleEnemyDefeated(object sender, EventArgs e)
        {
            int reward = 0;
            if (sender is EnemyController enemy) reward = Mathf.Max(1, enemy.ExperienceReward / Mathf.Max(1, expPerColDivisor));
            else if (sender is BossController) reward = bossReward;
            if (reward <= 0) return;

            Add(reward);
            Earned?.Invoke(reward);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            EnsureInit();
            _col = (int)Math.Min((long)_col + amount, 999999999L);
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            EnsureInit();
            if (amount < 0 || _col < amount) return false;
            _col -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Nạp từ save. value &lt; 0 = save cũ chưa có Col → dùng số khởi điểm.</summary>
        public void LoadFromSave(int value)
        {
            _initialized = true;
            _col = value < 0 ? Mathf.Max(0, startingCol) : value;
            Changed?.Invoke();
        }
    }
}
