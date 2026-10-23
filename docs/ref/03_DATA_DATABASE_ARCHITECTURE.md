# DATA ARCHITECTURE
## Game2D — Player Data, Game Data, Cloud Save

> Tài liệu QUYẾT ĐỊNH. Backend: Unity Gaming Services (UGS) Cloud Save, từ Mốc 1.
> Chi tiết Death/Account/Challenge: xem `09_DEATH_AND_DUEL_SYSTEM.md`.

---

## 1. BỐN NHÓM DỮ LIỆU

| Nhóm | Là gì | Lưu ở đâu |
|---|---|---|
| Static Definition | Item, quái, skill, quest, floor, boss… dùng chung | ScriptableObject, đi cùng build |
| **Player Data** | Dữ liệu riêng từng người: level, inventory, equipment, quest, tiến độ cá nhân, **trạng thái chết**, **challenge history** | Cloud Save — Player Data (mỗi người tự đọc/ghi của mình, gắn theo phiên đăng nhập) |
| **World State (Game Data)** | Boss/tầng/THỜI GIAN dùng CHUNG cho cả thế giới | Cloud Save — Game Data, Access Class **Default** (ai cũng đọc; **chỉ Cloud Code được ghi**) |
| Runtime State | HP hiện tại, vị trí, AI state, buff tạm, **trạng thái Safe Zone** | Bộ nhớ, KHÔNG lưu |

Quy tắc: Definition không chứa tiến độ người chơi. Player Data chỉ tham chiếu definition bằng ID. **Client không bao giờ ghi thẳng Game Data** — chỉ đọc; ghi phải qua một hàm Cloud Code (server-side), để không ai tự khai "tôi đã hạ boss" từ client đã bị sửa.

**Thời gian game (Time) là Game Data** — toàn cục, mọi người thấy cùng ngày/đêm (08).

**Trạng thái chết (isDead) là Player Data** — cá nhân, mỗi người chết riêng.

**Challenge History là Player Data** — cá nhân, mỗi người có lịch sử riêng.

## 2. ID ỔN ĐỊNH

Dạng chung `type.stable_snake_case`: `item.tide_blade`, `technique.blade_traverse`… Quy ước cố định: Floor dùng `floor.NN` (`floor.01`, `floor.02`); boss progression dùng `boss.floor_NN` (`boss.floor_01`). `NN` là số tầng, zero-pad tối thiểu 2 chữ số, không giới hạn trên; validation `^floor\.\d{2,}$` và `^boss\.floor_\d{2,}$`. Sắp xếp Floor theo `floorNumber`, không theo chuỗi ID. Tên hiển thị không sinh ID. ID không đổi sau khi dùng; nếu đã phát hành ID cũ thì phải có migration map, không âm thầm đổi.

## 3. STATIC DEFINITIONS

| Definition | Trường chính |
|---|---|
| ItemDefinition | id, tên, type, rarity/tier, icon, baseStats, stackable, maxStack, effect, sellValue |
| EnemyDefinition | id, tên, level, stats (HP/ATK/DEF/speed), aiProfile, expReward, dropTable, **allowedTimePhases, phaseStatModifiers** (08 §4.2) |
| BossDefinition | Enemy + phases, mechanics, enrage, mainDrop, rareDropTable, rematchRareMultiplier (mặc định 0.05), firstClearReward (**bonus theo từng account, không unique** — ADR-016); night variant là data theo 08 §4.5, chưa đóng field riêng |
| **WeaponDiscipline** | canonical data enum: `Blade`, `Knives`, `Needle`, `Greatblade`, `Maul`, `Cleaver` (07 §3). Code legacy `WeaponType` chỉ được migrate có kế hoạch. |
| **WeaponTechniqueDefinition** | id, weaponDiscipline, masteryRequired (0/25/55/85), inputSlot, activeWindows, totalMultiplier, cooldown, wind-up, recovery, movement/cancel/hit policy (07 §4) |
| QuestDefinition | id, NPC giao, objectives, rewards, prerequisite, floorRequirement, **requiredTimePhases (optional mask)** (08 §4.4) |
| NPCDefinition | id, tên, dialogue, quest liên quan, **allowedTimePhases** (08 §4.3) |
| DropTableDefinition | danh sách (itemId, tỉ lệ, số lượng min/max) — roll theo `itemTierId` của Floor (07 §7) |
| **FloorDefinition** | id, floorNumber, tên, scene/map, recommendedLevel, unlockRequirement, spawn configs, NPC, quest, boss, reward, **enemyPowerMultiplier, expMultiplier, itemTierId** (07 §6–7), **safeZoneAreas** (09 §1.2). Lighting dùng TimeConfig/Floor presentation data theo 08 §4.1; field riêng chưa được freeze. |
| DungeonDefinition (Mốc 1 tối giản; Mốc 2 đầy đủ) | id, floorId, waves, objectives, boss, reward |
| **TimeConfig** (client) | `dayNightCycleEnabled`, `resyncIntervalSeconds`, `maxSnapshotAgeSeconds`, `materialDriftGameHours` (08 §2–3); TimeConfig client **chỉ giữ rate/startHour làm giá trị mặc định presentation trước snapshot đầu tiên; không bao giờ là authority**. Snapshot chở `realSecondsPerGameDay` authoritative. |
| **SafeZoneConfig** | id, floorId, area (polygon/collider), type (Spawn/NPC/Shop/Transition) (09 §1.2) |
| EventDefinition (hoãn đến Mốc 2+) | điều kiện thời gian/sự kiện |

