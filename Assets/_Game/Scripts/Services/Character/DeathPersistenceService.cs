using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.Character;
using Game.Core.Events;
using Game.Core.World;
using UnityEngine;

namespace Game.Services.Character
{
    /// <summary>
    /// Nội dung file <c>death.pending.{playerId}.json</c> (mỗi tài khoản một file, D3): một cái chết CHƯA ghi lên nguồn chính được,
    /// gắn với đúng một tài khoản (nhiều tài khoản có thể dùng chung một máy → phải biết file này của ai).
    /// </summary>
    [Serializable]
    public class PendingDeathRecord
    {
        public string playerId;
        public DeathInfo deathInfo;
    }

    /// <summary>
    /// Ghi nhận permadeath và LƯU vào Player Data (key "death", docs/03 §5 / 09 §1.3–1.4, ADR-021).
    /// "Khóa account" là logic game (isDead = true), KHÔNG xóa gì trên UGS.
    ///
    /// Quy tắc critical write: OnPlayerDied / PlayerDeathRecorded CHỈ phát sau khi nguồn chính XÁC NHẬN đã ghi.
    /// Ghi lỗi → vẫn giữ trạng thái "đã chết" trong bộ nhớ (vẫn chặn chơi), KHÔNG phát sự kiện, ném
    /// <see cref="SaveUnavailableException"/>; <see cref="RetryRecordAsync"/> thử ghi lại bản đang chờ.
    ///
    /// Recovery record (D2): ghi nguồn chính lỗi rồi người chơi tắt app thì lần mở sau cloud vẫn nói "còn sống".
    /// Vì vậy khi ghi lỗi, service còn ghi file local <c>death.pending.{playerId}.json</c> = {playerId, DeathInfo}
    /// (D3: mỗi tài khoản một file; qua <see cref="ILocalRecoveryStore"/>). Lần load sau: file của ĐÚNG tài khoản đang đăng nhập thắng cloud → đã chết,
    /// rồi thử đẩy lên cloud; đẩy được thì xóa file. File của tài khoản khác thì bỏ qua và giữ nguyên.
    /// </summary>
    public class DeathPersistenceService : IDeathService
    {
        /// <summary>
        /// Tên recovery record CHUNG cũ (trước D3): <c>death.pending.json</c>, một file cho cả máy — hai tài khoản cùng chết
        /// lúc cloud lỗi thì file người sau đè người trước. Luồng mới KHÔNG còn ghi tên này; chỉ đọc nó để di chuyển
        /// (<see cref="MigrateLegacyPendingRecord"/>) và để AccountResetService dọn đúng file của tài khoản cũ.
        /// Tên mới theo tài khoản: <see cref="PendingRecoveryNameFor"/>.
        /// </summary>
        public const string PendingRecoveryName = "death.pending";

        private const string PendingRecoveryPrefix = "death.pending.";
        private const int MaxPlayerIdPartLength = 64;

        /// <summary>
        /// Tên recovery record RIÊNG của một tài khoản: <c>death.pending.{playerId}</c>
        /// (file thật là <c>death.pending.{playerId}.json</c> trong thư mục local của save service).
        /// PlayerId chỉ giữ chữ a–z A–Z, số, gạch dưới, gạch ngang. Có ký tự khác (hoặc quá dài) thì các ký tự đó bị bỏ
        /// và thêm <c>~</c> + mã băm 64-bit của PlayerId GỐC → tên luôn hợp lệ, giống nhau giữa các lần chạy,
        /// và hai PlayerId khác nhau không trùng tên. PlayerId rỗng/null (không biết tài khoản) → null (không có file nào).
        /// </summary>
        public static string PendingRecoveryNameFor(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            return PendingRecoveryPrefix + SanitizePlayerIdForFileName(playerId);
        }

        private static string SanitizePlayerIdForFileName(string playerId)
        {
            var safe = new StringBuilder(playerId.Length);
            bool changed = false;

            foreach (char c in playerId)
            {
                bool isSafe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (isSafe) safe.Append(c);
                else changed = true;
            }

            if (safe.Length > MaxPlayerIdPartLength)
            {
                safe.Length = MaxPlayerIdPartLength;
                changed = true;
            }

            // '~' không nằm trong bộ ký tự giữ lại nên chỉ có thể đến từ mã băm: PlayerId an toàn không bao giờ trùng PlayerId đã băm.
            if (changed) safe.Append('~').Append(Fnv1a64Hex(playerId));
            return safe.ToString();
        }

