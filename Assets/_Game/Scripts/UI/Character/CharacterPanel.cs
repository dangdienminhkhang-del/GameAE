using Game.Core.Stats;
using Game.PvE.Player;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Character
{
    /// <summary>
    /// Bảng Nhân vật: cấp, EXP, máu tối đa, tấn công, phòng thủ, chí mạng, tốc độ.
    /// CHỈ hiển thị — số liệu lấy từ PlayerProgress.Stats (đã gồm cộng thêm từ trang bị); dòng chữ do
    /// CharacterSheet (Core) dựng. UI dựng bằng code như InventoryPanel nên scene chỉ cần một object rỗng + một nút mở.
    /// Phím tắt PC: C. Nút Back/Esc đóng theo thứ tự ưu tiên (IBackHandler).
    /// Lưu ý: trong Game.UI.* phải viết UnityEngine.Time đầy đủ vì có namespace Game.UI.Time.
    /// </summary>
    public class CharacterPanel : MonoBehaviour, IBackHandler
    {
        /// <summary>Nằm dưới Túi (30) và trên Quest (20) trong chuỗi Back.</summary>
        public const int Priority = 25;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public void CloseFromBack() { Close(); }

        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private Button openButton;
        [Tooltip("Lấy font từ Text có sẵn (để khớp font các panel khác). Để trống = font mặc định.")]
        [SerializeField] private Text fontSource;

        private const float RefreshInterval = 0.5f;
        private GameObject _root;
        private Text _body;
        private float _refreshTimer;

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame) Toggle();

            if (!IsOpen) return;
            _refreshTimer -= UnityEngine.Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = RefreshInterval; // chỉ số đổi khi mặc/tháo đồ hoặc lên cấp lúc panel đang mở
                Refresh();
            }
        }

        public void Toggle()
        {
            EnsureUi();
            bool open = !_root.activeSelf;
            _root.SetActive(open);
            if (open)
            {
                _refreshTimer = RefreshInterval;
                Refresh();
            }
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void Refresh()
        {
            if (_body == null) return;
            CharacterStats stats = playerProgress != null ? playerProgress.Stats : null;
            if (stats == null)
            {
                _body.text = "Chưa có dữ liệu nhân vật.";
                return;
            }

            string[] lines = CharacterSheet.BuildLines(
                playerProgress.Level, playerProgress.CurrentExp, playerProgress.RequiredExp,
                stats.MaxHealth, stats.Attack, stats.Defense,
                stats.CriticalChance, stats.CriticalDamage, stats.MoveSpeed);
            _body.text = string.Join("\n", lines);
        }

        // ── Dựng UI ────────────────────────────────────────────────────────

        private void EnsureUi()
        {
            if (_root != null) return;

            Font font = fontSource != null ? fontSource.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Lớp phủ tối: chặn chạm xuống game phía sau.
            _root = new GameObject("_characterRoot", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(transform, false);
            Stretch((RectTransform)_root.transform);
            _root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(_root.transform, false);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(1000f, 720f);
            box.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 0.97f);

            Text title = MakeText(box.transform, "Title", font, 56, TextAnchor.MiddleCenter, new Color(0.96f, 0.84f, 0.45f));
            title.fontStyle = FontStyle.Bold;
            title.text = "Nhân vật";
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(30f, -130f);
            titleRt.offsetMax = new Vector2(-30f, -30f);

            _body = MakeText(box.transform, "Body", font, 42, TextAnchor.UpperLeft, Color.white);
            _body.lineSpacing = 1.3f;
            var bodyRt = (RectTransform)_body.transform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(60f, 190f);
            bodyRt.offsetMax = new Vector2(-60f, -150f);

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(box.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.sizeDelta = new Vector2(360f, 100f);
            closeRt.anchoredPosition = new Vector2(0f, 50f);
            closeGo.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            closeGo.GetComponent<Button>().onClick.AddListener(Close);

            Text closeText = MakeText(closeGo.transform, "Text", font, 44, TextAnchor.MiddleCenter, Color.white);
            closeText.text = "Đóng";
            Stretch((RectTransform)closeText.transform);

            _root.SetActive(false);
        }

        private static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
