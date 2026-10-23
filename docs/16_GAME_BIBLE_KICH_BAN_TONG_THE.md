# 16 — GAME BIBLE (v3): KỊCH BẢN & HỆ THỐNG TỔNG THỂ
## Game2D "Tháp Thủy Triều" — khớp `11`, `05`, `10`, `13` và hợp đồng đồ họa `14`/`15` (bản 2026-10-17)

> **Trạng thái: ĐỀ XUẤT v3 (2026-10-17).** Đổi số file từ 14 sang 16 vì `14_ART_CONTRACT.md` và `15_ART_HANDOFF.md` đã dùng số 14 và 15. Chưa phải contract; không thay `AGENTS.md`, `06`, `13`. Mục nào đổi contract đều ghi **ACR** (mẫu `13 §7`) hoặc **ADR** (`01 §6`); số thứ tự ADR do Integrator gán.
> **Cập nhật 2026-10-20 (product owner):** v3 được duyệt làm HƯỚNG SẢN PHẨM (ADR-031). AI được tự chọn và làm các mục ở §12 theo thứ tự giá trị mà không cần hỏi; mục đổi contract ghi ADR ngắn ở `01` §6 rồi làm (không cần chờ ACR hay Integrator); 16 điểm xung đột ở §3 do AI tự quyết theo hướng "giống thể loại ở mức cơ chế". Luật 7, 9, 10 giữ nguyên.
> **Khác bản v2:** số phase và số FB khớp `05`/`10`/`13` bản 2026-10-17 (Phase 9.5–9.9, FB-021 đến FB-033); đồ họa theo `14` và `15` (bỏ mục hợp đồng đồ họa riêng của mình); sửa các chỗ mình viết lệch với code: quái hiện chưa có role (`CombatRole` ở Phase 9.8), Floor 2 chưa có boss, đi bộ Thị trấn → Cổng Hầm khoảng 20–30 giây, hướng nhìn 8 hướng thay cho "nón 60°".
> **Đã đối chiếu:** 01, 02, 03, 04 (chưa đọc kỹ mục 15–38), 05, 06, 07, 08, 09, 10, 11, 12, 13, `14_ART_CONTRACT`, `15_ART_HANDOFF`. Chưa đọc code thật; chỗ nào lấy từ `05`/`13`/`14` đã ghi rõ.
> Mọi con số là **số khởi điểm**, phải chạy qua sandbox theo `07 §9` trước khi vào asset. Số `§` theo bản docs lúc đọc; nếu đánh số đã đổi, tra theo tiêu đề mục.

---

## 0. Cách dùng và ranh giới nguyên bản

**Cách đọc.** §1–3 chốt hướng và xung đột · §4–9 nội dung và hệ thống · §10 kịch bản Tầng 1 · §11–15 trình bày, lộ trình, bản đồ sửa docs, thuật ngữ.

**Nguyên tắc: giống ở cơ chế và cảm giác, khác ở mọi biểu đạt.** Tác phẩm cảm hứng là SAO; tài liệu này chỉ mượn "chất" của thể loại game leo tháp sinh tử.

| Giữ (là cơ chế/thể loại) | Không dùng (là biểu đạt của tác phẩm gốc) |
|---|---|
| Tháp nhiều tầng, hạ boss mở tầng kế · chết là mất · kiếm kỹ có tư thế–dẫn–khựng · mastery mở kỹ năng · ô kỹ năng giới hạn · thị trấn an toàn · boss học qua thăm dò · party/raid | Tên nhân vật, địa danh, tên kỹ năng, tên và thứ tự boss gốc · cảnh và biến cố cốt truyện gốc · UI, nhạc, icon, sprite · thiết kế quái đặc trưng của tác phẩm |

Quyết định của product owner (2026-10-13): chỉ nới ở mức cơ chế/thể loại. `11 §7` và `12 §3` hiện cấm cả cảm hứng ở mức đó, nên câu chữ của hai mục này phải sửa cho khớp (§13). Phép thử nguyên bản ở §14.

---

## 1. Tầm nhìn

> *Bạn ký giao ước thám hiểm Tháp Thủy Triều. Mỗi Floor giữ một chiếc Neo; hạ được người giữ Neo, tuyến hải đồ kế tiếp mở ra — cho cả những người đến sau. Nhưng nếu thất bại ngoài vùng bảo hộ, quyền truy cập của bạn bị niêm phong vĩnh viễn.*

Cảm giác mục tiêu: **căng thẳng có chủ đích** (mỗi trận có thể là trận cuối) · **kiếm kỹ có trọng lượng** · **thế giới có không gian và nhớ người đi trước**.

Năm trụ cột "chất Tháp" — bổ sung, không thay, bốn trụ cột ở `12 §2`:

| # | Trụ cột | Trạng thái |
|---|---|---|
| 1 | **Lối ra duy nhất là đi lên** — boss mở tuyến cho cả thế giới, thất bại ngoài vùng bảo hộ là niêm phong | Đã có (Phase 5.5, 8) |
| 2 | **Kiếm kỹ có trọng lượng** — Khởi thế → Lướt dẫn → Khựng → Nối Thế | Có sườn; thiếu phản hồi cảm giác (Phase 9.5), Nối Thế, lựa chọn (Mốc 2) |
| 3 | **Học Tháp bằng cái giá của người khác** — thăm dò boss, tin Người Vẽ Hải Đồ, Bia Neo, Dư Âm | Chưa có |
| 4 | **Tháp là nơi sống và có không gian** — thị trấn, vùng farm, hầm boss tách biệt; ngày/đêm; NPC có chuyện riêng | Một phần (Time, Quest, NPC); thiếu phân vùng → Phase 9.7 |
| 5 | **Cùng leo** — thang từ solo → hiện diện → bạn đồng hành → phòng boss hợp tác → Floor dùng chung (§8) | Mốc 2–3 |

---

## 2. Bảng quy đổi: "chất" thể loại → giữ thế nào, tên nguyên bản là gì

| # | Yếu tố làm nên "chất" | Docs hiện có | Cách giữ (tên nguyên bản) | Mốc |
|---|---|---|---|---|
| 1 | Tháp nhiều tầng; hạ boss mở tầng kế cho cả thế giới | Có: `11 §2` (Shared route), `03 §6`, Phase 8 | **Nhổ Neo** — boss tầng là *Giữ Neo*; hạ nó là nhổ Neo, tuyến kế mở. Giữ nguyên kỹ thuật, thêm nghi thức (§7.1) | 1 ✓ |
| 2 | Chết là mất; không hồi sinh | Có: `11 §2` (Access seal), `09 §1` | **Niêm Phong** — quyền truy cập bị niêm phong; hiệu ứng tan thành ánh sáng (§11) | 1 (chữ + VFX) |
| 3 | Kiếm kỹ: tư thế khởi động → hệ thống dẫn đòn → khựng sau đòn | Có sườn: `12 §4.2, §6`, `07 §4` | **Khởi thế → Lướt dẫn → Khựng**; thêm phản hồi cảm giác (§5.1) | 1 (Phase 9.5) |
| 4 | Nối kỹ thuật bằng tư thế khớp nhau | Chưa | **Nối Thế** qua `cancelPolicy` (§5.2) | 2 · ACR |
| 5 | Mastery mở kỹ năng; ô giới hạn buộc phải chọn | Có mastery; chưa có lựa chọn trong một band | 2 kỹ thuật mỗi band + **Ô Thế** (§5.3) | 2 · ACR |
| 6 | Kỹ năng phụ (tìm kiếm, ẩn thân, nấu ăn, rèn…) | Chưa | **Thuật Phụ** (§5.8) | 2 · ACR |
| 7 | Kỹ năng độc nhất mở bằng chuỗi nhiệm vụ cốt truyện | Chưa | **Thế Độc Môn — Song Thế** (§5.7) | 3 |
| 8 | Boss phải thăm dò, học pattern; có người bán tin | Có pattern 3 pha (`12 §7.2`); chưa có "thăm dò" | **Sổ Thăm Dò**, *Người Vẽ Hải Đồ*, *Mòng* (§7.4) | 1 (chữ) · 2 |
| 9 | Hầm có nhiều phòng, bẫy, mini-boss, cửa boss | Dungeon tối giản (`02 §17`) | **Hầm Boss** tối giản (Mốc 1), mê cung đầy đủ (Mốc 2) (§7.2) | 1 · 2 |
| 10 | Boss cột mốc khó hơn hẳn (kiểu tầng 25/50/75/100) | Chưa | **Trấn Thủ** ở tầng 5, 10 (MVP) → 25/50/75/100 (§6.3) | 2 |
| 11 | Bia tưởng niệm, thế giới nhớ người đã mất | Chưa; `09` cấm death ghi World State | **Bia Neo** + **Dư Âm** (§4.6) — cần ADR | 2–3 |
| 12 | Đồng đội: hoán vị, đòn chốt, raid | Chưa; `12 §1` là PvE solo | Thang cùng leo: hiện diện → bạn đồng hành AI → Phòng Boss hợp tác → Floor dùng chung (§8) | 2–3 |
| 13 | **Thế giới có khu vực**: thị trấn an toàn, nơi farm, hầm boss | Chưa: Phase 7 chỉ có Safe Zone, spawn, NPC, boss reference | **Phân vùng Tầng**: Thị trấn / Vùng Farm / Hầm Boss (§7.1) | **1 (Phase 9.7)** |
| 14 | Vùng an toàn + dịch chuyển nhanh | Safe Zone có (`09 §1.2`); chưa có waypoint | **Vùng Bảo Hộ** (= Safe Zone) + **Đèn Hiệu** (waypoint) | 1–2 |
| 15 | PK, người chơi "đỏ" | — | **Không làm.** Duel đồng thuận Half-Loss (`09 §3`) đã đủ; PK cộng permadeath không hợp mobile | — |
| 16 | Tên, nhân vật, kỹ năng, boss, cảnh, UI, nhạc của tác phẩm gốc | — | **Không dùng** (`11 §7`, §14) | — |

