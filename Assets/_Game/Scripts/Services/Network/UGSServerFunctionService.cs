using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;
using Game.Core.World;

namespace Game.Services.Network
{
    public class UGSServerFunctionService : IServerFunctionService
    {
        // Cloud Code Scripts (JavaScript) — không phải C# Modules.
        // Tên script khớp với file deploy: GetWorldTimeSnapshot.js, ReportBossDefeated.js
        private const string ScriptGetWorldTime    = "GetWorldTimeSnapshot";
        private const string ScriptReportBoss      = "ReportBossDefeated";

        // Hợp đồng response cố định của ReportBossDefeated.js (cloudcode/, task P8-1).
        private const string ExpectedBossStatus  = "DEFEATED";
        private const string ExpectedFloorStatus = "CLEARED";
        private static readonly Regex FloorIdPattern =
            new Regex(@"^floor\.\d{2,}$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Báo boss đã bị hạ và CHỈ trả kết quả khi server xác nhận hợp lệ.
        /// Mọi trường hợp còn lại (thiếu define UGS_CLOUDCODE_PRESENT, CloudCodeService null, exception,
        /// ok != true, thiếu field, ID không khớp) đều NÉM <see cref="ServerFunctionUnavailableException"/>:
        /// không nuốt lỗi, không trả "thành công giả" (docs/12 §7.2).
        /// </summary>
        public virtual async Task<BossDefeatResult> ReportBossDefeatedAsync(string bossId, string floorId)
        {
            Dictionary<string, object> raw;
            try
            {
                raw = await InvokeReportBossScriptAsync(bossId, floorId);
            }
            catch (ServerFunctionUnavailableException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServerFunctionUnavailableException(
                    ScriptReportBoss, $"gọi Cloud Code thất bại ({ex.GetType().Name}: {ex.Message}).", ex);
            }

            return ParseBossDefeatResponse(raw, bossId, floorId);
        }

        /// <summary>
        /// Seam duy nhất chạm SDK Cloud Code cho ReportBossDefeated (test ghi đè để không gọi server thật).
        /// Trả nguyên response thô; không nuốt lỗi.
        /// </summary>
        protected virtual Task<Dictionary<string, object>> InvokeReportBossScriptAsync(string bossId, string floorId)
        {
#if UGS_CLOUDCODE_PRESENT
            var cloudCode = Unity.Services.CloudCode.CloudCodeService.Instance;
            if (cloudCode == null)
            {
                throw new ServerFunctionUnavailableException(
                    ScriptReportBoss, "CloudCodeService.Instance là null (dịch vụ chưa khởi tạo/đăng nhập).");
            }

            var args = new Dictionary<string, object>
            {
                { "bossId", bossId },
                { "floorId", floorId }
            };
            return cloudCode.CallEndpointAsync<Dictionary<string, object>>(ScriptReportBoss, args);
#else
            throw new ServerFunctionUnavailableException(
                ScriptReportBoss,
                "thiếu define UGS_CLOUDCODE_PRESENT (chưa cài package Cloud Code) nên không thể xác nhận với server.");
#endif
        }

        /// <summary>
        /// Kiểm tra response của ReportBossDefeated theo hợp đồng cố định rồi dựng <see cref="BossDefeatResult"/>.
        /// Public static để EditMode test chạy được mà không cần SDK.
        /// NÉM <see cref="ServerFunctionUnavailableException"/> nếu: response null; ok khác true; thiếu/sai field;
        /// bossId/floorId không khớp yêu cầu; bossStatus != DEFEATED; floorStatus != CLEARED;
        /// nextFloorId không đúng dạng <c>floor.NN</c>.
        /// </summary>
        public static BossDefeatResult ParseBossDefeatResponse(
            IDictionary<string, object> response, string expectedBossId, string expectedFloorId)
        {
            if (response == null)
                throw Invalid("response rỗng (null).");

            if (!TryReadBool(response, "ok", out bool ok))
                throw Invalid("response thiếu field 'ok'.");
            if (!ok)
                throw Invalid("server trả ok=false.");

            if (!TryReadBool(response, "alreadyDefeated", out bool alreadyDefeated))
                throw Invalid("response thiếu field 'alreadyDefeated' (kiểu bool).");

            string bossId = RequireString(response, "bossId");
            string floorId = RequireString(response, "floorId");
            string bossStatus = RequireString(response, "bossStatus");
            string floorStatus = RequireString(response, "floorStatus");
            string nextFloorId = RequireString(response, "nextFloorId");

            if (!string.Equals(bossId, expectedBossId, StringComparison.Ordinal))
                throw Invalid($"bossId trong response ('{bossId}') không khớp yêu cầu ('{expectedBossId}').");
            if (!string.Equals(floorId, expectedFloorId, StringComparison.Ordinal))
                throw Invalid($"floorId trong response ('{floorId}') không khớp yêu cầu ('{expectedFloorId}').");

            if (!string.Equals(bossStatus, ExpectedBossStatus, StringComparison.OrdinalIgnoreCase))
                throw Invalid($"bossStatus='{bossStatus}', cần '{ExpectedBossStatus}'.");
            if (!string.Equals(floorStatus, ExpectedFloorStatus, StringComparison.OrdinalIgnoreCase))
                throw Invalid($"floorStatus='{floorStatus}', cần '{ExpectedFloorStatus}'.");

            if (!FloorIdPattern.IsMatch(nextFloorId))
                throw Invalid($"nextFloorId='{nextFloorId}' không đúng dạng floor.NN.");

            return new BossDefeatResult(alreadyDefeated, bossId, floorId, nextFloorId);
        }

        private static ServerFunctionUnavailableException Invalid(string reason)
            => new ServerFunctionUnavailableException(ScriptReportBoss, "response không hợp lệ: " + reason);

        private static string RequireString(IDictionary<string, object> dict, string name)
        {
            if (!dict.TryGetValue(name, out var raw) || raw == null)
                throw Invalid($"response thiếu field '{name}'.");

            string value = raw.ToString();
            if (string.IsNullOrEmpty(value))
                throw Invalid($"field '{name}' rỗng.");
            return value;
        }

        private static bool TryReadBool(IDictionary<string, object> dict, string name, out bool value)
        {
            value = false;
            if (!dict.TryGetValue(name, out var raw) || raw == null) return false;
            if (raw is bool b) { value = b; return true; }
            if (raw is string) return false; // không đoán "true"/"false" dạng chuỗi
            try { value = Convert.ToBoolean(raw); return true; }
            catch { return false; }
        }

        public virtual async Task<WorldTimeSnapshot> GetWorldTimeSnapshotAsync()
        {
#if UGS_CLOUDCODE_PRESENT
            try
            {
                if (Unity.Services.CloudCode.CloudCodeService.Instance == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "[UGSServerFunctionService] CloudCodeService.Instance is null; returning null (no authoritative snapshot).");
                    return null;
                }

                var result = await Unity.Services.CloudCode.CloudCodeService.Instance
                    .CallEndpointAsync<Dictionary<string, object>>(
                        ScriptGetWorldTime, new Dictionary<string, object>());
                return ConvertToSnapshot(result);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"[UGSServerFunctionService] GetWorldTimeSnapshot failed: {ex.Message}; returning null (no authoritative snapshot).");
                return null;
            }
#else
            UnityEngine.Debug.LogWarning(
                "[UGSServerFunctionService] UGS_CLOUDCODE_PRESENT not defined; returning null (no authoritative snapshot).");
            return null;
#endif
        }

