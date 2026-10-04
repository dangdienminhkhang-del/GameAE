# ORIGINAL GAMEPLAY BIBLE
## Game2D Original Action RPG Specification

> Nguồn thiết kế cho gameplay mới. Đọc tài liệu này trước khi thêm combat, vũ khí, kỹ năng, quest, boss hoặc balance.
> Tài liệu này định nghĩa nguyên lý và hợp đồng triển khai; số liệu cụ thể nằm ở `07_CHARACTER_COMBAT_DESIGN.md`; kế hoạch chia việc AI nằm ở `13_TWO_AGENT_IMPLEMENTATION_PLAN.md`.

---

## 1. Mục tiêu và phạm vi

Game2D là action RPG 2D cho Android, chơi PvE một mình trong một tháp nhiều tầng. Người chơi đăng nhập bắt buộc, khám phá tầng, chiến đấu, tích lũy sức mạnh, hoàn thành mục tiêu và vượt boss để mở tiến trình chung của thế giới.

Mốc 1 phải tạo được một lát cắt hoàn chỉnh, không phải một bộ hệ thống rời rạc:

```text
Đăng nhập → vào Floor 1 → nhận quest → chiến đấu → tăng sức mạnh
→ hoàn thành/đổi quest → vào boss → World State mở tầng kế
→ đăng xuất/đăng nhập lại vẫn đúng tiến trình
```

Phạm vi Mốc 1:

- Một loại vũ khí chính có 2 Technique đầu tiên.
- Floor 1 hoàn chỉnh và Floor 2 được thêm bằng data, không sửa Core.
- Ngày/đêm, quest, Safe Zone, permadeath PvE và boss state dùng chung.
- Duel 1v1 online chỉ Half-Loss; nó không tác động World State, inventory hay death.

Không mở rộng số lượng content nếu loop trên chưa chạy được trên điện thoại thật.

## 2. Bốn trụ cột thiết kế

| Trụ cột | Quy tắc thiết kế | Hệ quả khi code |
|---|---|---|
| Đọc được | Người chơi phải thấy trước đòn đánh, vùng nguy hiểm và thời điểm hở của cả hai bên. | Mỗi attack/Technique có wind-up, active window, recovery và telegraph rõ. |
| Lựa chọn có giá | Đòn mạnh, dash và đổi vị trí luôn có cooldown hoặc recovery. | Không có Technique vừa an toàn, vừa cơ động, vừa gây damage cao. |
| Sức mạnh có nguồn | Level, trang bị và mastery là ba nguồn riêng, đo được. | Không cộng stat bí mật trong UI hoặc logic scene. |
| Thế giới có hậu quả | Giờ game, boss clear và death có ý nghĩa thật. | World State chỉ server ghi; death là save critical; thời gian không được reset cục bộ. |

## 3. Quy tắc nguyên bản

- Chỉ dùng nguyên lý game chung: leo tầng, mastery vũ khí, combat thời gian thực, ngày/đêm, duel và rủi ro mất nhân vật.
- Không dùng một tác phẩm, nhân vật, cốt truyện, tên, biểu tượng, nhạc, UI, moveset, danh sách kỹ năng, mốc mastery hay công thức của bất kỳ IP nào làm đặc tả.
- Mọi tên hiển thị, ID content, animation, VFX, thoại và nhịp kỹ năng phải được tạo từ tài liệu này hoặc content brief riêng.
- Khi một đề xuất có mô tả “giống hệt nguồn tham chiếu”, dừng triển khai và thiết kế lại theo trụ cột ở §2.

## 4. Vòng lặp phút giây

### 4.1. Điều khiển Android

| Ý định | Điều khiển | Phản hồi bắt buộc |
|---|---|---|
| Di chuyển | Joystick trái | Hướng và animation đổi ngay; va chạm không xuyên tường. |
| Đòn thường | Nút Attack | Wind-up, hit xác nhận, recovery. |
| Technique | Nút Technique | Chỉ sáng khi hợp lệ; khi bị khóa phải nói rõ cooldown/mastery/trạng thái. |
| Né | Nút Dodge | Lướt ngắn, cooldown, không dùng để xuyên vật cản. |
| Tương tác | Nút Interact khi đứng gần | Highlight mục tiêu; không đòi bàn phím trên điện thoại. |

Phím bàn phím chỉ là input hỗ trợ trong Editor. Một feature chưa có đường đi bằng cảm ứng thì chưa đạt tiêu chí mobile.

### 4.2. Trạng thái combat chung

