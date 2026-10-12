using System;
using System.Collections.Generic;

namespace Game.Core.Items
{
    /// <summary>
    /// Kết quả hỏi "có dùng được vật phẩm tiêu hao này không?".
    /// UI chỉ cần nhìn kết quả này để làm mờ nút và hiện lý do (xem <see cref="ConsumableRules.DescribeResult"/>).
    /// </summary>
    public enum ConsumableUseResult
    {
        Ok = 0,
        /// <summary>Không phải đồ tiêu hao, hoặc không có chỉ số hồi máu.</summary>
        NotConsumable = 1,
        /// <summary>Trong túi không còn cái nào.</summary>
        OutOfStock = 2,
        /// <summary>Nhân vật đã chết (hoặc chưa có nhân vật) — bình máu KHÔNG BAO GIỜ hồi sinh (docs/07 §2).</summary>
        PlayerDead = 3,
        /// <summary>Máu đã đầy.</summary>
        FullHealth = 4,
        /// <summary>Vừa đánh / vừa bị đánh chưa đủ số giây quy định.</summary>
        InCombat = 5,
        /// <summary>Vừa dùng loại này chưa đủ thời gian chờ.</summary>
        OnCooldown = 6
    }

    /// <summary>
    /// Luật dùng vật phẩm tiêu hao — logic thuần, không đụng Unity, test được (docs/07 §2).
    /// Bình máu chỉ dùng được NGOÀI giao chiến, không hồi sinh, có thời gian chờ.
    /// </summary>
    public static class ConsumableRules
    {
        /// <summary>Số giây phải yên tĩnh (không đánh, không bị đánh) trước khi được dùng. Đổi ở đây hoặc truyền tham số.</summary>
        public const float DefaultCombatLockSeconds = 5f;

        /// <param name="secondsSinceCombat">Số giây từ lần đánh/bị đánh gần nhất (chưa từng giao chiến = vô cực).</param>
        /// <param name="secondsSinceLastUse">Số giây từ lần dùng gần nhất CỦA LOẠI NÀY (chưa từng dùng = vô cực).</param>
        /// <param name="quantity">Số lượng đang có trong túi.</param>
        public static ConsumableUseResult CanUse(
            ItemDefinition definition,
            double currentHp,
            double maxHp,
            float secondsSinceCombat,
            float secondsSinceLastUse,
            int quantity,
            float combatLockSeconds = DefaultCombatLockSeconds)
        {
            if (definition == null || definition.Type != ItemType.Consumable || definition.HealAmount <= 0.0)
                return ConsumableUseResult.NotConsumable;
            if (quantity <= 0) return ConsumableUseResult.OutOfStock;
            if (currentHp <= 0.0) return ConsumableUseResult.PlayerDead;
            if (currentHp >= maxHp) return ConsumableUseResult.FullHealth;
            if (secondsSinceCombat < combatLockSeconds) return ConsumableUseResult.InCombat;
            if (secondsSinceLastUse < definition.UseCooldownSeconds) return ConsumableUseResult.OnCooldown;
            return ConsumableUseResult.Ok;
        }

        /// <summary>Số máu thực sự được hồi: không vượt MaxHealth.</summary>
        public static double ComputeHeal(ItemDefinition definition, double currentHp, double maxHp)
        {
            if (definition == null) return 0.0;
            double room = Math.Max(0.0, maxHp - currentHp);
            return Math.Min(definition.HealAmount, room);
        }

        /// <summary>Chữ lý do bằng tiếng Việt cho UI (Ok = chuỗi rỗng).</summary>
        public static string DescribeResult(ConsumableUseResult result)
        {
            switch (result)
            {
                case ConsumableUseResult.Ok: return string.Empty;
                case ConsumableUseResult.NotConsumable: return "Không dùng được";
                case ConsumableUseResult.OutOfStock: return "Hết";
                case ConsumableUseResult.PlayerDead: return "Đã gục";
                case ConsumableUseResult.FullHealth: return "Máu đã đầy";
                case ConsumableUseResult.InCombat: return "Đang giao chiến";
                case ConsumableUseResult.OnCooldown: return "Đang chờ";
                default: return string.Empty;
            }
        }
    }

    /// <summary>
    /// Ghi nhớ MỐC THỜI GIAN giao chiến gần nhất và lần dùng gần nhất của từng vật phẩm.
    /// Thuần logic: người gọi truyền "now" vào (PlayerInventory truyền Time.time) nên test không cần chờ thật.
    /// </summary>
    public sealed class ConsumableUseClock
    {
        private float _lastCombatAt = float.NegativeInfinity;
        private readonly Dictionary<string, float> _lastUseAt = new Dictionary<string, float>();

        public void MarkCombat(float now) => _lastCombatAt = now;

        public void MarkUsed(string itemId, float now)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            _lastUseAt[itemId] = now;
        }

        /// <summary>Chưa từng giao chiến → +vô cực.</summary>
        public float SecondsSinceCombat(float now) => now - _lastCombatAt;

        /// <summary>Chưa từng dùng → +vô cực.</summary>
        public float SecondsSinceUse(string itemId, float now)
        {
            if (string.IsNullOrEmpty(itemId)) return float.PositiveInfinity;
            return _lastUseAt.TryGetValue(itemId, out float at) ? now - at : float.PositiveInfinity;
        }

        public void Reset()
        {
            _lastCombatAt = float.NegativeInfinity;
            _lastUseAt.Clear();
        }
    }
}
