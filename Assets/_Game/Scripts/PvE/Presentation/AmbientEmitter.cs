using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Phát hiệu ứng môi trường liên tục (hơi nước, tia lửa, bụi) cho Tầng 2. Chỉ trình bày; không có va chạm.
    /// Đặt lên một GameObject rỗng ở vị trí ống hơi / lò rèn / đèn.
    /// </summary>
    public class AmbientEmitter : MonoBehaviour
    {
        public enum Kind { Steam, Sparks, Embers }

        [SerializeField] private Kind kind = Kind.Steam;
        [SerializeField] private float ratePerSecond = 3f;
        [SerializeField] private float radius = 0.15f;
        [SerializeField] private int sortingOrder = 20;
        [SerializeField] private float maxDistanceFromCamera = 18f;

        private float _carry;
        private UnityEngine.Camera _cam;

        private void Update()
        {
            if (_cam == null) _cam = UnityEngine.Camera.main;
            if (_cam != null && Vector2.Distance(_cam.transform.position, transform.position) > maxDistanceFromCamera) return;

            _carry += ratePerSecond * Time.deltaTime;
            while (_carry >= 1f)
            {
                _carry -= 1f;
                Emit();
            }
        }

        private void Emit()
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius) * 0.5f, 0f);
            int layer = SortingLayer.NameToID("Default");
            switch (kind)
            {
                case Kind.Steam:
                    SpriteFader.Spawn(FxSprites.Dot, p, new Color(0.9f, 0.95f, 1f, 0.55f), Random.Range(1.0f, 1.6f),
                        new Vector2(Random.Range(-0.15f, 0.25f), Random.Range(0.6f, 1.0f)), 0.8f, 2.6f, 0f, layer, sortingOrder);
                    break;
                case Kind.Sparks:
                    SpriteFader.Spawn(FxSprites.Spark, p, new Color(1f, 0.8f, 0.35f, 1f), Random.Range(0.3f, 0.6f),
                        new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(0.8f, 2f)), 0.9f, 0.2f, -3.5f, layer, sortingOrder);
                    break;
                default:
                    SpriteFader.Spawn(FxSprites.Dot, p, new Color(1f, 0.55f, 0.2f, 0.9f), Random.Range(1.2f, 2.0f),
                        new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.3f, 0.7f)), 0.35f, 0.1f, 0f, layer, sortingOrder);
                    break;
            }
        }
    }
}
