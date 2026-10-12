/*
 * Wave 3 — Boss World Integration (docs/02 §21–25, docs/03 §6, docs/13 Wave 3)
 * Cloud Code JavaScript Script: ReportBossDefeated
 *
 * Deploy:
 *   ugs config set project-id <YOUR_PROJECT_ID>
 *   ugs config set environment-name production
 *   ugs deploy cloudcode/ReportBossDefeated.js
 *
 * Cloud Save JS SDK 1.4:
 *   getCustomItems(projectId, customId, keys?, after?, options?)
 *   setCustomItem(projectId, customId, { key, value, writeLock? }, options?)
 *
 * World State is server-owned. A boss write is deliberately completed before
 * the floor write; a retry reconciles a prior partial write instead of treating
 * it as a successful no-op.
 *
 * Tham số khi dán script lên Dashboard (QUAN TRỌNG):
 *   Dashboard KHÔNG đọc tham số khai báo trong script (module.exports.params ở cuối file
 *   chỉ có tác dụng khi deploy bằng UGS CLI). Nên khi dán code lên Dashboard phải THÊM TAY
 *   hai tham số ở mục Parameters:
 *     - bossId  (String, bắt buộc)
 *     - floorId (String, bắt buộc)
 *
 * Chạy thử trên Dashboard (Cloud Code > Scripts > ReportBossDefeated):
 *   1. Tạo Player ID token trước: ở khung Run, nhập Player ID của một tài khoản thật rồi bấm
 *      tạo (generate) token. Không có token thì nút Run không chạy được.
 *   2. Điền tham số: bossId = boss.floor_01, floorId = floor.01.
 *   3. Bấm Run. Kết quả đúng là { ok: true, alreadyDefeated: false, ..., nextFloorId: "floor.02" };
 *      chạy lại lần hai phải ra alreadyDefeated: true (idempotent).
 *
 * Publish (bắt buộc):
 *   Sau khi dán code và thêm tham số, bấm Save rồi bấm Publish. Chưa Publish thì game
 *   (CallEndpointAsync "ReportBossDefeated") vẫn gọi bản cũ hoặc báo không tìm thấy script.
 */

const { DataApi } = require("@unity-services/cloud-save-1.4");

const CUSTOM_ID = "world";
const WORLD_BOSSES_KEY = "world_bosses";
const WORLD_FLOORS_KEY = "world_floors";
const DEFAULT_AVAILABLE_FLOOR_ID = "floor.01";
const MAX_WRITE_ATTEMPTS = 3;
const BOSS_ID_PATTERN = /^boss\.floor_(\d{2,})$/;
const FLOOR_ID_PATTERN = /^floor\.(\d{2,})$/;

function errorMessage(error) {
  return error && error.message ? error.message : String(error);
}

function isConflict(error) {
  const status = error && (
    error.status ||
    error.statusCode ||
    (error.response && error.response.status)
  );
  return Number(status) === 409;
}

function log(logger, level, message) {
  if (logger && typeof logger[level] === "function") {
    logger[level](message);
  }
}

function validateIds(bossId, floorId) {
  if (typeof bossId !== "string" || !BOSS_ID_PATTERN.test(bossId)) {
    throw new Error(
      "[ReportBossDefeated] bossId phải có dạng boss.floor_NN (ví dụ boss.floor_01)."
    );
  }

  if (typeof floorId !== "string" || !FLOOR_ID_PATTERN.test(floorId)) {
    throw new Error(
      "[ReportBossDefeated] floorId phải có dạng floor.NN (ví dụ floor.01)."
    );
  }

  const bossNumber = bossId.match(BOSS_ID_PATTERN)[1];
  const floorNumber = floorId.match(FLOOR_ID_PATTERN)[1];
  if (bossNumber !== floorNumber) {
    throw new Error(
      `[ReportBossDefeated] bossId ${bossId} không thuộc floorId ${floorId}.`
    );
  }

  return floorNumber;
}

function incrementFloorNumber(floorNumber) {
  const digits = floorNumber.split("");
  let carry = 1;

  for (let index = digits.length - 1; index >= 0 && carry; index -= 1) {
    const incremented = Number(digits[index]) + carry;
    digits[index] = String(incremented % 10);
    carry = incremented >= 10 ? 1 : 0;
  }

  return carry ? `1${digits.join("")}` : digits.join("");
}

function getNextFloorId(floorNumber) {
  return `floor.${incrementFloorNumber(floorNumber)}`;
}

