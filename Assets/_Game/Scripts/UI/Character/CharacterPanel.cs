using System.Globalization;
using Game.Core.Stats;
using Game.PvE.Player;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Character
{
    /// <summary>
    /// Bảng Nhân vật: cấp, EXP, máu tối đa, tấn công, phòng thủ, chí mạng, tốc độ + cộng điểm STR/AGI (bên phải).
    /// Số liệu lấy từ PlayerProgress.Stats (đã gồm cộng thêm từ trang bị và điểm chỉ số); dòng chữ do
    /// CharacterSheet (Core) dựng. Điểm STR/AGI do PlayerStatAllocation giữ — bảng này chỉ gọi TryAllocate khi bấm "+".
    /// UI dựng bằng code như InventoryPanel nên scene chỉ cần một object rỗng + một nút mở.
    /// Phím tắt PC: C. Nút Back/Esc đóng theo thứ tự ưu tiên (IBackHandler).
    ///
    /// Bố cục: hộp 1380x860 (PanelSizeClamp tự co theo màn hình), hai thẻ trái/phải theo tỉ lệ, nút Đóng nằm dưới thẻ
    /// nên không còn đè lên chữ. Mọi chữ dùng phông Be Vietnam Pro của UiSkin.
    /// Lưu ý: trong Game.UI.* phải viết UnityEngine.Time đầy đủ vì có namespace Game.UI.Time.
    /// </summary>
    public class CharacterPanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        /// <summary>Nằm dưới Túi (30) và trên Quest (20) trong chuỗi Back.</summary>
        public const int Priority = 25;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() { Close(); }
        public void CloseExclusive() { Close(); }

        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private Button openButton;
        [Tooltip("Lấy font từ Text có sẵn (để khớp font các panel khác). Để trống = font Be Vietnam Pro của UiSkin.")]
        [SerializeField] private Text fontSource;

        private const float RefreshInterval = 0.5f;
        private static readonly Vector2 BoxDesignSize = new Vector2(1380f, 860f);
        private static readonly Color Gold = new Color(0.96f, 0.84f, 0.45f);
        private static readonly Color Muted = new Color(0.72f, 0.78f, 0.88f);
        private static readonly Color Danger = new Color(0.86f, 0.30f, 0.30f, 1f);
        private static readonly Color Good = new Color(0.30f, 0.72f, 0.40f, 1f);
        private static readonly string[] DefaultLabels = { "Cấp", "EXP", "Máu tối đa", "Tấn công", "Phòng thủ", "Chí mạng", "Tốc độ di chuyển" };

        private GameObject _root;
        private Font _font;
        private Font _boldFont;

        private Text[] _statLabels;
        private Text[] _statValues;
        private RectTransform _expFill;
        private Text _unspentText;
        private Text _strText;
        private Text _strDesc;
        private Text _agiText;
        private Text _agiDesc;
        private Button _strButton;
        private Button _agiButton;
        private PlayerStatAllocation _allocation;
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
                BackKeyRouterHost.Router.CloseExclusiveOthers(this);
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
            if (_statValues == null) return;
            CharacterStats stats = playerProgress != null ? playerProgress.Stats : null;
            if (stats == null)
            {
                for (int i = 0; i < _statValues.Length; i++)
                {
                    _statLabels[i].text = DefaultLabels[i];
                    _statValues[i].text = "—";
                }
                SetExpFill(0f);
                RefreshAllocation();
                return;
            }

            string[] lines = CharacterSheet.BuildLines(
                playerProgress.Level, playerProgress.CurrentExp, playerProgress.RequiredExp,
                stats.MaxHealth, stats.Attack, stats.Defense,
                stats.CriticalChance, stats.CriticalDamage, stats.MoveSpeed);

            for (int i = 0; i < _statValues.Length; i++)
            {
                if (i < lines.Length)
                {
                    SplitLine(lines[i], out string label, out string value);
                    _statLabels[i].text = label;
                    _statValues[i].text = value;
                }
                else
                {
                    _statLabels[i].text = DefaultLabels[i];
                    _statValues[i].text = "—";
                }
            }

            int required = playerProgress.RequiredExp;
            SetExpFill(required > 0 ? Mathf.Clamp01((float)playerProgress.CurrentExp / required) : 0f);
            RefreshAllocation();
        }

        /// <summary>Tách "Máu tối đa  310" → ("Máu tối đa", "310"); "Cấp 15" → ("Cấp", "15").</summary>
        private static void SplitLine(string line, out string label, out string value)
        {
            int idx = line.IndexOf("  ", System.StringComparison.Ordinal);
            int skip = 2;
            if (idx < 0) { idx = line.IndexOf(' '); skip = 1; }
            if (idx < 0) { label = line; value = string.Empty; return; }
            label = line.Substring(0, idx);
            value = line.Substring(idx + skip).Trim();
        }

        private void SetExpFill(float fraction)
        {
            if (_expFill == null) return;
            _expFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        private void RefreshAllocation()
        {
            if (_unspentText == null) return;
            if (_allocation == null && playerProgress != null) playerProgress.TryGetComponent(out _allocation);

            int unspent = _allocation != null ? _allocation.Unspent : 0;
            int str = _allocation != null ? _allocation.Strength : 0;
            int agi = _allocation != null ? _allocation.Agility : 0;

            _unspentText.text = unspent.ToString(CultureInfo.InvariantCulture);
            _unspentText.color = unspent > 0 ? Gold : new Color(0.62f, 0.64f, 0.70f);

            _strText.text = "STR  " + str.ToString(CultureInfo.InvariantCulture);
            _strDesc.text = $"Tấn công +{StatAllocation.AttackBonus(str).ToString("0.#", CultureInfo.InvariantCulture)}";
            _agiText.text = "AGI  " + agi.ToString(CultureInfo.InvariantCulture);
            _agiDesc.text = $"Chạy +{Percent(StatAllocation.MoveSpeedFractionBonus(agi))}%   Chí mạng +{Percent(StatAllocation.CritChanceBonus(agi))}%";

            bool canAllocate = unspent > 0 && _allocation != null;
            if (_strButton != null) _strButton.interactable = canAllocate;
            if (_agiButton != null) _agiButton.interactable = canAllocate;
        }

        private static string Percent(double fraction)
        {
            return (fraction * 100.0).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private void Allocate(AllocatableStat stat)
        {
            if (_allocation == null && playerProgress != null) playerProgress.TryGetComponent(out _allocation);
            if (_allocation != null && _allocation.TryAllocate(stat)) Refresh();
        }

        // ── Dựng UI ────────────────────────────────────────────────────────────

        private void EnsureUi()
        {
            if (_root != null) return;

            _font = UiSkin.DefaultFont != null ? UiSkin.DefaultFont
                  : fontSource != null ? fontSource.font
                  : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _boldFont = UiSkin.BoldFont != null ? UiSkin.BoldFont : _font;

            // Lớp phủ tối: chặn chạm xuống game phía sau.
            _root = new GameObject("_characterRoot", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(transform, false);
            Stretch((RectTransform)_root.transform);
            _root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(PanelSizeClamp));
            box.transform.SetParent(_root.transform, false);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = BoxDesignSize;
            box.GetComponent<PanelSizeClamp>().Configure(BoxDesignSize, 1400f);
            var boxImg = box.GetComponent<Image>();
            boxImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
            UiSkin.Panel(boxImg, 3);

            BuildTitle(box.transform);
            BuildStatsCard(box.transform);
            BuildAllocationCard(box.transform);
            BuildCloseButtons(box.transform);

            _root.SetActive(false);
        }

        private void BuildTitle(Transform box)
        {
            var plate = new GameObject("TitlePlate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(box, false);
            var rt = (RectTransform)plate.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -26f);
            rt.sizeDelta = new Vector2(560f, 96f);
            var img = plate.GetComponent<Image>();
            if (!UiSkin.Header(img, 3)) img.color = new Color(0.20f, 0.24f, 0.34f, 1f);

            Text title = MakeText(plate.transform, "Title", 52, TextAnchor.MiddleCenter, Gold, true);
            title.text = "Nhân vật";
            AddShadow(title);
            Stretch(title.rectTransform);
        }

        /// <summary>Thẻ trái: bảng chỉ số dạng "nhãn ... giá trị" + thanh EXP.</summary>
        private void BuildStatsCard(Transform box)
        {
            RectTransform card = MakeCard(box, "StatsCard", 0.03f, 0.485f);

            Text heading = MakeText(card, "Heading", 36, TextAnchor.MiddleLeft, Gold, true);
            heading.text = "Chỉ số";
            var hRt = heading.rectTransform;
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.offsetMin = new Vector2(28f, -68f);
            hRt.offsetMax = new Vector2(-28f, -14f);

            var list = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(card, false);
            var lRt = (RectTransform)list.transform;
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(16f, 16f);
            lRt.offsetMax = new Vector2(-16f, -72f);
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            int count = DefaultLabels.Length;
            _statLabels = new Text[count];
            _statValues = new Text[count];
            for (int i = 0; i < count; i++)
            {
                var row = new GameObject("Stat_" + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                row.transform.SetParent(list.transform, false);
                var rowImg = row.GetComponent<Image>();
                rowImg.raycastTarget = false;
                rowImg.color = i % 2 == 0 ? new Color(0.22f, 0.30f, 0.42f, 0.38f) : new Color(0f, 0f, 0f, 0f);
                row.GetComponent<LayoutElement>().preferredHeight = 56f;

                Text label = MakeText(row.transform, "Label", 32, TextAnchor.MiddleLeft, Muted, false);
                label.text = DefaultLabels[i];
                var labelRt = label.rectTransform;
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(0.46f, 1f);
                labelRt.offsetMin = new Vector2(16f, 0f);
                labelRt.offsetMax = Vector2.zero;

                Text value = MakeText(row.transform, "Value", 34, TextAnchor.MiddleRight, Color.white, true);
                value.text = "—";
                FitText(value, 22, 34);
                var valueRt = value.rectTransform;
                valueRt.anchorMin = new Vector2(0.46f, 0f);
                valueRt.anchorMax = new Vector2(1f, 1f);
                valueRt.offsetMin = Vector2.zero;
                valueRt.offsetMax = new Vector2(-16f, 0f);

                _statLabels[i] = label;
                _statValues[i] = value;

                // Thanh EXP ngay dưới dòng EXP (chỉ số 1).
                if (i == 1) BuildExpBar(list.transform);
            }
        }

        private void BuildExpBar(Transform parent)
        {
            var bar = new GameObject("ExpBar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            bar.transform.SetParent(parent, false);
            bar.GetComponent<LayoutElement>().preferredHeight = 30f;
            var frame = bar.GetComponent<Image>();
            frame.raycastTarget = false;
            if (!UiSkin.Inset(frame, 2)) frame.color = new Color(0.05f, 0.07f, 0.11f, 1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bar.transform, false);
            _expFill = (RectTransform)fillGo.transform;
            _expFill.anchorMin = new Vector2(0f, 0f);
            _expFill.anchorMax = new Vector2(0f, 1f);
            _expFill.pivot = new Vector2(0f, 0.5f);
            _expFill.offsetMin = new Vector2(6f, 6f);
            _expFill.offsetMax = new Vector2(-6f, -6f);
            var fill = fillGo.GetComponent<Image>();
            fill.raycastTarget = false;
            fill.color = new Color(0.92f, 0.72f, 0.26f, 1f);
        }

        /// <summary>Thẻ phải: điểm còn lại, hai hàng STR/AGI kèm nút "+", ghi chú.</summary>
        private void BuildAllocationCard(Transform box)
        {
            RectTransform card = MakeCard(box, "AllocationCard", 0.515f, 0.97f);

            Text heading = MakeText(card, "Heading", 36, TextAnchor.MiddleLeft, Gold, true);
            heading.text = "Cộng điểm chỉ số";
            var hRt = heading.rectTransform;
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.offsetMin = new Vector2(28f, -68f);
            hRt.offsetMax = new Vector2(-28f, -14f);

            // Dòng "Điểm còn lại": nhãn trái, số lớn phải
            RectTransform unspentRow = MakeStrip(card, "UnspentRow", -78f, 84f, new Color(0.22f, 0.30f, 0.42f, 0.38f));
            Text unspentLabel = MakeText(unspentRow, "Label", 34, TextAnchor.MiddleLeft, Muted, false);
            unspentLabel.text = "Điểm còn lại";
            var ulRt = unspentLabel.rectTransform;
            ulRt.anchorMin = Vector2.zero;
            ulRt.anchorMax = new Vector2(0.65f, 1f);
            ulRt.offsetMin = new Vector2(20f, 0f);
            ulRt.offsetMax = Vector2.zero;
            _unspentText = MakeText(unspentRow, "Value", 56, TextAnchor.MiddleRight, Gold, true);
            AddShadow(_unspentText);
            var uvRt = _unspentText.rectTransform;
            uvRt.anchorMin = new Vector2(0.65f, 0f);
            uvRt.anchorMax = Vector2.one;
            uvRt.offsetMin = Vector2.zero;
            uvRt.offsetMax = new Vector2(-24f, 0f);

            BuildAllocRow(card, "StrRow", -176f, "StrPlusButton", AllocatableStat.Strength, out _strText, out _strDesc, out _strButton);
            BuildAllocRow(card, "AgiRow", -318f, "AgiPlusButton", AllocatableStat.Agility, out _agiText, out _agiDesc, out _agiButton);

            Text hint = MakeText(card, "AllocationHint", 26, TextAnchor.UpperLeft, new Color(0.70f, 0.74f, 0.82f), false);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.offsetMin = new Vector2(28f, 18f);
            hintRt.offsetMax = new Vector2(-28f, 120f);
            hint.lineSpacing = 1.1f;
            hint.text = $"Mỗi cấp +{StatAllocation.PointsPerLevel} điểm. STR: +1 tấn công/điểm. AGI: +0,4% tốc chạy, +0,3% chí mạng/điểm (tối đa +30% mỗi loại).";
        }

        private void BuildAllocRow(RectTransform card, string name, float top, string buttonName, AllocatableStat stat,
            out Text title, out Text desc, out Button button)
        {
            RectTransform strip = MakeStrip(card, name, top, 128f, new Color(0.22f, 0.30f, 0.42f, 0.38f));

            title = MakeText(strip, "Title", 42, TextAnchor.MiddleLeft, Color.white, true);
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(22f, 0f);
            tRt.offsetMax = new Vector2(-140f, -8f);

            desc = MakeText(strip, "Desc", 30, TextAnchor.MiddleLeft, Muted, false);
            FitText(desc, 22, 30);
            var dRt = desc.rectTransform;
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0.5f);
            dRt.offsetMin = new Vector2(22f, 8f);
            dRt.offsetMax = new Vector2(-140f, 0f);

            button = MakeButton(strip, buttonName, "+", 60, Good, () => Allocate(stat));
            var bRt = (RectTransform)button.transform;
            bRt.anchorMin = bRt.anchorMax = bRt.pivot = new Vector2(1f, 0.5f);
            bRt.anchoredPosition = new Vector2(-20f, 0f);
            bRt.sizeDelta = new Vector2(104f, 96f);
        }

        private void BuildCloseButtons(Transform box)
        {
            Button topClose = MakeButton(box, "TopCloseButton", "X", 40, Danger, Close);
            var tcRt = (RectTransform)topClose.transform;
            tcRt.anchorMin = tcRt.anchorMax = tcRt.pivot = new Vector2(1f, 1f);
            tcRt.anchoredPosition = new Vector2(-26f, -26f);
            tcRt.sizeDelta = new Vector2(92f, 92f);

            Button close = MakeButton(box, "CloseButton", "Đóng", 42, Danger, Close);
            var cRt = (RectTransform)close.transform;
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0f);
            cRt.pivot = new Vector2(0.5f, 0f);
            cRt.anchoredPosition = new Vector2(0f, 34f);
            cRt.sizeDelta = new Vector2(340f, 84f);
        }

        // ── Helper dựng UI ─────────────────────────────────────────────────────

        /// <summary>Thẻ nền trũng nằm giữa tiêu đề và nút Đóng; x0/x1 là tỉ lệ ngang trong hộp.</summary>
        private RectTransform MakeCard(Transform box, string name, float x0, float x1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(box, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(x0, 0f);
            rt.anchorMax = new Vector2(x1, 1f);
            rt.offsetMin = new Vector2(0f, 142f);   // chừa chỗ nút Đóng ở đáy
            rt.offsetMax = new Vector2(0f, -140f);  // chừa chỗ tiêu đề ở đỉnh
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            if (!UiSkin.Inset(img, 2)) img.color = new Color(0.05f, 0.07f, 0.12f, 0.9f);
            return rt;
        }

        /// <summary>Dải ngang trong thẻ phải: top âm tính từ mép trên của thẻ.</summary>
        private static RectTransform MakeStrip(RectTransform card, string name, float top, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(card, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(16f, top - height);
            rt.offsetMax = new Vector2(-16f, top);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = color;
            return rt;
        }

        private Button MakeButton(Transform parent, string name, string label, int size, Color tint, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = tint;
            UiSkin.Button(img, 2);

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.75f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            Text text = MakeText(go.transform, "Text", size, TextAnchor.MiddleCenter, Color.white, true);
            text.text = label;
            AddShadow(text);
            Stretch(text.rectTransform);
            return button;
        }

        private Text MakeText(Transform parent, string name, int size, TextAnchor anchor, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = bold ? _boldFont : _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static void FitText(Text t, int minSize, int maxSize)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = maxSize;
            t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void AddShadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.65f);
            s.effectDistance = new Vector2(2f, -2f);
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
