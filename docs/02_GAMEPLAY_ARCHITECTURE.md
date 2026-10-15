# GAMEPLAY ARCHITECTURE
## Game2D — 2D Action RPG leo tầng (10 Floors MVP → 100 Floors)

> File này định nghĩa kiến trúc gameplay. Số liệu cụ thể ở `07`. Time System chi tiết ở `08`. Death & Duel chi tiết ở `09`.

---

# 1. MỤC ĐÍCH

Tài liệu này định nghĩa kiến trúc gameplay của game.

Mục tiêu:

- Gameplay code được chia thành các module độc lập.
- Core gameplay được dùng chung cho toàn bộ Floor.
- Floor-specific content chủ yếu được cấu hình bằng data.
- Dễ sửa gameplay mà không làm hỏng hệ thống không liên quan.
- Dễ debug.
- Dễ mở rộng từ 10 → 100 Floor.
- Có thể tái sử dụng core combat cho PvE và PvP.
- Có thể phát triển MVP trước rồi mở rộng thành đồ án tốt nghiệp.
- **Có hệ thống ngày/đêm độc lập, ảnh hưởng gameplay nhưng không phá vỡ cân bằng.**
- **Có permadeath trong PvE, có Safe Zone bảo vệ, có Guest Reset để dễ tiếp cận.**

Nguyên tắc quan trọng:

> CORE SYSTEMS + DATA/CONFIGURATION = GAMEPLAY

Không copy toàn bộ gameplay code cho từng Floor.

---

# 2. GAMEPLAY ARCHITECTURE OVERVIEW

Gameplay được chia thành các nhóm chính:

```text
GAMEPLAY
│
├── Player
├── Combat
├── Character Stats
├── Skills
├── Enemy
├── Boss
├── Item
├── Inventory
├── Equipment
├── Quest
├── NPC
├── Dungeon
├── Floor
├── Progression
├── World State
├── Time/Event              ← 08
├── Death/SafeZone          ← 09
├── Account                 ← 09
└── Save
```

Các hệ thống này không được gom toàn bộ vào một GameManager duy nhất.

3. CORE GAMEPLAY
Core gameplay là các hệ thống được sử dụng xuyên suốt toàn bộ game.

Bao gồm:

Player

Character Stats

Movement

Combat

Damage

Health

Enemy

Skill

Item

Inventory

Equipment

Quest

NPC

Dungeon

Boss

Progression

Time (ITimeService)

Death (IDeathService)

SafeZone (SafeZoneService)

Các hệ thống này phải có khả năng được tái sử dụng ở nhiều Floor.

4. PLAYER ARCHITECTURE
Player nên được chia thành các trách nhiệm riêng.

Ví dụ:

```text
Player
├── PlayerController
├── PlayerMovement
├── PlayerCombat
├── PlayerStats
├── PlayerSkillController
├── PlayerInventory
├── PlayerEquipment
├── PlayerInteraction
└── PlayerDeathHandler    ← MỚI (gọi IDeathService khi HP = 0)
```
Không tạo một:

```text
PlayerManager.cs
chứa toàn bộ gameplay logic của Player.
```

5. PLAYER MOVEMENT
PvE dùng góc nhìn top-down 2D, di chuyển tự do bằng joystick ảo trên điện thoại (bàn phím trong Editor). Duel (PvP) dùng side-view riêng, xem 04.

Movement chịu trách nhiệm:

nhận input

di chuyển

giới hạn movement

interaction với environment

animation state liên quan đến movement.

Movement không chịu trách nhiệm:

damage

inventory

quest

database

PvP matchmaking.

death (death do PlayerDeathHandler xử lý)

6. CHARACTER STATS
Character Stats chịu trách nhiệm quản lý các chỉ số.

Ví dụ:

HP

Max HP

Attack

Defense

Speed

Critical Chance

Critical Damage

Mana/Stamina nếu cần.

Số liệu khởi điểm (công thức Level, EXP curve): 07 §1.

Stats có thể được tạo từ:

```text
Base Stats
+
Level
+
Equipment
+
Skill/Buff
+
Temporary Effects
```
Không hard-code các giá trị balance vào nhiều script khác nhau.

7. COMBAT ARCHITECTURE
Combat được chia thành các thành phần nhỏ:

```text
Combat
├── Attack
├── Hit Detection
├── Damage Calculation
├── Damage Result
├── Health
├── Death                    ← chuyển sang DeathService (09)
├── Cooldown
├── Combat State
└── Weapon Technique (07 §3–4)
```
Combat Core phải được thiết kế để có thể dùng cho:

```text
PvE
 +
PvP
```
nhưng PvP-specific networking không được nhúng trực tiếp vào toàn bộ Combat Core.

Phân biệt death:

PvE: lethal damage ngoài Safe Zone → `IDeathService.RecordDeathAsync(DeathRecord)` → permadeath (09 §1.3).

