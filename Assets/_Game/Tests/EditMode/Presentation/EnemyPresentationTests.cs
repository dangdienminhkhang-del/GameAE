using System.Collections.Generic;
using System.Reflection;
using Game.Core.Combat;
using Game.PvE.Enemy;
using Game.PvE.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Quái dùng lại từ pool (EnemySpawner.ReviveSlots / vào lại tầng) phải về trạng thái SẠCH: màu trắng, hết mờ, scale gốc,
    /// và lớp phủ nháy "HitFlash" ẩn hoàn toàn (alpha 0). Nếu quên reset, quái hồi sinh sẽ xám/nhỏ/trong suốt hoặc còn lớp trắng.
    /// </summary>
    public class EnemyPresentationTests
    {
        private static readonly Color Telegraph = new Color(1f, 0.55f, 0.55f, 1f);

        private GameObject _go;
        private SpriteRenderer _renderer;
        private EnemyController _enemy;
        private readonly List<Object> _extra = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("PresentationTestEnemy");
            _go.SetActive(false); // dựng xong mới bật, giống pool
            _renderer = _go.AddComponent<SpriteRenderer>();
            _go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            _enemy = _go.AddComponent<EnemyController>();

            // EditMode có thể chưa chạy Awake: gọi tay một lần để có máy trạng thái và SpriteTint.
            FieldInfo machine = typeof(EnemyController).GetField("_machine", BindingFlags.Instance | BindingFlags.NonPublic);
            if (machine != null && machine.GetValue(_enemy) == null)
            {
                MethodInfo awake = typeof(EnemyController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                awake?.Invoke(_enemy, null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (Object o in _extra)
            {
                if (o != null) Object.DestroyImmediate(o);
            }
            _extra.Clear();
        }

        /// <summary>EnemyDefinition mặc định (40 máu) để đánh KHÔNG chết được; EnemyController có definition null sẽ chết ngay ở đòn 1.</summary>
        private EnemyDefinition GiveDefinition()
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _extra.Add(definition);
            _enemy.SetDefinition(definition);
            return definition;
        }

        private HitFlashOverlay Overlay()
        {
            return _go.GetComponent<HitFlashOverlay>();
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-4f, "r");
            Assert.AreEqual(expected.g, actual.g, 1e-4f, "g");
            Assert.AreEqual(expected.b, actual.b, 1e-4f, "b");
            Assert.AreEqual(expected.a, actual.a, 1e-4f, "a");
        }

        [Test]
        public void ResetForSpawn_AfterDeath_RestoresColorAlphaAndScale()
        {
            // Object dựng ở trạng thái tắt nên OnEnable chưa chạy: phải hồi đầy máu trước, nếu không máu = 0 và TakeDamage bỏ qua.
            _enemy.ResetForSpawn();

            // Sát thương cực lớn để chắc chắn chết dù definition/máu là bao nhiêu.
            _enemy.TakeDamage(new DamageInfo(1e12));
            Assert.IsTrue(_enemy.IsDead, "điều kiện của test: quái đã chết");
            Assert.Less(_renderer.color.r, 0.9f, "khi chết phải ngả xám");

            _enemy.ResetForSpawn();

            Assert.IsFalse(_enemy.IsDead);
            Assert.AreEqual(1f, _renderer.color.r, 1e-4f);
            Assert.AreEqual(1f, _renderer.color.g, 1e-4f);
            Assert.AreEqual(1f, _renderer.color.b, 1e-4f);
            Assert.AreEqual(1f, _renderer.color.a, 1e-4f, "hết mờ");
            Assert.AreEqual(1.25f, _go.transform.localScale.x, 1e-4f, "scale về gốc");
            Assert.AreEqual(1.25f, _go.transform.localScale.y, 1e-4f);
        }

        // ── Lớp phủ HitFlash và pool ────────────────────────────────────────────

        [Test]
        public void ResetForSpawn_AfterNonLethalHit_ReturnsOverlayToHidden_AlphaZero()
        {
            GiveDefinition();
            _enemy.ResetForSpawn();

            _enemy.TakeDamage(new DamageInfo(1.0));
            Assert.IsFalse(_enemy.IsDead, "điều kiện của test: đòn không giết");
            HitFlashOverlay overlay = Overlay();
            Assert.IsNotNull(overlay, "SpriteTint tự dựng overlay, không cần sửa prefab");
            Assert.IsTrue(overlay.IsVisible, "trúng đòn thì có lớp phủ nháy");
            Assert.Greater(overlay.Alpha, 0f);

            _enemy.ResetForSpawn(); // quái được dùng lại từ pool giữa lúc đang nháy

            Assert.IsFalse(overlay.IsVisible);
            Assert.AreEqual(0f, overlay.Alpha, 1e-6f);
            Assert.IsFalse(overlay.IsFlashing);
            AssertColor(Color.white, _renderer.color);
            Assert.AreEqual(1.25f, _go.transform.localScale.x, 1e-4f, "scale punch cũng được trả về");
        }

        [Test]
        public void ResetForSpawn_AfterDeathDuringFlash_OverlayStaysHidden_AndNextLifeStartsClean()
        {
            GiveDefinition();
            _enemy.ResetForSpawn();

            _enemy.TakeDamage(new DamageInfo(1.0));
            HitFlashOverlay overlay = Overlay();
            Assert.IsTrue(overlay.IsVisible);

            _enemy.TakeDamage(new DamageInfo(1e12)); // chết khi đang nháy
            Assert.IsTrue(_enemy.IsDead);
            Assert.IsFalse(overlay.IsVisible, "chết thì lớp phủ ẩn (chết thắng nháy)");

            _enemy.ResetForSpawn();

            Assert.IsFalse(overlay.IsVisible);
            Assert.AreEqual(0f, overlay.Alpha, 1e-6f);
            AssertColor(Color.white, _renderer.color);
        }

        [Test]
        public void SpawnedEnemy_HasNoVisibleOverlayUntilHit()
        {
            GiveDefinition();
            _enemy.ResetForSpawn();

            HitFlashOverlay overlay = Overlay();
            Assert.IsNotNull(overlay);
            Assert.IsFalse(overlay.IsVisible);
            Assert.AreEqual(0f, overlay.Alpha, 1e-6f);
            Assert.IsNotNull(_go.transform.Find("HitFlash"));
        }

        // ── Màu báo đòn và nháy ────────────────────────────────────────────────

        [Test]
        public void BeginWindup_ShowsTelegraphColor_WithoutAnyOverlay()
        {
            GiveDefinition();
            _enemy.ResetForSpawn();

            _enemy.BeginWindup();

            AssertColor(Telegraph, _renderer.color);
            Assert.IsFalse(Overlay().IsVisible);
        }

        [Test]
        public void HitDuringWindup_StillEndsTheWindupAsBefore_AndFlashIsTheFullWhiteOverlay()
        {
            // Luật cũ (KHÔNG đổi): TakeDamage luôn gọi EndWindup trước nên đòn trúng hủy báo đòn của quái.
            // Vì vậy lúc nháy quái đã về Base và lớp phủ đậm đầy đủ; màu hồng không bị "nháy đổi" mà bị chính luật hủy windup xóa.
            GiveDefinition();
            _enemy.ResetForSpawn();
            _enemy.BeginWindup();
            AssertColor(Telegraph, _renderer.color);

            _enemy.TakeDamage(new DamageInfo(1.0));

            Assert.IsFalse(_enemy.IsWindingUp);
            AssertColor(Color.white, _renderer.color);
            Assert.AreEqual(Game.Core.Presentation.CombatFeelConfig.Active.FlashStartAlpha, Overlay().Alpha, 1e-4f);
        }

        // ── Sự kiện máu (giữ từ P9.5) ──────────────────────────────────────────

        [Test]
        public void TakeDamage_RaisesHealthChangedWithNegativeDelta()
        {
            _enemy.ResetForSpawn();
            double lastDelta = 0.0;
            int calls = 0;
            _enemy.HealthChanged += (current, max, delta) => { lastDelta = delta; calls++; };

            _enemy.TakeDamage(new DamageInfo(1.0));

            Assert.GreaterOrEqual(calls, 1);
            Assert.Less(lastDelta, 0.0, "bị trúng thì delta âm để thanh máu biết hiện");
        }

        [Test]
        public void ResetForSpawn_RaisesHealthChangedWithZeroDelta_SoBarCanHide()
        {
            double lastDelta = -1.0;
            double lastCurrent = 0.0;
            double lastMax = 0.0;
            _enemy.HealthChanged += (current, max, delta) => { lastCurrent = current; lastMax = max; lastDelta = delta; };

            _enemy.ResetForSpawn();

            Assert.AreEqual(0.0, lastDelta);
            Assert.AreEqual(lastMax, lastCurrent, 1e-9, "reset = đầy máu");
        }
    }
}
