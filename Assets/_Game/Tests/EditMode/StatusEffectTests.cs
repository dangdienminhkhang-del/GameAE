using System.Collections.Generic;
using Game.Core.Combat;
using Game.PvE.Combat;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class StatusEffectTests
    {
        [Test]
        public void ApplyEffect_NewEffect_AddsToList()
        {
            var list = new List<StatusEffectInstance>();
            bool added = StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 8.0f, damagePerTick: 5.0, tickInterval: 1.0f);

            Assert.IsTrue(added);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(StatusEffectType.Poison, list[0].Type);
            Assert.AreEqual(8.0f, list[0].RemainingDuration);
            Assert.AreEqual(5.0, list[0].DamagePerTick);
            Assert.IsTrue(list[0].IsDoT);
            Assert.IsFalse(list[0].IsActionLocked);
        }

        [Test]
        public void ApplyEffect_ExistingEffect_RefreshesDurationAndDamage()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 5.0f, damagePerTick: 4.0);

            // Giả lập thời gian đã trôi 2s -> còn 3s
            list[0].RemainingDuration = 3.0f;

            // Áp dụng lại hiệu ứng với duration 8s và damage 6.0
            bool addedAgain = StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 8.0f, damagePerTick: 6.0);

            Assert.IsFalse(addedAgain, "Áp dụng lại hiệu ứng cũ phải trả về false (refresh).");
            Assert.AreEqual(1, list.Count, "Không được nhân đôi hiệu ứng.");
            Assert.AreEqual(8.0f, list[0].RemainingDuration, "Thời gian phải được làm mới lên 8s.");
            Assert.AreEqual(6.0, list[0].DamagePerTick, "Sát thương mỗi tick được nâng lên mức cao hơn.");
        }

        [Test]
        public void Tick_DoTEffect_FiresDamageAtInterval()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Bleed, 4.0f, damagePerTick: 10.0, tickInterval: 0.5f);

            var tickResults = new List<StatusEffectTickResult>();

            // Tick 0.2s: chưa đủ 0.5s -> chưa có damage
            StatusEffectRules.Tick(list, 0.2f, tickResults);
            Assert.AreEqual(0, tickResults.Count);

            // Tick thêm 0.35s (tổng 0.55s): đã vượt 0.5s -> phải sinh 1 nhịp sát thương 10 dmg
            StatusEffectRules.Tick(list, 0.35f, tickResults);
            Assert.AreEqual(1, tickResults.Count);
            Assert.AreEqual(StatusEffectType.Bleed, tickResults[0].Type);
            Assert.AreEqual(10.0, tickResults[0].Damage);
        }

        [Test]
        public void Tick_ExpiredEffect_RemovedFromList()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Stun, 1.5f);

            var tickResults = new List<StatusEffectTickResult>();

            // Tick 1.0s: còn 0.5s
            StatusEffectRules.Tick(list, 1.0f, tickResults);
            Assert.AreEqual(1, list.Count);

            // Tick thêm 0.6s: tổng 1.6s > 1.5s -> hết hạn và bị xóa
            StatusEffectRules.Tick(list, 0.6f, tickResults);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void HasActionLock_TrueForParalyzeAndStun_FalseForPoisonAndBleed()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 8.0f);
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Bleed, 4.0f);

            Assert.IsFalse(StatusEffectRules.HasActionLock(list), "Độc và Chảy Máu không khóa hành động.");

            StatusEffectRules.ApplyEffect(list, StatusEffectType.Paralyze, 4.0f);
            Assert.IsTrue(StatusEffectRules.HasActionLock(list), "Tê Liệt phải khóa hành động (chuẩn SAO Kuradeel).");

            StatusEffectRules.Cure(list, StatusEffectType.Paralyze);
            Assert.IsFalse(StatusEffectRules.HasActionLock(list));

            StatusEffectRules.ApplyEffect(list, StatusEffectType.Stun, 1.5f);
            Assert.IsTrue(StatusEffectRules.HasActionLock(list), "Choáng phải khóa hành động.");
        }

        [Test]
        public void Cure_SpecificType_OnlyCleansesTargetType()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 8.0f);
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Bleed, 4.0f);
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Paralyze, 4.0f);

            int removed = StatusEffectRules.Cure(list, StatusEffectType.Poison);
            Assert.AreEqual(1, removed);
            Assert.IsFalse(StatusEffectRules.HasEffect(list, StatusEffectType.Poison));
            Assert.IsTrue(StatusEffectRules.HasEffect(list, StatusEffectType.Bleed));
            Assert.IsTrue(StatusEffectRules.HasEffect(list, StatusEffectType.Paralyze));
        }

        [Test]
        public void Cure_None_CleansesAllEffects()
        {
            var list = new List<StatusEffectInstance>();
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Poison, 8.0f);
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Bleed, 4.0f);
            StatusEffectRules.ApplyEffect(list, StatusEffectType.Paralyze, 4.0f);

            int removed = StatusEffectRules.Cure(list, StatusEffectType.None);
            Assert.AreEqual(3, removed);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void StatusEffectController_ApplyParalyze_LocksPlayerAction()
        {
            var go = new GameObject("PlayerStatusTest", typeof(PlayerController), typeof(StatusEffectController));
            var pc = go.GetComponent<PlayerController>();
            var ctrl = go.GetComponent<StatusEffectController>();
            SetPrivateField(ctrl, "_playerController", pc);

            try
            {
                Assert.IsFalse(pc.IsActionLocked);

                ctrl.ApplyParalyze(4.0f);
                Assert.IsTrue(ctrl.HasEffect(StatusEffectType.Paralyze));
                Assert.IsTrue(pc.IsActionLocked, "Dính Tê Liệt phải set PlayerController.IsActionLocked = true.");

                ctrl.Cure(StatusEffectType.Paralyze);
                Assert.IsFalse(ctrl.HasEffect(StatusEffectType.Paralyze));
                Assert.IsFalse(pc.IsActionLocked, "Hóa giải Tê Liệt phải giải phóng IsActionLocked.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
    }
}
