using Game.Core.Items;
using UnityEngine;

namespace Game.PvE.Items
{
    /// <summary>Vật phẩm nằm trong scene: player chạm vào là nhặt (docs/Phase 5).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int quantity = 1;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool destroyOnPickup = true;

        private bool _taken;

        public ItemDefinition Item => item;
        public int Quantity => quantity;

        private void Awake()
        {
            if (spriteRenderer == null) TryGetComponent(out spriteRenderer);
            ApplyVisual();
        }

        /// <summary>Dùng cho đồ rơi lúc runtime (LootDropper).</summary>
        public void SetItem(ItemDefinition definition, int count)
        {
            item = definition;
            quantity = Mathf.Max(1, count);
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer != null && item != null && item.Icon != null) spriteRenderer.sprite = item.Icon;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken || other == null) return;

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            // Chỉ nhặt được một phần (kho đầy) → giữ phần còn lại trên sàn, KHÔNG làm mất đồ.
            int accepted = inventory.TryAddItem(item, quantity);
            if (accepted <= 0) return;

            quantity -= accepted;
            if (quantity > 0) return;

            _taken = true;
            if (destroyOnPickup) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
