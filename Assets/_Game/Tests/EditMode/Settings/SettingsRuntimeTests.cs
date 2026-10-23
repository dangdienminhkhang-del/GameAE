using Game.Core.Audio;
using Game.Core.Settings;
using Game.UI.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// SettingsRuntime là static: test luôn trả về kho mặc định và gỡ người nghe để không rò sang test khác
    /// (giống yêu cầu reset khi tắt Domain Reload).
    /// </summary>
    public class SettingsRuntimeTests
    {
        private MemorySettingsStore _store;
        private int _changedCalls;
        private PlayerSettings _lastChanged;

        private void OnChanged(PlayerSettings settings)
        {
            _changedCalls++;
            _lastChanged = settings;
        }

        [SetUp]
        public void SetUp()
        {
            _store = new MemorySettingsStore();
            _changedCalls = 0;
            SettingsRuntime.UseStore(_store);
            SettingsRuntime.Changed += OnChanged;
        }

        [TearDown]
        public void TearDown()
        {
            SettingsRuntime.Changed -= OnChanged;
            SettingsRuntime.UseStore(null);
        }

        [Test]
        public void Current_WithEmptyStore_IsDefaults()
        {
            Assert.AreEqual(SettingsRules.Defaults, SettingsRuntime.Current);
        }

        [Test]
        public void Current_LoadsWhatWasStored()
        {
            var saved = new PlayerSettings(0.2f, 0.4f, false, JoystickMode.Floating, 0.25f, 1.1f);
            SettingsRepository.Save(_store, saved);
            SettingsRuntime.UseStore(_store); // buộc nạp lại từ kho

            Assert.AreEqual(saved, SettingsRuntime.Current);
        }

        [Test]
        public void Apply_ClampsSavesAndRaisesChanged()
        {
            SettingsRuntime.Apply(new PlayerSettings(5f, -1f, true, JoystickMode.Floating, 0.9f, 9f));

            PlayerSettings expected = SettingsRules.Sanitize(new PlayerSettings(5f, -1f, true, JoystickMode.Floating, 0.9f, 9f));
            Assert.AreEqual(expected, SettingsRuntime.Current, "giá trị ngoài khoảng bị kẹp");
            Assert.AreEqual(expected, SettingsRepository.Load(_store), "đã lưu vào kho");
            Assert.AreEqual(1, _store.SaveCalls);
            Assert.AreEqual(1, _changedCalls);
            Assert.AreEqual(expected, _lastChanged);
        }

        [Test]
        public void Apply_SavesUnderSettingsPrefix_NeverTouchesPlayerData()
        {
            SettingsRuntime.Apply(SettingsRules.Defaults);

            foreach (string key in _store.Floats.Keys) StringAssert.StartsWith("settings.", key);
            foreach (string key in _store.Ints.Keys) StringAssert.StartsWith("settings.", key);
            Assert.IsTrue(_store.Floats.ContainsKey(AudioVolumes.MusicKey));
        }

        [Test]
        public void Apply_WithGarbage_FallsBackToDefaultsAndDoesNotThrow()
        {
            Assert.DoesNotThrow(() => SettingsRuntime.Apply(
                new PlayerSettings(float.NaN, float.PositiveInfinity, true, (JoystickMode)9, float.NaN, float.NaN)));

            Assert.AreEqual(SettingsRules.Defaults.DeadZone, SettingsRuntime.Current.DeadZone);
            Assert.AreEqual(SettingsRules.Defaults.Sensitivity, SettingsRuntime.Current.Sensitivity);
        }

        [Test]
        public void UseStore_Null_ReturnsToDefaultPlayerPrefsStoreWithoutThrowing()
        {
            Assert.DoesNotThrow(() => SettingsRuntime.UseStore(null));
            // Không đọc Current ở đây: nó sẽ chạm PlayerPrefs thật của máy dev. Chỉ cần chứng minh việc hoàn kho không ném lỗi.
        }

        [Test]
        public void ApplyVolumesToAudio_WithoutAudioService_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SettingsRuntime.ApplyVolumesToAudio());
        }
    }
}
