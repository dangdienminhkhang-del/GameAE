using System.Collections.Generic;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Sắp xếp độ sâu theo trục Y cho CẢ thế giới (nhà, cây, bụi, đèn, NPC, quái) chỉ với một script duy nhất.
    /// Vật nào có y nhỏ hơn người chơi (ở phía trước) thì vẽ đè lên người chơi; y lớn hơn thì vẽ dưới.
    /// Các vật với nhau cũng xếp đúng theo y. Thay cho việc gắn DepthSortByPlayer vào từng cây.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldYSort : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] statics;
        [SerializeField] private float[] baseY;
        [SerializeField] private float perTile = 16f;
        [SerializeField] private float range = 22f;

        private static readonly List<SpriteRenderer> Dynamics = new List<SpriteRenderer>();
        private SpriteRenderer _player;
        private float _lookup;

        public static void Register(SpriteRenderer sr) { if (sr != null && !Dynamics.Contains(sr)) Dynamics.Add(sr); }
        public static void Unregister(SpriteRenderer sr) { Dynamics.Remove(sr); }

        /// <summary>Gọi từ Editor: thu thập mọi SpriteRenderer con làm vật tĩnh.</summary>
        public void CollectStatics()
        {
            var list = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>(true));
            statics = list.ToArray();
            baseY = new float[statics.Length];
            for (int i = 0; i < statics.Length; i++) baseY[i] = statics[i].transform.position.y;
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                _lookup -= Time.deltaTime;
                if (_lookup > 0f) return;
                _lookup = 0.5f;
                var pc = FindAnyObjectByType<Game.PvE.Player.PlayerController>();
                if (pc != null) _player = pc.GetComponentInChildren<SpriteRenderer>();
                if (_player == null) return;
            }

            Vector3 pp = _player.transform.position;
            int po = _player.sortingOrder;
            float r2 = range * range;

            if (statics != null)
            {
                for (int i = 0; i < statics.Length; i++)
                {
                    SpriteRenderer sr = statics[i];
                    if (sr == null) continue;
                    Vector3 p = sr.transform.position;
                    float dx = p.x - pp.x, dy = p.y - pp.y;
                    if (dx * dx + dy * dy > r2) continue;
                    sr.sortingOrder = po + Mathf.Clamp(Mathf.RoundToInt((pp.y - baseY[i]) * perTile), -3000, 3000);
                }
            }

            for (int i = Dynamics.Count - 1; i >= 0; i--)
            {
                SpriteRenderer sr = Dynamics[i];
                if (sr == null) { Dynamics.RemoveAt(i); continue; }
                if (sr == _player) continue;
                float y = sr.transform.position.y;
                sr.sortingOrder = po + Mathf.Clamp(Mathf.RoundToInt((pp.y - y) * perTile), -3000, 3000);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Dynamics.Clear(); }
    }
}
