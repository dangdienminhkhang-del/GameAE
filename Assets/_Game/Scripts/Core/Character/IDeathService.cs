using System;
using System.Threading.Tasks;
using Game.Core.Character;

namespace Game.Core.Character
{
    public interface IDeathService
    {
        bool IsPlayerDead { get; }
        DeathInfo CurrentDeathInfo { get; }

        /// <summary>Chỉ phát SAU KHI cái chết đã được ghi và xác nhận bởi nguồn chính.</summary>
        event EventHandler<DeathInfo> OnPlayerDied;

        /// <summary>
        /// Đọc trạng thái chết từ nguồn chính. Ném <see cref="Game.Core.World.SaveUnavailableException"/>
        /// nếu không xác minh được — caller PHẢI coi là "chưa biết" và chặn vào game (fail-closed).
        /// </summary>
        Task<DeathInfo> LoadDeathInfoAsync();

        /// <summary>
        /// Ghi nhận cái chết (docs/09 §1.3). Đã chết rồi thì bỏ qua (giữ lần đầu).
        /// Hoàn tất bình thường = nguồn chính ĐÃ xác nhận (và đã phát OnPlayerDied / PlayerDeathRecorded).
        /// Không xác nhận được → NÉM <see cref="Game.Core.World.SaveUnavailableException"/>: trạng thái "đã chết"
        /// vẫn giữ trong bộ nhớ (vẫn chặn chơi), KHÔNG phát sự kiện; dùng <see cref="RetryRecordAsync"/> để thử lại.
        /// </summary>
        Task RecordDeathAsync(DeathRecord record);

        /// <summary>
        /// Thử ghi lại cái chết đang chờ xác nhận. Không có bản chờ thì không làm gì.
        /// Xác nhận được thì phát sự kiện đúng một lần; lỗi thì ném <see cref="Game.Core.World.SaveUnavailableException"/>.
        /// </summary>
        Task RetryRecordAsync();

        /// <summary>API cũ — chỉ gọi <see cref="RecordDeathAsync"/>. Giữ một phase để migrate.</summary>
        [Obsolete("Dùng RecordDeathAsync")]
        Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds);

        Task ResetDeathStateAsync();
    }
}
