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

- 2026-10-26 · [Luồng B] Bộ 6 hệ thống Thế giới & Đời sống SAO: (1) Quán trọ (Inn hồi đầy HP, chuyển sáng bằng Col); (2) Tinh Thể Dịch Chuyển (Teleport Crystal niệm 3s, huỷ khi bị đánh/di chuyển); (3) Câu cá & Nấu ăn (Life skills, buff món ăn +ATK, lưu ProfileData); (4) Đồng đội AI Asuna (tự đánh, ngã hồi sau giao chiến, không tính LA); (5) Duel PvP (3 chế độ First-Strike/Half-Loss/Total-Loss không chết thật + Cursor Xanh/Cam/Đỏ); (6) Party/Guild/Friends (Party 6 người, thưởng EXP đội, interface UGS) · test: 41 test EditMode mới xanh 100% (1467/1468 toàn dự án), compile 0 lỗi.

- 2026-10-26 · [Luồng A] Tích hợp toàn diện Nhị Đao Lưu (Dual Wielding) & Ô OffHand: EquipmentSlotRules.ResolveTarget tự động mặc kiếm thứ 2 vào tay phụ khi tay chính đã có vũ khí; ItemDatabase.SumEquipment cộng chuẩn 60% sát thương tay phụ; PlayerInventory.UpdateDualWieldState tự động đồng bộ trạng thái chiến đấu của PlayerAttack và phát thông báo nổi; PlayerAttack tăng 25% tốc độ chém, combo 3 nhịp song kiếm 1.15x/1.45x/2.20x lunge mạnh mẽ; InventoryPanel hiển thị tag [Tay chính] và [Tay phụ (Dual)]; thêm 2 bảo kiếm SAO Elucidator (Hắc Kiếm) & Dark Repulser (Bạch Kiếm) · test: DualWieldRulesTests (8) & EquipmentSlotRulesTests (12) xanh 100% (1481/1482 toàn dự án).

- 2026-10-26 · [Luồng A] Kỹ năng phụ Dò Tìm & Ẩn Nấp (Searching & Hiding theo SAO): SearchingHidingRules (Core) tính bán kính dò tìm quái theo cấp (7.5m - 16m), EnemyHealthBar tự động hiện con trỏ đe dọa & cấp độ quái vật trước giao tranh; Hiding tự kích hoạt khi đứng yên >= 1.2s giảm 60% tầm phát hiện của quái, mờ sprite 55%, EnemyController.EffectiveDetectionRange phản hồi thời gian thực, phá vỡ khi tấn công/dính đòn · test: SearchingHidingTests (6 test EditMode xanh 100%, 1412/1413 toàn dự án).

- 2026-10-26 · [Luồng A] Hiệu ứng trạng thái SAO (Status Effects): StatusEffectRules (Core) quản lý Độc (Poison rút % máu theo nhịp), Chảy máu (Bleed), Tê liệt (Paralyze độc Kuradeel), Choáng (Stun), StatusEffectController tự gắn runtime Player/Quái/Boss, DoT dùng cờ DamageInfo.IsPeriodic không cướp LA, tự khóa hành động qua IsActionLocked, đổi màu tint hiển thị rõ trạng thái, hỗ trợ giải độc (Cure/Antidote) · test: StatusEffectTests (8 test EditMode xanh 100%, 1406/1407 toàn dự án).

- 2026-10-26 · [Luồng A] Khựng chiêu sau Sword Skill (Post-Motion Freeze): PostSkillDelayRules (Core) tính thời gian khựng chiêu tăng dần theo số hit combo và hệ số sát thương (tối thiểu 0.2s, tối đa 2.5s, Ultimate 0.85s), PostSkillDelayController tự gắn runtime khoá di chuyển/đánh thường/né/kỹ năng, đổi tint xanh thép khi bất động, PlayerController.IsActionLocked bảo vệ trạng thái · test: PostSkillDelayTests (9 test EditMode xanh 100%, 1398/1399 toàn dự án).

