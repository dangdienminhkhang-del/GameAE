# 14A — PHỤ LỤC ART CONTRACT
## Game2D — Âm thanh, giao diện và font, hiệu ứng, thử độ dễ đọc trên máy thật

> **Trạng thái: ĐỀ XUẤT (2026-10-17).** Bổ sung cho `14_ART_CONTRACT.md`, không thay nó. `14` vẫn là nguồn sự thật cho hình vẽ (PPU 32, khung, điểm neo, bảng màu, màu báo đòn, 8 hướng, Sorting Layer). Nếu được duyệt, dán phần này vào cuối `14` thành mục 11–14, hoặc để nguyên file và thêm một dòng vào `15_ART_HANDOFF.md` §0: "Khi làm âm thanh, giao diện, hiệu ứng, đọc thêm `docs/14A`".
> Nguồn: `14`, `15`, `05` (Phase 9.5, 9.6, 9.8, Art Track), `13` (Wave 9.5c), `11` §5 và §7.
> Mọi quy tắc ở đây tuân theo `15` §7 (điều cấm): không sửa script/scene/prefab, không dùng thứ gì của tác phẩm có bản quyền, không tự điền "Pass".

---

## 11. Âm thanh

### 11.1 Code đã dự kiến sẵn (theo `05` P9.5 và `13` Wave 9.5c)

- Dịch vụ phát âm có pool `AudioSource`; `AudioIds` liệt kê mã âm thanh; `AudioCatalog` nối mã với clip. Âm lượng nhạc và SFX lưu local (PlayerPrefs), mặc định 0.7 và 0.9. Ra nền thì tắt tiếng, quay lại thì tiếp tục.
- **Không có AudioMixer ở Mốc 1** (`05` và `13` đều ghi "Không làm: mixer phức tạp"). Vì vậy mức âm lượng giữa các file phải đồng đều ngay từ lúc làm file, để khỏi phải chỉnh bằng code.
- Danh sách `AudioIds` do lane code cung cấp. Lane âm thanh hỏi danh sách này trước khi đặt tên file.

### 11.2 Danh mục lớp A1

| Nhóm | Âm thanh | Phase |
|---|---|---|
| Nhạc | Nhạc nền gameplay; nhạc boss | 9.5c |
| Nhạc | Nhạc theo vùng (Thị trấn, Vùng Farm, Hầm Boss) | 9.7a |
| SFX chiến đấu | Vung đòn, trúng, bị trúng, kỹ thuật, né | 9.5c |
| SFX khác | Nhặt, lên cấp, UI (nhấn, mở, đóng) | 9.5c |
| SFX quái Ranged | Báo đòn, bắn, đạn chạm Safe Zone | 9.8 |

### 11.3 Thông số

| Mục | Quy tắc gợi ý |
|---|---|
| Định dạng | OGG Vorbis |
| Nhạc | Loop liền mạch, 60–120 giây mỗi vòng; Load Type Streaming |
| SFX ngắn | Mono, 44.1 kHz, thường ≤ 1.5 giây; Load Type Decompress On Load |
| Mức | Nhạc quanh −18 LUFS; SFX đỉnh ≤ −3 dBFS; không méo tiếng |
| Cắt đầu cuối | Bỏ khoảng lặng thừa; SFX trúng và bị trúng phải nghe khác nhau rõ |

### 11.4 Đặt tên và thư mục

- Thư mục theo `14` §5: `Assets/_Game/Audio/Music`, `Assets/_Game/Audio/SFX`.
- Tên: `music_<tên>.ogg`, `sfx_<tên>.ogg`, `snake_case`, khớp `AudioIds` (ví dụ `sfx_hit_player_to_enemy.ogg`, `music_gameplay_town.ogg`). ⚠ Tên cụ thể chờ danh sách `AudioIds`.

### 11.5 Hướng âm thanh

Nước, kim loại, dây căng, gió (`11` §5). Một âm chuông làm dấu hiệu hệ thống nếu `16_GAME_BIBLE` §10.3 được duyệt: một hồi cho "Neo nhổ", chuông nhỏ cho "mở Thế mới". Chưa duyệt thì không làm riêng.

### 11.6 Nguồn và giấy phép

- Mỗi file âm thanh có một dòng ghi nguồn và giấy phép. ⚠ Hai tài liệu đang ghi hai chỗ khác nhau: `13` (Wave 9.5c) nói `Assets/_Game/Art/ASSET_SOURCES.md`, còn ghi chú K1 trong `05` nói `Assets/_Game/Audio/ASSET_SOURCES.md`. Nhờ Integrator chọn một chỗ rồi sửa tài liệu còn lại.
- Không dùng nhạc hay âm thanh của tác phẩm có bản quyền, kể cả "giống". Nhạc do AI tạo: ghi công cụ, ngày và điều khoản sử dụng. Không rõ giấy phép thì không dùng. Phụ lục này không phải tư vấn pháp lý.

---

## 12. Giao diện và font

### 12.1 Vùng chạm

- Nút chạm tối thiểu khoảng 48 dp (≈ 9 mm). Nút Attack, Technique, Dodge và nút bình không chồng vùng chạm của nhau.
- Joystick floating (Phase 9.6) dùng nửa trái màn hình: không đặt nút hay thông tin cần chạm ở vùng đó.
- Nút bình hiện trạng thái "Đang giao chiến" kèm đếm ngược (`05` P9.6): vẽ sao cho đọc được cả khi chỉ liếc qua.

### 12.2 Bố cục và hình

