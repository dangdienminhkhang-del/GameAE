# ART CONTRACT (lớp A0)
## Game2D — Hợp đồng đồ họa: mọi hình vẽ phải theo trang này

> Trạng thái: **ĐỀ XUẤT, đã được product owner giao quyền quyết định (2026-10-16)**. Chưa là quyết định chính thức cho tới khi ghi ADR ở `01` §6 và cập nhật `05`, `10` (FB-026), `11`, `13`.
> Nguồn sự thật: code hiện có (`PlayerController`, `EnemyController`, `BossController`, `DayNightLightController`, `Player.prefab`) và `05` §3 (Art Track). Số đo ở §2 lấy từ code thật, không đoán.
> Hình dựng sẵn bằng code nằm ở `tools/art_gen/` (xem §9). Mọi hình trong dự án này không được lấy từ, hoặc giống, tác phẩm có bản quyền (`AGENTS.md` luật 7, `11` §7).

---

## 1. Quyết định đã chốt

| Hạng mục | Quyết định | Vì sao (bằng chứng) |
|---|---|---|
| Phong cách | **Pixel art**, bảng màu cố định (§3), viền tối theo màu từng vật liệu. | Hình tạm hiện nay đã là 32 px, PPU 32, lọc Point. Rẻ, đồng bộ, sinh được bằng code. |
| Góc nhìn PvE | **Nhìn từ trên xuống, hơi nghiêng.** Chỉ vẽ **một hướng (quay phải)**; quay trái bằng `flipX`. | Code chỉ có `Facing = ±1` và `flipX`. Không cần vẽ 4–8 hướng ở Mốc 1. Hệ quả: không chữ/logo bất đối xứng trên nhân vật. |
| Lưới | **PPU 32**: 1 ô nền = 32×32 px = 1 đơn vị Unity. Lọc **Point**, không mipmap, nén **None**. | Khớp hình tạm. Pixel không bị nhòe. |
| Khung hình | Người chơi **48×48** (thân cao ~30 px, chừa chỗ cho kiếm). Quái thường **32×32**, Elite **48×48**, boss **64–128**. Ô nền **32×32**. Đèn/prop cao tuỳ ý, bội của 8. | Boss tạm là 64×64. Người chơi cần chỗ cho vũ khí vươn ra. |
| Điểm neo (pivot) | **Giữa chân**: x = 0.5, y = 2/chiều cao (cách đáy 2 px). Gốc `transform` = chỗ chân đứng; vùng va chạm và điểm ra đòn nằm ở gốc. | Game nhìn từ trên: sắp xếp trước/sau theo Y. Gốc ở chân thì đi sau/trước cột đúng. Hình sẽ nằm cao hơn hình tạm khoảng 0.5 đơn vị, chấp nhận. |
| Vũ khí | **Lớp riêng**, cùng khung với thân, ghép bằng **Sprite Library / Sprite Resolver** (gói `com.unity.2d.animation` 13.0.6 đã có). Mốc 1: 2 nhãn = Kiếm Sắt, Kiếm Neo Gỉ. Áo giáp: để Mốc 2 (chỉ icon). | Có nhiều vũ khí + Weapon Mastery. Vẽ liền vào thân thì mỗi vũ khí phải vẽ lại mọi động tác. |
| Ngày/đêm | **Giữ nguyên cách đang có**: `DayNightLightController` đổi màu **Global Light 2D** trên `Renderer2D`. Mọi hình thế giới dùng `Sprite-Lit-Default` (đã xác nhận ở Player.prefab). | Code đã làm xong, không phải sửa gì khi đổi sang hình thật. Xem ảnh `day_dusk_night.png`. |
| Đèn | Đèn hải lưu = **Point Light 2D**, tối đa **6 đèn** trong khung nhìn, **tắt bóng đổ**, không dùng Bloom ở Mốc 1. Phần tự phát sáng (lõi đèn, mắt boss) tách thành child `Glow` dùng `Sprite-Unlit-Default`. UI Canvas không bị ánh sáng ảnh hưởng. | Máy Android yếu. |
| Camera | Hiện `orthographic size = 5` (10 ô cao). Khi có sprite thật (A1) bật Pixel Perfect Camera của URP 2D: tham chiếu **640×360**, PPU 32 → size 5.625; chỉ dùng tỉ lệ nguyên (2× cho 720p, 3× cho 1080p). | Tỉ lệ lẻ làm pixel to nhỏ không đều, rung khi camera chạy. Đổi camera là sửa scene → cần chủ scene duyệt. |

