# 18 — Map Tầng 2 "Lò Rèn Ngập Nước" (bản mở rộng)

Trạng thái: **đã dựng** bằng `Assets/_Game/Editor/Floor02WorldBuilder.cs` (menu `Game/Floor 02/Build Big World`, chạy lại được, idempotent).
Bản cũ: hành lang 72×19 (~1.220 ô sàn). Bản mới: **~19.100 ô sàn** (≈15,6 lần), ~15.460 ô đi được (có khoảng trống cho nhân vật).

## 1. Vị trí trong thế giới
Toạ độ local lấy Trạm Van làm (0,0); thế giới = local + (1000, 0). Tầng 2 nằm hẳn ở x≈875–1210 để KHÔNG đụng Tầng 1
(đang mở rộng ở x −112…100, hầm x≥200, nhà x≥400 theo doc 17). Quy ước đề xuất: tầng N đặt gốc ở x = 1000×(N−1).

## 2. Các khu
| Khu | Zone | Ghi chú |
|---|---|---|
| Trạm Van | Town (duy nhất) + Safe Zone 44×28 | Điểm vào, 2 NPC (Người Vẽ Hải Đồ, thương nhân) |
| Bãi Phế Liệu (Lv 6–7) | Farm | Khu mở, dễ nhất, 15 đống phế liệu làm vật cản |
| Hành Lang Băng Tải (Lv 7) | Farm | 3 băng chuyền, trụ giữa lối |
| Lò Hơi Trung Tâm (Lv 7–8) | Farm | Lưới 12 khối nồi hơi, nhiều ống hơi |
| Mê Cung Ống (Lv 8–9) | Farm | Dạng rắn 7 làn, có 2 lối tắt, sàn lưới nhiều, Wire Stalker |
| Xưởng Rèn (Lv 9–10) | Farm | 2 Quản Áp Cổ (Elite), 2 băng chuyền, hố rèn |
| Ngã Tư Áp Lực | Refuge + Safe Zone | Trạm nghỉ giữa bản đồ, đường rẽ tới Xưởng Rèn / hầm boss |
| Hành Lang Hầm Điều Áp (Lv 9–10) | Farm | Dài 61 ô, 2 phòng phụ, đèn rèn dọc lối |
| Hầm Điều Áp | BossApproach + BossRoom | Boss giữ nguyên cụm cũ, chỉ dời nguyên khối tới local (188; 0,5) |
| Trạm Nghỉ ×4 | Refuge + Safe Zone | Đường Ống, Xưởng Rèn, Phế Liệu, Lò Hơi |

Có nhiều tuyến tới boss: Trạm Van → Hành Lang Băng Tải → Ngã Tư; hoặc vòng Mê Cung Ống → Băng Tải; hoặc Lò Hơi → Xưởng Rèn → Ngã Tư.
Khoảng cách ngắn nhất Trạm Van → Boss: ~193 bước. Cuối Mê Cung Ống: ~295 bước. Mọi trạm nghỉ cách điểm vào ≤ 145 bước.

## 3. Nội dung
- 31 spawner (≈111 quái đồng thời), mỗi khu một nhóm; đủ 5 loại quái của FloorDefinition_Floor02.
- 7 băng chuyền, 36 ống hơi, ~234 props + ~118 ambient (hơi, tia lửa, tàn lửa).
- Tâm spawner được "snap" tự động vào vị trí có đĩa tròn nằm trọn trên sàn và không giao Safe Zone.
- Tường 2 lớp: `Walls` (có collider, chỉ vành 2 ô quanh sàn + vật cản) và `WallsFill` (chỉ hiển thị, lấp 12 ô quanh sàn để camera không thấy khoảng trống).

## 4. Hệ thống mới / thay đổi
- `FloorZoneStreamer` (Assets/_Game/Scripts/PvE/Zones): bật/tắt nhóm `Streamed/Zone_*` (spawner, quái, props, hazard) theo khoảng cách camera (bật ≤45, tắt >58 ô). Chỉ là tối ưu, không đổi luật chơi. Zone, Safe Zone, NPC, boss KHÔNG nằm trong nhóm stream.
- `ZoneKind.Refuge = 4` (thêm cuối enum): vùng an toàn nhỏ ngoài Thị trấn; validator không đòi spawner và không đếm là Town.
- `Floor02WiringTests`: bỏ ràng buộc "đúng 1 spawner" (theo mẫu Tầng 1); test mới: hợp quái của mọi spawner == spawnEnemyIds, registrar đăng ký đủ mọi spawner, map ≥ 12.000 ô sàn, mọi spawner nằm trong nhóm stream.
- Template (băng chuyền, ống hơi, props, spawner, zone) nằm ở `Floor02/_Templates` (NGOÀI Content, inactive) để builder nhân bản; không xoá.

