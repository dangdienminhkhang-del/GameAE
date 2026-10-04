using System.Threading.Tasks;
using Game.Core.Account;

// ============================================================
// PHASE 1 NOTE: Hiện tại đây là PLACEHOLDER (mock/in-memory) AccountService.
// NHIỆM VỤ CỦA USER TRƯỚC KHI UPGRADE SANG UGS THẬT:
//   1. Package Manager → Unity Registry → Install 5 packages:
//      • Services Core, Authentication, Cloud Save, Lobby, Relay
//   2. Báo lại AI để upgrade class này sang UGS SDK THẬT:
//      • UnityServices.InitializeAsync()
//      • AuthenticationService.Instance.SignInAnonymouslyAsync()
// ============================================================
namespace Game.Services.Account
{
    public class AccountService : IAccountService
    {
        public bool IsSignedIn { get; private set; }
        public string PlayerId { get; private set; }
        public AccountType AccountType { get; private set; }
        public LinkProvider LinkedProvider { get; private set; }
        public bool IsLinked { get; private set; }

        public virtual async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public virtual async Task<bool> SignInAnonymouslyAsync()
        {
            await Task.CompletedTask;
            IsSignedIn = true;
            PlayerId = "guest_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            AccountType = Game.Core.Account.AccountType.Guest;
            IsLinked = false;
            LinkedProvider = Game.Core.Account.LinkProvider.None;
            return true;
        }

        public virtual async Task SignOutAsync()
        {
            await Task.CompletedTask;
            IsSignedIn = false;
            PlayerId = null;
        }
    }
}
