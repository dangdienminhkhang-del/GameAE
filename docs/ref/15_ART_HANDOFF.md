# ART HANDOFF
## Game2D — Bàn giao cho AI làm đồ họa

> Cập nhật 2026-10-16. Đọc file này TRƯỚC khi làm bất cứ việc gì về hình ảnh. Nó không thay `AGENTS.md`, `14_ART_CONTRACT.md` hay `13`; nó chỉ chỉ đường.

---

## 0. Cách dùng

- Đọc theo thứ tự: file này → `docs/14_ART_CONTRACT.md` (luật hình vẽ) → `docs/05` mục "Art Track" và Phase 9.5 → `docs/13` gói 9.5a, 9.5b, 9.5c.
- **Không đọc** cả thư mục `docs/` hay cả dự án (rất tốn hạn mức). Không đọc `Assets/_Game/Scenes/PvE_Gameplay.unity` (186 KB), `Library/`, `Temp/`, `Logs/`. Cần biết thứ trong scene thì hỏi người dùng.
- Người dùng không rành lập trình và dùng nhiều AI. Viết tiếng Việt đơn giản, giải thích từ khó, chỉ từng bước "bấm vào đâu". Báo cáo tối đa 6 dòng.
- Nút thắt của người dùng là hạn mức token. Làm tuần tự, mỗi lần MỘT lô hình nhỏ. Không tự tạo ảnh hay tệp mà người dùng không nhờ.

## 1. Mục tiêu

Game đẹp và "xịn", có cảm giác leo tháp kiểu game nhập vai anime trên điện thoại, góc nhìn như Liên Quân (nhân vật xoay đủ 8 hướng). Người dùng coi đồ họa là "linh hồn" của game; chưa cần vẽ vội, nhưng phần nền phải sẵn sàng.

**Bản quyền (bắt buộc):** lấy được *cảm giác*, không được lấy *thiết kế*. Không bắt chước nhân vật, vũ khí, giao diện của SAO, Liên Quân hay bất kỳ tác phẩm nào (`AGENTS.md` luật 7, `11` §7). Nhân vật, thế giới Luminport, boss đều nguyên bản.

## 2. Trạng thái (2026-10-16)

| Hạng mục | Trạng thái |
|---|---|
| A0 Art Contract | `docs/14` đã viết nhưng **chưa chính thức** (chưa ghi ADR ở `01` §6). Cổng A0 còn thiếu: nhập một mẫu vào Unity và chạy được trong game. |
| Hình mẫu | `tools/art_gen/` (Python). Mẫu hiện có chỉ **một hướng (Đông)** cho người chơi, Slime, 8 gạch sàn, đèn hải lưu. Chưa nhập vào Unity. |
| Hình trong game | Chỉ có hình tạm ở `Assets/_Game/Art/Placeholder` (khối màu 32×32, boss 64×64). |
| Code phải xong trước khi **nhập** hình | 9.5a (hướng 8 chiều) và 9.5b (một điểm quản lý màu sprite) trong `docs/13`. Kiểm `docs/HANDOFF.md` xem gói nào đã xong. Vẽ trước được; nhập vào game thì chờ. |

## 3. Quyết định đã chốt

Chi tiết và lý do nằm ở `docs/14`. Tóm tắt:

- Pixel art, 32 điểm ảnh một ô (PPU 32), lọc Point, nén None. Bảng màu cố định (`tools/art_gen/pixelkit.py`).
- Người chơi khung 48×48, quái thường 32×32, Elite 48×48, boss 64–128. Điểm neo ở chân (giữa, cách đáy 2 px).
- Vũ khí vẽ thành lớp riêng, cùng khung và cùng số khung với thân (ghép bằng Sprite Library).
- Ngày/đêm do Global Light 2D đổi màu (code đã có). Mọi hình thế giới dùng `Sprite-Lit-Default`.
- Màu báo đòn là chức năng: quái và boss phải "chịu được màu nhuộm" (khác hình gốc ≥ 60, chạy `tools/art_gen/tint_test.py`).

### 3.1. Tám hướng (quyết định mới 2026-10-16, sẽ chép vào `docs/14` §4.1)

