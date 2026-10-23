# MASTER PROJECT CONTEXT
## Game2D Android Online Action RPG

> Luật thiết kế cấp cao. Đọc `AGENTS.md` trước; đọc `12_ORIGINAL_GAMEPLAY_BIBLE.md` trước mọi thay đổi gameplay; đọc `13_TWO_AGENT_IMPLEMENTATION_PLAN.md` khi giao việc cho AI.
> Cập nhật: 20/10/2026 (ADR-028 đến ADR-031 ở §6; ADR-031 = chế độ AI tự chủ).

---

## 1. Game là gì

- Action RPG 2D landscape cho Android, Unity và C#.
- Online từ Mốc 1: đăng nhập ẩn danh là bắt buộc. PvE là trải nghiệm một người. Duel (phiên online realtime hai người) thuộc Mốc 2 (ADR-028).
- Người chơi chinh phục các Floor bằng combat đọc được, mastery vũ khí, loot, quest và boss. Boss clear làm thay đổi tiến trình Floor chung của thế giới.
- Điểm nhấn riêng: ngày/đêm toàn cục thay đổi content; permadeath trong PvE; Duel Half-Loss (Mốc 2) không có permadeath.
- Không phải thế giới mở nhiều người chơi đi lại cùng nhau ở Mốc 1. Khả năng đó để Mốc 3.

## 2. Gameplay loop

```text
Đăng nhập → chọn/vào Floor → khám phá → chiến đấu → EXP/loot/mastery
→ nhận/làm/trả quest → chuẩn bị boss → World State clear → Floor mới available
```

Quest cá nhân dùng state machine:

```text
Locked → Available → Active → ReadyToTurnIn → Completed
```

## 3. Mốc sản phẩm

| Mốc | Nội dung | Điều không mở rộng |
|---|---|---|
| Mốc 1 Nộp môn | Floor 1 hoàn chỉnh, Floor 2 bằng data, online login, Cloud Save, time, death; kết thúc ở Phase 11 (kiểm tổng) | Không MMORPG, không Duel, không ranked. |
| Mốc 2 Mở rộng | Duel 1v1 bằng mã phòng (Phase 10, mode đầu tiên là Half-Loss), full Technique cho các Discipline, dungeon, Floor 3–10, bot luyện tập, thông báo challenge | Không dedicated world server; Duel chỉ một mode (Half-Loss). |
| Mốc 3 Định hướng | Nhiều người trong Floor, matchmaking/ranked, Floor 11–100 | Chọn backend sau. |

## 4. Ràng buộc cứng

1. Một Core dùng cho nhiều Floor; Floor là data/content. Không `if floor == N`, không hard-code số Floor tối đa.
2. Hướng phụ thuộc: Content → PvE/PvP → Services → Core. Core không biết content cụ thể. PvE và PvP không tham chiếu nhau.
3. UI chỉ hiển thị state và gửi player intent. UI không tính damage, reward, save hoặc death.
4. Backend đi qua interface Core. PvE dùng `ISaveService`, `IWorldStateService`, `ITimeService`, `IDeathService`, account interfaces và server-function interface. PvP dùng `ILobbyService`, `IRelayService`, `INetworkTransport`.
5. Client chỉ đọc World State; mọi ghi World State phải qua server function.
6. Mobile-first: mỗi intent của người chơi có đường đi bằng cảm ứng; keyboard chỉ hỗ trợ Editor.
7. Không GameManager khổng lồ. Không tự tạo service nếu responsibility đã thuộc service hiện có.
8. Balance nằm trong data/config; code không chứa số combat theo content.
9. Permadeath PvE: death do Enemy/Boss/Trap khóa account logic. Safe Zone chặn damage. Duel không có death.
10. Duel (Mốc 2) mode đầu tiên chỉ có Half-Loss: HP dưới 50 phần trăm là thua.

## 5. Bản sắc nguyên bản

Game chỉ dùng những ý tưởng thiết kế phổ biến của thể loại: tháp nhiều tầng, luyện mastery vũ khí, action combat, ngày/đêm, duel và rủi ro thất bại.

