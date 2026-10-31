using Game.Core.Combat;
using Game.Core.Presentation;
using Game.PvE.Enemy;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Thanh máu trên đầu quái (docs/05 Phase 9.5 & SAO Cursor System):
    /// - Chỉ hiện sau khi quái bị trúng (hoặc khi được kích hoạt), ẩn khi chết hoặc sau vài giây không bị đánh.
    /// - Hiển thị chấm/con trỏ cursor và tên quái đổi màu theo chênh lệch cấp độ (levelQuái - levelNgườiChơi):
    ///   + Ngang cấp (±4): Vàng
    ///   + Nguy hiểm (+5 trở lên): Đỏ
    ///   + Yếu (-5 trở xuống): Nhạt / Xám
    /// - Tái sử dụng SpriteRenderer và TextMesh tạo 1 lần ở Awake, không Instantiate/Destroy trong combat.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        private const float BackZOffset = 0f;
        private const float FillZOffset = -0.01f;
        private const int BackSortingBoost = 20;
        private const int FillSortingBoost = 21;
        private const int TextSortingBoost = 22;

        [SerializeField] private EnemyController enemy;
        [Tooltip("Sprite của quái, dùng để đặt thanh máu ngay trên đầu. Để trống = SpriteRenderer trên cùng object.")]
        [SerializeField] private SpriteRenderer anchorRenderer;
        [Tooltip("Để trống = dùng CombatFeelConfig đang active.")]
        [SerializeField] private CombatFeelConfig config;

        private Transform _root;
        private Transform _fill;
        private SpriteRenderer _cursorRenderer;
        private TextMesh _nameMesh;
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

        private void Update()
        {
            // Nếu quái đã bị đánh trúng thì để HandleHealthChanged quản lý tự động ẩn/hiện
            if (_hitSinceSpawn) return;

            // Kỹ năng Searching (SAO): Tự động phát hiện quái và hiện con trỏ / cấp độ trong tầm Dò Tìm
            float searchingRadius = Skills.SearchingHidingController.CurrentPlayerSearchingRadius;
            if (searchingRadius > 0f)
            {
                Vector2 playerPos = Skills.SearchingHidingController.CurrentPlayerPosition;
                float sqrDist = (playerPos - (Vector2)transform.position).sqrMagnitude;
                if (sqrDist <= searchingRadius * searchingRadius && (enemy == null || !enemy.IsDead))
                {
                    if (_root == null || !_root.gameObject.activeSelf)
                    {
                        Show(1.0f);
                    }
                    return;
                }
            }

            // Nếu ra ngoài tầm dò tìm và chưa từng bị đánh -> ẩn
            if (_root != null && _root.gameObject.activeSelf && !_hitSinceSpawn)
            {
                Hide();
            }
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

        public void Show(float ratio)
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

            // Cập nhật Cursor và Tên theo chênh lệch cấp độ SAO
            UpdateCursorAndName(width, height);

            _root.gameObject.SetActive(true);
        }

        private void UpdateCursorAndName(float width, float height)
        {
            int playerLevel = ResolvePlayerLevel();
            int enemyLevel = enemy != null ? enemy.Level : 1;
            CursorThreatLevel threat = EnemyCursorRules.EvaluateEnemyThreat(enemyLevel, playerLevel);
            Color cursorColor = EnemyCursorRules.GetCursorColor(threat);

            // Cập nhật chấm cursor
            if (_cursorRenderer != null)
            {
                _cursorRenderer.color = cursorColor;
                _cursorRenderer.transform.localPosition = new Vector3(-width * 0.5f - 0.08f, 0f, FillZOffset);
                _cursorRenderer.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            }

            // Cập nhật nhãn tên + level
            if (_nameMesh != null)
            {
                string displayName = enemy != null && enemy.Definition != null ? enemy.Definition.DisplayName : "Enemy";
                _nameMesh.text = $"Lv.{enemyLevel} {displayName}";
                _nameMesh.color = cursorColor;
                _nameMesh.transform.localPosition = new Vector3(0f, height * 0.5f + 0.09f, FillZOffset);
            }
        }

        private int ResolvePlayerLevel()
        {
            if (enemy != null && enemy.Target != null)
            {
                var pc = enemy.Target.GetComponent<Player.PlayerController>();
                if (pc != null && pc.Stats != null) return pc.Stats.Level;
            }
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<Player.PlayerController>();
                if (pc != null && pc.Stats != null) return pc.Stats.Level;
            }
            return 1;
        }

        public void Hide()
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

            // Chấm Cursor chỉ báo mức độ nguy hiểm theo cấp độ
            var cursorObj = new GameObject("CursorDot");
            cursorObj.transform.SetParent(_root, false);
            _cursorRenderer = cursorObj.AddComponent<SpriteRenderer>();
            _cursorRenderer.sprite = pixel;
            SetSortingOrder(_cursorRenderer, FillSortingBoost);

            // Tên quái và Level
            Font font = Resources.Load<Font>("UISkin/Fonts/BeVietnamPro-SemiBold")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var nameObj = new GameObject("EnemyName");
            nameObj.transform.SetParent(_root, false);
            _nameMesh = nameObj.AddComponent<TextMesh>();
            if (font != null)
            {
                _nameMesh.font = font;
                var mr = nameObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = font.material;
                    if (anchorRenderer != null)
                    {
                        mr.sortingLayerID = anchorRenderer.sortingLayerID;
                        mr.sortingOrder = anchorRenderer.sortingOrder + TextSortingBoost;
                    }
                    else
                    {
                        mr.sortingOrder = TextSortingBoost;
                    }
                }
            }
            _nameMesh.fontSize = 28;
            _nameMesh.characterSize = 0.024f;
            _nameMesh.alignment = TextAlignment.Center;
            _nameMesh.anchor = TextAnchor.MiddleCenter;

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
            SetSortingOrder(sr, sortingBoost);
            return barObject.transform;
        }

        private void SetSortingOrder(SpriteRenderer sr, int sortingBoost)
        {
            if (anchorRenderer != null)
            {
                sr.sortingLayerID = anchorRenderer.sortingLayerID;
                sr.sortingOrder = anchorRenderer.sortingOrder + sortingBoost;
            }
            else
            {
                sr.sortingOrder = sortingBoost;
            }
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
