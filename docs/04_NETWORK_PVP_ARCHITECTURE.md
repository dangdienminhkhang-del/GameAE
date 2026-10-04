# NETWORK & PVP ARCHITECTURE
## Game2D — Duel online (Lobby + Relay + Netcode for GameObjects)

> Duel là PHIÊN NHỎ (2 người, 1 phòng), không phải MMO mở. Dùng từ Mốc 1 (Phase 10).
> PvP là module tuỳ chọn: mất mạng hoặc tắt Duel không được làm hỏng PvE.
> Chi tiết Death/Account/Challenge: xem `09_DEATH_AND_DUEL_SYSTEM.md`.

---

## 1. NGUYÊN TẮC CỐT LÕI

> PvP Core + PvP Rules + PvP Mode + Arena + Network Layer phải được tách biệt.

- PvP không phải là một phần mở rộng trực tiếp của Floor PvE.
- PvP Core không phụ thuộc vào một mode cụ thể.
- PvP Rules phải có thể thay đổi độc lập.
- Arena PvP độc lập với Floor PvE.
- Core Combat phải có thể test offline.
- Network Layer không được trộn vào toàn bộ Gameplay Core.
- Server/authoritative layer phải kiểm soát kết quả quan trọng.
- Không copy toàn bộ hệ thống cho từng PvP mode.
- **PvP KHÔNG có permadeath** — Duel chỉ để so kỹ năng.

## 2. NGĂN XẾP CÔNG NGHỆ & BACKEND ABSTRACTION

**Nguyên tắc:** PvP KHÔNG gọi trực tiếp SDK của UGS. PvP gọi interface ở Core. Cài đặt UGS nằm ở `Game.Services`.

### 2.1. Ngăn xếp UGS (cài đặt mặc định)

**Authentication (ẩn danh) → Lobby → Relay → Netcode for GameObjects (NGO)**, tất cả của Unity Gaming Services, đều còn hoạt động và có gói miễn phí. KHÔNG dùng Unity Multiplay — dịch vụ này đã ngừng hoạt động từ 1/4/2026.

- **Lobby**: phòng chờ có mã ngắn để hai người tìm nhau.
- **Relay**: nối hai máy qua Internet mà không cần port-forward.
- **Netcode for GameObjects**: đồng bộ vị trí, animation, HP, hành động thật sự.
- Mô hình **host-client**: một trong hai máy người chơi vừa làm host vừa chơi — không cần thuê server riêng cho Mốc 1–2.

### 2.2. Interface ở Core (PvP gọi)

```text
Game.Core/Network/
├── ILobbyService          ← tạo/join phòng, mã phòng, ready state
├── IRelayService          ← xin allocation, join code
├── INetworkTransport      ← khởi tạo NGO / transport, spawn player
└── IServerFunctionService ← gọi Cloud Code (ReportBossDefeated, GetWorldTimeSnapshotAsync)
```
2.3. Cài đặt UGS ở Services
```text
Game.Services/Network/
├── UGSLobbyService          (implements ILobbyService)
├── UGSRelayService          (implements IRelayService)
├── UGSNetworkTransport      (implements INetworkTransport, dùng NGO)
└── UGSServerFunctionService (implements IServerFunctionService, gọi Cloud Code)
```
2.4. BackendConfig
```text
Game.Services/Config/
└── BackendConfig (ScriptableObject)
    ├── backendType: "UGS" | "Spring" | "Hybrid"
    ├── lobbyProvider: "UGS" | "Custom"
    ├── relayProvider: "UGS" | "Custom"
    └── ...
```
Khi đổi sang Hybrid:

Viết SpringLobbyService, SpringRelayService, SpringNetworkTransport, SpringServerFunctionService (implements cùng interface).

Đổi BackendConfig.backendType = "Hybrid".

PvP KHÔNG phải sửa.

