using System.Collections.Generic;
using System.Reflection;
using Game.Core.Audio;
using Game.Services.Audio;
using Game.UI.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class AudioCatalogTests
    {
        private AudioCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<AudioCatalog>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
        }

        private void SetEntries(params AudioCatalog.Entry[] entries)
        {
            FieldInfo field = typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "AudioCatalog phải có field 'entries' (U1 điền qua Inspector/serialized field)");
            field.SetValue(_catalog, new List<AudioCatalog.Entry>(entries));
        }

        [Test]
        public void TryGet_FindsEntryById()
        {
            SetEntries(new AudioCatalog.Entry { id = AudioIds.SfxHitEnemy, volume = 0.6f });
            Assert.IsTrue(_catalog.TryGet(AudioIds.SfxHitEnemy, out AudioCatalog.Entry entry));
            Assert.AreEqual(0.6f, entry.volume, 1e-6f);
        }

        [Test]
        public void TryGet_UnknownNullOrEmptyId_ReturnsFalse_WithoutThrowing()
        {
            SetEntries(new AudioCatalog.Entry { id = "a" });
            Assert.IsFalse(_catalog.TryGet("zzz", out AudioCatalog.Entry e1));
            Assert.IsNull(e1);
            Assert.IsFalse(_catalog.TryGet(null, out _));
            Assert.IsFalse(_catalog.TryGet("", out _));
        }

        [Test]
        public void TryGet_DuplicateIds_FirstEntryWins()
        {
            SetEntries(
                new AudioCatalog.Entry { id = "dup", volume = 0.1f },
                new AudioCatalog.Entry { id = "dup", volume = 0.9f });
            Assert.IsTrue(_catalog.TryGet("dup", out AudioCatalog.Entry entry));
            Assert.AreEqual(0.1f, entry.volume, 1e-6f);
        }

        [Test]
        public void TryGet_SkipsNullAndBlankEntries()
        {
            SetEntries(null, new AudioCatalog.Entry { id = "" }, new AudioCatalog.Entry { id = "ok" });
            Assert.IsTrue(_catalog.TryGet("ok", out _));
        }

        [Test]
        public void Entry_Defaults_AreSane()
        {
            var entry = new AudioCatalog.Entry();
            Assert.That(entry.volume, Is.InRange(0f, 1f));
            Assert.GreaterOrEqual(entry.pitchJitter, 0f);
            Assert.GreaterOrEqual(entry.minIntervalSeconds, 0f);
        }
    }

    public class UiSfxRulesTests
    {
        [Test]
        public void PickSfxId_BackCloseCancel_UseBackSound_CaseInsensitive()
        {
            Assert.AreEqual(AudioIds.SfxUiBack, UiSfxRules.PickSfxId("BackButton"));
            Assert.AreEqual(AudioIds.SfxUiBack, UiSfxRules.PickSfxId("Btn_Close"));
            Assert.AreEqual(AudioIds.SfxUiBack, UiSfxRules.PickSfxId("cancel"));
            Assert.AreEqual(AudioIds.SfxUiBack, UiSfxRules.PickSfxId("CLOSE_PANEL"));
        }

        [Test]
        public void PickSfxId_OtherButtons_UseClick()
        {
            Assert.AreEqual(AudioIds.SfxUiClick, UiSfxRules.PickSfxId("PlayButton"));
            Assert.AreEqual(AudioIds.SfxUiClick, UiSfxRules.PickSfxId("Confirm"));
            Assert.AreEqual(AudioIds.SfxUiClick, UiSfxRules.PickSfxId(""));
            Assert.AreEqual(AudioIds.SfxUiClick, UiSfxRules.PickSfxId(null));
        }

        [Test]
        public void ShouldSkip_MatchesFragmentsCaseInsensitive()
        {
            string[] skip = { "Attack", "Dodge" };
            Assert.IsTrue(UiSfxRules.ShouldSkip("AttackButton", skip));
            Assert.IsTrue(UiSfxRules.ShouldSkip("btn_dodge", skip));
            Assert.IsFalse(UiSfxRules.ShouldSkip("InventoryButton", skip));
        }

        [Test]
        public void ShouldSkip_NullOrEmptyInputs_DoNotSkip()
        {
            Assert.IsFalse(UiSfxRules.ShouldSkip(null, new[] { "Attack" }));
            Assert.IsFalse(UiSfxRules.ShouldSkip("AttackButton", null));
            Assert.IsFalse(UiSfxRules.ShouldSkip("AttackButton", new[] { "", null }));
        }
    }
}
