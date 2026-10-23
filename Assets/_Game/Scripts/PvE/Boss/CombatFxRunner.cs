using System;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>
    /// Hiệu ứng chiến đấu tự cài khi vào game (không cần gắn vào scene):
    /// - Vòng cảnh báo dưới chân boss trong lúc báo đòn: bán kính = vùng trúng đòn, thu nhỏ vào và đậm dần.
    /// - Hiệu ứng tan khi quái hoặc boss bị hạ (lắng nghe GameEvents.OnEnemyDefeated).
    /// Sprite nằm ở Resources/CombatFx. Thiếu sprite thì không làm gì.
    /// </summary>
    public sealed class CombatFxRunner : MonoBehaviour
    {
        private const string RingPath = "CombatFx/FX_TelegraphRing";
        private const string DissipatePath = "CombatFx/FX_Dissipate";
        private const float RingOuterRadiusUnits = 0.9375f;   // bán kính ngoài của ảnh vòng ở scale 1 (30 px / 32 ppu)
        private const float BurstFps = 10f;

        private static CombatFxRunner _instance;

        private sealed class Ring
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public Transform Follow;
            public float Time;
            public float Duration;
            public float Radius;
            public bool Active;
        }

        private sealed class Burst
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public float Time;
        }

        private readonly Dictionary<BossController, Ring> _rings = new Dictionary<BossController, Ring>();
        private readonly List<Burst> _bursts = new List<Burst>();
        private Sprite _ring;
        private Sprite[] _dissipate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_instance != null) return;
            var go = new GameObject("CombatFxRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CombatFxRunner>();
        }

        private void OnEnable()
        {
            _ring = Resources.Load<Sprite>(RingPath);
            var all = Resources.LoadAll<Sprite>(DissipatePath);
            Array.Sort(all, (a, b) => string.CompareOrdinal(a.name, b.name));
            _dissipate = all;
            BossController.TelegraphStarted += OnTelegraph;
            GameEvents.OnEnemyDefeated += OnDefeated;
        }

        private void OnDisable()
        {
            BossController.TelegraphStarted -= OnTelegraph;
            GameEvents.OnEnemyDefeated -= OnDefeated;
        }

        private void OnTelegraph(BossController boss)
        {
            if (_ring == null || boss == null || boss.Definition == null) return;
            if (!_rings.TryGetValue(boss, out var ring) || ring.Go == null)
            {
                var go = new GameObject("TelegraphRing");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _ring;
                ring = new Ring { Go = go, Renderer = sr };
                _rings[boss] = ring;
            }
            var bossRenderer = boss.GetComponentInChildren<SpriteRenderer>();
            if (bossRenderer != null)
            {
                ring.Renderer.sharedMaterial = bossRenderer.sharedMaterial;
                ring.Renderer.sortingLayerID = bossRenderer.sortingLayerID;
                ring.Renderer.sortingOrder = bossRenderer.sortingOrder - 1;
            }
            ring.Follow = boss.transform;
            ring.Time = 0f;
            ring.Duration = Mathf.Max(0.2f, boss.Definition.AttackWindupSeconds);
            ring.Radius = Mathf.Max(0.5f, boss.Definition.AttackHitRadius);
            ring.Active = true;
            ring.Go.SetActive(true);
        }

        private void OnDefeated(object sender, EventArgs args)
        {
            var comp = sender as Component;
            if (comp == null) return;
            if (sender is BossController boss && _rings.TryGetValue(boss, out var ring) && ring.Go != null)
            {
                ring.Active = false;
                ring.Go.SetActive(false);
            }
            SpawnBurst(comp);
        }

        private void SpawnBurst(Component source)
        {
            if (_dissipate == null || _dissipate.Length == 0) return;
            var col = source.GetComponent<Collider2D>();
            float size = col != null ? Mathf.Max(col.bounds.size.x, col.bounds.size.y) : 1f;
            var go = new GameObject("DissipateFx");
            go.transform.SetParent(transform, false);
            go.transform.position = col != null ? col.bounds.center : source.transform.position;
            go.transform.localScale = Vector3.one * Mathf.Max(1f, size * 1.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _dissipate[0];
            bool big = source is BossController;
            sr.color = big ? new Color(1f, 0.62f, 0.3f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
            var srcRenderer = source.GetComponentInChildren<SpriteRenderer>();
            if (srcRenderer != null)
            {
                sr.sharedMaterial = srcRenderer.sharedMaterial;
                sr.sortingLayerID = srcRenderer.sortingLayerID;
                sr.sortingOrder = srcRenderer.sortingOrder + 1;
            }
            _bursts.Add(new Burst { Go = go, Renderer = sr, Time = 0f });
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            foreach (var kv in _rings)
            {
                var ring = kv.Value;
                if (!ring.Active || ring.Go == null) continue;
                ring.Time += dt;
                if (ring.Follow == null || ring.Time >= ring.Duration)
                {
                    ring.Active = false;
                    ring.Go.SetActive(false);
                    continue;
                }
                float p = ring.Time / ring.Duration;
                float ease = 1f - (1f - p) * (1f - p);
                float scale = ring.Radius / RingOuterRadiusUnits * Mathf.Lerp(1.5f, 1f, ease);   // từ to thu nhỏ vào vùng trúng đòn
                float pulse = 0.85f + 0.15f * Mathf.Sin(ring.Time * 28f);
                ring.Go.transform.position = ring.Follow.position + new Vector3(0f, -0.35f, 0f);
                ring.Go.transform.localScale = Vector3.one * scale;
                ring.Renderer.color = new Color(1f, 0.55f, 0.15f, Mathf.Lerp(0.25f, 0.95f, p) * pulse);
            }
            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                var b = _bursts[i];
                if (b.Go == null) { _bursts.RemoveAt(i); continue; }
                b.Time += dt;
                int frame = Mathf.FloorToInt(b.Time * BurstFps);
                if (frame >= _dissipate.Length)
                {
                    Destroy(b.Go);
                    _bursts.RemoveAt(i);
                    continue;
                }
                b.Renderer.sprite = _dissipate[frame];
            }
        }
    }
}
