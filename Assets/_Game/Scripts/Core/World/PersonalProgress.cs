using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Core.World
{
    /// <summary>
    /// Theo dõi TẦNG CAO NHẤT người chơi đã vào (Player Data key "progress", docs/03 §5).
    ///
    /// Lớp thuần: chỉ biết <see cref="ISaveService"/>, không biết Floor/PvE cụ thể.
    /// FloorLoader báo "đã vào tầng N" (qua callback do bootstrap nối), lớp này chỉ ghi nhận khi N lớn hơn mức hiện có.
    /// Tầng đã qua kiểm tra khóa của loader nên không thể vượt tầng World State đã mở (docs/03 §5: personalHighestFloor bị chặn bởi World State).
    ///
    /// Quy tắc:
    /// • Chỉ TĂNG. Giá trị trong bộ nhớ cập nhật NGAY, trước khi lưu — cái chết chụp từ bộ nhớ (không đợi đọc save).
    /// • Chỉ đổi personalHighestFloor; currentFloorId giữ nguyên như đã đọc.
    /// • KHÔNG BAO GIỜ ném ra ngoài: đọc/lưu lỗi chỉ log cảnh báo. Lưu lỗi → giữ giá trị trong bộ nhớ, thử lại ở lần lưu sau.
    /// • Chưa đọc được save thì không ghi đè (tránh hạ tầng cao nhất đã lưu xuống mức mặc định).
    /// </summary>
    public sealed class PersonalProgress
    {
        private readonly ISaveService _save;
        private readonly SemaphoreSlim _saveGate = new SemaphoreSlim(1, 1);

        private ProgressData _data = new ProgressData();
        private bool _loaded;
        private bool _dirty;

        public PersonalProgress(ISaveService saveService)
        {
            _save = saveService;
        }

        /// <summary>Tầng cao nhất đã vào (luôn &gt;= 1). Đọc từ bộ nhớ, không chờ gì.</summary>
        public int HighestFloor => _data.personalHighestFloor;

        /// <summary>currentFloorId như đã đọc từ save (lớp này không bao giờ đổi nó).</summary>
        public string CurrentFloorId => _data.currentFloorId;

        /// <summary>Có thay đổi trong bộ nhớ chưa lưu được lên save.</summary>
        public bool HasUnsavedChanges => _dirty;

        /// <summary>
        /// Đọc ProgressData từ key "progress". Lỗi hoặc chưa có → mặc định (tầng cao nhất = 1), log cảnh báo, KHÔNG ném.
        /// Nếu trong lúc chờ đọc người chơi đã vào tầng cao hơn thì giữ giá trị cao hơn (và đánh dấu cần lưu).
        /// </summary>
        public async Task LoadAsync()
        {
            ProgressData loaded = null;
            bool readOk = true;

            if (_save != null)
            {
                try
                {
                    loaded = await _save.LoadAsync<ProgressData>(SaveKeys.Progress);
                }
                catch (Exception error)
                {
                    readOk = false;
                    Debug.LogWarning($"[PersonalProgress] Không đọc được '{SaveKeys.Progress}', dùng mặc định (tầng cao nhất = 1): {error.Message}");
                }
            }

            if (loaded == null) loaded = new ProgressData();
            if (loaded.personalHighestFloor < 1) loaded.personalHighestFloor = 1;

            if (_data.personalHighestFloor > loaded.personalHighestFloor)
            {
                loaded.personalHighestFloor = _data.personalHighestFloor;
                _dirty = true;
            }

            _data = loaded;
            _loaded = readOk;
        }

        /// <summary>
        /// Báo người chơi vừa vào tầng số <paramref name="floorNumber"/>. Chỉ khi lớn hơn mức hiện có mới cập nhật
        /// (bộ nhớ NGAY) rồi lưu key "progress". Không tăng và không có gì chờ lưu → không lưu. Không bao giờ ném.
        /// </summary>
        public async Task RecordFloorEnteredAsync(int floorNumber)
        {
            if (floorNumber > _data.personalHighestFloor)
            {
                _data.personalHighestFloor = floorNumber;
                _dirty = true;
            }

            if (!_dirty) return;
            await SaveIfDirtyAsync();
        }

        private async Task SaveIfDirtyAsync()
        {
            if (_save == null) return; // không có nơi lưu: chỉ giữ trong bộ nhớ

            await _saveGate.WaitAsync();
            try
            {
                if (!_dirty) return; // lượt trước đã lưu giùm

                // Chưa đọc được save thì không ghi đè: bản đã lưu có thể đang cao hơn mức mặc định trong bộ nhớ.
                if (!_loaded) await LoadAsync();
                if (!_loaded) return;

                var snapshot = new ProgressData
                {
                    personalHighestFloor = _data.personalHighestFloor,
                    currentFloorId = _data.currentFloorId
                };

                // Bỏ cờ TRƯỚC khi chờ: tầng mới vào trong lúc đang lưu sẽ bật lại cờ và được lưu ở lượt kế.
                _dirty = false;
                try
                {
                    await _save.SaveAsync(SaveKeys.Progress, snapshot);
                }
                catch (Exception error)
                {
                    _dirty = true; // giữ nguyên giá trị trong bộ nhớ, lần lưu sau thử lại
                    Debug.LogWarning($"[PersonalProgress] Chưa lưu được '{SaveKeys.Progress}' (giữ trong bộ nhớ, sẽ thử lại): {error.Message}");
                }
            }
            finally
            {
                _saveGate.Release();
            }
        }
    }
}
