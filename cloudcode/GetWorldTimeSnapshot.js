/*
 * Wave 2B — Authoritative Time (docs/08 §3, docs/03 §16.4)
 * Cloud Code JavaScript Script: GetWorldTimeSnapshot
 *
 * Deploy (UGS CLI, tài liệu chính thức docs.unity.com/ugs):
 *   ugs config set project-id <YOUR_PROJECT_ID>
 *   ugs config set environment-name production
 *   ugs deploy cloudcode/GetWorldTimeSnapshot.js
 *
 * C# side (UGSServerFunctionService.cs — UGS_CLOUDCODE_PRESENT):
 *   CloudCodeService.Instance.CallEndpointAsync<Dictionary<string, object>>(
 *     "GetWorldTimeSnapshot", {})
 *
 * SDK signautres (Cloud Save JS SDK 1.4 DataApi, reference
 * cloud-code-sdk-documentation.cloud.unity3d.com/cloud-save/v1.4):
 *   getCustomItems(projectId, customId, keys?, after?, options?)
 *     → GetItemsResponse { results: [{ key, value, writeLock, created, modified }] }
 *     (chỉ có dạng số ít "plural", KHÔNG có getCustomItem.)
 *   setCustomItem(projectId, customId, setItemBatchBody?, options?)
 *     → SetItemBatchResponse { results: [{ key, writeLock }] }
 *
 * RULES (LUẬT CỨNG docs/03 §12 / docs/08 §5):
 *   - KHÔNG bao giờ trả snapshot giả khi lỗi. Lỗi non-key-missing → THROW.
 *   - Read/resync KHÔNG ghi Game Data (chỉ bootstrap / migration mới ghi, 1 lần).
 *   - SchemaVersion < 3 HOẶC rate/cycle khác hằng → migrate + revision+1 (03 §16.4).
 *   - Key chưa có → HTTP 200 với results = [] → null stored.
 */

const { DataApi } = require("@unity-services/cloud-save-1.4");

const REAL_SECONDS_PER_GAME_DAY = 1200;
const START_HOUR = 8;
const CYCLE_CONFIG_VERSION = 1;
const SCHEMA_VERSION = 3;
const CUSTOM_ID = "world";
const KEY = "world_time";

function nowUnix() {
  return Math.floor(Date.now() / 1000);
}

function wrapHours(h) {
  return (((h % 24.0) + 24.0) % 24.0);
}

function initialSnapshot(now) {
  return {
    schemaVersion: SCHEMA_VERSION,
    gameTimeHoursAtAnchor: START_HOUR,
    anchorUnixSeconds: now,
    revision: 1,
    cycleConfigVersion: CYCLE_CONFIG_VERSION,
    realSecondsPerGameDay: REAL_SECONDS_PER_GAME_DAY,
  };
}

function needsMigration(stored) {
  if (!stored) return true;
  if ((stored.schemaVersion || 0) < SCHEMA_VERSION) return true;
  if (stored.realSecondsPerGameDay !== REAL_SECONDS_PER_GAME_DAY) return true;
  if (stored.cycleConfigVersion !== CYCLE_CONFIG_VERSION) return true;
  return false;
}

function migrateAndAdvance(stored, now) {
  // Tính giờ hiện tại theo rate cũ để không "nhảy" ngày đột ngột (03 §16.4 re-anchor).
  const oldRate =
    stored && stored.realSecondsPerGameDay > 0
      ? stored.realSecondsPerGameDay
      : REAL_SECONDS_PER_GAME_DAY;
  const oldAnchor =
    stored && stored.anchorUnixSeconds ? stored.anchorUnixSeconds : now;
  const oldHours =
    stored && typeof stored.gameTimeHoursAtAnchor === "number"
      ? stored.gameTimeHoursAtAnchor
      : START_HOUR;
  const elapsed = Math.max(0, now - oldAnchor);
  const hoursPerSecond = 24.0 / oldRate;
  const currentHours = wrapHours(oldHours + elapsed * hoursPerSecond);

  const prevRevision =
    stored && typeof stored.revision === "number" ? stored.revision : 0;

  return {
    schemaVersion: SCHEMA_VERSION,
    gameTimeHoursAtAnchor: currentHours,
    anchorUnixSeconds: now,
    revision: prevRevision + 1,
    cycleConfigVersion: CYCLE_CONFIG_VERSION,
    realSecondsPerGameDay: REAL_SECONDS_PER_GAME_DAY,
  };
}

function reAnchorForRead(stored, now) {
  // Re-anchor tại thời điểm read (docs/08 §3.1) → KHÔNG ghi lại Game Data.
  const rate = stored.realSecondsPerGameDay;
  const elapsed = Math.max(0, now - stored.anchorUnixSeconds);
  const hoursPerSecond = 24.0 / rate;
  return {
    schemaVersion: stored.schemaVersion,
    gameTimeHoursAtAnchor: wrapHours(
      stored.gameTimeHoursAtAnchor + elapsed * hoursPerSecond
    ),
    anchorUnixSeconds: now,
    revision: stored.revision,
    cycleConfigVersion: stored.cycleConfigVersion,
    realSecondsPerGameDay: stored.realSecondsPerGameDay,
  };
}

async function loadStored(cloudSaveApi, projectId) {
  // DataApi signature: getCustomItems(projectId, customId, keys?, after?, options?)
  // Trả về data.results[]; key chưa có → results rỗng (HTTP 200).
  const res = await cloudSaveApi.getCustomItems(projectId, CUSTOM_ID, [KEY]);
  const item = (res.data.results || []).find((i) => i.key === KEY);
  return item ? item.value : null;
}

async function saveStored(cloudSaveApi, projectId, value) {
  // DataApi signature: setCustomItem(projectId, customId, body, options?)
  await cloudSaveApi.setCustomItem(projectId, CUSTOM_ID, {
    key: KEY,
    value: value,
  });
}

module.exports = async ({ params, context }) => {
  const { projectId } = context;
  const cloudSaveApi = new DataApi(context);
  const now = nowUnix();

  // Lỗi non-404 (auth/5xx/rate-limit…) → throw, KHÔNG nuốt (docs/03 §12).
  const stored = await loadStored(cloudSaveApi, projectId);

  let authoritative;
  if (needsMigration(stored)) {
    authoritative = migrateAndAdvance(stored, now);
    // Ghi lỗi → throw; KHÔNG trả snapshot giả.
    try {
      await saveStored(cloudSaveApi, projectId, authoritative);
    } catch (err) {
      throw new Error(
        "[GetWorldTimeSnapshot] Persist bootstrap/migration failed: " +
          (err && err.message ? err.message : err)
      );
    }
  } else {
    authoritative = stored;
  }

  // Read/resync KHÔNG ghi Game Data.
  return reAnchorForRead(authoritative, now);
};
