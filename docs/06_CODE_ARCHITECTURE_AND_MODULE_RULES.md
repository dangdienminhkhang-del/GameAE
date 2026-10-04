    # CODE ARCHITECTURE & MODULE RULES
## Game2D — quy tắc tổ chức code (Unity)

> Quy tắc về CẤU TRÚC CODE. Quy tắc hành vi của AI nằm ở `AGENTS.md`.
> Nếu xung đột với 01–05, 07, 08, 09: nêu rõ xung đột trước khi làm.

---

## 1. NGUYÊN TẮC GỐC

Tách rõ: Core / Services / Content / Game Mode (PvE, PvP) / UI. Không trộn thành một đống script.

Ba luật vàng:
1. Không giải quyết vấn đề cục bộ bằng viết lại toàn cục.
2. Không tạo hệ thống mới khi chưa kiểm tra hệ thống sẵn có không đáp ứng được.
3. Core không phụ thuộc tầng, item, quái hay mode PvP cụ thể.

## 2. CẤU TRÚC THƯ MỤC

```text
Assets/_Game/
├── Scripts/
│   ├── Core/      (Game.Core)
│   │   ├── Character/    (Stats, Movement, IDeathService, DeathReason, SafeZoneService)
│   │   ├── Combat/       (IDamageable, DamageCalculator, Attack, HitDetection)
│   │   ├── Stats/
│   │   ├── Skills/       (DodgeDefinition)
│   │   ├── Items/
│   │   ├── Inventory/
│   │   ├── Equipment/
│   │   ├── Quest/
│   │   ├── AI/           (StateMachine)
│   │   ├── Time/         (ITimeService, TimeOfDay, TimeConfig)
│   │   ├── Account/      (AccountType, IAccountService, IAccountResetService, IAccountLinkService)
│   │   ├── Network/      (ILobbyService, IRelayService, INetworkTransport, IServerFunctionService)
│   │   ├── World/        (IWorldStateService, ISaveService)
│   │   ├── Floor/        (FloorDefinition, FloorDatabase)
│   │   ├── Challenge/    (ChallengeHistory)
│   │   ├── Events/       (domain event contracts in 12 §11)
│   │   └── Utilities/
│   │
│   ├── Services/  (Game.Services)  Cài đặt thật của các interface trên:
│   │   ├── Account/      (AccountService, AccountResetService, AccountLinkService)
│   │   ├── Save/         (PlayerSaveService)
│   │   ├── World/        (WorldStateService)
│   │   ├── Time/         (TimeService)
│   │   ├── Death/        (DeathPersistenceService)
│   │   ├── Network/      (UGSLobbyService, UGSRelayService, UGSNetworkTransport,
│   │   │                  UGSServerFunctionService)
│   │   ├── Challenge/    (ChallengeHistoryService)
│   │   ├── Config/       (BackendConfig, GameConfig)
│   │   └── (Là nơi DUY NHẤT được `using Unity.Services.*` cho Account/Save/WorldState/Time/Death/Network)
│   │
│   ├── PvE/       (Game.PvE)
│   │   ├── Enemy/
│   │   ├── Player/
│   │   ├── Items/
│   │   ├── Quest/
│   │   ├── NPC/
│   │   ├── Skills/
│   │   ├── Boss/
│   │   ├── Dungeon/
│   │   ├── Floor/
│   │   ├── World/
│   │   ├── Mechanics/
│   │   ├── Time/         (DayNightLightController, TimeBasedSpawner, TimeBasedNPCController)
│   │   ├── Death/        (EnemyDamageHandler, BossDamageHandler, TrapDamageHandler)
│   │   ├── SafeZone/     (SafeZoneTrigger)
│   │   └── Dodge/        (DodgeController)
│   │
│   ├── PvP/       (Game.PvP)
│   │   ├── Core/         (DuelMode, HalfLossMode, HalfLossWinCondition, PvPMatch, Fighter)
│   │   ├── Lobby/        (gọi ILobbyService — KHÔNG gọi UGS trực tiếp)
│   │   ├── Relay/        (gọi IRelayService — KHÔNG gọi UGS trực tiếp)
│   │   ├── Network/      (gọi INetworkTransport — KHÔNG gọi NGO trực tiếp)
│   │   ├── Arena/
│   │   └── Challenge/    (ChallengeFlag, ChallengeRoomCreator, ChallengeHistoryRecorder)
│   │
│   └── UI/        (Game.UI)
│       ├── Common/
│       ├── HUD/
│       ├── Inventory/
│       ├── Quest/
│       ├── PvE/
│       ├── Floor/
│       ├── PvP/          (ChallengeRoomCreator UI, ChallengeHistoryScreen)
│       ├── Auth/         (LoginScreen có nút Reset Guest, LinkAccountScreen)
│       ├── Time/         (đồng hồ nhỏ)
│       ├── Death/        (DeathScreen, DeathCauseDisplay)
│       └── Skills/       (nút Technique/Dodge)
│
├── cloudcode/    (repo-root script Cloud Code; **KHÔNG** nằm dưới Assets/)
│   ├── GetWorldTimeSnapshot.js    (Wave 2B — hằng rate/startHour/cycleVersion)
│   └── ReportBossDefeated.js      (Wave 3)
├── Data/       Definition dùng chung từ 2 tầng trở lên + Config + Databases
├── Content/
│   └── Floors/Floor01 … Floor10   Nội dung riêng từng tầng (bao gồm SafeZone config)
├── Art/  Audio/  Prefabs/
├── Scenes/     Boot, MainMenu, PvE, PvP
└── Tests/      EditMode (Game.Tests.EditMode), PlayMode khi cần
```

