using System.Collections.Generic;
using Game.Core.World;
using Game.PvE.Exploration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Map
{
    /// <summary>
    /// Giao diện "Sổ Hải Đồ" kiểu SAO, 3 trạng thái (xem <see cref="MapPanelFlow"/>):
    /// icon bản đồ → bấm ra minimap → bấm minimap ra bản đồ toàn màn hình (hoặc phím M).
    /// Trên bản đồ lớn: người chơi, Đèn Hiệu; chạm Đèn Hiệu đã thắp để dịch chuyển nhanh.
    /// Tự dựng UI bằng code (không sửa scene host). Đặt trên Content của tầng: UI chỉ tồn tại khi đang ở tầng có bản đồ.
    /// Icon lấy từ Resources "UISkin/Icon_Map" (thả PNG vào Assets/_Game/Resources/UISkin); chưa có thì vẽ icon tạm.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapHudController : MonoBehaviour
    {
        private const string IconResourcePath = "UISkin/Icon_Map";

        [Header("Vị trí (tính từ góc trên-trái màn hình, đơn vị canvas 1920×1080)")]
        [SerializeField] private Vector2 minimapOffset = new Vector2(24f, -300f);
        [SerializeField] private float iconSize = 96f;
        [SerializeField] private float minimapSize = 210f;
        [SerializeField, Min(16)] private int minimapWindowCells = 56;
        [Tooltip("Để trống = tự nạp Resources/UISkin/Icon_Map; chưa có file thì dùng icon vẽ tạm.")]
        [SerializeField] private Sprite iconSprite;

        [Header("Tương tác")]
        [Tooltip("Chạm vào vùng trống (dưới ngón tay không có UI nào) khi đang có minimap thì thu về icon. Bản đồ toàn màn hình luôn thu khi chạm vùng trống.")]
        [SerializeField] private bool collapseMiniOnOutsideTap = true;

        private static readonly Color32 Unrevealed = new Color32(5, 7, 9, 255);
        private static readonly Color32 VoidColor = new Color32(9, 13, 17, 255);
        private static readonly Color32 WallColor = new Color32(34, 52, 64, 255);
        private static readonly Color32 FloorColorA = new Color32(96, 138, 158, 255);
        private static readonly Color32 FloorColorB = new Color32(86, 126, 146, 255);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
        private static readonly Color FrameColor = new Color(0.07f, 0.13f, 0.17f, 0.94f);

        /// <summary>Nhớ Collapsed/Mini qua các lần đổi tầng trong phiên.</summary>
        private static MapPanelState s_remembered = MapPanelState.Collapsed;

        private MapPanelState _state = MapPanelState.Collapsed;
        private Font _font;
        private Canvas _canvas;
        private RawImage _mini, _full;
        private Image _iconImage;
        private RectTransform _iconRect, _miniRect, _fullRect, _playerMarkerFull, _channelFill;
        private GameObject _fullPanel, _channelRoot;
        private Text _percent;
        private readonly Dictionary<BeaconWaypoint, RectTransform> _beaconMarkers = new Dictionary<BeaconWaypoint, RectTransform>();

        private Texture2D _tex;
        private Color32[] _pixels;
        private byte[] _kind;
        private int _tw, _th;
        private Rect _bounds;
        private int _lastVersion = -1;
        private float _nextRebuild;
        private Transform _player;

        public MapPanelState State => _state;

        /// <summary>Bộ điều khiển đang hoạt động (UI bản đồ chỉ tồn tại ở tầng có bản đồ).</summary>
        public static MapHudController Active { get; private set; }

        public bool CollapseOnOutsideTap => collapseMiniOnOutsideTap;

        /// <summary>Object gốc của minimap (cho OutsideTapCloser xác định vùng bên trong).</summary>
        public GameObject MinimapRoot => _miniRect != null ? _miniRect.gameObject : null;

        /// <summary>Chạm ra ngoài khi minimap đang mở → thu về icon.</summary>
        public void CollapseFromOutsideTap()
        {
            if (_miniRect == null) return;
            SetState(MapPanelFlow.OnOutsideTapPressed(_state));
        }

        private void OnEnable()
        {
            Active = this;
            if (_canvas == null) BuildUi();
            _canvas.gameObject.SetActive(true);
            SetState(MapPanelFlow.Restore(s_remembered));
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            if (_fullPanel != null) _fullPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }

        // ───────────── trạng thái ─────────────

        private void SetState(MapPanelState state)
        {
            _state = state;
            if (state != MapPanelState.Full) s_remembered = state;

            _miniRect.gameObject.SetActive(state == MapPanelState.Mini);
            bool full = state == MapPanelState.Full;
            if (_fullPanel.activeSelf != full)
            {
                _fullPanel.SetActive(full);
                if (full) RebuildBeaconMarkers();
            }
            // Icon sáng khi minimap đang mở, mờ nhẹ khi thu gọn.
            if (iconSprite != null) _iconImage.color = state == MapPanelState.Mini ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        private void OnIconClicked() { SetState(MapPanelFlow.OnIconPressed(_state)); }
        private void OnMiniClicked() { SetState(MapPanelFlow.OnMiniPressed(_state)); }
        private void OnEmptyAreaClicked() { SetState(MapPanelFlow.OnEmptyAreaPressed(_state)); }

        // ───────────── vòng cập nhật ─────────────

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame) SetState(MapPanelFlow.OnMapKeyPressed(_state));

            if (_tex == null) TryBuildTexture();
            var explorer = MapExplorer.Active;
            UpdateChannelBar();
            if (_tex == null || explorer == null || explorer.Grid == null) return;

            // Chỉ vẽ lại texture khi đang nhìn thấy bản đồ (đỡ tốn khi chỉ còn icon).
            if (_state != MapPanelState.Collapsed)
            {
                var grid = explorer.Grid;
                if (grid.Version != _lastVersion && UnityEngine.Time.unscaledTime >= _nextRebuild)
                {
                    Compose(grid);
                    _lastVersion = grid.Version;
                    _nextRebuild = UnityEngine.Time.unscaledTime + 0.5f;
                }
                if (_player == null) _player = MapExplorer.FindPlayer();
                if (_state == MapPanelState.Mini) UpdateMinimap();
                else UpdateFull(grid);
            }
        }

        // ───────────── dựng texture ─────────────

        private void TryBuildTexture()
        {
            var info = FloorMapInfo.Current;
            if (info == null || (info.FloorTilemap == null && !info.GroundIsOpen)) return;

            _bounds = info.WorldBounds;
            _tw = Mathf.Max(1, Mathf.CeilToInt(_bounds.width));
            _th = Mathf.Max(1, Mathf.CeilToInt(_bounds.height));
            _kind = new byte[_tw * _th];
            var floor = info.FloorTilemap;
            var walls = info.WallsTilemap;
            int x0 = Mathf.RoundToInt(_bounds.xMin), y0 = Mathf.RoundToInt(_bounds.yMin);
            for (int y = 0; y < _th; y++)
            {
                for (int x = 0; x < _tw; x++)
                {
                    var cell = new Vector3Int(x0 + x, y0 + y, 0);
                    if (info.GroundIsOpen) _kind[y * _tw + x] = (byte)((walls != null && walls.HasTile(cell)) ? 1 : 2);
                    else if (floor != null && floor.HasTile(cell)) _kind[y * _tw + x] = 2;
                    else if (walls != null && walls.HasTile(cell)) _kind[y * _tw + x] = 1;
                }
            }

            _pixels = new Color32[_tw * _th];
            _tex = new Texture2D(_tw, _th, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "MapTexture"
            };
            _mini.texture = _tex;
            _full.texture = _tex;
            _lastVersion = -1;
        }

        private void Compose(MapRevealGrid grid)
        {
            for (int y = 0; y < _th; y++)
            {
                int by = y / MapRevealGrid.BlockSize;
                for (int x = 0; x < _tw; x++)
                {
                    int i = y * _tw + x;
                    if (!grid.IsRevealed(x / MapRevealGrid.BlockSize, by)) { _pixels[i] = Unrevealed; continue; }
                    byte k = _kind[i];
                    _pixels[i] = k == 2 ? (((x + y) & 1) == 0 ? FloorColorA : FloorColorB) : k == 1 ? WallColor : VoidColor;
                }
            }
            _tex.SetPixels32(_pixels);
            _tex.Apply(false);
        }

        // ───────────── cập nhật hiển thị ─────────────

        private void UpdateMinimap()
        {
            if (_player == null) return;
            float cx = (_player.position.x - _bounds.xMin) / _tw;
            float cy = (_player.position.y - _bounds.yMin) / _th;
            float w = minimapWindowCells / (float)_tw, h = minimapWindowCells / (float)_th;
            _mini.uvRect = new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
        }

        private Vector2 WorldToFull(Vector2 world)
        {
            var size = _fullRect.rect.size;
            return new Vector2(
                ((world.x - _bounds.xMin) / _tw - 0.5f) * size.x,
                ((world.y - _bounds.yMin) / _th - 0.5f) * size.y);
        }

        private void UpdateFull(MapRevealGrid grid)
        {
            if (_player != null) _playerMarkerFull.anchoredPosition = WorldToFull(_player.position);
            _percent.text = $"Đã khám phá: {Mathf.RoundToInt(grid.Fraction * 100f)}%";
            foreach (var kv in _beaconMarkers)
                if (kv.Key != null) kv.Value.GetComponent<Image>().color = kv.Key.IsLit ? Gold : new Color(0.45f, 0.45f, 0.5f);
        }

        private void UpdateChannelBar()
        {
            var ft = FastTravelController.Instance;
            bool show = ft != null && ft.IsChanneling;
            if (_channelRoot.activeSelf != show) _channelRoot.SetActive(show);
            if (show) _channelFill.anchorMax = new Vector2(Mathf.Clamp01(ft.ChannelProgress), 1f);
        }

        private void RebuildBeaconMarkers()
        {
            foreach (var kv in _beaconMarkers) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _beaconMarkers.Clear();
            var info = FloorMapInfo.Current;
            foreach (var b in BeaconWaypoint.All)
            {
                if (b == null || info == null || b.FloorId != info.FloorId) continue;
                var rt = NewRect("Beacon_" + b.BeaconId, _fullRect, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
                rt.anchoredPosition = WorldToFull(b.transform.position);
                rt.localRotation = Quaternion.Euler(0, 0, 45f);
                rt.gameObject.AddComponent<Image>();
                var btn = rt.gameObject.AddComponent<Button>();
                var captured = b;
                btn.onClick.AddListener(() => OnBeaconClicked(captured));
                var label = NewText("Label", rt, b.DisplayName, 18, Color.white, TextAnchor.UpperCenter);
                label.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f);
                label.rectTransform.anchoredPosition = new Vector2(30f, -32f);
                label.rectTransform.sizeDelta = new Vector2(260f, 28f);
                _beaconMarkers[b] = rt;
            }
        }

        private void OnBeaconClicked(BeaconWaypoint b)
        {
            var ft = FastTravelController.Instance;
            if (ft == null) return;
            if (ft.Request(b) == FastTravelRules.Result.Ok) SetState(MapPanelFlow.OnTravelStarted(_state));
        }

        // ───────────── dựng UI ─────────────

        private void BuildUi()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (iconSprite == null) iconSprite = Resources.Load<Sprite>(IconResourcePath);
            if (FindFirstObjectByType<EventSystem>() == null)
                Debug.LogWarning("[MapHud] Không có EventSystem trong scene: nút bản đồ sẽ không bấm được.", this);

            var go = new GameObject("MapHudCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200; // trên mọi HUD khác: bản đồ toàn màn hình phải che hết
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)go.transform;

            BuildMinimap(root);
            BuildIcon(root);   // sau minimap để icon luôn nằm trên cùng, bấm được
            BuildFullMap(root);
            BuildChannelBar(root);
        }

        private void BuildIcon(RectTransform root)
        {
            _iconRect = NewRect("MapIcon", root, new Vector2(0f, 1f), new Vector2(iconSize, iconSize));
            _iconRect.pivot = new Vector2(0f, 1f);
            _iconRect.anchoredPosition = minimapOffset;
            _iconImage = _iconRect.gameObject.AddComponent<Image>();
            var btn = _iconRect.gameObject.AddComponent<Button>();
            btn.targetGraphic = _iconImage;
            btn.onClick.AddListener(OnIconClicked);

            if (iconSprite != null)
            {
                _iconImage.sprite = iconSprite;
                _iconImage.preserveAspect = true;
                _iconImage.color = Color.white;
                return;
            }

            // Icon tạm (khi chưa có Icon_Map.png): khung xanh than + kim cương vàng kiểu Đèn Hiệu.
            _iconImage.color = FrameColor;
            var ring = NewRect("Ring", _iconRect, new Vector2(0.5f, 0.5f), new Vector2(iconSize * 0.56f, iconSize * 0.56f));
            ring.localRotation = Quaternion.Euler(0, 0, 45f);
            var ringImg = ring.gameObject.AddComponent<Image>(); ringImg.color = Gold; ringImg.raycastTarget = false;
            var inner = NewRect("Inner", _iconRect, new Vector2(0.5f, 0.5f), new Vector2(iconSize * 0.38f, iconSize * 0.38f));
            inner.localRotation = Quaternion.Euler(0, 0, 45f);
            var innerImg = inner.gameObject.AddComponent<Image>(); innerImg.color = FrameColor; innerImg.raycastTarget = false;
            var dot = NewRect("Dot", _iconRect, new Vector2(0.5f, 0.5f), new Vector2(iconSize * 0.14f, iconSize * 0.14f));
            var dotImg = dot.gameObject.AddComponent<Image>(); dotImg.color = Gold; dotImg.raycastTarget = false;
        }

        private void BuildMinimap(RectTransform root)
        {
            _miniRect = NewRect("Minimap", root, new Vector2(0f, 1f), new Vector2(minimapSize, minimapSize));
            _miniRect.pivot = new Vector2(0f, 1f);
            _miniRect.anchoredPosition = minimapOffset + new Vector2(0f, -(iconSize + 8f));
            var frame = _miniRect.gameObject.AddComponent<Image>();
            frame.color = new Color(0.02f, 0.03f, 0.04f, 0.85f);
            var miniBtn = _miniRect.gameObject.AddComponent<Button>();
            miniBtn.targetGraphic = frame;
            miniBtn.onClick.AddListener(OnMiniClicked);
            var miniImg = NewRect("Image", _miniRect, new Vector2(0.5f, 0.5f), new Vector2(minimapSize - 8f, minimapSize - 8f));
            _mini = miniImg.gameObject.AddComponent<RawImage>();
            _mini.raycastTarget = false;
            var dot = NewRect("Player", _miniRect, new Vector2(0.5f, 0.5f), new Vector2(8f, 8f));
            var dotImg = dot.gameObject.AddComponent<Image>(); dotImg.color = Color.white; dotImg.raycastTarget = false;
            _miniRect.gameObject.SetActive(false);
        }

        private void BuildFullMap(RectTransform root)
        {
            _fullPanel = new GameObject("FullMap", typeof(RectTransform), typeof(Image));
            _fullPanel.transform.SetParent(root, false);
            var panelRt = (RectTransform)_fullPanel.transform;
            panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one; panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            _fullPanel.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.96f);

            var title = NewText("Title", panelRt, "SỔ HẢI ĐỒ", 34, Gold, TextAnchor.UpperLeft);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(40f, -24f);
            title.rectTransform.sizeDelta = new Vector2(600f, 48f);
            _percent = NewText("Percent", panelRt, "", 24, Color.white, TextAnchor.UpperLeft);
            _percent.rectTransform.anchorMin = _percent.rectTransform.anchorMax = _percent.rectTransform.pivot = new Vector2(0f, 1f);
            _percent.rectTransform.anchoredPosition = new Vector2(40f, -72f);
            _percent.rectTransform.sizeDelta = new Vector2(600f, 36f);
            var hint = NewText("Hint", panelRt, "Chạm Đèn Hiệu vàng để dịch chuyển (đứng yên 3 giây, không dùng khi đang giao chiến)   ·   Chạm vùng trống để thu bản đồ.", 20, new Color(0.8f, 0.85f, 0.9f), TextAnchor.LowerCenter);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            hint.rectTransform.sizeDelta = new Vector2(1400f, 32f);

            _fullRect = NewRect("MapImage", panelRt, new Vector2(0.5f, 0.5f), new Vector2(1500f, 840f));
            _fullRect.anchoredPosition = new Vector2(0f, -10f);
            _full = _fullRect.gameObject.AddComponent<RawImage>();
            _full.raycastTarget = false;
            _playerMarkerFull = NewRect("Player", _fullRect, new Vector2(0.5f, 0.5f), new Vector2(14f, 14f));
            var pimg = _playerMarkerFull.gameObject.AddComponent<Image>();
            pimg.color = new Color(0.4f, 1f, 1f);
            pimg.raycastTarget = false;

            // Cả panel là một nút: chạm vào bất cứ đâu KHÔNG phải Đèn Hiệu thì về icon (Đèn Hiệu là con của MapImage,
            // có Button riêng nằm trên panel nên hứng chạm trước).
            var panelBtn = _fullPanel.AddComponent<Button>();
            panelBtn.targetGraphic = _fullPanel.GetComponent<Image>();
            panelBtn.transition = Selectable.Transition.None;
            panelBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            panelBtn.onClick.AddListener(OnEmptyAreaClicked);
            _fullPanel.SetActive(false);
        }

        private void BuildChannelBar(RectTransform root)
        {
            _channelRoot = new GameObject("ChannelBar", typeof(RectTransform), typeof(Image));
            _channelRoot.transform.SetParent(root, false);
            var cr = (RectTransform)_channelRoot.transform;
            cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0f);
            cr.anchoredPosition = new Vector2(0f, 220f);
            cr.sizeDelta = new Vector2(420f, 22f);
            _channelRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            _channelFill = NewRect("Fill", cr, new Vector2(0f, 0f), Vector2.zero);
            _channelFill.anchorMin = Vector2.zero; _channelFill.anchorMax = new Vector2(0f, 1f);
            _channelFill.offsetMin = new Vector2(2f, 2f); _channelFill.offsetMax = new Vector2(-2f, -2f);
            _channelFill.gameObject.AddComponent<Image>().color = Gold;
            _channelRoot.SetActive(false);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            return rt;
        }

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private RectTransform NewButton(string name, Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var rt = NewRect(name, parent, new Vector2(0.5f, 0.5f), size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.22f, 0.92f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var t = NewText("Label", rt, label, 22, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            return rt;
        }
    }
}