## 4. FLOOR

Không tạo class/asset riêng cho từng tầng. Thêm Floor N = thêm FloorDefinition + scene + data quái/quest/boss/reward + SafeZoneConfig, đăng ký vào `FloorDatabase`. Số tầng tối đa đọc từ `GameConfig`.

## 5. PLAYER DATA (Cloud Save, cá nhân)

Người chơi đăng nhập (Authentication, ẩn danh — `AuthenticationService.Instance.PlayerId`) → Cloud Save tự gắn Player Data theo người đó. Chia thành vài key thay vì nhồi một key khổng lồ:

```json
// key "profile"
{
  "saveVersion": 1,
  "displayName": "Player",
  "level": 1,
  "exp": 0,
  "accountType": "Guest",          // "Guest" | "Official"
  "isLinked": false,                // đã link với bên thứ 3 chưa
  "linkedProvider": null            // "Google" | "Facebook" | "Apple" | null
}

// key "progress"                ← personalHighestFloor tăng khi vào tầng cao hơn (FloorLoader → PersonalProgress)
{
  "personalHighestFloor": 1,
  "currentFloorId": "floor.01"
}

// key "inventory"
{
  "stacks": [ { "itemId": "item.health_potion", "quantity": 5 } ],
  "instances": [ { "instanceId": "inst_0001", "itemId": "item.iron_sword", "upgradeLevel": 0, "rolledStats": { "atk": 15 } } ]
}

// key "equipment"
{ "weapon": "inst_0001", "armor": null }

// key "weaponProficiency" (legacy key name, canonical semantic = Weapon Mastery)
{ "blade": 25, "knives": 0, "needle": 0, "greatblade": 0, "maul": 0, "cleaver": 0 }

// key "quests"                    ← schema ở §16.1

// key "rewardClaims"              ← schema MỚI (Wave 3 dùng)
{
  "schemaVersion": 1,
  "claims": [
    {
      "type": "FirstClear",        // khóa duy nhất (type, bossId); chỉ ghi claim một lần
      "bossId": "boss.floor_01",
      "claimedAtUnixSeconds": 1696000000
    }
  ]
}

// key "death"                       ← MỚI (09 §1.3)
{
  "isDead": false,
  "deathTime": null,
  "deathCause": null,               // "Enemy" | "Boss" | "Trap"
  "deathSourceId": null,            // id của enemy/boss/trap đã giết player
  "finalLevel": null,
  "finalHighestFloor": null,       // tầng CAO NHẤT đã vào (= personalHighestFloor lúc chết)
  "finalFloor": null,               // tầng ĐANG ĐỨNG lúc chết (0/thiếu = không xác định; bản ghi cũ không có field này)
  "totalPlayTimeSeconds": 0
}

// key "challengeHistory"           ← schema chuẩn được định nghĩa tại đây; luật history ở 09 §4
{
  "schemaVersion": 2,
  "matches": [
    {
      "matchId": "match_001",
      "opponentId": "player_xyz",
      "opponentName": "Player B",
      "result": "WIN",              // "WIN" | "LOSE" | "DRAW" | "DISCONNECT"
      "myHpRemaining": 0.65,
      "opponentHpRemaining": 0.42,
      "durationSeconds": 120,
      "timestampUnixSeconds": 1696000000,
      "disconnectReason": null,
      "rulesetVersion": 1
    }
  ]
}
```

