using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Events;
using Game.Core.Skills;
using Game.Core.World;
using Game.Services;

namespace Game.PvE.Skills
{
    /// <summary>Mastery persistence qua ISaveService, fail-closed sau death/account lock.</summary>
    public class PlayerMasteryService : IMasteryService, IDisposable
    {
        private const int RetrySeconds = 5;

        public event EventHandler<TechniqueMasteryChangedArgs> TechniqueMasteryChanged;

        private readonly Dictionary<WeaponDiscipline, int> _mastery = new Dictionary<WeaponDiscipline, int>();
        private readonly ISaveService _saveService;
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _retryCancellation = new CancellationTokenSource();
        private bool _loaded;
        private bool _dirty;
        private bool _saveLocked;
        private bool _disposed;
        private bool _loadRetryScheduled;
        private bool _saveRetryScheduled;
        private int _revision;

        public PlayerMasteryService() : this(ServiceProvider.CreateSaveService()) { }

        public PlayerMasteryService(ISaveService saveService)
        {
            _saveService = saveService ?? ServiceProvider.CreateSaveService();
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
        }

        public int GetMastery(WeaponDiscipline discipline)
        {
            if (discipline == WeaponDiscipline.None) return 0;
            return _mastery.TryGetValue(discipline, out int value) ? Clamp(value, 0, 100) : 0;
        }

        public IReadOnlyDictionary<WeaponDiscipline, int> GetAll()
        {
            var snapshot = new Dictionary<WeaponDiscipline, int>();
            foreach (var pair in _mastery)
            {
                if (pair.Key != WeaponDiscipline.None) snapshot[pair.Key] = Clamp(pair.Value, 0, 100);
            }
            return snapshot;
        }

        public int AddMastery(WeaponDiscipline discipline, int credit)
        {
            if (discipline == WeaponDiscipline.None || credit <= 0) return 0;
            int oldValue = GetMastery(discipline);
            int newValue = Clamp(oldValue + credit, 0, 100);
            int actual = newValue - oldValue;
            if (actual <= 0) return 0;

            _mastery[discipline] = newValue;
            MarkDirty();
            TechniqueMasteryChanged?.Invoke(this, new TechniqueMasteryChangedArgs(discipline, oldValue, newValue));
            return actual;
        }

        public void SetMastery(WeaponDiscipline discipline, int value)
        {
            if (discipline == WeaponDiscipline.None) return;
            int oldValue = GetMastery(discipline);
            int newValue = Clamp(value, 0, 100);
            if (oldValue == newValue) return;

            _mastery[discipline] = newValue;
            MarkDirty();
            TechniqueMasteryChanged?.Invoke(this, new TechniqueMasteryChangedArgs(discipline, oldValue, newValue));
        }

        public async Task LoadAsync()
        {
            if (_loaded || !CanLoad()) return;
            bool loaded = await LoadOnceAsync();
            if (!loaded) ScheduleLoadRetry();
        }

        public async Task<bool> SaveAsync()
        {
            if (!CanSave()) return false;

            await _saveLock.WaitAsync();
            try
            {
                if (!CanSave()) return false;
                if (!_dirty) return true;

                int savedRevision = _revision;
                MasterySaveData data = MasterySaveData.FromDictionary(_mastery);
                try
                {
                    await _saveService.SaveAsync(MasterySaveKeys.Mastery, data);
                    if (_revision == savedRevision) _dirty = false;
                    return !_dirty;
                }
                catch (Exception)
                {
                    _dirty = true;
                    ScheduleSaveRetry();
                    return false;
                }
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _retryCancellation.Cancel();
            _retryCancellation.Dispose();
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
        }

        private async Task<bool> LoadOnceAsync()
        {
            if (!CanLoad()) return false;
            try
            {
                // null là "chưa có save" hợp lệ; exception mới là load failure.
                MasterySaveData data = await _saveService.LoadAsync<MasterySaveData>(MasterySaveKeys.Mastery);
                if (!CanLoad()) return false;

                _mastery.Clear();
                if (data != null)
                {
                    foreach (var pair in data.ToDictionary()) _mastery[pair.Key] = pair.Value;
                }
                _dirty = false;
                _loaded = true;
                return true;
            }
            catch (Exception)
            {
                _loaded = false;
                return false;
            }
        }

        private void ScheduleLoadRetry()
        {
            if (_loadRetryScheduled || _loaded || !CanLoad()) return;
            _ = RetryLoadAsync();
        }

        private async Task RetryLoadAsync()
        {
            _loadRetryScheduled = true;
            try
            {
                while (!_loaded && CanLoad())
                {
                    await Task.Delay(TimeSpan.FromSeconds(RetrySeconds), _retryCancellation.Token);
                    if (await LoadOnceAsync()) return;
                }
            }
            catch (OperationCanceledException) { }
            finally { _loadRetryScheduled = false; }
        }

        private void ScheduleSaveRetry()
        {
            if (_saveRetryScheduled || !_dirty || !CanSave()) return;
            _ = RetrySaveAsync();
        }

        private async Task RetrySaveAsync()
        {
            _saveRetryScheduled = true;
            try
            {
                while (_dirty && CanSave())
                {
                    await Task.Delay(TimeSpan.FromSeconds(RetrySeconds), _retryCancellation.Token);
                    if (await SaveAsync()) return;
                }
            }
            catch (OperationCanceledException) { }
            finally { _saveRetryScheduled = false; }
        }

        private void HandlePlayerDied(object sender, Game.Core.Combat.DamageInfo info) => LockSaving();
        private void HandleDeathRecorded(object sender, Game.Core.Character.DeathInfo info) => LockSaving();

        private void LockSaving()
        {
            _saveLocked = true;
            _dirty = false;
            _retryCancellation.Cancel();
        }

        private void MarkDirty()
        {
            _revision++;
            if (_loaded && !_saveLocked) _dirty = true;
        }

        private bool CanLoad() => !_disposed && !_saveLocked && _saveService != null;
        private bool CanSave() => _loaded && !_disposed && !_saveLocked && _saveService != null;

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
