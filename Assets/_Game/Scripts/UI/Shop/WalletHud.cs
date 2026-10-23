using System.Collections;
using Game.PvE.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Shop
{
    /// <summary>Hiển thị số Col ở góc trên-trái (dưới thanh HP/EXP) + chữ "+N" khi nhặt Col từ quái.</summary>
    public class WalletHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private Vector2 anchoredPosition = new Vector2(24f, -150f);

        private Text _label;
        private Text _gain;
        private Coroutine _gainRoutine;

        private void Awake()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label = MakeText("ColLabel", font, 40, new Color(0.95f, 0.76f, 0.31f), anchoredPosition);
            _gain = MakeText("ColGain", font, 32, new Color(0.7f, 1f, 0.8f), anchoredPosition + new Vector2(0f, -46f));
            _gain.text = "";
        }

        private Text MakeText(string name, Font font, int size, Color color, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(420, 50);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.8f); sh.effectDistance = new Vector2(2, -2);
            return t;
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
            if (_label != null && wallet != null) _label.text = wallet.Col.ToString("N0") + " Col";
        }

        private void ShowGain(int amount)
        {
            if (_gain == null) return;
            if (_gainRoutine != null) StopCoroutine(_gainRoutine);
            _gainRoutine = StartCoroutine(GainRoutine(amount));
        }

        private IEnumerator GainRoutine(int amount)
        {
            _gain.text = "+" + amount + " Col";
            yield return new WaitForSeconds(1.6f);
            _gain.text = "";
            _gainRoutine = null;
        }
    }
}
