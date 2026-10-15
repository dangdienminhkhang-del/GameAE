namespace Game.PvE.Floor
{
    /// <summary>
    /// Luật "khi nào được chuyển tầng" — hàm thuần, không đụng scene/service nên test được trong EditMode.
    ///
    /// Vì sao có luật này: chọn tầng dịch chuyển người chơi vào điểm vào (nằm trong Vùng An Toàn), hồi sinh quái
    /// và reset boss. Nếu mở được ở bất cứ đâu thì nút chọn tầng thành "thoát hiểm" giữa trận, phá Permadeath
    /// (docs/09 §1.3; docs/11 §2: Vùng An Toàn là vùng bảo hộ). Vì vậy chỉ cho chuyển khi người chơi đang đứng
    /// trong Vùng An Toàn và còn sống.
    ///
    /// Không có nhánh theo số tầng: luật chỉ phụ thuộc vị trí và trạng thái sống/chết của người chơi.
    /// </summary>
    public static class FloorTravelRules
    {
        /// <summary>Kết quả kiểm luật chuyển tầng.</summary>
        public enum Result
        {
            /// <summary>Được phép chuyển tầng.</summary>
            Ok = 0,

            /// <summary>Người chơi không đứng trong Vùng An Toàn.</summary>
            NotInSafeZone = 1,

            /// <summary>Nhân vật đã gục (permadeath) — không thể chuyển tầng.</summary>
            PlayerDead = 2
        }

        /// <summary>
        /// Kiểm luật chuyển tầng. <paramref name="playerDead"/> được ưu tiên trước
        /// <paramref name="playerInSafeZone"/>: nhân vật đã gục thì báo "đã gục", không báo "ngoài Vùng An Toàn".
        /// </summary>
        public static Result CanTravel(bool playerInSafeZone, bool playerDead)
        {
            if (playerDead) return Result.PlayerDead;
            if (!playerInSafeZone) return Result.NotInSafeZone;
            return Result.Ok;
        }
    }
}