- Nhìn từ trên xuống, hơi nghiêng. Nhân vật xoay đủ 8 hướng theo joystick.
- **Vẽ 5 bộ**: Nam `S`, Đông-Nam `SE`, Đông `E`, Đông-Bắc `NE`, Bắc `N`. Ba hướng phía Tây (Tây-Nam, Tây, Tây-Bắc) **lật ngang** bộ Đông-Nam, Đông, Đông-Bắc.
- Hệ quả cho người vẽ: không chữ hay logo bất đối xứng trên nhân vật; ánh sáng vẽ từ trên-trái ở mọi bộ (khi lật, ánh sáng lệch nhẹ, chấp nhận).
- Đặt tên: `Loại_Tên_Clip_Hướng.png`, ví dụ `Player_Body_Walk_SE.png`, `Player_Weapon_RustedAnchorBlade_Walk_NE.png`. Mẫu cũ không có hậu tố là hướng `E`.
- Animator của người chơi có hai tham số số thực `FaceX` (giá trị tuyệt đối) và `FaceY`; code đặt giá trị theo 8 hướng, hình lật do code lo (gói 9.5a).
- Hiệu ứng chém, vòng cảnh báo và tia: vẽ **một hướng** rồi để code xoay tự do, không vẽ 8 bộ.
- Duel (Phase 10c) là màn **nhìn ngang**, cần bộ hình riêng. Thuộc lớp A2, làm sau; đừng gộp vào A1.

## 4. Danh sách việc A1, theo thứ tự ưu tiên

| # | Hạng mục | Khung | Hướng | Clip (số khung) | Ghi chú |
|---|---|---|---|---|---|
| 1 | Người chơi, thân | 48×48 | 5 bộ | Idle 4 · Walk 6 · Attack 4 · Dodge 3 | Bản tối thiểu: Hurt 2 và Die 5 chỉ hướng `E` (lật cho phía Tây). Khoảng 92 khung. |
| 2 | Vũ khí lớp riêng: Kiếm Sắt, Kiếm Neo Gỉ | 48×48 | 5 bộ | như thân, trừ Hurt/Die | Khoảng 85 khung mỗi kiếm. Mẫu Kiếm Neo Gỉ hướng `E` đã có. |
| 3 | Quái: Slime, Lính Gác Lò Hơi (Guard), Elite Tầng 2, quái mới của 9.8 (tên tạm) | 32×32, Elite 48×48 | **đề xuất** 3 bộ (S, E, N), chéo dùng bộ gần nhất | Idle 4 · Walk 4 · Attack 4 · Hurt 1 · Die 4 | Giảm công vẽ. Chỉ nâng lên 5 bộ khi còn sức; cần người dùng xác nhận. Phải qua `tint_test.py`. |
| 4 | Boss Tầng 1 | 96×96 (tạm 64×64) | **đề xuất** 3 bộ | Idle 4 · Walk 4 · Telegraph 4 · Attack 4 · Punish 4 · PressureWindup 4 · PressureDash 2 · Die 6 | Boss tạm hiện không đạt kiểm tra màu (punish chỉ 38.1). Phải sáng và ít bão hòa. |
| 5 | Tileset: Tầng 1 (Thị trấn, Vùng Farm, Hầm Boss), Tầng 2 (Thị trấn, Vùng Farm) | ô 32×32 | không | gạch, mép, góc, vật cản | **Sau Phase 9.7a** (vì bố cục vùng quyết hình). |
| 6 | Vật dụng: đèn hải lưu, thùng, dây thừng, bảng, cổng hầm; NPC đứng yên | tuỳ | NPC 1 hướng `S` | NPC Idle 2 | Mẫu đèn hải lưu đã có. |
| 7 | Icon vật phẩm chính | 32×32 | không | 1 khung | Danh sách lấy từ ItemDatabase (hỏi người dùng). |
| 8 | Hiệu ứng: chém, trúng, tan khi chết, vòng cảnh báo dưới chân | tuỳ | 1 hướng, code xoay | vài khung | Vòng cảnh báo cần hook H6 (`docs/14` §7). |
| 9 | Giao diện tối thiểu: nút, thanh máu, khung | tuỳ | không | | Kiểm vùng chạm trên máy thật. |
| 10 | Nhạc và hiệu ứng âm thanh | | | | Theo gói 9.5c, ghi nguồn và giấy phép từng file. |

## 5. Quy trình cho mỗi lô hình

1. Nhận một lô nhỏ từ người dùng (ví dụ "Người chơi, Idle 5 hướng").
2. Vẽ theo `docs/14`: bảng màu §3, clip §4, khung và điểm neo §1 và §5.
3. Quái và boss: chạy `tools/art_gen/tint_test.py`, mọi số ≥ 60.
4. Ghi từng file vào `Assets/_Game/Art/ASSET_SOURCES.md` (nguồn, giấy phép, ngày).
5. Nhập vào Unity theo cài đặt ở `docs/14` §5. Người dùng làm trong Editor, AI viết từng bước.
6. Thử trong game. Người dùng chụp ảnh làm bằng chứng. **Không tự điền "Pass".**
7. Commit kèm file `.meta`. Đổi tên hoặc di chuyển hình ngay trong Unity Editor.
8. Báo cáo tối đa 6 dòng: đã làm gì / file ở đâu / việc người dùng cần làm / vấn đề.

