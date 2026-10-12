using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;
using Game.Core.World;
using UnityEngine;

#if UGS_CLOUDSAVE_PRESENT
using Unity.Services.CloudSave;
#endif

namespace Game.Services.World
{
    /// <summary>
    /// Quản lý World State toàn cục (docs/02 §21–24, docs/03 §6).
    /// ĐỌC: lấy trực tiếp từ Cloud Save Game Data (Default access) rồi cache.
    /// GHI: chỉ qua <see cref="IServerFunctionService"/>. Client không tự đổi World State —
    /// cache chỉ đổi SAU KHI server xác nhận, dựa trên response của server (docs/12 §7.2).
    /// </summary>
    public class WorldStateService : IWorldStateService
    {
        private const string CustomId = "world";
        private const string WorldFloorsKey = "world_floors";
        private const string WorldBossesKey = "world_bosses";
        private const string ReportBossFunctionName = "ReportBossDefeated";

        // Tầng/boss mở sẵn khi cloud chưa có dữ liệu. Khớp hằng đặt tên ở cloudcode/ReportBossDefeated.js.
        private const string DefaultOpenFloorId = "floor.01";
        private const string DefaultActiveBossId = "boss.floor_01";

        /// <summary>
        /// Dữ liệu thô đọc từ kho cloud: giá trị trạng thái dạng chuỗi
        /// (<c>"CLEARED"</c>, <c>"AVAILABLE"</c>, <c>"DEFEATED"</c>...). Một map null = key chưa có trên cloud.
        /// </summary>
        public sealed class CloudWorldData
        {
            /// <summary>world_floors: <c>"floor.01" → "CLEARED"</c></summary>
            public Dictionary<string, string> Floors;

            /// <summary>world_bosses: <c>"boss.floor_01" → "DEFEATED"</c></summary>
            public Dictionary<string, string> Bosses;
        }

        private readonly IServerFunctionService _serverFunctions;
        private readonly Dictionary<string, FloorStatus> _floorStatuses = new Dictionary<string, FloorStatus>
        {
            { DefaultOpenFloorId, FloorStatus.Available }
        };
        private readonly Dictionary<string, BossStatus> _bossStatuses = new Dictionary<string, BossStatus>
        {
            { DefaultActiveBossId, BossStatus.Active }
        };

        public WorldStateService(IServerFunctionService serverFunctions = null)
        {
            _serverFunctions = serverFunctions;
        }

        public virtual async Task<Dictionary<string, FloorStatus>> GetAllFloorStatusesAsync()
        {
            await RefreshFromCloudIfPossible();
            return new Dictionary<string, FloorStatus>(_floorStatuses);
        }

        public virtual async Task<FloorStatus> GetFloorStatusAsync(string floorId)
        {
            if (string.IsNullOrEmpty(floorId)) return FloorStatus.Locked;
            await RefreshFromCloudIfPossible();
            if (_floorStatuses.TryGetValue(floorId, out var status))
                return status;
            return floorId == DefaultOpenFloorId ? FloorStatus.Available : FloorStatus.Locked;
        }

        public virtual async Task<Dictionary<string, BossStatus>> GetAllBossStatusesAsync()
        {
            await RefreshFromCloudIfPossible();
            return new Dictionary<string, BossStatus>(_bossStatuses);
        }

        public virtual async Task<BossStatus> GetBossStatusAsync(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return BossStatus.Active;
            await RefreshFromCloudIfPossible();
            if (_bossStatuses.TryGetValue(bossId, out var status))
                return status;
            return BossStatus.Active;
        }

