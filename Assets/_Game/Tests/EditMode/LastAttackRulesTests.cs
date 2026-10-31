using System.IO;
using Game.Core.Combat;
using Game.Core.Items;
using Game.PvE.Boss;
using Game.UI.Boss;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Kiểm thử quy tắc đòn kết liễu Boss (Last Attack Bonus - LA) theo chuẩn SAO.
    /// </summary>
    public class LastAttackRulesTests
    {
        [Test]
        public void IsPlayerLastAttack_DirectPlayerDamage_ReturnsTrue()
        {
            var hitPlayerSourceId = new DamageInfo(50.0) { SourceId = "player" };
            Assert.IsTrue(LastAttackRules.IsPlayerLastAttack(hitPlayerSourceId));

            var hitPlayerPrefix = new DamageInfo(50.0) { SourceId = "player_sword" };
            Assert.IsTrue(LastAttackRules.IsPlayerLastAttack(hitPlayerPrefix));

            var goPlayer = new GameObject("PlayerCharacter");
            goPlayer.tag = "Player";
            try
            {
                var hitPlayerGo = new DamageInfo(50.0) { Source = goPlayer };
                Assert.IsTrue(LastAttackRules.IsPlayerLastAttack(hitPlayerGo));
            }
            finally
            {
                Object.DestroyImmediate(goPlayer);
            }
        }

        [Test]
        public void IsPlayerLastAttack_PeriodicDamageOrDoT_ReturnsFalse()
        {
            // Cờ IsPeriodic
            var dotHit = new DamageInfo(20.0) { SourceId = "player", IsPeriodic = true };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(dotHit), "Đòn DoT/Periodic không được tính là Last Attack.");

            // Tên nguồn DoT
            var poisonHit = new DamageInfo(15.0) { SourceId = "dot_poison" };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(poisonHit));

            var bleedHit = new DamageInfo(15.0) { SourceId = "bleed_effect" };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(bleedHit));
        }

        [Test]
        public void IsPlayerLastAttack_AICompanionOrEnvironment_ReturnsFalse()
        {
            var allyHit = new DamageInfo(30.0) { SourceId = "ally_npc_01" };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(allyHit), "Đồng đội AI không được tính là Last Attack của người chơi.");

            var petHit = new DamageInfo(10.0) { SourceId = "companion_pet" };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(petHit));

            var trapHit = new DamageInfo(50.0) { SourceId = "hazard_lava" };
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(trapHit));
        }

        [Test]
        public void IsPlayerLastAttack_NullDamage_ReturnsFalse()
        {
            Assert.IsFalse(LastAttackRules.IsPlayerLastAttack(null));
        }

        [Test]
        public void BossDefeatReportPanelModel_LastAttackBonusText_FormatsCorrectly()
        {
            var model = new BossDefeatReportPanelModel();
            Assert.IsNull(model.LastAttackBonusText);
            Assert.AreEqual(BossDefeatReportPanelModel.ConfirmedMessage, model.ConfirmedToastMessage);

            model.SetLastAttackBonus("Áo Choàng Bóng Đêm");
            StringAssert.Contains("Áo Choàng Bóng Đêm", model.LastAttackBonusText);
            StringAssert.Contains("LAST ATTACK BONUS", model.LastAttackBonusText);
            StringAssert.Contains("Áo Choàng Bóng Đêm", model.ConfirmedToastMessage);
        }

        [Test]
        public void BossDefinition_LastAttackRewardItem_PropertiesWork()
        {
            var def = ScriptableObject.CreateInstance<BossDefinition>();
            var item = ScriptableObject.CreateInstance<ItemDefinition>();

            typeof(BossDefinition).GetField("lastAttackRewardItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, item);
            typeof(BossDefinition).GetField("lastAttackRewardQuantity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, 2);

            try
            {
                Assert.AreEqual(item, def.LastAttackRewardItem);
                Assert.AreEqual(2, def.LastAttackRewardQuantity);
            }
            finally
            {
                Object.DestroyImmediate(def);
                Object.DestroyImmediate(item);
            }
        }
    }
}
