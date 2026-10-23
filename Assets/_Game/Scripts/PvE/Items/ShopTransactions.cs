using Game.Core.Items;
using Game.Core.Shop;

namespace Game.PvE.Items
{
    public enum ShopResult { Ok, NotEnoughCol, InventoryFull, Equipped, NotSellable, Invalid }

    /// <summary>Logic mua/bán thuần (không UI): kiểm tra → trừ/cộng Col → đổi đồ.</summary>
    public static class ShopTransactions
    {
        public static ShopResult Buy(PlayerInventory inventory, PlayerWallet wallet, ShopEntry entry)
        {
            if (inventory == null || wallet == null || entry == null || entry.item == null || entry.price < 0)
                return ShopResult.Invalid;
            if (wallet.Col < entry.price) return ShopResult.NotEnoughCol;

            int accepted = inventory.TryAddItem(entry.item, 1);
            if (accepted <= 0) return ShopResult.InventoryFull;

            wallet.TrySpend(entry.price);
            return ShopResult.Ok;
        }

        /// <summary>Bán một trang bị (instance). Món đang mặc phải tháo ra trước.</summary>
        public static ShopResult SellInstance(PlayerInventory inventory, PlayerWallet wallet, ShopDefinition shop, string instanceId)
        {
            if (inventory == null || wallet == null || shop == null) return ShopResult.Invalid;
            ItemInstance instance = inventory.Inventory.FindInstance(instanceId);
            if (instance == null) return ShopResult.Invalid;
            if (inventory.IsEquipped(instanceId)) return ShopResult.Equipped;

            ItemDefinition def = inventory.Database != null ? inventory.Database.Find(instance.itemId) : null;
            int price = shop.SellPriceOf(def);
            if (price <= 0) return ShopResult.NotSellable;
            if (!inventory.Inventory.RemoveInstance(instanceId)) return ShopResult.Invalid;

            wallet.Add(price);
            return ShopResult.Ok;
        }

        /// <summary>Bán 1 đơn vị của một stack (bình, nguyên liệu).</summary>
        public static ShopResult SellStackOne(PlayerInventory inventory, PlayerWallet wallet, ShopDefinition shop, string itemId)
        {
            if (inventory == null || wallet == null || shop == null) return ShopResult.Invalid;
            ItemDefinition def = inventory.Database != null ? inventory.Database.Find(itemId) : null;
            int price = shop.SellPriceOf(def);
            if (price <= 0) return ShopResult.NotSellable;
            if (!inventory.Inventory.RemoveStack(itemId, 1)) return ShopResult.Invalid;

            wallet.Add(price);
            return ShopResult.Ok;
        }
    }
}
