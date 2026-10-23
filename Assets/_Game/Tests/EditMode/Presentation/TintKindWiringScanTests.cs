using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Quét mã nguồn để giữ hai luật của ADR-029 mà test hành vi khó phủ hết (boss có nhiều state):
    /// 1. Mọi nơi đặt màu trạng thái đều khai báo TintKind, và màu báo đòn đi với đúng loại
    ///    (Telegraph/Pressure = DangerTelegraph, Punish/Recovery = Window, Cuồng nộ = Enrage).
    /// 2. GIÁ TRỊ màu báo đòn không đổi (docs/14 §2) và không ai ghi spriteRenderer.color trực tiếp ngoài SpriteTint.
    /// Bộ quét cũng được test riêng để không "xanh giả".
    /// </summary>
    public class TintKindWiringScanTests
    {
        private static string ScriptsPath(params string[] parts)
        {
            var all = new List<string> { Application.dataPath, "_Game", "Scripts" };
            all.AddRange(parts);
            return Path.Combine(all.ToArray());
        }

        private static string ReadCode(params string[] parts)
        {
            string path = ScriptsPath(parts);
            Assert.IsTrue(File.Exists(path), "Thiếu file: " + path);
            return StripComments(File.ReadAllText(path));
        }

        // ── Boss: màu ↔ loại ──────────────────────────────────────────────────

        [Test]
        public void Boss_EveryStateColor_IsPairedWithTheRightKind()
        {
            string code = ReadCode("PvE", "Boss", "BossController.cs");

            AssertPairs(code, "AttackTelegraphColor", "DangerTelegraph");
            AssertPairs(code, "PressureTelegraphColor", "DangerTelegraph");
            AssertPairs(code, "PunishColor", "Window");
            AssertPairs(code, "EnragedColor", "Enrage");
        }

        [Test]
        public void Boss_PressureColor_IsUsedForBothWindupAndDash()
        {
            string code = ReadCode("PvE", "Boss", "BossController.cs");
            int count = Regex.Matches(code, @"SetTint\(\s*PressureTelegraphColor\s*,\s*TintKind\.DangerTelegraph\s*\)").Count;
            Assert.AreEqual(2, count, "windup và lao đều là báo đòn nguy hiểm");
        }

        [Test]
        public void Boss_PunishColor_IsUsedForPunishAndRecovery()
        {
            string code = ReadCode("PvE", "Boss", "BossController.cs");
            int count = Regex.Matches(code, @"SetTint\(\s*PunishColor\s*,\s*TintKind\.Window\s*\)").Count;
            Assert.AreEqual(2, count, "Punish và Recovery sau lao đều là Window");
        }

        [Test]
        public void Boss_NoSetTintCall_LacksAKind()
        {
            string code = ReadCode("PvE", "Boss", "BossController.cs");
            Assert.AreEqual(0, CallsWithoutKind(code, "SetTint").Count, string.Join("; ", CallsWithoutKind(code, "SetTint")));
        }

        [Test]
        public void Boss_ResetAndRestore_UseEnrageOrBaseKind()
        {
            string code = ReadCode("PvE", "Boss", "BossController.cs");
            StringAssert.Contains("ResetAll(_isEnraged ? EnragedColor : Color.white, _isEnraged ? TintKind.Enrage : TintKind.Base)", code);
            StringAssert.Contains("SetTint(_isEnraged ? EnragedColor : Color.white, _isEnraged ? TintKind.Enrage : TintKind.Base)", code);
        }

        // ── Quái ──────────────────────────────────────────────────────────────

        [Test]
        public void Enemy_Telegraph_IsDangerTelegraph_AndNoOtherKindIsUsed()
        {
            string code = ReadCode("PvE", "Enemy", "EnemyController.cs");

            StringAssert.Contains("TintKind.DangerTelegraph", code);
            StringAssert.DoesNotContain("TintKind.Window", code);
            StringAssert.DoesNotContain("TintKind.Enrage", code);
            Assert.AreEqual(0, CallsWithoutKind(code, "SetStateTint").Count);
        }

        // ── Không có đường ghi màu trực tiếp, không có SetStateTint thiếu loại ──

        [Test]
        public void NoSetStateTintCall_AnywhereInPvE_LacksAKind()
        {
            var problems = new List<string>();
            foreach (string file in Directory.GetFiles(ScriptsPath("PvE"), "*.cs", SearchOption.AllDirectories))
            {
                foreach (string call in CallsWithoutKind(StripComments(File.ReadAllText(file)), "SetStateTint"))
                {
                    problems.Add(Path.GetFileName(file) + ": " + call);
                }
            }
            Assert.IsEmpty(problems, string.Join("; ", problems));
        }

        [Test]
        public void EnemyAndBoss_NeverWriteSpriteRendererColorDirectly()
        {
            // Mọi màu sprite đi qua SpriteTint; ghi thẳng .color sẽ làm hỏng thứ tự ưu tiên chết > state.
            Regex direct = new Regex(@"spriteRenderer\s*\.\s*color\s*=(?!=)");
            Assert.IsFalse(direct.IsMatch(ReadCode("PvE", "Enemy", "EnemyController.cs")), "EnemyController ghi color trực tiếp");
            Assert.IsFalse(direct.IsMatch(ReadCode("PvE", "Boss", "BossController.cs")), "BossController ghi color trực tiếp");
        }

        // ── Giá trị màu báo đòn giữ nguyên (docs/14 §2) ───────────────────────

        [Test]
        public void TelegraphColorValues_AreUnchanged()
        {
            string boss = ReadCode("PvE", "Boss", "BossController.cs");
            string enemy = ReadCode("PvE", "Enemy", "EnemyController.cs");

            StringAssert.Contains("TelegraphColor = new Color(1f, 0.55f, 0.55f, 1f)", enemy);
            StringAssert.Contains("AttackTelegraphColor = new Color(1f, 0.8f, 0.2f, 1f)", boss);
            StringAssert.Contains("PunishColor = new Color(0.7f, 0.7f, 1f, 1f)", boss);
            StringAssert.Contains("PressureTelegraphColor = new Color(0.75f, 0.2f, 1f, 1f)", boss);
            StringAssert.Contains("EnragedColor = new Color(1f, 0.5f, 0.5f, 1f)", boss);
        }

        // ── Bộ quét ───────────────────────────────────────────────────────────

        [Test]
        public void Scanner_FlagsCallsWithoutKind_AndAcceptsCallsWithKind()
        {
            Assert.AreEqual(1, CallsWithoutKind("SetTint(Color.red);", "SetTint").Count);
            Assert.AreEqual(0, CallsWithoutKind("SetTint(Color.red, TintKind.Base);", "SetTint").Count);
            Assert.AreEqual(0, CallsWithoutKind("private void SetTint(Color color, TintKind kind)\n{", "SetTint").Count);
            Assert.AreEqual(1, CallsWithoutKind("_tint.SetStateTint(c);\nx = 1;", "SetStateTint").Count);
            Assert.AreEqual(0, CallsWithoutKind(StripComments("// SetTint(Color.red);"), "SetTint").Count, "comment bị bỏ qua sau StripComments");
            Assert.AreEqual(0, CallsWithoutKind("_tint.SetStateTint(color, kind);", "SetStateTint").Count, "chuyển tiếp biến kind cũng hợp lệ");
        }

        [Test]
        public void Scanner_PairCheck_CatchesAWrongKind()
        {
            Assert.Throws<AssertionException>(() => AssertPairs("SetTint(PunishColor, TintKind.DangerTelegraph);", "PunishColor", "Window"));
            Assert.DoesNotThrow(() => AssertPairs("SetTint(PunishColor, TintKind.Window);", "PunishColor", "Window"));
        }

        private static void AssertPairs(string code, string colorName, string expectedKind)
        {
            var calls = Regex.Matches(code, @"SetTint\(\s*" + colorName + @"\s*,\s*([^)]*)\)");
            // Gọi bằng tên màu mà không có tham số thứ hai cũng bị coi là sai (xem Boss_NoSetTintCall_LacksAKind).
            Assert.Greater(calls.Count, 0, "Không thấy lệnh SetTint với " + colorName);
            foreach (Match call in calls)
            {
                Assert.AreEqual("TintKind." + expectedKind, call.Groups[1].Value.Trim(), colorName + " phải đi với " + expectedKind);
            }
        }

        private static List<string> CallsWithoutKind(string code, string method)
        {
            var result = new List<string>();
            foreach (Match match in Regex.Matches(code, method + @"\((?![^;{]*(TintKind|,\s*kind\b))[^;{]*"))
            {
                result.Add(match.Value.Trim());
            }
            return result;
        }

        private static string StripComments(string source)
        {
            var sb = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (c == '/' && next == '/')
                {
                    while (i < source.Length && source[i] != '\n') i++;
                }
                else if (c == '/' && next == '*')
                {
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    {
                        if (source[i] == '\n') sb.Append('\n');
                        i++;
                    }
                    i += 2;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}
