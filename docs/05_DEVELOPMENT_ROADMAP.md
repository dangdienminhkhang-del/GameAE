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
| 6 | Time, NPC, quest | Functional baseline có | Turn-in/touch/save queue và time two-account closure. |
| 6.5 | UGS thật | Auth + Player Cloud Save đã kiểm | Giữ log/evidence backend. |
| 7 | Floor framework | Next implementation | Wave 1B. |

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

### Phase 7.5 Blade Techniques

- Blade Foundation và Traverse definitions, generic executor, mastery 0–25, touch button feedback.
- Dodge theo DodgeDefinition (07 §4.4).
- Done: unlock theo mastery, cooldown/recovery/hit policy đúng; save/load mastery; no duplicate hits. Dodge dùng được bằng cảm ứng, cooldown đúng.

### Phase 8 Boss and Shared World State

- Boss pattern, server-authorized defeat request, world floor unlock, personal reward claim và rematch.
- Done: Account A defeat xác nhận server; Account B thấy Floor mới available; death không làm boss state sai.

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
