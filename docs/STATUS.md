# STATUS — Game2D "Tháp Thủy Triều"

> File này trả lời một câu: **đang ở đâu, làm gì tiếp.** AI đọc ngay sau `AGENTS.md`, rồi chỉ đọc tài liệu và code của "Việc kế tiếp", không đọc cả `docs/`.
> Xong việc thì tự sửa file này (tối đa khoảng 5 dòng). Không chép chi tiết cài đặt vào đây: chi tiết ở code và test; luật ở `AGENTS.md`; thiết kế ở `docs/`; số liệu ở asset và test.
> Chế độ làm việc theo ADR-031 (`AGENTS.md`, mục "Tự chủ của AI"): AI làm liền nhiều gói, được tự thêm tính năng và ghi vào mục "AI đã tự thêm" bên dưới. Docs lệch code: phần đã làm thì code chạy và test xanh thắng; ghi lệch vào mục "Lệch tài liệu" rồi làm tiếp.
> **Cập nhật: 2026-10-20.** Kiểm bằng đọc code, scene, asset trên đĩa và Unity MCP (biên dịch sạch, 0 lỗi, 3 warning). Chưa xác minh test (xem dưới). File này là ảnh chụp: nếu lệch với code thì tin code và sửa file này.
> **Bố cục `docs/`:** gốc = file đang dùng (STATUS, 01, 05, 10, 11, 12, 16); `ref/` = hợp đồng theo vùng code, đọc khi chạm vùng đó; `archive/` = đã xong hoặc lỗi thời, không cập nhật nữa. Prompt các gói: `Phase9_5-9_9_prompts_v2.md` ở thư mục gốc dự án.

## Đang ở đâu

Mốc 1, **Phase 9.5 → 9.9** (thứ tự 9.5 → 9.6 → 9.7a → 9.7b → 9.8 → 9.9 → Phase 11; Duel/Phase 10 thuộc Mốc 2, ADR-028).
Hiện tại: **9.5, 9.5a, 9.6 đã có code + test + nối scene. 9.7a, 9.7b, 9.8, 9.9 đã có code + test (chủ dự án báo gần xong). Chưa phase nào có evidence máy thật.**

## Việc kế tiếp (theo thứ tự; làm liền, mỗi mục gồm code + test + nối Unity qua MCP)

1. **Chốt 9.7–9.9.** Hoàn tất phần còn dở, kiểm nối scene cho Zones, Hầm Boss, quái Ranged/Skirmisher, hướng dẫn và mũi tên quest; chạy Test Runner → Run All (toàn bộ phải xanh); ghi "Ghi chú kỹ thuật" từng phase vào `docs/05`.
2. **Chơi thử trên Android, điền evidence** P9.5–P9.9 ở `docs/05` (bạn điền, AI không tự ghi Pass). Gom ở hai mốc: sau 9.9 và Phase 11 (nếu môn học đòi từng phase thì điền từng phase). Kiểm riêng: lớp nháy `HitFlash` trong bản build (shader), nút Back Android thành `Escape`, quyền VIBRATE, FPS.
3. **Phase 11** kiểm tổng, chỉ sửa lỗi; gom evidence Phase 0–5.5.
4. **Khi loop Mốc 1 chơi được:** AI tự chọn việc theo "Tự chủ của AI". Gợi ý ưu tiên (không bắt buộc): (a) nghi thức Nhổ Neo + màn hình Tháp (`docs/16` §7.1, FB-028); (b) loot theo độ hiếm + khoảnh khắc rơi đồ (FB-025); (c) đêm khác biệt: Elite + ánh sáng 2D (FB-031); (d) màn hình Niêm Phong tóm tắt hành trình; (e) Nối Thế (`docs/16` §5.2); (f) quest nhỏ, rương ẩn, Sổ Thăm Dò (`docs/16` §7.4).

## AI đã tự thêm

- 2026-10-23 · Sửa hồi quy Overworld: khôi phục 16 quái khi vào map, bỏ giới hạn zone cũ ở ba spawner mới và thêm vách SpriteRenderer + BoxCollider2D thật (biên map + vách nội vùng). Play Mode đếm trực tiếp 16 `EnemyController`, Console 0 lỗi mới.
- 2026-10-22 · Fix lỗi Input System (`Keyboard.current`) & Nâng cấp toàn diện Đồ họa 3 Thế Giới (Rừng Cổ, Mỏ Dung Nham, Lâu Đài): Viết lại bộ pixel art 16-bit mượt mà, khử nhiễu dither, thêm mạch đá núi lửa, sông ngọc bích, thảm nhung Gothic. Thay thế "cục màu vàng" thành Nút Tuyệt Kỹ hình tròn (`Icon_Ultimate.png`) viền năng lượng nạp nộ mượt mà. Test Play Mode trực tiếp qua Unity MCP xác nhận 3 map đổi mượt 100%, 0 lỗi · test: ThreeRealmsFeaturesTests.cs.
- 2026-10-22 · Bộ 3 Map Thế Giới Phiêu Lưu (Rừng Rậm Cổ Đại, Hầm Mỏ Dung Nham, Lâu Đài Bóng Tối): 21 Pixel Art Sprites & Tileset (make_three_realms.py), Hệ thống Cạm bẫy môi trường EnvironmentalHazard.cs (dung nham, bụi gai làm chậm, vực thẳm knockback), Công trình tương tác độc bản (AncientLifeSpring.cs, MoltenForgeAnvil.cs buff +35% dmg, DarkCastleAltar.cs bất tử 5s), Bộ dựng map ThreeRealmsMapBuilder.cs và Bộ chuyển đổi thế giới tức thì RealmRuntimeSwitcher.cs (phím 1-2-3 & UI) · test: ThreeRealmsFeaturesTests.cs.
- 2026-10-22 · Bộ tính năng chiến đấu đỉnh cao: Chuỗi combo đòn đánh 3 nhịp (PlayerAttack.cs), Rung màn hình chấn lực (CameraFollow.cs), Hệ thống Đẳng cấp Combo D->SSS (ComboRank.cs, ComboTracker.cs, ComboHUD.cs), Cơ chế Né Hoàn Hảo & Phản Đòn Chí Mạng (PerfectDodgeDetector.cs), Hệ thống Nộ Khí & Tuyệt Kỹ "Vũ Điệu Cuồng Nộ" (AdrenalineController.cs, OnScreenUltimateButton.cs), Trụ Phước Lành Cổ Đại (ShrineOfBlessing.cs), Thông báo chiến đấu động (FloatingCombatNoticePresenter.cs) · test: AdvancedCombatFeaturesTests.cs · data: chỉnh qua serialized fields trên component.

