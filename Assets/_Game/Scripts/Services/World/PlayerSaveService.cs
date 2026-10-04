using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.World;

namespace Game.Services.World
{
    public class PlayerSaveService : ISaveService
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public virtual async Task<T> LoadAsync<T>(string key)
        {
            await Task.CompletedTask;
            if (_cache.TryGetValue(key, out var value) && value is T typed)
            {
                return typed;
            }
            return default;
        }

        public virtual Task<T> LoadAuthoritativeAsync<T>(string key)
        {
            // Bộ nhớ trong tiến trình là nguồn duy nhất → không có chuyện "nguồn chính lỗi".
            return LoadAsync<T>(key);
        }

        public virtual async Task SaveAsync<T>(string key, T value)
        {
            await Task.CompletedTask;
            _cache[key] = value;
        }

        public virtual async Task DeleteAsync(string key)
        {
            await Task.CompletedTask;
            _cache.Remove(key);
        }

        public virtual async Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys)
        {
            await Task.CompletedTask;
            var result = new Dictionary<string, object>();
            foreach (var k in keys)
            {
                if (_cache.TryGetValue(k, out var v)) result[k] = v;
            }
            return result;
        }

        public virtual async Task SaveBatchAsync(Dictionary<string, object> keyValues)
        {
            await Task.CompletedTask;
            foreach (var kv in keyValues)
            {
                _cache[kv.Key] = kv.Value;
            }
        }
    }
}