PvP: HP không bao giờ chạm 0 trong mode Half-Loss — trận kết thúc khi ai còn < 50% HP (09 §3).

8. DAMAGE PIPELINE
Damage nên đi theo flow:

```text
Attack
   ↓
Hit Detection
   ↓
Attack Data
   ↓
Safe Zone guard for a protected victim → reject before IDamageable; HP unchanged
   ↓ if not protected
Damage Calculation
   ↓
Defense / Resistance
   ↓
Critical / Modifier
   ↓
Damage Result
   ↓
Apply Damage
   ↓
Health Changed
   ↓
Death Check
   ↓
[PvE] IDeathService.RecordDeathAsync(record) ← lethal damage ngoài Safe Zone
[PvE] EnemyDefeated event                   ← nếu là Enemy
[PvP] HalfLossWinCondition                  ← nếu là Fighter
```
Công thức damage khởi điểm: 07 §4.

Mỗi bước nên có trách nhiệm rõ ràng.

Không để một method hàng trăm dòng xử lý toàn bộ combat.

9. GAMEPLAY RULES VS IMPLEMENTATION
Gameplay rules phải được tách khỏi implementation khi có thể.

Ví dụ:

```text
Rule:

Basic Attack
Base multiplier = 1.0
Critical Chance = 10%
Critical Damage = 1.5x
```
Không nên rải các con số này trong:

```text
PlayerCombat.cs
Enemy.cs
Boss.cs
PvPCombat.cs
```
Các giá trị balance nên nằm trong:

configuration

data

ScriptableObject

database/static data

tùy kiến trúc dữ liệu cuối cùng.

10. WEAPON TECHNIQUE SYSTEM
Weapon Technique system gồm:

```text
WeaponTechniqueDefinition
TechniqueController
TechniqueExecutor
Cooldown
HitProfile
```
WeaponTechniqueDefinition chứa static data của Technique.

TechniqueController quyết định player có thể sử dụng Technique hay không — dựa trên Weapon Mastery của Discipline đang trang bị, không phải Character Level (07 §3).

TechniqueExecutor thực hiện Technique qua wind-up, active window và recovery chung.

HitProfile/Effect có thể xử lý:

damage

heal

buff

debuff

knockback

stun

slow

Các effect phải là module tái dùng hoặc data profile; không viết script riêng cho từng Technique.

Không tạo một script khổng lồ chứa tất cả skill.

Lưu ý Mốc 1: chỉ triển khai Blade Foundation và Blade Traverse. Control/Signature và các Discipline khác là Mốc 2 (ADR-012). Chi tiết data ở 07 §3–4 và 12 §6.

11. ENEMY ARCHITECTURE
Enemy được chia thành:

```text
Enemy
├── EnemyController
├── EnemyStats
├── EnemyAI
├── EnemyCombat
├── EnemyMovement
├── EnemyDrop
└── EnemyState
```
AI enemy nên sử dụng state-based architecture khi cần.

Ví dụ:

```text
IDLE
 ↓
DETECT
 ↓
CHASE
 ↓
ATTACK
 ↓
HURT
 ↓
DEAD
```
Các enemy có behavior tương tự phải tái sử dụng logic.

Time-aware: EnemyDefinition có `allowedTimePhases` và optional `phaseStatModifiers` (08 §4.2).

Damage to player: Safe Zone guard từ chối DamageRequest trước `IDamageable`; lethal damage resolve ngoài vùng này mới gọi `RecordDeathAsync(DeathRecord)` với reason = Enemy (09 §1.3).

12. ENEMY AI
Enemy AI không được phụ thuộc trực tiếp vào Floor cụ thể.

Không:

```text
if floor == 5
    do something
```
trừ khi đó thực sự là gameplay mechanic của Floor.

Behavior nên được cấu hình bằng data khi có thể.

Ví dụ:

```text
MeleeEnemy
RangedEnemy
TankEnemy
FastEnemy
BossEnemy
```
Có thể dùng chung AI architecture với các configuration khác nhau.

Time-aware AI: một số enemy có behavior khác ban đêm (hung hơn, tầm phát hiện xa hơn). Cấu hình qua data, không hard-code.

Safe Zone-aware AI: enemy KHÔNG spawn trong Safe Zone. Nếu player chạy vào Safe Zone, enemy dừng chase.

13. BOSS ARCHITECTURE
Boss sử dụng core Enemy + Combat nhưng có thêm hệ thống boss-specific.

Ví dụ:

```text
Boss
├── BossController
├── BossPhase
├── BossMechanic
├── BossAttack
└── BossReward
```
Boss có thể có:

nhiều phase

special attack

mechanic

enrage

main drop và rare drop (theo BossDefinition).

night variant là BossDefinition data (08 §4.5).

Boss-specific logic không được copy toàn bộ Enemy system.

