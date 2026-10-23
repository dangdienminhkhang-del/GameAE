using System.Collections.Generic;
using Game.Core.Guidance;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>Kho cờ trong bộ nhớ cho test luật gợi ý (không đụng PlayerPrefs).</summary>
    public sealed class MemoryTutorialStore : ITutorialStore
    {
        private readonly HashSet<string> _seen = new HashSet<string>();
        public bool Enabled = true;
        public int MarkCalls;

        public bool HasSeen(string playerId, string hintId) => _seen.Contains(playerId + "|" + hintId);

        public void MarkSeen(string playerId, string hintId)
        {
            MarkCalls++;
            _seen.Add(playerId + "|" + hintId);
        }

        public void ClearAll(string playerId, IEnumerable<string> knownHintIds)
        {
            _seen.RemoveWhere(k => k.StartsWith(playerId + "|"));
        }

        public bool GetEnabled() => Enabled;
        public void SetEnabled(bool enabled) => Enabled = enabled;
    }

    /// <summary>Toast giả: ghi lại hint nào thật sự hiện và số lần ẩn. Accept = false mô phỏng UI chưa sẵn sàng.</summary>
    public sealed class RecordingToastSink : ITutorialToastSink
    {
        public bool Accept = true;
        public readonly List<string> Shown = new List<string>();
        public int HideCalls;

        public bool TryShow(TutorialHint hint)
        {
            if (!Accept) return false;
            Shown.Add(hint.Id);
            return true;
        }

        public void Hide() => HideCalls++;
    }

    /// <summary>Luật hiển thị gợi ý (Phase 9.9): một lần mỗi hint và playerId, hàng đợi, một toast một lúc, bị chặn thì chờ rồi bỏ.</summary>
    public class TutorialRulesTests
    {
        private const string Player = "player-1";

        private MemoryTutorialStore _store;
        private RecordingToastSink _sink;
        private TutorialRules _rules;

        private static TutorialHint Hint(string id, int priority = 0, TutorialComplete complete = TutorialComplete.None, float seconds = 4f)
        {
            return new TutorialHint(id, "chữ " + id, "Hệ thống", priority, seconds, complete);
        }

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryTutorialStore();
            _sink = new RecordingToastSink();
            _rules = new TutorialRules(_store, TutorialRuleSettings.Default) { PlayerId = Player };
        }

        // ── Một lần mỗi hint và playerId ─────────────────────────────────────

        [Test]
        public void Request_Then_Update_ShowsTheToast_AndMarksItSeen()
        {
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.move"), 0));
            _rules.Update(0, false, _sink);

            CollectionAssert.AreEqual(new[] { "hint.move" }, _sink.Shown);
            Assert.IsTrue(_store.HasSeen(Player, "hint.move"));
            Assert.IsTrue(_rules.HasActive);
        }

        [Test]
        public void SeenHint_IsNotShownAgain_ForTheSamePlayer()
        {
            _rules.Request(Hint("hint.move"), 0);
            _rules.Update(0, false, _sink);
            _rules.Update(100, false, _sink); // hết giờ, ẩn

            Assert.AreEqual(TutorialRequestResult.AlreadySeen, _rules.Request(Hint("hint.move"), 101));
            _rules.Update(102, false, _sink);
            Assert.AreEqual(1, _sink.Shown.Count);
        }

        [Test]
        public void TwoPlayers_AreIndependent_NewPlayerSeesTheHintAgain()
        {
            _rules.Request(Hint("hint.move"), 0);
            _rules.Update(0, false, _sink);
            _rules.Update(100, false, _sink);

            _rules.PlayerId = "player-2";
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.move"), 101));
            _rules.Update(102, false, _sink); // toast trước vừa ẩn ở giây 100: phải qua khoảng cách tối thiểu 1.5 s

            Assert.AreEqual(2, _sink.Shown.Count);
            Assert.IsTrue(_store.HasSeen("player-2", "hint.move"));
        }

        [Test]
        public void InvalidId_IsRejected()
        {
            Assert.AreEqual(TutorialRequestResult.Invalid, _rules.Request(Hint(""), 0));
            Assert.AreEqual(TutorialRequestResult.Invalid, _rules.Request(Hint(null), 0));
            Assert.AreEqual(0, _rules.QueuedCount);
        }

        // ── Chỉ đánh dấu khi toast THẬT SỰ hiện ──────────────────────────────

        [Test]
        public void NotMarkedSeen_WhenTheToastCouldNotBeShown_AndRetriedLater()
        {
            _sink.Accept = false;
            _rules.Request(Hint("hint.attack"), 0);

            _rules.Update(0, false, _sink);
            _rules.Update(1, false, _sink);

            Assert.IsFalse(_store.HasSeen(Player, "hint.attack"), "chưa hiện thì chưa đánh dấu");
            Assert.AreEqual(0, _store.MarkCalls);
            Assert.AreEqual(1, _rules.QueuedCount, "vẫn giữ trong hàng đợi để thử lại");

            _sink.Accept = true;
            _rules.Update(2, false, _sink);

            Assert.IsTrue(_store.HasSeen(Player, "hint.attack"));
            CollectionAssert.AreEqual(new[] { "hint.attack" }, _sink.Shown);
        }

        [Test]
        public void NullSink_DoesNotMarkSeen()
        {
            _rules.Request(Hint("hint.attack"), 0);
            _rules.Update(0, false, null);
            Assert.IsFalse(_store.HasSeen(Player, "hint.attack"));
        }

        // ── Công tắc ─────────────────────────────────────────────────────────

        [Test]
        public void Disabled_NeitherShowsNorMarks()
        {
            _rules.SetEnabled(false);

            Assert.AreEqual(TutorialRequestResult.Disabled, _rules.Request(Hint("hint.move"), 0));
            _rules.Update(0, false, _sink);

            Assert.AreEqual(0, _sink.Shown.Count);
            Assert.AreEqual(0, _store.MarkCalls);
        }

        [Test]
        public void TurningOff_ClearsQueue_AndHidesTheActiveToast()
        {
            _rules.Request(Hint("hint.a", 2), 0);
            _rules.Request(Hint("hint.b", 1), 0);
            _rules.Update(0, false, _sink);
            Assert.IsTrue(_rules.HasActive);
            Assert.AreEqual(1, _rules.QueuedCount);

            _rules.SetEnabled(false);
            _rules.Update(1, false, _sink);

            Assert.IsFalse(_rules.HasActive);
            Assert.AreEqual(0, _rules.QueuedCount);
            Assert.AreEqual(1, _sink.HideCalls);
        }

        [Test]
        public void TurningOffThenOn_StartsFresh_AndHintsWorkAgain()
        {
            _rules.SetEnabled(false);
            _rules.SetEnabled(true);
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.move"), 0));
        }

        [Test]
        public void Constructor_ReadsTheStoredSwitch()
        {
            _store.Enabled = false;
            var rules = new TutorialRules(_store, TutorialRuleSettings.Default) { PlayerId = Player };
            Assert.IsFalse(rules.Enabled);
            Assert.AreEqual(TutorialRequestResult.Disabled, rules.Request(Hint("hint.move"), 0));
        }

        // ── Hàng đợi ─────────────────────────────────────────────────────────

        [Test]
        public void Queue_ShowsHigherPriorityFirst()
        {
            _rules.Request(Hint("hint.low", 1), 0);
            _rules.Request(Hint("hint.high", 9), 0.1);
            _rules.Update(0.2, false, _sink);

            Assert.AreEqual("hint.high", _sink.Shown[0]);
        }

        [Test]
        public void Queue_SamePriority_EarlierFirst()
        {
            _rules.Request(Hint("hint.first", 5), 0);
            _rules.Request(Hint("hint.second", 5), 1);
            _rules.Update(2, false, _sink);

            Assert.AreEqual("hint.first", _sink.Shown[0]);
        }

        [Test]
        public void Queue_NoDuplicates_QueuedOrActive()
        {
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.a"), 0));
            Assert.AreEqual(TutorialRequestResult.Duplicate, _rules.Request(Hint("hint.a"), 0.1));
            Assert.AreEqual(1, _rules.QueuedCount);

            _rules.Update(0.2, false, _sink); // hiện lên
            Assert.AreEqual(TutorialRequestResult.AlreadySeen, _rules.Request(Hint("hint.a"), 0.3), "đã hiện thì đã đánh dấu");
        }

        [Test]
        public void Queue_HoldsAtMostTwoWaiting()
        {
            _rules.Request(Hint("hint.a", 3), 0);
            _rules.Update(0, false, _sink); // a đang hiện
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.b", 2), 1));
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.c", 2), 1));

            Assert.AreEqual(TutorialRequestResult.Dropped, _rules.Request(Hint("hint.d", 1), 1), "đầy và kém quan trọng hơn");
            Assert.AreEqual(2, _rules.QueuedCount);
        }

        [Test]
        public void Queue_MoreImportantHint_ReplacesTheLeastImportantWaiting()
        {
            _rules.Request(Hint("hint.a", 3), 0);
            _rules.Update(0, false, _sink);
            _rules.Request(Hint("hint.b", 1), 1);
            _rules.Request(Hint("hint.c", 2), 1);

            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.d", 9), 1));

            Assert.AreEqual(2, _rules.QueuedCount);
            Assert.IsFalse(_rules.IsQueued("hint.b"), "cái kém nhất bị thay");
            Assert.IsTrue(_rules.IsQueued("hint.c"));
            Assert.IsTrue(_rules.IsQueued("hint.d"));
        }

        [Test]
        public void Queue_EqualPriority_DoesNotReplace()
        {
            _rules.Request(Hint("hint.a", 3), 0);
            _rules.Update(0, false, _sink);
            _rules.Request(Hint("hint.b", 2), 1);
            _rules.Request(Hint("hint.c", 2), 1);

            Assert.AreEqual(TutorialRequestResult.Dropped, _rules.Request(Hint("hint.d", 2), 1));
            Assert.IsTrue(_rules.IsQueued("hint.b"));
            Assert.IsTrue(_rules.IsQueued("hint.c"));
        }

        // ── Một toast một lúc, khoảng cách tối thiểu ─────────────────────────

        [Test]
        public void OnlyOneToastAtATime()
        {
            _rules.Request(Hint("hint.a", 5), 0);
            _rules.Request(Hint("hint.b", 4), 0);

            _rules.Update(0, false, _sink);
            _rules.Update(0.5, false, _sink);
            _rules.Update(3.9, false, _sink);

            CollectionAssert.AreEqual(new[] { "hint.a" }, _sink.Shown);
        }

        [Test]
        public void NextToast_WaitsTheMinimumGapAfterTheLastOneHides()
        {
            _rules.Request(Hint("hint.a", 5, seconds: 4f), 0);
            _rules.Request(Hint("hint.b", 4), 0);
            _rules.Update(0, false, _sink);

            _rules.Update(4.0, false, _sink); // a hết giờ → ẩn
            Assert.IsFalse(_rules.HasActive);
            _rules.Update(5.4, false, _sink); // mới 1.4 s
            Assert.AreEqual(1, _sink.Shown.Count, "chưa đủ 1.5 s");

            _rules.Update(5.5, false, _sink);
            CollectionAssert.AreEqual(new[] { "hint.a", "hint.b" }, _sink.Shown);
        }

        [Test]
        public void Toast_HidesAfterItsDisplaySeconds_WhenNoCompleteKind()
        {
            _rules.Request(Hint("hint.a", 1, TutorialComplete.None, 4f), 0);
            _rules.Update(0, false, _sink);
            _rules.Update(3.9, false, _sink);
            Assert.IsTrue(_rules.HasActive);

            _rules.Update(4.0, false, _sink);
            Assert.IsFalse(_rules.HasActive);
            Assert.AreEqual(1, _sink.HideCalls);
        }

        // ── Bị chặn: giữ chờ rồi bỏ, không đánh dấu ───────────────────────────

        [Test]
        public void Blocked_KeepsWaiting_ThenShowsWhenUnblocked()
        {
            _rules.Request(Hint("hint.a"), 0);
            _rules.Update(1, true, _sink);
            _rules.Update(5, true, _sink);
            Assert.AreEqual(0, _sink.Shown.Count);
            Assert.AreEqual(1, _rules.QueuedCount);

            _rules.Update(6, false, _sink);
            CollectionAssert.AreEqual(new[] { "hint.a" }, _sink.Shown);
        }

        [Test]
        public void Blocked_BeyondTheTimeout_DropsTheHint_WithoutMarkingSeen()
        {
            _rules.Request(Hint("hint.a"), 0);

            _rules.Update(9.9, true, _sink);
            Assert.AreEqual(1, _rules.QueuedCount);

            _rules.Update(10.0, true, _sink);
            Assert.AreEqual(0, _rules.QueuedCount);
            Assert.IsFalse(_store.HasSeen(Player, "hint.a"));
            Assert.AreEqual(0, _store.MarkCalls);
            Assert.AreEqual(0, _sink.Shown.Count);
        }

        [Test]
        public void DroppedByTimeout_CanBeRequestedAgainLater()
        {
            _rules.Request(Hint("hint.a"), 0);
            _rules.Update(10.0, true, _sink);

            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.a"), 11));
        }

        [Test]
        public void NoPlayerId_CountsAsBlocked_AndDropsAfterTimeout()
        {
            _rules.PlayerId = null;
            Assert.AreEqual(TutorialRequestResult.Queued, _rules.Request(Hint("hint.a"), 0));

            _rules.Update(1, false, _sink);
            Assert.AreEqual(0, _sink.Shown.Count, "chưa có playerId thì không hiện");
            _rules.Update(10, false, _sink);

            Assert.AreEqual(0, _rules.QueuedCount);
            Assert.AreEqual(0, _store.MarkCalls);
        }

        [Test]
        public void NoPlayerId_ThenPlayerIdArrives_ShowsTheHint()
        {
            _rules.PlayerId = "";
            _rules.Request(Hint("hint.a"), 0);
            _rules.Update(1, false, _sink);

            _rules.PlayerId = Player;
            _rules.Update(2, false, _sink);

            CollectionAssert.AreEqual(new[] { "hint.a" }, _sink.Shown);
            Assert.IsTrue(_store.HasSeen(Player, "hint.a"));
        }

        [Test]
        public void HintSeenElsewhereWhileWaiting_IsSkipped()
        {
            _rules.Request(Hint("hint.a"), 0);
            _store.MarkSeen(Player, "hint.a"); // xem ở phiên khác trong lúc chờ
            int marksBefore = _store.MarkCalls;

            _rules.Update(1, false, _sink);

            Assert.AreEqual(0, _sink.Shown.Count);
            Assert.AreEqual(marksBefore, _store.MarkCalls);
        }

        // ── Hoàn thành sớm ───────────────────────────────────────────────────

        [Test]
        public void CompleteEarly_HidesTheToast_AndKeepsItMarkedSeen()
        {
            _rules.Request(Hint("hint.move", 1, TutorialComplete.Moved), 0);
            _rules.Update(0, false, _sink);

            _rules.Notify(TutorialComplete.Moved, 1, _sink);

            Assert.IsFalse(_rules.HasActive);
            Assert.AreEqual(1, _sink.HideCalls);
            Assert.IsTrue(_store.HasSeen(Player, "hint.move"), "đã hiện nên đã đánh dấu");
        }

        [Test]
        public void Notify_OfAnotherKind_DoesNothing()
        {
            _rules.Request(Hint("hint.move", 1, TutorialComplete.Moved), 0);
            _rules.Update(0, false, _sink);

            _rules.Notify(TutorialComplete.HitLanded, 1, _sink);
            _rules.Notify(TutorialComplete.None, 1, _sink);

            Assert.IsTrue(_rules.HasActive);
            Assert.AreEqual(0, _sink.HideCalls);
        }

        [Test]
        public void Notify_DropsAWaitingHintOfTheSameKind_WithoutMarkingIt()
        {
            _rules.Request(Hint("hint.a", 9), 0);
            _rules.Request(Hint("hint.attack", 1, TutorialComplete.HitLanded), 0);
            _rules.Update(0, false, _sink); // a đang hiện, attack chờ

            _rules.Notify(TutorialComplete.HitLanded, 1, _sink); // người chơi đã đánh trúng: gợi ý "đánh" không còn cần

            Assert.IsFalse(_rules.IsQueued("hint.attack"));
            Assert.IsFalse(_store.HasSeen(Player, "hint.attack"));
        }

        [Test]
        public void CompleteEarly_StillRespectsTheGapBeforeTheNextToast()
        {
            _rules.Request(Hint("hint.a", 5, TutorialComplete.Moved), 0);
            _rules.Request(Hint("hint.b", 4), 0);
            _rules.Update(0, false, _sink);

            _rules.Notify(TutorialComplete.Moved, 1, _sink);
            _rules.Update(1.5, false, _sink);
            Assert.AreEqual(1, _sink.Shown.Count, "mới 0.5 s sau khi ẩn");

            _rules.Update(2.5, false, _sink);
            Assert.AreEqual(2, _sink.Shown.Count);
        }

        // ── Reset, hằng số ───────────────────────────────────────────────────

        [Test]
        public void ResetState_ClearsQueue_HidesActive_AndAllowsAnImmediateShow()
        {
            _rules.Request(Hint("hint.a", 5), 0);
            _rules.Request(Hint("hint.b", 4), 0);
            _rules.Update(0, false, _sink);

            _rules.ResetState(1, _sink);

            Assert.IsFalse(_rules.HasActive);
            Assert.AreEqual(0, _rules.QueuedCount);
            Assert.AreEqual(1, _sink.HideCalls);

            _store.ClearAll(Player, null);
            _rules.Request(Hint("hint.c"), 1);
            _rules.Update(1, false, _sink);
            Assert.AreEqual("hint.c", _sink.Shown[_sink.Shown.Count - 1], "ResetState bỏ luôn khoảng nghỉ");
        }

        [Test]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new TutorialRules(null, TutorialRuleSettings.Default));
        }

        [Test]
        public void DefaultSettings_MatchTheContract()
        {
            TutorialRuleSettings s = TutorialRuleSettings.Default;
            Assert.AreEqual(2, s.MaxQueued);
            Assert.AreEqual(1.5f, s.MinGapSeconds, 1e-6f);
            Assert.AreEqual(10f, s.BlockedTimeoutSeconds, 1e-6f);
        }

        [Test]
        public void Settings_ClampBadValues()
        {
            var s = new TutorialRuleSettings(0, -3f, -9f);
            Assert.AreEqual(1, s.MaxQueued);
            Assert.AreEqual(0f, s.MinGapSeconds);
            Assert.AreEqual(0f, s.BlockedTimeoutSeconds);
        }
    }
}
