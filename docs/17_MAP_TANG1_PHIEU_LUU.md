# 17 — MAP TẦNG 1 "BẾN CẠN MÙ SƯƠNG": BẢN ĐỒ PHIÊU LƯU
## Đề xuất thiết kế theo yêu cầu của chủ dự án (2026-10-21)

> **Trạng thái: ĐỀ XUẤT, chưa phải contract.** Chủ dự án cho phép thiết kế tự do, không bị ràng buộc bởi docs hiện có. AI khác chưa làm theo file này cho tới khi chủ dự án duyệt và ghi từng mục vào `docs/10` (FB-xxx). Chỗ lệch với `05` và `16` nằm ở §13; việc chủ dự án cần quyết nằm ở §14.
> Mọi con số là **số khởi điểm**: đo lại sau khi dựng blockout (gói W1). Tên, địa danh, quái, nhiệm vụ trong file này là nguyên bản; chỉ mượn cảm giác khám phá của thể loại leo tháp (nguồn cảm hứng đã nêu ở `16` §0).
> Toạ độ: ô (1 ô = 1 đơn vị Unity = 32 px), gốc (0,0) = tâm Thị trấn, +x = Đông, +y = Bắc. Tốc độ chạy 4,5 ô/giây (`PlayerController.moveSpeed`); camera nhìn được khoảng 17,8 × 10 ô.

---

## 0. Tóm tắt một phút

- **Hiện tại** (đọc từ `Floor01HarborSliceBuilder`): khu đi được chỉ ≈ 1.420 ô gồm cả hầm; Thị trấn là một hình chữ nhật đá 17×11 với một cây đèn; chỉ 6 loại ô phẳng; không cây, không nhà, không đồ vật; từ Thị trấn tới Cổng Hầm khoảng 60 ô, chạy thẳng ≈ 13 giây. Không có gì để *khám phá*.
- **Đề xuất:** biến Tầng 1 thành một vùng đất để phiêu lưu: ≈ 230 × 162 ô (13 × 16 màn hình camera), khu ngoài trời đi được ≈ 15.600 ô, cả hầm ≈ 18.100 ô (gấp ≈ 13 lần), **11 vùng + 4 hầm**, 4 điểm nghỉ an toàn, ≈ 60 điểm thú vị.
- **Cột sống phiêu lưu: "Ba Đèn, Một Neo".** Cổng Hầm Neo Kẹt bị xích Neo khóa. Muốn mở phải thắp ba Đèn Hiệu ở ba miền khác nhau: rừng đước, đồi cối xay, nghĩa địa tàu. Người chơi tự chọn thứ tự, tự tìm đường; mỗi miền có làng hoặc trại, nhiệm vụ, một hộ vệ và bí mật riêng.
- **Bản sắc riêng:** *Nhịp Triều* (thủy triều chạy theo đồng hồ game, triều rút mở lối ẩn), *Sổ Hải Đồ* (bản đồ tự vẽ khi đi), *Đèn Hiệu* (điểm nghỉ + dịch chuyển nhanh + tia sáng chỉ về Neo).
- **Dựng theo giai đoạn:** gói W1 (blockout toàn tầng, đi được hết) cho cảm giác "lớn gấp 13" ngay; art và nội dung đổ vào sau, từng vùng một (§12).

---

## 1. Chẩn đoán map hiện tại

| Hạng mục | Hiện tại | Hệ quả |
|---|---|---|
| Khu đi được | ≈ 1.420 ô (ngoài trời ≈ 890, hầm + phòng boss ≈ 530) | Chạy hết map trong vài chục giây |
| Thị trấn | 1 ô chữ nhật đá 17×11, 1 cây đèn | Không nhà, không phố, NPC đứng giữa sân |
| Địa hình | 6 loại ô: nước, cầu tàu, đá, sàn hầm, sàn boss, tường | Không cát, cỏ, cây, đá tảng, công trình, vật trang trí |
| Nối giữa các khu | cầu thẳng rộng 3–5 ô | Không rẽ nhánh, không vòng, không ngõ cụt |
| Thị trấn → Cổng Hầm | ≈ 60 ô ≈ 13 giây (docs ghi 20–30 giây) | Không có "hành trình" |
| Điểm thú vị / bí mật | 1 vịnh nhỏ có vài quái | Không có gì để phát hiện |
| Quái | 4 spawner, 2 loại quái (Slime, Dơi) | Mọi bãi giống nhau |

**Vì sao cảm giác "nhỏ và không phiêu lưu":** (1) ngắn và tuyến tính; (2) không có cột mốc nhìn thấy từ xa nên không có "đích"; (3) không có lựa chọn, mọi người đi cùng một đường; (4) không có lý do quay lại; (5) cảnh giống nhau từ đầu tới cuối.

---

## 2. Bảy trụ cột phiêu lưu

| # | Trụ cột | Nghĩa là | Thể hiện bằng |
|---|---|---|---|
| 1 | **Thấy trước, đi tới** | Từ phút đầu đã thấy nơi muốn đến | Neo Cổ khổng lồ ở chân trời phía Bắc; mỗi vùng có một cột mốc cao nhìn từ xa (cối xay, hải đăng đổ, cột buồm gãy, cây đước cổ thụ) |
| 2 | **Bản đồ trống, tự vẽ** | Đi tới đâu, bản đồ hiện tới đó | Sổ Hải Đồ (§6.1), % vẽ xong mỗi vùng, Người Vẽ Hải Đồ trả thưởng |
| 3 | **Mỗi ngã rẽ có chuyện** | Rẽ nhánh nào cũng có thứ để thấy | Ba lớp khám phá: tuyến chính → nhánh phụ (rương, nguyên liệu, lore) → bí mật (có dấu hiệu, không đoán mò) |
| 4 | **Biển có nhịp** | Giờ trong ngày và thủy triều mở hoặc đóng nơi chốn | Nhịp Triều (§6.3) + 6 pha ngày/đêm của `08` |
| 5 | **Đèn thắp sáng thế giới** | Đèn Hiệu vừa là đích, vừa là phần thưởng | Điểm nghỉ + dịch chuyển nhanh + hé bản đồ + chìa khóa Cổng Hầm (§6.2) |
| 6 | **Gần an toàn, xa nguy hiểm, công bằng** | Chết là mất tài khoản nên rủi ro phải đọc được | Vòng nguy hiểm theo màu, biển cảnh báo, điểm nghỉ rải đều (§8) |
| 7 | **Thế giới sống** | Có người ở, có dấu vết người đi trước | NPC theo giờ, thú vật, nhật ký đoàn tàu mất tích, Tàu Ma ban đêm |