- Tất cả tên, thế giới, cốt truyện, nhân vật, quái, boss, vật phẩm, Technique, UI, âm thanh, hình ảnh, animation và VFX do dự án tự tạo.
- Không dùng nguồn giải trí có sẵn làm đặc tả một đổi một cho gameplay, progression, skill list, số liệu hoặc nhịp chiến đấu.
- Khi cần tạo feature, dùng `12_ORIGINAL_GAMEPLAY_BIBLE.md` làm nguồn gameplay duy nhất.
- Bất kỳ content nào không truy được về definition/brief nội bộ đều không được thêm vào build.

## 6. Quyết định đã chốt

| ID | Quyết định |
|---|---|
| ADR-001 | Mốc 1 online bắt buộc; PvE solo; Duel realtime hai người (Duel thuộc Mốc 2 theo ADR-028). |
| ADR-002 | PvE top-down; Duel side-view. |
| ADR-003 | Static data là ScriptableObject; Player Data là Cloud Save qua `ISaveService`. |
| ADR-004 | World State boss/Floor/time là Game Data; chỉ server function ghi. |
| ADR-005 | Equipment dùng `ItemInstance`; consumable/material dùng stack. |
| ADR-006 | Backend Mốc 1 là UGS abstractions; không dùng Unity Multiplay. |
| ADR-007 | Duel host-client qua Relay, lobby code ngắn, chưa matchmaking (Mốc 2 theo ADR-028). |
| ADR-008 | Landscape, touch-first, APK thật là tiêu chí phase. |
| ADR-009 | Character Level, Weapon Mastery, Weapon Technique và Equipment Power là bốn trục độc lập. |
| ADR-010 | Duel Casual dùng PvE build snapshot; ranked normalization để Mốc 3. |
| ADR-011 | Time là chu kỳ game cấu hình được và World State toàn cục. Tỉ lệ chu kỳ là config server — đổi sang 1:1 thời gian thật (86400) không cần ADR, chỉ cần bump cycleConfigVersion + test hai account (Wave 0 D2). |
| ADR-012 | Mốc 1 chỉ có hai Blade Technique đầu tiên; các Discipline/Technique còn lại là Mốc 2. |
| ADR-013 | Permadeath PvE khóa account bằng Player Data; không revive item. |
| ADR-014 | Duel mode đầu tiên chỉ Half-Loss, không permadeath (Duel thuộc Mốc 2 theo ADR-028). |
| ADR-015 | Guest reset/link được; Official không reset; reset chỉ ở Login và Death Screen. |
| ADR-016 | First-clear reward là bonus không độc quyền; boss rematch vẫn có main drop. |
| ADR-017 | Mọi backend call đi qua abstraction ở Core; cài đặt cụ thể ở `Game.Services`. |
| ADR-018 | Gameplay specification nằm ở `12`; phân công song song và contract freeze nằm ở `13`. |
| ADR-019 | World time dùng server snapshot có anchor; client chỉ dự đoán presentation, không ghi hoặc xác thực bằng giờ thiết bị. |
| ADR-020 | Floor ID giữ `floor.NN` và boss progression ID giữ `boss.floor_NN`; mọi ID đã dùng là bất biến. `NN` zero-pad ≥2 chữ số, không giới hạn trên; sắp xếp theo `floorNumber` (03 §2). |
| ADR-021 | Safe Zone chặn DamageRequest trước `IDamageable`; death persistence dùng `RecordDeathAsync(DeathRecord)` và chỉ phát event sau xác nhận. |
| ADR-022 | Domain event names/payloads chuẩn ở `12 §11`; `ItemAcquired` (quest progress) khác `InventoryChanged` (state notification), time events giữ ngữ nghĩa riêng. |
| ADR-023 | Challenge history JSON chuẩn ở `03 §5`, result rules và cap ở `09 §4`; schema cũ migrate sang duration/timestamp/version có đơn vị rõ. |
| ADR-024 | Time rate do server sở hữu: snapshot chứa `realSecondsPerGameDay`; client dự đoán bằng rate trong snapshot; `realSecondsPerGameDay`/`startHour`/`cycleConfigVersion` nằm ở config server function (cùng repo), không ở `TimeConfig` client (08 §2–3). |
| ADR-025 | Dodge là hành động chung ở Mốc 1, dùng data `DodgeDefinition` (không phải Technique, không cần mastery), Wave 2A sở hữu; khởi điểm: lướt 0.18s, cooldown 1.2s, recovery 0.20s, không bất tử, không xuyên tường (07 §4.4). Đổi ý thì chỉnh ADR này. |
| ADR-026 | Hướng nhìn PvE là vector, hình hiển thị 8 hướng: đòn thường, Né và Technique đi theo hướng nhân vật đang nhìn; Animator nhận `FaceX`/`FaceY`; `Facing` ±1 giữ để tương thích; tự nhắm quái gần nhất là tùy chọn, mặc định tắt (`05` Phase 9.5, `10` FB-032/FB-033, `13` gói 9.5a). Duel vẫn nhìn ngang (ADR-002). |
| ADR-027 | Đồ họa (Art Track) chạy song song với các phase, không phải một phase riêng; luật hình vẽ ở `14`, bàn giao ở `15`. Pixel art PPU 32; người chơi vẽ 5 bộ hướng, quái và boss 3 bộ, hướng phía Tây là bản lật; vũ khí là lớp riêng; ngày/đêm do Global Light 2D; màu báo đòn là chức năng nên quái/boss phải qua kiểm tra màu; hình thật chỉ nhập vào game sau khi gói 9.5a và 9.5b xong. Hình tự tạo hoặc có giấy phép rõ, ghi nguồn ở `ASSET_SOURCES.md` (mục 5 trong file này). |
| ADR-028 | 2026-10-18 (product owner): Duel (Phase 10) chuyển sang Mốc 2. Mốc 1 kết thúc ở Phase 11 (kiểm tổng), không có Duel. Giữ số "Phase 10" để không vỡ tham chiếu; thứ tự làm: 9.5 → 9.9 → 11 → 10. Thiết kế Duel (Half-Loss, Lobby/Relay, ADR-007, ADR-010, ADR-014) không đổi; chỉ đổi mốc. |
| ADR-029 | 2026-10-18 (product owner): ưu tiên màu sprite là Chết > màu theo state hiện tại (báo đòn quái hồng nhạt; boss Telegraph vàng/cam, PressureWindup/Dash tím; Punish/Recovery xanh nhạt; Cuồng nộ) > gốc. Giá trị màu báo đòn giữ nguyên (14 §2). Nháy khi trúng KHÔNG còn là lớp màu nhân: nó là lớp phủ trắng riêng (child SpriteRenderer `HitFlash`), alpha 0.85 → 0 trong 0.08 s; khi đang có báo đòn nguy hiểm (quái windup, boss Telegraph/Pressure) thì alpha tối đa 0.5 để màu báo đòn vẫn đọc được. Lý do: báo đòn là thông tin sống còn vì permadeath; màu nhân không tạo được màu trắng. Thay cho thứ tự cũ "Chết > Báo đòn > Nháy khi trúng > Cuồng nộ > Gốc" (13 gói 9.5b, 14 §7 H2) và "chết > nháy > màu trạng thái" của `TintResolver` K1 (05 mục 9.5). |
| ADR-030 | 2026-10-18 (product owner): vai trò quái theo tầng (Phase 9.8). Skirmisher vào Tầng 2 (khớp 05 mục 9.8). Ranged ở phần sâu Vùng Farm Tầng 1 và ở Tầng 2. Tầng 3 trở đi (Mốc 2) có role/biến thể riêng, quyết khi tới (11 §3). |
| ADR-031 | 2026-10-20 (product owner): chế độ AI tự chủ. AI nhận cả phase hoặc nhiều gói và làm liền; được tự thêm tính năng, nội dung, hiệu ứng, UI và tự quyết các mục "Chờ quyết" ở `10` mà không cần hỏi; được đổi kiến trúc nếu ghi ADR trước và có test. Giữ nguyên luật 7 (bản quyền), 9 (backend abstraction), 10 (permadeath và tài khoản): chỉ chủ dự án đổi. Mục tiêu: giống thể loại game leo tháp sinh tử nhất có thể ở mức cơ chế, cấu trúc, nhịp chơi, cảm giác (`16`); tên, hình, âm thanh, UI, cốt truyện, thiết kế phải nguyên bản. Phase "xong để làm tiếp" (code, test xanh, nối scene, chơi thử trong Editor) khác "DONE để nộp" (thêm APK evidence); evidence máy thật gom ở mốc ghi trong `STATUS.md`. Docs lệch code: phần đã làm thì code chạy được và test xanh thắng, phần chưa làm thì docs thắng. Thay phần quy trình hai AI của ADR-018; nội dung gameplay ở `12` không đổi. |

