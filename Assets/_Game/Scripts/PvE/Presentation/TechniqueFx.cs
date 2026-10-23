using Mathf = UnityEngine.Mathf;
using System.Collections;
using Game.Core.Skills;
using Game.PvE.Player;
using Game.PvE.Skills;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Hiệu ứng cho chiêu (Technique): Core = vòng tụ khí + chém đôi cỡ lớn; Traverse = lướt với bóng ảnh + bụi.
    /// Chỉ trình bày: nghe TechniqueController.OnTechniqueStarted, không đổi luật chiến đấu.
    /// </summary>
    [RequireComponent(typeof(TechniqueController))]
    public class TechniqueFx : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] slashFrames;
        [SerializeField] private Color coreColor = new Color(0.5f, 0.95f, 1f, 1f);
        [SerializeField] private Color dashColor = new Color(0.75f, 0.55f, 1f, 0.6f);

        private TechniqueController _controller;

        private void Awake()
        {
            _controller = GetComponent<TechniqueController>();
            if (playerController == null) TryGetComponent(out playerController);
            if (body == null) TryGetComponent(out body);
        }

        private void OnEnable() { if (_controller != null) _controller.OnTechniqueStarted += HandleStarted; }
        private void OnDisable() { if (_controller != null) _controller.OnTechniqueStarted -= HandleStarted; }

        private void HandleStarted(object sender, TechniqueInputSlot slot)
        {
            if (slot == TechniqueInputSlot.Core) StartCoroutine(CoreRoutine());
            else StartCoroutine(TraverseRoutine());
        }

        private Vector2 Facing()
        {
            Vector2 d = playerController != null ? playerController.FacingDirection : Vector2.right;
            return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
        }

        private int Layer => body != null ? body.sortingLayerID : 0;
        private int Order => body != null ? body.sortingOrder : 10;

        private IEnumerator CoreRoutine()
        {
            // vòng tụ khí dưới chân
            Vector3 feet = transform.position + new Vector3(0f, 0.15f, 0f);
            SpriteFader.Spawn(FxSprites.Ring, feet, coreColor, 0.35f, Vector2.zero, 0.3f, 1.8f, 0f, Layer, Order - 2);
            yield return new WaitForSeconds(0.12f);
            for (int i = 0; i < 2; i++)
            {
                Vector2 dir = Facing();
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + (i == 0 ? -20f : 20f);
                Vector3 pos = transform.position + (Vector3)(dir * 0.25f) + new Vector3(0f, 0.7f, 0f);
                SpawnSlash(pos, angle, 1.25f);
                SpawnSparks(pos, 5, coreColor);
                yield return new WaitForSeconds(0.16f);
            }
        }

        private IEnumerator TraverseRoutine()
        {
            Vector3 feet = transform.position + new Vector3(0f, 0.1f, 0f);
            for (int i = 0; i < 4; i++)
                SpriteFader.Spawn(FxSprites.Dot, feet + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.15f), 0f),
                    new Color(0.85f, 0.8f, 0.95f, 0.7f), 0.4f, new Vector2(Random.Range(-0.6f, 0.6f), Random.Range(0.2f, 0.8f)),
                    1f, 2f, 0f, Layer, Order - 1);
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
                    SpriteFader.Ghost(sr, dashColor, 0.25f);
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
        }

        private void SpawnSlash(Vector3 position, float angle, float scale)
        {
            if (slashFrames == null || slashFrames.Length == 0) return;
            var go = new GameObject("FX_CoreSlash");
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = Layer; sr.sortingOrder = Order + 6; sr.color = coreColor;
            go.AddComponent<SpriteFlipbook>().Setup(slashFrames, 22f, false, true);
        }

        private void SpawnSparks(Vector3 center, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.2f, 2.6f);
                SpriteFader.Spawn(FxSprites.Spark, center, color, Random.Range(0.25f, 0.45f), v, 0.8f, 0.2f, -2f, Layer, Order + 7);
            }
        }
    }
}
