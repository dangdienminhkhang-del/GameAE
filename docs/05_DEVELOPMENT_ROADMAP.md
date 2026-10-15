# DEVELOPMENT ROADMAP
## Game2D Evidence Based Delivery Plan

> Nguồn thứ tự phase và điều kiện đóng phase. Cách hai AI làm song song nằm ở `13_TWO_AGENT_IMPLEMENTATION_PLAN.md`.
> Một phase chỉ DONE khi có code, tests, Unity Editor wiring, APK evidence và tài liệu khớp nhau.
> **Cập nhật 2026-10-14 (product owner):** thêm Phase 9.5–9.9 trước Duel để game có cảm giác leo tầng (phản hồi chiến đấu, nền UX, phân vùng tầng, quái đa vai trò, hướng dẫn). Duel (Phase 10) làm sau 9.9; nếu product owner chuyển Duel sang Mốc 2 thì phải cập nhật `01`, `12` §1 và `13` qua ADR. Art Track (cuối §3) vẫn là đề xuất.

---

## 1. Quy tắc giao hàng

1. Một phase tạo được giá trị chơi trên điện thoại, với account/mạng thật khi feature cần backend.
2. `Compiled` không phải `Done`.
3. Không thêm feature Mốc 2 để che giấu một loop Mốc 1 chưa hoàn chỉnh.
4. Mỗi feature persistence phải có save/load test; mỗi feature World State phải có test hai account.
5. Mỗi feature player intent phải có touch path. Keyboard Editor không đủ.
6. Phase song song chỉ được mở theo ownership/document contract ở `13`.
7. Mỗi phase từ 9.5 trở đi (trừ Phase 11) phải có ít nhất một thay đổi người chơi nhìn thấy, nghe thấy hoặc chạm được. "Đúng dữ liệu" chưa đủ để đóng phase nếu người chơi không cảm nhận được gì mới.
8. Phase Regression (11) chỉ kiểm tra và sửa lỗi, không thêm tính năng. Tính năng người chơi chạm hằng giây (pause, settings, joystick, nút bình) thuộc phase trước Duel (xem `10_FEATURE_BACKLOG.md`).
9. Phase 9.5–9.9 thuộc **Mốc 1** (hoàn thiện loop chơi), không phải Mốc 2, nên không vi phạm luật 3. Mỗi phase phải có work package (path sở hữu, path chỉ đọc, test bắt buộc) trước khi giao việc; đưa vào `13` khi product owner duyệt.
10. Thứ tự 9.5 → 9.9 do product owner chọn theo giá trị cảm giác leo tầng và phụ thuộc kỹ thuật (9.5 dựng `PlaySpeedState` và âm thanh cho 9.6 và 9.7; 9.9 chạy cuối để dạy đúng điều khiển cuối cùng). Đổi thứ tự hoặc cắt phase thì ghi vào `10`.

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
| 8 | Boss + World State | Code + tests đã có; device evidence chờ điền | Chạy bảng P8-5 trên máy thật; không tự điền `Pass`. |
| 9 | Data-only Floor 2 | Code + tests đã có; device evidence chờ điền | Chạy bảng P9-3 trên máy thật; không tự điền `Pass`. |
| 9.5 | Cảm giác chiến đấu + Âm thanh | Đã duyệt 2026-10-14, chưa làm | Bảng P9.5. |
| 9.6 | Nền UX | Đã duyệt 2026-10-14, chưa làm | Bảng P9.6. |
| 9.7 | Phân vùng Tầng (9.7a, 9.7b) | Đã duyệt 2026-10-14, chưa làm | Bảng P9.7a, P9.7b. |
| 9.8 | Quái đa vai trò | Đã duyệt 2026-10-14, chưa làm | Bảng P9.8. |
| 9.9 | Hướng dẫn + Mũi tên quest | Đã duyệt 2026-10-14, chưa làm | Bảng P9.9. |

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
- Done: Floor 1 playable; Floor 2 thêm bằng data/scene; không sửa Core/PvE logic để thêm Floor 2. (Lưu ý: khung chuyển tầng dùng chung — bật/tắt nội dung tầng, đặt người chơi, hồi sinh quái, reset boss — chưa có ở Phase 7 và được bổ sung một lần cho mọi tầng ở Phase 9, P9-1/P9-1b.)
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
| (a) A hạ boss, server xác nhận → B thấy Tầng 2 "Sẵn sàng" | Android A (Guest 1) + thiết bị hoặc Editor B (Guest 2), Wi-Fi | A vào Tầng 1, hạ boss, chờ dòng "Máy chủ đã ghi nhận. Tầng kế đã mở."; B đăng nhập rồi mở màn chọn tầng | B thấy Tầng 1 Cleared và Tầng 2 "Sẵn sàng" |  |Pass| ngày 11/10/2026  |
| (b) Mất mạng lúc boss gục → bảng Thử lại → có mạng bấm Thử lại → xác nhận | Android, Guest, bật chế độ máy bay trước khi boss gục | Hạ boss khi không có mạng → thấy bảng "Boss đã gục nhưng chưa báo được lên máy chủ..." → tắt chế độ máy bay → bấm Thử lại (thử bấm đúp) | Hiện "Đang gửi..." và không bấm đúp được; sau đó bảng ẩn, hiện "Máy chủ đã ghi nhận. Tầng kế đã mở." khoảng 3 giây; tài khoản khác thấy Tầng 2 "Sẵn sàng" |  |Pass  | 11/10/2026  |
| (c) Chết giữa trận boss ngoài Safe Zone → lý do Boss, boss/world không đổi | Android, Guest, Wi-Fi | Vào trận boss, để boss hạ gục khi đứng ngoài Safe Zone; sau đó tài khoản khác mở màn chọn tầng | Death Screen ghi nguyên nhân Boss; boss vẫn Active, Tầng 2 vẫn khóa; không có báo World State |  | Pass | 11/10/2026 |
| (d) Tài khoản khác hạ boss lần đầu → first-clear đúng một lần | 2 account Guest (A đã hạ boss, B chưa), Wi-Fi | B hạ boss (World Boss đã DEFEATED), rồi hạ thêm lần nữa; xem `rewardClaims` của B | B nhận bonus first-clear đúng một lần (một claim `FirstClear` cho `boss.floor_01`); các lần sau không nhận lại |  | Pass |  |
| (e) Tái đấu: không báo World State, đồ chính rơi, đồ hiếm giảm | Android, Guest đã nhận first-clear, Wi-Fi | Hạ boss lần thứ hai trở đi nhiều lần, ghi lại các món rơi | Không có lần báo World State mới; đồ chính vẫn rơi; đồ hiếm rơi ít hơn rõ rệt (chance nhân `rematchRareMultiplier` 0.05); không nhận thêm EXP |  | Pass |11/10/2026  |
| (f) Đóng/mở lại app giữ trạng thái | Android, Guest, Wi-Fi | Sau khi server xác nhận hạ boss: vuốt đóng app → mở lại → đăng nhập → vào màn chọn tầng | Boss vẫn DEFEATED, Tầng 2 vẫn "Sẵn sàng", first-clear không nhận lại |  |Pass  |12/10/2026  |
| (g) Lùi ra khỏi vùng đánh trong lúc telegraph → không dính đòn | Android, Guest, Wi-Fi | Đứng sát boss; khi boss telegraph đòn thường (vàng/cam) thì lùi ra xa hơn `attackHitRadius`; với đòn lao (tím) thì ra khỏi hành lang lao | HP không giảm; đòn thường trượt; đòn lao không trúng khi đứng ngoài hành lang |  | Pass |  |
| (h) Thời gian hạ boss lần đầu trên điện thoại ∈ 90–150 giây | Android thật, Guest Lv 5, Wi-Fi | Bấm đồng hồ từ lúc boss bắt đầu đuổi tới lúc boss gục (lần đầu) | 90–150 giây; số đo thật thắng ước lượng sandbox |  | Pass | 120 giây |

