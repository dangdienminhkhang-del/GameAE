using Game.Core.Combat;
using UnityEngine;

namespace Game.Core.Stats
{
    /// <summary>
    /// Chỉ số nhân vật = Base (theo Level) + Equipment bonus (docs/02 §6).
    /// Override MaxHealth (khi cần) luôn thắng công thức.
    /// </summary>
    public class CharacterStats : ICombatStats
    {
        public int Level { get; private set; }
        public double MaxHealth { get; private set; }
        public double Attack { get; private set; }
        public double Defense { get; private set; }
        public double MoveSpeed { get; private set; }
        public double CriticalChance { get; private set; }
        public double CriticalDamage { get; private set; }

        private double _baseMaxHealth;
        private double _baseAttack;
        private double _baseDefense;
        private double _bonusMaxHealth;
        private double _bonusAttack;
        private double _bonusDefense;
        private double? _maxHealthOverride;

        public CharacterStats(int level = 1)
        {
            SetLevel(level);
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Max(1, level);
            _baseMaxHealth = BaseMaxHealthForLevel(Level);
            _baseAttack = 10 + (Level - 1) * 2;
            _baseDefense = 5 + (Level - 1) * 1;
            MoveSpeed = 4.0;
            CriticalChance = 0.10;
            CriticalDamage = 1.5;
            Recompute();
        }

        /// <summary>MaxHealth gốc theo level (docs/07 §1) — KHÔNG gồm trang bị.</summary>
        public static double BaseMaxHealthForLevel(int level)
        {
            int safeLevel = Mathf.Max(1, level);
            return 100 + (safeLevel - 1) * 15;
        }

        /// <summary>Tổng chỉ số cộng thêm từ trang bị đang mặc.</summary>
        public void SetEquipmentBonus(double attack, double defense, double maxHealth)
        {
            _bonusAttack = attack;
            _bonusDefense = defense;
            _bonusMaxHealth = maxHealth;
            Recompute();
        }

        public void SetMaxHealthOverride(double max)
        {
            _maxHealthOverride = Mathf.Max(1f, (float)max);
            Recompute();
        }

        public static int ExpRequiredForNextLevel(int currentLevel)
        {
            // Làm tròn (không floor) để khớp đúng bảng số liệu ở docs/07 §1 (vd: level 2 = 283, không phải 282).
            return UnityEngine.Mathf.RoundToInt(100f * UnityEngine.Mathf.Pow(currentLevel, 1.5f));
        }

        private void Recompute()
        {
            MaxHealth = _maxHealthOverride ?? (_baseMaxHealth + _bonusMaxHealth);
            Attack = _baseAttack + _bonusAttack;
            Defense = _baseDefense + _bonusDefense;
        }
    }
}