```text
Idle / Move
  ├─ AttackRequested ─→ WindUp ─→ Active ─→ Recovery ─→ Idle / Move
  ├─ TechniqueRequested → Validate → WindUp ─→ Active ─→ Recovery ─→ Idle / Move
  ├─ DodgeRequested ─→ Dodge ─→ Recovery ─→ Idle / Move
  └─ HitReceived ─→ Hurt hoặc Death
```

Quy tắc:

1. Chỉ `Active` mới được tạo hitbox.
2. Mỗi hitbox phải có attack instance ID; một mục tiêu chỉ nhận một hit cho mỗi instance trừ khi definition cho phép multi-hit riêng.
3. `Recovery` là một phần balance, không được bỏ chỉ để combat nhanh hơn.
4. Cancel chỉ hợp lệ nếu `AttackDefinition`/`WeaponTechniqueDefinition` cho phép. Không viết ngoại lệ trong controller theo từng skill.
5. Damage resolution, cooldown và death là Core. Animation/UI chỉ nghe event và không tự trừ máu.

## 5. Chỉ số và damage contract

Mốc 1 dùng ba stat chiến đấu hiện có:

| Stat | Nguồn chính | Vai trò |
|---|---|---|
| MaxHealth | Character Level, equipment | Số lỗi người chơi có thể chịu trong combat. |
| Attack | Character Level, weapon, equipment | Đầu vào damage. |
| Defense | Character Level, armor, enemy definition | Giảm một phần damage nhận vào. |

Level, Weapon Mastery và Equipment Power không được thay thế lẫn nhau:

- Level là nền stat ổn định.
- Mastery mở khả năng dùng Technique, không cộng damage trực tiếp.
- Equipment thay đổi build qua stat roll/effect, không tự mở Technique.

Damage Mốc 1:

```text
rawDamage = attackerAttack × abilityMultiplier
finalDamage = max(1, round(rawDamage - defenderDefense × defenseWeight))
```

- `abilityMultiplier` và `defenseWeight` là data của definition/ruleset, không là magic number trong MonoBehaviour.
- Đòn nhiều hit có `totalMultiplier`; definition chia nó thành các `hitMultiplier` rõ ràng. Tổng hit multiplier không được vượt `totalMultiplier`.
- Nếu thay công thức, phải cập nhật pure EditMode tests, bảng TTK ở `07`, và PvP RuleSet cùng một pull request.

## 6. Weapon Discipline và Technique

### 6.1. Khái niệm

`Weapon Discipline` là một họ vũ khí có nhịp chơi riêng. `Weapon Technique` là action có dữ liệu, chỉ dùng được khi nhân vật trang bị đúng Discipline và đạt mastery yêu cầu.

Các Discipline dự kiến:

| Discipline | Nhịp chơi | Điểm mạnh | Điểm yếu |
|---|---|---|---|
| Blade | Cân bằng | Chuỗi đánh dễ đọc, guard window ngắn | Không vượt trội ở tầm hay burst. |
| Knives | Áp sát | Di chuyển, nhiều hit nhẹ | Tầm rất ngắn, hở nếu lao sai. |
| Needle | Kiểm soát tầm | Đâm thẳng, phản ứng chính xác | Góc đánh hẹp. |
| Greatblade | Cam kết | Damage một hit, quét nhóm nhỏ | Wind-up và recovery dài. |
| Maul | Phá phòng thủ | Stagger/knockback có giới hạn | Chậm, dễ bị vòng ra sau. |
| Cleaver | Momentum | Lướt theo hướng, áp lực liên tục | Khó dừng đúng vị trí. |

Tên display, animation và Technique cụ thể do content brief quyết định; bảng trên không phải danh sách move bắt buộc.

### 6.2. Mastery

Mastery riêng theo Discipline, từ 0 đến 100.

| Band | Mốc | Mở khóa | Mục đích |
|---|---:|---|---|
| Foundation | 0 | Basic attack và Core Technique | Học nhịp vũ khí. |
| Traverse | 25 | Technique cơ động hoặc đặt vị trí | Giải quyết khoảng cách. |
| Control | 55 | Technique kiểm soát/defensive | Tạo lựa chọn chiến thuật. |
| Signature | 85 | Technique mạnh có rủi ro cao | Phần thưởng cho người gắn bó. |

Mastery credit Mốc 1:

- Basic hit xác nhận: +1.
- Technique hit xác nhận: +2.
- Mỗi enemy instance có trần 4 credit; enemy bị farm lặp không tạo mastery vô hạn.
- Không cộng mastery khi đánh object, Safe Zone target hoặc mục tiêu đã chết.
- Mastery lưu Player Data; danh sách Technique đã mở được suy ra từ mastery, không lưu bản sao.

Mốc 1 chỉ triển khai Blade Foundation và Traverse. Các Band còn lại là Mốc 2; definition vẫn phải hỗ trợ từ đầu để không đổi model dữ liệu.

