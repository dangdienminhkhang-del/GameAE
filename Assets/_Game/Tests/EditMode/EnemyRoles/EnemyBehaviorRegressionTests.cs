using System.Reflection;
using Game.PvE.Enemy;
using Game.PvE.Enemy.Behaviors;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Hồi quy Phase 9.8: Chaser/Guard/Elite phải cho CÙNG quyết định di chuyển và tầm đánh như code cũ
    /// (đuổi thẳng bằng (đích − vị trí).normalized, vào Attack khi khoảng cách ≤ attackRange, rời khi &gt; attackRange, đánh cận chiến).
    /// Guard và Elite tạm dùng ChaserBehavior (chưa có thiết kế riêng, ghi ở Known issues).
    /// </summary>
    public class EnemyBehaviorRegressionTests
    {
        private const float AttackRange = 1.1f;

        private static readonly CombatRole[] LegacyRoles = { CombatRole.Chaser, CombatRole.Guard, CombatRole.Elite };

        private static EnemyRoleConfig Config(CombatRole role)
        {
            return new EnemyRoleConfig(role, AttackRange, 6f, 4.5f, 2.5f, 0.8f, 6f, 8f, 1.5f, 0.25f, 1f);
        }

        private static BehaviorInput Input(Vector2 self, Vector2 target)
        {
            return new BehaviorInput(self, target, Vector2.Distance(self, target), 0.016f);
        }

        // ── Enum và factory ────────────────────────────────────────────────

        [Test]
        public void CombatRole_ChaserIsZero_SoDefaultsAreSafe()
        {
            Assert.AreEqual(0, (int)CombatRole.Chaser);
            Assert.AreEqual(5, System.Enum.GetValues(typeof(CombatRole)).Length);
        }

        [Test]
        public void Factory_ChaserGuardElite_AllUseChaserBehavior_KeepingTheirOwnRole()
        {
            foreach (CombatRole role in LegacyRoles)
            {
                IEnemyBehavior behavior = EnemyBehaviorFactory.Create(Config(role));
                Assert.IsInstanceOf<ChaserBehavior>(behavior, role.ToString());
                Assert.AreEqual(role, behavior.Role);
            }
        }

        [Test]
        public void Factory_SkirmisherAndRanged_GetTheirOwnBehavior()
        {
            Assert.IsInstanceOf<SkirmisherBehavior>(EnemyBehaviorFactory.Create(Config(CombatRole.Skirmisher)));
            Assert.IsInstanceOf<RangedBehavior>(EnemyBehaviorFactory.Create(Config(CombatRole.Ranged)));
        }

        [Test]
        public void NullDefinition_ResolvesToChaserDefaults()
        {
            EnemyRoleConfig config = EnemyRoleConfig.From(null);
            Assert.AreEqual(CombatRole.Chaser, config.Role);
            Assert.IsInstanceOf<ChaserBehavior>(EnemyBehaviorFactory.Create(config));
        }

        // ── Quyết định di chuyển như code cũ ───────────────────────────────

        [Test]
        public void ChaseDirection_EqualsTheOldMoveToward_ForEveryLegacyRole()
        {
            Vector2[] selves = { Vector2.zero, new Vector2(3f, -2f), new Vector2(-5.5f, 7f), new Vector2(0.01f, 0f) };
            Vector2[] targets = { Vector2.zero, new Vector2(4f, 4f), new Vector2(-1f, 0f), new Vector2(0.01f, 0.2f) };

            foreach (CombatRole role in LegacyRoles)
            {
                IEnemyBehavior behavior = EnemyBehaviorFactory.Create(Config(role));
                foreach (Vector2 self in selves)
                foreach (Vector2 target in targets)
                {
                    Vector2 expected = (target - self).normalized; // y hệt MoveToward cũ
                    Vector2 actual = behavior.ChaseDirection(Input(self, target));
                    Assert.AreEqual(expected.x, actual.x, 1e-6f, $"{role} {self}→{target}");
                    Assert.AreEqual(expected.y, actual.y, 1e-6f, $"{role} {self}→{target}");
                }
            }
        }

        [Test]
        public void ChaseDirection_ZeroDistance_IsZeroNotNaN()
        {
            Vector2 d = EnemyBehaviorFactory.Create(Config(CombatRole.Chaser)).ChaseDirection(Input(Vector2.one, Vector2.one));
            Assert.IsFalse(float.IsNaN(d.x) || float.IsNaN(d.y));
            Assert.AreEqual(Vector2.zero, d);
        }

        // ── Tầm đánh như code cũ ────────────────────────────────────────────

        [Test]
        public void Engage_And_Disengage_FollowTheOldAttackRangeRule()
        {
            float[] distances = { 0f, 0.5f, AttackRange - 0.001f, AttackRange, AttackRange + 0.001f, 2f, 10f };

            foreach (CombatRole role in LegacyRoles)
            {
                IEnemyBehavior behavior = EnemyBehaviorFactory.Create(Config(role));
                foreach (float distance in distances)
                {
                    var input = new BehaviorInput(Vector2.zero, new Vector2(distance, 0f), distance, 0.016f);
                    Assert.AreEqual(distance <= AttackRange, behavior.ShouldEngage(input), $"{role} engage @ {distance}");
                    Assert.AreEqual(distance > AttackRange, behavior.ShouldDisengage(input), $"{role} disengage @ {distance}");
                }
            }
        }

        [Test]
        public void Engage_And_Disengage_AreAlwaysOpposites_ForLegacyRoles()
        {
            IEnemyBehavior behavior = EnemyBehaviorFactory.Create(Config(CombatRole.Chaser));
            for (float d = 0f; d < 3f; d += 0.07f)
            {
                var input = new BehaviorInput(Vector2.zero, new Vector2(d, 0f), d, 0.016f);
                Assert.AreNotEqual(behavior.ShouldEngage(input), behavior.ShouldDisengage(input), "tại " + d);
            }
        }

        // ── Đòn đánh vẫn là cận chiến cũ ───────────────────────────────────

        private GameObject _enemyGo;
        private GameObject _playerGo;
        private EnemyDefinition _definition;
        private EnemyController _enemy;

        private void BuildEnemy()
        {
            _enemyGo = new GameObject("RegressionEnemy");
            _enemyGo.SetActive(false);
            _enemyGo.AddComponent<SpriteRenderer>();
            _enemy = _enemyGo.AddComponent<EnemyController>();

            MethodInfo awake = typeof(EnemyController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(_enemy, null);

            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _enemy.SetDefinition(_definition);
            _enemy.ResetForSpawn();
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemyGo != null) Object.DestroyImmediate(_enemyGo);
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            if (_definition != null) Object.DestroyImmediate(_definition);
        }

        [Test]
        public void ChaserStrike_IsTheOldMeleeAttack_DamagesTargetOnceThenCoolsDown()
        {
            BuildEnemy();
            _playerGo = new GameObject("Player");
            _playerGo.transform.position = new Vector3(0.5f, 0f, 0f);
            var target = ProjectileTestPlayer.Create(_playerGo);
            typeof(EnemyController).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_enemy, _playerGo.transform);

            IEnemyBehavior behavior = EnemyBehaviorFactory.Create(EnemyRoleConfig.From(_definition));
            Assert.IsInstanceOf<ChaserBehavior>(behavior);

            double full = target.CurrentHealth;
            Assert.IsTrue(behavior.Strike(_enemy));
            double afterFirst = target.CurrentHealth;
            Assert.Less(afterFirst, full, "đòn cận chiến làm mất máu");

            Assert.IsFalse(behavior.Strike(_enemy), "đang hồi chiêu: không đánh lần hai");
            Assert.AreEqual(afterFirst, target.CurrentHealth, 1e-9);
        }

        [Test]
        public void ChaserStrike_WithoutTarget_DoesNothing()
        {
            BuildEnemy();
            Assert.IsFalse(EnemyBehaviorFactory.Create(EnemyRoleConfig.From(_definition)).Strike(_enemy));
        }

        // ── Tạo một lần, không tạo lại mỗi frame ───────────────────────────

        [Test]
        public void Behavior_IsCreatedOncePerDefinition_NotEveryReset()
        {
            BuildEnemy();
            FieldInfo field = typeof(EnemyController).GetField("_behavior", BindingFlags.Instance | BindingFlags.NonPublic);

            object first = field.GetValue(_enemy);
            _enemy.ResetForSpawn();
            _enemy.ResetForSpawn();
            object afterResets = field.GetValue(_enemy);

            Assert.IsNotNull(first);
            Assert.AreSame(first, afterResets, "hồi sinh từ pool dùng lại behavior, không tạo mới");
        }

        [Test]
        public void Behavior_IsRebuilt_WhenTheDefinitionChanges()
        {
            BuildEnemy();
            FieldInfo field = typeof(EnemyController).GetField("_behavior", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsInstanceOf<ChaserBehavior>(field.GetValue(_enemy));

            var ranged = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                var so = new UnityEditor.SerializedObject(ranged);
                so.FindProperty("role").intValue = (int)CombatRole.Ranged;
                so.ApplyModifiedPropertiesWithoutUndo();

                _enemy.SetDefinition(ranged);
                _enemy.ResetForSpawn();

                Assert.IsInstanceOf<RangedBehavior>(field.GetValue(_enemy));
            }
            finally
            {
                Object.DestroyImmediate(ranged);
            }
        }
    }
}
