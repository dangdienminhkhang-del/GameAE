using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Luật cứng #1 (AGENTS.md): Core không rẽ nhánh theo tầng cụ thể ("if floor == N", hard-code ID tầng).
    /// Test quét mã nguồn gameplay và fail nếu thấy:
    ///   1. chuỗi "floor.0" trong code (không tính comment, doc string, [Tooltip(...)]);
    ///   2. so sánh floorNumber/FloorNumber với hằng số, hoặc switch theo floorNumber.
    /// Các hàm quét (<see cref="FindViolations"/>) cũng được test riêng để chắc rằng bộ quét không "xanh giả".
    /// </summary>
    public class FloorBranchingScanTests
    {
        private static readonly string[] ScannedFolders = { "Core", "PvE", "UI", "Services" };

        /// <summary>Một dòng được phép ngoại lệ. Phải có lý do rõ ràng.</summary>
        private sealed class AllowedLine
        {
            public readonly string FileName;
            public readonly string LineContains;
            public readonly string Reason;

            public AllowedLine(string fileName, string lineContains, string reason)
            {
                FileName = fileName;
                LineContains = lineContains;
                Reason = reason;
            }
        }

        // Danh sách ngoại lệ nhỏ — mỗi mục có giải thích. Thêm mục mới phải được xem xét lại.
        private static readonly AllowedLine[] Allowed =
        {
            new AllowedLine("FloorDefinition.cs", "floorNumber < 1",
                "Kiểm tra dữ liệu hợp lệ (số tầng phải >= 1), không phải nhánh gameplay theo tầng."),
            new AllowedLine("FloorDatabase.cs", "floorNumber < 1",
                "Kiểm tra dữ liệu hợp lệ (số tầng phải >= 1), không phải nhánh gameplay theo tầng."),
            new AllowedLine("FloorDefinition.cs", "Phải theo dạng floor.NN",
                "Thông báo lỗi cho designer, chứa ID ví dụ trong chuỗi; không phải logic."),
            new AllowedLine("PlayerData.cs", "currentFloorId = \"floor.01\"",
                "Giá trị mặc định cho người chơi mới (tầng khởi đầu); là dữ liệu khởi tạo, không rẽ nhánh. Nên chuyển sang config sau."),
            new AllowedLine("PvEBootstrap.cs", "loaded.floorNumber > 0",
                "Chỉ kiểm tra số tầng hợp lệ (> 0) khi dò tầng đang chạy; không phải so sánh với một tầng cụ thể."),
            new AllowedLine("PvEBootstrap.cs", "def.floorNumber > 0",
                "Chỉ kiểm tra số tầng hợp lệ (> 0) khi dò tầng đang chạy; không phải so sánh với một tầng cụ thể."),
            new AllowedLine("BossDefinition.cs", "floorId = \"floor.01\"",
                "Giá trị mặc định của trường SerializeField khi tạo asset mới; mỗi asset boss tự gán floorId của mình."),
            new AllowedLine("WorldStateService.cs", "DefaultOpenFloorId = \"floor.01\"",
                "Tầng mở mặc định khi World State chưa có dữ liệu (docs/03 §6); là hằng dữ liệu, không rẽ nhánh theo tầng."),
        };

        // ── Quét thật ───────────────────────────────────────────────────────

        [Test]
        public void GameplayCode_HasNoBranchingOnFloorNumberOrFloorIdLiterals()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "_Game", "Scripts");
            Assert.IsTrue(Directory.Exists(scriptsRoot), "Không tìm thấy thư mục Scripts: " + scriptsRoot);

            int scannedFiles = 0;
            var offenders = new List<string>();

            foreach (string folder in ScannedFolders)
            {
                string dir = Path.Combine(scriptsRoot, folder);
                if (!Directory.Exists(dir)) continue;

                foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    scannedFiles++;
                    string fileName = Path.GetFileName(file);

                    foreach (string violation in FindViolations(File.ReadAllText(file)))
                    {
                        if (IsAllowed(fileName, violation)) continue;
                        offenders.Add($"{folder}/{fileName}: {violation.Trim()}");
                    }
                }
            }

            Assert.Greater(scannedFiles, 20, "Quét quá ít file — đường dẫn quét có thể sai.");
            Assert.IsEmpty(offenders,
                "Code gameplay không được rẽ nhánh theo số tầng/ID tầng cụ thể (AGENTS.md luật cứng #1):\n" +
                string.Join("\n", offenders));
        }

        private static bool IsAllowed(string fileName, string line)
        {
            foreach (var allowed in Allowed)
            {
                if (allowed.FileName == fileName && line.Contains(allowed.LineContains)) return true;
            }
            return false;
        }

        // ── Bộ quét (test riêng để không xanh giả) ──────────────────────────

        [Test]
        public void Scanner_FlagsFloorIdLiteralInCode()
        {
            var hits = FindViolations("var id = \"floor.01\";");
            Assert.AreEqual(1, hits.Count);
        }

        [Test]
        public void Scanner_FlagsFloorNumberComparedToConstant()
        {
            Assert.AreEqual(1, FindViolations("if (def.floorNumber == 2) Spawn();").Count);
            Assert.AreEqual(1, FindViolations("if (FloorNumber >= 3) Spawn();").Count);
            Assert.AreEqual(1, FindViolations("if (2 == floorNumber) Spawn();").Count);
            Assert.AreEqual(1, FindViolations("switch (def.floorNumber) { }").Count);
        }

        [Test]
        public void Scanner_AllowsFloorNumberUsedAsData()
        {
            Assert.AreEqual(0, FindViolations("spawner.SetFloorNumber(def.floorNumber);").Count);
            Assert.AreEqual(0, FindViolations("int floor = Mathf.Max(1, floorNumber);").Count);
            Assert.AreEqual(0, FindViolations("list.Sort((a, b) => a.floorNumber.CompareTo(b.floorNumber));").Count);
        }

        [Test]
        public void Scanner_IgnoresLineAndDocComments()
        {
            string source =
                "// if (floorNumber == 2) lỗi\n" +
                "/// <summary>ví dụ \"floor.01\"</summary>\n" +
                "int x = 1; // floor.02 chỉ là ví dụ\n";

            Assert.AreEqual(0, FindViolations(source).Count);
        }

        [Test]
        public void Scanner_IgnoresBlockComments()
        {
            string source = "/* if (floorNumber == 2)\n   floor.03 */\nint x = 1;";

            Assert.AreEqual(0, FindViolations(source).Count);
        }

        [Test]
        public void Scanner_IgnoresTooltipAttributes_IncludingConcatenatedStrings()
        {
            string source =
                "[Tooltip(\"ID dạng floor.NN, ví dụ floor.01\")]\n" +
                "[Tooltip(\"ID boss, ví dụ floor.02 \" +\n" +
                "         \"và floor.03\")]\n" +
                "public string id;\n";

            Assert.AreEqual(0, FindViolations(source).Count);
        }

        [Test]
        public void Scanner_StillFlagsCodeAfterATooltip()
        {
            string source = "[Tooltip(\"mô tả\")] public string id = \"floor.01\";";

            Assert.AreEqual(1, FindViolations(source).Count);
        }

        [Test]
        public void Scanner_DoesNotTreatSlashesInsideStringsAsComments()
        {
            // "http://" chứa // nhưng nằm trong chuỗi → phần code sau đó vẫn phải được quét.
            string source = "var url = \"http://x\"; var id = \"floor.01\";";

            Assert.AreEqual(1, FindViolations(source).Count);
        }

        // ── Cài đặt bộ quét ─────────────────────────────────────────────────

        private static readonly Regex FloorIdLiteral = new Regex(@"floor\.0", RegexOptions.Compiled);

        private static readonly Regex FloorNumberVsConstant = new Regex(
            @"\b\w*[Ff]loorNumber\b\s*(==|!=|<=|>=|<|>)\s*-?\d",
            RegexOptions.Compiled);

        private static readonly Regex ConstantVsFloorNumber = new Regex(
            @"-?\d[\d\.]*f?\s*(==|!=|<=|>=|<|>)\s*[\w\.]*[Ff]loorNumber\b",
            RegexOptions.Compiled);

        private static readonly Regex SwitchOnFloorNumber = new Regex(
            @"switch\s*\([^)]*[Ff]loorNumber",
            RegexOptions.Compiled);

        // [Tooltip("..." + "...")] — chuỗi nối nhiều dòng cũng bị loại.
        private static readonly Regex TooltipAttribute = new Regex(
            @"\[Tooltip\(\s*""(?:[^""\\]|\\.)*""(?:\s*\+\s*""(?:[^""\\]|\\.)*"")*\s*\)\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>Trả các dòng code vi phạm (đã bỏ comment, doc string, [Tooltip]).</summary>
        private static List<string> FindViolations(string source)
        {
            string code = TooltipAttribute.Replace(StripComments(source), string.Empty);

            var violations = new List<string>();
            foreach (string line in code.Split('\n'))
            {
                if (FloorIdLiteral.IsMatch(line)
                    || FloorNumberVsConstant.IsMatch(line)
                    || ConstantVsFloorNumber.IsMatch(line)
                    || SwitchOnFloorNumber.IsMatch(line))
                {
                    violations.Add(line);
                }
            }
            return violations;
        }

        /// <summary>
        /// Xóa comment //, /* */ (và ///) nhưng giữ nguyên nội dung chuỗi/ký tự, giữ số dòng.
        /// Chỉ cần đủ tốt cho việc quét; không phải parser C# đầy đủ.
        /// </summary>
        private static string StripComments(string source)
        {
            var sb = new StringBuilder(source.Length);
            int i = 0;
            int n = source.Length;

            while (i < n)
            {
                char c = source[i];
                char next = i + 1 < n ? source[i + 1] : '\0';

                if (c == '"')
                {
                    bool verbatim = i > 0 && source[i - 1] == '@';
                    sb.Append(c);
                    i++;
                    while (i < n)
                    {
                        char d = source[i];
                        sb.Append(d);
                        i++;
                        if (!verbatim && d == '\\' && i < n)
                        {
                            sb.Append(source[i]);
                            i++;
                            continue;
                        }
                        if (d == '"')
                        {
                            if (verbatim && i < n && source[i] == '"')
                            {
                                sb.Append('"');
                                i++;
                                continue;
                            }
                            break;
                        }
                    }
                    continue;
                }

                if (c == '\'')
                {
                    sb.Append(c);
                    i++;
                    while (i < n)
                    {
                        char d = source[i];
                        sb.Append(d);
                        i++;
                        if (d == '\\' && i < n)
                        {
                            sb.Append(source[i]);
                            i++;
                            continue;
                        }
                        if (d == '\'') break;
                    }
                    continue;
                }

                if (c == '/' && next == '/')
                {
                    while (i < n && source[i] != '\n') i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    i += 2;
                    while (i + 1 < n && !(source[i] == '*' && source[i + 1] == '/'))
                    {
                        if (source[i] == '\n') sb.Append('\n');
                        i++;
                    }
                    i += 2;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }
    }
}
