using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Lớp phủ nháy khi trúng đòn (ADR-029, docs/14 §2): một child SpriteRenderer tên "HitFlash" tự dựng lúc chạy
    /// (KHÔNG sửa prefab), cùng sprite với renderer chính, màu trắng, alpha giảm từ <c>flashStartAlpha</c> về 0.
    /// Vì là lớp riêng đè lên, nó không bao giờ làm đổi màu nhân của sprite (màu báo đòn, Punish, Cuồng nộ giữ nguyên).
    ///
    /// Cách dùng: <see cref="SpriteTint"/> tự gọi <see cref="Attach"/> khi được dựng cho một renderer, rồi đẩy thời gian THẬT
    /// vào <see cref="Advance"/> mỗi frame. Component này chỉ có LateUpdate để chép sprite/flipX/flipY từ renderer chính
    /// (sprite có thể đổi bởi Animator, flip đổi bởi code di chuyển) và CHỈ làm việc khi đang nháy.
    /// Quái dùng pool: <see cref="ResetOverlay"/> (từ SpriteTint.ResetAll) và OnDisable đều trả overlay về ẩn hoàn toàn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFlashOverlay : MonoBehaviour
    {
        public const string ChildName = "HitFlash";

        private const string UrpUnlitShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string FallbackUnlitShaderName = "Sprites/Default";
        private const int SortingOrderAboveMain = 1;

        private SpriteRenderer _main;
        private SpriteRenderer _flash;

        private Color _rgb = Color.white;
        private float _startAlpha;
        private float _maxAlphaUnderDanger;
        private float _duration;
        private float _elapsed;
        private bool _active;
        private bool _danger;

        /// <summary>Renderer của lớp phủ ("HitFlash"). null nếu chưa dựng được (chưa có renderer chính).</summary>
        public SpriteRenderer FlashRenderer => _flash;

        /// <summary>Lớp phủ đang hiện (renderer bật).</summary>
        public bool IsVisible => _flash != null && _flash.enabled;

        /// <summary>Alpha hiện tại của lớp phủ; 0 khi đã ẩn.</summary>
        public float Alpha => _flash != null ? _flash.color.a : 0f;

        /// <summary>Đang trong thời gian nháy.</summary>
        public bool IsFlashing => _active;

        /// <summary>
        /// Lấy overlay trên cùng GameObject với <paramref name="main"/> (thêm nếu chưa có) và dựng sẵn child "HitFlash".
        /// Gọi một lần lúc khởi tạo (Awake của chủ sở hữu), không gọi trong lúc chiến đấu.
        /// </summary>
        public static HitFlashOverlay Attach(SpriteRenderer main)
        {
            if (main == null) return null;

            HitFlashOverlay overlay = main.GetComponent<HitFlashOverlay>();
            if (overlay == null) overlay = main.gameObject.AddComponent<HitFlashOverlay>();
            overlay.Bind(main);
            return overlay;
        }

        private void Awake()
        {
            if (_main == null) TryGetComponent(out _main);
            EnsureBuilt();
        }

        private void Bind(SpriteRenderer main)
        {
            _main = main;
            EnsureBuilt();
        }

        /// <summary>Bắt đầu nháy. alpha đầu và thời lượng lấy từ CombatFeelConfig do SpriteTint truyền vào.</summary>
        public void Begin(Color rgb, float startAlpha, float durationSeconds, float maxAlphaUnderDanger, bool danger)
        {
            EnsureBuilt();
            if (_flash == null) return;

            _rgb = rgb;
            _startAlpha = startAlpha;
            _duration = durationSeconds;
            _maxAlphaUnderDanger = maxAlphaUnderDanger;
            _danger = danger;
            _elapsed = 0f;
            _active = HitFlashRules.IsActive(0f, durationSeconds);

            if (!_active)
            {
                HideInternal();
                return;
            }

            SyncFromMain();
            Refresh();
        }

        /// <summary>Đang có báo đòn nguy hiểm hay không (đổi giữa lúc nháy thì alpha được kẹp lại ngay).</summary>
        public void SetDanger(bool danger)
        {
            if (_danger == danger) return;
            _danger = danger;
            if (_active) Refresh();
        }

        /// <summary>Đẩy thời gian THẬT (unscaled) để dừng hình không kéo dài nháy. Không làm gì khi không nháy.</summary>
        public void Advance(float unscaledDeltaSeconds)
        {
            if (!_active) return;

            _elapsed += Mathf.Max(0f, unscaledDeltaSeconds);
            if (!HitFlashRules.IsActive(_elapsed, _duration))
            {
                HideInternal();
                return;
            }
            Refresh();
        }

        /// <summary>Ẩn hoàn toàn, alpha 0, hết nháy, bỏ cờ báo đòn. Dùng khi chết, reset từ pool, vào lại tầng.</summary>
        public void ResetOverlay()
        {
            _danger = false;
            HideInternal();
        }

        private void LateUpdate()
        {
            if (!_active) return;
            SyncFromMain();
            if (_flash != null && _main != null) _flash.enabled = _main.enabled && _flash.color.a > 0f;
        }

        private void OnDisable()
        {
            // Quái bị trả về pool / tầng bị tắt giữa lúc đang nháy: không để lớp phủ kẹt hiện khi dùng lại.
            ResetOverlay();
        }

        private void Refresh()
        {
            if (_flash == null) return;

            float alpha = HitFlashRules.Alpha(_elapsed, _duration, _startAlpha, _danger, _maxAlphaUnderDanger);
            _flash.color = new Color(_rgb.r, _rgb.g, _rgb.b, alpha);
            _flash.enabled = alpha > 0f && (_main == null || _main.enabled);
        }

        private void HideInternal()
        {
            _active = false;
            _elapsed = 0f;
            if (_flash == null) return;

            _flash.enabled = false;
            Color c = _flash.color;
            c.a = 0f;
            _flash.color = c;
        }

        private void SyncFromMain()
        {
            if (_main == null || _flash == null) return;

            _flash.sprite = _main.sprite;
            _flash.flipX = _main.flipX;
            _flash.flipY = _main.flipY;
            _flash.sortingLayerID = _main.sortingLayerID;
            _flash.sortingOrder = _main.sortingOrder + SortingOrderAboveMain;
        }

        /// <summary>Dựng child "HitFlash" một lần (idempotent). Gọi từ Awake và từ Attach/Begin phòng khi Awake chưa chạy (object đang tắt).</summary>
        private void EnsureBuilt()
        {
            if (_flash != null) return;
            if (_main == null) return;

            Transform existing = transform.Find(ChildName);
            GameObject child = existing != null ? existing.gameObject : new GameObject(ChildName);
            Transform t = child.transform;
            t.SetParent(transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            child.layer = gameObject.layer;

            if (!child.TryGetComponent(out _flash)) _flash = child.AddComponent<SpriteRenderer>();

            Material material = FlashMaterial();
            if (material != null) _flash.sharedMaterial = material;

            _flash.sprite = null;
            _flash.color = new Color(_rgb.r, _rgb.g, _rgb.b, 0f);
            _flash.sortingLayerID = _main.sortingLayerID;
            _flash.sortingOrder = _main.sortingOrder + SortingOrderAboveMain;
            _flash.enabled = false;
        }

        // ── Material Unlit dùng chung ──────────────────────────────────────────
        // Unlit để ánh sáng ngày/đêm (Global Light 2D) không làm lớp phủ trắng bị tối đi.

        private static Material s_material;

        private static Material FlashMaterial()
        {
            if (s_material != null) return s_material;

            Shader shader = Shader.Find(UrpUnlitShaderName);
            if (shader == null) shader = Shader.Find(FallbackUnlitShaderName);
            if (shader == null) return null; // dùng material mặc định của SpriteRenderer

            s_material = new Material(shader)
            {
                name = "HitFlash (runtime, unlit)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return s_material;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_material = null;
        }
    }
}
