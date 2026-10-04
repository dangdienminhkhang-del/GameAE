namespace Game.Core.Combat
{
    public interface IDamageable
    {
        double CurrentHealth { get; }
        double MaxHealth { get; }
        bool IsDead { get; }

        void TakeDamage(DamageInfo damage);
        void Heal(double amount);
        void SetMaxHealth(double maxHp, bool resetCurrent = false);
    }

    public class DamageInfo
    {
        public double Amount;
        public bool IsCritical;
        public double CriticalMultiplier = 1.5;
        public string SourceId;
        public object Source;

        public DamageInfo(double amount)
        {
            Amount = amount;
        }
    }
}
