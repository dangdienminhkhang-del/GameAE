using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.PvE.Environment
{
    /// <summary>Làm ánh sáng đèn/lò lửa/dung nham nhấp nháy nhẹ cho có sức sống.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public sealed class FlickerLight2D : MonoBehaviour
    {
        [SerializeField] private float amount = 0.18f;
        [SerializeField] private float speed = 3.2f;
        private Light2D _light;
        private float _baseIntensity, _baseRadius, _seed;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _baseIntensity = _light.intensity;
            _baseRadius = _light.pointLightOuterRadius;
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * speed) * 2f - 1f;
            _light.intensity = _baseIntensity * (1f + n * amount);
            _light.pointLightOuterRadius = _baseRadius * (1f + n * amount * 0.35f);
        }
    }
}
