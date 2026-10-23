using System.Collections;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.PvE.Dodge;
using Game.PvE.Environment;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Combat
{
    /// <summary>
    /// Hiển thị các thông báo chiến đấu đỉnh cao nổi bật giữa màn hình:
    /// - "PERFECT DODGE!" (Vàng kim)
    /// - "PHẢN ĐÒN CHÍ MẠNG!" (Cam đỏ)
    /// - "VŨ ĐIỆU CUỒNG NỘ!" (Tím ánh vàng)
    /// - "PHƯỚC LÀNH CỔ ĐẠI!" (Xanh ngọc)
    /// - "RANK S / RANK SSS!"
    /// </summary>
    public class FloatingCombatNoticePresenter : MonoBehaviour
    {
        public static FloatingCombatNoticePresenter Instance { get; private set; }

        private const int PoolSize = 4;
        private readonly List<Text> _pool = new List<Text>(PoolSize);
        private Transform _container;
        private Camera _mainCamera;

        private static readonly Color ColorPerfectDodge = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color ColorCounter = new Color(1f, 0.35f, 0.2f, 1f);
        private static readonly Color ColorUltimate = new Color(0.9f, 0.4f, 1f, 1f);
        private static readonly Color ColorBlessing = new Color(0.3f, 1f, 0.8f, 1f);

        private void Awake()
        {
            Instance = this;
            BuildPool();
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            // Lắng nghe các sự kiện đỉnh cao
            if (PerfectDodgeDetector.Instance != null)
            {
                PerfectDodgeDetector.Instance.PerfectDodgeTriggered += () => ShowNotice("PERFECT DODGE!", ColorPerfectDodge);
            }

            if (ComboTracker.Instance != null)
            {
                ComboTracker.Instance.RankChanged += HandleRankChanged;
            }

            ShrineOfBlessing.OnBlessingGranted += HandleBlessingGranted;
            Game.Core.Events.GameEvents.OnCombatNotice += HandleCombatNotice;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
            ShrineOfBlessing.OnBlessingGranted -= HandleBlessingGranted;
            Game.Core.Events.GameEvents.OnCombatNotice -= HandleCombatNotice;
        }

        private void HandleCombatNotice(string text, Color color)
        {
            ShowNotice(text, color);
        }

        private void HandleRankChanged(ComboRank rank)
        {
            if (rank == ComboRank.S || rank == ComboRank.SSS)
            {
                ShowNotice($"★ RANK {rank}! ★", ColorPerfectDodge);
            }
        }

        private void HandleBlessingGranted(BlessingType type, float duration)
        {
            string name = type == BlessingType.Swiftness ? "TỐC ĐỘ" : (type == BlessingType.Aegis ? "HỘ VỆ" : "HÚT MÁU");
            ShowNotice($"PHƯỚC LÀNH: {name}!", ColorBlessing);
        }

        public void ShowNotice(string text, Color color)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                Text item = _pool[i];
                if (!item.gameObject.activeSelf)
                {
                    StartCoroutine(AnimateNotice(item, text, color));
                    return;
                }
            }
        }

        private IEnumerator AnimateNotice(Text label, string message, Color color)
        {
            label.gameObject.SetActive(true);
            label.text = message;
            label.color = color;

            RectTransform rt = label.rectTransform;
            rt.anchoredPosition = new Vector2(0f, 160f);
            rt.localScale = Vector3.one * 1.4f;

            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += UnityEngine.Time.unscaledDeltaTime;
                float t = elapsed / duration;

                rt.anchoredPosition = new Vector2(0f, 160f + t * 45f);
                rt.localScale = Vector3.one * Mathf.Lerp(1.4f, 1.0f, t * 2f);

                Color c = color;
                c.a = Mathf.Clamp01(1f - (t * t));
                label.color = c;

                yield return null;
            }

            label.gameObject.SetActive(false);
        }

        private void BuildPool()
        {
            GameObject containerObj = new GameObject("CombatNoticeContainer", typeof(RectTransform));
            containerObj.transform.SetParent(transform, false);
            _container = containerObj.transform;

            RectTransform rt = containerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400f, 100f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < PoolSize; i++)
            {
                GameObject obj = new GameObject($"Notice_{i}", typeof(RectTransform), typeof(Text));
                obj.transform.SetParent(_container, false);
                Text txt = obj.GetComponent<Text>();
                txt.font = font;
                txt.fontSize = 32;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                obj.SetActive(false);
                _pool.Add(txt);
            }
        }
    }
}
