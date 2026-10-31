using System;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Social;
using Game.PvE.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.PvE.Social
{
    /// <summary>
    /// Quản lý Tổ Đội (Party) và tính năng Xã hội của người chơi trong màn chơi PvE.
    /// Lưu trữ danh sách thành viên trong nhóm (tối đa 6 người theo SAO).
    /// </summary>
    [DisallowMultipleComponent]
    public class SocialManager : MonoBehaviour
    {
        private readonly List<PartyMember> _partyMembers = new List<PartyMember>();

        public IReadOnlyList<PartyMember> PartyMembers => _partyMembers;

        private static SocialManager s_instance;
        public static SocialManager Instance => s_instance;

        public event Action PartyChanged;

        private void Awake()
        {
            s_instance = this;
            InitDefaultParty();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.pKey.wasPressedThisFrame)
            {
                GameEvents.RaiseSocialPanelRequested();
            }

            // Đồng bộ HP người chơi chính vào slot 0 của party
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null && _partyMembers.Count > 0)
            {
                var hp = player.GetComponent<HealthComponent>();
                if (hp != null)
                {
                    _partyMembers[0].currentHp = hp.CurrentHealth;
                    _partyMembers[0].maxHp = hp.MaxHealth;
                    _partyMembers[0].isDead = hp.IsDead;
                }
            }
        }

        private void InitDefaultParty()
        {
            _partyMembers.Clear();

            // Người chơi chính (Leader)
            _partyMembers.Add(new PartyMember("player_self", "Bạn (Kirito)", 15, 280, isLeader: true));

            // Đồng đội mặc định theo SAO
            _partyMembers.Add(new PartyMember("companion_asuna", "Asuna (Tia Chớp)", 15, 250, isLeader: false));
            _partyMembers.Add(new PartyMember("ally_klein", "Klein (Phong Lâm Hỏa Sơn)", 14, 260, isLeader: false));
            _partyMembers.Add(new PartyMember("ally_agil", "Agil (Tiệm Rìu Chiến)", 15, 320, isLeader: false));
        }

        public bool TryInviteMember(string id, string name, int level, double maxHp)
        {
            if (!PartyRules.CanAddMember(_partyMembers.Count, id, _partyMembers))
            {
                GameEvents.RaiseCombatNotice("Không thể thêm thành viên: Nhóm đã đầy (tối đa 6) hoặc người này đã có trong đội!", Color.yellow);
                return false;
            }

            _partyMembers.Add(new PartyMember(id, name, level, maxHp));
            PartyChanged?.Invoke();
            GameEvents.RaiseCombatNotice($"🎉 {name} đã gia nhập tổ đội!", Color.green);
            return true;
        }

        public void LeaveParty()
        {
            if (_partyMembers.Count > 1)
            {
                // Rời đội thì chỉ còn 1 mình
                var self = _partyMembers[0];
                _partyMembers.Clear();
                _partyMembers.Add(self);
                PartyChanged?.Invoke();
                GameEvents.RaiseCombatNotice("Bạn đã rời khỏi tổ đội.", Color.gray);
            }
        }
    }
}