---

## 3. Điểm lệch và xung đột cần quyết định TRƯỚC khi sửa docs

(Theo `AGENTS.md`/`06 §9`: docs lệch nhau thì báo lệch, không tự cho bên nào đúng.)

| # | Điểm lệch | Nguồn | Đề xuất |
|---|---|---|---|
| 1 | `11 §7` và `12 §3` cấm dựa vào IP ngoài; product owner chọn nới ở mức cơ chế/thể loại | `11 §7`, `12 §3` | Sửa câu chữ: cho phép cảm hứng cơ chế/thể loại; vẫn cấm tên, nhân vật, cốt truyện, danh sách kỹ năng/boss, UI, nhạc, asset. Ghi ADR |
| 2 | `11 §2`: không mô tả permadeath như tổn hại cơ thể thật | `11 §2` | v2 dùng "Niêm Phong" (quyền truy cập) và **bỏ** twist "ý thức bị luồn vào xích" của v1; bí ẩn cuối giữ mở như `11 §1` |
| 3 | Bảng Nhổ Neo, `world_echoes` cần dữ liệu toàn cục; death không ghi World State | `03 §6`, `09 §1` | MỘT key trang trí do Cloud Code ghi, không ảnh hưởng boss/tầng/thưởng. ADR + test hai account |
| 4 | Phòng Boss hợp tác dùng mạng cho PvE; `04 §44` tách PvE/PvP, `04 §45` để "nhiều người cùng một arena" cho Mốc 3 | `04 §44–45` | Cần ADR: cho phép PvE dùng lớp Lobby/Relay qua interface chung. Chỉ làm sau khi Duel (Phase 10) chạy ổn trên máy thật |
| 5 | `12 §1` PvE solo; `05 §4` nhiều người trong Floor là Mốc 3 | `12 §1`, `05 §4` | Giữ solo ở Mốc 1; thang cùng leo ở §8 |
| 6 | Gạt Đòn (parry) có thể bị xem là CC/né an toàn | `07 §10` | Không bất tử; stagger ngắn, có điều kiện; hụt thì recovery đầy đủ. Cần test |
| 7 | Field mới cho `WeaponTechniqueDefinition` (pose, chain) và `FloorDefinition` (vùng) | `13 §4` freeze | ACR (§5.2, §7.1) |
| 8 | Chuỗi chữ bảng báo boss đã cố định ở P8-5 | `05` P8 | Không đổi; giọng hệ thống là toast riêng (§10.3) |
| 9 | Hazard riêng từng tầng vs "Floor 2 data-only" | `05` P9 | Tầng 2 ở Mốc 1 chỉ khác quái, bản đồ, quest; hazard cần `PvE/Mechanics` nên để Mốc 2 |
| 10 | AI không tự sinh tính năng | `10 §1` | Mọi mục mới ghi vào `10` (đã có FB-021 đến FB-033; mục nào còn "Chờ quyết" thì chưa làm), chờ product owner duyệt |
| 11 | Phase 9.5–9.9 trước Duel có vẻ trái luật 3 của `05` | `05 §1` | Đã giải quyết bằng luật 9 và 10 của `05` (product owner duyệt 2026-10-14) |
| 12 | Code chưa có role cho quái (`EnemyDefinition` không có `CombatRole`); `12 §7.1`, `07 §5`, `11 §3` đã mô tả role | `05` P9.8, FB-030 | Bảng quái ở §6.2 là thiết kế; role thật chỉ có sau Phase 9.8 |
| 13 | Floor 2 chưa có boss nên chưa có Hầm Boss; Phase 9.7b không khóa cửa phòng boss mặc định | `05` P9.7 | §7 bám đúng 9.7; Trấn Thủ khóa cửa là Mốc 2, cần quyết định riêng và ACR |
| 14 | Phòng Boss hợp tác và luật rematch không EXP | `10` FB-019, FB-027 | Khi làm FB-027 phải đổi cách tính EXP sang từng account |
| 15 | Hướng nhìn 8 hướng thay cho "nón 60°" của bản v2 | `05` P9.5, `10` FB-032/033 | §5.1 đã sửa; tự nhắm là tùy chọn, mặc định tắt (FB-033) |
| 16 | Giá trị màu telegraph và aura | `14` §2 | §11 dẫn thẳng `14` §2, không lặp số ở đây |

---

## 4. Thế giới và cốt truyện (khớp `11`)

> `11 §1`: Mốc 1 chỉ kể qua môi trường, vật phẩm và vài NPC; không cần phản diện lớn hay giải thích toàn bộ bí ẩn. Vì vậy §4.5–4.7 là định hướng dài hạn, không vào Mốc 1–2.

### 4.1 Tiền đề

**Luminport** là thành phố cảng vận hành công trình thăm dò bí mật **Tháp Thủy Triều**. Mỗi Floor là một mô phỏng địa hình được nuôi bằng ký ức vật chất của biển, xưởng máy và các đoàn tàu đã mất tích. Người tham gia ký **giao ước thám hiểm**: thất bại ngoài vùng bảo hộ thì quyền truy cập bị **niêm phong** vĩnh viễn. Không ai biết người xây Tháp muốn khai thác năng lượng, lưu giữ ký ức hay tìm một lối đi dưới biển sâu.

Lớp thêm của v2 ("chất leo tháp"): mỗi Floor giữ một **Neo** — thiết bị neo mô phỏng xuống đáy biển. Hạ **Giữ Neo** (boss tầng) là **nhổ Neo** và mở tuyến hải đồ kế tiếp cho tất cả mọi người (luật *Shared route* của `11 §2`). Boss Tầng 1, "cỗ máy kéo neo" (`11 §3`), chính là chiếc Neo đầu tiên.

Tiếng nói của Tháp là **Chuông Phao**: hệ thống thông báo ngân theo thủy triều, mỗi hồi chuông gắn với một sự kiện (§10.3).

### 4.2 Luật của Tháp → hệ thống

| Luật (`11 §2`) | Diễn giải | Hệ thống | Ghi chú |
|---|---|---|---|
| **Niêm Phong** (Access seal) | Thất bại PvE ngoài vùng bảo hộ niêm phong quyền truy cập của identity hiện tại | `IDeathService`, `death.isDead` (`09 §1`) | Chuông Phao nói "quyền truy cập đã niêm phong"; không mô tả tổn hại thân thể |
| **Vùng Bảo Hộ** (Beacon refuge) | Đèn hải lưu đánh dấu vùng bảo hộ có dịch vụ | `SafeZoneService` | Chỉ Thị trấn là Safe Zone (§7.1) |
| **Nhổ Neo** (Shared route) | Boss bị hạ mở tuyến kế cho mọi người | `IWorldStateService` | Thông báo toàn Tháp ở lần resync kế (Mốc 2) |
| **Nhịp Triều** (Tide clock) | Chu kỳ ánh sáng riêng của Tháp | `ITimeService` (`08`) | Đêm: quái biến thể, Field boss |
| **Đấu Cảng** (Trial arena) | Duel là mô phỏng an toàn, kết thúc ở ngưỡng HP | Half-Loss RuleSet (`04`, `09 §3`) | Không permadeath |
| **Người Đến Sau** | Mỗi đợt người mới ký giao ước và thừa hưởng tuyến đã mở | Guest Reset / account mới (`09 §2`) + World State | Giải thích vì sao account mới vẫn thấy Tầng 2 đã mở |
| **Đèn Hiệu** | Điểm neo ánh sáng để ghi nhớ nơi đứng và dịch chuyển trong tầng | Waypoint (§7.1) | Mốc 2 |
| **Bia Neo, Dư Âm** | Tháp giữ dấu vết những người đã bị niêm phong | World State trang trí (§4.6) | ADR |

### 4.3 Phe phái

| Phe | Muốn gì | Vai trò trong game |
|---|---|---|
| **Liên Quân Tiên Phong** | Vượt tầng nhanh, chấp nhận hy sinh có kế hoạch | Nguồn nhiệm vụ chính, raid (Mốc 3); đối trọng đạo đức |
| **Thương Hội Cảng** | Giữ Luminport vận hành: rèn, vớt phế liệu, buôn | Nghề, cửa hàng, cường hóa; nhiệm vụ đời sống |
| **Hội Hải Đồ** | Vẽ lại tuyến; tin tức là quyền lực; trung lập | Bán tin boss/hầm, Sổ Thăm Dò, quest ẩn |
| **Hội Lặng** | Tin niêm phong là sự yên nghỉ, Tháp không cần bị vượt | Phản diện mềm Chương 3; nhiệm vụ đạo đức xám |

### 4.4 NPC (vai theo `11 §4`; tên riêng là tùy chọn)

Mọi NPC phải có `NPCDefinition` và dialogue/quest ID riêng (`11 §4`). NPC đã có trong game (ví dụ "Người Vẽ Hải Đồ" ở Tầng 2) giữ nguyên tên; tên riêng chỉ thêm nếu bạn muốn.

