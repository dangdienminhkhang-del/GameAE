using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Hiệu ứng thoáng qua: trôi, đổi cỡ và mờ dần rồi tự hủy. Dùng cho bóng ảnh lướt, khói hơi, tia lửa, vòng sóng.
    /// Chỉ trình bày. Sprite lấy từ <see cref="FxSprites"/> (tạo bằng code, không cần file ảnh).
    /// </summary>
    public class SpriteFader : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _life, _age;
        private Vector2 _velocity;
        private float _gravity;
        private Color _start;
        private float _endAlpha;
        private float _scaleFrom, _scaleTo;

        public static SpriteFader Spawn(Sprite sprite, Vector3 position, Color color, float life, Vector2 velocity,
            float scaleFrom, float scaleTo, float gravity, int sortingLayerId, int sortingOrder, float endAlpha = 0f)
        {
            var go = new GameObject("Fx");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
            var f = go.AddComponent<SpriteFader>();
            f._renderer = sr; f._life = Mathf.Max(0.05f, life); f._velocity = velocity; f._gravity = gravity;
            f._start = color; f._endAlpha = endAlpha; f._scaleFrom = scaleFrom; f._scaleTo = scaleTo;
            go.transform.localScale = Vector3.one * scaleFrom;
            return f;
        }

        /// <summary>Bản sao một SpriteRenderer (bóng ảnh lướt).</summary>
        public static SpriteFader Ghost(SpriteRenderer source, Color tint, float life)
        {
            if (source == null || source.sprite == null || !source.enabled) return null;
            var f = Spawn(source.sprite, source.transform.position, tint, life, Vector2.zero, 1f, 1f, 0f,
                source.sortingLayerID, source.sortingOrder - 1);
            f._renderer.flipX = source.flipX;
            f.transform.localScale = source.transform.lossyScale;
            f._scaleFrom = f._scaleTo = 1f;
            f._baseScale = source.transform.lossyScale;
            return f;
        }

        private Vector3 _baseScale = Vector3.one;

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            _velocity.y += _gravity * Time.deltaTime;
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            float s = Mathf.Lerp(_scaleFrom, _scaleTo, t);
            transform.localScale = _baseScale * s;
            if (_renderer != null)
            {
                Color c = _start;
                c.a = Mathf.Lerp(_start.a, _endAlpha, t);
                _renderer.color = c;
            }
            if (_age >= _life) Destroy(gameObject);
        }
    }
}
