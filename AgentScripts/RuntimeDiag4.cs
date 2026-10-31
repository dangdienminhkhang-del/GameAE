using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CItems = Game.Core.Items;
using PItems = Game.PvE.Items;
using UIInv = Game.UI.Inventory;

namespace AgentScripts
{
    public static class RuntimeDiag4
    {
        public static string Run()
        {
            var sb = new StringBuilder();
            var invPanel = Object.FindFirstObjectByType<UIInv.InventoryPanel>();
            if (invPanel == null) return "FAIL invPanel null";
            var tIP = typeof(UIInv.InventoryPanel);
            object g(string n) => tIP.GetField(n, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(invPanel);

            var playerInventory = g("playerInventory") as PItems.PlayerInventory;
            sb.Append("playerInventory ref null? ").Append(playerInventory == null).AppendLine();
            if (playerInventory == null) return sb.ToString();

            var pInv2 = Object.FindFirstObjectByType<PItems.PlayerInventory>();
            sb.Append("FindFirstObjectOf PlayerInventory same ref? ").Append(ReferenceEquals(playerInventory, pInv2)).AppendLine();

            sb.Append("Inventory null? ").Append(playerInventory.Inventory == null).AppendLine();
            if (playerInventory.Inventory != null) sb.Append("Inventory.Instances.Count=").Append(playerInventory.Inventory.Instances.Count).Append(" Stacks.Count=").AppendLine(playerInventory.Inventory.Stacks.Count.ToString());

            sb.Append("Database null? ").Append(playerInventory.Database == null).AppendLine();
            if (playerInventory.Database != null)
            {
                var tDB = typeof(CItems.ItemDatabase);
                var fLookup = tDB.GetField("_lookup", BindingFlags.NonPublic | BindingFlags.Instance);
                var lookupObj = fLookup?.GetValue(playerInventory.Database);
                sb.Append("_lookup dict null? ").Append(lookupObj == null).AppendLine();

                int lookupCount = 0;
                if (lookupObj != null) lookupCount = (int)lookupObj.GetType().GetProperty("Count").GetValue(lookupObj);
                sb.Append("_lookup.Count=").Append(lookupCount).Append("  Items (SO) count? ").AppendLine();

                if (playerInventory.Inventory != null)
                {
                    int eqThisRun = 0;
                    int defNull = 0;
                    int slotNone = 0;
                    foreach (var ins in playerInventory.Inventory.Instances)
                    {
                        var d = playerInventory.Database.Find(ins.itemId);
                        if (d == null) { defNull++; continue; }
                        if (d.Slot == CItems.EquipmentSlot.None) { slotNone++; continue; }
                        eqThisRun++;
                    }
                    sb.Append("Manual rebuild: defNull=").Append(defNull).Append(" slotNone=").Append(slotNone).Append(" equippable=").Append(eqThisRun).AppendLine();
                }
            }

            var eq = g("_equippable") as List<CItems.ItemInstance>;
            sb.Append("_equippable.Count=").Append(eq?.Count ?? -1).AppendLine();
            var rp = g("_rowPool") as List<GameObject>;
            int active = 0; if (rp != null) foreach (var r in rp) if (r.activeSelf) active++;
            sb.Append("_rowPool.Count=").Append(rp?.Count ?? -1).Append(" active=").Append(active).AppendLine();

            return sb.ToString();
        }
    }
}
