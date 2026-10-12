Mốc 1 còn 6 chặng, và phần nặng nhất là Duel (Phase 10). Cả hai phần dưới đây đều lấy từ `docs/05` và `docs/01`.

## Mốc 1 là gì, đang ở đâu

Mốc 1 là bản nộp môn: Tầng 1 chơi hoàn chỉnh, Tầng 2 thêm bằng dữ liệu, có đăng nhập, lưu đám mây, giờ do server quyết định, permadeath, boss mở tầng cho mọi người, và Duel 1 đấu 1 bằng mã phòng.

```text
Phase 0–6.5   Nền tảng, đăng nhập, di chuyển, đánh, EXP, túi đồ, chết, giờ, quest, lưu cloud   ✅
Phase 7       Khung chọn Tầng                      ✅ DONE (code + tests + Editor wiring + device)
Phase 7.5     Kỹ năng + Né                         ✅ DONE (code + tests + Editor wiring + device)
Task D + B    Khóa account khi chết + Cân bằng quái ✅ DONE (DeathRecord fail-closed + CombatBalance sandbox)
Phase 8       Boss + thế giới dùng chung           🔄 ĐANG THỰC HIỆN
Phase 9       Tầng 2 chỉ bằng dữ liệu              ○ chưa làm
Phase 10      Duel online (phòng, 2 máy)           ○ chưa làm  ← nặng nhất
Phase 11      Kiểm tổng trên điện thoại + hồ sơ    ○ chưa làm
```

## Mỗi bước diễn ra theo vòng lặp này
1. Lập kế hoạch theo đúng contract và ownership trong `docs/13`.
2. Viết/sửa code, tạo tests tương ứng.
3. Chạy **Test Runner → Run All** trong Unity (phải toàn xanh).
4. Kiểm tra trong Unity Editor, rồi build thử nghiệm trên điện thoại thật (kèm evidence table).
5. Báo cáo theo format chuẩn: Changed · Why · Not changed · Test đã làm · Impact · Known issues · Next.

## Các chặng

**Chặng 1: Đóng Phase 7 và 7.5 — ĐÃ XONG** ✅
- Né/Kỹ năng dùng được lúc đang đi (Rigidbody2D.Cast + MovePosition trong FixedUpdate).
- Bảng Chọn Tầng đã nối: FloorSelectCanvas active, FloorButton có FloorToggleButton đúng.
- FloorDatabase nạp cả Floor 1 và Floor 2 (floor.02 locked cần hạ boss).
- Kiểm trên điện thoại: ổn, không lỗi.

**Chặng 2: Task D (khóa account khi chết) + Task B (cân bằng) + Task P (bình máu) — ĐÃ XONG** ✅
- Task D: Chuyển sang `RecordDeathAsync(DeathRecord)` fail-closed, lưu thành công mới phát event khóa account, có `RetryRecordAsync`.
- Task B: Đã cân bằng `CombatBalanceConfig` (defenseWeight = 0.3), kiểm thử sandbox qua `BalanceSandboxTests` đảm bảo 8–15 quái/level, 3–5 đòn hạ quái, 10–20 đòn quái hạ người.
- Task P: Hoàn tất logic bình máu `ConsumableRules`, cập nhật `Item_HealthPotion.asset` (40 HP hồi, 5s cooldown).

**Chặng 3: Phase 8, Boss và thế giới dùng chung (ĐANG LÀM)**
- 8a: Tạo boss Tầng 1 (`boss.floor_01`) có animation/telegraph/punish window, phòng boss instance solo.
- 8b: Sửa script Cloud Code `ReportBossDefeated.js` sang Cloud Save JS SDK 1.4 (`@unity-services/cloud-save-1.4`) chuẩn xác.
- 8c: Gửi "boss đã bị hạ" lên server qua `IServerFunctionService.ReportBossDefeatedAsync`, cập nhật Shared World State (`world_bosses`, `world_floors`).
- 8d: Thưởng "lần hạ đầu" (FirstClear RewardClaim trong `PlayerData`), đánh lại boss (rematch) vẫn rơi main drop.
- 8e: Kiểm bằng hai tài khoản thật: A hạ boss, B thấy Tầng 2 mở (`Available`).
- **Xong khi:** A hạ boss, B thấy Tầng 2 mở; chết giữa trận boss ngoài Safe Zone thì khóa account đúng luật và không làm sai lệch boss state; tests EditMode toàn xanh.

**Chặng 4: Phase 9, Tầng 2 chỉ bằng dữ liệu (vừa)**
- Thêm Tầng 2 bằng asset và scene mới, không sửa code lõi.
- **Xong khi:** thêm Tầng 2 mà không phải viết thêm điều kiện "nếu tầng 2" ở đâu cả.

**Chặng 5: Phase 10, Duel online (rất lớn, chia nhỏ)**
- 10a: tạo phòng bằng mã ngắn (Lobby).
- 10b: nối hai máy với nhau (Relay).
- 10c: màn đấu nhìn ngang, thua khi HP dưới 50%.
- 10d: xử lý mất kết nối, dọn dẹp, lịch sử thách đấu.
- Chặng này dùng dịch vụ UGS mới (Lobby, Relay) nên cần bật trên dashboard và thử trên hai thiết bị thật.
- **Xong khi:** hai máy vào cùng một phòng, thấy trạng thái đồng bộ, HP dưới 50% thì kết thúc, mất mạng không làm kẹt màn hình.

**Chặng 6: Phase 11, Kiểm tổng và hồ sơ (vừa)**
- Chạy lại toàn bộ trên điện thoại, kiểm hiệu năng, thử mất mạng, rà tài liệu cho khớp.
- Các phase 0–5.5 gom hết bằng chứng, mỗi phase một bảng Pass thật trên máy thật.
- **Xong khi:** Tầng 1, giờ, chết, Tầng 2 bằng dữ liệu và Duel đều có dòng Pass trên máy thật.

## Rủi ro lớn nhất
1. **Duel (Chặng 5):** dùng dịch vụ mới, cần hai máy, dễ phát sinh lỗi mạng.
2. **Quota & Phối hợp:** làm tuần tự, mỗi lần một work package nhỏ rõ ràng.
3. **Thiếu bằng chứng cuối kỳ:** cần thu thập và ghi lại bảng Pass kiểm chứng trên thiết bị thật.

## Nếu thiếu thời gian
Theo `05 §4`, cắt phần mở rộng từ dưới lên. Không được cắt đăng nhập, lưu đám mây, giờ, chết, cảm ứng, thử trên APK và hồ sơ bằng chứng.

## Việc làm tiếp theo (Phase 8):
1. **Bước 1**: Viết lại script Cloud Code `cloudcode/ReportBossDefeated.js` dùng chuẩn `@unity-services/cloud-save-1.4`.
2. **Bước 2**: Thiết kế Boss Tầng 1 (`BossDefinition`, `BossController`, boss combat stats, telegraph, punish window).
3. **Bước 3**: Cập nhật logic hạ boss và FirstClear RewardClaim trong PlayerData / WorldState.
4. **Bước 4**: Thêm EditMode tests cho Boss / Defeat logic / World unlock.