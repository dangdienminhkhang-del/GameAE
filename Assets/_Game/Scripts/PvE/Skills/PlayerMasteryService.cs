using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Skills;
using Game.Core.World;
using Game.Services;

namespace Game.PvE.Skills
{
    /// <summary>
    /// Cài đặt IMasteryService cho PvE: giữ trạng thái mastery trong bộ nhớ,
    /// save/load qua ISaveService abstraction (KHÔNG gọi UGS trực tiếp).
    /// TechniqueComponent/PvEBootstrap giữ instance này.
    /// </summary>
    public class PlayerMasteryService : IMasteryService
    {
        public event EventHandler<TechniqueMasteryChangedArgs> TechniqueMasteryChanged;

        private readonly Dictionary<WeaponDiscipline, int> _mastery = new Dictionary<WeaponDiscipline, int>();
        private readonly ISaveService _saveService;
        private bool _loaded;

        public PlayerMasteryService()
            : this(ServiceProvider.CreateSaveService())
        {
        }

        public PlayerMasteryService(ISaveService saveService)
        {
            _saveService = saveService ?? ServiceProvider.CreateSaveService();
        }

        public int GetMastery(WeaponDiscipline discipline)
        {
            if (discipline == WeaponDiscipline.None) return 0;
            return _mastery.TryGetValue(discipline, out int v) ? Clamp(v, 0, 100) : 0;
        }

        public IReadOnlyDictionary<WeaponDiscipline, int> GetAll()
        {
            var snapshot = new Dictionary<WeaponDiscipline, int>();
            foreach (var pair in _mastery)
            {
                if (pair.Key == WeaponDiscipline.None) continue;
                snapshot[pair.Key] = Clamp(pair.Value, 0, 100);
            }
            return snapshot;
        }

        public int AddMastery(WeaponDiscipline discipline, int credit)
        {
            if (discipline == WeaponDiscipline.None || credit <= 0) return 0;
            int old = GetMastery(discipline);
            int raw = old + credit;
            int clamped = Clamp(raw, 0, 100);
            int actual = clamped - old;
            if (actual <= 0) return 0;

            _mastery[discipline] = clamped;
            TechniqueMasteryChanged?.Invoke(this, new TechniqueMasteryChangedArgs(discipline, old, clamped));
            return actual;
        }

        public void SetMastery(WeaponDiscipline discipline, int value)
        {
            if (discipline == WeaponDiscipline.None) return;
            int clamped = Clamp(value, 0, 100);
            int old = GetMastery(discipline);
            if (old == clamped) return;

            _mastery[discipline] = clamped;
            TechniqueMasteryChanged?.Invoke(this, new TechniqueMasteryChangedArgs(discipline, old, clamped));
        }

        public async Task LoadAsync()
        {
            if (_saveService == null) { _loaded = true; return; }
            try
            {
                MasterySaveData data = null;
                try
                {
                    data = await _saveService.LoadAsync<MasterySaveData>(MasterySaveKeys.Mastery);
                }
                catch (SaveUnavailableException)
                {
                    // Fallback offline: data mặc định rỗng.
                    data = null;
                }
                catch (Exception)
                {
                    data = null;
                }

                _mastery.Clear();
                if (data != null)
                {
                    Dictionary<WeaponDiscipline, int> dict = data.ToDictionary();
                    foreach (var pair in dict) _mastery[pair.Key] = pair.Value;
                }
                _loaded = true;
            }
            catch (Exception)
            {
                _loaded = true;
            }
        }

        public async Task SaveAsync()
        {
            if (_saveService == null) return;
            try
            {
                MasterySaveData data = MasterySaveData.FromDictionary(_mastery);
                await _saveService.SaveAsync(MasterySaveKeys.Mastery, data);
            }
            catch (Exception)
            {
                // Save thất bại: giữ trạng thái trong bộ nhớ.
                // UI có thể hiển thị cảnh báo nếu cần; critical path không throw.
            }
        }

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
