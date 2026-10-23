using Game.Core.Events;
using Game.Core.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Combat
{
    /// <summary>
    /// Số sát thương nổi (docs/05 Phase 9.5): số người chơi gây ra (trắng vàng, chí mạng cam) và số người chơi nhận (đỏ).
    /// Dùng pool 12 Text dựng sẵn ở Awake — KHÔNG Instantiate/Destroy khi chiến đấu; chuỗi số cache sẵn nên không cấp phát mỗi hit.
    /// Số trùng điểm được dời lên để không chồng chữ. Chỉ NGHE sự kiện, không tự tính sát thương.
    /// Gắn lên Canvas (Screen Space Overlay là đủ): nếu không gán Container, các Text được dựng làm con của chính object này.
    /// </summary>
    public sealed class DamageNumberPresenter : MonoBehaviour
    {
        private const int PoolSize = 12;

        [Tooltip("Vùng chứa các số (nên phủ kín màn hình). Để trống = RectTransform của chính object này.")]
        [SerializeField] private RectTransform container;
        [Tooltip("Camera thế giới để đổi vị trí quái sang màn hình. Để trống = Camera.main.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Font cho Text. Để trống = font dựng sẵn của Unity (LegacyRuntime).")]
        [SerializeField] private Font font;
        [Tooltip("HealthComponent của người chơi — chỉ OnHealthChanged phát từ đây mới hiện số 'nhận sát thương'. Để trống sẽ tự tìm lúc Start.")]
        [SerializeField] private MonoBehaviour healthSource;
        [Tooltip("Để trống = dùng CombatFeelConfig đang active.")]
        [SerializeField] private CombatFeelConfig config;
        [Tooltip("Số xuất hiện cao hơn vị trí mục tiêu chừng này (đơn vị thế giới).")]
        [SerializeField] private float worldYOffset = 0.6f;

        private sealed class Slot
        {
            public RectTransform Rect;
            public Text Label;
            public bool Active;
            public float Age;
            public Vector2 Origin;
            public Color Color;
        }

        private Slot[] _slots;
        private Vector2[] _originBuffer;
        private Canvas _canvas;
        private int _nextVictim;
        private CombatFeelConfig Config => CombatFeelConfig.Resolve(config);

        private void Awake()
        {
            if (container == null) container = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            BuildPool();
        }

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
            GameEvents.OnHealthChanged += HandleHealthChanged;
        }

        private void Start()
        {
            if (healthSource == null)
            {
                healthSource = FindFirstObjectByType<Game.PvE.Player.HealthComponent>();
            }
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            HideAll();
        }

        // ── Sự kiện ─────────────────────────────────────────────────────────

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Damage == null) return;
            if (!(args.Target is Component targetComponent) || targetComponent == null) return;

            Spawn(targetComponent.transform.position,
                DamageNumberRules.ToDisplayValue(args.Damage.Amount),
                DamageNumberRules.Classify(false, args.Damage.IsCritical));
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (args == null || args.Delta >= 0.0) return;
            if (healthSource == null || !ReferenceEquals(sender, healthSource)) return;

            Spawn(healthSource.transform.position,
                DamageNumberRules.ToDisplayValue(-args.Delta),
                DamageNumberRules.Classify(true, false));
        }

        // ── Hiển thị ────────────────────────────────────────────────────────

        private void Spawn(Vector3 worldPosition, int value, DamageNumberKind kind)
        {
            if (_slots == null || container == null) return;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(worldPosition + new Vector3(0f, worldYOffset, 0f));
            if (screen.z < 0f) return; // phía sau camera

            Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, uiCamera, out Vector2 local)) return;

            CombatFeelConfig cfg = Config;

            // Số trùng điểm: dời lên theo số lượng số đang bay ở gần.
            int activeCount = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Active) _originBuffer[activeCount++] = _slots[i].Origin;
            }
            int stackIndex = DamageNumberRules.CountNear(_originBuffer, activeCount, local, cfg.DamageNumberStackRadiusPixels);
            Vector2 origin = local + DamageNumberRules.StackOffset(stackIndex, cfg.DamageNumberStackStepPixels);

            Slot slot = AcquireSlot();
            slot.Active = true;
            slot.Age = 0f;
            slot.Origin = origin;
            slot.Color = cfg.ColorFor(kind);
            slot.Label.text = DamageNumberRules.GetText(value);
            slot.Label.fontSize = cfg.DamageNumberFontSize;
            slot.Label.color = slot.Color;
            slot.Rect.localPosition = new Vector3(origin.x, origin.y, 0f);
            slot.Rect.gameObject.SetActive(true);
        }

        private Slot AcquireSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].Active) return _slots[i];
            }

            // Hết chỗ: dùng lại số cũ nhất (xoay vòng) thay vì tạo thêm.
            Slot victim = _slots[_nextVictim];
            _nextVictim = (_nextVictim + 1) % _slots.Length;
            return victim;
        }

        private void Update()
        {
            if (_slots == null) return;

            CombatFeelConfig cfg = Config;
            // Thời gian THẬT: số vẫn bay mượt kể cả lúc dừng hình.
            float dt = UnityEngine.Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, cfg.DamageNumberSeconds);

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (!slot.Active) continue;

                slot.Age += dt;
                float t = slot.Age / duration;
                if (t >= 1f)
                {
                    slot.Active = false;
                    slot.Rect.gameObject.SetActive(false);
                    continue;
                }

                float eased = 1f - (1f - t) * (1f - t); // bay nhanh rồi chậm dần
                slot.Rect.localPosition = new Vector3(slot.Origin.x, slot.Origin.y + cfg.DamageNumberRisePixels * eased, 0f);

                Color color = slot.Color;
                color.a = Mathf.Clamp01(1f - t * t);
                slot.Label.color = color;
            }
        }

        private void HideAll()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Active = false;
                if (_slots[i].Rect != null) _slots[i].Rect.gameObject.SetActive(false);
            }
        }

        private void BuildPool()
        {
            if (_slots != null || container == null) return;

            Font useFont = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _slots = new Slot[PoolSize];
            _originBuffer = new Vector2[PoolSize];

            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject($"Number_{i:00}", typeof(RectTransform), typeof(Text), typeof(Outline));
                go.transform.SetParent(container, false);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(160f, 60f);

                Text label = go.GetComponent<Text>();
                label.font = useFont;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.raycastTarget = false; // số nổi không được chặn chạm vào nút

                Outline outline = go.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(2f, -2f);

                go.SetActive(false);
                _slots[i] = new Slot { Rect = rect, Label = label };
            }
        }
    }
}