### Phase 9 Data Only Floor Two

- Content Floor 2 qua definition/scene/data, dùng scaling có cấu hình.
- Done: thêm Floor 2 chỉ bằng dữ liệu/scene — không branch theo floor number và không có code Core/PvE riêng cho Floor 2. Khung chuyển tầng dùng chung (P9-1/P9-1b: `FloorSceneBinder`, `FloorActivation`, cổng chỉ chuyển tầng trong Safe Zone) là mã chung cho mọi tầng, được thêm một lần và có test chặn rẽ nhánh theo số tầng (`FloorBranchingScanTests`).
- Evidence (Task P9-3). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật đúng điều kiện ghi trong bảng; không tự điền `Pass`. Nội dung Tầng 2 ở `11` §3.1; số cân bằng và kết quả sandbox ở `07` §5.1, §6, §9.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) A hạ boss Tầng 1 → chọn Tầng 2 → vào Tầng 2 ở Safe Zone | Android, Guest, Wi-Fi | Hạ boss Tầng 1 (server xác nhận); đứng trong Safe Zone; mở bảng Chọn Tầng, chọn Tầng 2 | Người chơi xuất hiện trong Safe Zone Tầng 2; nội dung Tầng 1 (quái, boss, NPC, vật phẩm) biến mất |  | Pass |  |
| (b) Quái Tầng 2 mạnh hơn Tầng 1 đúng hệ số (đếm đòn Guard/Elite) | Android, Guest Lv 6, Wi-Fi | Đếm số đòn thường để hạ Guard và Elite; đếm số đòn mỗi con đánh trúng khi đứng im; so với bảng `07` §9 | Guard hạ trong khoảng 4–7 đòn, Elite hạ trong khoảng 12–20 giây (bấm đồng hồ); Guard hạ người 10–20 đòn, Elite 5–10 đòn |  | Pass |  |
| (c) NPC và quest Tầng 2 nhận/giao được bằng cảm ứng | Android, Guest, Wi-Fi | Đứng gần Người Vẽ Hải Đồ, bấm nút Talk/Quest → nhận "Kiểm tra van áp" → hạ 3 Lính Gác Lò Hơi → giao lại | Nhận được, tiến độ đếm đúng, giao được; thưởng 60 EXP + 2 Bình Máu đúng một lần |  | Pass |  |
| (d) Quay lại Tầng 1: quái hồi sinh, boss reset (rematch) | Android, Guest, Wi-Fi | Từ Safe Zone Tầng 2 chọn Tầng 1 | Nội dung Tầng 2 biến mất; quái Tầng 1 hồi sinh đủ; boss hồi đầy máu và đánh lại được (tái đấu, không có EXP boss) |  | Pass |  |
| (e) Chết ở Tầng 2: màn chết hiện "Chết ở tầng: 2" và "Tầng cao nhất: 2" | Android, Guest mới, Wi-Fi | Vào Tầng 2, ra ngoài Safe Zone, để quái hạ gục | Màn chết hiện "Chết ở tầng: 2" (cạnh Nguyên nhân) và "Tầng cao nhất: 2" (trong Thành tích) |  | Pass |  |
| (f) Ngoài Safe Zone bấm chọn tầng → bị từ chối, bảng không đóng; trong Safe Zone → chuyển được | Android, Guest, Wi-Fi | Đứng ngoài Safe Zone, mở Chọn Tầng, bấm một tầng → quan sát; quay vào Safe Zone, bấm lại | Ngoài Safe Zone: báo từ chối, bảng vẫn mở, không dịch chuyển; trong Safe Zone: chuyển tầng thành công |  | Pass |  |
| (g) Đang ở Tầng 1 bấm chọn Tầng 1 → không dịch chuyển, quái/boss không reset | Android, Guest, Wi-Fi | Đang đứng ở Tầng 1 (trong Safe Zone), hạ vài quái; mở Chọn Tầng, bấm Tầng 1 | Người chơi không bị đặt lại vị trí; quái đã hạ không hồi sinh; boss không reset |  | Pass |  |
| (h) Từng lên Tầng 2, về Tầng 1 rồi chết: "Chết ở tầng: 1" và "Tầng cao nhất: 2" | Android, Guest, Wi-Fi | Vào Tầng 2, quay lại Tầng 1, ra ngoài Safe Zone, để quái hạ gục | Màn chết hiện "Chết ở tầng: 1" và "Tầng cao nhất: 2" |  | Pass |  |

