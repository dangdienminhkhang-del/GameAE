using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// P9-1d — PersonalProgress: theo dõi tầng cao nhất đã vào (key "progress").
    /// Chỉ tăng; lưu đúng một lần mỗi lần tăng; lỗi đọc/lưu không ném và không làm mất giá trị trong bộ nhớ;
    /// không đổi currentFloorId.
    /// </summary>
    public class PersonalProgressTests
    {
        /// <summary>ISaveService giả: lưu JSON trong bộ nhớ, đếm số lần đọc/lưu, giả lập lỗi đọc/lưu.</summary>
        private sealed class FakeSave : ISaveService
        {
            public readonly Dictionary<string, string> Store = new Dictionary<string, string>();
            public bool FailLoad;
            public int FailSavesRemaining;
            public int LoadCalls;
            public int SaveCalls;

            public void Put<T>(string key, T value) => Store[key] = JsonUtility.ToJson(value);

            public ProgressData SavedProgress =>
                Store.TryGetValue(SaveKeys.Progress, out var json) ? JsonUtility.FromJson<ProgressData>(json) : null;

            public Task<T> LoadAsync<T>(string key)
            {
                LoadCalls++;
                if (FailLoad) throw new InvalidOperationException("giả lập lỗi đọc");
                T value = Store.TryGetValue(key, out var json) ? (T)JsonUtility.FromJson(json, typeof(T)) : default;
                return Task.FromResult(value);
            }

            public Task<T> LoadAuthoritativeAsync<T>(string key) => LoadAsync<T>(key);

            public Task SaveAsync<T>(string key, T value)
            {
                if (FailSavesRemaining > 0)
                {
                    FailSavesRemaining--;
                    throw new InvalidOperationException("giả lập lỗi lưu");
                }
                SaveCalls++;
                Store[key] = JsonUtility.ToJson(value);
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string key)
            {
                Store.Remove(key);
                return Task.CompletedTask;
            }

            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
                => Task.FromResult(new Dictionary<string, object>());

            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
        }

        private static void Run(Task task) => task.GetAwaiter().GetResult();

        // ── Khởi đầu / đọc ──────────────────────────────────────────────────

        [Test]
        public void NewInstance_HighestFloorIsOne()
        {
            var progress = new PersonalProgress(new FakeSave());

            Assert.AreEqual(1, progress.HighestFloor);
            Assert.IsFalse(progress.HasUnsavedChanges);
        }

        [Test]
        public void Load_ReadsStoredValues()
        {
            var save = new FakeSave();
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 3, currentFloorId = "test.c" });
            var progress = new PersonalProgress(save);

            Run(progress.LoadAsync());

            Assert.AreEqual(3, progress.HighestFloor);
            Assert.AreEqual("test.c", progress.CurrentFloorId);
        }

        [Test]
        public void Load_MissingKey_DefaultsToOne_WithoutThrowing()
        {
            var progress = new PersonalProgress(new FakeSave());

            Assert.DoesNotThrow(() => Run(progress.LoadAsync()));
            Assert.AreEqual(1, progress.HighestFloor);
        }

        [Test]
        public void Load_Failure_DefaultsToOne_WithoutThrowing()
        {
            var progress = new PersonalProgress(new FakeSave { FailLoad = true });

            Assert.DoesNotThrow(() => Run(progress.LoadAsync()));
            Assert.AreEqual(1, progress.HighestFloor);
        }

        [Test]
        public void Load_StoredValueBelowOne_IsClampedToOne()
        {
            var save = new FakeSave();
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 0 });
            var progress = new PersonalProgress(save);

            Run(progress.LoadAsync());

            Assert.AreEqual(1, progress.HighestFloor);
        }

        // ── Chỉ tăng ────────────────────────────────────────────────────────

        [Test]
        public void Record_HigherFloor_UpdatesMemoryAndSavesExactlyOnce()
        {
            var save = new FakeSave();
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Run(progress.RecordFloorEnteredAsync(2));

            Assert.AreEqual(2, progress.HighestFloor);
            Assert.AreEqual(1, save.SaveCalls);
            Assert.AreEqual(2, save.SavedProgress.personalHighestFloor);
            Assert.IsFalse(progress.HasUnsavedChanges);
        }

        [Test]
        public void Record_SameOrLowerFloor_DoesNotSaveAndNeverLowers()
        {
            var save = new FakeSave();
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());
            Run(progress.RecordFloorEnteredAsync(2));
            int savesAfterIncrease = save.SaveCalls;

            Run(progress.RecordFloorEnteredAsync(2)); // vào lại đúng tầng đó
            Run(progress.RecordFloorEnteredAsync(1)); // về tầng thấp hơn

            Assert.AreEqual(2, progress.HighestFloor, "Chỉ tăng, không bao giờ giảm.");
            Assert.AreEqual(savesAfterIncrease, save.SaveCalls, "Không tăng thì không lưu.");
        }

        [Test]
        public void Record_StartingFloor_DoesNotSave()
        {
            var save = new FakeSave();
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Run(progress.RecordFloorEnteredAsync(1));

            Assert.AreEqual(0, save.SaveCalls);
        }

        [Test]
        public void Record_EachIncrease_SavesOncePerIncrease()
        {
            var save = new FakeSave();
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Run(progress.RecordFloorEnteredAsync(2));
            Run(progress.RecordFloorEnteredAsync(3));

            Assert.AreEqual(3, progress.HighestFloor);
            Assert.AreEqual(2, save.SaveCalls);
            Assert.AreEqual(3, save.SavedProgress.personalHighestFloor);
        }

        [Test]
        public void Record_ZeroOrNegativeFloor_IsIgnored()
        {
            var save = new FakeSave();
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Run(progress.RecordFloorEnteredAsync(0));
            Run(progress.RecordFloorEnteredAsync(-4));

            Assert.AreEqual(1, progress.HighestFloor);
            Assert.AreEqual(0, save.SaveCalls);
        }

        // ── Không đổi currentFloorId ────────────────────────────────────────

        [Test]
        public void Record_KeepsCurrentFloorIdUnchanged()
        {
            var save = new FakeSave();
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 1, currentFloorId = "test.keep_me" });
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Run(progress.RecordFloorEnteredAsync(2));

            Assert.AreEqual("test.keep_me", save.SavedProgress.currentFloorId);
            Assert.AreEqual(2, save.SavedProgress.personalHighestFloor);
        }

        [Test]
        public void Record_WithoutExplicitLoad_LoadsFirst_SoStoredCurrentFloorIdIsNotOverwrittenByDefault()
        {
            var save = new FakeSave();
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 1, currentFloorId = "test.keep_me" });
            var progress = new PersonalProgress(save); // chưa gọi LoadAsync (ví dụ vào tầng ngay khi đang đọc)

            Run(progress.RecordFloorEnteredAsync(2));

            Assert.AreEqual(1, save.LoadCalls);
            Assert.AreEqual("test.keep_me", save.SavedProgress.currentFloorId);
            Assert.AreEqual(2, save.SavedProgress.personalHighestFloor);
        }

        // ── Lỗi lưu ─────────────────────────────────────────────────────────

        [Test]
        public void SaveFailure_KeepsMemoryValue_DoesNotThrow_AndRetriesOnNextSave()
        {
            var save = new FakeSave { FailSavesRemaining = 1 };
            var progress = new PersonalProgress(save);
            Run(progress.LoadAsync());

            Assert.DoesNotThrow(() => Run(progress.RecordFloorEnteredAsync(2)));
            Assert.AreEqual(2, progress.HighestFloor, "Lưu lỗi vẫn giữ giá trị trong bộ nhớ (cái chết dùng đúng).");
            Assert.AreEqual(0, save.SaveCalls);
            Assert.IsTrue(progress.HasUnsavedChanges);

            Run(progress.RecordFloorEnteredAsync(2)); // lần lưu sau: thử lại bản đang chờ

            Assert.AreEqual(1, save.SaveCalls);
            Assert.AreEqual(2, save.SavedProgress.personalHighestFloor);
            Assert.IsFalse(progress.HasUnsavedChanges);
        }

        // ── Lỗi đọc: không ghi đè bản đã lưu ────────────────────────────────

        [Test]
        public void LoadFailed_ThenRecord_KeepsMemory_ButDoesNotOverwriteStoredProgress()
        {
            var save = new FakeSave { FailLoad = true };
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 3 });
            var progress = new PersonalProgress(save);

            Assert.DoesNotThrow(() => Run(progress.RecordFloorEnteredAsync(2)));

            Assert.AreEqual(2, progress.HighestFloor);
            Assert.AreEqual(0, save.SaveCalls, "Chưa đọc được save thì không được ghi đè mức đã lưu.");
            Assert.AreEqual(3, save.SavedProgress.personalHighestFloor);
        }

        [Test]
        public void LoadRecovers_ThenSaveKeepsTheHigherStoredValue()
        {
            var save = new FakeSave { FailLoad = true };
            save.Put(SaveKeys.Progress, new ProgressData { personalHighestFloor = 3 });
            var progress = new PersonalProgress(save);
            Run(progress.RecordFloorEnteredAsync(2)); // đọc lỗi → chưa lưu

            save.FailLoad = false; // mạng có lại
            Run(progress.RecordFloorEnteredAsync(2));

            Assert.AreEqual(3, progress.HighestFloor, "Gộp: giữ mức cao hơn đã lưu.");
            Assert.AreEqual(3, save.SavedProgress.personalHighestFloor, "Không được hạ mức đã lưu xuống.");
        }

        // ── Không có nơi lưu ────────────────────────────────────────────────

        [Test]
        public void NullSaveService_KeepsMemoryAndNeverThrows()
        {
            var progress = new PersonalProgress(null);

            Assert.DoesNotThrow(() => Run(progress.LoadAsync()));
            Assert.DoesNotThrow(() => Run(progress.RecordFloorEnteredAsync(3)));
            Assert.AreEqual(3, progress.HighestFloor);
        }
    }
}
