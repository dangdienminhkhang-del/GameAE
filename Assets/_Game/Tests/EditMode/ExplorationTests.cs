using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public class ExplorationTests
    {
        private sealed class MemorySave : ISaveService
        {
            public readonly Dictionary<string, string> Store = new Dictionary<string, string>();
            public bool FailSave, FailLoad;
            public int Saves;

            public Task<T> LoadAsync<T>(string key)
            {
                if (FailLoad) throw new InvalidOperationException("offline");
                return Task.FromResult(Store.TryGetValue(key, out var json) ? JsonUtility.FromJson<T>(json) : default(T));
            }

            public Task<T> LoadAuthoritativeAsync<T>(string key) { return LoadAsync<T>(key); }

            public Task SaveAsync<T>(string key, T value)
            {
                if (FailSave) throw new InvalidOperationException("offline");
                Store[key] = JsonUtility.ToJson(value);
                Saves++;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string key) { Store.Remove(key); return Task.CompletedTask; }
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys) { return Task.FromResult(new Dictionary<string, object>()); }
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) { return Task.CompletedTask; }
        }

        private static void Wait(Task t) { t.GetAwaiter().GetResult(); }

        // ── MapRevealGrid ──

        [Test]
        public void Grid_RevealCircle_RevealsNearbyBlocksOnly_AndReportsChangeOnce()
        {
            var g = new MapRevealGrid(0f, 0f, 160f, 80f);
            Assert.AreEqual(20, g.BlocksX);
            Assert.AreEqual(10, g.BlocksY);

            Assert.IsTrue(g.RevealCircle(20f, 20f, 10f));
            Assert.IsTrue(g.IsRevealedAt(20f, 20f), "Khối chứa tâm phải được vẽ.");
            Assert.IsFalse(g.IsRevealedAt(120f, 60f), "Khối xa tâm không được vẽ.");
            Assert.Greater(g.Fraction, 0f);
            int version = g.Version;
            Assert.IsFalse(g.RevealCircle(20f, 20f, 10f), "Vẽ lại đúng vùng cũ không được báo thay đổi.");
            Assert.AreEqual(version, g.Version);
        }

        [Test]
        public void Grid_RevealCircle_AtEdgeAndOutside_DoesNotThrow()
        {
            var g = new MapRevealGrid(100f, 100f, 64f, 64f);
            Assert.DoesNotThrow(() => g.RevealCircle(0f, 0f, 12f));
            Assert.DoesNotThrow(() => g.RevealCircle(1000f, 1000f, 12f));
            Assert.AreEqual(0, g.RevealedCount, "Hình tròn nằm hoàn toàn ngoài bản đồ không vẽ khối nào.");
            Assert.IsTrue(g.RevealCircle(100f, 100f, 12f));
        }

        [Test]
        public void Grid_Base64_RoundTrips_AndMergeNeverLosesProgress()
        {
            var a = new MapRevealGrid(0f, 0f, 160f, 80f);
            a.RevealCircle(30f, 30f, 14f);
            var b = new MapRevealGrid(0f, 0f, 160f, 80f);
            b.RevealCircle(130f, 50f, 14f);
            int bOnly = b.RevealedCount;

            Assert.IsTrue(b.MergeBase64(a.ToBase64(), a.BlocksX, a.BlocksY));
            Assert.AreEqual(bOnly + a.RevealedCount, b.RevealedCount, "Gộp phải là hợp của hai vùng (hai vùng này không giao nhau).");
            Assert.IsTrue(b.IsRevealedAt(30f, 30f));
            Assert.IsTrue(b.IsRevealedAt(130f, 50f));
        }

        [Test]
        public void Grid_Merge_RejectsWrongSizeAndGarbage()
        {
            var a = new MapRevealGrid(0f, 0f, 160f, 80f);
            a.RevealCircle(30f, 30f, 14f);
            var other = new MapRevealGrid(0f, 0f, 64f, 64f);
            Assert.IsFalse(other.MergeBase64(a.ToBase64(), a.BlocksX, a.BlocksY), "Sai kích thước lưới phải bị từ chối.");
            Assert.IsFalse(a.MergeBase64("%%%không-phải-base64%%%", a.BlocksX, a.BlocksY));
            Assert.IsFalse(a.MergeBase64("", a.BlocksX, a.BlocksY));
        }

        // ── ExplorationProgress ──

        [Test]
        public void Progress_SaveThenLoad_RestoresMapAndBeacons()
        {
            var save = new MemorySave();
            var first = new ExplorationProgress(save);
            Wait(first.LoadAsync());
            var grid = first.GetGrid("floor.02", 0f, 0f, 160f, 80f);
            Assert.IsTrue(first.Reveal("floor.02", 40f, 40f, 12f));
            Assert.IsTrue(first.Light("beacon.a"));
            Assert.IsFalse(first.Light("beacon.a"), "Thắp lần hai không được báo là mới.");
            Assert.IsTrue(first.HasUnsavedChanges);
            Wait(first.SaveIfDirtyAsync());
            Assert.IsFalse(first.HasUnsavedChanges);

            var second = new ExplorationProgress(save);
            Wait(second.LoadAsync());
            var restored = second.GetGrid("floor.02", 0f, 0f, 160f, 80f);
            Assert.AreEqual(grid.RevealedCount, restored.RevealedCount);
            Assert.IsTrue(restored.IsRevealedAt(40f, 40f));
            Assert.IsTrue(second.IsLit("beacon.a"));
            Assert.IsFalse(second.IsLit("beacon.b"));
        }

        [Test]
        public void Progress_SaveFailure_KeepsMemoryAndRetries()
        {
            var save = new MemorySave();
            var p = new ExplorationProgress(save);
            Wait(p.LoadAsync());
            p.GetGrid("floor.02", 0f, 0f, 160f, 80f);
            p.Reveal("floor.02", 40f, 40f, 12f);
            p.Light("beacon.a");

            save.FailSave = true;
            LogAssert.ignoreFailingMessages = true;
            Wait(p.SaveIfDirtyAsync());
            LogAssert.ignoreFailingMessages = false;
            Assert.IsTrue(p.HasUnsavedChanges, "Lưu lỗi thì vẫn phải còn cờ cần lưu.");
            Assert.IsTrue(p.IsLit("beacon.a"), "Bộ nhớ vẫn giữ Đèn đã thắp.");

            save.FailSave = false;
            Wait(p.SaveIfDirtyAsync());
            Assert.IsFalse(p.HasUnsavedChanges);
            Assert.IsTrue(save.Store.ContainsKey(SaveKeys.Map));
            Assert.IsTrue(save.Store.ContainsKey(SaveKeys.Beacons));
        }

        [Test]
        public void Progress_DoesNotOverwriteSave_WhenItCouldNotBeRead()
        {
            var save = new MemorySave { FailLoad = true };
            var p = new ExplorationProgress(save);
            LogAssert.ignoreFailingMessages = true;
            Wait(p.LoadAsync());
            p.GetGrid("floor.02", 0f, 0f, 160f, 80f);
            p.Reveal("floor.02", 40f, 40f, 12f);
            Wait(p.SaveIfDirtyAsync());
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(0, save.Saves, "Chưa đọc được save thì không được ghi (sẽ xoá bản đồ đã lưu).");
            Assert.IsTrue(p.HasUnsavedChanges);
        }

        [Test]
        public void Progress_KeepsSavedDataOfFloorsNotLoadedThisSession()
        {
            var save = new MemorySave();
            var a = new ExplorationProgress(save);
            Wait(a.LoadAsync());
            a.GetGrid("floor.01", 0f, 0f, 160f, 80f);
            a.Reveal("floor.01", 40f, 40f, 12f);
            Wait(a.SaveIfDirtyAsync());

            var b = new ExplorationProgress(save);
            Wait(b.LoadAsync());
            b.GetGrid("floor.02", 0f, 0f, 160f, 80f);
            b.Reveal("floor.02", 10f, 10f, 12f);
            Wait(b.SaveIfDirtyAsync());

            var c = new ExplorationProgress(save);
            Wait(c.LoadAsync());
            Assert.IsTrue(c.GetGrid("floor.01", 0f, 0f, 160f, 80f).IsRevealedAt(40f, 40f), "Tầng 1 không được mất khi chỉ chơi Tầng 2.");
            Assert.IsTrue(c.GetGrid("floor.02", 0f, 0f, 160f, 80f).IsRevealedAt(10f, 10f));
        }

        [Test]
        public void SaveKeys_All_ContainsMapAndBeacons()
        {
            CollectionAssert.Contains(SaveKeys.All, SaveKeys.Map, "Reset nhân vật phải xoá cả bản đồ.");
            CollectionAssert.Contains(SaveKeys.All, SaveKeys.Beacons, "Reset nhân vật phải xoá cả Đèn Hiệu đã thắp.");
        }

        // ── FastTravelRules ──

        [Test]
        public void FastTravel_Rules_CoverEveryRefusalReason()
        {
            const float far = 50f;
            Assert.AreEqual(FastTravelRules.Result.Ok, FastTravelRules.Check(false, 99f, true, true, far));
            Assert.AreEqual(FastTravelRules.Result.PlayerDead, FastTravelRules.Check(true, 99f, true, true, far));
            Assert.AreEqual(FastTravelRules.Result.InCombat, FastTravelRules.Check(false, 4.9f, true, true, far));
            Assert.AreEqual(FastTravelRules.Result.Ok, FastTravelRules.Check(false, 5.0f, true, true, far), "Đúng 5 giây là hết khoá giao chiến.");
            Assert.AreEqual(FastTravelRules.Result.WrongFloor, FastTravelRules.Check(false, 99f, true, false, far));
            Assert.AreEqual(FastTravelRules.Result.DestinationLocked, FastTravelRules.Check(false, 99f, false, true, far));
            Assert.AreEqual(FastTravelRules.Result.AlreadyThere, FastTravelRules.Check(false, 99f, true, true, 1f));
        }

        [Test]
        public void FastTravel_MovementBreaksChannelOnlyBeyondThreshold()
        {
            Assert.IsFalse(FastTravelRules.ChannelBrokenByMovement(0f));
            Assert.IsFalse(FastTravelRules.ChannelBrokenByMovement(FastTravelRules.MoveCancelDistance));
            Assert.IsTrue(FastTravelRules.ChannelBrokenByMovement(FastTravelRules.MoveCancelDistance + 0.01f));
        }

        [Test]
        public void FastTravel_EveryRefusalHasAMessage()
        {
            foreach (FastTravelRules.Result r in Enum.GetValues(typeof(FastTravelRules.Result)))
            {
                if (r == FastTravelRules.Result.Ok) continue;
                Assert.IsFalse(string.IsNullOrEmpty(FastTravelRules.Describe(r)), $"Thiếu thông báo cho {r}.");
            }
        }
    }
}