### Phase 9.5 Cảm giác chiến đấu và Âm thanh

- Mục tiêu: đánh quái phải nhìn thấy, nghe thấy và cảm nhận được từng đòn. Hiện đòn trúng chưa có số sát thương, chưa nháy, chưa có tiếng, chưa có nhạc nền.
- Nguồn: FB-022, FB-023 trong `10`.
- Phạm vi:
  1. Số sát thương nổi (pool) cho đòn người chơi gây ra và đòn người chơi nhận; màu phân biệt hai loại.
  2. Nháy khi trúng (quái, boss, người chơi); không được xóa màu telegraph đang hiển thị.
  3. Dừng hình ngắn (tối đa 0.06 giây thời gian thực) chỉ cho đòn xác nhận của người chơi, có giới hạn tần suất. Mọi thay đổi `timeScale` đi qua một điểm duy nhất (`PlaySpeedState`) để Pause ở 9.6 dùng lại; không nơi nào khác ghi `Time.timeScale`.
  4. Hiệu ứng tan khi quái chết (trong `deathSeconds`), reset đúng khi quái được dùng lại từ pool.
  5. Thanh máu trên đầu quái; thanh máu boss trên HUD.
  6. Âm thanh: dịch vụ phát âm (pool AudioSource), nhạc nền gameplay và nhạc boss, SFX (vung đòn, trúng, bị trúng, kỹ thuật, né, nhặt, lên cấp, UI). Âm lượng nhạc/SFX lưu local (PlayerPrefs); màn Settings làm ở 9.6.
