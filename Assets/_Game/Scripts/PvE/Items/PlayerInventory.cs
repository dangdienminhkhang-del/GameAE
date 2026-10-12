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

        // ── Dùng vật phẩm tiêu hao (docs/07 §2) ──
        private readonly ConsumableUseClock _useClock = new ConsumableUseClock();
        private HealthComponent _health;

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
        /// <summary>Mốc thời gian giao chiến / lần dùng gần nhất (UI không cần đụng, test dùng để giả lập thời gian).</summary>
        public ConsumableUseClock UseClock => _useClock;
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
            GameEvents.OnHealthChanged += HandleHealthChangedForCombat;
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
            GameEvents.OnHitResolved += HandleHitResolved;
        }

        private void OnDisable()
        {
            _inventory.Changed -= HandleChanged;
            _equipment.Changed -= HandleChanged;
            GameEvents.OnExperienceChanged -= HandleExperienceChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
            GameEvents.OnHealthChanged -= HandleHealthChangedForCombat;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
            GameEvents.OnHitResolved -= HandleHitResolved;
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
            try
            {
                ApplyEquipmentStats();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }

            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }

            RequestSave();
        }

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

        /// <summary>Hạ được quái → tính là vừa giao chiến (docs/07 §2): chặn dùng bình trong vài giây.</summary>
        private void HandleEnemyDefeated(object sender, EventArgs e)
        {
            _useClock.MarkCombat(UnityEngine.Time.time);
        }

        /// <summary>
        /// Player đánh trúng quái (chưa chết cũng tính) → đang giao chiến, chặn dùng bình trong vài giây.
        /// Chỉ tính hit mà nguồn là chỉ số của CHÍNH player này.
        /// </summary>
        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Source == null) return;
            CharacterStats own = ResolveStats();
            if (own == null || !ReferenceEquals(args.Source, own)) return;

            _useClock.MarkCombat(UnityEngine.Time.time);
        }

        private void HandleHealthChangedForCombat(object sender, HealthChangedArgs args)
        {
            // Chỉ nghe máu của CHÍNH player: máu của quái đổi không được đánh dấu giao chiến và không kích hoạt lưu.
            HealthComponent own = ResolveHealth();
            if (own == null || !ReferenceEquals(sender, own)) return;

            if (args != null && args.Delta < 0.0)
                _useClock.MarkCombat(UnityEngine.Time.time);
            // HP thay đổi → lưu lại để khi vào lại game HP đúng
            RequestSave();
        }

        // ── Tự động lưu (docs/03 §12) ───────────────────────────────

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

        // ── Dùng vật phẩm tiêu hao ──────────────────────────────────

        /// <summary>Hỏi trạng thái có dùng được không mà không thực sự dùng.</summary>
        public ConsumableUseResult GetConsumableUseState(string itemId)
        {
            ItemDefinition definition = database != null ? database.Find(itemId) : null;
            HealthComponent health = ResolveHealth();
            float now = UnityEngine.Time.time;

            return ConsumableRules.CanUse(
                definition,
                health != null ? health.CurrentHealth : 0.0,
                health != null ? health.MaxHealth : 0.0,
                _useClock.SecondsSinceCombat(now),
                _useClock.SecondsSinceUse(itemId, now),
                _inventory.GetStack(itemId));
        }

        /// <summary>
        /// Dùng 1 vật phẩm tiêu hao: kiểm luật → ghi mốc dùng → hồi máu → trừ 1 trong túi.
        /// </summary>
        public ConsumableUseResult TryUseConsumable(string itemId)
        {
            ConsumableUseResult state = GetConsumableUseState(itemId);
            if (state != ConsumableUseResult.Ok) return state;

            ItemDefinition definition = database.Find(itemId);
            HealthComponent health = ResolveHealth();
            double heal = ConsumableRules.ComputeHeal(definition, health.CurrentHealth, health.MaxHealth);

            _useClock.MarkUsed(itemId, UnityEngine.Time.time);
            health.Heal(heal);
            if (!_inventory.RemoveStack(itemId, 1))
            {
                Debug.LogError($"[Inventory] Dùng '{itemId}' nhưng không trừ được 1 trong túi.", this);
            }
            return ConsumableUseResult.Ok;
        }

        // ── Trang bị ──────────────────────────────────────────────

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

        private HealthComponent ResolveHealth()
        {
            if (_health == null) TryGetComponent(out _health);
            return _health;
        }

        public ItemStats CurrentEquipmentBonus()
        {
            return database != null ? database.SumEquipment(_equipment, _inventory) : default;
        }

        // ── Save / Load ───────────────────────────────────────────

        public async Task SaveAsync()
        {
            if (_save == null || _saveLocked) return;

            InventoryData inventoryData = ToInventoryData();
            EquipmentData equipmentData = ToEquipmentData();

            // Lưu HP hiện tại vào ProfileData để restore khi vào lại game
            HealthComponent health = ResolveHealth();
            ProfileData profileData = playerProgress != null
                ? new ProfileData
                {
                    level     = playerProgress.Level,
                    exp       = playerProgress.CurrentExp,
                    currentHp = health != null ? health.CurrentHealth : 0.0
                }
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
                if (profile != null && playerProgress != null)
                    playerProgress.SetProgress(profile.level, profile.exp);

                var inventoryData = await _save.LoadAsync<InventoryData>(SaveKeys.Inventory);
                var equipmentData = await _save.LoadAsync<EquipmentData>(SaveKeys.Equipment);
                ApplyLoadedData(inventoryData, equipmentData);

                // Restore HP sau khi equipment stats đã áp dụng (MaxHealth đã đúng)
                // currentHp == 0 → save cũ chưa có field → dùng MaxHealth mặc định
                if (profile != null && profile.currentHp > 0.0)
                {
                    HealthComponent health = ResolveHealth();
                    if (health != null)
                        health.SetCurrentHealth(profile.currentHp);
                }

                _loaded = true;
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
                    instanceId      = instance.instanceId,
                    itemId          = instance.itemId,
                    upgradeLevel    = instance.upgradeLevel,
                    rolledAttack    = instance.rolledStats.attack,
                    rolledDefense   = instance.rolledStats.defense,
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
                armor  = _equipment.GetInstanceId(EquipmentSlot.Armor)
            };
        }
    }
}
