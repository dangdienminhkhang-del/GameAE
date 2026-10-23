 # Bộ prompt Phase 9.5 → 9.9 (cho Kiro và AI nối Unity) — bản v2 (2026-10-18)

Mục tiêu của cả bộ: làm cho game có cảm giác **leo tầng** thay vì chỉ "đánh quái trên một bãi". Thứ tự và lý do đã ghi trong `docs/05` (luật 9–10) và `docs/10` (FB-021 → FB-031). Duel (Phase 10) thuộc **Mốc 2** (ADR-028 ở `docs/01` §6), không nằm trong bộ này; thứ tự làm của Mốc 1 là 9.5 → 9.9 → Phase 11, rồi mới tới Phase 10.

## Cách dùng

1. Mỗi gói có **hai** prompt: **Kiro** (code + test + docs) rồi **AI nối Unity** (asset, prefab, scene). Luôn chạy Kiro trước, rồi đưa **báo cáo cuối và Handoff của Kiro** cho AI nối Unity (prompt Unity lấy tên class/field/số từ Handoff đó).
2. Copy NGUYÊN khối trong khung code, không cắt bớt (mỗi khối đã gồm phần chung và hợp đồng của gói).
3. Thứ tự mới (2026-10-18): K1 (✅ code xong) → K1b fix-tint (✅ code có, chỉ chạy bước đối chiếu) → U1 → **K0 → U0** (9.5a, hướng 8 hướng) → **chơi thử trên Android, điền bảng evidence P9.5 (a)–(i) trong docs/05** → K2 → U2 → (evidence P9.6) → K3a → U3a → K3b → U3b → (evidence P9.7) → K4 → U4 → (evidence P9.8) → K5 → U5 → (evidence P9.9) → **Phase 11 (kiểm tổng, kết thúc Mốc 1)**. Phase 10 (Duel) thuộc Mốc 2, làm SAU Phase 11 và không nằm trong bộ này. **Dừng lại ở mỗi mốc nếu có case không Pass**; không chồng phase lên nhau.
4. Trước Phase 9.7: nên chạy nốt evidence thiết bị của Phase 8 và Phase 9 (hiện docs/05 ghi "code và test đã có, evidence chờ điền").
5. Kiro tuyệt đối không tự điền "Pass" hay số đo thật; cột Actual/Pass/Ngày do bạn điền sau khi chơi trên máy thật.
6. Gói âm thanh placeholder đi kèm (file zip riêng): giải nén vào `Assets/_Game/Audio` của dự án TRƯỚC khi chạy U1 (repo hiện đã có 13 SFX + 3 nhạc trong thư mục này) (tên file khớp id nhạc/SFX; `sfx_enemy_shoot` dùng ở P9.8). Đây là âm thanh tổng hợp bằng code, chỉ để thử cảm giác; mình chưa nghe được bằng tai nên bạn nghe thử và chỉnh volume trong AudioCatalog.

## Trạng thái hiện tại (đối chiếu repo 2026-10-18)

| Gói | Code (Kiro) | Nối Unity | Ghi chú |
|---|---|---|---|
| K1 (P9.5) | ✅ có file (chưa xác minh biên dịch và test) | U1: ◐ asset có, scene/prefab chưa xác minh | Phủ evidence P9.5 (a)–(f), (i) |
| K1b (fix-tint, D2) | ✅ code có | không cần sửa prefab (HitFlash dựng lúc chạy) | Chỉ chạy bước đối chiếu |
| K0 (P9.5a) | ❌ chưa làm (chưa có `Core/Facing/`) | U0 chưa làm | Phủ evidence P9.5 (g), (h) |
| K2 → K5 | ❌ chưa làm | chưa làm | Theo thứ tự ở "Cách dùng" |

## Phụ thuộc

| Gói | Cần xong trước | Ghi chú |
|---|---|---|
| P9.5 (K1, U1) | Phase 9 | Dựng `PlaySpeedState` (một điểm duy nhất ghi `Time.timeScale`) và dịch vụ âm thanh. K1 đã xong code |
| P9.5-fix-tint (K1b) | K1 | Áp dụng ADR-029: nháy là lớp phủ `HitFlash` riêng, ưu tiên màu Chết > state > gốc. Chạy trước U1 |
| P9.5a (K0, U0) | K1, K1b, U1 | Hướng nhìn 8 hướng (`Core/Facing`); sửa PlayerController, PlayerAttack, DodgeController, TechniqueController. Phải xong và qua evidence P9.5 (g), (h) trước P9.6 |
| P9.6 | P9.5, P9.5a | Pause dùng `PlaySpeedState`; Settings dùng setter âm lượng; dead zone phải thống nhất với `FacingTracker` (xem K2 mục 3b) |
| P9.7a | P9.5, P9.6 | Cần nhạc theo vùng, `PlaySpeedState`; thêm `OnZoneEntered` vào `GameEvents` |
| P9.7b | P9.7a | Boss kích hoạt theo phòng |
| P9.8 | P9.5, P9.7a | Cần `SpriteTint`/`HitFlash` (K1b) và `EnemyLeashRules`; Skirmisher ở Tầng 2 (ADR-030) |
| P9.9 | P9.5, P9.6, P9.7a | Cần Settings, vùng và event `WindupStarted`. Mũi tên quest (Phần B) cắt trước nếu thiếu thời gian |
| Phase 11 | P9.5 → P9.9 | Kiểm tổng, chỉ sửa lỗi; kết thúc Mốc 1. Duel (Phase 10, Mốc 2) làm sau |

## Ràng buộc bản quyền (cứng)

Mọi tên, ID, thoại, art, âm thanh, UI trong bộ này do dự án tự đặt. Các prompt **không** nhắc tên, nhân vật, kỹ năng, vũ khí hay boss của tác phẩm có bản quyền nào, và đã dặn cả Kiro lẫn AI nối Unity không được dùng (AGENTS.md luật 7). Tên quái "Ống Phun" và "Dây Rình" chỉ là tên tạm.

## Các điểm cần bạn quyết hoặc biết

- **Đã chốt 2026-10-18 (không còn chờ):** Duel sang Mốc 2 (ADR-028); nháy khi trúng là lớp phủ trắng riêng, ưu tiên màu Chết > state > gốc (ADR-029); Skirmisher ở Tầng 2, Ranged ở sâu Vùng Farm Tầng 1 và Tầng 2 (ADR-030). `AGENTS.md`, `docs/01`, `docs/05`, `docs/12` §1, `docs/13`, `docs/14`, `docs/HANDOFF.md` đã cập nhật theo.
- **Hai ngưỡng chết chồng nhau:** dead zone joystick (P9.6) và ngưỡng chết của `FacingTracker` (P9.5a). K0 ghi giá trị và nơi cấu hình vào Handoff; K2 (mục 3b) quyết một nguồn duy nhất.
- **Chia evidence P9.5:** K1 phủ (a)–(f) và (i); K0 phủ (g) và (h). Điền bảng sau khi xong K0/U0.
- **Lệch tài liệu còn lại (Integrator xử lý, Kiro không tự sửa):** `docs/05` mục 9.5 và `docs/13` "Ghi chú 2026-10-18" còn nói `TintResolver` chưa sửa theo ADR-029 và bảng 9.5b còn nói "thêm child HitFlash vào prefab", trong khi code đã sửa và child được dựng lúc chạy.
- `OnZoneEntered` mang thêm `DisplayName` so với `docs/12` §11; Handoff của K3a sẽ báo để Integrator cập nhật `12`.
- Bình trong giao chiến: bình chỉ dùng ngoài giao chiến (theo `07` §2). Nếu bạn muốn đổi thì cần quyết định riêng ghi vào `10`.
- Boss: mặc định KHÔNG khóa cửa phòng; rời phòng thì boss về nhà **giữ nguyên máu và trạng thái enrage**. Pause: "Về Main Menu" bị khóa khi đang giao chiến (không cho Pause thành nút thoát hiểm). Cả hai bạn đổi được bằng quyết định ghi vào `10`.
- Cần xác minh trên máy thật: nút Back của Android có thật sự thành `Escape` trong Input System không; quyền VIBRATE có nằm trong bản build không.

---

## Gói P9.5 — Cảm giác chiến đấu và Âm thanh

### K1 — Prompt cho Kiro — ✅ ĐÃ LÀM XONG CODE (không chạy lại)

> Code ĐÃ LÀM XONG (đối chiếu repo 2026-10-18: file có thật; CHƯA xác minh được biên dịch Unity và kết quả test vì không chạy Unity). Đừng chạy lại K1. File thật trong `Assets/_Game/Scripts/`:
> - `Core/Presentation/`: PlaySpeedState, HitStopRules, CombatFeelConfig, TintResolver, DamageNumberRules, HealthBarRules
> - `Core/Audio/`: IAudioService, AudioIds, AudioVolumes, SfxThrottleRules, MusicRules
> - `Services/Audio/`: AudioService, AudioCatalog, AudioServiceLocator, AudioVolumeStore
> - `PvE/Presentation/`: PlaySpeedTicker, HitStopController, CombatSfxRouter, CombatMusicRouter, PlayerHitFeedback, EnemyHealthBar, SpriteTint, TintKind, HitFlashOverlay, HitFlashRules (hai file cuối và TintKind thuộc K1b)
> - `UI/Combat/`: DamageNumberPresenter · `UI/HUD/`: BossHealthBar · `UI/Audio/`: UiSfxRouter, UiSfxRules
> - Sửa có giới hạn: PvE/Enemy/EnemyController.cs, PvE/Boss/BossController.cs, PvE/Player/PlayerAttack.cs (event AttackPerformed), Services/ServiceProvider.cs
> - Test: Tests/EditMode/Presentation/ (DamageNumberRulesTests, EnemyPresentationTests, HitFlashRulesTests, HitStopRulesTests, PlaySpeedStateTests, SpriteTintTests, TimeScaleWriteScanTests, TintAndHealthBarRulesTests) và Tests/EditMode/Audio/ (AudioCatalogAndUiTests, AudioCoreTests)
> Phạm vi evidence của K1: bảng P9.5 case (a)–(f) và (i). Case (g), (h) thuộc gói 9.5a (K0). Phần "nháy khi trúng" và thứ tự ưu tiên màu trong prompt bên dưới đã bị THAY THẾ bởi ADR-029 (xem K1b).

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.5. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.5 Cảm giác chiến đấu và Âm thanh) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.5 — Cảm giác chiến đấu và Âm thanh ===

MỤC TIÊU (người chơi cảm nhận được): đánh quái phải nhìn thấy, nghe thấy và "đã tay". Hiện tại đòn trúng chưa có số sát thương, chưa nháy, chưa có tiếng, chưa có nhạc nền, quái chết biến mất đột ngột, quái và boss chưa có thanh máu.
Nguồn: docs/05 Phase 9.5; FB-022, FB-023 trong docs/10; docs/11 §5 (audio: nước, kim loại, dây căng, gió; mọi asset phải có nguồn hợp lệ).

ĐỌC CODE
PvE/Enemy/EnemyController.cs · PvE/Boss/BossController.cs · PvE/Player/HealthComponent.cs · PvE/Player/PlayerAttack.cs · PvE/Skills/TechniqueController.cs · PvE/Dodge/DodgeController.cs · Core/Events/GameEvents.cs · Core/Combat/AttackExecutor.cs (nơi duy nhất phát OnHitResolved) · UI/HUD/PlayerHUD.cs (mẫu lọc OnHealthChanged theo healthSource) · Services/ServiceProvider.cs (mẫu tạo service) · PvE/SafeZone/SafeZoneProvider.cs (mẫu reset static) · thư mục Assets/_Game/Tests (xem asmdef và cách đặt tên test hiện có).

SỰ THẬT ĐÃ XÁC MINH TRONG CODE (đừng đoán khác)
- GameEvents.OnHitResolved chỉ phát cho hit THẬT của NGƯỜI CHƠI (args.Source là CharacterStats của người chơi, args.Target là IDamageable, args.Damage là DamageInfo). Quái đánh người chơi KHÔNG đi qua event này; dùng OnHealthChanged, lọc theo sender == HealthComponent của người chơi (xem PlayerHUD).
- OnEnemyDefeated chỉ mang sender (EnemyController hoặc BossController), không có args.
- EnemyController đặt màu: telegraph (1, 0.55, 0.55), chết (0.4, 0.4, 0.4, 0.9), mặc định trắng; ResetForSpawn đặt lại trắng (quái dùng pool). TakeDamage gọi EndWindup (xóa telegraph) rồi vào HurtState hoặc DeadState.
- BossController đặt màu trực tiếp ở nhiều state: Telegraph (1, 0.8, 0.2), Punish và Recovery (0.7, 0.7, 1), Pressure telegraph và dash (0.75, 0.2, 1), Enrage (1, 0.5, 0.5), chết (0.3, 0.3, 0.3, 0.8). Boss KHÔNG bị tắt khi chết (reset trận khi vào lại tầng).
- Mọi timer của quái và boss dùng Time.deltaTime (có scale) nên dừng hình làm chúng chậm lại theo — đó là chủ ý.
- PlayerAttack chưa có event báo "vừa vung đòn".
- Không có Dodge i-frames (docs/07 §4.4). Dodge và Technique không thuộc phạm vi sửa của package này.

