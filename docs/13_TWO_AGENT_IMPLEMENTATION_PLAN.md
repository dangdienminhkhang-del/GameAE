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
