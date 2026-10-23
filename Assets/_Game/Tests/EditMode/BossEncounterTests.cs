using System;
using System.Reflection;
using Game.Core.Combat;
using Game.PvE.Boss;
using Game.PvE.Zones;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    /// <summary>Phase 9.7b — luật thuần của phòng boss (BossEncounterRules, ZonePresenceFilter).</summary>
    public class BossEncounterRulesTests
    {
        [Test]
        public void Idle_WithZone_PlayerOutsideButInDetectionRange_DoesNotEngage()
        {
            Assert.AreEqual(BossEncounterDecision.Stay,
                BossEncounterRules.Decide(BossEncounterPhase.Idle, true, playerInZone: false, playerInDetectionRange: true),
                "Hành lang/tiền sảnh nằm trong DetectionRange cũng không kích hoạt");
        }

        [Test]
        public void Idle_WithZone_PlayerInside_Engages_EvenWhenOutsideDetectionRange()
        {
            Assert.AreEqual(BossEncounterDecision.Engage,
                BossEncounterRules.Decide(BossEncounterPhase.Idle, true, playerInZone: true, playerInDetectionRange: false));
        }

        [Test]
        public void Idle_WithoutZone_KeepsLegacyDetectionRangeBehaviour()
        {
            Assert.AreEqual(BossEncounterDecision.Engage,
                BossEncounterRules.Decide(BossEncounterPhase.Idle, false, playerInZone: false, playerInDetectionRange: true));
            Assert.AreEqual(BossEncounterDecision.Stay,
                BossEncounterRules.Decide(BossEncounterPhase.Idle, false, playerInZone: false, playerInDetectionRange: false));
        }

        [Test]
        public void Combat_WithZone_LeavingEndsTheEncounter_StayingDoesNot()
        {
            Assert.AreEqual(BossEncounterDecision.ReturnHome,
                BossEncounterRules.Decide(BossEncounterPhase.Combat, true, playerInZone: false, playerInDetectionRange: true));
            Assert.AreEqual(BossEncounterDecision.Stay,
                BossEncounterRules.Decide(BossEncounterPhase.Combat, true, playerInZone: true, playerInDetectionRange: true));
        }

        [Test]
        public void Combat_WithoutZone_NeverReturnsHome()
        {
            Assert.AreEqual(BossEncounterDecision.Stay,
                BossEncounterRules.Decide(BossEncounterPhase.Combat, false, playerInZone: false, playerInDetectionRange: false));
        }

        [Test]
        public void ReturningHome_PlayerEntersAgain_EngagesImmediately_OtherwiseKeepsGoing()
        {
            Assert.AreEqual(BossEncounterDecision.Engage,
                BossEncounterRules.Decide(BossEncounterPhase.ReturningHome, true, playerInZone: true, playerInDetectionRange: true));
            Assert.AreEqual(BossEncounterDecision.Stay,
                BossEncounterRules.Decide(BossEncounterPhase.ReturningHome, true, playerInZone: false, playerInDetectionRange: true));
        }

        [Test]
        public void Dead_AlwaysStays()
        {
            foreach (bool inZone in new[] { true, false })
            {
                foreach (bool inRange in new[] { true, false })
                {
                    Assert.AreEqual(BossEncounterDecision.Stay, BossEncounterRules.Decide(BossEncounterPhase.Dead, true, inZone, inRange));
                    Assert.AreEqual(BossEncounterDecision.Stay, BossEncounterRules.Decide(BossEncounterPhase.Dead, false, inZone, inRange));
                }
            }
        }

        [Test]
        public void PressureCounter_InterruptedDuringWindupOrDash_BecomesDueAgain()
        {
            Assert.AreEqual(2, BossEncounterRules.PressureCounterAfterInterrupt(0, 2, true));
            Assert.AreEqual(3, BossEncounterRules.PressureCounterAfterInterrupt(3, 2, true), "Không bao giờ giảm bộ đếm");
        }

        [Test]
        public void PressureCounter_OtherInterruptions_AreUnchanged()
        {
            Assert.AreEqual(1, BossEncounterRules.PressureCounterAfterInterrupt(1, 2, false));
            Assert.AreEqual(0, BossEncounterRules.PressureCounterAfterInterrupt(0, 2, false));
        }

        [Test]
        public void ReengageCooldown_NeverShorterThanTheRemainingTimer()
        {
            Assert.AreEqual(1.2f, BossEncounterRules.ReengageCooldown(0f, 1.2f), 1e-5f);
            Assert.AreEqual(2.0f, BossEncounterRules.ReengageCooldown(2.0f, 1.2f), 1e-5f);
        }

        [Test]
        public void HomeArrivalAndTimeout_Boundaries()
        {
            Assert.IsTrue(BossEncounterRules.HasArrivedHome(BossEncounterRules.ReturnArriveDistance));
            Assert.IsFalse(BossEncounterRules.HasArrivedHome(BossEncounterRules.ReturnArriveDistance + 0.01f));
            Assert.IsFalse(BossEncounterRules.ReturnTimedOut(BossEncounterRules.ReturnMaxSeconds - 0.01f));
            Assert.IsTrue(BossEncounterRules.ReturnTimedOut(BossEncounterRules.ReturnMaxSeconds));
        }

        // ── ZonePresenceFilter ───────────────────────────────────────────────────

        [Test]
        public void Presence_StartsOutside_EntersImmediately()
        {
            var f = new ZonePresenceFilter(0.5f);
            Assert.IsFalse(f.Inside);
            Assert.IsTrue(f.Update(true, 0.016f));
        }

        [Test]
        public void Presence_LeavingNeedsToStayOutsideForTheDelay()
        {
            var f = new ZonePresenceFilter(0.5f);
            f.Update(true, 0.016f);

            Assert.IsTrue(f.Update(false, 0.3f), "Mới ra ngoài 0.3 s: vẫn tính là trong phòng");
            Assert.IsFalse(f.Update(false, 0.3f), "Đủ 0.6 s liên tục: đã rời");
        }

        [Test]
        public void Presence_BoundaryFlicker_NeverEndsTheEncounter()
        {
            var f = new ZonePresenceFilter(0.5f);
            f.Update(true, 0.016f);

            for (int i = 0; i < 50; i++)
            {
                f.Update(false, 0.2f);          // ra 0.2 s
                Assert.IsTrue(f.Update(true, 0.016f), "Quay vào trước khi đủ 0.5 s thì bộ đếm ra-ngoài bị xóa");
            }
        }

        [Test]
        public void Presence_NegativeOrZeroDelta_DoesNotAdvanceTheLeaveTimer()
        {
            var f = new ZonePresenceFilter(0.5f);
            f.Update(true, 0.016f);

            for (int i = 0; i < 10; i++) Assert.IsTrue(f.Update(false, -1f));
            for (int i = 0; i < 10; i++) Assert.IsTrue(f.Update(false, 0f));
        }

        [Test]
        public void Presence_ZeroDelay_LeavesAtOnce_AndResetClearsState()
        {
            var f = new ZonePresenceFilter(0f);
            f.Update(true, 0.016f);
            Assert.IsFalse(f.Update(false, 0.016f));

            var g = new ZonePresenceFilter(0.5f);
            g.Update(true, 0.016f);
            g.Reset();
            Assert.IsFalse(g.Inside);
        }
    }

    /// <summary>
    /// Phase 9.7b — BossController thật (không cần scene): kích hoạt theo phòng, rời phòng giữa trận, vào lại, reset theo tầng.
    /// Dựng ở trạng thái tắt rồi gọi Awake bằng reflection (giống BossHitRulesTests), điều khiển bằng StepEncounter.
    /// </summary>
    public class BossEncounterControllerTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private Rig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new Rig();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void PlayerOutsideRoom_InsideDetectionRange_BossStaysIdle()
        {
            _rig.MovePlayer(_rig.OutsideNearBoss);

            _rig.Step(0.1f);
            _rig.Step(0.1f);

            Assert.AreEqual("IdleState", _rig.StateName);
            Assert.IsFalse(_rig.Boss.IsEngaged);
        }

        [Test]
        public void PlayerEntersRoom_BossEngages()
        {
            _rig.MovePlayer(_rig.InsideRoom);

            _rig.Step(0.1f); // Idle → Chase
            Assert.AreEqual("ChaseState", _rig.StateName);
            _rig.Step(0.1f); // Chase.Tick báo vào trận
            Assert.IsTrue(_rig.Boss.IsEngaged);
        }

        [Test]
        public void LeavingRoomDuringTelegraph_ReturnsHome_KeepsHealthAndEnrage_AndDealsNoDamage()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);
            _rig.SetHealth(37.0, enraged: true);
            _rig.GoTo("TelegraphState");
            _rig.PullBossAwayFromHome();

            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.3f);
            Assert.AreEqual("TelegraphState", _rig.StateName, "Mới ra ngoài 0.3 s: chưa tính là rời phòng");
            _rig.Step(0.3f);

            Assert.AreEqual("ReturnHomeState", _rig.StateName);
            Assert.AreEqual(37.0, _rig.Boss.CurrentHealth, 1e-6, "Máu giữ nguyên, không hồi");
            Assert.IsTrue(_rig.Boss.IsEnraged, "Enrage giữ nguyên");
            Assert.IsFalse(_rig.Boss.IsEngaged, "Trận đã kết thúc");
            Assert.AreEqual(0, _rig.Player.HitCount, "Không còn damage nào sau khi rời phòng");
        }

        [Test]
        public void LeavingRoomDuringPressureDash_CancelsTheDash_ResetsGate_AndKeepsPressureDue()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.GoTo("PressureTelegraphState");
            _rig.GoTo("PressureDashState");
            _rig.PullBossAwayFromHome();
            _rig.Boss.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(5f, 0f);

            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.6f);

            Assert.AreEqual("ReturnHomeState", _rig.StateName);
            Assert.AreEqual(0, _rig.Player.HitCount, "Không còn damage nào sau khi rời phòng");
            Assert.IsFalse(_rig.PressureGateHasHit, "Cổng đòn lao được reset");
            Assert.IsTrue(_rig.Boss.IsPressureDue, "Rời phòng không hoãn được Pressure");

            // Và trong lúc đi về nhà, không có đòn lao nào nữa dù người chơi nằm trên đường cũ.
            _rig.MovePlayer(_rig.Boss.transform.position + new Vector3(1f, 0f, 0f) + Vector3.forward * 0f);
            for (int i = 0; i < 5; i++) _rig.Step(0.1f);
            Assert.AreEqual(0, _rig.Player.HitCount);
        }

        [Test]
        public void LeavingRoomDuringNormalChase_DoesNotChangePressureCounter()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);
            int before = _rig.Boss.NormalAttacksSincePressure;
            _rig.PullBossAwayFromHome();

            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.6f);

            Assert.AreEqual("ReturnHomeState", _rig.StateName);
            Assert.AreEqual(before, _rig.Boss.NormalAttacksSincePressure);
        }

        [Test]
        public void BoundaryFlicker_DoesNotEndTheEncounter()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);

            for (int i = 0; i < 20; i++)
            {
                _rig.MovePlayer(_rig.FarOutside);
                _rig.Step(0.2f);
                _rig.MovePlayer(_rig.InsideRoom);
                _rig.Step(0.05f);
            }

            Assert.AreNotEqual("ReturnHomeState", _rig.StateName);
            Assert.IsTrue(_rig.Boss.IsEngaged);
        }

        [Test]
        public void ReenteringWhileReturningHome_ChasesAgain_WithTheSameHealth()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);
            _rig.SetHealth(60.0, enraged: false);
            _rig.PullBossAwayFromHome();
            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.6f);
            Assert.AreEqual("ReturnHomeState", _rig.StateName);

            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.05f);

            Assert.AreEqual("ChaseState", _rig.StateName, "Vào lại phòng: Chase ngay, không đợi về tới nhà");
            Assert.AreEqual(60.0, _rig.Boss.CurrentHealth, 1e-6);
        }

        [Test]
        public void ReturnHome_ArrivesAtHome_ThenIdle_AndEngagesAgainOnReentry()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);
            _rig.PullBossAwayFromHome();
            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.6f);
            Assert.AreEqual("ReturnHomeState", _rig.StateName);

            _rig.Boss.transform.position = _rig.Home; // coi như đã đi về tới nhà
            _rig.Step(0.1f);
            Assert.AreEqual("IdleState", _rig.StateName);

            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            Assert.AreEqual("ChaseState", _rig.StateName);
        }

        [Test]
        public void WithoutEncounterZone_LegacyDetectionRangeStillWorks_AndNeverReturnsHome()
        {
            _rig.SetEncounterZone(null);

            _rig.MovePlayer(_rig.Home + new Vector3(_rig.Definition.DetectionRange + 5f, 0f, 0f));
            _rig.Step(0.1f);
            Assert.AreEqual("IdleState", _rig.StateName, "Ngoài DetectionRange: đứng yên");

            _rig.MovePlayer(_rig.Home + new Vector3(_rig.Definition.DetectionRange - 0.5f, 0f, 0f));
            _rig.Step(0.1f);
            Assert.AreEqual("ChaseState", _rig.StateName, "Trong DetectionRange: vào trận như Phase 8");

            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(2f);
            Assert.AreNotEqual("ReturnHomeState", _rig.StateName, "Không có phòng thì không bao giờ tự rời trận");
        }

        [Test]
        public void ResetEncounter_FromReturnHome_RestoresFullHealthAndIdle()
        {
            _rig.MovePlayer(_rig.InsideRoom);
            _rig.Step(0.1f);
            _rig.Step(0.1f);
            _rig.SetHealth(20.0, enraged: true);
            _rig.PullBossAwayFromHome();
            _rig.MovePlayer(_rig.FarOutside);
            _rig.Step(0.6f);
            Assert.AreEqual("ReturnHomeState", _rig.StateName);

            _rig.Boss.ResetEncounter(); // vào lại tầng: reset theo tầng, khác với rời phòng

            Assert.AreEqual(_rig.Boss.MaxHealth, _rig.Boss.CurrentHealth, 1e-6);
            Assert.IsFalse(_rig.Boss.IsEnraged);
            Assert.AreEqual("IdleState", _rig.StateName);
        }

        // ── Dàn dựng ────────────────────────────────────────────────────────────

        private sealed class TestPlayer : MonoBehaviour, IDamageable, ICombatStats
        {
            public int HitCount;
            public double CurrentHealth { get; private set; } = 1000.0;
            public double MaxHealth => 1000.0;
            public bool IsDead => CurrentHealth <= 0.0;
            public double Attack => 0.0;
            public double Defense => 0.0;
            public double CriticalChance => 0.0;
            public double CriticalDamage => 1.0;

            public void TakeDamage(DamageInfo damage)
            {
                HitCount++;
                CurrentHealth -= damage.Amount;
            }

            public void Heal(double amount) { }
            public void SetMaxHealth(double maxHp, bool resetCurrent = false) { }
        }

        private sealed class Rig : IDisposable
        {
            // Phòng boss: hộp 10×10 tâm (70, -11), tức x từ 65 tới 75. Boss đứng sát mép trái (home = (67.5, -11.5)).
            public readonly Vector3 Home = new Vector3(67.5f, -11.5f, 0f);
            public readonly Vector3 InsideRoom = new Vector3(70f, -11f, 0f);
            public readonly Vector3 OutsideNearBoss = new Vector3(62f, -11.5f, 0f); // ngoài phòng, cách boss 5.5 (trong DetectionRange mặc định)
            public readonly Vector3 FarOutside = new Vector3(40f, -11.5f, 0f);

            public readonly GameObject BossObject;
            public readonly GameObject PlayerObject;
            public readonly GameObject ZoneObject;
            public readonly BossController Boss;
            public readonly TestPlayer Player;
            public readonly BossDefinition Definition;

            public Rig()
            {
                Definition = ScriptableObject.CreateInstance<BossDefinition>();

                ZoneObject = new GameObject("BossEncounterTests_Zone");
                ZoneObject.transform.position = new Vector3(70f, -11f, 0f);
                var box = ZoneObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(10f, 10f);
                var zone = ZoneObject.AddComponent<ZoneVolume>();
                zone.Configure("zone.test_boss_room", "Phòng boss thử", ZoneKind.BossRoom, 10, null);

                BossObject = new GameObject("BossEncounterTests_Boss");
                BossObject.SetActive(false); // để Awake/Start không tự chạy; gọi tay bên dưới
                BossObject.transform.position = Home;
                Boss = BossObject.AddComponent<BossController>();
                Boss.SetDefinition(Definition);
                SetEncounterZone(zone);
                typeof(BossController).GetMethod("Awake", Private).Invoke(Boss, null);

                PlayerObject = new GameObject("BossEncounterTests_Player");
                Player = PlayerObject.AddComponent<TestPlayer>();
                typeof(BossController).GetField("_target", Private).SetValue(Boss, PlayerObject.transform);
                MovePlayer(FarOutside);
            }

            public string StateName
            {
                get
                {
                    object machine = typeof(BossController).GetField("_machine", Private).GetValue(Boss);
                    Type type = (Type)machine.GetType().GetProperty("CurrentStateType").GetValue(machine);
                    return type != null ? type.Name : null;
                }
            }

            public bool PressureGateHasHit
            {
                get
                {
                    object gate = typeof(BossController).GetField("_pressureHitGate", Private).GetValue(Boss);
                    return (bool)gate.GetType().GetProperty("HasHit").GetValue(gate);
                }
            }

            public void SetEncounterZone(ZoneVolume zone)
            {
                typeof(BossController).GetField("encounterZone", Private).SetValue(Boss, zone);
            }

            public void MovePlayer(Vector3 position)
            {
                PlayerObject.transform.position = position;
                Physics2D.SyncTransforms();
            }

            /// <summary>Boss đang đứng ở nhà thì "về nhà" xong ngay; kéo ra xa để thấy được state ReturnHome (vật lý không chạy trong EditMode).</summary>
            public void PullBossAwayFromHome()
            {
                BossObject.transform.position = Home + new Vector3(4f, 0f, 0f);
            }

            public void Step(float seconds)
            {
                typeof(BossController).GetMethod("StepEncounter", Private).Invoke(Boss, new object[] { seconds });
            }

            public void SetHealth(double health, bool enraged)
            {
                typeof(BossController).GetField("_health", Private).SetValue(Boss, health);
                typeof(BossController).GetField("_isEnraged", Private).SetValue(Boss, enraged);
            }

            /// <summary>Đưa state machine tới một state lồng bên trong BossController (theo tên).</summary>
            public void GoTo(string stateTypeName)
            {
                object machine = typeof(BossController).GetField("_machine", Private).GetValue(Boss);
                Type state = typeof(BossController).GetNestedType(stateTypeName, BindingFlags.NonPublic);
                Assert.IsNotNull(state, "Không thấy state " + stateTypeName);
                MethodInfo change = machine.GetType().GetMethod("ChangeTo").MakeGenericMethod(state);
                change.Invoke(machine, null);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(BossObject);
                Object.DestroyImmediate(PlayerObject);
                Object.DestroyImmediate(ZoneObject);
                Object.DestroyImmediate(Definition);
            }
        }
    }
}