## 6. Công cụ

- `tools/art_gen/` (Python, Pillow, numpy): vẽ bằng code theo "vật liệu", tự tô sáng tối và kẻ viền, mọi màu lấy từ bảng. Mạnh ở nền, vật dụng, quái dạng khối, hiệu ứng, giao diện. Yếu hơn ở nhân vật người nhiều tư thế (cần chỉnh từng tư thế).
- Gói Unity đã cài (kiểm lại trong `Packages/manifest.json` trước khi dùng): `com.unity.2d.animation` (Sprite Library và Sprite Resolver cho lớp vũ khí), nhập file Aseprite trực tiếp, nhập PSD, Tilemap và Tilemap Extras, SpriteShape. Renderer là URP 2D (`Assets/Settings/Renderer2D.asset`).
- Phân công đề nghị: để code vẽ khoảng hai phần ba khối lượng (nền, vật dụng, quái khối, hiệu ứng, giao diện). Nhân vật chính, boss và ảnh giới thiệu game nên do họa sĩ vẽ hoặc dùng hình có giấy phép rõ ràng. **Chưa quyết ai vẽ nhân vật chính**, hỏi người dùng.

## 7. Điều cấm

- Sửa script, scene, prefab, `.meta`, asmdef. Việc code do Kiro làm theo `docs/13`.
- Dùng hình, âm thanh, tên, giao diện của tác phẩm có bản quyền, hay làm thiết kế giống chúng.
- Đổi màu báo đòn (`docs/14` §2), số khung, số hướng hay điểm neo mà không hỏi người dùng.
- Dùng animation event để gây sát thương (đòn trúng hay trượt do số liệu quyết).
- Tạo tệp lớn hoặc ảnh không ai nhờ.
- Tự điền "Pass" vào bảng bằng chứng.

## 8. Việc đầu tiên của AI làm đồ họa mới

1. Đọc theo thứ tự ở §0.
2. Kiểm `docs/HANDOFF.md`: 9.5a và 9.5b đã xong chưa.
3. Hỏi người dùng đúng ba điều, gộp trong một tin: (a) ai vẽ nhân vật chính và boss; (b) bắt đầu từ lô nào; (c) có giữ thư mục `tools/art_gen` không.
4. Việc nhỏ làm được ngay: đóng cổng A0 bằng cách nhập Slime mẫu vào Unity (theo `docs/14` §5) và chạy thử.
5. Nhờ người điều phối (Integrator) ghi ADR cho Art Track ở `01` §6 và cập nhật `10` FB-026.

## 9. Mẫu giao việc

```text
Bạn nhận lô đồ họa <tên lô> trong docs/15 §4.
Đọc: docs/15 và docs/14. Không đọc cả thư mục docs/.
Chỉ tạo hoặc sửa trong Assets/_Game/Art/** (hình) và tools/art_gen/**.
Không sửa script, scene, prefab, .meta, asmdef.
Theo đúng bảng màu, khung, điểm neo, số khung, cách đặt tên ở docs/14.
Quái và boss phải qua tint_test.py (mọi số >= 60).
Ghi nguồn từng file vào Assets/_Game/Art/ASSET_SOURCES.md.
Không tự điền Pass.
Kết thúc tối đa 6 dòng: Đã làm / File / Việc cần làm trong Unity / Vấn đề.
```

## 10. Việc code nhỏ đang chờ duyệt (xem `docs/14` §7)

| Mã | Việc | Gói |
|---|---|---|
| H1 | Đặt màu `SpriteRenderer` của Player.prefab về trắng (đang nhuộm xanh lơ) | Thao tác Editor, khi nhập hình thật |
| H2 | Một điểm quản lý màu sprite | Đã nằm trong gói 9.5b (`13`) |
| H3 | Quái và boss có Animator và nhận hướng 8 chiều | Gói hook art, **chưa duyệt** |
| H4 | Vũ khí lớp riêng đổi theo trang bị | Gói hook art, **chưa duyệt** |
| H5 | Sorting Layer và trục sắp xếp theo Y | Thao tác Editor |
| H6 | Vòng cảnh báo dưới chân quái và boss | Gói hook art, **chưa duyệt** |

## 11. Câu hỏi mở

- ADR cho Art Track chưa ghi ở `01` §6.
- Bật Pixel Perfect Camera (tham chiếu 640×360, đổi camera là sửa scene, cần chủ scene duyệt).
- Quái và boss 3 hay 5 bộ hướng.
- Ai vẽ nhân vật chính và boss.
- Nguồn nhạc và âm thanh (giấy phép).
- Bộ hình nhìn ngang cho Duel (A2).