| NPC | ID gợi ý | Tên riêng | Vai trò | Chiều sâu |
|---|---|---|---|---|
| **Người Giữ Đèn** (Beacon Keeper) | `npc.beacon_keeper` | Tạ Hải | Tutorial, Vùng Bảo Hộ, quest turn-in; dạy Blade | Thuộc Đợt Đầu; mỗi tối đếm số người về; từng mất cả đội ở Tầng 5 (hé lộ ở Chương 1) |
| **Người Vẽ Hải Đồ** (Cartographer) | `npc.cartographer` | Bà Muối | Chọn Tầng, giải thích World State; bán tin boss (Sổ Thăm Dò) | Trả tin bằng "một chuyện chưa ai biết" |
| **Người Vớt Phế Liệu** (Salvager) | `npc.salvager` | Thạch Mộc | Quest vật liệu; rèn và cường hóa (Mốc 2) | Tin "thép nhớ chủ" |
| **Tín Hiệu Đêm** (Night Signal) | `npc.night_signal` | — | Chỉ xuất hiện một phase; quest phụ time-aware, không chặn quest chính (`08 §4.4`) | Mốc 2: nơi Dư Âm lên tiếng |
| **Mòng** | `npc.mong` | — | Hải âu cơ khí: hướng dẫn, trinh sát (Mốc 2) | Bí mật: mảnh còn lại của một hệ thống báo hiệu cũ từ chối giao thức (Chương 5) |
| **Vân Kha** | `npc.van_kha` | — | Bạn đồng hành (Mốc 2) | Từ chối vào Liên Quân vì không muốn ai phải chết theo mình |
| **Người Khắc Bia** | `npc.stone_carver` | — | Giữ Bia Neo (Mốc 2) | Khắc tên người đã bị niêm phong |
| **Sư Lặng** | `npc.silent_prior` | — | Lãnh đạo Hội Lặng (Chương 3) | Mất người thân vì niêm phong; tin họ đã yên nghỉ |

### 4.5 Năm chương cho 100 tầng (MVP chỉ làm Chương 1)

| Chương | Tầng | Chủ đề | Beat chính | Chất liệu |
|---|---|---|---|---|
| 1 — Bến Cạn | 1–10 | Học luật của Tháp | Neo đầu tiên (T1); Trấn Thủ tầng 5; tầng 10: Bia Neo có tên khắc từ trước ngày ký giao ước | Cảng, sương, lò hơi, muối |
| 2 — Xưởng Máy | 11–25 | Phe phái hình thành | Liên Quân và Thương Hội; tầng 25: Trấn Thủ tàn khốc, một đoàn bị diệt → Hội Lặng nổi lên | Xưởng, băng tải, máy móc |
| 3 — Hội Lặng | 26–50 | Bí ẩn niêm phong | Dư Âm lên tiếng; Thế Độc Môn; tầng 50: Giữ Neo mang mảnh trang bị của người chơi | Muối, gương, linh ảnh |
| 4 — Nước Dâng | 51–75 | Biển sâu | Áp suất, ngập; phe phái rạn nứt; tầng 75: lựa chọn của Người Giữ Đèn | Biển sâu, đoàn tàu mất tích |
| 5 — Đáy Tháp | 76–100 | Nguồn gốc Tháp | Mòng tiết lộ; tầng 100: quyết định cuối | Nguồn năng lượng, ký ức |

### 4.6 Bia Neo và Dư Âm (cần ADR — Mốc 2–3)

- **Bia Neo:** bia đá ở Thị trấn mỗi tầng, hiện số người đã bị niêm phong ở tầng đó và tên hiển thị của những người đồng ý. Người Khắc Bia là NPC gắn với bia.
- **Dư Âm:** nơi một người bị niêm phong, tầng đó có thể xuất hiện một bóng mờ. Chạm vào nhận một dòng nhắn chọn từ danh sách câu có sẵn (không chat tự do) và một vật phẩm nhỏ hoặc gợi ý đúng một pha boss. Tín Hiệu Đêm là nhân vật kể về chúng.
- **Dữ liệu:** một key trang trí `world_echoes` (Game Data, Access Default), do Cloud Code ghi sau khi bản ghi chết đã được xác nhận. Ẩn danh theo mặc định, chỉ hiện tên khi người chơi bật; mỗi tầng giữ tối đa 50 mục gần nhất. **Không** ảnh hưởng boss, tầng hay thưởng — giữ tinh thần `09`.
- **Rủi ro cần xử lý:** dữ liệu cá nhân, spam, lạm dụng lời nhắn. Vì vậy: opt-in hiển thị tên, câu nhắn từ danh sách đóng, giới hạn số mục mỗi tầng.

### 4.7 Bí ẩn cuối — định hướng (không vào Mốc 1–2)

`11 §1` để ngỏ mục đích của Tháp (năng lượng, ký ức hay lối đi dưới biển sâu). Ở tầng 100, người chơi tự chọn Tháp sẽ trở thành gì:

| Kết thúc | Hành động | Hệ quả |
|---|---|---|
| **A — Trả Quyền Truy Cập** | Nhổ chiếc Neo cuối | Mọi niêm phong được gỡ; Tháp rời bến và lộ lối đi dưới biển; Chuông Phao ngân hồi cuối; Mòng ở lại với người chơi |
| **B — Giữ Tháp Vận Hành** | Giữ Neo cuối | Tháp tiếp tục nuôi mô phỏng; ai ở lại thì an toàn nhưng không bao giờ ra; Chuông Phao tiếp tục ngân mỗi ngày |

Đã bỏ so với v1: twist "ý thức bị luồn vào xích" (xung đột `11 §2`).

### 4.8 Cách kể chuyện không cần cutscene tốn kém

1. **Giọng Chuông Phao** — thông báo hệ thống bằng chữ (§10.3): rẻ nhất, đậm chất nhất.
2. **Hội thoại + Quest** — dùng đúng Quest system sẵn có (Talk/Kill/Collect/Reach, `12 §8`).
3. **Bia Neo và Dư Âm** — lore ngắn, người chơi tự gặp.
4. **Mô tả vật phẩm** — flavour text của vũ khí, mảnh trang bị boss.
5. **Intro boss** bằng thẻ chữ và cổng phòng boss.
6. **Nhật ký Mòng** — mỗi hầm/boss mở thêm một trang, đọc ở Thị trấn.

---

## 5. Kiếm Kỹ (hệ thống vũ kỹ)

Nguyên tắc: **không đổi contract `07`/`12` ở Mốc 1.** Phần nào cần field mới thì đi qua ACR (`13 §7`).

### 5.1 Vòng lặp một kỹ thuật — thêm phản hồi cảm giác (Mốc 1, polish)

| Pha | Dữ liệu (đã có, `07 §4`) | Phản hồi người chơi (2D, cảm ứng) | Rủi ro |
|---|---|---|---|
| **Khởi thế** | `windUpSeconds` | Vũ khí phát sáng màu Discipline; hiện vệt hướng lướt ngắn trước mặt. Hướng đòn là hướng nhìn 8 hướng (theo joystick, giữ nguyên khi thả); tự nhắm quái gần nhất là tùy chọn, mặc định tắt (`05` P9.5, FB-033) | Đòn quái vẫn trúng bạn |
| **Lướt dẫn** | `activeWindows`, `movementPolicy` | Nhân vật lướt theo hướng đã khóa; dừng hình tối đa 0.06 s cho mỗi đòn xác nhận (qua `PlaySpeedState`, `05` P9.5); rung nhẹ; âm "ngân kiếm" cao dần theo hit | Không bất tử (`07 §10`) |
| **Khựng** | `recoverySeconds` | Vầng sáng tắt, nhân vật "thở ra"; nút kỹ thuật xám có vòng đếm | Quái có punish window |
| **Nối Thế** (Mốc 2) | `chainWindowSeconds` (mới) | Viền nút sáng đúng khoảng cửa sổ nối | Phạt chuỗi (§5.2) |

Ghi chú thiết kế: cảm giác "hệ thống dẫn đòn" đến từ **phản hồi tức thì** (sáng, âm, hướng), không từ windUp dài. Giữ windUp ngắn (0.05–0.20 s ở Core/Traverse) và dùng ánh sáng để báo. Màu aura Discipline **không dùng vàng, cam, tím** — đã dành cho telegraph (giá trị thật ở `14_ART_CONTRACT.md` §2) — để không nhầm khi đọc màn hình (`12 §2`, trụ cột "Đọc được").

### 5.2 Nối Thế — ACR soạn sẵn (Mốc 2)

```text
Current contract: WeaponTechniqueDefinition có cancelPolicy nhưng không có pose hay cửa sổ nối
  (12 §6.3, 13 §4.4). 12 §4.2 quy tắc 4: cancel chỉ hợp lệ nếu definition cho phép.
Problem proved by: (cần playtest) kỹ thuật rời rạc, ít chiều sâu thao tác.
Smallest proposed change: thêm vào WeaponTechniqueDefinition
    startPose, endPose     (enum Pose: High, Low, Thrust, Spin, Guard)
    chainWindowSeconds     (0 = không nối được)
    cancelPolicy = ChainOnMatch
  Luật: nối được khi A đang ở [cuối Active … +chainWindowSeconds] và B.startPose == A.endPose.
    B bỏ phần Recovery còn lại của A; windUp của B ×0.5.
    Mắt xích thứ n (n = 1..2) nhân multiplier ×0.9^n; tối đa 3 kỹ thuật một chuỗi.
    Cooldown của B tính bình thường. Tổng quãng dịch chuyển chuỗi ≤ maxChainTravel (config).
    Không nối sau Dodge; không nối khi vừa bị trúng đòn.
Affected modules/files: Core/Skills (definition, executor), PvE/Skills, UI/Skills (viền sáng), tests.
Migration/save impact: không (static data).
Tests added or changed: pose khớp/không khớp; trần 3 mắt xích; Σ multiplier không vượt trần band
  (07 §4.2); cooldown độc lập; không nối sau Dodge; không nối khi bị trúng.
Alternatives rejected: nối tự do (phá 07 §10); combo không pose (mất kỹ năng đọc thế).
```

### 5.3 Ô Thế và Loadout (Mốc 2 · ACR)

