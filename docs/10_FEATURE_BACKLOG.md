# FEATURE BACKLOG
## Game2D — Tính năng phát hiện được, chưa có trong Roadmap

> Docs 05 là scope TỐI THIỂU mỗi phase. File này ghi tính năng phát sinh khi test.
> KHÔNG tự thêm vào code. Chỉ ghi lại → quyết định sau.

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
| FB-001 | Settings screen (âm lượng, rung, độ nhạy joystick) | Phase 2 (test joystick trên máy) | Phase 11 (Regression) | Chờ quyết |
| FB-002 | Pause menu (resume, settings, về MainMenu) | Phase 2 (app không có cách thoát giữa chừng) | Phase 11 (Regression) | Chờ quyết |
| FB-003 | Save conflict UI (2 thiết bị cùng account) | Chưa — dự đoán | Phase 11 (Regression) | Chờ quyết |
| FB-004 | Confirm popup khi bấm "Tạo tài khoản mới" trên Death Screen | Phase 3 (nhận ra rủi ro lỡ tay) | Phase 5.5 (Death System) | Đã quyết (09 §1.4) — chờ evidence |
| FB-005 | Joystick floating (chạm bất kỳ đâu nửa trái màn hình) | Phase 3 (nhận ra joystick cố định phản ứng chậm) | Phase 11 (Regression) | Chờ quyết |
| FB-006 | Dead zone cho joystick (tránh run tay) | Phase 3 | Phase 11 (Regression) | Chờ quyết |
| FB-007 | Nút Dash/Né riêng | Phase 3 (game permadeath cần né đòn) | Wave 2A (ADR-025) | Đã quyết |
| FB-008 | Layout nút Technique theo slot Core/Traverse/Control/Signature | Phase 3 (dự đoán Phase 7.5 cần) | Phase 7.5 (Blade Technique) | Chờ quyết |
| FB-009 | Nút Item/Potion | Phase 3 (dự đoán Phase 5 cần) | Phase 11 (Regression) | Chờ quyết |
| FB-010 | Haptic feedback (rung khi trúng đòn, HP thấp) | Phase 3 | Phase 11 (Regression) | Chờ quyết |
| FB-011 | Test "chết oan" trên máy thật (đếm số lần chết do control) | Phase 3 (nhận ra rủi ro với permadeath) | Phase 11 (Regression) | Chờ quyết |
| FB-012 | Nút cảm ứng cho Nói chuyện / Túi / Sổ quest (Android không có bàn phím) | Phase 6 (test máy thật: chỉ có phím E/Q/1-9) | Phase 6.5 | Đã làm (PvEActionButton + nút trên Canvas) |
| FB-013 | Quái có windup/telegraph trước khi ra đòn (tránh "chết oan" vì permadeath) | Phase 6 (audit code) | Phase 6 | Đã làm (attackWindupSeconds 0.35s, đổi màu báo trước) |

*(Thêm dòng khi phát hiện. Đổi "Trạng thái" thành "Đã làm ở Phase X" hoặc "Bỏ qua — lý do".)*

---

## 3. TƯƠNG LAI — CHƯA CẦN QUYẾT

Danh sách tính năng phổ biến của 2D RPG mobile. Chưa spec, chưa cần quyết — chỉ để không quên.

### 3.1. UX / Cài đặt
- Settings chi tiết (chất lượng đồ hoạ, FPS cap, ngôn ngữ)
- Localization (Việt / Anh)
- Tutorial / Onboarding
- Loading screen có progress bar
- Splash screen thương hiệu
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
