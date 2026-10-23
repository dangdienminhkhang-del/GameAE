# TWO AGENT IMPLEMENTATION PLAN
## Game2D Parallel Work Contract

> Mục tiêu: hai AI làm song song nhưng không sửa cùng responsibility, cùng file hay cùng public contract.
> Tài liệu này là luật phân công. Nó không thay `AGENTS.md`, `06_CODE_ARCHITECTURE_AND_MODULE_RULES.md` hay review của người phát triển.

---

## 1. Cách dùng

1. Chọn đúng một Wave và đúng một Work Package cho mỗi AI.
2. Mỗi AI chỉ sửa paths được cấp quyền trong package của mình.
3. AI đọc input contract trước khi mở code. Nếu contract thiếu, ghi câu hỏi vào handoff; không tự đổi architecture.
4. Mỗi AI kết thúc bằng: changed files, public contract changed/not changed, tests, Unity Editor steps, APK evidence, known issues.
5. Integrator chạy merge checklist ở §8 trước khi bắt đầu Wave kế tiếp.

Không có hai AI cùng làm một work package. Không chia một class, prefab, scene hay ScriptableObject giữa hai AI.

## 2. Ownership và dependency

```text
Wave 0: Documentation freeze and interface contracts
                          ↓
Wave 1A: Quest/NPC/mobile hardening ∥ Wave 1B: Floor framework
                          ↓
Wave 2A: Weapon Technique system ∥ Wave 2B: Authoritative Time system
                          ↓
Wave 4: One-agent integration and two-agent regression
```

Wave 3: Boss/World integration — chỉ bắt đầu sau khi Wave 1B và 2B đã merge
        (chung file IServerFunctionService/UGSServerFunctionService với 2B);
        được phép song song với Wave 2A vì không chung file.
Task B (Integrator): Balance baseline sandbox — trước nghiệm thu Wave 2A và trước Phase 11 (07 §5–6, §9).
Task D (Integrator): migrate `OnPlayerDiedAsync` → `RecordDeathAsync(DeathRecord)`, giữ API cũ `[Obsolete]` một phase — sau Wave 0, trước Wave 3 (09 §1.3).
Khi chỉ có một AI làm: 1A → 2B → 1B → 2A → Task D → Wave 3 → Wave 4; Task B xong trước khi nghiệm thu 2A.

- Mỗi package là một vertical slice có thể build/test riêng.
- Package ở cùng Wave không được thêm dependency lên package còn lại trong Wave đó.
- Nếu một package cần API của package song song, đưa API vào Wave 0. Không chờ AI kia “code xong rồi xem”.
- Một AI có thể đọc toàn repo, nhưng chỉ write vào owned paths.

## 3. Quy tắc file an toàn

| Phạm vi | Quy tắc |
|---|---|
| C# | Sửa đúng owned paths. Không format/rewrite file ngoài phạm vi. |
| Docs | Mỗi package chỉ cập nhật section/file được giao; `01` và `13` chỉ Integrator sửa. |
| Scene/Prefab/asset | AI không sửa YAML thủ công. Tạo checklist Unity Editor để người phát triển wire/assign. |
| asmdef/package | Chỉ Integrator được sửa sau architecture review rõ ràng. |
| Backend | PvE/PvP gọi interface Core; chỉ `Game.Services` gọi SDK cụ thể. |
| Generated files | Không sửa `Library`, `Temp`, `.csproj`, `.slnx`, `.meta` độc lập. |

## 4. Contract freeze Wave 0

Integrator xác nhận các contract dưới đây trước khi giao việc. Đây là contract để AI code không cần đoán.

Wave 1A owns Quest event declarations in `Core/Events/GameEvents.cs` plus their publishers in `PvE/Enemy/EnemyController.cs` and `PvE/Items/PlayerInventory.cs`; it must add the ID/quantity payloads required by `12 §11`. Wave 2B owns the time event surface on `Core/Time/ITimeService.cs` (`TimeChanged`, `TimePhaseChanged`, `WorldTimeResynced`). Wave 2B may read the existing time event block in `GameEvents.cs` but must not edit that file. This keeps the packages independent.

Time code baseline also has `WorldTime.gameTimeHours/lastSyncRealTimeUnix` and `IServerFunctionService.UpdateWorldTimeAsync(double)`. Wave 2B owns migration to the snapshot/API frozen in §4.3; preserve migration behavior for old data if the key exists. Death API legacy is documented in `09 §1.3` and is outside these three packages.

### 4.1. Quest contract

```text
QuestStatus: Locked, Available, Active, ReadyToTurnIn, Completed
QuestDefinition: ID, objective, targetId, requiredCount, prerequisites,
                 floor requirement, time rule, reward, repeat rule
Quest authority: Quest service/logic owns state; UI only sends accept/turn-in intent.
Collect: accept seeds progress = min(required, owned count); a consume turn-in
         with insufficient quantity is refused and returns to Active.
```

