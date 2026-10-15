# FEATURE BACKLOG
## Game2D — Tính năng phát hiện được, chưa có trong Roadmap

> Docs 05 là scope TỐI THIỂU mỗi phase. File này ghi tính năng phát sinh khi test.
> KHÔNG tự thêm vào code. Chỉ ghi lại → quyết định sau.
> **Cập nhật 2026-10-14 (product owner):** duyệt Phase 9.5–9.9 (xem `05`). Các mục FB-001, 002, 005, 006, 009, 010, 011 chuyển vào Phase 9.6; thêm FB-021 đến FB-031. Mục còn "Chờ quyết" là đề xuất, chưa phải quyết định.

---

## 1. NGUYÊN TẮC

- **AI không tự sinh tính năng.** Chỉ được phép đề xuất, ghi vào đây, chờ quyết định.
- **Bạn là product owner.** Bạn quyết cái gì làm, cái gì không.
- **Phát hiện = ghi ngay.** Không cần quyết định luôn, chỉ cần không quên.
- **Cuối mỗi phase:** đọc file này, quyết định thêm gì vào phase tiếp.

---

## 2. PHÁT HIỆN KHI TEST — CẦN QUYẾT ĐỊNH

| ID | Tính năng | Phát hiện ở | Đề xuất cho phase | Trạng thái |
|---|---|---|---|---|
| FB-001 | Settings screen (âm lượng, rung, độ nhạy joystick) | Phase 2 (test joystick trên máy) | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 (bản tối thiểu; không có nút Reset Guest, AGENTS.md luật 10) |
| FB-002 | Pause menu (resume, settings, về MainMenu) | Phase 2 (app không có cách thoát giữa chừng) | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 (kèm nút Back của Android) |
| FB-003 | Save conflict UI (2 thiết bị cùng account) | Chưa — dự đoán | Phase 11 (Regression) | Chờ quyết |
| FB-004 | Confirm popup khi bấm "Tạo tài khoản mới" trên Death Screen | Phase 3 (nhận ra rủi ro lỡ tay) | Phase 5.5 (Death System) | Đã quyết (09 §1.4) — chờ evidence |
| FB-005 | Joystick floating (chạm bất kỳ đâu nửa trái màn hình) | Phase 3 (nhận ra joystick cố định phản ứng chậm) | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 |
| FB-006 | Dead zone cho joystick (tránh run tay) | Phase 3 | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 |
| FB-007 | Nút Dash/Né riêng | Phase 3 (game permadeath cần né đòn) | Wave 2A (ADR-025) | Đã quyết |
| FB-008 | Layout nút Technique theo slot Core/Traverse/Control/Signature | Phase 3 (dự đoán Phase 7.5 cần) | Phase 7.5 (Blade Technique) | Chờ quyết |
| FB-009 | Nút Item/Potion | Phase 3 (dự đoán Phase 5 cần) | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 (theo `ConsumableRules`: chỉ dùng ngoài giao chiến, `07` §2) |
| FB-010 | Haptic feedback (rung khi trúng đòn, HP thấp) | Phase 3 | Phase 9.6 (Nền UX) | Đã quyết 2026-10-14 |
| FB-011 | Test "chết oan" trên máy thật (đếm số lần chết do control) | Phase 3 (nhận ra rủi ro với permadeath) | Phase 9.6 (bài test cổng qua) | Đã quyết 2026-10-14 |
| FB-012 | Nút cảm ứng cho Nói chuyện / Túi / Sổ quest (Android không có bàn phím) | Phase 6 (test máy thật: chỉ có phím E/Q/1-9) | Phase 6.5 | Đã làm (PvEActionButton + nút trên Canvas) |
| FB-013 | Quái có windup/telegraph trước khi ra đòn (tránh "chết oan" vì permadeath) | Phase 6 (audit code) | Phase 6 | Đã làm (attackWindupSeconds 0.35s, đổi màu báo trước) |
| FB-014 | Quái hồi sinh tức thì mỗi lần đổi tầng → có thể farm XP bằng cách đi qua lại giữa hai tầng (gate Safe Zone chỉ giảm bớt). Nếu thấy lạm dụng: thêm thời gian chờ giữa hai lần chuyển tầng. | Phase 9 (review P9-1) | Phase 11 (Regression) | Chờ quyết |
| FB-015 | LootDropper tạo ItemPickup ở gốc scene đang active (không gắn vào tầng) → vật phẩm rơi không bị dọn khi tầng tắt/unload, tích lũy trong scene chính. Hướng: gắn pickup vào nội dung tầng hoặc cho tự biến mất sau một thời gian. | Phase 9 (review P9-1) | Phase 11 (Regression) | Chờ quyết |
| FB-016 | CameraFollow.useBounds đang tắt (không code nào bật). Nếu sau này bật giới hạn camera thì bounds phải theo từng tầng (hiện chỉ có một bộ minBounds/maxBounds). | Phase 9 (review P9-1) | Khi bật giới hạn camera | Chờ quyết |
| FB-017 | Game chưa khôi phục tầng đang đứng khi mở lại app: ProgressData.currentFloorId chưa được dùng, mỗi lần mở luôn bắt đầu ở Tầng 1. Lưu ý: từ P9-1d, `personalHighestFloor` (tầng cao nhất đã vào) ĐÃ được theo dõi và lưu ở cùng key "progress" (`PersonalProgress`, cập nhật qua `FloorLoader`) và `PersonalProgress` giữ nguyên `currentFloorId` khi lưu; chỉ `currentFloorId` là còn chưa dùng, nên khi làm mục này không cần đổi cách lưu tầng cao nhất. | Phase 9 (review P9-1) | Phase 11 (Regression) | Chờ quyết |
| FB-018 | Nút Chọn Tầng chỉ báo lỗi sau khi bấm khi đứng ngoài Safe Zone; có thể làm mờ nút hoặc hiện gợi ý "cần ở Vùng An Toàn" ngay trên nút. | Phase 9 (P9-1b) | Phase 11 (Regression) | Chờ quyết |
| FB-019 | Boss rematch không cho EXP (docs/02 §24). "Rematch" theo định nghĩa của thế giới (boss đã Defeated trong World State) nên tài khoản vào sau không nhận EXP boss ở lần hạ đầu của chính họ (vẫn nhận first-clear và đồ chính). Nếu muốn mỗi tài khoản nhận EXP boss một lần: tính theo RewardClaims FirstClear thay vì cờ World State. | Phase 9 (P9-1c) | — | Đã quyết (P9-1c) — hệ quả cần theo dõi |
| FB-020 | Số cân bằng Tầng 2 sát biên khoảng mục tiêu docs/07 (bảng kết quả P9-3, `07` §9): Guard Lv 6 = 7 đòn hạ Guard (tối đa 7), 10 đòn hạ người (tối thiểu 10), 8 Guard mỗi level (tối thiểu 8); Elite Lv 10 = 10 đòn hạ người (tối đa 10) và hạ Elite ≈ 12.9s (mức tối thiểu 12s); Elite Lv 6 ≈ 18.6s (mức tối đa 20s). Chỉnh chỉ số người chơi sau này dễ làm test đỏ; nếu cần đệm: giảm công gốc Guard 13→12. | Phase 9 (P9-3) | Phase 11 (Regression) | Chờ quyết |
| FB-021 | Phân vùng Tầng: Thị trấn (Safe Zone) / Vùng Farm / Hầm Boss tách biệt, nối bằng cổng đi bộ; quái không đuổi vào Thị trấn; boss chỉ kích hoạt trong phòng boss; camera và nhạc theo vùng | Sau Phase 9 (product owner: quái, boss, NPC, khu an toàn nằm chung một chỗ) | Phase 9.7 (9.7a bố cục và cổng, 9.7b phòng boss) | Đã quyết 2026-10-14 (ưu tiên cao) |
| FB-022 | Âm thanh: nhạc nền + SFX cơ bản (vụng, trúng, bị trúng, UI), âm lượng lưu local | Rà soát backlog sau Phase 9 (chưa có mục âm thanh) | Phase 9.5 | Đã quyết 2026-10-14 |
| FB-023 | Phản hồi đòn đánh: số sát thương, nháy khi trúng, dừng hình ngắn, hiệu ứng tan khi chết, thanh máu quái và boss | Như trên | Phase 9.5 | Đã quyết 2026-10-14 |
| FB-024 | Mũi tên chỉ mục tiêu quest (không làm mini-map) | Sau khi phân vùng (người chơi phải đi giữa các vùng) | Phase 9.9 | Đã quyết 2026-10-14 (cắt đầu tiên khi thiếu thời gian) |
| FB-025 | Nhãn độ hiếm cho item và so sánh trang bị khi nhặt | Rà soát backlog sau Phase 9 | Mốc 2 | Chờ quyết |
| FB-026 | Art Track chạy song song (A0 hợp đồng → A1 → A2 → A3), xem `05` §3 | Product owner hỏi khi nào làm đồ họa | Song song từ Phase 9.5 (tileset sau 9.7a) | Chờ quyết |
| FB-027 | Phòng Boss hợp tác 2–4 người vào bằng mã phòng, tái dùng Lobby/Relay/NGO của Phase 10 | Product owner: muốn nhiều người cùng đánh boss | Mốc 2, sau Duel | Chờ quyết; cần ADR vì `04` §44–45; luật rematch không EXP (FB-019) phải đổi sang tính theo từng account |
| FB-028 | Bảng Nhổ Neo / hiện diện thế giới (ai hạ boss đầu, số người đã vượt tầng) | Như trên | Mốc 2 | Chờ quyết; cần Cloud Code + ADR |
| FB-029 | Hướng dẫn nhập môn khoảng 3 phút: gợi ý một lần theo sự kiện bằng giọng hệ thống, dùng Quest/NPC sẵn có | Chuyển từ mục 3.1 | Phase 9.9 | Đã quyết 2026-10-14 |
| FB-030 | Quái đa vai trò: `CombatRole` trong `EnemyDefinition` (Chaser, Skirmisher, Ranged) và hành vi Ranged. Hiện mọi quái dùng chung một hành vi (đuổi → báo đòn → cận chiến); `12` §7.1, `07` §5 và `11` §3 đã mô tả role nhưng code chưa có | Rà soát docs với code (2026-10-14) | Phase 9.8 | Đã quyết 2026-10-14 |
| FB-031 | Quái theo pha giờ: `allowedTimePhases` / `phaseStatModifiers` (`02` §11, `08` §4.2) đã mô tả nhưng chưa có code | Rà soát docs với code (2026-10-14) | Sau Phase 9.8 | Chờ quyết |