- Không làm: hệ chí mạng mới, particle nặng, âm thanh 3D, mixer phức tạp, nhạc/SFX không rõ nguồn.
- Ràng buộc: pool, không Instantiate/Destroy trong combat; chỉ nghe event (`OnHitResolved`, `OnHealthChanged` đã lọc theo người chơi, `OnEnemyDefeated`...) và không tự trừ máu; PvE không tham chiếu UI; không tạo asmdef mới (`13` §3); asset âm thanh có nguồn và giấy phép ghi trong `ASSET_SOURCES.md` (`11` §5, §7).
- Done: đánh quái thường, Elite và boss đều có số, nháy, dừng hình ngắn và tiếng; quái chết có hiệu ứng; có nhạc nền; màu telegraph không bị phá; mọi case P7.5/P8/P9 đã Pass vẫn Pass.
- Evidence (Task P9.5). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Số, nháy, telegraph | Android, Guest, Wi-Fi | Đánh quái thường và Elite; để quái đánh trúng mình | Số sát thương đúng giá trị hiện ở đúng chỗ, màu khác khi mình bị đánh; quái nháy khi trúng; telegraph vẫn đúng màu |  |  |  |
| (b) Dừng hình không trễ input | Android, Guest | Đánh liên tục 20 đòn; bấm Dodge ngay sau một đòn trúng | Dừng hình ngắn; nút Attack và Dodge phản hồi ngay sau đó; bị đánh thì không dừng hình |  |  |  |
| (c) Quái chết và dùng lại từ pool | Android, Guest | Hạ 10 quái, chờ hồi sinh | Quái tan khi chết; quái hồi sinh hiện đủ màu, đủ kích thước, không còn mờ; không có lỗi Instantiate/Destroy mới trong Profiler |  |  |  |
| (d) Thanh máu quái và boss | Android, Guest | Đánh quái thường, Elite, boss | Thanh máu giảm đúng; quái chết thì thanh máu ẩn; boss có thanh trên HUD |  |  |  |
| (e) Nhạc và SFX | Android, Guest | Chơi gameplay, vào trận boss, bấm UI, nhặt đồ, lên cấp; bấm Home rồi quay lại app | Có nhạc nền và nhạc boss; SFX đúng sự kiện; ra nền thì tắt tiếng, quay lại tiếp tục đúng |  |  |  |
| (f) Hiệu năng | Android máy mục tiêu, 10 phút | Giao tranh liên tục với nhiều quái | Ghi FPS trung bình/thấp nhất và bộ nhớ; không rò rỉ rõ rệt. Ngưỡng FPS do product owner đặt (đề xuất ≥30) |  |  |  |
| (g) Không hồi quy | Android, Guest | Chạy lại các case P7.5, P8, P9 đã Pass; chạy toàn bộ test EditMode | Vẫn Pass; test xanh |  |  |  |

### Phase 9.6 Nền UX

- Mục tiêu: điều khiển và menu đủ tốt để chơi liên tục trên điện thoại mà không "chết oan" vì điều khiển.
- Nguồn: FB-001, 002, 005, 006, 009, 010, 011 trong `10`; loading bản nhẹ từ mục 3.1.
- Phạm vi: Pause (kèm nút Back của Android) dùng `PlaySpeedState` của 9.5; Settings tối thiểu (âm lượng nhạc/SFX, rung, kiểu joystick cố định/floating, dead zone); joystick floating ở nửa trái màn hình kèm dead zone; nút dùng bình; rung khi trúng đòn và HP thấp; overlay loading có thanh tiến độ khi vào gameplay.
- Nút dùng bình theo `ConsumableRules` và `07` §2: chỉ dùng được ngoài giao chiến. Trong giao chiến nút hiện "Đang giao chiến" kèm đếm ngược (khoảng 5 giây). Muốn cho dùng bình giữa trận thì phải đổi `07` §2 và `ConsumableRules` qua quyết định của product owner, không tự đổi trong phase này.
- Settings lưu local (PlayerPrefs), không vào Player Data; muốn đồng bộ đa thiết bị thì qua ACR. Không có nút Reset Guest trong Settings hay Main Menu (`AGENTS.md` luật 10).
- Pause và nút Back bị vô hiệu khi đang xử lý Death flow (`09` §1.3); thời gian thế giới từ server không dừng khi Pause.
- "Về Main Menu" trong Pause bị khóa khi đang giao chiến (cùng khái niệm "đang giao chiến" và đếm ngược của nút bình), để Pause không thành nút thoát hiểm giữa trận (`09` §1.2: Safe Zone không được là nút thoát hiểm; hiện chưa lưu vị trí nên vào lại điểm vào Safe Zone, FB-017). Thoát app bằng Home hay tắt app không chặn được; product owner có thể bỏ khóa này bằng quyết định ghi vào `10`.
- Mọi thứ có touch path (luật 5).
- Done: pause giữ nguyên trạng thái và resume đúng; nút Back mở Pause thay vì thoát app; 10 phút chơi liên tục trên máy thật có không quá 1 lần chết do lỗi điều khiển (FB-011); mọi case P9.5 đã Pass vẫn Pass.
- Evidence (Task P9.6). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Pause và nút Back | Android, Guest | Giữa trận bấm Pause rồi Resume; bấm nút Back (cả khi đang mở Túi/Quest/Settings); thử "Về Main Menu" ngay sau khi bị đánh rồi lại sau khi hết giao chiến; bấm Back khi Death Screen đang hiện | Game dừng; Resume tiếp tục đúng; Back đóng panel đang mở trước, không còn panel thì mở Pause thay vì thoát app; "Về Main Menu" bị khóa kèm đếm ngược khi đang giao chiến, dùng được khi hết; về Main Menu không mất tiến trình đã lưu; Back không làm gì trên Death Screen |  |  |  |
| (b) Settings | Android | Đổi âm lượng nhạc/SFX, rung, kiểu joystick; đóng và mở lại app | Giá trị giữ nguyên; tắt rung thì không rung; không có nút Reset Guest |  |  |  |
| (c) Joystick floating và dead zone | Android | Chạm nửa trái màn hình ở nhiều chỗ; để ngón tay run nhẹ | Joystick xuất hiện đúng chỗ chạm; run dưới dead zone không làm nhân vật nhích |  |  |  |
| (d) Nút bình | Android, Guest | Ngoài giao chiến HP thấp bấm nút bình; HP đầy bấm nút; hết bình bấm nút; trong giao chiến bấm nút | Ngoài giao chiến: một chạm dùng được, số bình giảm 1, hồi đúng; HP đầy và hết bình thì từ chối có lý do; trong giao chiến: hiện "Đang giao chiến" đếm ngược, không mất bình, hết đếm ngược thì dùng được |  |  |  |
| (e) Rung | Android | Bị trúng đòn; HP xuống thấp; tắt rung trong Settings | Rung khi trúng; rung cảnh báo HP thấp; tắt thì không rung |  |  |  |
| (f) Loading | Android | Mở app lạnh rồi vào gameplay | Có overlay loading với tiến độ; không có màn hình đen kéo dài không phản hồi |  |  |  |
| (g) Bài test "chết oan" (FB-011) | Android, Guest mới, 10 phút | Chơi liên tục ở khu có quái; ghi nguyên nhân từng lần chết | Không quá 1 lần chết do lỗi điều khiển |  |  |  |
| (h) Không hồi quy | Android, Guest | Chạy lại các case P9.5 đã Pass | Vẫn Pass |  |  |  |