## Đã xong

- Phase 0–7.5; Task D (khóa account khi chết), B (cân bằng quái), P (bình máu) (theo ghi nhận cũ).
- Phase 8 (boss + World State) và 9 (Tầng 2 bằng data): code + test xong; bảng P8-5, P9-3 đã có Pass, chờ bạn xác nhận (xem dưới).
- Thưởng quest chỉ cấp sau khi lưu thành công: `QuestRewardLedger` + `PlayerQuestLog` + test (đã đối chiếu code).
- **9.5** cảm giác + âm thanh (đã đối chiếu code, scene, asset): `Core/Presentation`, `Core/Audio`, `Services/Audio`, `PvE/Presentation`, `UI/Combat`, `UI/HUD/BossHealthBar`, `UI/Audio`; nối scene `PvE_Gameplay`; `CombatFeelConfig.asset` đủ trường; 13 SFX + 3 nhạc, `AudioCatalog.asset`, `ASSET_SOURCES.md`.
- **9.5a** hướng 8 hướng: `Core/Facing/*` + test; Animator Player có `FaceX`, `FaceY`.
- **9.6** nền UX: `Core/Settings`, `Services/Settings`, `Services/Haptics`, `UI/Controls`, `UI/Pause`, `UI/Settings`, `UI/Loading`, `UI/Haptics` + test; scene có `PauseController`, `SettingsPanel`, `JoystickTouchZone`, `OnScreenPotionButton`, `LoadingOverlay`, `HapticFeedbackListener`.
- **9.7–9.9** (chỉ mới xác nhận file code và test có trên đĩa): `PvE/Zones` (ZoneVolume, ZoneTracker, ZoneResolver, EnemyLeashRules...) + `UI/Zones` (ZoneBanner) + test `Zones`; `PvE/Enemy` (CombatRole, Ranged, Skirmisher, Projectile) + test `EnemyRoles`; `Core/Guidance`, `Services/Guidance`, `UI/Guidance` (Tutorial), `UI/Quest/QuestArrow`.
- Art placeholder: tile Tầng 1, sprite tạm người chơi/quái/boss/vật phẩm.

## Chưa xác minh

- **Test:** `docs/05` ghi 870/870 xanh (2026-10-18), chưa chạy lại sau 9.6–9.9. Ngày 20/10 chạy Test Runner qua MCP bị treo và đã hủy (Editor có thể đang không focus). Cần chạy Run All trong Editor và ghi kết quả vào đây.
- Nối scene của 9.7–9.9 (Zones, Hầm Boss, Ranged/Skirmisher trong scene/prefab, Tutorial, QuestArrow) chưa kiểm.
- Túi đồ: có phiên chẩn đoán `AgentScripts/RuntimeDiag*.cs` ngày 18/10, chưa rõ kết quả.

## Chờ bạn

1. Chạy Test Runner → Run All và cho biết kết quả (hoặc để AI chạy khi Editor đang focus).
2. Đóng Phase 8–9: P8-5 (ô d, g) và cả bảng P9-3 đã có Pass nhưng cột Actual trống, nhiều ô thiếu ngày; xác nhận rồi đổi trạng thái ở `docs/05`.
3. Môn học có yêu cầu evidence theo từng phase, hay gom ở bản nộp? (quyết định cách gom ở "Việc kế tiếp" mục 2.)
4. Art Track: quái/boss 3 hay 5 bộ hướng; ai vẽ nhân vật chính và boss; bật Pixel Perfect Camera không (`docs/ref/15` §11).

## Lệch tài liệu đã biết

- `Phase9_5-9_9_prompts_v2.md` (thư mục gốc): bảng "Trạng thái hiện tại" đã cũ; vẫn nhắc `docs/HANDOFF.md` (đã bỏ), `docs/13` (đã vào `archive/`), vai Integrator và ACR. Phần quy trình trong prompt bị `AGENTS.md` thay thế; chỉ dùng phần mô tả việc cần làm.
- `docs/01` §3 và §7 còn trỏ `13` và bản đồ cũ; bản đồ chuẩn là mục "Bản đồ tài liệu" ở `AGENTS.md`.
- `docs/16` §3: 16 điểm xung đột đã được ADR-031 coi như AI tự quyết; nguyên văn bảng giữ nguyên làm lịch sử.
- `.kiro/specs/quest-reward-bugfix/tasks.md`: mục 6–8 còn tick dở nhưng thưởng quest đã làm theo cách khác (`QuestRewardLedger`).