**Chất liệu:** giữ bản sắc biển – hải đăng – sương của `11`. Luật "mỗi Tầng là mô phỏng địa hình nuôi bằng ký ức của biển" cho phép mọi địa hình (rừng đước, đồi gió, vách đá), nên Tầng 1 không bị giới hạn ở cầu tàu.

---

## 3. Bản đồ tổng thể

Kích thước ≈ 230 × 162 ô; ngoài trời ≈ 15.600 ô đi được; hầm ≈ 2.500 ô (dải toạ độ riêng, §10.4). Sơ đồ màu: xem ảnh bản đồ kèm khi trao đổi; toạ độ để dựng: Phụ lục A.

```text
                                 BẮC
 VÁCH ĐÈN ─────────────► MŨI NEO ◄──────────── (từ Đồi Gió)
 (Hang Sương,            ✦ Trạm Tiền Tiêu
  Đèn Biển Cũ)           Neo Cổ · CỔNG HẦM (3 xích)
     ▲                        ▲
     │                        │  Đường Neo (trục chính)
 NGHĨA ĐỊA TÀU     ĐẦM SƯƠNG  │            ĐỒI GIÓ CỐI XAY
 (Pháo Đài Cạn,    (cầu ván,  │            (cối xay, ✦ Trạm Bơm,
  Đèn Xác Tàu)      lối bùn)  │             Đèn Cối Xay)
     ▲                        │                 ▲
     │                        │             Đèo Muối
 BÃI KÍNH BIỂN ◄── BẾN CẠN ✦ ─┴───────► RỪNG ĐƯỚC SƯƠNG
 ╞ Cầu Cạn ╡→ ĐẢO CHUÔNG       (Cổng Đông)  (✦ Làng Sàn, Đèn Đước)
 (chỉ khi triều rút)               NAM (biển, cầu tàu)
```

### 3.1 Ba tuyến lên Mũi Neo (đều tới được Trạm Tiền Tiêu)

| Tuyến | Đi qua | Dài (ô) | Chạy thẳng | Độ khó |
|---|---|---:|---:|---|
| **Trục giữa — Đường Neo** | Thị trấn → Ngã Tư Xích → Trạm Tiền Tiêu → Cổng Hầm | ≈ 119 | ≈ 26 s | Dễ → vừa |
| **Vòng Đông** | Thị trấn → Rừng Đước (Làng Sàn) → Đèo Muối → Đồi Gió (Trạm Bơm) → Mũi Neo | ≈ 156 | ≈ 35 s | Vừa |
| **Vòng Tây** | Thị trấn → Bãi Kính → Nghĩa Địa Tàu → Vách Đèn → Mũi Neo | ≈ 224 | ≈ 50 s | Khó |
| **Đường tắt (bí mật)** | Cầu ván Đầm Sương (Đường Neo ↔ Nghĩa Địa Tàu); lối bùn khi triều rút; lối thoát Hang Sương | — | — | Cần phát hiện |

### 3.2 Ba Đèn, Một Neo

Cổng Hầm Neo Kẹt có **3 xích** quấn quanh Neo Cổ. Mỗi Đèn Hiệu thắp lên làm một xích rơi. Đủ ba, Chuông Phao ngân, camera lướt qua ba tia sáng hội tụ về Neo, cổng mở.

| Đèn | Vùng | Hộ vệ (Elite) | Thử thách | Lv |
|---|---|---|---|---|
| **Đèn Đước** | Rừng Đước, gốc Đước Cổ | **Cua Giáp Mẹ**: nhả bọt xa, mai chắn phía trước, yếu ở lưng | Lạc trong sương, tìm đường ván tới gốc cổ thụ | 2–4 |
| **Đèn Cối Xay** | Đồi Gió, đỉnh đồi cao | **Bù Nhìn Muối**: xung gió đẩy lùi có vòng báo, khựng sau xung | Chỉnh cánh quạt cối xay cho gió thổi sáng đèn | 3–5 |
| **Đèn Xác Tàu** | Nghĩa Địa Tàu, tháp Pháo Đài Cạn | **Pháo Thủ Gỉ**: pháo bắn theo vạch báo 0,8 s, có vật che | Leo qua xác tàu, cầu thừng, vào pháo đài | 4–5 |

Chạy thẳng không đánh: ba chuyến một chiều từ Thị trấn ≈ 88 + 168 + 130 ô; thứ tự khôn ngoan ≈ 370 ô (≈ 83 s). Thực tế lần đầu ≈ **40–60 phút** (đánh, làm quest, nhặt đồ); người đã quen ≈ 10 phút. Mỗi Elite theo `07 §5`/`16 §6.3`: 12–20 s, có ít nhất một punish window.

Người chơi chọn thứ tự tự do; gợi ý Đước → Cối Xay → Xác Tàu theo độ khó tăng dần, và biển cảnh báo cho biết mức nguy hiểm của từng vùng (§8).

---

## 4. Các vùng

| Vùng | Vị trí, cỡ (ô) | Lv | Vai trò | Nghỉ an toàn |
|---|---|---|---|---|
| **Bến Cạn** (Thị trấn) | tâm (0,3); 56×34 + 3 cầu tàu | — | Căn cứ: NPC, quest, Chọn Tầng, mua bán, ngủ | **Vùng Bảo Hộ** |
| **Bãi Kính Biển** | Tây; ≈ 58×54 | 1–2 | Bãi tập sự: cát, kính biển, tới Điểm Ngắm Đầu Tiên | — |
| **Cầu Cạn + Đảo Chuông** | Tây xa; 13×5 + 22×24 | 4 | Bí mật tuyệt đối: chỉ vào được khi triều rút | Đèn Hiệu trên đảo |
| **Rừng Đước Sương** | Đông; ≈ 66×62 | 2–4 | Rừng, lối ván, sương dày; **Đèn Đước** | **Làng Sàn** |
| **Đường Neo** | trục Bắc; ≈ 119 dài, rộng 8–12 | 2–3 | Đường chính dễ đi, cột đèn, biển chỉ đường | Đèn Hiệu Ngã Tư Xích |
| **Đầm Sương** | giữa Tây-Bắc; 34×30 | 3–4 | Đầm, đảo nhỏ, cầu ván, lối bùn | — |
| **Đồi Gió Cối Xay** | Đông-Bắc; ≈ 82×54 | 3–5 | Đồi, cối xay, ruộng muối; **Đèn Cối Xay** | **Trạm Bơm** |
| **Nghĩa Địa Tàu** | Tây-Bắc; ≈ 52×58 | 4–5 | Xác tàu, pháo đài, đêm có ma; **Đèn Xác Tàu** | Đèn Hiệu ở Pháo Đài |
| **Vách Đèn** | Bắc; ≈ 46×52 | 4–5 | Vách đá, đường chữ chi, hải đăng đổ, Hang Sương | Đèn Hiệu ở Đèn Biển Cũ |
| **Mũi Neo** | Đông-Bắc xa; ≈ 76×42 | 5 | Mũi đất, Neo Cổ, đích của mọi tuyến | **Trạm Tiền Tiêu** |
| **Hầm Neo Kẹt** | dải toạ độ riêng | 5+ | Hầm chính, tới Phòng Boss | Tiền Sảnh (vùng nghỉ) |