*(Thêm dòng khi phát hiện. Đổi "Trạng thái" thành "Đã làm ở Phase X" hoặc "Bỏ qua — lý do".)*

---

## 3. TƯƠNG LAI — CHƯA CẦN QUYẾT

Danh sách tính năng phổ biến của 2D RPG mobile. Chưa spec, chưa cần quyết — chỉ để không quên.

### 3.1. UX / Cài đặt
- Settings chi tiết (chất lượng đồ hoạ, FPS cap, ngôn ngữ)
- Localization (Việt / Anh)
- Tutorial / Onboarding → đã chuyển thành FB-029 (Phase 9.9)
- Loading screen có progress bar → Phase 9.6 (bản nhẹ)
- Splash screen thương hiệu → để Phase 11 / Art Track (dùng splash của Player Settings, không làm màn riêng)
- Custom layout button (user kéo nút theo ý)
- Nhiều profile joystick (cố định / floating / nửa floating)

### 3.2. Meta / Xã hội
- Profile người chơi (xem thông tin bản thân, người khác)
- Leaderboard (level, floor, duel wins)
- Friend / Block list
- Mail / Inbox (nhận quà, thông báo)
- Achievement
- Season / Season pass

### 3.3. Gameplay mở rộng
- Daily quest / Daily reward
- Crafting nâng cao
- Enchant / Upgrade item
- Skill tree (nếu muốn mở rộng ngoài 4 trục 07)
- Pet / Companion
- Mount / Thú cưỡi
- Fishing / Cooking (mini-game)

