using Game.Core.Presentation;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Quản lý màu và scale hiệu ứng của MỘT sprite (ADR-029, docs/14 §2):
    /// - Màu NHÂN của renderer chính chỉ có hai lớp: chết &gt; màu trạng thái hiện tại (báo đòn, Punish, Cuồng nộ, gốc).
    /// - Nháy khi trúng là lớp phủ trắng RIÊNG (<see cref="HitFlashOverlay"/>, child "HitFlash"), không chạm vào màu nhân,
    ///   nên màu báo đòn không bao giờ bị đổi hay kẹt. Khi đang báo đòn nguy hiểm, lớp phủ nhạt hơn để màu báo đòn vẫn đọc được.
    /// Mọi nơi muốn đổi màu sprite phải đi qua đây, không ghi spriteRenderer.color trực tiếp.
    /// Lớp thuần C# (không phải MonoBehaviour): chủ sở hữu gọi <see cref="Tick"/> mỗi frame bằng thời gian THẬT.
    /// </summary>
    public sealed class SpriteTint
    {
        private readonly SpriteRenderer _renderer;
        private readonly Transform _scaleTarget;
        private readonly CombatFeelConfig _assignedConfig;
        private readonly Vector3 _baseScale;
        private readonly HitFlashOverlay _overlay;

        private Color _stateTint;
        private TintKind _stateKind;
        private bool _dead;
        private bool _fadeOnDeath;
        private Color _deadTint;
        private float _deathProgress;
        private float _flashRemaining;

        /// <param name="scaleTarget">Transform được scale khi punch/tan. null = không đụng scale (dùng cho người chơi và boss).</param>
        /// <param name="config">null = dùng CombatFeelConfig.Active tại thời điểm cần.</param>
        public SpriteTint(SpriteRenderer renderer, Transform scaleTarget, CombatFeelConfig config = null)
        {
            _renderer = renderer;
            _scaleTarget = scaleTarget;
            _assignedConfig = config;
            _baseScale = scaleTarget != null ? scaleTarget.localScale : Vector3.one;
            _stateTint = renderer != null ? renderer.color : Color.white;
            _stateKind = TintKind.Base;
            _overlay = HitFlashOverlay.Attach(renderer);
        }

        private CombatFeelConfig Config => CombatFeelConfig.Resolve(_assignedConfig);

        public Color StateTint => _stateTint;
        public TintKind StateKind => _stateKind;
        public bool IsFlashing => _flashRemaining > 0f;
        public bool IsDead => _dead;

        /// <summary>Lớp phủ nháy của sprite này (để test và debug). null nếu không có renderer.</summary>
        public HitFlashOverlay Overlay => _overlay;

        /// <summary>
        /// Đặt màu trạng thái (báo đòn, Punish, Cuồng nộ, màu gốc...) kèm LOẠI của nó. Nháy đang chạy không che màu này
        /// (lớp phủ là child riêng); chỉ loại <see cref="TintKind.DangerTelegraph"/> làm lớp phủ nhạt đi.
        /// </summary>
        public void SetStateTint(Color tint, TintKind kind)
        {
            _stateTint = tint;
            _stateKind = kind;
            if (_overlay != null) _overlay.SetDanger(HitFlashRules.IsDanger(kind));
            Apply();
        }

        /// <summary>Bắt đầu nháy ngắn (lớp phủ trắng + punch scale nếu có scaleTarget). Không làm gì khi đã chết.</summary>
        public void Flash()
        {
            if (_dead) return;

            CombatFeelConfig config = Config;
            _flashRemaining = config.FlashSeconds;
            if (_overlay != null)
            {
                _overlay.Begin(
                    config.FlashColor,
                    config.FlashStartAlpha,
                    config.FlashSeconds,
                    config.FlashMaxAlphaUnderDanger,
                    HitFlashRules.IsDanger(_stateKind));
            }
            Apply();
        }

        /// <summary>Vào trạng thái chết. fade = true: sau đó <see cref="SetDeathProgress"/> làm mờ và thu nhỏ (quái thường); false: giữ nguyên (boss).</summary>
        public void SetDead(Color deadTint, bool fade)
        {
            _dead = true;
            _fadeOnDeath = fade;
            _deadTint = deadTint;
            _deathProgress = 0f;
            _flashRemaining = 0f;
            if (_overlay != null) _overlay.ResetOverlay();
            Apply();
        }

        /// <summary>Tiến độ tan 0..1 (chỉ có tác dụng khi đã chết với fade = true).</summary>
        public void SetDeathProgress(float progress)
        {
            if (!_dead) return;
            _deathProgress = Mathf.Clamp01(progress);
            Apply();
        }

        /// <summary>
        /// Đưa về trạng thái sạch (dùng lại từ pool, vào lại tầng): hết chết, hết nháy, lớp phủ ẩn hoàn toàn,
        /// scale gốc, màu trạng thái = <paramref name="stateTint"/> thuộc loại <paramref name="kind"/>.
        /// </summary>
        public void ResetAll(Color stateTint, TintKind kind = TintKind.Base)
        {
            _stateTint = stateTint;
            _stateKind = kind;
            _dead = false;
            _fadeOnDeath = false;
            _deathProgress = 0f;
            _flashRemaining = 0f;
            if (_overlay != null) _overlay.ResetOverlay();
            Apply();
        }

        /// <summary>Gọi mỗi frame với thời gian THẬT để nháy không kéo dài khi dừng hình.</summary>
        public void Tick(float unscaledDeltaSeconds)
        {
            if (_flashRemaining <= 0f) return;

            float delta = Mathf.Max(0f, unscaledDeltaSeconds);
            _flashRemaining -= delta;
            if (_flashRemaining < 0f) _flashRemaining = 0f;
            if (_overlay != null)
            {
                // Hết nháy thì ẩn hẳn (không dựa vào so sánh số thực giữa hai bộ đếm).
                if (_flashRemaining <= 0f) _overlay.ResetOverlay();
                else _overlay.Advance(delta);
            }
            Apply();
        }

        /// <summary>Màu nhân đang áp lên renderer chính (để test và debug). Không bao gồm lớp phủ nháy.</summary>
        public Color Resolve()
        {
            CombatFeelConfig config = Config;
            Color dead = _deadTint;
            if (_dead && _fadeOnDeath)
            {
                dead = new Color(_deadTint.r, _deadTint.g, _deadTint.b, Mathf.Lerp(config.DeathStartAlpha, 0f, _deathProgress));
            }
            return TintResolver.Resolve(new TintState(_stateTint, _dead, dead));
        }

        private void Apply()
        {
            if (_renderer != null) _renderer.color = Resolve();
            if (_scaleTarget != null) _scaleTarget.localScale = _baseScale * ScaleFactor();
        }

        private float ScaleFactor()
        {
            CombatFeelConfig config = Config;

            if (_dead)
            {
                return _fadeOnDeath ? Mathf.Lerp(1f, config.DeathEndScale, _deathProgress) : 1f;
            }

            if (_flashRemaining > 0f && config.PunchScale > 1f)
            {
                // 1 → punch → 1 theo hình tam giác trong thời gian nháy.
                float progress = 1f - Mathf.Clamp01(_flashRemaining / config.FlashSeconds);
                float triangle = 1f - Mathf.Abs(2f * progress - 1f);
                return 1f + (config.PunchScale - 1f) * triangle;
            }

            return 1f;
        }
    }
}