### Phase 9.7 Phân vùng Tầng

- Mục tiêu: người chơi có cảm giác đi từ nơi này sang nơi khác. Hiện quái, boss, NPC và khu an toàn nằm chung một chỗ; boss kích hoạt chỉ theo khoảng cách; quái chưa có ràng buộc vùng.
- Nguồn: FB-021 trong `10`.
- Mỗi Floor tách thành vùng có mục đích riêng: **Thị trấn** (Safe Zone: NPC, quest, Chọn Tầng), **Vùng Farm** (quái thường, Elite, hồi sinh quái) và **Hầm Boss** (hành lang → tiền sảnh → phòng boss). Vùng Hầm Boss chỉ có khi `bossId` của Floor khác rỗng; Floor 2 hiện chưa có boss nên chỉ có Thị trấn và Vùng Farm. Các vùng nối bằng lối đi bộ thật, không dịch chuyển.
- Ràng buộc chung: một scene mỗi Floor; Safe Zone là Thị trấn theo `09` §1.2; Chọn Tầng chỉ trong Safe Zone (giữ P9 (f)); không có quái trong Thị trấn; mọi thứ qua được bằng cảm ứng; vào vùng chỉ hiện banner tên vùng, không popup xác nhận; thêm field vào `FloorDefinition` (nếu cần) phải qua ACR (`13` §7); không rẽ nhánh theo số tầng.
- Không làm: nhiều scene cộng dồn, puzzle hay cơ cấu trong hầm (Mốc 2), mini-map, nhiều người cùng một Floor (Mốc 3).

#### Phase 9.7a Bố cục vùng và cổng

- Phạm vi: thành phần vùng (`ZoneId`, loại vùng, vùng trigger); phát `ZoneEntered(zoneId, displayName)` theo hợp đồng `12` §11 (đồng thời phục vụ objective Reach; `displayName` cho banner tên vùng); camera bounds theo vùng (`CameraFollow.SetBounds`) có chuyển mượt; quái chỉ spawn trong vùng được gán và không bao giờ trong Safe Zone (`02` §19, `09` §1.2); `FloorSpawnRegistrar` hỗ trợ nhiều spawner (mỗi khu Farm một spawner); quái ngừng đuổi khi người chơi vào Safe Zone (`02` §12) và quay về điểm xuất phát; nhạc theo vùng (dùng dịch vụ âm thanh của 9.5); banner tên vùng.
- Done: quái chỉ spawn trong vùng được gán; quái không đuổi vào Thị trấn; đi bộ từ Thị trấn tới Cổng Hầm (hoặc cuối Vùng Farm nếu Floor chưa có boss) khoảng 20–30 giây khi không giao tranh (số khởi điểm, số đo thật thắng); camera và nhạc đổi theo vùng; Floor 2 cùng cấu trúc bằng data/scene; mọi case P7/P8/P9 và P9.5/P9.6 đã Pass vẫn Pass.
- Evidence (Task P9.7a). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Thị trấn an toàn | Android, Guest, Wi-Fi | Vào Tầng 1, đứng yên 60 giây trong Thị trấn | Không có quái xuất hiện hay đánh vào; NPC và bảng Chọn Tầng dùng được |  |  |  |
| (b) Vùng Farm có quái và hồi sinh | Android, Guest, Wi-Fi | Đi sang Vùng Farm; hạ 3 quái; chờ hồi sinh | Quái xuất hiện đúng vùng; hồi sinh theo cấu hình |  |  |  |
| (c) Quái không đuổi vào Thị trấn | Android, Guest | Kéo một quái đang đuổi về phía Thị trấn rồi vào Safe Zone | Quái dừng đuổi và quay về điểm xuất phát; không đánh được người chơi trong Safe Zone |  |  |  |
| (d) Thời gian đi bộ | Android, Guest | Bấm giờ từ Thị trấn tới Cổng Hầm, không giao tranh | Khoảng 20–30 giây; số đo thật thắng ước lượng |  |  |  |
| (e) Camera và nhạc theo vùng | Android, Guest | Đi qua từng vùng | Camera không lộ ngoài vùng; nhạc đổi khi qua vùng; banner tên vùng hiện một lần |  |  |  |
| (f) Reach theo vùng | Android, Guest | Nhận quest Reach (nếu có) và đi vào vùng đích | Tiến độ tăng đúng một lần qua `ZoneEntered`; không tăng khi chỉ đi ngang vùng khác |  |  |  |
| (g) Tầng 2 cùng cấu trúc, data-only | Android, Guest, Wi-Fi | Chọn Tầng 2 trong Thị trấn Tầng 1; đi qua các vùng Tầng 2 | Tầng 2 có Thị trấn và Vùng Farm; không có Hầm Boss vì chưa có boss; không sửa Core/PvE ngoài thành phần vùng dùng chung |  |  |  |
| (h) Không hồi quy | Android, Guest | Chạy lại các case P8, P9, P9.5, P9.6 đã Pass | Vẫn Pass |  |  |  |