### 4.1 Bến Cạn (Thị trấn, Safe Zone)

Bố cục (chi tiết công trình ở §5): Cảng Nam (3 cầu tàu, thuyền, cần cẩu) → Quảng Trường Đèn (sân tròn 14×14, Tháp Đèn Hiệu, Cổng Tháp = Chọn Tầng) → bốn phố: Phố Hải Đồ (Tây), Phố Xưởng (Đông), Chợ Muối (Đông-Bắc), Khu Nhà Dân (Tây-Bắc). Ba cổng: **Tây** → Bãi Kính, **Bắc** → Đường Neo, **Đông** → Rừng Đước.
**Không quái.** NPC đi lại theo giờ; chim mòng, tiếng chuông phao, tiếng búa xưởng.

### 4.2 Bãi Kính Biển (Lv 1–2)

- **Cảnh:** cát, đụn cỏ biển, gỗ trôi, vũng thủy triều, mảnh kính biển lấp lánh (nguyên liệu quest), lều bỏ hoang của đoàn tàu mất tích, xuồng úp (rương đầu tiên). Vài cây dừa nước.
- **Quái:** Slime Nước (đàn nhỏ), Dơi lúc chạng vạng.
- **Khám phá:** **Điểm Ngắm Đầu Tiên** trên đụn cát cao (camera nới ra, lần đầu thấy Neo Cổ ở phía Bắc: "cái đích"); Rương Dưới Rong (chỉ hiện khi triều rút); lối ra Cầu Cạn.
- **Nối:** Cổng Tây Thị trấn; phía Bắc vào Nghĩa Địa Tàu.

### 4.3 Cầu Cạn và Đảo Chuông (bí mật, Lv 4)

- Cầu Cạn là dải cát chỉ đi được khi **triều rút** (§6.3). Đảo có tháp chuông, nghĩa trang nhỏ, Người Giữ Chuông (quest "Tiếng Chuông Đảo"), Đèn Hiệu (chờ triều trong an toàn), và hầm phụ **Hầm Chuông** (Stretch).
- Phần thưởng bí mật: kỹ năng hoặc trang phục hiếm, không tăng sức mạnh vượt tầng (§7).

### 4.4 Rừng Đước Sương (Lv 2–4)

- **Cảnh:** rừng đước rễ chống, tán thấp (tán mờ đi khi người chơi chui vào dưới), kênh nước chặn đường, **lối ván** nối các khoảng đất, sương dày từng mảng, dây leo, đom đóm ban đêm.
- **Làng Sàn** (Vùng Bảo Hộ, 18×13): 5 nhà sàn, sàn giữa làng, bến xuồng, giàn phơi lưới. NPC: **Cô Chài** (chuỗi quest "Thuyền Mẹ Lạc"), **Lái Đò** (xuồng qua kênh), một đứa trẻ vá lưới (hay buột miệng gợi ý bí mật). Có Đèn Hiệu, bán bình máu và lưới.
- **Quái:** Cua Bắn Bọt (Ranged), Slime Đước (biến thể màu của Slime), Dơi đêm. Ổ Cua là khu đông quái.
- **Khám phá:** **Gốc Đước Cổ** (cây 6×8 ô, landmark, có Đèn Đước); thân cây rỗng (phòng ẩn, chặt dây leo); lối ván ẩn chỉ hiện nhờ đom đóm ban đêm; **Đèo Muối** lên Đồi Gió.

### 4.5 Đường Neo (Lv 2–3)

- Đường lát đá và ván, trụ xích hai bên, biển chỉ đường, **6 cột đèn đường hỏng** (quest "Thắp Đèn Đường": thắp cột nào thì hé bản đồ quanh cột đó), 2 chòi nghỉ, trạm thu phí đổ nát.
- **Ngã Tư Xích** (10,72): biển chỉ đường lớn + Đèn Hiệu; ngã ba tới Đồi Gió, Đầm Sương (cầu ván), Vách Đèn.
- **Quái:** Slime, Dơi, Pipe Spitter (có sẵn `enemy.pipe_spitter`).

### 4.6 Đầm Sương (Lv 3–4)

- Đầm nước, 3 đảo nhỏ có rương, **Cầu Ván Đầm** nối Đường Neo với Nghĩa Địa Tàu (đường tắt), sương dày. Cua Bắn Bọt.
- **Lối bùn:** khi triều rút lộ đường bùn ngang đầm (đường tắt bí mật, §7).

### 4.7 Đồi Gió Cối Xay (Lv 3–5)

- **Cảnh:** đồi cỏ và hoa dại, bờ đá khô, **5 cối xay** (landmark thấy từ xa), ruộng muối hình học, xóm bỏ hoang, bù nhìn.
- **Trạm Bơm** (Vùng Bảo Hộ, 15×11): máy bơm khổng lồ, đường ống. **Thợ Bơm** (quest "Cánh Quạt Gãy"). Có Đèn Hiệu.
- **Quái:** Dơi (Skirmisher), Pipe Spitter, Slime. Hộ vệ Đèn Cối Xay: Bù Nhìn Muối.
- **Khám phá:** Đèn Cối Xay (đỉnh đồi cao, giải đố cánh quạt); **Hầm Muối** (khe nứt vách muối); phòng ẩn sau cối xay; Mảnh Hải Đồ rơi lúc bình minh; Đỉnh Cối Xay là điểm ngắm thứ ba.

### 4.8 Nghĩa Địa Tàu (Lv 4–5)

- **Cảnh:** 3 xác tàu lớn mắc cạn, cột buồm gãy, cầu thừng nối thân tàu, đống mỏ neo, mộ gió. **Pháo Đài Cạn** (tàn tích pháo đài; Đèn Xác Tàu trên tháp).
- **Hầm phụ:** **Khoang Tàu Mất Tích** (trong Tàu Vỡ Lớn): cơ cấu dây thừng và cần gạt.
- **Quái:** Cua Bắn Bọt, Dơi; **ban đêm** Thủy Thủ Xương (nhóm 2–3) và field boss **Thuyền Phó Mục**. 8 trang **Nhật Ký Thuyền Trưởng** rải trong vùng (kể số phận đoàn tàu mất tích).

