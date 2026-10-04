using System.Threading.Tasks;
using Game.Core.Account;

namespace Game.Core.Account
{
    public interface IAccountService
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        AccountType AccountType { get; }
        LinkProvider LinkedProvider { get; }
        bool IsLinked { get; }

        Task InitializeAsync();
        Task<bool> SignInAnonymouslyAsync();
        Task SignOutAsync();
    }
}