Quy tắc đặt file:

Chỉ một tầng dùng → Content/Floors/FloorNN/(FloorDefinition, map/scene, data quái/quest/boss/reward, sprite/tilemap của tầng đó). Từ hai tầng trở lên → chuyển sang Data/. Class `FloorDefinition`/`FloorDatabase` nằm ở `Core/Floor`; asset thật nằm ở `Data/Databases` (database) và `Content/Floors/FloorNN` (definition + content tầng).

FloorDatabase (trong Data/Databases) liệt kê tất cả FloorDefinition; thêm tầng = thêm asset + đăng ký vào database.

## 3. ASSEMBLY DEFINITION & HƯỚNG PHỤ THUỘC

```text
Game.PvE ─┐
          ├─► Game.Services ─► Game.Core
Game.PvP ─┘
Game.UI  ─► Game.Core, Game.PvE, Game.PvP, Game.Services
```

Game.Core không tham chiếu ai, không có dependency tới UGS SDK — thuần logic, test được không cần mạng. Bao gồm:

ITimeService, TimeOfDay, TimeConfig

IDeathService, DeathReason, SafeZoneService

AccountType, IAccountService, IAccountResetService, IAccountLinkService

ILobbyService, IRelayService, INetworkTransport, IServerFunctionService

ISaveService, IWorldStateService

ChallengeHistory

Game.Services cài đặt interface của Core bằng gói UGS (Authentication, Cloud Save, Lobby, Relay, NGO). Là nơi DUY NHẤT được using Unity.Services.* cho Account/Save/WorldState/Time/Death/Network.

Game.PvP KHÔNG using Unity.Services.Lobby, Unity.Services.Relay, Unity.Netcode trực tiếp. Chỉ gọi interface ILobbyService, IRelayService, INetworkTransport từ Core.

Game.PvE KHÔNG using Unity.Services.* trực tiếp. Chỉ gọi interface từ Core.

PvE và PvP KHÔNG tham chiếu nhau. Vòng phụ thuộc = dừng, phân tích kiến trúc.

Interface đặt ở Core; cài đặt đặt ở Game.Services.

Event dùng chung định nghĩa trong Core/Events.

Tạo asmdef: Core, PvE, UI, Services từ Phase 0–1; PvP từ Phase 10 (Duel online).

