using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Phát lặp một dãy sprite trên SpriteRenderer (băng tải, đèn nhấp nháy...). Không dùng Animator cho gọn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteLoopAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
        [SerializeField] private bool randomizeStart = true;

        private float _clock;

        public void SetFrames(Sprite[] newFrames, float fps)
        {
            frames = newFrames ?? new Sprite[0];
            framesPerSecond = Mathf.Max(0.1f, fps);
        }

        private void Reset()
        {
            target = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            _clock = randomizeStart ? Random.value * 10f : 0f;
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            _clock += Time.deltaTime * framesPerSecond;
            int index = Mathf.FloorToInt(_clock) % frames.Length;
            if (frames[index] != null) target.sprite = frames[index];
        }
    }
}
