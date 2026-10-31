using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Quy tắc xác định đòn kết liễu Boss (Last Attack Bonus - LA) theo chuẩn SAO.
    /// - Chỉ đòn đánh do CHÍNH người chơi trực tiếp tung ra mới được tính (đòn thường, combo, skill, tuyệt kỹ).
    /// - Không tính sát thương theo thời gian (DoT như độc, chảy máu...).
    /// - Không tính sát thương từ đồng đội AI (Luồng B), quái đánh lẫn nhau, hoặc bẫy môi trường.
    /// </summary>
    public static class LastAttackRules
    {
        public const string PlayerSourceId = "player";

        /// <summary>
        /// Xác định xem đòn kết liễu này có phải do người chơi trực tiếp gây ra hay không.
        /// </summary>
        public static bool IsPlayerLastAttack(DamageInfo lethalDamage)
        {
            if (lethalDamage == null) return false;

            // 1. Loại trừ sát thương định kỳ / DoT (độc, chảy máu...)
            if (lethalDamage.IsPeriodic) return false;

            // 2. Kiểm tra SourceId
            if (!string.IsNullOrEmpty(lethalDamage.SourceId))
            {
                string id = lethalDamage.SourceId.Trim().ToLowerInvariant();

                // Loại trừ DoT, bẫy môi trường, quái, đồng đội AI
                if (id.StartsWith("dot") || id.Contains("poison") || id.Contains("bleed") ||
                    id.Contains("hazard") || id.Contains("trap") || id.StartsWith("env") ||
                    id.StartsWith("ai_") || id.StartsWith("ally") || id.Contains("companion") || id.Contains("pet"))
                {
                    return false;
                }

                if (id == "player" || id.StartsWith("player_"))
                {
                    return true;
                }
            }

            // 3. Kiểm tra đối tượng Source
            if (lethalDamage.Source != null)
            {
                if (lethalDamage.Source is Component comp)
                {
                    if (comp.CompareTag("Player")) return true;
                    string name = comp.gameObject.name.ToLowerInvariant();
                    if (name.Contains("player")) return true;
                }
                else if (lethalDamage.Source is GameObject go)
                {
                    if (go.CompareTag("Player")) return true;
                    string name = go.name.ToLowerInvariant();
                    if (name.Contains("player")) return true;
                }
                else
                {
                    string typeName = lethalDamage.Source.GetType().Name.ToLowerInvariant();
                    if (typeName.Contains("player") || typeName.Contains("characterstats"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
