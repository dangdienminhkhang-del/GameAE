using System.Threading.Tasks;
using Game.Core.Account;

#if UGS_CORE_PRESENT
using Unity.Services.Core;
#endif

#if UGS_AUTH_PRESENT
using Unity.Services.Authentication;
#endif

namespace Game.Services.Account
{
    public class UGSAccountService : IAccountService
    {
        public bool IsSignedIn
        {
            get
            {
#if UGS_AUTH_PRESENT
                return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;
#else
                return false;
#endif
            }
        }

        public string PlayerId
        {
            get
            {
#if UGS_AUTH_PRESENT
                return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn
                    ? AuthenticationService.Instance.PlayerId
                    : null;
#else
                return null;
#endif
            }
        }

        public AccountType AccountType
        {
            get
            {
#if UGS_AUTH_PRESENT
                // Guest = chỉ identity ẩn danh; đã link provider ngoài (Google/Apple/Facebook/Unity) → Official.
                return IsLinked ? AccountType.Official : AccountType.Guest;
#else
                return AccountType.Guest;
#endif
            }
        }

        public LinkProvider LinkedProvider => LinkProvider.None;

        public bool IsLinked
        {
            get
            {
#if UGS_AUTH_PRESENT
                // Đúng cách: xem danh sách identity của PlayerInfo. KHÔNG dùng SessionTokenExists —
                // giá trị đó gần như luôn true sau lần đăng nhập đầu, khiến mọi Guest bị coi là "đã link".
                try
                {
                    if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn) return false;

                    var info = AuthenticationService.Instance.PlayerInfo;
                    if (info == null || info.Identities == null) return false;

                    for (int i = 0; i < info.Identities.Count; i++)
                    {
                        var identity = info.Identities[i];
                        if (identity != null && identity.TypeId != "anonymous") return true;
                    }
                    return false;
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

        public virtual async Task InitializeAsync()
        {
#if UGS_CORE_PRESENT
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (ServicesInitializationException ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Init failed: {ex}");
                throw;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Init generic error: {ex}");
                throw;
            }
#else
            await Task.CompletedTask;
            throw new System.InvalidOperationException("[UGSAccountService] Unity.Services.Core package not installed. Install via Package Manager.");
#endif
        }

        public virtual async Task<bool> SignInAnonymouslyAsync()
        {
#if UGS_AUTH_PRESENT
            try
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    UnityEngine.Debug.Log($"[UGSAccountService] Already signed in: {AuthenticationService.Instance.PlayerId}");
                    return true;
                }
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                UnityEngine.Debug.Log($"[UGSAccountService] Sign-in anonymous OK: PlayerId={AuthenticationService.Instance.PlayerId}");
                return true;
            }
            catch (AuthenticationException ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Auth exception: {ex}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Request failed: code={ex.ErrorCode}, msg={ex.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Sign-in generic error: {ex}");
                return false;
            }
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        public virtual async Task SignOutAsync()
        {
#if UGS_AUTH_PRESENT
            try
            {
                if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SignOut(true);
                    UnityEngine.Debug.Log("[UGSAccountService] Signed out.");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[UGSAccountService] Sign-out error: {ex}");
            }
            await Task.CompletedTask;
#else
            await Task.CompletedTask;
#endif
        }
    }
}