function isMap(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function mapItem(results, key) {
  const item = results.find((candidate) => candidate && candidate.key === key);
  if (!item) {
    return { exists: false, value: {}, writeLock: undefined };
  }

  let value = item.value;
  if (typeof value === "string") {
    try {
      value = JSON.parse(value);
    } catch (_) {
      throw new Error(`[ReportBossDefeated] ${key} không phải JSON hợp lệ.`);
    }
  }

  if (!isMap(value)) {
    throw new Error(`[ReportBossDefeated] ${key} phải là object JSON.`);
  }

  return { exists: true, value, writeLock: item.writeLock };
}

async function loadWorldState(cloudSaveApi, projectId) {
  let response;
  try {
    response = await cloudSaveApi.getCustomItems(projectId, CUSTOM_ID, [
      WORLD_BOSSES_KEY,
      WORLD_FLOORS_KEY,
    ]);
  } catch (error) {
    throw new Error(
      `[ReportBossDefeated] Không đọc được World State: ${errorMessage(error)}`
    );
  }

  const results = response && response.data && Array.isArray(response.data.results)
    ? response.data.results
    : null;
  if (!results) {
    throw new Error("[ReportBossDefeated] Phản hồi World State không hợp lệ.");
  }

  return {
    bosses: mapItem(results, WORLD_BOSSES_KEY),
    floors: mapItem(results, WORLD_FLOORS_KEY),
  };
}

async function saveMap(cloudSaveApi, projectId, key, storedItem, value) {
  const itemToSave = { key, value };
  if (storedItem.exists) {
    if (typeof storedItem.writeLock !== "string" || storedItem.writeLock.length === 0) {
      throw new Error(`[ReportBossDefeated] ${key} thiếu writeLock khi cập nhật.`);
    }
    itemToSave.writeLock = storedItem.writeLock;
  }

  await cloudSaveApi.setCustomItem(projectId, CUSTOM_ID, itemToSave);
}

function requirePlayableFloor(floors, floorId) {
  const status = Object.prototype.hasOwnProperty.call(floors, floorId)
    ? floors[floorId]
    : floorId === DEFAULT_AVAILABLE_FLOOR_ID
      ? "AVAILABLE"
      : undefined;

  if (status !== "AVAILABLE" && status !== "CLEARED") {
    throw new Error(
      `[ReportBossDefeated] ${floorId} phải AVAILABLE hoặc CLEARED trước khi báo boss; trạng thái hiện tại: ${status || "MISSING"}.`
    );
  }
}

function reconcileFloors(floors, floorId, nextFloorId) {
  let changed = false;

  if (floors[floorId] !== "CLEARED") {
    floors[floorId] = "CLEARED";
    changed = true;
  }

  // A future floor may already be CLEARED through a later reconciliation.
  // Never regress it to AVAILABLE.
  if (floors[nextFloorId] !== "CLEARED" && floors[nextFloorId] !== "AVAILABLE") {
    floors[nextFloorId] = "AVAILABLE";
    changed = true;
  }

  return changed;
}

function successResponse(alreadyDefeated, bossId, floorId, nextFloorId) {
  return {
    ok: true,
    alreadyDefeated,
    bossId,
    floorId,
    bossStatus: "DEFEATED",
    floorStatus: "CLEARED",
    nextFloorId,
  };
}

module.exports = async ({ params, context, logger }) => {
  const { bossId, floorId } = params || {};
  const playerId = context && context.playerId ? context.playerId : "unknown";
  const requestContext = `playerId=${playerId} bossId=${bossId || "missing"}`;

  log(logger, "info", `[ReportBossDefeated] ${requestContext} received floorId=${floorId || "missing"}.`);
  const floorNumber = validateIds(bossId, floorId);
  const nextFloorId = getNextFloorId(floorNumber);
  const cloudSaveApi = new DataApi(context);

  for (let attempt = 1; attempt <= MAX_WRITE_ATTEMPTS; attempt += 1) {
    try {
      const stored = await loadWorldState(cloudSaveApi, context.projectId);
      const bosses = { ...stored.bosses.value };
      const floors = { ...stored.floors.value };

      requirePlayableFloor(floors, floorId);

      const alreadyDefeated = bosses[bossId] === "DEFEATED";
      if (!alreadyDefeated) {
        bosses[bossId] = "DEFEATED";
        // Preserve the required write order: boss before floor.
        await saveMap(
          cloudSaveApi,
          context.projectId,
          WORLD_BOSSES_KEY,
          stored.bosses,
          bosses
        );
      }

      // Also runs for an idempotent call, repairing a prior partial write.
      if (reconcileFloors(floors, floorId, nextFloorId)) {
        await saveMap(
          cloudSaveApi,
          context.projectId,
          WORLD_FLOORS_KEY,
          stored.floors,
          floors
        );
      }

      log(
        logger,
        "info",
        `[ReportBossDefeated] ${requestContext} confirmed; alreadyDefeated=${alreadyDefeated}; nextFloorId=${nextFloorId}.`
      );
      return successResponse(alreadyDefeated, bossId, floorId, nextFloorId);
    } catch (error) {
      if (!isConflict(error)) {
        throw error;
      }

      if (attempt === MAX_WRITE_ATTEMPTS) {
        throw new Error(
          `[ReportBossDefeated] Xung đột World State sau ${MAX_WRITE_ATTEMPTS} lần thử: ${errorMessage(error)}`
        );
      }

      log(
        logger,
        "warning",
        `[ReportBossDefeated] ${requestContext} write conflict; retry ${attempt + 1}/${MAX_WRITE_ATTEMPTS}.`
      );
    }
  }

  // The loop always returns or throws; keep an explicit fail-closed guard.
  throw new Error("[ReportBossDefeated] Không thể xác nhận World State.");
};

// Khai báo tham số cho UGS CLI khi deploy. Dashboard không đọc dòng này → xem chú thích đầu file.
module.exports.params = {
  bossId: { type: "String", required: true },
  floorId: { type: "String", required: true },
};