## 2. Quy tắc quan trọng nhất: màu báo đòn là **chức năng**, không phải trang trí

Hiện code báo đòn bằng cách **nhân màu cả hình** (`spriteRenderer.color`, kiểu nhân). Hình càng tối thì màu báo càng bẩn. Permadeath (chết là mất tài khoản) nên người chơi phải nhìn ra ngay.

| Trạng thái | Màu nhân (R,G,B) | Nguồn |
|---|---|---|
| Quái chuẩn bị ra đòn | (1, 0.55, 0.55) hồng nhạt | `EnemyController.SetTelegraph` |
| Boss đòn thường | (1, 0.8, 0.2) vàng/cam | `BossController.TelegraphState` |
| Boss khựng (punish) | (0.7, 0.7, 1) xanh nhạt | `PunishWindowState` |
| Boss lao (pressure) | (0.75, 0.2, 1) tím | `PressureTelegraphColor` |
| Cuồng nộ | (1, 0.5, 0.5) | `BossController` |
| Chết | (0.3, 0.3, 0.3, a≈0.8–0.9) xám | cả hai |

**Quy tắc "hình chịu được màu" (tint-safe) cho mọi quái và boss:**
1. Thân chính phải **sáng và ít bão hòa** (trung bình kênh màu ≥ ~150/255). Chi tiết tối chỉ là viền, bóng, mắt.
2. Nhân với từng màu mà con đó dùng ở bảng trên, hình phải **khác hình gốc ≥ 60** (khoảng cách màu trung bình). Chạy `tools/art_gen/tint_test.py`.
3. Nền (gạch, prop) **không dùng diện tích lớn** màu tím bão hòa, hồng, xanh lam nhạt bão hòa, cam bão hòa. Vàng beacon chỉ ở vật nhỏ (đèn, khóa, vật phẩm, NPC).

Kết quả đo mẫu (chạy `tint_test.py`): Slime mẫu thấp nhất 69.0 (đạt). Boss tạm hiện tại: punish chỉ 38.1, đòn thường 59.4, cuồng nộ 54.2 (**không đạt**, vì nền xanh tối). Cách sửa lâu dài ở hook H2 (§7).

## 3. Bảng màu (5 bậc mỗi dải: 0 viền, 1 bóng, 2 gốc, 3 sáng, 4 điểm sáng)

Màu chính theo `11` §5: **xanh than, xám muối, đỏ gỉ, vàng beacon.** Mã đầy đủ nằm trong `tools/art_gen/pixelkit.py` (`PALETTE`), đó là nguồn duy nhất; không tự thêm màu ngoài bảng khi chưa sửa file đó.

| Dải | Gốc (bậc 2) | Dùng cho |
|---|---|---|
| xanh than / nước | #173d52 | nước, bóng đổ, áo mưa (#2a6a6a) |
| xám muối | #aab4c2 | tóc, kim loại, lưỡi kiếm |
| đỏ gỉ | #97402b | khăn, gỉ sét, chắn kiếm |
| vàng beacon | #e9b82c | đèn, khóa, ánh sáng. Rất nhỏ. |
| gỗ | #55402a / #5f482f | cầu tàu |
| đá | #363e4e / #3b4354 | lát bờ |
| da | #b98163 | mặt, tay |
| kính biển | #a5dccb | Slime (sáng để màu báo đòn còn đọc được) |

## 4. Bộ animation chuẩn

Mỗi clip là **một dải ngang**, ô = khung của nhân vật. Tên tham số Animator đặt **đúng như cột cuối**.

