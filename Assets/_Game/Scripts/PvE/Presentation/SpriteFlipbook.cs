using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Phát một dãy sprite lên SpriteRenderer (NPC thở, đèn nhấp nháy, hiệu ứng chém/trúng).
    /// Chỉ trình bày — không chứa logic game. Dùng cho vật không cần Animator.
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float fps = 6f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool destroyWhenDone;
        [SerializeField] private bool randomizeStart;

        private float _time;

        public void Setup(Sprite[] newFrames, float newFps, bool shouldLoop, bool destroyAtEnd)
        {
            frames = newFrames;
            fps = newFps;
            loop = shouldLoop;
            destroyWhenDone = destroyAtEnd;
            _time = 0f;
            Apply();
        }

        private void Awake()
        {
            if (target == null) TryGetComponent(out target);
        }

        private void OnEnable()
        {
            _time = randomizeStart && frames != null && fps > 0f ? Random.value * frames.Length / fps : 0f;
            Apply();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || fps <= 0f) return;
            _time += Time.deltaTime;
            if (!loop && _time * fps >= frames.Length)
            {
                if (destroyWhenDone) Destroy(gameObject);
                else _time = (frames.Length - 1) / fps;
            }
            Apply();
        }

        private void Apply()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            int index = Mathf.FloorToInt(_time * fps);
            index = loop ? index % frames.Length : Mathf.Clamp(index, 0, frames.Length - 1);
            target.sprite = frames[index];
        }
    }
}