### 6.3. Weapon Technique Definition

Một `WeaponTechniqueDefinition` tối thiểu có:

```text
id
weaponDiscipline
masteryRequired
inputSlot                 // Core, Traverse, Control, Signature
cooldownSeconds
windUpSeconds
activeWindows[]           // thời điểm, hit profile, movement policy
recoverySeconds
totalMultiplier
movementPolicy            // None, Step, Dash, Commit
cancelPolicy
hitProfileIds
pveRules / pvpRules
presentationId
```

Definition không chứa code custom cho từng skill. Cơ chế mới cần được biểu diễn bằng hit profile, movement policy, status effect đã đăng ký hoặc một module có thể tái dùng; nếu không làm được, mở Architecture Change Request theo `13`.

## 7. Enemy và boss

### 7.1. Enemy

Mỗi enemy có một `CombatRole`: Chaser, Skirmisher, Guard, Ranged hoặc Elite. Role xác định hành vi, không phải Floor number.

Enemy bắt buộc có:

- Telegraph trước attack.
- Thời gian từ lần trúng này đến lần trúng kế tiếp đủ để đọc trên màn hình điện thoại.
- Spawn rule theo Floor data và Time rule; không spawn trong Safe Zone.
- Reward table, EXP và stat từ definition/scaling.

### 7.2. Boss

Boss Mốc 1 cần 3 pha đọc được:

```text
Observe → Punish Window → Pressure Window → lặp lại
```

- `Observe`: người chơi nhìn pattern và reposition.
- `Punish Window`: boss kết thúc attack cam kết; người chơi có thời gian tấn công.
- `Pressure Window`: boss buộc người chơi di chuyển/né, không đứng đánh tự do.

Boss chết chỉ phát request lên server function. Server xác nhận rồi mới đổi World State. Client không được tự mở Floor tiếp theo.

## 8. Quest và tương tác

### 8.1. Quest state machine

```text
Locked → Available → Active → ReadyToTurnIn → Completed
```

- `Locked`: prerequisite/floor/time chưa đúng.
- `Available`: có thể nhận.
- `Active`: objective đang đếm.
- `ReadyToTurnIn`: objective đủ nhưng chưa trao thưởng.
- `Completed`: đã trao thưởng và save thành công.

`Completed` không được dùng để chỉ “đã đủ số lượng”. Với quest repeatable, completion tạo cooldown/reset rule trong definition rồi trở lại `Available`; không reset trực tiếp từ UI.

### 8.2. Objective contract

| Objective | Event đầu vào | Điều kiện hợp lệ | Ghi chú |
|---|---|---|---|
| Kill | `EnemyDefeated(enemyId)` | quest Active, target ID khớp | Không đếm enemy đã chết trước khi nhận quest. |
| Collect | `ItemAcquired(itemId, quantity)` | quest Active, target ID khớp; chỉ quantity dương từ acquisition đã commit | Definition quyết định consume-on-turn-in hay giữ item. Khi nhận quest, seed `progress = min(required, số lượng đang có)`; turn-in cần consume mà không đủ số lượng → từ chối, quest về `Active`. |
| Talk | `NpcTalked(npcId)` | NPC và quest target khớp | Không tự nhận quest khi chỉ đi ngang NPC. |
| Reach | `ZoneEntered(zoneId)` | quest Active, zone khớp | Trigger chỉ báo intent, Quest service quyết định progress. |

Quest có time rule phải kiểm tra ở cả lúc nhận và lúc tiến triển. UI chỉ giải thích “đúng thời điểm nào”; UI không tự chặn hoặc tự cộng progress.

## 9. Thời gian, world và death

### 9.1. Time authority

`world_time` là dữ liệu chung. Client khởi động bằng snapshot server, tính thời gian dự đoán bằng offset và rate trong snapshot (`realSecondsPerGameDay`), và resync định kỳ. Chỉ Cloud Code/server function được ghi snapshot mới.

Khi dịch vụ time không sẵn sàng:

- Game dùng snapshot đã xác nhận gần nhất để hiển thị.
- Không tự ghi giờ local lên World State.
- Quest chính không được trở nên bất khả thi; dùng alternative objective hoặc UI báo thời gian không chắc chắn.

### 9.2. Death

Permadeath là rủi ro PvE, không phải lỗi mạng. `death.isDead` phải được save authoritative trước khi Death Screen mở. Nếu save critical không xác nhận được, input combat bị khóa và UI chỉ cho retry/thoát; không cho tiếp tục chơi bằng state không chắc chắn.

Safe Zone chặn damage trước khi `IDamageable` nhận request; nó không hồi sinh hoặc xóa death đã xác nhận.

## 10. Balance targets

