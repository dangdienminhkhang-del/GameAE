using System;
using System.Collections.Generic;
using Game.Core.Guidance;
using UnityEngine;

namespace Game.Services.Guidance
{
    /// <summary>Kho khóa-giá trị tối thiểu mà hướng dẫn cần (mặc định PlayerPrefs; test dùng bản trong bộ nhớ hoặc bản luôn lỗi).</summary>
    public interface ITutorialKeyValueBackend
    {
        bool TryGetInt(string key, out int value);
        void SetInt(string key, int value);
        bool TryGetString(string key, out string value);
        void SetString(string key, string value);
        void Delete(string key);
        void Save();
    }

    public sealed class PlayerPrefsTutorialBackend : ITutorialKeyValueBackend
    {
        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            if (!PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetInt(key, 0);
            return true;
        }

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        public bool TryGetString(string key, out string value)
        {
            value = null;
            if (!PlayerPrefs.HasKey(key)) return false;
            value = PlayerPrefs.GetString(key, string.Empty);
            return true;
        }

        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        public void Delete(string key) => PlayerPrefs.DeleteKey(key);

        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>
    /// Cờ hướng dẫn lưu CỤC BỘ bằng PlayerPrefs (Phase 9.9). Không vào Player Data, không gọi server.
    /// - Cờ "đã xem": khóa <c>tutorial.{playerId}.{hintId}</c>; hai playerId độc lập (Guest mới tự thấy lại gợi ý).
    /// - Chỉ mục <c>tutorial.{playerId}._index</c> ghi lại hint đã đánh dấu, để <see cref="ClearAll"/> chỉ xóa đúng khóa của gói này
    ///   (không đụng <c>settings.audio.*</c> hay khóa nào khác).
    /// - Công tắc: khóa <c>settings.tutorial_hints</c> do chính kho này giữ (KHÔNG đổi hợp đồng ISettingsStore/PlayerSettings của P9.6). Mặc định bật.
    /// - Mọi truy cập PlayerPrefs bọc try/catch: kho hỏng thì không ném lỗi; cờ vẫn được nhớ trong bộ nhớ suốt phiên để gợi ý không hiện lặp.
    /// </summary>
    public sealed class PlayerPrefsTutorialStore : ITutorialStore
    {
        public const string KeyPrefix = "tutorial.";
        public const string IndexSuffix = "._index";
        public const string EnabledKey = "settings.tutorial_hints";
        private const char IndexSeparator = ';';

        private readonly ITutorialKeyValueBackend _backend;
        private readonly HashSet<string> _seenInMemory = new HashSet<string>();
        private bool? _enabledInMemory;

        public PlayerPrefsTutorialStore() : this(new PlayerPrefsTutorialBackend()) { }

        public PlayerPrefsTutorialStore(ITutorialKeyValueBackend backend)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public static string SeenKey(string playerId, string hintId) => KeyPrefix + playerId + "." + hintId;
        public static string IndexKey(string playerId) => KeyPrefix + playerId + IndexSuffix;

        public bool HasSeen(string playerId, string hintId)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(hintId)) return false;

            string key = SeenKey(playerId, hintId);
            if (_seenInMemory.Contains(key)) return true;

            try
            {
                return _backend.TryGetInt(key, out int value) && value != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void MarkSeen(string playerId, string hintId)
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(hintId)) return;

            string key = SeenKey(playerId, hintId);
            _seenInMemory.Add(key);

            try
            {
                _backend.SetInt(key, 1);
                AddToIndex(playerId, hintId);
                _backend.Save();
            }
            catch (Exception)
            {
                // Kho hỏng: vẫn nhớ trong bộ nhớ. Không ném lỗi để không làm vỡ gameplay.
            }
        }

        public void ClearAll(string playerId, IEnumerable<string> knownHintIds)
        {
            if (string.IsNullOrEmpty(playerId)) return;

            var ids = new HashSet<string>();
            try
            {
                foreach (string id in ReadIndex(playerId)) ids.Add(id);
            }
            catch (Exception)
            {
                // Chỉ mục đọc lỗi: dùng danh mục hint truyền vào.
            }

            if (knownHintIds != null)
            {
                foreach (string id in knownHintIds)
                {
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }

            foreach (string id in ids) _seenInMemory.Remove(SeenKey(playerId, id));

            try
            {
                foreach (string id in ids) _backend.Delete(SeenKey(playerId, id));
                _backend.Delete(IndexKey(playerId));
                _backend.Save();
            }
            catch (Exception)
            {
                // Không ném lỗi.
            }
        }

        public bool GetEnabled()
        {
            if (_enabledInMemory.HasValue) return _enabledInMemory.Value;

            try
            {
                return !_backend.TryGetInt(EnabledKey, out int value) || value != 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public void SetEnabled(bool enabled)
        {
            _enabledInMemory = enabled;

            try
            {
                _backend.SetInt(EnabledKey, enabled ? 1 : 0);
                _backend.Save();
            }
            catch (Exception)
            {
                // Không ném lỗi; giá trị vẫn có hiệu lực trong phiên.
            }
        }

        private void AddToIndex(string playerId, string hintId)
        {
            var ids = new List<string>(ReadIndex(playerId));
            if (ids.Contains(hintId)) return;

            ids.Add(hintId);
            _backend.SetString(IndexKey(playerId), string.Join(IndexSeparator.ToString(), ids));
        }

        private IEnumerable<string> ReadIndex(string playerId)
        {
            if (!_backend.TryGetString(IndexKey(playerId), out string raw) || string.IsNullOrEmpty(raw))
                return Array.Empty<string>();

            return raw.Split(new[] { IndexSeparator }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
