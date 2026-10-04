# TIME SYSTEM DESIGN
## Game2D Authoritative Day Night Cycle

> Time là World State chung, có chu kỳ cấu hình được và không dùng đồng hồ local làm nguồn authority.
> Wave 2B trong `13_TWO_AGENT_IMPLEMENTATION_PLAN.md` sở hữu implementation của tài liệu này.

---

## 1. Mục đích

Ngày/đêm phải tạo lựa chọn content nhưng không được chặn người chơi mobile khỏi quest chính. Nó ảnh hưởng ánh sáng, spawn, NPC, quest phụ và boss variant theo data. Time không phụ thuộc Floor; mỗi Floor chỉ tiêu thụ snapshot time chung.

## 2. Game Time Model

```text
One game day = 1,200 real seconds by default
gameHours per real second = 24 / realSecondsPerGameDay
gameHour range = [0, 24)
```

| Phase | Khoảng giờ game | Mục đích presentation |
|---|---|---|
| Dawn | 05:00–07:00 | Chuyển sáng, visibility dần tăng. |
| Morning | 07:00–11:00 | Ánh sáng rõ, content ngày. |
| Noon | 11:00–13:00 | Ánh sáng mạnh. |
| Afternoon | 13:00–17:00 | Chuyển ấm. |
| Evening | 17:00–19:00 | Telegraph chuyển tối. |
| Night | 19:00–05:00 | Ánh sáng thấp, content đêm. |

`TimeConfig` (client — presentation/sync) có:

```text
dayNightCycleEnabled            // cờ debug client (tắt hiệu ứng theo phase khi test); không ảnh hưởng authority
resyncIntervalSeconds           // default 300
maxSnapshotAgeSeconds           // default 900
```

Config của server function (cùng repo) sở hữu rate và bootstrap:

```text
realSecondsPerGameDay           // default 1200; server dùng để tính giờ từ anchor
startHour                       // chỉ cho bootstrap/new world
cycleConfigVersion              // tăng khi đổi rate hoặc phase boundaries
```

Snapshot chở `realSecondsPerGameDay` (§3.1) nên client dự đoán bằng đúng rate của server. TimeConfig client **chỉ giữ rate/startHour làm giá trị mặc định presentation trước snapshot đầu tiên; không bao giờ là authority**; `startHour`/`cycleConfigVersion` là hằng trong script Cloud Code cùng repo.

`TimePhaseMask` là tập các phase trong bảng §2: Dawn, Morning, Noon, Afternoon, Evening, Night. `allowedTimePhases`/`requiredTimePhases` dùng mask này. Ranh giới phase là bảng hằng `TimePhaseTable` trong `Core/Time`, không phải field config. Khi rate hoặc ranh giới phase đổi, bump `cycleConfigVersion` ở config server. Client lấy `cycleConfigVersion` và `realSecondsPerGameDay` từ snapshot; nếu version trong snapshot lớn hơn hằng `SupportedCycleConfigVersion` (Core/Time) thì chặn time-sensitive content và báo cần cập nhật build.

Test config có thể dùng chu kỳ 60 giây/game day — chỉ đặt ở config server của môi trường test, trả về 1200 trước khi bàn giao. Test config không được ship làm production asset.

## 3. Authority Contract

### 3.1. Snapshot

```text
WorldTimeSnapshot
  schemaVersion: int
  gameTimeHoursAtAnchor: double
  anchorUnixSeconds: long       // server timestamp
  revision: long
  cycleConfigVersion: int
  realSecondsPerGameDay: double // rate của server; client dự đoán bằng giá trị này
```

`world_time` là Game Data bootstrap/read model có schema duy nhất ở `03 §16.4`. Server function đọc anchor, tính giờ hiện tại bằng server time, rồi trả snapshot re-anchor tại server timestamp; nếu key chưa có thì bootstrap một lần theo `startHour`. Snapshot re-anchor từ lần đọc không cần ghi Game Data ở mỗi resync. Client không coi `DateTime.Now` là giờ authoritative.

### 3.2. Interface boundary

```text
ITimeService
  CurrentSnapshot
  CurrentGameTimeHours
  CurrentPhase
  TimeChanged(gameTimeHours, gameDay, phase)
  TimePhaseChanged(currentPhase)
  WorldTimeResynced(snapshot)
  InitializeAsync()
  ResyncAsync()
  Tick(deltaRealSeconds)

IServerFunctionService
  GetWorldTimeSnapshotAsync()
```

`GetWorldTimeSnapshotAsync()` là method chuẩn duy nhất cho read/resync; server tự bootstrap idempotent nếu chưa có snapshot. `UpdateWorldTime(...)` và `InitializeWorldTimeAsync(...)` là tên legacy/đề xuất cũ, không dùng trong implementation mới. `ITimeService.InitializeAsync()` gọi server snapshot khi khởi động; `ResyncAsync()` đọc lại snapshot. PvE `WorldClock` chỉ gọi `ITimeService`, không downcast concrete service và không biết Cloud Code/UGS. Client dự đoán presentation dùng `realSecondsPerGameDay` trong snapshot, không lấy rate từ config client.