2.5. Quy tắc code PvP
```text
ĐÚNG:
  PvP/Challenge/ChallengeRoomCreator
      ↓
  ILobbyService.CreateRoomAsync()
      ↓
  (cài đặt thật: UGSLobbyService → Unity.Services.Lobby)

SAI:
  PvP/Challenge/ChallengeRoomCreator
      ↓
  Unity.Services.Lobby.CreateLobbyAsync()   ← KHÔNG ĐƯỢC
```
Đăng nhập là điều kiện bắt buộc để gọi Lobby/Relay — đây là cơ chế kỹ thuật hiện thực hoá "phải online mới chơi được".

## 3. PVP ARCHITECTURE OVERVIEW

```text
PVP
│
├── PvP Core
│   ├── Match State
│   ├── Fighter State
│   ├── Win/Lose (Half-Loss)
│   ├── Round
│   └── Battle Result
│
├── PvP Rules
│   ├── Stat Rules
│   ├── Damage Rules
│   ├── Skill Rules
│   ├── Equipment Rules
│   └── PvE → PvP Conversion
│
├── PvP Modes
│   └── Casual Battle (Half-Loss) [Mốc 1]
│
├── PvP Challenge
│   ├── Challenge Flag
│   ├── Challenge History
│   └── Notification [Mốc 2]
│
├── PvP Arena
│   ├── Arena Map
│   ├── Camera
│   ├── Spawn
│   ├── Boundaries
│   └── Environment Rules
│
├── Matchmaking [Mốc 3]
│
├── Battle Room
│
└── Network Synchronization
    ├── Connection
    ├── State
    ├── Input
    ├── Validation
    └── Reconnection
```
4. GÓC NHÌN & THAM CHIẾU LỐI CHƠI
Side-view/fighting view — khác PvE top-down (02 §5). Lối chơi tham chiếu game đối kháng 2D — CHỈ về cơ chế: camera, bộ điều khiển, thanh máu hai bên, nhịp combo/round. Không dùng nhân vật, tên, sprite, âm thanh, UI hay moveset của IP nào (01 §5).

5. MATCH STATE
Một trận đấu cần có state rõ ràng.

Ví dụ:

```text
WAITING
   ↓
MATCH_FOUND
   ↓
ROOM_CREATED
   ↓
PLAYERS_READY
   ↓
COUNTDOWN
   ↓
FIGHTING
   ↓
ROUND_END
   ↓
MATCH_END
   ↓
RESULT
```
Nếu có nhiều round:

```text
FIGHTING
 ↓
ROUND_END
 ↓
NEXT_ROUND
 ↓
FIGHTING
```
Không dùng nhiều boolean rời rạc để biểu diễn match state nếu state machine phù hợp hơn.

MVP Mốc 1: chỉ cần 1 round, không cần best-of-N. Best-of-N là mở rộng Mốc 3.

6. FIGHTER STATE
Mỗi fighter có trạng thái gameplay riêng.

Ví dụ:

```text
IDLE
RUN
JUMP
ATTACK
SKILL
HIT
STUN
KNOCKBACK
DEAD                        ← không dùng ở Mốc 1 vì Half-Loss kết thúc trước khi HP = 0
```
Fighter state phải được đồng bộ đúng giữa hai client/server tùy networking architecture cuối cùng.

7. PVP GAME MODES
PvP phải được thiết kế theo dạng mode.

```text
PvP Core
    │
    └── Casual Battle (Mốc 1–2)
        └── Half-Loss mode
```
Không hard-code Casual Battle vào PvP Core.

8. CURRENT MVP MODE — CASUAL 1V1 (HALF-LOSS)
Mode đầu tiên:

Casual 1v1 Arena — Half-Loss.

Đặc điểm:

2 người chơi.

Arena riêng.

Side-view.

Battle trực tiếp.

Ai còn < 50% HP thì thua.

Không ai chết.

Có thể sử dụng PvE-derived build.

Không ảnh hưởng trực tiếp đến PvE World State.

Kết thúc trận trả Battle Result.

9. RANKED 1V1 (MỐC 3)
Ranked là mode độc lập với Casual.

Khi triển khai:

có rating/rank

