using System;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace Game.UI.Auth
{
    public class AuthErrorScreen : MonoBehaviour
    {
#if TMP_PRESENT
        [SerializeField] private TMP_Text errorMessageTextTMP;
#endif
        [SerializeField] private Text errorMessageTextLegacy;
        [SerializeField] private Button retryButton;

        public event EventHandler OnRetryClicked;

        private void Awake()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(HandleRetryButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
            }
        }

        public void Show(string message)
        {
            gameObject.SetActive(true);
            var text = message ?? "Lỗi kết nối";
#if TMP_PRESENT
            if (errorMessageTextTMP != null)
            {
                errorMessageTextTMP.text = text;
                return;
            }
#endif
            if (errorMessageTextLegacy != null)
            {
                errorMessageTextLegacy.text = text;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void HandleRetryButtonClicked()
        {
            OnRetryClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
