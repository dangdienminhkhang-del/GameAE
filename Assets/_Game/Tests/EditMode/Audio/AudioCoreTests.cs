using System.Collections.Generic;
using Game.Core.Audio;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class AudioVolumesTests
    {
        [Test]
        public void Sanitize_ClampsToZeroOne()
        {
            Assert.AreEqual(0f, AudioVolumes.Sanitize(-0.5f, 0.7f));
            Assert.AreEqual(1f, AudioVolumes.Sanitize(3f, 0.7f));
            Assert.AreEqual(0.4f, AudioVolumes.Sanitize(0.4f, 0.7f), 1e-6f);
        }

        [Test]
        public void Sanitize_CorruptValues_FallBackToDefault()
        {
            Assert.AreEqual(0.7f, AudioVolumes.Sanitize(float.NaN, 0.7f), 1e-6f);
            Assert.AreEqual(0.7f, AudioVolumes.Sanitize(float.PositiveInfinity, 0.7f), 1e-6f);
            Assert.AreEqual(0.7f, AudioVolumes.Sanitize(float.NegativeInfinity, 0.7f), 1e-6f);
        }

        [Test]
        public void Defaults_AreWithinRange_AndKeysAreStable()
        {
            Assert.That(AudioVolumes.DefaultMusic, Is.InRange(0f, 1f));
            Assert.That(AudioVolumes.DefaultSfx, Is.InRange(0f, 1f));
            Assert.AreEqual("settings.audio.music", AudioVolumes.MusicKey);
            Assert.AreEqual("settings.audio.sfx", AudioVolumes.SfxKey);
        }

        [Test]
        public void SfxFinal_MultipliesEntryScaleAndPlayerVolume_AndStaysInRange()
        {
            Assert.AreEqual(0.4f, AudioVolumes.SfxFinal(0.8f, 1f, 0.5f), 1e-5f);
            Assert.AreEqual(0.2f, AudioVolumes.SfxFinal(0.8f, 0.5f, 0.5f), 1e-5f);
            Assert.AreEqual(1f, AudioVolumes.SfxFinal(1f, 5f, 1f), 1e-5f, "không vượt 1");
            Assert.AreEqual(0f, AudioVolumes.SfxFinal(0.8f, 1f, 0f), 1e-5f, "người chơi tắt SFX = im lặng");
            Assert.AreEqual(0f, AudioVolumes.SfxFinal(0.8f, -1f, 1f), 1e-5f, "scale âm không tạo âm lượng âm");
        }

        [Test]
        public void MusicFinal_AppliesFadeWeight()
        {
            Assert.AreEqual(0.35f, AudioVolumes.MusicFinal(0.5f, 0.7f, 1f), 1e-5f);
            Assert.AreEqual(0.175f, AudioVolumes.MusicFinal(0.5f, 0.7f, 0.5f), 1e-5f);
            Assert.AreEqual(0f, AudioVolumes.MusicFinal(0.5f, 0.7f, 0f), 1e-5f);
        }
    }

    public class SfxThrottleRulesTests
    {
        [Test]
        public void IsTooSoon_BlocksWithinInterval_AllowsAfter()
        {
            Assert.IsTrue(SfxThrottleRules.IsTooSoon(1.05, 1.0, 0.1f));
            Assert.IsFalse(SfxThrottleRules.IsTooSoon(1.20, 1.0, 0.1f));
        }

        [Test]
        public void IsTooSoon_ZeroOrNegativeInterval_NeverBlocks()
        {
            Assert.IsFalse(SfxThrottleRules.IsTooSoon(1.0, 1.0, 0f));
            Assert.IsFalse(SfxThrottleRules.IsTooSoon(1.0, 1.0, -1f));
        }

        [Test]
        public void HasFreeVoice_ComparesAgainstCap()
        {
            Assert.IsTrue(SfxThrottleRules.HasFreeVoice(0, 8));
            Assert.IsTrue(SfxThrottleRules.HasFreeVoice(7, 8));
            Assert.IsFalse(SfxThrottleRules.HasFreeVoice(8, 8));
            Assert.IsFalse(SfxThrottleRules.HasFreeVoice(9, 8));
        }

        [Test]
        public void Tracker_ThrottlesPerIdIndependently()
        {
            var tracker = new SfxThrottleTracker();
            Assert.IsFalse(tracker.IsTooSoon("a", 1.0, 0.1f), "chưa phát lần nào");

            tracker.Record("a", 1.0);
            Assert.IsTrue(tracker.IsTooSoon("a", 1.05, 0.1f));
            Assert.IsFalse(tracker.IsTooSoon("b", 1.05, 0.1f), "id khác không bị ảnh hưởng");
            Assert.IsFalse(tracker.IsTooSoon("a", 1.2, 0.1f));
        }

        [Test]
        public void Tracker_ClearForgetsHistory_AndNullIdIsSafe()
        {
            var tracker = new SfxThrottleTracker();
            tracker.Record("a", 1.0);
            tracker.Clear();
            Assert.IsFalse(tracker.IsTooSoon("a", 1.01, 0.1f));

            Assert.DoesNotThrow(() => tracker.Record(null, 1.0));
            Assert.IsFalse(tracker.IsTooSoon(null, 1.0, 0.1f));
        }
    }

    public class MusicRulesTests
    {
        [Test]
        public void ChooseTrack_BossWhileAnyBossEngaged_OtherwiseGameplay()
        {
            Assert.AreEqual("music.gameplay", MusicRules.ChooseTrack(0, "music.gameplay", "music.boss"));
            Assert.AreEqual("music.boss", MusicRules.ChooseTrack(1, "music.gameplay", "music.boss"));
            Assert.AreEqual("music.boss", MusicRules.ChooseTrack(3, "music.gameplay", "music.boss"));
        }
    }

    public class AudioIdsTests
    {
        [Test]
        public void AllIds_AreUniqueNonEmptyAndNamespaced()
        {
            var seen = new HashSet<string>();
            foreach (string id in AudioIds.All)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(id));
                Assert.IsTrue(id.StartsWith("sfx.") || id.StartsWith("music."), "id phải bắt đầu bằng sfx. hoặc music.: " + id);
                Assert.IsTrue(seen.Add(id), "trùng id: " + id);
            }
        }

        [Test]
        public void All_ContainsEveryRequiredId()
        {
            var all = new HashSet<string>(AudioIds.All);
            string[] required =
            {
                AudioIds.SfxPlayerSwing, AudioIds.SfxHitEnemy, AudioIds.SfxHitPlayer, AudioIds.SfxEnemyDie,
                AudioIds.SfxTechnique, AudioIds.SfxDodge, AudioIds.SfxPickup, AudioIds.SfxLevelUp,
                AudioIds.SfxUiClick, AudioIds.SfxUiBack, AudioIds.SfxPotion, AudioIds.SfxBossTelegraph,
                AudioIds.MusicGameplay, AudioIds.MusicBoss
            };
            foreach (string id in required) Assert.IsTrue(all.Contains(id), "thiếu trong AudioIds.All: " + id);
        }

        [Test]
        public void NullAudioService_IsSilentAndNeverThrows()
        {
            IAudioService service = NullAudioService.Instance;
            Assert.DoesNotThrow(() =>
            {
                service.PlaySfx(AudioIds.SfxHitEnemy);
                service.PlaySfx(null);
                service.PlayMusic(AudioIds.MusicBoss, 1f);
                service.StopMusic(0.5f);
            });
        }
    }
}
