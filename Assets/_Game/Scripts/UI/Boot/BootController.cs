using System;
using System.Collections;
using Game.Core.Account;
using Game.Services;
using Game.Services.Config;
using Game.UI.Auth;
using Game.UI.Common;
using UnityEngine;

namespace Game.UI.Boot
{
    public class BootController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig;

        private IAccountService _account;
        private IAccountResetService _reset;
        private IAccountLinkService _link;

        public AuthErrorScreen authErrorScreen;
        public GameObject loadingIndicator;

        private void Awake()
        {
            if (backendConfig != null)
            {
                ServiceProvider.SetConfig(backendConfig);
            }

            _account = ServiceProvider.CreateAccountService();
            _reset = ServiceProvider.CreateAccountResetService();
            _link = ServiceProvider.CreateAccountLinkService();

            if (authErrorScreen != null)
            {
                authErrorScreen.gameObject.SetActive(false);
                authErrorScreen.OnRetryClicked += HandleRetryClicked;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
            }
        }

        private IEnumerator Start()
        {
            yield return RunAuthenticationFlow();
        }

        private void OnDestroy()
        {
            if (authErrorScreen != null)
            {
                authErrorScreen.OnRetryClicked -= HandleRetryClicked;
            }
        }

        private IEnumerator RunAuthenticationFlow()
        {
            if (authErrorScreen != null) authErrorScreen.gameObject.SetActive(false);
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            Exception initError = null;
            bool initOk = false;
            yield return WrapAsync(async () =>
            {
                try
                {
                    await _account.InitializeAsync();
                    initOk = true;
                }
                catch (Exception ex)
                {
                    initError = ex;
                }
            });
            if (!initOk)
            {
                ShowError("Không thể kết nối dịch vụ", initError);
                yield break;
            }

            Exception signInError = null;
            bool signInOk = false;
            yield return WrapAsync(async () =>
            {
                try
                {
                    signInOk = await _account.SignInAnonymouslyAsync();
                }
                catch (Exception ex)
                {
                    signInError = ex;
                    signInOk = false;
                }
            });

            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            if (!signInOk || !_account.IsSignedIn)
            {
                ShowError("Đăng nhập thất bại. Kiểm tra mạng.", signInError);
                yield break;
            }

            Debug.Log($"[Boot] Signed in. PlayerId={_account.PlayerId}");
            yield return SceneLoader.LoadSceneAsync(SceneLoader.MainMenuSceneName);
        }

        private void ShowError(string message, Exception ex)
        {
            if (ex != null)
            {
                Debug.LogError($"[Boot] {message}: {ex}");
            }
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (authErrorScreen != null)
            {
                authErrorScreen.Show(message);
            }
        }

        private void HandleRetryClicked(object sender, EventArgs e)
        {
            StartCoroutine(RunAuthenticationFlow());
        }

        /// <summary>
        /// Chặn treo vô hạn: UGS có thể không bao giờ trả task (mất mạng, hoặc Editor bật
        /// "Enter Play Mode Options" tắt Domain Reload làm SDK kẹt trạng thái cũ). Không có timeout
        /// thì người chơi chỉ thấy "Đang đăng nhập..." mãi mà không biết vì sao.
        /// Quá hạn → ném TimeoutException để ShowError hiện AuthErrorScreen kèm nút "Thử lại".
        /// </summary>
        private const float AuthStepTimeoutSeconds = 20f;

        private static IEnumerator WrapAsync(Func<System.Threading.Tasks.Task> asyncFunc)
        {
            var task = asyncFunc();
            float deadline = UnityEngine.Time.realtimeSinceStartup + AuthStepTimeoutSeconds;

            while (!task.IsCompleted && UnityEngine.Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!task.IsCompleted)
            {
                throw new TimeoutException(
                    $"[Boot] Dịch vụ không phản hồi sau {AuthStepTimeoutSeconds:0}s — kiểm tra mạng rồi thử lại.");
            }

            if (task.IsFaulted && task.Exception != null)
            {
                throw task.Exception;
            }
        }
    }
}
