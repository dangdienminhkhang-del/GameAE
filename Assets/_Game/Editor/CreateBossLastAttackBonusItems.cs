using UnityEditor;
using UnityEngine;
using Game.Core.Items;
using Game.PvE.Boss;
using System.Collections.Generic;

namespace Game.Editor
{
    public static class CreateBossLastAttackBonusItems
    {
        [MenuItem("Game/Combat/Create Last Attack Bonus Item")]
        public static void CreateItems()
        {
            string physicalPath = System.IO.Path.Combine(Application.dataPath, "_Game/Data/Items/BossReward");
            if (!System.IO.Directory.Exists(physicalPath))
            {
                System.IO.Directory.CreateDirectory(physicalPath);
                AssetDatabase.Refresh();
            }

            string coatPath = "Assets/_Game/Data/Items/BossReward/Item_CoatOfMidnight.asset";
            var coat = AssetDatabase.LoadAssetAtPath<ItemDefinition>(coatPath);
            if (coat == null)
            {
                coat = ScriptableObject.CreateInstance<ItemDefinition>();
                SetField(coat, "id", "item.coat_of_midnight");
                SetField(coat, "displayName", "Áo Choàng Bóng Đêm");
                SetField(coat, "type", ItemType.Armor);
                SetField(coat, "tier", 2);
                SetField(coat, "equipmentSlot", EquipmentSlot.Armor);
                SetField(coat, "sellValue", 200);

                var baseStats = new ItemStats(attack: 3, defense: 8, maxHealth: 30);
                SetField(coat, "baseStats", baseStats);

                var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/Item_HarborCoat.png");
                if (icon != null) SetField(coat, "icon", icon);

                AssetDatabase.CreateAsset(coat, coatPath);
            }

            // Gán vào Boss_Floor01.asset
            var boss01 = AssetDatabase.LoadAssetAtPath<BossDefinition>("Assets/_Game/Data/Enemies/Boss_Floor01.asset");
            if (boss01 != null)
            {
                SetField(boss01, "lastAttackRewardItem", coat);
                SetField(boss01, "lastAttackRewardQuantity", 1);
                EditorUtility.SetDirty(boss01);
            }

            // 2. Đăng ký vào ItemDatabase.asset
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Game/Data/Items/ItemDatabase.asset");
            if (db != null)
            {
                var listField = typeof(ItemDatabase).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (listField != null)
                {
                    var list = (List<ItemDefinition>)listField.GetValue(db);
                    if (!list.Contains(coat))
                    {
                        list.Add(coat);
                        EditorUtility.SetDirty(db);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateBossLastAttackBonusItems] Đã tạo Item_CoatOfMidnight và gán LA cho Boss Floor 1!");
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