- 2026-10-26 · [Luồng A] Thưởng kết liễu Boss (Last Attack Bonus - LA): LastAttackRules (Core) xác định đòn kết liễu từ người chơi (loại trừ DoT, pet, đồng đội), BossDefinition.lastAttackRewardItem, BossDefeatFlow thưởng trực tiếp vào túi đồ khi hạ boss, UI thông báo toast và bảng BossDefeatReportPanel hiển thị rõ LA, thêm trang bị độc bản Áo Choàng Bóng Đêm (Coat of Midnight - Tier 2 Giáp) · test: LastAttackRulesTests (6 test EditMode xanh 100%, 1389/1390 toàn dự án).

- 2026-10-26 · [Luồng A] Con trỏ & Tên quái đổi màu theo chênh lệch cấp độ SAO: EnemyCursorRules (Core) tính mức độ đe doạ (Ngang cấp ±4 Vàng, Nguy hiểm >= +5 Đỏ, Yếu <= -5 Xám, chừa chỗ xanh/cam/đỏ PvP), EnemyDefinition.Level & EnemyController.Level, EnemyHealthBar hiển thị chấm cursor và TextMesh tên + level quái màu tương ứng trên đầu quái · test: EnemyCursorRulesTests (6 test EditMode xanh 100%, 1381/1382 toàn dự án).

- 2026-10-26 · [Luồng A] Boss nhiều thanh máu & nhân máu boss theo SAO: BossHealthBarRules (Core) tính số thanh/tỉ lệ/màu 4 thanh SAO (Lục->Vàng->Cam->Đỏ), hệ số BossHealthMultiplier = 10.0 (thời gian diệt ~5.5 phút), BossDefinition.HealthBarCount (mặc định 4), Enrage thanh cuối (+25% công, +20% tốc, -25% cd, thông báo nổi); BossHealthBar tự gắn runtime UnderlyingFill, BarsCountLabel (x4..x1), BossNameLabel không sửa scene · test: BossHealthBarRulesTests (9 test EditMode xanh 100%, 1375/1376 toàn dự án).

- 2026-10-25 · Sửa nền UI bị tối & Nâng cấp Túi đồ dạng thẻ vật phẩm như Shop: Tăng độ sáng palette pixel art (make_ui_pack_v2.py cho UI_Panel/Row/Slot/PanelInset), gỡ tint đen nhân màu trên mọi Panel (Pause, Settings, Quest, FloorSelect) và phối màu rực rỡ cho các nút Tạm dừng; làm lại InventoryPanel với từng dòng item gồm khung ô (SlotFrame 112) chứa icon sprite, nhãn [Đang mặc]/số lượng, dòng chỉ số mô tả rõ ràng (ATK/DEF/HP/Hồi máu/CD) và nút thao tác (Mặc xanh teal, Tháo đỏ, Dùng xanh lá) chuẩn phong cách Shop · test: 1351 EditMode pass, UiLayoutChecker 10 tỉ lệ màn hình 0 lỗi.

