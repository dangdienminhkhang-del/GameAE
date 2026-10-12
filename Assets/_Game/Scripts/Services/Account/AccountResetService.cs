using System;
using System.Threading.Tasks;
using Game.Core.Account;
using Game.Core.World;
using UnityEngine;

namespace Game.Services.Account
{
    public class AccountResetService : IAccountResetService
    {
        private readonly IAccountService _account;
        private readonly ISaveService _save;

        public AccountResetService()
        {
        }

        public AccountResetService(IAccountService account)
        {
            _account = account;
        }

        /// <param name="save">
        /// Dùng để xóa sạch Player Data của nhân vật cũ khi reset. Bỏ trống → không xóa gì
        /// (chỉ nên dùng trong test); ServiceProvider luôn truyền vào.
        /// </param>
        public AccountResetService(IAccountService account, ISaveService save)
        {
            _account = account;
            _save = save;
        }

        public virtual bool CanResetCurrentAccount()
        {
            try
            {
                return _account != null && _account.AccountType == Game.Core.Account.AccountType.Guest && !_account.IsLinked;
            }
            catch
            {
                // UGS chưa khởi tạo (chơi thẳng scene PvE) → coi như không reset được, không làm sập UI.
                return false;
            }
        }

        public virtual async Task<bool> ResetGuestAccountAsync()
        {
            if (!CanResetCurrentAccount()) return false;
            if (_account == null) return false;

            // D3: lấy PlayerId của tài khoản CŨ trước khi đăng xuất/đăng nhập tài khoản mới — sau đó
            // _account.PlayerId đã là của tài khoản mới, dùng nó sẽ xóa nhầm file recovery của tài khoản mới.
            string oldPlayerId = TryGetPlayerId();

            await _account.SignOutAsync();
            bool signedIn = await _account.SignInAnonymouslyAsync();

            // Đăng nhập tài khoản mới thất bại → KHÔNG xóa gì: người chơi vẫn ở Death Screen và thử lại được.
            if (!signedIn) return false;

            await WipePlayerDataAsync(oldPlayerId);
            return true;
        }

        /// <summary>PlayerId của tài khoản đang đăng nhập; không biết (chưa đăng nhập/UGS chưa khởi tạo) → null.</summary>
        private string TryGetPlayerId()
        {
            try
            {
                string id = _account?.PlayerId;
                return string.IsNullOrEmpty(id) ? null : id;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa recovery record cái chết của ĐÚNG tài khoản cũ: file <c>death.pending.{oldPlayerId}.json</c>, và file chung
        /// cũ <c>death.pending.json</c> (trước D3) nếu playerId trong đó là của tài khoản cũ. File của tài khoản khác (kể cả
        /// tài khoản mới vừa đăng nhập) giữ nguyên. Không biết PlayerId cũ thì không xóa gì — không đoán.
        /// </summary>
        private static void DeleteOldAccountDeathPending(ILocalRecoveryStore recovery, string oldPlayerId)
        {
            if (string.IsNullOrEmpty(oldPlayerId))
            {
                Debug.LogWarning("[AccountReset] Không biết PlayerId của tài khoản cũ nên không xóa death.pending.{playerId}.json nào " +
                                 "(không đoán, không đụng file của tài khoản khác).");
                return;
            }

            try
            {
                string ownName = Game.Services.Character.DeathPersistenceService.PendingRecoveryNameFor(oldPlayerId);
                if (!recovery.DeleteRecovery(ownName))
                {
                    Debug.LogWarning($"[AccountReset] Không xóa được {ownName}.json của tài khoản cũ.");
                }

                // File chung cũ: chỉ xóa khi là của đúng tài khoản cũ (playerId khác → đó là cái chết của người khác, giữ lại).
                const string legacyName = Game.Services.Character.DeathPersistenceService.PendingRecoveryName;
                var legacy = recovery.ReadRecovery<Game.Services.Character.PendingDeathRecord>(legacyName);
                if (legacy != null && string.Equals(legacy.playerId, oldPlayerId, StringComparison.Ordinal))
                {
                    if (!recovery.DeleteRecovery(legacyName))
                    {
                        Debug.LogWarning("[AccountReset] Không xóa được death.pending.json cũ của tài khoản cũ.");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[AccountReset] Không xóa được recovery record cái chết của tài khoản cũ: {error.Message}");
            }
        }

        /// <summary>
        /// Xóa MỌI key Player Data (level, đồ, quest, trạng thái chết...) để nhân vật mới bắt đầu từ đầu.
        /// Chạy SAU khi đã có Player ID mới: tài khoản cũ (đã chết) vẫn giữ nguyên trạng thái bị khóa.
        /// </summary>
        /// <param name="oldPlayerId">PlayerId của tài khoản cũ (lấy trước khi đăng nhập tài khoản mới); null = không biết.</param>
        private async Task WipePlayerDataAsync(string oldPlayerId)
        {
            if (_save == null) return;

            foreach (string key in SaveKeys.All)
            {
                try
                {
                    await _save.DeleteAsync(key);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[AccountReset] Không xóa được Player Data '{key}': {error.Message}");
                }
            }

            // Recovery record cái chết chưa ghi lên cloud được (D2) của tài khoản cũ: nằm NGOÀI SaveKeys.All
            // (file local riêng, không lên cloud) nên phải xóa riêng — nếu không, dữ liệu cũ còn sót trên máy.
            // D3: mỗi tài khoản một file death.pending.{playerId}.json → CHỈ xóa file của tài khoản CŨ.
            if (_save is ILocalRecoveryStore recovery)
            {
                DeleteOldAccountDeathPending(recovery, oldPlayerId);

                // Dấu chờ đồng bộ (S1) của tài khoản cũ: "file local mới hơn cloud" không còn đúng vì file local vừa bị xóa.
                // DeleteAsync ở trên đã xóa từng dấu; đoạn này đảm bảo dấu vẫn mất kể cả khi một DeleteAsync bị lỗi giữa chừng.
                // Key "death" không có dấu (cơ chế riêng ở trên) nên bỏ qua.
                foreach (string key in SaveKeys.All)
                {
                    if (key == SaveKeys.Death) continue;

                    try
                    {
                        if (!recovery.DeleteRecovery(Game.Services.World.CloudPlayerSaveService.PendingMarkerName(key)))
                        {
                            Debug.LogWarning($"[AccountReset] Không xóa được dấu chờ đồng bộ '{key}' của tài khoản cũ.");
                        }
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning($"[AccountReset] Không xóa được dấu chờ đồng bộ '{key}': {error.Message}");
                    }
                }
            }
        }
    }
}
