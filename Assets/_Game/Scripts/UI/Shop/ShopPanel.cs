using System.Collections.Generic;
using Game.Core.Items;
using Game.Core.NPC;
using Game.Core.Shop;
using Game.PvE.Items;
using Game.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI.Shop
{
    /// <summary>
    /// Giao diện cửa hàng NPC (Mua / Bán). Tự dựng toàn bộ UI lúc chạy dưới Canvas, không cần prefab.
    /// Chỉ hiển thị + gửi ý định; logic giao dịch nằm ở ShopTransactions.
    /// </summary>
    public class ShopPanel : MonoBehaviour, IBackHandler
    {
        public const int Priority = 35;

        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerWallet wallet;
        [Tooltip("Tự đóng khi player đi xa NPC hơn khoảng này (đơn vị thế giới).")]
        [SerializeField] private float closeDistance = 6f;

        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color Row = new Color(0.14f, 0.16f, 0.24f, 1f);
        private static readonly Color BtnOn = new Color(0.20f, 0.55f, 0.50f, 1f);
        private static readonly Color BtnOff = new Color(0.30f, 0.30f, 0.36f, 1f);
        private static readonly Color TabOn = new Color(0.78f, 0.57f, 0.23f, 1f);
        private static readonly Color TabOff = new Color(0.20f, 0.22f, 0.30f, 1f);

        private Font _font;
        private GameObject _root;
        private Text _title, _colText, _message;
        private RectTransform _content;
        private Button _buyTab, _sellTab;
        private ShopDefinition _shop;
        private NPCDefinition _npc;
        private Component _npcSource;
        private bool _sellMode;

        public int BackPriority => Priority;
        public bool IsOpen => _root != null && _root.activeSelf;
        public void CloseFromBack() => Close();

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            BackKeyRouterHost.Router.Register(this);
            ShopEvents.OnShopRequested += HandleShopRequested;
        }

        private void OnDisable()
        {
            BackKeyRouterHost.Router.Unregister(this);
            ShopEvents.OnShopRequested -= HandleShopRequested;
            if (wallet != null) wallet.Changed -= RefreshColLabel;
        }

        private void Update()
        {
            if (!IsOpen) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Close(); return; }

            if (_npcSource != null && playerInventory != null)
            {
                float d = Vector2.Distance(_npcSource.transform.position, playerInventory.transform.position);
                if (d > closeDistance) Close();
            }
        }

        // ── Mở / đóng ──────────────────────────────────────────────────────

        private void HandleShopRequested(object sender, ShopRequest request)
        {
            if (request == null || request.Shop == null) return;
            if (playerInventory == null) playerInventory = FindAnyObjectByType<PlayerInventory>();
            if (wallet == null && playerInventory != null) playerInventory.TryGetComponent(out wallet);
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (playerInventory == null || wallet == null) return;

            _shop = request.Shop;
            _npc = request.Npc;
            _npcSource = sender as Component;
            _sellMode = false;

            wallet.Changed -= RefreshColLabel;
            wallet.Changed += RefreshColLabel;

            _title.text = _npc != null ? _npc.DisplayName : _shop.DisplayName;
            _message.text = _shop.Greeting;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
            if (wallet != null) wallet.Changed -= RefreshColLabel;
            _npcSource = null;
        }

        // ── Dựng UI ────────────────────────────────────────────────────────

        private void Build()
        {
            _root = NewUi("ShopPanelRoot", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            GameObject border = NewUi("Border", _root.transform);
            SetAnchors(border.GetComponent<RectTransform>(), 0.17f, 0.09f, 0.83f, 0.91f);
            Image borderImg = border.AddComponent<Image>(); borderImg.color = Gold;

            GameObject win = NewUi("Window", border.transform);
            RectTransform wr = win.GetComponent<RectTransform>();
            Stretch(wr); wr.offsetMin = new Vector2(4, 4); wr.offsetMax = new Vector2(-4, -4);
            Image winImg = win.AddComponent<Image>(); winImg.color = Ink; if (UiSkin.Panel(borderImg, 3)) { winImg.color = Color.clear; wr.offsetMin = new Vector2(24, 24); wr.offsetMax = new Vector2(-24, -24); }

            _title = NewText("Title", win.transform, "Cửa hàng", 46, Gold, TextAnchor.MiddleLeft);
            SetAnchors(_title.rectTransform, 0.03f, 0.89f, 0.62f, 0.99f);
            _colText = NewText("Col", win.transform, "0 Col", 40, Color.white, TextAnchor.MiddleRight);
            SetAnchors(_colText.rectTransform, 0.60f, 0.89f, 0.97f, 0.99f);

            _message = NewText("Message", win.transform, "", 30, new Color(0.85f, 0.88f, 0.95f), TextAnchor.UpperLeft);
            SetAnchors(_message.rectTransform, 0.03f, 0.78f, 0.97f, 0.89f);

            _buyTab = NewButton("BuyTab", win.transform, "MUA", 34, TabOn, () => { _sellMode = false; Refresh(); });
            SetAnchors(_buyTab.GetComponent<RectTransform>(), 0.03f, 0.69f, 0.24f, 0.78f);
            _sellTab = NewButton("SellTab", win.transform, "BÁN", 34, TabOff, () => { _sellMode = true; Refresh(); });
            SetAnchors(_sellTab.GetComponent<RectTransform>(), 0.25f, 0.69f, 0.46f, 0.78f);

            GameObject scrollGo = NewUi("Scroll", win.transform);
            SetAnchors(scrollGo.GetComponent<RectTransform>(), 0.03f, 0.13f, 0.97f, 0.68f);
            scrollGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<RectMask2D>();

            GameObject content = NewUi("Content", scrollGo.transform);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = new Vector2(8, 0); _content.offsetMax = new Vector2(-8, 0);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.padding = new RectOffset(0, 0, 8, 8);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

            Button close = NewButton("Close", win.transform, "ĐÓNG", 34, BtnOff, Close);
            SetAnchors(close.GetComponent<RectTransform>(), 0.35f, 0.02f, 0.65f, 0.11f);
        }

        // ── Làm mới danh sách ──────────────────────────────────────────────

        private void Refresh()
        {
            if (_shop == null) return;
            RefreshColLabel();
            _buyTab.GetComponent<Image>().color = _sellMode ? TabOff : TabOn;
            _sellTab.GetComponent<Image>().color = _sellMode ? TabOn : TabOff;

            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);

            if (!_sellMode) BuildBuyRows(); else BuildSellRows();
        }

        private void RefreshColLabel()
        {
            if (_colText != null && wallet != null) _colText.text = wallet.Col.ToString("N0") + " Col";
        }

        private void BuildBuyRows()
        {
            foreach (ShopEntry entry in _shop.Stock)
            {
                if (entry == null || entry.item == null) continue;
                ShopEntry e = entry;
                bool affordable = wallet.Col >= e.price;
                AddRow(e.item, Describe(e.item), e.price + " Col", "Mua", affordable, () =>
                {
                    ShopResult r = ShopTransactions.Buy(playerInventory, wallet, e);
                    _message.text = r == ShopResult.Ok ? _shop.BoughtLine
                        : r == ShopResult.NotEnoughCol ? _shop.NotEnoughLine : "Túi đồ không nhận thêm được.";
                    Refresh();
                });
            }
        }

        private void BuildSellRows()
        {
            int rows = 0;
            var instances = new List<ItemInstance>(playerInventory.Inventory.Instances);
            foreach (ItemInstance inst in instances)
            {
                ItemDefinition def = playerInventory.Database.Find(inst.itemId);
                int price = _shop.SellPriceOf(def);
                if (def == null || price <= 0) continue;
                string iid = inst.instanceId;
                bool equipped = playerInventory.IsEquipped(iid);
                AddRow(def, equipped ? "Đang mặc — tháo ra mới bán được" : Describe(def), price + " Col", "Bán", !equipped, () =>
                {
                    ShopResult r = ShopTransactions.SellInstance(playerInventory, wallet, _shop, iid);
                    _message.text = r == ShopResult.Ok ? _shop.SoldLine : "Món này chưa bán được.";
                    Refresh();
                });
                rows++;
            }

            var stacks = new List<ItemStack>(playerInventory.Inventory.Stacks);
            foreach (ItemStack st in stacks)
            {
                ItemDefinition def = playerInventory.Database.Find(st.itemId);
                int price = _shop.SellPriceOf(def);
                if (def == null || price <= 0) continue;
                string id = st.itemId;
                AddRow(def, "Đang có: " + playerInventory.Inventory.GetStack(id), price + " Col", "Bán 1", true, () =>
                {
                    ShopResult r = ShopTransactions.SellStackOne(playerInventory, wallet, _shop, id);
                    _message.text = r == ShopResult.Ok ? _shop.SoldLine : "Món này chưa bán được.";
                    Refresh();
                });
                rows++;
            }

            if (rows == 0) _message.text = "Bạn chưa có món nào để bán.";
        }

        private static string Describe(ItemDefinition d)
        {
            var parts = new List<string>();
            if (d.BaseStats.attack != 0) parts.Add("Tấn công +" + d.BaseStats.attack);
            if (d.BaseStats.defense != 0) parts.Add("Phòng thủ +" + d.BaseStats.defense);
            if (d.BaseStats.maxHealth != 0) parts.Add("HP tối đa +" + d.BaseStats.maxHealth);
            if (d.HealAmount > 0) parts.Add("Hồi " + d.HealAmount + " HP");
            if (d.Type == ItemType.Weapon) parts.Insert(0, "Vũ khí");
            else if (d.Type == ItemType.Armor) parts.Insert(0, "Giáp");
            return parts.Count > 0 ? string.Join("  •  ", parts) : "Vật phẩm";
        }

        private void AddRow(ItemDefinition def, string sub, string priceText, string actionLabel, bool enabled, UnityEngine.Events.UnityAction onClick)
        {
            GameObject row = NewUi("Row", _content);
            Image rowImg = row.AddComponent<Image>(); rowImg.color = Row; UiSkin.Button(rowImg, 2);
            LayoutElement le = row.AddComponent<LayoutElement>(); le.minHeight = 120; le.preferredHeight = 120;

            GameObject icon = NewUi("Icon", row.transform);
            SetAnchors(icon.GetComponent<RectTransform>(), 0.01f, 0.1f, 0.01f, 0.9f);
            RectTransform ir = icon.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0, 0.5f); ir.anchorMax = new Vector2(0, 0.5f); ir.pivot = new Vector2(0, 0.5f);
            ir.anchoredPosition = new Vector2(14, 0); ir.sizeDelta = new Vector2(96, 96);
            Image img = icon.AddComponent<Image>(); img.sprite = def.Icon; img.preserveAspect = true; img.enabled = def.Icon != null;

            Text name = NewText("Name", row.transform, def.DisplayName, 36, Color.white, TextAnchor.MiddleLeft);
            SetAnchors(name.rectTransform, 0.14f, 0.50f, 0.64f, 0.95f);
            Text desc = NewText("Desc", row.transform, sub, 26, new Color(0.7f, 0.78f, 0.9f), TextAnchor.MiddleLeft);
            SetAnchors(desc.rectTransform, 0.14f, 0.05f, 0.64f, 0.52f);
            Text price = NewText("Price", row.transform, priceText, 34, Gold, TextAnchor.MiddleRight);
            SetAnchors(price.rectTransform, 0.62f, 0.1f, 0.80f, 0.9f);

            Button b = NewButton("Action", row.transform, actionLabel, 32, enabled ? BtnOn : BtnOff, onClick);
            SetAnchors(b.GetComponent<RectTransform>(), 0.82f, 0.18f, 0.98f, 0.82f);
            b.interactable = enabled;
        }

        // ── Helper dựng UI ─────────────────────────────────────────────────

        private static GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return go;
        }

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            GameObject go = NewUi(name, parent);
            Text t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = NewUi(name, parent);
            Image im = go.AddComponent<Image>(); im.color = color; UiSkin.Button(im, 2);
            Button b = go.AddComponent<Button>(); b.targetGraphic = im;
            if (onClick != null) b.onClick.AddListener(onClick);
            Text t = NewText("Label", go.transform, label, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return b;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
