# DEATH AND DUEL SYSTEM
## Game2D Permadeath Account and Half Loss Duel

> Death là rủi ro PvE có persistence nghiêm ngặt. Duel là session PvP tách biệt, không death và không làm thay đổi tiến trình PvE.

---

## 1. Permadeath PvE

### 1.1. Quy tắc

- Enemy, Boss hoặc Trap gây lethal damage ngoài Safe Zone yêu cầu death record; `PlayerDeathRecorded` chỉ phát sau khi persistence xác nhận.
- Death lock account bằng `death.isDead = true`. Không xóa account thật trên backend.
- Không respawn, revive item, rollback inventory hoặc tự hồi sinh.
- Duel không tạo DeathRecord hoặc `PlayerDeathRecorded` dù HP snapshot thay đổi.

### 1.2. Safe Zone

Safe Zone là runtime state từ Floor data, gồm spawn, NPC/service hub, shop hoặc transition area.

```text
Damage request
  → SafeZone guard
  → if protected: reject damage and give readable feedback
  → else: normal IDamageable resolution
```

- Enemy không spawn trong Safe Zone.
- Safe Zone có thể hồi HP chậm qua service riêng, nhưng không được thay `death.isDead` về false.
- Trigger dùng Collider2D; state không lưu Player Data.
- Chuyển tầng chỉ được khi người chơi đang đứng trong Safe Zone (kiểm theo vị trí) và còn sống; Safe Zone không được thành nút thoát hiểm giữa trận. Điểm vào của mọi tầng phải nằm trong Safe Zone của tầng đó (chi tiết `02` §19).

### 1.3. Critical death flow

```text
HP reaches 0 outside Safe Zone
  → freeze combat input
  → capture DeathRecord from current authoritative player state
  → await IDeathService.RecordDeathAsync(DeathRecord)
  ├─ confirmed: show Death Screen, account stays locked
  └─ unavailable: remain blocked, show Retry or Exit; never resume combat
```

`DeathRecord` tối thiểu:

```text
isDead
deathTime
deathCause                 // Enemy, Boss, Trap
deathSourceId
finalLevel
finalHighestFloor          // tầng cao nhất đã vào (= personalHighestFloor)
finalFloor                 // tầng đang đứng lúc chết (0 = không xác định)
totalPlayTimeSeconds
```

Save death là critical write. Local cache có thể giữ recovery record, nhưng không cho phép player tiếp tục gameplay khi cloud authoritative write chưa xác nhận.

Migration note: code baseline hiện có `OnPlayerDiedAsync(...)`; đây là API legacy. Cần chuyển sang `RecordDeathAsync(DeathRecord)` trong một task death riêng trước khi caller mới dùng contract này. Wave 1A/1B/2B không sở hữu migration đó.

### 1.4. Death Screen

Hiển thị nguyên nhân, nguồn death, play time, level cuối, tầng lúc chết, Floor cao nhất và trạng thái account, chia hai nhóm:

```text
Nguyên nhân: ...
Chết ở tầng: {finalFloor}

Thành tích
Level cuối: ...
Tầng cao nhất: {finalHighestFloor}
Thời gian chơi: mm:ss
```

Bản ghi cũ chưa có `finalFloor` (đọc ra 0): "Chết ở tầng" lấy `finalHighestFloor`. Cả hai bằng 0 thì hiện "?" (không đoán là tầng 1).

| Account state | Nút hợp lệ |
|---|---|
| Guest chưa linked | Tạo account mới, Thoát |
| Official hoặc Guest đã linked | Thoát |
| Death record chưa confirm | Thử lại, Thoát |

`Tạo account mới` phải có confirm dialog nói rõ tiến trình cũ không còn chơi được. Không đặt nút reset ở Settings hoặc Main Menu.

## 2. Account lifecycle

| Loại | Tạo bằng | Reset | Link |
|---|---|---|---|
| Guest | Anonymous sign-in | Có, từ Login/Death Screen | Có, một provider ngoài. |
| Official | Provider identity hoặc Guest đã linked | Không | Đã identity-bound. |

