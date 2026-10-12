using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Items;
using Game.Core.World;
using UnityEngine;

namespace Game.PvE.Boss
{
    /// <summary>Kết quả authoritative của một first-clear claim.</summary>
    public enum FirstClearClaimResult
    {
        Claimed = 0,
        AlreadyClaimed = 1,
        Unconfirmed = 2,
        NotConfigured = 3
    }

    /// <summary>
    /// Phần thưởng hạ boss (docs/02 §23–24, docs/03 §5): first-clear là claim theo account;
    /// main drop có ở mọi lần hạ; rare drop rematch dùng hệ số từ BossDefinition.
    /// </summary>
    public static class BossRewardHandler
    {
        public const string FirstClearType = "FirstClear";

        // Một save service trong một process chỉ chạy một read-modify-write claim một lúc.
        // Cloud authoritative write vẫn là điểm xác nhận trước khi item được cấp.
        private static readonly ConditionalWeakTable<ISaveService, SemaphoreSlim> ClaimGates =
            new ConditionalWeakTable<ISaveService, SemaphoreSlim>();

        /// <summary>
        /// Đọc và ghi RewardClaims qua nguồn authoritative. Nếu không đọc/ghi xác nhận được,
        /// tuyệt đối không tạo claim trống, không ghi đè claim cũ và không cấp item.
        /// </summary>
        public static async Task<FirstClearClaimResult> ClaimFirstClearRewardAsync(
            string bossId,
            ItemDefinition rewardItem,
            int quantity,
            ISaveService saveService,
            Items.PlayerInventory inventory = null)
        {
            Func<ItemDefinition, int, int> grantItem = inventory != null
                ? new Func<ItemDefinition, int, int>(inventory.TryAddItem)
                : null;
            return await ClaimFirstClearRewardAsync(bossId, rewardItem, quantity, saveService, grantItem);
        }

