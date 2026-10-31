using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Skills;
using Game.PvE.Player;
using Game.PvE.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class PostSkillDelayTests
    {
        [Test]
        public void CalculateDelay_SingleHitNormalDamage_ReturnsBaseRecovery()
        {
            float delay = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.25f, hitCount: 1, totalMultiplier: 1.0);
            Assert.AreEqual(0.25f, delay, 1e-4f, "Kỹ năng 1 hit sát thương chuẩn phải giữ nguyên recovery cơ bản.");
        }

        [Test]
        public void CalculateDelay_MultiHitCombo_IncreasesFreezeDuration()
        {
            float singleHit = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.30f, hitCount: 1, totalMultiplier: 1.0);
            float quadHit = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.30f, hitCount: 4, totalMultiplier: 1.0);

            Assert.Greater(quadHit, singleHit, "Chiêu 4-hit combo phải có thời gian khựng lâu hơn chiêu 1 hit.");
            Assert.AreEqual(0.30f + (3 * PostSkillDelayRules.DelayPerExtraHit), quadHit, 1e-4f);
        }

        [Test]
        public void CalculateDelay_HighDamageSkill_IncreasesFreezeDuration()
        {
            float normalDmg = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.25f, hitCount: 1, totalMultiplier: 1.2);
            float heavyDmg = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.25f, hitCount: 1, totalMultiplier: 3.5);

            Assert.Greater(heavyDmg, normalDmg, "Chiêu sát thương cực lớn (hệ số 3.5×) phải khựng lâu hơn chiêu thường.");
        }

        [Test]
        public void CalculateDelay_ClampsToMinAndMax()
        {
            float veryLow = PostSkillDelayRules.CalculateDelay(baseRecovery: 0.01f, hitCount: 1, totalMultiplier: 0.5);
            Assert.AreEqual(PostSkillDelayRules.MinDelaySeconds, veryLow, 1e-4f, "Phải clamp ở mức khựng tối thiểu.");

            float extreme = PostSkillDelayRules.CalculateDelay(baseRecovery: 5.0f, hitCount: 20, totalMultiplier: 50.0);
            Assert.AreEqual(PostSkillDelayRules.MaxDelaySeconds, extreme, 1e-4f, "Phải clamp ở mức khựng tối đa.");
        }

        [Test]
        public void CalculateDelay_FromDefinition_CalculatesAccurately()
        {
            var def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            SetPrivateField(def, "recoverySeconds", 0.35f);
            SetPrivateField(def, "totalMultiplier", 2.5);

            var windows = new List<TechniqueHitWindow>
            {
                new TechniqueHitWindow { startTimeOffset = 0f, duration = 0.1f, hitMultiplier = 1.0 },
                new TechniqueHitWindow { startTimeOffset = 0.15f, duration = 0.1f, hitMultiplier = 1.5 }
            };
            SetPrivateField(def, "activeWindows", windows);

            try
            {
                float delay = PostSkillDelayRules.CalculateDelay(def);
                Assert.Greater(delay, 0.35f, "Chiêu 2-hit damage 2.5 phải có delay > base 0.35s.");
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void PostSkillDelayRules_IsDelayActive_And_CanAct()
        {
            float endTime = 10.0f;

            Assert.IsTrue(PostSkillDelayRules.IsDelayActive(endTime, 5.0f), "5.0s < 10.0s -> delay vẫn còn hoạt động.");
            Assert.IsFalse(PostSkillDelayRules.CanAct(endTime, 5.0f), "Đang delay thì không được hành động.");
            Assert.AreEqual(5.0f, PostSkillDelayRules.GetRemainingDelay(endTime, 5.0f), 1e-4f);

            Assert.IsFalse(PostSkillDelayRules.IsDelayActive(endTime, 10.5f), "10.5s > 10.0s -> đã hết delay.");
            Assert.IsTrue(PostSkillDelayRules.CanAct(endTime, 10.5f), "Hết delay thì được hành động bình thường.");
            Assert.AreEqual(0f, PostSkillDelayRules.GetRemainingDelay(endTime, 10.5f), 1e-4f);
        }

        [Test]
        public void PlayerController_IsActionLocked_PreventsMovement()
        {
            var go = new GameObject("PlayerTest", typeof(Rigidbody2D), typeof(PlayerController));
            var pc = go.GetComponent<PlayerController>();
            var rb = go.GetComponent<Rigidbody2D>();
            SetPrivateField(pc, "_rb", rb);

            try
            {
                pc.IsActionLocked = true;
                Assert.IsTrue(pc.IsActionLocked);

                // Giả lập rb có vận tốc ban đầu
                rb.linearVelocity = new Vector2(5f, 5f);

                // Gọi FixedUpdate qua reflection
                typeof(PlayerController).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(pc, null);

                Assert.AreEqual(Vector2.zero, rb.linearVelocity, "Khi IsActionLocked, linearVelocity phải bị ép về 0.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlayerAttack_IsReady_FalseWhenPlayerActionLocked()
        {
            var go = new GameObject("PlayerTest", typeof(Rigidbody2D), typeof(PlayerController), typeof(PlayerAttack));
            var pc = go.GetComponent<PlayerController>();
            var attack = go.GetComponent<PlayerAttack>();

            try
            {
                pc.IsActionLocked = true;
                Assert.IsFalse(attack.IsReady, "Đang bị khóa hành động thì PlayerAttack không được sẵn sàng.");
                Assert.IsFalse(attack.TryAttack(), "TryAttack phải trả về false khi bị ActionLocked.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PostSkillDelayController_ApplyFreeze_LocksPlayerAction()
        {
            var go = new GameObject("PlayerTest", typeof(Rigidbody2D), typeof(PlayerController), typeof(PostSkillDelayController));
            var pc = go.GetComponent<PlayerController>();
            var freezeCtrl = go.GetComponent<PostSkillDelayController>();

            try
            {
                Assert.IsFalse(freezeCtrl.IsFrozen);
                Assert.IsFalse(pc.IsActionLocked);

                freezeCtrl.ApplyFreeze(1.5f, "TestSkill");
                Assert.IsTrue(freezeCtrl.IsFrozen, "Sau ApplyFreeze, IsFrozen phải là true.");
                Assert.IsTrue(pc.IsActionLocked, "Sau ApplyFreeze, PlayerController.IsActionLocked phải là true.");
                Assert.Greater(freezeCtrl.RemainingFreezeTime, 0f);
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
