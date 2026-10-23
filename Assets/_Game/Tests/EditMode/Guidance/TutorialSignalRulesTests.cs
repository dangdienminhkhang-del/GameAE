using System;
using System.Collections.Generic;
using Game.Core.Guidance;
using Game.Core.Skills;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Các cảm biến thuần của hướng dẫn (Phase 9.9): bắt cạnh Safe Zone, máu thấp có trễ, di chuyển liên tục,
    /// tín hiệu mở khóa kỹ thuật, floor_select chỉ khi bảng boss đã đóng và đã xác nhận, phát hiện mặc đồ, chọn hint theo trigger.
    /// </summary>
    public class TutorialSignalRulesTests
    {
        // ── Cạnh Safe Zone ───────────────────────────────────────────────────

        [Test]
        public void SafeZoneEdge_FirstSampleOnlyRemembers_NoEdge()
        {
            var detector = new SafeZoneEdgeDetector();
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(true), "bắt đầu game đứng trong Safe Zone không tính là 'vào'");
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(true));
        }

        [Test]
        public void SafeZoneEdge_LeftFiresExactlyOnce()
        {
            var detector = new SafeZoneEdgeDetector();
            detector.Update(true);

            Assert.AreEqual(SafeZoneEdge.Left, detector.Update(false));
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(false));
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(false));
        }

        [Test]
        public void SafeZoneEdge_EnteredFiresExactlyOnce_AfterLeaving()
        {
            var detector = new SafeZoneEdgeDetector();
            detector.Update(true);
            detector.Update(false);

            Assert.AreEqual(SafeZoneEdge.Entered, detector.Update(true));
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(true));
        }

        [Test]
        public void SafeZoneEdge_EachCrossingFiresOnce_OverARoundTrip()
        {
            var detector = new SafeZoneEdgeDetector();
            var edges = new List<SafeZoneEdge>();
            foreach (bool inside in new[] { true, true, false, false, true, true, false, true })
            {
                SafeZoneEdge e = detector.Update(inside);
                if (e != SafeZoneEdge.None) edges.Add(e);
            }

            CollectionAssert.AreEqual(
                new[] { SafeZoneEdge.Left, SafeZoneEdge.Entered, SafeZoneEdge.Left, SafeZoneEdge.Entered }, edges);
        }

        [Test]
        public void SafeZoneEdge_StartingOutside_FirstSampleIsNotAnEdge_ThenEnterFires()
        {
            var detector = new SafeZoneEdgeDetector();
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(false));
            Assert.AreEqual(SafeZoneEdge.Entered, detector.Update(true));
        }

        [Test]
        public void SafeZoneEdge_Reset_ForgetsTheState()
        {
            var detector = new SafeZoneEdgeDetector();
            detector.Update(true);
            detector.Reset();
            Assert.AreEqual(SafeZoneEdge.None, detector.Update(false), "sau Reset mẫu đầu lại chỉ ghi nhớ");
        }

        // ── Máu thấp có trễ ──────────────────────────────────────────────────

        [Test]
        public void LowHealth_FiresOnceWhenCrossingTheThreshold()
        {
            var gate = new LowHealthGate(0.35f, 0.5f);

            Assert.IsFalse(gate.Update(80, 100));
            Assert.IsFalse(gate.Update(40, 100));
            Assert.IsTrue(gate.Update(34, 100));
            Assert.IsFalse(gate.Update(30, 100), "đang thấp: không bắn lặp");
            Assert.IsFalse(gate.Update(10, 100));
        }

        [Test]
        public void LowHealth_DoesNotRepeat_WhenHealthWobblesAroundTheThreshold()
        {
            var gate = new LowHealthGate(0.35f, 0.5f);
            int fired = 0;
            double[] samples = { 34, 36, 33, 38, 34, 45, 35, 49, 34 };
            foreach (double hp in samples) if (gate.Update(hp, 100)) fired++;

            Assert.AreEqual(1, fired, "dao động quanh ngưỡng (chưa hồi tới 50%) chỉ bắn một lần");
        }

        [Test]
        public void LowHealth_RearmsOnlyAfterRecoveringToTheRearmRatio()
        {
            var gate = new LowHealthGate(0.35f, 0.5f);
            Assert.IsTrue(gate.Update(30, 100));
            Assert.IsFalse(gate.Update(49, 100));
            Assert.IsFalse(gate.Update(30, 100), "chưa hồi đủ nên chưa nạp lại");

            Assert.IsFalse(gate.Update(60, 100)); // hồi đủ → nạp lại
            Assert.IsTrue(gate.Update(30, 100));
        }

        [Test]
        public void LowHealth_DeadOrBadInput_NeverFires()
        {
            var gate = new LowHealthGate(0.35f, 0.5f);
            Assert.IsFalse(gate.Update(0, 100), "máu 0 (đã chết) không bắn");
            Assert.IsFalse(gate.Update(10, 0));
            Assert.IsFalse(gate.Update(10, -5));
            Assert.IsFalse(gate.Update(double.NaN, 100));
            Assert.IsFalse(gate.Update(10, double.NaN));
        }

        [Test]
        public void LowHealth_RearmBelowTriggerIsRaisedToTrigger()
        {
            var gate = new LowHealthGate(0.4f, 0.1f); // cấu hình sai: nạp lại thấp hơn ngưỡng bắn
            Assert.IsTrue(gate.Update(30, 100));
            Assert.IsFalse(gate.Update(30, 100));
            Assert.IsFalse(gate.Update(41, 100)); // hồi qua ngưỡng nạp lại (đã nâng lên 0.4)
            Assert.IsTrue(gate.Update(30, 100));
        }

        [Test]
        public void LowHealth_Reset_ArmsAgain()
        {
            var gate = new LowHealthGate(0.35f, 0.5f);
            gate.Update(30, 100);
            gate.Reset();
            Assert.IsTrue(gate.Update(30, 100));
        }

        // ── Di chuyển liên tục ───────────────────────────────────────────────

        [Test]
        public void MoveStreak_FiresAfterEnoughConsecutiveMovingTicks_Once()
        {
            var detector = new MoveStreakDetector(2, 0.3f);
            const float dt = 0.25f;

            Assert.IsFalse(detector.Update(0.5f, dt));
            Assert.IsTrue(detector.Update(0.5f, dt));
            Assert.IsFalse(detector.Update(0.5f, dt), "chỉ báo một lần");
        }

        [Test]
        public void MoveStreak_StandingStillResetsTheStreak()
        {
            var detector = new MoveStreakDetector(3, 0.3f);
            const float dt = 0.25f;

            detector.Update(0.5f, dt);
            detector.Update(0.5f, dt);
            Assert.IsFalse(detector.Update(0f, dt), "đứng lại: chuỗi về 0");
            Assert.IsFalse(detector.Update(0.5f, dt));
            Assert.IsFalse(detector.Update(0.5f, dt));
            Assert.IsTrue(detector.Update(0.5f, dt));
        }

        [Test]
        public void MoveStreak_SlowDrift_DoesNotCount()
        {
            var detector = new MoveStreakDetector(2, 0.3f);
            Assert.IsFalse(detector.Update(0.01f, 0.25f));
            Assert.IsFalse(detector.Update(0.01f, 0.25f));
        }

        [Test]
        public void MoveStreak_BadDelta_IsIgnored()
        {
            var detector = new MoveStreakDetector(1, 0.3f);
            Assert.IsFalse(detector.Update(5f, 0f));
            Assert.IsFalse(detector.Update(5f, -1f));
            Assert.IsFalse(detector.Update(float.NaN, 0.25f));
            Assert.IsTrue(detector.Update(5f, 0.25f));
        }

        [Test]
        public void MoveStreak_Reset_AllowsFiringAgain()
        {
            var detector = new MoveStreakDetector(1, 0.3f);
            Assert.IsTrue(detector.Update(1f, 0.25f));
            detector.Reset();
            Assert.IsTrue(detector.Update(1f, 0.25f));
        }

        [Test]
        public void MoveStreak_NonPositiveConfirmTicks_BehavesAsOne()
        {
            var detector = new MoveStreakDetector(0, 0.3f);
            Assert.IsTrue(detector.Update(1f, 0.25f));
        }

        // ── Tín hiệu mở khóa kỹ thuật ────────────────────────────────────────

        [Test]
        public void TechniqueUnlock_LockedReasons_MeanNotUnlocked()
        {
            Assert.IsFalse(TechniqueUnlockRules.IsUnlocked(TechniqueValidationError.NoWeaponEquipped));
            Assert.IsFalse(TechniqueUnlockRules.IsUnlocked(TechniqueValidationError.DefinitionMissing));
            Assert.IsFalse(TechniqueUnlockRules.IsUnlocked(TechniqueValidationError.LockedMastery));
            Assert.IsFalse(TechniqueUnlockRules.IsUnlocked(TechniqueValidationError.WrongDiscipline));
        }

        [Test]
        public void TechniqueUnlock_EveryOtherReason_MeansUnlocked_BusyOrCooldownStillCount()
        {
            var locked = new HashSet<TechniqueValidationError>
            {
                TechniqueValidationError.NoWeaponEquipped,
                TechniqueValidationError.DefinitionMissing,
                TechniqueValidationError.LockedMastery,
                TechniqueValidationError.WrongDiscipline
            };

            foreach (TechniqueValidationError reason in Enum.GetValues(typeof(TechniqueValidationError)))
            {
                if (locked.Contains(reason)) continue;
                Assert.IsTrue(TechniqueUnlockRules.IsUnlocked(reason), reason.ToString());
            }
        }

        [Test]
        public void RisingEdge_FiresOnlyWhenGoingFromLockedToUnlocked()
        {
            var edge = new RisingEdgeDetector();

            Assert.IsFalse(edge.Update(false));
            Assert.IsTrue(edge.Update(true));
            Assert.IsFalse(edge.Update(true));
            Assert.IsFalse(edge.Update(false));
            Assert.IsTrue(edge.Update(true), "khóa lại rồi mở lại thì báo lại");
        }

        [Test]
        public void RisingEdge_AlreadyUnlockedAtFirstSample_FiresOnce()
        {
            var edge = new RisingEdgeDetector();
            Assert.IsTrue(edge.Update(true));
            Assert.IsFalse(edge.Update(true));
        }

        [Test]
        public void RisingEdge_Reset_ClearsMemory()
        {
            var edge = new RisingEdgeDetector();
            edge.Update(true);
            edge.Reset();
            Assert.IsTrue(edge.Update(true));
        }

        // ── floor_select: boss hạ + xác nhận + bảng đã đóng ──────────────────

        [Test]
        public void FloorUnlock_NeedsDefeatConfirmAndClosedPanel_AllThree()
        {
            var gate = new FloorUnlockGate();

            Assert.IsFalse(gate.Update(true), "chưa có gì");

            gate.MarkDefeated();
            Assert.IsFalse(gate.Update(true), "chưa xác nhận");

            gate.MarkConfirmed();
            Assert.IsFalse(gate.Update(false), "bảng boss còn mở: chưa phát");
            Assert.IsTrue(gate.Update(true), "đủ ba điều kiện");
        }

        [Test]
        public void FloorUnlock_ConfirmWithoutDefeat_DoesNotFire()
        {
            var gate = new FloorUnlockGate();
            gate.MarkConfirmed();
            Assert.IsFalse(gate.Update(true));
        }

        [Test]
        public void FloorUnlock_FiresOnce_ThenRearmsForTheNextBoss()
        {
            var gate = new FloorUnlockGate();
            gate.MarkDefeated();
            gate.MarkConfirmed();

            Assert.IsTrue(gate.Update(true));
            Assert.IsFalse(gate.Update(true), "không phát lặp");

            gate.MarkDefeated();
            gate.MarkConfirmed();
            Assert.IsTrue(gate.Update(true));
        }

        [Test]
        public void FloorUnlock_PanelClosesLater_StillFiresOnceItCloses()
        {
            var gate = new FloorUnlockGate();
            gate.MarkDefeated();
            gate.MarkConfirmed();

            Assert.IsFalse(gate.Update(false));
            Assert.IsFalse(gate.Update(false));
            Assert.IsTrue(gate.Update(true));
        }

        [Test]
        public void FloorUnlock_Reset_ForgetsEverything()
        {
            var gate = new FloorUnlockGate();
            gate.MarkDefeated();
            gate.MarkConfirmed();
            gate.Reset();
            Assert.IsFalse(gate.Update(true));
        }

        // ── Mặc đồ ───────────────────────────────────────────────────────────

        [Test]
        public void Equip_FirstSampleOnlyRemembers()
        {
            var detector = new EquipChangeDetector();
            Assert.IsFalse(detector.Update("sword-1", "armor-1"));
        }

        [Test]
        public void Equip_NewNonEmptyInstanceInASlot_Fires()
        {
            var detector = new EquipChangeDetector();
            detector.Update(null, null);

            Assert.IsTrue(detector.Update("sword-1", null));
            Assert.IsFalse(detector.Update("sword-1", null));
            Assert.IsTrue(detector.Update("sword-1", "armor-1"));
        }

        [Test]
        public void Equip_SwappingToADifferentInstance_Fires()
        {
            var detector = new EquipChangeDetector();
            detector.Update("sword-1", null);
            Assert.IsTrue(detector.Update("sword-2", null));
        }

        [Test]
        public void Equip_TakingOffIsNotEquipping()
        {
            var detector = new EquipChangeDetector();
            detector.Update("sword-1", "armor-1");

            Assert.IsFalse(detector.Update(null, "armor-1"));
            Assert.IsFalse(detector.Update("", ""));
        }

        [Test]
        public void Equip_Reset_ForgetsTheSlots()
        {
            var detector = new EquipChangeDetector();
            detector.Update("sword-1", null);
            detector.Reset();
            Assert.IsFalse(detector.Update("sword-2", null));
        }

        // ── Chọn hint theo trigger ───────────────────────────────────────────

        private static TutorialHintDefinition MakeHint(string id, TutorialTrigger trigger, TutorialComplete complete, int priority = 0)
        {
            var definition = UnityEngine.ScriptableObject.CreateInstance<TutorialHintDefinition>();
            definition.Configure(id, "chữ", priority, trigger, complete);
            return definition;
        }

        [Test]
        public void TriggerMap_CollectsOnlyHintsOfThatTrigger()
        {
            TutorialHintDefinition a = MakeHint("hint.attack", TutorialTrigger.LeftSafeZone, TutorialComplete.HitLanded);
            TutorialHintDefinition b = MakeHint("hint.safe_zone", TutorialTrigger.EnteredSafeZone, TutorialComplete.None);
            TutorialHintDefinition c = MakeHint("hint.never", TutorialTrigger.None, TutorialComplete.None);
            try
            {
                var output = new List<TutorialHint>();
                TutorialTriggerMap.Collect(new[] { a, b, c }, TutorialTrigger.LeftSafeZone, output);

                Assert.AreEqual(1, output.Count);
                Assert.AreEqual("hint.attack", output[0].Id);
                Assert.AreEqual(TutorialComplete.HitLanded, output[0].CompleteKind);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
                UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void TriggerMap_NoneTrigger_AndNullInputs_CollectNothing()
        {
            TutorialHintDefinition c = MakeHint("hint.never", TutorialTrigger.None, TutorialComplete.None);
            try
            {
                var output = new List<TutorialHint>();
                TutorialTriggerMap.Collect(new[] { c }, TutorialTrigger.None, output);
                TutorialTriggerMap.Collect(null, TutorialTrigger.LeftSafeZone, output);
                TutorialTriggerMap.Collect(new[] { c }, TutorialTrigger.LeftSafeZone, null);
                TutorialTriggerMap.Collect(new TutorialHintDefinition[] { null }, TutorialTrigger.LeftSafeZone, output);

                Assert.AreEqual(0, output.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void TriggerMap_SeveralHintsOnTheSameTrigger_AreAllCollected()
        {
            TutorialHintDefinition a = MakeHint("hint.a", TutorialTrigger.ItemAcquired, TutorialComplete.None, 1);
            TutorialHintDefinition b = MakeHint("hint.b", TutorialTrigger.ItemAcquired, TutorialComplete.None, 2);
            try
            {
                var output = new List<TutorialHint>();
                TutorialTriggerMap.Collect(new[] { a, b }, TutorialTrigger.ItemAcquired, output);
                Assert.AreEqual(2, output.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }

        // ── Hint definition ──────────────────────────────────────────────────

        [Test]
        public void HintDefinition_Defaults_AreSystemSpeakerAndFourSeconds()
        {
            var definition = UnityEngine.ScriptableObject.CreateInstance<TutorialHintDefinition>();
            try
            {
                Assert.AreEqual("Hệ thống", definition.SpeakerLabel);
                Assert.AreEqual(4f, definition.DisplaySeconds, 1e-6f);
                Assert.AreEqual(70, TutorialHintDefinition.MaxTextLength);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void HintDefinition_BadDisplaySeconds_AreClampedUp()
        {
            var definition = UnityEngine.ScriptableObject.CreateInstance<TutorialHintDefinition>();
            try
            {
                definition.Configure("hint.x", "chữ", 0, TutorialTrigger.None, TutorialComplete.None, -3f, "");
                Assert.AreEqual(TutorialHintDefinition.MinDisplaySeconds, definition.DisplaySeconds, 1e-6f);
                Assert.AreEqual("Hệ thống", definition.SpeakerLabel, "nhãn trống dùng mặc định");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void GuidanceConfig_SensorsNeverExceedFourHz_AndRearmNeverBelowTrigger()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<GuidanceConfig>();
            try
            {
                Assert.LessOrEqual(config.SensorHz, GuidanceConfig.MaxSensorHz);
                Assert.GreaterOrEqual(config.SensorIntervalSeconds, 0.25f - 1e-6f);
                Assert.GreaterOrEqual(config.LowHealthRearmRatio, config.LowHealthRatio);
                Assert.AreEqual(0.35f, config.LowHealthRatio, 1e-6f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
