using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Luật cứng (docs/05 Phase 9.5): CHỈ MỘT file được ghi Time.timeScale — Core/Presentation/PlaySpeedState.cs.
    /// Nếu hai nơi cùng ghi, dừng hình sẽ đè pause (hoặc ngược lại) và game có thể kẹt ở tốc độ lạ.
    /// Test quét mã nguồn Scripts/ và fail khi thấy phép gán timeScale ở file khác. Bộ quét cũng được test riêng để không "xanh giả".
    /// </summary>
    public class TimeScaleWriteScanTests
    {
        private const string AllowedFileName = "PlaySpeedState.cs";

        // Bắt: timeScale = x, += , -= , *= , /=. Không bắt: ==, >=, <=, != (so sánh) hay tên như hitStopTimeScale.
        private static readonly Regex Assignment = new Regex(@"\btimeScale\s*(=(?!=)|\+=|-=|\*=|/=)", RegexOptions.Compiled);

        [Test]
        public void OnlyPlaySpeedState_WritesTimeScale()
        {
            string root = Path.Combine(Application.dataPath, "_Game", "Scripts");
            Assert.IsTrue(Directory.Exists(root), "Không tìm thấy thư mục Scripts: " + root);

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == AllowedFileName) continue;

                foreach (int line in FindViolations(File.ReadAllText(file)))
                {
                    violations.Add($"{Path.GetFileName(file)}:{line}");
                }
            }

            Assert.IsEmpty(violations,
                "Chỉ PlaySpeedState được ghi Time.timeScale (dùng PlaySpeedState.Shared.PushPause/PopPause/RequestHitStop). Vi phạm: "
                + string.Join(", ", violations));
        }

        [Test]
        public void PlaySpeedState_ReallyContainsTheSingleWrite()
        {
            string path = Path.Combine(Application.dataPath, "_Game", "Scripts", "Core", "Presentation", AllowedFileName);
            Assert.IsTrue(File.Exists(path), "Thiếu " + path);
            Assert.AreEqual(1, new List<int>(FindViolations(File.ReadAllText(path))).Count,
                "PlaySpeedState phải có đúng MỘT dòng ghi timeScale (nếu 0, bộ quét hỏng hoặc dừng hình không còn hoạt động).");
        }

        // ── Bộ quét ─────────────────────────────────────────────────────────

        [Test]
        public void Scanner_DetectsAssignments()
        {
            Assert.AreEqual(1, Count("Time.timeScale = 0f;"));
            Assert.AreEqual(1, Count("UnityEngine.Time.timeScale=1;"));
            Assert.AreEqual(1, Count("Time.timeScale += 0.1f;"));
            Assert.AreEqual(1, Count("Time.timeScale *= 0.5f;"));
        }

        [Test]
        public void Scanner_IgnoresComparisonsAndSimilarNames()
        {
            Assert.AreEqual(0, Count("if (Time.timeScale == 0f) return;"));
            Assert.AreEqual(0, Count("if (Time.timeScale >= 1f) return;"));
            Assert.AreEqual(0, Count("if (Time.timeScale <= 0.5f) return;"));
            Assert.AreEqual(0, Count("if (Time.timeScale != 1f) return;"));
            Assert.AreEqual(0, Count("float hitStopTimeScale = 0.05f;"));
            Assert.AreEqual(0, Count("float s = Time.timeScale;"));
        }

        [Test]
        public void Scanner_IgnoresComments()
        {
            Assert.AreEqual(0, Count("// Time.timeScale = 0f;"));
            Assert.AreEqual(0, Count("/* Time.timeScale = 0f; */"));
            Assert.AreEqual(0, Count("/// <summary>không được Time.timeScale = 0</summary>"));
            Assert.AreEqual(1, Count("/* ghi chú */ Time.timeScale = 0f;"));
        }

        [Test]
        public void Scanner_ReportsLineNumbers()
        {
            var lines = new List<int>(FindViolations("int a;\nint b;\nTime.timeScale = 0f;\n"));
            Assert.AreEqual(new[] { 3 }, lines.ToArray());
        }

        private static int Count(string source)
        {
            return new List<int>(FindViolations(source)).Count;
        }

        /// <summary>Trả số dòng (bắt đầu từ 1) có phép gán timeScale, bỏ qua comment.</summary>
        private static IEnumerable<int> FindViolations(string source)
        {
            string[] lines = StripComments(source).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (Assignment.IsMatch(lines[i])) yield return i + 1;
            }
        }

        /// <summary>Xóa comment // và /* */ nhưng GIỮ nguyên số dòng để báo đúng vị trí.</summary>
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
