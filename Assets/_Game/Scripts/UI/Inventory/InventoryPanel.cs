using System.Collections.Generic;
using Game.Core.Items;
using Game.PvE.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Inventory
{
    /// <summary>
    /// UI kho đồ (điện thoại + PC).
    /// Hiển thị trang bị (instance — Mặc/Tháo) và MỌI stack trong túi (tên + số lượng); chỉ loại tiêu hao có nút Dùng.
    /// bodyText (Body) trong scene chỉ dùng làm font source — bị ẩn lúc runtime.
    /// Phím tắt PC: I = bật/tắt, 1..9 = mặc, 0 = tháo hết.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text bodyText;   // font source — bị ẩn lúc runtime

        private const int   RowHeight            = 96;
        private const int   RowButtonWidth       = 200;
        private const int   UnequipAllHeight     = 96;
        private const float ListTopOffset        = 100f;
        private const float ListBottomOffset     = 110f;
        private const float RowSpacing           = 6f;
        /// <summary>Chu kỳ làm mới trạng thái nút Dùng (giây) khi panel mở.</summary>
        private const float ConsumableRefreshInterval = 0.5f;

        private const string ContainerName  = "_inventoryDynamicList";
        private const string UnequipAllName = "_inventoryUnequipAllBtn";
        private const string HeaderName     = "_inventoryHeader";

        // Pool trang bị (ItemInstance — mặc/tháo)
        private readonly List<ItemInstance> _equippable = new List<ItemInstance>();
        // Mọi stack trong túi (ItemStack): loại tiêu hao có nút Dùng, loại khác chỉ hiện tên + số lượng
        private readonly List<ItemStack>    _stacks     = new List<ItemStack>();
        private readonly List<GameObject>   _rowPool    = new List<GameObject>();

        private RectTransform        _listContainerRt;
        private ScrollRect           _listScroll;
        private RectTransform        _listContentRt;
        private VerticalLayoutGroup  _listContentVlg;
        private Button               _unequipAllBtn;
        private Text                 _headerText;

        private bool  _uiBuilt;
        private float _consumableRefreshTimer;
        private bool  _hasConsumableStack;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (playerInventory != null) playerInventory.Changed += Refresh;
            HideLegacyBodyTexts();
            EnsureDynamicUI();
            Refresh();
        }

        private void OnDisable()
        {
            if (playerInventory != null) playerInventory.Changed -= Refresh;
        }

        private void Update()
        {
            // Phím tắt PC
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.iKey.wasPressedThisFrame) Toggle();
                for (int i = 1; i <= 9; i++)
                {
                    var key = keyboard[UnityEngine.InputSystem.Key.Digit1 + (i - 1)];
                    if (key != null && key.wasPressedThisFrame) EquipByIndex(i);
                }
                if (keyboard.digit0Key.wasPressedThisFrame) UnequipAll();
            }

            // Làm mới nút Dùng định kỳ (cooldown đếm ngược, hết giao chiến) — chỉ khi có tiêu hao
            if (_uiBuilt && _hasConsumableStack)
            {
                _consumableRefreshTimer -= UnityEngine.Time.deltaTime;
                if (_consumableRefreshTimer <= 0f)
                {
                    _consumableRefreshTimer = ConsumableRefreshInterval;
                    RefreshConsumableButtons();
                }
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void Toggle()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(!panelRoot.activeSelf);
            if (panelRoot.activeSelf)
            {
                HideLegacyBodyTexts();
                EnsureDynamicUI();
                Refresh();
            }
        }

        public void EquipByIndex(int index)
        {
            if (playerInventory == null) return;
            if (index < 1 || index > _equippable.Count) return;
            playerInventory.Equip(_equippable[index - 1].instanceId);
        }

        public void UnequipAll()
        {
            if (playerInventory == null) return;
            playerInventory.Unequip(EquipmentSlot.Weapon);
            playerInventory.Unequip(EquipmentSlot.Armor);
        }

        /// <summary>Mặc/tháo trang bị theo instanceId.</summary>
        public void ToggleByInstanceId(string instanceId)
        {
            if (playerInventory == null || string.IsNullOrEmpty(instanceId)) return;
            int index = _equippable.FindIndex(it => it.instanceId == instanceId) + 1;
            if (index <= 0) return;
            if (playerInventory.IsEquipped(instanceId))
                UnequipByInstanceIdInternal(instanceId);
            else
                playerInventory.Equip(instanceId);
        }

        /// <summary>Dùng 1 vật phẩm tiêu hao. UI chỉ gửi ý định — logic nằm trong PlayerInventory.</summary>
        public void UseConsumable(string itemId)
        {
            if (playerInventory == null || string.IsNullOrEmpty(itemId)) return;
            // TryUseConsumable trả kết quả nhưng UI không xử lý — Changed sẽ fire → Refresh() tự chạy.
            playerInventory.TryUseConsumable(itemId);
        }

        private bool UnequipByInstanceIdInternal(string instanceId)
        {
            ItemInstance inst = playerInventory.Inventory.FindInstance(instanceId);
            if (inst == null) return false;
            ItemDefinition def = playerInventory.Database != null ? playerInventory.Database.Find(inst.itemId) : null;
            if (def == null || def.Slot == EquipmentSlot.None) return false;
            return playerInventory.Unequip(def.Slot);
        }

        // ── Ẩn Body text cũ từ scene ──────────────────────────────────────
        private void HideLegacyBodyTexts()
        {
            if (panelRoot == null) return;
            for (int i = 0; i < panelRoot.transform.childCount; i++)
            {
                Transform child = panelRoot.transform.GetChild(i);
                Text txt = child.GetComponent<Text>();
                if (txt != null && !child.name.StartsWith("_inventory"))
                {
                    txt.enabled = false;
                    txt.raycastTarget = false;
                }
            }
        }

        // ── Refresh ────────────────────────────────────────────────────────

        public void Refresh()
        {
            if (playerInventory == null) return;
            EnsureDynamicUI();
            if (!_uiBuilt) return;

            _equippable.Clear();
            _stacks.Clear();
            _hasConsumableStack = false;

            // Header: vũ khí + áo + bonus stats
            ItemStats bonus = playerInventory.CurrentEquipmentBonus();
            string weaponName = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Weapon));
            string armorName  = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Armor));
            if (_headerText != null)
                _headerText.text = $"Vũ khí: {weaponName}  |  Áo: {armorName}\n"
                                 + $"ATK+{bonus.attack:0.#}  DEF+{bonus.defense:0.#}  HP+{bonus.maxHealth:0.#}";

            // Thu thập trang bị (instance — Mặc/Tháo)
            foreach (ItemInstance instance in playerInventory.Inventory.Instances)
            {
                ItemDefinition def = playerInventory.Database != null
                    ? playerInventory.Database.Find(instance.itemId) : null;
                if (def != null && def.Slot != EquipmentSlot.None)
                    _equippable.Add(instance);
            }

            // Thu thập MỌI stack (tên + số lượng); chỉ loại Consumable mới có nút Dùng
            foreach (ItemStack stack in playerInventory.Inventory.Stacks)
            {
                ItemDefinition def = playerInventory.Database != null
                    ? playerInventory.Database.Find(stack.itemId) : null;
                _stacks.Add(stack);
                if (def != null && def.Type == ItemType.Consumable) _hasConsumableStack = true;
            }

            RefreshAllRows();
            _consumableRefreshTimer = 0f; // cập nhật trạng thái nút ngay trong Update kế tiếp
        }

        // ── Row management ─────────────────────────────────────────────────

        private void RefreshAllRows()
        {
            if (_listContentVlg == null || _listContentRt == null) return;
            int needed = _equippable.Count + _stacks.Count;

            while (_rowPool.Count < needed)
            {
                GameObject row = CreateRow();
                row.transform.SetParent(_listContentRt, false);
                _rowPool.Add(row);
            }

            int rowIdx = 0;

            // Phần 1: trang bị
            for (int i = 0; i < _equippable.Count; i++, rowIdx++)
            {
                _rowPool[rowIdx].SetActive(true);
                BindEquipRow(_rowPool[rowIdx], _equippable[i], rowIdx + 1);
            }

            // Phần 2: mọi stack (tiêu hao có nút Dùng, loại khác chỉ hiện tên + số lượng)
            for (int i = 0; i < _stacks.Count; i++, rowIdx++)
            {
                _rowPool[rowIdx].SetActive(true);
                BindStackRow(_rowPool[rowIdx], _stacks[i]);
            }

            // Ẩn rows thừa
            for (int i = rowIdx; i < _rowPool.Count; i++)
                _rowPool[i].SetActive(false);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listContentRt);

            if (_listContentRt != null)
                _listContentRt.anchoredPosition = new Vector2(0f, 0f);
            if (_listScroll != null)
                _listScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Chỉ làm mới trạng thái nút Dùng — không rebuild toàn bộ.</summary>
        private void RefreshConsumableButtons()
        {
            if (playerInventory == null) return;
            int equipCount = _equippable.Count;
            for (int i = 0; i < _stacks.Count; i++)
            {
                int rowIdx = equipCount + i;
                if (rowIdx >= _rowPool.Count) break;
                GameObject row = _rowPool[rowIdx];
                if (!row.activeSelf) continue;

                Button btn     = row.transform.Find("ActionBtn")?.GetComponent<Button>();
                Text   btnText = btn != null ? btn.transform.Find("Text")?.GetComponent<Text>() : null;
                if (btn == null || btnText == null || !btn.gameObject.activeSelf) continue; // dòng loại khác: không có nút Dùng

                UpdateConsumableButton(btn, btnText, _stacks[i].itemId);
            }
        }

        private void BindEquipRow(GameObject row, ItemInstance instance, int displayIndex)
        {
            ItemDefinition def = playerInventory != null && playerInventory.Database != null
                ? playerInventory.Database.Find(instance.itemId) : null;
            string displayName = def != null ? def.DisplayName : instance.itemId;
            bool equipped = playerInventory != null && playerInventory.IsEquipped(instance.instanceId);

            Text   label   = row.transform.Find("Label").GetComponent<Text>();
            Button btn     = row.transform.Find("ActionBtn").GetComponent<Button>();
            Text   btnText = btn.transform.Find("Text").GetComponent<Text>();

            label.text       = $"[{displayIndex}] {displayName}{(equipped ? "  ✓" : "")}";
            btnText.text     = equipped ? "Tháo" : "Mặc";
            btn.gameObject.SetActive(true); // hàng dùng lại từ pool: nút có thể từng bị ẩn
            btn.interactable = true;

            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
                btnImg.color = equipped
                    ? new Color(0.85f, 0.25f, 0.25f, 0.95f)   // đỏ = Tháo
                    : new Color(0.2f,  0.5f,  0.9f,  0.95f);  // xanh dương = Mặc

            string id = instance.instanceId;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ToggleByInstanceId(id));
        }

        private void BindStackRow(GameObject row, ItemStack stack)
        {
            ItemDefinition def = playerInventory != null && playerInventory.Database != null
                ? playerInventory.Database.Find(stack.itemId) : null;
            string displayName = def != null ? def.DisplayName : stack.itemId;

            Text   label   = row.transform.Find("Label").GetComponent<Text>();
            Button btn     = row.transform.Find("ActionBtn").GetComponent<Button>();
            Text   btnText = btn.transform.Find("Text").GetComponent<Text>();

            label.text = $"{displayName}  x{stack.quantity}";

            // Chỉ vật phẩm tiêu hao mới có nút Dùng; loại khác chỉ hiện tên + số lượng (ẩn nút).
            bool isConsumable = def != null && def.Type == ItemType.Consumable;
            btn.onClick.RemoveAllListeners();
            btn.gameObject.SetActive(isConsumable);
            if (!isConsumable) return;

            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
                btnImg.color = new Color(0.15f, 0.65f, 0.35f, 0.95f); // xanh lá = Dùng

            string itemId = stack.itemId;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => UseConsumable(itemId));

            UpdateConsumableButton(btn, btnText, itemId);
        }

        /// <summary>Cập nhật interactable + text nút Dùng theo trạng thái hiện tại.</summary>
        private void UpdateConsumableButton(Button btn, Text btnText, string itemId)
        {
            ConsumableUseResult state = playerInventory.GetConsumableUseState(itemId);
            bool canUse = state == ConsumableUseResult.Ok;
            btn.interactable = canUse;
            btnText.text     = canUse ? "Dùng" : ConsumableRules.DescribeResult(state);
        }

        private GameObject CreateRow()
        {
            Font font     = bodyText != null ? bodyText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            int  fontSize = bodyText != null ? Mathf.Max(24, bodyText.fontSize) : 28;

            var row = new GameObject("Row",
                typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(Image));

            row.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

            var le = row.GetComponent<LayoutElement>();
            le.minHeight       = RowHeight;
            le.preferredHeight = RowHeight;
            le.flexibleWidth   = 1f;

            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.childControlWidth      = true;
            hl.childControlHeight     = true;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.spacing = 8f;
            hl.padding = new RectOffset(10, 10, 6, 6);

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(row.transform, false);
            var lt = labelGo.GetComponent<Text>();
            lt.font               = font;
            lt.fontSize           = fontSize;
            lt.alignment          = TextAnchor.MiddleLeft;
            lt.color              = Color.white;
            lt.raycastTarget      = false;
            lt.horizontalOverflow = HorizontalWrapMode.Wrap;
            lt.verticalOverflow   = VerticalWrapMode.Truncate;
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelLe.minWidth      = 100f;

            // ActionBtn
            var btnGo = new GameObject("ActionBtn",
                typeof(RectTransform), typeof(Button), typeof(Image));
            btnGo.transform.SetParent(row.transform, false);
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 0.95f);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.minWidth       = RowButtonWidth;
            btnLe.preferredWidth = RowButtonWidth;
            btnLe.minHeight      = RowHeight - 12;
            btnLe.flexibleHeight = 1f;

            var btnColors = btnGo.GetComponent<Button>().colors;
            btnColors.normalColor      = Color.white;
            btnColors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            btnColors.pressedColor     = new Color(0.7f,  0.7f,  0.7f,  1f);
            btnColors.disabledColor    = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            btnGo.GetComponent<Button>().colors = btnColors;

            // ActionBtn > Text
            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var brt = (RectTransform)btnTextGo.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bt = btnTextGo.GetComponent<Text>();
            bt.font          = font;
            bt.fontSize      = Mathf.Max(24, fontSize);
            bt.alignment     = TextAnchor.MiddleCenter;
            bt.color         = Color.white;
            bt.raycastTarget = false;
            bt.text          = "Mặc";

            return row;
        }

        // ── EnsureDynamicUI ────────────────────────────────────────────────

        private void EnsureDynamicUI()
        {
            if (_uiBuilt &&
                _listContainerRt != null &&
                _listContentRt   != null &&
                _listContentVlg  != null)
                return;

            if (panelRoot == null) return;

            _uiBuilt = false;
            _rowPool.Clear();

            Transform panelTf = panelRoot.transform;
            Font font = bodyText != null ? bodyText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Destroy các GO cũ do code tạo (tránh duplicate sau domain reload)
            Transform existingContainer = panelTf.Find(ContainerName);
            if (existingContainer != null)
                UnityEngine.Object.DestroyImmediate(existingContainer.gameObject);

            Transform existingBtn = panelTf.Find(UnequipAllName);
            if (existingBtn != null)
                UnityEngine.Object.DestroyImmediate(existingBtn.gameObject);

            Transform existingHeader = panelTf.Find(HeaderName);
            if (existingHeader != null)
                UnityEngine.Object.DestroyImmediate(existingHeader.gameObject);

            // ── Header ──────────────────────────────────────────────────────
            var headerGo = new GameObject(HeaderName, typeof(RectTransform), typeof(Text));
            headerGo.transform.SetParent(panelTf, false);
            var headerRt = (RectTransform)headerGo.transform;
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot     = new Vector2(0.5f, 1f);
            headerRt.offsetMin = new Vector2(12, -ListTopOffset);
            headerRt.offsetMax = new Vector2(-12, -4);
            _headerText               = headerGo.GetComponent<Text>();
            _headerText.font          = font;
            _headerText.fontSize      = bodyText != null ? Mathf.Max(22, bodyText.fontSize) : 22;
            _headerText.alignment     = TextAnchor.UpperLeft;
            _headerText.color         = Color.white;
            _headerText.raycastTarget = false;
            _headerText.text          = "Vũ khí: (trống)";

            // ── Scroll container ────────────────────────────────────────────
            var containerGo = new GameObject(ContainerName, typeof(RectTransform), typeof(Image));
            containerGo.transform.SetParent(panelTf, false);
            _listContainerRt           = (RectTransform)containerGo.transform;
            _listContainerRt.anchorMin = new Vector2(0, 0);
            _listContainerRt.anchorMax = new Vector2(1, 1);
            _listContainerRt.pivot     = new Vector2(0.5f, 0.5f);
            _listContainerRt.offsetMin = new Vector2(12,  ListBottomOffset);
            _listContainerRt.offsetMax = new Vector2(-12, -ListTopOffset);
            containerGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

            // Viewport — RectMask2D không cần Image alpha=0
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(_listContainerRt, false);
            var viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(4, 4);
            viewportRt.offsetMax = new Vector2(-4, -4);

            // Content
            var contentGo = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportRt, false);
            _listContentRt           = (RectTransform)contentGo.transform;
            _listContentRt.anchorMin = new Vector2(0, 1);
            _listContentRt.anchorMax = new Vector2(1, 1);
            _listContentRt.pivot     = new Vector2(0.5f, 1f);
            _listContentRt.sizeDelta = new Vector2(0, 0);

            _listContentVlg = contentGo.GetComponent<VerticalLayoutGroup>();
            _listContentVlg.childControlWidth      = true;
            _listContentVlg.childControlHeight     = true;
            _listContentVlg.childForceExpandWidth  = true;
            _listContentVlg.childForceExpandHeight = false;
            _listContentVlg.spacing = RowSpacing;
            _listContentVlg.padding = new RectOffset(6, 6, 6, 6);

            var csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            _listScroll                  = containerGo.AddComponent<ScrollRect>();
            _listScroll.viewport         = viewportRt;
            _listScroll.content          = _listContentRt;
            _listScroll.horizontal       = false;
            _listScroll.vertical         = true;
            _listScroll.movementType     = ScrollRect.MovementType.Clamped;
            _listScroll.scrollSensitivity = 32f;

            // ── Nút Tháo hết ────────────────────────────────────────────────
            var unequipGo = new GameObject(UnequipAllName,
                typeof(RectTransform), typeof(Button), typeof(Image));
            unequipGo.transform.SetParent(panelTf, false);
            _unequipAllBtn = unequipGo.GetComponent<Button>();
            var unequipRt = (RectTransform)unequipGo.transform;
            unequipRt.anchorMin = new Vector2(0, 0);
            unequipRt.anchorMax = new Vector2(1, 0);
            unequipRt.pivot     = new Vector2(0.5f, 0f);
            unequipRt.offsetMin = new Vector2(12, 8);
            unequipRt.offsetMax = new Vector2(-12, 8 + UnequipAllHeight);
            unequipGo.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f, 0.95f);

            var utGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            utGo.transform.SetParent(unequipGo.transform, false);
            var utRt = (RectTransform)utGo.transform;
            utRt.anchorMin = Vector2.zero;
            utRt.anchorMax = Vector2.one;
            utRt.offsetMin = Vector2.zero;
            utRt.offsetMax = Vector2.zero;
            var ut = utGo.GetComponent<Text>();
            ut.font          = font;
            ut.fontSize      = bodyText != null ? Mathf.Max(28, bodyText.fontSize) : 28;
            ut.alignment     = TextAnchor.MiddleCenter;
            ut.color         = Color.white;
            ut.raycastTarget = false;
            ut.text          = "Tháo hết";

            var uc = _unequipAllBtn.colors;
            uc.normalColor      = Color.white;
            uc.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            uc.pressedColor     = new Color(0.7f,  0.7f,  0.7f,  1f);
            _unequipAllBtn.colors = uc;
            _unequipAllBtn.onClick.RemoveAllListeners();
            _unequipAllBtn.onClick.AddListener(UnequipAll);

            _uiBuilt = true;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private string Describe(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || playerInventory == null) return "(trống)";
            ItemInstance inst = playerInventory.Inventory.FindInstance(instanceId);
            if (inst == null) return "(trống)";
            ItemDefinition def = playerInventory.Database != null
                ? playerInventory.Database.Find(inst.itemId) : null;
            return def != null ? def.DisplayName : inst.itemId;
        }
    }
}
