using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core.World
{
    public interface ISaveService
    {
        Task<T> LoadAsync<T>(string key);

        /// <summary>
        /// Đọc từ NGUỒN CHÍNH, KHÔNG fallback sang file local khi nguồn chính lỗi/chưa sẵn sàng
        /// (ném <see cref="SaveUnavailableException"/>). Dùng cho dữ liệu fail-closed như "death".
        /// Khi chưa có backend cloud (mock/offline) thì file local chính là nguồn duy nhất.
        /// </summary>
        Task<T> LoadAuthoritativeAsync<T>(string key);
        Task SaveAsync<T>(string key, T value);
        Task DeleteAsync(string key);
        Task<Dictionary<string, object>> LoadBatchAsync(IEnumerable<string> keys);
        Task SaveBatchAsync(Dictionary<string, object> keyValues);
    }
}