- Bốn input slot = bốn nút hiện có (Core / Traverse / Control / Signature).
- Mỗi band mở **2 kỹ thuật** có cùng `masteryRequired` (0/25/55/85 — không đổi `07 §3`). Người chơi chọn một kỹ thuật cho mỗi slot; đổi được ở Thị trấn (Vùng Bảo Hộ), không đổi giữa trận.
- Lưu: Player Data key `loadout`, ví dụ `{ "blade": { "Core": "technique.blade_core", "Traverse": "technique.blade_traverse" } }`. Chỉ lưu **lựa chọn**, không lưu "đã mở" — availability vẫn suy ra từ mastery (`03 §3`, §15).
- Kỹ thuật đã chọn không còn hợp lệ → dùng mặc định của slot.

### 5.4 Thuần Thục (mastery)

Giữ nguyên `07 §3` (0–100, credit, trần 4 mỗi enemy). Thêm **Thử Thế**: mỗi mốc 25/55/85 có một quest nhỏ của NPC dạy thế — chỉ là lore và thưởng cosmetic, **không** là điều kiện mở khóa, để giữ contract "availability = mastery ≥ required".

### 5.5 Catalog Blade (Mốc 1 ✓ + Mốc 2)

Số nằm trong dải `07 §4.1–4.2`. Σ = tổng hit multiplier, không vượt `totalMultiplier`.

| ID | Tên tạm | Band · Mastery | CD (s) | WindUp (s) | Recovery (s) | Σ mult | Chuyển động | Ghi chú |
|---|---|---|---:|---:|---:|---:|---|---|
| `technique.blade_core` | Trảm Thuận | Foundation · 0 | — | — | — | — | — | **Đã có (Mốc 1)** — giữ nguyên số trong asset |
| `technique.blade_core_b` | Nhịp Đôi | Foundation · 0 | 2.8 | 0.10 | 0.30 | 1.25 (0.60+0.65) | Step ngắn | Hai hit; Mốc 2 |
| `technique.blade_traverse` | Lướt Ngang | Traverse · 25 | 4.5 | 0.12 | 0.38 | 1.20 | Step | **Đã có (Mốc 1)**, khớp `07 §4.3` |
| `technique.blade_traverse_b` | Chém Vọt | Traverse · 25 | 5.5 | 0.15 | 0.45 | 1.10 | Dash 3 đơn vị | Hở sau cú vọt; Mốc 2 |
| `technique.blade_control_a` | Gạt Đòn | Control · 55 | 7.0 | 0.05 | 0.60 (hụt) | 1.60 (thành công) | None | Cửa sổ gạt 0.18 s. Thành công: phản đòn + stagger 0.8 s lên quái thường/Elite; boss chỉ mở thêm punish window 0.5 s. Hụt: 0 damage, recovery đầy đủ. **Không bất tử** |
| `technique.blade_control_b` | Cắt Nhịp | Control · 55 | 8.0 | 0.20 | 0.55 | 1.50 | Quét vòng nhỏ | Ngắt telegraph của quái thường, không với boss |
| `technique.blade_signature_a` | Triều Dâng | Signature · 85 | 12.0 | 0.35 | 1.00 | 2.50 (0.40/0.45/0.50/0.55/0.60) | Commit, tiến ~4 bước | Telegraph mạnh, không hủy được |
| `technique.blade_signature_b` | Neo Chìm | Signature · 85 | 13.0 | 0.40 | 1.15 | 2.40 (0.90 nhảy + 1.50 nện) | Commit, nhảy tới | Vùng nện tròn r ≈ 1.6; hở lớn |

### 5.6 Năm Discipline còn lại (ý đồ, Mốc 2–3)

| Discipline | Nhịp | Traverse | Control | Signature |
|---|---|---|---|---|
| **Knives** (Dao Găm) | Áp sát, nhiều hit nhẹ | **Lách Bóng** — lách ra sau lưng mục tiêu | **Xé Dấu** — đánh dấu điểm yếu, hit kế mạnh hơn | **Mưa Dao** — 8 hit nhẹ, tầm rất ngắn |
| **Needle** (Kim Kiếm) | Kiểm soát tầm, đâm thẳng | **Đâm Vọt** — lao thẳng một đường | **Bắt Mũi** — phản đâm ngắt telegraph nếu đúng lúc | **Chùm Kim** — 7 mũi đâm liên tiếp |
| **Greatblade** (Trọng Kiếm) | Cam kết, wind-up dài | **Bước Nặng** — tiến chậm, giữ thế đỡ | **Chắn Trọng** — chắn một đòn trước mặt, vỡ chắn thì khựng | **Bổ Núi** — một hit lớn, recovery dài |
| **Maul** (Chùy) | Phá phòng thủ, stagger | **Lăn Chùy** — lăn đổi vị trí | **Đập Giáp** — giảm DEF hiệu dụng của mục tiêu trong thời gian ngắn | **Chấn Địa** — quét vòng, stagger nhóm nhỏ |
| **Cleaver** (Rìu Cong) | Động lượng, áp lực liên tục | **Xoáy Đà** — lướt xoay giữ đà | **Gặt Vòng** — quét rộng nửa vòng | **Cuồng Đà** — nhiều vòng xoay, khó dừng đúng chỗ |

Mỗi kỹ thuật phải trả lời ba câu của `07 §4.1` (di chuyển thế nào, hit nào, punish window gì) trước khi vào asset.

### 5.7 Thế Độc Môn — Song Thế (Mốc 3)

Mở bằng chuỗi quest Chương 2 + Blade mastery ≥ 55 + hai vũ khí Blade (ô tay phụ). Có bộ kỹ thuật riêng; đánh đổi: Defense hiệu dụng giảm ~15%. Là phần thưởng cốt truyện nhưng **không duy nhất** — bất kỳ account nào hoàn thành chuỗi quest đều có (cùng tinh thần "bonus không unique", ADR-016).

### 5.8 Thuật Phụ (Mốc 2 · ACR)

Ô Thuật phụ: 1 ở Lv 1; thêm 1 ở Lv 10, 25, 40 (tối đa 4). Mỗi Thuật phụ có mastery riêng 0–100.

| Thuật | Tác dụng | Giới hạn |
|---|---|---|
| **Tìm Kiếm** | Hiện cửa/rương ẩn và bẫy trong bán kính R (tăng theo mastery) | Tốn một ô; không lộ pattern boss |
| **Ẩn Thân** | Giảm tầm phát hiện của quái thường | Mất khi tấn công; vô hiệu với boss |
| **Hồi Khí** | Hồi HP chậm khi ngoài giao tranh ≥ 5 s | Không hoạt động trong phòng boss; không phải revive (`07 §2`) |
| **Câu Cá** | Mini-game ở cảng/sông, ra nguyên liệu | Không ảnh hưởng combat |
| **Nấu Ăn** | Món buff 10 phút (ATK hoặc DEF) | Trần +10%, không cộng dồn; kiểm TTK ≤ 35% (`07 §2`) |
| **Rèn** | Chế tạo và cường hóa (§5.9) | Cần Thạch Mộc hoặc lò rèn thị trấn |
| **Bào Chế** | Bình hồi máu mạnh hơn | Không revive |

### 5.9 Cường hóa trang bị (Mốc 2)

Dùng `upgradeLevel` 0–10 đã có trong `ItemInstance` (`03 §5`). Tỉ lệ ví dụ: 100/95/90/80/70/60/50/40/30/20 %. Thất bại: mất nguyên liệu, **không** mất vật phẩm, không hạ cấp (hợp mobile). Mỗi cấp +3% stat chính; chọn thuộc tính *Sắc* (ATK) hoặc *Vững* (DEF/MaxHealth). Ràng buộc: TTK quái cùng cấp không đổi quá 35% (`07 §2`). **Không làm độ bền vũ khí** — thêm gánh nặng mà ít giá trị trên điện thoại.

---

## 6. Quái vật (Bestiary)

### 6.1 Khung phân loại

Quái = **Role** (đã có: Chaser, Skirmisher, Guard, Ranged, Elite — `12 §7.1`) × **Họ** (cụm 10 tầng, §4.5) × **Biến thể** (thường / đêm / Elite / Field boss).

Mọi quái bắt buộc: telegraph ≥ 0.4 s · một hành vi *đọc được* · một điểm yếu hoặc trạng thái (ví dụ lưng cua) · không spawn trong Vùng Bảo Hộ (`09 §1.2`).

> Hiện **chưa có role trong code**: `EnemyDefinition` không có `CombatRole`, mọi quái dùng chung một hành vi đuổi → báo đòn → cận chiến (`05` P9.8, FB-030). Guard và Elite ở Tầng 2 hiện chỉ khác chỉ số. Cột Role ở §6.2 là thiết kế; Phase 9.8 thêm `CombatRole` với hành vi Skirmisher và Ranged, còn Guard và Elite tạm dùng hành vi Chaser cho tới khi có thiết kế riêng.

### 6.2 Quái Tầng 1–3 (theo `11 §3`; nguồn cho hai quái mới của Phase 9.8)

Số gốc ở Tầng 1 (power ×1.0); `FloorScaling` nhân lên (`07 §6`). Chạy lại `BalanceSandboxTests` cho từng quái mới (`07 §9`). "Có sẵn" nghĩa là đã có trong dự án theo `05`/`07`; tên và số của quái có sẵn lấy theo asset thật.