## 7. Bản đồ tài liệu

| File | Nội dung | Đọc khi |
|---|---|---|
| `AGENTS.md` | Luật làm việc | Luôn |
| `02_GAMEPLAY_ARCHITECTURE.md` | Gameplay flow, Floor, world, boss | Sửa gameplay/PvE |
| `03_DATA_DATABASE_ARCHITECTURE.md` | Definition, Player Data, World State | Sửa save/data/backend |
| `04_NETWORK_PVP_ARCHITECTURE.md` | Duel/Lobby/Relay/network | Sửa PvP |
| `05_DEVELOPMENT_ROADMAP.md` | Phase và evidence | Bắt đầu/đóng phase |
| `06_CODE_ARCHITECTURE_AND_MODULE_RULES.md` | Modules và asmdef | Tạo/refactor module |
| `07_CHARACTER_COMBAT_DESIGN.md` | Balance, mastery, Technique | Sửa combat/item/skill |
| `08_TIME_SYSTEM_DESIGN.md` | World time | Sửa time/content time-aware |
| `09_DEATH_AND_DUEL_SYSTEM.md` | Death/account/challenge | Sửa death/account/challenge |
| `11_WORLD_STORY.md` | World, content tone, naming | Tạo content |
| `12_ORIGINAL_GAMEPLAY_BIBLE.md` | Gameplay nguyên bản và contracts | Mọi gameplay mới |
| `13_TWO_AGENT_IMPLEMENTATION_PLAN.md` | Ownership/dependency/handoff AI | Giao việc song song |
| `14_ART_CONTRACT.md` | Luật hình vẽ: khung, màu, 8 hướng, animation, màu báo đòn | Tạo/nhập hình, sửa hiển thị nhân vật |
| `15_ART_HANDOFF.md` | Bàn giao cho AI làm đồ họa | Bắt đầu việc đồ họa |

