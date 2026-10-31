import sys

with open('Assets/_Game/Scripts/UI/Inventory/InventoryPanel.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update Header text formatting
old_header = """            // Header: vũ khí + áo + bonus stats
            ItemStats bonus = playerInventory.CurrentEquipmentBonus();
            string weaponName = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Weapon));
            string armorName  = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Armor));
            if (_headerText != null)
                _headerText.text = $"Vũ khí: {weaponName}  |  Áo: {armorName}\\n"
                                 + $"ATK+{bonus.attack:0.#}  DEF+{bonus.defense:0.#}  HP+{bonus.maxHealth:0.#}";"""

new_header = """            // Header: vũ khí + áo + bonus stats
            ItemStats bonus = playerInventory.CurrentEquipmentBonus();
            string weaponName = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Weapon));
            string armorName  = Describe(playerInventory.Equipment.GetInstanceId(EquipmentSlot.Armor));
            if (_headerText != null)
            {
                _headerText.supportRichText = true;
                _headerText.text = $"<color=#e2b63f>Vũ khí:</color> {weaponName}   <color=#e2b63f>Áo:</color> {armorName}\\n"
                                 + $"<color=#91b1d4>Tổng chỉ số:</color>  ATK +{bonus.attack:0.#}  •  DEF +{bonus.defense:0.#}  •  HP +{bonus.maxHealth:0.#}";
            }"""

if old_header not in code:
    # Try normalized newlines
    code_norm = code.replace('\r\n', '\n')
    old_header_norm = old_header.replace('\r\n', '\n')
    if old_header_norm in code_norm:
        code = code_norm.replace(old_header_norm, new_header.replace('\r\n', '\n'))
        print("Replaced header (normalized newlines)")
    else:
        print("ERROR: old_header not found!")
        sys.exit(1)
else:
    code = code.replace(old_header, new_header)
    print("Replaced header directly")

# 2. Add DescribeItem and replace BindEquipRow, BindStackRow, CreateRow
old_rows_section = """        private void BindEquipRow(GameObject row, ItemInstance instance, int displayIndex)
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
            Font font     = (bodyText != null ? bodyText.font : null) ?? UiSkin.DefaultFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            int  fontSize = bodyText != null ? Mathf.Max(28, bodyText.fontSize) : 28;

            var row = new GameObject("Row",
                typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(Image));

            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f); UiSkin.Row(row.GetComponent<Image>());

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
            UiSkin.ApplyFont(lt);
            lt.fontSize           = Mathf.Max(24, fontSize);
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
            btnGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 0.95f); UiSkin.Button(btnGo.GetComponent<Image>());
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
            UiSkin.ApplyFont(bt, bold: true);
            bt.fontSize      = Mathf.Max(24, fontSize);
            bt.alignment     = TextAnchor.MiddleCenter;
            bt.color         = Color.white;
            bt.raycastTarget = false;
            bt.text          = "Mặc";

            return row;
        }"""

new_rows_section = """        private static string DescribeItem(ItemDefinition d)
        {
            if (d == null) return "Vật phẩm";
            var parts = new List<string>();
            if (d.Type == ItemType.Weapon)
                parts.Add(d.Tier > 0 ? $"Vũ khí (Bậc {d.Tier})" : "Vũ khí");
            else if (d.Type == ItemType.Armor)
                parts.Add(d.Tier > 0 ? $"Giáp (Bậc {d.Tier})" : "Giáp");
            else if (d.Type == ItemType.Consumable)
                parts.Add("Tiêu hao");
            else if (d.Type == ItemType.Material)
                parts.Add("Nguyên liệu");
            else
                parts.Add("Vật phẩm");

            if (d.BaseStats.attack > 0) parts.Add($"ATK +{d.BaseStats.attack:0.#}");
            if (d.BaseStats.defense > 0) parts.Add($"DEF +{d.BaseStats.defense:0.#}");
            if (d.BaseStats.maxHealth > 0) parts.Add($"HP +{d.BaseStats.maxHealth:0.#}");
            if (d.HealAmount > 0) parts.Add($"Hồi {d.HealAmount:0.#} HP");
            if (d.UseCooldownSeconds > 0f) parts.Add($"CD {d.UseCooldownSeconds:0.#}s");

            return string.Join("  •  ", parts);
        }

        private void BindEquipRow(GameObject row, ItemInstance instance, int displayIndex)
        {
            ItemDefinition def = playerInventory != null && playerInventory.Database != null
                ? playerInventory.Database.Find(instance.itemId) : null;
            string displayName = def != null ? def.DisplayName : instance.itemId;
            bool equipped = playerInventory != null && playerInventory.IsEquipped(instance.instanceId);

            // Icon
            Image iconImg = row.transform.Find("SlotFrame/Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = def != null ? def.Icon : null;
                iconImg.enabled = iconImg.sprite != null;
                iconImg.preserveAspect = true;
            }

            // Qty badge
            Text qtyText = row.transform.Find("SlotFrame/QtyBadge")?.GetComponent<Text>();
            if (qtyText != null) qtyText.text = "";

            // Name & Desc
            Text nameText = row.transform.Find("InfoColumn/NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.supportRichText = true;
                nameText.text = equipped
                    ? $"[{displayIndex}] {displayName}  <color=#4ee44e>[Đang mặc ✓]</color>"
                    : $"[{displayIndex}] {displayName}";
            }

            Text descText = row.transform.Find("InfoColumn/DescText")?.GetComponent<Text>();
            if (descText != null)
            {
                descText.text = DescribeItem(def);
            }

            // ActionBtn
            Button btn = row.transform.Find("ActionBtn")?.GetComponent<Button>();
            Text btnText = btn != null ? btn.transform.Find("Text")?.GetComponent<Text>() : null;
            if (btn != null && btnText != null)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;
                btnText.text = equipped ? "Tháo" : "Mặc";

                Image btnImg = btn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = equipped
                        ? new Color(0.85f, 0.30f, 0.30f, 0.98f)   // Đỏ = Tháo
                        : new Color(0.20f, 0.58f, 0.75f, 0.98f);  // Xanh dương = Mặc
                }

                string id = instance.instanceId;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ToggleByInstanceId(id));
            }
        }

        private void BindStackRow(GameObject row, ItemStack stack)
        {
            ItemDefinition def = playerInventory != null && playerInventory.Database != null
                ? playerInventory.Database.Find(stack.itemId) : null;
            string displayName = def != null ? def.DisplayName : stack.itemId;

            // Icon
            Image iconImg = row.transform.Find("SlotFrame/Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = def != null ? def.Icon : null;
                iconImg.enabled = iconImg.sprite != null;
                iconImg.preserveAspect = true;
            }

            // Qty badge
            Text qtyText = row.transform.Find("SlotFrame/QtyBadge")?.GetComponent<Text>();
            if (qtyText != null)
            {
                qtyText.text = stack.quantity > 1 ? $"x{stack.quantity}" : "";
            }

            // Name & Desc
            Text nameText = row.transform.Find("InfoColumn/NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.supportRichText = true;
                nameText.text = $"{displayName}  <color=#ffd255>x{stack.quantity}</color>";
            }

            Text descText = row.transform.Find("InfoColumn/DescText")?.GetComponent<Text>();
            if (descText != null)
            {
                descText.text = DescribeItem(def);
            }

            // ActionBtn
            Button btn = row.transform.Find("ActionBtn")?.GetComponent<Button>();
            Text btnText = btn != null ? btn.transform.Find("Text")?.GetComponent<Text>() : null;
            if (btn != null)
            {
                bool isConsumable = def != null && def.Type == ItemType.Consumable;
                btn.gameObject.SetActive(isConsumable);
                btn.onClick.RemoveAllListeners();

                if (isConsumable)
                {
                    Image btnImg = btn.GetComponent<Image>();
                    if (btnImg != null)
                        btnImg.color = new Color(0.18f, 0.68f, 0.40f, 0.98f); // Xanh lá = Dùng

                    string itemId = stack.itemId;
                    btn.onClick.AddListener(() => UseConsumable(itemId));

                    if (btnText != null)
                        UpdateConsumableButton(btn, btnText, itemId);
                }
            }
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
            Font font = (bodyText != null ? bodyText.font : null) ?? UiSkin.DefaultFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            var row = new GameObject("Row",
                typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement), typeof(Image));

            var rowImg = row.GetComponent<Image>();
            rowImg.color = Color.white;
            UiSkin.Row(rowImg);

            var le = row.GetComponent<LayoutElement>();
            le.minHeight       = RowHeight;
            le.preferredHeight = RowHeight;
            le.flexibleWidth   = 1f;

            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.childControlWidth      = false;
            hl.childControlHeight     = false;
            hl.childForceExpandWidth  = false;
            hl.childForceExpandHeight = false;
            hl.spacing = 14f;
            hl.padding = new RectOffset(12, 12, 10, 10);
            hl.childAlignment = TextAnchor.MiddleLeft;

            // 1. SlotFrame (chứa icon + badge số lượng)
            var slotGo = new GameObject("SlotFrame",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotGo.transform.SetParent(row.transform, false);
            var slotRt = (RectTransform)slotGo.transform;
            slotRt.sizeDelta = new Vector2(112f, 112f);
            var slotLe = slotGo.GetComponent<LayoutElement>();
            slotLe.minWidth = 112f;
            slotLe.preferredWidth = 112f;
            slotLe.minHeight = 112f;
            slotLe.preferredHeight = 112f;
            var slotImg = slotGo.GetComponent<Image>();
            slotImg.color = Color.white;
            UiSkin.Slot(slotImg);

            // 1.1 Icon bên trong slot
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(10f, 10f);
            iconRt.offsetMax = new Vector2(-10f, -10f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 1.2 QtyBadge góc dưới phải của ô
            var qtyGo = new GameObject("QtyBadge", typeof(RectTransform), typeof(Text), typeof(Outline));
            qtyGo.transform.SetParent(slotGo.transform, false);
            var qtyRt = (RectTransform)qtyGo.transform;
            qtyRt.anchorMin = new Vector2(0f, 0f);
            qtyRt.anchorMax = new Vector2(1f, 0f);
            qtyRt.pivot     = new Vector2(1f, 0f);
            qtyRt.anchoredPosition = new Vector2(0f, 4f);
            qtyRt.sizeDelta = new Vector2(0f, 26f);
            var qtyText = qtyGo.GetComponent<Text>();
            UiSkin.ApplyFont(qtyText, bold: true);
            qtyText.fontSize = 22;
            qtyText.alignment = TextAnchor.LowerRight;
            qtyText.color = Color.white;
            qtyText.raycastTarget = false;
            var qtyOutline = qtyGo.GetComponent<Outline>();
            qtyOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            qtyOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // 2. InfoColumn (cột thông tin: tên + chỉ số mô tả)
            var infoGo = new GameObject("InfoColumn",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            infoGo.transform.SetParent(row.transform, false);
            var infoLe = infoGo.GetComponent<LayoutElement>();
            infoLe.flexibleWidth = 1f;
            infoLe.minHeight = 112f;

            var vl = infoGo.GetComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.spacing = 6f;
            vl.childAlignment = TextAnchor.MiddleLeft;

            // 2.1 NameText
            var nameGo = new GameObject("NameText", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(infoGo.transform, false);
            var nameText = nameGo.GetComponent<Text>();
            UiSkin.ApplyFont(nameText, bold: true);
            nameText.fontSize = 32;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.preferredHeight = 42f;

            // 2.2 DescText
            var descGo = new GameObject("DescText", typeof(RectTransform), typeof(Text));
            descGo.transform.SetParent(infoGo.transform, false);
            var descText = descGo.GetComponent<Text>();
            UiSkin.ApplyFont(descText);
            descText.fontSize = 24;
            descText.alignment = TextAnchor.MiddleLeft;
            descText.color = new Color(0.60f, 0.78f, 0.95f);
            descText.raycastTarget = false;
            var descLe = descGo.AddComponent<LayoutElement>();
            descLe.preferredHeight = 36f;

            // 3. ActionBtn (nút Mặc / Tháo / Dùng)
            var btnGo = new GameObject("ActionBtn",
                typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
            btnGo.transform.SetParent(row.transform, false);
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.20f, 0.58f, 0.75f, 0.98f);
            UiSkin.Button(btnImg);

            var btnRt = (RectTransform)btnGo.transform;
            btnRt.sizeDelta = new Vector2(RowButtonWidth, 88f);
            var btnLe = btnGo.GetComponent<LayoutElement>();
            btnLe.minWidth       = RowButtonWidth;
            btnLe.preferredWidth = RowButtonWidth;
            btnLe.minHeight      = 88f;
            btnLe.preferredHeight = 88f;

            var btnColors = btnGo.GetComponent<Button>().colors;
            btnColors.normalColor      = Color.white;
            btnColors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            btnColors.pressedColor     = new Color(0.7f,  0.7f,  0.7f,  1f);
            btnColors.disabledColor    = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            btnGo.GetComponent<Button>().colors = btnColors;

            // 3.1 ActionBtn > Text
            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var brt = (RectTransform)btnTextGo.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bt = btnTextGo.GetComponent<Text>();
            UiSkin.ApplyFont(bt, bold: true);
            bt.fontSize      = 28;
            bt.alignment     = TextAnchor.MiddleCenter;
            bt.color         = Color.white;
            bt.raycastTarget = false;
            bt.text          = "Mặc";

            return row;
        }"""

code_norm = code.replace('\r\n', '\n')
old_rows_norm = old_rows_section.replace('\r\n', '\n')
if old_rows_norm in code_norm:
    code_norm = code_norm.replace(old_rows_norm, new_rows_section.replace('\r\n', '\n'))
    print("Replaced rows section")
else:
    print("ERROR: old_rows_section not found!")
    sys.exit(1)

# 3. Update EnsureDynamicUI to ensure panelRoot has Color.white and listContainer uses Inset
old_dynamic_ui = """            Font font = (bodyText != null ? bodyText.font : null) ?? UiSkin.DefaultFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var pImg = panelRoot.GetComponent<Image>();
            if (pImg != null) UiSkin.Panel(pImg);"""

new_dynamic_ui = """            Font font = (bodyText != null ? bodyText.font : null) ?? UiSkin.DefaultFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var pImg = panelRoot.GetComponent<Image>();
            if (pImg != null)
            {
                pImg.color = Color.white;
                UiSkin.Panel(pImg);
            }"""

if old_dynamic_ui.replace('\r\n', '\n') in code_norm:
    code_norm = code_norm.replace(old_dynamic_ui.replace('\r\n', '\n'), new_dynamic_ui.replace('\r\n', '\n'))
    print("Replaced dynamic UI panel image color")

# Also update list container color
old_container = """            containerGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);"""
new_container = """            var cImg = containerGo.GetComponent<Image>();
            cImg.color = Color.white;
            UiSkin.Inset(cImg);"""

if old_container.replace('\r\n', '\n') in code_norm:
    code_norm = code_norm.replace(old_container.replace('\r\n', '\n'), new_container.replace('\r\n', '\n'))
    print("Replaced list container image with Inset")

# Restore original Windows CRLF
code_final = code_norm.replace('\n', '\r\n')
with open('Assets/_Game/Scripts/UI/Inventory/InventoryPanel.cs', 'w', encoding='utf-8') as f:
    f.write(code_final)

print("InventoryPanel.cs updated successfully!")
