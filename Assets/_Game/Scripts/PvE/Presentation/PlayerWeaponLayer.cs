using System;
using System.Collections.Generic;
using Game.Core.Items;
using Game.PvE.Items;
using UnityEngine;

namespace Game.PvE.Presentation
{
    /// <summary>
    /// Vẽ vũ khí đang trang bị lên người player. Thân (body) không cầm vũ khí; mỗi vũ khí có 1 atlas overlay
    /// với sprite trùng tên sprite của thân. Mỗi LateUpdate copy tên sprite + flipX của thân sang overlay.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PlayerWeaponLayer : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string itemId;
            public Sprite[] sprites;
        }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private List<Entry> entries = new List<Entry>();
        [Tooltip("Vũ khí hiển thị khi chưa trang bị gì (để trống = tay không).")]
        [SerializeField] private string fallbackItemId = "";
        [SerializeField] private int orderOffset = 1;

        private SpriteRenderer _layer;
        private readonly Dictionary<string, Dictionary<string, Sprite>> _maps = new Dictionary<string, Dictionary<string, Sprite>>();
        private Dictionary<string, Sprite> _current;

        private void Awake()
        {
            if (body == null) TryGetComponent(out body);
            if (inventory == null) TryGetComponent(out inventory);

            var go = new GameObject("WeaponLayer");
            go.transform.SetParent(transform, false);
            _layer = go.AddComponent<SpriteRenderer>();

            foreach (Entry e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.itemId) || e.sprites == null) continue;
                var map = new Dictionary<string, Sprite>(e.sprites.Length);
                foreach (Sprite s in e.sprites) if (s != null) map[s.name] = s;
                _maps[e.itemId] = map;
            }
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        private void Refresh()
        {
            string itemId = fallbackItemId;
            if (inventory != null && inventory.Database != null)
            {
                string instanceId = inventory.Equipment.GetInstanceId(EquipmentSlot.Weapon);
                ItemInstance instance = string.IsNullOrEmpty(instanceId) ? null : inventory.Inventory.FindInstance(instanceId);
                if (instance != null) itemId = instance.itemId;
            }

            _current = !string.IsNullOrEmpty(itemId) && _maps.TryGetValue(itemId, out var map) ? map : null;
        }

        private void LateUpdate()
        {
            if (_layer == null || body == null) return;

            Sprite bodySprite = body.sprite;
            Sprite overlay = null;
            if (_current != null && bodySprite != null) _current.TryGetValue(bodySprite.name, out overlay);

            _layer.sprite = overlay;
            _layer.enabled = overlay != null && body.enabled;
            _layer.flipX = body.flipX;
            _layer.color = body.color;
            _layer.sharedMaterial = body.sharedMaterial;
            _layer.sortingLayerID = body.sortingLayerID;
            _layer.sortingOrder = body.sortingOrder + orderOffset;
        }
    }
}