| Tầng | Quái | Role | Hành vi / điểm yếu | Số gốc HP · ATK · DEF |
|---|---|---|---|---|
| 1 | Slime Nước | Chaser | Có sẵn | 50 · 11 · 0 |
| 1 | Dơi | Skirmisher (thiết kế) | Có sẵn; hành vi Skirmisher có ở Phase 9.8 | 40 · 12 · 0 |
| 1 | **Cua Bắn Bọt** (đề xuất, ứng viên quái Ranged của Phase 9.8) | Ranged | `11 §3` yêu cầu Chaser + ranged để dạy đổi vị trí: nhả bọt chậm, dễ né | 35 · 10 · 0 |
| 1 (đêm) | **Thủy Thủ Xương** (đề xuất) | Chaser, nhóm 2–3 | Chỉ ra ban đêm (`allowedTimePhases`); thay một phần mật độ Slime | 45 · 12 · 0 |
| 2 | Lính Gác Lò Hơi | Guard | Có sẵn về chỉ số (`05` P9); chặn đòn trực diện và lưng yếu là thiết kế sau Phase 9.8 | theo asset |
| 2 | Elite Tầng 2 | Elite | Có sẵn (`05` P9); mục tiêu 12–20 đòn | theo asset |
| 3 | **Sâu Muối Kết Tinh** (đề xuất) | Skirmisher | `11 §3`: Skirmisher; lao vòng rồi rút; vỡ kết tinh khi chết, để lại vùng chậm nhỏ | 40 · 12 · 1 |
| 3 (đêm) | **Hoa Muối Đêm** (đề xuất) | Ranged tĩnh | Chỉ nở ban đêm (time-aware, `11 §3`); không chặn quest chính (`08 §4.4`) | 30 · 11 · 0 |

Hai quái mới của Phase 9.8: gợi ý Cua Bắn Bọt (Ranged, Tầng 1) và chuyển Dơi sang Skirmisher. Tên là tạm, product owner đổi sau.

### 6.3 Các hạng boss

| Hạng | Ví dụ | Pha | Mục tiêu thời gian | Ghi chú |
|---|---|---|---|---|
| **Elite** | Bù Nhìn | 1–2 | 12–20 s (`07 §5`) | Có ít nhất 1 punish window |
| **Field boss** (đêm) | Thuyền Phó Mục (Tầng 1) | 2 | 30–60 s | Xuất hiện theo giờ đêm (`allowedTimePhases`); rơi giáp của tầng |
| **Giữ Neo** | Cỗ Máy Kéo Neo (đã có) | 3: Observe → Punish → Pressure | 90–150 s lần đầu (`07 §5.2`) | Mỗi tầng một Giữ Neo; hạ nó mở tầng kế |
| **Trấn Thủ** | Tầng 5, 10 (MVP) → 25, 50, 75, 100 | 4 | 180–240 s (**số mới**, cần ADR + sandbox) | HP ≈ ×1.6 Giữ Neo cùng tầng; thêm pha **Cửa Khóa** (cửa phòng đóng đến hết pha 2); có cơ chế phòng (cát lún, sóng dâng…); không one-shot (`07 §5.2`) |

### 6.4 Biến thể đêm

Dùng `allowedTimePhases` + `phaseStatModifiers` (`03 §3`, `08 §4.2`). Giá trị cụ thể lấy theo `08` — ⚠ chưa đối chiếu.

---

## 7. Phân vùng Tầng, hầm boss và kế hoạch 10 tầng đầu

### 7.1 Ba vùng của một Tầng (Phase 9.7: 9.7a bố cục và cổng, 9.7b Hầm Boss)

Hiện quái, boss, NPC và khu an toàn nằm chung một khu vì không phase nào giao việc chia khu: Phase 7 chỉ yêu cầu Floor có Safe Zone, spawn, NPC và boss reference. Mỗi Floor cần ba vùng tách biệt, nối bằng cổng:

```text
 THỊ TRẤN (Vùng Bảo Hộ) ──Cổng Thị Trấn──► VÙNG FARM ──Cổng Hầm──► HẦM BOSS
 NPC · quest · Chọn Tầng                   quái · Elite · vật liệu   hành lang → tiền sảnh → phòng boss
```

| Vùng | Mục đích | Quái | Safe Zone (`09 §1.2`) | Nội dung |
|---|---|---|---|---|
| **Thị trấn** | Nghỉ, nhận/giao quest, Chọn Tầng, mua bán | Không | Có | Bốn vai NPC của `11 §4`; Đèn Hiệu 1 và Bia Neo (Mốc 2) |
| **Vùng Farm** | Kiếm EXP, vật liệu, thử kỹ thuật | Có, 2–3 khu | Không | Khu quái thường (Chaser/Ranged), khu Guard, hang Elite; Field boss ban đêm (Mốc 2) |
| **Hầm Boss** | Thử thách cuối tầng | Có (hành lang) | Không | Hành lang 1–2 đợt quái → Tiền Sảnh (vùng nghỉ, không spawn) → Phòng Boss |

Quy tắc:

1. Chỉ Thị trấn là Safe Zone. Tiền Sảnh chỉ là *vùng nghỉ* (không spawn quái), không Chọn Tầng, không mua bán.
2. Quái chỉ spawn trong vùng được gán; spawner bật theo vị trí người chơi để giữ hiệu năng (`02 §39`).
3. Boss chỉ kích hoạt khi người chơi vào Phòng Boss. Mặc định không khóa cửa: người chơi rời phòng thì trận kết thúc, boss về vị trí gốc, giữ máu và enrage (`05` P9.7b). Luật Phase 8 giữ nguyên.
4. Đi bộ từ Thị trấn tới Cổng Hầm khoảng 20–30 giây khi không giao tranh (số khởi điểm của `05` P9.7a; số đo thật thắng).
5. Cổng đi qua bằng cảm ứng; vào Hầm Boss chỉ hiện banner, **không popup xác nhận** (`10 §4`).
6. Ranh giới vùng đọc được bằng địa hình, ánh sáng, biển báo. Vàng beacon chỉ ở vật nhỏ (đèn, khóa, vật phẩm, NPC; `14` §2): dùng cho Đèn Hiệu và Vùng Bảo Hộ.
7. Floor N dùng cùng bộ thành phần vùng; thêm tầng là data/scene, không code theo số tầng (Phase 9).
8. Hầm Boss chỉ có khi `bossId` của Floor khác rỗng; Floor 2 hiện chỉ có Thị trấn và Vùng Farm (`05` P9.7).

**Cách làm (đã chốt trong `05` P9.7a):** một scene mỗi Floor; thành phần vùng (`ZoneId`, loại vùng, trigger) phát `ZoneEntered`; camera bounds theo vùng; mỗi khu Farm một spawner; nhạc và banner tên vùng. Field mới cho `FloorDefinition` (nếu cần) đi qua ACR (`13 §7`).

**Nghi thức Nhổ Neo** (đề xuất, chưa thuộc phase nào; nếu duyệt thì ghi FB mới) sau khi boss gục: Chuông Phao ngân, camera nâng 3–5 giây, bảng P8-5 xác nhận với server (chuỗi chữ giữ nguyên), rồi mở tuyến tầng kế.

### 7.2 Hầm Boss tối giản (Mốc 1) và mê cung đầy đủ (Mốc 2)

| Loại phòng | Chức năng | Mốc |
|---|---|---|
| **Hành lang** | 1–2 đợt quái; có thể có một Elite | 1 |
| **Tiền Sảnh** | Vùng nghỉ: không spawn; bia lore; Sổ Thăm Dò (chữ) | 1 |
| **Phòng Boss** | Giữ Neo hoặc Trấn Thủ | 1 |
| **Phòng Cửa / Đèn Hiệu** | Điểm kích hoạt dịch chuyển đầu hầm | 2 |
| **Phòng Cơ Cấu** | Cần gạt/bàn đạp mở đường; chỗ đặt bẫy | 2 |
| **Phòng Giữ** | Mini-boss giữ cần gạt hoặc chìa | 2 |
| **Phòng Ẩn** | Rương; cần Tìm Kiếm để thấy | 2 |

**Data shape đề xuất** (`DungeonDefinition`, `03 §3`) cho Mốc 2: `rooms[]` gồm `roomId`, `type`, `spawnSetId`, `links[]`, `lock` (Lever | Key | None), `isSafe`, `revealSkillLevel`. Hầm là data cộng một bộ module cơ chế tái dùng (`PvE/Mechanics/`, `06 §4`), không viết script riêng cho từng hầm. **Đường tắt:** sau Phòng Cơ Cấu mở cửa tắt về đầu hầm, giảm đi lại trong phiên ngắn trên điện thoại.

### 7.3 Bẫy công bằng (vì permadeath)

Mọi bẫy phải thỏa cả bốn điều:

1. Báo trước ≥ 0.6 s bằng hình và âm.
2. Sát thương ≤ 35% MaxHealth ở level khuyến nghị (không one-shot).
3. Có dấu hiệu môi trường nhìn thấy trước, hoặc lộ ra bằng Tìm Kiếm.
4. Không đặt ngay lối vào phòng.

Chết do bẫy ghi `DeathReason.Trap` (`09 §1.3`). Ngưỡng 35% là số khởi điểm.

### 7.4 Phòng Boss, rút lui và Sổ Thăm Dò

- **Rút lui:** Giữ Neo — rời phòng thì boss về vị trí gốc, giữ máu và enrage (`05` P9.7b). Trấn Thủ — cửa khóa đến hết pha 2 là đề xuất Mốc 2, cần quyết định riêng của product owner và ACR. Đèn Hiệu vô hiệu trong phòng boss.
- **Sổ Thăm Dò:** vào Tiền Sảnh, Mòng đọc những pha boss người chơi *đã biết*. Nguồn biết: (i) mua tin Người Vẽ Hải Đồ, (ii) tự thấy một pha lần đầu, (iii) Dư Âm (Mốc 2–3). Mốc 1 chỉ cần chữ; Mốc 2 lưu Player Data `bossNotes: { "boss.floor_01": ["phase.pressure"] }`.
- Tin của Người Vẽ Hải Đồ là **gợi ý đọc pha**, không lộ con số; trả bằng tiền hoặc vật phẩm.

### 7.5 Kế hoạch 10 tầng đầu (Chương 1)

