using System.Threading.Tasks;
using Game.Core.Time;
using Game.Core.World;

namespace Game.Core.Network
{
    public interface IServerFunctionService
    {
        /// <summary>
        /// Báo "boss đã bị hạ" lên server và trả kết quả server ĐÃ XÁC NHẬN (docs/02 §22, docs/12 §7.2).
        /// NÉM <see cref="ServerFunctionUnavailableException"/> khi không xác nhận được
        /// (lỗi mạng, thiếu Cloud Code, ok != true, response thiếu field...). Không bao giờ trả
        /// "thành công giả": caller chỉ được đổi World State khi nhận được <see cref="BossDefeatResult"/>.
        /// </summary>
        Task<BossDefeatResult> ReportBossDefeatedAsync(string bossId, string floorId);

        /// <summary>
        /// Lấy snapshot thời gian authoritative từ server.
        /// Trả về <c>null</c> nếu không có snapshot được xác nhận (lỗi mạng, lỗi Cloud Code, field thiếu…):
        /// caller phải giữ <c>IsAuthoritative=false</c> và tiếp tục tick local (docs/08 §5 / docs/03 §12).
        /// </summary>
        Task<WorldTimeSnapshot> GetWorldTimeSnapshotAsync();
    }
}