Vòng phụ thuộc = dừng và phân tích kiến trúc.

3.1. Kiểm tra Abstraction (khuyến nghị)
Trong CI hoặc pre-commit, kiểm tra:

Game.Core KHÔNG có using Unity.Services.*

Game.Core KHÔNG có using Unity.Netcode.*

Game.PvE KHÔNG có using Unity.Services.*

Game.PvP KHÔNG có using Unity.Services.Lobby.*, Unity.Services.Relay.*, Unity.Netcode.* (trừ Game.PvP/Network/ wrapper nếu có)

Nếu vi phạm → build fail. Điều này đảm bảo abstraction không bị phá vỡ theo thời gian.

3.2. BackendConfig
```text
Game.Services/Config/BackendConfig.cs (ScriptableObject)
├── backendType: "UGS" | "Spring" | "Hybrid"
├── lobbyProvider: "UGS" | "Custom"
├── relayProvider: "UGS" | "Custom"
├── saveProvider: "UGS" | "Spring" | "Custom"
├── worldStateProvider: "UGS" | "Spring" | "Custom"
└── timeProvider: "UGS" | "Spring" | "Custom"
```
Dùng khi: dependency injection quyết định cài đặt nào được dùng. Ví dụ:

```text
BackendConfig.backendType == "UGS"
   → AccountService, UGSLobbyService, UGSRelayService, ...

BackendConfig.backendType == "Spring"
   → SpringAccountService, SpringLobbyService, SpringRelayService, ...

BackendConfig.backendType == "Hybrid"
   → SpringAccountService (cho account/save)
   → UGSLobbyService, UGSRelayService (cho realtime)
```

### 3.3. ServiceProvider

**Mục đích:** Tập trung TẤT CẢ việc "tạo service" vào MỘT file duy nhất. Gameplay gọi `ServiceProvider.CreateXxxService()`, không gọi `new XxxService()` trực tiếp.

**Vị trí:** `Game.Services/ServiceProvider.cs`

**Ví dụ:**

```csharp
// Game.Services/ServiceProvider.cs
using Game.Core;

namespace Game.Services
{
    public static class ServiceProvider
    {
        public static IAccountService CreateAccountService()
        {
            return BackendConfig.backendType switch
            {
                "UGS"    => new AccountService(),
                "Spring" => new SpringAccountService(),
                "Hybrid" => new SpringAccountService(),
                _        => new AccountService()
            };
        }

        public static ISaveService CreateSaveService()
        {
            return BackendConfig.backendType switch
            {
                "UGS"    => new UGSPlayerSaveService(),
                "Spring" => new SpringPlayerSaveService(),
                "Hybrid" => new SpringPlayerSaveService(),
                _        => new UGSPlayerSaveService()
            };
        }

        // ...
    }
}
```

Cách dùng trong gameplay:

```csharp
// ĐÚNG:
_accountService = ServiceProvider.CreateAccountService();

// SAI:
_accountService = new AccountService();   // ← Gameplay không được biết class cụ thể
```

Khi đổi backend:

Chỉ sửa ServiceProvider.cs.

Gameplay KHÔNG phải sửa.

Mối quan hệ với BackendConfig:

```text
ServiceProvider  →  đọc  →  BackendConfig  →  quyết định class nào
     ↓
Gameplay
```
BackendConfig = ScriptableObject chứa cấu hình ("UGS" / "Spring" / "Hybrid").

ServiceProvider = class static đọc BackendConfig và trả về cài đặt đúng.

Quy tắc:

Mọi service (Account, Save, WorldState, Time, Death, Lobby, Relay, Network) đều phải qua ServiceProvider.

Gameplay KHÔNG new XxxService() trực tiếp.

Chỉ ServiceProvider được new XxxService().

4. DATA-DRIVEN
Logic: CombatSystem, InventoryService, QuestService, TimeService, DeathService… Data: DragonSword, Goblin, Floor07Boss, TimeConfig, SafeZoneConfig… (là asset ScriptableObject, không phải class).

