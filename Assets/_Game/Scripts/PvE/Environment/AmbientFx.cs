using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Hạt không khí bám theo camera: đom đóm (ban đêm), lá bay (ban ngày, thị trấn/đồng cỏ),
    /// tàn lửa (khe nứt dung nham), bụi sáng xanh (pháo đài). Tự bật/tắt theo vùng và độ tối.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientFx : MonoBehaviour
    {
        [SerializeField] private float riftFrom = 38f, riftTo = 72f, citadelFrom = 72f;
        private ParticleSystem _fireflies, _leaves, _embers, _motes;
        private Light2D _global;
        private UnityEngine.Camera _cam;
        private Transform _root;

        private void Start()
        {
            _cam = UnityEngine.Camera.main;
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None)) if (l.lightType == Light2D.LightType.Global) { _global = l; break; }
            _root = new GameObject("AmbientFxRoot").transform;
            var mat = new Material(Shader.Find("Sprites/Default"));
            _fireflies = Make("Fireflies", mat, 7f, 0.4f, 0.12f, new Color(0.85f, 1f, 0.45f, 0.95f), new Color(0.6f, 1f, 0.6f, 0.9f), 0.1f, 0.35f, Vector2.zero, 0.8f, 40);
            _leaves = Make("Leaves", mat, 9f, 0.5f, 0.13f, new Color(0.45f, 0.75f, 0.3f, 0.9f), new Color(0.85f, 0.75f, 0.3f, 0.9f), 0.3f, 0.7f, new Vector2(-0.45f, -0.3f), 0.25f, 30);
            _embers = Make("Embers", mat, 5f, 0.5f, 0.09f, new Color(1f, 0.7f, 0.2f, 1f), new Color(1f, 0.3f, 0.1f, 0.9f), 0.35f, 0.9f, new Vector2(0.1f, 0.6f), 0.5f, 90);
            _motes = Make("Motes", mat, 8f, 0.4f, 0.07f, new Color(0.6f, 0.85f, 1f, 0.8f), new Color(0.8f, 0.9f, 1f, 0.6f), 0.05f, 0.25f, new Vector2(0f, 0.12f), 0.35f, 40);
        }

        private ParticleSystem Make(string n, Material mat, float life, float spdMin, float size, Color c0, Color c1, float spdA, float spdB, Vector2 drift, float noise, int max)
        {
            var go = new GameObject(n); go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spdA, spdB); main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c0, c1); main.maxParticles = max; main.playOnAwake = true; main.gravityModifier = 0f;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(18f, 11f, 0.1f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World; vel.x = drift.x; vel.y = drift.y; vel.z = 0f;
            var ns = ps.noise; ns.enabled = true; ns.strength = noise; ns.frequency = 0.35f; ns.scrollSpeed = 0.2f; ns.quality = ParticleSystemNoiseQuality.Low;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.sortingOrder = 4000; r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private void LateUpdate()
        {
            if (_cam == null) { _cam = UnityEngine.Camera.main; if (_cam == null) return; }
            Vector3 p = _cam.transform.position; _root.position = new Vector3(p.x, p.y, 0f);
            float h = _cam.orthographicSize * 2f, w = h * _cam.aspect;
            foreach (var ps in new[] { _fireflies, _leaves, _embers, _motes }) { var sh = ps.shape; sh.scale = new Vector3(w * 1.1f, h * 1.1f, 0.1f); }
            float dark = _global != null ? 1f - Mathf.Clamp01(_global.color.grayscale * _global.intensity) : 0f;
            bool rift = p.x >= riftFrom && p.x < riftTo, citadel = p.x >= citadelFrom, open = p.x < riftFrom;
            Set(_fireflies, open ? Mathf.Clamp01((dark - 0.12f) * 3f) * 4f : 0f);
            Set(_leaves, open ? Mathf.Clamp01(1f - dark * 2f) * 1.6f : 0f);
            Set(_embers, rift ? 16f : 0f);
            Set(_motes, citadel ? 5f : 0f);
        }

        private static void Set(ParticleSystem ps, float rate) { var e = ps.emission; e.rateOverTime = rate; }
    }
}