### 4.2. Floor contract

```text
FloorDefinition: ID, display data, scene reference/key, recommended level,
                 world unlock rule, spawn set, NPC set, quest set, safe zones,
                 boss reference, scaling, item tier
Floor loader input: floor ID
Floor loader result: loaded playable floor or explicit blocked/error state
```

Floor ID phải theo `floor.NN`; không sinh ID từ display name. Boss progression reference dùng `boss.floor_NN` (03 §2).

### 4.3. Time contract

```text
ITimeService must expose read-only current snapshot and resync operation.
IServerFunctionService.GetWorldTimeSnapshotAsync() returns the server snapshot and
idempotently bootstraps it if absent; clients never submit a game hour to write.
No PvE code may downcast ITimeService to a concrete service.
The snapshot carries realSecondsPerGameDay; the client predicts presentation with
the snapshot rate. TimeConfig client **chỉ giữ rate/startHour làm giá trị mặc định
presentation trước snapshot đầu tiên; không bao giờ là authority**; the production
rate/startHour/cycleConfigVersion live in the Cloud Code scripts constants at repo
`cloudcode/` (same repo).
```

Canonical snapshot schema is `03 §16.4`; canonical TimeConfig fields are in `08 §2`.
Legacy `UpdateWorldTime(...)` and proposed `InitializeWorldTimeAsync(...)` are not implementation contracts.

### 4.4. Technique contract

```text
WeaponTechniqueDefinition is static data.
Skill executor validates equipment, mastery, cooldown and combat state.
Core resolves hit/damage/cooldown; presentation listens to events.
```

### 4.5. Shared event contract

Wave 1A/2B use the domain event names and payloads in `12 §11`. In particular, quest Collect consumes `ItemAcquired(itemId, quantity)` only after a positive inventory addition commits; time consumers use `TimeChanged`, `TimePhaseChanged`, and `WorldTimeResynced` for their separate semantics. Do not invent `PhaseChanged` aliases.

### 4.6. Stable ID contract

Floor IDs use `floor.NN`; boss progression IDs use `boss.floor_NN`. These preserve the existing baseline IDs. Display names never generate IDs. See `03 §2`. `NN` is the floor number, zero-padded to at least 2 digits, no upper bound; validation `^floor\.\d{2,}$` and `^boss\.floor_\d{2,}$`; floors sort by `floorNumber`, never by the ID string.

### 4.7. Safe Zone and death boundary

Safe Zone rejects a `DamageRequest` before `IDamageable`; protected damage leaves HP unchanged. Lethal PvE damage outside the zone calls `IDeathService.RecordDeathAsync(DeathRecord)`. Emit `PlayerDeathRecorded` only after persistence confirms. Existing `OnPlayerDiedAsync(...)` is a migration source, not the frozen target API (see `09 §1.3`).

Only Integrator changes the contract text. A coding AI may propose a change using the form in §7, but must not implement it until confirmed.

## 5. Work packages

### Wave 1A Quest NPC Mobile Hardening

**Owner:** AI A  
**Goal:** make the Phase 6 quest loop truthful on Android: receive → progress → turn in → reward → persistent reload.

| Field | Contract |
|---|---|
| Owned code | `Core/Quest/**`, Quest event declarations only in `Core/Events/GameEvents.cs`, `PvE/Quest/**`, `PvE/NPC/**`, `UI/Quest/**`, event emission only in `PvE/Enemy/EnemyController.cs` and `PvE/Items/PlayerInventory.cs`, matching tests. |
| Owned docs | `02` quest section and `03` quest save schema section only. |
| Read-only | Time, Floor, Combat, Services, and non-Quest event declarations in `Core/Events/GameEvents.cs`. |
| Must implement | `ReadyToTurnIn`; prerequisite/time validation; target-specific Collect; turn-in reward; a touch interaction path; serial/queued quest saves. |
| Must not implement | New Floor logic, time synchronization, new weapon system, boss/world changes. |
| Required tests | status transitions; wrong target ignored; time gate; save ordering; reload after Active and after Completed. |
| Device evidence | Touch accept, touch quest log, touch turn-in, close/reopen app and verify saved state. |

**Handoff output:** exact event names used, data migration behavior for legacy quest states, Unity objects the developer must connect.

### Wave 1B Floor Framework

**Owner:** AI B  
**Goal:** load Floor 1 and a second data-only floor through the same framework.

| Field | Contract |
|---|---|
| Owned code | `Core/Floor/**`, `PvE/Floor/**`, `UI/Floor/**`, matching tests. |
| Owned docs | `02` floor section and `05` Phase 7 section only. |
| Read-only | Quest, Time, Skills, Boss, Services; use their already frozen interfaces only. |
| Must implement | Floor definition/database validation; unlock read through world-state interface; explicit loading/error state; spawn/NPC/quest registration from data; Safe Zone references from data. |
| Must not implement | Boss defeat server write, skill mastery, direct UGS calls, per-floor `if` branches. |
| Required tests | unknown ID; locked floor; Floor 1 load; Floor 2 added without Core/PvE code change. |
| Device evidence | Select/load Floor 1, reject locked floor, login second account and see correct read-only world state. |

