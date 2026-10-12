# DEVELOPMENT ROADMAP
## Game2D Evidence Based Delivery Plan

> Nguồn thứ tự phase và điều kiện đóng phase. Cách hai AI làm song song nằm ở `13_TWO_AGENT_IMPLEMENTATION_PLAN.md`.
> Một phase chỉ DONE khi có code, tests, Unity Editor wiring, APK evidence và tài liệu khớp nhau.

---

## 1. Quy tắc giao hàng

1. Một phase tạo được giá trị chơi trên điện thoại, với account/mạng thật khi feature cần backend.
2. `Compiled` không phải `Done`.
3. Không thêm feature Mốc 2 để che giấu một loop Mốc 1 chưa hoàn chỉnh.
4. Mỗi feature persistence phải có save/load test; mỗi feature World State phải có test hai account.
5. Mỗi feature player intent phải có touch path. Keyboard Editor không đủ.
6. Phase song song chỉ được mở theo ownership/document contract ở `13`.

## 2. Trạng thái baseline

| Phase | Nội dung | Trạng thái tài liệu | Evidence còn cần chốt |
|---:|---|---|---|
| 0 | Project, Android, modules | Code baseline có | Consolidate APK/device record. |
| 1 | Authentication | Đã kiểm backend thật | Retry/no-network evidence giữ cùng phase report. |
| 2 | Player touch movement | Code baseline có | Device movement/collision evidence. |
| 3 | Combat core | Code baseline có | Regression damage tests. |
| 4 | Enemy, EXP, level | Code baseline có | Device loop evidence. |
| 5 | Inventory/equipment/save | Code baseline có | Save/load regression evidence. |
| 5.5 | Death, Safe Zone, account | Code baseline có | Guest/Official full device evidence. |
| 6 | Time, NPC, quest | DONE (Wave 1A + 2B) | Evidence Editor + Android, xem bảng bên dưới. |
| 6.5 | UGS thật | Auth + Player Cloud Save đã kiểm | Giữ log/evidence backend. |
| 7   | Floor framework          | DONE (Wave 1B)  | Code + tests + Editor wiring + device evidence. |
| 7.5 | Blade Techniques + Dodge | DONE (Wave 2A) | Code + tests + Editor wiring + device evidence. |

“Code baseline có” không đồng nghĩa phase đã đóng. Không sửa bảng thành DONE nếu evidence vẫn thiếu.

## 3. Mốc 1 Nộp môn

### Phase 0 Nền tảng

- Unity Android project landscape, asmdef Core/PvE/PvP/UI/Services, documentation và cloud project.
- Done: APK mở được trên máy thật; project cloud linked; cấu trúc module có compile boundary.

### Phase 1 Authentication

- Boot → services initialize → anonymous sign-in → Main Menu.
- Mất mạng: error state + retry; không lách vào gameplay.
- Done: account thật đăng nhập, retry đúng, không crash trên máy thật.

### Phase 2 Player Touch Movement

- Player top-down, joystick, collision, camera, idle/walk.
- Done: không xuyên tường; touch responsively; keyboard chỉ là Editor support.

### Phase 3 Combat Core

- Health, IDamageable, hitbox/hurtbox, cooldown, event death, training target, pure damage tests.
- Done: active window mới trúng; damage đúng config; target death và warning/error không mới.

### Phase 4 Enemy and Progression

- Enemy role/state machine, EXP, level, HUD.
- Done: ít nhất hai role enemy; level change save được; enemy defeat không gọi UI/backend trực tiếp.

### Phase 5 Inventory and Equipment

- Item definition, drop table, inventory stack/instance, equipment, Cloud Save.
- Done: loot → equip → stat change → close/reopen app đúng trên account thật.

### Phase 5.5 Death and Account

- Death service, Safe Zone, Death Screen, guest reset/link account.
- Done: Enemy/Boss/Trap death lưu authoritative; Guest/Official flows đúng; PvP không gọi death.

### Phase 6 Time NPC Quest Closure

Phase này được đóng theo hai work package độc lập trong `13`:

- Wave 1A: Quest/NPC/mobile hardening.
- Wave 2B: authoritative world time.

Done:

