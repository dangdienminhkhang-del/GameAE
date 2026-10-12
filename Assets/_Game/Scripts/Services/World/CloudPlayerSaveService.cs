using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.World;
using UnityEngine;

#if UGS_CLOUDSAVE_PRESENT
using Unity.Services.CloudSave;
#endif

namespace Game.Services.World
{
    /// <summary>
    /// Cổng nhỏ tới Cloud Save mà <see cref="CloudPlayerSaveService"/> dùng cho đường lưu thường và đọc thường
    /// (dấu chờ đồng bộ, S1). Mặc định là UGS Cloud Save thật; tách ra để test EditMode giả lập cloud lỗi/chậm/có bản cũ.
    /// KHÔNG dùng cho đường "death" (SaveAuthoritativeAsync / LoadAuthoritativeAsync giữ nguyên).
    /// </summary>
    public interface ICloudSavePort
    {
        /// <summary>Cloud đã sẵn sàng (đã đăng nhập, dịch vụ đã khởi tạo) chưa.</summary>
        bool IsReady { get; }

        /// <summary>Đọc key từ cloud. Không có key → default. Lỗi mạng/dịch vụ → NÉM.</summary>
        Task<T> LoadAsync<T>(string key);

        /// <summary>Ghi key lên cloud. Chỉ hoàn tất khi cloud xác nhận; lỗi → NÉM.</summary>
        Task SaveAsync<T>(string key, T value);
    }

    /// <summary>
    /// ISaveService thật (Phase 5): Player Data trên Unity Cloud Save khi đã đăng nhập,
    /// tự động fallback sang file JSON trong persistentDataPath khi offline/chưa đăng nhập.
    /// DTO lưu bằng JsonUtility nên phải là class [Serializable] với field public (xem Core/World/PlayerData.cs).
    /// Cũng là <see cref="ILocalRecoveryStore"/> (D2): file recovery chỉ nằm trên máy, không bao giờ lên cloud.
    ///
    /// Dấu chờ đồng bộ (S1): <see cref="SaveAsync{T}"/> ghi local rồi thử cloud (nuốt lỗi cloud). Cloud lỗi/chưa sẵn sàng thì
    /// key đó có thêm file <c>sync.pending.{key}.json</c> = {playerId}, nghĩa là "bản local MỚI HƠN cloud". Cloud ghi được thì xóa dấu.
    /// Key "death" có cơ chế riêng (SaveAuthoritativeAsync + death.pending.json) nên KHÔNG dùng dấu này.
    /// </summary>
    public class CloudPlayerSaveService : ISaveService, IAuthoritativeSaveService, ILocalRecoveryStore
    {
        /// <summary>Nội dung <c>sync.pending.{key}.json</c>: bản local của key này mới hơn cloud, thuộc tài khoản <c>playerId</c>.</summary>
        [Serializable]
        public class SyncPendingRecord
        {
            public string playerId;
        }

        /// <summary>Nội dung <c>lastPlayerId.json</c>: PlayerId đăng nhập gần nhất (dùng khi mất mạng nên chưa biết PlayerId).</summary>
        [Serializable]
        public class LastPlayerRecord
        {
            public string playerId;
        }

        /// <summary>Tên recovery của file dấu chờ cho <paramref name="key"/>; file thật là <c>sync.pending.{key}.json</c>.</summary>
        public static string PendingMarkerName(string key) => SyncPendingPrefix + key;

        private const string SyncPendingPrefix = "sync.pending.";
        private const string LastPlayerIdName = "lastPlayerId";

        // Bộ đếm "lần lưu thứ mấy" theo key. static vì ServiceProvider.CreateSaveService() tạo instance mới mỗi lần gọi,
        // mà mọi instance dùng chung một thư mục phải thấy chung một bộ đếm. Khóa = đường dẫn file dấu chờ.
        private static readonly Dictionary<string, int> SaveVersions = new Dictionary<string, int>();
        private static readonly object SaveVersionsLock = new object();
        private static bool _warnedUnknownPlayer;

        private readonly string _localDirectory;
        private readonly IAccountService _account;
        private readonly ICloudSavePort _cloud;
        private string _lastPersistedPlayerId;

