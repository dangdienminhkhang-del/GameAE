using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Cửa phòng boss (kiểu cổng boss trong SAO): hai cánh sắt khép giữa hành lang. Người chơi đứng trước cửa
    /// một lúc (thanh tiến độ hiện ra) thì hai cánh co dần vào hai bên tường, chừa lối đi.
    /// Không cần phím nào nên dùng được cả bàn phím lẫn cảm ứng. Rời đi giữa chừng thì tiến độ tụt dần.
    /// Mở rồi thì giữ mở cho tới khi nhóm tầng được bật lại (OnEnable đặt về đóng).
    /// Logic nằm ở Tick(...) để kiểm thử được mà không cần vật lý.
    /// Hai cánh dùng SpriteRenderer chế độ Tiled (đặt sẵn trong scene).
    /// </summary>
    public sealed class BossGate : MonoBehaviour
    {
        public enum GateState { Closed, Opening, Open }

        [Header("Hình")]
        [SerializeField] private SpriteRenderer leafNorth;
        [SerializeField] private SpriteRenderer leafSouth;
        [SerializeField] private SpriteRenderer seam;
        [SerializeField, Min(0.5f)] private float halfHeight = 3f;
        [SerializeField, Min(0.05f)] private float openLeafHeight = 0.2f;
        [SerializeField, Min(0.1f)] private float leafWidth = 1f;
        [SerializeField] private SpriteRenderer progressBack;
        [SerializeField] private SpriteRenderer progressFill;
        [SerializeField, Min(0.5f)] private float progressBarWidth = 3f;
        [SerializeField, Min(0.05f)] private float progressBarHeight = 0.22f;

        [Header("Va chạm")]
        [Tooltip("Bật khi cổng đóng: chặn kín cả hành lang.")]
        [SerializeField] private Collider2D[] closedColliders = new Collider2D[0];
        [Tooltip("Bật khi cổng đã mở (để trống nếu mở là thông hoàn toàn).")]
        [SerializeField] private Collider2D[] openColliders = new Collider2D[0];

        [Header("Thời gian")]
        [SerializeField, Min(0.2f)] private float holdSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float openSeconds = 0.8f;
        [SerializeField, Min(0.1f)] private float decayMultiplier = 1.5f;

        private static Sprite s_whiteCentered;
        private static Sprite s_whiteLeft;

        private GateState _state = GateState.Closed;
        private float _progress;
        private float _openTime;
        private bool _playerInside;

        public GateState State => _state;
        public float Progress => _progress;
        public bool IsOpen => _state == GateState.Open;

        private static Sprite WhiteSprite(Vector2 pivot, ref Sprite cache)
        {
            if (cache != null) return cache;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            cache = Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot, 1f);
            return cache;
        }

        private void Awake()
        {
            if (seam != null && seam.sprite == null) seam.sprite = WhiteSprite(new Vector2(0.5f, 0.5f), ref s_whiteCentered);
            if (progressBack != null && progressBack.sprite == null) progressBack.sprite = WhiteSprite(new Vector2(0.5f, 0.5f), ref s_whiteCentered);
            if (progressFill != null && progressFill.sprite == null) progressFill.sprite = WhiteSprite(new Vector2(0f, 0.5f), ref s_whiteLeft);
        }

        private void OnEnable()
        {
            ResetClosed();
        }

        /// <summary>Đặt cổng về đóng kín (dùng khi bật lại tầng hoặc khi kiểm thử).</summary>
        public void ResetClosed()
        {
            _state = GateState.Closed;
            _progress = 0f;
            _openTime = 0f;
            _playerInside = false;
            SetColliders(closedColliders, true);
            SetColliders(openColliders, false);
            ApplyOpening(0f);
            UpdateBar();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null) _playerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && other.GetComponentInParent<PlayerController>() != null) _playerInside = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime, _playerInside);
        }

        /// <summary>Một bước logic: playerInRange là người chơi đang đứng trong vùng trước cổng.</summary>
        public void Tick(float deltaTime, bool playerInRange)
        {
            switch (_state)
            {
                case GateState.Closed:
                    if (playerInRange) _progress += deltaTime / holdSeconds;
                    else _progress = Mathf.Max(0f, _progress - deltaTime / holdSeconds * decayMultiplier);
                    if (_progress >= 1f)
                    {
                        _progress = 1f;
                        _state = GateState.Opening;
                        _openTime = 0f;
                    }
                    UpdateBar();
                    break;

                case GateState.Opening:
                    _openTime += deltaTime;
                    float k = Mathf.Clamp01(_openTime / openSeconds);
                    ApplyOpening(k * k);
                    if (k >= 1f)
                    {
                        _state = GateState.Open;
                        SetColliders(closedColliders, false);
                        SetColliders(openColliders, true);
                    }
                    UpdateBar();
                    break;
            }
        }

        /// <summary>t = 0 đóng kín, t = 1 mở hết: hai cánh co dần vào hai đầu hành lang.</summary>
        private void ApplyOpening(float t)
        {
            float h = Mathf.Lerp(halfHeight, openLeafHeight, Mathf.Clamp01(t));
            Place(leafNorth, +1f, h);
            Place(leafSouth, -1f, h);
            if (seam != null)
            {
                seam.enabled = t < 0.999f;
                var c = seam.color; c.a = 1f - Mathf.Clamp01(t);
                seam.color = c;
                seam.transform.localScale = new Vector3(leafWidth * 0.9f, 0.12f, 1f);
            }
        }

        private void Place(SpriteRenderer leaf, float side, float height)
        {
            if (leaf == null) return;
            leaf.size = new Vector2(leafWidth, height);
            var p = leaf.transform.localPosition;
            leaf.transform.localPosition = new Vector3(p.x, side * (halfHeight - height * 0.5f), p.z);
        }

        private void UpdateBar()
        {
            bool show = _state == GateState.Closed && _progress > 0.001f;
            if (progressBack != null)
            {
                progressBack.enabled = show;
                progressBack.transform.localScale = new Vector3(progressBarWidth + 0.12f, progressBarHeight + 0.12f, 1f);
            }
            if (progressFill != null)
            {
                progressFill.enabled = show;
                progressFill.transform.localScale = new Vector3(Mathf.Max(0.0001f, progressBarWidth * _progress), progressBarHeight, 1f);
                var p = progressFill.transform.localPosition;
                progressFill.transform.localPosition = new Vector3(-progressBarWidth * 0.5f, p.y, p.z);
            }
        }

        private static void SetColliders(Collider2D[] colliders, bool enabled)
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = enabled;
        }
    }
}