`rewardClaims` chỉ ghi claim một lần với khóa duy nhất (`type`, `bossId`); không ghi drop lặp lại từ rematch. Wave 3 chốt thứ tự ghi claim và cấp item — mặc định ghi claim trước khi cấp item để không double-reward.

instanceId sinh bằng bộ đếm hoặc GUID, không trùng. Trang bị = ItemInstance; consumable/material = stack.

Weapon Technique KHÔNG lưu riêng danh sách "đã mở" — availability suy ra từ weapon mastery của discipline >= `WeaponTechniqueDefinition.masteryRequired` mỗi lần cần dùng, nên không cần field lưu trữ thêm (07 §3–4).

personalHighestFloor bị chặn bởi World State toàn cục (mục 6) — không thể vượt quá tầng đã mở cho cả thế giới, nhưng cũng không tự nhảy tới đó, người chơi vẫn phải tự đi. Giá trị này được cập nhật khi người chơi vào một tầng cao hơn mức đã lưu: `FloorLoader` báo qua callback "đã vào tầng", lớp `PersonalProgress` (Core/World) chỉ tăng và ghi key `progress` (chỉ đổi `personalHighestFloor`, giữ nguyên `currentFloorId`). Lưu lỗi thì giữ giá trị trong bộ nhớ và thử lại ở lần lưu sau; lúc chết, giá trị lấy từ bộ nhớ chứ không đọc lại save. Lưu ý: `currentFloorId` hiện chưa được dùng — game chưa khôi phục tầng đang đứng khi mở lại app (việc tương lai).

Challenge History giới hạn 100 trận gần nhất để tránh phình Player Data. Khi vượt 100, xóa trận cũ nhất.

Khi đọc history dạng cũ, migrate `duration` → `durationSeconds`, `timestamp` → `timestampUnixSeconds`, giữ `result`/`opponentName`, đặt `disconnectReason = null` và `rulesetVersion = 1`; lưu schemaVersion 2 trước khi loại shape cũ.

Khi player chết:

death.isDead = true

death.deathTime = timestamp

death.deathCause = "Enemy" | "Boss" | "Trap"

death.finalLevel = level hiện tại

death.finalHighestFloor = personalHighestFloor (tầng cao nhất đã vào, lấy từ bộ nhớ lúc chết; không bao giờ thấp hơn tầng đang đứng)

death.finalFloor = tầng đang đứng lúc chết (0 = không xác định; bản ghi cũ không có field này đọc ra 0)

Save NGAY lập tức (không đợi định kỳ) — đây là sự kiện critical.

Các key khác (profile, progress, inventory, equipment, weapon mastery trong key legacy `weaponProficiency`, quests) giữ nguyên — không xóa, vì account chưa bị xóa thật. Chỉ có isDead = true khiến account không vào được game.

6. WORLD STATE (Game Data, toàn cục)
```json
// Game Data, key "world_floors", Access Class Default
{ "floor.01": "CLEARED", "floor.02": "AVAILABLE" }

// Game Data, key "world_bosses", Access Class Default
{ "boss.floor_01": "DEFEATED" }

// Game Data, key "world_time", Access Class Default — schema ở §16.4
```
Client đọc `world_floors` và `world_bosses` trực tiếp (Default cho phép đọc công khai); `world_time` phải lấy qua server snapshot endpoint để có server timestamp/re-anchor.

World State writes/bootstrap chỉ qua Cloud Code/server function:

ReportBossDefeated(bossId, floorId): client gọi hàm này sau khi hạ boss; hàm kiểm tra sơ bộ hợp lệ rồi mới cập nhật Game Data.

GetWorldTimeSnapshotAsync(): đọc snapshot chung; server tự khởi tạo snapshot một lần nếu key chưa có. Không nhận game hour do client gửi và không có client write path.

Boss DEFEATED là một lần cho cả thế giới. Sau đó ai cũng đánh lại được qua Boss Rematch / Memory Dungeon để luyện/lấy rare drop, nhưng KHÔNG ghi lại vào World State.

RewardClaim (account nào đã nhận first-clear bonus của boss nào) là Player Data, cá nhân; drop từ rematch không ghi claim.

Death KHÔNG ảnh hưởng World State — người chơi A chết không làm thay đổi boss/tầng của thế giới.

Truy cập qua IWorldStateService (đọc trực tiếp Game Data; ghi gọi Cloud Code) — interface đặt ở Core, cài đặt ở Game.Services (06 §3).

7. TIME DATA (Game Data, toàn cục)
Chi tiết đầy đủ ở 08_TIME_SYSTEM_DESIGN.md §3 và §6.