### 4.8b Vách Đèn (Lv 4–5)

- **Cảnh:** vách đá, đường chữ chi, cầu thừng, thông rạp gió, sương lùa.
- **Đèn Biển Cũ:** hải đăng đổ; leo lên đỉnh thấy toàn cảnh (camera nới xa) và hé ≈ 30% bản đồ. **Hang Sương:** hầm phụ 3–4 phòng, sương che hố, đèn lồng soi đường; lối thoát ẩn ra Đường Neo.

### 4.9 Mũi Neo (Lv 5)

- Quảng trường đổ nát, xích neo to bằng cái nhà, **Neo Cổ** (landmark 10×12 ô).
- **Trạm Tiền Tiêu** (Vùng Bảo Hộ, 14×11): lều, lửa trại, Đội Trưởng Tiên Phong, thầy thuốc, bàn của Người Vẽ Hải Đồ (Sổ Thăm Dò). Đây là "thị trấn cuối" trước hầm.
- **Cổng Hầm**: 3 xích, 3 ổ đèn (§3.2). Đèn Hiệu vô hiệu trong hầm.

### 4.10 Hầm Neo Kẹt

Giữ **phòng boss hiện có** (19×17) làm điểm cuối. Mốc 1 chỉ cần: Cửa Hầm → Hành Lang Xích (1–2 đợt quái, bẫy xích có báo trước) → Tiền Sảnh (vùng nghỉ, Mòng đọc Sổ Thăm Dò) → Phòng Boss. Mốc 2 theo `16 §7.2`: Kho Neo Ngập (hành lang **chịu ảnh hưởng triều**: triều rút mới đi được tầng dưới có rương), Phòng Cơ Cấu, Phòng Giữ, Phòng Ẩn, đường tắt về đầu hầm.

### 4.11 Quái và hộ vệ cần thêm

Có sẵn: Slime, Dơi, Pipe Spitter. Dùng **biến thể màu/cỡ** của quái có sẵn để tiết kiệm art (Slime Đước, Slime Cát…). Thêm mới tối thiểu: **Cua Bắn Bọt** (Ranged, đã đề xuất ở `16 §6.2`), **Thủy Thủ Xương** (đêm), 3 Elite hộ vệ (Cua Giáp Mẹ, Bù Nhìn Muối, Pháo Thủ Gỉ), 1 field boss đêm (Thuyền Phó Mục). Cân bằng qua `BalanceSandboxTests`.

### 4.12 Quest gợi ý (cần thêm loại mục tiêu `Discover`, `Interact`; §10.5)

| Vùng | Quest | Ý |
|---|---|---|
| Thị trấn | Bước Đầu → Lưỡi Cho Người Mới → Tin Đắt (có sẵn) | Tin Đắt tặng **Sổ Hải Đồ** và kể về "Ba Đèn" |
| Đường Neo | Thắp Đèn Đường | thắp 6 cột đèn, hé bản đồ |
| Làng Sàn | Thuyền Mẹ Lạc | tìm chiếc thuyền trôi trong sương |
| Đồi Gió | Cánh Quạt Gãy | sửa cối xay, nhận Búa Thợ (mở Hầm Muối) |
| Nghĩa Địa Tàu | Nhật Ký Thuyền Trưởng | gom 8 trang, mở Khoang Tàu |
| Đảo Chuông | Tiếng Chuông Đảo | bí mật tuyệt đối |
| Mũi Neo | Ba Đèn, Một Neo | nhiệm vụ chính của Tầng |

---

## 5. Bến Cạn: danh sách công trình

| Công trình | Cỡ (ô) | Chức năng | Có nội thất |
|---|---|---|---|
| Tháp Đèn Hiệu | 4×7 | Đèn chính, Vùng Bảo Hộ, dịch chuyển | Không |
| Cổng Tháp | 8×5 | Chọn Tầng (khung sắt + xích) | Không |
| Nhà Đèn | 7×6 | Người Giữ Đèn: dạy cơ bản, quest | Có |
| Kho Hải Đồ | 8×6 | Người Vẽ Hải Đồ: Sổ Hải Đồ, bán tin | Có |
| Xưởng Phế Liệu | 9×7 | Người Vớt Phế Liệu: quest vật liệu, rèn (Mốc 2) | Có |
| Quán Mặn | 9×7 | nghỉ, ngủ qua đêm hoặc qua một nhịp triều, tin đồn | Có |
| Quầy Thuốc Muối | 5×4 | mua bình máu | Không |
| Chợ Muối | 14×8 (6 sạp) | mua bán vật dụng, lưới, dây | Không |
| Nhà dân × 4 | 6×5 | NPC thường; 1 nhà có nắp hầm rượu | 2 nhà |
| Kho hàng × 2 | 9×6 | trang trí, góc trốn tìm | Không |
| Bãi Tập | 12×8 | hình nộm, hướng dẫn kỹ thuật | Không |
| Bia Neo | 3×3 | (Mốc 2) | Không |
| Cảng Nam | 3 cầu tàu, 2 cần cẩu, 4 thuyền | cảnh, Tàu Ma ban đêm | Không |
| Tường biển + 3 cổng + chòi canh | | ranh giới | Không |

NPC: Người Giữ Đèn, Người Vẽ Hải Đồ, Người Vớt Phế Liệu, Chủ Quán Mặn, Tín Hiệu Đêm (chỉ ban đêm, quảng trường), 6 dân thường (thoại 1–2 dòng, đổi chỗ theo giờ).

---

## 6. Hệ thống khám phá

### 6.1 Sổ Hải Đồ (bản đồ tự vẽ)

- Nhận ở quest **Tin Đắt**; khi có sổ, HUD hiện nút **Bản đồ** và minimap góc màn hình (tắt được).
- Sương chiến tranh theo **ô khối 8×8 ô** (≈ 29 × 21 = 609 khối, ≈ 76 byte bitset). Đi tới đâu, bán kính 12 ô quanh người chơi được vẽ. Lưu vào Player Data (`map.floor01`), ghi mỗi lần đổi vùng và khi thoát.
- Mỗi vùng có **% vẽ xong**; 100% → Người Vẽ Hải Đồ trả thưởng một lần (vật tiêu hao Common + một dấu hiệu trang trí).
- Bản đồ hiện: vùng đã biết, điểm nghỉ, Đèn Hiệu đã thắp, màu nguy hiểm từng vùng (vùng chưa tới ghi "?"), tia sáng ba Đèn khi đã thắp.
- Hải đăng, điểm ngắm, Đèn Biển Cũ **hé sẵn một mảng bản đồ**.