**Handoff output:** asset creation checklist and the exact FloorDefinition fields that must be assigned in Unity Editor.

### Wave 2A Weapon Technique System

**Owner:** AI A  
**Goal:** implement the first two Blade Techniques using data and a generic executor.

| Field | Contract |
|---|---|
| Owned code | `Core/Skills/**` (executor/controller + khai báo event `TechniqueMasteryChanged` trên mastery interface — như `ITimeService` giữ time event), `PvE/Skills/**`, `PvE/Dodge/**`, `UI/Skills/**`, matching tests. `Core/Combat` giữ damage/hit/health, không chứa Technique executor. TechniqueController/DodgeController là component mới, người phát triển gắn vào Player prefab trong Unity Editor; không sửa file Player hiện có; nếu cần hook vào PlayerCombat thì gửi Architecture Change Request. |
| Owned docs | `07` technique sections only. |
| Read-only | Existing Core combat primitives, Items, Floor, Time, UI. |
| Must implement | mastery persistence DTO contract via existing save abstraction; definition validation; cooldown; wind-up/active/recovery; hit instance protection; mastery cap per enemy; dodge action chung theo `DodgeDefinition` (07 §4.4, ADR-025). |
| Must not implement | A new resource system, a custom executor per Technique, PvP balance changes, direct inventory mutations. |
| Required tests | locked technique rejected; mastery unlock; cooldown; multi-hit total; duplicate hit prevention; save/load mastery. |
| Device evidence | both Techniques usable with touch; invalid state feedback; close/reopen preserves mastery. |

### Wave 2B Authoritative Time System

**Owner:** AI B  
**Goal:** replace local-only time with a World State snapshot and server-authorized resync.

| Field | Contract |
|---|---|
| Owned code | `Core/Time/**` (including WorldTimeSnapshot DTO), `Core/Network/IServerFunctionService.cs`, `Services/Time/**`, `Services/Network/UGSServerFunctionService.cs`, `PvE/Time/**`, `UI/Time/**`, `cloudcode/GetWorldTimeSnapshot.js` (script chứa hằng `realSecondsPerGameDay`, `startHour`, `cycleConfigVersion`), matching tests. |
| Owned docs | `08` and Time sections in `03` only. |
| Read-only | Quest, Floor, Skills, Boss, and `Core/Events/GameEvents.cs`. |
| Must implement | startup snapshot; deterministic offset; periodic resync; server-function read/bootstrap path; explicit unavailable/fallback behavior; `ITimeService` time events; interface-only WorldClock usage; prediction uses the `realSecondsPerGameDay` rate carried in the snapshot (08 §3.1). |
| Must not implement | quest gating, time-specific content, direct Game Data write from PvE. |
| Required tests | phase boundaries; offset/wrap; stale snapshot; failed resync; two-account comparison. |
| Device evidence | two real accounts see same phase; force 60-second test cycle only in test config; normal config restored before handoff; script đã deploy lên UGS (người phát triển deploy, AI liệt kê bước); hai account thật gọi script đã deploy. |

### Wave 3 Boss World Integration

**Owner:** Integrator or one AI only after Waves 1B and 2B merge  
**Goal:** boss defeat changes global World State once, safely, and unlocks next floor for other accounts.

| Field | Contract |
|---|---|
| Owned code | `Core/World/**`, `PvE/Boss/**`, `cloudcode/ReportBossDefeated.js`, necessary owned Services World/Network adapters, matching tests. |
| Owned docs | `02` boss section and `03` World State section. |
| Must implement | idempotent defeat request, player reward claim separate from world clear, failure/retry UI, second-account verification. |
| Must not implement | direct World State writes from PvE, account death changes from boss flow, Phase 10 duel scope. |

### Phase 9.5 — ba work package, làm tuần tự 9.5a → 9.5b → 9.5c

> Nguồn phạm vi: `05` Phase 9.5. Một AI làm một package; xong package này mới làm package kế (§1). 9.5a làm trước vì hình vẽ 8 hướng (`14` §4.1) phụ thuộc vào nó.
> Bổ sung 2026-10-16 (product owner chọn): hướng nhân vật nhìn là vector 8 hướng kiểu game MOBA di động (như Liên Quân). Đã ghi vào `05` Phase 9.5 mục 0 và `10` FB-032, FB-033.
> Người làm package đọc: `AGENTS.md`, `docs/01`, `docs/06`, `docs/12`, `docs/13`, `docs/14` §2 và §4, phần Phase 9.5 trong `docs/05`.

