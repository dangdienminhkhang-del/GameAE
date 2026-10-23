using System;
using System.Collections.Generic;
using Game.Core.Skills;

namespace Game.Core.Guidance
{
    public enum SafeZoneEdge
    {
        None = 0,
        Entered = 1,
        Left = 2
    }

    /// <summary>
    /// Bắt CẠNH vào/ra Safe Zone từ chuỗi mẫu đo. Mẫu đầu tiên chỉ ghi nhớ trạng thái (không phát cạnh),
    /// nên bắt đầu game đang đứng trong Safe Zone không bị tính là "vào".
    /// </summary>
    public sealed class SafeZoneEdgeDetector
    {
        private bool _known;
        private bool _inside;

        public SafeZoneEdge Update(bool insideSafeZone)
        {
            if (!_known)
            {
                _known = true;
                _inside = insideSafeZone;
                return SafeZoneEdge.None;
            }

            if (insideSafeZone == _inside) return SafeZoneEdge.None;
            _inside = insideSafeZone;
            return insideSafeZone ? SafeZoneEdge.Entered : SafeZoneEdge.Left;
        }

        public void Reset()
        {
            _known = false;
            _inside = false;
        }
    }

    /// <summary>
    /// Máu thấp có TRỄ: bắn một lần khi tỉ lệ máu xuống tới ngưỡng; chỉ bắn lại sau khi máu đã hồi lên tới ngưỡng nạp lại.
    /// Nhờ vậy máu dao động quanh ngưỡng không bắn lặp. Máu bằng 0 (đã chết) không bắn.
    /// </summary>
    public sealed class LowHealthGate
    {
        private readonly float _triggerRatio;
        private readonly float _rearmRatio;
        private bool _armed = true;

        public LowHealthGate(float triggerRatio, float rearmRatio)
        {
            _triggerRatio = triggerRatio;
            _rearmRatio = rearmRatio < triggerRatio ? triggerRatio : rearmRatio;
        }

        /// <summary>Trả true đúng lần máu vừa xuống ngưỡng.</summary>
        public bool Update(double current, double max)
        {
            if (max <= 0.0 || double.IsNaN(current) || double.IsNaN(max)) return false;

            double ratio = current / max;
            if (ratio >= _rearmRatio)
            {
                _armed = true;
                return false;
            }

            if (_armed && current > 0.0 && ratio <= _triggerRatio)
            {
                _armed = false;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _armed = true;
        }
    }

    /// <summary>
    /// "Di chuyển liên tục": đủ <c>confirmTicks</c> nhịp liên tiếp có tốc độ ≥ ngưỡng thì báo MỘT lần (chốt cho tới khi Reset).
    /// Cảm biến chạy tối đa 4 nhịp/giây nên mặc định 2 nhịp ≈ 0.5 giây (số khởi điểm, đo thật thắng).
    /// </summary>
    public sealed class MoveStreakDetector
    {
        private readonly int _confirmTicks;
        private readonly float _minSpeed;
        private int _streak;
        private bool _fired;

        public MoveStreakDetector(int confirmTicks, float minSpeed)
        {
            _confirmTicks = confirmTicks < 1 ? 1 : confirmTicks;
            _minSpeed = minSpeed < 0f ? 0f : minSpeed;
        }

        public bool Update(float distanceMoved, float deltaSeconds)
        {
            if (_fired || deltaSeconds <= 0f || float.IsNaN(distanceMoved)) return false;

            float speed = distanceMoved / deltaSeconds;
            if (speed >= _minSpeed) _streak++;
            else _streak = 0;

            if (_streak < _confirmTicks) return false;
            _fired = true;
            return true;
        }

        public void Reset()
        {
            _streak = 0;
            _fired = false;
        }
    }

