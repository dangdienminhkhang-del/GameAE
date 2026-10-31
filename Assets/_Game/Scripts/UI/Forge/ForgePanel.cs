using System.Collections.Generic;
using System.Globalization;
using Game.Core.Items;
using Game.PvE.Blacksmith;
using Game.PvE.Items;
using Game.UI.Common;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Forge
{
    /// <summary>
    /// Bảng Thợ rèn (SAO): sửa độ bền và cường hóa trang bị bằng Col. Dựng bằng code dưới Canvas lúc chạy
    /// (cùng mẫu với InnDialoguePanel), không cần sửa scene. Mở khi BlacksmithInteractable phát Requested.
    /// UI chỉ gọi PlayerInventory.TryRepair/TryEnhance — luật nằm ở Core/Items (DurabilityRules, UpgradeRules).
    /// Lưu ý: trong Game.UI.* phải viết UnityEngine.Time đầy đủ vì có namespace Game.UI.Time.
    /// </summary>
    public class ForgePanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 37;
        private const float AutoCloseDistance = 7f;

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color RowBg = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color ListBg = new Color(0.10f, 0.11f, 0.17f, 1f);
        private static readonly Color RepairColor = new Color(0.18f, 0.60f, 0.38f, 1f);
        private static readonly Color EnhanceColor = new Color(0.82f, 0.48f, 0.15f, 1f);
        private static readonly Color CloseColor = new Color(0.35f, 0.35f, 0.42f, 1f);

        private Font _font;
        private GameObject _root;
        private GameObject _openButton;
        private Text _colText;
        private Text _statusText;
        private Text _emptyText;
        private RectTransform _listContent;

        private PlayerInventory _inventory;
        private PlayerWallet _wallet;
        private BlacksmithInteractable _openSmith;      // thợ rèn đang mở bảng
        private BlacksmithInteractable _inRangeSmith;   // thợ rèn người chơi đang đứng gần
        private readonly List<GameObject> _rows = new List<GameObject>();

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private static ForgePanel s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            SceneManager.sceneLoaded += (scene, mode) => EnsureOnCanvas();
            EnsureOnCanvas();
        }

        public static void EnsureOnCanvas()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "PvE_Gameplay" && currentScene != "Floor02") return;
            if (s_instance != null) return;

            var canvas = GameObject.Find("/Canvas");
            if (canvas == null)
            {
                var canvasComp = FindAnyObjectByType<Canvas>();
                if (canvasComp != null) canvas = canvasComp.gameObject;
            }

            if (canvas != null && canvas.transform.Find("ForgePanel") == null)
            {
                var panelGo = new GameObject("ForgePanel", typeof(RectTransform), typeof(ForgePanel));
                panelGo.transform.SetParent(canvas.transform, false);
            }
        }

        private void Awake()
        {
            s_instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Stretch((RectTransform)transform); // phủ kín Canvas để neo nút nổi và bảng đúng chỗ
            Build();
            _root.SetActive(false);
            _openButton.SetActive(false);
        }

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
            BlacksmithInteractable.Requested += HandleRequested;
            BlacksmithInteractable.InRangeChanged += HandleInRangeChanged;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            BlacksmithInteractable.Requested -= HandleRequested;
            BlacksmithInteractable.InRangeChanged -= HandleInRangeChanged;
            Unbind();
        }

        private void HandleRequested(BlacksmithInteractable smith)
        {
            Open(smith);
        }

        private void HandleInRangeChanged(BlacksmithInteractable smith, bool inRange)
        {
            if (inRange) _inRangeSmith = smith;
            else if (ReferenceEquals(_inRangeSmith, smith)) _inRangeSmith = null;
        }

        private void Update()
        {
            // Nút nổi cho điện thoại: chỉ hiện khi đứng gần thợ rèn và bảng đang đóng.
            bool showOpen = _inRangeSmith != null && !IsOpen;
            if (_openButton != null && _openButton.activeSelf != showOpen) _openButton.SetActive(showOpen);

            if (!IsOpen) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (_openSmith != null && _inventory != null)
            {
                float dist = Vector2.Distance(_inventory.transform.position, _openSmith.transform.position);
                if (dist > AutoCloseDistance) Close();
            }
        }

        public void Open(BlacksmithInteractable smith)
        {
            _openSmith = smith;
            Bind();
            BackKeyRouterHost.Router.CloseExclusiveOthers(this);
            _statusText.text = string.Empty;
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            Unbind();
            _openSmith = null;
        }

        private void Bind()
        {
            Unbind();
            _inventory = FindAnyObjectByType<PlayerInventory>();
            if (_inventory == null) return;
            _wallet = _inventory.GetComponent<PlayerWallet>();
            _inventory.Changed += Refresh;
            if (_wallet != null) _wallet.Changed += RefreshCol;
        }

        private void Unbind()
        {
            if (_inventory != null) _inventory.Changed -= Refresh;
            if (_wallet != null) _wallet.Changed -= RefreshCol;
            _inventory = null;
            _wallet = null;
        }

        // ── Làm mới ────────────────────────────────────────────────────────────

        private void RefreshCol()
        {
            if (_colText != null) _colText.text = $"{(_wallet != null ? _wallet.Col : 0)} Col";
        }

        private void Refresh()
        {
            RefreshCol();
            RebuildRows();
        }

        private void RebuildRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null) Destroy(_rows[i]);
            }
            _rows.Clear();
            if (_inventory == null || _inventory.Database == null) return;

            // Trang bị đang mặc lên đầu, rồi đến đồ trong túi.
            var equipped = new List<ItemInstance>();
            var spare = new List<ItemInstance>();
            foreach (ItemInstance instance in _inventory.Inventory.Instances)
            {
                ItemDefinition def = _inventory.Database.Find(instance.itemId);
                if (def == null || def.Slot == EquipmentSlot.None) continue;
                if (_inventory.IsEquipped(instance.instanceId)) equipped.Add(instance);
                else spare.Add(instance);
            }

            _emptyText.gameObject.SetActive(equipped.Count + spare.Count == 0);
            foreach (ItemInstance instance in equipped) _rows.Add(BuildRow(instance, true));
            foreach (ItemInstance instance in spare) _rows.Add(BuildRow(instance, false));
        }

        private GameObject BuildRow(ItemInstance instance, bool equipped)
        {
            ItemDefinition def = _inventory.Database.Find(instance.itemId);
            string id = instance.instanceId;

            GameObject row = NewUi("Row", _listContent);
            row.AddComponent<Image>().color = RowBg;
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.minHeight = 160f;
            rowLe.preferredHeight = 160f;
            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = true;
            hl.spacing = 12f;
            hl.padding = new RectOffset(14, 14, 12, 12);

            // Cột thông tin
            Text info = NewText("Info", row.transform, BuildInfo(instance, def, equipped), 26, Color.white, TextAnchor.MiddleLeft);
            info.supportRichText = true;
            info.lineSpacing = 1.1f;
            var infoLe = info.gameObject.AddComponent<LayoutElement>();
            infoLe.flexibleWidth = 1f;
            infoLe.minWidth = 240f;

            // Nút Sửa
            bool wears = DurabilityRules.Wears(def.MaxDurability);
            int missing = DurabilityRules.Missing(instance.durability, def.MaxDurability);
            int repairCost = _inventory.GetRepairCost(id);
            string repairLabel = !wears ? "Không hao mòn" : (missing <= 0 ? "Đủ bền" : $"Sửa\n{repairCost} Col");
            Button repair = NewButton("Repair", row.transform, repairLabel, 24, RepairColor, () => OnRepair(id));
            repair.interactable = wears && missing > 0;
            var repairLe = repair.gameObject.AddComponent<LayoutElement>();
            repairLe.preferredWidth = 230f;
            repairLe.minWidth = 200f;

            // Nút Cường hóa
            bool canUp = UpgradeRules.CanUpgrade(instance.upgradeLevel);
            string enhanceLabel = canUp
                ? $"Cường hóa +{instance.upgradeLevel + 1}\n{_inventory.GetEnhanceCost(id)} Col · {Pct(_inventory.GetEnhanceChance(id))}%"
                : "Đã tối đa";
            Button enhance = NewButton("Enhance", row.transform, enhanceLabel, 24, EnhanceColor, () => OnEnhance(id));
            enhance.interactable = canUp;
            var enhanceLe = enhance.gameObject.AddComponent<LayoutElement>();
            enhanceLe.preferredWidth = 330f;
            enhanceLe.minWidth = 280f;

            return row;
        }

        private static string BuildInfo(ItemInstance instance, ItemDefinition def, bool equipped)
        {
            string upgrade = instance.upgradeLevel > 0 ? $" <color=#f2c14e>{UpgradeRules.Label(instance.upgradeLevel)}</color>" : string.Empty;
            string worn = equipped ? "  <color=#4ee44e>[Đang mặc]</color>" : string.Empty;
            string line1 = $"<b>{def.DisplayName}</b>{upgrade}{worn}";

            string line2;
            if (!DurabilityRules.Wears(def.MaxDurability))
                line2 = "<color=#9fb4cf>Không hao mòn</color>";
            else if (DurabilityRules.IsBroken(instance.durability, def.MaxDurability))
                line2 = $"<color=#ff5555>ĐÃ HỎNG — không cộng chỉ số ({DurabilityRules.Label(instance.durability, def.MaxDurability)})</color>";
            else if (DurabilityRules.IsLow(instance.durability, def.MaxDurability))
                line2 = $"Độ bền: <color=#ffa040>{DurabilityRules.Label(instance.durability, def.MaxDurability)} (sắp hỏng)</color>";
            else
                line2 = $"Độ bền: {DurabilityRules.Label(instance.durability, def.MaxDurability)}";

            ItemStats s = def.BaseStats + instance.rolledStats;
            s += UpgradeRules.BonusStats(s, instance.upgradeLevel);
            var parts = new List<string>();
            if (s.attack > 0) parts.Add($"ATK {Fmt(s.attack)}");
            if (s.defense > 0) parts.Add($"DEF {Fmt(s.defense)}");
            if (s.maxHealth > 0) parts.Add($"HP {Fmt(s.maxHealth)}");
            string line3 = parts.Count > 0 ? "<color=#9fd0f5>" + string.Join("  •  ", parts) + "</color>" : string.Empty;

            return line1 + "\n" + line2 + (line3.Length > 0 ? "\n" + line3 : string.Empty);
        }

        // ── Hành động ────────────────────────────────────────────────────────────

        private void OnRepair(string instanceId)
        {
            if (_inventory == null) return;
            ItemInstance instance = _inventory.Inventory.FindInstance(instanceId);
            ItemDefinition def = instance != null ? _inventory.Database.Find(instance.itemId) : null;
            string name = def != null ? def.DisplayName : "Trang bị";
            int cost = _inventory.GetRepairCost(instanceId);

            RepairResult result = _inventory.TryRepair(instanceId);
            switch (result)
            {
                case RepairResult.Ok: SetStatus($"Đã sửa {name} (−{cost} Col).", new Color(0.4f, 0.95f, 0.5f)); break;
                case RepairResult.NotEnoughCol: SetStatus($"Không đủ Col để sửa (cần {cost} Col).", new Color(1f, 0.45f, 0.4f)); break;
                case RepairResult.AlreadyFull: SetStatus($"{name} vẫn còn đầy độ bền.", Color.yellow); break;
                case RepairResult.NoDurability: SetStatus($"{name} không hao mòn.", Color.yellow); break;
                default: SetStatus("Không tìm thấy trang bị.", new Color(1f, 0.45f, 0.4f)); break;
            }
            Refresh();
        }

        private void OnEnhance(string instanceId)
        {
            if (_inventory == null) return;
            ItemInstance instance = _inventory.Inventory.FindInstance(instanceId);
            ItemDefinition def = instance != null ? _inventory.Database.Find(instance.itemId) : null;
            string name = def != null ? def.DisplayName : "Trang bị";
            int cost = _inventory.GetEnhanceCost(instanceId);

            EnhanceResult result = _inventory.TryEnhance(instanceId);
            int level = instance != null ? instance.upgradeLevel : 0; // TryEnhance đã cập nhật trên cùng instance
            switch (result)
            {
                case EnhanceResult.Success: SetStatus($"Cường hóa THÀNH CÔNG! {name} {UpgradeRules.Label(level)}", new Color(0.4f, 0.95f, 0.5f)); break;
                case EnhanceResult.Failed: SetStatus($"Thất bại... mất {cost} Col, {name} giữ nguyên.", new Color(1f, 0.65f, 0.3f)); break;
                case EnhanceResult.FailedDowngraded: SetStatus($"Thất bại! {name} tụt xuống {UpgradeRules.Label(level)} (mất {cost} Col).", new Color(1f, 0.4f, 0.4f)); break;
                case EnhanceResult.NotEnoughCol: SetStatus($"Không đủ Col để cường hóa (cần {cost} Col).", new Color(1f, 0.45f, 0.4f)); break;
                case EnhanceResult.Broken: SetStatus($"{name} đang hỏng — hãy sửa trước khi cường hóa.", new Color(1f, 0.45f, 0.4f)); break;
                case EnhanceResult.MaxLevel: SetStatus($"{name} đã đạt cấp cường hóa tối đa.", Color.yellow); break;
                case EnhanceResult.NotUpgradable: SetStatus($"{name} không thể cường hóa.", Color.yellow); break;
                default: SetStatus("Không tìm thấy trang bị.", new Color(1f, 0.45f, 0.4f)); break;
            }
            Refresh();
        }

        private void SetStatus(string message, Color color)
        {
            if (_statusText == null) return;
            _statusText.text = message;
            _statusText.color = color;
        }

        private static string Fmt(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
        private static string Pct(double fraction) => (fraction * 100.0).ToString("0", CultureInfo.InvariantCulture);

        // ── Dựng UI ────────────────────────────────────────────────────────────

        private void Build()
        {
            _root = NewUi("ForgePanelRoot", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject border = NewUi("Border", _root.transform);
            SetAnchors(border.GetComponent<RectTransform>(), 0.08f, 0.05f, 0.92f, 0.95f);
            border.AddComponent<PanelSizeClamp>();
            border.AddComponent<Image>().color = Gold;

            Button topClose = NewButton("TopClose", border.transform, "X", 32, new Color(0.85f, 0.25f, 0.25f, 0.95f), Close);
            var tcRt = topClose.GetComponent<RectTransform>();
            tcRt.anchorMin = tcRt.anchorMax = tcRt.pivot = new Vector2(1f, 1f);
            tcRt.anchoredPosition = new Vector2(-8f, -8f);
            tcRt.sizeDelta = new Vector2(70f, 70f);

            GameObject win = NewUi("Window", border.transform);
            RectTransform wr = win.GetComponent<RectTransform>();
            Stretch(wr);
            wr.offsetMin = new Vector2(4, 4);
            wr.offsetMax = new Vector2(-4, -4);
            win.AddComponent<Image>().color = Ink;

            Text title = NewText("Title", win.transform, "Thợ rèn", 38, Gold, TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            SetAnchors(title.rectTransform, 0.04f, 0.88f, 0.60f, 0.98f);

            _colText = NewText("Col", win.transform, "0 Col", 32, Gold, TextAnchor.MiddleRight);
            SetAnchors(_colText.rectTransform, 0.55f, 0.88f, 0.90f, 0.98f);

            Text hint = NewText("Hint", win.transform,
                "Sửa độ bền và cường hóa trang bị bằng Col. Cường hóa có thể thất bại; từ +7 trở lên thất bại sẽ tụt 1 cấp.",
                22, new Color(0.62f, 0.7f, 0.82f), TextAnchor.MiddleLeft);
            SetAnchors(hint.rectTransform, 0.04f, 0.82f, 0.96f, 0.88f);

            // Khung danh sách có cuộn
            GameObject container = NewUi("ListContainer", win.transform);
            SetAnchors(container.GetComponent<RectTransform>(), 0.03f, 0.19f, 0.97f, 0.81f);
            container.AddComponent<Image>().color = ListBg;

            GameObject viewport = NewUi("Viewport", container.transform);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            Stretch(vpRt);
            vpRt.offsetMin = new Vector2(4, 4);
            vpRt.offsetMax = new Vector2(-4, -4);
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUi("Content", viewport.transform);
            _listContent = content.GetComponent<RectTransform>();
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.sizeDelta = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = container.AddComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = _listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            _emptyText = NewText("Empty", container.transform, "Bạn chưa có trang bị nào để sửa hay cường hóa.", 28, new Color(0.7f, 0.74f, 0.82f), TextAnchor.MiddleCenter);
            Stretch(_emptyText.rectTransform);
            _emptyText.gameObject.SetActive(false);

            _statusText = NewText("Status", win.transform, string.Empty, 26, Color.yellow, TextAnchor.MiddleCenter);
            SetAnchors(_statusText.rectTransform, 0.04f, 0.10f, 0.96f, 0.18f);

            Button close = NewButton("CloseButton", win.transform, "Đóng", 28, CloseColor, Close);
            SetAnchors(close.GetComponent<RectTransform>(), 0.36f, 0.015f, 0.64f, 0.095f);

            // Nút nổi mở bảng (cho điện thoại, nằm ngoài _root để không bị ẩn theo bảng)
            Button open = NewButton("OpenButton", transform, "Thợ rèn (E)", 30, EnhanceColor, OpenFromButton);
            _openButton = open.gameObject;
            var obRt = open.GetComponent<RectTransform>();
            obRt.anchorMin = obRt.anchorMax = obRt.pivot = new Vector2(0.5f, 0f);
            obRt.anchoredPosition = new Vector2(0f, 300f);
            obRt.sizeDelta = new Vector2(420f, 100f);
        }

        private void OpenFromButton()
        {
            if (_inRangeSmith != null) _inRangeSmith.Interact();
        }

        private GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var go = NewUi(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = _font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            Text text = NewText("Label", go.transform, label, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return btn;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
