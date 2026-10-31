using System;
using Game.Core.Events;
using Game.Core.Stats;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Cầu nối giữa luật cộng điểm STR/AGI (<see cref="StatAllocation"/>) và người chơi: giữ điểm đã cộng,
    /// đẩy hiệu ứng vào <see cref="CharacterStats"/>, báo UI/save khi đổi. Điểm chưa dùng = 3 × (level − 1) − đã cộng.
    /// Lưu/nạp do PlayerInventory đảm nhiệm (nơi duy nhất gọi save profile). Tự được PlayerController gắn vào.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStatAllocation : MonoBehaviour
    {
        private readonly StatAllocation _allocation = new StatAllocation();
        private PlayerProgress _progress;

        /// <summary>Phát khi điểm đã cộng đổi hoặc điểm chưa dùng đổi (lên cấp).</summary>
        public event Action Changed;

        public int Strength => _allocation.Strength;
        public int Agility => _allocation.Agility;
        public int Unspent => _allocation.Unspent(CurrentLevel);

        private int CurrentLevel
        {
            get
            {
                if (_progress == null) TryGetComponent(out _progress);
                return _progress != null ? _progress.Level : 1;
            }
        }

        private void Awake()
        {
            TryGetComponent(out _progress);
        }

        private void OnEnable()
        {
            _allocation.Changed += HandleAllocationChanged;
            GameEvents.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            _allocation.Changed -= HandleAllocationChanged;
            GameEvents.OnLevelUp -= HandleLevelUp;
        }

        private void Start()
        {
            ApplyToStats();
        }

        /// <summary>Cộng 1 điểm vào chỉ số. False nếu hết điểm.</summary>
        public bool TryAllocate(AllocatableStat stat)
        {
            return _allocation.TryAllocate(stat, CurrentLevel);
        }

        /// <summary>Nạp từ save — gọi SAU khi level đã được nạp (PlayerInventory.LoadAsync lo thứ tự này).</summary>
        public void LoadFromSave(int strength, int agility)
        {
            _allocation.LoadFrom(strength, agility, CurrentLevel);
        }

        private void HandleAllocationChanged()
        {
            ApplyToStats();
            Changed?.Invoke();
        }

        private void HandleLevelUp(object sender, int newLevel)
        {
            Changed?.Invoke(); // điểm chưa dùng tăng → UI cập nhật
        }

        private void ApplyToStats()
        {
            CharacterStats stats = _progress != null ? _progress.Stats : null;
            if (stats == null && TryGetComponent(out _progress)) stats = _progress.Stats;
            if (stats == null) return;

            stats.SetAllocationBonus(
                StatAllocation.AttackBonus(_allocation.Strength),
                StatAllocation.MoveSpeedFractionBonus(_allocation.Agility),
                StatAllocation.CritChanceBonus(_allocation.Agility));
            GameEvents.RaiseStatsChanged(this);
        }
    }
}