### 6.2 Đèn Hiệu (waypoint, điểm nghỉ, chìa khóa)

| Loại | Ở đâu | Chức năng |
|---|---|---|
| Đèn Hiệu thường | Làng Sàn, Trạm Bơm, Trạm Tiền Tiêu, Ngã Tư Xích, Pháo Đài, Đèn Biển Cũ, Đảo Chuông… (≈ 10) | Dịch chuyển nhanh, vùng 10 ô không spawn quái |
| **Đèn Ba** | Đước, Cối Xay, Xác Tàu | Như trên + mở xích Cổng Hầm + phát **tia sáng** (đường sáng trong thế giới và trên bản đồ) chỉ về Neo Cổ |
| Đèn Chính | Tháp Đèn Hiệu Thị trấn | Vùng Bảo Hộ, đổi tầng |

- Thắp bằng giữ 1,5 giây (Đèn Ba: sau khi hạ hộ vệ hoặc giải đố).
- Dịch chuyển nhanh giữa các đèn đã thắp: **không dùng khi đang giao chiến** (5 giây sau sát thương cuối), kênh 3 giây, bị hủy nếu trúng đòn; không dịch chuyển vào hầm hay phòng boss; không đổi Tầng (chỉ từ Vùng Bảo Hộ như luật `11 §2`).
- **Vùng Bảo Hộ ngoài Thị trấn:** Làng Sàn, Trạm Bơm, Trạm Tiền Tiêu (và Đảo Chuông). Các Đèn Hiệu còn lại là *Điểm Nghỉ*: không spawn quái nhưng không phải Safe Zone.

### 6.3 Nhịp Triều

Một ngày game = 1.200 giây thật (`08`), đồng bộ từ server nên **mọi người thấy cùng một mực triều**.

- Chu kỳ triều: 12 giờ game = **600 giây thật**; **triều rút** quanh 06:00 và 18:00 game, cửa sổ ±2 giờ game = **≈ 200 giây thật (3 phút 20)**; còn lại là triều lên.
- Khi triều rút: mở **Cầu Cạn → Đảo Chuông**; hiện **Rương Dưới Rong**; hiện **lối bùn** qua Đầm Sương; **Kho Neo** trong hầm cạn nước (có rương).
- UI: biểu tượng triều nhỏ ở HUD + đếm ngược; Chuông Phao ngân **60 giây trước khi triều lên**.
- **Triều không bao giờ gây sát thương hay làm chết:** nếu người chơi đứng trên lối đang ngập, đẩy nhẹ về ô khô gần nhất. Đảo Chuông có Đèn Hiệu để chờ triều rút.
- Không có snapshot thời gian hợp lệ → mặc định triều lên (đóng), để không bị lợi dụng chỉnh giờ máy.
- Lõi là hàm thuần từ `gameHour` (dễ test); phần hiển thị chỉ bật/tắt Tilemap và collider.

### 6.4 Ngày và đêm (theo `08`)

| Pha | Hiệu ứng khám phá |
|---|---|
| Bình minh | Sương mỏng, thấy xa; Mòng thả **Mảnh Hải Đồ** ở Đồi Gió |
| Ngày | Nội dung thường; NPC ở chỗ làm |
| Chiều tối | Telegraph chuyển tối; Chuông Phao nhắc |
| Đêm | Thủy Thủ Xương và Thuyền Phó Mục xuất hiện ở Nghĩa Địa Tàu; Tín Hiệu Đêm ở Thị trấn; đom đóm lộ **lối ván ẩn**; Tàu Ma ở cầu tàu (khi cũng triều rút) |

### 6.5 Điểm ngắm (vista)

4 điểm: Điểm Ngắm Đầu Tiên (Bãi Kính), Đèn Biển Cũ (Vách Đèn), Đỉnh Cối Xay (Đồi Gió), Mũi Neo. Vào vùng điểm ngắm thì camera nới từ ortho 5 lên 8 trong 1,2 giây rồi trả lại khi rời. Cần thêm trường `orthoSizeOverride` cho zone (§10.5).

### 6.5b Dấu hiệu bí mật (tells)

Không bí mật nào bắt đoán mò hoàn toàn: **tiếng chuông nhỏ** khi tới gần (≤ 8 ô), lấp lánh, dấu chân dẫn đường, vệt sương mỏng, NPC buột miệng. Bí mật **không** hiện sẵn trên bản đồ.

### 6.6 Sưu tầm

Nhật Ký Thuyền Trưởng (8 trang), Mảnh Hải Đồ (6 mảnh ghép thành vị trí kho báu), Chuông Nhỏ (5 cái giấu rải rác, đủ 5 có thưởng), kính biển và dây (nguyên liệu quest).

---

## 7. Bí mật và phần thưởng

| # | Bí mật | Vùng | Điều kiện | Dấu hiệu |
|---:|---|---|---|---|
| 1 | Đảo Chuông | Cầu Cạn | Triều rút | Tiếng chuông xa, cát lộ ra |
| 2 | Rương Dưới Rong | Bãi Kính | Triều rút | Rong biển lấp lánh |
| 3 | Lối bùn ngang Đầm Sương | Đầm Sương | Triều rút | Dấu chân dẫn xuống nước |
| 4 | Thân cây rỗng | Gốc Đước Cổ | Chặt dây leo | Dây leo rung khi lại gần |
| 5 | Lối ván ẩn | Rừng Đước | Ban đêm | Đom đóm bay thành vệt |
| 6 | Hầm Muối | Đồi Gió | Có Búa Thợ (quest) | Vết nứt vách muối, gió rít |
| 7 | Phòng ẩn sau cối xay | Đồi Gió | Chỉnh cánh quạt đúng hướng | Một cối xay quay ngược |
| 8 | Kho báu Mảnh Hải Đồ | Đồi Gió | Gom 6 mảnh (rơi lúc bình minh) | Tiếng hải âu |
| 9 | Tàu Ma | Cầu tàu Thị trấn | Đêm + triều rút | Sương đậm, tiếng còi |
| 10 | Hầm Rượu Chìm *(Stretch)* | Thị trấn | Chìa từ Làng Sàn | Nắp cống khóa sau Quán Mặn |
| 11 | Khoang Tàu Mất Tích | Nghĩa Địa Tàu | Có 3 trang Nhật Ký | Dây thừng buông từ Tàu Vỡ Lớn |
| 12 | Lối thoát Hang Sương | Vách Đèn | Xong giải đố sương | Gió thổi từ khe đá |
| 13 | Chuông Nhỏ × 5 | rải rác | Nghe và tìm | Tiếng chuông |

