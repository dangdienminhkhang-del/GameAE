using System.Linq;
using Game.PvE.Enemy;
using Game.PvE.Enemy.Behaviors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// P9.8: field vai trò của EnemyDefinition có mặc định an toàn (asset cũ vẫn là Chaser), OnValidate kẹp đúng thứ tự,
    /// và factory chọn đúng behavior theo vai trò.
    /// </summary>
    public class EnemyDefinitionDefaultsTests
    {
        private const string EnemiesFolder = "Assets/_Game/Data/Enemies";

        // Hai quái mới của P9.8 (asset do bước Unity tạo). Mọi quái KHÁC phải giữ role Chaser.
        private static readonly string[] NewRoleEnemyIds = { "enemy.pipe_spitter", "enemy.wire_stalker" };

        private EnemyDefinition _def;

        [SetUp]
        public void Setup()
        {
            _def = ScriptableObject.CreateInstance<EnemyDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_def != null) Object.DestroyImmediate(_def);
        }

        [Test]
        public void Chaser_IsEnumValueZero_SoOldAssetsDefaultCorrectly()
        {
            Assert.AreEqual(0, (int)CombatRole.Chaser);
        }

        [Test]
        public void NewDefinition_DefaultsToChaser()
        {
            Assert.AreEqual(CombatRole.Chaser, _def.Role);
        }

        [Test]
        public void Defaults_SatisfyRangeOrdering()
        {
            Assert.Less(_def.RetreatRange, _def.PreferredRange);
            Assert.LessOrEqual(_def.PreferredRange, _def.ShootRange);
            Assert.LessOrEqual(_def.ShootRange, _def.DetectionRange);
        }

        [Test]
        public void Defaults_ProjectileIsDodgeable()
        {
            float flight = _def.ShootRange / _def.ProjectileSpeed;
            Assert.GreaterOrEqual(flight, Game.PvE.Enemy.Projectile.ProjectileMath.MinDodgeFlightSeconds);
            Assert.GreaterOrEqual(_def.ProjectileLifetime, flight, "đạn phải sống đủ lâu để bay hết tầm bắn");
        }

        [Test]
        public void ClampRoleFields_FixesWrongOrdering()
        {
            var so = new SerializedObject(_def);
            so.FindProperty("detectionRange").floatValue = 6f;
            so.FindProperty("retreatRange").floatValue = 10f;
            so.FindProperty("preferredRange").floatValue = 1f;
            so.FindProperty("shootRange").floatValue = 20f;
            so.ApplyModifiedPropertiesWithoutUndo();

            _def.ClampRoleFields();

            Assert.LessOrEqual(_def.ShootRange, _def.DetectionRange);
            Assert.LessOrEqual(_def.PreferredRange, _def.ShootRange);
            Assert.Less(_def.RetreatRange, _def.PreferredRange);
            Assert.GreaterOrEqual(_def.RetreatRange, 0f);
        }

        [Test]
        public void ClampRoleFields_ClampsNonPositiveNumbers()
        {
            var so = new SerializedObject(_def);
            so.FindProperty("retreatSeconds").floatValue = -1f;
            so.FindProperty("projectileSpeed").floatValue = 0f;
            so.FindProperty("projectileLifetime").floatValue = -5f;
            so.FindProperty("projectileRadius").floatValue = 0f;
            so.FindProperty("projectileDamageScale").floatValue = -2f;
            so.ApplyModifiedPropertiesWithoutUndo();

            _def.ClampRoleFields();

            Assert.Greater(_def.RetreatSeconds, 0f);
            Assert.Greater(_def.ProjectileSpeed, 0f);
            Assert.Greater(_def.ProjectileLifetime, 0f);
            Assert.Greater(_def.ProjectileRadius, 0f);
            Assert.GreaterOrEqual(_def.ProjectileDamageScale, 0f);
        }

        [Test]
        public void ClampRoleFields_NaN_DoesNotPropagate()
        {
            var so = new SerializedObject(_def);
            so.FindProperty("preferredRange").floatValue = float.NaN;
            so.FindProperty("retreatRange").floatValue = float.NaN;
            so.FindProperty("shootRange").floatValue = float.NaN;
            so.ApplyModifiedPropertiesWithoutUndo();

            _def.ClampRoleFields();

            Assert.IsFalse(float.IsNaN(_def.PreferredRange));
            Assert.IsFalse(float.IsNaN(_def.RetreatRange));
            Assert.IsFalse(float.IsNaN(_def.ShootRange));
        }

        [Test]
        public void ExistingEnemyAssets_AllStayChaser()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder });
            Assert.IsNotEmpty(guids, "Không tìm thấy EnemyDefinition nào trong " + EnemiesFolder);

            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || NewRoleEnemyIds.Contains(asset.Id)) continue;
                Assert.AreEqual(CombatRole.Chaser, asset.Role,
                    $"Quái cũ '{asset.Id}' phải giữ role Chaser (hành vi như trước P9.8).");
            }
        }

        [Test]
        public void Factory_ChaserGuardElite_UseChaserBehavior()
        {
            foreach (CombatRole role in new[] { CombatRole.Chaser, CombatRole.Guard, CombatRole.Elite })
            {
                IEnemyBehavior behavior = EnemyBehaviorFactory.Create(ConfigFor(role));
                Assert.IsInstanceOf<ChaserBehavior>(behavior, role.ToString());
            }
        }

        [Test]
        public void Factory_SkirmisherAndRanged_UseTheirOwnBehavior()
        {
            Assert.IsInstanceOf<SkirmisherBehavior>(EnemyBehaviorFactory.Create(ConfigFor(CombatRole.Skirmisher)));
            Assert.IsInstanceOf<RangedBehavior>(EnemyBehaviorFactory.Create(ConfigFor(CombatRole.Ranged)));
        }

        [Test]
        public void RoleConfig_FromNullDefinition_IsChaserDefaults()
        {
            EnemyRoleConfig config = EnemyRoleConfig.From(null);
            Assert.AreEqual(CombatRole.Chaser, config.Role);
            Assert.Less(config.RetreatRange, config.PreferredRange);
            Assert.LessOrEqual(config.PreferredRange, config.ShootRange);
        }

        private static EnemyRoleConfig ConfigFor(CombatRole role)
        {
            return new EnemyRoleConfig(role, 1.1f, 6f, 4.5f, 2.5f, 0.8f, 6f, 8f, 1.5f, 0.25f, 1f);
        }
    }
}
