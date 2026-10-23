using System.Reflection;
using Game.Core.Combat;
using Game.Core.Presentation;
using Game.PvE.Boss;
using Game.PvE.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// BossController + lớp phủ HitFlash (ADR-029): trúng đòn lúc boss đang báo đòn thì màu báo đòn NGUYÊN VẸN,
    /// lớp phủ nhạt (≤ 0.5); vào lại tầng (ResetEncounter) trả lớp phủ về ẩn hoàn toàn.
    /// Boss không có definition trong test: máu mặc định 100, không có Enrage/luồng chết nên đòn nhỏ không kích hoạt gì thêm.
    /// </summary>
    public class BossPresentationTests
    {
        private static readonly Color AttackTelegraph = new Color(1f, 0.8f, 0.2f, 1f);
        private static readonly Color Punish = new Color(0.7f, 0.7f, 1f, 1f);

        private GameObject _go;
        private SpriteRenderer _renderer;
        private BossController _boss;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PresentationTestBoss");
            _renderer = _go.AddComponent<SpriteRenderer>();
            _boss = _go.AddComponent<BossController>();

            // EditMode không tự chạy Awake: gọi tay để có máy trạng thái và SpriteTint.
            MethodInfo awake = typeof(BossController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(_boss, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private void SetStateTint(Color color, TintKind kind)
        {
            MethodInfo setTint = typeof(BossController).GetMethod(
                "SetTint", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Color), typeof(TintKind) }, null);
            Assert.IsNotNull(setTint, "BossController.SetTint(Color, TintKind) phải tồn tại");
            setTint.Invoke(_boss, new object[] { color, kind });
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-4f, "r");
            Assert.AreEqual(expected.g, actual.g, 1e-4f, "g");
            Assert.AreEqual(expected.b, actual.b, 1e-4f, "b");
            Assert.AreEqual(expected.a, actual.a, 1e-4f, "a");
        }

        [Test]
        public void HitDuringTelegraph_KeepsTelegraphColor_AndOverlayIsCappedAtMaxUnderDanger()
        {
            SetStateTint(AttackTelegraph, TintKind.DangerTelegraph);

            _boss.TakeDamage(new DamageInfo(5.0));

            Assert.IsFalse(_boss.IsDead);
            AssertColor(AttackTelegraph, _renderer.color);
            HitFlashOverlay overlay = _go.GetComponent<HitFlashOverlay>();
            Assert.IsNotNull(overlay);
            Assert.IsTrue(overlay.IsVisible);
            Assert.LessOrEqual(overlay.Alpha, CombatFeelConfig.Active.FlashMaxAlphaUnderDanger + 1e-4f);
            Assert.Greater(overlay.Alpha, 0f);
        }

        [Test]
        public void HitDuringPunishWindow_KeepsPunishColor_AndFlashIsFull()
        {
            SetStateTint(Punish, TintKind.Window);

            _boss.TakeDamage(new DamageInfo(5.0));

            AssertColor(Punish, _renderer.color);
            Assert.AreEqual(CombatFeelConfig.Active.FlashStartAlpha, _go.GetComponent<HitFlashOverlay>().Alpha, 1e-4f);
        }

        [Test]
        public void ResetEncounter_AfterHit_ReturnsOverlayToHidden_AlphaZero_AndColorToBase()
        {
            SetStateTint(AttackTelegraph, TintKind.DangerTelegraph);
            _boss.TakeDamage(new DamageInfo(5.0));
            HitFlashOverlay overlay = _go.GetComponent<HitFlashOverlay>();
            Assert.IsTrue(overlay.IsVisible);

            _boss.ResetEncounter();

            Assert.IsFalse(overlay.IsVisible);
            Assert.AreEqual(0f, overlay.Alpha, 1e-6f);
            Assert.IsFalse(overlay.IsFlashing);
            AssertColor(Color.white, _renderer.color);
        }
    }
}
