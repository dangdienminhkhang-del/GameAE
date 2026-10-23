using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Core.Items;
using UnityEngine;

namespace Game.PvE.Items
{
    /// <summary>
    /// Rương kho báu: player chạm vào là mở (giống ItemPickup), nhận đồ + Col một lần.
    /// - Kên đầy: phần nhận được giữ lại trong rương, KHÔNG mất đồ; rương chưa đóng, quay lại nhận nốt.
    /// - Trạng thái đã mở chỉ nhớ trong phiên chạy hiện tại (static, KHÔNG lưu cloud) — vì thế mở lại app
    ///   thì rương có lại. Đây là giới hạn đã biết; lưu lâu dài cần quyết định về persistence (permadeath fail-closed).
    /// - chestId phải duy nhất giữa các rương.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TreasureChest : MonoBehaviour
    {
        [System.Serializable]
        public struct Reward
        {
            public ItemDefinition item;
            public int quantity;
        }

        [SerializeField] private string chestId = "chest";
        [SerializeField] private Reward[] rewards;
        [Tooltip("Col nhận thêm khi mở rương (0 = không có).")]
        [SerializeField] private int colReward;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;

        private static readonly HashSet<string> Opened = new HashSet<string>();

        private int[] _remaining;
        private bool _opened;
        private GameObject _floatingText;

        public string ChestId => chestId;
        public int RewardCount => rewards != null ? rewards.Length : 0;
        public int ColReward => colReward;

        // ── Trạng thái đã mở (trong phiên) ──────────────────────────────────────

        public static bool IsOpened(string id)
        {
            return !string.IsNullOrEmpty(id) && Opened.Contains(id);
        }

        /// <summary>Đánh dấu đã mở. Trả true nếu là lần đầu, false nếu ID rỗng hoặc đã mở từ trước.</summary>
        public static bool TryMarkOpened(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return Opened.Add(id);
        }

        /// <summary>Chỉ cho test: xoá trạng thái đã mở.</summary>
        public static void ResetOpenedForTests()
        {
            Opened.Clear();
        }

        // Editor tắt Domain Reload thì static sống qua các lần Play — xoá để mỗi lần Play bắt đầu sạch.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Opened.Clear();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);

            _remaining = new int[RewardCount];
            for (int i = 0; i < _remaining.Length; i++) _remaining[i] = Mathf.Max(1, rewards[i].quantity);

            if (IsOpened(chestId))
            {
                _opened = true;
                ApplyOpenedVisual();
            }
            else if (spriteRenderer != null && closedSprite != null)
            {
                spriteRenderer.sprite = closedSprite;
            }
        }

        private void OnDisable()
        {
            if (_floatingText != null) Destroy(_floatingText);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_opened || other == null) return;

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            // Thử nhận từng món; nhận được bao nhiêu trừ bấy nhiêu, phần dư giữ lại.
            bool complete = true;
            for (int i = 0; i < RewardCount; i++)
            {
                if (rewards[i].item == null || _remaining[i] <= 0) continue;
                int accepted = inventory.TryAddItem(rewards[i].item, _remaining[i]);
                _remaining[i] -= Mathf.Max(0, accepted);
                if (_remaining[i] > 0) complete = false;
            }

            if (!complete)
            {
                ShowFloating("Túi đầy — dọn chỗ rồi quay lại");
                return;
            }

            if (colReward > 0)
            {
                PlayerWallet wallet = other.GetComponentInParent<PlayerWallet>();
                if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
                if (wallet != null) wallet.Add(colReward);
            }

            _opened = true;
            TryMarkOpened(chestId);
            ApplyOpenedVisual();
            ShowFloating(Describe());
        }

        // ── Hiển thị ──────────────────────────────────────────────────────────

        private void ApplyOpenedVisual()
        {
            if (spriteRenderer != null && openSprite != null) spriteRenderer.sprite = openSprite;
            if (TryGetComponent(out Collider2D col)) col.enabled = false;
        }

        private string Describe()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < RewardCount; i++)
            {
                if (rewards[i].item == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append('+').Append(Mathf.Max(1, rewards[i].quantity)).Append(' ').Append(rewards[i].item.DisplayName);
            }
            if (colReward > 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append('+').Append(colReward).Append(" Col");
            }
            return sb.Length > 0 ? sb.ToString() : "Rương trống";
        }

        private void ShowFloating(string text)
        {
            if (_floatingText != null) Destroy(_floatingText);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _floatingText = new GameObject("ChestText");
            _floatingText.transform.position = transform.position + new Vector3(0f, 0.9f, 0f);

            var tm = _floatingText.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.92f, 0.5f, 1f);

            var mr = _floatingText.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = 200;

            StartCoroutine(FloatAndFade(_floatingText, tm));
        }

        private IEnumerator FloatAndFade(GameObject go, TextMesh tm)
        {
            const float duration = 1.8f;
            Vector3 start = go.transform.position;
            Color c = tm.color;
            float t = 0f;
            while (t < duration)
            {
                if (go == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                go.transform.position = start + Vector3.up * (0.8f * k);
                c.a = 1f - k * k;
                tm.color = c;
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }
}
