using System.Collections.Generic;
using System.Reflection;
using Game.Core.Character;
using Game.PvE.Bootstrap;
using Game.UI.Death;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Task P9-1d — màn chết hiện "Chết ở tầng" (cạnh Nguyên nhân) và "Tầng cao nhất" (trong Thành tích),
    /// và hai giá trị này được chọn đúng lúc chết (từ Phase 9 chúng có thể khác nhau).
    /// </summary>
    public class DeathFloorTrackingTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        // ── PvEBootstrap.ResolveDeathFloors (hàm thuần) ─────────────────────

        [Test]
        public void ResolveDeathFloors_WentUpThenBackDown_KeepsHighestAndCurrentApart()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: 1, trackedHighest: 2);
            Assert.AreEqual(1, r.finalFloor, "Chết ở Tầng 1.");
            Assert.AreEqual(2, r.finalHighestFloor, "Nhưng tầng cao nhất vẫn là 2.");
        }

        [Test]
        public void ResolveDeathFloors_DyingOnTheHighestFloor_BothEqual()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: 2, trackedHighest: 1);
            Assert.AreEqual(2, r.finalFloor);
            Assert.AreEqual(2, r.finalHighestFloor, "Tầng cao nhất không bao giờ thấp hơn tầng đang đứng.");
        }

        [Test]
        public void ResolveDeathFloors_NoTrackedHighest_UsesCurrentFloor()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: 3, trackedHighest: 0);
            Assert.AreEqual(3, r.finalFloor);
            Assert.AreEqual(3, r.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_UnknownCurrentFloor_StaysZero_AndKeepsTrackedHighest()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: 0, trackedHighest: 2);
            Assert.AreEqual(0, r.finalFloor, "Không biết tầng đang đứng thì để 0, không đoán.");
            Assert.AreEqual(2, r.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_NothingKnown_BothZero_NotGuessedAsOne()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: 0, trackedHighest: 0);
            Assert.AreEqual(0, r.finalFloor);
            Assert.AreEqual(0, r.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_NegativeValues_AreTreatedAsZero()
        {
            var r = PvEBootstrap.ResolveDeathFloors(currentFloor: -1, trackedHighest: -5);
            Assert.AreEqual(0, r.finalFloor);
            Assert.AreEqual(0, r.finalHighestFloor);
        }

        // ── DeathRecord → DeathInfo ─────────────────────────────────────────

        [Test]
        public void DeathRecord_ToDeathInfo_CarriesBothFloors()
        {
            var record = new DeathRecord(DeathReason.Enemy, "enemy.slime", 7, 2, 1, 600, 1_700_000_000L);

            DeathInfo info = record.ToDeathInfo();

            Assert.AreEqual(2, info.finalHighestFloor);
            Assert.AreEqual(1, info.finalFloor);
            Assert.AreEqual(1, record.FinalFloor);
            Assert.AreEqual(2, record.FinalHighestFloor);
            Assert.IsTrue(info.isDead);
            Assert.AreEqual(7, info.finalLevel);
            Assert.AreEqual(600, info.totalPlayTimeSeconds);
        }

        [Test]
        public void DeathRecord_OldSixArgConstructor_LeavesFinalFloorUnknown()
        {
            var record = new DeathRecord(DeathReason.Boss, "boss.floor_01", 5, 1, 300, 1_700_000_000L);

            Assert.AreEqual(0, record.FinalFloor);
            Assert.AreEqual(0, record.ToDeathInfo().finalFloor);
            Assert.AreEqual(1, record.ToDeathInfo().finalHighestFloor, "Các field cũ giữ nguyên tên và ý nghĩa.");
        }

        [Test]
        public void DeathInfo_RoundTripsThroughJson_WithFinalFloor()
        {
            var info = new DeathRecord(DeathReason.Enemy, "enemy.bat", 6, 2, 1, 90, 1_700_000_000L).ToDeathInfo();

            DeathInfo back = JsonUtility.FromJson<DeathInfo>(JsonUtility.ToJson(info));

            Assert.AreEqual(1, back.finalFloor);
            Assert.AreEqual(2, back.finalHighestFloor);
        }

        [Test]
        public void DeathInfo_OldJsonWithoutFinalFloor_ReadsAsZero()
        {
            const string oldJson =
                "{\"isDead\":true,\"deathTimeUnix\":1700000000,\"deathCause\":\"Enemy\",\"deathSourceId\":\"enemy.slime\"," +
                "\"finalLevel\":4,\"finalHighestFloor\":1,\"totalPlayTimeSeconds\":120}";

            DeathInfo info = JsonUtility.FromJson<DeathInfo>(oldJson);

            Assert.IsTrue(info.isDead);
            Assert.AreEqual(0, info.finalFloor, "Bản ghi cũ không có finalFloor → 0 (không xác định), vẫn đọc được.");
            Assert.AreEqual(1, info.finalHighestFloor);
        }

        // ── Chữ hiện trên màn chết (hàm thuần) ──────────────────────────────

        private static string[] Lines(DeathInfo info) => DeathScreenController.BuildDetailsText(info).Split('\n');

        [Test]
        public void DetailsText_ShowsDeathFloorBesideCause_AndHighestFloorUnderAchievements()
        {
            var info = new DeathInfo
            {
                isDead = true, deathCause = "Enemy", finalLevel = 8,
                finalFloor = 1, finalHighestFloor = 2, totalPlayTimeSeconds = 125
            };

            string[] lines = Lines(info);

            Assert.AreEqual("Nguyên nhân: Quái vật", lines[0]);
            Assert.AreEqual("Chết ở tầng: 1", lines[1]);
            Assert.AreEqual("", lines[2], "Dòng trống tách hai nhóm.");
            Assert.AreEqual("Thành tích", lines[3]);
            Assert.AreEqual("Level cuối: 8", lines[4]);
            Assert.AreEqual("Tầng cao nhất: 2", lines[5]);
            Assert.AreEqual("Thời gian chơi: 02:05", lines[6]);
            Assert.AreEqual(7, lines.Length);
        }

        [Test]
        public void DetailsText_OldRecordWithoutFinalFloor_UsesHighestFloorForDeathFloor()
        {
            var info = new DeathInfo { isDead = true, deathCause = "Boss", finalLevel = 5, finalFloor = 0, finalHighestFloor = 2 };

            string[] lines = Lines(info);

            Assert.AreEqual("Chết ở tầng: 2", lines[1]);
            Assert.AreEqual("Tầng cao nhất: 2", lines[5]);
        }

        [Test]
        public void DetailsText_BothFloorsUnknown_ShowsQuestionMarks_NotFloorOne()
        {
            var info = new DeathInfo { isDead = true, deathCause = "Trap", finalLevel = 3, finalFloor = 0, finalHighestFloor = 0 };

            string[] lines = Lines(info);

            Assert.AreEqual("Chết ở tầng: ?", lines[1]);
            Assert.AreEqual("Tầng cao nhất: ?", lines[5]);
        }

        [Test]
        public void DetailsText_DiedOnFloorTwo_HighestMissing_FallsBackToDeathFloor()
        {
            var info = new DeathInfo { isDead = true, deathCause = "Enemy", finalLevel = 6, finalFloor = 2, finalHighestFloor = 0 };

            string[] lines = Lines(info);

            Assert.AreEqual("Chết ở tầng: 2", lines[1]);
            Assert.AreEqual("Tầng cao nhất: 2", lines[5], "Tầng cao nhất không được hiện thấp hơn tầng đã chết.");
        }

        [Test]
        public void DetailsText_NullInfo_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => DeathScreenController.BuildDetailsText(null));
        }

        // ── Show() dùng đúng chữ trên (controller inactive để Awake không chạy) ──

        [Test]
        public void Show_WritesTheTwoGroupLayoutIntoTheDetailsText()
        {
            var host = new GameObject("DeathScreen");
            host.SetActive(false); // Awake tạo AccountResetService thật — không cần cho test chữ
            _created.Add(host);
            var controller = host.AddComponent<DeathScreenController>();

            var panel = new GameObject("Panel");
            _created.Add(panel);
            var detailsGo = new GameObject("Details");
            _created.Add(detailsGo);
            var details = detailsGo.AddComponent<Text>();

            Set(controller, "panelRoot", panel);
            Set(controller, "detailsText", details);

            controller.Show(new DeathInfo
            {
                isDead = true, deathCause = "Enemy", finalLevel = 9,
                finalFloor = 1, finalHighestFloor = 2, totalPlayTimeSeconds = 61
            });

            StringAssert.Contains("Chết ở tầng: 1", details.text);
            StringAssert.Contains("Tầng cao nhất: 2", details.text);
            StringAssert.Contains("Level cuối: 9", details.text);
            Assert.Less(details.text.IndexOf("Chết ở tầng", System.StringComparison.Ordinal),
                details.text.IndexOf("Thành tích", System.StringComparison.Ordinal),
                "\"Chết ở tầng\" nằm ở nhóm trên (cạnh Nguyên nhân), \"Tầng cao nhất\" nằm trong Thành tích.");
        }

        private static void Set(DeathScreenController controller, string field, object value)
        {
            FieldInfo f = typeof(DeathScreenController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"DeathScreenController không còn field '{field}'");
            f.SetValue(controller, value);
        }
    }
}