Các số chính thức ở `07`; bảng này là tiêu chí kiểm:

| Encounter tại recommended level | Target | Lý do |
|---|---|---|
| Enemy thường | 3–5 basic hit hoặc 1 Technique + 1–2 basic hit | Người chơi hiểu nhịp combat nhanh. |
| Elite | 12–20 giây, có ít nhất 1 punish window | Kiểm tra né/đặt vị trí. |
| Boss Floor 1 | 90–150 giây lần đầu, có checkpoint pattern chứ không checkpoint HP | Rủi ro cao nhưng không kéo dài quá mobile session. |
| Technique burst | Không tự hạ enemy cùng cấp từ full HP nếu không có setup | Tránh một nút giải quyết mọi tình huống. |

Mỗi thay đổi số phải được kiểm qua ít nhất: damage unit test, combat sandbox ở recommended level, và APK một thiết bị. PvP thêm test hai thiết bị.

## 11. Data và event boundary

| Loại | Chủ sở hữu | Ví dụ |
|---|---|---|
| Static data | ScriptableObject/definition | Weapon technique, enemy, floor, quest, boss. |
| Player Data | `ISaveService` | Level, inventory, equipment, mastery, quest state, death. |
| World State | `IWorldStateService` + server function | Floor/boss status, world time. |
| Runtime state | Scene memory | Current HP, active attack, current target, Safe Zone presence. |

Gameplay code chỉ phát intent/event có dữ liệu đủ dùng. Đây là domain event names và payload contract chuẩn; C# event member có thể theo convention `On<Name>`:

```text
AttackRequested
HitResolved
EnemyDefeated(enemyId)
ItemAcquired(itemId, quantity)                 // chỉ sau khi inventory add thành công
InventoryChanged(itemId, quantityDelta, currentQuantity) // mọi thay đổi inventory đã commit
NpcTalked(npcId)
ZoneEntered(zoneId)
QuestStateChanged(questId, state)
TechniqueMasteryChanged(discipline, value)
TimeChanged(gameTimeHours, gameDay, phase)      // presentation tick/correction
TimePhaseChanged(currentPhase)                  // chỉ khi qua ranh giới phase
WorldTimeResynced(snapshot)                     // ITimeService event sau resync được chấp nhận
PlayerDeathRecorded(deathRecord)               // chỉ sau death save được xác nhận
```

`PhaseChanged` và `PlayerDied` không phải domain event names chuẩn. `ItemAcquired` phục vụ objective Collect; `InventoryChanged` phục vụ consumer cần biết state inventory đã đổi. Safe Zone từ chối damage trước khi `IDamageable` nhận request. Death persistence dùng `RecordDeathAsync(DeathRecord)`; chỉ phát `PlayerDeathRecorded` sau khi ghi critical thành công.

Không truyền MonoBehaviour/UI object qua ranh giới Core. Dùng ID, DTO nhỏ hoặc domain event.

## 12. Tiêu chí hoàn thành một feature

Một feature gameplay chỉ hoàn thành khi có đủ:

1. Definition/data được mô tả và có validation cho ID, range, reference.
2. Pure logic test cho luật quan trọng.
3. Runtime integration không gọi SDK backend trực tiếp từ PvE/PvP.
4. UI cảm ứng cho mọi player intent; bàn phím Editor chỉ hỗ trợ test.
5. Save/load hoặc World State behavior được xác minh nếu feature có persistence.
6. Kịch bản APK thật ghi trong evidence table: thiết bị, mạng, account, kết quả, ngày test.

## 13. Không được làm

- Hard-code floor, technique, enemy hoặc quest ID trong Core.
- Trao quest reward trước khi turn-in.
- Dùng `async void`/fire-and-forget cho save critical hoặc nhiều save cạnh tranh mà không có queue/version policy.
- Để một MonoBehaviour vừa nhận input, tính damage, thay đổi inventory, gọi backend và cập nhật UI.
- Thêm Technique bằng một chuỗi `if weapon == ...` thay vì definition + executor dùng chung.
- Bỏ mobile input, test APK, hoặc evidence vì feature “đã chạy trong Editor”.

## 14. Thứ tự đọc cho AI

| Công việc | Bắt buộc đọc |
|---|---|
| Mọi gameplay mới | `01`, tài liệu này, `02`, `07`, `13` |
| Quest/NPC/mobile interaction | `02`, `03`, §8 và `13` Wave 1A |
| Floor/boss/world | `02`, `03`, §7, §9 và `13` Wave 1B/3 |
| Time | `03`, `08`, §9 và `13` Wave 2B |
| Technique/combat | `06`, `07`, §4–6 và `13` Wave 2A |
| Duel | `04`, `09`, §5 và §12 |
