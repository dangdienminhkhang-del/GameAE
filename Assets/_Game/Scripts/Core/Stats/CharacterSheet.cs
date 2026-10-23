using System.Globalization;
using UnityEngine;

namespace Game.Core.Stats
{
    /// <summary>
    /// Dựng các dòng chữ cho bảng Nhân vật (thuần — UI chỉ hiển thị chuỗi này).
    /// Dùng InvariantCulture để số thập phân luôn là dấu chấm, không phụ thuộc ngôn ngữ máy.
    /// </summary>
    public static class CharacterSheet
    {
        public static string[] BuildLines(
            int level, int exp, int expRequired,
            double maxHealth, double attack, double defense,
            double criticalChance, double criticalDamage, double moveSpeed)
        {
            var ci = CultureInfo.InvariantCulture;
            int percent = expRequired > 0 ? Mathf.Clamp(exp * 100 / expRequired, 0, 100) : 0;

            return new[]
            {
                "Cấp " + level.ToString(ci),
                "EXP " + exp.ToString(ci) + "/" + expRequired.ToString(ci) + " (" + percent.ToString(ci) + "%)",
                "Máu tối đa  " + maxHealth.ToString("0", ci),
                "Tấn công  " + attack.ToString("0.#", ci),
                "Phòng thủ  " + defense.ToString("0.#", ci),
                "Chí mạng  " + (criticalChance * 100.0).ToString("0.#", ci) + "% (sát thương x" + criticalDamage.ToString("0.##", ci) + ")",
                "Tốc độ di chuyển  " + moveSpeed.ToString("0.#", ci)
            };
        }
    }
}
