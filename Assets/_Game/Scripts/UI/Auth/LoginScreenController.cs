using System;
using System.Collections;
using Game.Core.Account;
using Game.Services;
using Game.Services.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Auth
{
    public class LoginScreenController : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig;
        [SerializeField] private Button resetGuestButton;
        [SerializeField] private GameObject resetConfirmationDialog;
        [SerializeField] private Button confirmResetButton;
        [SerializeField] private Button cancelResetButton;

        private IAccountService _account;
        private IAccountResetService _resetService;

        public event EventHandler OnGuestResetCompleted;

        private void Awake()
        {
            if (backendConfig != null)
            {
                ServiceProvider.SetConfig(backendConfig);
            }

            _account = ServiceProvider.CreateAccountService();
            _resetService = ServiceProvider.CreateAccountResetService();

            if (resetGuestButton != null)
            {
                resetGuestButton.onClick.AddListener(HandleResetGuestClicked);
            }
            if (confirmResetButton != null)
            {
                confirmResetButton.onClick.AddListener(HandleConfirmResetClicked);
            }
            if (cancelResetButton != null)
            {
                cancelResetButton.onClick.AddListener(HandleCancelResetClicked);
            }
            if (resetConfirmationDialog != null)
            {
                resetConfirmationDialog.SetActive(false);
            }
        }

        private void Start()
        {
            RefreshResetButtonState();
        }

        private void OnDestroy()
        {
            if (resetGuestButton != null) resetGuestButton.onClick.RemoveListener(HandleResetGuestClicked);
            if (confirmResetButton != null) confirmResetButton.onClick.RemoveListener(HandleConfirmResetClicked);
            if (cancelResetButton != null) cancelResetButton.onClick.RemoveListener(HandleCancelResetClicked);
        }

        public void RefreshResetButtonState()
        {
            if (resetGuestButton != null)
            {
                resetGuestButton.gameObject.SetActive(_resetService.CanResetCurrentAccount());
            }
        }

        private void HandleResetGuestClicked()
        {
            if (!_resetService.CanResetCurrentAccount()) return;
            if (resetConfirmationDialog != null)
            {
                resetConfirmationDialog.SetActive(true);
            }
            else
            {
                StartCoroutine(DoReset());
            }
        }

        private void HandleCancelResetClicked()
        {
            if (resetConfirmationDialog != null)
            {
                resetConfirmationDialog.SetActive(false);
            }
        }

        private void HandleConfirmResetClicked()
        {
            if (resetConfirmationDialog != null)
            {
                resetConfirmationDialog.SetActive(false);
            }
            StartCoroutine(DoReset());
        }

        private IEnumerator DoReset()
        {
            Exception ex = null;
            bool ok = false;
            yield return WrapAsync(async () =>
            {
                try
                {
                    ok = await _resetService.ResetGuestAccountAsync();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });

            if (ok)
            {
                Debug.Log($"[Login] Guest reset complete. New PlayerId={_account.PlayerId}");
                RefreshResetButtonState();
                OnGuestResetCompleted?.Invoke(this, EventArgs.Empty);
            }
            else if (ex != null)
            {
                Debug.LogError($"[Login] Guest reset failed: {ex}");
            }
        }

        private static IEnumerator WrapAsync(Func<System.Threading.Tasks.Task> asyncFunc)
        {
            var task = asyncFunc();
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted && task.Exception != null)
            {
                throw task.Exception;
            }
        }
    }
}