#### Wave 9.5a Hướng nhìn 8 hướng

**Owner:** một AI  
**Goal:** đòn đánh thường, Né và Kỹ thuật đi đúng theo hướng nhân vật đang nhìn ở mọi góc (không chỉ trái/phải). Animator nhận được hướng để sau này gắn hình 8 hướng mà không phải sửa code.

| Field | Contract |
|---|---|
| Owned code (mới) | `Core/Facing/**`: `Direction8`, `FacingMath`, `FacingTracker`, `HitboxMath`, `AimAssist`; test EditMode tương ứng. |
| Owned code (sửa tối thiểu) | `PvE/Player/PlayerController.cs`, `PvE/Player/PlayerAttack.cs`, `PvE/Dodge/DodgeController.cs`, `PvE/Skills/TechniqueController.cs`. Chỉ sửa những dòng dùng `Facing` hoặc hướng; không đổi gì khác. |
| Owned docs | Đoạn mô tả hướng đánh / hướng Né trong `07` §4 (nếu có). Không sửa phần khác. |
| Read-only | Enemy, Boss, Floor, Quest, Time, Items, Services, UI (kể cả `OnScreen*Button`), `Core/Input`. |
| Must implement | (1) `Direction8` (Đông, Đông-Bắc, Bắc, Tây-Bắc, Tây, Tây-Nam, Nam, Đông-Nam; bước 45°, bắt đầu từ trục +X ngược chiều kim đồng hồ). `FacingMath`: `FromVector` (làm tròn tới hướng gần nhất, vector gần 0 thì trả hướng dự phòng), `ToVector`, `ToArtSlot` (trả một trong 5 khe hình S, SE, E, NE, N kèm cờ `flipX`; các hướng phía Tây là lật). (2) `FacingTracker` thuần C#: giữ vector hướng đã chuẩn hóa, mặc định (1, 0); chỉ đổi khi độ lớn vượt ngưỡng chết; thả joystick thì giữ hướng cũ. (3) `PlayerController`: thêm `FacingDirection` (Vector2 chuẩn hóa), `Facing8` (Direction8), `SetFacingDirection(Vector2)`. `Facing` (int ±1) và việc lật `flipX` giữ nguyên hành vi hiện tại. (4) `PlayerAttack`: tâm vùng đánh = `HitboxMath.Center(vị trí, FacingDirection, offset)`, tức `vị trí + FacingDirection * offset.x + (0, offset.y)`. Khi hướng là (±1, 0) kết quả phải trùng từng con số với công thức cũ (`x + offset.x * facing`, `y + offset.y`). (5) `DodgeController`: hướng lướt = `FacingDirection` đã chuẩn hóa thay cho `(facing, 0)`; `Rigidbody2D.Cast` và quãng đường dùng cùng vector đó. (6) `TechniqueController`: tâm vùng đánh theo `HitboxMath`; hướng dịch chuyển Step/Dash/Commit theo `FacingDirection`, cả nhánh có Rigidbody2D lẫn nhánh dự phòng. (7) Animator: `PlayerController` đặt hai float `FaceX` (giá trị tuyệt đối, đã làm tròn theo 8 hướng) và `FaceY` mỗi FixedUpdate, **chỉ khi Animator có đủ hai tham số** (kiểm một lần ở Awake, không để cảnh báo lặp mỗi khung hình). (8) Tự nhắm tùy chọn (`AimAssist`) trong `PlayerAttack`: trường `autoAim` (mặc định **tắt**) và `autoAimRadius` (mặc định 2.5). Khi bật, lúc bấm đánh chọn mục tiêu còn sống gần nhất trong bán kính (lọc theo `TargetLayers`), quay `FacingDirection` về phía nó rồi mới tính vùng đánh; không có mục tiêu thì dùng hướng đang nhìn. (9) Tìm trong `Scripts/` mọi chỗ khác còn dùng `Facing` hoặc `flipX`; ghi vào handoff chỗ nào để nguyên, chỗ nào đã sửa. |
| Must not implement | Hướng 8 chiều cho quái và boss (làm ở gói hook art, `14` §7). Đổi dead zone joystick (thuộc 9.6). Animation event gây sát thương. Sửa prefab/scene (người phát triển tự thêm `FaceX`, `FaceY` vào Animator). asmdef mới. Thêm trường vào `AttackDefinition` hay `WeaponTechniqueDefinition`. Đổi ý nghĩa của `HitboxOffset.y`. |
| Required tests | `Direction8`: đủ 8 hướng và các biên 22.5°. Vector bằng 0 thì trả hướng dự phòng. `ToArtSlot`: 8 hướng ra đúng 5 khe và cờ lật. `FacingTracker`: ngưỡng chết, giữ hướng khi thả, chuẩn hóa. `HitboxMath`: trùng công thức cũ ở (±1, 0) với vài offset; ở 8 hướng độ dài `offset.x` được giữ nguyên. `AimAssist`: chọn gần nhất; ngoài bán kính thì không chọn; bỏ qua mục tiêu null hoặc đã chết; hai mục tiêu cùng khoảng cách thì chọn theo thứ tự ổn định. |
| Device evidence | Quái đứng ngay trên, dưới và chéo thì đòn thường trúng. Né theo 8 hướng đi đúng hướng, không xuyên tường. Kỹ thuật Traverse đi theo hướng nhìn. Thả joystick thì hướng nhìn giữ nguyên. Tự nhắm tắt và bật cho kết quả khác nhau đúng như mô tả. Các case P7.5 đã Pass vẫn Pass. Ghi vào bảng P9.5, case (g) và (h) (`05`). |

