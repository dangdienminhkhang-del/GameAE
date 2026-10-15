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
    /// P9-1d — "Chết ở tầng" (tầng đang đứng) và "Tầng cao nhất" (tầng cao nhất đã vào) tách thành hai giá trị:
    /// ResolveDeathFloors, DeathRecord → DeathInfo, đọc bản ghi cũ, và chữ trên màn chết.
    /// </summary>
    public class DeathFloorTests
    {
        // ── PvEBootstrap.ResolveDeathFloors (hàm thuần) ─────────────────────

        [Test]
        public void ResolveDeathFloors_WentUpThenDiedLower_FinalIsCurrent_HighestIsTracked()
        {
            var floors = PvEBootstrap.ResolveDeathFloors(currentFloor: 1, trackedHighest: 2);

            Assert.AreEqual(1, floors.finalFloor);
            Assert.AreEqual(2, floors.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_DiedOnHighestFloor_BothEqual()
        {
            var floors = PvEBootstrap.ResolveDeathFloors(currentFloor: 2, trackedHighest: 1);

            Assert.AreEqual(2, floors.finalFloor);
            Assert.AreEqual(2, floors.finalHighestFloor, "Tầng cao nhất không bao giờ thấp hơn tầng đang đứng.");
        }

        [Test]
        public void ResolveDeathFloors_TrackedHighestZero_HighestFollowsCurrentFloor()
        {
            var floors = PvEBootstrap.ResolveDeathFloors(currentFloor: 3, trackedHighest: 0);

            Assert.AreEqual(3, floors.finalFloor);
            Assert.AreEqual(3, floors.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_CurrentFloorUnknown_FinalIsZero_HighestIsTracked()
        {
            var floors = PvEBootstrap.ResolveDeathFloors(currentFloor: 0, trackedHighest: 2);

            Assert.AreEqual(0, floors.finalFloor, "Không biết tầng đang đứng thì ghi 0, không đoán.");
            Assert.AreEqual(2, floors.finalHighestFloor);
        }

        [Test]
        public void ResolveDeathFloors_NegativeValues_TreatedAsZero()
        {
            var floors = PvEBootstrap.ResolveDeathFloors(currentFloor: -1, trackedHighest: -5);

            Assert.AreEqual(0, floors.finalFloor);
            Assert.AreEqual(0, floors.finalHighestFloor);
        }

        // ── DeathRecord → DeathInfo ─────────────────────────────────────────

        [Test]
        public void DeathRecord_NewOverload_CarriesBothFloors_ToDeathInfo()
        {
            var record = new DeathRecord(DeathReason.Enemy, "enemy.test", 7,
                finalHighestFloor: 2, finalFloor: 1, totalPlayTimeSeconds: 90, deathTimeUnix: 1700000000L);

            Assert.AreEqual(2, record.FinalHighestFloor);
            Assert.AreEqual(1, record.FinalFloor);

            DeathInfo info = record.ToDeathInfo();
            Assert.AreEqual(2, info.finalHighestFloor);
            Assert.AreEqual(1, info.finalFloor);
            Assert.AreEqual(7, info.finalLevel);
            Assert.AreEqual(90, info.totalPlayTimeSeconds);
            Assert.AreEqual(1700000000L, info.deathTimeUnix);
        }

        [Test]
        public void DeathRecord_OldSixArgConstructor_StillWorks_WithUnknownFinalFloor()
        {
            var record = new DeathRecord(DeathReason.Boss, "boss.test", 12, 3, 4567, 1700000123L);

            Assert.AreEqual(3, record.FinalHighestFloor);
            Assert.AreEqual(0, record.FinalFloor);
            Assert.AreEqual(0, record.ToDeathInfo().finalFloor);
        }

        // ── Đọc/ghi JSON (JsonUtility) ──────────────────────────────────────

        [Test]
        public void DeathInfo_Json_OldRecordWithoutFinalFloor_ReadsAsZero()
        {
            const string oldJson =
                "{\"isDead\":true,\"deathCause\":\"Enemy\",\"finalLevel\":5,\"finalHighestFloor\":2,\"totalPlayTimeSeconds\":60}";

            DeathInfo info = JsonUtility.FromJson<DeathInfo>(oldJson);

            Assert.IsTrue(info.isDead);
            Assert.AreEqual(2, info.finalHighestFloor);
            Assert.AreEqual(0, info.finalFloor, "Bản ghi cũ thiếu field → 0 (không xác định).");
        }

        [Test]
        public void DeathInfo_Json_RoundTrip_KeepsBothFloors()
        {
            var info = new DeathRecord(DeathReason.Trap, "trap.test", 4, 2, 1, 30, 1700000000L).ToDeathInfo();

            DeathInfo restored = JsonUtility.FromJson<DeathInfo>(JsonUtility.ToJson(info));

            Assert.AreEqual(2, restored.finalHighestFloor);
            Assert.AreEqual(1, restored.finalFloor);
        }

        // ── Chữ trên màn chết ───────────────────────────────────────────────

        private static DeathInfo Info(int finalFloor, int highest) => new DeathInfo
        {
            isDead = true,
            deathCause = "Enemy",
            finalLevel = 5,
            finalFloor = finalFloor,
            finalHighestFloor = highest,
            totalPlayTimeSeconds = 125
        };

        [Test]
        public void DetailsText_HasTheTwoGroupLayout()
        {
            string text = DeathScreenController.BuildDetailsText(Info(finalFloor: 1, highest: 2));

            Assert.AreEqual(
                "Nguyên nhân: Quái vật\n" +
                "Chết ở tầng: 1\n" +
                "\n" +
                "Thành tích\n" +
                "Level cuối: 5\n" +
                "Tầng cao nhất: 2\n" +
                "Thời gian chơi: 02:05",
                text);
        }

        [Test]
        public void DetailsText_DiedBelowHighest_ShowsBothFloors()
        {
            string text = DeathScreenController.BuildDetailsText(Info(finalFloor: 1, highest: 2));

            StringAssert.Contains("Chết ở tầng: 1", text);
            StringAssert.Contains("Tầng cao nhất: 2", text);
        }

        [Test]
        public void DetailsText_OldRecord_DeathFloorFallsBackToHighest()
        {
            string text = DeathScreenController.BuildDetailsText(Info(finalFloor: 0, highest: 2));

            StringAssert.Contains("Chết ở tầng: 2", text);
            StringAssert.Contains("Tầng cao nhất: 2", text);
        }

        [Test]
        public void DetailsText_BothZero_ShowsQuestionMark_NotFloorOne()
        {
            string text = DeathScreenController.BuildDetailsText(Info(finalFloor: 0, highest: 0));

            StringAssert.Contains("Chết ở tầng: ?", text);
            StringAssert.Contains("Tầng cao nhất: ?", text);
            StringAssert.DoesNotContain("tầng: 1", text);
        }

        [Test]
        public void DetailsText_NullInfo_DoesNotThrow_AndShowsQuestionMarks()
        {
            string text = null;
            Assert.DoesNotThrow(() => text = DeathScreenController.BuildDetailsText(null));

            StringAssert.Contains("Chết ở tầng: ?", text);
            StringAssert.Contains("Tầng cao nhất: ?", text);
        }

        // ── Show() thật ghi đúng chữ vào ô chi tiết ─────────────────────────

        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
                if (go != null) Object.DestroyImmediate(go);
            _created.Clear();
        }

        [Test]
        public void Show_WritesBothFloorLines_ToTheDetailsText()
        {
            // Controller để INACTIVE nên Awake/OnEnable không chạy (Awake tạo AccountResetService thật — không cần cho test chữ).
            var host = new GameObject("DeathScreen");
            host.SetActive(false);
            _created.Add(host);
            var controller = host.AddComponent<DeathScreenController>();

            var detailsObject = new GameObject("Details");
            _created.Add(detailsObject);
            var details = detailsObject.AddComponent<Text>();

            FieldInfo field = typeof(DeathScreenController).GetField("detailsText", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "DeathScreenController không còn field 'detailsText'");
            field.SetValue(controller, details);

            controller.Show(Info(finalFloor: 1, highest: 2));

            StringAssert.Contains("Chết ở tầng: 1", details.text);
            StringAssert.Contains("Thành tích", details.text);
            StringAssert.Contains("Tầng cao nhất: 2", details.text);
        }
    }
}