Damage to player: Safe Zone guard từ chối DamageRequest trước `IDamageable`; lethal damage resolve ngoài vùng này mới gọi `RecordDeathAsync(DeathRecord)` với reason = Boss.

14. ITEM / INVENTORY / EQUIPMENT
Gameplay code phải tách:

```text
Item Definition
       ↓
Inventory Ownership
       ↓
Equipment
```
Inventory chịu trách nhiệm ownership.

Equipment chịu trách nhiệm equipped state.

Item Definition chịu trách nhiệm item information.

Không để Inventory tự xử lý toàn bộ equipment/combat logic.

Trang bị là ItemInstance; consumable/material là stack (03 §5). Item tier (Common → Epic) theo Floor: 07 §7.

Khi chết: toàn bộ Inventory không còn truy cập được (vì account bị khóa; dữ liệu vẫn giữ trên server — 03 §5). Không đáng lo vì hầu hết item farm lại được. First-clear reward là bonus, không phải unique (ADR-016).

15. QUEST ARCHITECTURE
Quest system:

```text
QuestDefinition
       ↓
QuestManager
       ↓
QuestObjective
       ↓
QuestProgress
       ↓
QuestReward
```
Objective có thể là:

Kill

Collect

Reach

Talk

Clear Dungeon

Defeat Boss.

Quest system không được hard-code toàn bộ quest vào từng Floor.

Time-aware: QuestDefinition có `requiredTimePhases` mask optional (08 §4.4). Quest chính không bị khóa bởi một phase; time rule phải có đường hoàn thành thay thế.

Death-aware: Quest progress thuộc Player Data. Khi account bị khóa (chết), quest progress mất cùng account. Đây là hệ quả tự nhiên của permadeath.

16. NPC ARCHITECTURE
NPC chịu trách nhiệm:

interaction

dialogue trigger

quest interaction

shop interaction nếu có.

NPC không nên chứa toàn bộ Quest System.

Ví dụ:

```text
Player
 ↓
Interact
 ↓
NPC
 ↓
Dialogue / Quest / Shop
```
Time-aware: NPCDefinition có `allowedTimePhases` (08 §4.3).

Safe Zone: NPC thường đặt trong Safe Zone. Không bị enemy tấn công.

17. DUNGEON ARCHITECTURE
Dungeon là một gameplay container có thể tái sử dụng:

```text
Dungeon
├── Entry
├── Spawn
├── Waves
├── Objectives
├── Boss
└── Reward
```
Dungeon có thể được sử dụng ở nhiều Floor bằng configuration khác nhau.

Không tạo một Dungeon System riêng cho mỗi Floor.

Mốc 1: Dungeon tối giản cho Floor 1. Dungeon đầy đủ và tái sử dụng nhiều Floor ở Mốc 2.

Trap: Dungeon có thể có trap. Safe Zone guard chặn damage trước `IDamageable`; lethal damage resolve ngoài Safe Zone → `RecordDeathAsync(DeathRecord)` với reason = Trap (09 §1.3).

Safe Zone: Dungeon thường KHÔNG có Safe Zone bên trong (trừ khu entry). Người chơi phải cẩn thận khi vào dungeon.

18. FLOOR ARCHITECTURE
Floor là một content container.

Ví dụ:

```text
Floor01
├── Map
├── Spawn Config
├── Enemy Config
├── Quest Config
├── Dungeon Config
├── Boss Config
├── Reward Config
└── SafeZone Config        ← MỚI
```
Core gameplay nằm ngoài Floor.

Floor không sở hữu:

riêng một Combat System

riêng một Inventory System

riêng một Player System

riêng một Quest Engine.

riêng một Time System.

riêng một Death System.

19. FLOOR LOADING
Flow dự kiến:

```text
Player selects Floor
        ↓
Check personal progression
        ↓
Check world state
        ↓
Check account state (isDead?)    ← MỚI
        ↓
Load Floor Definition
        ↓
Load Map/Scene
        ↓
Load Floor Configuration
        ↓
Đọc Time hiện tại (ITimeService)
        ↓
Spawn Safe Zone triggers
        ↓
Spawn enemies/NPC/Dungeon theo Time
        ↓
Start gameplay
```
Floor loading phải được thiết kế độc lập với combat.

Nếu account isDead = true: không load Floor. Hiện Death Screen.

Khung chuyển tầng đã cài (Phase 9, P9-1/P9-1b) — dùng chung cho mọi tầng, không rẽ nhánh theo số tầng (`FloorBranchingScanTests` chặn):

```text
FloorLoader.LoadFloorAsync(id)
  → kiểm account chết → cổng luật chuyển tầng (lần 1, từ chối sớm)
  → FloorDefinition → world state / personal progression
  → cổng luật chuyển tầng (lần 2, NGAY TRƯỚC khi unload/nạp/kích hoạt)
  → nạp scene additive (tầng nằm sẵn trong scene host thì không nạp)
  → FloorActivation.ActivateFloor(def):
      tắt MỌI FloorSceneBinder khác → bật binder đích → đặt người chơi tại EntryPoint
```

