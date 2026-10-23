# AGENTS.md — Game2D

> File tự nạp mỗi phiên. Giữ ngắn. Chi tiết ở `docs/`.

## Dự án

2D Action RPG leo tầng cho Android (Unity + C#). Đang ở Mốc 1: **ONLINE** — phải đăng nhập (ẩn danh) mới chơi; PvE chơi một mình nhưng tiến trình/boss dùng chung server. Duel (phiên realtime 2 người, không cùng máy) thuộc **Mốc 2** (ADR-028 ở `docs/01` §6); Mốc 1 kết thúc ở Phase 11 (kiểm tổng). Có **hệ thống ngày/đêm** và **permadeath** (chết PvE là chết vĩnh viễn) là điểm nhấn gameplay.
Phase hiện tại và việc kế tiếp: xem `docs/STATUS.md` (thứ tự làm 9.5 → 9.9 → 11 → 10; điều kiện đóng phase: `docs/05`).
Lưu ý Editor: nếu bật "Enter Play Mode Options" (tắt Domain Reload) thì Play lần 2 trở đi KHÔNG chạy lại luồng đăng nhập ở scene Boot → màn hình đứng ở "Đang đăng nhập...". Tắt tuỳ chọn đó trong Project Settings → Editor.
Danh tính, mốc, quyết định đã chốt: `docs/01_MASTER_PROJECT_CONTEXT.md`.

## Luật cứng

1. Một hệ thống Core → nhiều tầng. Tầng là data/content: không copy code theo tầng, không `if floor == N` trong Core, không hard-code số tầng tối đa.
2. Hướng phụ thuộc: Content → PvE/PvP → Services → Core. Core không biết Floor/Enemy/Item/PvP cụ thể. Không phụ thuộc vòng. PvE và PvP không tham chiếu nhau.
3. UI chỉ hiển thị và gửi ý định; không chứa logic damage, thưởng, inventory, save, death.
4. Không GameManager/PlayerManager khổng lồ; kiểm tra hệ thống sẵn có trước khi tạo cái mới.
5. Balance nằm trong config/ScriptableObject (số liệu khởi điểm: `docs/07`); không magic number.
6. Save qua `ISaveService`; World State qua `IWorldStateService`; Time qua `ITimeService`; Death qua `IDeathService`.
7. **BẢN QUYỀN — LUẬT CỨNG:** Không dùng tên game "Sword Art Online", "SAO", "Aincrad", "Aincrad Online" hay bất kỳ biến thể nào. Không dùng tên nhân vật SAO (Kirito, Asuna, Klein, Agil, Silica, Lisbeth, Heathcliff...). Không dùng tên skill SAO (Starburst Stream, Vorpal Strike, Rage Spike, Eclipse, Horizontal Square, Vertical Square...). Không dùng tên vũ khí SAO (Elucidator, Dark Repulser, Lambent Light...). Không dùng tên boss SAO (The Gleam Eyes, Illfang the Kobold Lord, The Skull Reaper...). Không dùng hình ảnh, âm thanh, UI, cốt truyện, thiết kế nhân vật từ SAO. Không dùng tên hay cơ chế nhận diện của Naruto, Bleach hay bất kỳ IP nào khác. CHỈ được lấy ý tưởng cơ chế và cấu trúc chung của thể loại game leo tháp sinh tử (leo tầng, boss tầng mở đường cho mọi người chơi, mastery vũ khí và Weapon Technique, nối kỹ năng, đội/raid, ngày/đêm, duel side-view, permadeath...). Càng giống thể loại ở mức này càng tốt (hướng: `docs/16`); mọi tên, hình ảnh, âm thanh, UI, cốt truyện, thiết kế phải nguyên bản.
8. Được đổi kiến trúc mà không cần chờ xác nhận, nhưng phải ghi ADR ngắn ở `docs/01` §6 TRƯỚC khi làm (hiện trạng → vấn đề → quyết định → module bị ảnh hưởng) và có test. "Kiến trúc" gồm: hướng phụ thuộc giữa module, cấu trúc save/World State (phải tương thích ngược), interface dịch vụ và Cloud Code, giao thức mạng. Thêm class, thư mục, asmdef trong một module hoặc data mới thì không cần ADR. Chỉ chủ dự án được đổi luật 7, 9, 10.
9. **BACKEND ABSTRACTION — LUẬT CỨNG:**

   * Không gọi thẳng SDK của Unity Gaming Services (UGS) từ code gameplay (PvE/PvP) — luôn qua interface ở Core, cài đặt ở `Game.Services`.
   * **PvE**: qua `ISaveService`, `IWorldStateService`, `ITimeService`, `IDeathService`, `IAccountService`, `IAccountResetService`, `IAccountLinkService`, `IServerFunctionService`.
   * **PvP**: qua `ILobbyService`, `IRelayService`, `INetworkTransport` — KHÔNG gọi trực tiếp `Unity.Services.Lobby`, `Unity.Services.Relay`, `Unity.Netcode` từ code PvP.
   * **Cài đặt UGS** nằm ở `Game.Services` (`UGSLobbyService`, `UGSRelayService`, `UGSNetworkTransport`, `UGSServerFunctionService`, v.v.).
   * **Mục đích:** cho phép đổi sang backend khác (Spring, Multiplayer Services SDK mới...) mà chỉ cần viết cài đặt mới, không sửa gameplay.
   * **BackendConfig** (ScriptableObject) quyết định backend nào được dùng: `"UGS"` | `"Spring"` | `"Hybrid"`.
   * KHÔNG dùng Unity Multiplay (đã ngừng 4/2026).
10. **PERMADEATH + ACCOUNT:**

    * **PvE: chết do enemy/boss/trap → account bị khóa → phải tạo account mới.** Không respawn, không revive item. Có Safe Zone (spawn + NPC + shop) bảo vệ.
    * **PvP: KHÔNG có permadeath.** Duel thuộc Mốc 2 (ADR-028); mode đầu tiên là **Half-Loss** (ai còn < 50% HP thì thua).
    * **Guest Account:** tạo bằng ẩn danh, reset được, link được với Google/Facebook/Apple. **Nút Reset Guest chỉ ở Login Screen (góc trên trái) và Death Screen** — KHÔNG ở Settings, KHÔNG ở MainMenu.
    * **Official Account:** không reset được. Chết → chỉ có nút "Thoát".
    * **"Khóa account" là logic game** (Player Data `isDead = true`), KHÔNG xóa thật trên UGS.

## Cách làm việc

* **Đầu phiên:** đọc `docs/STATUS.md` (đang ở đâu, việc kế tiếp) rồi chỉ đọc tài liệu/code của đúng việc đó. **Cuối phiên:** tự cập nhật `docs/STATUS.md` (tối đa khoảng 5 dòng; chi tiết cài đặt để ở code và test, không chép vào đó).
* Chế độ làm việc (ADR-031): AI nhận cả một phase hoặc nhiều gói liên tiếp và làm liền (code + test + nối Unity qua MCP + cập nhật STATUS), không cần chờ chủ dự án giữa các gói. Không tự sang phase kế khi phase hiện tại chưa "xong để làm tiếp" (code, test xanh, nối scene, chơi thử trong Editor). Chỉ dừng và hỏi chủ dự án khi: test đỏ mà sửa hai lần không được; cần đổi luật 7, 9 hoặc 10; cần máy thật hoặc APK; hành động phá hủy (xóa dữ liệu, git phá hủy). Nhiều AI chạy song song chỉ khi chia path rõ ràng (`archive/13`); không cùng sửa một file.
* Trước khi sửa: tìm file liên quan, ĐỌC code hiện có, xác định phụ thuộc. Không viết code thay thế chỉ dựa vào tên file.
* Đọc vừa đủ: task → tài liệu liên quan → module → phụ thuộc trực tiếp. Không đọc cả project.
* Bug: tái hiện → xác định module → tìm nguyên nhân gốc → sửa nhỏ nhất → test → kiểm tra hồi quy.
* Đổi gameplay/kiến trúc lớn: phân tích tác động trước, ghi ADR (luật 8), rồi làm; không cần chờ.
* Việc lớn: chia bước (thiết kế → lõi → tích hợp → test); mỗi bước build và test được.
* Không đụng file ngoài phạm vi; giữ nguyên thay đổi của người dùng; không thao tác git phá hủy khi chưa hỏi.
* Docs và code lệch nhau: phần đã làm thì code chạy được và test xanh thắng; phần chưa làm thì docs thắng. Ghi lệch vào `docs/STATUS.md` rồi làm tiếp.

## Tự chủ của AI (chủ dự án duyệt 2026-10-20, ADR-031)

* AI được tự thêm tính năng, hệ thống, nội dung, hiệu ứng, âm thanh, UI mới và tự quyết các mục "Chờ quyết" ở `docs/10`, miễn là tốt cho game và không phạm luật cứng 1–7, 9, 10. Không cần hỏi trước.
* Mục tiêu sản phẩm: giống thể loại leo tháp sinh tử nhất có thể về cơ chế, cấu trúc tháp, nhịp leo tầng và cảm giác chiến đấu (`docs/16`). Luật 7 vẫn là trần: không dùng tên, hình, âm thanh, UI, cốt truyện hay thiết kế của SAO.
* Mỗi thứ AI tự thêm phải có test (hoặc kiểm tra trong Editor nếu là hiệu ứng), chỉnh được bằng data khi hợp lý, và được ghi một dòng vào `docs/STATUS.md` mục "AI đã tự thêm" cùng một dòng ở `docs/10`. Chủ dự án xem lại sau và có thể yêu cầu bỏ.
* Thứ tự ưu tiên khi tự chọn việc: (1) làm loop leo tầng đang chơi hấp dẫn hơn (nhìn, nghe, chạm); (2) cho ba điểm nhấn permadeath, tháp dùng chung, ngày/đêm có tiếng vang; (3) chiều sâu chiến đấu; (4) nội dung mới. Việc lớn chia bước nhỏ, mỗi bước chơi được; sau mỗi thay đổi lớn toàn bộ test EditMode xanh và vào được Play Mode.

## Unity

* Người dùng chưa có nền tảng lập trình game sâu: giải thích ngắn gọn code quan trọng.
* AI sửa file text (C#, `.asmdef`, JSON, docs). KHÔNG sửa tay `.unity/.prefab/.asset/.meta` trừ khi được yêu cầu.
* Được dùng Unity MCP để tạo/gắn GameObject, component, field và lưu scene/prefab (đi qua Editor, không sửa tay YAML); nên một AI làm được cả code lẫn nối Unity trong một lượt. Không lưu đè scene đang có thay đổi chưa lưu của người dùng (`isDirty`); nếu Unity không phản hồi thì báo lại, không dồn lệnh.
* Việc trong Unity Editor AI tự làm qua MCP; chỉ ghi **Việc cần làm trong Unity Editor** khi AI không làm được, và **Cách test** khi cần chủ dự án thử trên điện thoại.
* Android: landscape + cảm ứng; chơi thử nhanh trong Editor mỗi gói, test trên máy thật ở các mốc ghi trong `docs/STATUS.md`; tránh Instantiate/Destroy liên tục trong combat (pooling khi cần).

## Báo cáo sau mỗi task

Xong gì (file chính) · Test (xanh/đỏ, bao nhiêu) · Cần chủ dự án làm (chỉ khi có) · Lệch hoặc rủi ro · Bước tiếp. Tối đa 5 dòng.

## Bản đồ tài liệu (đọc khi cần, không đọc hết)

Bố cục: `docs/` giữ file đang dùng; `docs/ref/` = hợp đồng theo vùng code (đọc khi chạm đúng vùng đó); `docs/archive/` = đã xong hoặc lỗi thời (không cập nhật nữa). Số `NN` trong tài liệu = file `NN_*.md` ở một trong ba thư mục đó. Prompt các gói Phase 9.7–9.9: `Phase9_5-9_9_prompts_v2.md` ở thư mục gốc dự án (STATUS ghi số dòng từng gói).

|Việc|Đọc|
|-|-|
|Đang ở đâu, việc kế tiếp, việc chờ chủ dự án quyết (đọc đầu phiên)|`STATUS.md`|
|Bắt đầu phase mới|`05` — phần phase đó|
|Sửa gameplay, combat, enemy, boss, quest, floor/world|`12` trước, rồi `02` — phần liên quan|
|Data, save, inventory, item, world state, time data|`03`|
|Duel/PvP/network|`04` (+ `02` §49)|
|Tạo module, thư mục, asmdef, refactor|`06`|
|Số liệu Level/EXP/Weapon Mastery/Weapon Technique/Item Tier|`07` (+ `12` khi đổi contract)|
|Hệ thống ngày/đêm|`08`|
|Permadeath, tài khoản, Duel Challenge|`09`|
|Cốt truyện, thế giới, tên tầng, NPC, hướng art/UI|`11`|
|Hướng sản phẩm, ý tưởng hệ thống, backlog Mốc 2–3, độ giống thể loại|`16`|
|Chia việc cho hai AI, ownership, merge gate (đã lưu trữ; chỉ khi thật sự chạy hai AI song song)|`archive/13`|
|Backlog ý tưởng, quyết định cắt/đổi thứ tự phase|`10`|
|Đồ họa: khung hình, bảng màu, hướng nhìn 8 hướng, animation, thư mục Art|`14` (+ `15` nếu là AI làm đồ họa)|

## Cập nhật tài liệu

Đổi danh tính/mốc/quyết định → `01`; nguyên lý gameplay/public contract → `12` (và `02` nếu kiến trúc gameplay đổi); data/save → `03`; PvP/network → `04`; thứ tự phase → `05`; cấu trúc code → `06`; số liệu combat/progression → `07`; ngày/đêm → `08`; death/account/challenge → `09`; ownership hoặc thứ tự phối hợp hai AI → `13`; đồ họa/Art Track → `14` (và `15` nếu bàn giao đổi); tiến độ, việc kế tiếp và mục AI đã tự thêm → `STATUS.md` (kèm một dòng ở `10`). Quyết định lớn: ghi ADR ở `01` §6.