Tóm tắt:

Key: world_time trong Game Data.

Schema duy nhất: `WorldTimeSnapshot` ở §16.4.

Đọc: client gọi `GetWorldTimeSnapshotAsync` khi khởi động/resync; dự đoán presentation từ anchor bằng monotonic elapsed time và `realSecondsPerGameDay` trong snapshot.

Ghi/bootstrap: chỉ server function tạo snapshot ban đầu hoặc re-anchor khi config cycle đổi. Client không gửi giờ để ghi.

Tần suất resync client: theo `resyncIntervalSeconds`; resync chỉ đọc snapshot server.

Lý do toàn cục: trong game, mọi người chơi thấy cùng ngày/đêm — không thể A thấy sáng, B thấy tối.

Không lưu vào Player Data — đây là World State, không phải cá nhân.

8. DEATH DATA (Player Data, cá nhân) — MỚI
Chi tiết đầy đủ ở 09_DEATH_AND_DUEL_SYSTEM.md Phần 1.

Tóm tắt:

Key: death trong Player Data.

Cấu trúc: xem §5 ở trên.

Ghi khi: player chết (HP = 0 ngoài Safe Zone do Enemy/Boss/Trap).

Đọc khi: BootController khởi động app → kiểm tra isDead.

Nếu isDead = true:

Không cho vào game.

Hiện Death Screen.

Guest: có nút "Tạo tài khoản mới".

Official: chỉ có nút "Thoát".

Save NGAY lập tức khi chết — không đợi định kỳ.

Không xóa các key khác (profile, progress, inventory...) — chúng vẫn còn nhưng account không vào được.

9. ACCOUNT DATA (Player Data + UGS Authentication)
9.1. Phân biệt Account vs Character
Trong Mốc 1, Account và Character là một — mỗi account có một character duy nhất. Nếu chết → account bị khóa → phải tạo account mới.

(Mốc 3 có thể tách Character khỏi Account để cho phép nhiều character trên một account — nhưng Mốc 1 giữ đơn giản.)

9.2. Loại tài khoản
Loại	Tạo bằng	Reset được?	Link được?	Lưu ở đâu
Guest	SignInAnonymouslyAsync()	✅	✅	UGS Player ID (ẩn danh)
Official	Google/Facebook/Apple	❌	—	UGS Player ID (liên kết)
9.3. Trường trong Player Data
```json
// key "profile"
{
  "accountType": "Guest",           // "Guest" | "Official"
  "isLinked": false,
  "linkedProvider": null            // "Google" | "Facebook" | "Apple" | null
}
```
accountType = "Guest" → hiện nút Reset ở Login Screen và Death Screen.

accountType = "Official" → không hiện nút Reset.

isLinked = true → account đã link với bên thứ 3 → coi như Official.

9.4. Guest Reset
Flow:

```text
Guest Account → bấm "Reset Guest" (Login) hoặc "Tạo tài khoản mới" (Death)
   ↓
Xác nhận
   ↓
SignOutAsync() → xóa Player ID cũ (trên client)
   ↓
SignInAnonymouslyAsync() → tạo Player ID mới
   ↓
Player Data reset (khởi tạo mới)
   ↓
Về Login Screen / MainMenu
```
Lưu ý UGS:

UGS KHÔNG cho phép xóa account thật từ client.

Cách làm: SignOutAsync() → SignInAnonymouslyAsync() tạo Player ID mới.

Player ID cũ vẫn tồn tại trên server Unity nhưng không dùng nữa. Chấp nhận được cho Mốc 1.

10. CHALLENGE HISTORY DATA (Player Data, cá nhân) — MỚI
Chi tiết đầy đủ ở 09_DEATH_AND_DUEL_SYSTEM.md §4 (Challenge).

Tóm tắt:

Key: challengeHistory trong Player Data.

Cấu trúc: xem §5 ở trên.

Ghi khi: kết thúc trận Duel có isChallenge = true.

Giới hạn: 100 trận gần nhất.

Không lưu: realtime movement, attack, HP từng frame.

Mốc 1: ghi đơn giản.

Mốc 3: mở rộng thành MatchHistory đầy đủ + Ranked stats.

11. SAFE ZONE DATA
Safe Zone KHÔNG lưu vào save — chỉ là runtime state.

Định nghĩa: trong FloorDefinition.safeZoneAreas (ScriptableObject).

Runtime: SafeZoneService (Core) kiểm tra player có trong vùng nào không.

