using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CItems = Game.Core.Items;
using PItems = Game.PvE.Items;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class InventoryPanelRuntimeDiag
    {
        public static string Run()
        {
            var sb = new StringBuilder();

            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL: InventoryPanel not found anywhere in scene";
            sb.Append("Found InventoryPanel on: ").Append(invPanel.gameObject.name).Append(" path: ").AppendLine(GetPath(invPanel.gameObject));

            var pInv = Object.FindFirstObjectByType<PItems.PlayerInventory>();
            sb.Append("PlayerInventory found? ").Append(pInv != null).AppendLine();

            var tIP = typeof(UIInv.InventoryPanel);
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;

            bool uiBuilt = (bool)(GetField(tIP, "_uiBuilt", invPanel) ?? false);
            var playerInventory = GetField(tIP, "playerInventory", invPanel) as PItems.PlayerInventory;
            var panelRoot = GetField(tIP, "panelRoot", invPanel) as GameObject;
            var bodyText = GetField(tIP, "bodyText", invPanel) as Text;
            var equippable = GetField(tIP, "_equippable", invPanel) as List<CItems.ItemInstance>;
            var rowPool = GetField(tIP, "_rowPool", invPanel) as List<GameObject>;
            var listContentVlg = GetField(tIP, "_listContentVlg", invPanel) as VerticalLayoutGroup;
            var listContainerRt = GetField(tIP, "_listContainerRt", invPanel) as RectTransform;
            var listScroll = GetField(tIP, "_listScroll", invPanel) as ScrollRect;
            var listContentRt = GetField(tIP, "_listContentRt", invPanel) as RectTransform;
            var unequipAllBtn = GetField(tIP, "_unequipAllBtn", invPanel) as Button;

            sb.AppendLine().AppendLine("---- InventoryPanel state ----");
            sb.Append("  _uiBuilt = ").AppendLine(uiBuilt.ToString());
            sb.Append("  playerInventory ref null? ").Append(playerInventory == null).AppendLine();
            string panelRootInfo = panelRoot == null ? "NULL" : (panelRoot.name + " active=" + panelRoot.activeSelf);
            sb.Append("  panelRoot = ").AppendLine(panelRootInfo);
            sb.Append("  bodyText null? ").Append(bodyText == null).AppendLine();
            sb.Append("  _equippable.Count = ").Append(equippable?.Count ?? -1).AppendLine();
            if (equippable != null) foreach (var e in equippable) sb.Append("    * equippable item: ").AppendLine(e.itemId);
            sb.Append("  _rowPool.Count = ").Append(rowPool?.Count ?? -1).AppendLine();
            if (rowPool != null)
            {
                int a = 0;
                foreach (var r in rowPool)
                {
                    if (r.activeSelf) a++;
                    RectTransform rt = r.GetComponent<RectTransform>();
                    string parentName = r.transform.parent != null ? r.transform.parent.name : "ORPHAN";
                    sb.Append("    pool row: ").Append(r.name).Append(" active=").Append(r.activeSelf).Append(" parent=").Append(parentName);
                    if (rt != null) sb.Append(" sizeDelta=").Append(rt.sizeDelta).Append(" anchoredPos=").Append(rt.anchoredPosition);
                    sb.AppendLine();
                }
                sb.Append("    TOTAL active rows in pool = ").Append(a).Append(" / ").Append(rowPool.Count).AppendLine();
            }
            string lcInfo = listContainerRt == null ? "NULL" : ("name=" + listContainerRt.name);
            sb.Append("  _listContainerRt = ").AppendLine(lcInfo);
            if (listContainerRt != null)
            {
                var rt = listContainerRt;
                sb.Append("     anchored: anchorMin=").Append(rt.anchorMin.ToString("0.00"))
                  .Append(" anchorMax=").Append(rt.anchorMax.ToString("0.00"))
                  .Append(" offsetMin=").Append(rt.offsetMin)
                  .Append(" offsetMax=").Append(rt.offsetMax)
                  .Append(" sizeDelta=").AppendLine(rt.sizeDelta.ToString("0"));
                sb.Append("     GO activeSelf=").Append(rt.gameObject.activeSelf).Append(" inHierarchy=").AppendLine(rt.gameObject.activeInHierarchy.ToString());
                var img = rt.GetComponent<Image>();
                if (img != null) sb.Append("     Image color = ").Append(img.color.ToString("0.00")).Append(" (alpha=").Append(img.color.a.ToString("0.00")).Append(") enabled=").AppendLine(img.enabled.ToString());
            }
            sb.Append("  _listScroll null? ").Append(listScroll == null).AppendLine();
            if (listScroll != null)
            {
                sb.Append("    viewport null? ").Append(listScroll.viewport == null).AppendLine();
                sb.Append("    content null? ").Append(listScroll.content == null).AppendLine();
                sb.Append("    horizontal=").Append(listScroll.horizontal).Append(" vertical=").Append(listScroll.vertical).AppendLine();
                sb.Append("    ScrollRect GO active=").Append(listScroll.gameObject.activeSelf).Append(" enabled=").AppendLine(listScroll.enabled.ToString());
                if (listScroll.viewport != null)
                {
                    var vrt = listScroll.viewport as RectTransform;
                    sb.Append("    viewport: rect W=").Append(vrt.rect.width.ToString("0")).Append(" H=").AppendLine(vrt.rect.height.ToString("0"));
                    var mask = listScroll.viewport.GetComponent<Mask>();
                    sb.Append("    Mask enabled? ").Append(mask != null && mask.enabled).Append(" showGraphic=").Append(mask != null ? mask.showMaskGraphic : false).AppendLine();
                    var imgVp = listScroll.viewport.GetComponent<Image>();
                    if (imgVp != null) sb.Append("    viewport Image color a=").Append(imgVp.color.a.ToString("0.00")).Append(" enabled=").AppendLine(imgVp.enabled.ToString());
                }
            }
            if (listContentRt != null)
            {
                sb.Append("  _listContentRt: name=").Append(listContentRt.name)
                  .Append(" rect W=").Append(listContentRt.rect.width.ToString("0"))
                  .Append(" H=").Append(listContentRt.rect.height.ToString("0"))
                  .Append(" anchoredPos=").AppendLine(listContentRt.anchoredPosition.ToString("0"));
                sb.Append("    anchorMin=").Append(listContentRt.anchorMin.ToString("0.00"))
                  .Append(" anchorMax=").Append(listContentRt.anchorMax.ToString("0.00"))
                  .Append(" sizeDelta=").Append(listContentRt.sizeDelta.ToString("0"))
                  .Append(" pivot=").AppendLine(listContentRt.pivot.ToString("0.00"));
                sb.Append("    child count=").AppendLine(listContentRt.childCount.ToString());
                for (int i = 0; i < listContentRt.childCount; i++)
                {
                    var ch = listContentRt.GetChild(i);
                    var crt = ch as RectTransform;
                    sb.Append("      child[").Append(i).Append("]=").Append(ch.name)
                      .Append(" active=").Append(ch.gameObject.activeSelf);
                    if (crt != null) sb.Append(" pos=").Append(crt.anchoredPosition).Append(" sizeDelta=").Append(crt.sizeDelta.ToString("0"));
                    sb.AppendLine();
                }
                var csf = listContentRt.GetComponent<ContentSizeFitter>();
                if (csf != null) sb.Append("    CSF vfit=").Append(csf.verticalFit).Append(" hfit=").AppendLine(csf.horizontalFit.ToString());
            }
            else sb.AppendLine("  _listContentRt = NULL");
            sb.Append("  _listContentVlg (VerticalLayoutGroup) = ").Append(listContentVlg != null ? "EXISTS" : "NULL").AppendLine();
            if (listContentVlg != null)
            {
                sb.Append("    enabled=").Append(listContentVlg.enabled)
                  .Append(" controlW=").Append(listContentVlg.childControlWidth)
                  .Append(" controlH=").Append(listContentVlg.childControlHeight)
                  .Append(" expandW=").Append(listContentVlg.childForceExpandWidth)
                  .Append(" expandH=").Append(listContentVlg.childForceExpandHeight)
                  .Append(" padding=").Append(listContentVlg.padding)
                  .Append(" spacing=").AppendLine(listContentVlg.spacing.ToString("0.0"));
            }
            string unequipInfo;
            if (unequipAllBtn != null)
            {
                var t = unequipAllBtn.transform.Find("Text");
                string text = t != null ? t.GetComponent<Text>()?.text ?? "NO TEXT COMP" : "NO TEXT CHILD";
                unequipInfo = "name=" + unequipAllBtn.gameObject.name + " text=" + text;
            }
            else unequipInfo = "NULL";
            sb.Append("  _unequipAllBtn = ").AppendLine(unequipInfo);

            sb.AppendLine().AppendLine("---- panelRoot children (2 levels) ----");
            if (panelRoot != null) PrintHierarchy(panelRoot.transform, sb, 0, 2);

            sb.AppendLine().AppendLine("---- PlayerInventory state ----");
            if (pInv != null && pInv.Inventory != null)
            {
                sb.Append("  Stacks.Count=").Append(pInv.Inventory.Stacks.Count)
                  .Append("  Instances.Count=").AppendLine(pInv.Inventory.Instances.Count.ToString());
                foreach (var ins in pInv.Inventory.Instances)
                {
                    var d = pInv.Database != null ? pInv.Database.Find(ins.itemId) : null;
                    string slot = d != null ? d.Slot.ToString() : "?";
                    sb.Append("    Instance itemId=").Append(ins.itemId)
                      .Append(" slot=").Append(slot)
                      .Append(" isEq=").AppendLine(pInv.IsEquipped(ins.instanceId).ToString());
                }
            }

            return sb.ToString();
        }

        private static object GetField(System.Type t, string name, object target)
        {
            var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return f != null ? f.GetValue(target) : null;
        }

        private static string GetPath(GameObject go)
        {
            var t = go.transform;
            var sb = new StringBuilder(go.name);
            while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); }
            return "/" + sb;
        }

        private static void PrintHierarchy(Transform t, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            var rt = t as RectTransform;
            string sizeInfo = "";
            if (rt != null) sizeInfo = " [rect W=" + rt.rect.width.ToString("0") + " H=" + rt.rect.height.ToString("0") + "]";
            sb.Append(indent).Append("->").Append(t.gameObject.name).Append(" active=").Append(t.gameObject.activeSelf).AppendLine(sizeInfo);
            for (int i = 0; i < t.childCount; i++) PrintHierarchy(t.GetChild(i), sb, depth + 1, maxDepth);
        }
    }
}