        /// <summary>
        /// Biến thể thuần của first-clear claim: caller truyền hành động cấp item sau khi claim
        /// authoritative được xác nhận. Dùng bởi <see cref="BossDefeatFlow"/> và EditMode tests.
        /// </summary>
        public static async Task<FirstClearClaimResult> ClaimFirstClearRewardAsync(
            string bossId,
            ItemDefinition rewardItem,
            int quantity,
            ISaveService saveService,
            Func<ItemDefinition, int, int> grantItem)
        {
            if (string.IsNullOrEmpty(bossId) || saveService == null)
                return FirstClearClaimResult.Unconfirmed;

            // Không claim một reward chưa thể cấp: nếu data/presentation chưa được gán,
            // account vẫn phải có thể nhận nó sau khi content được hoàn thiện.
            if (rewardItem == null || grantItem == null || quantity <= 0)
                return FirstClearClaimResult.NotConfigured;

            if (!(saveService is IAuthoritativeSaveService authoritativeSave))
            {
                Debug.LogWarning("[BossReward] Save service không hỗ trợ ghi authoritative RewardClaims; không cấp First-Clear reward.");
                return FirstClearClaimResult.Unconfirmed;
            }

            SemaphoreSlim gate = ClaimGates.GetValue(saveService, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                RewardClaimsData storedClaims;
                try
                {
                    storedClaims = await saveService.LoadAuthoritativeAsync<RewardClaimsData>(SaveKeys.RewardClaims);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[BossReward] Chưa xác nhận được RewardClaims: {error.Message}");
                    return FirstClearClaimResult.Unconfirmed;
                }

                // Chỉ null từ một lần đọc authoritative THÀNH CÔNG mới là "chưa có key".
                if (storedClaims != null && storedClaims.HasClaim(FirstClearType, bossId))
                    return FirstClearClaimResult.AlreadyClaimed;

                // Không mutate object vừa load: fake/cache có thể trả cùng reference; write thất bại
                // không được để một bản claim chưa xác nhận xuất hiện trong bộ nhớ.
                RewardClaimsData claimsToSave = CopyClaims(storedClaims);
                claimsToSave.AddClaim(FirstClearType, bossId, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                try
                {
                    await authoritativeSave.SaveAuthoritativeAsync(SaveKeys.RewardClaims, claimsToSave);
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[BossReward] Chưa xác nhận được ghi RewardClaims: {error.Message}");
                    return FirstClearClaimResult.Unconfirmed;
                }

                // Claim đã được source chính xác nhận rồi mới được cấp item.
                grantItem(rewardItem, quantity);

                return FirstClearClaimResult.Claimed;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>API bool tương thích callers/test cũ; chỉ true khi claim vừa được xác nhận.</summary>
        public static async Task<bool> TryClaimFirstClearRewardAsync(
            string bossId,
            ItemDefinition rewardItem,
            int quantity,
            ISaveService saveService,
            Items.PlayerInventory inventory = null)
        {
            return await ClaimFirstClearRewardAsync(bossId, rewardItem, quantity, saveService, inventory)
                == FirstClearClaimResult.Claimed;
        }

        /// <summary>Main drops rơi ở lần World Boss lẫn rematch.</summary>
        public static List<KeyValuePair<ItemDefinition, int>> RollMainDrops(
            BossDefinition definition,
            float randomRoll = -1f)
        {
            return definition == null
                ? new List<KeyValuePair<ItemDefinition, int>>()
                : RollDrops(definition.MainDrops, 1f, randomRoll);
        }

        /// <summary>
        /// Rare drops dùng chance đầy đủ khi World Boss chưa bị hạ; rematch nhân chance với
        /// <see cref="BossDefinition.RematchRareMultiplier"/> (mặc định 0.05).
        /// </summary>
        public static List<KeyValuePair<ItemDefinition, int>> RollRareDrops(
            BossDefinition definition,
            bool isRematch,
            float randomRoll = -1f)
        {
            if (definition == null) return new List<KeyValuePair<ItemDefinition, int>>();
            float multiplier = isRematch ? definition.RematchRareMultiplier : 1f;
            return RollDrops(definition.RareDrops, multiplier, randomRoll);
        }

        /// <summary>Alias cũ: chỉ roll main drops.</summary>
        public static List<KeyValuePair<ItemDefinition, int>> RollDrops(BossDefinition definition, float randomRoll = -1f)
        {
            return RollMainDrops(definition, randomRoll);
        }

        /// <summary>Roll một table với chance multiplier rõ ràng, thuần theo input ngoài Unity Random mặc định.</summary>
        public static List<KeyValuePair<ItemDefinition, int>> RollDrops(
            IReadOnlyList<BossDefinition.BossDrop> drops,
            float chanceMultiplier,
            float randomRoll = -1f)
        {
            var results = new List<KeyValuePair<ItemDefinition, int>>();
            if (drops == null) return results;

            float multiplier = Mathf.Clamp01(chanceMultiplier);
            foreach (BossDefinition.BossDrop drop in drops)
            {
                if (drop == null || drop.item == null) continue;

                float roll = randomRoll >= 0f ? randomRoll : UnityEngine.Random.value;
                float chance = Mathf.Clamp01(drop.chance) * multiplier;
                if (roll > chance) continue;

                int min = Mathf.Max(1, drop.minCount);
                int max = Mathf.Max(min, drop.maxCount);
                int count = UnityEngine.Random.Range(min, max + 1);
                if (count > 0)
                    results.Add(new KeyValuePair<ItemDefinition, int>(drop.item, count));
            }

            return results;
        }

        private static RewardClaimsData CopyClaims(RewardClaimsData source)
        {
            var copy = new RewardClaimsData
            {
                schemaVersion = source != null ? source.schemaVersion : 1,
                claims = new List<RewardClaimEntry>()
            };

            if (source?.claims == null) return copy;
            foreach (RewardClaimEntry claim in source.claims)
            {
                if (claim == null) continue;
                copy.claims.Add(new RewardClaimEntry
                {
                    type = claim.type,
                    bossId = claim.bossId,
                    claimedAtUnixSeconds = claim.claimedAtUnixSeconds
                });
            }
            return copy;
        }
    }
}
