using System.Reflection;
using System.Text.RegularExpressions;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Stats;
using Game.PvE.Enemy;
using Game.PvE.Enemy.Behaviors;
using Game.PvE.Enemy.Projectile;
using Game.PvE.SafeZone;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    internal static class ProjectileTestKit
    {
        public static void Invoke(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(info, "Thiếu phương thức " + method);
            info.Invoke(target, null);
        }

        public static EnemyProjectile[] All(EnemyProjectilePool pool)
        {
            return pool.GetComponentsInChildren<EnemyProjectile>(true);
        }

        public static EnemyProjectile FirstActive(EnemyProjectilePool pool)
        {
            foreach (EnemyProjectile p in All(pool)) if (p.IsActive) return p;
            return null;
        }
    }

    public class EnemyProjectilePoolTests
    {
        private GameObject _template;
        private GameObject _poolGo;
        private EnemyProjectilePool _pool;
        private int _firedCount;

        private void OnFired() => _firedCount++;

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("ProjectileTemplate");
            _template.AddComponent<EnemyProjectile>();
            _poolGo = new GameObject("ProjectilePool");
            _pool = _poolGo.AddComponent<EnemyProjectilePool>();
            _firedCount = 0;
            EnemyProjectilePool.Fired += OnFired;
        }

        [TearDown]
        public void TearDown()
        {
            EnemyProjectilePool.Fired -= OnFired;
            ProjectileTestKit.Invoke(_pool, "OnDisable"); // trả Current về null, thu hồi đạn
            Object.DestroyImmediate(_poolGo);
            Object.DestroyImmediate(_template);
        }

        private bool Fire()
        {
            return _pool.TryFire(Vector2.zero, Vector2.right, 8f, 1.5f, 0.25f, new DamageInfo(5.0), null);
        }

        [Test]
        public void Prewarm_BuildsTheFixedNumberOfInactiveProjectiles()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 5);

            Assert.AreEqual(5, _pool.Capacity);
            Assert.AreEqual(5, _pool.FreeCount);
            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(5, _pool.InstantiateCount);
            foreach (EnemyProjectile p in ProjectileTestKit.All(_pool))
            {
                Assert.IsFalse(p.gameObject.activeSelf, "đạn dựng sẵn phải tắt");
            }
        }

        [Test]
        public void Prewarm_Twice_IsIgnored()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 3);
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 10);

            Assert.AreEqual(3, _pool.Capacity);
            Assert.AreEqual(3, _pool.InstantiateCount);
        }

        [Test]
        public void Prewarm_NullPrefab_DoesNothing()
        {
            _pool.Prewarm(null, 5);
            Assert.AreEqual(0, _pool.Capacity);
            Assert.AreEqual(0, _pool.FreeCount);
        }

        [Test]
        public void TryFire_BeforePrewarm_ReturnsFalse_WithoutThrowing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Hết đạn"));
            Assert.IsFalse(Fire());
        }

        [Test]
        public void TryFire_TakesFromPool_AndNeverInstantiatesAfterPrewarm()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 3);
            int before = _pool.InstantiateCount;

            Assert.IsTrue(Fire());
            Assert.IsTrue(Fire());

            Assert.AreEqual(2, _pool.ActiveCount);
            Assert.AreEqual(1, _pool.FreeCount);
            Assert.AreEqual(before, _pool.InstantiateCount, "không Instantiate khi chiến đấu");
            Assert.AreEqual(3, ProjectileTestKit.All(_pool).Length, "số đạn trong scene không tăng");
        }

        [Test]
        public void TryFire_NullDamage_IsRejected()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 2);
            Assert.IsFalse(_pool.TryFire(Vector2.zero, Vector2.right, 8f, 1.5f, 0.25f, null, null));
            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(2, _pool.FreeCount);
        }

        [Test]
        public void TryFire_RaisesFiredOncePerShot()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 3);
            Fire();
            Fire();
            Assert.AreEqual(2, _firedCount);
        }

        [Test]
        public void Release_ReturnsProjectileForReuse_WithoutNewInstances()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 1);
            Assert.IsTrue(Fire());
            EnemyProjectile active = ProjectileTestKit.FirstActive(_pool);
            Assert.IsNotNull(active);

            _pool.Release(active);
            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(1, _pool.FreeCount);
            Assert.IsFalse(active.IsActive);

            Assert.IsTrue(Fire(), "dùng lại viên vừa trả");
            Assert.AreSame(active, ProjectileTestKit.FirstActive(_pool));
            Assert.AreEqual(1, _pool.InstantiateCount);
        }

        [Test]
        public void Release_Twice_IsSafe_AndDoesNotDuplicateInFreeList()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 2);
            Fire();
            EnemyProjectile active = ProjectileTestKit.FirstActive(_pool);

            _pool.Release(active);
            _pool.Release(active);
            _pool.Release(null);

            Assert.AreEqual(2, _pool.FreeCount, "trả hai lần không làm free list phình ra");
            Assert.AreEqual(0, _pool.ActiveCount);
        }

        [Test]
        public void Exhausted_DropsTheShot_AndLogsExactlyOnce()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 2);
            Assert.IsTrue(Fire());
            Assert.IsTrue(Fire());

            LogAssert.Expect(LogType.Warning, new Regex("Hết đạn"));
            Assert.IsFalse(Fire(), "hết chỗ: bỏ phát bắn");
            // Lần thứ hai và thứ ba KHÔNG được log nữa: một cảnh báo thừa sẽ làm test đỏ (LogAssert).
            Assert.IsFalse(Fire());
            Assert.IsFalse(Fire());

            Assert.AreEqual(2, _pool.ActiveCount);
            Assert.AreEqual(2, _pool.InstantiateCount, "hết đạn cũng không Instantiate thêm");
            Assert.AreEqual(2, _firedCount, "phát bắn bị bỏ không phát tiếng bắn");
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveProjectile()
        {
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 4);
            Fire();
            Fire();
            Fire();

            _pool.ReleaseAll();

            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(4, _pool.FreeCount);
            Assert.IsNull(ProjectileTestKit.FirstActive(_pool));
        }

        [Test]
        public void Current_IsRegisteredOnEnable_AndClearedOnDisable()
        {
            ProjectileTestKit.Invoke(_pool, "OnEnable");
            Assert.AreSame(_pool, EnemyProjectilePool.Current);

            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 2);
            Fire();
            ProjectileTestKit.Invoke(_pool, "OnDisable");

            Assert.IsNull(EnemyProjectilePool.Current);
            Assert.AreEqual(0, _pool.ActiveCount, "pool tắt (tầng tắt) thì thu hồi hết đạn");
        }

        [Test]
        public void Disable_OfAnOlderPool_DoesNotClearAnotherPoolsRegistration()
        {
            var otherGo = new GameObject("OtherPool");
            try
            {
                var other = otherGo.AddComponent<EnemyProjectilePool>();
                ProjectileTestKit.Invoke(_pool, "OnEnable");
                ProjectileTestKit.Invoke(other, "OnEnable");
                Assert.AreSame(other, EnemyProjectilePool.Current);

                ProjectileTestKit.Invoke(_pool, "OnDisable");
                Assert.AreSame(other, EnemyProjectilePool.Current);

                ProjectileTestKit.Invoke(other, "OnDisable");
                Assert.IsNull(EnemyProjectilePool.Current);
            }
            finally
            {
                Object.DestroyImmediate(otherGo);
            }
        }

        [Test]
        public void Bound_Player_IsCachedAndNotRebuiltWhenSameTransform()
        {
            var playerGo = new GameObject("Player");
            try
            {
                ProjectileTestPlayer.Create(playerGo);
                _pool.BindPlayer(playerGo.transform);
                IDamageable first = _pool.PlayerDamageable;
                _pool.BindPlayer(playerGo.transform);

                Assert.IsNotNull(first);
                Assert.AreSame(first, _pool.PlayerDamageable);
                Assert.AreSame(playerGo.transform, _pool.PlayerTransform);
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
            }
        }
    }

    /// <summary>Đạn trong thế giới (Phase 9.8): trúng đúng một lần qua đường chung, không bypass Safe Zone, thu hồi đúng các tình huống.</summary>
    public class EnemyProjectileWorldTests
    {
        private GameObject _template;
        private GameObject _poolGo;
        private EnemyProjectilePool _pool;
        private GameObject _playerGo;
        private PlayerHandle _player;
        private GameObject _zoneGo;
        private BoxCollider2D _zone;
        private GameObject _wallGo;
        private int _healthChangedCount;
        private double _lastDelta;

        /// <summary>Bao HealthComponent thật: TakeDamageCalls = số lần OnHealthChanged phát ra (mỗi lần trúng đúng một lần).</summary>
        private sealed class PlayerHandle
        {
            private readonly Game.PvE.Player.HealthComponent _health;
            private readonly System.Func<int> _damageCalls;

            public PlayerHandle(Game.PvE.Player.HealthComponent health, System.Func<int> damageCalls)
            {
                _health = health;
                _damageCalls = damageCalls;
            }

            public int TakeDamageCalls => _damageCalls();
            public double Health => _health.CurrentHealth;
            public bool Dead
            {
                set { if (value) ProjectileTestPlayer.Kill(_health); }
            }
        }

        private void OnHealthChanged(object sender, HealthChangedArgs args)
        {
            _healthChangedCount++;
            _lastDelta = args.Delta;
        }

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("ProjectileTemplate");
            _template.AddComponent<EnemyProjectile>();
            _poolGo = new GameObject("ProjectilePool");
            _pool = _poolGo.AddComponent<EnemyProjectilePool>();
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 4);
            ProjectileTestKit.Invoke(_pool, "OnEnable");

            _playerGo = new GameObject("Player");
            _playerGo.transform.position = new Vector3(5f, 0f, 0f);
            _player = new PlayerHandle(ProjectileTestPlayer.Create(_playerGo), () => _healthChangedCount);
            _pool.BindPlayer(_playerGo.transform);

            _healthChangedCount = 0;
            GameEvents.OnHealthChanged += OnHealthChanged;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnHealthChanged -= OnHealthChanged;
            if (_zone != null) SafeZoneProvider.GetOrCreate().Unregister(_zone);
            ProjectileTestKit.Invoke(_pool, "OnDisable");
            Object.DestroyImmediate(_poolGo);
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_playerGo);
            if (_zoneGo != null) Object.DestroyImmediate(_zoneGo);
            if (_wallGo != null) Object.DestroyImmediate(_wallGo);
        }

        private EnemyProjectile FireFromOrigin(float speed = 10f, float lifetime = 2f, EnemyController owner = null)
        {
            bool fired = _pool.TryFire(Vector2.zero, Vector2.right, speed, lifetime, 0.25f, new DamageInfo(7.0), owner);
            Assert.IsTrue(fired);
            return ProjectileTestKit.FirstActive(_pool);
        }

        private void MakeSafeZone(Vector2 center, Vector2 size)
        {
            _zoneGo = new GameObject("SafeZone");
            _zoneGo.transform.position = new Vector3(center.x, center.y, 0f);
            _zone = _zoneGo.AddComponent<BoxCollider2D>();
            _zone.size = size;
            Physics2D.SyncTransforms();
            SafeZoneProvider.GetOrCreate().Register(_zone);
        }

        // ── Trúng người chơi ────────────────────────────────────────────────

        [Test]
        public void HittingThePlayer_DealsDamageExactlyOnce_ThroughTheCommonPath()
        {
            EnemyProjectile projectile = FireFromOrigin();

            projectile.Tick(0.5f); // đi 5 đơn vị: tới đúng chỗ người chơi
            projectile.Tick(0.5f); // đạn đã thu hồi: tick tiếp không làm gì

            Assert.AreEqual(1, _player.TakeDamageCalls);
            Assert.AreEqual(93.0, _player.Health, 1e-6);
            Assert.AreEqual(1, _healthChangedCount, "OnHealthChanged phát đúng một lần (đường chung), không có đường thứ hai");
            Assert.AreEqual(-7.0, _lastDelta, 1e-6);
            Assert.AreEqual(0, _pool.ActiveCount, "trúng người chơi thì đạn được thu hồi");
        }

        [Test]
        public void HugeDeltaStep_StillHitsThePlayer_NoTunneling()
        {
            EnemyProjectile projectile = FireFromOrigin(speed: 10f, lifetime: 5f);

            projectile.Tick(2f); // bước dài 20 đơn vị, người chơi ở giữa đoạn

            Assert.AreEqual(1, _player.TakeDamageCalls);
            Assert.AreEqual(0, _pool.ActiveCount);
        }

        [Test]
        public void Projectile_MovesStraight_AndDoesNotHomeInOnTheMovingPlayer()
        {
            EnemyProjectile projectile = FireFromOrigin();
            Vector2 locked = projectile.Direction;

            _playerGo.transform.position = new Vector3(0f, 10f, 0f); // người chơi né sang chỗ khác
            projectile.Tick(0.2f);
            projectile.Tick(0.2f);

            Assert.AreEqual(locked, projectile.Direction, "đạn không đổi hướng theo người chơi");
            Assert.AreEqual(0, _player.TakeDamageCalls, "né khỏi đường đạn thì không mất máu");
            Assert.AreEqual(4f, projectile.transform.position.x, 1e-3f);
            Assert.AreEqual(0f, projectile.transform.position.y, 1e-3f);
        }

        [Test]
        public void PlayerStandingStillInThePath_StillTakesTheHit_NoIFrames()
        {
            EnemyProjectile projectile = FireFromOrigin();
            projectile.Tick(0.6f);
            Assert.AreEqual(1, _player.TakeDamageCalls);
        }

        // ── Hết giờ ─────────────────────────────────────────────────────────

        [Test]
        public void Lifetime_ReleasesTheProjectile_WithoutDamage()
        {
            _playerGo.transform.position = new Vector3(50f, 0f, 0f);
            EnemyProjectile projectile = FireFromOrigin(speed: 1f, lifetime: 0.1f);

            projectile.Tick(0.05f);
            Assert.AreEqual(1, _pool.ActiveCount);
            projectile.Tick(0.06f);

            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(0, _player.TakeDamageCalls);
        }

        // ── Safe Zone ───────────────────────────────────────────────────────

        [Test]
        public void PlayerInsideSafeZone_TakesNoDamage_ProjectileIsReleased()
        {
            MakeSafeZone(new Vector2(5f, 0f), new Vector2(4f, 4f)); // người chơi ở (5,0) nằm trong vùng

            // Đạn xuất phát ngay sát người chơi để chạm người chơi trước khi gặp biên vùng.
            _playerGo.transform.position = new Vector3(1f, 0f, 0f);
            _zone.transform.position = new Vector3(1f, 0f, 0f);
            Physics2D.SyncTransforms();
            EnemyProjectile projectile = FireFromOrigin();

            projectile.Tick(0.2f);

            Assert.AreEqual(0, _player.TakeDamageCalls, "đạn không bypass Safe Zone");
            Assert.AreEqual(0, _healthChangedCount);
            Assert.AreEqual(0, _pool.ActiveCount);
        }

        [Test]
        public void ProjectileEnteringASafeZone_IsReleasedBeforeReachingAnything()
        {
            MakeSafeZone(new Vector2(6f, 0f), new Vector2(4f, 4f)); // x từ 4 đến 8
            _playerGo.transform.position = new Vector3(20f, 0f, 0f);
            EnemyProjectile projectile = FireFromOrigin();

            projectile.Tick(0.5f); // tới x = 5: đã vào vùng

            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(0, _player.TakeDamageCalls);
        }

        // ── Người chơi chết, tầng tắt ───────────────────────────────────────

        [Test]
        public void PlayerDeath_ReleasesAllInFlightProjectiles()
        {
            FireFromOrigin();
            _pool.TryFire(new Vector2(0f, 3f), Vector2.right, 5f, 2f, 0.25f, new DamageInfo(7.0), null);
            Assert.AreEqual(2, _pool.ActiveCount);

            GameEvents.RaisePlayerDied(this, new DamageInfo(1.0));

            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(4, _pool.FreeCount);
        }

        [Test]
        public void AlreadyDeadPlayer_IsNotDamaged_AndProjectileIsReleased()
        {
            EnemyProjectile projectile = FireFromOrigin();
            _player.Dead = true;

            projectile.Tick(0.5f);

            Assert.AreEqual(0, _player.TakeDamageCalls);
            Assert.AreEqual(0, _pool.ActiveCount);
        }

        [Test]
        public void FloorDeactivated_ReleasesProjectilesFiredByItsEnemies()
        {
            var floorRoot = new GameObject("FloorContent");
            var enemyGo = new GameObject("FloorEnemy");
            enemyGo.transform.SetParent(floorRoot.transform);
            var owner = enemyGo.AddComponent<EnemyController>();
            try
            {
                EnemyProjectile projectile = FireFromOrigin(speed: 1f, lifetime: 5f, owner: owner);
                _playerGo.transform.position = new Vector3(50f, 0f, 0f);

                floorRoot.SetActive(false); // tầng bị tắt
                projectile.Tick(0.05f);

                Assert.AreEqual(0, _pool.ActiveCount, "đạn của quái thuộc tầng đã tắt phải được thu hồi");
            }
            finally
            {
                Object.DestroyImmediate(floorRoot);
            }
        }

        [Test]
        public void PoolDisabledWithTheFloor_ReleasesEverything()
        {
            FireFromOrigin();
            ProjectileTestKit.Invoke(_pool, "OnDisable");
            Assert.AreEqual(0, _pool.ActiveCount);
        }

        // ── Vật cản ─────────────────────────────────────────────────────────

        [Test]
        public void Obstacle_StopsTheProjectile_BeforeItReachesThePlayer()
        {
            _wallGo = new GameObject("Wall");
            _wallGo.transform.position = new Vector3(2f, 0f, 0f);
            _wallGo.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 4f);
            Physics2D.SyncTransforms();
            _pool.ObstacleMask = 1 << _wallGo.layer;

            EnemyProjectile projectile = FireFromOrigin();
            projectile.Tick(0.5f);

            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(0, _player.TakeDamageCalls);
        }

        [Test]
        public void EmptyObstacleMask_DoesNotQueryPhysics_AndProjectileKeepsFlying()
        {
            _playerGo.transform.position = new Vector3(50f, 0f, 0f);
            EnemyProjectile projectile = FireFromOrigin(speed: 2f, lifetime: 5f);

            projectile.Tick(0.5f);

            Assert.AreEqual(1, _pool.ActiveCount);
            Assert.AreEqual(1f, projectile.transform.position.x, 1e-3f);
        }
    }

    /// <summary>Hành vi Ranged ở mức quái thật: bắn đúng MỘT viên theo hướng khóa lúc bắn, có hồi chiêu, không bắn vào Safe Zone.</summary>
    public class RangedStrikeTests
    {
        private GameObject _template;
        private GameObject _poolGo;
        private EnemyProjectilePool _pool;
        private GameObject _enemyGo;
        private EnemyController _enemy;
        private GameObject _playerGo;
        private EnemyDefinition _definition;
        private int _firedCount;

        private void OnFired() => _firedCount++;

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("ProjectileTemplate");
            _template.AddComponent<EnemyProjectile>();
            _poolGo = new GameObject("ProjectilePool");
            _pool = _poolGo.AddComponent<EnemyProjectilePool>();
            _pool.Prewarm(_template.GetComponent<EnemyProjectile>(), 4);
            ProjectileTestKit.Invoke(_pool, "OnEnable");
            _firedCount = 0;
            EnemyProjectilePool.Fired += OnFired;

            _playerGo = new GameObject("Player");
            _playerGo.transform.position = new Vector3(4f, 3f, 0f);
            ProjectileTestPlayer.Create(_playerGo);

            _enemyGo = new GameObject("RangedEnemy");
            _enemyGo.SetActive(false);
            _enemy = _enemyGo.AddComponent<EnemyController>();

            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(_definition);
            so.FindProperty("role").intValue = (int)CombatRole.Ranged;
            so.ApplyModifiedPropertiesWithoutUndo();
            _enemy.SetDefinition(_definition);

            typeof(EnemyController).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_enemy, _playerGo.transform);
        }

        [TearDown]
        public void TearDown()
        {
            EnemyProjectilePool.Fired -= OnFired;
            ProjectileTestKit.Invoke(_pool, "OnDisable");
            Object.DestroyImmediate(_poolGo);
            Object.DestroyImmediate(_template);
            Object.DestroyImmediate(_enemyGo);
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_definition);
        }

        private IEnemyBehavior RangedBehaviorOf() => EnemyBehaviorFactory.Create(EnemyRoleConfig.From(_definition));

        [Test]
        public void Strike_FiresExactlyOneProjectile_AimedAtTheTargetAtThatMoment()
        {
            IEnemyBehavior behavior = RangedBehaviorOf();
            Assert.IsInstanceOf<RangedBehavior>(behavior);

            bool fired = behavior.Strike(_enemy);

            Assert.IsTrue(fired);
            Assert.AreEqual(1, _pool.ActiveCount);
            Assert.AreEqual(1, _firedCount);
            EnemyProjectile projectile = ProjectileTestKit.FirstActive(_pool);
            Assert.AreEqual(0.8f, projectile.Direction.x, 1e-4f, "hướng tới (4,3) từ gốc");
            Assert.AreEqual(0.6f, projectile.Direction.y, 1e-4f);
        }

        [Test]
        public void Strike_LocksDirection_PlayerMovingAfterwardsDoesNotBendTheShot()
        {
            IEnemyBehavior behavior = RangedBehaviorOf();
            behavior.Strike(_enemy);
            EnemyProjectile projectile = ProjectileTestKit.FirstActive(_pool);
            Vector2 locked = projectile.Direction;

            _playerGo.transform.position = new Vector3(-8f, 9f, 0f);
            projectile.Tick(0.1f);

            Assert.AreEqual(locked, projectile.Direction);
        }

        [Test]
        public void Strike_StartsTheAttackCooldown()
        {
            Assert.IsTrue(_enemy.IsReadyToAttack, "điều kiện của test");
            RangedBehaviorOf().Strike(_enemy);
            Assert.IsFalse(_enemy.IsReadyToAttack, "bắn xong phải hồi chiêu theo definition");
        }

        [Test]
        public void Strike_NeverFiresAtATargetInsideTheSafeZone()
        {
            typeof(EnemyController).GetField("_targetInSafeZone", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_enemy, true);
            Assert.IsTrue(_enemy.TargetInSafeZone, "điều kiện của test");

            bool fired = RangedBehaviorOf().Strike(_enemy);

            Assert.IsFalse(fired);
            Assert.AreEqual(0, _pool.ActiveCount);
            Assert.AreEqual(0, _firedCount);
        }

        [Test]
        public void Strike_WithoutAPoolInTheScene_DoesNothingButStillCoolsDown()
        {
            ProjectileTestKit.Invoke(_pool, "OnDisable"); // Current = null
            bool fired = RangedBehaviorOf().Strike(_enemy);

            Assert.IsFalse(fired);
            Assert.IsFalse(_enemy.IsReadyToAttack, "không có pool vẫn hồi chiêu để quái không lặp báo đòn từng khung hình");
        }

        [Test]
        public void Strike_PoolExhausted_DropsTheShot_AndStillCoolsDown()
        {
            for (int i = 0; i < 4; i++) _pool.TryFire(Vector2.zero, Vector2.right, 1f, 5f, 0.25f, new DamageInfo(1.0), null);
            LogAssert.Expect(LogType.Warning, new Regex("Hết đạn"));

            bool fired = RangedBehaviorOf().Strike(_enemy);

            Assert.IsFalse(fired);
            Assert.AreEqual(4, _pool.ActiveCount);
            Assert.IsFalse(_enemy.IsReadyToAttack);
        }

        [Test]
        public void ProjectileDamage_ScaleZero_GivesZero_AndPositiveScaleGivesAtLeastOne()
        {
            // Không so hai lần tính damage với nhau (có thể dính chí mạng ngẫu nhiên); chỉ kiểm các mốc tất định.
            DamageInfo none = _enemy.CreateAttackDamage(0f);
            DamageInfo some = _enemy.CreateAttackDamage(0.01f);

            Assert.IsNotNull(none);
            Assert.IsNotNull(some);
            Assert.AreEqual(0.0, none.Amount, 1e-9);
            Assert.GreaterOrEqual(some.Amount, 1.0);
        }
    }
}
