using UnityEngine;

namespace Game.Core.Combat
{
    [CreateAssetMenu(fileName = "CombatBalanceConfig", menuName = "Game/Combat/Balance Config")]
    public class CombatBalanceConfig : ScriptableObject
    {
        [Tooltip("Defense weight PvE (docs/07 §4.2). Mốc 1: 0.3")]
        public double defenseWeight = 0.3;
    }
}