**Chính sách thưởng:** vật phẩm theo `itemTierId` của tầng (Common). Bí mật cho **trang trí, tiện ích, nguyên liệu hiếm, một ít vật tiêu hao**; không tặng sức mạnh vượt tầng (giữ cân bằng `07`). Số rương: ≈ 18 (10 thường, 6 bí mật, 2 trong hầm).

---

## 8. Độ khó công bằng (vì permadeath)

| Mức | Màu | Lv | Vùng |
|---|---|---|---|
| An toàn | Xanh lá | — | Thị trấn, Làng Sàn, Trạm Bơm, Trạm Tiền Tiêu |
| Dễ | Xanh dương | 1–2 | Bãi Kính |
| Vừa | Vàng | 2–4 | Đường Neo, Rừng Đước, Đầm Sương |
| Khó | Cam | 3–5 | Đồi Gió, Nghĩa Địa Tàu, Vách Đèn |
| Rất khó | Đỏ | 5+ | Mũi Neo, Hầm Neo Kẹt |

Quy tắc:

1. **Biết trước khi vào:** biển cảnh báo và sương đậm dần ở ranh vùng nguy hiểm, 15–20 ô trước ranh; banner tên vùng kèm **mức nguy hiểm + Lv khuyến nghị**; màu nguy hiểm trên bản đồ.
2. **Điểm nghỉ rải đều:** mọi điểm trên ba tuyến chính cách một điểm nghỉ hoặc Vùng Bảo Hộ **≤ 60 giây chạy (≈ 270 ô)**.
3. **Có đường lui:** mọi vùng có lối thoát về phía an toàn, quái không đuổi ra khỏi vùng của chúng.
4. **Elite đọc được:** có hào quang nhìn thấy, báo "có kẻ mạnh gần đây" trong 20 ô; không có Elite ở cửa vào vùng.
5. **Không bất ngờ:** không spawn trong 12 ô quanh Đèn Hiệu và lối vào hầm, không spawn trong 6 ô quanh người chơi; một lần chạm trán ≤ 4 quái thường.
6. **Bẫy theo `16 §7.3`:** báo trước ≥ 0,6 s, sát thương ≤ 35% máu tối đa, có dấu hiệu môi trường, không đặt ngay lối vào phòng.
7. Triều và đêm **không** gây sát thương trực tiếp (§6.3).

---

## 9. Số đo và ngân sách

| Chỉ số | Mục tiêu khởi điểm |
|---|---|
| Kích thước | ≈ 230 × 162 ô (13 × 16 màn hình) |
| Đi được | ngoài trời ≈ 15.600 ô; hầm ≈ 2.500; hiện tại ≈ 1.420 |
| Tốc độ | 4,5 ô/giây (đo lại trong game) |
| Tuyến chính | 26 / 35 / 50 giây chạy thẳng (§3.1) |
| Cứ ≤ 40 giây trên tuyến chính có một thứ mới để thấy | (điểm thú vị, cột mốc, NPC, rương, cảnh) |
| Điểm thú vị | ≈ 60: 1 thị trấn, 3 làng/trại, 4 hầm, ≈ 10 Đèn Hiệu, 4 điểm ngắm, ≈ 18 rương, ≈ 12 điểm lore, ≈ 8 cụm nguyên liệu, 5 Elite/field boss |
| Spawner | ≈ 40 nhóm; tối đa ≈ 24 quái sống cùng lúc (spawner chỉ bật gần người chơi) |
| Hiệu năng Android | giữ FPS đang có (P9.5 f); ≤ 150 draw call; ≤ 900 renderer đang bật; thêm ≤ 60 MB bộ nhớ cho cả tầng |
| Lưu bản đồ | ≤ 100 byte mỗi tầng |

---

## 10. Kỹ thuật dựng

### 10.1 Cấu trúc scene
Dưới `Floor01/`: `Zones/<Vùng>/{Props, Spawners, Volumes}`. Một `Grid` dùng chung với nhiều Tilemap: `Water` (collider), `Ground` (-10), `Detail` (-9), `Cliff` (collider), `Obstacles` (collider: thân cây, đá, chân nhà; dùng `TilemapCollider2D` + `CompositeCollider2D`), `Canopy` (trên đầu người chơi, mờ dần khi chui vào). Cây và công trình là prefab có điểm gốc ở **chân**, sắp theo trục Y (kiểm tra cấu hình sort hiện tại ở `ref/14`). Tilemap ≈ 37.000 ô không đáng ngại; điểm nặng là số renderer, nên cần §10.3.

### 10.2 Dựng từ dữ liệu, không vẽ tay
`Floor01WorldSpec` (ScriptableObject hoặc JSON): đa giác từng vùng và loại địa hình, đường đi, điểm thú vị, bảng spawn, chân công trình, bí mật. `Floor01WorldBuilder` (Editor, **thay** `BuildLayout` của `Floor01HarborSliceBuilder`; chạy lại được nhiều lần): dựng Tilemap theo đa giác, làm bờ tự nhiên bằng noise, rải cây và đá theo mật độ từng địa hình (tránh đường và lối đi), đặt `ZoneVolume`, spawner, Đèn Hiệu. Sửa map = sửa dữ liệu rồi chạy lại. `ZoneLayoutValidator` mở rộng: đến được mọi điểm thú vị (flood fill có chừa bán kính nhân vật), spawn không rơi vào nước hoặc tường, đo độ dài tuyến, đếm khoảng cách giữa các điểm thú vị, báo cáo ra Console.

### 10.3 Nạp theo vùng
`ZoneStreamer`: bật/tắt `Props` và `Spawners` của vùng khi người chơi ở trong 45 ô, trễ 10 ô để không nhấp nháy. Tilemap luôn bật.

### 10.4 Nội thất và hầm
Nội thất nhà và các hầm phụ đặt ở **dải toạ độ xa** cùng scene (hầm x ≥ 200, nhà x ≥ 400). `InteriorDoor` (chạm cửa → fade 0,25 s → dịch tới điểm vào của phòng; ra thì về đúng cửa); camera bounds theo `ZoneVolume`; không spawn quái trong nhà.

### 10.5 Thành phần và hợp đồng mới (mỗi mục cần ADR/ACR, `13 §7`)

