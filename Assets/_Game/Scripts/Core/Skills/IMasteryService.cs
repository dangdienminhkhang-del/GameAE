using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Core.Skills
{
    /// <summary>
    /// Event args khi mastery đổi — UI/Save/Progression nghe.
    /// </summary>
    public class TechniqueMasteryChangedArgs : EventArgs
    {
        public WeaponDiscipline Discipline;
        public int OldValue;
        public int NewValue;

        public TechniqueMasteryChangedArgs(WeaponDiscipline d, int oldValue, int newValue)
        {
            Discipline = d;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Quản lý Weapon Mastery theo từng Discipline (range 0-100).
    /// Giữ event TechniqueMasteryChanged như ITimeService giữ time event (docs/13 Wave 2A contract).
    /// Persistence qua ISaveService abstraction (không gọi UGS trực tiếp).
    /// </summary>
    public interface IMasteryService
    {
        /// <summary>Khi mastery đổi (cả khi tăng / override khi load).</summary>
        event EventHandler<TechniqueMasteryChangedArgs> TechniqueMasteryChanged;

        /// <summary>Đọc mastery hiện tại (0-100). Discipline None luôn = 0.</summary>
        int GetMastery(WeaponDiscipline discipline);

        /// <summary>Đọc tất cả mastery hiện có (read-only snapshot).</summary>
        IReadOnlyDictionary<WeaponDiscipline, int> GetAll();

        /// <summary>Cộng mastery, clamp 0-100; bắn event nếu thực sự đổi. Trả về lượng cộng thực tế (0 nếu vượt cap).</summary>
        int AddMastery(WeaponDiscipline discipline, int credit);

        /// <summary>Override trực tiếp (khi load save); bắn event nếu khác value cũ.</summary>
        void SetMastery(WeaponDiscipline discipline, int value);

        /// <summary>Đọc MasterySaveData từ nguồn lưu. Caller bootstrap (PvEBootstrap hoặc MasteryService cài đặt) gọi khi vào gameplay.</summary>
        Task LoadAsync();

        /// <summary>Lưu MasterySaveData xuống nguồn lưu (ISaveService). Gọi sau khi đổi và khi exit scene.</summary>
        Task SaveAsync();
    }
}
