using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.World;

namespace Game.PvE.Boss
{
    /// <summary>Trạng thái báo World State của một lần boss gục.</summary>
    public enum BossDefeatReportStatus
    {
        NotRequired = 0,
        Confirmed = 1,
        Failed = 2
    }

    /// <summary>Kết quả thuần của luồng boss chết, để BossController chỉ chịu trách nhiệm MonoBehaviour/presentation.</summary>
    public sealed class BossDefeatFlowResult
    {
        public bool IsRematch { get; internal set; }
        public BossDefeatReportStatus ReportStatus { get; internal set; }
        public Exception ReportError { get; internal set; }
        public FirstClearClaimResult FirstClearResult { get; internal set; }
        public IReadOnlyList<KeyValuePair<Game.Core.Items.ItemDefinition, int>> MainDrops { get; internal set; }
        public IReadOnlyList<KeyValuePair<Game.Core.Items.ItemDefinition, int>> RareDrops { get; internal set; }
    }

    /// <summary>
    /// Luồng thuần sau khi boss chết (docs/02 §22–24): đọc World State → chỉ report khi cần;
    /// reward claim độc lập với kết quả report; rematch không ghi World State.
    /// Không là MonoBehaviour, nên kiểm thử bằng fake service mà không cần scene.
    /// </summary>
    public sealed class BossDefeatFlow
    {
        private readonly IWorldStateService _worldState;
        private readonly ISaveService _saveService;

        public BossDefeatFlow(IWorldStateService worldState, ISaveService saveService)
        {
            _worldState = worldState;
            _saveService = saveService;
        }

        /// <summary>
        /// Boss này có phải rematch không: World State báo boss đã Defeated.
        /// Đọc lỗi → false (không suy ra rematch; server idempotent sẽ quyết định khi report).
        /// BossController gọi hàm này TRƯỚC để EXP, đồ hiếm và báo cáo dùng cùng một giá trị.
        /// </summary>
        public Task<bool> ResolveIsRematchAsync(string bossId)
        {
            return IsAlreadyDefeatedAsync(bossId);
        }

        /// <summary>
        /// Xử lý report và rewards. <paramref name="grantItem"/> chỉ được gọi khi player còn sống;
        /// first-clear claim được authoritative-confirm trước khi callback cấp item.
        /// <paramref name="knownRematch"/>: đã biết rematch hay chưa (từ <see cref="ResolveIsRematchAsync"/>);
        /// có giá trị thì dùng luôn, KHÔNG đọc World State lần nữa. null → tự đọc như trước.
        /// </summary>
        public async Task<BossDefeatFlowResult> HandleDefeatAsync(
            BossDefinition definition,
            bool isPlayerDead,
            Func<Game.Core.Items.ItemDefinition, int, int> grantItem,
            bool? knownRematch = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            var result = new BossDefeatFlowResult
            {
                IsRematch = knownRematch ?? await IsAlreadyDefeatedAsync(definition.Id),
                FirstClearResult = FirstClearClaimResult.NotConfigured,
                MainDrops = Array.Empty<KeyValuePair<Game.Core.Items.ItemDefinition, int>>(),
                RareDrops = Array.Empty<KeyValuePair<Game.Core.Items.ItemDefinition, int>>()
            };

            // A known defeated World Boss is a rematch and MUST NOT report again.
            // If read failed we get false and deliberately report: Cloud Code is idempotent.
            if (result.IsRematch)
            {
                result.ReportStatus = BossDefeatReportStatus.NotRequired;
            }
            else
            {
                var report = await ReportAsync(definition);
                result.ReportStatus = report.status;
                result.ReportError = report.error;
            }

            // Death never writes World State, but a player already locked at the moment the boss
            // resolves must not receive any account reward or loot.
            if (isPlayerDead) return result;

            try
            {
                result.FirstClearResult = await BossRewardHandler.ClaimFirstClearRewardAsync(
                    definition.Id,
                    definition.FirstClearRewardItem,
                    definition.FirstClearRewardQuantity,
                    _saveService,
                    grantItem);
            }
            catch
            {
                // First-clear cannot be awarded without a confirmed claim. Main/rare drops remain independent.
                result.FirstClearResult = FirstClearClaimResult.Unconfirmed;
            }

            result.MainDrops = BossRewardHandler.RollMainDrops(definition);
            result.RareDrops = BossRewardHandler.RollRareDrops(definition, result.IsRematch);
            GrantAll(result.MainDrops, grantItem);
            GrantAll(result.RareDrops, grantItem);
            return result;
        }

        /// <summary>UI retry only repeats the World State report, never repeats EXP/reward/drop processing.</summary>
        public async Task<(BossDefeatReportStatus status, Exception error)> RetryDefeatReportAsync(BossDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return await ReportAsync(definition);
        }

        private async Task<bool> IsAlreadyDefeatedAsync(string bossId)
        {
            if (_worldState == null) return false;
            try
            {
                return await _worldState.GetBossStatusAsync(bossId) == BossStatus.Defeated;
            }
            catch
            {
                // Fail closed for the read: do not infer a rematch and let the idempotent server endpoint decide.
                return false;
            }
        }

        private async Task<(BossDefeatReportStatus status, Exception error)> ReportAsync(BossDefinition definition)
        {
            if (_worldState == null)
            {
                return (BossDefeatReportStatus.Failed,
                    new InvalidOperationException("IWorldStateService không sẵn sàng để báo boss đã bị hạ."));
            }

            try
            {
                await _worldState.ReportBossDefeated(definition.Id, definition.FloorId);
                return (BossDefeatReportStatus.Confirmed, null);
            }
            catch (Exception error)
            {
                return (BossDefeatReportStatus.Failed, error);
            }
        }

        private static void GrantAll(
            IReadOnlyList<KeyValuePair<Game.Core.Items.ItemDefinition, int>> drops,
            Func<Game.Core.Items.ItemDefinition, int, int> grantItem)
        {
            if (drops == null || grantItem == null) return;
            foreach (KeyValuePair<Game.Core.Items.ItemDefinition, int> drop in drops)
            {
                if (drop.Key != null && drop.Value > 0)
                    grantItem(drop.Key, drop.Value);
            }
        }
    }
}
