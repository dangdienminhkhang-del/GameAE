using Game.Core.Combat;
using Game.PvE.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Kiểm thử quy tắc hiển thị màu Cursor theo chênh lệch cấp độ theo chuẩn SAO (EnemyCursorRules).
    /// </summary>
    public class EnemyCursorRulesTests
    {
        [Test]
        public void EqualThreat_WithinPlusMinusFourLevels_ReturnsEqualAndYellow()
        {
            // Ngang cấp (±4)
            for (int diff = -4; diff <= 4; diff++)
            {
                int playerLevel = 10;
                int enemyLevel = playerLevel + diff;

                var threat = EnemyCursorRules.EvaluateEnemyThreat(enemyLevel, playerLevel);
                Assert.AreEqual(CursorThreatLevel.Equal, threat, $"Chênh lệch {diff} phải là Equal (Vàng).");

                Color c = EnemyCursorRules.GetCursorColor(threat);
                Assert.Greater(c.r, 0.7f, "Màu vàng phải có r cao");
                Assert.Greater(c.g, 0.7f, "Màu vàng phải có g cao");
                Assert.Less(c.b, 0.4f, "Màu vàng phải có b thấp");
            }
        }

        [Test]
        public void DangerousThreat_FiveLevelsHigherOrMore_ReturnsDangerousAndRed()
        {
            int playerLevel = 5;

            // +5 cấp trở lên
            int[] dangerousDeltas = { 5, 6, 10, 50 };
            foreach (int delta in dangerousDeltas)
            {
                int enemyLevel = playerLevel + delta;
                var threat = EnemyCursorRules.EvaluateEnemyThreat(enemyLevel, playerLevel);
                Assert.AreEqual(CursorThreatLevel.Dangerous, threat, $"Cao hơn +{delta} cấp phải là Dangerous (Đỏ).");

                Color c = EnemyCursorRules.GetCursorColor(threat);
                Assert.Greater(c.r, 0.8f, "Màu đỏ nguy hiểm phải có r cao");
                Assert.Less(c.g, 0.4f, "Màu đỏ nguy hiểm phải có g thấp");
                Assert.Less(c.b, 0.4f, "Màu đỏ nguy hiểm phải có b thấp");
            }
        }

        [Test]
        public void WeakThreat_FiveLevelsLowerOrMore_ReturnsWeakAndGray()
        {
            int playerLevel = 10;

            // -5 cấp trở xuống
            int[] weakDeltas = { -5, -6, -8, -9 };
            foreach (int delta in weakDeltas)
            {
                int enemyLevel = playerLevel + delta;
                var threat = EnemyCursorRules.EvaluateEnemyThreat(enemyLevel, playerLevel);
                Assert.AreEqual(CursorThreatLevel.Weak, threat, $"Thấp hơn {delta} cấp phải là Weak (Xám).");

                Color c = EnemyCursorRules.GetCursorColor(threat);
                Assert.AreEqual(c.r, c.g, 0.15f, "Màu xám/nhạt có các kênh gần nhau");
                Assert.AreEqual(c.g, c.b, 0.15f, "Màu xám/nhạt có các kênh gần nhau");
            }
        }

        [Test]
        public void PlayerPvPCursors_ReturnDistinctColorsForGreenOrangeRed()
        {
            Color green = EnemyCursorRules.GetCursorColor(CursorThreatLevel.PlayerGreen);
            Color orange = EnemyCursorRules.GetCursorColor(CursorThreatLevel.PlayerOrange);
            Color red = EnemyCursorRules.GetCursorColor(CursorThreatLevel.PlayerRed);

            // Xanh lá: g vượt trội
            Assert.Greater(green.g, green.r);
            Assert.Greater(green.g, green.b);

            // Cam: r cao, g vừa, b thấp
            Assert.Greater(orange.r, orange.g);
            Assert.Greater(orange.g, orange.b);

            // Đỏ sát thủ: r vượt trội
            Assert.Greater(red.r, red.g);
            Assert.Greater(red.r, red.b);
        }

        [Test]
        public void EnemyDefinition_LevelDefaultsToOne_AndClampsPositive()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                Assert.GreaterOrEqual(def.Level, 1, "Cấp độ quái mặc định tối thiểu là 1.");
            }
            finally
            {
                Object.DestroyImmediate(def);
            }
        }

        [Test]
        public void EnemyController_RuntimeLevel_CanBeOverridden()
        {
            var go = new GameObject("TestEnemy", typeof(EnemyController));
            var controller = go.GetComponent<EnemyController>();
            try
            {
                Assert.AreEqual(1, controller.Level, "Chưa set level phải mặc định là 1.");
                controller.SetLevel(7);
                Assert.AreEqual(7, controller.Level, "SetLevel(7) phải cập nhật controller.Level thành 7.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
