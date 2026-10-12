using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Game.Core.Events;
using Game.Core.Quest;
using Game.Core.World;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task 2 — Preservation property tests.
    ///
    /// Xác nhận các hành vi ĐÚNG hiện tại không bị phá vỡ sau khi fix.
    ///   • Tất cả phải PASS trên code CHƯA SỬA   ← baseline được confirm
    ///   • Tất cả phải vẫn PASS sau khi fix       ← regression check
    ///
    /// Chạy trong Unity Test Runner: Window → General → Test Runner → EditMode.
    /// </summary>
    public class QuestRewardPreservationTests
    {
        // ══════════════════════════════════════════════════════════════════
        // Fake save services
        // ══════════════════════════════════════════════════════════════════

        /// <summary>ISaveService luôn thành công ngay lập tức.</summary>
        private class InstantSuccessSaveService : ISaveService
        {
            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task<T> LoadAuthoritativeAsync<T>(string key) => Task.FromResult(default(T));
            public Task SaveAsync<T>(string key, T value) => Task.CompletedTask;
            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
                => Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
        }

        /// <summary>ISaveService không bao giờ complete — dùng để test các trường hợp không cần save.</summary>
        private class NeverCompleteSaveService : ISaveService
        {
            public Task<T> LoadAsync<T>(string key) => Task.FromResult(default(T));
            public Task<T> LoadAuthoritativeAsync<T>(string key) => Task.FromResult(default(T));
            // Trả về Task không bao giờ complete — writer sẽ bị block ở await
            public Task SaveAsync<T>(string key, T value) => new TaskCompletionSource<object>().Task;
            public Task DeleteAsync(string key) => Task.CompletedTask;
            public Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
                => Task.FromResult(new Dictionary<string, object>());
            public Task SaveBatchAsync(Dictionary<string, object> keyValues) => Task.CompletedTask;
        }

        // ══════════════════════════════════════════════════════════════════
        // Reflection helpers (dùng chung cho cả 2 test file)
        // ══════════════════════════════════════════════════════════════════

        private static void SetPrivateField(object obj, string name, object value)
        {
            FieldInfo field = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{name}' không tìm thấy trên {obj.GetType().Name}");
            field.SetValue(obj, value);
        }

        private static T GetPrivateField<T>(object obj, string name)
        {
            FieldInfo field = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{name}' không tìm thấy trên {obj.GetType().Name}");
            return (T)field.GetValue(obj);
        }

        // ══════════════════════════════════════════════════════════════════
        // Quest definition helpers
        // ══════════════════════════════════════════════════════════════════

        private static QuestDefinition CreateKillQuest(
            string questId = "quest.preservation.kill",
            int requiredCount = 1,
            int expReward = 50)
        {
            var def = ScriptableObject.CreateInstance<QuestDefinition>();
            SetQuestField(def, "id", questId);
            SetQuestField(def, "objectiveType", QuestObjectiveType.Kill);
            SetQuestField(def, "targetId", "enemy.slime");
            SetQuestField(def, "requiredCount", requiredCount);
            SetQuestField(def, "experienceReward", expReward);
            return def;
        }

        private static QuestDefinition CreateCollectQuest(
            string questId = "quest.preservation.collect",
            string targetId = "item.herb",
            int requiredCount = 3,
            int expReward = 80)
        {
            var def = ScriptableObject.CreateInstance<QuestDefinition>();
            SetQuestField(def, "id", questId);
            SetQuestField(def, "objectiveType", QuestObjectiveType.Collect);
            SetQuestField(def, "targetId", targetId);
            SetQuestField(def, "requiredCount", requiredCount);
            SetQuestField(def, "experienceReward", expReward);
            return def;
        }

        private static void SetQuestField<T>(QuestDefinition def, string name, T value)
        {
            FieldInfo field = typeof(QuestDefinition).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                $"QuestDefinition: serialized field '{name}' không tồn tại. Kiểm tra tên field trong QuestDefinition.cs");
            field.SetValue(def, value);
        }

        // ══════════════════════════════════════════════════════════════════
        // PlayerQuestLog factory — tạo log với state đã inject, không qua MonoBehaviour lifecycle
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo PlayerQuestLog với quest ở trạng thái <paramref name="status"/>.
        /// Inject <paramref name="saveService"/> làm backend save.
        /// </summary>
        private static Game.PvE.Quest.PlayerQuestLog CreateQuestLog(
            QuestDefinition quest,
            QuestStatus status,
            int progress,
            ISaveService saveService)
        {
            var go = new GameObject("TestQuestLog_Preservation");
            var log = go.AddComponent<Game.PvE.Quest.PlayerQuestLog>();

            // Inject save service và trạng thái loaded
            SetPrivateField(log, "_save", saveService);
            SetPrivateField(log, "_loaded", true);
            SetPrivateField(log, "_destroyed", false);
            SetPrivateField(log, "_saveLocked", false);

            // Hủy cancellation token để tắt retry loop nếu Start kịp chạy
            var cts = GetPrivateField<System.Threading.CancellationTokenSource>(
                log, "_loadRetryCancellation");
            cts?.Cancel();

            // Thêm quest vào knownQuests
            FieldInfo knownQuestsField = typeof(Game.PvE.Quest.PlayerQuestLog)
                .GetField("knownQuests", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(knownQuestsField, "Field 'knownQuests' không tìm thấy trong PlayerQuestLog");
            var knownList = knownQuestsField.GetValue(log) as List<QuestDefinition>;
            Assert.IsNotNull(knownList, "knownQuests list null sau Awake");
            knownList.Add(quest);

            // Load state theo tham số
            if (status != QuestStatus.Available)
            {
                var data = new QuestData();
                data.quests.Add(new QuestEntryData
                {
                    questId  = quest.Id,
                    status   = (int)status,
                    progress = progress
                });
                log.LoadFromData(data);
            }

            return log;
        }

        // ══════════════════════════════════════════════════════════════════
        // NPCInteractable helpers
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Tạo NPCInteractable với state được inject thủ công — không cần scene đầy đủ.</summary>
        private static Game.PvE.NPC.NPCInteractable CreateNPCInteractable(
            bool hasPhaseRestriction,
            bool isAvailableInitially,
            bool playerInRange)
        {
            var go = new GameObject("TestNPC_Preservation");
            go.AddComponent<BoxCollider2D>(); // NPCInteractable có [RequireComponent(Collider2D)]
            var npc = go.AddComponent<Game.PvE.NPC.NPCInteractable>();

            var npcDef = ScriptableObject.CreateInstance<Game.Core.NPC.NPCDefinition>();
            SetNPCField(npcDef, "id", "npc.test.preservation");
            SetNPCField(npcDef, "displayName", "Test NPC");

            SetPrivateField(npc, "definition", npcDef);
            SetPrivateField(npc, "_hasPhaseRestriction", hasPhaseRestriction);
            SetPrivateField(npc, "_isAvailable", isAvailableInitially);
            SetPrivateField(npc, "_playerInRange", playerInRange);
            SetPrivateField(npc, "_time", null);

            return npc;
        }

        private static void SetNPCField<T>(Game.Core.NPC.NPCDefinition def, string name, T value)
        {
            FieldInfo field = typeof(Game.Core.NPC.NPCDefinition).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                field = def.GetType().BaseType?.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"NPCDefinition: serialized field '{name}' không tồn tại");
            field.SetValue(def, value);
        }

        private static void InvokeApplyAvailability(Game.PvE.NPC.NPCInteractable npc)
        {
            MethodInfo method = typeof(Game.PvE.NPC.NPCInteractable)
                .GetMethod("ApplyAvailability", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Method 'ApplyAvailability' không tìm thấy trong NPCInteractable");
            method.Invoke(npc, null);
        }

        // ══════════════════════════════════════════════════════════════════
        // Test 1 — Preservation: TryTurnIn với quest không ReadyToTurnIn → luôn trả false
        //
        // Requirements: 3.1
        // PASS trên code chưa sửa ← baseline
        // PASS sau fix            ← regression check (TryTurnIn không thay đổi guard này)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void TurnIn_QuestNotReadyToTurnIn_AlwaysReturnsFalse()
        {
            // ARRANGE
            // Quest chỉ ở Active (chưa đủ kill objective: cần 3, mới kill 1)
            var save = new InstantSuccessSaveService();
            QuestDefinition quest = CreateKillQuest(
                questId: "quest.preservation.active",
                requiredCount: 3,
                expReward: 100);

            int rewardCount = 0;
            EventHandler<int> expHandler = (_, __) => rewardCount++;
            GameEvents.OnExperienceGained += expHandler;

            Game.PvE.Quest.PlayerQuestLog questLog = null;
            try
            {
                // Status = Active, progress = 1 (chưa đủ 3)
                questLog = CreateQuestLog(quest, QuestStatus.Active, 1, save);

                // Xác nhận precondition: quest ở Active, chưa ReadyToTurnIn
                Assert.IsFalse(questLog.IsReadyToTurnIn(quest),
                    "Precondition: quest phải ở Active (chưa đủ objective)");
                Assert.IsTrue(questLog.IsActive(quest),
                    "Precondition: quest phải ở Active");

                // ACT
                bool result = questLog.TryTurnIn(quest);

                // ASSERT — Preservation: guard không thay đổi sau fix
                Assert.IsFalse(result,
                    "PRESERVATION: TryTurnIn với quest Active phải luôn trả false — " +
                    "guard này không được thay đổi sau khi fix Bug 1.");

                Assert.AreEqual(0, rewardCount,
                    "PRESERVATION: Không được cấp reward khi TryTurnIn trả false.");

                // Quest vẫn ở Active sau khi TryTurnIn thất bại
                Assert.IsTrue(questLog.IsActive(quest),
                    "PRESERVATION: Quest phải vẫn ở Active sau TryTurnIn thất bại.");
            }
            finally
            {
                GameEvents.OnExperienceGained -= expHandler;
                if (questLog != null) UnityEngine.Object.DestroyImmediate(questLog.gameObject);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Test 2 — Preservation: TryTurnIn Collect quest thiếu item → rollback về Active
        //
        // Requirements: 3.2
        // PASS trên code chưa sửa ← baseline
        // PASS sau fix            ← regression check (rollback logic không thay đổi)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void TurnIn_CollectInsufficientItems_RollsBackToActive()
        {
            // ARRANGE
            // Collect quest: cần 5 herb, inventory chỉ có 2
            const int required = 5;
            const int owned = 2;
            const string itemId = "item.herb.preservation";

            var save = new InstantSuccessSaveService();
            QuestDefinition quest = CreateCollectQuest(
                questId: "quest.preservation.collect.insufficient",
                targetId: itemId,
                requiredCount: required,
                expReward: 120);

            int rewardCount = 0;
            EventHandler<int> expHandler = (_, __) => rewardCount++;
            GameEvents.OnExperienceGained += expHandler;

            // Tạo PlayerInventory mock bằng AddComponent — không cần database đầy đủ.
            // PlayerInventory.Inventory.GetStack(itemId) sẽ trả 0 nếu không có item.
            // Ta tạo một inventory object trực tiếp và inject vào field.
            var inventoryGO = new GameObject("TestInventory_Preservation");
            var playerInventory = inventoryGO.AddComponent<Game.PvE.Items.PlayerInventory>();

            // Thêm item vào kho nội bộ của PlayerInventory qua reflection (Inventory field)
            FieldInfo inventoryField = typeof(Game.PvE.Items.PlayerInventory)
                .GetField("_inventory", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(inventoryField, "Field '_inventory' không tìm thấy trong PlayerInventory");
            var internalInventory = inventoryField.GetValue(playerInventory) as Game.Core.Items.Inventory;
            Assert.IsNotNull(internalInventory, "Internal Inventory null trong PlayerInventory");

            // Thêm 'owned' items vào inventory (nhỏ hơn 'required')
            internalInventory.AddStack(itemId, owned, 99);

            Game.PvE.Quest.PlayerQuestLog questLog = null;
            try
            {
                // Quest ở ReadyToTurnIn với progress = required
                questLog = CreateQuestLog(quest, QuestStatus.ReadyToTurnIn, required, save);

                // Xác nhận precondition
                Assert.IsTrue(questLog.IsReadyToTurnIn(quest),
                    "Precondition: quest phải ở ReadyToTurnIn trước TryTurnIn");
                Assert.AreEqual(owned, internalInventory.GetStack(itemId),
                    $"Precondition: inventory phải có {owned} item (thiếu so với required={required})");

                // ACT — truyền PlayerInventory trực tiếp qua overload có tham số inventory
                bool result = questLog.TryTurnIn(quest, playerInventory);

                // ASSERT — Preservation
                Assert.IsFalse(result,
                    "PRESERVATION: TryTurnIn Collect quest thiếu item phải trả false — " +
                    "rollback behavior không được thay đổi sau fix.");

                Assert.AreEqual(0, rewardCount,
                    "PRESERVATION: Không được cấp reward khi Collect quest thiếu item.");

                // Quest phải rollback về Active (không về Available hay Completed)
                QuestState stateAfter = questLog.GetState(quest);
                Assert.IsNotNull(stateAfter, "QuestState không tìm thấy sau TryTurnIn");
                Assert.AreEqual(QuestStatus.Active, stateAfter.status,
                    "PRESERVATION: Quest phải rollback về Active khi Collect thiếu item.");

                // Progress phải được cập nhật theo số lượng thực tế trong inventory
                Assert.AreEqual(owned, stateAfter.progress,
                    $"PRESERVATION: Progress sau rollback phải = {owned} (số item hiện có), không phải {required}.");
            }
            finally
            {
                GameEvents.OnExperienceGained -= expHandler;
                if (questLog != null) UnityEngine.Object.DestroyImmediate(questLog.gameObject);
                UnityEngine.Object.DestroyImmediate(inventoryGO);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Test 3 — Preservation: TryTurnIn thành công → Publish event fires ngay lập tức
        //
        // Requirements: 2.4, 3.3, 3.4
        // Xác nhận: thông báo "Hoàn thành nhiệm vụ!" vẫn được publish ĐỒNG BỘ trong TryTurnIn,
        // không phải defer như GrantReward. UI phải thấy Completed ngay — đây là hành vi không đổi.
        //
        // PASS trên code chưa sửa ← baseline
        // PASS sau fix            ← regression (Publish giữ nguyên vị trí, chỉ GrantReward bị defer)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void TurnIn_CompletedPublishEvent_FiresImmediately()
        {
            // ARRANGE — dùng NeverCompleteSaveService để đảm bảo test không phụ thuộc vào save hoàn thành
            // Nếu Publish fires ngay thì test PASS dù save chưa xong; sau fix điều này vẫn đúng.
            var save = new NeverCompleteSaveService();
            QuestDefinition quest = CreateKillQuest(
                questId: "quest.preservation.publish",
                requiredCount: 1,
                expReward: 75);

            bool questChangedFired = false;
            QuestStatus statusWhenFired = QuestStatus.Available;
            string messageWhenFired = null;

            EventHandler<QuestChangedArgs> questChangedHandler = (sender, args) =>
            {
                if (args.Quest == quest && args.Status == QuestStatus.Completed)
                {
                    questChangedFired = true;
                    statusWhenFired = args.Status;
                    messageWhenFired = args.Message;
                }
            };
            GameEvents.OnQuestChanged += questChangedHandler;

            Game.PvE.Quest.PlayerQuestLog questLog = null;
            try
            {
                // Quest ở ReadyToTurnIn
                questLog = CreateQuestLog(quest, QuestStatus.ReadyToTurnIn, 1, save);

                Assert.IsTrue(questLog.IsReadyToTurnIn(quest),
                    "Precondition: quest phải ở ReadyToTurnIn trước TryTurnIn");
                Assert.IsFalse(questChangedFired,
                    "Precondition: OnQuestChanged chưa được gọi trước TryTurnIn");

                // ACT — gọi TryTurnIn đồng bộ (save sẽ không bao giờ hoàn thành)
                bool result = questLog.TryTurnIn(quest);

                // ASSERT — Preservation: event fired ĐỒNG BỘ trong TryTurnIn, trước khi save xong
                Assert.IsTrue(result,
                    "PRESERVATION: TryTurnIn với quest ReadyToTurnIn phải trả true.");

                Assert.IsTrue(questChangedFired,
                    "PRESERVATION: GameEvents.OnQuestChanged phải được gọi NGAY trong TryTurnIn " +
                    "(đồng bộ, trước khi writer hoàn thành). Nếu FAIL: Publish bị defer sai.");

                Assert.AreEqual(QuestStatus.Completed, statusWhenFired,
                    "PRESERVATION: Status trong event phải là Completed ngay sau TryTurnIn thành công.");

                // Quest trong bộ nhớ phải là Completed ngay sau TryTurnIn
                Assert.IsTrue(questLog.IsCompleted(quest),
                    "PRESERVATION: IsCompleted phải true ngay sau TryTurnIn thành công — " +
                    "trạng thái bộ nhớ không được defer.");
            }
            finally
            {
                GameEvents.OnQuestChanged -= questChangedHandler;
                if (questLog != null) UnityEngine.Object.DestroyImmediate(questLog.gameObject);
                UnityEngine.Object.DestroyImmediate(quest);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Test 4 — Preservation: _playerInRange KHÔNG bị reset khi NPC vẫn available
        //
        // Requirements: 3.6
        // Fix 2 thêm `if (!_isAvailable) _playerInRange = false` — chỉ reset khi unavailable.
        // Khi _isAvailable = true, _playerInRange phải giữ nguyên.
        //
        // PASS trên code chưa sửa ← baseline (không có `if` nào reset)
        // PASS sau fix            ← regression (if chỉ chạy khi !_isAvailable)
        // ══════════════════════════════════════════════════════════════════

        [Test]
        public void PlayerInRange_NotResetWhenNpcStaysAvailable()
        {
            // ARRANGE
            // NPC không có phase restriction → _isAvailable sẽ = true sau ApplyAvailability
            // (_isAvailable = !_hasPhaseRestriction || (...) → !false = true)
            var go = new GameObject("TestNPC_AvailablePreservation");
            go.AddComponent<BoxCollider2D>(); // NPCInteractable có [RequireComponent(Collider2D)] — thiếu thì AddComponent trả null
            var npc = go.AddComponent<Game.PvE.NPC.NPCInteractable>();
            Assert.IsNotNull(npc, "AddComponent<NPCInteractable> trả null (thiếu Collider2D?)");

            var npcDef = ScriptableObject.CreateInstance<Game.Core.NPC.NPCDefinition>();
            SetNPCField(npcDef, "id", "npc.test.available");
            SetNPCField(npcDef, "displayName", "Always Available NPC");

            // NPC không có giới hạn thời gian → HasPhaseRestriction = false → _isAvailable = true
            SetNPCField(npcDef, "onlyAtSpecificTime", false);

            SetPrivateField(npc, "definition", npcDef);
            // _hasPhaseRestriction = false → ApplyAvailability sẽ cho _isAvailable = true
            SetPrivateField(npc, "_hasPhaseRestriction", false);
            SetPrivateField(npc, "_playerInRange", true);  // Player đang đứng trong vùng
            SetPrivateField(npc, "_isAvailable", true);    // NPC hiện đang available
            SetPrivateField(npc, "_time", null);

            try
            {
                // Xác nhận precondition
                Assert.IsTrue(GetPrivateField<bool>(npc, "_playerInRange"),
                    "Precondition: _playerInRange phải true trước ApplyAvailability");

                // ACT — ApplyAvailability với _hasPhaseRestriction=false → _isAvailable = true
                InvokeApplyAvailability(npc);

                // Xác nhận _isAvailable vẫn true (không có restriction → luôn available)
                bool isAvailableAfter = GetPrivateField<bool>(npc, "_isAvailable");
                Assert.IsTrue(isAvailableAfter,
                    "Precondition: _isAvailable phải vẫn true (NPC không có phase restriction)");

                // ASSERT — Preservation: _playerInRange KHÔNG được reset khi NPC available
                bool playerInRangeAfter = GetPrivateField<bool>(npc, "_playerInRange");
                Assert.IsTrue(playerInRangeAfter,
                    "PRESERVATION: _playerInRange phải vẫn true sau ApplyAvailability khi _isAvailable=true. " +
                    "Fix 2 chỉ được reset _playerInRange khi _isAvailable=false — " +
                    "nếu FAIL: Fix 2 bị over-aggressive, reset cả khi NPC available.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(npcDef);
            }
        }
    }
}
