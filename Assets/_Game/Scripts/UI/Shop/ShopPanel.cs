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
    ///
    /// Bố cục: hộp giữa màn hình 1100x860 (PanelSizeClamp tự co), tiêu đề + số Col ở đỉnh, lời thoại, hai tab,
    /// danh sách cuộn, nút Đóng ở đáy. Mỗi hàng dùng HorizontalLayoutGroup (ô icon | tên + mô tả | giá | nút)
    /// nên chữ không còn chồng lên nhau khi tên dài.
    /// </summary>
    public class ShopPanel : MonoBehaviour, IExclusivePanel, IPanelRoot
    {
        public const int Priority = 35;

        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerWallet wallet;
        [Tooltip("Tự đóng khi player đi xa NPC hơn khoảng này (đơn vị thế giới).")]
        [SerializeField] private float closeDistance = 6f;

        private static readonly Vector2 BoxDesignSize = new Vector2(1100f, 860f);
        private static readonly Color Gold = new Color(0.95f, 0.76f, 0.31f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.13f, 0.97f);
        private static readonly Color BtnOn = new Color(0.20f, 0.60f, 0.52f, 1f);
        private static readonly Color BtnOff = new Color(0.34f, 0.34f, 0.40f, 1f);
        private static readonly Color TabOn = new Color(0.86f, 0.62f, 0.22f, 1f);
        private static readonly Color TabOff = new Color(0.26f, 0.32f, 0.44f, 1f);
        private static readonly Color Danger = new Color(0.86f, 0.30f, 0.30f, 1f);
        private static readonly Color Muted = new Color(0.72f, 0.80f, 0.92f, 1f);
        private static readonly Color PriceBad = new Color(1f, 0.50f, 0.45f, 1f);

        private Font _font;
        private Font _boldFont;
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
        public GameObject PanelRoot => _root;
        public void CloseFromBack() => Close();
        public void CloseExclusive() => Close();

        private void Awake()
        {
            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _font = UiSkin.DefaultFont != null ? UiSkin.DefaultFont : builtin;
            _boldFont = UiSkin.BoldFont != null ? UiSkin.BoldFont : _font;
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
            BackKeyRouterHost.Router.CloseExclusiveOthers(this);
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

            // Hộp giữa màn hình: neo giữa + PanelSizeClamp giữ kích thước, tự co trên màn hẹp.
            GameObject border = NewUi("Border", _root.transform);
            RectTransform br = border.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = BoxDesignSize;
            border.AddComponent<Game.UI.Common.PanelSizeClamp>().Configure(BoxDesignSize, 1100f);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = Gold;
            bool skinned = UiSkin.Panel(borderImg, 3);

            GameObject win = NewUi("Window", border.transform);
            RectTransform wr = win.GetComponent<RectTransform>();
            Stretch(wr);
            Image winImg = win.AddComponent<Image>();
            winImg.raycastTarget = false;
            if (skinned)
            {
                winImg.color = Color.clear;
                wr.offsetMin = new Vector2(26f, 26f);
                wr.offsetMax = new Vector2(-26f, -26f);
            }
            else
            {
                winImg.color = Ink;
                wr.offsetMin = new Vector2(4f, 4f);
                wr.offsetMax = new Vector2(-4f, -4f);
            }

            // Đỉnh: tiêu đề (trái) + số Col (phải, chừa chỗ nút X)
            _title = NewText("Title", win.transform, "Cửa hàng", 48, Gold, TextAnchor.MiddleLeft, true);
            Shadow(_title);
            TopStretch(_title.rectTransform, 24f, 380f, 14f, 84f);

            GameObject pill = NewUi("ColPill", win.transform);
            RectTransform pr = pill.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(1f, 1f);
            pr.anchoredPosition = new Vector2(-106f, -22f);
            pr.sizeDelta = new Vector2(300f, 68f);
            Image pillImg = pill.AddComponent<Image>();
            pillImg.raycastTarget = false;
            if (!UiSkin.Toast(pillImg, 2)) pillImg.color = new Color(0f, 0f, 0f, 0.4f);

            if (UiSkin.CoinIcon != null)
            {
                GameObject coin = NewUi("Coin", pill.transform);
                RectTransform cr = coin.GetComponent<RectTransform>();
                cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0f, 0.5f);
                cr.anchoredPosition = new Vector2(18f, 0f);
                cr.sizeDelta = new Vector2(44f, 44f);
                Image ci = coin.AddComponent<Image>();
                ci.sprite = UiSkin.CoinIcon;
                ci.preserveAspect = true;
                ci.raycastTarget = false;
            }
            _colText = NewText("Col", pill.transform, "0 Col", 36, Color.white, TextAnchor.MiddleRight, true);
            _colText.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform colRt = _colText.rectTransform;
            colRt.anchorMin = Vector2.zero;
            colRt.anchorMax = Vector2.one;
            colRt.offsetMin = new Vector2(70f, 0f);
            colRt.offsetMax = new Vector2(-18f, 0f);

            // Nút X góc trên phải của hộp
            Button topClose = NewButton("TopClose", border.transform, "X", 40, Danger, Close);
            RectTransform tcRt = topClose.GetComponent<RectTransform>();
            tcRt.anchorMin = tcRt.anchorMax = tcRt.pivot = new Vector2(1f, 1f);
            tcRt.anchoredPosition = new Vector2(-24f, -24f);
            tcRt.sizeDelta = new Vector2(92f, 92f);

            // Lời thoại NPC
            _message = NewText("Message", win.transform, "", 28, new Color(0.85f, 0.88f, 0.95f), TextAnchor.UpperLeft, false);
            _message.resizeTextForBestFit = true;
            _message.resizeTextMinSize = 22;
            _message.resizeTextMaxSize = 28;
            TopStretch(_message.rectTransform, 24f, 24f, 106f, 72f);

            // Hai tab Mua / Bán
            _buyTab = NewButton("BuyTab", win.transform, "MUA", 34, TabOn, () => { _sellMode = false; Refresh(); });
            TopLeft(_buyTab.GetComponent<RectTransform>(), 24f, 188f, 210f, 72f);
            _sellTab = NewButton("SellTab", win.transform, "BÁN", 34, TabOff, () => { _sellMode = true; Refresh(); });
            TopLeft(_sellTab.GetComponent<RectTransform>(), 244f, 188f, 210f, 72f);

            // Danh sách cuộn
            GameObject scrollGo = NewUi("Scroll", win.transform);
            RectTransform sr = scrollGo.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(16f, 112f);
            sr.offsetMax = new Vector2(-16f, -272f);
            Image scrollImg = scrollGo.AddComponent<Image>();
            if (!UiSkin.Inset(scrollImg, 2)) scrollImg.color = new Color(0f, 0f, 0f, 0.25f);
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<RectMask2D>();

            GameObject content = NewUi("Content", scrollGo.transform);
            _content = content.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = new Vector2(10, 0); _content.offsetMax = new Vector2(-10, 0);
            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.padding = new RectOffset(0, 0, 10, 10);
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

            // Nút Đóng ở đáy
            Button close = NewButton("Close", win.transform, "ĐÓNG", 36, Danger, Close);
            RectTransform clr = close.GetComponent<RectTransform>();
            clr.anchorMin = clr.anchorMax = clr.pivot = new Vector2(0.5f, 0f);
            clr.anchoredPosition = new Vector2(0f, 20f);
            clr.sizeDelta = new Vector2(320f, 76f);
        }

        // ── Làm mới danh sách ──────────────────────────────────────────────

        private void Refresh()
        {
            if (_shop == null) return;
            RefreshColLabel();
            SetTab(_buyTab, !_sellMode);
            SetTab(_sellTab, _sellMode);

            for (int i = _content.childCount - 1; i >= 0; i--) Destroy(_content.GetChild(i).gameObject);

            if (!_sellMode) BuildBuyRows(); else BuildSellRows();
            if (_content.parent != null && _content.parent.TryGetComponent(out ScrollRect sr)) sr.verticalNormalizedPosition = 1f;
        }

        private static void SetTab(Button tab, bool active)
        {
            tab.GetComponent<Image>().color = active ? TabOn : TabOff;
            Text label = tab.GetComponentInChildren<Text>();
            if (label != null) label.color = active ? new Color(0.14f, 0.09f, 0.03f, 1f) : Color.white;
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
                AddRow(e.item, Describe(e.item), e.price + " Col", affordable, "Mua", affordable, () =>
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
                AddRow(def, equipped ? "Đang mặc — tháo ra mới bán được" : Describe(def), price + " Col", true, "Bán", !equipped, () =>
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
                AddRow(def, "Đang có: " + playerInventory.Inventory.GetStack(id), price + " Col", true, "Bán 1", true, () =>
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

        /// <summary>Một hàng: [ô icon] [tên / mô tả] [giá] [nút]. Dùng layout group nên không bao giờ chồng chữ.</summary>
        private void AddRow(ItemDefinition def, string sub, string priceText, bool priceOk, string actionLabel, bool enabled, UnityEngine.Events.UnityAction onClick)
        {
            GameObject row = NewUi("Row", _content);
            Image rowImg = row.AddComponent<Image>();
            rowImg.raycastTarget = false;
            if (!UiSkin.Row(rowImg, 3)) rowImg.color = new Color(0.14f, 0.16f, 0.24f, 1f);
            else rowImg.color = enabled ? Color.white : new Color(0.72f, 0.74f, 0.82f, 1f);
            LayoutElement le = row.AddComponent<LayoutElement>(); le.minHeight = 112; le.preferredHeight = 112;
            HorizontalLayoutGroup h = row.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 18, 10, 10);
            h.spacing = 16;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            // Ô icon
            GameObject slot = NewUi("IconSlot", row.transform);
            Image slotImg = slot.AddComponent<Image>();
            slotImg.raycastTarget = false;
            if (!UiSkin.Slot(slotImg, 2)) slotImg.color = new Color(0f, 0f, 0f, 0.35f);
            LayoutElement sle = slot.AddComponent<LayoutElement>(); sle.preferredWidth = 88; sle.preferredHeight = 88; sle.minWidth = 88; sle.minHeight = 88;
            GameObject icon = NewUi("Icon", slot.transform);
            RectTransform ir = icon.GetComponent<RectTransform>();
            Stretch(ir); ir.offsetMin = new Vector2(10, 10); ir.offsetMax = new Vector2(-10, -10);
            Image img = icon.AddComponent<Image>(); img.sprite = def.Icon; img.preserveAspect = true; img.enabled = def.Icon != null; img.raycastTarget = false;

            // Tên + mô tả
            GameObject info = NewUi("Info", row.transform);
            VerticalLayoutGroup v = info.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2; v.childAlignment = TextAnchor.MiddleLeft;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            LayoutElement ile = info.AddComponent<LayoutElement>(); ile.flexibleWidth = 1; ile.minWidth = 200;
            Text name = NewText("Name", info.transform, def.DisplayName, 36, Color.white, TextAnchor.MiddleLeft, true);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text desc = NewText("Desc", info.transform, sub, 26, Muted, TextAnchor.MiddleLeft, false);
            desc.resizeTextForBestFit = true; desc.resizeTextMinSize = 20; desc.resizeTextMaxSize = 26;

            // Giá
            GameObject priceBox = NewUi("PriceBox", row.transform);
            LayoutElement ple = priceBox.AddComponent<LayoutElement>(); ple.preferredWidth = 190; ple.minWidth = 150;
            HorizontalLayoutGroup ph = priceBox.AddComponent<HorizontalLayoutGroup>();
            ph.spacing = 8; ph.childAlignment = TextAnchor.MiddleRight;
            ph.childControlWidth = true; ph.childControlHeight = true;
            ph.childForceExpandWidth = false; ph.childForceExpandHeight = false;
            if (UiSkin.CoinIcon != null)
            {
                GameObject coin = NewUi("Coin", priceBox.transform);
                Image ci = coin.AddComponent<Image>(); ci.sprite = UiSkin.CoinIcon; ci.preserveAspect = true; ci.raycastTarget = false;
                LayoutElement cle = coin.AddComponent<LayoutElement>(); cle.preferredWidth = 36; cle.preferredHeight = 36;
            }
            Text price = NewText("Price", priceBox.transform, priceText, 34, priceOk ? Gold : PriceBad, TextAnchor.MiddleRight, true);
            price.horizontalOverflow = HorizontalWrapMode.Overflow;
            price.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Nút hành động
            Button b = NewButton("Action", row.transform, actionLabel, 32, enabled ? BtnOn : BtnOff, onClick);
            LayoutElement ble = b.gameObject.AddComponent<LayoutElement>(); ble.preferredWidth = 170; ble.minWidth = 170; ble.preferredHeight = 76; ble.minHeight = 76;
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

        private Text NewText(string name, Transform parent, string text, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            GameObject go = NewUi(name, parent);
            Text t = go.AddComponent<Text>();
            t.font = bold ? _boldFont : _font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private Button NewButton(string name, Transform parent, string label, int size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = NewUi(name, parent);
            Image im = go.AddComponent<Image>(); im.color = color; UiSkin.Button(im, 2);
            Button b = go.AddComponent<Button>(); b.targetGraphic = im;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cb.disabledColor = new Color(0.62f, 0.62f, 0.66f, 0.85f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            Text t = NewText("Label", go.transform, label, size, Color.white, TextAnchor.MiddleCenter, true);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow(t);
            Stretch(t.rectTransform);
            return b;
        }

        private static void Shadow(Text t)
        {
            var s = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.65f);
            s.effectDistance = new Vector2(2f, -2f);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        /// <summary>Neo đỉnh, kéo ngang: left/right là lề, top là khoảng cách từ mép trên, height là chiều cao.</summary>
        private static void TopStretch(RectTransform rt, float left, float right, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height)); rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Neo góc trên trái với kích thước cố định.</summary>
        private static void TopLeft(RectTransform rt, float x, float top, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -top);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