1. Touch receive/progress/turn-in quest; reward chỉ xuất hiện sau turn-in.
2. Locked/time/prerequisite/target rule được test.
3. Quest save tuần tự, reload không mất state hoặc double reward.
4. Hai account thật thấy cùng world time phase sau resync.
5. Time unavailable hiển thị snapshot đã xác nhận nếu có, không dùng giờ thiết bị làm authority/client-write; time-sensitive content bị chặn nếu snapshot cũ hoặc tuổi không xác định.

### Phase 7 Floor Framework

- `FloorDefinition`, database, loader, selector, loading/error state.
- Floor content được đăng ký bằng data: spawn, NPC, quest, Safe Zone, boss reference, scaling.
- Done: Floor 1 playable; Floor 2 thêm bằng data/scene; không sửa Core/PvE logic để thêm Floor 2.
- Evidence: FloorDatabase.asset có 2 floor (floor.01 Available, floor.02 Locked cần boss); FloorSelectCanvas active, FloorButton có FloorToggleButton (không nhầm PvEActionButton); FloorLoader kiểm tra world state + scene host; 20 test EditMode pass (FloorFramework 6, FloorScaling 4, FloorSelectorTests 10); kiểm trên điện thoại OK.

### Phase 7.5 Blade Techniques + Dodge

- Blade Foundation và Traverse definitions, generic executor, mastery 0–25, touch button feedback.
- Dodge theo DodgeDefinition (07 §4.4).
- Done: unlock theo mastery, cooldown/recovery/hit policy đúng; save/load mastery; no duplicate hits. Dodge dùng được bằng cảm ứng, cooldown đúng.
- Evidence: TechniqueController + DodgeController trên Player prefab; 4 nút TechniqueButton + 1 DodgeButton active trong Canvas; Dodge/Technique điều phối chéo (từ chối khi đang chạy cái kia); Rigidbody2D.Cast + MovePosition trong FixedUpdate không xuyên tường; 22 test EditMode pass (WeaponTechnique 12, Wave2ABalance 10); kiểm trên điện thoại OK.

### Phase 8 Boss and Shared World State

- Boss pattern, server-authorized defeat request, world floor unlock, personal reward claim và rematch.
- Done: Account A defeat xác nhận server; Account B thấy Floor mới available; death không làm boss state sai.
- Evidence (Task P8-5). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật đúng điều kiện ghi trong bảng; không tự điền `Pass`. Số liệu boss và giả định sandbox ở `07` §5.2, §9.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) A hạ boss, server xác nhận → B thấy Tầng 2 "Sẵn sàng" | Android A (Guest 1) + thiết bị hoặc Editor B (Guest 2), Wi-Fi | A vào Tầng 1, hạ boss, chờ dòng "Máy chủ đã ghi nhận. Tầng kế đã mở."; B đăng nhập rồi mở màn chọn tầng | B thấy Tầng 1 Cleared và Tầng 2 "Sẵn sàng" |  |  |  |
| (b) Mất mạng lúc boss gục → bảng Thử lại → có mạng bấm Thử lại → xác nhận | Android, Guest, bật chế độ máy bay trước khi boss gục | Hạ boss khi không có mạng → thấy bảng "Boss đã gục nhưng chưa báo được lên máy chủ..." → tắt chế độ máy bay → bấm Thử lại (thử bấm đúp) | Hiện "Đang gửi..." và không bấm đúp được; sau đó bảng ẩn, hiện "Máy chủ đã ghi nhận. Tầng kế đã mở." khoảng 3 giây; tài khoản khác thấy Tầng 2 "Sẵn sàng" |  |  |  |
| (c) Chết giữa trận boss ngoài Safe Zone → lý do Boss, boss/world không đổi | Android, Guest, Wi-Fi | Vào trận boss, để boss hạ gục khi đứng ngoài Safe Zone; sau đó tài khoản khác mở màn chọn tầng | Death Screen ghi nguyên nhân Boss; boss vẫn Active, Tầng 2 vẫn khóa; không có báo World State |  |  |  |
| (d) Tài khoản khác hạ boss lần đầu → first-clear đúng một lần | 2 account Guest (A đã hạ boss, B chưa), Wi-Fi | B hạ boss (World Boss đã DEFEATED), rồi hạ thêm lần nữa; xem `rewardClaims` của B | B nhận bonus first-clear đúng một lần (một claim `FirstClear` cho `boss.floor_01`); các lần sau không nhận lại |  |  |  |
| (e) Tái đấu: không báo World State, đồ chính rơi, đồ hiếm giảm | Android, Guest đã nhận first-clear, Wi-Fi | Hạ boss lần thứ hai trở đi nhiều lần, ghi lại các món rơi | Không có lần báo World State mới; đồ chính vẫn rơi; đồ hiếm rơi ít hơn rõ rệt (chance nhân `rematchRareMultiplier` 0.05) |  |  |  |
| (f) Đóng/mở lại app giữ trạng thái | Android, Guest, Wi-Fi | Sau khi server xác nhận hạ boss: vuốt đóng app → mở lại → đăng nhập → vào màn chọn tầng | Boss vẫn DEFEATED, Tầng 2 vẫn "Sẵn sàng", first-clear không nhận lại |  |  |  |
| (g) Lùi ra khỏi vùng đánh trong lúc telegraph → không dính đòn | Android, Guest, Wi-Fi | Đứng sát boss; khi boss telegraph đòn thường (vàng/cam) thì lùi ra xa hơn `attackHitRadius`; với đòn lao (tím) thì ra khỏi hành lang lao | HP không giảm; đòn thường trượt; đòn lao không trúng khi đứng ngoài hành lang |  |  |  |
| (h) Thời gian hạ boss lần đầu trên điện thoại ∈ 90–150 giây | Android thật, Guest Lv 5, Wi-Fi | Bấm đồng hồ từ lúc boss bắt đầu đuổi tới lúc boss gục (lần đầu) | 90–150 giây; số đo thật thắng ước lượng sandbox |  |  |  |

