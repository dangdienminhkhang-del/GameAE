using Game.Core.Events;
using Game.Core.Time;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.PvE.Clock
{
    /// <summary>
    /// Đổi màu/cường độ Global Light 2D theo pha ngày/đêm (docs/08 §3.1).
    /// Chỉ áp dụng khi có Light2D (URP 2D); nếu chưa gán thì tự tìm 1 Global Light trong scene.
    /// Namespace Game.PvE.Clock để không che UnityEngine.Time.
    /// </summary>
    public class DayNightLightController : MonoBehaviour
    {
        [SerializeField] private Light2D globalLight;
        [SerializeField] private TimeConfig config;
        [SerializeField] private float transitionSpeed = 2.5f;

        private Color _targetColor = Color.white;

        private void Awake()
        {
            if (globalLight == null)
            {
                var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].lightType == Light2D.LightType.Global)
                    {
                        globalLight = lights[i];
                        break;
                    }
                }
            }

            if (globalLight != null) _targetColor = globalLight.color;
        }

        private void OnEnable()
        {
            GameEvents.OnTimePhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTimePhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(object sender, TimeChangedArgs args)
        {
            Apply(args.Phase);
        }

        /// <summary>Đặt màu theo pha (dùng được từ test/AI điều khiển).</summary>
        public void Apply(TimeOfDay phase)
        {
            if (config == null) return;
            _targetColor = config.ColorForPhase(phase);
        }

        private void Update()
        {
            if (globalLight == null) return;
            globalLight.color = Color.Lerp(globalLight.color, _targetColor, UnityEngine.Time.deltaTime * transitionSpeed);
        }
    }
}
