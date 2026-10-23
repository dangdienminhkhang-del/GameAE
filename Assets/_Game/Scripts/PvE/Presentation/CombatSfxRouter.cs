using System;
using Game.Core.Audio;
using Game.Core.Events;
using Game.Core.Skills;
using Game.PvE.Boss;
using Game.PvE.Dodge;
using Game.PvE.Enemy.Projectile;
using Game.PvE.Items;
using Game.PvE.Player;
using Game.PvE.Skills;
using Game.Services;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Nghe sự kiện chiến đấu và phát SFX qua IAudioService (docs/05 Phase 9.5). Chỉ NGHE, không tự trừ máu hay đổi trạng thái.
    /// Gắn MỘT lần trong scene gameplay; các tham chiếu để trống sẽ tự tìm lúc Start.
    /// </summary>
    public sealed class CombatSfxRouter : MonoBehaviour
    {
        [SerializeField] private PlayerAttack playerAttack;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private DodgeController dodgeController;
        [Tooltip("Máu người chơi — chỉ OnHealthChanged phát từ component này mới tính là 'người chơi bị trúng'.")]
        [SerializeField] private HealthComponent playerHealth;
        [Tooltip("Túi đồ — để phát SFX khi dùng bình thành công (nút Bình và bảng túi đều tính). Để trống = tự tìm lúc Start.")]
        [SerializeField] private PlayerInventory playerInventory;

        private bool _boundInstanceEvents;

        private static IAudioService Sound => ServiceProvider.GetAudioService();

        private void OnEnable()
        {
            GameEvents.OnHitResolved += HandleHitResolved;
            GameEvents.OnHealthChanged += HandleHealthChanged;
            GameEvents.OnEnemyDefeated += HandleEnemyDefeated;
            GameEvents.OnItemAcquiredWithPayload += HandleItemAcquired;
            GameEvents.OnLevelUp += HandleLevelUp;
            BossController.TelegraphStarted += HandleBossTelegraph;
            EnemyProjectilePool.Fired += HandleEnemyShoot;
            BindInstanceEvents();
        }

        private void Start()
        {
            // Tự tìm phần còn thiếu một lần (scene đang dựng dần), rồi gắn lại.
            if (playerAttack == null) playerAttack = FindFirstObjectByType<PlayerAttack>();
            if (techniqueController == null) techniqueController = FindFirstObjectByType<TechniqueController>();
            if (dodgeController == null) dodgeController = FindFirstObjectByType<DodgeController>();
            if (playerHealth == null) playerHealth = FindFirstObjectByType<HealthComponent>();
            if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();

            UnbindInstanceEvents();
            BindInstanceEvents();
        }

        private void OnDisable()
        {
            GameEvents.OnHitResolved -= HandleHitResolved;
            GameEvents.OnHealthChanged -= HandleHealthChanged;
            GameEvents.OnEnemyDefeated -= HandleEnemyDefeated;
            GameEvents.OnItemAcquiredWithPayload -= HandleItemAcquired;
            GameEvents.OnLevelUp -= HandleLevelUp;
            BossController.TelegraphStarted -= HandleBossTelegraph;
            EnemyProjectilePool.Fired -= HandleEnemyShoot;
            UnbindInstanceEvents();
        }

        private void BindInstanceEvents()
        {
            if (_boundInstanceEvents) return;
            _boundInstanceEvents = true;

            if (playerAttack != null) playerAttack.AttackPerformed += HandleAttackPerformed;
            if (techniqueController != null) techniqueController.OnTechniqueStarted += HandleTechniqueStarted;
            if (dodgeController != null) dodgeController.OnDodgeStarted += HandleDodgeStarted;
            if (playerInventory != null) playerInventory.ConsumableUsed += HandleConsumableUsed;
        }

        private void UnbindInstanceEvents()
        {
            if (!_boundInstanceEvents) return;
            _boundInstanceEvents = false;

            if (playerAttack != null) playerAttack.AttackPerformed -= HandleAttackPerformed;
            if (techniqueController != null) techniqueController.OnTechniqueStarted -= HandleTechniqueStarted;
            if (dodgeController != null) dodgeController.OnDodgeStarted -= HandleDodgeStarted;
            if (playerInventory != null) playerInventory.ConsumableUsed -= HandleConsumableUsed;
        }

        private void HandleAttackPerformed(object sender, EventArgs e)
        {
            if (playerAttack != null && playerAttack.CurrentComboStep == 2)
            {
                Sound.PlaySfx(AudioIds.SfxTechnique);
            }
            else
            {
                Sound.PlaySfx(AudioIds.SfxPlayerSwing);
            }
        }

        private void HandleHitResolved(object sender, HitResolvedArgs args)
        {
            if (args == null || args.Target == null) return;
            Sound.PlaySfx(AudioIds.SfxHitEnemy);
        }

        private void HandleHealthChanged(object sender, HealthChangedArgs args)
        {
            if (args == null || args.Delta >= 0.0) return;
            if (playerHealth == null || !ReferenceEquals(sender, playerHealth)) return;
            Sound.PlaySfx(AudioIds.SfxHitPlayer);
        }

        // Quái thường và boss đều phát OnEnemyDefeated.
        private void HandleEnemyDefeated(object sender, EventArgs e) => Sound.PlaySfx(AudioIds.SfxEnemyDie);

        private void HandleTechniqueStarted(object sender, TechniqueInputSlot slot) => Sound.PlaySfx(AudioIds.SfxTechnique);

        private void HandleDodgeStarted(object sender, EventArgs e) => Sound.PlaySfx(AudioIds.SfxDodge);

        private void HandleItemAcquired(object sender, ItemAcquiredArgs args)
        {
            if (args == null || args.Quantity <= 0) return;
            Sound.PlaySfx(AudioIds.SfxPickup);
        }

        private void HandleLevelUp(object sender, int newLevel) => Sound.PlaySfx(AudioIds.SfxLevelUp);

        private void HandleConsumableUsed() => Sound.PlaySfx(AudioIds.SfxPotion);

        private void HandleBossTelegraph(BossController boss) => Sound.PlaySfx(AudioIds.SfxBossTelegraph);

        private void HandleEnemyShoot() => Sound.PlaySfx(AudioIds.SfxEnemyShoot);
    }
}
