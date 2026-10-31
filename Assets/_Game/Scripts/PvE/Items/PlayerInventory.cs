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
    /// <summary>Kết quả sửa trang bị ở thợ rèn.</summary>
    public enum RepairResult { Ok, NotFound, NoDurability, AlreadyFull, NotEnoughCol }

    /// <summary>Kết quả cường hóa trang bị ở thợ rèn.</summary>
    public enum EnhanceResult { Success, Failed, FailedDowngraded, NotFound, NotUpgradable, MaxLevel, Broken, NotEnoughCol }

    /// <summary>
    /// Kho + trang bị của player: nhặt đồ, mặc/tháo, cộng chỉ số, lưu/tải qua ISaveService.
    /// Là nơi DUY NHẤT gọi save cho profile/inventory/equipment (docs/03 §5).
    /// Cũng giữ độ bền, sửa chữa và cường hóa trang bị (luật ở Core/Items/DurabilityRules, UpgradeRules).
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private ItemDatabase database;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private bool loadOnStart = true;
        [Tooltip("Tự động lưu sau N giây kể từ thay đổi ĐẦU TIÊN chưa lưu (gom các thay đổi trong N giây thành 1 lần ghi; thay đổi liên tục như hồi máu không làm lùi lịch lưu).")]
        [SerializeField] private float saveDelaySeconds = 2f;

        private const float SaveRetrySeconds = 5f;

        private readonly Inventory _inventory = new Inventory();
        private readonly Equipment _equipment = new Equipment();
        private ISaveService _save;
        private CharacterStats _stats;
        private PlayerWallet _wallet;
        private PlayerStatAllocation _allocation;
        private LifeSkills.PlayerLifeSkills _lifeSkills;
        private bool _allocationSubscribed;
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

        /// <summary>Số giây còn lại của khóa giao chiến (0 = đã hết). Chỉ đọc, dùng cùng mốc với luật dùng bình (UI Pause/Bình dùng).</summary>
        public float CombatLockRemainingSeconds
        {
            get
            {
                float since = _useClock.SecondsSinceCombat(UnityEngine.Time.time);
                float remaining = ConsumableRules.DefaultCombatLockSeconds - since;
                return remaining > 0f ? remaining : 0f;
            }
        }

        /// <summary>Đang giao chiến (vừa đánh/bị đánh chưa đủ số giây khóa). Không đổi luật, chỉ đọc.</summary>
        public bool IsInCombat => CombatLockRemainingSeconds > 0f;

        public event Action Changed;

        /// <summary>Vừa dùng thành công một vật phẩm tiêu hao (đã hồi máu và trừ 1 trong túi). Nút Bình và bảng túi đều đi qua đây nên nghe ở đây là phủ cả hai (SFX bình).</summary>
        public event Action ConsumableUsed;

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
            if (_wallet == null) TryGetComponent(out _wallet);
            if (_wallet != null) _wallet.Changed += RequestSave;
            if (_lifeSkills == null) TryGetComponent(out _lifeSkills);
            if (_lifeSkills != null) _lifeSkills.Changed += RequestSave;
            SubscribeAllocation();
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
            if (_wallet != null) _wallet.Changed -= RequestSave;
            if (_lifeSkills != null) _lifeSkills.Changed -= RequestSave;
            UnsubscribeAllocation();
            GameEvents.OnExperienceChanged -= HandleExperienceChanged;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameEvents.OnPlayerDeathRecorded -= HandleDeathRecorded;
            GameEvents.OnHealthChanged -= HandleHealthChangedForCombat;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
            GameEvents.OnHitResolved -= HandleHitResolved;
        }

        /// <summary>Điểm STR/AGI đổi → cần lưu. Component do PlayerController gắn, có thể chưa tồn tại lúc OnEnable nên gọi lại ở Start.</summary>
        private void SubscribeAllocation()
        {
            if (_allocation == null) TryGetComponent(out _allocation);
            if (_allocation == null || _allocationSubscribed) return;
            _allocation.Changed += RequestSave;
            _allocationSubscribed = true;
        }

        private void UnsubscribeAllocation()
        {
            if (_allocation != null && _allocationSubscribed) _allocation.Changed -= RequestSave;
            _allocationSubscribed = false;
        }

        private async void Start()
        {
            SubscribeAllocation();

            if (!loadOnStart)
            {
                _loaded = true;
                UpdateDualWieldState();
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
        /// Chỉ tính hit mà nguồn là chỉ số của CHÍNH player này. Mỗi hit cũng làm vũ khí đang cầm hao 1 độ bền.
        /// </summary>
        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Source == null) return;
            CharacterStats own = ResolveStats();
            if (own == null || !ReferenceEquals(args.Source, own)) return;

            _useClock.MarkCombat(UnityEngine.Time.time);
            WearSlot(EquipmentSlot.Weapon);
            WearSlot(EquipmentSlot.OffHand);
        }

        private void HandleHealthChangedForCombat(object sender, HealthChangedArgs args)
        {
            // Chỉ nghe máu của CHÍNH player: máu của quái đổi không được đánh dấu giao chiến và không kích hoạt lưu.
            HealthComponent own = ResolveHealth();
            if (own == null || !ReferenceEquals(sender, own)) return;

            if (args != null && args.Delta < 0.0)
            {
                _useClock.MarkCombat(UnityEngine.Time.time);
                WearSlot(EquipmentSlot.Armor); // bị đánh → giáp hao 1 độ bền
            }
            // HP thay đổi → lưu lại để khi vào lại game HP đúng
            RequestSave();
        }

        // ── Tự động lưu (docs/03 §12) ───────────────────────────────

        /// <summary>
        /// Đánh dấu cần lưu. Lịch lưu chỉ được đặt ở thay đổi ĐẦU TIÊN chưa lưu (không lùi lịch ở các thay đổi sau):
        /// các thay đổi nhỏ liên tục (hồi máu tự nhiên mỗi giây) vẫn được lưu đều sau mỗi saveDelaySeconds
        /// thay vì bị hoãn đến khi ngừng thay đổi. Dữ liệu ghi là trạng thái TẠI THỜI ĐIỂM lưu nên các thay đổi
        /// xảy ra trong khoảng chờ vẫn được gom vào cùng một lần ghi.
        /// </summary>
        public void RequestSave()
        {
            if (!_loaded || _loading || _saveLocked) return;
            if (!_dirty) _saveDueAt = UnityEngine.Time.unscaledTime + saveDelaySeconds;
            _dirty = true;
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
            ConsumableUsed?.Invoke();
            return ConsumableUseResult.Ok;
        }

        // ── Trang bị ──────────────────────────────────────────────

        public bool Equip(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            if (instance == null) return false;

            ItemDefinition definition = database != null ? database.Find(instance.itemId) : null;
            if (definition == null || definition.Slot == EquipmentSlot.None) return false;

            // Yêu cầu STR (SAO: đồ nặng cần đủ STR). Món đang mặc rồi thì không chặn.
            if (_allocation == null) TryGetComponent(out _allocation);
            int strength = _allocation != null ? _allocation.Strength : 0;
            if (!_equipment.IsEquipped(instanceId) && !EquipRequirementRules.MeetsStrength(definition.RequiredStrength, strength))
            {
                GameEvents.RaiseCombatNotice(EquipRequirementRules.MissingMessage(definition.RequiredStrength, strength), new Color(1f, 0.55f, 0.35f));
                return false;
            }

            // Phụ kiện có 2 ô: EquipmentSlotRules chọn ô trống đầu tiên (xem luật để biết trường hợp đầy ô).
            EquipmentSlot target = EquipmentSlotRules.ResolveTarget(definition.Slot, _equipment, instanceId);
            if (target == EquipmentSlot.None) return false;

            _equipment.Equip(target, instanceId);
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

        private PlayerAttack _playerAttack;

        private PlayerAttack ResolveAttack()
        {
            if (_playerAttack == null) TryGetComponent(out _playerAttack);
            return _playerAttack;
        }

        /// <summary>
        /// Đồng bộ trạng thái Kỹ năng Độc Nhất Nhị Đao Lưu (Dual Wielding theo SAO) dựa trên trang bị cả 2 tay.
        /// </summary>
        public void UpdateDualWieldState()
        {
            bool hasMain = !string.IsNullOrEmpty(_equipment.GetInstanceId(EquipmentSlot.Weapon));
            bool hasOff = !string.IsNullOrEmpty(_equipment.GetInstanceId(EquipmentSlot.OffHand));
            bool isDual = DualWieldRules.IsDualWielding(hasMain, hasOff);

            PlayerAttack attack = ResolveAttack();
            if (attack != null)
            {
                bool previous = attack.IsDualWielding;
                attack.IsDualWielding = isDual;
                if (isDual != previous)
                {
                    if (isDual)
                    {
                        GameEvents.RaiseCombatNotice("⚔ NHỊ ĐAO LƯU (DUAL WIELDING)! ⚔", new Color(0.35f, 0.9f, 1f));
                    }
                    else if (previous)
                    {
                        GameEvents.RaiseCombatNotice("Hủy Nhị Đao Lưu", Color.gray);
                    }
                }
            }
        }

        public void ApplyEquipmentStats()
        {
            CharacterStats stats = ResolveStats();
            if (stats == null || database == null) return;

            ItemStats bonus = database.SumEquipment(_equipment, _inventory);
            stats.SetEquipmentBonus(bonus.attack, bonus.defense, bonus.maxHealth);
            GameEvents.RaiseStatsChanged(this);

            UpdateDualWieldState();
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

        // ── Độ bền, sửa chữa, cường hóa (thợ rèn) ─────────────────

        private ItemDefinition DefinitionOf(ItemInstance instance)
        {
            return instance != null && database != null ? database.Find(instance.itemId) : null;
        }

        /// <summary>Hao 1 độ bền món đang mặc ở ô này. Cảnh báo khi sắp hỏng; hỏng thì tính lại chỉ số.</summary>
        private void WearSlot(EquipmentSlot slot)
        {
            string instanceId = _equipment.GetInstanceId(slot);
            if (string.IsNullOrEmpty(instanceId)) return;

            ItemInstance instance = _inventory.FindInstance(instanceId);
            ItemDefinition definition = DefinitionOf(instance);
            if (definition == null || !DurabilityRules.Wears(definition.MaxDurability)) return;
            if (DurabilityRules.IsBroken(instance.durability, definition.MaxDurability)) return;

            bool wasLow = DurabilityRules.IsLow(instance.durability, definition.MaxDurability);
            instance.durability = DurabilityRules.ApplyWear(instance.durability, definition.MaxDurability, DurabilityRules.WearPerHit);
            RequestSave();

            if (DurabilityRules.IsBroken(instance.durability, definition.MaxDurability))
            {
                GameEvents.RaiseCombatNotice($"{definition.DisplayName} đã HỎNG! Hãy tới thợ rèn.", new Color(1f, 0.3f, 0.3f));
                HandleChanged(); // hỏng → không còn cộng chỉ số, tính lại và làm mới UI
            }
            else if (!wasLow && DurabilityRules.IsLow(instance.durability, definition.MaxDurability))
            {
                GameEvents.RaiseCombatNotice($"{definition.DisplayName} sắp hỏng!", new Color(1f, 0.65f, 0.25f));
            }
        }

        /// <summary>Phí sửa đầy món này (Col). 0 nếu không cần sửa / không hao mòn / không tìm thấy.</summary>
        public int GetRepairCost(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            ItemDefinition definition = DefinitionOf(instance);
            if (definition == null) return 0;
            return DurabilityRules.RepairCost(instance.durability, definition.MaxDurability, definition.SellValue, instance.upgradeLevel);
        }

        /// <summary>Sửa đầy độ bền, trừ Col theo phần đang thiếu.</summary>
        public RepairResult TryRepair(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            ItemDefinition definition = DefinitionOf(instance);
            if (definition == null) return RepairResult.NotFound;
            if (!DurabilityRules.Wears(definition.MaxDurability)) return RepairResult.NoDurability;
            if (DurabilityRules.Missing(instance.durability, definition.MaxDurability) <= 0) return RepairResult.AlreadyFull;

            int cost = GetRepairCost(instanceId);
            if (_wallet == null || !_wallet.TrySpend(cost)) return RepairResult.NotEnoughCol;

            instance.durability = definition.MaxDurability;
            HandleChanged();
            return RepairResult.Ok;
        }

        /// <summary>Phí một lần cường hóa (Col). 0 nếu không cường hóa được.</summary>
        public int GetEnhanceCost(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            ItemDefinition definition = DefinitionOf(instance);
            if (definition == null || definition.Slot == EquipmentSlot.None) return 0;
            return UpgradeRules.Cost(definition.Tier, instance.upgradeLevel);
        }

        /// <summary>Tỉ lệ thành công của lần cường hóa kế (0..1).</summary>
        public double GetEnhanceChance(string instanceId)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            return instance != null ? UpgradeRules.SuccessChance(instance.upgradeLevel) : 0.0;
        }

        public EnhanceResult TryEnhance(string instanceId)
        {
            return TryEnhance(instanceId, UnityEngine.Random.value);
        }

        /// <summary>Cường hóa một lần. roll01 truyền vào để test được; trừ Col dù thành công hay thất bại.</summary>
        public EnhanceResult TryEnhance(string instanceId, double roll01)
        {
            ItemInstance instance = _inventory.FindInstance(instanceId);
            ItemDefinition definition = DefinitionOf(instance);
            if (definition == null) return EnhanceResult.NotFound;
            if (definition.Slot == EquipmentSlot.None) return EnhanceResult.NotUpgradable;
            if (!UpgradeRules.CanUpgrade(instance.upgradeLevel)) return EnhanceResult.MaxLevel;
            if (DurabilityRules.IsBroken(instance.durability, definition.MaxDurability)) return EnhanceResult.Broken;

            int cost = UpgradeRules.Cost(definition.Tier, instance.upgradeLevel);
            if (_wallet == null || !_wallet.TrySpend(cost)) return EnhanceResult.NotEnoughCol;

            UpgradeOutcome outcome = UpgradeRules.Resolve(instance.upgradeLevel, roll01, out int newLevel);
            instance.upgradeLevel = newLevel;
            HandleChanged();

            switch (outcome)
            {
                case UpgradeOutcome.Success: return EnhanceResult.Success;
                case UpgradeOutcome.FailedDowngraded: return EnhanceResult.FailedDowngraded;
                default: return EnhanceResult.Failed;
            }
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
                    currentHp = health != null ? health.CurrentHealth : 0.0,
                    col       = _wallet != null ? _wallet.Col : -1,
                    strPoints = _allocation != null ? _allocation.Strength : 0,
                    agiPoints = _allocation != null ? _allocation.Agility : 0,
                    fishingProficiency = _lifeSkills != null ? _lifeSkills.FishingProficiency : 0,
                    cookingProficiency = _lifeSkills != null ? _lifeSkills.CookingProficiency : 0
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

                if (_wallet != null) _wallet.LoadFromSave(profile != null ? profile.col : -1);

                // Điểm STR/AGI: nạp SAU khi level đã được đặt (để cắt theo tổng điểm của level). Save cũ = 0/0.
                SubscribeAllocation();
                if (_allocation != null && profile != null)
                    _allocation.LoadFromSave(profile.strPoints, profile.agiPoints);

                if (_lifeSkills == null) TryGetComponent(out _lifeSkills);
                if (_lifeSkills != null && profile != null)
                    _lifeSkills.LoadFromSave(profile.fishingProficiency, profile.cookingProficiency);

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
                    var loaded = new ItemInstance(
                        entry.instanceId,
                        entry.itemId,
                        new ItemStats(entry.rolledAttack, entry.rolledDefense, entry.rolledMaxHealth),
                        entry.upgradeLevel);
                    loaded.durability = entry.durability; // -1 (save cũ) = đầy
                    instances.Add(loaded);
                }
            }
            _inventory.LoadFrom(stacks, instances);

            var slots = new List<KeyValuePair<EquipmentSlot, string>>();
            if (equipmentData != null)
            {
                if (!string.IsNullOrEmpty(equipmentData.weapon)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Weapon, equipmentData.weapon));
                if (!string.IsNullOrEmpty(equipmentData.armor)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Armor, equipmentData.armor));
                if (!string.IsNullOrEmpty(equipmentData.offHand)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.OffHand, equipmentData.offHand));
                if (!string.IsNullOrEmpty(equipmentData.accessory)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Accessory, equipmentData.accessory));
                if (!string.IsNullOrEmpty(equipmentData.accessory2)) slots.Add(new KeyValuePair<EquipmentSlot, string>(EquipmentSlot.Accessory2, equipmentData.accessory2));
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
                    rolledMaxHealth = instance.rolledStats.maxHealth,
                    durability      = instance.durability
                });
            }
            return data;
        }

        public EquipmentData ToEquipmentData()
        {
            return new EquipmentData
            {
                weapon     = _equipment.GetInstanceId(EquipmentSlot.Weapon),
                armor      = _equipment.GetInstanceId(EquipmentSlot.Armor),
                offHand    = _equipment.GetInstanceId(EquipmentSlot.OffHand),
                accessory  = _equipment.GetInstanceId(EquipmentSlot.Accessory),
                accessory2 = _equipment.GetInstanceId(EquipmentSlot.Accessory2)
            };
        }
    }
}
