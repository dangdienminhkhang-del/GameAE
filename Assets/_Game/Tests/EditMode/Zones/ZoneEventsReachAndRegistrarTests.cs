using System.Collections.Generic;
using System.Reflection;
using Game.Core.Events;
using Game.Core.Floor;
using Game.Core.Quest;
using Game.Core.Time;
using Game.PvE.Enemy;
using Game.PvE.Floor;
using Game.PvE.Zones;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Zones
{
    /// <summary>Phase 9.7a — sự kiện ZoneEntered, objective Reach, chọn nhạc theo vùng.</summary>
    public class ZoneEnteredEventTests
    {
        [Test]
        public void RaiseZoneEntered_DeliversSenderAndPayload()
        {
            object receivedSender = null;
            ZoneEnteredArgs received = null;
            System.EventHandler<ZoneEnteredArgs> handler = (s, a) => { receivedSender = s; received = a; };

            GameEvents.OnZoneEntered += handler;
            try
            {
                var sender = new object();
                GameEvents.RaiseZoneEntered(sender, new ZoneEnteredArgs { ZoneId = "zone.test_farm", DisplayName = "Vùng Thử" });

                Assert.AreSame(sender, receivedSender);
                Assert.IsNotNull(received);
                Assert.AreEqual("zone.test_farm", received.ZoneId);
                Assert.AreEqual("Vùng Thử", received.DisplayName);
            }
            finally
            {
                GameEvents.OnZoneEntered -= handler;
            }
        }

        [Test]
        public void RaiseZoneEntered_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => GameEvents.RaiseZoneEntered(this, new ZoneEnteredArgs { ZoneId = "zone.none" }));
        }
    }

    public class ReachObjectiveTests
    {
        private const string TargetZone = "zone.test_farm";

        private static QuestDefinition CreateReachQuest(int requiredCount = 1, QuestObjectiveType type = QuestObjectiveType.Reach)
        {
            QuestDefinition definition = ScriptableObject.CreateInstance<QuestDefinition>();
            Set(definition, "id", "quest.reach_test");
            Set(definition, "objectiveType", type);
            Set(definition, "targetId", TargetZone);
            Set(definition, "requiredCount", requiredCount);
            return definition;
        }

        private static void Set<T>(QuestDefinition definition, string name, T value)
        {
            FieldInfo field = typeof(QuestDefinition).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Thiếu field: {name}");
            field.SetValue(definition, value);
        }

        private static bool Counts(QuestDefinition quest, QuestLog log, string zoneId) =>
            QuestRules.CanAdvance(quest, log, QuestObjectiveType.Reach, zoneId, TimeOfDay.Morning, true);

        [Test]
        public void ActiveQuest_MatchingZone_Counts()
        {
            var quest = CreateReachQuest();
            var log = new QuestLog();
            log.Accept(quest.Id);

            Assert.IsTrue(Counts(quest, log, TargetZone));
        }

        [Test]
        public void WrongZoneId_IsIgnored()
        {
            var quest = CreateReachQuest();
            var log = new QuestLog();
            log.Accept(quest.Id);

            Assert.IsFalse(Counts(quest, log, "zone.other"));
            Assert.IsFalse(Counts(quest, log, TargetZone.ToUpperInvariant()), "So khớp theo đúng ký tự, không bỏ qua hoa thường.");
        }

        [Test]
        public void QuestNotActive_IsIgnored()
        {
            var quest = CreateReachQuest();
            var log = new QuestLog(); // chưa nhận

            Assert.IsFalse(Counts(quest, log, TargetZone));
        }

        [Test]
        public void CompletedOrReadyQuest_IsIgnored()
        {
            var quest = CreateReachQuest(1);
            var log = new QuestLog();
            log.Accept(quest.Id);
            log.AddProgress(quest.Id, quest.RequiredCount, 1); // → ReadyToTurnIn

            Assert.IsFalse(Counts(quest, log, TargetZone), "Đã đủ tiến độ thì không đếm thêm.");
        }

        [Test]
        public void OtherObjectiveType_DoesNotAdvanceFromZone()
        {
            var quest = CreateReachQuest(1, QuestObjectiveType.Talk);
            var log = new QuestLog();
            log.Accept(quest.Id);

            Assert.IsFalse(Counts(quest, log, TargetZone));
        }

        [Test]
        public void EachRealEntry_CountsOnce_BoundaryJitterDoesNot()
        {
            var quest = CreateReachQuest(2);
            var log = new QuestLog();
            log.Accept(quest.Id);

            var tracker = new ZoneTrackerLogic(2);
            var inTown = new List<ZoneCandidate> { new ZoneCandidate("zone.test_town", 0, 10f) };
            var inFarm = new List<ZoneCandidate> { new ZoneCandidate(TargetZone, 0, 10f) };

            int Feed(List<ZoneCandidate> where)
            {
                if (!tracker.Update(where, false, out string entered)) return 0;
                if (!Counts(quest, log, entered)) return 0;
                log.AddProgress(quest.Id, quest.RequiredCount, 1);
                return 1;
            }

            Feed(inTown);

            // Đứng nhích qua lại ở biên Farm/Town: không lần nào đủ nhịp để tính là vào Farm.
            int counted = 0;
            for (int i = 0; i < 10; i++) counted += Feed(i % 2 == 0 ? inFarm : inTown);
            Assert.AreEqual(0, counted);
            Assert.AreEqual(0, log.Get(quest.Id).progress);

            // Vào Farm thật (đủ nhịp) → đếm một lần, đứng lại trong Farm không đếm thêm.
            counted += Feed(inFarm);
            counted += Feed(inFarm);
            for (int i = 0; i < 5; i++) counted += Feed(inFarm);
            Assert.AreEqual(1, counted);
            Assert.AreEqual(1, log.Get(quest.Id).progress);

            // Ra Town rồi vào Farm lần nữa → đủ 2 → ReadyToTurnIn.
            Feed(inTown); Feed(inTown);
            Feed(inFarm); Feed(inFarm);
            Assert.IsTrue(log.IsReadyToTurnIn(quest.Id));
        }
    }

    public class ZoneMusicRulesTests
    {
        [Test]
        public void NoBossEngaged_UsesZoneMusic()
        {
            Assert.AreEqual("music.zone", ZoneMusicRules.Choose("music.zone", 0));
        }

        [Test]
        public void BossEngaged_ZoneMusicDoesNotOverrideBossMusic()
        {
            Assert.IsNull(ZoneMusicRules.Choose("music.zone", 1));
            Assert.IsNull(ZoneMusicRules.Choose("music.zone", 3));
        }

        [Test]
        public void ZoneWithoutMusicId_KeepsCurrentMusic()
        {
            Assert.IsNull(ZoneMusicRules.Choose(null, 0));
            Assert.IsNull(ZoneMusicRules.Choose(string.Empty, 0));
        }
    }

    /// <summary>FloorSpawnRegistrar phải đăng ký TẤT CẢ spawner của tầng (spawner chính cũ + các spawner bổ sung).</summary>
    public class FloorSpawnRegistrarMultiSpawnerTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
            {
                if (o != null) Object.DestroyImmediate(o);
            }
            _created.Clear();
        }

        private EnemySpawner NewSpawner(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<EnemySpawner>();
        }

        private FloorDefinition NewDefinition(int floorNumber, params string[] enemyIds)
        {
            FloorDefinition def = ScriptableObject.CreateInstance<FloorDefinition>();
            _created.Add(def);
            def.id = "floor.test";
            def.floorNumber = floorNumber;
            def.spawnEnemyIds = new List<string>(enemyIds);
            return def;
        }

        private FloorSpawnRegistrar NewRegistrar(EnemySpawner primary, params EnemySpawner[] extra)
        {
            var go = new GameObject("Registrar");
            _created.Add(go);
            var registrar = go.AddComponent<FloorSpawnRegistrar>();
            SetField(registrar, "enemySpawner", primary);
            SetField(registrar, "additionalSpawners", extra);
            return registrar;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Thiếu field: {name}");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Thiếu field: {name}");
            return (T)field.GetValue(target);
        }

        private static void AssertRegistered(EnemySpawner spawner, int floorNumber, string enemyId)
        {
            Assert.AreEqual(floorNumber, GetField<int>(spawner, "floorNumber"), $"{spawner.name}: sai số tầng.");
            var allowed = GetField<HashSet<string>>(spawner, "_allowedEnemyIds");
            Assert.IsNotNull(allowed, $"{spawner.name}: chưa nhận danh sách quái.");
            Assert.IsTrue(allowed.Contains(enemyId), $"{spawner.name}: thiếu quái '{enemyId}'.");
        }

        [Test]
        public void Register_ConfiguresPrimaryAndAdditionalSpawners()
        {
            EnemySpawner a = NewSpawner("A"), b = NewSpawner("B"), c = NewSpawner("C");
            FloorSpawnRegistrar registrar = NewRegistrar(a, b, c);

            registrar.Register(NewDefinition(4, "enemy.test"));

            AssertRegistered(a, 4, "enemy.test");
            AssertRegistered(b, 4, "enemy.test");
            AssertRegistered(c, 4, "enemy.test");
            Assert.AreEqual("floor.test", registrar.RegisteredFloorId);
        }

        [Test]
        public void Register_OnlyAdditionalSpawners_StillRegistersThem()
        {
            EnemySpawner b = NewSpawner("B");
            FloorSpawnRegistrar registrar = NewRegistrar(null, b);

            registrar.Register(NewDefinition(2, "enemy.test"));

            AssertRegistered(b, 2, "enemy.test");
        }

        [Test]
        public void Register_NullEntriesInArray_AreSkipped()
        {
            EnemySpawner a = NewSpawner("A"), c = NewSpawner("C");
            FloorSpawnRegistrar registrar = NewRegistrar(a, null, c);

            Assert.DoesNotThrow(() => registrar.Register(NewDefinition(3, "enemy.test")));

            AssertRegistered(a, 3, "enemy.test");
            AssertRegistered(c, 3, "enemy.test");
        }

        [Test]
        public void Register_CalledTwice_IsSafe()
        {
            EnemySpawner a = NewSpawner("A"), b = NewSpawner("B");
            FloorSpawnRegistrar registrar = NewRegistrar(a, b);
            FloorDefinition def = NewDefinition(5, "enemy.test");

            registrar.Register(def);
            Assert.DoesNotThrow(() => registrar.Register(def));

            AssertRegistered(a, 5, "enemy.test");
            AssertRegistered(b, 5, "enemy.test");
        }
    }
}
