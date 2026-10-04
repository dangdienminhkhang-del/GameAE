using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Time;
using Game.Core.World;

namespace Game.Services.World
{
    public class WorldStateService : IWorldStateService
    {
        public virtual async Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
        {
            await Task.CompletedTask;
            return new Dictionary<string, FloorStatus>
            {
                { "floor.01", FloorStatus.Available }
            };
        }

        public virtual async Task<FloorStatus> GetFloorStatusAsync(string floorId)
        {
            await Task.CompletedTask;
            return floorId == "floor.01" ? FloorStatus.Available : FloorStatus.Locked;
        }

        public virtual async Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync()
        {
            await Task.CompletedTask;
            return new Dictionary<string, BossStatus>
            {
                { "boss.floor_01", BossStatus.Active }
            };
        }

        public virtual async Task<BossStatus> GetBossStatusAsync(string bossId)
        {
            await Task.CompletedTask;
            return BossStatus.Active;
        }

        public virtual async Task<WorldTime> GetWorldTimeAsync()
        {
            await Task.CompletedTask;
            return new WorldTime
            {
                gameTimeHours = 8.0,
                lastSyncRealTimeUnix = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeSeconds()
            };
        }

        public virtual async Task ReportBossDefeated(string bossId, string floorId)
        {
            await Task.CompletedTask;
        }
    }
}