Hạng item theo `07 §7`: Tầng 1–2 Common · 3–4 Uncommon · 5–6 Rare · 7–10 Epic. Level khuyến nghị `5N−4 … 5N`. Tầng 1–3 lấy tên và chất liệu từ `11 §3`; Tầng 4–10 là đề xuất cùng palette (`11 §5`).

| Tầng | Tên | Chất liệu · hazard | Hầm | Giữ Neo / Trấn Thủ ★ | Beat |
|---:|---|---|---|---|---|
| 1 | Bến Cạn Mù Sương | Cầu tàu mục, dây neo, sương mặn, beacon vàng | Hầm Neo Kẹt | **Cỗ Máy Kéo Neo** (`11 §3`, đã có) | Luật Tháp; Neo đầu tiên |
| 2 | Lò Rèn Ngập Nước | Lò hơi rỉ, băng tải, ống nước nóng | Hầm Điều Áp | **Bộ Điều Áp Cổ** (`11 §3`; chưa có trong game, Floor 2 hiện chưa có boss) | Người Vẽ Hải Đồ (đã có); Elite đầu tiên |
| 3 | Vườn Muối Âm | Thực vật kết tinh, hồ phản chiếu, gió chói | Hang Tinh Thể | **Sinh Vật Gom Tinh Thể** (`11 §3`) | Quái time-aware; Tín Hiệu Đêm xuất hiện |
| 4 | Mỏ Chuông Chìm | Đường ray xe goòng, hầm ngập | Lò Sâu | **Quản Đốc Mỏ** | Rèn và cường hóa |
| 5 ★ | Cồn Ảo Ảnh | Ảo ảnh (quái giả), gió cát | Cung Cát | **Trấn Thủ Cồn** | Kể lại việc Người Giữ Đèn mất đội; Hội Lặng xuất hiện nhỏ |
| 6 | Đầm Gương Mặn | Ma gương nhân bản | Gương Vỡ | **Kẻ Soi** | Nhiệm vụ đạo đức xám đầu tiên |
| 7 | Thành Cơ Khí | Hơi nước, ống xả | Xưởng Đồng Hồ | **Thợ Đồng Hồ** | Vào tier Epic; gợi ý Thế Độc Môn |
| 8 | Băng Nguyên Lặng | Vùng lạnh làm chậm; lửa trại là điểm nghỉ | Hang Băng | **Bà Chúa Tuyết** | Dư Âm đầu tiên có lời nhắn |
| 9 | Hỏa Sơn Ngầm | Dung nham | Lò Rèn Núi | **Lò Sống** | Người Vớt Phế Liệu rèn vũ khí cốt truyện |
| 10 ★ | Đèn Biển Cao | Bầu trời, gió; ngọn hải đăng khổng lồ | Đỉnh Đèn | **Trấn Thủ Đèn Biển** | Bia Neo: tên khắc từ trước ngày ký giao ước → mở Chương 2 |

> Floor 2 đã làm data-only ở Phase 9. Hazard ở cột "Chất liệu" (gió, bào tử, ray, ảo ảnh…) là cơ chế cần module `PvE/Mechanics`, nên để Mốc 2; Mốc 1 chỉ khác nhau ở quái, bản đồ, quest.

---

## 8. Cùng leo: từ solo đến đánh boss nhiều người

Hiện mỗi người chơi một Floor, một boss riêng và chỉ "gặp nhau" qua World State chung (A hạ boss thì B thấy tầng mở). Nhiều người cùng đánh boss là bước nhảy lớn về kỹ thuật, nên đi theo thang:

| Bậc | Tên | Mô tả | Mốc | Cỡ | Điều kiện |
|---|---|---|---|---|---|
| 0 | **Solo + thế giới chung** | Hiện tại | 1 ✓ | — | — |
| 1 | **Hiện diện bất đồng bộ** | Bảng Nhổ Neo (ai hạ boss đầu, số người đã vượt tầng), Bia Neo, Dư Âm: thấy dấu vết người khác mà không cần realtime | 2 | M | Cloud Code + ADR (FB-028) |
| 1.5 | **Bạn đồng hành AI + Hoán Thế** | Cảm giác "đánh cùng" khi chưa có mạng nhiều người | 2 | L | ACR |
| 2 | **Phòng Boss hợp tác** | 2–4 người vào chung một phòng boss riêng bằng mã phòng; tái dùng Lobby/Relay/NGO của Phase 10 | 2, sau Phase 10 | L–XL | ADR (`04 §44–45`), ACR (FB-027) |
| 3 | **Floor dùng chung** | Thấy nhau ở Thị trấn và Vùng Farm; party/raid lớn; Hội; Đòn Chốt | 3 | XL | Dedicated world backend (`05 §4`) |

### 8.1 Bạn đồng hành và Hoán Thế (Mốc 2)

- **Bạn đồng hành AI** (tối đa 1, ví dụ Vân Kha): `CompanionDefinition` { role, technique set, aiProfile }; chạy cục bộ, không cần mạng.
- **Hoán Thế:** khi boss tung đòn "Phá Thế" (phá thế đỡ), đồng hành đỡ thay, mở cửa sổ ~1.5 s: người chơi nhận ~+25% damage và hồi cooldown Core. Số khởi điểm, cần sandbox.

### 8.2 Phòng Boss hợp tác — cách làm khả thi nhất để có cảnh "nhiều người đánh boss"

- **Luồng:** một người tạo phòng ở Tiền Sảnh và nhận mã; người khác nhập mã; khi đủ người cùng vào Phòng Boss, host chạy boss. Mỗi người điều khiển nhân vật mình; sát thương và hồi máu tính riêng; máu và độ khó của boss scale theo số người (hệ số cấu hình).
- **Thưởng:** mỗi tài khoản nhận first-clear và drop riêng qua flow server hiện có (`ReportBossDefeated`), đúng một lần mỗi tài khoản. Chết trong phòng vẫn theo `09` (niêm phong). Luật rematch không EXP hiện tính theo World State (FB-019): khi boss đã Defeated thì tài khoản vào sau không nhận EXP boss lần đầu của chính họ, nên phải đổi sang tính theo `RewardClaims` của từng account.
- **Tái dùng:** Lobby + Relay + NGO host-client của Phase 10 (`04 §2`). Vì vậy chỉ nên bắt đầu khi Duel đã chạy ổn trên máy thật.
- **Rủi ro:** `BossController` và `EnemyController` hiện chạy offline-first; để đồng bộ phải chuyển sang host-authoritative (đây là phần tốn nhất). Host có lợi thế độ trễ (`04 §42`, chấp nhận được cho đồ án). Mất kết nối host kết thúc trận nên cần luồng "Thử lại" như Phase 8. Cần test 2–3 thiết bị.
- **Vì sao chưa phải Floor dùng chung:** thấy nhau giữa Thị trấn và Vùng Farm cần dedicated server (`04 §42`), để Mốc 3.
- **Đòn Chốt** (Mốc 3): hit cuối nhận thưởng *cosmetic* (không tăng sức mạnh) để tránh tranh giành; thưởng sức mạnh chia theo đóng góp.

### 8.3 Duel

**Đấu Cảng** (= Duel Half-Loss): đồng thuận, không permadeath, không World State (`09 §3`). Không có PK ngoài Duel (§2, hàng 15).

---

## 9. Kinh tế và nghề (gọn)

- **Tiền tệ:** *Đồng Neo*. Nguồn: quái, quest, bán đồ. Sink chỉ ba loại: thuốc, cường hóa, nguyên liệu nấu/bào chế.
- **Không** thu phí Đèn Hiệu, **không** độ bền vũ khí, **không** giao dịch người–người ở Mốc 1–2 (tránh gian lận backend; `10 §4`).
- Hàng rơi theo tier của tầng (`07 §7`); boss giữ drop chính cố định, rare giảm khi rematch (`07 §7`, `BossDefinition`).
- Nghề (Rèn, Nấu Ăn, Bào Chế, Câu Cá) là **Thuật Phụ** (§5.8), không phải hệ thống riêng.

---

## 10. Kịch bản Tầng 1 (lát cắt dọc cho Mốc 1)

Mục tiêu: mọi cảnh dưới đây chạy được bằng hệ thống **đã có** (Quest, Technique, Floor, Boss, Death) cộng phân vùng (Phase 9.7), chữ và dữ liệu — không cần contract mới ngoài ACR của vùng nếu có.

### 10.1 Nhịp cảnh

