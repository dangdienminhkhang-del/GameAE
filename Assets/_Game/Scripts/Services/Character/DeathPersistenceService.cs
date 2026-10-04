using System;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Events;
using Game.Core.World;

namespace Game.Services.Character
{
    /// <summary>
    /// Ghi nhận permadeath và LƯU vào Player Data (key "death", docs/03 §5 / 09 §1.4).
    /// "Khóa account" là logic game (isDead = true), KHÔNG xóa gì trên UGS.
    /// </summary>
    public class DeathPersistenceService : IDeathService
    {
        private readonly ISaveService _save;
        private DeathInfo _deathInfo = new DeathInfo();

        public DeathPersistenceService(ISaveService saveService = null)
        {
            _save = saveService;
        }

        public bool IsPlayerDead => _deathInfo != null && _deathInfo.isDead;
        public DeathInfo CurrentDeathInfo => _deathInfo;

        public event EventHandler<DeathInfo> OnPlayerDied;

        public virtual async Task<DeathInfo> LoadDeathInfoAsync()
        {
            if (_save != null)
            {
                // Fail-closed: đọc nguồn chính, lỗi thì NÉM (SaveUnavailableException) chứ không fallback local
                // — nếu không, tắt mạng/xóa file local là "sống lại" (docs/03 §12).
                DeathInfo loaded = await _save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death);
                if (loaded != null) _deathInfo = loaded;
            }
            return _deathInfo;
        }

        public virtual async Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds)
        {
            // Chỉ chết MỘT lần: gọi lại (2 đòn cùng frame...) không được ghi đè nguyên nhân/thời điểm chết đầu tiên.
            if (IsPlayerDead) return;

            _deathInfo = new DeathInfo
            {
                isDead = true,
                deathCause = reason.ToString(),
                deathSourceId = sourceId,
                deathTimeUnix = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                finalLevel = currentLevel,
                finalHighestFloor = highestFloor,
                totalPlayTimeSeconds = playTimeSeconds
            };

            if (_save != null) await _save.SaveAsync(SaveKeys.Death, _deathInfo);

            OnPlayerDied?.Invoke(this, _deathInfo);
            GameEvents.RaisePlayerDeathRecorded(this, _deathInfo);
        }

        public virtual async Task ResetDeathStateAsync()
        {
            _deathInfo = new DeathInfo();
            if (_save != null) await _save.SaveAsync(SaveKeys.Death, _deathInfo);
        }
    }
}