matchmaking theo skill/rating

PvP normalization

season nếu cần

leaderboard.

Ranked không bắt buộc phải sử dụng cùng rules với Casual.

10. PVP RULE SET
Mỗi mode có thể sử dụng một Rule Set riêng.

Ví dụ:

```text
Casual (Mốc 1–2)
→ PvE-derived Build (dùng thẳng)
→ Half-Loss

Ranked (Mốc 3)
→ Normalized Stats
→ Có thể dùng mode khác

Tournament (tương lai)
→ Fixed/Standardized Build
```
Do đó không được hard-code:

```text
PvP always uses PvE stats
```
vào Core.

11. PVE-DERIVED PVP BUILD
Casual PvP hiện tại có thể sử dụng dữ liệu từ PvE.

Flow:

```text
Player PvE Data
      ↓
Equipment
      ↓
Skills
      ↓
Character Build
      ↓
PvP Conversion Rules
      ↓
PvP Fighter
```
PvE data được dùng làm nguồn build.

Nhưng PvP không nhất thiết sử dụng nguyên giá trị PvE.

12. PVP STAT CONVERSION
Ví dụ:

```text
PvE:
ATK = 500
DEF = 300
HP = 5000

        ↓

PvP Conversion (Ranked, Mốc 3)

ATK = 120
DEF = 100
HP = 3000
```
Các hệ số phải được cấu hình, không hard-code rải rác trong nhiều class.

Casual Mốc 1–2 KHÔNG conversion — dùng thẳng build thật (ADR-010).

13. PVP NORMALIZATION (MỐC 3)
Một Rule Set khác có thể chuẩn hóa:

```text
Level
HP
ATK
DEF
Speed
```
Ví dụ:

```text
Player A PvE Power = 5000
Player B PvE Power = 12000

Ranked:
A → normalized
B → normalized
```
Mục tiêu:

Player skill phải có ảnh hưởng lớn hơn chênh lệch farm power.

14. EQUIPMENT IN PVP
Equipment có thể có ba trạng thái:

```text
FULL
Dùng toàn bộ hoặc gần toàn bộ effect.
```

```text
CONVERTED
Chuyển PvE stats sang PvP stats.
```

```text
DISABLED
Equipment chỉ có tác dụng trong PvE.
```

Ví dụ:

```text
Dragon Sword
PvE:
+50 ATK

Casual:
+50 PvP ATK (dùng thẳng)

Ranked:
+15 PvP ATK (converted)
```
Rule này thuộc PvP Rule Set, không thuộc Item Core.

15. SKILL IN PVP
Skill có thể được chia:

```text
Shared Skill
PvE Skill
PvP Skill
```
Một skill PvE có thể:

dùng nguyên bản

được điều chỉnh

bị giới hạn

bị vô hiệu hóa trong PvP.

Ví dụ:

```text
PvE:
Stun = 3 seconds

PvP (Ranked):
Stun = 1 second
```
PvP balance không nên sửa trực tiếp PvE skill behavior nếu không cần.

16. PVP ARENA
Arena là battle environment riêng.

Arena gồm:

```text
Arena
├── Map
├── Camera
├── Spawn Points
├── Boundaries
├── Collision
└── Environment Rules
```
Không dùng trực tiếp Floor map cho PvP.

Arena KHÔNG dùng Time System — Duel là phiên ngắn, luôn ở điều kiện ánh sáng cố định.

17. SIDE-VIEW FIGHTING CAMERA
PvP MVP sử dụng side-view.

Đặc điểm:

camera ngang.

hai fighter nằm trong cùng battle space.

camera tập trung vào hai fighter.

giới hạn arena.

hỗ trợ jump/dash/attack tùy combat design.

Camera PvP không phụ thuộc Camera PvE.

18. PVP MOVEMENT
PvP movement có thể khác PvE.

PvE:

```text
Free movement (top-down)
```
PvP:

```text
Left
Right
Jump
Dash
Attack
Skill
```
PvP Fighter Controller không nên phụ thuộc trực tiếp vào Player Movement Controller của PvE.