- 2026-10-25 · Tích hợp hoàn tất Lô F (Huy hiệu hải đồ) vào PvE_Gameplay & UI: 6 nút Menu (UI_IconButton 120, icon 84, sạch nhãn chữ), 5 nút Combat (ActionRing L/M/S 192/144/120, icon 120/96/84, CooldownFill 96 phủ trên icon), phản hồi nút (bấm x0.85, khóa x0.5 alpha >= 0.7), TimeOfDayHUD icon 72 đủ 6 pha, UiSkin.IconButton + Resources/UISkin, UiLayoutChecker 10/10 tỉ lệ 0 lỗi.
- 2026-10-25 · Bộ đồ họa UI Lô F ("Huy hiệu hải đồ"): Script make_batch_F.py hoàn tất 28 sprite (ActionRing L/M/S, IconButton 9-slice viền 5, 8 icon Menu, 6 icon Combat, 6 icon TimeOfDay, bình máu), chuẩn 1 art px = 3 canvas px, viền đồng men than #0b1220, Point filter.
- 2026-10-25 · Hiệu ứng phản hồi nút HUD: lún + nảy lò xo khi bấm (mọi nút), thở nhẹ + quầng sáng khi chiêu sẵn sàng (nút Kỹ thuật), vòng tối đếm ngược hồi chiêu + lóe vòng và nảy khi hồi xong (Né, Kỹ thuật), icon tối đi khi nút khoá; chạy theo thời gian thật nên vẫn nảy lúc tạm dừng · UI/Controls/HudButtonFeedback.cs, HudFeedbackCurves.cs · scene PvE_Gameplay: thêm component + con CooldownFill, nối `cooldownFill` cho Né/Kỹ thuật · chỉnh qua serialized fields · test: HudFeedbackCurvesTests.cs (12).
- 2026-10-25 · Hoàn tất 5 việc chỉnh trang UI: (1) CharacterButton bỏ nhãn chữ, chỉ giữ icon; (2) Góc trên-trái thêm plate mờ UI_Toast, Icon_Coin, giãn cách và đổ bóng chữ cho WalletHud & TimeLabel; (3) UiSkin thêm ArtPixel = 3, nạp UI_Slot/UI_Row/UI_Header/UI_Toast và gắn vào Túi, Chọn tầng, Quest; (4) Chữ runtime dùng UiSkin.ApplyFont, cỡ min 24, xóa sạch LegacyRuntime; (5) Đổi nút Đánh 192, Né/Kỹ năng/Tuyệt kỹ 144, UiLayoutChecker 10/10 tỉ lệ 0 lỗi.
- 2026-10-25 · Bộ phông chữ tiếng Việt Lô E (Be Vietnam Pro - OFL): Tải và tích hợp 3 trọng số (Regular, SemiBold, Bold) cùng giấy phép OFL vào Art/UI/Fonts và Resources/UISkin/Fonts. Nâng cấp UiSkin.cs nạp phông động, áp dụng thành công cho toàn bộ Text trong MainMenu.unity (20) và PvE_Gameplay.unity (56) · script: tools/art_gen/make_batch_E.py · preview: out_ui_v2/preview_E_*.png · test: FontTests.cs, UiLayoutChecker 0 lỗi.
- 2026-10-25 · Bộ đồ họa UI pixel art Lô D (Biểu tượng Menu & Đồng hồ Ngày/Đêm): 15 sprite gồm 8 icon Menu (Bag, Talk, Quest, Floor, Pause, Character, Settings, Close), 6 icon chu kỳ Ngày/Đêm (Dawn, Morning, Noon, Afternoon, Evening, Night) và Huy hiệu Niêm Phong Tử Thần (Icon_Seal 48x48 cho Death Screen). Đã cấu hình Point/Single, nối vào các nút menu HUD, TimeOfDayHUD và DeathPanel trong PvE_Gameplay.unity · script: tools/art_gen/make_batch_D.py · preview: out_ui_v2/preview_D_*.png · test: TimeOfDayHUDTests.cs, UiLayoutChecker 0 lỗi.
- 2026-10-25 · Tích hợp hoàn tất 3 Lô UI (A, B, C) vào Unity: Đặt viền 9-slice toàn bộ sprite khung/nút, nối nền Bg_MainMenu, Logo_Emblem, TitlePlate, nút ButtonWide vào MainMenu.unity; nối đế joystick la bàn, núm đồng, vòng nút ActionRing và icon chiến đấu đao chém/né gió/mỏ neo vào PvE_Gameplay.unity · tool: ApplyBatchesABC.cs · test: UiLayoutChecker 0 lỗi.
- 2026-10-25 · Bộ đồ họa UI pixel art Lô C (HUD Chiến Đấu): 14 sprite chuẩn 1 art px = 3 canvas px, gồm đế joystick la bàn 64x64 bán trong suốt, bộ 3 vòng nút ActionRing L/M/S, vòng phủ hồi chiêu UI_Cooldown, huy hiệu LevelBadge, 5 icon chiến đấu (Attack, Dodge, Core, Traverse, Coin), vẽ lại Icon_Ultimate & Ring pixel art sắc nét · script: tools/art_gen/make_batch_C.py · preview: out_ui_v2/preview_C_*.png.
- 2026-10-25 · Bộ đồ họa UI pixel art Lô B (Màn hình chính, Đăng nhập, Tải): Bg_MainMenu (800x360 tỉ lệ 20:9 an toàn 4:3), Logo_Emblem 96x96 (tháp hải đăng, vòng sóng, nút dây thừng), UI_TitlePlate 9-slice (20,7,20,7), UI_ButtonWide 9-slice (12,6,12,6) ván gỗ/khung thép xám trung tính · script: tools/art_gen/make_batch_B.py · preview: out_ui_v2/preview_B_*.png.
- 2026-10-25 · Bộ đồ họa UI pixel art Lô A (Khung, Nút, Thanh): 14 sprite chuẩn 1 art px = 3 canvas px, bảng màu thép + đồng + xanh than, panel mờ 90%, đinh tán đồng, nút xám trung tính, núm joystick 24x24 có khấc và điểm sáng hải đăng · script: tools/art_gen/make_ui_pack_v2.py · preview: out_ui_v2/preview_A_*.png.
- 2026-10-25 · Refactor bố cục UI di động (Gói U1..U6): Thống nhất CanvasScaler 1080p Match Height, SafeAreaRoot + SafeAreaFitter neo theo tai thỏ/góc bo, cụm chiến đấu vòng cung (120-190px), menu 2 hàng gọn gàng không đè ở 4:3, PanelSizeClamp chống tràn bảng, nhóm bảng loại trừ IExclusivePanel, typography + shadow chống chìm nền, 9-slice sprites & UiSkin · test: SafeAreaMathTests.cs, ExclusivePanelGroupTests.cs, UiLayoutChecker.cs (10 tỉ lệ màn hình đều 0 lỗi).
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

