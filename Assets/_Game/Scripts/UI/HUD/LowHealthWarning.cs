using Game.Core.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.HUD
{
    /// <summary>
    /// Viền đỏ nhấp nháy khi máu thấp. CHỈ hiển thị: mọi ngưỡng/độ đậm nằm trong LowHealthWarningRules (Core).
    /// PlayerHUD gọi SetHealthRatio — dùng chung bộ lọc healthSource của HUD, nên máu quái/boss không kích hoạt.
    /// Texture viền tạo lúc chạy (không cần asset art); không chặn chạm (raycastTarget = false).
    /// Lưu ý: trong Game.UI.* phải viết UnityEngine.Time đầy đủ vì có namespace Game.UI.Time.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class LowHealthWarning : MonoBehaviour
    {
        [SerializeField] private RawImage vignette;

        private const int TextureSize = 64;
        private float _ratio = 1f;
        private Texture2D _texture;

        private void Awake()
        {
            if (vignette == null) TryGetComponent(out vignette);
            if (vignette == null) return;

            _texture = BuildVignetteTexture();
            vignette.texture = _texture;
            vignette.raycastTarget = false;
            Apply(0f);
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }

        /// <summary>Tỉ lệ máu 0..1 (do PlayerHUD gọi mỗi khi máu đổi).</summary>
        public void SetHealthRatio(float ratio)
        {
            _ratio = ratio;
            if (!LowHealthWarningRules.IsLow(ratio)) Apply(0f);
        }

        private void Update()
        {
            if (vignette == null || !LowHealthWarningRules.IsLow(_ratio)) return;
            // unscaledTime: vẫn nhấp nháy đúng nhịp khi game tạm dừng.
            Apply(LowHealthWarningRules.Intensity(_ratio, UnityEngine.Time.unscaledTime));
        }

        private void Apply(float alpha)
        {
            if (vignette == null) return;
            Color c = vignette.color;
            c.a = alpha;
            vignette.color = c;
            vignette.enabled = alpha > 0.001f;
        }

        /// <summary>Radial: giữa trong suốt, rìa màn hình đỏ.</summary>
        private static Texture2D BuildVignetteTexture()
        {
            var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = (x + 0.5f) / TextureSize * 2f - 1f;
                    float ny = (y + 0.5f) / TextureSize * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f; // 0 ở tâm, 1 ở góc
                    byte a = (byte)(255f * Mathf.SmoothStep(0.45f, 0.95f, r));
                    pixels[y * TextureSize + x] = new Color32(217, 13, 13, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