19. HITBOX / HURTBOX
PvP combat cần phân biệt:

```text
Hitbox
Hurtbox
Attack Range
Projectile
Area of Effect
```
Các thành phần này phải được thiết kế deterministic càng nhiều càng tốt.

## 20. LUỒNG MỘT TRẬN DUEL

```text
LOBBY (tạo phòng / vào phòng bằng mã, cả hai bấm sẵn sàng)
   ↓
   ILobbyService.CreateRoomAsync() / JoinRoomAsync()
   ↓
RELAY_CONNECT (host xin Relay allocation → sinh join code; client dùng code kết nối)
   ↓
   IRelayService.CreateAllocationAsync() / JoinAllocationAsync()
   ↓
LOADING (load scene Arena, spawn 2 Fighter theo Build Snapshot)
   ↓
   INetworkTransport.StartHost() / StartClient()
   ↓
COUNTDOWN → FIGHTING → ROUND_END → RESULT
   ↓
CLEANUP (rời Lobby, đóng Relay allocation) → về Menu
   ↓
   ILobbyService.LeaveRoomAsync() / IRelayService.DisposeAsync()
```
Mất kết nối giữa chừng → RESULT ghi "disconnect", dọn phòng, không kẹt màn hình chờ.

21. MATCHMAKING (MỐC 3)
Flow:

```text
Player enters PvP
       ↓
Select Mode
       ↓
Select/confirm rules
       ↓
Matchmaking Queue
       ↓
Opponent Found
       ↓
Create Match
       ↓
Create Room
       ↓
Load Arena
```
Matchmaking phải độc lập với Arena gameplay.

Mốc 1–2: vào phòng bằng mã Lobby ngắn, chưa cần matchmaking tự động (ADR-007).

22. BATTLE ROOM
Mỗi trận có một Room/Match ID.

Ví dụ:

```text
Match ID
Room ID
Player A
Player B
Game Mode
Rule Set
Arena
Match State
isChallenge          ← MỚI
duelMode             ← MỚI ("Half-Loss")
```
Room tồn tại trong thời gian trận đấu.

Sau khi trận kết thúc:

```text
MATCH_END
 ↓
RESULT
 ↓
ROOM CLOSED
```
23. NETWORK AUTHORITY
PvP không được tin tưởng hoàn toàn vào client.

Client gửi:

```text
Input / Action
```
Server hoặc authoritative simulation quyết định:

```text
Valid?
 ↓
Movement
 ↓
Attack
 ↓
Hit
 ↓
Damage
 ↓
Result
```
Không để client tự gửi:

```text
"I dealt 99999 damage"
```
và server chấp nhận trực tiếp.

Mốc 1–2 (host-client): host là authority. Chấp nhận được cho đồ án môn học (04 §42).

24. NETWORK DATA
Chỉ đồng bộ những dữ liệu cần thiết.

Ví dụ:

```text
Player position
Direction
Action
Animation state nếu cần
Skill usage
Attack state
HP
Status
Round state
Match state
```
Không gửi toàn bộ database Player liên tục.

25. SERVER VALIDATION
Server cần validate:

match membership

action timing

cooldown

skill availability

legal movement

damage result

match state

player state.

Mục tiêu:

Client không thể tự ý sửa kết quả trận đấu.

26. LATENCY
PvP architecture phải tính tới network latency.

MVP có thể sử dụng:

server authoritative state

interpolation

client prediction nếu cần

reconciliation nếu cần.

Không nên triển khai kỹ thuật network phức tạp trước khi gameplay offline ổn định.

27. RECONNECTION
Nếu một player mất kết nối:

```text
CONNECTED
 ↓
DISCONNECTED
 ↓
RECONNECT WINDOW
```
Nếu reconnect thành công:

```text
RESUME MATCH
```
Nếu timeout:

```text
FORFEIT
```
Thời gian reconnect là configurable.

Mốc 1: reconnect tối giản — nếu timeout quá ngắn thì ghi disconnect và kết thúc trận.

