using System;

namespace Game.PvE.Boss
{
    /// <summary>Boss đang ở giai đoạn nào của trận (gom các state của BossController thành 4 nhóm cho luật phòng boss).</summary>
    public enum BossEncounterPhase
    {
        /// <summary>Đứng yên ở nhà, chưa vào trận.</summary>
        Idle = 0,
        /// <summary>Đang trong trận: đuổi, báo đòn, đánh, khựng, lao.</summary>
        Combat = 1,
        /// <summary>Người chơi đã rời phòng, boss đang đi về nhà.</summary>
        ReturningHome = 2,
        /// <summary>Boss đã chết (luồng chết lo phần còn lại).</summary>
        Dead = 3
    }

    /// <summary>Việc boss nên làm ở nhịp này theo luật phòng boss.</summary>
    public enum BossEncounterDecision
    {
        Stay = 0,
        /// <summary>Vào (hoặc vào lại) trận: sang Chase.</summary>
        Engage = 1,
        /// <summary>Kết thúc trận: hủy báo đòn và lao, đi về nhà, GIỮ máu và enrage.</summary>
        ReturnHome = 2
    }

    /// <summary>
    /// Luật thuần của phòng boss (Phase 9.7b, docs/05, docs/12 §7.2). Không biết scene, tầng hay boss cụ thể:
    /// BossController chỉ hỏi "người chơi có trong phòng không" rồi làm theo quyết định ở đây.
    /// Khi không có phòng (hasEncounterZone = false) thì giữ hành vi cũ: chỉ DetectionRange, không bao giờ tự rời trận.
    /// </summary>
    public static class BossEncounterRules
    {
        /// <summary>Boss coi là "về tới nhà" khi cách vị trí gốc không quá khoảng này (đơn vị thế giới).</summary>
        public const float ReturnArriveDistance = 0.2f;

        /// <summary>Quá thời gian này mà chưa về tới nhà (kẹt vật cản) thì thôi, về Idle tại chỗ. Giây.</summary>
        public const float ReturnMaxSeconds = 12f;

        /// <summary>Người chơi phải ở ngoài phòng liên tục chừng này giây mới tính là đã rời (chống chớp ở biên phòng). Giây.</summary>
        public const float DefaultLeaveDelaySeconds = 0.5f;

        public static BossEncounterDecision Decide(
            BossEncounterPhase phase,
            bool hasEncounterZone,
            bool playerInZone,
            bool playerInDetectionRange)
        {
            switch (phase)
            {
                case BossEncounterPhase.Idle:
                    bool shouldEngage = hasEncounterZone ? playerInZone : playerInDetectionRange;
                    return shouldEngage ? BossEncounterDecision.Engage : BossEncounterDecision.Stay;

                case BossEncounterPhase.Combat:
                    return hasEncounterZone && !playerInZone ? BossEncounterDecision.ReturnHome : BossEncounterDecision.Stay;

                case BossEncounterPhase.ReturningHome:
                    return hasEncounterZone && playerInZone ? BossEncounterDecision.Engage : BossEncounterDecision.Stay;

                default:
                    return BossEncounterDecision.Stay;
            }
        }

        public static bool HasArrivedHome(float distanceToHome)
        {
            return distanceToHome <= ReturnArriveDistance;
        }

        public static bool ReturnTimedOut(float secondsSinceReturnStarted)
        {
            return secondsSinceReturnStarted >= ReturnMaxSeconds;
        }

        /// <summary>
        /// Bộ đếm đòn Pressure sau khi trận bị ngắt. Nếu bị ngắt đúng lúc báo đòn hoặc đang lao Pressure thì bộ đếm
        /// đã bị đặt về 0 lúc bắt đầu telegraph; trả lại trạng thái "đến lượt Pressure" để việc rời phòng KHÔNG hoãn được Pressure.
        /// Các trường hợp khác giữ nguyên.
        /// </summary>
        public static int PressureCounterAfterInterrupt(int currentCounter, int pressureEveryNAttacks, bool interruptedDuringPressureWindupOrDash)
        {
            if (!interruptedDuringPressureWindupOrDash) return currentCounter;
            return Math.Max(currentCounter, pressureEveryNAttacks);
        }

        /// <summary>
        /// Chờ giữa hai đòn sau khi trận bị ngắt: không ngắn hơn một chu kỳ đòn thường, để người chơi vào lại phòng
        /// không bị đánh ngay lập tức. Không bao giờ rút ngắn thời gian chờ còn lại.
        /// </summary>
        public static float ReengageCooldown(float currentAttackTimer, float attackCooldown)
        {
            return Math.Max(currentAttackTimer, attackCooldown);
        }
    }

    /// <summary>
    /// Lọc "người chơi có đang trong phòng không" để không bắt đầu và kết thúc trận liên tục khi đứng sát biên:
    /// VÀO phòng được tính NGAY; RA khỏi phòng chỉ được tính sau khi ở ngoài liên tục đủ <c>leaveDelaySeconds</c>.
    /// Dùng thay cho trạng thái "vùng hiện tại" của ZoneTracker, vì ZoneTracker giữ vùng cũ khi người chơi đứng ngoài mọi vùng.
    /// </summary>
    public sealed class ZonePresenceFilter
    {
        private readonly float _leaveDelaySeconds;
        private bool _inside;
        private float _outsideSeconds;

        public ZonePresenceFilter(float leaveDelaySeconds = BossEncounterRules.DefaultLeaveDelaySeconds)
        {
            _leaveDelaySeconds = float.IsNaN(leaveDelaySeconds) || leaveDelaySeconds < 0f ? 0f : leaveDelaySeconds;
        }

        /// <summary>Kết quả đã lọc ở lần cập nhật gần nhất.</summary>
        public bool Inside => _inside;

        /// <summary>Đưa kết quả đo thô của nhịp này vào; trả về kết quả đã lọc.</summary>
        public bool Update(bool rawInside, float deltaSeconds)
        {
            if (rawInside)
            {
                _inside = true;
                _outsideSeconds = 0f;
                return true;
            }

            if (!_inside) return false;

            _outsideSeconds += deltaSeconds > 0f ? deltaSeconds : 0f;
            if (_outsideSeconds >= _leaveDelaySeconds)
            {
                _inside = false;
                _outsideSeconds = 0f;
            }
            return _inside;
        }

        public void Reset()
        {
            _inside = false;
            _outsideSeconds = 0f;
        }
    }
}