        /// <param name="localDirectory">Thư mục file local. Bỏ trống → persistentDataPath/PlayerData.</param>
        /// <param name="account">
        /// Nguồn PlayerId cho dấu chờ đồng bộ. Bỏ trống → dịch vụ tài khoản dùng chung của app
        /// (<see cref="ServiceProvider.GetAccountService"/>); chủ yếu để test truyền giả.
        /// </param>
        /// <param name="cloud">
        /// Cổng tới Cloud Save cho đường lưu/đọc thường. Bỏ trống → UGS Cloud Save thật (không có UGS → không có cloud);
        /// chủ yếu để test truyền giả.
        /// </param>
        public CloudPlayerSaveService(string localDirectory = null, IAccountService account = null, ICloudSavePort cloud = null)
        {
            _localDirectory = string.IsNullOrEmpty(localDirectory)
                ? Path.Combine(Application.persistentDataPath, "PlayerData")
                : localDirectory;
            _account = account;
            _cloud = cloud ?? CreateDefaultCloudPort();
        }

        public string LocalDirectory => _localDirectory;

        private static bool CloudReady
        {
            get
            {
#if UGS_CLOUDSAVE_PRESENT
                try
                {
                    return Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized;
                }
                catch
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public async Task<T> LoadAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;

            // Dấu chờ đồng bộ (S1): tiến độ chơi lúc mất mạng chỉ có ở file local, bản cloud đang CŨ hơn.
            // Có dấu của ĐÚNG tài khoản đang đăng nhập → đọc local (mới hơn) rồi đẩy lên cloud.
            // Không có dấu, hoặc dấu của tài khoản khác → như cũ (cloud trước, lỗi mới dùng local), không đụng vào dấu.
            if (HasOwnPendingMarker(key))
            {
                T local = LoadLocal<T>(key);
                if ((object)local != null)
                {
                    await PushPendingAsync(key, local);
                    return local;
                }
                // File local mất/hỏng thì không còn gì mới hơn cloud để giữ → đọc cloud như bình thường.
            }

            return await LoadCloudFirstAsync<T>(key);
        }

        /// <summary>Đường đọc thường: cloud trước, cloud lỗi/chưa sẵn sàng mới dùng file local.</summary>
        private async Task<T> LoadCloudFirstAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;

            if (_cloud != null && _cloud.IsReady)
            {
                try
                {
                    return await _cloud.LoadAsync<T>(key);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[CloudPlayerSaveService] Cloud load '{key}' lỗi, dùng file local: {error.Message}");
                }
            }

            return LoadLocal<T>(key);
        }

        /// <summary>
        /// Đọc từ nguồn chính, KHÔNG fallback local khi cloud lỗi (fail-closed — dùng cho key "death").
        /// - Có UGS Cloud Save: cloud chưa sẵn sàng hoặc lỗi mạng → ném SaveUnavailableException.
        /// - Chưa có UGS (mock/offline hiện tại): file local là nguồn duy nhất nên đọc local.
        /// </summary>
        public async Task<T> LoadAuthoritativeAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;

#if UGS_CLOUDSAVE_PRESENT
            if (!CloudReady) throw new SaveUnavailableException(key, null);

            try
            {
                Dictionary<string, Unity.Services.CloudSave.Models.Item> data =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                if (data != null && data.TryGetValue(key, out var item))
                {
                    return item.Value.GetAs<T>();
                }
                return default;
            }
            catch (Exception error)
            {
                throw new SaveUnavailableException(key, error);
            }
#else
            await Task.CompletedTask;
            return LoadLocal<T>(key);
#endif
        }

        public async Task SaveAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;

            SaveLocal(key, value);

            // Dấu chờ đồng bộ (S1): chỉ key thường và chỉ khi có cloud để đồng bộ. Key "death" có cơ chế riêng.
            bool tracked = IsTrackedKey(key);
            string playerId = tracked ? ResolvePlayerId() : null; // luôn gọi để ghi lastPlayerId.json khi đang biết PlayerId
            int version = tracked ? BeginSaveVersion(key) : 0;

            bool cloudConfirmed = await TrySaveCloudAsync(key, value);

            if (!tracked) return;

