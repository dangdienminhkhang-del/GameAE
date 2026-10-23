using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// Bảng id → clip (docs/05 Phase 9.5). Tạo asset: Create → Game → Audio → Audio Catalog
    /// (gợi ý Assets/_Game/Audio/AudioCatalog.asset). Mỗi entry ứng với một id trong Game.Core.Audio.AudioIds.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Game/Audio/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Id ổn định, ví dụ sfx.hit_enemy hoặc music.gameplay (xem AudioIds).")]
            public string id;
            [Tooltip("Một hoặc nhiều clip; SFX chọn ngẫu nhiên một clip mỗi lần phát. Nhạc dùng clip đầu tiên. Để trống = im lặng.")]
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 0.8f;
            [Tooltip("Lệch cao độ ngẫu nhiên ± giá trị này (SFX). 0.05 là đủ để đỡ nhàm.")]
            [Range(0f, 0.5f)] public float pitchJitter = 0f;
            [Tooltip("Khoảng cách tối thiểu (giây) giữa hai lần phát cùng id, chống spam. 0 = không giới hạn.")]
            public float minIntervalSeconds = 0f;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string id, out Entry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(id)) return false;

            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(id, out entry);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, Entry>(entries.Count);
            foreach (Entry entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                // Trùng id: entry đầu tiên thắng (OnValidate cảnh báo trong Editor).
                if (!_lookup.ContainsKey(entry.id)) _lookup.Add(entry.id, entry);
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }

        private void OnValidate()
        {
            _lookup = null;

            var seen = new HashSet<string>();
            foreach (Entry entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                if (!seen.Add(entry.id))
                {
                    Debug.LogWarning($"[AudioCatalog] Trùng id '{entry.id}' — chỉ entry đầu tiên được dùng.", this);
                }
            }
        }
    }
}
