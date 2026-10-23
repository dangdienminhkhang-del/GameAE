using Game.Core.Presentation;
using Game.PvE.Enemy;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Thanh máu trên đầu quái (docs/05 Phase 9.5): chỉ hiện sau khi quái bị trúng, ẩn khi chết hoặc sau vài giây không bị đánh.
    /// Cập nhật bằng sự kiện EnemyController.HealthChanged, KHÔNG poll máu trong Update. Hai SpriteRenderer con được dựng một lần
    /// ở Awake (không Instantiate/Destroy khi chiến đấu); quái dùng lại từ pool thì thanh máu tự ẩn và reset.
    /// Gắn lên Enemy.prefab (cùng object với EnemyController).
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        private const float BackZOffset = 0f;
        private const float FillZOffset = -0.01f;
        private const int BackSortingBoost = 20;
        private const int FillSortingBoost = 21;

        [SerializeField] private EnemyController enemy;
        [Tooltip("Sprite của quái, dùng để đặt thanh máu ngay trên đầu. Để trống = SpriteRenderer trên cùng object.")]
        [SerializeField] private SpriteRenderer anchorRenderer;
        [Tooltip("Để trống = dùng CombatFeelConfig đang active.")]
        [SerializeField] private CombatFeelConfig config;

        private Transform _root;
        private Transform _fill;
        private bool _hitSinceSpawn;

        private CombatFeelConfig Config => CombatFeelConfig.Resolve(config);

        private void Awake()
        {
            if (enemy == null) TryGetComponent(out enemy);
            if (anchorRenderer == null) TryGetComponent(out anchorRenderer);
            Build();
        }

        private void OnEnable()
        {
            _hitSinceSpawn = false;
            Hide();
            if (enemy != null) enemy.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (enemy != null) enemy.HealthChanged -= HandleHealthChanged;
            CancelInvoke();
        }

        private void HandleHealthChanged(double current, double max, double delta)
        {
            bool dead = current <= 0.0;
            if (delta < 0.0) _hitSinceSpawn = true;
            else if (delta == 0.0 && current >= max) _hitSinceSpawn = false; // quái vừa được hồi sinh/reset

            float hideAfter = Config.EnemyHealthBarHideSeconds;
            if (HealthBarRules.ShouldShowEnemyBar(_hitSinceSpawn, dead, 0f, hideAfter))
            {
                Show(HealthBarRules.Ratio(current, max));
                CancelInvoke(nameof(Hide));
                Invoke(nameof(Hide), hideAfter);
            }
            else
            {
                CancelInvoke(nameof(Hide));
                Hide();
            }
        }

        private void Show(float ratio)
        {
            if (_root == null) return;

            CombatFeelConfig cfg = Config;
            float width = cfg.EnemyHealthBarWidth;
            float height = cfg.EnemyHealthBarHeight;

            // Đặt ngay trên mép sprite; bù scale của quái (đang tan/punch) để thanh máu giữ kích thước thế giới cố định.
            Vector3 parentScale = transform.lossyScale;
            float inverseX = Mathf.Approximately(parentScale.x, 0f) ? 1f : 1f / Mathf.Abs(parentScale.x);
            float inverseY = Mathf.Approximately(parentScale.y, 0f) ? 1f : 1f / Mathf.Abs(parentScale.y);
            _root.localScale = new Vector3(inverseX, inverseY, 1f);

            float top = anchorRenderer != null ? anchorRenderer.bounds.max.y : transform.position.y + 0.5f;
            _root.position = new Vector3(transform.position.x, top + cfg.EnemyHealthBarOffsetY, transform.position.z);

            _root.GetChild(0).localScale = new Vector3(width, height, 1f);
            float fillWidth = width * Mathf.Clamp01(ratio);
            _fill.localScale = new Vector3(fillWidth, height, 1f);
            _fill.localPosition = new Vector3(-width * 0.5f + fillWidth * 0.5f, 0f, FillZOffset);

            _root.gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Build()
        {
            if (_root != null) return;

            CombatFeelConfig cfg = Config;
            Sprite pixel = PixelSprite();

            var rootObject = new GameObject("HealthBar");
            _root = rootObject.transform;
            _root.SetParent(transform, false);

            _fill = CreateBar("Fill", pixel, cfg.HealthBarFillColor, FillSortingBoost, FillZOffset);
            Transform back = CreateBar("Back", pixel, cfg.HealthBarBackColor, BackSortingBoost, BackZOffset);
            back.SetSiblingIndex(0); // Show() dùng GetChild(0) làm nền

            rootObject.SetActive(false);
        }

        private Transform CreateBar(string barName, Sprite pixel, Color color, int sortingBoost, float z)
        {
            var barObject = new GameObject(barName);
            barObject.transform.SetParent(_root, false);
            barObject.transform.localPosition = new Vector3(0f, 0f, z);

            SpriteRenderer sr = barObject.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            if (anchorRenderer != null)
            {
                sr.sortingLayerID = anchorRenderer.sortingLayerID;
                sr.sortingOrder = anchorRenderer.sortingOrder + sortingBoost;
            }
            else
            {
                sr.sortingOrder = sortingBoost;
            }
            return barObject.transform;
        }

        // ── Sprite 1×1 dùng chung ───────────────────────────────────────────

        private static Sprite s_pixel;

        private static Sprite PixelSprite()
        {
            if (s_pixel != null) return s_pixel;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            s_pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            s_pixel.hideFlags = HideFlags.HideAndDontSave;
            return s_pixel;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_pixel = null;
        }
    }
}