            if (!cloudConfirmed)
            {
                // Cloud lỗi/chưa sẵn sàng → bản local giờ MỚI HƠN cloud. Luôn ghi dấu (kể cả khi đây không còn là
                // lần lưu mới nhất: dấu thừa chỉ tốn một lần đẩy lên cloud, còn thiếu dấu thì mất tiến độ).
                WritePendingMarker(key, playerId);
            }
            else if (IsLatestSaveVersion(key, version))
            {
                // Chỉ lần lưu MỚI NHẤT được xóa dấu: một lần lưu cũ hoàn tất muộn không được xóa dấu của lần lưu mới hơn.
                // (Nếu dấu đang là của tài khoản khác thì file local của họ cũng đã bị SaveLocal ở trên ghi đè rồi → dấu đó hết nghĩa.)
                ClearPendingMarker(key);
            }
        }

        /// <summary>
        /// Ghi dữ liệu QUAN TRỌNG (key "death", docs/09 §1.3): ghi local trước (bản recovery), rồi ghi nguồn chính
        /// và CHỈ hoàn tất khi nguồn chính xác nhận. Khác <see cref="SaveAsync{T}"/> (nuốt lỗi cloud).
        /// - Có UGS Cloud Save: cloud chưa sẵn sàng hoặc lỗi mạng → ném SaveUnavailableException (local vẫn giữ làm recovery).
        /// - Chưa có UGS (mock/offline hiện tại): file local là nguồn duy nhất → ghi local thành công là xác nhận;
        ///   ghi local lỗi thì ném SaveUnavailableException.
        /// </summary>
        public async Task SaveAuthoritativeAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key rỗng", nameof(key));

            bool localSaved = TrySaveLocal(key, value, out Exception localError);

#if UGS_CLOUDSAVE_PRESENT
            if (!localSaved)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Không ghi được bản recovery local '{key}': {localError?.Message}");
            }

            if (!CloudReady) throw new SaveUnavailableException(key, null);

            try
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, value } });
            }
            catch (Exception error)
            {
                throw new SaveUnavailableException(key, error);
            }
#else
            if (!localSaved) throw new SaveUnavailableException(key, localError);
            await Task.CompletedTask;
#endif
        }

        public async Task DeleteAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            DeleteLocal(key);

            // Dấu chờ đồng bộ (S1): dấu nói "file local của key này mới hơn cloud". File local đã bị xóa thì dấu hết nghĩa,
            // nên xóa dấu của key này NGAY SAU khi xóa local (trước cả bước gọi cloud bên dưới, để cloud lỗi cũng không sót dấu).
            // Xóa bất kể dấu thuộc tài khoản nào: reset Guest chạy SAU khi đã đăng nhập tài khoản mới, lúc đó dấu còn lại
            // là của tài khoản CŨ (và file local của họ đang bị xóa). Key "death" không có dấu nên không đụng tới.
            if (IsTrackedKey(key)) ClearPendingMarker(key);

#if UGS_CLOUDSAVE_PRESENT
            if (CloudReady)
            {
                try
                {
                    await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[CloudPlayerSaveService] Cloud delete '{key}' lỗi: {error.Message}");
                }
            }
#endif
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, object>();
            if (keys == null) return result;

            foreach (string key in keys)
            {
                string json = ReadLocalJson(key);
                if (json != null) result[key] = json;
            }
            await Task.CompletedTask;
            return result;
        }

        public async Task SaveBatchAsync(Dictionary<string, object> keyValues)
        {
            if (keyValues == null) return;

            foreach (var pair in keyValues)
            {
                if (pair.Value is string json) WriteLocalJson(pair.Key, json);
            }
            await Task.CompletedTask;
        }

        // ── Dấu chờ đồng bộ (S1) ──────────────────────────────────────────────

        /// <summary>Key có dùng dấu chờ không: không có cloud (không có gì để đồng bộ) hoặc key "death" (cơ chế riêng) → không.</summary>
        private bool IsTrackedKey(string key)
        {
            return _cloud != null && !string.IsNullOrEmpty(key) && key != SaveKeys.Death;
        }

        private static ICloudSavePort CreateDefaultCloudPort()
        {
#if UGS_CLOUDSAVE_PRESENT
            return new UgsCloudSavePort();
#else
            return null;
#endif
        }