| Đối tượng | Clip (số khung, fps) | Tham số |
|---|---|---|
| Người chơi (thân và vũ khí, **cùng số khung, cùng thứ tự**) | Idle 4 @6 lặp · Walk 6 @10 lặp · Attack 4 · Dodge 3 · Hurt 2 · Die 5 | `IsWalking` (bool, **đã có** ở `PlayerController`) · trigger `Attack`, `Core`, `Traverse`, `Dodge`, `Hit`, `Die` |
| Quái thường | Idle 4 · Walk 4 · Attack 4 (khung đầu = lấy đà) · Hurt 1 · Die 4 | `IsWalking`, trigger `Attack`, `Hit`, `Die` |
| Boss | Idle 4 · Walk 4 · Telegraph 4 · Attack 4 · Punish 4 · PressureWindup 4 · PressureDash 2 · Die 6 | trigger theo state của `BossController` |

Hiện chỗ đã có sẵn ô tên trigger trên Player.prefab (đang để trống): `PlayerAttack.attackTriggerName`, `TechniqueController.triggerCoreTechnique / triggerTraverseTechnique`, `DodgeController.dodgeTriggerName`. Điền `Attack`, `Core`, `Traverse`, `Dodge` là xong, không sửa code.

**Animation chạy theo game, không điều khiển game.** Đòn trúng hay trượt do số liệu và cửa sổ đánh quyết (`07` §4, `BossHitRules`). **Cấm** dùng animation event để gây sát thương.

## 5. Thứ tự vẽ (Sorting Layer) và thư mục

- Sorting Layer (thêm trong **Project Settings → Tags and Layers**, không sửa tay file): `Background` → `Ground` → `Props` → `Characters` → `FX` → `Overlay`. Hiện chỉ có `Default`.
- `Characters` và `Props` sắp theo Y: **Renderer2D → Transparency Sort Mode = Custom Axis (0, 1, 0)**.
- Thư mục: `Assets/_Game/Art/{Characters/Player, Characters/Enemies, Characters/Bosses, Environment/Floor01, Environment/Floor02, Props, Items, UI, VFX}` và `Assets/_Game/Audio/{Music,SFX}`. Thư mục `Art/Placeholder` giữ nguyên cho tới khi A3.
- Tên file: `Loại_Tên_Clip.png`, ví dụ `Player_Body_Walk.png`, `Enemy_Slime_Idle.png`, `Tiles_Floor01_Harbor.png`, `Prop_BeaconLamp.png`. Ô cắt đặt tên `…_0, …_1`.
- Cài đặt nhập: Texture Type **Sprite (2D and UI)** · Mode **Multiple** (cắt theo lưới) · PPU **32** · Mesh Type **Full Rect** · Filter **Point** · Compression **None** · Mipmap **tắt** · Pivot **Custom (0.5, 2/H)** cho nhân vật, quái, boss, prop cao; **Center** cho gạch.

## 6. Ranh giới code và art

1. Vùng va chạm (collider) **tách khỏi hình**; đổi hình không được làm lệch va chạm.
2. Hình và animator gắn qua prefab hoặc definition, không viết cứng trong script.
3. Người làm art **không sửa script**; người làm code **không sửa thư mục Art** (trừ gắn dây).
4. Mỗi scene một chủ tại một thời điểm. Tilemap + collider + Sorting Layer là chỗ dễ đụng nhau nhất.
5. Commit luôn kèm file `.meta`; đổi tên hoặc di chuyển hình **trong Unity Editor**.

## 7. Việc code nhỏ cần làm trước khi hình thật vào (mỗi việc cần product owner duyệt theo `AGENTS.md` luật 8)

