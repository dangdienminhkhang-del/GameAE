# WORLD STORY
## Game2D Content Tone and Naming Guide

> Content brief nguyên bản cho world, Floor, NPC, art direction và naming. Không dùng reference ngoài dự án làm content spec.

---

## 1. Tiền đề

Thành phố cảng **Luminport** vận hành một công trình thăm dò bí mật tên **Tháp Thủy Triều**. Mỗi Floor là một mô phỏng địa hình được nuôi bằng ký ức vật chất của biển, xưởng máy và các đoàn tàu đã mất tích. Người tham gia ký một giao ước thám hiểm: nếu thất bại ngoài vùng bảo hộ, quyền truy cập của họ bị niêm phong vĩnh viễn.

Không ai biết người đã xây Tháp nhằm khai thác năng lượng, lưu giữ ký ức hay tìm một lối đi dưới biển sâu. Mốc 1 chỉ kể qua môi trường, vật phẩm và vài NPC; không cần phản diện lớn hay giải thích toàn bộ bí ẩn.

## 2. Luật trong thế giới

| Luật | Diễn giải narrative | Code contract |
|---|---|---|
| Access seal | Thất bại PvE khóa quyền truy cập của identity hiện tại. | `IDeathService`, `death.isDead`. |
| Beacon refuge | Đèn hải lưu đánh dấu vùng bảo hộ có dịch vụ. | `SafeZoneService`. |
| Shared route | Một boss bị đánh bại làm bản đồ hàng hải chung mở tuyến tiếp theo. | `IWorldStateService`. |
| Tide clock | Tháp có chu kỳ ánh sáng riêng tác động môi trường/content. | `ITimeService`. |
| Trial arena | Duel là mô phỏng an toàn, kết thúc ở ngưỡng HP. | Half-Loss PvP RuleSet. |

Không mô tả permadeath như tổn hại cơ thể thật. Rủi ro là mất quyền truy cập của character/account, đúng với flow game mobile và account system.

## 3. Floor content direction

| Floor | Tên tạm | Chất liệu | Hành vi chính | Boss concept |
|---:|---|---|---|---|
| 1 | Bến Cạn Mù Sương | Cầu tàu mục, dây neo, sương mặn, beacon vàng | Chaser và ranged để dạy reposition | Cỗ máy kéo neo bị kẹt, có telegraph quét và punish window. |
| 2 | Lò Rèn Ngập Nước | Lò hơi rỉ, băng tải, ống nước nóng | Guard và elite để dạy Technique positioning | Bộ điều áp cổ, thay đổi vùng nguy hiểm theo phase. |
| 3 | Vườn Muối Âm | Thực vật kết tinh, hồ phản chiếu, gió chói | Skirmisher và time-aware content | Sinh vật gom tinh thể, có window khi đổi lõi. |

Floor N chỉ là data/content. Chất liệu mới không được kéo theo class/logic riêng nếu có thể biểu diễn bằng existing definition.

## 4. NPC direction

| NPC role | Giọng nói | Gameplay role |
|---|---|---|
| Beacon Keeper | Ngắn, thực tế, không bí ẩn quá mức | Safe Zone, tutorial, quest turn-in. |
| Cartographer | Ghi chú hành trình và route | Floor selector, world-state explanation. |
| Salvager | Thực dụng, trao đổi vật liệu | Item/collect quest. |
| Night Signal | Chỉ xuất hiện trong một phase | Time-aware optional quest, không chặn main quest. |

NPC phải có `NPCDefinition` và dialogue/quest IDs riêng. Không dùng thoại dài để truyền luật mà UI system đã phải giải thích.

## 5. Art and UI tone

- Palette: xanh than, xám muối, đỏ gỉ và vàng beacon.
- Shape language: công nghiệp bo tròn, dây thừng, ký hiệu hải đồ, panel mờ.
- UI: readable trên màn landscape nhỏ; icon đơn giản, trạng thái combat/time/death dễ phân biệt.
- Audio: nước, kim loại, dây căng, gió; mọi asset phải có nguồn sử dụng hợp lệ.
- Animation: telegraph ưu tiên silhouette, ánh sáng và motion; không tái tạo shot/moveset từ media ngoài dự án.

## 6. Naming rules

1. Tên rõ nghĩa, 2–4 từ; không lấy dịch gần nghĩa của tên external đã biết.
2. ID dùng quy ước ổn định ở `03 §2`: Floor `floor.NN` (ví dụ `floor.01`), boss progression `boss.floor_NN` (ví dụ `boss.floor_01`), các ID content khác dùng `type.semantic_snake_case` (ví dụ `npc.beacon_keeper`). Tên hiển thị như “Bến Cạn Mù Sương” không phải ID.
3. Technique đặt theo hành động/cảm giác trong game, không theo danh sách move tham chiếu.
4. Trước khi thêm tên, kiểm tra trong project để tránh trùng ID hoặc content đã tồn tại.

## 7. Content checklist

- [ ] Có gameplay role, Floor/quest/boss definition ID và reward purpose.
- [ ] Có telegraph/readability nếu là enemy/boss/Technique.
- [ ] Không yêu cầu một mechanic mới chỉ để kể chuyện.
- [ ] Art/audio/UI có nguồn hợp lệ và style riêng.
- [ ] Không dựa vào nhân vật, thế giới, hình ảnh, âm thanh, tên hoặc story beat của một IP ngoài dự án.