### 3.3. Runtime flow

```text
Boot
  → TimeService requests server snapshot
  → calculate predicted game hour from anchor with monotonic elapsed time and snapshot rate
  → publish TimeChanged
  → Tick predicts locally with Stopwatch/delta time
  → interval elapsed or app resume
  → request new server snapshot
  → smooth correct small drift; snap only if drift is material
```

- Client chỉ dự đoán presentation giữa hai lần resync bằng monotonic elapsed time, không lấy đồng hồ thiết bị làm nguồn authority.
- Server validates time-sensitive world write bằng server timestamp, không tin client phase.
- Một client không có quyền đổi `world_time` để farm content.
- `revision` đổi khi snapshot persisted/bootstrap/config version đổi; một resync read có thể trả anchor mới hơn cùng revision. Bỏ response có revision thấp hơn, hoặc cùng revision nhưng anchor cũ hơn.

## 4. Content Rules

### 4.1. Lighting

- `DayNightLightController` chỉ nghe `ITimeService`/domain event.
- Light color/intensity nằm trong `TimeConfig` hoặc Floor presentation config, không hard-code trong scene controller.
- Chuyển phase dùng transition ngắn, không blink toàn màn hình.

### 4.2. Spawn

`EnemyDefinition` dùng field chuẩn `allowedTimePhases` (mask) và optional `phaseStatModifiers`. Spawner đánh giá phase khi spawn tick; enemy đã spawn không tự biến mất lúc phase đổi trừ definition yêu cầu despawn an toàn.

Không dùng `if floor == N` cho spawn time rule. Không spawn trong Safe Zone dù allowed phase hợp lệ.

### 4.3. NPC

`NPCDefinition` dùng field chuẩn `allowedTimePhases` (mask). Khi NPC không available:

- Presentation hidden/disabled rõ ràng.
- Collider/interact target cũng phải disabled; không để collider vô hình cản player.
- Quest UI nói rõ NPC trở lại ở phase nào nếu quest liên quan.

### 4.4. Quest

- Quest chính luôn có đường hoàn thành ngoài một cửa sổ thời gian duy nhất.
- Quest time-aware dùng field optional `requiredTimePhases` (mask), được kiểm tra lúc receive, progress và turn-in theo `QuestDefinition`.
- Nếu snapshot quá cũ, quest không tự fail; UI báo state time unknown và không cấp progress cần authority.

### 4.5. Boss

Night variant chỉ được thêm bằng `BossDefinition` data. Nó phải đổi ít nhất một telegraph/pattern hoặc reward choice có thể nhìn thấy; không chỉ nhân HP/Attack rồi gọi là content mới.

## 5. Offline and Failure Behavior

| Tình huống | Hành vi |
|---|---|
| Không lấy được snapshot lúc boot | Có thể hiển thị snapshot xác nhận gần nhất như giá trị đứng yên và “đang đồng bộ”; không ngoại suy qua process restart. Nếu tuổi không xác định/quá limit, chặn time-sensitive content. |
| Resync thất bại khi đang chơi | Tiếp tục presentation từ snapshot gần nhất trong `maxSnapshotAgeSeconds`; retry theo backoff; không ghi World State. Sau process restart, cần resync thành công trước khi coi tuổi snapshot là đáng tin. |
| Drift nhỏ | Smooth correction để tránh ánh sáng/NPC nhảy bất ngờ. |
| Drift lớn | Update phase ngay, ghi warning telemetry/log và refresh affected content theo rule. |
| Server function unavailable | Không mở boss/time-gated reward state; UI có retry/exit rõ. |

## 6. Data and Save Rules

- World time không nằm trong Player Data.
- `world_time` chỉ server function/Cloud Code được write.
- Snapshot có revision; response revision thấp hơn snapshot hiện có, hoặc cùng revision với anchor cũ hơn, bị bỏ qua.
- Time-related player progress vẫn nằm ở Player Data, nhưng server-sensitive reward không được quyết định bằng clock local.

## 7. Test Matrix

| Loại | Case |
|---|---|
| EditMode | Phase boundary, midnight wrap, rate config, snapshot offset, revision reject. |
| EditMode | Drift correction rule, stale snapshot rule, no concrete-service downcast from PvE. |
| PlayMode | Light transition, spawn allowed mask, NPC collider disable, quest time receive/progress/turn-in. |
| Two account | Hai account nhận cùng phase sau resync; one cannot influence the other by local clock change. |
| Device | 60-second cycle (server config môi trường test) shows a full cycle; production config (1200) restored; app pause/resume resync works. |

## 8. Không được làm

- Dùng local device clock làm authority.
- Client ghi trực tiếp `world_time`.
- Client gửi game hour do chính client tính để server ghi.
- Dùng concrete `TimeService` type check ở PvE.
- Chặn quest chính chỉ vì hiện sai phase.
- Hard-code time rule trong Floor scene hoặc NPC MonoBehaviour.
- Để sprite NPC biến mất nhưng collider/interact logic vẫn sống.
