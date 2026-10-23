using Game.Core.Facing;
using Game.PvE.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>PlayerController giữ hướng nhìn 8 hướng nhưng Facing (±1) và flipX vẫn như cũ (docs/13 Wave 9.5a).</summary>
    public class PlayerFacingTests
    {
        private GameObject _go;
        private PlayerController _player;
        private SpriteRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PlayerFacingTest");
            _go.AddComponent<Rigidbody2D>();
            _renderer = _go.AddComponent<SpriteRenderer>();
            _player = _go.AddComponent<PlayerController>();
            typeof(PlayerController).GetField("spriteRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_player, _renderer);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Default_IsEast_WithLegacyFacingPlusOne()
        {
            Assert.AreEqual(new Vector2(1f, 0f), _player.FacingDirection);
            Assert.AreEqual(Direction8.East, _player.Facing8);
            Assert.AreEqual(1, _player.Facing);
        }

        [Test]
        public void SetFacingDirection_North_ChangesDirectionButKeepsLegacyFacingAndFlip()
        {
            _player.SetFacingDirection(new Vector2(-0.1f, 1f)); // lệch nhẹ về trái nhưng làm tròn thành Bắc

            Assert.AreEqual(Direction8.North, _player.Facing8);
            Assert.AreEqual(1, _player.Facing, "Bắc không có thành phần ngang: Facing giữ nguyên");
            Assert.IsFalse(_renderer.flipX);
        }

        [Test]
        public void SetFacingDirection_West_FlipsSpriteAndFacing()
        {
            _player.SetFacingDirection(new Vector2(-1f, 0f));

            Assert.AreEqual(Direction8.West, _player.Facing8);
            Assert.AreEqual(-1, _player.Facing);
            Assert.IsTrue(_renderer.flipX);
        }

        [Test]
        public void SetFacingDirection_SouthWest_FlipsSprite()
        {
            _player.SetFacingDirection(new Vector2(-1f, -1f));

            Assert.AreEqual(Direction8.SouthWest, _player.Facing8);
            Assert.AreEqual(-1, _player.Facing);
            Assert.IsTrue(_renderer.flipX);
        }

        [Test]
        public void SetFacingDirection_ZeroOrNaN_IsIgnored()
        {
            _player.SetFacingDirection(new Vector2(0f, 1f));
            _player.SetFacingDirection(Vector2.zero);
            _player.SetFacingDirection(new Vector2(float.NaN, 0f));

            Assert.AreEqual(Direction8.North, _player.Facing8);
        }

        [Test]
        public void FacingDirection_IsAlwaysUnitLength()
        {
            foreach (Vector2 v in new[] { new Vector2(3f, 1f), new Vector2(-0.2f, -5f), new Vector2(1f, 1f) })
            {
                _player.SetFacingDirection(v);
                Assert.AreEqual(1f, _player.FacingDirection.magnitude, 1e-4f, v.ToString());
            }
        }
    }
}
