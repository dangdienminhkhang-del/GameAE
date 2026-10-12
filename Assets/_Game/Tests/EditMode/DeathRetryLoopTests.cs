using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.World;
using Game.PvE.Bootstrap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Vòng tự thử lại ghi cái chết (docs/09 §1.3): khi "chưa xác nhận" thì cứ 5 giây thử lại MỘT lần;
    /// một vòng duy nhất, không chạy chồng; dừng khi xác nhận được; nút "Thử lại" vẫn gọi ngay.
    /// Mọi test dùng service giả hoàn tất đồng bộ + Tick(dt) thủ công → không có thời gian thật, không phụ thuộc Unity chạy.
    /// </summary>
    public class DeathRetryLoopTests
    {
        private class FakeDeathService : IDeathService
        {
            public int RetryCalls;
            public Func<Task> OnRetry = () => Task.CompletedTask;

            public bool IsPlayerDead => true;
            public DeathInfo CurrentDeathInfo { get; } = new DeathInfo { isDead = true };

#pragma warning disable CS0067
            public event EventHandler<DeathInfo> OnPlayerDied;
#pragma warning restore CS0067

            public Task<DeathInfo> LoadDeathInfoAsync() => Task.FromResult(CurrentDeathInfo);
            public Task RecordDeathAsync(DeathRecord record) => Task.CompletedTask;

            public Task RetryRecordAsync()
            {
                RetryCalls++;
                return OnRetry();
            }

            [Obsolete("Dùng RecordDeathAsync")]
            public Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds) =>
                Task.CompletedTask;

            public Task ResetDeathStateAsync() => Task.CompletedTask;
        }

        private FakeDeathService _service;
        private List<Exception> _failures;
        private DeathRetryLoop _loop;

        private static Task Fail() => Task.FromException(new SaveUnavailableException("death", null));

        [SetUp]
        public void SetUp()
        {
            _service = new FakeDeathService();
            _failures = new List<Exception>();
            _loop = new DeathRetryLoop(_service, DeathRetryLoop.DefaultIntervalSeconds, _failures.Add);
        }

        // ── Chu kỳ ────────────────────────────────────────────────────────────

        [Test]
        public void DefaultInterval_IsFiveSeconds()
        {
            Assert.AreEqual(5f, DeathRetryLoop.DefaultIntervalSeconds);
        }

        [Test]
        public void Tick_BeforeStart_NeverRetries()
        {
            _loop.Tick(100f);

            Assert.AreEqual(0, _service.RetryCalls);
            Assert.IsFalse(_loop.IsRunning);
        }

        [Test]
        public void Tick_RetriesOnlyOnceAFullIntervalHasPassed()
        {
            _service.OnRetry = Fail;
            _loop.Start();

            _loop.Tick(4f);
            Assert.AreEqual(0, _service.RetryCalls, "Chưa đủ 5 giây thì chưa thử");

            _loop.Tick(1f);
            Assert.AreEqual(1, _service.RetryCalls, "Đủ 5 giây → thử đúng một lần");
        }

        [Test]
        public void NonPositiveInterval_FallsBackToDefault()
        {
            var loop = new DeathRetryLoop(_service, 0f, null);
            _service.OnRetry = Fail;
            loop.Start();

            loop.Tick(4f);
            Assert.AreEqual(0, _service.RetryCalls);
            loop.Tick(1f);
            Assert.AreEqual(1, _service.RetryCalls);
        }

        [Test]
        public void Constructor_NullService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DeathRetryLoop(null, 5f, null));
        }

        // ── Lỗi: vẫn chết, vòng tiếp tục ──────────────────────────────────────

        [Test]
        public void FailedAttempt_KeepsLoopRunning_RetriesEveryInterval_AndReportsEachFailure()
        {
            _service.OnRetry = Fail;
            _loop.Start();

            for (int i = 1; i <= 3; i++)
            {
                _loop.Tick(5f);
                Assert.AreEqual(i, _service.RetryCalls, $"Chu kỳ thứ {i} phải thử lại");
            }

            Assert.IsTrue(_loop.IsRunning, "Lỗi không được làm dừng vòng (vẫn chưa xác nhận → vẫn phải thử tiếp)");
            Assert.IsFalse(_loop.IsAttempting);
            Assert.AreEqual(3, _failures.Count);
            Assert.IsInstanceOf<SaveUnavailableException>(_failures[0]);
        }

        [Test]
        public void AttemptThatThrowsSynchronously_IsTreatedAsFailure_NotCrash()
        {
            _service.OnRetry = () => throw new SaveUnavailableException("death", null);
            _loop.Start();

            Assert.DoesNotThrow(() => _loop.Tick(5f));

            Assert.IsTrue(_loop.IsRunning);
            Assert.IsFalse(_loop.IsAttempting);
            Assert.AreEqual(1, _failures.Count);
        }

        [Test]
        public void FailureCallbackThatThrows_DoesNotKillTheLoop()
        {
            var loop = new DeathRetryLoop(_service, 5f, _ => throw new InvalidOperationException("boom"));
            _service.OnRetry = Fail;
            loop.Start();

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            loop.Tick(5f);
            Assert.IsFalse(loop.IsAttempting, "Callback lỗi không được kẹt cờ 'đang thử'");
            Assert.IsTrue(loop.IsRunning);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            loop.Tick(5f);
            Assert.AreEqual(2, _service.RetryCalls, "Vòng vẫn chạy tiếp ở chu kỳ sau");
        }

        [Test]
        public void NullFailureCallback_IsFine()
        {
            var loop = new DeathRetryLoop(_service, 5f, null);
            _service.OnRetry = Fail;
            loop.Start();

            Assert.DoesNotThrow(() => loop.Tick(5f));
            Assert.IsTrue(loop.IsRunning);
        }

        // ── Thành công: dừng ──────────────────────────────────────────────────

        [Test]
        public void SuccessfulAttempt_StopsTheLoop_AndNothingRetriesAfterwards()
        {
            _loop.Start();

            _loop.Tick(5f);

            Assert.AreEqual(1, _service.RetryCalls);
            Assert.IsFalse(_loop.IsRunning, "Xác nhận được → dừng vòng");
            Assert.IsEmpty(_failures);

            _loop.Tick(50f);
            _loop.Tick(50f);
            Assert.AreEqual(1, _service.RetryCalls, "Đã dừng thì không thử nữa");
        }

        [Test]
        public void FailuresThenSuccess_StopsRightAfterTheSuccess()
        {
            int attempt = 0;
            _service.OnRetry = () => ++attempt < 3 ? Fail() : Task.CompletedTask;
            _loop.Start();

            for (int i = 0; i < 6; i++) _loop.Tick(5f);

            Assert.AreEqual(3, _service.RetryCalls, "Hai lần lỗi + một lần được, sau đó không thử nữa");
            Assert.AreEqual(2, _failures.Count);
            Assert.IsFalse(_loop.IsRunning);
        }

        [Test]
        public void Stop_StopsRetrying()
        {
            _service.OnRetry = Fail;
            _loop.Start();
            _loop.Stop(); // ví dụ bootstrap bị hủy

            _loop.Tick(100f);

            Assert.AreEqual(0, _service.RetryCalls);
            Assert.IsFalse(_loop.IsRunning);
        }

        // ── MỘT vòng duy nhất, không chạy chồng ───────────────────────────────

        [Test]
        public void Start_CalledRepeatedly_IsStillOneLoop_AndDoesNotResetTheClock()
        {
            // Mỗi lần thử lỗi, bootstrap phát lại Unconfirmed → Start() lại: không được tạo vòng thứ hai hay lùi đồng hồ.
            _service.OnRetry = Fail;
            _loop.Start();
            _loop.Tick(3f);

            _loop.Start();
            _loop.Start();
            _loop.Tick(2f);

            Assert.AreEqual(1, _service.RetryCalls, "Start() lặp lại không được làm đồng hồ chạy lại từ đầu");

            _loop.Tick(5f);
            Assert.AreEqual(2, _service.RetryCalls, "Vẫn chỉ MỘT lần thử mỗi chu kỳ");
        }

        [Test]
        public void WhileAnAttemptIsInFlight_NothingOverlaps_NeitherTicksNorTheRetryButton()
        {
            var pending = new TaskCompletionSource<bool>();
            _service.OnRetry = () => pending.Task;
            _loop.Start();

            _loop.Tick(5f);
            Assert.AreEqual(1, _service.RetryCalls);
            Assert.IsTrue(_loop.IsAttempting);

            _loop.Tick(50f);
            _loop.Tick(50f);
            Task manual = _loop.RetryNowAsync();

            Assert.AreEqual(1, _service.RetryCalls, "Còn lần thử dở thì không chạy chồng (cả tự động lẫn bấm tay)");
            Assert.IsTrue(manual.IsCompleted, "Bấm tay lúc đang thử dở: bỏ qua, không treo");

            // Dọn: hoàn tất lần thử dở. Phần còn lại của nó có thể chạy sau test nhưng chỉ đụng vào đối tượng giả này.
            pending.SetResult(true);
        }

        // ── Nút "Thử lại": gọi ngay ──────────────────────────────────────────

        [Test]
        public void RetryNow_RunsImmediately_EvenBeforeTheIntervalAndWithoutTheLoop()
        {
            _service.OnRetry = Fail;

            _loop.RetryNowAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, _service.RetryCalls);
            Assert.AreEqual(1, _failures.Count);
            Assert.IsFalse(_loop.IsRunning, "Bấm tay không tự bật vòng (vòng do trạng thái 'chưa xác nhận' bật)");
        }

        [Test]
        public void RetryNow_Success_StopsARunningLoop()
        {
            _loop.Start();

            _loop.RetryNowAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, _service.RetryCalls);
            Assert.IsFalse(_loop.IsRunning);
            _loop.Tick(50f);
            Assert.AreEqual(1, _service.RetryCalls);
        }

        [Test]
        public void RetryNow_Failure_KeepsTheLoop_AndRestartsTheClock()
        {
            _service.OnRetry = Fail;
            _loop.Start();
            _loop.Tick(4f);

            _loop.RetryNowAsync().GetAwaiter().GetResult();
            Assert.AreEqual(1, _service.RetryCalls);
            Assert.IsTrue(_loop.IsRunning);

            _loop.Tick(4f);
            Assert.AreEqual(1, _service.RetryCalls, "Vừa thử tay xong thì chưa tới lần tự động kế tiếp");

            _loop.Tick(1f);
            Assert.AreEqual(2, _service.RetryCalls, "5 giây sau lần thử tay mới tự thử lại");
        }
    }
}
