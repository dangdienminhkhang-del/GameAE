using System.Reflection;
using Game.Core.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Giải màu nhân (ADR-029): Chết &gt; màu trạng thái hiện tại &gt; (gốc = trắng, là một màu trạng thái Base).
    /// Nháy khi trúng KHÔNG còn là lớp màu nhân nên màu trạng thái luôn được trả nguyên vẹn.
    /// </summary>
    public class TintResolverTests
    {
        // Bảng màu báo đòn ở docs/14 §2 (giá trị phải giữ nguyên, ADR-029).
        private static readonly Color EnemyTelegraph = new Color(1f, 0.55f, 0.55f, 1f);
        private static readonly Color BossTelegraph = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly Color BossPunish = new Color(0.7f, 0.7f, 1f, 1f);
        private static readonly Color BossPressure = new Color(0.75f, 0.2f, 1f, 1f);
        private static readonly Color BossEnrage = new Color(1f, 0.5f, 0.5f, 1f);
        private static readonly Color Dead = new Color(0.4f, 0.4f, 0.4f, 0.9f);

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-6f, "r");
            Assert.AreEqual(expected.g, actual.g, 1e-6f, "g");
            Assert.AreEqual(expected.b, actual.b, 1e-6f, "b");
            Assert.AreEqual(expected.a, actual.a, 1e-6f, "a");
        }

        [Test]
        public void NotDead_ReturnsStateTintUnchanged_ForEveryStateColor()
        {
            Color[] states = { Color.white, EnemyTelegraph, BossTelegraph, BossPunish, BossPressure, BossEnrage };
            foreach (Color state in states)
            {
                AssertColor(state, TintResolver.Resolve(new TintState(state, false, Dead)));
            }
        }

        [Test]
        public void NotDead_KeepsStateAlpha_NotOverwritten()
        {
            var faded = new Color(1f, 0.55f, 0.55f, 0.5f);
            AssertColor(faded, TintResolver.Resolve(new TintState(faded, false, Dead)));
        }

        [Test]
        public void Dead_BeatsEveryStateColor()
        {
            Color[] states = { Color.white, EnemyTelegraph, BossTelegraph, BossPunish, BossPressure, BossEnrage };
            foreach (Color state in states)
            {
                AssertColor(Dead, TintResolver.Resolve(new TintState(state, true, Dead)));
            }
        }

        [Test]
        public void TintState_HasNoFlashLayer_SoFlashCanNeverChangeTheStateColor()
        {
            // Nháy là lớp phủ trắng riêng (HitFlashOverlay). Nếu ai đó thêm lại trường "flash" vào TintState,
            // nháy sẽ lại có đường để đè màu báo đòn — test này chặn điều đó.
            FieldInfo[] fields = typeof(TintState).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.AreEqual(3, fields.Length, "TintState chỉ gồm StateTint, IsDead, DeadTint");
            foreach (FieldInfo field in fields)
            {
                StringAssert.DoesNotContain("flash", field.Name.ToLowerInvariant());
            }
        }

        [Test]
        public void ResolveIsStable_CalledRepeatedly_ForTheSameState()
        {
            var state = new TintState(BossPressure, false, Dead);
            Color first = TintResolver.Resolve(state);
            for (int i = 0; i < 10; i++) AssertColor(first, TintResolver.Resolve(state));
        }
    }

    public class HealthBarRulesTests
    {
        [Test]
        public void Ratio_ClampsAndHandlesBadInput()
        {
            Assert.AreEqual(0.5f, HealthBarRules.Ratio(50.0, 100.0), 1e-5f);
            Assert.AreEqual(1f, HealthBarRules.Ratio(150.0, 100.0), 1e-5f);
            Assert.AreEqual(0f, HealthBarRules.Ratio(-5.0, 100.0), 1e-5f);
            Assert.AreEqual(0f, HealthBarRules.Ratio(10.0, 0.0), 1e-5f);
            Assert.AreEqual(0f, HealthBarRules.Ratio(10.0, -1.0), 1e-5f);
            Assert.AreEqual(0f, HealthBarRules.Ratio(double.NaN, 100.0), 1e-5f);
        }

        [Test]
        public void EnemyBar_HiddenUntilHit()
        {
            Assert.IsFalse(HealthBarRules.ShouldShowEnemyBar(false, false, 0f, 3f));
            Assert.IsTrue(HealthBarRules.ShouldShowEnemyBar(true, false, 0f, 3f));
        }

        [Test]
        public void EnemyBar_HidesWhenDead_OrAfterTimeout()
        {
            Assert.IsFalse(HealthBarRules.ShouldShowEnemyBar(true, true, 0f, 3f));
            Assert.IsTrue(HealthBarRules.ShouldShowEnemyBar(true, false, 2.9f, 3f));
            Assert.IsFalse(HealthBarRules.ShouldShowEnemyBar(true, false, 3f, 3f));
        }

        [Test]
        public void BossBar_ShowsOnlyWhileEngagedAndAlive()
        {
            Assert.IsFalse(HealthBarRules.ShouldShowBossBar(false, false));
            Assert.IsTrue(HealthBarRules.ShouldShowBossBar(true, false));
            Assert.IsFalse(HealthBarRules.ShouldShowBossBar(true, true));
            Assert.IsFalse(HealthBarRules.ShouldShowBossBar(false, true));
        }
    }
}
