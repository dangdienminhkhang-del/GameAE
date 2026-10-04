using UnityEngine;

namespace Game.Core.Combat
{
    /// <summary>
    /// Cooldown theo MỐC THỜI GIAN (readyAt) thay vì coroutine:
    /// tắt/bật lại component hay đổi scene cũng không bị "kẹt cooldown" vĩnh viễn.
    /// </summary>
    public class CooldownSystem
    {
        private float _readyAt;

        /// <summary>Giữ tham số owner cho tương thích caller cũ; không dùng coroutine nữa.</summary>
        public CooldownSystem(MonoBehaviour owner = null)
        {
        }

        public bool IsReady => UnityEngine.Time.time >= _readyAt;
        public float Remaining => Mathf.Max(0f, _readyAt - UnityEngine.Time.time);

        public void StartCooldown(float duration)
        {
            if (duration <= 0f) return;
            _readyAt = UnityEngine.Time.time + duration;
        }

        public void Reset()
        {
            _readyAt = 0f;
        }
    }
}