| Mục | Nội dung |
|---|---|
| `MapRevealService` (Core, thuần) + UI Bản đồ/minimap | §6.1; round-trip tuần tự hóa có test |
| `BeaconWaypoint`, `FastTravelService` | §6.2; luật không dùng khi giao chiến |
| `TideClock` (Core/Time, thuần) + `TideZoneController` | §6.3; test biên (qua nửa đêm, đổi pha) |
| `ZoneKind` thêm (chỉ **thêm cuối**, không đổi số cũ): `Village=4, Dungeon=5, Secret=6, Vista=7, Refuge=8` | |
| `ZoneVolume` thêm: `dangerTier`, `recommendedLevel`, `orthoSizeOverride` | |
| Mục tiêu quest mới: `Discover` (tới vùng), `Interact` (thắp đèn, chạm vật) | |
| Player Data mới: `map.floor01`, `beacons.lit` | lưu sau khi xác nhận thành công, theo cách `QuestRewardLedger` |
| `ZoneStreamer`, `InteriorDoor` | §10.3, §10.4 |

### 10.6 Kiểm thử
EditMode: round-trip bản đồ, `TideClock` ở các biên, luật dịch chuyển nhanh, `Validator` đo đường đi. Chơi thử trên máy thật: danh sách ở `docs/05` theo mẫu các phase trước (bạn điền Pass).

---

## 11. Gói art

Giữ luật `ref/14`: ô 32 px, lọc Point, bảng màu xanh than, xám muối, đỏ gỉ; **vàng beacon chỉ ở vật nhỏ** (đèn, ổ khóa, vật phẩm). Thêm 4 dải màu cho cảnh mới nhưng **giảm độ bão hòa** để hợp tông sương: *đước* (xanh lục xám), *cát*, *cỏ gió* (ô liu xám), *đất sét*. Lớp sương xanh than phủ toàn cảnh để hợp nhất màu.

**Nguyên tắc tiết kiệm art:** nhà dựng bằng **bộ lắp ghép** (tường, mái, cửa, cửa sổ ghép theo chân công trình), chỉ công trình đặc biệt mới vẽ riêng. Mỗi lần giao **một lô nhỏ** như `ref/15` yêu cầu.

| Lô | Nội dung | Số hình (ước) |
|---|---|---:|
| **A. Nền** (RuleTile) | cát, cát ướt, cỏ gió, bùn đước, đường đá, vách đá + chân vách, nước nông, muối, đất sạch (ván và đá đã có) | ≈ 10 bộ |
| **B. Cây, cỏ** | đước 3 cỡ, dừa nước, thông rạp gió 2, cây chết 2, **Đước Cổ** (landmark 6×8), bụi 3, sậy 2, hoa 3 | ≈ 16 |
| **C. Bộ nhà lắp ghép** | tường gỗ/đá/tôn gỉ, mái, cửa, cửa sổ, hàng rào | ≈ 20 mảnh |
| **D. Công trình đặc biệt** | Tháp Đèn Hiệu, Cổng Tháp, cối xay ×2, trạm bơm, hải đăng đổ, xác tàu ×3, pháo đài, **Neo Cổ + xích**, cổng hầm, nhà sàn ×3, lều ×3, tháp chuông | ≈ 18 |
| **E. Đồ vật** | thùng, phuy, dây, lưới, 4 biển báo, cột đèn đường, bia, rương ×2, xuồng, mỏ neo nhỏ, ghế, giếng, giàn phơi… | ≈ 40 |
| **F. Hiệu ứng** | sương cuộn, tia sáng Đèn (cộng sáng), bọt sóng, lấp lánh kính biển, cánh quạt quay, mòng bay, đom đóm | ≈ 10 |
| **G. Nội thất** | sàn, tường 4 bộ + ≈ 15 đồ | ≈ 20 |

Tổng ≈ 130 hình. Thứ tự ưu tiên để "trông như một nơi thật": **A → B → C → E → D → F → G**. Có thể dựng blockout bằng ô màu và khối chữ nhật trước khi có art (W1).

---

## 12. Lộ trình và gói việc

Mỗi gói là một phiên làm việc (code + test + nối Unity qua MCP khi có). Thiếu thời gian thì **cắt từ dưới lên** (W11, W10, W8 cắt được; lõi là W1–W3, W5–W7, W9).

| Gói | Nội dung | Xong khi |
|---|---|---|
| **W0** | Đo tốc độ, camera, hiệu năng gốc; xác nhận cấu hình sort; chốt §14 | Có số đo, quyết định ghi vào `docs/10` |
| **W1** | **Blockout toàn tầng:** `Floor01WorldSpec` + `Floor01WorldBuilder` + ô màu xám + `ZoneVolume` + spawner theo vùng + dời boss về Mũi Neo + `Validator` | Đi được từ Thị trấn tới mọi vùng và tới Phòng Boss; Validator không lỗi; thấy rõ map lớn gấp ≈ 13 |
| **W2** | Khám phá lõi: Sổ Hải Đồ + minimap + Đèn Hiệu + dịch chuyển + biển/banner nguy hiểm + điểm ngắm | Bản đồ hiện dần, lưu/khôi phục đúng; dịch chuyển đúng luật |
| **W3** | Bến Cạn: bộ nhà lắp ghép + đồ vật + NPC + nội thất + 3 cổng | Thị trấn có phố, có nhà vào được |
| **W4** | Art lô A + B + E cho Bãi Kính, Đường Neo, Rừng Đước | Ba vùng không còn là ô phẳng |
| **W5** | Rừng Đước + Làng Sàn + Đèn Đước + Cua Giáp Mẹ + quest | Thắp được Đèn Đước |
| **W6** | Đồi Gió + Trạm Bơm + Đèn Cối Xay + Bù Nhìn Muối + giải đố | Thắp được Đèn Cối Xay |
| **W7** | Nghĩa Địa Tàu + Pháo Đài + Đèn Xác Tàu + Khoang Tàu + ban đêm | Thắp được Đèn Xác Tàu |
| **W8** | Vách Đèn + Hang Sương + Đèn Biển Cũ | Có toàn cảnh, hầm phụ chơi được |
| **W9** | Mũi Neo + Cổng Ba Đèn + Hầm Neo Kẹt (nối boss) | Thắp đủ ba đèn mở được cổng, đánh boss |
| **W10** | Nhịp Triều + Cầu Cạn + Đảo Chuông + lối bùn | Triều rút/lên đúng giờ, không ai bị kẹt |
| **W11** | Sương, nhạc theo vùng, NPC theo giờ, bí mật còn lại, cân bằng (sandbox), chơi thử | Checklist chơi thử qua |

**Lát cắt tối thiểu có hồn:** W1 + W2 + W3 + W5 + W9 (thị trấn thật, rừng đước, Ba Đèn rút gọn còn 1 đèn, hầm và boss).

