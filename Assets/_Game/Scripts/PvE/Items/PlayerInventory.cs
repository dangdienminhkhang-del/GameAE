using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Character;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Items;
using Game.Core.Stats;
using Game.Core.World;
using Game.PvE.Player;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Items
{
    /// <summary>
    /// Kho + trang bị của player: nhặt đồ, mặc/tháo, cộng chỉ số, lưu/tải qua ISaveService.
    /// Là nơi DUY NHẤT gọi save cho profile/inventory/equipment (docs/03 §5).
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private ItemDatabase database;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private bool loadOnStart = true;
        [Tooltip("Tự động lưu sau N giây kể từ thay đổi cuối cùng (gom nhiều thay đổi thành 1 lần ghi).")]
        [SerializeField] private float saveDelaySeconds = 2f;

        private const float SaveRetrySeconds = 5f;

        private readonly Inventory _inventory = new Inventory();
        private readonly Equipment _equipment = new Equipment();
        private ISaveService _save;
        private CharacterStats _stats;
        private int _instanceCounter;

        // ── Trạng thái tự động lưu ──
        private bool _loaded;      // chỉ được lưu SAU khi nạp xong, khỏi ghi dữ liệu mặc định đè save thật
        private bool _loading;     // đang nạp: các thay đổi do LoadFrom/SetProgress sinh ra không tính là "cần lưu"
        private bool _saveLocked;  // đã chết → account bị khóa, không ghi gì nữa (tránh tạo lại file sau khi reset)
        private bool _dirty;
        private bool _saving;
        private float _saveDueAt;

        public Inventory Inventory => _inventory;
        public Equipment Equipment => _equipment;
        public ItemDatabase Database => database;
        public event Action Changed;

        private void Awake()
        {
            if (playerController == null) TryGetComponent(out playerController);
            if (playerProgress == null) TryGetComponent(out playerProgress);
            _save = ServiceProvider.CreateSaveService();
        }

        private void OnEnable()
        {
            _inventory.Changed += HandleChanged;
            _equipment.Changed += HandleChanged;
            GameEvents.OnExperienceChanged += HandleExperienceChanged;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded += HandleDeathRecorded;
        }

        private void OnDisable()
        {
            _inventory.Changed -= HandleChanged;
            _equipment.Changed -= HandleChanged;
            GameEvents.OnExperienceChanged -= HandleExperienceChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
        }

        private async void Start()
        {
            if (!loadOnStart)
            {
                _loaded = true;
                return;
            }

            try
            {
                await LoadAsync();
            }
            catch (Exception error)
            {
                // Nạp lỗi → _loaded vẫn false → KHÔNG tự động lưu (không ghi dữ liệu mặc định đè save thật).
                Debug.LogError($"[Save] Nạp Player Data lỗi, tạm tắt tự động lưu: {error}", this);
            }
        }

        private void HandleChanged()
        {
            ApplyEquipmentStats();
            Changed?.Invoke();
            RequestSave(); // nhặt / mặc / tháo đồ
        }

        /// <summary>Lên level / nhận EXP → lưu (profile).</summary>
        private void HandleExperienceChanged(object sender, ExperienceChangedArgs args)
        {
            RequestSave();
        }

        private void HandlePlayerDied(object sender, DamageInfo lastHit) => LockSaving();
        private void HandleDeathRecorded(object sender, DeathInfo info) => LockSaving();

        private void LockSaving()
        {
            _saveLocked = true;
            _dirty = false;
        }

        // ── Tự động lưu (docs/03 §12) ───────────────────────────────

        /// <summary>
        /// Đánh dấu "cần lưu" — sẽ ghi sau <c>saveDelaySeconds</c> (gom nhiều thay đổi, tránh ghi mỗi lần giết quái).
        /// Bỏ qua nếu chưa nạp xong, đang nạp, hoặc đã chết.
        /// </summary>
        public void RequestSave()
        {
            if (!_loaded || _loading || _saveLocked) return;
            _dirty = true;
            _saveDueAt = UnityEngine.Time.unscaledTime + saveDelaySeconds;
        }

        private void Update()
        {
            if (_dirty && !_saving && _loaded && !_saveLocked && UnityEngine.Time.unscaledTime >= _saveDueAt)
            {
                _dirty = false;
                _ = RunSaveAsync();
            }
        }

        /// <summary>Ghi ngay nếu đang có thay đổi chưa lưu (pause / thoát app / đổi scene).</summary>
        public void FlushNow()
        {
            if (!_dirty || !_loaded || _saveLocked) return;
            _dirty = false;
            _ = RunSaveAsync();
        }

        private async Task RunSaveAsync()
        {
            _saving = true;
            try
            {
                await SaveAsync();
            }
            catch (Exception error)
            {
                // Không nuốt lỗi: đánh dấu lại để thử ghi sau, tránh mất tiến trình mà không ai biết.
                Debug.LogError($"[Save] Lưu Player Data thất bại, sẽ thử lại: {error}");
                _dirty = true;
                _saveDueAt = UnityEngine.Time.unscaledTime + SaveRetrySeconds;
            }
            finally
            {
                _saving = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushNow();
        }

        private void OnApplicationQuit()
        {
            FlushNow();
        }

        private void OnDestroy()
        {
            FlushNow();
        }

        // ── Nhặt / thêm đồ ────────────────────────────────────────

        /// <summary>Nhặt item: trả về SỐ LƯỢNG ĐÃ NHẬN THẬT (0 = không nhận được gì).</summary>
        public int TryAddItem(ItemDefinition definition, int quantity = 1)
        {
            if (definition == null || quantity <= 0) return 0;

            int accepted;
            if (definition.Stackable)
            {
                accepted = _inventory.AddStack(definition.Id, quantity, definition.MaxStack);
            }
            else
            {
                accepted = quantity;
                for (int i = 0; i < quantity; i++)
                {
                    _inventory.AddInstance(new ItemInstance(NextInstanceId(), definition.Id, default, 0));
                }
            }

            if (accepted > 0)
            {
                GameEvents.RaiseItemAcquired(this);
                GameEvents.RaiseItemAcquired(this, new ItemAcquiredArgs { ItemId = definition.Id, Quantity = accepted });
                RequestSave();
            }
            return accepted;
        }

        private string NextInstanceId()
        {
            _instanceCounter++;
            return $"inst_{DateTime.UtcNow.Ticks}_{_instanceCounter}";
        }

        // ── Trang bị ──────────────────────────────────────────────

        /// <summary>Mặc instance theo slot của definition. Instance cũ trong slot vẫn nằm trong kho.</summary>
        public bool Equip(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            if (instance == null) return false;

            ItemDefinition definition = database != null ? database.Find(instance.itemId) : null;
            if (definition == null || definition.Slot == EquipmentSlot.None) return false;

            _equipment.Equip(definition.Slot, instanceId);
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            return _equipment.Unequip(slot) != null;
        }

        public bool IsEquipped(string instanceId)
        {
            return _equipment.IsEquipped(instanceId);
        }

        /// <summary>Cộng chỉ số trang bị vào CharacterStats rồi báo HUD.</summary>
        public void ApplyEquipmentStats()
        {
            CharacterStats stats = ResolveStats();
            if (stats == null || database == null) return;

            ItemStats bonus = database.SumEquipment(_equipment, _inventory);
            stats.SetEquipmentBonus(bonus.attack, bonus.defense, bonus.maxHealth);
            GameEvents.RaiseStatsChanged(this);
        }

        private CharacterStats ResolveStats()
        {
            if (_stats == null && playerController != null) _stats = playerController.Stats;
            return _stats;
        }

        /// <summary>Tổng chỉ số cộng thêm hiện tại (để UI hiển thị).</summary>
        public ItemStats CurrentEquipmentBonus()
        {
            return database != null ? database.SumEquipment(_equipment, _inventory) : default;
        }

        // ── Save / Load ───────────────────────────────────────────

        public async Task SaveAsync()
        {
            if (_save == null || _saveLocked) return;

            // Chụp snapshot cả 3 nhóm TRƯỚC khi await: dữ liệu nhất quán tại một thời điểm, và bản ghi local
            // (đồng bộ) không phụ thuộc vào việc cloud trả lời kịp khi app bị pause/đóng.
            InventoryData inventoryData = ToInventoryData();
            EquipmentData equipmentData = ToEquipmentData();
            ProfileData profileData = playerProgress != null
                ? new ProfileData { level = playerProgress.Level, exp = playerProgress.CurrentExp }
                : null;

            await _save.SaveAsync(SaveKeys.Inventory, inventoryData);
            await _save.SaveAsync(SaveKeys.Equipment, equipmentData);
            if (profileData != null) await _save.SaveAsync(SaveKeys.Profile, profileData);
        }

        public async Task LoadAsync()
        {
            if (_save == null) return;

            _loading = true;
            try
            {
                var profile = await _save.LoadAsync<ProfileData>(SaveKeys.Profile);
                if (profile != null && playerProgress != null) playerProgress.SetProgress(profile.level, profile.exp);

                var inventoryData = await _save.LoadAsync<InventoryData>(SaveKeys.Inventory);
                var equipmentData = await _save.LoadAsync<EquipmentData>(SaveKeys.Equipment);
                ApplyLoadedData(inventoryData, equipmentData);

                _loaded = true; // chỉ bật khi nạp THÀNH CÔNG — sau đó mới tự động lưu
            }
            finally
            {
                _loading = false;
            }
        }

        public void ApplyLoadedData(InventoryData inventoryData, EquipmentData equipmentData)
        {
            var stacks = new List<ItemStack>();
            if (inventoryData != null && inventoryData.stacks != null)
            {
                foreach (StackEntry entry in inventoryData.stacks)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.itemId)) stacks.Add(new ItemStack(entry.itemId, entry.quantity));
                }
            }

            var instances = new List<ItemInstance>();
            if (inventoryData != null && inventoryData.instances != null)
            {
                foreach (InstanceEntry entry in inventoryData.instances)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.instanceId)) continue;
                    instances.Add(new ItemInstance(
                        entry.instanceId,
                        entry.itemId,
                        new ItemStats(entry.rolledAttack, entry.rolledDefense, entry.rolledMaxHealth),
                        entry.upgradeLevel));
                }
            }
            _inventory.LoadFrom(stacks, instances);

            var slots = new List<KeyValuePair<EquipmentSlot, string>>();
            if (equipmentData != null)
            {
                if (!string.IsNullOrEmpty(equipmentData.weapon)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Weapon, equipmentData.weapon));
                if (!string.IsNullOrEmpty(equipmentData.armor)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Armor, equipmentData.armor));
            }
            _equipment.LoadFrom(slots);
            ApplyEquipmentStats();
        }

        public InventoryData ToInventoryData()
        {
            var data = new InventoryData();
            foreach (ItemStack stack in _inventory.Stacks)
            {
                data.stacks.Add(new StackEntry { itemId = stack.itemId, quantity = stack.quantity });
            }
            foreach (ItemInstance instance in _inventory.Instances)
            {
                data.instances.Add(new InstanceEntry
                {
                    instanceId = instance.instanceId,
                    itemId = instance.itemId,
                    upgradeLevel = instance.upgradeLevel,
                    rolledAttack = instance.rolledStats.attack,
                    rolledDefense = instance.rolledStats.defense,
                    rolledMaxHealth = instance.rolledStats.maxHealth
                });
            }
            return data;
        }

        public EquipmentData ToEquipmentData()
        {
            return new EquipmentData
            {
                weapon = _equipment.GetInstanceId(EquipmentSlot.Weapon),
                armor = _equipment.GetInstanceId(EquipmentSlot.Armor)
            };
        }
    }
}
