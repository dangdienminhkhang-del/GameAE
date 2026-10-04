using System.Collections.Generic;
using System.Reflection;
using Game.Core.Combat;
using Game.Core.Skills;
using Game.Core.Stats;
using Game.PvE.Skills;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Wave 2A — Weapon Technique unit tests.
    /// Các test bắt buộc theo docs/13 Wave 2A Required tests:
    ///   1. locked technique rejected
    ///   2. mastery unlock
    ///   3. cooldown
    ///   4. multi-hit total
    ///   5. duplicate hit prevention
    ///   6. save/load mastery
    /// </summary>
    [TestFixture]
    public class WeaponTechniqueTests
    {
        // ─────────────────────────────────────────────────────────────────
        // 1. Locked technique rejected
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void Validate_RejectsWhenMasteryBelowRequired()
        {
            WeaponTechniqueDefinition def = MakeBladeTechnique(TechniqueInputSlot.Core, masteryRequired: 30);

            TechniqueValidationResult result = TechniqueExecutor.Validate(
                def,
                WeaponDiscipline.Blade,
                currentMastery: 15,
                isCooldownReady: true);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(TechniqueValidationError.LockedMastery, result.Error);
            StringAssert.Contains("30", result.Message, "Phải nói rõ mastery cần bao nhiêu.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. Mastery unlock (khi >= masteryRequired thì pass)
        // ─────────────────────────────────────────────────────────────────
        [TestCase(25, 25, ExpectedResult = true)]
        [TestCase(25, 24, ExpectedResult = false)]
        [TestCase(0, 0, ExpectedResult = true)]
        [TestCase(85, 100, ExpectedResult = true)]
        public bool Validate_PassesWhenMasteryAtOrAboveRequired(int required, int current)
        {
            WeaponTechniqueDefinition def = MakeBladeTechnique(TechniqueInputSlot.Traverse, masteryRequired: required);
            TechniqueValidationResult result = TechniqueExecutor.Validate(
                def, WeaponDiscipline.Blade, current, isCooldownReady: true);
            return result.IsValid;
        }

        [Test]
        public void Validate_RejectsWrongDiscipline()
        {
            WeaponTechniqueDefinition blade = MakeBladeTechnique(TechniqueInputSlot.Core, masteryRequired: 0);
            TechniqueValidationResult result = TechniqueExecutor.Validate(
                blade, WeaponDiscipline.Knives, currentMastery: 50, isCooldownReady: true);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(TechniqueValidationError.WrongDiscipline, result.Error);
        }

        [Test]
        public void Validate_RejectsNoWeaponEquipped()
        {
            WeaponTechniqueDefinition blade = MakeBladeTechnique(TechniqueInputSlot.Core, masteryRequired: 0);
            TechniqueValidationResult result = TechniqueExecutor.Validate(
                blade, WeaponDiscipline.None, currentMastery: 0, isCooldownReady: true);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(TechniqueValidationError.NoWeaponEquipped, result.Error);
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. Cooldown
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void Validate_RejectsWhenCooldownNotReady()
        {
            WeaponTechniqueDefinition def = MakeBladeTechnique(TechniqueInputSlot.Core, masteryRequired: 0);
            TechniqueValidationResult result = TechniqueExecutor.Validate(
                def, WeaponDiscipline.Blade, currentMastery: 10, isCooldownReady: false);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(TechniqueValidationError.Cooldown, result.Error);
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. Multi-hit total
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void CalculateTotalMultiplier_SumsIndividualHitMultipliers()
        {
            WeaponTechniqueDefinition def = MakeThreeHitCombo();
            double sum = TechniqueExecutor.CalculateTotalMultiplier(def);

            // 3 hits: 0.4 + 0.4 + 0.5 = 1.3 total multi (không vượt totalMultiplier=1.35)
            Assert.AreEqual(1.3, sum, 0.0001);
            Assert.LessOrEqual(sum, def.TotalMultiplier, "Tổng hit không được vượt totalMultiplier của definition.");
        }

        [Test]
        public void CalculateHitsForTarget_ThreeHits_AllNonZeroDamage()
        {
            var attacker = new CharacterStats(level: 5); // ATK=18, DEF=9 theo docs/07 §1
            var def = MakeThreeHitCombo();
            var rolls = new List<double> { 0.5, 0.5, 0.5 }; // không crit

            List<DamageInfo> hits = TechniqueExecutor.CalculateHitsForTarget(
                attacker, defenderDefense: 0.0, def, rolls, sourceId: "test");

            Assert.AreEqual(3, hits.Count, "Phải có đúng 3 hit.");

            double totalDamage = 0.0;
            for (int i = 0; i < hits.Count; i++)
            {
                Assert.Greater(hits[i].Amount, 0.0, $"Hit {i} phải > 0.");
                totalDamage += hits[i].Amount;
            }

            // ATK 18 * (0.4 + 0.4 + 0.5) = 18 * 1.3 = 23.4 (round xuống = 23)
            // defense 0 nên max(1, 23.4) = 23.4 → round = 23
            Assert.AreEqual(23.0, totalDamage, 1.0, "Tổng damage 3 hit phải khớp tổng multiplier.");
        }

        // ─────────────────────────────────────────────────────────────────
        // 5. Duplicate hit prevention
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void HitInstanceProtector_MarksAndRejectsDuplicateHitWindow()
        {
            var protector = new HitInstanceProtector();
            long activationA = protector.BeginActivation();
            long activationB = protector.BeginActivation();
            Assert.AreNotEqual(activationA, activationB, "Mỗi activation phải có id khác nhau.");

            // Window 0, target 999 → lần 1 OK
            bool first = protector.TryMarkHit(windowIdx: 0, targetInstanceId: 999);
            Assert.IsTrue(first, "Lần đầu tiên phải được phép gây damage.");

            // Trùng window + target → bị chặn
            bool duplicate = protector.TryMarkHit(windowIdx: 0, targetInstanceId: 999);
            Assert.IsFalse(duplicate, "Trùng hit phải bị chặn (anticheat duplicate).");

            // Window khác → được phép
            bool newWindow = protector.TryMarkHit(windowIdx: 1, targetInstanceId: 999);
            Assert.IsTrue(newWindow, "Window khác được phép gây damage lại.");

            // Target khác → được phép
            bool newTarget = protector.TryMarkHit(windowIdx: 0, targetInstanceId: 888);
            Assert.IsTrue(newTarget, "Target khác được phép gây damage.");
        }

        [Test]
        public void MasteryCreditTracker_EnforcesPerEnemyCap()
        {
            var tracker = new MasteryCreditTracker();
            const int enemyId = 12345;

            int a = tracker.TryConsume(enemyId, 2);  // +2 (dùng 2)
            int b = tracker.TryConsume(enemyId, 3);  // +2 (còn 2, cap 4)
            int c = tracker.TryConsume(enemyId, 1);  // +0 (đủ 4)

            Assert.AreEqual(2, a);
            Assert.AreEqual(2, b);
            Assert.AreEqual(0, c);
            Assert.AreEqual(0, tracker.GetRemaining(enemyId));
        }

        // ─────────────────────────────────────────────────────────────────
        // 6. Save/load mastery
        // ─────────────────────────────────────────────────────────────────
        [Test]
        public void MasterySaveData_RoundTrip_PreservesAllDisciplines()
        {
            var original = new Dictionary<WeaponDiscipline, int>
            {
                { WeaponDiscipline.Blade, 27 },
                { WeaponDiscipline.Knives, 55 },
                { WeaponDiscipline.Maul, 0 }
            };

            MasterySaveData saved = MasterySaveData.FromDictionary(original);
            Dictionary<WeaponDiscipline, int> loaded = saved.ToDictionary();

            Assert.AreEqual(27, loaded[WeaponDiscipline.Blade]);
            Assert.AreEqual(55, loaded[WeaponDiscipline.Knives]);
            Assert.AreEqual(0, loaded[WeaponDiscipline.Maul]);
            // None không bao giờ được lưu
            Assert.IsFalse(loaded.ContainsKey(WeaponDiscipline.None));
        }

        [Test]
        public void MasterySaveData_GetValueSetValue_RoundTrips()
        {
            var data = new MasterySaveData();
            Assert.AreEqual(0, data.GetValue(WeaponDiscipline.Blade));

            data.SetValue(WeaponDiscipline.Blade, 68);
            Assert.AreEqual(68, data.GetValue(WeaponDiscipline.Blade));

            // Clamp trên 100
            data.SetValue(WeaponDiscipline.Blade, 150);
            Assert.AreEqual(100, data.GetValue(WeaponDiscipline.Blade));

            // Clamp dưới 0
            data.SetValue(WeaponDiscipline.Greatblade, -5);
            Assert.AreEqual(0, data.GetValue(WeaponDiscipline.Greatblade));
        }

        [Test]
        public void PlayerMasteryService_AddMastery_FiresEventAndClamps()
        {
            var service = new PlayerMasteryService(saveService: null);

            int fired = 0;
            WeaponDiscipline lastDisc = WeaponDiscipline.None;
            int lastOld = -1;
            int lastNew = -1;
            service.TechniqueMasteryChanged += (_, args) =>
            {
                fired++;
                lastDisc = args.Discipline;
                lastOld = args.OldValue;
                lastNew = args.NewValue;
            };

            int actual = service.AddMastery(WeaponDiscipline.Blade, 30);
            Assert.AreEqual(30, actual);
            Assert.AreEqual(30, service.GetMastery(WeaponDiscipline.Blade));
            Assert.AreEqual(1, fired);
            Assert.AreEqual(WeaponDiscipline.Blade, lastDisc);
            Assert.AreEqual(0, lastOld);
            Assert.AreEqual(30, lastNew);

            // Cộng đẩy lên 100 (clamp)
            int actual2 = service.AddMastery(WeaponDiscipline.Blade, 9999);
            Assert.AreEqual(70, actual2, "Chỉ cộng thêm 70 để lên 100.");
            Assert.AreEqual(100, service.GetMastery(WeaponDiscipline.Blade));
            Assert.AreEqual(2, fired);

            // Cộng nữa = 0
            int actual3 = service.AddMastery(WeaponDiscipline.Blade, 10);
            Assert.AreEqual(0, actual3);
            Assert.AreEqual(2, fired, "Không bắn event nếu giá trị không đổi.");

            // None không cộng được
            int actual4 = service.AddMastery(WeaponDiscipline.None, 50);
            Assert.AreEqual(0, actual4);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void WeaponDefinitionValidator_CatchesMalformedData()
        {
            // Không có windows
            var bad = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            Assert.IsFalse(bad.IsDefinitionValid(out string err1));
            Assert.IsNotNull(err1);
            Assert.IsNotEmpty(err1);

            // Tổng hit vượt totalMultiplier
            var badSum = WeaponTechniqueDefinition.CreateDefault(
                "technique.bad", WeaponDiscipline.Blade, TechniqueInputSlot.Core, totalMult: 0.5);
            // Mặc định 1 window 1.0 multiplier. Tổng 1.0 > 0.5 → lỗi.
            Assert.IsFalse(badSum.IsDefinitionValid(out string err2));
            StringAssert.Contains("tổng hitMultiplier", err2);
        }

        [Test]
        public void WeaponTypeToDisciplineConverter_MapsKnownTypes()
        {
            Assert.AreEqual(WeaponDiscipline.Blade,
                WeaponTypeToDisciplineConverter.Convert(Game.Core.Items.WeaponType.Sword));
            Assert.AreEqual(WeaponDiscipline.Blade,
                WeaponTypeToDisciplineConverter.Convert(Game.Core.Items.WeaponType.Rapier));
            Assert.AreEqual(WeaponDiscipline.Knives,
                WeaponTypeToDisciplineConverter.Convert(Game.Core.Items.WeaponType.Dagger));
            Assert.AreEqual(WeaponDiscipline.Cleaver,
                WeaponTypeToDisciplineConverter.Convert(Game.Core.Items.WeaponType.TwoHandedAxe));
            Assert.AreEqual(WeaponDiscipline.Maul,
                WeaponTypeToDisciplineConverter.Convert(Game.Core.Items.WeaponType.Mace));
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────
        private static WeaponTechniqueDefinition MakeBladeTechnique(
            TechniqueInputSlot slot, int masteryRequired)
        {
            WeaponTechniqueDefinition def = WeaponTechniqueDefinition.CreateDefault(
                "technique.test", WeaponDiscipline.Blade, slot, totalMult: 1.0);
            SetPrivateField(def, "masteryRequired", masteryRequired);
            return def;
        }

        private static WeaponTechniqueDefinition MakeThreeHitCombo()
        {
            return new ThreeHitBuilder().Build();
        }

        private class ThreeHitBuilder
        {
            public WeaponTechniqueDefinition Build()
            {
                WeaponTechniqueDefinition def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
                SetPrivateField(def, "id", "technique.combo");
                SetPrivateField(def, "displayName", "Three Hit");
                SetPrivateField(def, "weaponDiscipline", WeaponDiscipline.Blade);
                SetPrivateField(def, "inputSlot", TechniqueInputSlot.Core);
                SetPrivateField(def, "masteryRequired", 0);
                SetPrivateField(def, "cooldownSeconds", 2.5f);
                SetPrivateField(def, "windUpSeconds", 0.08f);
                SetPrivateField(def, "recoverySeconds", 0.30f);
                SetPrivateField(def, "totalMultiplier", 1.35);
                SetPrivateField(def, "hitboxRadius", 0.9f);
                SetPrivateField(def, "hitboxOffset", new UnityEngine.Vector2(0.6f, 0.0f));
                SetPrivateField(def, "criticalChanceOverride", -1.0);
                SetPrivateField(def, "criticalDamageOverride", -1.0);
                var windows = new List<TechniqueHitWindow>
                {
                    new TechniqueHitWindow(0.00f, 0.10f, 0.40, MovementPolicy.None),
                    new TechniqueHitWindow(0.12f, 0.10f, 0.40, MovementPolicy.Step, 0.3f),
                    new TechniqueHitWindow(0.26f, 0.12f, 0.50, MovementPolicy.None),
                };
                SetPrivateField(def, "activeWindows", windows);
                return def;
            }
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.SetField | BindingFlags.GetField);
            if (f == null)
            {
                // Fallback: tìm field theo tên không có prefix (nếu ScriptableObject field bình thường,
                // không phải auto-generated property backing field).
                foreach (FieldInfo cand in obj.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (cand.Name == fieldName ||
                        cand.Name.EndsWith(fieldName) ||
                        fieldName.EndsWith(cand.Name))
                    {
                        cand.SetValue(obj, value);
                        return;
                    }
                }
                Assert.Fail("Không tìm thấy field " + fieldName + " trên " + obj.GetType().Name);
                return;
            }
            f.SetValue(obj, value);
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Balance range tests (docs/07 §4.1, §4.2, §4.4) — Wave 2A fix
    // Kiểm tra 2 Technique asset + DodgeDefinition nằm trong dải cho phép.
    // ──────────────────────────────────────────────────────────────────────
    [TestFixture]
    public class Wave2ABalanceTests
    {
        // ── Core Technique (Blade Foundation) ────────────────────────────

        [Test]
        public void BladeFoundation_Cooldown_InCoreRange_2to3s()
        {
            // Core slot: cooldown 2–3s (docs/07 §4.1)
            var def = MakeCore(cooldown: 2.5f, totalMult: 1.25, windows: new[] { (0.0, 0.6), (0.15, 0.65) });
            Assert.GreaterOrEqual(def.CooldownSeconds, 2f, "Core cooldown >= 2s");
            Assert.LessOrEqual(def.CooldownSeconds, 3f, "Core cooldown <= 3s");
        }

        [Test]
        public void BladeFoundation_TotalMultiplier_InCoreRange_115to135()
        {
            // Core: totalMultiplier 1.15–1.35 (docs/07 §4.2)
            var def = MakeCore(cooldown: 2.5f, totalMult: 1.25, windows: new[] { (0.0, 0.6), (0.15, 0.65) });
            Assert.GreaterOrEqual(def.TotalMultiplier, 1.15, "Core mult >= 1.15");
            Assert.LessOrEqual(def.TotalMultiplier, 1.35, "Core mult <= 1.35");
        }

        [Test]
        public void BladeFoundation_Recovery_InCoreRange_020to035s()
        {
            // Core recovery 0.20–0.35s (docs/07 §4.1)
            var def = MakeCore(cooldown: 2.5f, totalMult: 1.25, windows: new[] { (0.0, 0.6), (0.15, 0.65) });
            Assert.GreaterOrEqual(def.RecoverySeconds, 0.20f, "Core recovery >= 0.20s");
            Assert.LessOrEqual(def.RecoverySeconds, 0.35f, "Core recovery <= 0.35s");
        }

        // ── Traverse Technique (Blade Traverse) ──────────────────────────

        [Test]
        public void BladeTraverse_Cooldown_InTraverseRange_4to6s()
        {
            // Traverse cooldown 4–6s (docs/07 §4.1)
            var def = MakeTraverse(cooldown: 4.5f, recovery: 0.38f, totalMult: 1.20,
                windows: new[] { (0.0, 0.5), (0.12, 0.35), (0.26, 0.35) });
            Assert.GreaterOrEqual(def.CooldownSeconds, 4f, "Traverse cooldown >= 4s");
            Assert.LessOrEqual(def.CooldownSeconds, 6f, "Traverse cooldown <= 6s");
        }

        [Test]
        public void BladeTraverse_TotalMultiplier_InTraverseRange_100to125()
        {
            // Traverse: totalMultiplier 1.00–1.25 (docs/07 §4.2)
            var def = MakeTraverse(cooldown: 4.5f, recovery: 0.38f, totalMult: 1.20,
                windows: new[] { (0.0, 0.5), (0.12, 0.35), (0.26, 0.35) });
            Assert.GreaterOrEqual(def.TotalMultiplier, 1.00, "Traverse mult >= 1.00");
            Assert.LessOrEqual(def.TotalMultiplier, 1.25, "Traverse mult <= 1.25");
        }

        [Test]
        public void BladeTraverse_Recovery_InTraverseRange_030to050s()
        {
            // Traverse recovery 0.30–0.50s (docs/07 §4.1)
            var def = MakeTraverse(cooldown: 4.5f, recovery: 0.38f, totalMult: 1.20,
                windows: new[] { (0.0, 0.5), (0.12, 0.35), (0.26, 0.35) });
            Assert.GreaterOrEqual(def.RecoverySeconds, 0.30f, "Traverse recovery >= 0.30s");
            Assert.LessOrEqual(def.RecoverySeconds, 0.50f, "Traverse recovery <= 0.50s");
        }

        [Test]
        public void BladeTraverse_SumHitMultipliers_NotExceedTotalMultiplier()
        {
            var def = MakeTraverse(cooldown: 4.5f, recovery: 0.38f, totalMult: 1.20,
                windows: new[] { (0.0, 0.5), (0.12, 0.35), (0.26, 0.35) });
            double sum = def.SumHitMultipliers();
            Assert.LessOrEqual(sum, def.TotalMultiplier + 0.0001,
                "Tổng hitMultiplier không được vượt totalMultiplier");
        }

        // ── Dodge Definition ──────────────────────────────────────────────

        [Test]
        public void DodgeDefinition_Balance_MatchesDocs07_44()
        {
            // docs/07 §4.4: duration 0.18, cooldown 1.2, recovery 0.20, no i-frames
            var def = DodgeDefinition.CreateDefault();
            Assert.AreEqual(0.18f, def.DodgeDurationSeconds, 0.01f, "Dodge duration ~0.18s");
            Assert.AreEqual(1.2f,  def.CooldownSeconds,      0.01f, "Dodge cooldown ~1.2s");
            Assert.AreEqual(0.20f, def.RecoverySeconds,      0.01f, "Dodge recovery ~0.20s");
        }

        [Test]
        public void DodgeDefinition_DashDistance_ReasonableRange()
        {
            // Dodge dash distance hợp lý: 1–3 unit
            var def = DodgeDefinition.CreateDefault();
            Assert.GreaterOrEqual(def.DashDistance, 1f, "Dodge distance >= 1u");
            Assert.LessOrEqual(def.DashDistance, 3f, "Dodge distance <= 3u");
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static WeaponTechniqueDefinition MakeCore(
            float cooldown, double totalMult, (double hitMult, double ignore)[] windows)
        {
            var def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            SetField(def, "id",              "technique.blade_foundation");
            SetField(def, "weaponDiscipline", WeaponDiscipline.Blade);
            SetField(def, "inputSlot",        TechniqueInputSlot.Core);
            SetField(def, "masteryRequired",  0);
            SetField(def, "cooldownSeconds",  cooldown);
            SetField(def, "windUpSeconds",    0.10f);
            SetField(def, "recoverySeconds",  0.25f);
            SetField(def, "totalMultiplier",  totalMult);
            var wins = new System.Collections.Generic.List<TechniqueHitWindow>();
            for (int i = 0; i < windows.Length; i++)
                wins.Add(new TechniqueHitWindow((i * 0.15f), 0.12f, windows[i].hitMult, MovementPolicy.None));
            SetField(def, "activeWindows", wins);
            return def;
        }

        private static WeaponTechniqueDefinition MakeTraverse(
            float cooldown, float recovery, double totalMult, (double hitMult, double ignore)[] windows)
        {
            var def = ScriptableObject.CreateInstance<WeaponTechniqueDefinition>();
            SetField(def, "id",              "technique.blade_traverse");
            SetField(def, "weaponDiscipline", WeaponDiscipline.Blade);
            SetField(def, "inputSlot",        TechniqueInputSlot.Traverse);
            SetField(def, "masteryRequired",  25);
            SetField(def, "cooldownSeconds",  cooldown);
            SetField(def, "windUpSeconds",    0.08f);
            SetField(def, "recoverySeconds",  recovery);
            SetField(def, "totalMultiplier",  totalMult);
            var wins = new System.Collections.Generic.List<TechniqueHitWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                var policy = i == 0 ? MovementPolicy.Dash : MovementPolicy.None;
                wins.Add(new TechniqueHitWindow(i * 0.14f, 0.10f, windows[i].hitMult, policy, i == 0 ? 1.8f : 0f));
            }
            SetField(def, "activeWindows", wins);
            return def;
        }

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            f?.SetValue(obj, value);
        }
    }
}