    /// <summary>
    /// Tín hiệu "kỹ thuật đã mở khóa": đọc từ lý do IsSlotReady của TechniqueController. Chưa mở khi thiếu vũ khí,
    /// thiếu định nghĩa, chưa đủ mastery hoặc sai hệ vũ khí; các lý do còn lại (đang bận, đang hồi chiêu) nghĩa là ĐÃ mở.
    /// </summary>
    public static class TechniqueUnlockRules
    {
        /// <summary>
        /// IsSlotReady trả CombatBusy TRƯỚC khi kiểm vũ khí/mastery (đang Dodge hay đang chạy kỹ thuật), nên lý do này KHÔNG cho biết
        /// kỹ thuật đã mở hay chưa: bỏ mẫu đo đó, chờ nhịp sau.
        /// </summary>
        public static bool IsConclusive(TechniqueValidationError reason)
        {
            return reason != TechniqueValidationError.CombatBusy;
        }

        public static bool IsUnlocked(TechniqueValidationError reason)
        {
            switch (reason)
            {
                case TechniqueValidationError.NoWeaponEquipped:
                case TechniqueValidationError.DefinitionMissing:
                case TechniqueValidationError.LockedMastery:
                case TechniqueValidationError.WrongDiscipline:
                    return false;
                default:
                    return true;
            }
        }
    }

    /// <summary>Cạnh lên: báo true đúng lần trạng thái đổi từ "chưa mở" sang "đã mở".</summary>
    public sealed class RisingEdgeDetector
    {
        private bool _previous;

        public bool Update(bool current)
        {
            bool rising = current && !_previous;
            _previous = current;
            return rising;
        }

        public void Reset()
        {
            _previous = false;
        }
    }

    /// <summary>
    /// hint.floor_select chỉ phát khi: boss đã bị hạ VÀ máy chủ đã xác nhận VÀ bảng báo boss (P8-5) đã đóng.
    /// Phát đúng một lần cho mỗi lần boss hạ + xác nhận; sau đó tự nạp lại cho boss kế.
    /// </summary>
    public sealed class FloorUnlockGate
    {
        private bool _defeated;
        private bool _confirmed;

        public void MarkDefeated()
        {
            _defeated = true;
        }

        public void MarkConfirmed()
        {
            _confirmed = true;
        }

        public bool Update(bool bossPanelClosed)
        {
            if (!_defeated || !_confirmed || !bossPanelClosed) return false;
            _defeated = false;
            _confirmed = false;
            return true;
        }

        public void Reset()
        {
            _defeated = false;
            _confirmed = false;
        }
    }

    /// <summary>
    /// Phát hiện "vừa mặc một trang bị": instanceId của một slot đổi sang giá trị KHÁC RỖNG so với mẫu trước.
    /// Mẫu đầu tiên chỉ ghi nhớ. Tháo đồ (về rỗng) không tính là mặc.
    /// </summary>
    public sealed class EquipChangeDetector
    {
        private bool _known;
        private string _weapon;
        private string _armor;

        public bool Update(string weaponInstanceId, string armorInstanceId)
        {
            bool equipped = _known &&
                            ((!string.IsNullOrEmpty(weaponInstanceId) && weaponInstanceId != _weapon) ||
                             (!string.IsNullOrEmpty(armorInstanceId) && armorInstanceId != _armor));
            _known = true;
            _weapon = weaponInstanceId;
            _armor = armorInstanceId;
            return equipped;
        }

        public void Reset()
        {
            _known = false;
            _weapon = null;
            _armor = null;
        }
    }

    /// <summary>Chọn các hint trong danh mục theo tín hiệu. Hint không có trigger (None) không bao giờ được chọn.</summary>
    public static class TutorialTriggerMap
    {
        public static void Collect(IReadOnlyList<TutorialHintDefinition> hints, TutorialTrigger trigger, List<TutorialHint> output)
        {
            if (hints == null || output == null || trigger == TutorialTrigger.None) return;

            for (int i = 0; i < hints.Count; i++)
            {
                TutorialHintDefinition definition = hints[i];
                if (definition == null || definition.Trigger != trigger) continue;
                output.Add(definition.ToHint());
            }
        }
    }
}