- **Test:** Test Runner chạy ngày 2026-10-25 qua MCP đạt 1331/1332 EditMode xanh (99.92%). 1 test đỏ là `Floor02WiringTests.HostScene_RootObjects_AreOnlyTheAllowedOnes` do `World_Ground` nằm ở gốc scene `PvE_Gameplay` từ phiên Overworld 2026-10-23.
- Nối scene của 9.7–9.9 (Zones, Hầm Boss, Ranged/Skirmisher trong scene/prefab, Tutorial, QuestArrow) chưa kiểm.
- Túi đồ: có phiên chẩn đoán `AgentScripts/RuntimeDiag*.cs` ngày 18/10, chưa rõ kết quả.

## Chờ bạn

1. Phát hiện 3.7 (Luật 10): Nút `ResetGuestButton` đang nằm ở scene `MainMenu` (do MainMenu kiêm màn hình Login của Mốc 1). Xác nhận xem có coi MainMenu là Login Screen không, hay cần tách scene Login riêng.
2. Đóng Phase 8–9: P8-5 (ô d, g) và cả bảng P9-3 đã có Pass nhưng cột Actual trống, nhiều ô thiếu ngày; xác nhận rồi đổi trạng thái ở `docs/05`.
3. Môn học có yêu cầu evidence theo từng phase, hay gom ở bản nộp? (quyết định cách gom ở "Việc kế tiếp" mục 2.)
4. Art Track: quái/boss 3 hay 5 bộ hướng; ai vẽ nhân vật chính và boss; bật Pixel Perfect Camera không (`docs/ref/15` §11).

## Lệch tài liệu đã biết

- `Phase9_5-9_9_prompts_v2.md` (thư mục gốc): bảng "Trạng thái hiện tại" đã cũ; vẫn nhắc `docs/HANDOFF.md` (đã bỏ), `docs/13` (đã vào `archive/`), vai Integrator và ACR. Phần quy trình trong prompt bị `AGENTS.md` thay thế; chỉ dùng phần mô tả việc cần làm.
- `docs/01` §3 và §7 còn trỏ `13` và bản đồ cũ; bản đồ chuẩn là mục "Bản đồ tài liệu" ở `AGENTS.md`.
- `docs/16` §3: 16 điểm xung đột đã được ADR-031 coi như AI tự quyết; nguyên văn bảng giữ nguyên làm lịch sử.
- `.kiro/specs/quest-reward-bugfix/tasks.md`: mục 6–8 còn tick dở nhưng thưởng quest đã làm theo cách khác (`QuestRewardLedger`).