## 8. Trạng thái hiện tại
- Phase 6 đã DONE.
- Phase 7 (Floor Framework) và Phase 7.5 (Blade Techniques + Dodge) đã DONE.
- Code baseline đã có implementation từ Phase 0 đến Phase 9.
- Đăng nhập ẩn danh và Cloud Save Player Data đã được kiểm thử với backend thật theo `AGENTS.md`.
- Phase 8 (Boss and Shared World State) và Phase 9 (Floor 2 data-only) đã có code + tests; device evidence (P8-5, P9-3) còn chờ điền nên chưa ghi DONE (`05` §2).
- Phase đang làm theo roadmap: Phase 9.5–9.9 (thuộc Mốc 1, đã duyệt 2026-10-14, xem `05`). Thứ tự làm: 9.5 → 9.9 → 11 → 10. Mốc 1 kết thúc ở Phase 11; Phase 10 Duel thuộc Mốc 2 (ADR-028) và làm sau Phase 11. 
- Không tự ghi `DONE` trong roadmap chỉ vì code compile. Mỗi phase cần test, Unity Editor wiring và APK evidence.

## 9. Death và account tóm tắt

- PvE death ngoài Safe Zone do Enemy/Boss/Trap: `death.isDead = true`, save authoritative, disable input, mở Death Screen.
- Guest chưa linked: được tạo account mới từ Death Screen hoặc Login Screen. Official: chỉ thoát.
- PvP: Half-Loss kết thúc trận trước khi HP về 0. Không ghi death và không ảnh hưởng World State.

Chi tiết: `09_DEATH_AND_DUEL_SYSTEM.md`.
