using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using CItems = Game.Core.Items;
using PItems = Game.PvE.Items;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class InjectItemsAndSnap
    {
        public static string Run()
        {
            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL invPanel null";
            var pInv = Object.FindFirstObjectByType<PItems.PlayerInventory>();
            if (pInv == null) return "FAIL player inv null";

            // If ItemDatabase lookup null, trigger EnsureLookup via reflection (also reset _built=true leak via Enter Play Mode Options)
            var db = pInv.Database;
            if (db != null) {
                var tDB = typeof(CItems.ItemDatabase);
                var builtFi = tDB.GetField("_built", BindingFlags.NonPublic | BindingFlags.Instance);
                if (builtFi != null) builtFi.SetValue(db, false);
                var ensureMi = tDB.GetMethod("EnsureLookup", BindingFlags.NonPublic | BindingFlags.Instance);
                ensureMi?.Invoke(db, null);
                var lookupFi = tDB.GetField("_lookup", BindingFlags.NonPublic | BindingFlags.Instance);
                if (lookupFi != null && lookupFi.GetValue(db) == null) {
                    // fallback: build _lookup manually from items field
                    var itemsFi = tDB.GetField("items", BindingFlags.NonPublic | BindingFlags.Instance);
                    var items = itemsFi?.GetValue(db) as List<CItems.ItemDefinition>;
                    if (items != null) {
                        var dict = new Dictionary<string, CItems.ItemDefinition>();
                        foreach (var it in items) if (it != null && !string.IsNullOrEmpty(it.Id)) dict[it.Id] = it;
                        lookupFi.SetValue(db, dict);
                    }
                }
            }
            var dPot = db.Find("item.health_potion");
            var dSword = db.Find("item.iron_sword");
            var dArmor = db.Find("item.leather_armor");
            if (dPot == null || dSword == null || dArmor == null) return $"DB lookup FAIL pot={dPot != null} sword={dSword != null} armor={dArmor != null}";

            // Add items (use TryAddItem API so the whole event chain fires → Changed → InventoryPanel.Refresh)
            int ok1 = pInv.TryAddItem(dPot, 5);
            int ok2 = pInv.TryAddItem(dSword, 1);
            int ok3 = pInv.TryAddItem(dArmor, 1);
            int ok4 = pInv.TryAddItem(dSword, 1);
            int ok5 = pInv.TryAddItem(dArmor, 1);
            int ok6 = pInv.TryAddItem(dSword, 1);

            // Open inventory panel (make sure panelRoot active)
            var tIP = typeof(UIInv.InventoryPanel);
            var panelRoot = tIP.GetField("panelRoot", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as GameObject;
            if (panelRoot != null) panelRoot.SetActive(true);

            // Explicit Refresh
            var miR = tIP.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance);
            miR?.Invoke(invPanel, null);

            // Try to open via Toggle() if the panel was closed
            var miToggle = tIP.GetMethod("Toggle", BindingFlags.Public | BindingFlags.Instance);
            miToggle?.Invoke(invPanel, null);
            // If it actually has bool parameter, try alternate:
            if (miToggle == null)
            {
                foreach (var m in tIP.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    if (m.Name == "Toggle" && m.GetParameters().Length == 0)
                        m.Invoke(invPanel, null);
            }
            // Force open second try if still closed
            if (panelRoot != null && !panelRoot.activeSelf)
                panelRoot.SetActive(true);

            // Also need to update layout for capture
            Canvas.ForceUpdateCanvases();
            var scroll = tIP.GetField("_listScroll", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as ScrollRect;
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();

            // State for report
            int instances = pInv.Inventory?.Instances.Count ?? -1;
            int stacks = pInv.Inventory?.Stacks.Count ?? -1;
            var eqField = tIP.GetField("_equippable", BindingFlags.NonPublic | BindingFlags.Instance);
            var eq = eqField?.GetValue(invPanel) as List<CItems.ItemInstance>;
            int rpCount = (tIP.GetField("_rowPool", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as List<GameObject>)?.Count ?? -1;
            int rpActive = 0;
            var rpList = tIP.GetField("_rowPool", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel) as List<GameObject>;
            if (rpList != null) foreach (var r in rpList) if (r.activeSelf) rpActive++;

            string firstLabel = "";
            string firstBtn = "";
            if (rpList != null && rpList.Count > 0) {
                var row0 = rpList[0];
                if (row0.activeSelf) {
                    firstLabel = row0.transform.Find("Label")?.GetComponent<Text>()?.text ?? "";
                    firstBtn = row0.transform.Find("ActionBtn")?.transform.Find("Text")?.GetComponent<Text>()?.text ?? "";
                }
            }

            return $"OK. added items hp={ok1} s1={ok2} a1={ok3} s2={ok4} a2={ok5} s3={ok6}\nInventory.Instances={instances} Stacks={stacks}\n_equippable.Count={eq?.Count ?? -1}\nrowPool.Count={rpCount} active={rpActive}\nFirstRow label='{firstLabel}' btn='{firstBtn}'";
        }
    }
}