Cơ chế: dùng Collider2D trigger, không check mỗi frame.

Khi vào Safe Zone: isInSafeZone = true.

Khi ra khỏi Safe Zone: isInSafeZone = false.

Không lưu vào Player Data — khi khởi động lại, player spawn ở Safe Zone mặc định của Floor.

12. ĐỒNG BỘ & LỖI MẠNG
Ghi Player Data khi: nhận thưởng quan trọng, level up, hoàn thành quest, clear boss/tầng, chuyển scene, pause/thoát app, chết.

Khi chết: ghi NGAY lập tức (không đợi định kỳ) — đây là sự kiện critical.

Giữ một bản cache trong bộ nhớ lúc đang chơi; đồng bộ lên Cloud Save tại các mốc trên. Ghi thất bại (mất mạng) → thử lại hoặc báo lỗi rõ ràng, KHÔNG âm thầm mất tiến trình.

Mốc 1–2 giả định có mạng khi chơi; không cần xây hệ thống đồng bộ ngoại tuyến phức tạp.

saveVersion + hàm migrate khi đổi cấu trúc Player Data.

Time sync thất bại: tiếp tục hiển thị snapshot đã xác nhận gần nhất trong giới hạn tuổi; không dùng giờ thiết bị làm authority. Nếu tuổi không xác định/quá cũ/không có snapshot, chặn hành động content cần time authority và cho retry.

Death sync thất bại: thử lại 3 lần; nếu vẫn fail, lưu local + báo lỗi. Không cho player tiếp tục chơi.

13. PvP DATA
Build Snapshot khi vào Duel: lấy từ Player Data hiện tại (level, weapon mastery, equipment, available Technique) + RuleSet (04 §4) — Casual gần như dùng thẳng, KHÔNG sửa Inventory/Equipment thật. Kết quả trận (thắng/thua) ghi vào challengeHistory nếu là Challenge Room. MatchHistory đầy đủ, Ranked, Matchmaker tự động → Mốc 3.

PvP KHÔNG dùng Time Data — Duel là phiên ngắn, không cần ngày/đêm.

PvP KHÔNG dùng Death Data — Duel chỉ Half-Loss, không có permadeath.

14. MỐC 3 — MỞ RỘNG (chưa chốt công nghệ)
Server chạy 24/7 cho world nhiều người thấy nhau live (dedicated server hoặc nhà cung cấp thứ ba, vì Multiplay của Unity đã ngừng), Matchmaker cho Duel, phân tích/telemetry, tách Character khỏi Account (nhiều character/account). Chọn công nghệ khi bắt đầu Mốc 3.

15. KHÔNG ĐƯỢC LÀM
Client ghi thẳng vào Game Data (World State, bao gồm Time) — phải qua Cloud Code.

Bảng/asset riêng cho mỗi Floor hoặc mỗi Player. ItemDefinition riêng cho từng bản sở hữu. Lưu tiến độ trong Definition. Lưu runtime state.

Lưu danh sách Technique đã mở riêng — availability suy ra từ weapon mastery.

Nhồi toàn bộ Player Data vào một key khổng lồ khi có thể chia hợp lý.

Lưu Time vào Player Data (Time là toàn cục).

Dùng giờ thiết bị/lịch thực làm authority. Đổi chu kỳ sang 1:1 thời gian thật (86400) chỉ là đổi config server: bump cycleConfigVersion và test hai account, không cần ADR mới.

Xóa account thật trên UGS — chỉ khóa logic (isDead = true).

Xóa Player Data khi chết — giữ nguyên, chỉ đánh dấu isDead.

Lưu Safe Zone vào save — Safe Zone là runtime, không persistent.

Lưu Challenge History không giới hạn — giới hạn 100 trận.

Lưu realtime PvP state (movement, attack, HP từng frame) vào save.

## 16. DATA CONTRACT UPDATE

Các contract ở mục này được ưu tiên khi tài liệu cũ dùng thuật ngữ hoặc flow cũ. Chi tiết gameplay xem `12`; ownership AI xem `13`.

### 16.1. Quest persistence

```json
// key "quests"
{
  "schemaVersion": 2,
  "quests": [
    {
      "questId": "quest.example",
      "status": "Active",
      "progress": 2,
      "acceptedAtWorldTime": 8.25,
      "readyToTurnInAt": null,
      "completedAt": null
    }
  ]
}
```