        private static WorldTimeSnapshot ConvertToSnapshot(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[UGSServerFunctionService] Cloud Code returned null payload; no snapshot.");
                return null;
            }

            bool Has(string name) => dict.TryGetValue(name, out var v) && v != null;
            T Get<T>(string name, Func<object, T> convert, out bool ok)
            {
                ok = false;
                if (!dict.TryGetValue(name, out var v) || v == null) return default;
                try { T val = convert(v); ok = true; return val; }
                catch { return default; }
            }

            int schema = Get("schemaVersion", v => Convert.ToInt32(v), out var ok1);
            double gameHours = Get("gameTimeHoursAtAnchor", v => Convert.ToDouble(v), out var ok2);
            long anchor = Get("anchorUnixSeconds", v => Convert.ToInt64(v), out var ok3);
            long revision = Get("revision", v => Convert.ToInt64(v), out var ok4);
            int cycleVer = Get("cycleConfigVersion", v => Convert.ToInt32(v), out var ok5);
            double rate = Get("realSecondsPerGameDay", v => Convert.ToDouble(v), out var ok6);

            if (!ok1 || !ok2 || !ok3 || !ok4 || !ok5 || !ok6)
            {
                UnityEngine.Debug.LogWarning(
                    "[UGSServerFunctionService] Cloud Code response missing required field " +
                    "(schemaVersion/gameTimeHoursAtAnchor/anchorUnixSeconds/revision/realSecondsPerGameDay); returning null.");
                return null;
            }
            if (rate <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"[UGSServerFunctionService] realSecondsPerGameDay={rate} is non-positive; returning null.");
                return null;
            }

            return new WorldTimeSnapshot
            {
                schemaVersion = schema,
                gameTimeHoursAtAnchor = gameHours,
                anchorUnixSeconds = anchor,
                revision = revision,
                cycleConfigVersion = cycleVer,
                realSecondsPerGameDay = rate
            };
        }
    }
}