- `FloorSceneBinder` (PvE/Floor): nằm trên object LUÔN BẬT của mỗi tầng; giữ `floorId`, `contentRoot` (bật/tắt nội dung tầng), `entryPoint`, `FloorSpawnRegistrar`. Tầng nằm sẵn trong scene host (Tầng 1, scene chính không gỡ được) cũng phải gom vào một `contentRoot` như mọi tầng.
- `IFloorActivationListener` (Core/Floor): `EnemySpawner` (hồi sinh quái) và `BossController` (reset trận để đánh lại) lắng nghe; thêm loại nội dung cần reset khi vào tầng chỉ cần implement interface này.
- Cổng luật chuyển tầng (`FloorTravelRules`): chỉ chuyển tầng khi người chơi đang đứng trong Safe Zone (kiểm theo VỊ TRÍ, `SafeZoneService.IsSafeZoneAt`) VÀ còn sống; ngược lại `FloorLoadStatus.NotAllowed` kèm câu thông báo cho người chơi, không unload, không đổi gì. Chọn lại đúng tầng đang đứng thì không dịch chuyển, không reset. Quy tắc này để Safe Zone không thành nút thoát hiểm giữa trận (docs/09 §1.2).
- Mỗi tầng: `entryPoint` phải nằm TRONG một Safe Zone của chính tầng đó (nếu không, người chơi hạ cánh ngoài vùng bảo hộ và không chuyển tầng tiếp được); vùng spawn quái nằm NGOÀI Safe Zone (docs/09 §1.2).

20. PERSONAL PROGRESSION
Player progression gồm:

Level

EXP

Unlocked Floor

Quest Progress

Skills

Equipment

Achievements nếu có.

Personal progression không được thay đổi chỉ vì Floor data thay đổi.

Death-aware: khi chết, toàn bộ personal progression mất cùng account (vì account bị khóa). Đây là hệ quả tự nhiên của permadeath.

21. GLOBAL WORLD STATE
Ghi chú phạm vi: Từ Mốc 1, World State dùng server thật (Cloud Save Game Data qua IWorldStateService — 03 §6): boss bị hạ và tầng mở là TOÀN CỤC, ai cũng thấy, không chỉ riêng người vừa hạ boss. §22–25 dưới đây áp dụng thật từ Mốc 1, không còn là "giữ chỗ cho tương lai". Phần CHƯA làm ở Mốc 1–2 là thấy người chơi khác di chuyển trực tiếp trong Floor cùng lúc — đó là Mốc 3 (01 §3).

Game có một World State dùng cho những sự kiện ảnh hưởng tới toàn bộ thế giới.

Ví dụ:

```text
Floor 1 Boss = DEFEATED
Floor 2 Boss = DEFEATED
Floor 3 Boss = ACTIVE
```
World State khác với Personal Progression.

Time là một phần của World State — mọi người chơi thấy cùng ngày/đêm (08 §3).

Death KHÔNG ảnh hưởng World State. Người chơi A chết không làm thay đổi trạng thái boss/tầng của thế giới.

22. GLOBAL FLOOR BOSS
Boss của mỗi Floor là một trận instance solo trong Mốc 1: người chơi vào đánh boss của Floor đó một mình.

Flow:

```text
Player enters Floor Boss
        ↓
Player defeats Boss
        ↓
Client gửi ReportBossDefeated(bossId, floorId) → server xác nhận (idempotent)
        ↓
World Boss Defeated
        ↓
Floor becomes CLEARED
        ↓
Next Floor becomes AVAILABLE
```

Boss defeat là World State (03 §6). Client không tự đổi World State — chỉ server function ghi sau khi xác nhận.

Death-aware: nếu player chết trong trận boss, player đó bị khóa account. Boss vẫn có thể bị người khác hạ. World State vẫn cập nhật bình thường.

23. BOSS REWARD
Boss reward không phụ thuộc thứ tự người hạ boss:

```text
Boss defeated (World State)
        ↓
Mỗi account nhận first-clear bonus đúng một lần cho mỗi boss (RewardClaim — 03 §5)
        ↓
Sau đó rematch cho main drop (BossDefinition)
```

- First-clear reward = bonus theo từng người chơi (ADR-016): mỗi account nhận bonus một lần khi lần đầu hạ boss đó; không phụ thuộc ai hạ trước.
- Rare items không phải item duy nhất bị người đầu tiên lấy — mọi người đều có đường nhận qua rematch/roll.
- Bonus item KHÔNG phải unique thật — tương lai có thể quay lại theo mùa/sự kiện.

24. BOSS REMATCH
Người chơi đến sau khi World Boss đã bị đánh bại vẫn cần có cách hợp lý để tiếp cận các rare items.

