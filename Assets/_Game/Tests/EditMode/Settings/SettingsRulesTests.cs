using System.Collections.Generic;
using Game.Core.Audio;
using Game.Core.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    internal sealed class MemorySettingsStore : ISettingsStore
    {
        public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
        public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public int SaveCalls;

        public bool TryGetFloat(string key, out float value) => Floats.TryGetValue(key, out value);
        public void SetFloat(string key, float value) => Floats[key] = value;
        public bool TryGetInt(string key, out int value) => Ints.TryGetValue(key, out value);
        public void SetInt(string key, int value) => Ints[key] = value;
        public void Save() => SaveCalls++;
    }

    public class SettingsRulesTests
    {
        [Test]
        public void Defaults_AreInsideAllowedRanges()
        {
            PlayerSettings d = SettingsRules.Defaults;
            Assert.AreEqual(AudioVolumes.DefaultMusic, d.MusicVolume);
            Assert.AreEqual(AudioVolumes.DefaultSfx, d.SfxVolume);
            Assert.IsTrue(d.HapticsEnabled);
            Assert.AreEqual(JoystickMode.Fixed, d.Joystick);
            Assert.That(d.DeadZone, Is.InRange(SettingsRules.MinDeadZone, SettingsRules.MaxDeadZone));
            Assert.That(d.Sensitivity, Is.InRange(SettingsRules.MinSensitivity, SettingsRules.MaxSensitivity));
            Assert.AreEqual(d, SettingsRules.Sanitize(d), "mặc định đã hợp lệ, Sanitize không đổi gì");
        }

        [Test]
        public void Sanitize_ClampsEveryField()
        {
            var s = SettingsRules.Sanitize(new PlayerSettings(5f, -2f, true, JoystickMode.Floating, 0.9f, 9f));
            Assert.AreEqual(1f, s.MusicVolume);
            Assert.AreEqual(0f, s.SfxVolume);
            Assert.AreEqual(SettingsRules.MaxDeadZone, s.DeadZone);
            Assert.AreEqual(SettingsRules.MaxSensitivity, s.Sensitivity);

            s = SettingsRules.Sanitize(new PlayerSettings(0.5f, 0.5f, false, JoystickMode.Fixed, 0f, 0.1f));
            Assert.AreEqual(SettingsRules.MinDeadZone, s.DeadZone);
            Assert.AreEqual(SettingsRules.MinSensitivity, s.Sensitivity);
        }

        [Test]
        public void Sanitize_GarbageFallsBackToDefault()
        {
            var s = SettingsRules.Sanitize(new PlayerSettings(float.NaN, float.PositiveInfinity, true, (JoystickMode)7, float.NaN, float.NegativeInfinity));
            PlayerSettings d = SettingsRules.Defaults;
            Assert.AreEqual(d.MusicVolume, s.MusicVolume);
            Assert.AreEqual(d.SfxVolume, s.SfxVolume);
            Assert.AreEqual(d.Joystick, s.Joystick);
            Assert.AreEqual(d.DeadZone, s.DeadZone);
            Assert.AreEqual(d.Sensitivity, s.Sensitivity);
        }

        [Test]
        public void DeadZoneMinimum_IsLargerThanFacingNoiseThreshold()
        {
            // Mục 3b: ngưỡng của FacingTracker phải nhỏ hơn dead zone nhỏ nhất của Settings, để dead zone chỉ có MỘT nguồn.
            Assert.Less(Game.Core.Facing.FacingTracker.DefaultDeadZone, SettingsRules.MinDeadZone);
        }

        [Test]
        public void Repository_EmptyStore_GivesDefaults()
        {
            Assert.AreEqual(SettingsRules.Defaults, SettingsRepository.Load(new MemorySettingsStore()));
            Assert.AreEqual(SettingsRules.Defaults, SettingsRepository.Load(null));
        }

        [Test]
        public void Repository_SaveThenLoad_RoundTrips()
        {
            var store = new MemorySettingsStore();
            var saved = new PlayerSettings(0.3f, 0.6f, false, JoystickMode.Floating, 0.2f, 1.2f);
            SettingsRepository.Save(store, saved);
            Assert.AreEqual(1, store.SaveCalls);
            Assert.AreEqual(saved, SettingsRepository.Load(store));
        }

        [Test]
        public void Repository_UsesSettingsPrefix_AndSharesAudioKeys()
        {
            var store = new MemorySettingsStore();
            SettingsRepository.Save(store, SettingsRules.Defaults);
            foreach (string key in store.Floats.Keys) StringAssert.StartsWith("settings.", key);
            foreach (string key in store.Ints.Keys) StringAssert.StartsWith("settings.", key);
            Assert.IsTrue(store.Floats.ContainsKey(AudioVolumes.MusicKey));
            Assert.IsTrue(store.Floats.ContainsKey(AudioVolumes.SfxKey));
        }

        [Test]
        public void Repository_GarbageStoredValues_FallBackToDefault()
        {
            var store = new MemorySettingsStore();
            store.Floats[SettingsRepository.DeadZoneKey] = float.NaN;
            store.Floats[SettingsRepository.SensitivityKey] = 99f;
            store.Ints[SettingsRepository.JoystickKey] = 42;
            PlayerSettings loaded = SettingsRepository.Load(store);
            Assert.AreEqual(SettingsRules.DefaultDeadZone, loaded.DeadZone);
            Assert.AreEqual(SettingsRules.MaxSensitivity, loaded.Sensitivity);
            Assert.AreEqual(JoystickMode.Fixed, loaded.Joystick);
        }
    }
}