#### Phase 9.7b Hầm Boss và phòng boss

- Phạm vi: boss chỉ kích hoạt khi người chơi vào phòng boss (không còn kích hoạt chỉ theo `DetectionRange`); hành lang và tiền sảnh không kích hoạt boss; banner khi vào Hầm Boss.
- Mặc định: không khóa cửa phòng boss. Người chơi rời phòng thì trận kết thúc: hủy telegraph và đòn lao đang chạy, boss về vị trí gốc, giữ nguyên máu và trạng thái enrage. Muốn khóa cửa trong trận thì phải có quyết định của product owner (permadeath cần đường lui rõ ràng) và ACR.
- Không đổi luật Phase 8: boss chết vẫn gửi request lên server; chết trong phòng boss ghi nguyên nhân Boss; `ResetEncounter` khi đổi tầng giữ nguyên; rematch không EXP.
- Done: boss không kích hoạt từ ngoài phòng; rời phòng thì boss về và giữ máu; vào lại thì đánh tiếp đúng trạng thái; mọi case P8 (a)–(h) đã Pass vẫn Pass.
- Evidence (Task P9.7b). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Boss không kích hoạt từ ngoài | Android, Guest | Đứng ở hành lang và tiền sảnh | Boss không đuổi, không telegraph; vào phòng boss mới kích hoạt |  |  |  |
| (b) Rời phòng giữa trận | Android, Guest | Đánh boss xuống khoảng nửa máu, đang telegraph thì chạy ra cửa | Không dính đòn ngoài phòng; boss về vị trí gốc, máu và enrage giữ nguyên |  |  |  |
| (c) Vào lại phòng | Android, Guest | Quay lại phòng sau (b) | Boss tiếp tục với máu cũ; hạ được boss; server xác nhận như P8 (a) |  |  |  |
| (d) Chết ở từng vùng | Android, 2 Guest mới | Account 1 chết ở Vùng Farm; account 2 chết trong phòng boss | Death Screen ghi đúng nguyên nhân và "Chết ở tầng"; Thị trấn không gây chết |  |  |  |
| (e) Không hồi quy | Android, Guest | Chạy lại các case P8 (a)–(h) | Vẫn Pass |  |  |  |

### Phase 9.8 Quái đa vai trò