```text
Boss defeated
      ↓
Boss Rematch / Memory Dungeon
      ↓
Player fights an instance/version
      ↓
Possible rare rewards
```

World Boss progression không bị reset. Rematch chỉ phục vụ gameplay/reward.

- Rematch giữ main drop như boss thật (ADR-016, 07 §7); phần rare ngẫu nhiên nhân 0.05, đặt trong `BossDefinition` (data, không hard-code).
- Rematch không cho EXP; chỉ đồ chính và đồ hiếm.
- Death-aware: Boss Rematch cũng có permadeath. Nếu chết trong Rematch → account bị khóa.

25. WORLD STATE VS PERSONAL STATE
Ví dụ:

```text
WORLD STATE

Floor 5 Boss = DEFEATED
Floor 6 = AVAILABLE
```
Trong khi:

```text
PLAYER A

Level = 25
Highest Floor = 6
isDead = false

PLAYER B

Level = 12
Highest Floor = 4
isDead = true (đã chết)
```
World Boss đã mở đường.

Nhưng player vẫn phải thỏa mãn personal requirements nếu game design yêu cầu.

Player B đã chết → không chơi được nữa, dù World State vẫn mở.

26. TIME / EVENT SYSTEM
Chi tiết đầy đủ ở 08_TIME_SYSTEM_DESIGN.md.

Tóm tắt:

Time System độc lập với Floor.

Chu kỳ game cấu hình được (mặc định 1 ngày game = 20 phút thực).

Pha: DAWN → MORNING → NOON → AFTERNOON → EVENING → NIGHT.

Ảnh hưởng: enemy spawn, NPC, quest, boss phase, ánh sáng.

Lưu ở Game Data (toàn cục), ghi qua Cloud Code.

Không hard-code time logic vào từng Floor.

Mốc 1–2 dùng chu kỳ game cấu hình được; đồng hồ thiết bị/lịch thực không là authority (ADR-011, 08 §3).

27. UI SEPARATION
UI không nên chứa gameplay logic quan trọng.

Ví dụ:

Không:

```text
Button.OnClick
→ trực tiếp sửa HP/database/inventory
```
Nên:

```text
UI
 ↓
Gameplay System
 ↓
Game State
 ↓
UI Update
```
UI chỉ hiển thị và gửi user intent.

UI Time: đồng hồ nhỏ hiển thị giờ game + icon pha (08 §2, §4.1).

UI Death: Death Screen (09 §1.4) — chỉ hiển thị, không xử lý logic chết.

UI Auth: Login Screen (có nút Reset Guest góc trên trái), Link Account Screen (09 §2).

28. GAME MANAGER
Không sử dụng một GameManager để quản lý mọi thứ.

Có thể có các service/manager cấp cao như:

```text
GameStateManager
Scene/FloorManager
SaveManager
AudioManager
TimeManager (qua ITimeService)
DeathManager (qua IDeathService)
AccountManager (qua IAccountService)
```
Nhưng mỗi manager phải có trách nhiệm rõ ràng.

Không tạo:

```text
UltimateGameManager.cs
chứa Player + Combat + Inventory + Quest + Floor + PvP + Save + Time + Death + Account.
```

29. EVENT-DRIVEN COMMUNICATION
Các module nên giao tiếp thông qua interface/event khi phù hợp.

Ví dụ:

```text
Enemy died
    ↓
EnemyDefeated(enemyId) event
    ↓
 ┌───────────────┬──────────────┬──────────────┐
 ↓               ↓              ↓
EXP System    Quest System   Drop System
```
Enemy không cần biết trực tiếp toàn bộ Quest/EXP/Inventory system.

Điều này giúp giảm coupling.

Time event:

```text
TimeChanged / TimePhaseChanged / WorldTimeResynced
    ↓
 ┌───────────────┬──────────────┬──────────────┐
 ↓               ↓              ↓
Light System   Spawn System   UI System
```
Death event:

```text
Lethal damage outside Safe Zone
    ↓
RecordDeathAsync(DeathRecord) → confirmed save/isDead
    ↓
PlayerDeathRecorded(deathRecord)
    ↓
UI Death Screen / account routing
```
30. DEPENDENCY RULE
Dependency nên đi theo hướng:

```text
Core
 ↓
Gameplay Systems
 ↓
World/Content
 ↓
UI
```
Không tạo dependency vòng.

Ví dụ không nên:

```text
Inventory → UI → Combat → Inventory
```
nếu có thể tránh.

31. GAMEPLAY CHANGE WORKFLOW
Khi người phát triển muốn thay đổi gameplay:

Ví dụ:

"Đổi damage formula."

AI phải làm:

Step 1 — Analyze
Xác định gameplay rule hiện tại.

Step 2 — Impact Analysis
Liệt kê module bị ảnh hưởng.

Ví dụ:

```text
Combat
DamageCalculator
CharacterStats
EnemyStats
PvP Combat Adapter
```
Step 3 — Plan
Đề xuất file/class cần sửa.

