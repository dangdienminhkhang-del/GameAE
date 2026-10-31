using System;
using System.Collections.Generic;

namespace Game.Core.LifeSkills
{
    /// <summary>
    /// Bản ghi công thức món ăn trong Sword Art Online (docs/16 & prompt).
    /// Asuna là đầu bếp huyền thoại với Rank Max Cooking.
    /// </summary>
    [Serializable]
    public class CookingRecipe
    {
        public string id;
        public string dishName;
        public string resultItemId;
        public int requiredCookingLevel = 1;
        public string requiredIngredientId;
        public int requiredIngredientCount = 1;
        public double instantHeal = 50.0;
        public double attackBuff = 5.0;
        public float buffDurationSeconds = 180f; // 3 phút
    }

    /// <summary>
    /// Hiệu ứng buff tạm thời nhận được sau khi ăn món ăn nấu chín.
    /// </summary>
    [Serializable]
    public class CookingBuffInstance
    {
        public string dishName;
        public double attackBonus;
        public float durationSeconds;
        public float remainingSeconds;

        public bool IsExpired => remainingSeconds <= 0f;

        public CookingBuffInstance(string dishName, double attackBonus, float durationSeconds)
        {
            this.dishName = dishName;
            this.attackBonus = attackBonus;
            this.durationSeconds = durationSeconds;
            this.remainingSeconds = durationSeconds;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || remainingSeconds <= 0f) return;
            remainingSeconds -= deltaSeconds;
            if (remainingSeconds < 0f) remainingSeconds = 0f;
        }
    }

    /// <summary>
    /// Luật cho kỹ năng Đời Sống: Câu Cá (Fishing) & Nấu Ăn (Cooking) theo SAO.
    /// Logic C# thuần, dễ kiểm thử.
    /// </summary>
    public static class LifeSkillRules
    {
        public const string FishItemId = "item.fresh_fish";
        public const string GrilledFishItemId = "item.grilled_fish";
        public const string SandwichItemId = "item.rabbit_sandwich";

        public const int ExpPerFishingSuccess = 10;
        public const int ExpPerCookingSuccess = 15;

        public const float BaseReactionWindowSeconds = 1.2f;
        public const float MaxReactionWindowSeconds = 2.5f;

        /// <summary>
        /// Tính cấp độ kỹ năng đời sống dựa trên điểm thông thạo (Proficiency).
        /// Cứ 100 điểm = +1 cấp độ (Cấp 1 từ 0-99 exp).
        /// </summary>
        public static int GetSkillLevel(int proficiency)
        {
            if (proficiency <= 0) return 1;
            return (proficiency / 100) + 1;
        }

        /// <summary>
        /// Tính cửa sổ phản xạ (timing window) khi cá cắn câu.
        /// Kỹ năng câu càng cao thì cửa sổ bấm càng rộng, dễ câu hơn.
        /// </summary>
        public static float CalculateReactionWindow(int fishingProficiency)
        {
            int level = GetSkillLevel(fishingProficiency);
            float bonus = (level - 1) * 0.08f;
            return Math.Min(BaseReactionWindowSeconds + bonus, MaxReactionWindowSeconds);
        }

        /// <summary>
        /// Kiểm tra xem người chơi có giật cần kịp trong cửa sổ phản xạ không.
        /// </summary>
        public static bool CheckFishingSuccess(float reactionTimeSeconds, float allowedWindow)
        {
            if (reactionTimeSeconds < 0f) return false;
            return reactionTimeSeconds <= allowedWindow;
        }

        /// <summary>
        /// Kiểm tra xem có đủ nguyên liệu và cấp độ để nấu công thức hay không.
        /// </summary>
        public static bool CanCook(CookingRecipe recipe, int cookingProficiency, int ownedIngredientCount)
        {
            if (recipe == null) return false;
            int level = GetSkillLevel(cookingProficiency);
            if (level < recipe.requiredCookingLevel) return false;
            if (ownedIngredientCount < recipe.requiredIngredientCount) return false;

            return true;
        }

        /// <summary>
        /// Tạo danh sách các công thức nấu ăn mặc định của game.
        /// </summary>
        public static List<CookingRecipe> GetDefaultRecipes()
        {
            return new List<CookingRecipe>
            {
                new CookingRecipe
                {
                    id = "recipe.grilled_fish",
                    dishName = "Cá Nướng Thịt Ngọt",
                    resultItemId = GrilledFishItemId,
                    requiredCookingLevel = 1,
                    requiredIngredientId = FishItemId,
                    requiredIngredientCount = 1,
                    instantHeal = 60.0,
                    attackBuff = 4.0,
                    buffDurationSeconds = 180f // 3 phút
                },
                new CookingRecipe
                {
                    id = "recipe.rabbit_sandwich",
                    dishName = "Bánh Mì Kẹp Thịt Thỏ",
                    resultItemId = SandwichItemId,
                    requiredCookingLevel = 2,
                    requiredIngredientId = FishItemId,
                    requiredIngredientCount = 2,
                    instantHeal = 120.0,
                    attackBuff = 8.0,
                    buffDurationSeconds = 300f // 5 phút
                }
            };
        }
    }
}