| # | Cảnh | Vùng | Nhân vật | Người chơi làm | Hệ thống dùng |
|---:|---|---|---|---|---|
| 1 | **Cổng Cảng** | Boot/Login | Chuông Phao | Chạm để vào; Chuông Phao nhắc Ba Điều | Auth, kiểm `death` (`09 §2`). Chỉ chữ |
| 2 | **Bến Cạn** | Thị trấn | Người Giữ Đèn | Học di chuyển, đòn thường. Quest **Bước Đầu**: Talk + Kill 3 Slime ở Vùng Farm gần cổng | Quest (Talk, Kill), Safe Zone |
| 3 | **Xưởng phế liệu** | Thị trấn | Người Vớt Phế Liệu | Quest **Lưỡi Cho Người Mới**: Collect nguyên liệu → nhận **Kiếm Triều** (`item.tide_blade`) | Quest (Collect), Inventory/Equipment |
| 4 | **Học Thế** | Thị trấn | Người Giữ Đèn | Dùng **Trảm Thuận**; đạt mastery 25 → chuông nhỏ, mở **Lướt Ngang** | Technique + mastery (Phase 7.5) |
| 5 | **Tin Đắt** | Thị trấn | Người Vẽ Hải Đồ | Quest **Tin Đắt**: Talk + Collect → nhận gợi ý Hầm Neo Kẹt và một pha boss | Quest; Sổ Thăm Dò (chữ) |
| 6 | **Vùng Farm** | Vùng Farm | — | Qua Cổng Thị Trấn; hạ Slime, Dơi, Cua Bắn Bọt; ban đêm có Thủy Thủ Xương | Spawn theo vùng (Phase 9.7), Time (`08`), role từ Phase 9.8 |
| 7 | **Cổng Hầm** | Ranh Farm/Hầm | — | Banner "Hầm Boss"; hành lang 1–2 đợt quái | Banner, không popup xác nhận (`10 §4`) |
| 8 | **Tiền Sảnh** | Hầm Boss | Mòng, Người Giữ Đèn | Chuẩn bị; Mòng đọc Sổ Thăm Dò | Vùng nghỉ; chữ |
| 9 | **Giữ Neo** | Phòng Boss | Cỗ Máy Kéo Neo | Hạ boss: Observe → Punish → Pressure; Enrage ở ngưỡng HP cuối | `BossController` (đã có) |
| 10 | **Nhổ Neo** | Phòng Boss → Thị trấn | Chuông Phao, Người Giữ Đèn | Chuông Phao ngân; bảng "Máy chủ đã ghi nhận. Tầng kế đã mở."; thưởng First-Clear | World State, `BossDefeatFlow`, UI P8-5 (đã có) |
| 11 | **Niêm Phong** *(nhánh)* | Nơi chết | Chuông Phao | Hiệu ứng tan thành ánh sáng; Death Screen; Guest: "Tạo tài khoản mới" | Death (`09 §1.3–1.4`) |
| 12 | **Người Đến Sau** | Màn chọn tầng (account B) | Chuông Phao | Thấy Tầng 1 Cleared, Tầng 2 "Sẵn sàng": "Neo thứ nhất đã nhổ bởi người đi trước" | Floor select; evidence (a) của P8 |

### 10.2 Lời thoại mẫu (nguyên bản)

| Ai | Câu |
|---|---|
| **Chuông Phao** (Cổng Cảng) | "Luminport chào bạn. Giao ước đã ký. Chuông Phao chỉ nhắc ba điều, một lần: Quyền truy cập đã niêm phong thì không mở lại. Vùng Bảo Hộ là nơi duy nhất không ai bị hại. Hạ Giữ Neo, tuyến kế tiếp mở cho tất cả." |
| **Người Giữ Đèn** (đón) | "Đừng nhìn lên. Nhìn xuống chân. Ở đây người ta ngã vì mải ngắm sương." |
| **Người Giữ Đèn** (dạy Thế) | "Kiếm nhớ thế. Cậu đứng đúng, nó tự lướt. Đứng sai, nó vẫn lướt — nhưng không về phía cậu muốn." |
| **Người Vớt Phế Liệu** | "Thép biết nhớ chủ. Đừng coi nó là đồ mượn." |
| **Người Vẽ Hải Đồ** | "Tin không miễn phí. Trả tôi một chuyện chưa ai biết, tôi trả cậu hai chuyện." |
| **Mòng** | "Hầm này có mùi dầu! Mùi dầu nghĩa là… à, tôi chưa chắc nghĩa là gì." |
| **Chuông Phao** (Nhổ Neo) | "Neo thứ nhất đã nhổ. Tuyến kế tiếp mở. Ghi nhận: {tên người chơi}." |
| **Chuông Phao** (người xem sau) | "Neo thứ nhất đã nhổ bởi người đi trước." |
| **Chuông Phao** (Niêm Phong) | "Quyền truy cập đã niêm phong. Cảm ơn bạn đã leo." |
| **Người Giữ Đèn** (sau boss) | "Tầng hai mở rồi. Có người đang chờ trên đó — và có người đã không về được. Nhớ cả hai." |

### 10.3 Giọng Chuông Phao (quy tắc cho mọi chữ hệ thống)

- Ngắn, thì hiện tại, không cảm thán, hay đếm số; xưng "bạn".
- Không đùa, không an ủi. Chỉ gọi tên người chơi khi **ghi nhận** (Nhổ Neo).
- Một hồi chuông = một Neo được nhổ; hai hồi = một người khác bị niêm phong (Mốc 2); chuông nhỏ = mở Thế mới. Không dùng chuông cho việc nhỏ hơn.
- Mẫu: "Bạn đã lên cấp {n}." · "Thế mới: {tên}." · "Vùng Bảo Hộ. Không ai bị hại ở đây." · "Bạn rời Vùng Bảo Hộ."
- **Chuỗi kỹ thuật của bảng báo boss (P8-5) giữ nguyên** — có test và là evidence. Giọng Chuông Phao là toast riêng, hiện *sau* khi bảng xác nhận (Phase 9.9: "giọng hệ thống").

---

## 11. Trình bày (2D, Android) và Art Track

Đồ họa do hai file của AI khác quyết định; Game Bible chỉ thêm ý đồ trải nghiệm, không viết hợp đồng đồ họa riêng để khỏi có hai nguồn sự thật.

- **Hợp đồng hình vẽ:** `14_ART_CONTRACT.md` (PPU 32, khung, điểm neo, bảng màu, màu báo đòn là chức năng, 8 hướng, vũ khí tách lớp, Sorting Layer). **Bàn giao:** `15_ART_HANDOFF.md`. Art Track đã chốt ở `05` §3 (ADR-026, ADR-027).
- **Âm thanh, giao diện, hiệu ứng, thử trên máy thật:** phụ lục `14A_ART_CONTRACT_PHU_LUC.md` (đề xuất; nếu duyệt thì dán vào cuối `14` hoặc thêm một dòng vào `15` §0).
- **Ý đồ trải nghiệm** (không phải contract):
  - Đọc được trước tiên (`12 §2`): telegraph = màu + hình vùng + chuyển động; aura kỹ thuật không dùng màu dành riêng cho telegraph (`14` §2).
  - Vàng beacon chỉ ở vật nhỏ (`14` §2): dùng cho Đèn Hiệu và Vùng Bảo Hộ.
  - Chuông Phao là motif âm thanh (§10.3): một hồi = Neo nhổ; chuông nhỏ = mở Thế mới.
  - Cảnh bị niêm phong "tan thành ánh sáng" là hiệu ứng làm bằng code trong `deathSeconds` của Phase 9.5; art chỉ cung cấp hạt sáng nhỏ nếu cần.

---

## 12. Lộ trình và ưu tiên

### 12.1 Thứ tự hiện hành (theo `05` bản 2026-10-17)

| Phase | Nội dung | Nguồn |
|---|---|---|
| **9.5** | Hướng nhìn 8 hướng, phản hồi trúng đòn, âm thanh (9.5a → 9.5b → 9.5c) | FB-022, 023, 032, 033 |
| **9.6** | Nền UX: Pause + Back, Settings tối thiểu, joystick floating + dead zone, nút bình, rung, loading | FB-001, 002, 005, 006, 009, 010, 011 |
| **9.7** | Phân vùng Tầng: 9.7a bố cục và cổng, 9.7b Hầm Boss và phòng boss | FB-021 |
| **9.8** | Quái đa vai trò (`CombatRole`, Skirmisher, Ranged) | FB-030 |
| **9.9** | Hướng dẫn nhập môn và mũi tên quest | FB-029, 024 |
| **10** | Duel; lớp mạng không hard-code 2 người | `04` |
| **11** | Regression; Save conflict UI | FB-003 |
| **Art Track** | Song song, đã chốt 2026-10-16 (ADR-026, 027) | FB-026 |

Thứ tự 9.5 → 9.9 do product owner chọn (`05 §1` luật 10); Game Bible không đổi thứ tự đó. Khi thiếu thời gian, cắt từ dưới lên theo `05` (mũi tên quest ở 9.9 trước).

### 12.2 Phần "chất Tháp" nằm ở phase nào

| Ý trong Game Bible | Phase | Ghi chú |
|---|---|---|
| Kiếm kỹ có phản hồi (§5.1: sáng, âm, hướng, dừng hình) | 9.5 | 8 hướng, dừng hình ≤ 0.06 s, âm thanh |
| Thế giới có khu vực (§7.1) | 9.7 | Thị trấn / Vùng Farm / Hầm Boss; Hầm Boss chỉ khi Floor có boss |
| Quái cư xử khác nhau (§6) | 9.8 | Hai quái mới: gợi ý Cua Bắn Bọt (Ranged) và Dơi sang Skirmisher |
| Giọng hệ thống (§10.3) và hướng dẫn | 9.9 | Toast riêng, không đổi chuỗi P8-5 |
| Nghi thức Nhổ Neo (§7.1) | Chưa có phase | Đề xuất; nếu duyệt thì ghi FB mới |
| Mê cung nhiều phòng, Nối Thế, Loadout, Thuật Phụ, bạn đồng hành | Mốc 2 | Bảng §12.3 |

### 12.3 Backlog Mốc 2–3

Cỡ: S nhỏ · M vừa · L lớn · XL rất lớn.

