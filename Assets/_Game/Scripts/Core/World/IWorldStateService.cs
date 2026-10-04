using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Time;
using Game.Core.World;

namespace Game.Core.World
{
    public interface IWorldStateService
    {
        Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync();
        Task<FloorStatus> GetFloorStatusAsync(string floorId);

        Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync();
        Task<BossStatus> GetBossStatusAsync(string bossId);

        Task<WorldTime> GetWorldTimeAsync();

        Task ReportBossDefeated(string bossId, string floorId);
    }
}
