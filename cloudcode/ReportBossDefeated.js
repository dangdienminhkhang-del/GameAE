// Cloud Code Script — ReportBossDefeated
// Wave 3 (Boss/World State) — ghi nhận boss bị đánh bại, mở Floor tiếp theo
//
// Deploy:
//   ugs cloud-code scripts deploy cloudcode/ReportBossDefeated.js \
//     --project-id <YOUR_PROJECT_ID> --environment-name production
//
// Params: { bossId: string, floorId: string }

const { DataApi } = require("@unity-services/cloud-save-1.0");

const WORLD_BOSSES_KEY = "world_bosses";
const WORLD_FLOORS_KEY = "world_floors";

module.exports = async ({ params, context, logger }) => {
  const { projectId, environmentId } = context;
  const { bossId, floorId } = params;

  if (!bossId || !floorId) {
    throw new Error("bossId và floorId là bắt buộc.");
  }

  // Đọc world_bosses
  let bosses = {};
  try {
    const res = await DataApi.getCustomDataItem(projectId, environmentId, WORLD_BOSSES_KEY, {});
    bosses = res?.data?.value ?? {};
    if (typeof bosses === "string") bosses = JSON.parse(bosses);
  } catch (_) {
    bosses = {};
  }

  // Idempotent: nếu đã DEFEATED thì không ghi lại
  if (bosses[bossId] === "DEFEATED") {
    return { ok: true, alreadyDefeated: true, bossId };
  }

  // Ghi boss DEFEATED
  bosses[bossId] = "DEFEATED";
  try {
    await DataApi.setCustomDataItem(projectId, environmentId, WORLD_BOSSES_KEY, {
      body: { value: bosses },
    });
  } catch (err) {
    throw new Error("Lỗi ghi world_bosses: " + err.message);
  }

  // Mở floor tiếp theo (world_floors)
  let floors = {};
  try {
    const res2 = await DataApi.getCustomDataItem(projectId, environmentId, WORLD_FLOORS_KEY, {});
    floors = res2?.data?.value ?? {};
    if (typeof floors === "string") floors = JSON.parse(floors);
  } catch (_) {
    floors = {};
  }

  floors[floorId] = "CLEARED";
  try {
    await DataApi.setCustomDataItem(projectId, environmentId, WORLD_FLOORS_KEY, {
      body: { value: floors },
    });
  } catch (err) {
    logger.warning("Lỗi ghi world_floors (non-fatal): " + err.message);
  }

  logger.info(`Boss ${bossId} của ${floorId} đã bị đánh bại.`);
  return { ok: true, alreadyDefeated: false, bossId, floorId };
};