**ACR (đã được product owner yêu cầu 2026-10-16):**

```text
Current contract: hướng nhân vật là int Facing = ±1 (PlayerController); PlayerAttack, DodgeController, TechniqueController dùng nó để đặt vùng đánh và hướng lướt chỉ theo trục ngang.
Problem proved by: PlayerAttack.ResolveHitboxCenter (x + offset.x * facing, y + offset.y); DodgeController.TryDodge (new Vector2(facing, 0)); TechniqueController.ApplyWindowMovementOnce và ResolveHitboxCenter. Quái nằm ngay trên hoặc dưới người chơi thì đòn dễ trượt; Né và Traverse chỉ đi ngang (đã ghi nhận ở lỗi 'lướt chỉ ngang').
Smallest proposed change: thêm FacingDirection (vector chuẩn hóa) và dùng nó ở ba nơi trên; giữ Facing int như cũ.
Affected modules/files: bốn file C# ở bảng trên, thêm Core/Facing/**.
Migration/save impact: không; không đổi dữ liệu lưu.
Tests added or changed: mục Required tests ở trên.
Alternatives rejected: giữ trái/phải (không đạt yêu cầu góc nhìn); đổi sang nhân vật 3D (đổi kiến trúc, không hợp Mốc 1).
```

**Handoff output:** danh sách dòng đã đổi; bước Unity Editor còn lại (thêm tham số Float `FaceX`, `FaceY` vào Animator Controller của Player, chưa bắt buộc cho tới khi có hình 8 hướng); các chỗ còn dùng `Facing` int.

#### Wave 9.5b Phản hồi trúng đòn

**Owner:** một AI, sau khi 9.5a đã merge  
**Goal:** đánh quái thấy số, thấy nháy, có dừng hình ngắn, quái tan khi chết, có thanh máu; không phá màu báo đòn (`05` Phase 9.5 mục 1–5).

| Field | Contract |
|---|---|
| Owned code | `Core/Presentation/**` (mới: `PlaySpeedState`, `SpriteTintResolver`, logic pool số sát thương); `PvE/Presentation/**` (mới: `PlaySpeedController`, `SpriteTintController`, `DamageNumberView`, `EnemyHealthBar`); `UI/Combat/**` (thanh máu boss trên HUD); sửa tối thiểu `PvE/Enemy/EnemyController.cs` và `PvE/Boss/BossController.cs`, chỉ ở những chỗ ghi `spriteRenderer.color` (đổi thành gọi `SpriteTintController`; nếu chưa gắn component thì giữ màu cũ để không vỡ prefab hiện có); test tương ứng. |
| Owned docs | `07` phần phản hồi chiến đấu (nếu có) và mục 9.5 trong `05` chỉ ở phần evidence. |
| Read-only | Items, Floor, Quest, Time, Services, Skills, `Core/Events/GameEvents.cs`. Nếu event hiện có thiếu dữ liệu cho số sát thương (con số, chí mạng, vị trí, nguồn là người chơi hay quái) thì gửi ACR trước, không tự thêm. |
| Must implement | (1) Số sát thương nổi dùng pool, hai màu khác nhau cho đòn người chơi gây ra và đòn người chơi nhận. (2) `SpriteTintController` là **điểm duy nhất** đặt màu nhân của sprite; thứ tự ưu tiên **Chết > màu theo state hiện tại > gốc** (ADR-029; màu state: báo đòn quái hồng nhạt; boss Telegraph vàng/cam, PressureWindup/Dash tím, Punish/Recovery xanh nhạt; Cuồng nộ; `14` §7 H2). Các giá trị màu báo đòn giữ **nguyên** như bảng ở `14` §2. Nháy khi trúng KHÔNG còn là lớp màu nhân: là lớp phủ trắng riêng, child SpriteRenderer tên `HitFlash`, alpha 0.85 → 0 trong 0.08 giây; khi đang có báo đòn nguy hiểm (quái windup, boss Telegraph/Pressure) thì alpha tối đa 0.5 để màu báo đòn vẫn đọc được. `TintResolver` hiện có (đang theo thứ tự cũ chết > nháy > màu trạng thái, nháy là màu nhân) phải được chỉnh cho khớp. (3) Dừng hình qua `PlaySpeedState`, nơi duy nhất ghi `Time.timeScale` (tìm trong `Scripts/` để chắc không còn chỗ khác); tối đa 0.06 giây thời gian thực, có giới hạn tần suất, chỉ cho đòn xác nhận của người chơi; đòn người chơi bị trúng thì không dừng hình. (4) Hiệu ứng tan khi chết trong `deathSeconds`; reset đúng khi quái được dùng lại từ pool. (5) Thanh máu trên đầu quái (ẩn khi chết); thanh máu boss trên HUD. |
| Must not implement | Hệ chí mạng mới. Particle nặng. `Instantiate`/`Destroy` trong lúc chiến đấu. PvE tham chiếu UI. Đổi giá trị màu báo đòn. Đổi logic trúng đòn hay sát thương. Sửa prefab/scene YAML. asmdef mới. |
| Required tests | `SpriteTintResolver`: mọi tổ hợp trạng thái cho đúng màu theo thứ tự ưu tiên Chết > màu state hiện tại > gốc. Logic `HitFlash` (thuần số): alpha 0.85 → 0 trong 0.08 giây; trần alpha 0.5 khi đang có báo đòn nguy hiểm (quái windup, boss Telegraph/Pressure); nháy không làm đổi màu state. `PlaySpeedState`: nhiều yêu cầu cùng lúc, hết hạn theo thời gian thực, Pause thắng dừng hình, trần 0.06 giây, giới hạn tần suất. Pool: lấy và trả lại đúng, không cấp phát thêm khi đủ chỗ. Tỉ lệ máu luôn nằm trong 0 đến 1. Reset khi dùng lại từ pool. |
| Device evidence | Bảng P9.5, các case (a), (b), (c), (d), (f), (g) trong `05`. |

