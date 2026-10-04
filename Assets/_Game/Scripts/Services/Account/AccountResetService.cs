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

            await _account.SignOutAsync();
            bool signedIn = await _account.SignInAnonymouslyAsync();

            // Đăng nhập tài khoản mới thất bại → KHÔNG xóa gì: người chơi vẫn ở Death Screen và thử lại được.
            if (!signedIn) return false;

            await WipePlayerDataAsync();
            return true;
        }

        /// <summary>
        /// Xóa MỌI key Player Data (level, đồ, quest, trạng thái chết...) để nhân vật mới bắt đầu từ đầu.
        /// Chạy SAU khi đã có Player ID mới: tài khoản cũ (đã chết) vẫn giữ nguyên trạng thái bị khóa.
        /// </summary>
        private async Task WipePlayerDataAsync()
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
        }
    }
}