| # | Hạng mục | Cỡ | Phụ thuộc | Mốc | Giá trị "chất Tháp" |
|---:|---|---|---|---|---|
| 1 | Nối Thế + Loadout (§5.2–5.3) | M | ACR | 2 | Rất cao |
| 2 | Control/Signature Blade, Gạt Đòn (§5.5) | M | #1 | 2 | Cao |
| 3 | Mê cung đầy đủ + Đèn Hiệu (§7.2) | L | ACR Floor/Dungeon | 2 | Cao |
| 4 | Bestiary Tầng 3–10 + Trấn Thủ Tầng 5, 10 (§6, §7.5) | L | #3 | 2 | Cao |
| 5 | Bạn đồng hành + Hoán Thế (§8.1) | L | #2, #4 | 2 | Cao |
| 6 | Hiện diện bất đồng bộ: Bảng Nhổ Neo (FB-028), Bia Neo, Dư Âm (§4.6, §8) | M | ADR, Cloud Code | 2 | Cao (cảm xúc) |
| 7 | **Phòng Boss hợp tác** (§8.2, FB-027) | L–XL | Phase 10 Pass, ADR `04 §44–45` | 2 | Rất cao, nhưng rủi ro lớn |
| 8 | Thuật Phụ + Cường hóa (§5.8–5.9) | L | ACR | 2 | Trung bình |
| 9 | Năm Discipline còn lại (§5.6) | L mỗi cái | #1, #2 | 2–3 | Trung bình |
| 10 | Floor dùng chung, party/raid, Hội (§8) | XL | Backend Mốc 3 | 3 | Rất cao, rất đắt |
| 11 | Chương 2–5, Tầng 11–100 (§4.5) | XL | tất cả | 3 | Nội dung |
| 12 | Nhãn độ hiếm item và so sánh trang bị (FB-025) | S | — | 2 | Trung bình |
| 13 | Quái theo pha giờ: Thủy Thủ Xương, Hoa Muối Đêm (FB-031) | M | Phase 9.8 | 2 | Trung bình |

### 12.4 Gói việc (khớp `13`)

`13` hiện có Wave 9.5a, 9.5b, 9.5c. Theo `05 §1` luật 9, mỗi phase phải có work package (path sở hữu, path chỉ đọc, test bắt buộc) trước khi giao việc. Còn thiếu các gói dưới đây; paths chỉ là gợi ý, Integrator chốt theo `06`.

| Gói | Phase | Mục tiêu | Paths gợi ý | Contract mới |
|---|---|---|---|---|
| 9.6 | Nền UX | Pause/Back, Settings, joystick floating + dead zone, nút bình, rung, loading | `UI/`, `PvE/Input`, dùng `PlaySpeedState` của 9.5 | Không (Settings lưu local) |
| 9.7a | Bố cục vùng và cổng | `ZoneId`, `ZoneEntered`, camera bounds theo vùng, spawner theo vùng, banner | `PvE/Floor`, `Content/Floors/Floor0N`, `CameraFollow` | ACR nếu `FloorDefinition` cần field |
| 9.7b | Hầm Boss và phòng boss | Boss kích hoạt trong phòng, rời phòng thì về gốc | `PvE/Boss` (sửa tối thiểu) | Không đổi luật Phase 8 |
| 9.8 | Quái đa vai trò | `CombatRole`, Skirmisher, Ranged, đạn pool | `PvE/Enemy`, `Core/Data` | ACR (`EnemyDefinition`) |
| 9.9 | Hướng dẫn + mũi tên quest | Toast gợi ý, cờ local, mũi tên | `UI/`, đọc `PvE/Quest` | Không |

Mốc 2: tên gói do Integrator đặt khi mở Mốc 2; hạng mục ở §12.3.

Giữ nguyên luật `13`: mỗi gói một owner, không chia một class/prefab/scene giữa hai AI, asmdef chỉ Integrator sửa.

---

## 13. Bản đồ sửa từng file docs

| File | Sửa / thêm |
|---|---|
| `05` | **Đã tích hợp** trong bản của bạn (2026-10-17): Phase 9.5–9.9, Art Track, luật 7–10. Bản `05` mình soạn trước đó bỏ |
| `10` | **Đã tích hợp** (FB-021 đến FB-033). Còn "Chờ quyết": FB-025, 027, 028, 031 |
| `01` | Thêm tầm nhìn (§1) và năm trụ cột. Ghi ADR: (a) "cảm hứng cơ chế/thể loại, biểu đạt nguyên bản"; (b) `world_echoes` và Bảng Nhổ Neo; (c) hạng Trấn Thủ và mục tiêu thời gian; (d) bỏ độ bền vũ khí; (e) Phòng Boss hợp tác (FB-027) nếu duyệt. ADR-026 và ADR-027 đã ghi cho 8 hướng và Art Track |
| `02` | Thêm mục Phân vùng Tầng (§7.1 của Game Bible); mục 17 của `02` (Dungeon): Hầm tối giản và Template; mục 13 của `02` (Boss): hạng Trấn Thủ; thêm Bạn đồng hành và Hoán Thế |
| `03` | `FloorDefinition`: `zones` (nếu cần), `dungeonId`. Player Data: `loadout`, `bossNotes`, `progress.activatedWaypoints`, ô Thuật phụ. Game Data: `world_echoes` |
| `04` | Giữ Duel. Thêm ghi chú: interface mạng không hard-code 2 người; mở lối cho Phòng Boss hợp tác qua ADR (`04 §44–45`) |
| `06` | Thư mục mới `Core/Skills/Chain`, `Core/Dungeon`, `PvE/Dungeon`, `PvE/Companion`, `UI/Narrative`; nhắc lại quy tắc Mechanics |
| `07` | Catalog Blade (§5.5), bảng quái (§6.2), hạng Trấn Thủ, luật bẫy (§7.3), phạt chuỗi, Thuật Phụ, cường hóa |
| `08` | Không đổi. Ghi thêm ví dụ: Thủy Thủ Xương, Hoa Muối Đêm, Field boss đêm đều dùng `allowedTimePhases` |
| `09` | Bia Neo/Dư Âm (trang trí), hiệu ứng tan thành ánh sáng, chữ Chuông Phao trên Death Screen (giữ nguyên nút) |
| `11` | §1: thêm Neo và Chuông Phao; §2: thêm Nhổ Neo, Đèn Hiệu; §3: thêm Tầng 4–10 (§7.5); §4: thêm Mòng, Vân Kha, Người Khắc Bia, Sư Lặng; **§7: sửa dòng cấm IP** thành "không sao chép tên, nhân vật, cảnh, story beat, UI, nhạc, asset của IP ngoài; cho phép cơ chế/thể loại" |
| `12` | **Viết lại §3** cùng ý như `11 §7`; thêm Nối Thế và Loadout vào §6; thêm Trấn Thủ vào §7; thêm domain event mới (nếu Nối Thế phát event) vào §11 |
| `13` | Thêm work package 9.6, 9.7a, 9.7b, 9.8, 9.9 (§12.4); Integrator ghi |
| `14` ART_CONTRACT, `15` ART_HANDOFF | Do AI khác soạn; là chuẩn đồ họa. Game Bible dẫn tới, không lặp lại. Vì thế Game Bible chuyển sang số 16 |
| `14A` (mới) | Phụ lục đề xuất của mình: âm thanh, giao diện và font, hiệu ứng, cách thử trên máy thật |

---

## 14. Checklist nguyên bản (chạy trước khi sửa docs và trước mỗi gói nội dung)

1. Có tên riêng nào (người, nơi, kỹ năng, boss, vật phẩm) trùng hoặc na ná tác phẩm gốc? → đổi.
2. Có cảnh hay chuỗi biến cố tái hiện gần nguyên văn một đoạn truyện gốc? → viết lại.
3. Danh sách kỹ năng, thứ tự boss, thứ tự mở khóa có phải bản sao? → tự thiết kế theo trụ cột §1.
4. UI, âm thanh, icon, sprite có mượn từ nguồn? → không dùng.
5. Người chưa xem tác phẩm gốc có thấy đây là một thế giới riêng không? → phải có.
6. Mô tả game trong ba câu mà không nhắc tên tác phẩm gốc được không? (Tháp Thủy Triều · Chuông Phao · Niêm Phong · Người Đến Sau) → nếu không, nội dung còn quá gần.

Giống ở **cơ chế** thì ổn; giống ở **tên, cảnh, câu chữ, hình ảnh** thì phải sửa.

---

## 15. Bảng thuật ngữ và ID

| Thuật ngữ hiển thị | Nghĩa | Trong code / docs |
|---|---|---|
| **Tháp Thủy Triều** | Công trình thăm dò của Luminport | `11 §1`; project `GameAE` / `Game2D` |
| **Neo** | Thiết bị neo mô phỏng của một Floor | `boss.floor_NN` = `DEFEATED` |
| **Giữ Neo** | Boss thường mỗi tầng | `BossDefinition` (3 pha) |
| **Trấn Thủ** | Boss cột mốc | `BossDefinition` (4 pha) ⚠ mới |
| **Nhổ Neo** | Hạ Giữ Neo/Trấn Thủ, mở tuyến kế | `ReportBossDefeated` → World State |
| **Chuông Phao** | Giọng hệ thống của Tháp | Chữ thông báo/UI |
| **Niêm Phong** | Permadeath (mất quyền truy cập) | `death.isDead` |
| **Người Đến Sau** | Account mới / Guest reset | `09 §2` |
| **Vùng Bảo Hộ** | Safe Zone (chỉ ở Thị trấn) | `SafeZoneService` |
| **Thị trấn / Vùng Farm / Hầm Boss** | Ba vùng của một Tầng | `zones` ⚠ mới (nếu cần) |
| **Đèn Hiệu** | Waypoint trong tầng | `waypointId` ⚠ mới |
| **Thế** | Weapon Technique | `WeaponTechniqueDefinition` |
| **Khởi thế / Lướt dẫn / Khựng** | WindUp / Active / Recovery | `07 §4` |
| **Nối Thế** | Chain | `cancelPolicy = ChainOnMatch` ⚠ mới |
| **Thuần Thục** | Weapon Mastery | key `weaponProficiency` |
| **Thuật Phụ** | Extra skills | ⚠ mới |
| **Bia Neo / Dư Âm** | Memorial / Echo | `world_echoes` ⚠ mới |
| **Sổ Thăm Dò** | Ghi chú pha boss đã biết | `bossNotes` ⚠ mới |
| **Phòng Boss hợp tác** | Co-op boss 2–4 người, mã phòng | FB-027 ⚠ chờ quyết |
| **Đấu Cảng** | Duel Half-Loss | `04`, `09 §3` |

*Hết tài liệu.*
