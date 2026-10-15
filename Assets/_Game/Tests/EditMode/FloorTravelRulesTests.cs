using Game.Core.Floor;
using Game.PvE.Floor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P9-1b — luật chuyển tầng (FloorTravelRules) và trạng thái FloorLoadStatus.NotAllowed.
    /// Phần cổng trong FloorLoader / BuildPlan / PvEBootstrap có test riêng ở các bước sau của P9-1b.
    /// </summary>
    public class FloorTravelRulesTests
    {
        // ── FloorTravelRules.CanTravel: đủ 4 tổ hợp ──────────────────────────────

        [TestCase(true,  false, FloorTravelRules.Result.Ok)]
        [TestCase(false, false, FloorTravelRules.Result.NotInSafeZone)]
        [TestCase(true,  true,  FloorTravelRules.Result.PlayerDead)]
        [TestCase(false, true,  FloorTravelRules.Result.PlayerDead)] // gục được ưu tiên trước "ngoài Vùng An Toàn"
        public void CanTravel_CoversAllFourCombinations(bool inSafeZone, bool dead, FloorTravelRules.Result expected)
        {
            Assert.AreEqual(expected, FloorTravelRules.CanTravel(inSafeZone, dead));
        }

        [Test]
        public void CanTravel_OnlyOkWhenInSafeZoneAndAlive()
        {
            foreach (bool inSafeZone in new[] { false, true })
            {
                foreach (bool dead in new[] { false, true })
                {
                    bool ok = FloorTravelRules.CanTravel(inSafeZone, dead) == FloorTravelRules.Result.Ok;
                    Assert.AreEqual(inSafeZone && !dead, ok, $"inSafeZone={inSafeZone}, dead={dead}");
                }
            }
        }

        // ── FloorLoadStatus.NotAllowed / FloorLoadResult.NotAllowedResult ────────

        [Test]
        public void NotAllowedResult_CarriesPlayerMessageVerbatim()
        {
            const string message = "Chỉ có thể chuyển tầng khi đang ở Vùng An Toàn.";

            var result = FloorLoadResult.NotAllowedResult(message);

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.IsNull(result.Floor, "Chỉ Loaded mới có Floor");
            Assert.AreEqual(message, result.ErrorMessage, "Thông báo cho người chơi phải giữ nguyên văn, không thêm tiền tố");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NotAllowedResult_EmptyReason_StillHasAMessage(string reason)
        {
            var result = FloorLoadResult.NotAllowedResult(reason);

            Assert.AreEqual(FloorLoadStatus.NotAllowed, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage),
                "ErrorMessage luôn có giá trị khi Status != Loaded");
        }

        [Test]
        public void ExistingStatusesKeepTheirValuesAndFactories()
        {
            // Giá trị số của status cũ không đổi (có thể đã được lưu/so sánh ở nơi khác).
            Assert.AreEqual(0, (int)FloorLoadStatus.Loaded);
            Assert.AreEqual(1, (int)FloorLoadStatus.Locked);
            Assert.AreEqual(2, (int)FloorLoadStatus.NotFound);
            Assert.AreEqual(3, (int)FloorLoadStatus.Error);
            Assert.AreEqual(4, (int)FloorLoadStatus.NotAllowed);

            Assert.AreEqual(FloorLoadStatus.Locked, FloorLoadResult.LockedResult("x").Status);
            Assert.AreEqual(FloorLoadStatus.NotFound, FloorLoadResult.NotFoundResult("x").Status);
            Assert.AreEqual(FloorLoadStatus.Error, FloorLoadResult.ErrorResult("x").Status);
            Assert.AreEqual(FloorLoadStatus.Loaded, FloorLoadResult.Success(null).Status);
        }
    }
}
