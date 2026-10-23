# Nguồn và giấy phép âm thanh (Assets/_Game/Audio)

Tất cả file dưới đây là **placeholder tổng hợp bằng code, do product owner cung cấp, thuộc dự án**. Không dùng sample, loop, thư viện âm thanh hay bản thu của bên thứ ba; không mô phỏng bài hát hay tác phẩm nào. Dùng tự do trong dự án này.
Mục đích: để game có tiếng trong lúc làm P9.5–P9.9 và thử cảm giác chiến đấu. Thay bằng âm thanh thật ở Art Track (lớp A1) và cập nhật bảng này khi thay.
Danh sách đối chiếu với thư mục thật ngày 2026-10-18: 13 file trong `Sfx/` và 3 file trong `Music/` (tổng 16).

| File | Id (AudioIds) | Mô tả | Nguồn | Giấy phép | Ngày |
|---|---|---|---|---|---|
| Sfx/sfx_player_swing.wav | sfx.player_swing | Vung đòn: nhiễu qua bộ lọc quét tần số | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_hit_enemy.wav | sfx.hit_enemy | Đòn trúng quái: thud + click + kim loại ngắn | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_hit_player.wav | sfx.hit_player | Người chơi bị trúng: thud trầm có méo | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_enemy_die.wav | sfx.enemy_die | Quái chết: quét tần số giảm + hơi xì | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_technique.wav | sfx.technique | Kỹ thuật vũ khí: quét tần số tăng + chuông | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_dodge.wav | sfx.dodge | Né: nhiễu cao ngắn | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_pickup.wav | sfx.pickup | Nhặt đồ: hai nốt chuông ngắn | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_level_up.wav | sfx.level_up | Lên cấp: bốn nốt chuông + vang | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_ui_click.wav | sfx.ui_click | Bấm UI: tick ngắn | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_ui_back.wav | sfx.ui_back | Quay lại UI: tick thấp dần | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_potion.wav | sfx.potion | Dùng bình: các bọt nước | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_boss_telegraph.wav | sfx.boss_telegraph | Boss báo đòn: rung trầm + còi tăng dần | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Sfx/sfx_enemy_shoot.wav | sfx.enemy_shoot | Quái bắn (P9.8): vút ngắn + thump | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Music/music_town.ogg | music.town | Thị trấn: pad, nốt gảy thưa, gió; loop liền mạch | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Music/music_gameplay.ogg | music.gameplay | Chơi thường: bass, kick nhẹ, nốt gảy; loop liền mạch | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |
| Music/music_boss.ogg | music.boss | Boss: drone, kick dồn, clap, stab; loop liền mạch | Tổng hợp bằng code, do product owner cung cấp | Thuộc dự án | 2026-10-18 |

## Quy tắc khi thêm âm thanh

- Mỗi file mới phải có một dòng ở đây: nguồn cụ thể, giấy phép, ngày. Không rõ giấy phép thì không đưa vào dự án.
- Không dùng âm thanh, nhạc hay bản thu từ tác phẩm có bản quyền (AGENTS.md luật 7).
- Tên file khớp id: `sfx.player_swing` → `sfx_player_swing.wav`, `music.gameplay` → `music_gameplay.ogg`.

## Xác nhận nguồn gốc (2026-10-18, gói P9.5-fix-tint)

Đã đối chiếu thư mục `Assets/_Game/Audio` với bảng trên: có đúng **16 file** âm thanh (13 SFX trong `Sfx/`, 3 nhạc trong `Music/`) và cả 16 đều có dòng ở bảng, không thiếu, không thừa.
Nguồn của cả 16 file: **tổng hợp bằng code, do product owner cung cấp (gói placeholder giải nén vào dự án), thuộc dự án**. Ngày xác nhận: 2026-10-18 (ngày ở bảng là ngày tạo file). Không có file nào lấy từ bên thứ ba.