Guest reset flow:

```text
Confirm → sign out old identity → sign in/create new anonymous identity
→ initialize new Player Data → return Login/Main Menu
```

- Account cũ có thể còn tồn tại trên backend; game chỉ ngừng dùng nó.
- Reset chỉ hợp lệ khi account chưa linked và flow user đang ở Login Screen hoặc Death Screen.
- Boot luôn đọc death state trước khi vào gameplay. `isDead = true` phải đi Death Screen, không được load Floor.

## 3. Half Loss Duel

### 3.1. Rule

Mốc 1 chỉ có một mode.

```text
Start: both fighters at 100 percent snapshot HP
Fighting: network authority resolves combat
End: a fighter below 50 percent MaxHealth loses
```

- Kết quả có thể Win, Lose, Draw hoặc Disconnect.
- Không có death, item loss, mastery credit, World State write hoặc Inventory write.
- UI hiển thị vạch 50 phần trăm rõ ràng trên thanh HP.

### 3.2. Match lifecycle

```text
Lobby → Relay Connect → Loading → Countdown → Fighting
→ Round End → Result → Cleanup → Menu
```

Mất kết nối phải đi Result/cleanup, không để scene, Lobby hoặc Relay resource treo.

### 3.3. Build snapshot

PvP đọc player level, equipment, mastery và available Technique một lần trước trận để tạo snapshot. Trong trận không đọc inventory liên tục và không ghi lại equipment thật.

Casual Mốc 1 dùng gần với PvE values. Nếu cần chỉnh stun/damage/movement cho fair play, đặt ở `PvPRuleSet`, không sửa definition PvE tại runtime.

## 4. Challenge

| Room type | `isChallenge` | Lưu history |
|---|---:|---:|
| Casual | false | Không |
| Challenge | true | Có |

Mốc 1 flow:

```text
Creator opens room → gets code → shares code outside game
→ joiner enters code → both ready → duel → result
→ each player persists one bounded history entry → cleanup
```

`challengeHistory` giữ 100 trận gần nhất. Schema JSON duy nhất nằm ở `03 §5`. Entry phải có result từ góc nhìn người xem history với các giá trị `WIN | LOSE | DRAW | DISCONNECT`, thời lượng giây, timestamp Unix, version ruleset và `disconnectReason` nullable. `opponentName` là display snapshot tùy chọn.

Một player không được tự ghi history cho opponent. Mỗi client chỉ ghi entry Player Data của chính nó sau result được authority xác nhận.

## 5. Module boundary

| Module | Responsibility |
|---|---|
| Core Character | Death reason/record, `IDeathService`, Safe Zone domain state. |
| Services | Account reset/link, death persistence, backend adapters. |
| PvE | Damage source, Safe Zone trigger, death request. |
| PvP | Match state, Half-Loss win condition, snapshot, challenge flag. |
| UI | Intent/feedback/death screen/history display only. |

PvP không tham chiếu PvE enemy, Floor, Time hoặc death implementation. Chúng chỉ dùng Core contracts và Services abstraction.

## 6. Test matrix

| Level | Case |
|---|---|
| EditMode | Safe Zone rejects damage; death reason mapping; confirmed death lock; Half-Loss boundaries; history cap. |
| PlayMode | Death Screen after enemy/boss/trap; invisible reset controls for Official; no reset in Main Menu; disconnect cleanup. |
| Device | Guest death/new account; linked account death/exit only; two-device duel; challenge history reload. |
| Failure | Death save unavailable blocks combat; retry succeeds; match disconnect does not create death. |

## 7. Không được làm

- Respawn/revive in PvE.
- Reset Official account or put reset button in Settings/Main Menu.
- Let local fallback unlock a player whose death save is unresolved.
- Let PvP affect death, World State or real inventory.
- Add a second Duel mode before Mốc 3 approval.
- Delete backend account as a substitute for game-level death lock.
