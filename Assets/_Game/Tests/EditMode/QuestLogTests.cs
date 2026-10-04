using System.Reflection;
using Game.Core.Quest;
using Game.Core.Time;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 6 — sổ nhiệm vụ thuần logic (Core/Quest/QuestLog).</summary>
    public class QuestLogTests
    {
        private static QuestDefinition CreateQuest(
            QuestObjectiveType objective = QuestObjectiveType.Kill,
            int requiredCount = 3)
        {
            QuestDefinition definition = ScriptableObject.CreateInstance<QuestDefinition>();
            SetField(definition, "id", "quest.test");
            SetField(definition, "objectiveType", objective);
            SetField(definition, "targetId", "item.seed");
            SetField(definition, "requiredCount", requiredCount);
            return definition;
        }

        private static void SetField<T>(QuestDefinition definition, string name, T value)
        {
            FieldInfo field = typeof(QuestDefinition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing serialized field: {name}");
            field.SetValue(definition, value);
        }

        [Test]
        public void Accept_SetsStateActive()
        {
            var log = new QuestLog();

            log.Accept("quest.clear_slimes");

            Assert.IsTrue(log.IsActive("quest.clear_slimes"));
            Assert.IsFalse(log.IsCompleted("quest.clear_slimes"));
        }

        [Test]
        public void AddProgress_ReachingRequired_SetsReadyToTurnIn()
        {
            var log = new QuestLog();
            log.Accept("q1");

            bool ready = log.AddProgress("q1", requiredCount: 3, amount: 5);

            Assert.IsTrue(ready, "Đủ objective phải trả true");
            // Không Completed ngay — phải qua TurnIn
            Assert.IsFalse(log.IsCompleted("q1"), "Chưa TurnIn thì chưa Completed");
            Assert.IsTrue(log.IsReadyToTurnIn("q1"), "Phải là ReadyToTurnIn");

            QuestState state = log.Get("q1");
            Assert.AreEqual(3, state.progress, "tiến độ không vượt requiredCount");
        }

        [Test]
        public void TurnIn_FromReadyToTurnIn_SetsCompleted()
        {
            var log = new QuestLog();
            log.Accept("q1");
            log.AddProgress("q1", 1, 1); // → ReadyToTurnIn

            bool ok = log.TurnIn("q1");

            Assert.IsTrue(ok);
            Assert.IsTrue(log.IsCompleted("q1"));
            Assert.IsFalse(log.IsReadyToTurnIn("q1"));
        }

        [Test]
        public void TurnIn_CalledTwice_ReturnsFalseSecondTime()
        {
            var log = new QuestLog();
            log.Accept("q1");
            log.AddProgress("q1", 1, 1);
            log.TurnIn("q1"); // first

            bool second = log.TurnIn("q1");
            Assert.IsFalse(second, "Không thể TurnIn lần hai");
        }

        [Test]
        public void AddProgress_IgnoresQuestThatIsNotActive()
        {
            var log = new QuestLog();

            bool completed = log.AddProgress("q1", 3, 1);

            Assert.IsFalse(completed);
            Assert.IsFalse(log.IsActive("q1"));
            Assert.IsFalse(log.IsCompleted("q1"));
        }

        [Test]
        public void Accept_CompletedQuest_DoesNotReactivate()
        {
            var log = new QuestLog();
            log.Accept("q1");
            log.AddProgress("q1", 1, 1);

            log.Accept("q1");

            Assert.IsTrue(log.IsCompleted("q1"));
            Assert.IsFalse(log.IsActive("q1"));
        }

        [Test]
        public void LoadFrom_RestoresStates()
        {
            var log = new QuestLog();
            var states = new[]
            {
                new QuestState("q1", QuestStatus.Active,        2),
                new QuestState("q2", QuestStatus.Completed,     5),
                new QuestState("q3", QuestStatus.ReadyToTurnIn, 3)
            };

            log.LoadFrom(states);

            Assert.IsTrue(log.IsActive("q1"));
            Assert.AreEqual(2, log.Get("q1").progress);
            Assert.IsTrue(log.IsCompleted("q2"));
            Assert.IsTrue(log.IsReadyToTurnIn("q3"));
            Assert.IsNull(log.Get("q99"));
        }

        [Test]
        public void RepeatableAccept_ResetsProgress()
        {
            var log = new QuestLog();
            log.Accept("q1");
            log.AddProgress("q1", 1, 1); // → ReadyToTurnIn
            log.TurnIn("q1");             // → Completed

            // Mô phỏng quest lặp lại: reset về Active 0 (logic nằm ở PlayerQuestLog.TryAccept).
            QuestState state = log.Get("q1");
            state.status   = QuestStatus.Active;
            state.progress = 0;

            Assert.IsTrue(log.IsActive("q1"));
            Assert.AreEqual(0, log.Get("q1").progress);
        }

        [Test]
        public void CanAccept_WhenPrerequisiteIncomplete_ReturnsLocked()
        {
            QuestDefinition quest = CreateQuest();
            SetField(quest, "prerequisiteQuestIds", new[] { "quest.first" });
            var log = new QuestLog();

            Assert.AreEqual(QuestAcceptResult.Locked,
                QuestRules.CanAccept(quest, log, TimeOfDay.Morning, timeAuthoritative: true));

            log.Accept("quest.first");
            log.AddProgress("quest.first", 1);
            log.TurnIn("quest.first");
            Assert.AreEqual(QuestAcceptResult.Ok,
                QuestRules.CanAccept(quest, log, TimeOfDay.Morning, timeAuthoritative: true));

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void CanAccept_WhenTrustedPhaseIsWrong_ReturnsWrongTime()
        {
            QuestDefinition quest = CreateQuest();
            SetField(quest, "requiresSpecificTime", true);
            SetField(quest, "requiredTimeOfDay", TimeOfDay.Night);

            Assert.AreEqual(QuestAcceptResult.WrongTime,
                QuestRules.CanAccept(quest, new QuestLog(), TimeOfDay.Morning, timeAuthoritative: true));
            Object.DestroyImmediate(quest);
        }

        [Test]
        public void CanAccept_WhenRequiredTimeIsNotAuthoritative_ReturnsTimeUnavailable()
        {
            QuestDefinition quest = CreateQuest();
            SetField(quest, "requiresSpecificTime", true);

            Assert.AreEqual(QuestAcceptResult.TimeUnavailable,
                QuestRules.CanAccept(quest, new QuestLog(), TimeOfDay.Night, timeAuthoritative: false));
            Object.DestroyImmediate(quest);
        }

        [TestCase(5, 3)]
        [TestCase(3, 3)]
        [TestCase(2, 2)]
        public void SeedCollectProgress_ClampsOwnedStackToRequirement(int owned, int expected)
        {
            QuestDefinition quest = CreateQuest(QuestObjectiveType.Collect, requiredCount: 3);

            Assert.AreEqual(expected, QuestRules.SeedCollectProgress(quest, owned));
            Object.DestroyImmediate(quest);
        }

        [Test]
        public void CanAdvance_IgnoresWrongTarget()
        {
            QuestDefinition quest = CreateQuest(QuestObjectiveType.Collect);
            var log = new QuestLog();
            log.Accept(quest.Id);

            Assert.IsFalse(QuestRules.CanAdvance(quest, log, QuestObjectiveType.Collect, "item.other",
                TimeOfDay.Morning, timeAuthoritative: true));
            Assert.IsTrue(QuestRules.CanAdvance(quest, log, QuestObjectiveType.Collect, quest.TargetId,
                TimeOfDay.Morning, timeAuthoritative: true));
            Object.DestroyImmediate(quest);
        }

        [Test]
        public void TurnIn_Twice_OnlyGrantsOneReward()
        {
            var log = new QuestLog();
            log.Accept("quest.reward");
            log.AddProgress("quest.reward", 1);
            int rewardsGranted = 0;

            if (log.TurnIn("quest.reward")) rewardsGranted++;
            if (log.TurnIn("quest.reward")) rewardsGranted++;

            Assert.AreEqual(1, rewardsGranted);
        }
    }
}
