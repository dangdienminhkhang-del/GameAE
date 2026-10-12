namespace Game.Core.Combat
{
    /// <summary>
    /// Balance runtime — nạp từ CombatBalanceConfig asset qua PvEBootstrap.Awake.
    /// </summary>
    public static class CombatBalance
    {
        public static double DefenseWeight { get; set; } = 1.0;
    }
}