28. MATCH RESULT
Khi trận kết thúc:

```text
Winner
Loser
Reason
Duration
Mode
Arena
Timestamp
isChallenge          ← MỚI
duelMode             ← MỚI
```
Nếu cần:

```text
Damage dealt
Kills
Combo
MVP
```
Kết quả được gửi về server và lưu theo Data Architecture.

29. PVP REWARDS
PvP reward phải tách khỏi combat logic.

Ví dụ:

```text
Match Result
     ↓
Reward Service
     ↓
XP / Rank / Item (nếu có)
```
Combat không trực tiếp cộng tiền vào database.

Mốc 1: có thể không cần reward, chỉ cần ghi thống kê vào Challenge History.

30. PVP DOES NOT DIRECTLY MODIFY PVE WORLD
Một trận PvP:

```text
Player A vs Player B
```
không được làm thay đổi:

Floor Boss state

Floor clear state

Enemy state

Dungeon state

Time state

Death state (không ai chết trong PvP)

trừ khi một game mode tương lai cố tình thiết kế mechanic đó.

31. PVP AND INVENTORY
PvP không được tự ý sửa Inventory.

Nếu Casual cho phép equipment:

```text
Inventory
 ↓
Build Snapshot
 ↓
PvP Match
```
Tạo một snapshot/build representation để trận đấu sử dụng.

Không cần liên tục đọc Inventory trong lúc combat.

32. PVP BUILD SNAPSHOT
Khi match bắt đầu:

```text
Player PvE State
       ↓
Create PvP Build Snapshot
       ↓
Apply PvP Rules
       ↓
Fighter initialized
```
Ví dụ:

```text
PvP Build
├── HP
├── ATK
├── DEF
├── Speed
├── Skills
└── Equipment Effects
```
Sau khi match kết thúc, snapshot bị hủy.

Casual Mốc 1–2: snapshot gần như giữ nguyên giá trị PvE (ADR-010).

33. FUTURE PVP MODES
Architecture phải cho phép thêm:

```text
1v1
2v2
Tournament
Training
Boss Challenge
Team Battle
```
mà không phải viết lại PvP Core.

Không cần triển khai các mode này trong MVP.

34. PVP CORE VS PVP MODE
Phải phân biệt:

PVP CORE
```text
Match
Fighter
Combat Integration
Round
Result
```
PVP MODE
```text
Casual (Half-Loss) [Mốc 1]
Ranked [Mốc 3]
Tournament [tương lai]
```
PVP RULE
```text
Stats
Equipment
Skills
Damage
Time
```
NETWORK
```text
Connection
Synchronization
Validation
Reconnection
```
Các lớp này không được trộn thành một hệ thống duy nhất.

35. GAMEPLAY / NETWORK SEPARATION
Core combat nên có thể chạy offline/test.

Ví dụ:

```text
Combat Core
     ↑
     │
 ┌───┴────┐
 │        │
 PvE     PvP
          │
       Network
```
Network không được là dependency bắt buộc của mọi combat code.

Điều này giúp:

test combat offline

debug dễ hơn

phát triển PvE trước

giảm độ phức tạp MVP.

### 35.1. Backend Abstraction trong PvP

PvP không được gọi trực tiếp `Unity.Services.Lobby`, `Unity.Services.Relay`, `Unity.Netcode`.

**Đúng:**
```text
PvP → ILobbyService / IRelayService / INetworkTransport → Game.Services → UGS SDK
```
Sai:

```text
PvP → Unity.Services.Lobby / Unity.Services.Relay / Unity.Netcode
```
Lý do: cho phép đổi backend (Spring, Multiplayer Services SDK mới) mà không sửa PvP. Chỉ cần viết cài đặt mới ở Game.Services và đổi BackendConfig.

### 35.2. Kiểm tra tự động (khuyến nghị)
Trong CI hoặc pre-commit, kiểm tra:

Game.PvP KHÔNG có using Unity.Services.Lobby;

