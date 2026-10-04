using Game.Core.Events;
using Game.Core.Stats;
using UnityEngine;

namespace Game.PvE.Player
{
    /// <summary>
    /// Tiến trình của người chơi: nhận EXP từ quái (EnemyController phát OnExperienceGained),
    /// tính level bằng LevelProgression, cập nhật CharacterStats rồi phát event cho UI.
    /// Là NGUỒN DUY NHẤT của phép tính level — PlayerHUD không tự tính nữa.
    /// </summary>
    public class PlayerProgress : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private bool logProgress = false;

        private readonly LevelProgression _progression = new LevelProgression();
        private CharacterStats _stats;

        public int Level => _progression.Level;
        public int CurrentExp => _progression.CurrentExp;
        public int RequiredExp => _progression.RequiredExp;
        public CharacterStats Stats => ResolveStats();

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
        }

        private void OnEnable()
        {
            _progression.ResetTo(startingLevel);
            GameEvents.OnExperienceGained += HandleExperienceGained;
            ResolveStats();
            Publish();
        }

        private void OnDisable()
        {
            GameEvents.OnExperienceGained -= HandleExperienceGained;
        }

        /// <summary>Lấy muộn vì PlayerController tạo Stats trong Awake của nó (thứ tự không xác định).</summary>
        private CharacterStats ResolveStats()
        {
            if (_stats == null && playerController != null) _stats = playerController.Stats;
            return _stats;
        }

        private void HandleExperienceGained(object sender, int exp)
        {
            int levelUps = _progression.AddExperience(exp);
            ApplyLevel();

            if (logProgress || levelUps > 0)
            {
                Debug.Log($"[PlayerProgress] +{exp} EXP → Lv.{_progression.Level} ({_progression.CurrentExp}/{_progression.RequiredExp}), levelUps={levelUps}", this);
            }

            if (levelUps > 0) GameEvents.RaiseLevelUp(this, _progression.Level);
            Publish();
        }

        private void ApplyLevel()
        {
            CharacterStats stats = ResolveStats();
            if (stats != null) stats.SetLevel(_progression.Level);
            GameEvents.RaiseStatsChanged(this);
        }

        /// <summary>Nạp tiến trình từ save (docs/03 §5 — key "profile").</summary>
        public void SetProgress(int level, int exp)
        {
            _progression.ResetTo(level < 1 ? 1 : level, exp < 0 ? 0 : exp);
            ApplyLevel();
            Publish();
        }

        private void Publish()
        {
            GameEvents.RaiseExperienceChanged(this, new ExperienceChangedArgs
            {
                Level = _progression.Level,
                Current = _progression.CurrentExp,
                Required = _progression.RequiredExp
            });
        }
    }
}