| Mã | Việc | Gộp vào |
|---|---|---|
| H1 | `Player.prefab`: đặt `SpriteRenderer.color` về **trắng** (đang là cyan 0.35/0.85/1 vì hình tạm là hình trắng; để vậy thì nhân vật thật bị nhuộm xanh). Điền 4 tên trigger ở §4. Thao tác trong Editor. | A1 |
| H2 | **Một điểm duy nhất quản lý màu sprite** với thứ tự ưu tiên Chết > Báo đòn > Nháy khi trúng > Cuồng nộ > Gốc, thay cho nhiều chỗ đang ghi `spriteRenderer.color` thẳng (`EnemyController`: SetTelegraph, ResetForSpawn, OnDeath; `BossController`: Telegraph, Punish, Pressure, Enrage, Defeat). Tuỳ chọn: shader "nháy màu" thay cho nhân màu để bỏ giới hạn §2. | Phase 9.5 (đã có yêu cầu nháy khi trúng không xoá màu báo đòn) |
| H3 | Quái và boss hiện **không có Animator** (hình lấy từ `definition.Sprite`, một khung tĩnh). Thêm Animator tuỳ chọn + trigger theo state. Thêm trường vào `EnemyDefinition`/`BossDefinition` là ACR. | Phase 9.5 / 9.8 |
| H4 | `EquipmentVisual`: nghe sự kiện mặc/tháo đồ rồi đặt nhãn Sprite Resolver cho lớp vũ khí. Không đụng logic Inventory. | A1 (khi có kiếm thật) |
| H5 | Thêm Sorting Layer và Custom Axis ở §5. Thao tác trong Editor. | A1 |
| H6 | Ô `TelegraphFX` (vòng cảnh báo dưới chân) cho quái/boss, để báo đòn không chỉ dựa vào màu. | Phase 9.5 / 9.8 |

**Thứ tự đề nghị:** A0 (trang này) → làm H2, H3, H6 trong Phase 9.5 → vẽ A1 sau khi 9.5 xong (vì 9.5 đổi cách hiển thị) → tileset sau 9.7a (vì vùng/cổng quyết bố cục).

## 8. Nguồn gốc và bản quyền: file `Assets/_Game/Art/ASSET_SOURCES.md`

Mỗi hình/âm thanh có một dòng. Không có dòng thì không vào repo.

| Đường dẫn | Tên | Nguồn | Giấy phép | Ai thêm | Ngày | Ghi chú |
|---|---|---|---|---|---|---|
| Art/Characters/Player/Player_Body_Idle.png | Người chơi, thân, Idle | Tự tạo bằng `tools/art_gen` | Thuộc dự án | (tên) | 2026-10-16 | |

Hình lấy từ ngoài: ghi URL và tên giấy phép (CC0 hoặc tương đương cho phép dùng thương mại); đính kèm bản sao giấy phép nếu có. Không dùng hình, âm thanh, UI, nhân vật, tên của bất kỳ tác phẩm nào có bản quyền (`11` §7). Thiết kế nhân vật chính là nguyên bản của Luminport, không bắt chước ngoại hình hay vũ khí của IP khác.

## 9. Hình sinh bằng code

- Thư mục `tools/art_gen/` (nằm ngoài `Assets`, Unity không nhập). Cần Python + Pillow + numpy. Chạy `python make_sample.py` thì PNG ra thư mục `out/`; chạy `python tint_test.py` để kiểm tra §2.
- Mạnh ở: gạch nền, prop, quái kiểu khối/máy móc, vật phẩm, hiệu ứng, UI. Yếu hơn ở: nhân vật người nhiều động tác (cần chỉnh tay từng tư thế). Không thay thế họa sĩ cho hình quảng bá.

## 10. Cổng qua A0

- [x] Hợp đồng một văn bản (trang này).
- [x] Có hình mẫu theo đúng khung, pivot, bảng màu: người chơi (Idle 4, Walk 6, vũ khí tách lớp), Slime Idle 4, 8 gạch sàn, đèn hải lưu.
- [x] Slime mẫu đạt kiểm tra màu báo đòn (§2).
- [x] Xem trước ngày/đêm bằng cách mô phỏng Global Light 2D (`day_dusk_night.png`).
- [ ] **Chưa làm:** nhập mẫu vào Unity và chạy trong game (cần người dùng làm và chụp ảnh). Chưa có bằng chứng thì chưa đóng A0.