- Canvas UI không bị ánh sáng ảnh hưởng (`14` §1). Panel và nút dùng 9-slice (Sprite Border) để không méo.
- UI neo theo safe area của màn hình (tai thỏ, góc bo): việc của lane code.
- Icon vật phẩm và icon kỹ thuật 32×32 (`15` §4 hạng mục 7).
- Trạng thái combat, giờ trong ngày và chết phải phân biệt được bằng hình dạng hoặc biểu tượng, không chỉ bằng màu.

### 12.3 Font

- **Phải hiển thị đủ dấu tiếng Việt.** Thử bằng câu: "Quyền truy cập đã niêm phong. Hạ Giữ Neo, tuyến kế tiếp mở. Ặ Ễ Ỡ Ữ".
- Tạo TextMeshPro font asset có bộ ký tự tiếng Việt. Cỡ chữ tối thiểu khoảng 14 sp. Ví dụ các font giấy phép OFL trên Google Fonts hỗ trợ tiếng Việt: Noto Sans, Be Vietnam Pro (kiểm lại giấy phép từng font trước khi dùng).
- Mọi font cũng phải có dòng trong `ASSET_SOURCES.md`.

### 12.4 Số sát thương và thanh máu (Phase 9.5b)

- Số sát thương đọc được trong khoảng nửa giây: nét dày, có viền tối. Hai loại (đòn mình gây ra, đòn mình nhận) khác nhau rõ về màu **và** vị trí hoặc cỡ.
- Không dùng màu dành riêng cho báo đòn (vàng/cam, tím, hồng/đỏ nhạt, xanh nhạt; `14` §2). Gợi ý: đòn mình gây ra dùng trắng hoặc xám muối sáng; đòn mình nhận dùng đỏ gỉ sáng có viền. Giá trị cuối cùng do `CombatFeelConfig` quyết, lane code cân nhắc.
- Thanh máu quái (trên đầu, ẩn khi chết) và thanh máu boss (HUD): đầy, giảm và thấp phải phân biệt được không chỉ bằng màu; có viền; boss có thể thêm vạch pha.

### 12.5 Splash và loading

Dùng splash của Player Settings, không làm màn riêng (`10` mục 3.1). Overlay loading có thanh tiến độ (`05` P9.6) theo bảng màu nền của `14` §3.

---

## 13. Hiệu ứng (VFX)

### 13.1 Quy tắc chung

- Hiệu ứng chém, vòng cảnh báo, tia: vẽ **một hướng**, để code xoay tự do (`15` §3.1).
- Phần tự phát sáng tách thành child dùng `Sprite-Unlit-Default`; phần còn lại dùng `Sprite-Lit-Default` (`14` §1). Sorting Layer `FX` (`14` §5).
- Nhẹ: mỗi hiệu ứng ≤ 8 khung, ≤ 64×64 px (trừ hiệu ứng boss); không Particle System nặng; pool do lane code lo (`05` P9.5: "không particle nặng").

### 13.2 Danh mục lớp A1

| Hiệu ứng | Ghi chú | Phase |
|---|---|---|
| Chém (hit arc) | Một hướng; khớp vùng đánh thật | 9.5 |
| Tia khi trúng | Ngắn, không che quái | 9.5 |
| Quái tan khi chết | Phần tan chính do code làm trong `deathSeconds`; art chỉ cần hạt sáng nhỏ nếu muốn thêm | 9.5 |
| Vòng cảnh báo dưới chân quái/boss | Cần hook H6 (`14` §7); là phần bổ sung, không thay màu báo đòn | Gói hook art |
| Đạn quái Ranged | Chậm đủ để né; tương phản rõ với nền; có viền | 9.8 |

### 13.3 Điều không được làm

- Không đổi hay thay thế màu báo đòn (`14` §2): màu nhân vẫn là chức năng. Vòng cảnh báo chỉ bổ sung.
- Hạt sáng khi tan không dùng vàng beacon (`14` §2: vàng beacon chỉ ở vật nhỏ như đèn, khóa, vật phẩm, NPC); dùng màu theo vật liệu của quái (ví dụ kính biển cho Slime).
- Hiệu ứng không che quái hoặc vòng cảnh báo của quái đang tấn công.

---

## 14. Thử độ dễ đọc trên máy thật

Chạy cho mỗi lô hình (quái, boss, tileset, UI, VFX) trước khi coi là xong. Kết quả ghi bằng ảnh chụp và bảng; không tự điền "Pass" (`05`).

| # | Phép thử | Cách làm | Đạt khi |
|---|---|---|---|
| 1 | Silhouette | Chụp màn hình, chuyển đen trắng hoặc tăng tương phản mạnh | Nhân vật, quái, nền vẫn phân biệt được |
| 2 | Hai giây | Cho một người chưa chơi nhìn ảnh chụp 2 giây | Chỉ ra được nhân vật, quái nguy hiểm, vùng báo đòn |
| 3 | Màu báo đòn | Chạy `tools/art_gen/tint_test.py` (`14` §2) | Mọi số ≥ 60 |
| 4 | Mù màu | Bật bộ lọc chỉnh màu của Android (Cài đặt → Hỗ trợ tiếp cận; tên mục tùy hãng máy) | Báo đòn vẫn thấy nhờ hình vùng và chuyển động |
| 5 | Ngày và đêm | Chơi qua các pha giờ với Global Light 2D | Quái và vòng cảnh báo vẫn đọc được ở đêm |
| 6 | Vùng chạm | 3 người chơi 5 phút, đếm lần bấm trượt hoặc nhầm nút | Không có nhầm nút nghiêm trọng; ghi số liệu |
| 7 | Hiệu năng | 10 phút giao tranh nhiều quái (P9.5 (f)) | FPS không thấp hơn ngưỡng product owner đặt |
| 8 | Điều kiện đọc | Màn khoảng 6 inch, độ sáng 50%, cách mắt 30–40 cm | Các phép thử trên vẫn đạt |

*Hết phụ lục.*
