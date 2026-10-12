using System.Threading.Tasks;

namespace Game.Core.World
{
    /// <summary>
    /// Ghi dữ liệu QUAN TRỌNG (vd. permadeath key "death") và chỉ hoàn tất khi NGUỒN CHÍNH xác nhận
    /// (docs/09 §1.3, ADR-021). Khác <see cref="ISaveService.SaveAsync{T}"/> — hàm đó nuốt lỗi cloud.
    /// Interface riêng để KHÔNG vỡ các class/test fake đang cài <see cref="ISaveService"/>.
    /// </summary>
    public interface IAuthoritativeSaveService
    {
        /// <summary>
        /// Ghi local trước (bản recovery), rồi ghi nguồn chính. Task chỉ hoàn tất khi nguồn chính xác nhận.
        /// Ném <see cref="SaveUnavailableException"/> nếu nguồn chính chưa sẵn sàng hoặc ghi lỗi.
        /// Khi không có backend cloud (mock/offline), file/bộ nhớ local là nguồn duy nhất → ghi local là xác nhận.
        /// </summary>
        Task SaveAuthoritativeAsync<T>(string key, T value);
    }

    /// <summary>
    /// Kho file recovery CỤC BỘ (D2): giữ bản ghi mà nguồn chính chưa nhận được — vd. cái chết ghi cloud lỗi
    /// rồi người chơi tắt app (nếu không, lần mở sau cloud vẫn nói "còn sống" → sống lại).
    /// Khác <see cref="ISaveService"/>: file này CHỈ nằm trên máy, KHÔNG BAO GIỜ được đẩy lên cloud.
    /// Tách thành interface riêng (không thêm vào <see cref="IAuthoritativeSaveService"/>) vì service lưu trong
    /// bộ nhớ (PlayerSaveService) không có đĩa và đang cài interface kia — thêm member sẽ làm vỡ nó.
    /// Mỗi <c>name</c> ứng với một file <c>{name}.json</c> trong thư mục local của service.
    /// </summary>
    public interface ILocalRecoveryStore
    {
        /// <summary>Ghi (đè) file recovery. Trả false nếu ghi lỗi — KHÔNG ném, vì caller đang xử lý một lỗi khác.</summary>
        bool TryWriteRecovery<T>(string name, T value);

        /// <summary>Đọc file recovery. Không có file, hoặc file hỏng → <c>default</c> (không ném).</summary>
        T ReadRecovery<T>(string name);

        /// <summary>Xóa file recovery. Không có file thì coi như đã xóa (true). Trả false nếu xóa lỗi.</summary>
        bool DeleteRecovery(string name);
    }
}