**Chơi thử (đo cảm giác phiêu lưu):** người chơi mới tự tìm được ≥ 3 bí mật trong 45 phút mà không cần chỉ dẫn; không có đoạn tuyến chính > 40 giây mà không thấy gì mới; không điểm nào cách điểm nghỉ > 60 giây; không ai kẹt vì triều.

---

## 13. Lệch với docs hiện có (cần ADR/ACR trước khi làm)

| # | Docs | Nội dung docs | Đề xuất ở file này |
|---|---|---|---|
| 1 | `16 §7.1`, `05` P9.7a | 3 vùng (Thị trấn, Farm, Hầm); đi bộ 20–30 s | 11 vùng; mỗi tuyến 26–50 s + Ba Đèn |
| 2 | `16 §7.1` quy tắc 1 | Chỉ Thị trấn là Safe Zone | Thêm 3 Vùng Bảo Hộ nhỏ (Làng Sàn, Trạm Bơm, Trạm Tiền Tiêu) + Đảo Chuông |
| 3 | `05` P9.7b | Không khóa cửa boss | Cổng **Hầm** khóa bằng Ba Đèn; Phòng Boss vẫn không khóa, luật rematch giữ nguyên |
| 4 | `13 §4` freeze | `FloorDefinition`, `ZoneKind`, `ZoneVolume`, quest | Thêm trường/giá trị mới (§10.5), chỉ thêm, không đổi cũ |
| 5 | `03`/Player Data | Chưa có dữ liệu bản đồ | `map.floor01`, `beacons.lit` |
| 6 | `ref/14` | Bảng màu, tile 32 px | Thêm 4 dải màu giảm bão hòa; bộ nhà lắp ghép |
| 7 | `16 §6.2` | Cua Bắn Bọt, Thủy Thủ Xương, Thuyền Phó Mục chỉ là đề xuất | Dùng làm quái của vùng; thêm 3 Elite hộ vệ |
| 8 | Slice hiện tại | `Floor01HarborSliceBuilder` | Giữ làm menu cũ; `Floor01WorldBuilder` thay thế |

---

## 14. Quyết định cần chủ dự án (kèm mặc định đề xuất)

1. **Cỡ map:** *Lớn* ≈ 230×162 dựng theo giai đoạn (đề xuất) hoặc *Vừa* ≈ 75% (≈ 9.000 ô đi được).
2. **Khóa Cổng Hầm bằng Ba Đèn:** có (đề xuất, vì chính nó biến map thành hành trình) hoặc không (cổng mở sẵn, ba đèn chỉ là phần thưởng).
3. **Nhịp Triều:** có, làm ở W10 (đề xuất, cắt được nếu thiếu thời gian).
4. **Vùng Bảo Hộ ngoài Thị trấn:** đồng ý 3 điểm nhỏ (đề xuất).
5. **Art:** ai tạo cây, nhà, đồ vật (art AI hay bạn cung cấp) và phong cách: pixel 32 px như Player và nền hiện tại, hay vẽ tay chi tiết như bộ boss. Nên chọn **một** để toàn tầng đồng nhất.

---

## Phụ lục A — Toạ độ để dựng (ô; gốc = tâm Thị trấn; +x Đông, +y Bắc)

**Vùng đa giác**

| Vùng | Điểm (x,y) |
|---|---|
| Bến Cạn | (-28,-14) (28,-14) (28,20) (-28,20) |
| Bãi Kính Biển | (-29,-12) (-36,-20) (-52,-27) (-72,-27) (-84,-20) (-88,4) (-86,24) (-62,26) (-34,28) (-29,20) |
| Rừng Đước Sương | (29,-13) (34,-24) (52,-31) (74,-32) (90,-25) (95,-8) (97,12) (94,30) (60,30) (30,31) |
| Đồi Gió Cối Xay | (24,33) (24,64) (34,80) (58,86) (80,86) (100,80) (106,62) (102,44) (92,33) (60,32) |
| Nghĩa Địa Tàu | (-85,28) (-84,60) (-76,80) (-58,84) (-42,82) (-33,72) (-33,40) (-38,30) (-60,26) |
| Vách Đèn | (-33,70) (-10,68) (2,76) (4,96) (-2,112) (-14,120) (-30,116) (-40,100) (-42,84) |
| Mũi Neo | (12,100) (22,90) (44,88) (70,92) (86,102) (88,118) (78,128) (52,130) (28,126) (14,114) |

**Hình tròn, chữ nhật:** Đầm Sương = elip tâm (-14,48), bán kính 17 × 15 · Đảo Chuông = elip tâm (-112,6), 11 × 12 · Cầu Cạn = chữ nhật (-101,3)–(-88,8) · Làng Sàn = (53,-10)–(71,3) · Trạm Bơm = (68,52)–(83,64) · Trạm Tiền Tiêu = (24,92)–(37,102).

**Đường**

| Tuyến | Điểm |
|---|---|
| Đường Neo | (0,20) (4,40) (6,58) (10,72) (18,84) (28,94) (30,97) (40,100) (52,106) (62,106) |
| Vòng Tây | (-28,2) (-60,10) (-62,27) (-58,50) (-45,76) (-30,92) (-12,104) (10,106) (24,98) |
| Vòng Đông | (28,2) (53,0) (62,14) (62,30) (66,50) (75,58) (90,72) (80,92) (70,98) (62,106) |
| Cầu Ván Đầm | (5,52) (-14,52) (-30,56) |

**Điểm chính**

| Điểm | (x,y) | Điểm | (x,y) |
|---|---|---|---|
| Tháp Đèn Hiệu (Thị trấn) | (0,3) | Neo Cổ | (62,118) |
| Làng Sàn | (62,-4) | Cổng Hầm | (62,106) |
| Đèn Đước (Gốc Đước Cổ) | (80,22) | Ổ Cua | (46,10) |
| Trạm Bơm | (75,58) | Đèn Cối Xay | (92,70) |
| Cối xay (cụm) | (68,62) | Hầm Muối | (45,45) |
| Pháo Đài / Đèn Xác Tàu | (-66,76) | Tàu Vỡ Lớn (Khoang Tàu) | (-58,48) |
| Trạm Tiền Tiêu | (30,97) | Hang Sương (cửa) | (-32,96) |
| Đèn Biển Cũ | (-12,108) | Điểm Ngắm Đầu Tiên | (-62,22) |
| Ngã Tư Xích | (10,72) | Đảo Chuông | (-112,6) |
| Rương Dưới Rong | (-70,-10) | Tàu Ma (cầu tàu 3) | (0,-18) |
