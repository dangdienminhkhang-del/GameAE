using System;
using System.Collections.Generic;
using Game.Core.Items;
using Game.Core.NPC;
using UnityEngine;

namespace Game.Core.Shop
{
    /// <summary>Một dòng hàng bán: item + giá (Col).</summary>
    [Serializable]
    public class ShopEntry
    {
        public ItemDefinition item;
        [Min(1)] public int price = 10;
    }

    /// <summary>
    /// Cửa hàng của một NPC thương nhân: danh sách hàng bán + giá thu mua lại.
    /// Giá thu mua = ItemDefinition.SellValue × sellPriceScale (tối thiểu 1 Col).
    /// </summary>
    [CreateAssetMenu(fileName = "ShopDefinition", menuName = "Game/Core/Shop Definition")]
    public class ShopDefinition : ScriptableObject
    {
        [SerializeField] private string id = "shop.new";
        [SerializeField] private string displayName = "Cửa hàng";
        [TextArea] [SerializeField] private string greeting = "Chào mừng! Xem hàng nhé.";
        [TextArea] [SerializeField] private string boughtLine = "Giao dịch vui vẻ!";
        [TextArea] [SerializeField] private string soldLine = "Được, tôi nhận món này.";
        [TextArea] [SerializeField] private string notEnoughLine = "Không đủ Col rồi, bạn ơi.";
        [SerializeField] private List<ShopEntry> stock = new List<ShopEntry>();
        [SerializeField] private float sellPriceScale = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Greeting => greeting;
        public string BoughtLine => boughtLine;
        public string SoldLine => soldLine;
        public string NotEnoughLine => notEnoughLine;
        public IReadOnlyList<ShopEntry> Stock => stock;

        /// <summary>Giá thu mua lại một item từ player (0 = không thu).</summary>
        public int SellPriceOf(ItemDefinition item)
        {
            if (item == null || item.SellValue <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(item.SellValue * Mathf.Max(0f, sellPriceScale)));
        }
    }

    /// <summary>Yêu cầu mở cửa hàng (NPC → UI). Tách khỏi GameEvents để không đụng file sự kiện chung.</summary>
    public class ShopRequest
    {
        public NPCDefinition Npc;
        public ShopDefinition Shop;
    }

    public static class ShopEvents
    {
        public static event Action<object, ShopRequest> OnShopRequested;
        public static void RaiseShopRequested(object sender, ShopRequest request) => OnShopRequested?.Invoke(sender, request);
    }
}