Step 4 — Implement
Chỉ sửa các module cần thiết.

Step 5 — Test
Test PvE.

Sau đó test PvP nếu Combat Core được dùng chung.

Step 6 — Regression Check
Đảm bảo các module không liên quan không bị ảnh hưởng.

32. BUG FIX WORKFLOW
Khi có bug:

```text
BUG
 ↓
Reproduce
 ↓
Identify affected system
 ↓
Find root cause
 ↓
Determine minimal fix
 ↓
Implement
 ↓
Test
 ↓
Regression test
```
Không được sửa code dựa trên phỏng đoán nếu có thể xác định nguyên nhân.

33. MINIMAL CHANGE PRINCIPLE
Khi sửa bug:

Sửa ít nhất số module cần thiết để giải quyết root cause.

Ví dụ:

Bug:

"Potion không hồi HP."

Không được tự tiện sửa:

Floor

Enemy

Quest

PvP

Boss

Time

Death

nếu không liên quan.

34. GAMEPLAY CHANGE IMPACT MATRIX
Mỗi thay đổi gameplay phải được phân loại:

LOCAL CHANGE
Chỉ ảnh hưởng một module.

Ví dụ:

```text
Enemy attack animation
```
CROSS-SYSTEM CHANGE
Ảnh hưởng nhiều gameplay modules.

Ví dụ:

```text
Critical Hit
→ Combat
→ Stats
→ Item
→ Skill
→ UI
```
GLOBAL CHANGE
Ảnh hưởng architecture.

Ví dụ:

```text
Offline → Online
Single-player → Multiplayer
Đổi hệ thống Time từ cá nhân sang toàn cục
Thêm permadeath vào PvE
```
Global change phải được phân tích trước khi code.

35. VERSIONING GAMEPLAY RULES
Khi thay đổi gameplay quan trọng, AI phải ghi lại:

```text
Before:
Damage = Attack × 1.0

After:
Damage = Attack × 1.2
```
Nếu cần, cập nhật:

```text
GAMEPLAY_RULES.md
```
Không để gameplay rules quan trọng chỉ tồn tại trong code.

36. FLOOR CONTENT VS CORE CODE
Phải phân biệt:

CORE CODE
```text
Combat
Enemy AI
Quest Engine
Inventory Engine
Skill Engine
Time System
Death System
SafeZone System
```
FLOOR CONTENT
```text
Floor01
Floor02
Floor03
...
```
Nếu thêm Floor 11 mà cần viết lại Combat System thì architecture đang có vấn đề.

37. 10 → 100 FLOOR TEST
Architecture phải được kiểm tra bằng giả lập:

Nếu thêm:

```text
Floor11
Floor12
...
Floor100
```
thì:

Core Combat không đổi.

Player System không đổi.

Inventory System không đổi.

Quest Engine không đổi.

Enemy Engine không đổi.

Boss Engine không đổi.

Time System không đổi.

Death System không đổi.

SafeZone System không đổi.

Chỉ thêm content/configuration khi có thể.

38. PvE / PvP SHARED GAMEPLAY
Shared:

```text
Character Stats
Skill Definition
Combat Rules
Damage Calculation
```
PvE-specific:

```text
Enemy AI
World Boss
Dungeon
Quest
Floor
Time (PvE dùng time; PvP không dùng)
Death / SafeZone (PvE có permadeath; PvP không)
```
PvP-specific:

```text
Match
Room
Synchronization
Matchmaking
Network State
Arena
HalfLossWinCondition
Challenge
```
PvP networking không được làm dependency bắt buộc của Offline/PvE core.

PvP KHÔNG dùng Time System — Duel là phiên ngắn, không cần ngày/đêm.

PvP KHÔNG dùng Death System — Duel chỉ Half-Loss, không có permadeath.

39. PERFORMANCE
Vì game chạy trên Android:

Ưu tiên:

object pooling cho enemy/projectile khi cần

hạn chế Instantiate/Destroy liên tục

tránh Update() không cần thiết

hạn chế allocations trong combat loop

load asset theo nhu cầu

unload scene/asset khi phù hợp.

Time System cập nhật mỗi giây (không phải mỗi frame) để tiết kiệm.

SafeZone check dùng trigger, không check mỗi frame.

Không tối ưu sớm bằng cách làm architecture khó hiểu.

Ưu tiên code rõ ràng trước, sau đó profile và tối ưu theo dữ liệu thực tế.

40. SAVE INTEGRATION
Gameplay systems không được tự ý ghi database/save file ở mọi nơi.

Nên có:

```text
Gameplay Event
      ↓
State Change
      ↓
Save System
```
Ví dụ:

```text
Boss defeated
      ↓
WorldState changed
      ↓
Save/Synchronize
```
Time:

```text
Time advanced
      ↓
WorldTime changed
      ↓
Cloud Code GetWorldTimeSnapshotAsync (boot/resync)
```
Death:

```text
Player died
      ↓
isDead = true trong Player Data
      ↓
Save NGAY (không đợi định kỳ) — vì đây là sự kiện critical
```
41. DEBUGGING CONTEXT
Khi yêu cầu AI debug:

Người phát triển nên cung cấp:

```text
1. Bug description
2. Expected behavior
3. Actual behavior
4. Relevant file(s)
5. Error/log
6. Recent changes
```
AI không nên yêu cầu toàn bộ project nếu module liên quan đã đủ.

Nếu cần thêm context:

Chỉ yêu cầu những file có khả năng liên quan.

42. CODE CHANGE REPORT
Sau mỗi thay đổi code quan trọng, AI phải báo:

```text
Changed:
- File A
- File B

Why:
- Reason

Not changed:
- File C
- File D

Impact:
- PvE: affected/not affected
- PvP: affected/not affected
- Save: affected/not affected
- UI: affected/not affected
- Time: affected/not affected
- Death: affected/not affected
```
Điều này giúp kiểm soát AI-generated code.

43. IMPLEMENTATION RULE
Khi feature lớn:

Không implement toàn bộ một lần.

Chia thành:

```text
Design
 ↓
Core implementation
 ↓
Integration
 ↓
Test
 ↓
Polish
```
Mỗi milestone phải build/test được.

44. DO NOT OVERENGINEER
Không tạo architecture phức tạp chỉ để "trông chuyên nghiệp".

Nếu một hệ thống đơn giản giải quyết được vấn đề thì ưu tiên giải pháp đơn giản.

Mọi abstraction phải có lý do.

45. CURRENT GAMEPLAY PRIORITY
Thứ tự phát triển: xem 05_DEVELOPMENT_ROADMAP.md (nguồn duy nhất).

46. FINAL PRINCIPLES
Gameplay architecture phải tuân thủ:

Principle 1
One Core System → Many Floors.

Principle 2
Data defines content; code defines behavior.

Principle 3
Separate gameplay rules from implementation.

Principle 4
Fix the smallest necessary module.

Principle 5
Analyze impact before major gameplay changes.

Principle 6
PvE and PvP share core gameplay where appropriate but remain independently maintainable.

Principle 7
World progression and personal progression are separate concepts.

Principle 8
Global Boss progression must not permanently deprive late players of important rewards.

Principle 9
Do not create a giant GameManager.

Principle 10
Every major gameplay system must be independently understandable, testable and replaceable.

Principle 11
Time System là toàn cục và độc lập với Floor. Authority là snapshot server; độ dài chu kỳ chỉ là config server. Đổi chu kỳ sang 1:1 thời gian thật (86400) là đổi config, bump cycleConfigVersion và test hai account — không cần ADR.

Principle 12
Không sao chép nội dung có bản quyền — chỉ lấy ý tưởng cơ chế.

Principle 13
Permadeath là PvE-only. PvP chỉ Half-Loss. Safe Zone bảo vệ. Guest Reset dễ tiếp cận.

Principle 14
Account bị khóa khi chết là logic game, không xóa thật trên UGS.

Principle 15
First-clear reward là bonus, không phải unique. Không permanently deprive late players.

47. TARGET ARCHITECTURE
Mục tiêu cuối cùng:

```text
                    GAME
                     │
        ┌────────────┴────────────┐
        │                         │
   CORE GAMEPLAY               DATA
        │                         │
 ┌──────┼─────────┐       ┌──────┼───────┐
 │      │         │       │      │       │
Player Combat   Enemy    Item   Quest   Floor
 │      │         │       │      │       │
 └──────┴─────────┴───────┴──────┴───────┘
                     │
              WORLD / PROGRESSION
                     │
             ┌───────┴────────┐
             │                │
        Personal State    World State
             │                │
             │                ├── Time (toàn cục)
             │                │
             └───────┬────────┘
                     │
                  SAVE
                     │
             ┌───────┴────────┐
             │                │
             PvE              PvP
             │                │
        Time System       (không dùng Time)
        Death System      (không permadeath)
        SafeZone          Half-Loss only
        Account           Challenge
             │                │
                         Network Layer
```
Mục tiêu:

Build the gameplay systems once, configure them with data, reuse them across all Floors, and keep changes isolated to the smallest affected module.

48. DEATH SYSTEM (PvE)
Chi tiết đầy đủ ở 09_DEATH_AND_DUEL_SYSTEM.md Phần 1.

48.1. Tóm tắt
Chết do enemy/boss/trap → account bị khóa → phải tạo account mới.

Safe Zone (spawn + NPC + shop) chặn damage trước khi tới `IDamageable`.

Không revive item.

Không respawn.