- Mục tiêu: Vùng Farm có quái cư xử khác nhau, người chơi phải đổi cách đánh. Hiện mọi quái dùng chung một hành vi (đuổi → báo đòn → cận chiến) vì `EnemyDefinition` chưa có role; `12` §7.1, `07` §5 và `11` §3 đã mô tả role nhưng code chưa có.
- Nguồn: FB-030 trong `10`.
- Phạm vi: `CombatRole` (Chaser, Skirmisher, Guard, Ranged, Elite theo `12` §7.1) là field trong `EnemyDefinition`, mặc định Chaser nên asset hiện có không đổi hành vi; hành vi mới cho Skirmisher (áp sát, đánh, lùi) và Ranged (giữ khoảng cách, báo đòn rõ, bắn đạn pool theo hướng đã khóa cuối windup nên né được). Guard và Elite tạm dùng hành vi Chaser cho tới khi có thiết kế riêng.
- Hai định nghĩa quái mới (tên tạm, product owner đổi sau) tạo bằng data; đạn đi qua đường damage chung (Safe Zone chặn damage trước `IDamageable`, `02` §8) và biến mất khi vào Safe Zone. Dodge không có i-frames (`07` §4.4) nên người chơi né đạn bằng cách di chuyển hoặc lướt ra khỏi đường đạn; đạn phải chậm đủ để làm được việc đó trên màn hình điện thoại.
- Cân bằng theo `07` §5 (Ranged/Skirmisher hạ trong 8–14 giây ở level đề xuất); số đặt trong ScriptableObject; cập nhật bảng và test cân bằng.
- Ràng buộc: role chọn hành vi qua cấu trúc nhỏ (không nhồi vào `EnemyController`); không `if floor == N`; pool cho đạn; không asmdef mới; tên và art không lấy từ tác phẩm có bản quyền.
- Không làm: quái theo pha giờ (FB-031), AI nhóm, boss mới.
- Done: Vùng Farm Tầng 1–2 có ít nhất ba hành vi nhìn thấy khác nhau; đạn né được bằng di chuyển hoặc Dodge; số cân bằng khớp `07` §5; mọi case P9.5–P9.7 đã Pass vẫn Pass.
- Evidence (Task P9.8). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Ba hành vi khác nhau | Android, Guest | Đi qua Vùng Farm, quan sát quái cận chiến, quái áp sát rồi lùi, quái bắn xa | Nhận ra được ba hành vi khác nhau mà không cần đọc tài liệu |  |  |  |
| (b) Đạn né được | Android, Guest | Bị quái Ranged ngắm; sau khi thấy báo đòn thì di chuyển hoặc bấm Dodge ra khỏi đường đạn; lần khác đứng yên | Có báo đòn rõ trước khi bắn; ra khỏi đường đạn kịp thì không mất máu; đứng yên thì mất máu đúng giá trị; Dodge không miễn sát thương nếu vẫn nằm trong đường đạn |  |  |  |
| (c) Đạn và Safe Zone | Android, Guest | Đứng ngay mép Safe Zone khi quái Ranged bắn | Không mất máu trong Safe Zone; đạn biến mất, không kẹt lại |  |  |  |
| (d) Thời gian hạ quái | Android, Guest, level đề xuất | Bấm giờ hạ Ranged và Skirmisher | Mỗi con 8–14 giây; số đo thật thắng ước lượng |  |  |  |
| (e) Hồi sinh và pool | Android, Guest | Hạ quái các role, chờ hồi sinh, đổi tầng qua lại | Quái và đạn dùng lại từ pool đúng; không còn đạn bay sót khi đổi tầng |  |  |  |
| (f) Không hồi quy | Android, Guest | Chạy lại P8, P9, P9.5–P9.7 đã Pass; chạy toàn bộ test EditMode | Vẫn Pass; test xanh |  |  |  |

### Phase 9.9 Hướng dẫn và Mũi tên quest

- Mục tiêu: người chơi mới hiểu cách chơi trong khoảng 3 phút mà không cần đọc tài liệu.
- Nguồn: FB-029, FB-024 trong `10`.
- Phạm vi: gợi ý một lần theo sự kiện bằng "giọng hệ thống" (toast riêng, không đổi chuỗi chữ của bảng báo boss ở P8-5), dùng Quest và NPC sẵn có; cờ đã xem lưu local (PlayerPrefs theo playerId); công tắc "Gợi ý hướng dẫn" trong Settings. Các gợi ý: di chuyển, đánh, né (khi thấy báo đòn đầu tiên), kỹ thuật (khi mở khóa), dùng bình (HP thấp, ngoài giao chiến), Safe Zone và Chọn Tầng, nhặt và mặc đồ.
- Mũi tên chỉ mục tiêu quest ở mép màn hình: Talk trỏ tới NPC theo `npcId`; Reach trỏ tới vùng đích; Kill/Collect trỏ tới tâm Vùng Farm của tầng. Ẩn khi tới nơi.
- Không làm: mini-map, khóa input bắt buộc làm theo, video hướng dẫn, nút Reset Guest hay xóa tài khoản. "Xem lại hướng dẫn" chỉ xóa cờ gợi ý local.
- Cắt từ dưới lên khi thiếu thời gian: mũi tên quest trước, rồi các gợi ý ít quan trọng. Giữ gợi ý di chuyển, đánh, né và bình.
- Done: Guest mới không đọc tài liệu vẫn làm được di chuyển, đánh, né, dùng kỹ thuật, dùng bình, vào Safe Zone trong khoảng 3 phút; mọi case P9.5–P9.8 đã Pass vẫn Pass.
- Evidence (Task P9.9). Cột `Actual`, `Pass`, `Ngày` để trống cho tới khi chạy thật; không tự điền `Pass`.

| Case | Thiết bị/account/network | Bước | Expected | Actual | Pass | Ngày |
|---|---|---|---|---|---|---|
| (a) Người chơi mới | Android, Guest mới | Người chưa từng chơi làm theo gợi ý, không đọc tài liệu | Trong khoảng 3 phút biết di chuyển, đánh, né, dùng kỹ thuật, dùng bình, vào Safe Zone |  |  |  |
| (b) Gợi ý chỉ hiện một lần | Android, Guest mới | Làm đủ rồi đóng và mở lại app | Gợi ý đã hiện không hiện lại; bật "Xem lại hướng dẫn" thì hiện lại |  |  |  |
| (c) Tắt gợi ý | Android, Guest mới | Tắt "Gợi ý hướng dẫn" trong Settings rồi chơi | Không có toast gợi ý nào hiện |  |  |  |
| (d) Mũi tên quest | Android, Guest | Nhận quest cần tới Vùng Farm hoặc nói chuyện với NPC | Mũi tên chỉ đúng hướng; ẩn khi tới nơi |  |  |  |
| (e) Giọng hệ thống không đổi bảng boss | Android, Guest, Wi-Fi | Hạ boss Tầng 1 | Bảng P8-5 giữ nguyên chữ "Máy chủ đã ghi nhận. Tầng kế đã mở."; toast riêng hiện sau bảng, không thay chữ |  |  |  |
| (f) Không hồi quy | Android, Guest | Chạy lại P8, P9, P9.5–P9.8 đã Pass; chạy toàn bộ test EditMode | Vẫn Pass; test xanh |  |  |  |

