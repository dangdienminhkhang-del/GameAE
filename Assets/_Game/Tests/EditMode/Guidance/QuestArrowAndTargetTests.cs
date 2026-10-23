using System;
using System.Collections.Generic;
using Game.Core.Guidance;
using Game.Core.Quest;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>Hình học mũi tên quest (Phase 9.9): ẩn khi mục tiêu trong khung, kẹp mép có lề, đúng ở 16:9 / 20:9 / safe area, không NaN.</summary>
    public class QuestArrowMathTests
    {
        private static readonly Rect Screen16x9 = new Rect(0f, 0f, 1920f, 1080f);
        private static readonly Rect Screen20x9 = new Rect(0f, 0f, 2400f, 1080f);
        private static readonly Rect NotchedSafeArea = new Rect(88f, 0f, 2224f, 1080f); // 20:9 có tai thỏ bên trái và vạch bên phải

        private const float Edge = 96f;
        private const float View = 48f;

        private static void AssertFinite(QuestArrowResult r)
        {
            Assert.IsFalse(float.IsNaN(r.ScreenPosition.x) || float.IsNaN(r.ScreenPosition.y) || float.IsNaN(r.AngleDegrees));
            Assert.IsFalse(float.IsInfinity(r.ScreenPosition.x) || float.IsInfinity(r.ScreenPosition.y) || float.IsInfinity(r.AngleDegrees));
        }

        // ── Ẩn khi mục tiêu đã trong khung ───────────────────────────────────

        [Test]
        public void TargetInsideTheView_HidesTheArrow()
        {
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(960f, 540f), Screen16x9, Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(10f, 10f), Screen16x9, Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(1900f, 1070f), Screen16x9, Edge, View).Visible);
        }

        [Test]
        public void TargetJustOutsideButWithinTheViewMargin_StillHides()
        {
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(1920f + View - 1f, 540f), Screen16x9, Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(-View + 1f, 540f), Screen16x9, Edge, View).Visible);
        }

        [Test]
        public void TargetBeyondTheViewMargin_ShowsTheArrow()
        {
            Assert.IsTrue(QuestArrowMath.Compute(new Vector2(1920f + View + 10f, 540f), Screen16x9, Edge, View).Visible);
        }

        // ── Góc ──────────────────────────────────────────────────────────────

        [Test]
        public void Angles_PointTowardTheTarget_InTheFourDirections()
        {
            Assert.AreEqual(0f, QuestArrowMath.Compute(new Vector2(4000f, 540f), Screen16x9, Edge, View).AngleDegrees, 1e-3f);
            Assert.AreEqual(90f, QuestArrowMath.Compute(new Vector2(960f, 4000f), Screen16x9, Edge, View).AngleDegrees, 1e-3f);
            Assert.AreEqual(180f, Mathf.Abs(QuestArrowMath.Compute(new Vector2(-4000f, 540f), Screen16x9, Edge, View).AngleDegrees), 1e-3f);
            Assert.AreEqual(-90f, QuestArrowMath.Compute(new Vector2(960f, -4000f), Screen16x9, Edge, View).AngleDegrees, 1e-3f);
        }

        [Test]
        public void Angle_Diagonal_Is45Degrees()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(960f + 3000f, 540f + 3000f), Screen16x9, Edge, View);
            Assert.AreEqual(45f, r.AngleDegrees, 1e-3f);
        }

        // ── Vị trí kẹp mép có lề ─────────────────────────────────────────────

        [Test]
        public void Position_RightEdge_IsInsetByTheMargin()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(4000f, 540f), Screen16x9, Edge, View);
            Assert.IsTrue(r.Visible);
            Assert.AreEqual(1920f - Edge, r.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(540f, r.ScreenPosition.y, 1e-2f);
        }

        [Test]
        public void Position_TopEdge_IsInsetByTheMargin()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(960f, 4000f), Screen16x9, Edge, View);
            Assert.AreEqual(960f, r.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(1080f - Edge, r.ScreenPosition.y, 1e-2f);
        }

        [Test]
        public void Position_LeftAndBottomEdges_AreInsetByTheMargin()
        {
            QuestArrowResult left = QuestArrowMath.Compute(new Vector2(-4000f, 540f), Screen16x9, Edge, View);
            QuestArrowResult bottom = QuestArrowMath.Compute(new Vector2(960f, -4000f), Screen16x9, Edge, View);
            Assert.AreEqual(Edge, left.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(Edge, bottom.ScreenPosition.y, 1e-2f);
        }

        [Test]
        public void Position_AlwaysInsideTheSafeAreaMinusMargin_ForManyTargets()
        {
            foreach (Rect area in new[] { Screen16x9, Screen20x9, NotchedSafeArea })
            {
                for (int i = 0; i < 72; i++)
                {
                    float angle = i * 5f * Mathf.Deg2Rad;
                    Vector2 target = area.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6000f;
                    QuestArrowResult r = QuestArrowMath.Compute(target, area, Edge, View);

                    Assert.IsTrue(r.Visible);
                    AssertFinite(r);
                    Assert.GreaterOrEqual(r.ScreenPosition.x, area.xMin + Edge - 1e-2f, $"{area} {i * 5}°");
                    Assert.LessOrEqual(r.ScreenPosition.x, area.xMax - Edge + 1e-2f, $"{area} {i * 5}°");
                    Assert.GreaterOrEqual(r.ScreenPosition.y, area.yMin + Edge - 1e-2f, $"{area} {i * 5}°");
                    Assert.LessOrEqual(r.ScreenPosition.y, area.yMax - Edge + 1e-2f, $"{area} {i * 5}°");
                }
            }
        }

        [Test]
        public void Position_LiesOnTheLineFromScreenCenterToTheTarget()
        {
            Vector2 target = new Vector2(3500f, 2000f);
            QuestArrowResult r = QuestArrowMath.Compute(target, Screen16x9, Edge, View);

            Vector2 toTarget = (target - Screen16x9.center).normalized;
            Vector2 toArrow = (r.ScreenPosition - Screen16x9.center).normalized;
            Assert.AreEqual(0f, toTarget.x * toArrow.y - toTarget.y * toArrow.x, 1e-4f, "cùng hướng từ tâm");
            Assert.Greater(Vector2.Dot(toTarget, toArrow), 0.999f);
        }

        // ── Tỉ lệ khung và safe area ─────────────────────────────────────────

        [Test]
        public void Works_At20by9_RightEdgeUsesTheWiderScreen()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(6000f, 540f), Screen20x9, Edge, View);
            Assert.AreEqual(2400f - Edge, r.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(540f, r.ScreenPosition.y, 1e-2f);
        }

        [Test]
        public void SameAngleTarget_LandsOnDifferentEdges_At16by9And20by9()
        {
            // Góc 25° so với tâm: ở 16:9 góc chéo của vùng có lề là ~27.2° nên trúng cạnh PHẢI trước; ở 20:9 (rộng hơn) góc chéo là ~21.9° nên trúng cạnh TRÊN trước.
            Vector2 dir = new Vector2(Mathf.Cos(25f * Mathf.Deg2Rad), Mathf.Sin(25f * Mathf.Deg2Rad));
            QuestArrowResult narrow = QuestArrowMath.Compute(Screen16x9.center + dir * 6000f, Screen16x9, Edge, View);
            QuestArrowResult wide = QuestArrowMath.Compute(Screen20x9.center + dir * 6000f, Screen20x9, Edge, View);

            Assert.AreEqual(Screen16x9.xMax - Edge, narrow.ScreenPosition.x, 1e-1f);
            Assert.Less(narrow.ScreenPosition.y, Screen16x9.yMax - Edge);

            Assert.AreEqual(Screen20x9.yMax - Edge, wide.ScreenPosition.y, 1e-1f);
            Assert.Less(wide.ScreenPosition.x, Screen20x9.xMax - Edge);
        }

        [Test]
        public void SafeArea_NotchedScreen_KeepsTheArrowInsideTheSafeRect()
        {
            QuestArrowResult right = QuestArrowMath.Compute(new Vector2(6000f, 540f), NotchedSafeArea, Edge, View);
            QuestArrowResult left = QuestArrowMath.Compute(new Vector2(-6000f, 540f), NotchedSafeArea, Edge, View);

            Assert.AreEqual(NotchedSafeArea.xMax - Edge, right.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(NotchedSafeArea.xMin + Edge, left.ScreenPosition.x, 1e-2f);
        }

        [Test]
        public void SafeArea_TargetInTheNotch_IsOutsideTheSafeAreaSoItShowsAnArrow()
        {
            // Mục tiêu nằm trong vùng bị tai thỏ che (x < xMin của safe area - lề nhìn): người chơi không thấy nên vẫn cần mũi tên.
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(10f, 540f), NotchedSafeArea, Edge, 10f);
            Assert.IsTrue(r.Visible);
        }

        // ── Không NaN, dữ liệu xấu ───────────────────────────────────────────

        [Test]
        public void BadTarget_HidesTheArrow_WithoutNaN()
        {
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(float.NaN, 5f), Screen16x9, Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(5f, float.PositiveInfinity), Screen16x9, Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(float.NegativeInfinity, 0f), Screen16x9, Edge, View).Visible);
        }

        [Test]
        public void BadSafeArea_HidesTheArrow()
        {
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(5000f, 0f), new Rect(0f, 0f, 0f, 1080f), Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(5000f, 0f), new Rect(0f, 0f, 1920f, -5f), Edge, View).Visible);
            Assert.IsFalse(QuestArrowMath.Compute(new Vector2(5000f, 0f), new Rect(float.NaN, 0f, 1920f, 1080f), Edge, View).Visible);
        }

        [Test]
        public void HugeMargin_DoesNotProduceNaN_ArrowSitsAtTheCenter()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(5000f, 3000f), Screen16x9, 5000f, View);
            AssertFinite(r);
            Assert.IsTrue(r.Visible);
            Assert.AreEqual(Screen16x9.center.x, r.ScreenPosition.x, 1e-2f);
            Assert.AreEqual(Screen16x9.center.y, r.ScreenPosition.y, 1e-2f);
        }

        [Test]
        public void NegativeMargins_AreTreatedAsZero()
        {
            QuestArrowResult r = QuestArrowMath.Compute(new Vector2(6000f, 540f), Screen16x9, -50f, -50f);
            Assert.IsTrue(r.Visible);
            Assert.AreEqual(1920f, r.ScreenPosition.x, 1e-2f);
        }

        [Test]
        public void TargetExactlyAtTheScreenCenter_IsInsideSoHidden()
        {
            Assert.IsFalse(QuestArrowMath.Compute(Screen16x9.center, Screen16x9, Edge, View).Visible);
        }

        [Test]
        public void HiddenResult_IsFinite()
        {
            AssertFinite(QuestArrowResult.Hidden);
            Assert.IsFalse(QuestArrowResult.Hidden.Visible);
        }

        // ── Đã tới gần ───────────────────────────────────────────────────────

        [Test]
        public void IsNear_InsideTheArrivalRadius_True()
        {
            Assert.IsTrue(QuestArrowMath.IsNear(Vector2.zero, new Vector2(2f, 0f), 3f));
            Assert.IsTrue(QuestArrowMath.IsNear(Vector2.zero, new Vector2(3f, 0f), 3f), "đúng bán kính vẫn tính là gần");
        }

        [Test]
        public void IsNear_OutsideTheRadius_False()
        {
            Assert.IsFalse(QuestArrowMath.IsNear(Vector2.zero, new Vector2(3.1f, 0f), 3f));
        }

        [Test]
        public void IsNear_BadInput_IsFalse_AndNegativeRadiusActsAsZero()
        {
            Assert.IsFalse(QuestArrowMath.IsNear(new Vector2(float.NaN, 0f), Vector2.zero, 3f));
            Assert.IsFalse(QuestArrowMath.IsNear(Vector2.zero, new Vector2(0f, float.PositiveInfinity), 3f));
            Assert.IsFalse(QuestArrowMath.IsNear(Vector2.zero, new Vector2(0.5f, 0f), -4f));
            Assert.IsTrue(QuestArrowMath.IsNear(Vector2.one, Vector2.one, -4f), "trùng điểm");
        }
    }

    /// <summary>Chọn mục tiêu cho mũi tên quest (Phase 9.9), thuần logic: Talk → NPC, Reach → vùng, Kill/Collect → Vùng Farm gần nhất, trả quest ở NPC.</summary>
    public class QuestTargetRulesTests
    {
        private static QuestTargetCandidate Active(QuestObjectiveType type, string target, string turnIn = null, string id = "q")
            => new QuestTargetCandidate(id, QuestStatus.Active, type, target, turnIn);

        private static QuestTargetCandidate Ready(string turnIn, string id = "q")
            => new QuestTargetCandidate(id, QuestStatus.ReadyToTurnIn, QuestObjectiveType.Talk, "x", turnIn);

        [Test]
        public void NoQuests_NullOrEmpty_GivesNone()
        {
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.Select(null).Kind);
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.Select(new List<QuestTargetCandidate>()).Kind);
        }

        [Test]
        public void Talk_PointsToTheNpcById()
        {
            QuestTarget t = QuestTargetRules.ForCandidate(Active(QuestObjectiveType.Talk, "npc.elder"));
            Assert.AreEqual(QuestTargetKind.Npc, t.Kind);
            Assert.AreEqual("npc.elder", t.Id);
        }

        [Test]
        public void Reach_PointsToTheZoneById()
        {
            QuestTarget t = QuestTargetRules.ForCandidate(Active(QuestObjectiveType.Reach, "zone.floor_01_farm"));
            Assert.AreEqual(QuestTargetKind.Zone, t.Kind);
            Assert.AreEqual("zone.floor_01_farm", t.Id);
        }

        [Test]
        public void KillAndCollect_PointToTheNearestFarm_WithNoId()
        {
            foreach (QuestObjectiveType type in new[] { QuestObjectiveType.Kill, QuestObjectiveType.Collect })
            {
                QuestTarget t = QuestTargetRules.ForCandidate(Active(type, "anything"));
                Assert.AreEqual(QuestTargetKind.NearestFarm, t.Kind, type.ToString());
                Assert.IsNull(t.Id);
            }
        }

        [Test]
        public void TalkAndReach_WithoutATargetId_GiveNone()
        {
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.ForCandidate(Active(QuestObjectiveType.Talk, "")).Kind);
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.ForCandidate(Active(QuestObjectiveType.Reach, null)).Kind);
        }

        [Test]
        public void ReadyToTurnIn_PointsOnlyToTheTurnInNpc()
        {
            QuestTarget t = QuestTargetRules.ForCandidate(Ready("npc.giver"));
            Assert.AreEqual(QuestTargetKind.Npc, t.Kind);
            Assert.AreEqual("npc.giver", t.Id);
        }

        [Test]
        public void ReadyToTurnIn_IgnoresTheObjective_NeverPointsAtTheFarm()
        {
            var kill = new QuestTargetCandidate("q", QuestStatus.ReadyToTurnIn, QuestObjectiveType.Kill, "slime", "npc.giver");
            QuestTarget t = QuestTargetRules.ForCandidate(kill);
            Assert.AreEqual(QuestTargetKind.Npc, t.Kind);
            Assert.AreEqual("npc.giver", t.Id);
        }

        [Test]
        public void ReadyToTurnIn_WithoutTurnInNpc_HidesTheArrow()
        {
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.ForCandidate(Ready(null)).Kind);
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.ForCandidate(Ready("")).Kind);
        }

        [Test]
        public void StatusesOtherThanActiveAndReady_GiveNone()
        {
            foreach (QuestStatus status in Enum.GetValues(typeof(QuestStatus)))
            {
                if (status == QuestStatus.Active || status == QuestStatus.ReadyToTurnIn) continue;
                var quest = new QuestTargetCandidate("q", status, QuestObjectiveType.Talk, "npc.elder", "npc.elder");
                Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.ForCandidate(quest).Kind, status.ToString());
            }
        }

        [Test]
        public void Select_PicksTheFirstQuestThatGivesATarget_InListOrder()
        {
            var list = new List<QuestTargetCandidate>
            {
                new QuestTargetCandidate("done", QuestStatus.Active, QuestObjectiveType.Talk, "", null), // không cho mục tiêu: bỏ qua
                Active(QuestObjectiveType.Kill, "slime", id: "first"),
                Active(QuestObjectiveType.Talk, "npc.other", id: "second"),
            };

            QuestTarget t = QuestTargetRules.Select(list);
            Assert.AreEqual(QuestTargetKind.NearestFarm, t.Kind);
        }

        [Test]
        public void Select_SkipsSelfCompletingReadyQuest_AndUsesTheNextOne()
        {
            var list = new List<QuestTargetCandidate>
            {
                Ready(null),
                Active(QuestObjectiveType.Reach, "zone.floor_01_boss_room")
            };

            QuestTarget t = QuestTargetRules.Select(list);
            Assert.AreEqual(QuestTargetKind.Zone, t.Kind);
            Assert.AreEqual("zone.floor_01_boss_room", t.Id);
        }

        [Test]
        public void Select_AllWithoutTargets_GivesNone()
        {
            var list = new List<QuestTargetCandidate> { Ready(null), Active(QuestObjectiveType.Talk, "") };
            Assert.AreEqual(QuestTargetKind.None, QuestTargetRules.Select(list).Kind);
        }

        // ── Vùng Farm gần nhất ───────────────────────────────────────────────

        [Test]
        public void NearestIndex_PicksTheClosestPoint()
        {
            var points = new List<Vector2> { new Vector2(10f, 0f), new Vector2(3f, 4f), new Vector2(-20f, 5f) };
            Assert.AreEqual(1, QuestTargetRules.NearestIndex(Vector2.zero, points));
            Assert.AreEqual(2, QuestTargetRules.NearestIndex(new Vector2(-18f, 5f), points));
        }

        [Test]
        public void NearestIndex_EmptyOrNull_IsMinusOne()
        {
            Assert.AreEqual(-1, QuestTargetRules.NearestIndex(Vector2.zero, null));
            Assert.AreEqual(-1, QuestTargetRules.NearestIndex(Vector2.zero, new List<Vector2>()));
        }

        [Test]
        public void NearestIndex_Tie_PicksTheFirst()
        {
            var points = new List<Vector2> { new Vector2(5f, 0f), new Vector2(-5f, 0f) };
            Assert.AreEqual(0, QuestTargetRules.NearestIndex(Vector2.zero, points));
        }

        [Test]
        public void NearestIndex_SkipsNaNPoints()
        {
            var points = new List<Vector2> { new Vector2(float.NaN, 0f), new Vector2(7f, 0f) };
            Assert.AreEqual(1, QuestTargetRules.NearestIndex(Vector2.zero, points));
        }

        [Test]
        public void NearestIndex_SinglePoint_IsThatPoint()
        {
            Assert.AreEqual(0, QuestTargetRules.NearestIndex(new Vector2(99f, 99f), new List<Vector2> { Vector2.zero }));
        }
    }
}
