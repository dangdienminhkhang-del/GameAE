using Game.Core.Combat;
using Game.PvE.Environment;
using Game.PvE.Floor;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class ThreeRealmsFeaturesTests
    {
        private GameObject _playerGo;
        private PlayerController _playerController;
        private HealthComponent _health;
        private AdrenalineController _adrenaline;

        [SetUp]
        public void SetUp()
        {
            _playerGo = new GameObject("TestPlayer");
            _playerGo.tag = "Player";
            _playerController = _playerGo.AddComponent<PlayerController>();
            _health = _playerGo.AddComponent<HealthComponent>();
            _adrenaline = _playerGo.AddComponent<AdrenalineController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            PlayerAttack.ExternalDamageMultiplier = 1.0f;
        }

        [Test]
        public void PlayerController_SpeedMultiplierModifiesSpeed()
        {
            _playerController.SpeedMultiplier = 0.5f;
            Assert.AreEqual(0.5f, _playerController.SpeedMultiplier, 0.001f);

            _playerController.SpeedMultiplier = 1.25f;
            Assert.AreEqual(1.25f, _playerController.SpeedMultiplier, 0.001f);

            _playerController.SpeedMultiplier = -5f; // clamped to 0.1
            Assert.AreEqual(0.1f, _playerController.SpeedMultiplier, 0.001f);
        }

        [Test]
        public void EnvironmentalHazard_EnumTypesDefined()
        {
            Assert.AreEqual(0, (int)HazardType.MagmaLava);
            Assert.AreEqual(1, (int)HazardType.ThornBramble);
            Assert.AreEqual(2, (int)HazardType.VoidAbyss);
        }

        [Test]
        public void MoltenForgeAnvil_AppliesExternalDamageMultiplier()
        {
            var anvilGo = new GameObject("TestAnvil");
            anvilGo.AddComponent<CircleCollider2D>();
            var anvil = anvilGo.AddComponent<MoltenForgeAnvil>();

            // External multiplier starts at 1.0
            Assert.AreEqual(1.0f, PlayerAttack.ExternalDamageMultiplier, 0.001f);

            // Trigger enter with player
            var col = _playerGo.AddComponent<CircleCollider2D>();
            typeof(MoltenForgeAnvil).GetMethod("OnTriggerEnter2D", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(anvil, new object[] { col });

            Assert.AreEqual(1.35f, PlayerAttack.ExternalDamageMultiplier, 0.001f);

            Object.DestroyImmediate(anvilGo);
        }

        [Test]
        public void DarkCastleAltar_GrantsAdrenalineAndInvulnerability()
        {
            var altarGo = new GameObject("TestAltar");
            altarGo.AddComponent<CircleCollider2D>();
            var altar = altarGo.AddComponent<DarkCastleAltar>();

            var col = _playerGo.AddComponent<CircleCollider2D>();
            float adBefore = _adrenaline.CurrentAdrenaline;

            typeof(DarkCastleAltar).GetMethod("OnTriggerEnter2D", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(altar, new object[] { col });

            Assert.IsTrue(_health.IsInvulnerable);
            Assert.AreEqual(adBefore + 50.0f, _adrenaline.CurrentAdrenaline, 0.001f);

            Object.DestroyImmediate(altarGo);
        }

        [Test]
        public void RealmTheme_EnumHasThreeRealms()
        {
            Assert.AreEqual(1, (int)RealmTheme.AncientForest);
            Assert.AreEqual(2, (int)RealmTheme.LavaMines);
            Assert.AreEqual(3, (int)RealmTheme.DarkCastle);
        }
    }
}