## 5. Chưa làm (đề xuất bước sau)
1. (Đã xong, xem mục 7.)
2. Rương kho báu ở 2 phòng phụ và các ngõ cụt (chưa có prefab `TreasureChest`).
3. NPC/quest thêm ở trạm nghỉ (cần NPCDefinition, và test đang bắt đúng 2 NPC cho Tầng 2).
4. Art riêng cho từng khu (hiện dùng chung bộ tile Floor02; Mê Cung Ống chỉ khác ở tỉ lệ sàn lưới).
5. Đo hiệu năng trên thiết bị: lúc vào tầng registrar gọi SpawnAll cho cả 31 spawner (prewarm ~200 quái). Nếu giật, chuyển sang spawn lười theo streamer.

## 6. Sổ Hải Đồ + Đèn Hiệu (kiểu SAO) — đã làm
- **Bản đồ tự vẽ**: đi tới đâu hiện tới đó (sương chiến tranh, khối 8×8 ô, bán kính vẽ 12 ô). Lưu Player Data key `map` (một key cho mọi tầng, ~200 byte/tầng) — `MapRevealGrid` + `ExplorationProgress` (Core/World, thuần, có test).
- **Minimap** góc trên-trái (dưới thanh HP) + **bản đồ toàn màn hình** (nút BẢN ĐỒ hoặc phím M): người chơi, Đèn Hiệu vàng (đã thắp) / xám (chưa), % khám phá.
- **Đèn Hiệu** (`BeaconWaypoint`): 6 cột đèn Safe Zone của Tầng 2. Tới gần lần đầu thì thắp, lưu key `beacons`. Trạm Van tự thắp khi vào tầng.
- **Dịch chuyển nhanh** (`FastTravelController`, luật ở `FastTravelRules`): chạm Đèn Hiệu vàng trên bản đồ, đứng yên 3 giây. Từ chối khi đã chết, trong 5 giây sau khi trúng đòn, Đèn chưa thắp, khác tầng, hoặc đang đứng sát đèn đích. Trúng đòn hoặc tự di chuyển > 0,75 ô thì huỷ.
- `SaveKeys.All` đã thêm `map`, `beacons` (reset nhân vật xoá cả hai).
- Code gameplay không chứa ID tầng: `floorId`/`beaconId` nằm trong dữ liệu scene (do `Floor02MapSystemInstaller` gán; builder tự gọi cuối mỗi lần dựng).
- UI tự dựng bằng code (Canvas riêng, sortingOrder 200), chỉ tồn tại khi Content tầng đang bật. Nếu muốn đổi vị trí minimap: sửa `minimapOffset` trên `MapSystem/MapHudController`.
- Đã kiểm thử Play Mode: vẽ bản đồ khi đi, thắp Đèn Ngã Tư, dịch chuyển Ngã Tư → Trạm Van, UI minimap/toàn màn hình.

## 7. Tầng 1 cũng đã có Sổ Hải Đồ
- Menu `Game/Floor 01/Install Map System` (`Floor01MapSystemInstaller`, cần mở scene `PvE_Gameplay`). Chạy lại được và **tự đo lại giới hạn bản đồ** từ ảnh `World_Ground` + tilemap khối chặn, nên sau khi mở rộng map Tầng 1 (docs/17) chỉ cần bấm lại.
- Tầng 1 vẽ đất bằng sprite, tilemap chỉ có `Tile_Blocker` → `FloorMapInfo.groundIsOpen = true`: ô không có khối chặn là đi được.
- Mọi Safe Zone trong `Floor01/Content` tự thành Đèn Hiệu; tên lấy từ Zone chứa nó (hiện có 1: "Thị trấn"). Thêm Safe Zone mới rồi bấm lại menu là có Đèn Hiệu.
- Id tầng đọc từ `FloorSceneBinder` (không hardcode). Ghi chú: Tầng 1 chỉ có 1 Đèn Hiệu nên dịch chuyển nhanh hiện chỉ là "về Thị trấn".