Game.PvP KHÔNG có using Unity.Services.Relay;

Game.PvP KHÔNG có using Unity.Netcode; (trừ Game.PvP/Network/ wrapper)

Nếu vi phạm → build fail.

36. PVP DEVELOPMENT ORDER
Không làm toàn bộ PvP một lần.

Phase 1
Offline PvP prototype:

```text
Fighter A
vs
Fighter B
```
Không network.

Phase 2
Arena:

camera

boundaries

spawn

collision.

Phase 3
Combat:

attack

hitbox

damage

skill

Half-Loss win condition (không KO).

Phase 4
PvP Rules:

normalization (Mốc 3)

PvE-derived build (Mốc 1–2)

equipment

skill restrictions.

Phase 5
Online Room:

create room

join room

ready.

Phase 6
Realtime synchronization.

Phase 7
Matchmaking (Mốc 3).

Phase 8
Result/reward + Challenge History.

Phase 9
Reconnect/error handling.

37. CHANGING PVP GAMEPLAY
Nếu sau này thay đổi cách solo:

Ví dụ:

"Từ Half-Loss chuyển sang best-of-three."

Chỉ cần ảnh hưởng:

```text
Match Rules
Round System
Result System
UI
```
Không được tự tiện sửa:

```text
Inventory
PvE Floor
Quest
Enemy AI
Database Core
Time System
Death System
```
nếu không liên quan.

38. PVP CHANGE IMPACT ANALYSIS
Trước mỗi thay đổi lớn, AI phải phân tích:

```text
Change:
PvP becomes Best-of-3

Affected:
- Match State
- Round Manager
- Result
- UI

Possibly affected:
- Rewards

Not affected:
- Inventory
- PvE Combat
- Floor
- Boss
- Time
- Death
```
Sau đó mới implement.

39. BUG FIX WORKFLOW
Khi PvP có bug:

```text
Bug
 ↓
Reproduce
 ↓
Determine:
Client?
Server?
Combat?
Rule?
Arena?
Network?
 ↓
Find root cause
 ↓
Minimal fix
 ↓
Offline test
 ↓
Online test
 ↓
Regression
```
Không sửa đồng thời nhiều hệ thống nếu chưa xác định nguyên nhân.

40. NETWORK BUG CLASSIFICATION
Bug cần được phân loại:

Gameplay Bug
Ví dụ:

Skill gây sai damage.

Synchronization Bug
Ví dụ:

Player A thấy đối thủ ở vị trí khác Player B.

Authority Bug
Ví dụ:

Client tự thay đổi HP.

Match State Bug
Ví dụ:

Match đã kết thúc nhưng client vẫn đánh được.

Connection Bug
Ví dụ:

Reconnect không khôi phục trận.

Việc phân loại giúp giới hạn phạm vi sửa code.

41. PVP SECURITY PRINCIPLES
Không tin tưởng:

client damage

client HP

client reward

client match result

client inventory ownership.

Server phải kiểm tra dữ liệu quan trọng.

42. PVP LIMITATIONS ĐÃ BIẾT
Mô hình host-client nghĩa là host cũng đang chơi, nên không công bằng tuyệt đối (host có lợi thế độ trễ nhỏ) — CHẤP NHẬN ĐƯỢC cho đồ án môn học. Công bằng tuyệt đối cần dedicated server, để dành Mốc 3.

43. LIÊN KẾT VỚI WORLD STATE
Kết quả Duel KHÔNG ảnh hưởng World State (boss/tầng) hay Inventory thật — hai hệ thống độc lập. Xem 03 §6–13.

44. PVE VÀ PVP TÁCH BIỆT
PvE (Floor, quái, quest, dungeon, boss, World State, Time, Death, SafeZone) và PvP (Lobby, Relay, Arena, Duel, Challenge) chỉ dùng chung Core + Game.Services (stats, combat, skill, account). Không để logic PvP làm hỏng PvE; mất mạng khi vào Duel không được crash cả game.