PATH ĐƯỢC SỞ HỮU
Tạo mới:
- Core/Presentation/** (PlaySpeedState, HitStopRules, CombatFeelConfig)
- Core/Audio/** (IAudioService, AudioVolumes, SfxThrottleRules, AudioIds)
- Services/Audio/** (AudioService, AudioCatalog, PlayerPrefs store cho âm lượng)
- PvE/Presentation/** (PlaySpeedTicker, CombatSfxRouter, EnemyHealthBar, các helper hiệu ứng)
- UI/Combat/** (DamageNumberPresenter) · UI/Audio/** (UiSfxRouter) · UI/HUD/BossHealthBar.cs
- Tests/EditMode: thư mục con mới cho Presentation và Audio, đặt theo cấu trúc test hiện có
- Assets/_Game/Audio/ASSET_SOURCES.md (file text; nếu đã tồn tại thì chỉ thêm dòng)
Sửa có giới hạn (chỉ đúng phần nêu):
- PvE/Enemy/EnemyController.cs: bộ giải màu (flash không phá telegraph), tan khi chết, event HealthChanged, event WindupStarted (static hoặc instance theo mẫu code)
- PvE/Boss/BossController.cs: bộ giải màu + flash, event HealthChanged, event EngagedChanged. KHÔNG đổi luật Phase 8, không đụng luồng báo server.
- PvE/Player/PlayerAttack.cs: chỉ thêm một event C# nhỏ báo vừa vung đòn
- Services/ServiceProvider.cs: chỉ thêm factory/getter cho audio theo mẫu hiện có
- Core/Events/GameEvents.cs: KHÔNG sửa. Cần event mới thì dùng C# event trên chính class PvE.
- docs/05 mục Phase 9.5 (chỉ ghi chú kỹ thuật, KHÔNG điền evidence) và docs/06 (cây thư mục, nếu có liệt kê)
Chỉ đọc: mọi thứ còn lại.

PHẢI LÀM
1. PlaySpeedState (Core/Presentation): MỘT điểm duy nhất ghi Time.timeScale. API gợi ý: PushPause/PopPause (đếm lồng nhau), RequestHitStop(seconds, scale), Tick(unscaledDelta), IsPaused, ForceReset. Quy tắc: đang pause → 0; ngoài pause mà đang dừng hình → scale (0.05); còn lại → 1. Không có chỗ nào khác trong code được gán Time.timeScale (thêm test quét mã nguồn chặn điều này, theo mẫu FloorBranchingScanTests). PlaySpeedTicker (PvE/Presentation) gọi Tick(Time.unscaledDeltaTime) mỗi Update và gọi ForceReset khi bị tắt hoặc hủy, để timeScale không kẹt khi đổi scene.
2. HitStopRules (pure) + CombatFeelConfig (ScriptableObject, mọi số nằm ở đây): thời lượng dừng hình thường 0.04s, đòn kết liễu 0.06s, đánh boss 0.05s, trần tuyệt đối 0.06s (docs/05), khoảng cách tối thiểu giữa hai lần dừng 0.15s, scale 0.05. Chỉ dừng hình cho hit của người chơi (OnHitResolved). Không dừng hình khi đang pause, khi người chơi đã chết, hay khi bị đánh. Dùng thời gian thực (unscaled) để tính hết dừng hình.
3. Số sát thương nổi: UI/Combat/DamageNumberPresenter. Pool cố định 12 phần tử, Canvas Screen Space Overlay, đổi vị trí bằng Camera.WorldToScreenPoint. Nguồn: OnHitResolved (số người chơi gây ra, màu sáng) và OnHealthChanged của người chơi với Delta < 0 (số nhận vào, màu đỏ). Vị trí lấy từ Target ép về Component; nếu không ép được thì bỏ qua và log đúng một lần. Số bay lên khoảng 0.6s rồi mờ, tính bằng thời gian unscaled. Không cấp phát mỗi hit: cache sẵn chuỗi số nguyên. Luật thuần (DamageNumberRules): làm tròn về nguyên, tối thiểu 1, chọn loại màu, dời vị trí khi nhiều số trùng điểm.
4. [ĐÃ THAY THẾ bởi K1b / ADR-029 — đừng làm theo thứ tự ưu tiên ở đây] 4. Nháy khi trúng (quái, boss, người chơi): flash ngắn 0.08s, có thể kèm scale punch 1 → 1.08 → 1 (nếu làm thì ResetForSpawn phải trả scale về đúng). Flash KHÔNG được xóa, đổi hay làm "kẹt" các màu telegraph/trạng thái liệt kê ở trên: dùng một bộ giải màu với thứ tự ưu tiên chết > flash > tint của state hiện tại > mặc định, và khôi phục đúng tint sau flash. Pure test cho bộ giải màu. Hiệu ứng hit của người chơi nằm trong PvE/Presentation (nghe OnHealthChanged đã lọc), không sửa PlayerController. (Quyết định hiện hành: thứ tự là chết > màu state hiện tại > gốc; nháy là lớp phủ trắng riêng "HitFlash", alpha 0.85 → 0 trong 0.08 s, tối đa 0.5 khi có báo đòn nguy hiểm.)
5. Quái tan khi chết: trong deathSeconds, alpha từ 0.9 về 0 và scale về khoảng 0.85, xong mới trả pool. ResetForSpawn phải khôi phục màu, alpha, scale, timer flash (quái dùng lại từ pool phải trông như mới). Boss không bị tan (giữ hành vi chết hiện tại).
6. Thanh máu: (a) EnemyHealthBar tự dựng bằng SpriteRenderer con trong Awake (không cần sprite asset: dùng một sprite trắng 1×1 tạo một lần và dùng chung); chỉ hiện sau khi bị trúng, ẩn sau 3 giây không bị đánh hoặc khi chết; cập nhật bằng event HealthChanged, KHÔNG polling Update. (b) UI/HUD/BossHealthBar: hiện khi BossController.EngagedChanged(true) (boss rời trạng thái Idle), ẩn khi false hoặc boss chết; cập nhật bằng HealthChanged. Pure: tỉ lệ máu kẹp 0..1, quy tắc hiện/ẩn.
7. Âm thanh:
   - Core/Audio: IAudioService với PlaySfx(string id, float volumeScale = 1), PlayMusic(string id, float fadeSeconds), StopMusic(float fadeSeconds), MusicVolume và SfxVolume (0..1). AudioIds: hằng chuỗi id chung (không tên content cụ thể): sfx.player_swing, sfx.hit_enemy, sfx.hit_player, sfx.enemy_die, sfx.technique, sfx.dodge, sfx.pickup, sfx.level_up, sfx.ui_click, sfx.ui_back, sfx.potion, sfx.boss_telegraph, music.gameplay, music.boss, music.town. Thêm id mới thì ghi vào Handoff.
   - Services/Audio: AudioService (MonoBehaviour) có pool AudioSource cho SFX (8 voice) và 2 AudioSource cho nhạc để crossfade; AudioCatalog (ScriptableObject) ánh xạ id → clip(s), volume, pitch jitter, khoảng cách tối thiểu giữa hai lần phát; id thiếu thì log một lần và im lặng, không crash. Ra nền (OnApplicationFocus false) thì tạm dừng audio, vào lại thì tiếp tục. Âm lượng nhạc/SFX lưu local bằng PlayerPrefs (khóa settings.audio.music, settings.audio.sfx; mặc định 0.7 và 0.9), không đưa vào Player Data, không có UI ở package này (Settings làm ở P9.6; chỉ cần setter có hiệu lực ngay).
   - Pure test: AudioVolumes (kẹp 0..1, mặc định), SfxThrottleRules (khoảng cách tối thiểu, trần số voice).
   - Điểm phát SFX: vung đòn (event mới trên PlayerAttack), trúng quái (OnHitResolved), bị trúng (OnHealthChanged của người chơi), quái chết (OnEnemyDefeated), technique (TechniqueController.OnTechniqueStarted), né (event sẵn có của DodgeController nếu có; nếu không thì ghi vào Handoff, đừng sửa DodgeController), nhặt đồ (OnItemAcquiredWithPayload), lên cấp (OnLevelUp). UI click: UiSfxRouter gắn ở gốc mỗi Canvas, runtime thêm listener vào các Button con khi bật (không sửa prefab); nút sinh động về sau chưa được phủ — ghi vào Known issues.
   - Nhạc: music.gameplay khi vào scene PvE; music.boss khi boss vào trận (EngagedChanged true), quay lại music.gameplay khi false hoặc boss chết; crossfade 1 giây. Nhạc theo vùng làm ở P9.7a.
   - Không dùng mixer phức tạp, không âm 3D. ASSET_SOURCES.md ghi nguồn và giấy phép từng file; gói placeholder do product owner cung cấp (file WAV/OGG đặt tên khớp id, ví dụ sfx_player_swing.wav, music_gameplay.ogg). Thiếu file nào thì game vẫn chạy im lặng ở chỗ đó. TUYỆT ĐỐI không tải hay nhúng âm thanh không rõ giấy phép.

KHÔNG LÀM
Hệ chí mạng mới; particle nặng; âm thanh 3D; AudioMixer; hit-stop cho đòn người chơi nhận; sửa Dodge/Technique/PlayerController; đổi luật boss; thêm event vào GameEvents; Settings UI (P9.6); nhạc theo vùng (P9.7a).

TEST BẮT BUỘC (EditMode)
HitStopRulesTests (trần 0.06s; khoảng cách tối thiểu; không dừng khi pause/đã chết/không phải hit người chơi; kết liễu và boss dài hơn thường nhưng không vượt trần) · PlaySpeedStateTests (pause thắng dừng hình; pause lồng nhau; dừng hình hết theo unscaled delta; ForceReset trả về 1; không bao giờ kẹt ≠ 1 khi mọi yêu cầu đã hủy) · quét mã chặn gán Time.timeScale ngoài PlaySpeedState · DamageNumberRulesTests · [test màu đã đổi theo K1b/ADR-029] TintResolverTests (chết thắng màu state; màu state trả nguyên vẹn; nháy là lớp phủ riêng nên không đi qua đây) · EnemyController: ResetForSpawn khôi phục màu, alpha, scale (dựng GameObject trong EditMode như các test hiện có) · HealthBarRulesTests · AudioVolumesTests · SfxThrottleRulesTests.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.5 trong docs/05: K1 chỉ phủ case (a)–(f) và (i). Case (g) và (h) thuộc gói 9.5a (K0). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````

### K1b — Prompt cho Kiro: P9.5-fix-tint (áp dụng D2/ADR-029 vào code K1) — ✅ code đã có, chạy để kiểm

> Làm giữa K1 và U1. Repo hiện đã có HitFlashOverlay, HitFlashRules, TintKind, SpriteTint, TintResolver mới và test tương ứng, nên prompt này bắt đầu bằng "Bước 0: đối chiếu" và chỉ sửa điểm nào còn thiếu. Nếu bạn đã chạy Prompt B trước đó thì chỉ cần kết quả đối chiếu.

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.5-fix-tint. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/01 §6 (ADR-029) → docs/05 (mục Phase 9.5, ghi chú kỹ thuật) → docs/13 (gói 9.5b, "Ghi chú 2026-10-18") → docs/14 §2 và §7 H2 → docs/12 §3 → docs/06. Sau đó đọc code thật trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và áp dụng quyết định ADR-029 (D2) lên code của K1. Nó có hiệu lực thay cho gói 9.5b trong docs/13 ở phần màu/nháy (K1 đã gộp 9.5b + 9.5c).

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.5-fix-tint — Áp dụng ADR-029 (D2) vào code K1 ===

TRẠNG THÁI ĐÃ THẤY TRONG REPO (đối chiếu 2026-10-18, chưa chạy Unity nên chưa biết test xanh hay đỏ): code của gói này ĐÃ CÓ — Core/Presentation/TintResolver.cs (chỉ còn hai lớp: chết > màu state), PvE/Presentation/{HitFlashOverlay, HitFlashRules, TintKind, SpriteTint}.cs, các trường flash mới trong CombatFeelConfig, EnemyController và BossController đã gọi SpriteTint.SetStateTint(color, TintKind) / Flash() / SetDead(), và test SpriteTintTests, HitFlashRulesTests, TintAndHealthBarRulesTests, EnemyPresentationTests.
=> BƯỚC 0 (bắt buộc làm trước): đối chiếu từng điểm trong "PHẢI LÀM" bên dưới với code thật. Nếu đã đủ hết và toàn bộ test EditMode xanh thì CHỈ báo "đã có", ghi bảng đối chiếu (điểm → file/dòng → đạt hay không) vào báo cáo và DỪNG. Chỉ sửa đúng điểm nào còn thiếu hoặc sai.

MỤC TIÊU: báo đòn là thông tin sống còn vì permadeath; nháy khi trúng không được che màu báo đòn. Màu nhân không tạo được màu trắng nên nháy phải là lớp phủ riêng.
Nguồn: ADR-029 ở docs/01 §6; docs/14 §2 và §7 H2; docs/05 mục 9.5.

ĐỌC CODE
Core/Presentation/TintResolver.cs · Core/Presentation/CombatFeelConfig.cs · PvE/Presentation/SpriteTint.cs · HitFlashOverlay.cs · HitFlashRules.cs · TintKind.cs · PvE/Enemy/EnemyController.cs · PvE/Boss/BossController.cs · PvE/Presentation/PlayerHitFeedback.cs · Tests/EditMode/Presentation/*.

PATH ĐƯỢC SỞ HỮU
Sửa có giới hạn, chỉ phần màu/nháy: Core/Presentation/TintResolver.cs, Core/Presentation/CombatFeelConfig.cs (chỉ trường nháy), PvE/Presentation/{SpriteTint, HitFlashOverlay, HitFlashRules, TintKind}.cs, PvE/Enemy/EnemyController.cs và PvE/Boss/BossController.cs (chỉ những dòng ghi màu sprite hoặc gọi nháy), Tests/EditMode/Presentation/*, docs/05 mục Phase 9.5 (chỉ ghi chú kỹ thuật, KHÔNG điền evidence).
Chỉ đọc: mọi thứ còn lại. Không sửa prefab/scene/asset.
LƯU Ý CHỒNG SỬA: EnemyController và BossController đã bị sửa bởi K1; về sau còn bị sửa bởi 9.7a, 9.7b, 9.8. Đọc bản hiện tại, sửa tối thiểu, KHÔNG viết lại hay ghi đè phần của gói khác.

PHẢI LÀM (đối chiếu từng điểm)
1. Màu nhân của sprite chỉ có hai lớp ưu tiên: Chết > màu theo state hiện tại > gốc. Màu state: báo đòn quái hồng nhạt (1, 0.55, 0.55); boss Telegraph vàng/cam (1, 0.8, 0.2), PressureWindup và Dash tím (0.75, 0.2, 1), Punish và Recovery xanh nhạt (0.7, 0.7, 1); Cuồng nộ (1, 0.5, 0.5). Giá trị màu giữ NGUYÊN theo docs/14 §2.
2. Nháy khi trúng KHÔNG phải màu nhân. Nó là lớp phủ trắng riêng: child SpriteRenderer tên "HitFlash" (tự dựng lúc chạy, không sửa prefab), dùng material Unlit để ánh sáng ngày/đêm không làm tối, sắp xếp ngay trên sprite chính, chép sprite/flip từ sprite chính. Alpha 0.85 → 0 tuyến tính trong 0.08 giây THỜI GIAN THỰC (không kéo dài khi dừng hình). Khi đang có báo đòn nguy hiểm (quái windup, boss Telegraph, boss Pressure) thì alpha tối đa 0.5 để màu báo đòn vẫn đọc được. Mọi số nằm trong CombatFeelConfig (flashSeconds, flashColor, flashStartAlpha, flashMaxAlphaUnderDanger).
3. SpriteTint là điểm DUY NHẤT ghi spriteRenderer.color của quái, boss và người chơi (tìm trong Assets/_Game/Scripts để chắc không còn chỗ khác ghi trực tiếp). Mỗi state truyền kèm TintKind (Base, DangerTelegraph, Window, Enrage); chỉ DangerTelegraph làm lớp phủ nhạt đi. Đổi state giữa lúc đang nháy thì alpha được kẹp lại ngay.
4. Chết: lớp phủ nháy tắt hẳn; quái tan (alpha 0.9 → 0, scale về khoảng 0.85) như K1; boss giữ hành vi chết cũ.
5. Quái dùng pool: ResetForSpawn và OnDisable trả lớp phủ về ẩn hoàn toàn (alpha 0, hết nháy, bỏ cờ báo đòn), màu, alpha, scale về mặc định.
6. Không đổi luật Phase 8, luồng báo server của boss, hay giá trị màu báo đòn.

KHÔNG LÀM
Đổi giá trị màu báo đòn; thêm hệ chí mạng; particle nặng; Instantiate/Destroy trong lúc chiến đấu (child HitFlash dựng một lần ở Awake); sửa prefab/scene; sửa luồng chết hay báo server của boss; đụng phần 9.5a (Facing).

TEST BẮT BUỘC (EditMode; đã có thì chỉ cần chứng minh còn phủ)
TintResolver: mọi tổ hợp cho đúng thứ tự Chết > màu state > gốc; màu state trả nguyên vẹn kể cả alpha · HitFlashRules: alpha 0.85 → 0 trong 0.08 giây; trần 0.5 khi báo đòn nguy hiểm; không âm hoặc NaN; đổi state giữa chừng thì kẹp ngay · SpriteTint: nháy KHÔNG làm đổi màu state; chết xóa nháy; ResetAll trả sạch · EnemyController: ResetForSpawn khôi phục màu, alpha, scale và ẩn lớp phủ. Chạy TOÀN BỘ test EditMode.

GHI VÀO HANDOFF
(a) Bảng đối chiếu bước 0. (b) Lệch tài liệu cần Integrator xử lý: docs/05 mục 9.5 và docs/13 "Ghi chú 2026-10-18" còn viết TintResolver chưa sửa theo ADR-029 và bảng 9.5b còn nói "thêm child HitFlash vào prefab", trong khi code đã sửa và child được dựng lúc chạy (không cần sửa prefab). (c) Việc Unity: KHÔNG cần thêm child HitFlash vào prefab; chỉ kiểm CombatFeelConfig.asset có đủ trường nháy mới với giá trị 0.08 / trắng / 0.85 / 0.5.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.5 case (a) (số, nháy, telegraph vẫn đúng màu). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````


### U1 — Prompt cho AI nối Unity — ◐ CHƯA XÁC MINH XONG

> Đã thấy trong repo: `Assets/_Game/Audio/` có 13 SFX + 3 nhạc placeholder, `AudioCatalog.asset`, `ASSET_SOURCES.md`, và `Assets/_Game/Data/Presentation/CombatFeelConfig.asset`. Việc nối scene và prefab (AudioService, PlaySpeedTicker, DamageNumberPresenter, BossHealthBar, EnemyHealthBar trên Enemy.prefab, UiSfxRouter...) chưa xác minh được từ file. Chạy prompt này SAU K1 và K1b; bước nào đã có thì ghi "đã có" thay vì làm lại. Ngoài ra kiểm CombatFeelConfig.asset có đủ trường nháy mới (flashColor, flashStartAlpha 0.85, flashMaxAlphaUnderDanger 0.5) và `AudioCatalog.asset` (HANDOFF ghi volume đang 0.8 đồng loạt, khác bảng giá trị bên dưới).

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.5, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.5 (Cảm giác chiến đấu và Âm thanh) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.5, Console sạch, toàn bộ test EditMode xanh. Nếu Handoff của Kiro chưa có mục "Việc cần làm trong Unity Editor" thì dừng và báo.
Nguồn tên class/field/đường dẫn: Handoff của Kiro. Nếu tên thật khác với tên gọi dưới đây thì làm theo tên thật và ghi lại.

VIỆC CẦN LÀM
1. Âm thanh nhập vào Unity (đường dẫn thật theo Handoff; gợi ý Assets/_Game/Audio/Sfx và Assets/_Game/Audio/Music).
   - Liệt kê file có thật. Product owner cung cấp WAV/OGG đặt tên khớp id (ví dụ sfx_player_swing.wav, music_gameplay.ogg). File nào thiếu thì để trống ô clip tương ứng — game phải chạy im lặng ở chỗ đó, KHÔNG tự tải hay tự tạo âm thanh.
   - Chỉnh import qua công cụ importer của Unity (không sửa tay .meta), rồi ĐỌC LẠI giá trị sau khi reimport và báo trước/sau:
     SFX: Force To Mono bật; Load Type = Decompress On Load nếu clip dưới khoảng 200 KB chưa nén, ngược lại Compressed In Memory; Android override: Vorbis, quality khoảng 0.6; Sample Rate Setting = Override 22050 Hz cho SFX và UI.
     Nhạc: Force To Mono tắt; Load Type = Streaming; Vorbis quality khoảng 0.5; 44100 Hz; Preload Audio Data tắt; Load In Background bật.
   - Không tạo AudioMixer. Chỉ có đúng MỘT AudioListener trong mỗi scene chạy được (thường trên Main Camera); báo nếu thấy hai.
2. AudioCatalog.asset (đường dẫn theo Handoff; gợi ý Assets/_Game/Data/Audio/AudioCatalog.asset). Tạo một entry cho MỌI id trong AudioIds của Kiro, gán clip nếu có file. Giá trị khởi đầu (product owner chỉnh bằng tai sau): volume 0.6–1.0 (ui_click và ui_back 0.6, hit_player 1.0, các id còn lại 0.7–0.9); pitch jitter ±0.05 cho swing, hit_enemy, enemy_die, dodge, còn lại 0; khoảng cách tối thiểu giữa hai lần phát: swing 0.05 s, hit_enemy 0.04 s, hit_player 0.10 s, enemy_die 0.06 s, dodge 0.10 s, ui_click 0.05 s, boss_telegraph 0.30 s, còn lại 0. Nhạc volume 1.
3. CombatFeelConfig.asset (đường dẫn theo Handoff). Giá trị phải KHỚP mặc định Kiro đã đặt (hit-stop thường 0.04 s, kết liễu 0.06 s, boss 0.05 s, trần 0.06 s, khoảng cách tối thiểu 0.15 s, scale 0.05; flash 0.08 s, flashColor trắng, flashStartAlpha 0.85, flashMaxAlphaUnderDanger 0.5; scale punch 1.08). KHÔNG đổi số; thấy số lệch với docs/05 thì báo.
4. Scene khởi đầu của game (scene nạp đầu, lấy tên từ Build Settings): tạo MỘT đối tượng AudioService (persistent theo Handoff), gán AudioCatalog. Đảm bảo không bị nhân đôi khi quay lại scene này. Kiểm xem vào thẳng scene gameplay trong Editor thì âm thanh có chạy không và báo kết quả (đừng tự sửa code).
5. Scene gameplay (Floor 1 nằm sẵn trong scene này):
   - PlaySpeedTicker (đối tượng riêng, gán theo Handoff); CombatSfxRouter; gán mọi field tham chiếu (người chơi, AudioService nếu cần).
   - DamageNumberPresenter + Canvas riêng: Screen Space Overlay, Canvas Scaler Scale With Screen Size với reference và match GIỐNG Canvas HUD hiện có (đọc từ HUD), sorting order thấp hơn Pause, Death Screen và bảng báo boss. Pool 12 phần tử Text (uGUI Text) theo đúng cấu trúc Kiro mô tả; raycastTarget TẮT trên mọi phần tử.
   - BossHealthBar trên Canvas HUD: ẩn lúc đầu, nằm ở phía trên giữa, không che nút điều khiển; gán BossController của Boss_Floor01 (tham chiếu scene).
   - Enemy.prefab (sửa trên ASSET prefab, không để override ở scene): thêm EnemyHealthBar (thanh tự dựng nên không cần sprite); kiểm các spawner đang dùng đúng prefab này và pool sinh ra quái có thanh máu. Không sửa gì khác trên prefab.
   - UiSfxRouter ở gốc MỖI Canvas có Button (HUD, MainMenu, Inventory, Quest, Chọn Tầng, Death, Login nếu có). Không thêm vào Canvas không có Button.
   - Nhạc: gán music.gameplay cho thành phần Kiro nói sẽ phát nhạc khi vào scene PvE; gán music.boss cho luồng boss vào trận (theo Handoff).
6. Lưu scene và prefab. Ghi lại từng thay đổi (đường dẫn + field + giá trị).

KIỂM NHANH (Play Mode; chụp ảnh Game view; đọc Console)
- Đánh một con quái: hiện số sát thương nổi, quái nháy, có hit-stop ngắn và Time.timeScale trở về 1 sau đó (đọc giá trị bằng eval sau 1 giây). Đánh liên tục không làm timeScale kẹt.
- Khi quái đang báo đòn (hồng nhạt) mà bị trúng: lớp phủ trắng "HitFlash" hiện nhạt (tối đa khoảng 0.5) rồi tắt trong khoảng 0.08 s; màu báo đòn vẫn thấy rõ xuyên qua, và không bị đổi hay kẹt sau khi hết nháy (ADR-029). Child "HitFlash" do code tự dựng lúc chạy: xác nhận nó xuất hiện dưới quái, boss và người chơi trong Play Mode, và KHÔNG thêm nó vào prefab.
- Quái chết thì mờ dần rồi biến mất; chờ hồi sinh và chụp ảnh: quái hồi sinh phải màu, độ mờ, kích thước bình thường (không còn mờ hay nhỏ).
- Thanh máu quái chỉ hiện sau khi bị trúng và ẩn sau khoảng 3 giây; thanh máu boss hiện khi tới gần và đánh nhau, ẩn khi boss chết.
- Thiếu clip: không có lỗi hay spam Console (chỉ tối đa một log mỗi id thiếu).
- Dừng Play Mode: Time.timeScale = 1 (đọc lại), Console sạch, lưu tất cả.
- CHƯA kiểm được (để product owner làm trên Android): FPS khi nhiều quái và số nổi, cảm giác âm lượng, tiếng khi ra nền rồi vào lại, nhạc crossfade khi boss vào trận.

KHÔNG LÀM
Không sửa C#; không tạo AudioMixer; không tự thêm âm thanh; không đổi balance; không đổi Pause/Settings (làm ở P9.6).
````


## Gói P9.5a — Hướng nhìn 8 hướng (làm SAU U1, TRƯỚC P9.6)

> Đánh số K0/U0 vì thuộc 9.5a (docs/13), nhưng CHẠY sau K1/K1b/U1. Trạng thái: CHƯA LÀM (repo chưa có `Core/Facing/`). Làm xong thì chơi thử Android và điền bảng evidence P9.5 (case (g), (h) và chạy lại (a)–(f), (i)) rồi mới sang P9.6.

### K0 — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.5a. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.5, phần phạm vi mục 0 và case (g), (h)) → docs/12 §3, §4, §13 → docs/13 §3, §6–§9 và gói Wave 9.5a → docs/14 §4.1 và §7 H7 → docs/10 (FB-032, FB-033) → docs/01 §6 (ADR-026) → docs/06. Sau đó đọc code thật trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package là docs/13 gói Wave 9.5a; phần bên dưới chép lại từng ý và thêm sự thật đã xác minh trong code. Nếu bản chép và docs/13 lệch nhau thì docs/13 thắng; ghi lệch vào Handoff. ACR (đổi Facing int thành vector) đã được product owner yêu cầu 2026-10-16 và ghi trong docs/13, không cần gửi lại.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.5a — Hướng nhìn 8 hướng ===

MỤC TIÊU (người chơi cảm nhận được): đòn đánh thường, Né và Kỹ thuật đi đúng theo hướng nhân vật đang nhìn ở mọi góc (8 hướng), không chỉ trái hoặc phải. Hiện nay lướt chỉ đi ngang và đòn dễ trượt khi quái đứng ngay trên hoặc dưới. Animator nhận được hướng (FaceX, FaceY) để sau này gắn hình 8 hướng mà không phải sửa code.
Nguồn: docs/05 Phase 9.5 mục 0; FB-032, FB-033 ở docs/10; ADR-026 ở docs/01 §6; docs/14 §4.1 và §7 H7.
Điều kiện và thứ tự: chạy SAU K1, K1b và U1 trong bộ này. K1 đã sửa PlayerAttack (thêm event AttackPerformed); đọc bản hiện tại và KHÔNG ghi đè.

ĐỌC CODE
PvE/Player/PlayerController.cs · PvE/Player/PlayerAttack.cs · PvE/Dodge/DodgeController.cs · PvE/Skills/TechniqueController.cs · Core/Combat/HitDetection.cs · Core/Combat/IDamageable.cs · Core/Combat/AttackDefinition.cs (HitboxOffset, HitboxRadius, TargetLayers) · Core/Skills/WeaponTechniqueDefinition.cs, TechniqueHitWindow.cs, MovementPolicy.cs · Core/Input/IInputReader.cs · PvE/Player/PlayerInputReader.cs · UI/Controls/OnScreenJoystick.cs (chỉ đọc) · Tests/EditMode (xem test nào chạm Facing, hitbox, Dodge, Technique) · prefab Player trong Assets/_Game/Prefabs (CHỈ ĐỌC, để biết attackOrigin có được gán không và Animator Controller đang có tham số nào).

SỰ THẬT ĐÃ XÁC MINH TRONG CODE (đối chiếu 2026-10-18; đừng đoán khác)
- PlayerController.Facing là int, private set, mặc định 1. Trong FixedUpdate: velocity = _currentMove.normalized * moveSpeed; chỉ khi spriteRenderer != null thì _currentMove.x > 0.01 → flipX = false, Facing = 1; _currentMove.x < -0.01 → flipX = true, Facing = -1; x gần 0 thì giữ nguyên (đi thẳng lên hoặc xuống vẫn giữ hướng ngang cũ). animator.SetBool("IsWalking", sqrMagnitude > 0.001) gọi MỖI FixedUpdate và KHÔNG kiểm tham số có tồn tại. _currentMove là vector THÔ từ IInputReader.OnMove (handler HandleMove(object, Vector2)). OnScreenJoystick đưa vào vị trí kéo / handleRange (độ dài ≤ 1), CHƯA có dead zone (dead zone là việc của P9.6). Thả joystick thì vector về (0, 0).
- PlayerAttack.ResolveHitboxCenter: nếu attackOrigin != null thì trả attackOrigin.position (KHÔNG theo Facing); ngược lại new Vector2(pos.x + offset.x * facing, pos.y + offset.y) với offset = attackDefinition.HitboxOffset. TryAttack: kiểm IsReady, kiểm Stats → phát AttackPerformed (event do K1 thêm, giữ nguyên vị trí gọi, trước mọi OnHitResolved) → tính center → _hitDetection.FindTargets(center, HitboxRadius, TargetLayers) → ApplyHit từng mục tiêu → StartCooldown → animator.SetTrigger nếu attackTriggerName không rỗng.
- HitDetection.FindTargets trả IReadOnlyList<IDamageable>, và IDamageable KHÔNG có vị trí (chỉ máu, TakeDamage, Heal, SetMaxHealth). Muốn biết vị trí mục tiêu phải đi qua collider hoặc Component. HitDetection thuộc Core/Combat, KHÔNG nằm trong package này (chỉ đọc).
- DodgeController.TryDodge: facing = playerController.Facing; _dodgeDir = new Vector2(facing, 0); _rb.Cast(_dodgeDir, filter, hits, wantDist) cắt khoảng cách tới vật cản (trừ 0.05); FixedUpdate: _rb.MovePosition(_rb.position + _dodgeDir * step). Dùng UnityEngine.Time.time (có scale). Không i-frames (ADR-025). Log debug in "dir={facing}".
- TechniqueController.ApplyWindowMovementOnce có HAI nhánh: có Rigidbody2D (facing = Facing; dir = new Vector2(facing, 0); rb.Cast rồi rb.MovePosition) và nhánh dự phòng không có Rigidbody2D (pos.x += facing * distance; chỉ dùng trong test EditMode). ResolveHitboxCenter(def) = new Vector2(pos.x + offset.x * facing, pos.y + offset.y). Trigger Animator của Technique qua FireAnimatorTrigger.
- Quái và boss tự quản flipX trong EnemyController và BossController: NGOÀI phạm vi package này.
- Chưa có thư mục Core/Facing trong repo và chưa có tham số FaceX hay FaceY nào trong code.
- Các chỗ khác có thể còn dùng Facing hoặc flipX của người chơi (UI, bootstrap, camera, test): chưa liệt kê được; phải tự tìm (mục 9).

PATH ĐƯỢC SỞ HỮU (khớp docs/13)
Tạo mới: Core/Facing/** gồm Direction8, FacingMath, FacingTracker, HitboxMath, AimAssist; Tests/EditMode/Facing/ (test tương ứng).
Sửa tối thiểu (chỉ những dòng dùng Facing hoặc hướng, cộng phần Animator và autoAim nêu dưới): PvE/Player/PlayerController.cs · PvE/Player/PlayerAttack.cs · PvE/Dodge/DodgeController.cs · PvE/Skills/TechniqueController.cs.
Docs: đoạn mô tả hướng đánh hoặc hướng Né trong docs/07 §4 (nếu có). Không sửa phần khác của docs.
Chỉ đọc: Enemy, Boss, Floor, Quest, Time, Items, Services, UI (kể cả OnScreen*Button), Core/Input, Core/Combat.

PHẢI LÀM
1. Direction8: Đông, Đông-Bắc, Bắc, Tây-Bắc, Tây, Tây-Nam, Nam, Đông-Nam; bước 45°, bắt đầu từ trục +X ngược chiều kim đồng hồ (Đông 0°, Đông-Bắc 45°, Bắc 90°...). FacingMath: FromVector (làm tròn tới hướng gần nhất; vector gần 0 thì trả hướng dự phòng do người gọi truyền vào), ToVector (vector đơn vị), ToArtSlot (trả một trong 5 khe hình S, SE, E, NE, N kèm cờ flipX; ba hướng phía Tây SW, W, NW là bản lật của SE, E, NE — docs/14 §4.1).
2. FacingTracker (thuần C#): giữ vector hướng đã chuẩn hóa, mặc định (1, 0). Chỉ đổi khi độ lớn đầu vào vượt ngưỡng chết; thả joystick (vector gần 0) thì GIỮ hướng cũ. Ngưỡng chết là tham số có mặc định nhỏ (đề xuất 0.1; chỉ lọc nhiễu), KHÔNG phải dead zone joystick. P9.6 sẽ thêm dead zone ở đầu ra JoystickMath; để tránh hai ngưỡng chồng nhau, ghi vào Handoff: giá trị mặc định, nơi cấu hình (field serialize trên PlayerController), và rằng đầu vào của FacingTracker là vector THÔ ở gói này.
3. PlayerController: thêm FacingDirection (Vector2 chuẩn hóa), Facing8 (Direction8), SetFacingDirection(Vector2). Facing (int ±1) và việc lật flipX GIỮ NGUYÊN hành vi hiện tại (để không vỡ chỗ khác).
4. PlayerAttack: tâm vùng đánh = HitboxMath.Center(vị trí, FacingDirection, offset), tức vị trí + FacingDirection * offset.x + (0, offset.y). Khi hướng là (±1, 0) kết quả phải TRÙNG TỪNG CON SỐ với công thức cũ (x + offset.x * facing, y + offset.y). Nhánh attackOrigin != null GIỮ NGUYÊN (không thuộc hợp đồng): nếu prefab Player đang gán attackOrigin thì đòn không theo 8 hướng; hãy ghi điều này vào Handoff (có gán hay không) để bước Unity quyết định.
5. DodgeController: hướng lướt = FacingDirection đã chuẩn hóa thay cho new Vector2(facing, 0); rb.Cast và quãng đường dùng cùng vector đó.
6. TechniqueController: tâm vùng đánh theo HitboxMath; hướng dịch chuyển Step, Dash, Commit theo FacingDirection, cả nhánh có Rigidbody2D lẫn nhánh dự phòng.
7. Animator: PlayerController đặt hai float FaceX (giá trị tuyệt đối, theo bảng 5 khe hình ở docs/14 §4.1: S (0, −1), SE (0.71, −0.71), E (1, 0), NE (0.71, 0.71), N (0, 1)) và FaceY mỗi FixedUpdate, CHỈ KHI Animator có đủ hai tham số: kiểm MỘT lần ở Awake (ví dụ duyệt animator.parameters), không để cảnh báo lặp mỗi khung hình. Quy ước chính xác (0.71 hay 1 cho chéo) do bạn chốt và ghi vào Handoff.
8. Tự nhắm tùy chọn (AimAssist) trong PlayerAttack: trường autoAim (mặc định TẮT) và autoAimRadius (mặc định 2.5). Khi bật, lúc bấm đánh: chọn mục tiêu còn sống GẦN NHẤT trong bán kính (lọc theo TargetLayers), quay FacingDirection về phía nó RỒI mới tính vùng đánh; không có mục tiêu thì dùng hướng đang nhìn. Vì IDamageable không có vị trí, lấy ứng viên qua Physics2D.OverlapCircle theo TargetLayers và Component của collider; dùng buffer tái sử dụng, không cấp phát mỗi đòn; KHÔNG sửa HitDetection. Hai mục tiêu cùng khoảng cách thì chọn theo thứ tự ổn định. AimAssist là luật thuần (nhận danh sách vị trí, trả chỉ số).
9. Tìm trong Assets/_Game/Scripts và Tests mọi chỗ khác còn dùng Facing hoặc flipX của người chơi; ghi vào Handoff chỗ nào để nguyên, chỗ nào đã sửa.

KHÔNG LÀM
Hướng 8 chiều cho quái và boss (làm ở gói hook art, docs/14 §7). Đổi dead zone joystick (thuộc P9.6). Animation event gây sát thương. Sửa prefab hoặc scene (người nối Unity tự thêm FaceX, FaceY vào Animator). asmdef mới. Thêm trường vào AttackDefinition hay WeaponTechniqueDefinition. Đổi ý nghĩa của HitboxOffset.y. Sửa HitDetection hay IDamageable. Đổi luật Dodge (không i-frames), cooldown hay hit policy của Technique.

TEST BẮT BUỘC (EditMode)
Direction8: đủ 8 hướng và các biên 22.5°; vector bằng 0 thì trả hướng dự phòng · ToArtSlot: 8 hướng ra đúng 5 khe và cờ lật · FacingTracker: ngưỡng chết, giữ hướng khi thả, luôn chuẩn hóa · HitboxMath: trùng công thức cũ ở (±1, 0) với vài offset khác nhau; ở 8 hướng độ dài offset.x được giữ nguyên · AimAssist: chọn gần nhất; ngoài bán kính thì không chọn; bỏ qua mục tiêu null hoặc đã chết; hai mục tiêu cùng khoảng cách thì chọn theo thứ tự ổn định · Animator: không phát cảnh báo khi thiếu FaceX/FaceY (nếu test được). Chạy TOÀN BỘ test EditMode (các test Dodge và Technique cũ phải vẫn xanh).

EVIDENCE TRÊN MÁY THẬT
Bảng P9.5 trong docs/05, case (g) (đánh, Né, Technique theo 8 hướng; thả joystick giữ hướng) và (h) (tự nhắm bật/tắt), kèm chạy lại các case P7.5 đã Pass và case (i). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.

GHI VÀO HANDOFF
Danh sách dòng đã đổi (file, dòng, trước/sau) · bước Unity còn lại (thêm tham số Float FaceX, FaceY vào Animator Controller của Player; chưa bắt buộc cho tới khi có hình 8 hướng) · các chỗ còn dùng Facing int · prefab Player có gán attackOrigin hay không · giá trị và nơi cấu hình ngưỡng chết của FacingTracker (cho K2) · quy ước giá trị FaceX/FaceY chéo.
````

### U0 — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.5a, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.5a (Hướng nhìn 8 hướng) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.5a (K0), Console sạch, toàn bộ test EditMode xanh. Nếu Handoff của K0 chưa có mục "Việc cần làm trong Unity Editor" thì dừng và báo. U1 đã xong trước đó.
Nguồn tên class/field/đường dẫn: Handoff của K0. Nếu tên thật khác với tên gọi dưới đây thì làm theo tên thật và ghi lại.

VIỆC CẦN LÀM
1. Animator Controller của Player (đường dẫn lấy từ Handoff hoặc từ component Animator trên prefab Player): thêm HAI tham số kiểu Float tên chính xác FaceX và FaceY. Chỉ thêm tham số; KHÔNG thêm state hay transition, KHÔNG xóa hay đổi tham số có sẵn (đặc biệt IsWalking). Kiểm xem controller có đang dùng chung cho nhiều prefab không; nếu có thì ghi lại.
2. PlayerAttack trên prefab Player: kiểm autoAim đang TẮT và autoAimRadius = 2.5 (mặc định của Kiro). Nếu Handoff nói prefab đang gán attackOrigin: KHÔNG tự đổi; ghi lại tên đối tượng được gán và báo, vì khi đó đòn không đi theo 8 hướng (product owner quyết có để trống ô đó hay không).
3. KHÔNG sửa Animation clip, KHÔNG thêm Sprite 8 hướng (hình thật nhập sau, theo docs/14).

KIỂM NHANH (Play Mode; chụp ảnh Game view; đọc Console)
- Console không có cảnh báo "Parameter 'FaceX' does not exist" hay tương tự, kể cả sau 30 giây chạy.
- Đọc giá trị FaceX, FaceY của Animator khi đẩy joystick (hoặc phím WASD trong Editor) theo 8 hướng: ra đúng quy ước trong Handoff; thả ra thì giữ giá trị cũ.
- Đặt một quái hoặc training target ngay trên, ngay dưới và ở hai chéo; bấm đánh: đòn trúng ở cả bốn vị trí (đọc máu hoặc số sát thương nổi).
- Né theo 8 hướng: đi đúng hướng đang nhìn, không xuyên tường.
- Technique Traverse đi theo hướng nhìn.
- Bật autoAim (CHỈ trong lần thử này): đứng cách quái khoảng 2 ô, bấm đánh mà không hướng joystick về quái, nhân vật quay về quái và đòn trúng. Tắt autoAim lại và làm lại: đòn đi theo hướng đang nhìn. Kết thúc phải để autoAim TẮT.
- Dừng Play Mode: Console sạch, autoAim tắt, Time.timeScale = 1, lưu tất cả.
- CHƯA kiểm được (để product owner làm trên Android): cảm giác điều khiển bằng cảm ứng, case (g) và (h) của bảng P9.5, hồi quy P7.5.

KHÔNG LÀM
Không sửa C#; không đổi balance; không thêm hình; không bật autoAim mặc định; không sửa quái hoặc boss.
````



## Gói P9.6 — Nền UX cảm ứng

### K2 — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.6. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.6 Nền UX) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.6 — Nền UX cảm ứng ===

MỤC TIÊU (người chơi cảm nhận được): chơi liên tục trên điện thoại mà không "chết oan" vì điều khiển. Hiện không có Pause, không có Settings, joystick cố định và không có dead zone, không có nút dùng bình, không rung, vào game không có tiến độ tải.
Nguồn: docs/05 Phase 9.6; FB-001, 002, 005, 006, 009, 010, 011 trong docs/10; docs/07 §2 (bình chỉ dùng ngoài giao chiến); docs/09 §1.2 và §1.3; AGENTS.md luật 10.
Điều kiện: P9.5 (K1, K1b) và P9.5a (K0) đã xong (cần PlaySpeedState, IAudioService và Core/Facing/FacingTracker).

ĐỌC CODE
UI/Controls/OnScreenJoystick.cs · UI/Controls/PvEActionButton.cs · UI/Controls/OnScreenAttackButton.cs · UI/Skills/OnScreenDodgeButton.cs · UI/HUD/PlayerHUD.cs · UI/Inventory/InventoryPanel.cs · UI/Quest/QuestPanel.cs · UI/Floor/FloorSelectPanel.cs · UI/Death/DeathScreenController.cs · UI/Common/SceneLoader.cs · UI/MainMenu/MainMenuController.cs · UI/Boot/BootController.cs · PvE/Items/PlayerInventory.cs (GetConsumableUseState, TryUseConsumable, UseClock) · Core/Items/ConsumableRules.cs (CanUse, DescribeResult, DefaultCombatLockSeconds) · PvE/Bootstrap/PvEBootstrap.cs (AssignInputReader) · Core/Presentation/PlaySpeedState.cs (từ P9.5) · Core/Audio/IAudioService.cs (từ P9.5).
ĐỌC THÊM (từ 9.5a): Core/Facing/FacingTracker.cs · PvE/Player/PlayerController.cs (FacingDirection, ngưỡng chết của FacingTracker) · Handoff của K0.

SỰ THẬT ĐÃ XÁC MINH TRONG CODE
- OnScreenJoystick: một pointer, handleRange 70, KHÔNG có dead zone, đầu ra = vị trí kéo / handleRange kẹp độ dài.
- ConsumableRules.CanUse trả enum {Ok, NotConsumable, OutOfStock, PlayerDead, FullHealth, InCombat, OnCooldown}; DescribeResult trả câu tiếng Việt; thời gian giao chiến mặc định 5 giây. PlayerInventory đánh dấu "đang giao chiến" bằng Time.time (CÓ scale) mỗi khi người chơi trúng quái, hạ quái hoặc bị mất máu; UseClock là public.
- Item bình máu có id item.health_potion (hồi 40, cooldown 5 giây). KHÔNG hard-code id này trong UI: dùng field serialize.
- HP hiện tại ĐƯỢC lưu (ProfileData.currentHp). Vị trí chưa được lưu (FB-017): vào lại game luôn bắt đầu ở điểm vào Tầng 1, trong Safe Zone.
- SceneLoader.LoadSceneAsync chỉ chờ isDone, chưa báo tiến độ.
- Unity Input System (1.20): nút Back của Android được map thành Keyboard.escapeKey; cần xác minh trên máy thật (ghi vào "Cách test").
- Death flow chặn input chiến đấu khi chết (docs/09 §1.3); Pause và Back phải bị vô hiệu trong lúc đó.

PATH ĐƯỢC SỞ HỮU
Tạo mới:
- Core/Settings/** (PlayerSettings dữ liệu, SettingsRules, ISettingsStore) · Services/Settings/** (PlayerPrefsSettingsStore)
- Services/Haptics/** (HapticsService + HapticThrottle) · UI/Haptics/** (HapticFeedbackListener)
- UI/Pause/** (PauseController, PauseRules, BackKeyRouter, IBackHandler) · UI/Settings/** · UI/Loading/** (LoadingOverlay, LoadingProgressMapper)
- UI/Controls/JoystickTouchZone.cs · UI/Controls/JoystickMath.cs · UI/Controls/OnScreenPotionButton.cs (+ view model thuần)
- Tests/EditMode: thư mục con mới cho Settings, Pause, Controls, Haptics, Loading
Sửa có giới hạn:
- UI/Controls/OnScreenJoystick.cs (dead zone, chế độ floating, độ nhạy)
- UI/Common/SceneLoader.cs (thêm overload báo tiến độ, giữ nguyên API cũ)
- UI/MainMenu/MainMenuController.cs và UI/Boot/BootController.cs (chỉ nối LoadingOverlay)
- UI/Inventory/InventoryPanel.cs, UI/Quest/QuestPanel.cs, UI/Floor/FloorSelectPanel.cs (chỉ implement IBackHandler và đăng ký khi bật/tắt)
- PvE/Items/PlayerInventory.cs (chỉ thêm accessor đọc: IsInCombat và CombatLockRemainingSeconds, dựa trên UseClock và ConsumableRules.DefaultCombatLockSeconds; KHÔNG đổi luật)
- Core/Presentation/CombatFeelConfig.cs (chỉ thêm field thời lượng và ngưỡng rung)
- docs/05 mục Phase 9.6 (ghi chú kỹ thuật, KHÔNG điền evidence)
Chỉ đọc: mọi thứ còn lại. Không sửa PvE Player/Enemy/Boss, DeathScreenController, PlayerHUD.

PHẢI LÀM
1. Pause + Back (UI/Pause).
   - PauseController dùng PlaySpeedState.PushPause/PopPause (KHÔNG ghi Time.timeScale). Panel Pause có: Tiếp tục, Cài đặt, Về Main Menu.
   - PauseRules (pure): CanPause = không đang chết, không trong Death flow (nghe OnPlayerDied, OnPlayerDeathRecorded, OnDeathRecordUnconfirmed; không bao giờ cho pause sau đó trong scene này), không đang tải. Test đầy đủ.
   - Thời gian thế giới (server) không dừng khi Pause; không có gì để làm ở package này ngoài việc không đụng nó.
   - "Về Main Menu" bị khóa khi người chơi đang giao chiến (PlayerInventory.IsInCombat) để Pause không thành nút thoát hiểm giữa trận (docs/09 §1.2). Vì Time.time dừng khi Pause nên KHÔNG hiện đếm ngược trong Pause: hiện chữ "Đang giao chiến — tiếp tục chơi cho tới khi hết giao chiến" và vô hiệu nút. Test thuần cho quy tắc khóa (MainMenuQuitGate).
   - Về Main Menu: gọi PlayerInventory.FlushNow trước khi đổi scene, PopPause đủ số lần (timeScale phải về 1), không mất tiến trình đã lưu.
   - BackKeyRouter (pure phần ưu tiên): Back đóng panel đang mở theo thứ tự trên cùng trước (Settings, Túi, Quest, Chọn Tầng); không còn panel thì mở Pause nếu CanPause; đang ở Pause thì Back = Tiếp tục; Death Screen đang hiện thì Back KHÔNG làm gì và KHÔNG thoát app. IBackHandler do các panel implement và tự đăng ký. Không để Back thoát app ở bất kỳ trạng thái nào trong gameplay.
2. Settings (UI/Settings + Core/Settings + Services/Settings).
   - Nội dung: âm lượng nhạc, âm lượng SFX (dùng setter của IAudioService, có hiệu lực ngay), bật/tắt rung, kiểu joystick (Cố định hoặc Floating), dead zone, độ nhạy (nhân handleRange trong khoảng 0.8–1.3). Tất cả lưu local bằng PlayerPrefs qua ISettingsStore, khóa có tiền tố settings., KHÔNG vào Player Data.
   - SettingsRules (pure): giá trị mặc định, kẹp khoảng, đọc giá trị rác/thiếu khóa thì về mặc định.
   - TUYỆT ĐỐI không có nút Reset Guest, xóa tài khoản hay đăng xuất trong Settings hay Main Menu (AGENTS.md luật 10).
   - Mở được từ Pause và từ Main Menu.
3. Joystick floating + dead zone.
   - JoystickMath (pure): Apply(delta, range, deadZone) → vector đầu ra; độ dài dưới deadZone → 0; trên ngưỡng thì co giãn lại (len − dz)/(1 − dz) để không bị nhảy vọt; độ dài đầu ra kẹp ≤ 1; hướng giữ nguyên.
   - JoystickTouchZone: vùng cảm ứng trong suốt phủ NỬA TRÁI màn hình. Chế độ Floating: chạm ở đâu thì nền joystick hiện ở đó (kẹp trong màn hình, chừa lề), kéo để di chuyển, nhả thì ẩn hoặc về vị trí gốc. Chế độ Cố định: hành vi cũ (zone vô hiệu).
   - Vẫn một pointer cho joystick; phải dùng được ĐỒNG THỜI với nút Đánh/Né/Technique bên phải (đa chạm). Zone không được chặn raycast của nút khác (nằm dưới các nút trong thứ tự vẽ).
   - OnScreenJoystick giữ API cũ (AssignInputReader) để PvEBootstrap không phải đổi.
3b. HAI NGƯỠNG CHẾT CHỒNG NHAU (bắt buộc xử lý). P9.5a (K0) đã thêm Core/Facing/FacingTracker với một ngưỡng chết riêng cho hướng nhìn, còn P9.6 thêm dead zone joystick (JoystickMath). Nếu để hai ngưỡng độc lập thì người chơi kéo nhẹ vẫn có thể đi mà không đổi hướng nhìn (hoặc ngược lại), và thanh trượt Dead zone trong Settings sẽ không có tác dụng thống nhất.
   - ĐỌC Core/Facing/FacingTracker.cs, PvE/Player/PlayerController.cs và Handoff của K0 (có ghi giá trị mặc định và nơi cấu hình ngưỡng của FacingTracker).
   - Quyết MỘT nguồn duy nhất cho dead zone. Đề xuất: JoystickMath quyết đầu ra (dưới dead zone thì vector = 0, trên ngưỡng thì co giãn lại như mô tả ở trên); FacingTracker chỉ lọc vector ĐÃ QUA dead zone, nên ngưỡng của nó phải rất nhỏ (chỉ lọc nhiễu số) và KHÔNG lớn hơn dead zone nhỏ nhất mà Settings cho phép.
   - Được phép sửa TỐI THIỂU để làm việc đó: giá trị ngưỡng của FacingTracker (hoặc nơi gán nó trong PlayerController) và test tương ứng. Cần sửa nhiều hơn thì dừng phần đó và ghi ACR vào Handoff.
   - Thêm test: vector dưới dead zone cho đầu ra 0 và FacingTracker giữ hướng cũ; vector ngay trên dead zone làm hướng nhìn đổi đúng.
   - GHI VÀO HANDOFF: nguồn dead zone đã chọn, giá trị cuối cùng của cả hai ngưỡng, và file/dòng đã sửa.
4. Nút dùng bình (UI/Controls/OnScreenPotionButton).
   - Field serialize itemId (Unity AI gán). Một chạm gọi PlayerInventory.TryUseConsumable(itemId). Hiển thị số bình còn lại, trạng thái bằng view model thuần (PotionButtonViewModel.From(ConsumableUseResult, số lượng, giây còn lại)): Ok → sáng; InCombat → mờ + chữ "Đang giao chiến" kèm đếm ngược giây còn lại (CombatLockRemainingSeconds); OnCooldown → mờ + đếm ngược; OutOfStock, FullHealth, PlayerDead → mờ. Chạm vào nút mờ thì hiện toast lý do bằng ConsumableRules.DescribeResult (không dùng chuỗi tự chế).
   - KHÔNG đổi ConsumableRules hay cho dùng bình giữa trận. Nếu product owner muốn thì đó là quyết định đổi docs/07 §2, không phải việc của package này.
5. Rung (Services/Haptics + UI/Haptics).
   - HapticsService: Android dùng Vibrator qua androidjni với VibrationEffect khi API ≥ 26 (có fallback Handheld.Vibrate); Editor và nền tảng khác không làm gì; tôn trọng cài đặt Rung; HapticThrottle (pure): khoảng cách tối thiểu 80ms giữa hai xung.
   - HapticFeedbackListener (UI): rung ngắn (khoảng 40ms) khi người chơi mất máu (OnHealthChanged của người chơi, Delta < 0); cảnh báo HP thấp (hai xung ngắn) MỖI LẦN HP rơi xuống dưới 30% (có độ trễ: chỉ báo lại sau khi HP lên trên 35%). Không rung khi đã chết. Số liệu nằm trong CombatFeelConfig.
   - Ghi vào Handoff: quyền VIBRATE trong AndroidManifest sau khi build (Unity AI sẽ kiểm); không tự sửa manifest.
6. Loading nhẹ (UI/Loading + SceneLoader).
   - SceneLoader.LoadSceneAsync(sceneName, mode, IProgress<float>) giữ API cũ. LoadingProgressMapper (pure): AsyncOperation.progress chạy 0→0.9 được ánh xạ về 0→1, đơn điệu tăng, không nhảy lùi, có thời gian hiện tối thiểu (ví dụ 0.4s) để không chớp.
   - LoadingOverlay: thanh tiến độ và dòng chữ; dùng khi Boot → MainMenu và MainMenu → gameplay. Không làm splash riêng (dùng splash của Player Settings, việc của Unity AI/Art Track).

KHÔNG LÀM
Đồng bộ Settings lên cloud; layout nút do người chơi tự kéo; nhiều profile joystick; cho phép dùng bình trong giao chiến; nút Reset Guest; lưu vị trí người chơi (FB-017); mini-map; sửa Death Screen.

TEST BẮT BUỘC (EditMode)
JoystickMathTests (dưới dead zone = 0; liên tục tại ngưỡng; kẹp ≤ 1; giữ hướng) · test phối hợp dead zone với FacingTracker (mục 3b) · PauseRulesTests · MainMenuQuitGateTests · BackKeyRouterTests (thứ tự ưu tiên; Death Screen chặn; không bao giờ thoát app) · SettingsRulesTests (mặc định, kẹp, giá trị rác) · PotionButtonViewModelTests (đủ mọi giá trị ConsumableUseResult) · HapticThrottleTests · low-HP hysteresis (pure) · LoadingProgressMapperTests (đơn điệu, kết thúc đúng 1) · test PlaySpeedState về 1 sau Pause/Resume lồng nhau và sau khi đổi scene.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.6 trong docs/05 (case a–h), gồm bài test "chết oan" 10 phút (FB-011). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````

### U2 — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.6, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.6 (Nền UX cảm ứng) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.6; P9.5 (PlaySpeedState, AudioService) đã nối. Console sạch, test EditMode xanh.
Nguồn tên class/field/đường dẫn: Handoff của Kiro. Tên thật khác với tên dưới đây thì theo tên thật và ghi lại.

VIỆC CẦN LÀM
1. Pause và Back.
   - Nút Pause trên Canvas HUD, góc trên bên phải, vùng chạm tối thiểu 96×96 px ở reference 1920×1080, cách nút kế bên tối thiểu 24 px.
   - Panel Pause (prefab hoặc con của Canvas theo Handoff): "Tiếp tục", "Cài đặt", "Về Main Menu". Panel khởi đầu TẮT. Nút "Về Main Menu" có dòng chữ trạng thái khóa kèm đếm ngược khi đang giao chiến (theo field Kiro đặt).
   - BackKeyRouter (đối tượng persistent theo Handoff) gán tham chiếu; các panel Túi, Quest, Chọn Tầng, Settings, Pause tự đăng ký IBackHandler (không cần nối tay nếu Kiro nói đã tự đăng ký).
2. Settings (panel riêng): thanh trượt Âm lượng nhạc và SFX (0–1), công tắc Rung, lựa chọn kiểu joystick (Cố định/Floating), thanh trượt Dead zone và Độ nhạy (0.8–1.3). Gán mọi field theo Handoff. TUYỆT ĐỐI KHÔNG có nút Reset Guest, xóa tài khoản hay đăng xuất trong Settings, Pause hay Main Menu (AGENTS.md luật 10). Nút mở Settings: trong Pause và trong Main Menu theo Handoff.
3. Joystick.
   - Giữ nguyên joystick cố định hiện có (làm phương án Cố định). Thêm JoystickTouchZone: một Image trong suốt phủ NỬA TRÁI màn hình, đặt TRƯỚC (dưới) mọi nút trong thứ tự hierarchy để các nút luôn nhận chạm trước; handle floating theo prefab/đối tượng Kiro nêu. Nửa phải màn hình phải còn nguyên cho nút hành động. Vùng này không được phủ nút Pause hay nút mở Túi/Quest (các nút đó nằm TRÊN nó).
   - Gán AssignInputReader và các tham chiếu theo Handoff; chỉ MỘT nguồn joystick đang hoạt động tại một thời điểm theo cài đặt.
4. Nút Bình (OnScreenPotionButton): đặt gần cụm nút Đánh/Né nhưng KHÔNG chồng lên; vùng chạm ≥ 96×96 px ref; icon lấy từ icon bình có sẵn trong Assets/_Game/Art/Items (nếu không có thì để trống, đừng tự vẽ); có chữ số lượng và một dòng chữ đếm ngược "Đang giao chiến" theo field Kiro đặt. Gán PlayerInventory và ViewModel theo Handoff. Không hard-code id bình ở scene.
5. Rung (HapticsService, persistent theo Handoff). Kiểm quyền VIBRATE: tìm AndroidManifest đã sinh trong bản build/Library (hoặc kiểm Player Settings → Publishing); nếu thiếu quyền VIBRATE thì KHÔNG tự thêm custom manifest — báo lại cho product owner kèm phương án.
6. Loading: LoadingOverlay (prefab theo Handoff) đặt trong scene Boot và MainMenu, Canvas sorting order cao nhất, khởi đầu TẮT; gán vào SceneLoader hoặc nơi Kiro nêu. Tên scene lấy từ Build Settings.
7. Kiểm bố cục ở các độ phân giải Game view: 1280×720, 1920×1080, 2400×1080 (20:9). Chụp ảnh: không nút nào chồng nhau, không nút nào ra ngoài màn hình, Pause không nằm trong vùng khuyết, joystick zone không che nút.
8. Lưu scene và prefab; ghi lại từng thay đổi (đường dẫn + field + giá trị).

KIỂM NHANH (Play Mode)
- Bấm Pause: game dừng (Time.timeScale = 0); Tiếp tục: về 1. Bấm Pause hai lần liên tiếp không kẹt. Phím Escape (đại diện nút Back của Android) mở Pause; khi đang mở Túi/Quest/Settings thì đóng panel đó trước.
- Death Screen đang hiện: Pause và Back không làm gì.
- Nút Bình: ngoài giao chiến thì dùng được khi HP chưa đầy; trong giao chiến thì khóa và hiện đếm ngược; HP đầy hay hết bình thì đúng thông báo theo ConsumableRules.
- "Về Main Menu" bị khóa trong giao chiến và dùng được sau khi hết đếm ngược.
- Joystick: floating hiện ở chỗ chạm nửa trái; dưới dead zone thì đứng yên; kéo qua dead zone thì đi; đổi kiểu joystick trong Settings có hiệu lực. Chạm nửa phải không kích hoạt joystick.
- Settings: đổi âm lượng/Rung/joystick, thoát Play rồi vào lại → giá trị còn nguyên (PlayerPrefs).
- Loading overlay hiện khi chuyển Boot → MainMenu → gameplay.
- Dừng Play: timeScale = 1, Console sạch, lưu tất cả.
- CHƯA kiểm được (product owner làm trên Android): nút Back vật lý có thực sự thành Escape, rung thật, thanh loading trên máy yếu, bài test chơi 10 phút của FB-011.

KHÔNG LÀM
Không sửa C#; không thêm nút Reset Guest/xóa tài khoản/đăng xuất; không đổi Death Screen; không sửa Player Settings hay manifest nếu chưa được product owner đồng ý.
````


## Gói P9.7a — Phân vùng Tầng: Thị trấn và Vùng Farm

### K3a — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.7a. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.7 (mục 9.7a)) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.7a — Phân vùng Tầng: bố cục vùng và cổng ===

MỤC TIÊU (người chơi cảm nhận được): đi từ Thị trấn sang Vùng Farm mất thời gian thật, camera và nhạc đổi theo vùng, quái không đuổi vào Thị trấn. Hiện quái, NPC, khu an toàn và boss nằm chung một chỗ; quái spawn quanh một tâm duy nhất và có thể rơi vào Safe Zone; quái đuổi tiếp khi người chơi đã vào Safe Zone; camera không có bounds.
Nguồn: docs/05 Phase 9.7 và 9.7a; FB-021 trong docs/10; docs/12 §8.2 (Reach) và §11 (ZoneEntered); docs/02 §12 (quái dừng đuổi khi người chơi vào Safe Zone), §19 (entryPoint trong Safe Zone, spawn ngoài Safe Zone); docs/09 §1.2.
Điều kiện: P9.5 và P9.6 đã xong.

ĐỌC CODE
PvE/Floor/FloorSpawnRegistrar.cs · PvE/Floor/FloorSceneBinder.cs · PvE/Floor/FloorActivation.cs · PvE/Enemy/EnemySpawner.cs · PvE/Enemy/EnemyController.cs · PvE/Camera/CameraFollow.cs · PvE/SafeZone/SafeZoneProvider.cs và SafeZoneTrigger.cs · Core/Character/SafeZoneService.cs · Core/Events/GameEvents.cs · PvE/Quest/PlayerQuestLog.cs và Core/Quest/QuestRules.cs · Core/Quest/QuestDefinition.cs (QuestObjectiveType có Reach) · Core/Floor/FloorDefinition.cs · Core/Audio/IAudioService.cs (từ P9.5) · Editor/FloorSetupWizard.cs (mẫu Editor tool) · test FloorBranchingScanTests.

SỰ THẬT ĐÃ XÁC MINH TRONG CODE
- FloorSpawnRegistrar giữ MỘT field enemySpawner (đã được gán trong scene). Đổi tên field này sẽ làm mất liên kết đã serialize: GIỮ NGUYÊN tên và thêm field mảng riêng cho các spawner bổ sung.
- EnemySpawner.RandomPoint chọn điểm quanh spawnCenter theo bán kính, không kiểm Safe Zone. Spawner đặt vị trí TRƯỚC khi SetActive(true); OnEnable của EnemyController gọi ResetForSpawn — nên vị trí xuất phát của quái có thể được ghi nhận trong ResetForSpawn.
- EnemyController không có khái niệm vị trí gốc hay Safe Zone: ChaseState chỉ ngừng khi quá 1.5 lần DetectionRange; IdleState chuyển sang Chase khi trong DetectionRange.
- CameraFollow có SetBounds(Bounds) và SnapToTarget nhưng KHÔNG ai gọi SetBounds. Khi vùng hẹp hơn khung nhìn thì Mathf.Clamp với min > max không căn giữa; phải xử lý (căn giữa trục đó).
- GameEvents chưa có ZoneEntered dù docs/12 §11 đã định nghĩa là hợp đồng. QuestObjectiveType.Reach tồn tại nhưng chưa có gì phát hoặc nghe ZoneEntered.
- Safe Zone dựa trên VỊ TRÍ: SafeZoneProvider.GetOrCreate().IsSafeZoneAt(position). Static, đã có reset khi load.
- FloorDefinition_Floor02 có bossId rỗng nên Tầng 2 không có Hầm Boss.

LƯU Ý CHỒNG SỬA: EnemyController đã bị sửa bởi các gói trước (K1 và K1b; gói 9.8 sau này cũng sẽ sửa). Đọc diff và Handoff của gói trước, đọc bản hiện tại của file, sửa tối thiểu và KHÔNG ghi đè phần của gói khác.

PATH ĐƯỢC SỞ HỮU
Tạo mới:
- PvE/Zones/** (ZoneKind, ZoneVolume, ZoneTracker, ZoneResolver, ZoneCameraController, ZoneMusicRouter, SpawnPointPicker, EnemyLeashRules)
- UI/Zones/** (ZoneBanner)
- Editor/ZoneLayoutValidator.cs (menu Tools/Game2D/Validate Floor Zones; xem bên dưới)
- Tests/EditMode: thư mục con mới cho Zones
Sửa có giới hạn:
- Core/Events/GameEvents.cs: CHỈ thêm OnZoneEntered/RaiseZoneEntered và lớp ZoneEnteredArgs { string ZoneId; string DisplayName } (DisplayName là phần mở rộng payload so với docs/12 §11; ghi vào Handoff để Integrator cập nhật 12)
- PvE/Floor/FloorSpawnRegistrar.cs (nhiều spawner) · PvE/Floor/FloorSceneBinder.cs (chỉ nếu cần để đăng ký vùng)
- PvE/Enemy/EnemySpawner.cs (điểm spawn tránh Safe Zone, tùy chọn gắn với một ZoneVolume) · PvE/Enemy/EnemyController.cs (vị trí gốc, dừng đuổi khi người chơi trong Safe Zone, quay về)
- PvE/Camera/CameraFollow.cs (chuyển bounds mượt, căn giữa khi vùng hẹp)
- PvE/Quest/PlayerQuestLog.cs và Core/Quest/QuestRules.cs (chỉ phần Reach nghe ZoneEntered)
- docs/05 mục Phase 9.7a (ghi chú kỹ thuật, KHÔNG điền evidence), docs/02 §19 (một đoạn mô tả vùng) và docs/12 §11 chỉ nếu Integrator cho phép — nếu không thì ghi vào Handoff
Chỉ đọc: mọi thứ còn lại, kể cả BossController (việc của P9.7b).

PHẢI LÀM
1. ZoneVolume (PvE/Zones, MonoBehaviour trên Collider2D trigger): zoneId, displayName, kind (ZoneKind: Town, Farm, BossApproach, BossRoom), priority, musicId (có thể rỗng), cameraBounds (Bounds hoặc Collider2D tham chiếu). ID theo quy ước ổn định (đối chiếu docs/03 §2 và docs/11 §6): đề xuất zone.floor_01_town, zone.floor_01_farm, zone.floor_01_boss_room; báo trong Handoff nếu docs/03 yêu cầu khác. Không đưa tên Floor/quái cụ thể vào code.
2. ZoneTracker: xác định vùng chứa người chơi (OverlapPoint khoảng 10 lần/giây, không alloc, không Find mỗi frame) và phát GameEvents.RaiseZoneEntered khi người chơi chuyển vùng. Có trễ chống chớp ở biên (ra khỏi vùng phải vượt biên một khoảng nhỏ mới tính là rời). ZoneResolver (pure): chọn vùng theo priority khi chồng lấn; không vùng nào chứa → vùng cũ được giữ (không phát event). Phát ZoneEntered khi tầng được kích hoạt và người chơi được đặt ở EntryPoint (để banner, nhạc và camera đúng ngay từ đầu).
3. Camera theo vùng: ZoneCameraController đổi bounds của CameraFollow khi ZoneEntered, chuyển mượt (khoảng 0.4s, dùng unscaled time để không bị dừng hình làm lệch), SnapToTarget khi tầng vừa kích hoạt. Phần toán kẹp bounds tách thành hàm thuần (CameraBoundsMath) có test, xử lý trường hợp vùng hẹp hơn khung nhìn (căn giữa trục đó).
4. Spawn theo vùng và tránh Safe Zone: SpawnPointPicker (pure, nhận hàm kiểm "bị chặn" và nguồn ngẫu nhiên) thử tối đa N lần để lấy điểm KHÔNG nằm trong Safe Zone (và nằm trong ZoneVolume gán cho spawner nếu có); hết N lần thì dùng spawnCenter đã được kiểm. EnemySpawner dùng nó cho cả spawn lẫn hồi sinh (RandomPoint và ReviveSlots). FloorSpawnRegistrar: thêm field mảng spawner bổ sung (giữ nguyên field cũ) và đăng ký TẤT CẢ spawner (SetAllowedEnemyIds, SetFloorNumber, SpawnAll) — Register vẫn an toàn khi gọi nhiều lần.
5. Quái dừng đuổi khi người chơi vào Safe Zone và quay về: EnemyLeashRules (pure): quyết định bỏ mục tiêu khi vị trí mục tiêu nằm trong Safe Zone; quái quay về vị trí gốc (ghi nhận trong ResetForSpawn) rồi về Idle; trong lúc quay về nếu người chơi ra khỏi Safe Zone và vào lại DetectionRange thì được đuổi lại. IdleState cũng phải coi mục tiêu trong Safe Zone là không hợp lệ (nếu không quái đứng ở rìa sẽ nhảy qua lại giữa Idle và Chase). Kiểm Safe Zone theo chu kỳ ngắn (ví dụ 0.2s), không mỗi frame. Không thêm state công khai ngoài những gì cần; không phá các test hiện có. Hành vi trước đó của quái ngoài tình huống này KHÔNG đổi.
6. ZoneEntered và Reach: phát event theo docs/12 §11 và để objective Reach đếm tiến độ qua nó. Quest service quyết định tiến độ (trigger chỉ báo ý định): quest phải Active, zoneId khớp targetId, mỗi lần chuyển vào vùng đích đếm một lần, không đếm khi vùng đó đã là vùng hiện tại và người chơi chỉ nhúc nhích ở biên. Test với QuestRules.
7. Banner và nhạc: ZoneBanner (UI) hiện displayName của vùng khoảng 2 giây rồi mờ, không popup xác nhận, không chặn thao tác; không hiện lại cho cùng một vùng trong 5 giây (chống chớp). ZoneMusicRouter (PvE) đổi nhạc theo musicId của vùng qua IAudioService.PlayMusic (crossfade); nhạc boss của P9.5 (boss đang trong trận) được ưu tiên hơn nhạc vùng; vùng không có musicId thì giữ nhạc hiện tại.
8. ZoneLayoutValidator (Editor script, menu Tools/Game2D/Validate Floor Zones): chạy trên scene đang mở và báo (Console + hộp thoại): mỗi Floor có đúng một vùng Town nằm trong một Safe Zone; entryPoint nằm trong Safe Zone; mọi EnemySpawner có spawnCenter ngoài Safe Zone; có vùng BossRoom khi và chỉ khi FloorDefinition.bossId khác rỗng; không có ZoneVolume trùng zoneId; vùng Farm có ít nhất một spawner. Đây là công cụ cho Unity AI/product owner kiểm bố cục, không ghi gì vào scene.
9. Không rẽ nhánh theo số tầng: FloorBranchingScanTests phải vẫn xanh. Tầng 2 phải dùng đúng cùng các thành phần (data/scene).

KHÔNG LÀM
Phòng boss và kích hoạt boss theo phòng (P9.7b); nhiều scene cộng dồn; cổng teleport; puzzle/cơ cấu; mini-map; thêm field vào FloorDefinition (nếu bắt buộc thì ACR); khóa cửa; nhạc boss mới; nhiều người trong một Floor.

TEST BẮT BUỘC (EditMode)
ZoneResolverTests (priority, chồng lấn, không vùng nào, giữ vùng cũ) · ZoneTracker hysteresis (pure) · CameraBoundsMathTests (kẹp đúng; vùng hẹp căn giữa; không NaN) · SpawnPointPickerTests (không bao giờ trả điểm trong Safe Zone khi còn điểm hợp lệ; fallback an toàn; tôn trọng ZoneVolume) · FloorSpawnRegistrar đăng ký đủ mọi spawner (dựng GameObject như test hiện có) · EnemyLeashRulesTests (bỏ mục tiêu trong Safe Zone; quay về; đuổi lại khi ra khỏi Safe Zone; Idle không nhảy qua lại) · ReachObjectiveTests qua ZoneEntered (đếm một lần; sai zoneId bị bỏ qua; quest chưa Active bị bỏ qua) · ZoneBanner cooldown (pure) · GameEvents.ZoneEntered phát đúng payload · test ReviveSlots hiện có vẫn xanh.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.7a trong docs/05 (case a–h), gồm bấm giờ đi bộ Thị trấn → Cổng Hầm khoảng 20–30 giây. Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````

### U3a — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.7a, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.7a (Phân vùng Tầng: Thị trấn và Vùng Farm) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.7a; P9.5 và P9.6 đã nối. Console sạch, test EditMode xanh. Hiện Floor 1 nằm trong scene gameplay chính và mọi thứ (NPC, Safe Zone, quái, boss) đang chung một chỗ; Floor 2 nạp additive.
Nguồn tên class/field/đường dẫn: Handoff của Kiro.

NGUYÊN TẮC BỐ CỤC
- Safe Zone dựa trên VỊ TRÍ (collider Safe Zone hiện có). Thị trấn phải nằm TRỌN trong Safe Zone; entryPoint (điểm vào) phải nằm trong Safe Zone; spawner và điểm spawn của quái phải nằm NGOÀI Safe Zone. Không phóng to Safe Zone để cho vừa; nếu Thị trấn lòi ra ngoài thì THU NHỎ vùng Thị trấn (Safe Zone ảnh hưởng chặn damage và spawn nên không đổi lặng lẽ). Nếu thật sự phải đổi Safe Zone thì dừng và hỏi product owner.
- Đi bộ từ entryPoint tới đầu Vùng Farm rồi tới Cổng Hầm (cuối Vùng Farm) mất khoảng 20–30 giây. Đọc tốc độ di chuyển thật của người chơi (field moveSpeed ở component/definition của người chơi) rồi tính khoảng cách mục tiêu = moveSpeed × 25 giây; ghi số đã dùng.
- Không đổi art, không thêm tilemap hay sprite nền (Art Track làm sau). Vùng chỉ là collider trigger và Gizmo; nếu cần dễ nhìn cho bước kiểm thì dùng Gizmo/màu trong Scene view, không thêm hình vào Game view.

VIỆC CẦN LÀM (cho TỪNG tầng; tầng 2 làm trong scene của tầng 2, cùng thành phần, cùng quy ước — KHÔNG làm riêng một kiểu khác)
1. Tạo đối tượng gốc "Zones" dưới gốc của tầng. Tạo ZoneVolume (BoxCollider2D hoặc PolygonCollider2D, isTrigger bật) cho: Thị trấn (kind Town), Vùng Farm (kind Farm).
   - zoneId đúng quy ước Kiro báo (đề xuất zone.floor_01_town, zone.floor_01_farm; Tầng 2: zone.floor_02_town, zone.floor_02_farm); displayName tạm: "Thị trấn", "Vùng Farm" (product owner đổi sau); priority theo Handoff (Farm và Thị trấn không chồng nhau nên priority bằng nhau là được; vùng nhỏ hơn thắng nếu chồng).
   - musicId: Thị trấn = music.town; Farm = music.gameplay. cameraBounds: tham chiếu chính collider của vùng (hoặc field Kiro nêu).
   - Tầng 2 KHÔNG có Hầm Boss (FloorDefinition_Floor02 có bossId rỗng) nên chỉ làm Thị trấn và Vùng Farm.
2. Vùng Farm: đặt cách Thị trấn đủ xa để thỏa 20–30 giây (xem trên); hình dạng đủ rộng cho spawner nhưng không rộng quá mức; lối đi từ Thị trấn sang Vùng Farm phải thông (không bị collider chắn).
3. Spawner: mỗi EnemySpawner hiện có chuyển spawnCenter vào BÊN TRONG Vùng Farm và NGOÀI Safe Zone, bán kính sao cho hình tròn spawn nằm trong Vùng Farm; gán field ZoneVolume cho spawner (nếu Kiro thêm) = Vùng Farm của tầng. FloorSpawnRegistrar: giữ nguyên field enemySpawner đã gán; mọi spawner còn lại cho vào mảng spawner bổ sung. Không đổi SpawnEntry (definition, count, respawnSeconds) trừ khi bố cục bắt buộc (khi đó ghi lại).
4. NPC: giữ trong Thị trấn (trong Safe Zone). Không đổi NPC definition.
5. Camera: gán ZoneCameraController (đối tượng và tham chiếu CameraFollow trên Main Camera theo Handoff). Kiểm camera chuyển bounds mượt khi đi qua ranh vùng và đứng giữa khi vùng hẹp hơn khung nhìn.
6. Đối tượng điều phối: ZoneTracker (tham chiếu người chơi theo tag Player hoặc theo Handoff), ZoneMusicRouter (tham chiếu AudioService/IAudioService theo Handoff), ZoneBanner trên Canvas HUD (Text + CanvasGroup, ở phía trên giữa; raycastTarget tắt; không che nút điều khiển).
7. Tầng 2 nạp additive: kiểm các thành phần điều phối không bị nhân đôi khi Tầng 2 nạp (nếu Tầng 2 cần ZoneTracker riêng hay dùng chung thì theo Handoff) và vùng của hai tầng không chồng lên nhau trong thế giới nếu chúng cùng hiện.
8. Chạy menu Tools/Game2D/Validate Floor Zones trên từng scene. Kết quả phải sạch, NGOẠI TRỪ lỗi "Tầng 1 thiếu vùng BossRoom" (sẽ hết sau bước P9.7b). Mọi lỗi khác thì sửa bố cục (không sửa C#) hoặc báo lại.
9. Lưu scene; ghi lại từng thay đổi (đường dẫn + field + giá trị) và số liệu bố cục (khoảng cách và thời gian đi bộ dự tính).

KIỂM NHANH (Play Mode; chụp ảnh Game view; đọc Console)
- Vào Tầng 1: banner tên Thị trấn hiện khoảng 2 giây rồi mờ; nhạc town (đọc id nhạc đang phát qua eval nếu AudioService cho phép).
- Đi (dùng phím di chuyển trong Editor) sang Vùng Farm: banner Vùng Farm, nhạc đổi, camera đổi bounds mượt. Đi lại biên vài lần: banner không chớp (không hiện lại trong 5 giây).
- Lấy mẫu vị trí spawn: gọi spawner spawn/hồi sinh ít nhất 200 lần (eval) và đếm số điểm nằm trong Safe Zone (SafeZoneProvider.IsSafeZoneAt). Phải bằng 0.
- Dụ một con quái đuổi rồi chạy vào Safe Zone: quái bỏ đuổi và quay về chỗ cũ; không nhảy qua lại Idle/Chase ở rìa.
- Ước lượng thời gian đi bộ entryPoint → cuối Vùng Farm bằng khoảng cách ÷ moveSpeed; báo số.
- Đổi sang Tầng 2 (qua Chọn Tầng): lặp lại các mục trên.
- Dừng Play: Console sạch, lưu tất cả.
- CHƯA kiểm được (product owner làm trên Android): bấm giờ đi bộ thật 20–30 giây, nhạc crossfade theo vùng trên máy, FPS.

KHÔNG LÀM
Không sửa C#; không đổi Safe Zone; không thêm art; không thêm cổng teleport hay cửa; không tạo vùng BossRoom/BossApproach (làm ở bước P9.7b); không đổi FloorDefinition.
````


## Gói P9.7b — Hầm Boss và phòng boss

### K3b — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.7b. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.7 (mục 9.7b)) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.7b — Hầm Boss và phòng boss ===

MỤC TIÊU (người chơi cảm nhận được): boss không còn "thức dậy" từ xa. Người chơi đi qua hành lang và tiền sảnh, vào phòng boss thì trận mới bắt đầu; chạy ra khỏi phòng thì trận kết thúc và boss quay về giữ nguyên máu. Hiện boss chuyển Idle → Chase chỉ theo DetectionRange, không có phòng, không có giới hạn.
Nguồn: docs/05 Phase 9.7 và 9.7b; FB-021 trong docs/10; docs/12 §7.2; docs/02 §13 và §22–24; docs/09 §1.3.
Điều kiện: P9.7a đã xong (cần ZoneVolume, ZoneTracker, ZoneEntered, ZoneBanner).

ĐỌC CODE
PvE/Boss/BossController.cs (đọc kỹ toàn bộ state machine) · PvE/Boss/BossDefinition.cs · PvE/Boss/BossHitRules.cs · PvE/Boss/BossDefeatFlow.cs · PvE/Zones/** (từ P9.7a) · PvE/Floor/FloorActivation.cs · UI/HUD/BossHealthBar.cs (từ P9.5) · UI/Zones/ZoneBanner.cs · Tests liên quan Boss (BossHitRulesTests, BossBalanceSandboxTests).

SỰ THẬT ĐÃ XÁC MINH TRONG CODE
- BossController: IdleState chuyển sang ChaseState khi DistanceToTarget ≤ definition.DetectionRange (không có phòng hay leash). Màu state: Telegraph vàng/cam, Punish và Recovery xanh nhạt, Pressure telegraph và dash tím, Enrage hồng.
- Đòn lao di chuyển bằng Rigidbody2D.linearVelocity và gây damage qua _pressureHitGate (đúng một lần mỗi lần lao); đòn thường kiểm vùng đánh tại thời điểm đánh (BossHitRules).
- ResetEncounter (gọi từ OnFloorActivated): máu đầy, hết enrage, về _homePosition, về Idle. Đây là reset khi VÀO tầng, KHÔNG phải khi rời phòng.
- Boss chết → DeadState → HandleDefeatAsync: phát OnEnemyDefeated/OnBossDefeated, báo server, thưởng. Luồng này KHÔNG được đổi.
- Rematch (boss đã DEFEATED trong World State) không cho EXP; luật này nằm trong BossDefeatFlow, không đụng.

PATH ĐƯỢC SỞ HỮU
Tạo mới: PvE/Boss/BossEncounterRules.cs (pure) · Tests/EditMode: test mới cho BossEncounterRules và các test GameObject cần thiết.
Sửa có giới hạn:
- PvE/Boss/BossController.cs: CHỈ (a) field serialize encounterZone (tham chiếu ZoneVolume, có thể null), (b) điều kiện rời Idle, (c) state mới ReturnHome và đường rời trận, (d) không đổi bất cứ thứ gì thuộc luồng chết, báo server, thưởng, Pressure, tính damage.
- PvE/Zones/ZoneTracker.cs (chỉ thêm API hỏi "người chơi có đang trong vùng X không" nếu P9.7a chưa có)
- docs/05 mục Phase 9.7b (ghi chú kỹ thuật, KHÔNG điền evidence)
Chỉ đọc: mọi thứ còn lại. Không đổi BossDefinition (nếu cần field mới thì ACR), BossHitRules, BossDefeatFlow, FloorLoader.

PHẢI LÀM
1. Kích hoạt theo phòng. BossController nhận encounterZone (ZoneVolume kind BossRoom). Khi encounterZone KHÁC null: boss chỉ rời Idle (sang Chase) khi người chơi đang ở BÊN TRONG vùng đó; hành lang và tiền sảnh (vùng BossApproach) không kích hoạt dù nằm trong DetectionRange. Khi encounterZone là null: hành vi CŨ giữ nguyên (chỉ DetectionRange) để scene hiện có không vỡ trước khi Unity AI nối vùng. Không dùng kiểm tra tên Floor hay id boss.
2. Rời phòng giữa trận. BossEncounterRules (pure) quyết định: người chơi rời encounterZone trong lúc boss đang Chase, Telegraph, Attack, Punish, Pressure* → trận kết thúc. Thực hiện: hủy telegraph và đòn lao đang chạy (không còn damage nào sau khi rời — _pressureHitGate được reset, StopMoving, trả màu đúng), boss đi về _homePosition bằng MoveToward (state ReturnHome mới) rồi về Idle. GIỮ NGUYÊN máu hiện tại, GIỮ trạng thái enrage, KHÔNG gọi ResetHealth/ResetEncounter, không đụng bộ đếm đòn Pressure ngoài việc đặt lại hợp lý (ghi rõ lựa chọn vào Handoff). Trong ReturnHome nếu người chơi vào lại phòng thì boss chuyển sang Chase ngay.
   - Người chơi chỉ chạm biên phòng: dùng trễ (hysteresis) của ZoneTracker để không bắt đầu và kết thúc trận liên tục.
   - Người chơi chết trong phòng: giữ luồng hiện tại (Death flow lo); boss về Idle như cũ theo logic sẵn có, không thêm hành vi mới.
3. Vào lại phòng: boss tiếp tục với máu cũ (và enrage nếu đã có), đánh được và hạ được; DefeatFlow, báo server, thưởng và rematch hoạt động y như Phase 8.
4. Vào tầng/reset: OnFloorActivated → ResetEncounter vẫn đưa boss về máu đầy như hiện tại (đây là reset theo tầng, không phải rời phòng).
5. Banner: vào vùng BossApproach hiện banner tên vùng bằng ZoneBanner có sẵn (đặt displayName ở scene, việc của Unity AI). Không popup xác nhận.
6. Cửa phòng: KHÔNG khóa cửa mặc định (permadeath cần đường lui rõ ràng). Nếu product owner muốn khóa cửa trong trận thì đó là quyết định riêng cần ACR; ghi vào Known issues rằng mặc định là không khóa.
7. Bảo vệ khỏi lạm dụng: ghi vào Handoff phân tích ngắn xem kiểu "ra phòng rồi vào lại" có tạo lợi thế bất công không (máu boss giữ nguyên nên không hồi máu; telegraph hủy nên không dính đòn khi ở ngoài). Nếu thấy lỗ hổng thì mô tả, đừng tự thêm luật.

KHÔNG LÀM
Khóa cửa; hồi máu boss khi rời phòng; sửa Pressure, damage, phần thưởng, báo server; đổi BossDefinition; boss mới; nhiều phòng boss; mini-boss; đổi nhạc boss (P9.5 đã có).

TEST BẮT BUỘC (EditMode)
BossEncounterRulesTests: không kích hoạt khi người chơi ngoài vùng dù trong DetectionRange · kích hoạt khi vào vùng · rời vùng lúc Telegraph/Pressure thì hủy và về · máu và enrage giữ nguyên sau khi rời · vào lại thì tiếp tục · encounterZone null thì giữ hành vi cũ · ResetEncounter khi kích hoạt tầng vẫn đầy máu. Test GameObject cho BossController (nếu dựng được trong EditMode như các test boss hiện có): không còn damage lên người chơi sau khi rời phòng; không phát lại OnBossDefeated; BossHitRulesTests và BossBalanceSandboxTests hiện có vẫn xanh.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.7b trong docs/05 (case a–e), kèm chạy lại P8 (a)–(h). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````

### U3b — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.7b, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.7b (Hầm Boss và phòng boss, Tầng 1) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.7b; bước nối P9.7a (u3a) đã xong. Console sạch, test EditMode xanh.
Nguồn tên class/field/đường dẫn: Handoff của Kiro. Tầng 2 KHÔNG có Hầm Boss (bossId rỗng): chỉ làm cho Tầng 1.

VIỆC CẦN LÀM
1. Trong đối tượng "Zones" của Tầng 1 tạo hai ZoneVolume (collider trigger):
   - BossApproach: phủ hành lang và tiền sảnh trước phòng boss. zoneId đúng quy ước Kiro (đề xuất zone.floor_01_boss_approach); displayName tạm "Cổng Hầm Boss"; kind BossApproach; nhạc để trống (giữ nhạc Vùng Farm).
   - BossRoom: phủ phòng boss. zoneId zone.floor_01_boss_room; displayName tạm "Phòng Boss"; kind BossRoom; musicId để TRỐNG (nhạc boss do luồng boss vào trận phát, theo P9.5). cameraBounds = collider phòng.
2. Bố cục: BossApproach nằm giữa cuối Vùng Farm và cửa phòng; BossRoom đủ rộng để né đòn lao của boss (đọc BossDefinition và đòn lao Pressure để biết quãng lao; đề nghị chiều ngắn nhất của phòng ≥ 2 lần quãng lao). Vị trí đứng ban đầu của Boss_Floor01 (đây chính là điểm nhà _homePosition) nằm BÊN TRONG BossRoom, phía sâu. Không dựng cửa hay vật chắn đóng lối (KHÔNG khóa cửa; permadeath cần đường lui).
3. Gán encounterZone của BossController trên đối tượng boss TRONG SCENE (không phải asset) = ZoneVolume BossRoom. Không đổi BossDefinition.
4. Boss không được tự kích hoạt từ hành lang dù đứng trong DetectionRange: kiểm ở phần kiểm nhanh. Nếu DetectionRange dài tới mức người chơi ở Vùng Farm đã "thấy" boss thì KHÔNG sửa số, chỉ ghi lại (việc gating đã do encounterZone lo).
5. Chạy menu Tools/Game2D/Validate Floor Zones trên scene Tầng 1: phải sạch hoàn toàn (kể cả điều kiện "có BossRoom khi bossId khác rỗng"). Tầng 2 vẫn sạch và KHÔNG có vùng BossRoom.
6. Lưu scene; ghi lại từng thay đổi (đường dẫn + field + giá trị).

KIỂM NHANH (Play Mode)
- Đứng ở BossApproach trong tầm DetectionRange: banner "Cổng Hầm Boss" hiện; boss vẫn Idle (đọc state hay field qua get_component_properties/eval; boss không đổi màu, không di chuyển).
- Bước vào BossRoom: boss rời Idle sang Chase; thanh máu boss hiện; nhạc boss vào.
- Đánh boss xuống một phần (ghi lại máu), bước ra khỏi phòng: boss hủy đòn đang báo hoặc đang lao, KHÔNG gây thêm damage lên người chơi sau khi rời (theo dõi HP người chơi), boss đi về điểm nhà, máu boss giữ nguyên số đã ghi (không hồi), trạng thái enrage giữ nếu đã có.
- Vào lại phòng: boss tiếp tục với máu cũ. Đứng sát biên phòng đi tới đi lui: không bắt đầu/kết thúc trận liên tục (có trễ).
- Vào Tầng 1 lại (qua Chọn Tầng hoặc nạp lại scene): boss đầy máu (reset theo tầng).
- Nếu Editor đã đăng nhập Guest và có mạng: hạ boss một lần để xem luồng thưởng và báo server chạy bình thường và bảng "Máy chủ đã ghi nhận. Tầng kế đã mở." vẫn nguyên chữ; nếu không thì ghi "chưa kiểm".
- Dừng Play: Console sạch, lưu tất cả.
- CHƯA kiểm được (product owner làm trên Android): P9.7b (a)–(e) và chạy lại P8 (a)–(h).

KHÔNG LÀM
Không sửa C#; không đổi BossDefinition; không khóa cửa; không tạo boss hay phòng boss thứ hai; không thêm BossRoom cho Tầng 2.
````


## Gói P9.8 — Quái đa vai trò (Ranged và Skirmisher)

### K4 — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.8. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.8 Quái đa vai trò) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.8 — Quái đa vai trò (Skirmisher và Ranged) ===

MỤC TIÊU (người chơi cảm nhận được): đi qua Vùng Farm gặp quái cư xử khác nhau và phải đổi cách đánh. Hiện mọi quái dùng chung một hành vi (đuổi → báo đòn → cận chiến) vì EnemyDefinition chưa có role. Sau package này: quái áp sát rồi lùi (Skirmisher), quái đứng xa bắn đạn né được (Ranged), quái cũ vẫn y như cũ.
Nguồn: docs/05 Phase 9.8; FB-030 trong docs/10; docs/12 §7.1 (CombatRole); docs/07 §4.4 (Dodge KHÔNG có i-frames), §5 (thời gian hạ quái Ranged/Skirmisher 8–14 giây); docs/11 §3 (role theo tầng); docs/02 §8 (Safe Zone chặn damage trước IDamageable), §12, §19.
Điều kiện: P9.5 và P9.7a đã xong (cần bộ giải màu/flash, EnemyLeashRules, ZoneVolume).

ĐỌC CODE
PvE/Enemy/EnemyController.cs (đọc kỹ từng state Idle/Chase/Attack/Hurt/Dead và cách tính damage lên người chơi) · PvE/Enemy/EnemyDefinition.cs (hoặc vị trí thật của nó; nếu ĐÃ có khái niệm tương tự role/elite/tier thì dùng cái có sẵn, báo lệch, đừng tạo trùng) · PvE/Enemy/EnemySpawner.cs · PvE/Zones/** và EnemyLeashRules (P9.7a) · PvE/Presentation/** (P9.5, bộ giải màu) · PvE/Floor/FloorActivation.cs và interface IFloorActivationListener · PvE/SafeZone/SafeZoneProvider.cs · Core/Combat/** (DamageInfo, IDamageable, đường gây damage lên người chơi) · PvE/Dodge/DodgeController.cs (chỉ đọc, để biết quãng và thời gian lướt) · Core/Events/GameEvents.cs · các test sandbox cân bằng quái hiện có trong Tests/EditMode (tên chứa Sandbox; dùng lại mô hình của chúng) · FloorBranchingScanTests · docs/03 §2 và docs/11 §6 (quy ước ID content).

SỰ THẬT ĐÃ XÁC MINH TRONG CODE (đừng đoán khác)
- Một Enemy.prefab dùng chung cho mọi loại quái; loại quái được quyết bởi EnemyDefinition (ScriptableObject). Asset EnemyDefinition hiện có KHÔNG được đổi hành vi và KHÔNG bị sửa file: field mới phải có giá trị mặc định an toàn (role = Chaser) để asset cũ nạp lên vẫn hoạt động như trước.
- EnemyController có state Idle/Chase/Attack/Hurt/Dead; ResetForSpawn chạy trong OnEnable (quái dùng pool); timer dùng Time.deltaTime có scale (dừng hình và Pause làm quái chậm theo — là chủ ý, đạn cũng phải theo cùng quy ước).
- Dodge không có i-frames: người chơi né đạn bằng cách di chuyển hoặc lướt RA KHỎI đường đạn. Nếu vẫn nằm trong đường đạn lúc đạn tới thì vẫn mất máu.
- Safe Zone chặn damage trước IDamageable (docs/02 §8). Safe Zone dựa trên vị trí: SafeZoneProvider.GetOrCreate().IsSafeZoneAt(position).
- Hiệu ứng nháy, số sát thương và âm thanh bị đánh của người chơi (P9.5) đã tự nghe OnHealthChanged của người chơi; đạn chỉ cần gây damage qua đúng đường chung thì các hiệu ứng đó tự chạy, KHÔNG thêm đường thứ hai (tránh số sát thương hiện hai lần).

LƯU Ý CHỒNG SỬA: EnemyController đã bị sửa bởi các gói trước (K1, K1b và 9.7a: vị trí gốc, dừng đuổi khi người chơi trong Safe Zone, quay về). Đọc diff và Handoff của các gói đó, đọc bản hiện tại của file, sửa tối thiểu (đúng ba điểm nối behavior) và KHÔNG ghi đè.

PATH ĐƯỢC SỞ HỮU
Tạo mới:
- PvE/Enemy/CombatRole.cs (enum: Chaser, Skirmisher, Guard, Ranged, Elite; Chaser là giá trị 0 để mặc định đúng)
- PvE/Enemy/Behaviors/** (IEnemyBehavior, ChaserBehavior, SkirmisherBehavior, RangedBehavior, EnemyBehaviorFactory, EnemyPositioningRules, SkirmisherCycleRules, RangedAttackRules — phần luật là pure, không dùng UnityEngine ngoài Vector2/Mathf)
- PvE/Enemy/Projectile/** (EnemyProjectile, EnemyProjectilePool, ProjectileMath pure)
- Tests/EditMode: thư mục con mới cho Enemy Roles
Sửa có giới hạn:
- PvE/Enemy/EnemyDefinition.cs: chỉ thêm field serialize role, preferredRange, retreatRange, retreatSeconds, shootRange, projectileSpeed, projectileLifetime, projectileRadius, projectileDamageScale (mọi field có mặc định và OnValidate kẹp giá trị; ví dụ retreatRange < preferredRange ≤ shootRange ≤ detectionRange). KHÔNG đổi, xóa hay đổi tên field cũ.
- PvE/Enemy/EnemyController.cs: chỉ thêm ba điểm nối cho behavior — (1) hướng di chuyển trong Chase, (2) điều kiện vào Attack (tầm đánh), (3) hành động tại thời điểm đánh (kiểm vùng cận chiến như cũ, hay bắn đạn). Không nhồi logic role vào EnemyController. Role Chaser/Guard/Elite phải cho CÙNG kết quả như code cũ.
- Core/Audio/AudioIds.cs và PvE/Presentation/CombatSfxRouter.cs (P9.5): chỉ thêm một hằng sfx.enemy_shoot và một dòng phát khi quái bắn; thiếu clip thì im lặng theo chuẩn catalog.
- docs/05 mục Phase 9.8 (ghi chú kỹ thuật, KHÔNG điền evidence)
Chỉ đọc: mọi thứ còn lại, kể cả BossController, PlayerController, DodgeController, FloorDefinition.

PHẢI LÀM
1. Role và field. CombatRole + các field mới trong EnemyDefinition (xem trên), mặc định an toàn, OnValidate kẹp. Tạo EnemyBehaviorFactory chọn behavior theo role một lần khi quái được cấu hình (không tạo mới mỗi frame, không cấp phát trong Update). Guard và Elite tạm dùng ChaserBehavior (ghi vào Known issues: chưa có thiết kế riêng).
2. Skirmisher (áp sát, đánh, lùi). Chu kỳ: Approach (đuổi tới tầm đánh) → Attack (báo đòn rồi đánh bằng đúng cơ chế cận chiến và windup sẵn có) → Retreat (lùi ra tới retreatRange trong retreatSeconds, hướng lùi là hướng xa người chơi, không lùi vào tường nếu có hướng khác; không chui vào Safe Zone) → quay lại Approach. SkirmisherCycleRules (pure): chuyển pha theo timer và khoảng cách có trễ để không dao động; không mắc kẹt khi bị chặn đường (hết retreatSeconds thì vẫn sang Approach).
3. Ranged (giữ khoảng cách, báo đòn, bắn). Quái đứng ở vùng [retreatRange, preferredRange]: người chơi đến gần hơn retreatRange thì lùi; xa hơn shootRange thì áp sát; trong tầm bắn thì vào windup (dùng telegraph và màu windup có sẵn, KHÔNG đổi chúng) rồi bắn một viên theo hướng đã KHÓA ở cuối windup (đạn không bám theo — đó là điều làm đạn né được), rồi hồi chiêu theo definition. Bị dồn vào góc không lùi được thì vẫn đánh bình thường (không đứng im vô tận). EnemyPositioningRules và RangedAttackRules (pure): hướng di chuyển, điều kiện bắn, hồi chiêu, không NaN khi khoảng cách = 0.
4. Đạn. EnemyProjectile + EnemyProjectilePool (MonoBehaviour trong scene PvE, đăng ký vào một static Current khi OnEnable và gỡ khi OnDisable; static reset an toàn khi tắt Domain Reload theo mẫu SafeZoneProvider; quái lấy pool một lần và cache, KHÔNG FindObjectOfType mỗi lần bắn). Pool dựng sẵn số lượng cố định (serialize, mặc định 16); hết chỗ thì bỏ phát bắn đó và log đúng một lần, không Instantiate trong combat. Đạn di chuyển thẳng bằng transform với Time.deltaTime (có scale), bước di chuyển kiểm va chạm theo đoạn (chống xuyên khi dt lớn) bằng toán thuần ProjectileMath, dùng API Physics2D không cấp phát (Unity 6, không để warning obsolete mới). Đạn bị thu hồi khi: hết projectileLifetime, trúng người chơi, chạm lớp vật cản (LayerMask serialize), vào Safe Zone, người chơi chết, hoặc tầng bị tắt (pool có ReleaseAll gọi qua IFloorActivationListener hoặc cơ chế hiện có của FloorActivation; đọc code để chọn đúng). Damage lên người chơi đi qua ĐÚNG đường mà EnemyController đang dùng cho đòn cận chiến (không tạo đường damage mới); damage = damage của definition × projectileDamageScale. Người chơi đang ở Safe Zone thì không nhận damage (IDamageable đã chặn — thêm test xác nhận đạn không bypass).
5. Né được trên điện thoại. Đạn phải đủ chậm: thời gian bay từ shootRange tới người chơi tối thiểu 0.5 s (giả định của package này, product owner đổi được; để thành hằng có tên trong ProjectileMath hoặc field của definition, không magic number). Test cân bằng assert điều này cho hai definition mới.
6. Leash và Safe Zone (P9.7a). Cả Skirmisher và Ranged phải tôn trọng EnemyLeashRules: bỏ mục tiêu khi người chơi vào Safe Zone, quay về vị trí gốc, không bắn vào Safe Zone, không lùi vào Safe Zone.
7. Hai definition mới, chỉ là SỐ trong Handoff (asset do AI nối Unity tạo; bạn KHÔNG tạo .asset). Tên tạm do dự án tự đặt, không lấy từ tác phẩm có bản quyền: Ranged = "Enemy_PipeSpitter" (id đề xuất enemy.pipe_spitter), Skirmisher = "Enemy_WireStalker" (id đề xuất enemy.wire_stalker); đối chiếu quy ước ID ở docs/03 §2 và id quái hiện có, báo lệch nếu khác. Handoff phải có bảng: maxHp, damage, attackRange, windup, cooldown, moveSpeed, detectionRange, preferredRange, retreatRange, retreatSeconds, shootRange, projectileSpeed, projectileLifetime, projectileRadius, projectileDamageScale, EXP và drop (đề xuất theo cùng tầng với quái hiện có), role.
8. Cân bằng (docs/07 §5). Dựng RangedEnemyBalanceSandboxTests theo mô hình của các sandbox hiện có: mô hình giây = số đòn × 0.45 ÷ 0.70; yêu cầu thời gian hạ mỗi con 8–14 giây ở level đề xuất của tầng (tương đương khoảng 13–21 đòn người chơi; tính từ damage người chơi thật, không đoán); số đòn quái cần để hạ người chơi và số quái mỗi level theo khung ở docs/07 §5 (đối chiếu khung đó; nếu khác con số này thì theo docs/07 và ghi lệch vào Handoff). Số nằm trong Handoff phải qua được test này.
9. Vị trí đặt quái (chỉ ghi vào Handoff cho bước Unity): ĐÃ CHỐT (ADR-030, 2026-10-18): Ranged ở phần sâu của Vùng Farm Tầng 1 và ở Tầng 2; Skirmisher ở Tầng 2. Khớp docs/05 mục 9.8 và docs/11 §3. Tầng 3 trở đi (Mốc 2) có role/biến thể riêng, quyết khi tới, KHÔNG làm ở package này.
10. Không rẽ nhánh theo số tầng hay id quái trong code; FloorBranchingScanTests và test quét Time.timeScale (P9.5) vẫn xanh.

KHÔNG LÀM
Quái theo pha ngày/đêm (FB-031); AI nhóm; boss mới hay đòn boss mới; Guard và Elite có hành vi riêng; đổi asset EnemyDefinition hiện có; tạo asmdef mới; sửa Dodge để có i-frames; hệ chí mạng hay trạng thái mới; asset art mới (dùng lại sprite hiện có làm placeholder, Art Track làm sau).

TEST BẮT BUỘC (EditMode)
EnemyDefinitionDefaultsTests (mặc định role = Chaser; OnValidate kẹp đúng thứ tự retreatRange < preferredRange ≤ shootRange ≤ detectionRange; asset cũ không đổi) · EnemyPositioningRulesTests (lùi khi quá gần; giữ khi trong dải; áp sát khi quá xa; khoảng cách 0 không NaN; bị dồn góc) · SkirmisherCycleRulesTests (Approach → Attack → Retreat → Approach; retreatSeconds đúng; không dao động; hết thời gian vẫn thoát khi bị chặn) · RangedAttackRulesTests (windup → khóa hướng → bắn; hồi chiêu; không bắn khi mục tiêu trong Safe Zone) · ProjectileMathTests (bước di chuyển; kiểm đoạn-tròn chống xuyên khi dt lớn; hết lifetime; thời gian bay tối thiểu 0.5 s) · EnemyProjectilePoolTests (dựng sẵn; lấy/trả dùng lại; hết chỗ thì bỏ và log một lần; ReleaseAll; không Instantiate sau prewarm) · test GameObject: đạn không gây damage khi người chơi trong Safe Zone, bị thu hồi khi vào Safe Zone, khi người chơi chết và khi tầng tắt; damage lên người chơi đi đúng một lần qua đường chung (OnHealthChanged phát đúng một lần) · EnemyBehaviorRegressionTests (role Chaser/Guard/Elite cho cùng quyết định di chuyển và tầm đánh như code cũ; các test EnemyController hiện có vẫn xanh) · RangedEnemyBalanceSandboxTests (8–14 giây cho cả hai definition mới; thời gian bay tối thiểu) · FloorBranchingScanTests xanh.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.8 trong docs/05 (đọc bảng để biết đủ các case; gồm ba hành vi nhận ra được, đạn né được, đạn và Safe Zone, thời gian hạ quái 8–14 giây, hồi sinh và pool, không hồi quy). Bạn chỉ chuẩn bị "Cách test"; không tự điền kết quả.
````

### U4 — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.8, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.8 (Quái đa vai trò: Ranged và Skirmisher) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.8 (có bảng số của hai định nghĩa quái mới trong Handoff và RangedEnemyBalanceSandboxTests xanh); bước nối P9.7a/b đã xong. Console sạch, test EditMode xanh.
Nguồn tên class/field/đường dẫn VÀ MỌI CON SỐ: Handoff của Kiro. Bạn KHÔNG tự nghĩ số cân bằng; thiếu số nào thì dừng và báo.

VIỆC CẦN LÀM
1. Hai EnemyDefinition mới (đường dẫn cùng thư mục với Enemy_Slime/Enemy_Bat hiện có). Cách làm: nhân bản một định nghĩa hiện có cùng tầng làm khuôn (để thừa hưởng các field không liên quan như drop, tag, layer), rồi ghi đè TOÀN BỘ field theo bảng của Kiro:
   - Enemy_PipeSpitter: role Ranged, id (đề xuất enemy.pipe_spitter, đúng quy ước Kiro đã đối chiếu), displayName tạm theo Handoff hoặc "Ống Phun".
   - Enemy_WireStalker: role Skirmisher, id (đề xuất enemy.wire_stalker), displayName tạm theo Handoff hoặc "Dây Rình".
   - Tên là tạm, do dự án tự đặt, không lấy từ tác phẩm có bản quyền. Sprite: dùng lại sprite placeholder có sẵn trong dự án, chọn sprite KHÁC các quái cùng tầng để người chơi phân biệt được bằng mắt; báo đã chọn sprite nào. Không vẽ, không tải art.
   - KHÔNG sửa bất kỳ EnemyDefinition hiện có (role mặc định Chaser giữ hành vi cũ). Sau khi xong, mở một định nghĩa cũ và xác nhận role = Chaser.
2. EnemyProjectile.prefab (đường dẫn theo Handoff, gợi ý Assets/_Game/Prefabs/Enemies/): GameObject + SpriteRenderer + EnemyProjectile (không cần Rigidbody hay collider nếu Kiro nói đạn tự kiểm va chạm bằng toán). Sprite: một sprite tròn nhỏ có sẵn trong Assets/_Game/Art/Placeholder; nếu không có sprite tròn thì dùng sprite tích hợp của Unity (Knob) và ghi nguồn. Màu sáng khác nổi bật so với nền và người chơi; kích thước = projectileRadius × 2 (đọc từ bảng Kiro). Sorting layer cùng lớp quái.
3. EnemyProjectilePool trong scene gameplay chính (MỘT pool dùng chung cho mọi tầng): gán prefab đạn, số lượng dựng sẵn theo Handoff (mặc định 16), LayerMask vật cản = các layer chắn đường thật của dự án (kiểm Tags & Layers và vật chắn của Floor 1/2, báo lại layer nào), đảm bảo pool tồn tại một bản duy nhất và không bị nhân đôi khi Tầng 2 nạp additive. Kết nối thu hồi đạn khi đổi tầng theo cách Kiro mô tả.
4. FloorDefinition_Floor01 và FloorDefinition_Floor02: thêm id quái mới vào danh sách id quái được phép (spawnEnemyIds hoặc tên field thật) — NẾU THIẾU BƯỚC NÀY spawner sẽ lọc bỏ quái mới. Tầng 1: enemy.pipe_spitter. Tầng 2: enemy.pipe_spitter và enemy.wire_stalker. Không đổi field khác.
5. Spawner (theo vị trí Kiro nêu ở Handoff): Tầng 1 — Ranged ở phần SÂU của Vùng Farm; Tầng 2 — Ranged và Skirmisher. Thêm SpawnEntry cho hai quái mới (count, respawnSeconds theo cách Kiro đề xuất hoặc bằng mức của quái cùng tầng). Tổng số quái đồng thời mỗi tầng trong khoảng 8–15 (đếm cả quái cũ); mỗi tầng phải có ít nhất 2 con của mỗi role mới được đặt ở tầng đó. Điểm spawn nằm trong Vùng Farm và ngoài Safe Zone (dùng ZoneVolume gán cho spawner như ở bước P9.7a). Chạy lại Tools/Game2D/Validate Floor Zones, phải sạch.
6. AudioCatalog: thêm entry cho id sfx.enemy_shoot (nếu Kiro thêm); thiếu clip thì để trống.
7. Nếu có danh mục content khác phải đăng ký quái mới (catalog, bảng EXP, bảng quest kill...) thì CHỈ làm theo Handoff của Kiro; không đoán.
8. Lưu scene, prefab, asset; ghi lại từng thay đổi (đường dẫn + field + giá trị).

KIỂM NHANH (Play Mode; chụp ảnh Game view; đọc Console)
- Quái Ranged: đứng xa giữ khoảng cách; khi người chơi đến gần thì lùi; có báo đòn (nháy đỏ) rõ trước khi bắn; đạn bay thẳng theo hướng đã khóa, không bám theo.
- Đứng yên cho đạn trúng: HP giảm đúng giá trị tính từ bảng Kiro (số damage × hệ số đạn). Di chuyển hay lướt ra khỏi đường đạn kịp thì không mất máu; Dodge KHÔNG miễn sát thương nếu vẫn nằm trong đường đạn.
- Đứng trong Safe Zone khi quái Ranged bắn: không mất máu; đạn biến mất, không kẹt lại.
- Quái Skirmisher: áp sát, đánh, rồi lùi, rồi áp sát lại; không dao động tại chỗ.
- Hạ từng loại: đếm số đòn cần (đòn người chơi thật) và so với bảng Kiro (khoảng 13–21 đòn tương đương 8–14 giây); báo số đo. Chờ hồi sinh: quái hồi sinh bình thường.
- Đổi tầng qua lại: không còn đạn bay sót (đếm đối tượng đạn đang bật trong pool bằng eval; phải bằng 0 sau khi đổi tầng).
- Toàn bộ test EditMode xanh; dừng Play: Console sạch; lưu tất cả.
- CHƯA kiểm được (product owner làm trên Android): bấm giờ thật 8–14 giây, cảm giác né đạn trên màn hình điện thoại, nhận ra ba hành vi mà không cần đọc tài liệu.

KHÔNG LÀM
Không sửa C#; không đổi EnemyDefinition cũ hay Enemy.prefab trừ khi Handoff yêu cầu; không tự đặt số cân bằng; không tạo art mới; không thêm boss.
````


## Gói P9.9 — Hướng dẫn và Mũi tên quest

### K5 — Prompt cho Kiro

````text
BẠN LÀ AI LẬP TRÌNH cho dự án Unity "Game2D" (thư mục dự án D:\Ae\GameAE, Unity 6000.3.24f1, Android landscape). Bạn nhận ĐÚNG MỘT Work Package: P9.9. Làm xong thì báo cáo và DỪNG; không tự sang package khác và không thêm tính năng ngoài danh sách bên dưới.

ĐỌC TRƯỚC (đúng thứ tự, vừa đủ): AGENTS.md → docs/05 (mục Phase 9.9 Hướng dẫn và Mũi tên quest) → docs/12 §3, §7, §11, §13 → docs/13 §3, §6–§9 → docs/06 → các tài liệu ghi trong package. Sau đó đọc code thật của các file trong mục "Đọc code" TRƯỚC khi sửa.
Trạng thái tài liệu (cập nhật 2026-10-18): AGENTS.md, docs/HANDOFF.md và docs/05 là nguồn thứ tự phase: 9.5 → 9.9 → Phase 11 → Phase 10. Duel (Phase 10) thuộc Mốc 2 (ADR-028 ở docs/01 §6), không phải việc của package này. docs/13 có work package 9.5a/9.5b/9.5c và tóm tắt 9.6–9.9; hợp đồng đầy đủ của package nằm ngay trong prompt này. KHÔNG sửa AGENTS.md, HANDOFF.md, docs/01, docs/13 (chỉ Integrator được sửa); nếu thấy lệch hay thiếu thì ghi vào Handoff.
QUY ƯỚC ĐƯỜNG DẪN: trong package, `Core/`, `PvE/`, `PvP/`, `Services/`, `UI/` là thư mục con của `Assets/_Game/Scripts/`. Riêng `Editor/` và `Tests/EditMode/` nằm ngay dưới `Assets/_Game/` (đã có `Assets/_Game/Editor/Game.Editor.asmdef` và `Assets/_Game/Tests/EditMode/`). Asset Unity nằm ở `Assets/_Game/{Art,Content,Data,Prefabs,Scenes,Audio}`. Nếu thư mục thật khác với mô tả này thì làm theo cấu trúc thật, rồi ghi vào Handoff.
Hợp đồng của package nằm ngay bên dưới và có hiệu lực thay cho mục Work Package trong docs/13 cho tới khi Integrator đưa nó vào 13.

LUẬT CỨNG
1. Chỉ sửa "Path được sở hữu". Không sửa .unity/.prefab/.asset/.meta, không sửa asmdef, không tạo asmdef mới (thật sự cần thì gửi Architecture Change Request theo docs/13 §7 rồi dừng phần đó).
2. Không đổi public contract hiện có. Event/field/API mới chỉ được thêm khi package liệt kê; mọi thứ khác → ACR.
3. BẢN QUYỀN (AGENTS.md luật 7, docs/12 §3, docs/11 §7): tên hiển thị, ID, sprite, âm thanh, thoại, UI đều do dự án tự đặt. Tuyệt đối không dùng hay nhắc tên, nhân vật, kỹ năng, vũ khí, boss, hình ảnh, âm thanh của bất kỳ tác phẩm có bản quyền nào, kể cả trong comment, tên file, tên test hay commit. Chỉ dùng cơ chế chung.
4. Kiến trúc: Core không biết Floor/Enemy/Item cụ thể và không dùng UGS; PvE không tham chiếu UI hay PvP; UI chỉ hiển thị và gửi ý định; không GameManager khổng lồ; balance nằm trong ScriptableObject, không magic number; không `if floor == N`; trong combat dùng pool, không Instantiate/Destroy.
5. Mọi ý định người chơi phải có đường cảm ứng (docs/12 §4.1). Phím bàn phím chỉ để test trong Editor.
6. Static và event toàn cục phải reset an toàn khi tắt Domain Reload (mẫu: `PvE/SafeZone/SafeZoneProvider.cs` dùng RuntimeInitializeOnLoadMethod). Không để Time.timeScale, đăng ký event hay coroutine rò sang scene khác.
7. Đọc code thật trước khi sửa. Thay đổi nhỏ nhất đủ làm việc; không format hay viết lại file ngoài phạm vi. Docs và code lệch nhau: báo lệch, đừng tự cho bên nào đúng.
8. Test: viết EditMode test cho mọi luật thuần (danh sách trong package); chạy TOÀN BỘ test EditMode hiện có và phải xanh (kể cả FloorBranchingScanTests); kiểm tra Unity Console không có lỗi hay warning mới.
9. KHÔNG tự điền "Pass" hay số đo thật vào bảng evidence trong docs/05; để trống cột Actual, Pass, Ngày.

BÁO CÁO CUỐI (bắt buộc đủ mục)
Changed (file) · Why · Not changed · Test đã làm (tên test + kết quả) · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất)
+ "Việc cần làm trong Unity Editor": liệt kê CHÍNH XÁC từng GameObject, prefab, asset, field cần tạo hoặc gán (tên, đường dẫn, giá trị). Một AI khác sẽ làm theo đúng danh sách này nên thiếu một bước là hỏng.
+ "Cách test" (Editor và điện thoại).
+ Handoff theo docs/13 §9 (Package, Owned files changed, Public contract, Tests, Console, Unity Editor actions remaining, Device evidence còn thiếu, Known limitations, Suggested next package only).

=== WORK PACKAGE P9.9 — Hướng dẫn và Mũi tên quest ===

MỤC TIÊU (người chơi cảm nhận được): người chơi mới hiểu cách chơi trong khoảng 3 phút mà không cần đọc tài liệu: có gợi ý ngắn đúng lúc (di chuyển, đánh, né, kỹ thuật, bình, Khu an toàn, nhặt và mặc đồ) và có mũi tên ở mép màn hình chỉ tới mục tiêu quest. Hiện không có hướng dẫn nào.
Nguồn: docs/05 Phase 9.9; FB-029 và FB-024 trong docs/10; docs/12 §4.1 (đường cảm ứng), §8 (Quest/NPC/Reach), §11; docs/07 §2 (bình chỉ dùng ngoài giao chiến) và §4.4 (Dodge không i-frames); docs/09 §1.2.
Điều kiện: P9.5, P9.6, P9.7a đã xong (cần PlaySpeedState, ISettingsStore và Settings, ZoneVolume/ZoneEntered, PlayerInventory.IsInCombat, event WindupStarted của quái). P9.8 nên xong trước nhưng không bắt buộc.
Thứ tự làm và cắt: làm PHẦN A (gợi ý) trước và báo xong, rồi PHẦN B (mũi tên quest). Thiếu thời gian thì cắt PHẦN B trước (docs/05 9.9). Handoff nói rõ phần nào đã xong.

ĐỌC CODE
Core/Quest/QuestRules.cs · Core/Quest/QuestDefinition.cs (QuestObjectiveType: Talk, Kill, Collect, Reach; có "hoàn thành thì trả ở NPC" hay tự hoàn thành?) · PvE/Quest/PlayerQuestLog.cs · code NPC hiện có (component NPC có npcId; đọc để biết đường dẫn thật và đã có lookup/registry chưa) · Core/Events/GameEvents.cs (OnHitResolved, OnHealthChanged, OnItemAcquiredWithPayload, OnLevelUp, OnBossDefeated, OnPlayerDied, OnZoneEntered, các event quest) · PvE/Enemy/EnemyController.cs (event WindupStarted từ P9.5) · PvE/Skills/TechniqueController.cs (tín hiệu "mở khóa" thật là gì? OnTechniqueStarted chỉ báo vừa dùng) · PvE/Dodge/DodgeController.cs (có event né không) · PvE/Items/PlayerInventory.cs (IsInCombat, CombatLockRemainingSeconds từ P9.6; sự kiện dùng bình/mặc đồ) · Core/Items/ConsumableRules.cs · PvE/SafeZone/SafeZoneProvider.cs · PvE/Zones/** và UI/Zones/ZoneBanner.cs (P9.7a) · UI/Settings/** (P9.6: SettingsPanel và ISettingsStore) · luồng báo boss P8-5: BossDefeatFlow và panel hiển thị (chuỗi "Máy chủ đã ghi nhận. Tầng kế đã mở." KHÔNG được đổi) · cách các script UI hiện có lấy người chơi (mẫu PlayerHUD) và cách lấy playerId (Services) · docs/04 (để chữ gợi ý không mâu thuẫn luật chết).

SỰ THẬT ĐÃ XÁC MINH (đừng đoán khác)
- Bình chỉ dùng được NGOÀI giao chiến (≥ 5 giây kể từ lúc bị đánh hay đánh; ConsumableRules.CanUse, DefaultCombatLockSeconds = 5). Gợi ý bình phải nói đúng điều này ("thoát khỏi giao chiến rồi bấm Bình"), không hứa dùng bình giữa trận.
- Dodge không có i-frames (07 §4.4): lời gợi ý né phải nói "lướt ra khỏi đường đòn", KHÔNG nói "bất tử" hay "miễn sát thương".
- Vị trí người chơi không được lưu (FB-017); vào lại tầng đặt ở EntryPoint trong Safe Zone.
- Reset Guest chỉ có ở Login Screen và Death Screen (AGENTS.md luật 10). Gói này KHÔNG thêm nút Reset Guest hay xóa tài khoản vào đâu cả.
- Cờ gợi ý là cờ LOCAL (PlayerPrefs), khóa theo playerId; Guest mới (playerId mới) tự thấy lại gợi ý. Không đưa vào Player Data, không gọi server.
- Core không biết Floor/Enemy/Item cụ thể; UI chỉ hiển thị và gửi ý định; không FindObjectOfType trong Update.

LƯU Ý CHỒNG SỬA: EnemyController đã bị sửa bởi các gói trước (K1, K1b, 9.7a, 9.8). Package này chỉ ĐỌC nó: đọc bản hiện tại (và Handoff của các gói đó) rồi lấy event WindupStarted từ đúng chỗ đang có; KHÔNG sửa và không ghi đè.

PATH ĐƯỢC SỞ HỮU
Tạo mới:
- Core/Guidance/** : TutorialHintDefinition (ScriptableObject: hintId, text, speakerLabel mặc định "Hệ thống", priority, displaySeconds mặc định 4, trigger kind, completeKind), enum TutorialTrigger và TutorialComplete (chung, KHÔNG chứa tên content cụ thể), TutorialRules (pure), ITutorialStore, QuestArrowMath (pure), QuestTargetRules (pure)
- Services/Guidance/PlayerPrefsTutorialStore.cs
- UI/Guidance/** : TutorialToastPresenter, TutorialTriggerRouter, TutorialSensors
- UI/Quest/QuestArrow.cs và QuestTargetResolver.cs (thư mục UI/Quest nếu đã có thì thêm file; không sửa file cũ)
- Tests/EditMode: thư mục con mới cho Guidance
Sửa có giới hạn:
- UI/Settings/** (P9.6): chỉ thêm hai field serialize (một Toggle "Gợi ý hướng dẫn", một Button "Xem lại hướng dẫn"), cả hai CÓ THỂ null (scene cũ không vỡ) và code nối chúng vào ITutorialStore (SetEnabled và ClearAll). "Xem lại hướng dẫn" chỉ xóa cờ gợi ý local của playerId hiện tại.
- Component NPC hiện có: CHỈ thêm đăng ký vào một danh sách static (OnEnable thêm, OnDisable bỏ, reset an toàn khi tắt Domain Reload) NẾU chưa có lookup NPC theo npcId. Nếu đã có thì dùng nó và không sửa gì.
- Panel báo boss P8-5: CHỈ thêm quyền đọc trạng thái (property hay event nhỏ báo "đã đóng", "đã xác nhận") NẾU chưa có. KHÔNG đổi chuỗi chữ, bố cục, thứ tự hay luồng báo server.
- docs/05 mục Phase 9.9 (ghi chú kỹ thuật, KHÔNG điền evidence)
Chỉ đọc: mọi thứ còn lại, kể cả GameEvents (KHÔNG thêm event), QuestRules, PlayerQuestLog, EnemyController, TechniqueController, DodgeController, PlayerInventory.

PHẦN A — GỢI Ý MỘT LẦN
A1. TutorialRules (pure): (a) mỗi hintId chỉ hiện một lần cho mỗi playerId; chỉ được đánh dấu "đã xem" khi toast THẬT SỰ hiện; (b) công tắc tắt thì không hiện và không đánh dấu; (c) hàng đợi sắp theo priority rồi thời điểm, không trùng hintId, tối đa 2 phần tử chờ; (d) chỉ một toast tại một thời điểm, cách nhau tối thiểu 1.5 s; (e) khi đang bị chặn (Pause, người chơi chết, bảng báo boss đang mở, chưa có playerId) thì giữ chờ với thời hạn 10 s rồi bỏ, KHÔNG đánh dấu đã xem; (f) hint có completeKind thì tự ẩn sớm khi người chơi đã làm việc đó (đánh dấu đã xem), nếu không thì ẩn sau displaySeconds. Mọi hằng số nằm trong một ScriptableObject cấu hình hoặc field có tên, không magic number.
A2. ITutorialStore + PlayerPrefsTutorialStore: HasSeen/MarkSeen/ClearAll theo playerId, khóa dạng "tutorial.{playerId}.{hintId}" (dùng danh mục hint để ClearAll chỉ xóa khóa của gói này, KHÔNG xóa khóa khác trong PlayerPrefs như settings.audio.*); cờ bật/tắt (GetEnabled/SetEnabled, mặc định bật) do chính store này giữ bằng khóa settings.tutorial_hints để KHÔNG đổi hợp đồng ISettingsStore/PlayerSettings của P9.6 (đọc ISettingsStore thật; chỉ khi nó hỗ trợ khóa bool tùy ý thì mới dùng chung, và ghi lựa chọn vào Handoff). Mọi truy cập PlayerPrefs bọc try/catch an toàn.
A3. TutorialToastPresenter (UI/Guidance): hiện một toast chữ ngắn (uGUI Text, CanvasGroup mờ vào/ra) ở vị trí không che nút điều khiển, không chặn cảm ứng (raycastTarget tắt), dùng thời gian unscaled. Tách rõ khỏi bảng báo boss: không dùng cùng đối tượng, không sửa chữ của bảng đó.
A4. TutorialTriggerRouter + TutorialSensors (UI/Guidance): chuyển sự kiện thật thành hint, KHÔNG polling mỗi frame (cảm biến chạy ≤ 4 lần/giây, cache tham chiếu người chơi lúc bật). Bảng hint (id gợi ý → nguồn tín hiệu thật → hoàn thành khi):
   - hint.move → người chơi sẵn sàng và công tắc bật → hoàn thành khi người chơi di chuyển liên tục khoảng 0.3 s
   - hint.attack → lần đầu người chơi RA KHỎI Safe Zone (cảm biến SafeZoneProvider.IsSafeZoneAt, bắt cạnh vào/ra) → hoàn thành ở OnHitResolved đầu tiên
   - hint.dodge → event WindupStarted đầu tiên của quái đang nhắm người chơi → hoàn thành khi có event né (nếu DodgeController có; nếu không thì tự ẩn)
   - hint.technique → tín hiệu mở khóa kỹ thuật THẬT (đọc code; nếu không có tín hiệu nào lấy được mà không sửa file ngoài path thì BỎ hint này và ghi vào Handoff) → hoàn thành ở OnTechniqueStarted
   - hint.potion → HP người chơi ≤ ngưỡng cấu hình (đề xuất 35 phần trăm, lọc OnHealthChanged theo máu của người chơi như PlayerHUD) → hoàn thành khi dùng bình, hoặc tự ẩn
   - hint.safe_zone → lần đầu quay VÀO Safe Zone sau khi đã ra ngoài (cạnh vào) → tự ẩn
   - hint.pickup → OnItemAcquiredWithPayload đầu tiên → tự ẩn
   - hint.equip → lần đầu nhận một món đồ mặc được (lấy loại item từ payload/definition, KHÔNG hard-code id) → hoàn thành khi mặc, hoặc tự ẩn
   - hint.floor_select → boss bị hạ VÀ bảng báo boss P8-5 đã đóng VÀ trạng thái báo cáo là "đã xác nhận" (nếu không xác định được trạng thái thì không phát, ghi Known issues) → tự ẩn
   Chữ gợi ý (do dự án tự đặt, giọng hệ thống, tối đa 70 ký tự, không tên tác phẩm có bản quyền); product owner duyệt trước khi chốt, nằm trong asset do AI nối Unity tạo từ Handoff của bạn:
   move "Chạm và kéo ở nửa trái màn hình để di chuyển." · attack "Bấm nút Đánh để tấn công quái." · dodge "Quái nhấp nháy đỏ là sắp ra đòn. Lướt ra khỏi đường đòn." · technique "Kỹ thuật vũ khí đã mở khóa. Bấm nút Kỹ thuật khi sẵn sàng." · potion "HP thấp. Thoát khỏi giao chiến rồi bấm Bình." · safe_zone "Khu an toàn: quái không đánh được bạn ở đây." · pickup "Bạn nhặt được một món đồ. Mở Túi để xem." · equip "Mở Túi để mặc trang bị." · floor_select "Tầng kế đã mở. Vào Khu an toàn và dùng Chọn Tầng." (kiểm lại từng câu với luật hiện hành ở docs/02, 04, 07; câu nào lệch thì sửa và ghi vào Handoff).
A5. Settings (xem "Sửa có giới hạn"): công tắc "Gợi ý hướng dẫn" bật/tắt có hiệu lực ngay (tắt thì toast đang hiện ẩn đi); nút "Xem lại hướng dẫn" xóa cờ local và hiện thông báo ngắn "Đã đặt lại gợi ý"; cả hai chỉ chạm cờ local.

PHẦN B — MŨI TÊN QUEST
B1. QuestTargetResolver (UI/Quest) + QuestTargetRules (pure): chọn quest cần chỉ (quest Active đầu tiên theo thứ tự PlayerQuestLog; ghi lựa chọn vào Handoff), rồi mục tiêu theo loại objective: Talk → NPC theo npcId (lookup NPC); Reach → tâm bounds của ZoneVolume có zoneId trùng; Kill hoặc Collect → tâm Vùng Farm gần nhất của tầng hiện tại (ZoneVolume kind Farm); quest hoàn thành mà cần trả ở NPC thì chỉ tới NPC trả quest (đọc QuestRules), tự hoàn thành thì ẩn. Resolve một lần khi quest đổi, khi tiến độ đổi và khi tầng kích hoạt (cache vị trí đích); KHÔNG Find mỗi frame.
B2. QuestArrow (UI/Quest) + QuestArrowMath (pure): mũi tên (Image xoay) bám mép màn hình theo hướng tới mục tiêu, tôn trọng Screen.safeArea và lề cấu hình; ẨN khi mục tiêu đang nằm trong khung nhìn (cộng lề), khi người chơi đã tới gần (bán kính cấu hình), khi Pause, khi người chơi chết, khi không có quest hay đích. Mũi tên không chặn cảm ứng. Đúng ở 16:9 và 20:9. Không có số khoảng cách bắt buộc.
B3. Không mini-map, không bắt buộc làm theo.

KHÔNG LÀM
Mini-map; khóa input bắt buộc làm theo; video hướng dẫn; popup chặn; nút Reset Guest hay xóa tài khoản; đổi chuỗi hay luồng bảng báo boss P8-5; thêm event vào GameEvents; sửa QuestRules, PlayerQuestLog, EnemyController, TechniqueController, DodgeController, PlayerInventory; lưu cờ gợi ý lên server hay Player Data; hint mới ngoài bảng A4 (đề xuất thêm thì ghi vào Handoff).

TEST BẮT BUỘC (EditMode)
TutorialRulesTests (một lần mỗi hint và playerId; tắt công tắc không đánh dấu; hàng đợi theo priority và không trùng; một toast một lúc và khoảng cách tối thiểu; bị chặn thì giữ chờ rồi bỏ khi quá hạn mà không đánh dấu; hoàn thành sớm đánh dấu đã xem) · PlayerPrefsTutorialStoreTests (hai playerId độc lập; ClearAll chỉ xóa khóa tutorial, không xóa settings.audio.*; công tắc mặc định bật; không ném lỗi khi PlayerPrefs lỗi) · TutorialTriggerRouter test thuần: bắt cạnh Safe Zone vào/ra đúng một lần, ngưỡng HP thấp có trễ để không bắn lặp, floor_select chỉ phát khi bảng boss đã đóng và đã xác nhận, hint.technique/dodge/equip theo tín hiệu đã chọn · test khóa chuỗi bảng báo boss "Máy chủ đã ghi nhận. Tầng kế đã mở." (nếu là hằng hay field trong code; chỉ khóa, không sửa) · QuestArrowMathTests (mục tiêu trong khung → ẩn; ngoài khung → góc đúng và vị trí kẹp mép có lề; gần → ẩn; không NaN; 16:9 và 20:9; safe area) · QuestTargetRulesTests (Talk → npcId; Reach → zoneId; Kill/Collect → Vùng Farm gần nhất; không quest → null; quest cần trả ở NPC) · test Settings: công tắc có hiệu lực ngay, "Xem lại hướng dẫn" chỉ xóa cờ tutorial · FloorBranchingScanTests xanh; toàn bộ test EditMode hiện có xanh.

EVIDENCE TRÊN MÁY THẬT
Bảng P9.9 trong docs/05 (case a–f). Bạn chỉ chuẩn bị "Cách test" (gồm cách chạy thử bằng Guest mới); không tự điền kết quả.
````

### U5 — Prompt cho AI nối Unity

````text
BẠN LÀ AI NỐI UNITY (có công cụ điều khiển Unity Editor). Việc của bạn: làm ĐÚNG danh sách "Việc cần làm trong Unity Editor" trong báo cáo/Handoff của Kiro cho package P9.9, cộng các yêu cầu bổ sung bên dưới. Danh sách của Kiro là nguồn chính cho tên class, field, đường dẫn; phần dưới đây là các giá trị và ràng buộc cố định của product owner.

KHÔNG sửa file C#, asmdef hay docs. Thiếu class hay field so với danh sách: dừng và báo lại, đừng tự viết code. Không xóa asset hiện có. Không đổi Player Settings hay Build Settings trừ khi bước yêu cầu. Không đổi thứ nằm ngoài danh sách.

CÁCH LÀM
1. Kiểm tra Console sạch lỗi sau khi Unity biên dịch xong; nếu có lỗi biên dịch thì dừng và báo.
2. Tạo/gán asset, prefab, scene object đúng tên và đường dẫn; dùng công cụ sửa prefab/scene của Unity (không sửa tay YAML).
3. Lưu scene và prefab đã đổi. Ghi lại từng thay đổi: đường dẫn + field + giá trị.
4. Chạy toàn bộ test EditMode, báo số pass/fail.
5. Vào Play Mode kiểm nhanh các điểm ghi ở "Kiểm nhanh" (chụp ảnh Game view và đọc Console). Ghi rõ điều đã kiểm và điều CHƯA kiểm được; việc cần máy Android thật thì để lại cho product owner.
6. Tắt Play Mode, kiểm Console lần cuối, lưu tất cả.

BỐI CẢNH CỐ ĐỊNH
- Unity 6000.3.24f1, URP 2D, Android landscape, uGUI (Canvas) với Text; tag Player; không bật "Enter Play Mode Options".
- Tên scene lấy từ Build Settings (đừng đoán). Floor 1 nằm sẵn trong scene gameplay chính; Floor 2 nạp additive.
- Asset hiện có để tái dùng: Enemy.prefab (một prefab dùng cho mọi loại quái), Boss_Floor01, Enemy_Bat/BoilerGuard/PressureWarden/Slime, NPC_Cartographer/Keeper, FloorDefinition_Floor01/02, DodgeDefinition.
- BẢN QUYỀN (AGENTS.md luật 7): không dùng tên hay asset của tác phẩm có bản quyền. Asset placeholder phải có nguồn ghi trong ASSET_SOURCES.md.

BÁO CÁO CUỐI: danh sách thay đổi (đường dẫn + field + giá trị), kết quả test EditMode, tình trạng Console, ảnh chụp (nếu có), và danh sách việc phải kiểm trên điện thoại.

=== UNITY — nối P9.9 (Hướng dẫn và Mũi tên quest) ===

ĐIỀU KIỆN: Kiro đã báo xong P9.9 (Phần A bắt buộc; Phần B nếu chưa cắt); bước nối P9.6 (Settings) và P9.7a (vùng) đã xong. Console sạch, test EditMode xanh.
Nguồn tên class/field/đường dẫn VÀ CHỮ GỢI Ý CHỐT: Handoff của Kiro (chữ đã được product owner duyệt). Không tự viết lại chữ.

VIỆC CẦN LÀM
PHẦN A — Gợi ý
1. Tạo TutorialHintDefinition asset (đường dẫn theo Handoff, gợi ý Assets/_Game/Data/Guidance/Hints/) cho từng hint trong bảng Kiro (hint.move, hint.attack, hint.dodge, hint.technique nếu Kiro không bỏ, hint.potion, hint.safe_zone, hint.pickup, hint.equip, hint.floor_select): id, chữ, speakerLabel, priority, displaySeconds, trigger, completeKind đúng như Handoff. Nếu có asset danh mục hay cấu hình (catalog, TutorialConfig) thì tạo và gán đủ.
2. Toast (TutorialToastPresenter): dựng trong Canvas HUD (hoặc Canvas riêng theo Handoff), Text uGUI + CanvasGroup; đặt ở phía trên giữa, DƯỚI vị trí ZoneBanner ít nhất 80 px reference và không che nút điều khiển; chiều rộng tối đa khoảng 50 phần trăm màn hình; raycastTarget TẮT. Sorting order THẤP hơn Pause, Death Screen và bảng báo boss P8-5 (toast hiện sau bảng, không đè lên và không đổi chữ của bảng).
3. TutorialTriggerRouter và TutorialSensors: đặt vào đối tượng điều phối trong scene gameplay, gán mọi tham chiếu (người chơi, presenter, store, catalog, bảng báo boss) theo Handoff.
4. Settings: gán hai field mới trên SettingsPanel (tạo ở bước P9.6): Toggle "Gợi ý hướng dẫn" và Button "Xem lại hướng dẫn", đặt trong bố cục Settings, nhãn tiếng Việt đúng như vậy. Không thêm nút Reset Guest hay xóa tài khoản.
PHẦN B — Mũi tên quest (nếu Kiro đã làm)
5. Sprite mũi tên: tạo bằng code một sprite tam giác/mũi tên 64×64 (Texture2D → PNG) lưu Assets/_Game/Art/Placeholder/QuestArrow.png, import dạng Sprite (Point filter nếu phong cách pixel, nén None), ghi vào Assets/_Game/Art/ASSET_SOURCES.md (tạo nếu chưa có): "QuestArrow.png — tự sinh bằng code, thuộc dự án". Không dùng hình có bản quyền.
6. QuestArrow trên Canvas HUD: Image mũi tên (xoay quanh tâm), raycastTarget TẮT, khởi đầu ẩn; gán QuestTargetResolver, camera, PlayerQuestLog và mọi field theo Handoff; kích thước khoảng 64×64 px ref, lề mép màn hình theo field Kiro đặt (mặc định đủ để không nằm dưới Pause hay joystick).
7. Không tạo quest mới. Reach chỉ kiểm bằng test EditMode ở gói này.
8. Lưu scene, prefab, asset; ghi lại từng thay đổi (đường dẫn + field + giá trị).

KIỂM NHANH (Play Mode; chụp ảnh Game view; đọc Console)
Trước khi bắt đầu, xóa cờ gợi ý local bằng cách gọi ITutorialStore.ClearAll cho playerId hiện tại (qua eval) — KHÔNG xóa toàn bộ PlayerPrefs.
- Vào game: hint.move hiện, biến mất khi người chơi di chuyển liên tục.
- Bước RA khỏi Safe Zone lần đầu: hint.attack; đánh quái thì hint ẩn.
- Có quái báo đòn lần đầu: hint.dodge (chữ nói "lướt ra khỏi đường đòn", không nói bất tử).
- Hạ HP người chơi dưới ngưỡng (ví dụ nhận đòn thật hoặc dùng eval đặt HP): hint.potion nói đúng "thoát khỏi giao chiến rồi bấm Bình".
- Nhặt đồ: hint.pickup; món mặc được: hint.equip.
- Quay lại Safe Zone sau khi đã ra ngoài: hint.safe_zone.
- Chỉ MỘT toast tại một thời điểm, cách nhau tối thiểu 1.5 giây; mở Pause hay chết thì toast không đè lên các màn đó.
- Dừng Play rồi vào lại: gợi ý đã hiện KHÔNG hiện lại. Bấm "Xem lại hướng dẫn" trong Settings: hiện lại được. Tắt "Gợi ý hướng dẫn": không có toast nào hiện. Kiểm giá trị âm lượng trong Settings KHÔNG bị xóa sau "Xem lại hướng dẫn".
- Hạ boss (nếu Editor có Guest và mạng): bảng P8-5 hiện đúng chữ "Máy chủ đã ghi nhận. Tầng kế đã mở."; hint.floor_select chỉ hiện SAU khi bảng đóng. Nếu không kiểm được thì ghi "chưa kiểm".
- Mũi tên quest: nhận quest Talk thì mũi tên chỉ NPC, ẩn khi tới gần hay khi NPC đang nằm trong khung nhìn; nhận quest Kill/Collect thì chỉ tâm Vùng Farm; không quest thì ẩn; ẩn khi Pause hay chết. Chụp ở 1280×720, 1920×1080, 2400×1080: mũi tên không nằm dưới nút nào.
- Dừng Play: Console sạch, lưu tất cả.
- CHƯA kiểm được (product owner làm trên Android): bài test Guest mới 3 phút (P9.9 (a)), đóng mở app, cảm giác chữ gợi ý trên màn nhỏ.

KHÔNG LÀM
Không sửa C#; không đổi chữ hay luồng bảng báo boss P8-5; không tạo quest mới; không thêm hint ngoài danh sách; không xóa PlayerPrefs ngoài cờ gợi ý.
````

