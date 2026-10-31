using System.Collections;
using Game.PvE.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Shop
{
    /// <summary>
    /// Hiển thị số Col ở góc trên-trái (dưới đồng hồ) + chữ "+N Col" khi nhặt Col từ quái.
    /// Bố cục (huy hiệu tròn + thanh viên thuốc) nằm trong scene PvE_Gameplay → WalletHud;
    /// script này chỉ cập nhật chữ, không tự đặt lại vị trí lúc chạy.
    /// </summary>
    public class WalletHud : MonoBehaviour
    {
        private const float GainShowSeconds = 1.6f;
        private const float GainFadeSeconds = 0.45f;

        [SerializeField] private PlayerWallet wallet;
        [Tooltip("Chữ hiện số Col (ví dụ \"1.654 Col\").")]
        [SerializeField] private Text label;
        [Tooltip("Chữ hiện \"+N Col\" khi nhận thêm. Để trống = không hiện.")]
        [SerializeField] private Text gain;

        private Coroutine _gainRoutine;

        private void Awake()
        {
            if (label == null) label = FindText("ColLabel");
            if (gain == null) gain = FindText("ColGain");
            if (gain != null) gain.text = "";
        }

        private Text FindText(string objectName)
        {
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t.name == objectName) return t;
            }
            return null;
        }

        private void OnEnable()
        {
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (wallet == null) return;
            wallet.Changed += Refresh;
            wallet.Earned += ShowGain;
            Refresh();
        }

        private void OnDisable()
        {
            if (wallet == null) return;
            wallet.Changed -= Refresh;
            wallet.Earned -= ShowGain;
        }

        private void Refresh()
        {
            if (label != null && wallet != null)
            {
                // Số lớn, chữ "Col" nhỏ hơn cho gọn (Text legacy hỗ trợ rich text).
                label.text = wallet.Col.ToString("N0") + "<size=20> Col</size>";
            }
        }

        private void ShowGain(int amount)
        {
            if (gain == null) return;
            if (_gainRoutine != null) StopCoroutine(_gainRoutine);
            _gainRoutine = StartCoroutine(GainRoutine(amount));
        }

        private IEnumerator GainRoutine(int amount)
        {
            gain.text = "+" + amount + " Col";
            SetGainAlpha(1f);
            yield return new WaitForSeconds(GainShowSeconds);

            for (float t = 0f; t < GainFadeSeconds; t += UnityEngine.Time.unscaledDeltaTime)
            {
                SetGainAlpha(1f - t / GainFadeSeconds);
                yield return null;
            }

            gain.text = "";
            SetGainAlpha(1f);
            _gainRoutine = null;
        }

        private void SetGainAlpha(float alpha)
        {
            var c = gain.color;
            c.a = alpha;
            gain.color = c;
        }
    }
}