### Phase 10 Duel Online

- Lobby code, Relay, transport abstraction, Arena side-view, Half-Loss, cleanup, challenge history.
- Done: hai thiết bị vào được một room, thấy state đồng bộ, HP dưới 50 phần trăm kết thúc, disconnect không kẹt UI.
- Làm sau Phase 9.9 theo thứ tự product owner chọn (2026-10-14). `01` và `12` §1 vẫn liệt kê Duel trong Mốc 1; nếu product owner chuyển Duel sang Mốc 2 thì phải cập nhật hai tài liệu đó và `13` qua ADR. Art Track lớp A2 chạy song song với phase này.
- Các interface `ILobbyService`, `IRelayService`, `INetworkTransport` (`04` §2.2) không hard-code số người chơi bằng 2; Duel vẫn chỉ 2 người. Việc này giữ đường cho Phòng Boss hợp tác ở Mốc 2 (FB-027, cần ADR vì `04` §44–45) mà không phải đổi interface.

### Phase 11 Mốc 1 Regression

- Android performance, offline/error states, tests, evidence table, documentation alignment.
- Done: Floor 1 loop, world time, death, Floor 2 data-only và Duel đều pass theo device evidence.
- Regression cho Phase 9.5–9.9 (cảm giác, UX, vùng, role quái, hướng dẫn) và Art Track lớp A3. Phase này chỉ kiểm tra và sửa lỗi, không thêm tính năng (luật 8).

### Art Track — chạy song song, không phải phase — ĐỀ XUẤT

Đồ họa và âm thanh làm theo lớp, song song với các phase; không dồn đến sau Phase 11. Code không chờ art: greybox/placeholder hiện tại giữ nguyên cho tới khi asset sẵn sàng, rồi thay bằng data (sprite, animator, prefab variant), không sửa logic.

| Lớp | Chạy song song với | Phạm vi | Cổng qua |
|---|---|---|---|
| A0 Art Contract | Trước khi vẽ hoặc nhập asset | Palette (`11` §5), PPU và kích thước sprite, pivot, bộ animation chuẩn (idle, walk, attack, hit, die), sorting layers, màu telegraph đã dành (vàng/cam đòn thường, tím Pressure, hồng enrage, xanh nhạt punish, đỏ nhạt báo đòn quái), thư mục `Assets/_Game/Art` và `Audio`, file `ASSET_SOURCES.md` ghi nguồn và giấy phép | Một trang contract; một asset mẫu nhập đúng contract chạy được trong game |
| A1 | Phase 9.5–9.9 (tileset sau 9.7a) | Tileset Floor 1–2 theo các vùng, nhân vật người chơi, quái đang có và quái mới của 9.8, boss Tầng 1, icon item chính, UI skin tối thiểu, nhạc và SFX thay placeholder | Không thiếu sprite/animator; FPS đo thật không tụt dưới ngưỡng product owner đặt ở P9.5 (f) |
| A2 | Phase 10 | Arena Duel (side-view), UI Duel, hiệu ứng kỹ thuật dùng chung | Như A1, thêm kiểm tra hai thiết bị |
| A3 | Phase 11 | Dọn placeholder còn sót, atlas và nén, kiểm tra máy yếu | Không còn placeholder trong luồng Mốc 1 |
| Mốc 2–3 | Theo Floor mới | Art Floor 3–10, quái mới, boss mới, VFX kỹ thuật | Cùng cổng A1 |

Luật: (1) mọi asset có nguồn và giấy phép hợp lệ (`11` §5); (2) lane art không sửa script, lane code không sửa thư mục Art (trừ wiring); (3) mỗi batch art vào repo sau một smoke test trên máy; (4) không dùng asset, sprite, nhạc, tên hay UI của bất kỳ tác phẩm có bản quyền nào (`11` §7, `12` §3).

## 4. Mốc 2 và Mốc 3

| Mốc | Chỉ làm sau khi Mốc 1 ổn định |
|---|---|
| Mốc 2 | Control/Signature Technique, các Weapon Discipline khác, dungeon đầy đủ (nhiều phòng, cơ cấu, mini-boss), Floor 3–10, PvP bot/notification. Đề xuất, chờ quyết: Phòng Boss hợp tác 2–4 người bằng mã phòng, tái dùng Lobby/Relay/NGO của Phase 10 (FB-027); Bảng Nhổ Neo và hiện diện thế giới (FB-028); nhãn độ hiếm item (FB-025); quái theo pha giờ (FB-031). |
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