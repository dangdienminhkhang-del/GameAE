using System;
using System.Collections.Generic;
using Game.Services.Guidance;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Backend trong bộ nhớ (giống PlayerPrefs nhưng không đụng máy thật).</summary>
    public sealed class MemoryTutorialBackend : ITutorialKeyValueBackend
    {
        public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        public int SaveCalls;

        public bool TryGetInt(string key, out int value) => Ints.TryGetValue(key, out value);
        public void SetInt(string key, int value) => Ints[key] = value;
        public bool TryGetString(string key, out string value) => Strings.TryGetValue(key, out value);
        public void SetString(string key, string value) => Strings[key] = value;

        public void Delete(string key)
        {
            Ints.Remove(key);
            Strings.Remove(key);
        }

        public void Save() => SaveCalls++;

        public int KeyCount => Ints.Count + Strings.Count;
    }

    /// <summary>Backend luôn ném lỗi: mô phỏng PlayerPrefs hỏng (ổ đĩa đầy, quyền bị từ chối).</summary>
    public sealed class ThrowingTutorialBackend : ITutorialKeyValueBackend
    {
        public bool TryGetInt(string key, out int value) { throw new InvalidOperationException("hỏng"); }
        public void SetInt(string key, int value) { throw new InvalidOperationException("hỏng"); }
        public bool TryGetString(string key, out string value) { throw new InvalidOperationException("hỏng"); }
        public void SetString(string key, string value) { throw new InvalidOperationException("hỏng"); }
        public void Delete(string key) { throw new InvalidOperationException("hỏng"); }
        public void Save() { throw new InvalidOperationException("hỏng"); }
    }

    /// <summary>Kho cờ hướng dẫn (Phase 9.9): khóa theo playerId, ClearAll chỉ xóa khóa của gói, công tắc riêng, chịu được kho hỏng.</summary>
    public class PlayerPrefsTutorialStoreTests
    {
        private MemoryTutorialBackend _backend;
        private PlayerPrefsTutorialStore _store;

        [SetUp]
        public void SetUp()
        {
            _backend = new MemoryTutorialBackend();
            _store = new PlayerPrefsTutorialStore(_backend);
        }

        // ── Cờ "đã xem" ──────────────────────────────────────────────────────

        [Test]
        public void NothingSeen_ByDefault()
        {
            Assert.IsFalse(_store.HasSeen("p1", "hint.move"));
        }

        [Test]
        public void MarkSeen_ThenHasSeen_True()
        {
            _store.MarkSeen("p1", "hint.move");
            Assert.IsTrue(_store.HasSeen("p1", "hint.move"));
        }

        [Test]
        public void TwoPlayers_AreIndependent()
        {
            _store.MarkSeen("p1", "hint.move");

            Assert.IsTrue(_store.HasSeen("p1", "hint.move"));
            Assert.IsFalse(_store.HasSeen("p2", "hint.move"), "Guest mới (playerId mới) thấy lại gợi ý");
        }

        [Test]
        public void Hints_AreIndependent_ForTheSamePlayer()
        {
            _store.MarkSeen("p1", "hint.move");
            Assert.IsFalse(_store.HasSeen("p1", "hint.attack"));
        }

        [Test]
        public void Seen_PersistsAcrossStoreInstances()
        {
            _store.MarkSeen("p1", "hint.move");
            var second = new PlayerPrefsTutorialStore(_backend);
            Assert.IsTrue(second.HasSeen("p1", "hint.move"));
        }

        [Test]
        public void KeyFormat_IsTutorialPlayerHint()
        {
            Assert.AreEqual("tutorial.p1.hint.move", PlayerPrefsTutorialStore.SeenKey("p1", "hint.move"));
            _store.MarkSeen("p1", "hint.move");
            Assert.IsTrue(_backend.Ints.ContainsKey("tutorial.p1.hint.move"));
        }

        [Test]
        public void EmptyIds_AreIgnored_AndWriteNothing()
        {
            _store.MarkSeen("", "hint.move");
            _store.MarkSeen(null, "hint.move");
            _store.MarkSeen("p1", "");
            _store.MarkSeen("p1", null);

            Assert.AreEqual(0, _backend.KeyCount);
            Assert.IsFalse(_store.HasSeen("", "hint.move"));
            Assert.IsFalse(_store.HasSeen("p1", null));
        }

        // ── ClearAll chỉ xóa khóa của gói ────────────────────────────────────

        [Test]
        public void ClearAll_RemovesThePlayersFlags()
        {
            _store.MarkSeen("p1", "hint.move");
            _store.MarkSeen("p1", "hint.attack");

            _store.ClearAll("p1", null);

            Assert.IsFalse(_store.HasSeen("p1", "hint.move"));
            Assert.IsFalse(_store.HasSeen("p1", "hint.attack"));
            Assert.IsFalse(new PlayerPrefsTutorialStore(_backend).HasSeen("p1", "hint.move"), "xóa thật ở kho, không chỉ trong bộ nhớ");
        }

        [Test]
        public void ClearAll_DoesNotTouchOtherKeys_LikeAudioSettings()
        {
            _backend.SetInt("settings.audio.music_volume", 7);
            _backend.SetString("settings.audio.profile", "x");
            _backend.SetInt("account.something", 3);
            _store.SetEnabled(false);
            _store.MarkSeen("p1", "hint.move");

            _store.ClearAll("p1", new[] { "hint.move" });

            Assert.AreEqual(7, _backend.Ints["settings.audio.music_volume"]);
            Assert.AreEqual("x", _backend.Strings["settings.audio.profile"]);
            Assert.AreEqual(3, _backend.Ints["account.something"]);
            Assert.IsFalse(_store.GetEnabled(), "'Xem lại hướng dẫn' không đổi công tắc");
        }

        [Test]
        public void ClearAll_DoesNotTouchAnotherPlayersFlags()
        {
            _store.MarkSeen("p1", "hint.move");
            _store.MarkSeen("p2", "hint.move");

            _store.ClearAll("p1", null);

            Assert.IsTrue(_store.HasSeen("p2", "hint.move"));
        }

        [Test]
        public void ClearAll_UsesKnownHintIds_EvenIfTheIndexIsLost()
        {
            _store.MarkSeen("p1", "hint.move");
            _backend.Delete(PlayerPrefsTutorialStore.IndexKey("p1")); // chỉ mục mất

            _store.ClearAll("p1", new[] { "hint.move" });

            Assert.IsFalse(new PlayerPrefsTutorialStore(_backend).HasSeen("p1", "hint.move"));
        }

        [Test]
        public void ClearAll_EmptyPlayerId_DoesNothing()
        {
            _store.MarkSeen("p1", "hint.move");
            int before = _backend.KeyCount;

            _store.ClearAll("", null);
            _store.ClearAll(null, null);

            Assert.AreEqual(before, _backend.KeyCount);
        }

        [Test]
        public void MarkSeen_Twice_DoesNotDuplicateTheIndexEntry()
        {
            _store.MarkSeen("p1", "hint.move");
            _store.MarkSeen("p1", "hint.move");

            Assert.AreEqual("hint.move", _backend.Strings[PlayerPrefsTutorialStore.IndexKey("p1")]);
        }

        // ── Công tắc ─────────────────────────────────────────────────────────

        [Test]
        public void Switch_DefaultsToOn()
        {
            Assert.IsTrue(_store.GetEnabled());
        }

        [Test]
        public void Switch_UsesItsOwnKey_AndPersists()
        {
            _store.SetEnabled(false);

            Assert.AreEqual("settings.tutorial_hints", PlayerPrefsTutorialStore.EnabledKey);
            Assert.AreEqual(0, _backend.Ints["settings.tutorial_hints"]);
            Assert.IsFalse(new PlayerPrefsTutorialStore(_backend).GetEnabled());

            _store.SetEnabled(true);
            Assert.IsTrue(new PlayerPrefsTutorialStore(_backend).GetEnabled());
        }

        [Test]
        public void Switch_IsNotPartOfThePerPlayerFlags()
        {
            _store.SetEnabled(false);
            _store.ClearAll("p1", new[] { "hint.move" });
            Assert.IsFalse(_store.GetEnabled());
        }

        // ── Kho hỏng: không ném lỗi, vẫn nhớ trong phiên ─────────────────────

        [Test]
        public void BrokenBackend_NeverThrows()
        {
            var store = new PlayerPrefsTutorialStore(new ThrowingTutorialBackend());

            Assert.DoesNotThrow(() => store.HasSeen("p1", "hint.move"));
            Assert.DoesNotThrow(() => store.MarkSeen("p1", "hint.move"));
            Assert.DoesNotThrow(() => store.ClearAll("p1", new[] { "hint.move" }));
            Assert.DoesNotThrow(() => store.GetEnabled());
            Assert.DoesNotThrow(() => store.SetEnabled(false));
        }

        [Test]
        public void BrokenBackend_StillRemembersWithinTheSession_SoHintsDoNotRepeat()
        {
            var store = new PlayerPrefsTutorialStore(new ThrowingTutorialBackend());

            store.MarkSeen("p1", "hint.move");
            Assert.IsTrue(store.HasSeen("p1", "hint.move"));

            store.ClearAll("p1", new[] { "hint.move" });
            Assert.IsFalse(store.HasSeen("p1", "hint.move"));
        }

        [Test]
        public void BrokenBackend_SwitchStillWorksInMemory_AndDefaultsToOn()
        {
            var store = new PlayerPrefsTutorialStore(new ThrowingTutorialBackend());
            Assert.IsTrue(store.GetEnabled());

            store.SetEnabled(false);
            Assert.IsFalse(store.GetEnabled());
        }

        [Test]
        public void NullBackend_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new PlayerPrefsTutorialStore(null));
        }
    }
}