**Handoff output:** các bước Unity Editor (prefab thanh máu; prefab số sát thương; **không** cần gắn component màu/nháy hay thêm child `HitFlash` vào prefab Player, Enemy, Boss, vì `SpriteTint` là lớp C# thuần và child `HitFlash` do `HitFlashOverlay` tự dựng lúc chạy, xem “Ghi chú 2026-10-18” bên dưới); danh sách chỗ trong `EnemyController` và `BossController` đã đổi.

#### Wave 9.5c Âm thanh

**Owner:** một AI, sau 9.5b  
**Goal:** có nhạc nền và hiệu ứng âm thanh cho các hành động chính (`05` Phase 9.5 mục 6).

| Field | Contract |
|---|---|
| Owned code | `Core/Audio/**` (`IAudioService`, `AudioCue`, logic âm lượng), `PvE/Audio/**` (dịch vụ phát có pool `AudioSource`, trình phát nhạc, nối event với âm thanh), test tương ứng. |
| Owned assets/docs | `Assets/_Game/Audio/**` do người phát triển nhập; AI ghi từng file vào `Assets/_Game/Art/ASSET_SOURCES.md` (nguồn và giấy phép). |
| Read-only | Mọi thứ còn lại, kể cả `Core/Events/GameEvents.cs`. |
| Must implement | Nhạc nền gameplay và nhạc boss. SFX: vung đòn, trúng, bị trúng, kỹ thuật, né, nhặt, lên cấp, UI. Âm lượng nhạc và SFX lưu local (PlayerPrefs, qua interface để test được). Ra nền thì tắt tiếng, quay lại thì tiếp tục đúng. |
| Must not implement | Âm thanh 3D. Mixer phức tạp. Âm thanh không rõ nguồn hay giấy phép. Màn Settings (thuộc 9.6). Sửa prefab/scene YAML. asmdef mới. |
| Required tests | Nối event với âm thanh đúng. Âm lượng bị kẹp trong 0 đến 1. Hành vi khi ra nền và quay lại. Khóa lưu và đọc đúng. |
| Device evidence | Bảng P9.5, case (e) và (f) trong `05`. |

**Handoff output:** danh sách file âm thanh cần nhập, bước Unity Editor, các dòng đã thêm vào `ASSET_SOURCES.md`.

#### Ghi chú 2026-10-18: K1 đã gộp 9.5b + 9.5c

Prompt K1 của product owner đã gộp 9.5b và 9.5c, chạy trước 9.5a, và dùng tên/đường dẫn khác hai bảng trên: `Core/Presentation/**`, `Core/Audio/**`, `Services/Audio/**`, `PvE/Presentation/**` (`PlaySpeedTicker`, `HitStopController`, `CombatSfxRouter`...), `UI/Combat/**`, `UI/HUD/BossHealthBar.cs`, `UI/Audio/**`. Chi tiết ở `05` mục 9.5 "Ghi chú kỹ thuật". Đường dẫn thực tế trong code thắng hai bảng trên. Code đã viết; chờ nối Unity (U1) và evidence P9.5 nên chưa ghi xong. Integrator đối chiếu Gate §8 khi merge. Ưu tiên màu nháy so với báo đòn đã chốt 2026-10-18 (ADR-029): Chết > màu state > gốc, nháy là lớp phủ trắng `HitFlash` riêng (bảng 9.5b ở trên đã sửa theo). `TintResolver` đã được sửa theo ADR-029 (chỉ còn chết > màu state; nháy không còn là màu nhân), test xanh ngày 2026-10-18.

**Quyết định thiết kế (product owner đồng ý 2026-10-18): code thật thắng bảng 9.5b ở phần màu/nháy.** Nháy là child SpriteRenderer `HitFlash` do `PvE/Presentation/HitFlashOverlay` **tự dựng một lần lúc chạy**; `SpriteTint` là lớp C# thuần (không phải component) do `EnemyController`, `BossController`, `PlayerHitFeedback` tạo và gọi. Vì vậy **không cần sửa prefab Player, Enemy, Boss** cho màu/nháy. Lý do: không có bước wire nào để quên (quen thì nháy mất mà không có lỗi), không xung đột YAML khi nhiều AI sửa prefab, và test được bằng GameObject thật. Đánh đổi: child `HitFlash` không hiện sẵn trong prefab (chỉ xuất hiện khi Play), và child dùng `Shader.Find` nên cần kiểm trên APK rằng lớp phủ vẫn trắng sáng ban đêm; nếu bị mất shader thì thêm shader `Universal Render Pipeline/2D/Sprite-Unlit-Default` vào Always Included Shaders.

**Bảng đối chiếu tên** (tên trong bảng 9.5b/9.5c → tên thật trong code): `SpriteTintResolver` → `Core/Presentation/TintResolver`; `SpriteTintController` → `PvE/Presentation/SpriteTint` (lớp thuần) + `HitFlashOverlay` + `HitFlashRules` + `TintKind`; `PlaySpeedController` → `PvE/Presentation/PlaySpeedTicker`; file nguồn âm thanh `Assets/_Game/Art/ASSET_SOURCES.md` → `Assets/_Game/Audio/ASSET_SOURCES.md`.

### Phase 9.6 → 9.9 — tóm tắt work package (ghi 2026-10-18 theo yêu cầu product owner)

> Hợp đồng đầy đủ (đường dẫn sở hữu, chỉ đọc, test bắt buộc, việc Unity Editor) nằm trong prompt K2–K5 của `Phase9_5-9_9_prompts.md`; phần dưới chỉ tóm tắt để Integrator đối chiếu Gate §8. `Core/`, `PvE/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`; `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/`. Làm tuần tự 9.6 → 9.7a → 9.7b → 9.8 → 9.9 (`05` §1 luật 10). Evidence ở các bảng P9.6–P9.9 trong `05`.

**9.6 Nền UX cảm ứng** (cần xong trước: 9.5, dùng `PlaySpeedState` và `IAudioService`)
- Tạo mới: `Core/Settings/**`, `Services/Settings/**`, `Services/Haptics/**`, `UI/Haptics/**`, `UI/Pause/**`, `UI/Settings/**`, `UI/Loading/**`, `UI/Controls/` (`JoystickTouchZone`, `JoystickMath`, `OnScreenPotionButton`).
- Sửa có giới hạn: `OnScreenJoystick`, `SceneLoader` (thêm overload tiến độ), `MainMenuController` và `BootController` (chỉ nối loading), `InventoryPanel`/`QuestPanel`/`FloorSelectPanel` (chỉ `IBackHandler`), `PlayerInventory` (chỉ accessor `IsInCombat`, `CombatLockRemainingSeconds`), `CombatFeelConfig` (thêm field).
- Không làm: đồng bộ Settings lên cloud, dùng bình giữa trận, nút Reset Guest, lưu vị trí, sửa Death Screen/Player/Enemy/Boss/`PlayerHUD`.

**9.7a Bố cục vùng và cổng** (cần xong trước: 9.5, 9.6)
- Tạo mới: `PvE/Zones/**`, `UI/Zones/**`, `Editor/ZoneLayoutValidator.cs`.
- Sửa có giới hạn: `Core/Events/GameEvents.cs` (chỉ thêm `OnZoneEntered` và `ZoneEnteredArgs`), `FloorSpawnRegistrar` (nhiều spawner, giữ nguyên field cũ), `FloorSceneBinder`, `EnemySpawner` (tránh Safe Zone), `EnemyController` (vị trí gốc, dừng đuổi khi người chơi trong Safe Zone), `CameraFollow`, `PlayerQuestLog` và `QuestRules` (chỉ phần Reach).
- Không làm: phòng boss, nhiều scene cộng dồn, teleport, mini-map, thêm field vào `FloorDefinition` (cần ACR), khóa cửa.

**9.7b Hầm Boss và phòng boss** (cần xong trước: 9.7a)
- Tạo mới: `PvE/Boss/BossEncounterRules.cs`.
- Sửa có giới hạn: `BossController` (field `encounterZone`, điều kiện rời Idle, state `ReturnHome`; không đụng luồng chết, báo server, thưởng, Pressure, damage), `ZoneTracker` (API hỏi vùng nếu thiếu).
- Không làm: khóa cửa, hồi máu khi rời phòng, sửa `BossDefinition`, `BossHitRules`, `BossDefeatFlow`.

**9.8 Quái đa vai trò** (cần xong trước: 9.5, 9.7a)
- Tạo mới: `PvE/Enemy/CombatRole.cs`, `PvE/Enemy/Behaviors/**`, `PvE/Enemy/Projectile/**`.
- Sửa có giới hạn: `EnemyDefinition` (chỉ thêm field, mặc định `Chaser`), `EnemyController` (ba điểm nối behavior), `AudioIds` và `CombatSfxRouter` (thêm `sfx.enemy_shoot`).
- Không làm: quái theo giờ ngày/đêm, AI nhóm, boss mới, hành vi riêng cho Guard/Elite, sửa Dodge, đổi asset `EnemyDefinition` cũ.

**9.9 Hướng dẫn và Mũi tên quest** (cần xong trước: 9.5, 9.6, 9.7a; Phần A trước, Phần B cắt trước nếu thiếu thời gian)
- Tạo mới: `Core/Guidance/**`, `Services/Guidance/**`, `UI/Guidance/**`, `UI/Quest/QuestArrow.cs`, `UI/Quest/QuestTargetResolver.cs`.
- Sửa có giới hạn: `UI/Settings/**` (hai field có thể null), component NPC (danh sách static nếu chưa có lookup), panel báo boss P8-5 (chỉ thêm quyền đọc trạng thái).
- Không làm: mini-map, khóa input, đổi chữ hoặc luồng bảng báo boss, thêm event vào `GameEvents`, sửa `QuestRules`/`PlayerQuestLog`/`EnemyController`/`TechniqueController`/`DodgeController`/`PlayerInventory`, lưu cờ gợi ý lên server.

## 6. Task prompt template

Mỗi AI nhận đúng prompt dạng này:

```text
Bạn sở hữu Work Package <ID> trong docs/13.
Đọc: AGENTS.md, docs/01, docs/06, docs/12, docs/13 và các tài liệu được liệt kê trong package.
Chỉ được sửa các paths owned. Không sửa scene/prefab/asset YAML hay asmdef.
Không thay public contract. Nếu thiếu contract, dừng và gửi Architecture Change Request.
Thực hiện implementation nhỏ nhất, chạy các test bắt buộc, kiểm tra Unity Console.
Kết thúc bằng Changed / Why / Tests / Unity Editor steps / Device test / Known issues / Handoff.
```

## 7. Architecture Change Request

Nếu không thể làm đúng package bằng contract hiện có, AI chỉ gửi mẫu này và chờ xác nhận:

```text
Current contract:
Problem proved by:
Smallest proposed change:
Affected modules/files:
Migration/save impact:
Tests added or changed:
Alternatives rejected:
```

Ví dụ hợp lệ: cần thêm `ReadyToTurnIn` vào enum quest. Ví dụ không hợp lệ: “tôi tạo GameManager để làm nhanh”.

## 8. Merge and verification gate

Integrator phải kiểm trước khi merge bất kỳ package nào:

1. Không có file ngoài owned paths bị sửa.
2. Không có `using` SDK backend trực tiếp trong PvE/PvP.
3. Public contract giữ nguyên hoặc có Change Request đã duyệt.
4. EditMode tests liên quan pass.
5. Unity Console không có compile error mới; warning mới phải được giải thích.
6. Hướng dẫn Unity Editor đầy đủ: component, reference, layer, input, build config.
7. Evidence APK đủ cho package đó.
8. Docs owned section phản ánh code đã merge; status không được tự tuyên bố “xong” nếu thiếu device evidence.

## 9. Handoff protocol

AI hoàn thành không tự làm package kế tiếp. Nó gửi handoff có cấu trúc:

```text
Package:
Commit or change list:
Owned files changed:
Public contract:
Tests run and result:
Unity Console result:
Unity Editor actions remaining:
APK/device evidence:
Known limitations:
Suggested next package only:
```

Integrator đọc handoff, kiểm Gate §8, rồi mới giao package mới cho AI còn lại.

## 10. Definition of done toàn dự án

Một phase chỉ được ghi `DONE` ở roadmap khi có implementation, test, Unity Editor wiring, APK evidence và tài liệu khớp nhau. “Code compile” hoặc “scene chạy trong Editor” không đủ.
