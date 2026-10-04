using System.Threading.Tasks;
using Game.Core.Account;

namespace Game.Services.Account
{
    public class AccountLinkService : IAccountLinkService
    {
        public virtual async Task<bool> LinkWithGoogleAsync()
        {
            await Task.CompletedTask;
            return false;
        }

        public virtual async Task<bool> LinkWithFacebookAsync()
        {
            await Task.CompletedTask;
            return false;
        }

        public virtual async Task<bool> LinkWithAppleAsync()
        {
            await Task.CompletedTask;
            return false;
        }
    }
}