### Phase 9 Data Only Floor Two

- Content Floor 2 qua definition/scene/data, dùng scaling có cấu hình.
- Done: thêm Floor 2 không cần branch theo floor number hoặc code Core/PvE mới.

### Phase 10 Duel Online

- Lobby code, Relay, transport abstraction, Arena side-view, Half-Loss, cleanup, challenge history.
- Done: hai thiết bị vào được một room, thấy state đồng bộ, HP dưới 50 phần trăm kết thúc, disconnect không kẹt UI.

### Phase 11 Mốc 1 Regression

- Android performance, offline/error states, tests, evidence table, documentation alignment.
- Done: Floor 1 loop, world time, death, Floor 2 data-only và Duel đều pass theo device evidence.

## 4. Mốc 2 và Mốc 3

| Mốc | Chỉ làm sau khi Mốc 1 ổn định |
|---|---|
| Mốc 2 | Control/Signature Technique, các Weapon Discipline khác, dungeon đầy đủ, Floor 3–10, PvP bot/notification. |
| Mốc 3 | Nhiều người trong Floor, dedicated world backend, ranked/matchmaking, Floor 11–100. |

Khi thiếu thời gian, cắt content mở rộng từ dưới lên. Không cắt authentication, Cloud Save, Time, Death, touch input, APK tests hoặc documentation evidence.

## 5. Phase Evidence Template

Mỗi phase tạo một bảng evidence trong report/issue:

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| Ví dụ quest turn-in | Android A, Guest, Wi-Fi | Nhận → đủ objective → trả NPC | Một reward, state Completed sau save |  |  |  |

Không tự điền `Pass` nếu chưa chạy trên điều kiện ghi trong bảng.

| Case | Thiết bị/account/network | Bước | Expected | Pass | Ngày |
| Quest touch | Android, Guest, Wi-Fi | Nhận → đánh 3 slime → giao bằng nút Interact | Reward một lần, quest Completed | Pass | |
| Đóng app sau turn-in | Android, Guest | Giao quest → vuốt đóng app ngay → mở lại | Quest Completed, EXP tăng đúng một lần | Pass | |
| Time hai máy | Editor + Android | Mở cùng lúc, so đồng hồ HUD | Cùng phase, lệch vài giây | Pass | |
| Mất mạng | Android, chế độ máy bay | Mở app khi không có mạng | Không sập, hiện giờ gần nhất | Pass | |