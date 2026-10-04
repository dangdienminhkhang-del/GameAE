using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Network;
using Game.Core.Time;

namespace Game.Services.Network
{
    public class UGSServerFunctionService : IServerFunctionService
    {
        // Cloud Code Scripts (JavaScript) — không phải C# Modules.
        // Tên script khớp với file deploy: GetWorldTimeSnapshot.js, ReportBossDefeated.js
#if UGS_CLOUDCODE_PRESENT
        private const string ScriptGetWorldTime    = "GetWorldTimeSnapshot";
        private const string ScriptReportBoss      = "ReportBossDefeated";
#endif

        public virtual async Task ReportBossDefeatedAsync(string bossId, string floorId)
        {
#if UGS_CLOUDCODE_PRESENT
            try
            {
                if (Unity.Services.CloudCode.CloudCodeService.Instance != null)
                {
                    var args = new Dictionary<string, object>
                    {
                        { "bossId", bossId },
                        { "floorId", floorId }
                    };
                    await Unity.Services.CloudCode.CloudCodeService.Instance
                        .CallEndpointAsync<object>(ScriptReportBoss, args);
                }
            }
            catch { /* swallow — server functions are best-effort */ }
#else
            await Task.CompletedTask;
#endif
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