- Allowed status: `Locked`, `Available`, `Active`, `ReadyToTurnIn`, `Completed`.
- Legacy `Completed` có progress đủ nhưng chưa reward phải migrate thành `ReadyToTurnIn`; chỉ legacy state đã có reward evidence mới giữ `Completed`.
- `QuestDefinition` giữ rule tĩnh. Save chỉ giữ state của player, không copy reward/prerequisite/target data.
- Quest writer phải serial hóa writes theo player và key `quests`, hoặc dùng revision tăng dần để response cũ không ghi đè state mới.

### 16.2. Weapon mastery migration

Khái niệm chuẩn là `Weapon Mastery` theo `Weapon Discipline`. Save key hiện có `weaponProficiency` có thể được giữ trong Mốc 1 để tương thích dữ liệu cũ, nhưng value phải bị giới hạn 0–100 và semantics theo `07 §3`.

Khi Wave 2A triển khai, migration phải:

1. Đọc save version cũ và clamp/convert value hiện hữu theo bảng migration được chốt trước khi ship.
2. Không xóa data cũ trước khi save mới xác nhận thành công.
3. Không lưu danh sách Technique đã mở; availability luôn suy ra từ mastery + definition.

### 16.3. Weapon Technique Definition

`WeaponTechniqueDefinition` thay cho model kỹ năng vũ khí cũ. Nó là Static Definition và có tối thiểu:

```text
id, weaponDiscipline, masteryRequired, inputSlot, cooldownSeconds,
windUpSeconds, activeWindows, recoverySeconds, totalMultiplier,
movementPolicy, cancelPolicy, hitProfileIds, pveRules, pvpRules
```

Không lưu `activeWindows`, multiplier, cooldown hoặc unlock list vào Player Data.

### 16.4. World time snapshot

```json
// Game Data key "world_time"
{
  "schemaVersion": 3,
  "gameTimeHoursAtAnchor": 14.5,
  "anchorUnixSeconds": 1696000000,
  "revision": 42,
  "cycleConfigVersion": 1,
  "realSecondsPerGameDay": 1200
}
```

- `realSecondsPerGameDay` nằm trong snapshot: client dự đoán presentation bằng rate này; TimeConfig client **chỉ giữ rate/startHour làm giá trị mặc định presentation trước snapshot đầu tiên; không bao giờ là authority**.
- Đây là schema duy nhất của Game Data key `world_time`. Khi gặp legacy fields `gameTime`/`lastSyncRealTime`, code DTO `gameTimeHours`/`lastSyncRealTimeUnix`, hoặc schemaVersion 2 `gameTimeHours`/`writtenAtUnixSeconds`, giữ game hour hợp lệ, bỏ timestamp cũ làm authority, re-anchor bằng server timestamp hiện tại, gán cycle config version và `realSecondsPerGameDay` hiện tại từ config server rồi tăng revision. Nếu game hour không hợp lệ, bootstrap từ `startHour`.
- Client dự đoán giờ presentation theo anchor và monotonic elapsed time; không dùng đồng hồ thiết bị để xác thực quest/reward/world write.
- Client không được ghi key này trực tiếp.
- `IServerFunctionService.GetWorldTimeSnapshotAsync()` trả snapshot hiện tại; nếu chưa có, server function bootstrap một snapshot ban đầu theo `startHour` một cách idempotent.
- Endpoint tính giờ hiện tại từ anchor đã lưu bằng server time, trả snapshot mới anchor tại thời điểm server trả lời; read/resync không ghi Game Data định kỳ. Không có API nhận giờ do client tính/gửi.
- Server timestamp là nguồn anchor. `revision` tăng khi persisted snapshot/bootstrap/config version đổi; response cùng revision nhưng anchor cũ hơn anchor đã nhận cũng phải bị loại.
- Nếu snapshot/resync không có, dùng snapshot xác nhận gần nhất chỉ để presentation trong `maxSnapshotAgeSeconds`; quá hạn thì không cho thực hiện content cần time authority.

### 16.5. Save severity

| Loại write | Ví dụ | Chính sách |
|---|---|---|
| Critical | death, reward turn-in, boss reward claim | Xác nhận server/cloud trước khi mở state cuối; retry/exit nếu không xác nhận. |
| Ordered | quest progress, inventory, mastery | Serialize per key hoặc revision compare. |
| Best effort | HUD preference, noncritical UI state | Có thể retry nền, không được ảnh hưởng combat. |

`ISaveService` vẫn là abstraction duy nhất cho Player Data. Gameplay không gọi Cloud Save SDK trực tiếp.
