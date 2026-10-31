using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CItems = Game.Core.Items;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class RuntimeDiag3
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL not found";
            var tIP = typeof(UIInv.InventoryPanel);

            // Force OnEnable -> EnsureDynamicUI chain by calling Toggle (open) manually
            // but instead directly call Refresh via reflection
            var miRefresh = tIP.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
            if (miRefresh != null) miRefresh.Invoke(invPanel, null);

            object getv(string n) => tIP.GetField(n, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel);
            var eq = getv("_equippable") as List<CItems.ItemInstance>;
            var rp = getv("_rowPool") as List<GameObject>;
            var lc = getv("_listContentRt") as RectTransform;

            sb.Append("equippable.Count=").Append(eq?.Count ?? -1).AppendLine();
            sb.Append("rowPool.Count=").Append(rp?.Count ?? -1).Append("  rows:");
            if (rp != null) foreach (var r in rp) sb.Append(r.activeSelf ? "1" : "0");
            sb.AppendLine();
            sb.Append("Content.childCount=").Append(lc?.childCount ?? -1).Append(" rect H=").Append(lc != null ? lc.rect.height.ToString("0") : "null").Append(" anchoredPos.y=").Append(lc != null ? lc.anchoredPosition.y.ToString("0") : "null").AppendLine();

            if (lc != null)
            {
                for (int i = 0; i < lc.childCount; i++)
                {
                    var ch = lc.GetChild(i);
                    bool rowActiveInPool = false;
                    GameObject go = ch.gameObject;
                    if (rp != null) foreach (var r in rp) if ((object)r == (object)go) { rowActiveInPool = true; break; }
                    var lbl = ch.Find("Label")?.GetComponent<Text>();
                    var btnT = ch.Find("ActionBtn")?.transform.Find("Text")?.GetComponent<Text>();
                    sb.Append("  child[").Append(i).Append("] name=").Append(ch.name)
                      .Append(" activeSelf=").Append(go.activeSelf)
                      .Append(" inPoolRef=").Append(rowActiveInPool)
                      .Append(" label=").Append(lbl?.text ?? "null")
                      .Append(" btn=").AppendLine(btnT?.text ?? "null");
                }
            }
            return sb.ToString();
        }
    }
}
