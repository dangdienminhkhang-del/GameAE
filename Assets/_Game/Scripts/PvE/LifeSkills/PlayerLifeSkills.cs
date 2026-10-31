using System;
using System.Collections.Generic;
using Game.Core.LifeSkills;
using UnityEngine;

namespace Game.PvE.LifeSkills
{
    /// <summary>
    /// Quản lý kỹ năng đời sống của nhân vật: Câu cá & Nấu ăn (theo Sword Art Online).
    /// Quản lý buff món ăn tạm thời (ví dụ: +ATK trong 3-5 phút).
    /// Tự gắn trên Player lúc chạy.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerLifeSkills : MonoBehaviour
    {
        private int _fishingProficiency;
        private int _cookingProficiency;

        private readonly List<CookingBuffInstance> _activeBuffs = new List<CookingBuffInstance>();

        public int FishingProficiency => _fishingProficiency;
        public int CookingProficiency => _cookingProficiency;

        public int FishingLevel => LifeSkillRules.GetSkillLevel(_fishingProficiency);
        public int CookingLevel => LifeSkillRules.GetSkillLevel(_cookingProficiency);

        public IReadOnlyList<CookingBuffInstance> ActiveBuffs => _activeBuffs;

        /// <summary>Tổng điểm tấn công tăng thêm từ tất cả món ăn đang có hiệu lực.</summary>
        public double TotalAttackBonus
        {
            get
            {
                double total = 0.0;
                for (int i = 0; i < _activeBuffs.Count; i++)
                {
                    total += _activeBuffs[i].attackBonus;
                }
                return total;
            }
        }

        public event Action Changed;
        public event Action<CookingBuffInstance> BuffApplied;

        private void Update()
        {
            if (_activeBuffs.Count == 0) return;

            float dt = Time.deltaTime;
            bool anyRemoved = false;
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _activeBuffs[i];
                buff.Tick(dt);
                if (buff.IsExpired)
                {
                    _activeBuffs.RemoveAt(i);
                    anyRemoved = true;
                }
            }

            if (anyRemoved)
            {
                Changed?.Invoke();
            }
        }

        public void AddFishingExp(int amount)
        {
            if (amount <= 0) return;
            _fishingProficiency += amount;
            Changed?.Invoke();
        }

        public void AddCookingExp(int amount)
        {
            if (amount <= 0) return;
            _cookingProficiency += amount;
            Changed?.Invoke();
        }

        public void ApplyBuff(CookingBuffInstance buff)
        {
            if (buff == null) return;
            _activeBuffs.Add(buff);
            BuffApplied?.Invoke(buff);
            Changed?.Invoke();
        }

        public void LoadFromSave(int fishingProf, int cookingProf)
        {
            _fishingProficiency = Mathf.Max(0, fishingProf);
            _cookingProficiency = Mathf.Max(0, cookingProf);
            Changed?.Invoke();
        }
    }
}
