using System.Collections.Generic;
using System.Reflection;
using Game.Core.Presentation;
using Game.PvE.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// SpriteTint + lớp phủ HitFlash trên SpriteRenderer thật (ADR-029):
    /// - màu nhân của sprite chính chỉ có chết &gt; màu trạng thái; nháy KHÔNG BAO GIỜ đổi nó;
    /// - nháy là child "HitFlash" riêng: trắng, alpha 0.85 → 0, tối đa 0.5 khi báo đòn nguy hiểm;
    /// - ResetAll / chết / OnDisable trả overlay về ẩn hoàn toàn.
    /// Dùng CombatFeelConfig mặc định (CombatFeelConfig.Active) nên các số lấy từ config, không hard-code ở test.
    /// Lưu ý EditMode không tự chạy Awake/LateUpdate/OnDisable của MonoBehaviour: test gọi tay qua reflection khi cần.
    /// </summary>
    public class SpriteTintTests
    {
        // Màu báo đòn theo docs/14 §2: giá trị giữ nguyên.
        private static readonly Color Telegraph = new Color(1f, 0.55f, 0.55f, 1f);
        private static readonly Color Punish = new Color(0.7f, 0.7f, 1f, 1f);
        private static readonly Color Enrage = new Color(1f, 0.5f, 0.5f, 1f);

        private GameObject _go;
        private SpriteRenderer _renderer;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SpriteTintTest");
            _renderer = _go.AddComponent<SpriteRenderer>();
            _renderer.color = Color.white;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (Object o in _created)
            {
                if (o != null) Object.DestroyImmediate(o);
            }
            _created.Clear();
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-4f, "r");
            Assert.AreEqual(expected.g, actual.g, 1e-4f, "g");
            Assert.AreEqual(expected.b, actual.b, 1e-4f, "b");
            Assert.AreEqual(expected.a, actual.a, 1e-4f, "a");
        }

        private Sprite NewSprite()
        {
            var texture = new Texture2D(4, 4);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _created.Add(texture);
            _created.Add(sprite);
            return sprite;
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, "Thiếu phương thức " + method);
            info.Invoke(target, null);
        }

        // ── Nháy không đổi màu state ───────────────────────────────────────────

        [Test]
        public void Flash_OverTelegraph_KeepsTelegraphOnRenderer_AndUsesSeparateOverlay()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.SetStateTint(Telegraph, TintKind.DangerTelegraph);
            AssertColor(Telegraph, _renderer.color);

            tint.Flash();
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            Assert.IsTrue(tint.IsFlashing);
            AssertColor(Telegraph, _renderer.color); // màu báo đòn KHÔNG bị nháy đổi
            Assert.IsTrue(tint.Overlay.IsVisible);
            Assert.AreEqual(Mathf.Min(cfg.FlashStartAlpha, cfg.FlashMaxAlphaUnderDanger), tint.Overlay.Alpha, 1e-4f);

            tint.Tick(10f);
            Assert.IsFalse(tint.IsFlashing);
            AssertColor(Telegraph, _renderer.color);
            Assert.IsFalse(tint.Overlay.IsVisible);
            Assert.AreEqual(0f, tint.Overlay.Alpha, 1e-6f);
        }

        [Test]
        public void StateColor_NeverChanges_DuringTheWholeFlash_ForEveryKind()
        {
            (Color color, TintKind kind)[] states =
            {
                (Color.white, TintKind.Base),
                (Telegraph, TintKind.DangerTelegraph),
                (Punish, TintKind.Window),
                (Enrage, TintKind.Enrage),
            };

            foreach ((Color color, TintKind kind) in states)
            {
                var tint = new SpriteTint(_renderer, null);
                tint.SetStateTint(color, kind);
                tint.Flash();

                float step = CombatFeelConfig.Active.FlashSeconds / 8f;
                for (int i = 0; i < 12; i++)
                {
                    AssertColor(color, _renderer.color);
                    tint.Tick(step);
                }
                AssertColor(color, _renderer.color);
                Assert.IsFalse(tint.Overlay.IsVisible, "hết nháy phải ẩn: " + kind);
            }
        }

        [Test]
        public void StateTintChangedDuringFlash_IsNotLost()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            tint.SetStateTint(Telegraph, TintKind.DangerTelegraph); // ví dụ: quái bắt đầu windup đúng lúc đang nháy
            AssertColor(Telegraph, _renderer.color);
            tint.Tick(10f);
            AssertColor(Telegraph, _renderer.color);
        }

        // ── Lớp phủ: màu, alpha, báo đòn ──────────────────────────────────────

        [Test]
        public void Flash_OnBase_OverlayIsWhiteAtStartAlpha()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            Assert.IsTrue(tint.Overlay.IsVisible);
            Assert.AreEqual(cfg.FlashStartAlpha, tint.Overlay.Alpha, 1e-4f);
            Color rgb = tint.Overlay.FlashRenderer.color;
            Assert.AreEqual(cfg.FlashColor.r, rgb.r, 1e-4f);
            Assert.AreEqual(cfg.FlashColor.g, rgb.g, 1e-4f);
            Assert.AreEqual(cfg.FlashColor.b, rgb.b, 1e-4f);
        }

        [Test]
        public void Config_FlashDefaults_AreWhiteOverlay_085_And05UnderDanger()
        {
            CombatFeelConfig cfg = CombatFeelConfig.Active;
            AssertColor(Color.white, new Color(cfg.FlashColor.r, cfg.FlashColor.g, cfg.FlashColor.b, 1f));
            Assert.AreEqual(0.85f, cfg.FlashStartAlpha, 1e-6f);
            Assert.AreEqual(0.5f, cfg.FlashMaxAlphaUnderDanger, 1e-6f);
            Assert.AreEqual(0.08f, cfg.FlashSeconds, 1e-6f);
        }

        [Test]
        public void OverlayAlpha_DecaysWithRealTimeTicks()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            tint.Tick(cfg.FlashSeconds * 0.5f);
            Assert.IsTrue(tint.IsFlashing);
            Assert.AreEqual(cfg.FlashStartAlpha * 0.5f, tint.Overlay.Alpha, 1e-3f);

            tint.Tick(cfg.FlashSeconds);
            Assert.IsFalse(tint.IsFlashing);
            Assert.IsFalse(tint.Overlay.IsVisible);
            Assert.AreEqual(0f, tint.Overlay.Alpha, 1e-6f);
        }

        [Test]
        public void FlashStopsByRealTimeTicks()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            tint.Tick(CombatFeelConfig.Active.FlashSeconds * 0.5f);
            Assert.IsTrue(tint.IsFlashing);
            tint.Tick(CombatFeelConfig.Active.FlashSeconds);
            Assert.IsFalse(tint.IsFlashing);
        }

        [Test]
        public void WindowAndEnrage_AreNotCapped_OnlyDangerTelegraphIs()
        {
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            var window = new SpriteTint(_renderer, null);
            window.SetStateTint(Punish, TintKind.Window);
            window.Flash();
            Assert.AreEqual(cfg.FlashStartAlpha, window.Overlay.Alpha, 1e-4f, "Punish/Recovery: nháy đầy đủ");

            window.SetStateTint(Enrage, TintKind.Enrage);
            Assert.AreEqual(cfg.FlashStartAlpha, window.Overlay.Alpha, 1e-4f, "Cuồng nộ: nháy đầy đủ");

            window.SetStateTint(Telegraph, TintKind.DangerTelegraph);
            Assert.AreEqual(cfg.FlashMaxAlphaUnderDanger, window.Overlay.Alpha, 1e-4f, "báo đòn nguy hiểm: kẹp ngay");
        }

        [Test]
        public void DangerEndingMidFlash_LiftsTheCapAgain()
        {
            var tint = new SpriteTint(_renderer, null);
            CombatFeelConfig cfg = CombatFeelConfig.Active;
            tint.SetStateTint(Telegraph, TintKind.DangerTelegraph);
            tint.Flash();
            Assert.AreEqual(cfg.FlashMaxAlphaUnderDanger, tint.Overlay.Alpha, 1e-4f);

            tint.SetStateTint(Color.white, TintKind.Base);
            Assert.AreEqual(cfg.FlashStartAlpha, tint.Overlay.Alpha, 1e-4f);
        }

        [Test]
        public void DangerCap_SurvivesResetOfOverlayBetweenFlashes()
        {
            // Sau khi một lần nháy kết thúc, state vẫn là báo đòn → lần nháy sau vẫn phải bị kẹp.
            var tint = new SpriteTint(_renderer, null);
            CombatFeelConfig cfg = CombatFeelConfig.Active;
            tint.SetStateTint(Telegraph, TintKind.DangerTelegraph);

            tint.Flash();
            tint.Tick(10f);
            tint.Flash();

            Assert.AreEqual(cfg.FlashMaxAlphaUnderDanger, tint.Overlay.Alpha, 1e-4f);
        }

        // ── Chết, reset ────────────────────────────────────────────────────────

        [Test]
        public void Dead_BeatsFlash_HidesOverlay_AndFlashIsIgnoredAfterDeath()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            Assert.IsTrue(tint.Overlay.IsVisible);

            var dead = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            tint.SetDead(dead, false);
            AssertColor(dead, _renderer.color);
            Assert.IsFalse(tint.Overlay.IsVisible);
            Assert.AreEqual(0f, tint.Overlay.Alpha, 1e-6f);

            tint.Flash();
            Assert.IsFalse(tint.IsFlashing);
            Assert.IsFalse(tint.Overlay.IsVisible, "đã chết thì không nháy nữa");
            AssertColor(dead, _renderer.color);
        }

        [Test]
        public void DeathFade_GoesFromStartAlphaToTransparent_AndShrinks()
        {
            Vector3 baseScale = new Vector3(2f, 3f, 1f);
            _go.transform.localScale = baseScale;
            var tint = new SpriteTint(_renderer, _go.transform);
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            tint.SetDead(new Color(0.4f, 0.4f, 0.4f, 1f), true);
            Assert.AreEqual(cfg.DeathStartAlpha, _renderer.color.a, 1e-4f);

            tint.SetDeathProgress(1f);
            Assert.AreEqual(0f, _renderer.color.a, 1e-4f);
            Assert.AreEqual(baseScale.x * cfg.DeathEndScale, _go.transform.localScale.x, 1e-4f);
            Assert.AreEqual(baseScale.y * cfg.DeathEndScale, _go.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void BossStyleDeath_DoesNotFadeOrShrink()
        {
            _go.transform.localScale = Vector3.one;
            var tint = new SpriteTint(_renderer, null);
            var dead = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            tint.SetDead(dead, false);
            tint.SetDeathProgress(1f);
            AssertColor(dead, _renderer.color);
        }

        [Test]
        public void ResetAll_RestoresColorAlphaScale_AndClearsFlashDeathAndOverlay()
        {
            Vector3 baseScale = new Vector3(1.5f, 1.5f, 1f);
            _go.transform.localScale = baseScale;
            var tint = new SpriteTint(_renderer, _go.transform);

            tint.Flash();
            tint.SetDead(new Color(0.4f, 0.4f, 0.4f, 1f), true);
            tint.SetDeathProgress(0.9f);
            tint.ResetAll(Color.white);

            Assert.IsFalse(tint.IsDead);
            Assert.IsFalse(tint.IsFlashing);
            Assert.IsFalse(tint.Overlay.IsVisible);
            Assert.AreEqual(0f, tint.Overlay.Alpha, 1e-6f);
            AssertColor(Color.white, _renderer.color);
            Assert.AreEqual(baseScale.x, _go.transform.localScale.x, 1e-4f);
            Assert.AreEqual(baseScale.y, _go.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void ResetAll_MidFlash_OverlayHiddenAndNextFlashStartsClean()
        {
            var tint = new SpriteTint(_renderer, null);
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            tint.SetStateTint(Telegraph, TintKind.DangerTelegraph);
            tint.Flash();
            tint.Tick(cfg.FlashSeconds * 0.25f);
            tint.ResetAll(Color.white);

            Assert.IsFalse(tint.Overlay.IsVisible);
            AssertColor(Color.white, _renderer.color);

            tint.Flash(); // đã reset về Base: nháy đầy đủ, không "nhớ" báo đòn cũ
            Assert.AreEqual(cfg.FlashStartAlpha, tint.Overlay.Alpha, 1e-4f);
        }

        [Test]
        public void ResetAll_WithKind_StoresTheKind()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.ResetAll(Enrage, TintKind.Enrage);
            Assert.AreEqual(TintKind.Enrage, tint.StateKind);
            AssertColor(Enrage, _renderer.color);
        }

        [Test]
        public void OnDisable_MidFlash_HidesOverlay()
        {
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            Assert.IsTrue(tint.Overlay.IsVisible);

            InvokePrivate(tint.Overlay, "OnDisable"); // pool: quái bị trả về giữa lúc nháy

            Assert.IsFalse(tint.Overlay.IsVisible);
            Assert.AreEqual(0f, tint.Overlay.Alpha, 1e-6f);
            Assert.IsFalse(tint.Overlay.IsFlashing);
        }

        // ── Cấu trúc overlay: child, sorting, material, đồng bộ sprite ─────────

        [Test]
        public void Overlay_IsAChildNamedHitFlash_AboveTheMainRenderer()
        {
            _renderer.sortingOrder = 7;
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();

            SpriteRenderer flash = tint.Overlay.FlashRenderer;
            Assert.IsNotNull(flash);
            Assert.AreEqual("HitFlash", flash.gameObject.name);
            Assert.AreSame(_go.transform, flash.transform.parent);
            Assert.AreEqual(Vector3.zero, flash.transform.localPosition);
            Assert.AreEqual(Vector3.one, flash.transform.localScale);
            Assert.AreEqual(_renderer.sortingLayerID, flash.sortingLayerID);
            Assert.AreEqual(8, flash.sortingOrder, "renderer chính + 1");
        }

        [Test]
        public void Overlay_UsesAnUnlitMaterial_SoDayNightLightDoesNotDimIt()
        {
            var tint = new SpriteTint(_renderer, null);
            Material material = tint.Overlay.FlashRenderer.sharedMaterial;

            Assert.IsNotNull(material);
            Assert.IsNotNull(material.shader);
            Assert.Contains(material.shader.name, new[] { "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Sprites/Default" });
        }

        [Test]
        public void Overlay_CopiesSpriteAndFlip_AtFlash_AndFollowsInLateUpdate()
        {
            Sprite first = NewSprite();
            Sprite second = NewSprite();
            _renderer.sprite = first;
            _renderer.flipX = true;
            var tint = new SpriteTint(_renderer, null);

            tint.Flash();
            SpriteRenderer flash = tint.Overlay.FlashRenderer;
            Assert.AreSame(first, flash.sprite);
            Assert.IsTrue(flash.flipX);
            Assert.IsFalse(flash.flipY);

            // Animator đổi sprite, code di chuyển đổi flip giữa lúc đang nháy.
            _renderer.sprite = second;
            _renderer.flipX = false;
            _renderer.flipY = true;
            InvokePrivate(tint.Overlay, "LateUpdate");

            Assert.AreSame(second, flash.sprite);
            Assert.IsFalse(flash.flipX);
            Assert.IsTrue(flash.flipY);
        }

        [Test]
        public void Overlay_LateUpdate_DoesNothingWhenNotFlashing()
        {
            Sprite first = NewSprite();
            Sprite second = NewSprite();
            _renderer.sprite = first;
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            tint.Tick(10f);

            _renderer.sprite = second;
            InvokePrivate(tint.Overlay, "LateUpdate");

            Assert.AreNotSame(second, tint.Overlay.FlashRenderer.sprite, "không nháy thì không chép sprite");
            Assert.IsFalse(tint.Overlay.IsVisible);
        }

        [Test]
        public void SecondSpriteTintOnSameRenderer_ReusesTheSingleOverlayAndChild()
        {
            var a = new SpriteTint(_renderer, null);
            var b = new SpriteTint(_renderer, null);

            Assert.AreSame(a.Overlay, b.Overlay);
            Assert.AreEqual(1, _go.GetComponents<HitFlashOverlay>().Length);

            int hitFlashChildren = 0;
            foreach (Transform child in _go.transform)
            {
                if (child.name == "HitFlash") hitFlashChildren++;
            }
            Assert.AreEqual(1, hitFlashChildren);
        }

        [Test]
        public void NullRenderer_IsSafe()
        {
            var tint = new SpriteTint(null, null);

            Assert.IsNull(tint.Overlay);
            Assert.DoesNotThrow(() =>
            {
                tint.SetStateTint(Telegraph, TintKind.DangerTelegraph);
                tint.Flash();
                tint.Tick(0.05f);
                tint.SetDead(Color.gray, true);
                tint.ResetAll(Color.white);
            });
        }

        // ── Scale punch (giữ như cũ) ──────────────────────────────────────────

        [Test]
        public void Punch_PeaksMidFlash_AndReturnsToBaseScale()
        {
            _go.transform.localScale = Vector3.one;
            var tint = new SpriteTint(_renderer, _go.transform);
            CombatFeelConfig cfg = CombatFeelConfig.Active;

            tint.Flash();
            tint.Tick(cfg.FlashSeconds * 0.5f);
            Assert.AreEqual(cfg.PunchScale, _go.transform.localScale.x, 1e-3f);

            tint.Tick(cfg.FlashSeconds);
            Assert.AreEqual(1f, _go.transform.localScale.x, 1e-4f);
        }

        [Test]
        public void Overlay_FollowsPunchScale_ByBeingAChild_NotByTouchingItsOwnScale()
        {
            _go.transform.localScale = Vector3.one;
            var tint = new SpriteTint(_renderer, _go.transform);
            tint.Flash();
            tint.Tick(CombatFeelConfig.Active.FlashSeconds * 0.5f);

            Assert.AreEqual(Vector3.one, tint.Overlay.FlashRenderer.transform.localScale);
            Assert.Greater(tint.Overlay.FlashRenderer.transform.lossyScale.x, 1f, "con của object đang punch nên to theo");
        }

        [Test]
        public void NullScaleTarget_NeverTouchesTransform()
        {
            _go.transform.localScale = new Vector3(4f, 4f, 1f);
            var tint = new SpriteTint(_renderer, null);
            tint.Flash();
            tint.Tick(CombatFeelConfig.Active.FlashSeconds * 0.5f);
            Assert.AreEqual(4f, _go.transform.localScale.x, 1e-4f);
        }
    }
}