Không tạo class mới chỉ vì có content mới: dùng EnemyController + EnemyDefinition, không Goblin01Controller.

Definition (ScriptableObject) là dữ liệu chỉ đọc lúc chạy; trạng thái thay đổi nằm ở runtime object/save.

Không hard-code số tầng vào Core; số tầng tối đa đọc từ GameConfig.

Tầng cần cơ chế riêng: dùng status effect/mechanic module + cấu hình, đặt trong PvE/Mechanics/ và cô lập.

5. QUY TẮC THEO MODULE
Player: nhiều component nhỏ (Controller, Movement, Combat, Stats, Skill, Inventory, Equipment, Interaction, DeathHandler); không PlayerManager khổng lồ.

PvE Player Controller và PvP Fighter Controller là hai thứ riêng, chung Stats/Combat/Skill ở Core. Không UniversalPlayerController đầy if PvE / if PvP.

Combat Core: attack, hit detection, damage calculation (07 §4), health, status effect, death, Weapon Technique executor/contracts (07 §3–4). Không nhúng network hay luật riêng của PvP vào Combat Core.

UI: lớp hiển thị. Gameplay phát event, UI lắng nghe. UI không sở hữu logic damage, quyền sở hữu item, thưởng, kết quả PvP, save, death, reset account.

Save: Gameplay State → ISaveService → Cloud Save. Không đọc/ghi rải rác. Khi chết: ghi NGAY lập tức.

Time: ITimeService (Core) → cài đặt TimeService (Services) → `IServerFunctionService.GetWorldTimeSnapshotAsync()` để bootstrap/read-resync snapshot server. Không gửi game hour từ client. PvE subscribe `TimeChanged`/`TimePhaseChanged`/`WorldTimeResynced` theo đúng ngữ nghĩa ở 12 §11. PvP KHÔNG dùng Time.

Death: IDeathService (Core) → cài đặt DeathPersistenceService (Services). Safe Zone guard chạy trước IDamageable; PvE gọi `RecordDeathAsync(DeathRecord)` sau lethal damage ngoài Safe Zone. `PlayerDeathRecorded` chỉ phát sau save xác nhận. PvP KHÔNG dùng Death.

SafeZone: SafeZoneService (Core); trigger đặt trong PvE scene. Runtime state, KHÔNG lưu save.

Account: IAccountService (đăng nhập), IAccountResetService (reset Guest), IAccountLinkService (link Google/Facebook/Apple). Cài đặt ở Game.Services.

Challenge: ChallengeFlag (data class), ChallengeRoomCreator (PvP), ChallengeHistoryRecorder (Services). Ghi vào Player Data.

Network (PvP từ Phase 10): cô lập trong Game.PvP: Gameplay State → Sync Layer (NGO) → Transport (Relay).
**Backend Abstraction:** Mọi giao tiếp backend phải qua interface ở Core. Cài đặt cụ thể ở `Game.Services`. `BackendConfig` quyết định backend nào được dùng. PvP không gọi UGS trực tiếp — gọi `ILobbyService`, `IRelayService`, `INetworkTransport`. PvE không gọi UGS trực tiếp — gọi `ISaveService`, `IWorldStateService`, `ITimeService`, `IDeathService`, `IServerFunctionService`.

PvP: không phụ thuộc Floor cụ thể; mode/rule/arena/network tách lớp (04).

6. QUY ƯỚC CODE
Single Responsibility; ưu tiên composition hơn inheritance; kế thừa chỉ khi thật sự is-a (Enemy → Melee/Ranged/Boss; DuelMode → HalfLoss).

Interface khi nhiều hệ thống cần cùng hành vi; không tạo interface cho mọi class.

Event để giảm phụ thuộc, nhưng đừng để thành mạng phụ thuộc vô hình.

Singleton/Manager: trước khi tạo, trả lời — nó sở hữu trách nhiệm/state nào? ai gọi? phụ thuộc gì? vòng đời và thứ tự khởi tạo ra sao? test được không? trách nhiệm này có thuộc hệ thống sẵn có không?