### 3.4. Monetize (chưa quyết)
- Shop / IAP
- Battle pass
- Cosmetic / Skin
- Gacha (nếu muốn)

### 3.5. Kỹ thuật
- Analytics / Telemetry
- Crash reporting
- A/B testing
- Replay system
- Cloud sync đa thiết bị

---

## 4. ĐÃ QUYẾT ĐỊNH — KHÔNG LÀM Ở MỐC 1–2

| Tính năng | Lý do | Khi nào |
|---|---|---|
| Trade giữa người chơi | Cần server authoritative, anti-cheat phức tạp | Mốc 3+ |
| Guild / Clan | Cần backend phức tạp | Mốc 3+ |
| Chat realtime | Cần moderation, backend | Mốc 3+ |
| Voice chat | Ngoài scope đồ án | Không |
| Matchmaking tự động | Đã ghi Mốc 3 (05 §4) | Mốc 3 |
| Confirm popup "xác nhận chết" (kiểu "chắc chắn muốn chết?") | Phá vỡ tính permadeath | Không |

---

## 5. CÁCH DÙNG

- **Khi test:** phát hiện thiếu gì → thêm mục 2.
- **Cuối phase:** đọc mục 2, quyết định:
  - Thêm vào phase tiếp → cập nhật `05_DEVELOPMENT_ROADMAP.md`.
  - Để tương lai → chuyển xuống mục 3.
  - Không làm → chuyển xuống mục 4.
- **Khi AI đề xuất tính năng mới:** bắt AI ghi vào đây, KHÔNG code luôn.
- **Cuối Mốc:** đọc lại toàn bộ, quyết định lớn cho Mốc tiếp theo.
