using System.Threading.Tasks;

namespace Game.Core.Account
{
    public interface IAccountResetService
    {
        Task<bool> ResetGuestAccountAsync();
        bool CanResetCurrentAccount();
    }
}