45. KHÔNG TRIỂN KHAI (trừ khi được yêu cầu)
Battle royale, nhiều người cùng một arena, voice chat, anti-cheat phức tạp, matchmaking nâng cao/ranked, dedicated server riêng, nhiều Duel Mode ở Mốc 1 (chỉ Half-Loss), permadeath trong PvP — để dành Mốc 3.
**Gọi trực tiếp UGS SDK từ code PvP** — phải qua `ILobbyService`/`IRelayService`/`INetworkTransport`

46. TARGET PVP ARCHITECTURE
```text
                         PVP
                          │
              ┌───────────┴───────────┐
              │                       │
           PVP CORE                 NETWORK
              │                       │
       ┌──────┼──────┐          ┌─────┼─────┐
       │      │      │          │     │     │
     Match Fighter Result      Sync Validation
       │      │
       └──────┘
              │
         PVP RULES
              │
       ┌──────┴────────┐
       │               │
   PvE-derived      Normalized
      Build           Build
    (Mốc 1–2)        (Mốc 3)
       │               │
       └──────┬────────┘
              │
          PVP MODES
              │
       ┌──────┴──────┐
       │             │
    Casual         Ranked
  (Half-Loss)     (Mốc 3)
    (Mốc 1–2)        │
       │             │
       └──────┬──────┘
              │
          PVP CHALLENGE
              │
       ┌──────┴──────┐
       │             │
   Challenge      Casual
     Room          Room
       │             │
	└──────┬──────┘
                 │
        ┌──────┴──────┐
       │            	  │
   ILobbyService   INetworkTransport
   IRelayService
       │
       ↓
   Game.Services
       │
   ┌───┴────┐
   │           │
  UGS        Spring
 (Mốc 1)   (tương lai)
   │           │
   └───┬────┘
        │
   BackendConfig
```
47. CORE DESIGN GOAL
Build the PvP system as a reusable platform rather than a single fixed 1v1 implementation.

The first implementation is:

Casual Side-view 1v1 Half-Loss using PvE-derived builds with minimal conversion.

However, the architecture must allow the game to later change to:

fully normalized PvP

different combat rules

different arena rules

different match formats

different team sizes

different reward systems

without rewriting the entire PvP system.

48. DUEL MODES (MỐC 1: CHỈ HALF-LOSS)
48.1. Nguyên tắc
Mốc 1: CHỈ 1 mode — Half-Loss.

Không có permadeath trong PvP.

Mục đích: solo để so kỹ năng, không phải sống chết.

Các mode khác (First Strike, Total Loss) để dành Mốc 3 hoặc tương lai.

48.2. Half-Loss
Định nghĩa: Ai còn dưới 50% HP thì thua.

Cơ chế:

Bắt đầu trận: cả hai có 100% HP.

Đánh nhau.

Khi một người còn < 50% HP → người đó thua.

Người thắng: còn ≥ 50% HP.

Không ai chết. Sau trận, cả hai về trạng thái bình thường.

Tại sao Half-Loss?

Trận đấu kết thúc nhanh hơn (không cần đánh đến hết HP).

Không có cảm giác "chết".

Khuyến khích kỹ năng phòng thủ.

Phù hợp với PvE có permadeath — PvP là nơi thư giãn.

48.3. Win Condition
```text
FIGHTING
   ↓
Kiểm tra mỗi frame (hoặc mỗi 0.1s):
   - Player A HP < 50%? → B thắng
   - Player B HP < 50%? → A thắng
   - Cả hai cùng < 50%? → ai thấp hơn thua, nếu bằng thì hòa
   ↓
ROUND_END → RESULT
```
48.4. Module
```text
Game.PvP/Core/
├── DuelMode (enum: HalfLoss)
├── IDuelMode (interface)
├── HalfLossMode (implementation)
├── WinCondition (abstract)
└── HalfLossWinCondition (implementation)
```
48.5. UI
Thanh HP hai bên — hiển thị rõ.

Vạch 50% — đường kẻ mờ trên thanh HP để người chơi biết ngưỡng thua.

