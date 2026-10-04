using System.Threading.Tasks;

namespace Game.Core.Account
{
    public interface IAccountLinkService
    {
        Task<bool> LinkWithGoogleAsync();
        Task<bool> LinkWithFacebookAsync();
        Task<bool> LinkWithAppleAsync();
    }
}
