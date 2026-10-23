using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Guidance;
using Game.Core.Quest;
using Game.PvE.NPC;
using Game.PvE.Player;
using Game.PvE.Quest;
using Game.PvE.Zones;
using UnityEngine;

namespace Game.UI.Quest
{
    /// <summary>
    /// Tìm đích cho mũi tên quest (Phase 9.9). Luật chọn quest và loại đích nằm ở <see cref="QuestTargetRules"/> (thuần, có test);
    /// lớp này chỉ đổi đích logic thành VỊ TRÍ thế giới rồi cache. Chọn lại khi quest đổi, khi vào vùng mới (cũng là lúc tầng kích hoạt)
    /// và theo nhịp chậm (không Find mỗi frame: NPC và vùng được tra từ danh sách đăng ký sẵn).
    /// Quest được chọn: quest ĐẦU TIÊN theo thứ tự danh sách quest của PlayerQuestLog (knownQuests) đang Active hoặc ReadyToTurnIn.
    /// </summary>
    public sealed class QuestTargetResolver : MonoBehaviour
    {
        /// <summary>Chọn lại đích tối thiểu mỗi chừng này giây thời gian thật (phòng NPC/vùng bật sau khi tầng kích hoạt).</summary>
        public const float ReResolveIntervalSeconds = 1f;

        [SerializeField] private PlayerQuestLog questLog;

        private readonly List<QuestTargetCandidate> _candidates = new List<QuestTargetCandidate>(8);
        private readonly List<Vector2> _farmPoints = new List<Vector2>(4);
        private PlayerController _player;
        private float _nextResolveAt;
        private bool _dirty = true;

        /// <summary>Đích đã chọn (Kind = None nghĩa là không có).</summary>
        public QuestTarget Target { get; private set; }

        /// <summary>Có đích và đã biết vị trí của nó trên tầng hiện tại.</summary>
        public bool HasTarget { get; private set; }

        public Vector2 TargetPosition { get; private set; }

        /// <summary>Vị trí người chơi (cache; null nếu chưa có).</summary>
        public Transform PlayerTransform => _player != null ? _player.transform : null;

        /// <summary>Đích hoặc vị trí đích vừa đổi.</summary>
        public event Action Changed;

        private void Awake()
        {
            if (questLog == null) questLog = FindAnyObjectByType<PlayerQuestLog>();
        }

        private void OnEnable()
        {
            GameEvents.OnQuestChanged += HandleQuestChanged;
            GameEvents.OnZoneEntered += HandleZoneEntered;
            _dirty = true;
        }

        private void OnDisable()
        {
            GameEvents.OnQuestChanged -= HandleQuestChanged;
            GameEvents.OnZoneEntered -= HandleZoneEntered;
        }

        private void Update()
        {
            if (questLog == null) return;

            float now = UnityEngine.Time.unscaledTime;
            if (!_dirty && now < _nextResolveAt) return;

            _dirty = false;
            _nextResolveAt = now + ReResolveIntervalSeconds;
            Resolve();
        }

        private void HandleQuestChanged(object sender, QuestChangedArgs args) => _dirty = true;
        private void HandleZoneEntered(object sender, ZoneEnteredArgs args) => _dirty = true;

        /// <summary>Chọn lại đích ngay.</summary>
        public void Resolve()
        {
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();

            BuildCandidates();
            QuestTarget target = QuestTargetRules.Select(_candidates);

            bool hasPosition = TryGetPosition(target, out Vector2 position);
            bool changed = target.Kind != Target.Kind || target.Id != Target.Id ||
                           hasPosition != HasTarget || (hasPosition && position != TargetPosition);

            Target = target;
            HasTarget = hasPosition;
            TargetPosition = hasPosition ? position : Vector2.zero;

            if (changed) Changed?.Invoke();
        }

        private void BuildCandidates()
        {
            _candidates.Clear();
            IReadOnlyList<QuestDefinition> quests = questLog.KnownQuests;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null) continue;

                QuestState state = questLog.GetState(quest);
                if (state == null) continue;

                _candidates.Add(new QuestTargetCandidate(
                    quest.Id, state.status, quest.ObjectiveType, quest.TargetId, FindTurnInNpcId(quest)));
            }
        }

        /// <summary>NPC đang bật phát quest này (nơi trả quest). Rỗng nếu không có NPC nào trên tầng hiện tại.</summary>
        private static string FindTurnInNpcId(QuestDefinition quest)
        {
            IReadOnlyList<NPCInteractable> npcs = NPCInteractable.Registered;
            for (int i = 0; i < npcs.Count; i++)
            {
                NPCInteractable npc = npcs[i];
                if (npc != null && npc.Definition != null && npc.Definition.OfferedQuest == quest) return npc.Definition.Id;
            }
            return null;
        }

        private bool TryGetPosition(QuestTarget target, out Vector2 position)
        {
            position = Vector2.zero;
            switch (target.Kind)
            {
                case QuestTargetKind.Npc:
                {
                    NPCInteractable npc = NPCInteractable.FindByNpcId(target.Id);
                    if (npc == null) return false;
                    position = npc.transform.position;
                    return true;
                }
                case QuestTargetKind.Zone:
                {
                    ZoneVolume zone = ZoneVolume.Find(target.Id);
                    if (zone == null) return false;
                    position = zone.CameraBounds.center;
                    return true;
                }
                case QuestTargetKind.NearestFarm:
                    return TryGetNearestFarm(out position);
                default:
                    return false;
            }
        }

        private bool TryGetNearestFarm(out Vector2 position)
        {
            position = Vector2.zero;
            if (_player == null) return false;

            _farmPoints.Clear();
            IReadOnlyList<ZoneVolume> zones = ZoneVolume.All;
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneVolume zone = zones[i];
                if (zone != null && zone.Kind == ZoneKind.Farm) _farmPoints.Add(zone.CameraBounds.center);
            }

            int nearest = QuestTargetRules.NearestIndex(_player.transform.position, _farmPoints);
            if (nearest < 0) return false;
            position = _farmPoints[nearest];
            return true;
        }
    }
}