Cảnh báo khi HP gần 50% (màu đỏ nhấp nháy).

Kết quả: "Bạn thắng" / "Bạn thua" / "Hòa".

48.6. Không được làm
Thêm mode khác ở Mốc 1.

Permadeath trong PvP.

Xử lý "chết" trong PvP.

Ảnh hưởng World State từ kết quả Duel.

49. CHALLENGE SYSTEM (THÁCH ĐẤU)
49.1. Nguyên tắc
Challenge flag phân biệt phòng casual và phòng thách đấu.

Challenge History lưu vào Player Data (100 trận gần nhất).

Mốc 1: đơn giản — flag + mode + history. Không cần notification.

Mốc 2: thêm notification in-game + Accept/Reject UI.

Mốc 3: friend list, bảng thách đấu công khai.

49.2. Phân biệt 2 loại phòng
Loại phòng	Flag	Mục đích	Ghi lịch sử?
Casual Room	isChallenge = false	Đánh chơi, không ghi lịch sử	❌
Challenge Room	isChallenge = true	Thách đấu, ghi lịch sử	✅
49.3. Flow Challenge (Mốc 1)
```text
A muốn thách đấu B
   ↓
A vào PvP → "Tạo phòng thách đấu"
   ↓
Chọn Duel Mode: Half-Loss (chỉ có 1 mode)
   ↓
Sinh mã phòng (Lobby) với isChallenge = true
   ↓
A gửi mã cho B (Zalo, Discord, v.v.)
   ↓
B vào PvP → "Vào phòng" → nhập mã
   ↓
Cả hai bấm "Sẵn sàng"
   ↓
Load Arena → Countdown → FIGHTING
   ↓
Ai còn < 50% HP → ROUND_END → RESULT
   ↓
Ghi Challenge History (Player Data)
   ↓
Cả hai về Menu
```
49.4. Flow Challenge (Mốc 2 — có notification)
```text
A muốn thách đấu B (đang online)
   ↓
A vào PvP → "Thách đấu bạn bè"
   ↓
Chọn B từ friend list (hoặc nhập tên)
   ↓
Chọn Duel Mode: Half-Loss
   ↓
Gửi lời mời qua UGS Lobby
   ↓
B nhận thông báo in-game
   ↓
B chấp nhận → vào phòng tự động
   hoặc
B từ chối → A nhận thông báo
   ↓
Nếu chấp nhận → load Arena → FIGHTING
```
49.5. Challenge History
Lưu vào Player Data theo schema duy nhất ở 03 §5; luật result và giới hạn xem 09 §4. Không định nghĩa JSON schema riêng trong tài liệu PvP này.

Ghi khi: kết thúc trận có isChallenge = true.

Không lưu: realtime movement, attack, HP từng frame.

49.6. UI Challenge
Challenge Room Creator: UI tạo phòng thách đấu (chọn mode, sinh mã).

Challenge History Screen: xem lịch sử 100 trận gần nhất.

Challenge Notification (Mốc 2): popup khi nhận lời mời.

Accept/Reject UI (Mốc 2): nút chấp nhận/từ chối.

49.7. Module
```text
Game.PvP/Challenge/
├── ChallengeFlag (data class)
├── ChallengeRoomCreator (service)
├── ChallengeHistoryRecorder (service)
└── ChallengeNotification (Mốc 2)
```
49.8. Lộ trình
Tính năng
Challenge flag + Half-Loss Mốc 1
Ghi Challenge History	Mốc 1	
Xem lịch sử	Mốc 1	
Notification in-game	Mốc 2
Accept/Reject UI 	Mốc 2
Friend list	 
Bảng thách đấu công khai	Mốc 3
MatchHistory đầy đủ	Mốc 3
Ranked stats	Mốc 3
49.9. Không được làm
Notification ở Mốc 1 (để Mốc 2).

Friend list ở Mốc 1 (để Mốc 3).

Lưu Challenge History không giới hạn.

Ảnh hưởng World State từ Challenge.

Permadeath trong Challenge.