#if UGS_CLOUDSAVE_PRESENT
        /// <summary>Cổng mặc định: UGS Cloud Save thật (giữ nguyên cách gọi cũ).</summary>
        private sealed class UgsCloudSavePort : ICloudSavePort
        {
            public bool IsReady => CloudReady;

            public async Task<T> LoadAsync<T>(string key)
            {
                Dictionary<string, Unity.Services.CloudSave.Models.Item> data =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                if (data != null && data.TryGetValue(key, out var item))
                {
                    return item.Value.GetAs<T>();
                }
                return default;
            }

            public async Task SaveAsync<T>(string key, T value)
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, value } });
            }
        }
#endif

        /// <summary>Ghi key lên cloud. True = cloud XÁC NHẬN đã ghi. Chưa sẵn sàng hoặc lỗi → false (không ném, chỉ log).</summary>
        private async Task<bool> TrySaveCloudAsync<T>(string key, T value)
        {
            if (_cloud == null || !_cloud.IsReady) return false;

            try
            {
                await _cloud.SaveAsync(key, value);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Cloud save '{key}' lỗi (đã lưu local): {error.Message}");
                return false;
            }
        }

        private int BeginSaveVersion(string key)
        {
            string versionKey = PathFor(PendingMarkerName(key));
            lock (SaveVersionsLock)
            {
                SaveVersions.TryGetValue(versionKey, out int version);
                version++;
                SaveVersions[versionKey] = version;
                return version;
            }
        }

        private bool IsLatestSaveVersion(string key, int version)
        {
            string versionKey = PathFor(PendingMarkerName(key));
            lock (SaveVersionsLock)
            {
                return SaveVersions.TryGetValue(versionKey, out int latest) && latest == version;
            }
        }

        private void WritePendingMarker(string key, string playerId)
        {
            if (playerId == null)
            {
                // Không biết file này của ai thì không đối chiếu được lúc load → không tạo dấu mồ côi.
                if (!_warnedUnknownPlayer)
                {
                    _warnedUnknownPlayer = true;
                    Debug.LogWarning("[CloudPlayerSaveService] Cloud lỗi nhưng chưa biết PlayerId (chưa đăng nhập lần nào) " +
                                     "nên không tạo được dấu chờ đồng bộ — tiến độ vẫn nằm ở file local.");
                }
                return;
            }

            TryWriteRecovery(PendingMarkerName(key), new SyncPendingRecord { playerId = playerId });
        }

        private void ClearPendingMarker(string key)
        {
            DeleteRecovery(PendingMarkerName(key));
        }

        /// <summary>
        /// PlayerId của tài khoản hiện tại: lấy từ <see cref="IAccountService"/> (như DeathPersistenceService, D2);
        /// chưa biết (mất mạng/chưa đăng nhập) thì dùng PlayerId đăng nhập gần nhất đã lưu ở <c>lastPlayerId.json</c>.
        /// Mỗi lần biết PlayerId thật thì ghi lại vào <c>lastPlayerId.json</c>. Không biết gì cả → null.
        /// </summary>
        private string ResolvePlayerId()
        {
            string current = TryGetAccountPlayerId();
            if (current != null)
            {
                RememberPlayerId(current);
                return current;
            }

            return ReadLastPlayerId();
        }

        private string TryGetAccountPlayerId()
        {
            try
            {
                IAccountService account = _account ?? ServiceProvider.GetAccountService();
                string id = account?.PlayerId;
                return string.IsNullOrEmpty(id) ? null : id;
            }
            catch
            {
                // AuthenticationService.Instance ném lỗi khi UGS chưa khởi tạo — không phải sự cố, chỉ là chưa biết PlayerId.
                return null;
            }
        }

        private void RememberPlayerId(string playerId)
        {
            if (string.Equals(_lastPersistedPlayerId, playerId, StringComparison.Ordinal)) return;

            // Đọc file trước để không ghi đĩa lại mỗi lần lưu khi PlayerId không đổi.
            if (string.Equals(ReadLastPlayerId(), playerId, StringComparison.Ordinal) ||
                TryWriteRecovery(LastPlayerIdName, new LastPlayerRecord { playerId = playerId }))
            {
                _lastPersistedPlayerId = playerId;
            }
        }

        private string ReadLastPlayerId()
        {
            LastPlayerRecord record = ReadRecovery<LastPlayerRecord>(LastPlayerIdName);
            return string.IsNullOrEmpty(record?.playerId) ? null : record.playerId;
        }

        /// <summary>
        /// Có dấu chờ của key và dấu đó thuộc ĐÚNG tài khoản đang đăng nhập không.
        /// Không có dấu / dấu hỏng / chưa biết mình là ai / dấu của tài khoản KHÁC (cùng máy) → false, và KHÔNG đụng vào file dấu.
        /// </summary>
        private bool HasOwnPendingMarker(string key)
        {
            if (!IsTrackedKey(key)) return false;

            // Luôn resolve để lastPlayerId.json được ghi khi đang biết PlayerId (kể cả khi chưa có dấu nào).
            string current = ResolvePlayerId();

            SyncPendingRecord marker = ReadRecovery<SyncPendingRecord>(PendingMarkerName(key));
            if (marker == null || string.IsNullOrEmpty(marker.playerId)) return false;

            return current != null && string.Equals(marker.playerId, current, StringComparison.Ordinal);
        }

        /// <summary>
        /// Đẩy bản local (mới hơn) lên cloud. Được → xóa dấu (chỉ khi đây vẫn là lần ghi mới nhất của key,
        /// cùng quy tắc với <see cref="SaveAsync{T}"/>). Chưa được (mất mạng/lỗi) → GIỮ dấu, lần mở sau thử lại.
        /// </summary>
        private async Task PushPendingAsync<T>(string key, T local)
        {
            int version = BeginSaveVersion(key);
            bool confirmed = await TrySaveCloudAsync(key, local);

            if (confirmed && IsLatestSaveVersion(key, version)) ClearPendingMarker(key);
        }

        // ── ILocalRecoveryStore (D2): file recovery chỉ nằm trên máy ──────────

        /// <summary>Ghi <c>{name}.json</c> trong thư mục local. KHÔNG đụng cloud. Lỗi → false (không ném).</summary>
        public bool TryWriteRecovery<T>(string name, T value)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (TrySaveLocal(name, value, out Exception error)) return true;

            Debug.LogWarning($"[CloudPlayerSaveService] Không ghi được file recovery '{name}': {error?.Message}");
            return false;
        }

        /// <summary>Đọc <c>{name}.json</c>. Không có file hoặc file hỏng → default (LoadLocal đã log cảnh báo nếu hỏng).</summary>
        public T ReadRecovery<T>(string name)
        {
            return string.IsNullOrEmpty(name) ? default : LoadLocal<T>(name);
        }

        /// <summary>Xóa <c>{name}.json</c>. Không có file → true. Xóa lỗi (bị khóa...) → false.</summary>
        public bool DeleteRecovery(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            try
            {
                string path = PathFor(name);
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Không xóa được file recovery '{name}': {error.Message}");
                return false;
            }
        }

        // ── Local file fallback ───────────────────────────────────

        private string PathFor(string key)
        {
            return Path.Combine(_localDirectory, key + ".json");
        }

        private T LoadLocal<T>(string key)
        {
            string json = ReadLocalJson(key);
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Local load '{key}' lỗi: {error.Message}");
                return default;
            }
        }

        private void SaveLocal<T>(string key, T value)
        {
            try
            {
                Directory.CreateDirectory(_localDirectory);
                WriteLocalJson(key, JsonUtility.ToJson(value, true));
            }
            catch (Exception error)
            {
                Debug.LogError($"[CloudPlayerSaveService] Local save '{key}' lỗi: {error.Message}");
            }
        }

        /// <summary>Ghi local nhưng KHÔNG nuốt lỗi: trả false + lỗi để caller quyết định (dùng cho ghi quan trọng).</summary>
        private bool TrySaveLocal<T>(string key, T value, out Exception error)
        {
            try
            {
                Directory.CreateDirectory(_localDirectory);
                WriteLocalJson(key, JsonUtility.ToJson(value, true));
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        private void DeleteLocal(string key)
        {
            try
            {
                string path = PathFor(key);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Local delete '{key}' lỗi: {error.Message}");
            }
        }

        private string ReadLocalJson(string key)
        {
            try
            {
                string path = PathFor(key);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch
            {
                return null;
            }
        }

        private void WriteLocalJson(string key, string json)
        {
            Directory.CreateDirectory(_localDirectory);
            File.WriteAllText(PathFor(key), json);
        }
    }
}