48.2. Flow
```text
DamageRequest
   ↓
Safe Zone guard trước IDamageable
   ├── Protected → reject damage; HP không đổi
   └── Not protected → resolve damage
        ↓ nếu lethal
        CHẾT
         ↓
IDeathService.RecordDeathAsync(DeathRecord)
        ↓
Ghi Player Data: { "isDead": true, "deathTime": ..., "deathCause": ... }
        ↓
Disable input → Hiện Death Screen
        ↓
Guest: nút "Tạo tài khoản mới"
Official: nút "Thoát"
```
48.3. Module
```text
Game.Core/Character/
├── IDeathService
├── DeathService
├── DeathReason (enum: Enemy, Boss, Trap)
└── SafeZoneService

Game.Core/Events/
└── PlayerDeathRecorded (sau khi save xác nhận)

Game.Services/
└── DeathPersistenceService

Game.PvE/Death/
├── EnemyDamageHandler
├── BossDamageHandler
└── TrapDamageHandler

Game.UI/Death/
├── DeathScreen
└── DeathCauseDisplay
```
48.4. Không được làm
Respawn sau khi chết.

Revive item.

Permadeath trong PvP.

Xóa account thật trên UGS.

49. DUEL CHALLENGE (PvP)
Chi tiết đầy đủ ở 09_DEATH_AND_DUEL_SYSTEM.md Phần 3 và 04_NETWORK_PVP_ARCHITECTURE.md.

49.1. Tóm tắt
Chỉ 1 mode ở Mốc 1: Half-Loss (ai còn < 50% HP thì thua).

Không permadeath trong PvP.

Challenge flag phân biệt phòng casual và phòng thách đấu.

Challenge History lưu vào Player Data (100 trận gần nhất).

49.2. Flow
```text
A tạo phòng thách đấu → Half-Loss mode
   ↓
Sinh mã Lobby → A gửi mã cho B
   ↓
B nhập mã → cả hai sẵn sàng
   ↓
Load Arena → Countdown → FIGHTING
   ↓
Ai còn < 50% HP → ROUND_END → RESULT
   ↓
Ghi Challenge History (Player Data)
   ↓
Cả hai về Menu
```
49.3. Lộ trình
Tính năng	Mốc 1	Mốc 2	Mốc 3
Challenge flag + Half-Loss	✅		
Ghi Challenge History	✅		
Notification in-game		✅	
Accept/Reject UI		✅	
Friend list			✅
Bảng thách đấu công khai			✅
49.4. Không được làm
Nhiều Duel Mode ở Mốc 1 (chỉ Half-Loss).

Permadeath trong PvP.

Ảnh hưởng World State từ kết quả Duel.

50. FINAL NOTE
Mọi thay đổi lớn phải:

Phân tích tác động.

Ghi ADR ở 01 §6.

Cập nhật file liên quan.

Test trước khi sang phase tiếp theo.

Mục tiêu cuối cùng:

Build once, reuse many times.
Chết là hết trong PvE, nhưng PvP chỉ để so kỹ năng.
Time và Death là toàn cục, độc lập với Floor.
Safe Zone bảo vệ, Guest Reset dễ tiếp cận.

## 51. IMPLEMENTATION CONTRACT UPDATE

`12_ORIGINAL_GAMEPLAY_BIBLE.md` và `13_TWO_AGENT_IMPLEMENTATION_PLAN.md` là phần bổ sung có hiệu lực cho các flow dưới đây. Nếu tài liệu cũ mô tả khác, contract này được ưu tiên.

### 51.1. Quest

```text
Locked → Available → Active → ReadyToTurnIn → Completed
```

- Objective đủ chỉ chuyển `Active` sang `ReadyToTurnIn`; reward chỉ được cấp sau player gửi turn-in intent tới NPC hợp lệ.
- `QuestDefinition` phải biểu diễn prerequisite, floor requirement, time rule, repeat rule và policy consume item.
- `Kill`, `Collect`, `Talk`, `Reach` đều so target ID. Event không đúng target bị bỏ qua.
- Nhận quest, progress và turn-in đều có đường input cảm ứng; keyboard là hỗ trợ Editor.
- Quest save dùng queue/version policy. Nhiều request save không được ghi ngược state cũ lên state mới.

### 51.2. Combat và Technique

- Combat state bắt buộc: WindUp → Active → Recovery. Chỉ Active tạo hitbox.
- Weapon Technique là static definition + generic executor; không code branch theo tên Technique hoặc WeaponType cụ thể.
- Level, mastery, equipment là ba nguồn power tách biệt. Mastery mở Technique, không tự cộng Attack.

### 51.3. World state

- Time, boss clear và floor availability là World State toàn cục.
- Death là Player Data cá nhân, không phải World State.
- PvE chỉ request server function; server function là đường ghi duy nhất cho World State.

### 51.4. Mobile gate

Mọi player intent trong Mốc 1 phải được chạy trên landscape touch UI. Một feature chỉ có phím `E`, `Q` hoặc keyboard shortcut không được xem là hoàn thành.