        public virtual async Task<WorldTime> GetWorldTimeAsync()
        {
            await Task.CompletedTask;
            return new WorldTime
            {
                gameTimeHours = 8.0,
                lastSyncRealTimeUnix = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Báo boss đã bị hạ. Thứ tự BẮT BUỘC (docs/12 §7.2): hỏi server TRƯỚC → server xác nhận → mới đổi cache.
        /// - Thành công: cache theo response (boss Defeated, floor Cleared, <c>nextFloorId</c> do server chỉ định → Available;
        ///   không bao giờ hạ Cleared xuống Available).
        /// - Lỗi (mạng, thiếu server function, response sai): NÉM lại, cache giữ nguyên, tầng kế vẫn Locked.
        /// - Gọi lại khi boss đã Defeated: vẫn hỏi server (server tự sửa trạng thái lệch), không báo lỗi.
        /// </summary>
        public virtual async Task ReportBossDefeated(string bossId, string floorId)
        {
            if (string.IsNullOrEmpty(bossId))
                throw new ArgumentException("bossId không được rỗng.", nameof(bossId));
            if (string.IsNullOrEmpty(floorId))
                throw new ArgumentException("floorId không được rỗng.", nameof(floorId));

            if (_serverFunctions == null)
            {
                // Không có server thì KHÔNG có xác nhận: tuyệt đối không "thành công giả" bằng cache cục bộ.
                throw new ServerFunctionUnavailableException(
                    ReportBossFunctionName, "WorldStateService chưa được cấp IServerFunctionService.");
            }

            BossDefeatResult result;
            try
            {
                result = await _serverFunctions.ReportBossDefeatedAsync(bossId, floorId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[WorldStateService] Server chưa xác nhận boss '{bossId}' ({floorId}) — giữ nguyên World State: {ex.Message}");
                throw;
            }

            // Phòng thủ thêm: kể cả khi implementation khác của IServerFunctionService trả kết quả lạ.
            ValidateConfirmation(result, bossId, floorId);

            ApplyConfirmedDefeat(result);
        }

        private static void ValidateConfirmation(BossDefeatResult result, string bossId, string floorId)
        {
            if (result == null)
                throw new ServerFunctionUnavailableException(ReportBossFunctionName, "server trả kết quả rỗng.");
            if (!string.Equals(result.BossId, bossId, StringComparison.Ordinal) ||
                !string.Equals(result.FloorId, floorId, StringComparison.Ordinal))
            {
                throw new ServerFunctionUnavailableException(
                    ReportBossFunctionName,
                    $"kết quả ('{result.BossId}', '{result.FloorId}') không khớp yêu cầu ('{bossId}', '{floorId}').");
            }
            if (string.IsNullOrEmpty(result.NextFloorId))
                throw new ServerFunctionUnavailableException(ReportBossFunctionName, "server không trả nextFloorId.");
        }

        private void ApplyConfirmedDefeat(BossDefeatResult result)
        {
            _bossStatuses[result.BossId] = BossStatus.Defeated;
            _floorStatuses[result.FloorId] = FloorStatus.Cleared;

            // Tầng kế do SERVER chỉ định. Không bao giờ hạ Cleared xuống Available.
            if (!_floorStatuses.TryGetValue(result.NextFloorId, out var nextStatus) ||
                nextStatus != FloorStatus.Cleared)
            {
                _floorStatuses[result.NextFloorId] = FloorStatus.Available;
            }
        }

        /// <summary>
        /// Seam đọc World State từ kho Cloud Save (Game Data, customId "world", Access Class Default).
        /// Trả null nếu chưa thể đọc (SDK chưa có/dịch vụ chưa khởi tạo) → dùng cache hiện có.
        /// NÉM nếu đọc lỗi (caller fail-closed: giữ cache, không mở tầng nào mới).
        /// EditMode test ghi đè để giả lập một kho cloud dùng chung giữa nhiều account.
        /// </summary>
        protected virtual async Task<CloudWorldData> LoadWorldStateFromCloudAsync()
        {
#if UGS_CLOUDSAVE_PRESENT
            if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
                return null;

            var customData = await CloudSaveService.Instance.Data.Custom.LoadAsync(
                CustomId, new HashSet<string> { WorldFloorsKey, WorldBossesKey });
            if (customData == null) return null;

            var data = new CloudWorldData();
            if (customData.TryGetValue(WorldFloorsKey, out var floorsItem))
                data.Floors = floorsItem.Value.GetAs<Dictionary<string, string>>();
            if (customData.TryGetValue(WorldBossesKey, out var bossesItem))
                data.Bosses = bossesItem.Value.GetAs<Dictionary<string, string>>();
            return data;
#else
            await Task.CompletedTask;
            return null;
#endif
        }

        private async Task RefreshFromCloudIfPossible()
        {
            try
            {
                var data = await LoadWorldStateFromCloudAsync();
                if (data != null) ApplyCloudData(data);
            }
            catch (Exception ex)
            {
                // Fail-closed: đọc cloud lỗi thì giữ nguyên cache. Tầng chưa từng được xác nhận
                // không có trong cache nên vẫn Locked — không bao giờ tự mở tầng khi đọc lỗi.
                Debug.LogWarning($"[WorldStateService] Không tải được Custom World State từ cloud: {ex.Message}");
            }
        }

        private void ApplyCloudData(CloudWorldData data)
        {
            if (data.Floors != null)
            {
                foreach (var kvp in data.Floors)
                {
                    if (Enum.TryParse<FloorStatus>(kvp.Value, true, out var parsed))
                        _floorStatuses[kvp.Key] = parsed;
                }
            }

            if (data.Bosses != null)
            {
                foreach (var kvp in data.Bosses)
                {
                    if (Enum.TryParse<BossStatus>(kvp.Value, true, out var parsed))
                        _bossStatuses[kvp.Key] = parsed;
                }
            }
        }
    }
}
