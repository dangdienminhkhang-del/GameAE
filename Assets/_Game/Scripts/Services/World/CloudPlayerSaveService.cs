using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Game.Core.World;
using UnityEngine;

#if UGS_CLOUDSAVE_PRESENT
using Unity.Services.CloudSave;
#endif

namespace Game.Services.World
{
    /// <summary>
    /// ISaveService thật (Phase 5): Player Data trên Unity Cloud Save khi đã đăng nhập,
    /// tự động fallback sang file JSON trong persistentDataPath khi offline/chưa đăng nhập.
    /// DTO lưu bằng JsonUtility nên phải là class [Serializable] với field public (xem Core/World/PlayerData.cs).
    /// </summary>
    public class CloudPlayerSaveService : ISaveService
    {
        private readonly string _localDirectory;

        public CloudPlayerSaveService(string localDirectory = null)
        {
            _localDirectory = string.IsNullOrEmpty(localDirectory)
                ? Path.Combine(Application.persistentDataPath, "PlayerData")
                : localDirectory;
        }

        public string LocalDirectory => _localDirectory;

        private static bool CloudReady
        {
            get
            {
#if UGS_CLOUDSAVE_PRESENT
                try
                {
                    return Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized;
                }
                catch
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public async Task<T> LoadAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;

#if UGS_CLOUDSAVE_PRESENT
            if (CloudReady)
            {
                try
                {
                    Dictionary<string, Unity.Services.CloudSave.Models.Item> data =
                        await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                    if (data != null && data.TryGetValue(key, out var item))
                    {
                        return item.Value.GetAs<T>();
                    }
                    return default;
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[CloudPlayerSaveService] Cloud load '{key}' lỗi, dùng file local: {error.Message}");
                }
            }
#endif
            return LoadLocal<T>(key);
        }

        /// <summary>
        /// Đọc từ nguồn chính, KHÔNG fallback local khi cloud lỗi (fail-closed — dùng cho key "death").
        /// - Có UGS Cloud Save: cloud chưa sẵn sàng hoặc lỗi mạng → ném SaveUnavailableException.
        /// - Chưa có UGS (mock/offline hiện tại): file local là nguồn duy nhất nên đọc local.
        /// </summary>
        public async Task<T> LoadAuthoritativeAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key)) return default;

#if UGS_CLOUDSAVE_PRESENT
            if (!CloudReady) throw new SaveUnavailableException(key, null);

            try
            {
                Dictionary<string, Unity.Services.CloudSave.Models.Item> data =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
                if (data != null && data.TryGetValue(key, out var item))
                {
                    return item.Value.GetAs<T>();
                }
                return default;
            }
            catch (Exception error)
            {
                throw new SaveUnavailableException(key, error);
            }
#else
            await Task.CompletedTask;
            return LoadLocal<T>(key);
#endif
        }

        public async Task SaveAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return;

            SaveLocal(key, value);

#if UGS_CLOUDSAVE_PRESENT
            if (CloudReady)
            {
                try
                {
                    await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, value } });
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[CloudPlayerSaveService] Cloud save '{key}' lỗi (đã lưu local): {error.Message}");
                }
            }
#endif
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            DeleteLocal(key);

#if UGS_CLOUDSAVE_PRESENT
            if (CloudReady)
            {
                try
                {
                    await CloudSaveService.Instance.Data.Player.DeleteAsync(key);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[CloudPlayerSaveService] Cloud delete '{key}' lỗi: {error.Message}");
                }
            }
#endif
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, object>();
            if (keys == null) return result;

            foreach (string key in keys)
            {
                string json = ReadLocalJson(key);
                if (json != null) result[key] = json;
            }
            await Task.CompletedTask;
            return result;
        }

        public async Task SaveBatchAsync(Dictionary<string, object> keyValues)
        {
            if (keyValues == null) return;

            foreach (var pair in keyValues)
            {
                if (pair.Value is string json) WriteLocalJson(pair.Key, json);
            }
            await Task.CompletedTask;
        }

        // ── Local file fallback ───────────────────────────────────

        private string PathFor(string key)
        {
            return Path.Combine(_localDirectory, key + ".json");
        }

        private T LoadLocal<T>(string key)
        {
            string json = ReadLocalJson(key);
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Local load '{key}' lỗi: {error.Message}");
                return default;
            }
        }

        private void SaveLocal<T>(string key, T value)
        {
            try
            {
                Directory.CreateDirectory(_localDirectory);
                WriteLocalJson(key, JsonUtility.ToJson(value, true));
            }
            catch (Exception error)
            {
                Debug.LogError($"[CloudPlayerSaveService] Local save '{key}' lỗi: {error.Message}");
            }
        }

        private void DeleteLocal(string key)
        {
            try
            {
                string path = PathFor(key);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[CloudPlayerSaveService] Local delete '{key}' lỗi: {error.Message}");
            }
        }

        private string ReadLocalJson(string key)
        {
            try
            {
                string path = PathFor(key);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch
            {
                return null;
            }
        }

        private void WriteLocalJson(string key, string json)
        {
            Directory.CreateDirectory(_localDirectory);
            File.WriteAllText(PathFor(key), json);
        }
    }
}
