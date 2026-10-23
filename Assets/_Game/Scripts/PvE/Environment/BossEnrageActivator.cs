using Game.PvE.Boss;
using UnityEngine;

namespace Game.PvE.Environment
{
    /// <summary>
    /// Đổi vùng nguy hiểm theo phase boss: khi boss nổi điên (IsEnraged) thì bật nhóm đối tượng chỉ định
    /// (ví dụ thêm lỗ xả hơi); khi boss được đặt lại hoặc chưa nổi điên thì tắt.
    /// Chỉ đọc trạng thái của BossController, không đổi gì ở boss.
    /// </summary>
    public sealed class BossEnrageActivator : MonoBehaviour
    {
        [SerializeField] private BossController boss;
        [SerializeField] private GameObject[] showWhenEnraged = new GameObject[0];
        [SerializeField] private GameObject[] hideWhenEnraged = new GameObject[0];

        private bool _applied;
        private bool _lastState;

        private void OnEnable()
        {
            _applied = false;
        }

        private void Update()
        {
            bool enraged = boss != null && boss.IsEnraged && !boss.IsDead;
            if (_applied && enraged == _lastState) return;
            _applied = true;
            _lastState = enraged;
            Set(showWhenEnraged, enraged);
            Set(hideWhenEnraged, !enraged);
        }

        private static void Set(GameObject[] targets, bool active)
        {
            if (targets == null) return;
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] != null && targets[i].activeSelf != active) targets[i].SetActive(active);
        }
    }
}
