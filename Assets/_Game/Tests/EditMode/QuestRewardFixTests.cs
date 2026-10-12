using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Events;
using Game.Core.Quest;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task 7 — Fix-checking tests (Phase 6, tiêu chí 3: không mất state, không double reward).
    ///
    /// Thay cho hai test "exploration" cũ (vốn kỳ vọng BUG còn tồn tại và sẽ đỏ sau khi sửa).
    /// Ở đây mọi assertion kỳ vọng hành vi ĐÃ SỬA:
    ///   • Thưởng chỉ được cấp sau khi lần lưu chứa Completed thành công, và đúng một lần.
    ///   • Lưu lỗi / còn đang lưu / người chơi chết → không cấp thưởng.
    ///   • NPC bị ẩn → _playerInRange bị reset (không kẹt phím E).
    ///
    /// Lưu ý EditMode: Awake/OnEnable/OnDestroy của MonoBehaviour KHÔNG chạy, nên test tự inject state
    /// bằng reflection và tự "tắt" writer ở cuối (Shutdown) để không còn log lỗi trễ sang test khác.
    /// </summary>
    public class QuestRewardFixTests
    {
        // ══════════════════════════════════════════════════════════════════
        // Fake ISaveService
        // ══════════════════════════════════════════════════════════════════

        private abstract class FakeSaveBase : ISaveService
        {
            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task<T> LoadAuthoritativeAsync<T>(string key) => Task.FromResult(default(T));
            public abstract Task SaveAsync<T>(string key, T value);
            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
                => Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
        }

        /// <summary>Lưu thành công ngay lập tức.</summary>
        private sealed class InstantSuccessSave : FakeSaveBase
        {
            public override Task SaveAsync<T>(string key, T value) => Task.CompletedTask;
        }

        /// <summary>Lưu luôn lỗi.</summary>
        private sealed class AlwaysFailSave : FakeSaveBase
        {
            public override Task SaveAsync<T>(string key, T value)
                => Task.FromException(new Exception("Simulated save failure"));
        }

        /// <summary>Lưu chỉ hoàn thành khi test cho phép (mô phỏng mạng chậm).</summary>
        private sealed class ManualSave : FakeSaveBase
        {
            public readonly TaskCompletionSource<bool> Gate = new TaskCompletionSource<bool>();
            public override Task SaveAsync<T>(string key, T value) => Gate.Task;
        }

        // ══════════════════════════════════════════════════════════════════
        // Reflection helpers
        // ══════════════════════════════════════════════════════════════════

        private static void SetPrivateField(object obj, string name, object value)
        {
            FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{name}' không tìm thấy trên {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        private static T GetPrivateField<T>(object obj, string name)
        {
            FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{name}' không tìm thấy trên {obj.GetType().Name}");
            return (T)field.GetValue(obj);
        }

        private static void InvokePrivate(object obj, string method)
        {
            MethodInfo info = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"Method '{method}' không tìm thấy trên {obj.GetType().Name}");
            info.Invoke(obj, null);
        }

        // ══════════════════════════════════════════════════════════════════
        // Quest / PlayerQuestLog helpers
        // ══════════════════════════════════════════════════════════════════

        private static QuestDefinition CreateKillQuest(string questId, int expReward = 200)
        {
            var def = ScriptableObject.CreateInstance<QuestDefinition>();
            SetField(def, "id", questId);
            SetField(def, "objectiveType", QuestObjectiveType.Kill);
            SetField(def, "targetId", "enemy.slime");
            SetField(def, "requiredCount", 1);
            SetField(def, "experienceReward", expReward);
            return def;
        }

        private static void SetField<T>(QuestDefinition def, string name, T value)
        {
            FieldInfo field = typeof(QuestDefinition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"QuestDefinition: field '{name}' không tồn tại");
            field.SetValue(def, value);
        }

        /// <summary>
        /// PlayerQuestLog đã loaded, quest ở ReadyToTurnIn, dùng save giả.
        /// KHÔNG hủy _loadRetryCancellation ở đây: token đã hủy làm Task.Delay ném ngay và writer
        /// sẽ tự khởi động lại liên tục khi save lỗi.
        /// </summary>
        private static Game.PvE.Quest.PlayerQuestLog CreateReadyLog(QuestDefinition quest, ISaveService save)
        {
            var go = new GameObject("TestQuestLog_Fix");
            var log = go.AddComponent<Game.PvE.Quest.PlayerQuestLog>();
            SetPrivateField(log, "_save", save);

            var data = new QuestData();
            data.quests.Add(new QuestEntryData
            {
                questId = quest.Id,
                status = (int)QuestStatus.ReadyToTurnIn,
                progress = quest.RequiredCount
            });
            log.LoadFromData(data); // đặt _loaded = true
            return log;
        }

        /// <summary>Dừng writer còn treo (Task.Delay / save chưa xong) rồi hủy GameObject.</summary>
        private static void Shutdown(Game.PvE.Quest.PlayerQuestLog log)
        {
            if (log == null) return;
            SetPrivateField(log, "_destroyed", true);
            GetPrivateField<System.Threading.CancellationTokenSource>(log, "_loadRetryCancellation").Cancel();
            UnityEngine.Object.DestroyImmediate(log.gameObject);
        }

        // ══════════════════════════════════════════════════════════════════
        // QuestRewardLedger (logic thuần)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void Ledger_CommitAfterSuccess_ReturnsRewardExactlyOnce()
        {
            var ledger = new QuestRewardLedger();
            QuestDefinition quest = CreateKillQuest("quest.ledger.once");
            try
            {
                ledger.Enqueue(quest);
                ledger.BeginSave();

                IReadOnlyList<QuestDefinition> first = ledger.CommitSave();
                IReadOnlyList<QuestDefinition> second = ledger.CommitSave();

                Assert.AreEqual(1, first.Count, "Lần commit đầu phải trả đúng 1 thưởng.");
                Assert.AreSame(quest, first[0]);
                Assert.AreEqual(0, second.Count, "Commit lần hai không được trả thưởng nữa.");
                Assert.AreEqual(0, ledger.PendingCount);
                Assert.AreEqual(0, ledger.InFlightCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(quest); }
        }

        [Test]
        public void Ledger_SaveFailsThenRetrySucceeds_RewardGrantedOnceAfterSuccess()
        {
            var ledger = new QuestRewardLedger();
            QuestDefinition quest = CreateKillQuest("quest.ledger.retry");
            try
            {
                ledger.Enqueue(quest);

                // Lần lưu đầu lỗi → thưởng quay lại hàng chờ, không mất.
                ledger.BeginSave();
                ledger.RollbackSave();
                Assert.AreEqual(1, ledger.PendingCount, "Lưu lỗi: thưởng phải còn trong hàng chờ.");
                Assert.AreEqual(0, ledger.InFlightCount);

                // Lần thử lại thành công → cấp đúng một lần.
                ledger.BeginSave();
                IReadOnlyList<QuestDefinition> granted = ledger.CommitSave();
                Assert.AreEqual(1, granted.Count);
                Assert.AreEqual(0, ledger.CommitSave().Count, "Không được cấp lần hai.");
            }
            finally { UnityEngine.Object.DestroyImmediate(quest); }
        }

        [Test]
        public void Ledger_RewardEnqueuedDuringInFlightSave_NotCommittedWithEarlierSave()
        {
            var ledger = new QuestRewardLedger();
            QuestDefinition a = CreateKillQuest("quest.ledger.a");
            QuestDefinition b = CreateKillQuest("quest.ledger.b");
            try
            {
                ledger.Enqueue(a);
                ledger.BeginSave();     // snapshot đầu chỉ chứa A
                ledger.Enqueue(b);      // B hoàn thành khi lần lưu đầu còn đang chạy

                IReadOnlyList<QuestDefinition> firstCommit = ledger.CommitSave();
                Assert.AreEqual(1, firstCommit.Count, "Chỉ thưởng A thuộc về snapshot đầu.");
                Assert.AreSame(a, firstCommit[0]);
                Assert.AreEqual(1, ledger.PendingCount, "Thưởng B phải chờ lần lưu kế tiếp.");

                ledger.BeginSave();
                IReadOnlyList<QuestDefinition> secondCommit = ledger.CommitSave();
                Assert.AreEqual(1, secondCommit.Count);
                Assert.AreSame(b, secondCommit[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void Ledger_Clear_DropsPendingAndInFlight()
        {
            var ledger = new QuestRewardLedger();
            QuestDefinition a = CreateKillQuest("quest.ledger.clear.a");
            QuestDefinition b = CreateKillQuest("quest.ledger.clear.b");
            try
            {
                ledger.Enqueue(a);
                ledger.BeginSave();
                ledger.Enqueue(b);

                ledger.Clear();

                Assert.AreEqual(0, ledger.PendingCount);
                Assert.AreEqual(0, ledger.InFlightCount);
                ledger.BeginSave();
                Assert.AreEqual(0, ledger.CommitSave().Count, "Sau Clear không còn thưởng nào để cấp.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // PlayerQuestLog.TryTurnIn
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void TurnIn_SaveFails_QuestCompletedInMemory_RewardNotGranted()
        {
            LogAssert.Expect(LogType.Error, new Regex("Save thất bại"));

            QuestDefinition quest = CreateKillQuest("quest.fix.savefails");
            int rewardCount = 0;
            EventHandler<int> handler = (_, __) => rewardCount++;
            GameEvents.OnExperienceGained += handler;

            Game.PvE.Quest.PlayerQuestLog log = null;
            try
            {
                log = CreateReadyLog(quest, new AlwaysFailSave());

                bool result = log.TryTurnIn(quest);

                Assert.IsTrue(result, "TryTurnIn phải ghi nhận turn-in.");
                Assert.IsTrue(log.IsCompleted(quest), "UI phải thấy Completed ngay trong bộ nhớ.");
                Assert.AreEqual(0, rewardCount,
                    "Lưu chưa thành công thì KHÔNG được cấp thưởng (chống double-reward khi app bị đóng).");
                Assert.AreEqual(1, GetPrivateField<QuestRewardLedger>(log, "_rewards").PendingCount,
                    "Thưởng phải còn trong hàng chờ để cấp sau khi lưu thành công.");
            }
            finally
            {
                GameEvents.OnExperienceGained -= handler;
                Shutdown(log);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        [Test]
        public void TurnIn_SaveSucceeds_RewardGrantedExactlyOnce()
        {
            QuestDefinition quest = CreateKillQuest("quest.fix.savesucceeds", expReward: 200);
            int rewardCount = 0;
            int lastExp = 0;
            EventHandler<int> handler = (_, exp) => { rewardCount++; lastExp = exp; };
            GameEvents.OnExperienceGained += handler;

            Game.PvE.Quest.PlayerQuestLog log = null;
            try
            {
                log = CreateReadyLog(quest, new InstantSuccessSave());

                bool first = log.TryTurnIn(quest);
                bool second = log.TryTurnIn(quest);
                log.SaveAsync().Wait();

                Assert.IsTrue(first);
                Assert.IsFalse(second, "Turn-in lần hai phải bị từ chối.");
                Assert.IsTrue(log.IsCompleted(quest));
                Assert.AreEqual(1, rewardCount, "Thưởng phải được cấp đúng một lần.");
                Assert.AreEqual(200, lastExp);
            }
            finally
            {
                GameEvents.OnExperienceGained -= handler;
                Shutdown(log);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        [Test]
        public void TurnIn_SaveStillInFlight_RewardNotGrantedYet()
        {
            QuestDefinition quest = CreateKillQuest("quest.fix.inflight");
            int rewardCount = 0;
            EventHandler<int> handler = (_, __) => rewardCount++;
            GameEvents.OnExperienceGained += handler;

            var save = new ManualSave();
            Game.PvE.Quest.PlayerQuestLog log = null;
            try
            {
                log = CreateReadyLog(quest, save);

                Assert.IsTrue(log.TryTurnIn(quest));

                QuestRewardLedger ledger = GetPrivateField<QuestRewardLedger>(log, "_rewards");
                Assert.AreEqual(0, rewardCount, "Lần lưu còn đang chạy thì chưa được cấp thưởng.");
                Assert.AreEqual(1, ledger.InFlightCount, "Thưởng phải thuộc về lần lưu đang chạy.");
                Assert.AreEqual(0, ledger.PendingCount);
            }
            finally
            {
                GameEvents.OnExperienceGained -= handler;
                // Shutdown TRƯỚC (đặt _destroyed) rồi mới nhả lần lưu treo: writer thấy _destroyed nên
                // không cấp thưởng và không log lỗi. (TrySetCanceled sẽ bị writer coi là lưu lỗi → LogError.)
                Shutdown(log);
                save.Gate.TrySetResult(true);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        [Test]
        public void TurnIn_PlayerDiesWhileSaving_RewardDropped()
        {
            QuestDefinition quest = CreateKillQuest("quest.fix.death");
            int rewardCount = 0;
            EventHandler<int> handler = (_, __) => rewardCount++;
            GameEvents.OnExperienceGained += handler;

            var save = new ManualSave();
            Game.PvE.Quest.PlayerQuestLog log = null;
            try
            {
                log = CreateReadyLog(quest, save);
                Assert.IsTrue(log.TryTurnIn(quest));

                // Người chơi chết khi lần lưu còn đang chạy → khóa lưu, bỏ thưởng.
                InvokePrivate(log, "LockSaving");
                QuestRewardLedger ledger = GetPrivateField<QuestRewardLedger>(log, "_rewards");
                Assert.AreEqual(0, ledger.PendingCount);
                Assert.AreEqual(0, ledger.InFlightCount);

                // Lần lưu cũ hoàn thành muộn cũng không được cấp thưởng.
                save.Gate.TrySetResult(true);
                Assert.AreEqual(0, rewardCount, "Đã chết thì không cấp thưởng.");
            }
            finally
            {
                GameEvents.OnExperienceGained -= handler;
                Shutdown(log);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // NPCInteractable
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void Npc_BecomesUnavailable_ResetsPlayerInRange()
        {
            var go = new GameObject("TestNPC_Fix");
            go.AddComponent<BoxCollider2D>(); // NPCInteractable yêu cầu một Collider2D
            var npc = go.AddComponent<Game.PvE.NPC.NPCInteractable>();
            var npcDef = ScriptableObject.CreateInstance<Game.Core.NPC.NPCDefinition>();

            try
            {
                SetPrivateField(npc, "definition", npcDef);
                SetPrivateField(npc, "_hasPhaseRestriction", true); // NPC giới hạn theo pha
                SetPrivateField(npc, "_time", null);                // chưa có giờ tin cậy → phải ẩn
                SetPrivateField(npc, "_playerInRange", true);
                SetPrivateField(npc, "_isAvailable", true);

                InvokePrivate(npc, "ApplyAvailability");

                Assert.IsFalse(GetPrivateField<bool>(npc, "_isAvailable"),
                    "Giờ chưa authoritative + NPC có giới hạn pha → NPC phải không khả dụng.");
                Assert.IsFalse(GetPrivateField<bool>(npc, "_playerInRange"),
                    "NPC bị ẩn thì _playerInRange phải về false (collider tắt nên OnTriggerExit2D không chạy).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(npcDef);
            }
        }
    }
}
