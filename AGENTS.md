# AGENTS.md — Game2D

> File tự nạp mỗi phiên. Giữ ngắn. Chi tiết ở `docs/`.

## Dự án

2D Action RPG leo tầng cho Android (Unity + C#). Đang ở Mốc 1: **ONLINE** — phải đăng nhập (ẩn danh) mới chơi; PvE chơi một mình nhưng tiến trình/boss dùng chung server; Duel là phiên realtime nhỏ (2 người, không cùng máy). Có **hệ thống ngày/đêm** và **permadeath** (chết PvE là chết vĩnh viễn) là điểm nhấn gameplay.
Phase hiện tại: **Phase 9.5–9.9** (thứ tự và điều kiện đóng phase: `docs/05`). Phase 8 và 9 đã có code + tests; device evidence (bảng P8-5, P9-3) còn chờ điền nên chưa ghi DONE.
Lưu ý Editor: nếu bật "Enter Play Mode Options" (tắt Domain Reload) thì Play lần 2 trở đi KHÔNG chạy lại luồng đăng nhập ở scene Boot → màn hình đứng ở "Đang đăng nhập...". Tắt tuỳ chọn đó trong Project Settings → Editor.
Danh tính, mốc, quyết định đã chốt: `docs/01_MASTER_PROJECT_CONTEXT.md`.

## Luật cứng

1. Một hệ thống Core → nhiều tầng. Tầng là data/content: không copy code theo tầng, không `if floor == N` trong Core, không hard-code số tầng tối đa.
2. Hướng phụ thuộc: Content → PvE/PvP → Services → Core. Core không biết Floor/Enemy/Item/PvP cụ thể. Không phụ thuộc vòng. PvE và PvP không tham chiếu nhau.
3. UI chỉ hiển thị và gửi ý định; không chứa logic damage, thưởng, inventory, save, death.
4. Không GameManager/PlayerManager khổng lồ; kiểm tra hệ thống sẵn có trước khi tạo cái mới.
5. Balance nằm trong config/ScriptableObject (số liệu khởi điểm: `docs/07`); không magic number.
6. Save qua `ISaveService`; World State qua `IWorldStateService`; Time qua `ITimeService`; Death qua `IDeathService`.
7. **BẢN QUYỀN — LUẬT CỨNG:** Không dùng tên game "Sword Art Online", "SAO", "Aincrad", "Aincrad Online" hay bất kỳ biến thể nào. Không dùng tên nhân vật SAO (Kirito, Asuna, Klein, Agil, Silica, Lisbeth, Heathcliff...). Không dùng tên skill SAO (Starburst Stream, Vorpal Strike, Rage Spike, Eclipse, Horizontal Square, Vertical Square...). Không dùng tên vũ khí SAO (Elucidator, Dark Repulser, Lambent Light...). Không dùng tên boss SAO (The Gleam Eyes, Illfang the Kobold Lord, The Skull Reaper...). Không dùng hình ảnh, âm thanh, UI, cốt truyện, thiết kế nhân vật từ SAO. Không dùng tên hay cơ chế nhận diện của Naruto, Bleach hay bất kỳ IP nào khác. CHỈ được lấy ý tưởng cơ chế chung (leo tầng, mastery vũ khí và Weapon Technique, ngày/đêm, duel side-view, permadeath).
8. Không tự đổi kiến trúc. Cần đổi thì nêu: hiện trạng → vấn đề → đề xuất → module bị ảnh hưởng, rồi chờ xác nhận.
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
    * **PvP: KHÔNG có permadeath.** Chỉ 1 mode ở Mốc 1: **Half-Loss** (ai còn < 50% HP thì thua).
    * **Guest Account:** tạo bằng ẩn danh, reset được, link được với Google/Facebook/Apple. **Nút Reset Guest chỉ ở Login Screen (góc trên trái) và Death Screen** — KHÔNG ở Settings, KHÔNG ở MainMenu.
    * **Official Account:** không reset được. Chết → chỉ có nút "Thoát".
    * **"Khóa account" là logic game** (Player Data `isDead = true`), KHÔNG xóa thật trên UGS.

## Cách làm việc

* Mỗi AI chỉ nhận một work package mỗi lần. Chỉ chạy song song theo ownership, public contract và merge gate trong `docs/13`; không cùng sửa file hoặc tự đổi contract. Xong thì báo cáo và DỪNG; không tự sang phase kế, không thêm tính năng ngoài yêu cầu.
* Trước khi sửa: tìm file liên quan, ĐỌC code hiện có, xác định phụ thuộc. Không viết code thay thế chỉ dựa vào tên file.
* Đọc vừa đủ: task → tài liệu liên quan → module → phụ thuộc trực tiếp. Không đọc cả project.
* Bug: tái hiện → xác định module → tìm nguyên nhân gốc → sửa nhỏ nhất → test → kiểm tra hồi quy.
* Đổi gameplay/kiến trúc lớn: phân tích tác động trước; chờ xác nhận nếu thuộc kiến trúc.
* Việc lớn: chia bước (thiết kế → lõi → tích hợp → test); mỗi bước build và test được.
* Không đụng file ngoài phạm vi; giữ nguyên thay đổi của người dùng; không thao tác git phá hủy khi chưa hỏi.
* Docs và code lệch nhau: báo lệch, đừng tự cho là bên nào đúng.

## Unity

* Người dùng chưa có nền tảng lập trình game sâu: giải thích ngắn gọn code quan trọng.
* AI sửa file text (C#, `.asmdef`, JSON, docs). KHÔNG sửa tay `.unity/.prefab/.asset/.meta` trừ khi được yêu cầu.
* Mọi câu trả lời có code kết thúc bằng hai mục: **Việc cần làm trong Unity Editor** và **Cách test** (Editor + điện thoại).
* Android: landscape + cảm ứng; test trên máy thật cuối mỗi phase; tránh Instantiate/Destroy liên tục trong combat (pooling khi cần).

## Báo cáo sau mỗi task

Changed (file) · Why · Not changed · Test đã làm · Impact (PvE / PvP / Save / UI / Time / Death) · Known issues · Next (chỉ đề xuất).

## Bản đồ tài liệu (đọc khi cần, không đọc hết)

|Việc|Đọc|
|-|-|
|Bắt đầu phase mới|`05` — phần phase đó|
|Sửa gameplay, combat, enemy, boss, quest, floor/world|`12` trước, rồi `02` — phần liên quan|
|Data, save, inventory, item, world state, time data|`03`|
|Duel/PvP/network|`04` (+ `02` §49)|
|Tạo module, thư mục, asmdef, refactor|`06`|
|Số liệu Level/EXP/Weapon Mastery/Weapon Technique/Item Tier|`07` (+ `12` khi đổi contract)|
|Hệ thống ngày/đêm|`08`|
|Permadeath, tài khoản, Duel Challenge|`09`|
|Cốt truyện, thế giới, tên tầng, NPC, hướng art/UI|`11`|
|Chia việc cho hai AI, ownership, contract, handoff/merge gate|`13`|

## Cập nhật tài liệu

Đổi danh tính/mốc/quyết định → `01`; nguyên lý gameplay/public contract → `12` (và `02` nếu kiến trúc gameplay đổi); data/save → `03`; PvP/network → `04`; thứ tự phase → `05`; cấu trúc code → `06`; số liệu combat/progression → `07`; ngày/đêm → `08`; death/account/challenge → `09`; ownership hoặc thứ tự phối hợp hai AI → `13`. Quyết định lớn: ghi ADR ở `01` §6.

