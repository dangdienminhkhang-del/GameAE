using System;
using Game.Core.Events;
using Game.PvE.Player;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Hiệu ứng chém (lúc vung đòn) và tia trúng đòn (khi đòn của người chơi trúng mục tiêu).
    /// Chỉ trình bày: lắng nghe PlayerAttack.AttackPerformed và GameEvents.OnHitResolved, không đổi luật chiến đấu.
    /// </summary>
    [RequireComponent(typeof(PlayerAttack))]
    public class PlayerCombatFx : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Sprite[] slashFrames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private float slashDistance = 0.7f;
        [SerializeField] private float slashFps = 20f;
        [SerializeField] private float hitFps = 24f;

        private PlayerAttack _attack;

        private void Awake()
        {
            _attack = GetComponent<PlayerAttack>();
            if (playerController == null) TryGetComponent(out playerController);
        }

        private void OnEnable()
        {
            if (_attack != null) _attack.AttackPerformed += HandleAttack;
            GameEvents.OnHitResolved += HandleHit;
        }

        private void OnDisable()
        {
            if (_attack != null) _attack.AttackPerformed -= HandleAttack;
            GameEvents.OnHitResolved -= HandleHit;
        }

        private void HandleAttack(object sender, EventArgs e)
        {
            if (slashFrames == null || slashFrames.Length == 0) return;
            Vector2 dir = playerController != null ? playerController.FacingDirection : Vector2.right;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
            dir.Normalize();

            // Vị trí ngang thân người chơi, không bay lên đầu
            Vector3 pos = transform.position + (Vector3)(dir * slashDistance);
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            int step = _attack != null ? _attack.CurrentComboStep : 0;
            bool isCounter = _attack != null && _attack.IsCounterStanceActive;

            if (isCounter)
            {
                // Phản đòn: Bão kiếm đỏ rực cực đại
                Spawn("FX_Slash_Counter", pos, baseAngle, slashFrames, slashFps * 1.3f, new Vector3(2.6f, 2.6f, 1f), new Color(1f, 0.25f, 0.25f, 1f));
                Camera.CameraFollow.RequestShake(0.22f, 0.26f);
            }
            else
            {
                switch (step)
                {
                    case 0:
                        // Nhịp 1: Chém nhanh xanh băng
                        Spawn("FX_Slash_1", pos, baseAngle, slashFrames, slashFps, new Vector3(1.35f, 1.35f, 1f), new Color(0.7f, 1f, 1f, 1f));
                        break;
                    case 1:
                        // Nhịp 2: Chém chéo ngược màu cam rực
                        Spawn("FX_Slash_2", pos, baseAngle + 25f, slashFrames, slashFps * 1.15f, new Vector3(1.6f, -1.6f, 1f), new Color(1f, 0.7f, 0.25f, 1f));
                        Camera.CameraFollow.RequestShake(0.08f, 0.12f);
                        break;
                    case 2:
                        // Nhịp 3 (Finisher): Đại kiếm trảm bão tố hoàng kim
                        Spawn("FX_Slash_Finisher", pos, baseAngle, slashFrames, slashFps * 1.25f, new Vector3(2.3f, 2.3f, 1f), new Color(1f, 0.9f, 0.15f, 1f));
                        // Vệt chém phụ tạo hiệu ứng chữ X bùng nổ
                        Spawn("FX_Slash_Cross", pos, baseAngle - 35f, slashFrames, slashFps * 1.25f, new Vector3(1.9f, -1.9f, 1f), new Color(1f, 0.5f, 0.1f, 0.85f));
                        Camera.CameraFollow.RequestShake(0.18f, 0.22f);
                        break;
                }
            }
        }

        private void HandleHit(object sender, HitResolvedArgs e)
        {
            if (hitFrames == null || hitFrames.Length == 0 || e == null) return;
            if (!(e.Target is Component component)) return;

            Vector3 pos = component.transform.position + new Vector3(0f, 0.1f, 0f);
            bool isCrit = e.Damage != null && e.Damage.IsCritical;
            Color sparkColor = isCrit ? new Color(1f, 0.4f, 0.1f, 1f) : Color.white;
            float sparkScale = isCrit ? 1.8f : 1.4f;

            Spawn("FX_Hit", pos, UnityEngine.Random.Range(0f, 360f), hitFrames, hitFps, Vector3.one * sparkScale, sparkColor);
            Camera.CameraFollow.RequestShake(0.06f, 0.1f);
        }

        private void Spawn(string label, Vector3 position, float angle, Sprite[] frames, float fps, Vector3 scale, Color tint)
        {
            var go = new GameObject(label);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.color = tint;
            var reference = GetComponent<SpriteRenderer>();
            if (reference != null)
            {
                renderer.sortingLayerID = reference.sortingLayerID;
                renderer.sortingOrder = reference.sortingOrder + 10;
            }
            var book = go.AddComponent<SpriteFlipbook>();
            book.Setup(frames, fps, false, true);
        }
    }
}
