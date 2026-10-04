using System;
using System.Threading.Tasks;
using Game.Core.Character;

namespace Game.Core.Character
{
    public interface IDeathService
    {
        bool IsPlayerDead { get; }
        DeathInfo CurrentDeathInfo { get; }

        event EventHandler<DeathInfo> OnPlayerDied;

        /// <summary>
        /// Đọc trạng thái chết từ nguồn chính. Ném <see cref="Game.Core.World.SaveUnavailableException"/>
        /// nếu không xác minh được — caller PHẢI coi là "chưa biết" và chặn vào game (fail-closed).
        /// </summary>
        Task<DeathInfo> LoadDeathInfoAsync();
        Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds);
        Task ResetDeathStateAsync();
    }
}
