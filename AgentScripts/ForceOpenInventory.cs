using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Text;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class ForceOpenInventory
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL invPanel null";
            var tIP = typeof(UIInv.InventoryPanel);

            var panelRoot = tIP.GetField("panelRoot", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as GameObject;
            if (panelRoot == null) return "panelRoot null";

            // FORCE OPEN — do not toggle, always set active=true
            panelRoot.SetActive(true);

            var listContainer = panelRoot.transform.Find("_inventoryDynamicList")?.gameObject;
            if (listContainer != null) listContainer.SetActive(true);

            // Force canvas rebuild + refresh
            Canvas.ForceUpdateCanvases();
            var miR = tIP.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
            miR?.Invoke(invPanel, null);

            // Reset scroll to top explicitly
            var scroll = tIP.GetField("_listScroll", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as ScrollRect;
            if (scroll != null) {
                scroll.verticalNormalizedPosition = 1f;
                scroll.enabled = true;
            }
            Canvas.ForceUpdateCanvases();
            var lc = tIP.GetField("_listContentRt", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as RectTransform;
            if (lc != null) {
                var ap = lc.anchoredPosition; ap.y = 0f; lc.anchoredPosition = ap;
                LayoutRebuilder.ForceRebuildLayoutImmediate(lc);
            }
            Canvas.ForceUpdateCanvases();

            // State report
            var rp = tIP.GetField("_rowPool", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as System.Collections.Generic.List<GameObject>;
            int active = 0; if (rp != null) foreach (var r in rp) if (r.activeSelf) active++;
            string firstLabel = ""; string firstBtn = "";
            if (rp != null && rp.Count > 0 && rp[0].activeSelf) {
                firstLabel = rp[0].transform.Find("Label")?.GetComponent<Text>()?.text ?? "";
                firstBtn = rp[0].transform.Find("ActionBtn")?.transform.Find("Text")?.GetComponent<Text>()?.text ?? "";
            }
            sb.Append("panelRoot.activeSelf=").Append(panelRoot.activeSelf);
            sb.Append("  rowPool=").Append(rp?.Count ?? -1).Append(" active=").Append(active);
            sb.Append("\nFirstRow: label='").Append(firstLabel).Append("' btn='").Append(firstBtn).Append("'");
            sb.Append("\nlistContent anchoredPos.y=").Append(lc != null ? lc.anchoredPosition.y.ToString("0.00") : "null");
            return sb.ToString();
        }
    }
}