File lớn là dấu hiệu cảnh báo, không có giới hạn dòng cứng: "Đủ nhỏ để hiểu, đủ lớn để có nghĩa."

Balance/magic number: đặt tên hằng hoặc đưa vào config (07 là nguồn số liệu khởi điểm); không viết trần trụi.

Đặt tên rõ nghĩa: InventoryService, FloorDefinition, PvPMatch, TimeService, DeathService, SafeZoneService, AccountResetService, ChallengeHistoryService. Cấm Manager2, NewScript, Temp, FinalManager, Helper mơ hồ. Tên file trùng tên class chính.

ID ổn định, tách khỏi tên hiển thị (03 §2).

Lỗi phải rõ ràng: không nuốt im lặng lỗi thiếu ID, equipment sai, tầng không tồn tại, skill sai, account đã chết. Log dạng [Death] Account locked: cause=Enemy, source=goblin_01.

7. KIỂM THỬ
Unity Test Framework, EditMode cho logic thuần:

damage (07 §4), stats/EXP (07 §1), inventory add/remove, equipment stats, quest, save/load, PvP build snapshot.
- **Backend Abstraction:** mock `ILobbyService`, `IRelayService`, `INetworkTransport`, `IServerFunctionService` — PvP phải test được không cần UGS thật.
- **BackendConfig switch:** test đổi `backendType` → cài đặt đúng được khởi tạo.

Time phase calculation.

DeathService: khi HP = 0, không Safe Zone → isDead = true.

SafeZoneService: trong Safe Zone → HP không giảm.

HalfLossWinCondition: HP < 50% → LOSE.

ChallengeHistory: thêm match → lưu đúng, giới hạn 100.

AccountResetService: reset Guest → Player ID mới.

Kiểm thử tích hợp cho: Player + Inventory, Quest + Enemy, Boss + Floor Progress, Save + Load, Time + Spawn, Death + Account, Death + Save.

Cuối mỗi phase: build, chạy trên máy thật, chạy test liên quan, kiểm tra hồi quy.

8. UNITY EDITOR, GIT VÀ ASSET
AI sửa được file text: C#, .asmdef, JSON, tài liệu. Việc trong Editor (tạo asset ScriptableObject, gán Inspector, dựng prefab/scene/tilemap/animator, Player Settings, build APK, cài URP 2D Light2D, setup SafeZone Collider2D) do người làm; AI liệt kê từng bước.

Không sửa tay .unity/.prefab/.asset/.meta trừ khi được yêu cầu.

Git: .gitignore chuẩn Unity, COMMIT file .meta; commit nhỏ, đúng phạm vi; không commit APK/Library.

AI không xóa hoặc ghi đè hàng loạt, không reset thay đổi của người dùng, không thao tác git phá hủy khi chưa hỏi.

Scene có trách nhiệm rõ (Boot, MainMenu, PvE, PvP…). Đối tượng tái sử dụng làm thành prefab.

Art/Audio nhóm theo mục đích.

9. BẢO TRÌ
Hiệu năng: đo → tìm điểm nghẽn → tối ưu → đo lại.

Code tạm đánh dấu TODO / TEMPORARY / PROTOTYPE / DEBUG ONLY.

Refactor khi: trùng lặp đáng kể, hướng phụ thuộc sai, module nhiều trách nhiệm không liên quan, cùng lỗi lặp lại do kiến trúc. Không refactor phần không liên quan.

Thay hệ thống cũ: Cũ → kế hoạch tương thích/migrate → Mới → Validate → Xóa cũ.

Architecture drift: ghi lại và quyết định có refactor không.

Quyết định lớn ghi ADR ở 01 §6. Docs và code lệch nhau: báo lệch, đừng tự cho là bên nào đúng.

Công cụ debug tách khỏi gameplay, bật bằng cờ, tắt trước khi phát hành.

Không chắc code thuộc module nào: xác định trách nhiệm và hướng phụ thuộc trước.
