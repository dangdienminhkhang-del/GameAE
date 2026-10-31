using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class InventoryPanelRuntimeDiag2
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL: InventoryPanel not found";
            sb.AppendLine("InventoryPanel found");

            var tIP = typeof(UIInv.InventoryPanel);
            var getVal = new System.Func<string,object>((nm) => tIP.GetField(nm, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel));

            var listScroll = getVal("_listScroll") as ScrollRect;
            var listContentRt = getVal("_listContentRt") as RectTransform;
            var rowPool = getVal("_rowPool") as List<GameObject>;

            // Trigger Refresh manually so new fix runs
            var miRefresh = tIP.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
            if (miRefresh != null) miRefresh.Invoke(invPanel, null);
            sb.AppendLine("Refresh() invoked");

            int activeRows = 0;
            string firstRowLabel = "(none)";
            string firstRowBtn = "(none)";
            if (rowPool != null)
            {
                foreach (var r in rowPool)
                {
                    if (!r.activeSelf) continue;
                    if (activeRows == 0)
                    {
                        var lbl = r.transform.Find("Label")?.GetComponent<Text>();
                        var btn = r.transform.Find("ActionBtn")?.GetComponent<Button>();
                        var txt = btn != null ? btn.transform.Find("Text")?.GetComponent<Text>() : null;
                        firstRowLabel = lbl?.text;
                        firstRowBtn = txt?.text;
                    }
                    activeRows++;
                }
            }

            sb.Append("rowPool.Count=").Append(rowPool?.Count ?? -1)
              .Append("  activeRows=").Append(activeRows).AppendLine();
            sb.Append("First visible row label=").Append(firstRowLabel)
              .Append("  btn=").AppendLine(firstRowBtn);

            if (listScroll != null) sb.Append("verticalNormalizedPosition=").Append(listScroll.verticalNormalizedPosition.ToString("0.00")).AppendLine();
            if (listContentRt != null)
            {
                sb.Append("Content.rect W=").Append(listContentRt.rect.width.ToString("0"))
                  .Append(" H=").Append(listContentRt.rect.height.ToString("0"))
                  .Append("  anchoredPos.y=").AppendLine(listContentRt.anchoredPosition.y.ToString("0.00"));
                if (listContentRt.childCount > 0)
                {
                    var first = listContentRt.GetChild(0);
                    var frt = first as RectTransform;
                    sb.Append("First child (Row 0): active=").Append(first.gameObject.activeSelf)
                      .Append(" localPosY=").Append(frt != null ? frt.anchoredPosition.y.ToString("0.00") : "?").AppendLine();
                    var lbl = first.transform.Find("Label")?.GetComponent<Text>();
                    var btnT = first.transform.Find("ActionBtn")?.transform.Find("Text")?.GetComponent<Text>();
                    sb.Append("    label=").Append(lbl?.text ?? "null").Append("  btnText=").AppendLine(btnT?.text ?? "null");
                }
            }
            sb.AppendLine("== End ==");
            return sb.ToString();
        }
    }
}
