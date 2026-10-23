using Game.UI.Zones;
using NUnit.Framework;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — banner tên vùng: chống hiện lại trong 5 giây và độ mờ theo thời gian.</summary>
    public class ZoneBannerTests
    {
        [Test]
        public void FirstShow_IsAllowed()
        {
            var cooldown = new ZoneBannerCooldown();
            Assert.IsTrue(cooldown.TryShow("zone.a", 10f));
        }

        [Test]
        public void SameZoneWithinCooldown_IsBlocked()
        {
            var cooldown = new ZoneBannerCooldown(5f);
            cooldown.TryShow("zone.a", 10f);

            Assert.IsFalse(cooldown.TryShow("zone.a", 12f));
            Assert.IsFalse(cooldown.TryShow("zone.a", 14.9f));
        }

        [Test]
        public void SameZoneAfterCooldown_IsAllowedAgain()
        {
            var cooldown = new ZoneBannerCooldown(5f);
            cooldown.TryShow("zone.a", 10f);

            Assert.IsTrue(cooldown.TryShow("zone.a", 15f));
        }

        [Test]
        public void BlockedAttempt_DoesNotExtendCooldown()
        {
            var cooldown = new ZoneBannerCooldown(5f);
            cooldown.TryShow("zone.a", 10f);
            cooldown.TryShow("zone.a", 14f); // bị chặn, không được tính là lần hiện

            Assert.IsTrue(cooldown.TryShow("zone.a", 15f));
        }

        [Test]
        public void DifferentZones_AreIndependent()
        {
            var cooldown = new ZoneBannerCooldown(5f);
            cooldown.TryShow("zone.a", 10f);

            Assert.IsTrue(cooldown.TryShow("zone.b", 10.5f));
            Assert.IsFalse(cooldown.TryShow("zone.a", 11f));
        }

        [Test]
        public void EmptyId_NeverShows()
        {
            var cooldown = new ZoneBannerCooldown();
            Assert.IsFalse(cooldown.TryShow(null, 1f));
            Assert.IsFalse(cooldown.TryShow(string.Empty, 1f));
        }

        [Test]
        public void Clear_ForgetsHistory()
        {
            var cooldown = new ZoneBannerCooldown(5f);
            cooldown.TryShow("zone.a", 10f);
            cooldown.Clear();

            Assert.IsTrue(cooldown.TryShow("zone.a", 10.1f));
        }

        [Test]
        public void DefaultCooldown_IsFiveSeconds()
        {
            Assert.AreEqual(5f, ZoneBannerCooldown.DefaultCooldownSeconds);
        }

        [Test]
        public void Fade_FullyVisibleThenFadesThenGone()
        {
            Assert.AreEqual(1f, ZoneBannerFade.Alpha(0f), 0.0001f);
            Assert.AreEqual(1f, ZoneBannerFade.Alpha(1.4f), 0.0001f);
            Assert.AreEqual(0.5f, ZoneBannerFade.Alpha(1.75f), 0.0001f);
            Assert.AreEqual(0f, ZoneBannerFade.Alpha(2f), 0.0001f);
            Assert.AreEqual(0f, ZoneBannerFade.Alpha(5f), 0.0001f);
        }

        [Test]
        public void Fade_NegativeElapsedOrZeroVisible_IsZero()
        {
            Assert.AreEqual(0f, ZoneBannerFade.Alpha(-1f));
            Assert.AreEqual(0f, ZoneBannerFade.Alpha(0f, 0f, 0.5f));
        }

        [Test]
        public void Fade_ZeroFadeSeconds_StaysFullUntilEnd()
        {
            Assert.AreEqual(1f, ZoneBannerFade.Alpha(1.99f, 2f, 0f), 0.0001f);
            Assert.AreEqual(0f, ZoneBannerFade.Alpha(2f, 2f, 0f), 0.0001f);
        }

        [Test]
        public void Fade_FadeLongerThanVisible_DoesNotProduceInvalidAlpha()
        {
            float alpha = ZoneBannerFade.Alpha(0.5f, 1f, 3f);
            Assert.GreaterOrEqual(alpha, 0f);
            Assert.LessOrEqual(alpha, 1f);
        }
    }
}