        /// <summary>FNV-1a 64-bit trên UTF-8: ổn định giữa các lần chạy/nền tảng (string.GetHashCode thì không).</summary>
        private static string Fnv1a64Hex(string text)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (byte b in Encoding.UTF8.GetBytes(text))
                {
                    hash ^= b;
                    hash *= 1099511628211UL;
                }
                return hash.ToString("x16");
            }
        }

        private readonly ISaveService _save;
        private readonly IAccountService _account;
        private readonly SemaphoreSlim _writeGate = new SemaphoreSlim(1, 1);
        private DeathInfo _deathInfo = new DeathInfo();
        private bool _awaitingConfirmation;

        /// <param name="saveService">Nơi lưu Player Data. Null → không có persistence.</param>
        /// <param name="accountService">
        /// Nguồn PlayerId để gắn recovery record. Bỏ trống → dùng dịch vụ tài khoản dùng chung của app
        /// (<see cref="ServiceProvider.GetAccountService"/>); chủ yếu để test truyền giả.
        /// </param>
        public DeathPersistenceService(ISaveService saveService = null, IAccountService accountService = null)
        {
            _save = saveService;
            _account = accountService;
        }

        public bool IsPlayerDead => _deathInfo != null && _deathInfo.isDead;
        public DeathInfo CurrentDeathInfo => _deathInfo;

        /// <summary>True khi đã chết trong bộ nhớ nhưng nguồn chính chưa xác nhận đã ghi.</summary>
        public bool IsAwaitingConfirmation => _awaitingConfirmation;

        public event EventHandler<DeathInfo> OnPlayerDied;

        public virtual async Task<DeathInfo> LoadDeathInfoAsync()
        {
            // Đã chết trong bộ nhớ mà chưa ghi được: bản trên nguồn chính còn cũ ("sống") — không được đè lên.
            if (_awaitingConfirmation) return _deathInfo;

            // D3: file chung cũ death.pending.json (trước khi tách theo tài khoản) → chuyển thành file riêng của đúng tài khoản này
            // rồi đi tiếp luồng bình thường bên dưới (đọc file riêng, đẩy lên cloud, xóa khi đẩy được).
            MigrateLegacyPendingRecord();

            // Recovery record của đúng tài khoản đang đăng nhập: lần chết trước ghi cloud lỗi rồi tắt app.
            // Nó thắng cloud (cloud đang "sống" là bản cũ) và cũng không cần đọc cloud — đã chắc chắn là chết.
            DeathInfo recovered = ReadOwnPendingRecord()?.deathInfo;
            if (recovered != null && recovered.isDead)
            {
                _deathInfo = recovered;
                await TryPushRecoveredDeathAsync(recovered);
                return _deathInfo;
            }

            if (_save != null)
            {
                // Fail-closed: đọc nguồn chính, lỗi thì NÉM (SaveUnavailableException) chứ không fallback local
                // — nếu không, tắt mạng/xóa file local là "sống lại" (docs/03 §12).
                DeathInfo loaded = await _save.LoadAuthoritativeAsync<DeathInfo>(SaveKeys.Death);
                if (loaded != null) _deathInfo = loaded;
            }
            return _deathInfo;
        }

        public virtual async Task RecordDeathAsync(DeathRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            // Chỉ chết MỘT lần: gọi lại (2 đòn cùng frame...) không được ghi đè nguyên nhân/thời điểm chết đầu tiên.
            if (IsPlayerDead) return;

            _deathInfo = record.ToDeathInfo();
            _awaitingConfirmation = true;

            await ConfirmPendingAsync();
        }

        public virtual Task RetryRecordAsync()
        {
            return ConfirmPendingAsync();
        }

        [Obsolete("Dùng RecordDeathAsync")]
        public virtual Task OnPlayerDiedAsync(DeathReason reason, string sourceId, int currentLevel, int highestFloor, int playTimeSeconds)
        {
            var record = new DeathRecord(
                reason,
                sourceId,
                currentLevel,
                highestFloor,
                playTimeSeconds,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
            return RecordDeathAsync(record);
        }

        public virtual async Task ResetDeathStateAsync()
        {
            _awaitingConfirmation = false;
            _deathInfo = new DeathInfo();

            // Reset hợp lệ phải xóa luôn recovery record của tài khoản này, nếu không lần load sau file đó
            // sẽ ép "đã chết" trở lại.
            ClearOwnPendingRecord();

            if (_save != null) await _save.SaveAsync(SaveKeys.Death, _deathInfo);
        }

        /// <summary>
        /// Ghi bản đang chờ; chỉ khi nguồn chính xác nhận mới phát sự kiện, và đúng MỘT lần
        /// (hai lần gọi chồng nhau — vd. bấm Thử lại liên tục — chỉ lần ghi thành công đầu tiên phát).
        /// </summary>
        private async Task ConfirmPendingAsync()
        {
            DeathInfo confirmed;

            await _writeGate.WaitAsync();
            try
            {
                if (!_awaitingConfirmation) return; // không có bản chờ, hoặc lượt khác đã xác nhận rồi

                try
                {
                    await WriteDeathAsync(_deathInfo);
                }
                catch (SaveUnavailableException)
                {
                    // Nguồn chính chưa nhận: để lại recovery record trên máy phòng khi người chơi tắt app
                    // trước khi Thử lại được (nếu không, lần mở sau cloud vẫn "sống" → sống lại).
                    WritePendingRecord(_deathInfo);
                    throw;
                }

                _awaitingConfirmation = false;
                ClearOwnPendingRecord(); // nguồn chính đã có → không cần recovery record nữa
                confirmed = _deathInfo;
            }
            finally
            {
                _writeGate.Release();
            }

            OnPlayerDied?.Invoke(this, confirmed);
            GameEvents.RaisePlayerDeathRecorded(this, confirmed);
        }

        /// <summary>
        /// Đẩy cái chết khôi phục từ recovery record lên nguồn chính. Được thì xóa file; chưa được thì GIỮ file,
        /// vẫn "đã chết" và đánh dấu chờ xác nhận (để bản "sống" trên cloud không đè, và <see cref="RetryRecordAsync"/> dùng được).
        /// Không phát sự kiện: cái chết đã xảy ra ở phiên trước, caller đã nhận kết quả "đã chết" từ Load.
        /// </summary>
        private async Task TryPushRecoveredDeathAsync(DeathInfo info)
        {
            await _writeGate.WaitAsync();
            try
            {
                await WriteDeathAsync(info);
                ClearOwnPendingRecord();
            }
            catch (SaveUnavailableException error)
            {
                _awaitingConfirmation = true;
                Debug.LogWarning($"[DeathPersistence] Có death.pending.{{playerId}}.json nhưng chưa đẩy được lên nguồn chính (giữ file, vẫn coi là đã chết): {error.Message}");
            }
            finally
            {
                _writeGate.Release();
            }
        }

        /// <summary>
        /// Ghi bằng SaveAuthoritativeAsync nếu service lưu hỗ trợ (chỉ hoàn tất khi nguồn chính xác nhận),
        /// ngược lại dùng SaveAsync. Mọi lỗi đều thành <see cref="SaveUnavailableException"/> (không nuốt).
        /// </summary>
        private async Task WriteDeathAsync(DeathInfo info)
        {
            if (_save == null) return; // không có persistence → không có gì để chờ xác nhận

            try
            {
                if (_save is IAuthoritativeSaveService authoritative)
                    await authoritative.SaveAuthoritativeAsync(SaveKeys.Death, info);
                else
                    await _save.SaveAsync(SaveKeys.Death, info);
            }
            catch (SaveUnavailableException)
            {
                throw;
            }
            catch (Exception error)
            {
                throw new SaveUnavailableException(SaveKeys.Death, error);
            }
        }

        // ── Recovery record theo tài khoản (death.pending.{playerId}.json) ──────────────────────────────

        /// <summary>
        /// PlayerId của tài khoản đang đăng nhập, lấy từ <see cref="IAccountService"/> có sẵn
        /// (UGS: AuthenticationService.PlayerId). Chưa đăng nhập / UGS chưa khởi tạo (vd. chơi thẳng scene PvE) → null.
        /// </summary>
        private string ResolveCurrentPlayerId()
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

        private void WritePendingRecord(DeathInfo info)
        {
            // Service lưu không có đĩa (vd. bộ nhớ trong) → không có chỗ ghi recovery.
            if (!(_save is ILocalRecoveryStore store)) return;

            string playerId = ResolveCurrentPlayerId();
            if (playerId == null)
            {
                // Không biết file này của ai thì không thể đối chiếu lúc load → không tạo file mồ côi.
                Debug.LogWarning("[DeathPersistence] Không biết PlayerId hiện tại nên không tạo được death.pending.{playerId}.json " +
                                 "(cái chết vẫn chặn chơi trong phiên này, nhưng tắt app trước khi lưu được sẽ mất).");
                return;
            }

            var record = new PendingDeathRecord { playerId = playerId, deathInfo = info };
            // File RIÊNG của tài khoản này: hai tài khoản cùng chết lúc cloud lỗi không còn đè file của nhau.
            if (!store.TryWriteRecovery(PendingRecoveryNameFor(playerId), record))
            {
                Debug.LogWarning("[DeathPersistence] Ghi death.pending.{playerId}.json thất bại — không có bản recovery nếu tắt app trước khi lưu được.");
            }
        }

        /// <summary>
        /// Recovery record của tài khoản đang đăng nhập (CHỈ đọc đúng file <c>death.pending.{playerId}.json</c> của nó);
        /// null nếu không có file / file hỏng / chưa biết PlayerId. File của tài khoản khác không bao giờ được mở,
        /// và file chung cũ <c>death.pending.json</c> không còn được đọc ở luồng này.
        /// </summary>
        private PendingDeathRecord ReadOwnPendingRecord()
        {
            if (!(_save is ILocalRecoveryStore store)) return null;

            // Không biết mình là ai → không có file nào để đọc (không đoán, không đụng file của ai).
            string current = ResolveCurrentPlayerId();
            if (current == null) return null;

            PendingDeathRecord pending = store.ReadRecovery<PendingDeathRecord>(PendingRecoveryNameFor(current));
            if (pending == null) return null;

            // Tên file đã theo tài khoản; vẫn đối chiếu playerId bên trong phòng file bị chép/đổi tên nhầm.
            return string.Equals(pending.playerId, current, StringComparison.Ordinal) ? pending : null;
        }

        /// <summary>
        /// Xóa recovery record của tài khoản đang đăng nhập (CHỈ file <c>death.pending.{playerId}.json</c> của nó).
        /// Chưa biết PlayerId → không xóa gì. File của tài khoản khác và file chung cũ giữ nguyên.
        /// </summary>
        private void ClearOwnPendingRecord()
        {
            if (!(_save is ILocalRecoveryStore store)) return;

            string current = ResolveCurrentPlayerId();
            if (current == null) return;

            store.DeleteRecovery(PendingRecoveryNameFor(current));
        }

        /// <summary>
        /// Di chuyển file chung cũ <c>death.pending.json</c> (trước D3) sang file riêng của tài khoản đang đăng nhập.
        /// - Chưa biết PlayerId, file không có/hỏng, hoặc playerId KHÁC tài khoản hiện tại → không làm gì, giữ nguyên file
        ///   (cái chết đó là của tài khoản khác; tài khoản đó sẽ tự di chuyển khi nó đăng nhập).
        /// - playerId khớp và có cái chết: coi là bản chờ — chép sang <c>death.pending.{playerId}.json</c> rồi mới xóa file cũ,
        ///   để cloud còn lỗi thì bản chờ vẫn nằm an toàn ở file riêng (xóa file cũ trước khi chép xong là mất cái chết).
        ///   Bước đẩy lên cloud do luồng bình thường ngay sau đó lo (<see cref="TryPushRecoveredDeathAsync"/>).
        /// - Đã có sẵn file riêng (luồng mới ghi) thì file riêng thắng, chỉ xóa file cũ.
        /// - Chép không được (đĩa lỗi) → giữ file cũ, lần load sau thử lại.
        /// </summary>
        private void MigrateLegacyPendingRecord()
        {
            if (!(_save is ILocalRecoveryStore store)) return;

            string current = ResolveCurrentPlayerId();
            if (current == null) return;

            PendingDeathRecord legacy = store.ReadRecovery<PendingDeathRecord>(PendingRecoveryName);
            if (legacy == null) return;
            if (!string.Equals(legacy.playerId, current, StringComparison.Ordinal)) return;
            if (legacy.deathInfo == null || !legacy.deathInfo.isDead) return;

            string ownName = PendingRecoveryNameFor(current);
            bool ownExists = store.ReadRecovery<PendingDeathRecord>(ownName) != null;

            if (!ownExists && !store.TryWriteRecovery(ownName, legacy))
            {
                Debug.LogWarning("[DeathPersistence] Không chép được death.pending.json cũ sang file riêng của tài khoản — giữ file cũ, sẽ thử lại lần sau.");
                return;
            }

            store.DeleteRecovery(PendingRecoveryName);
        }
    }
}
