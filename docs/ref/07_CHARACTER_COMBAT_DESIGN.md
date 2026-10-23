# CHARACTER COMBAT DESIGN
## Game2D Balance and Weapon Technique Specification

> Nguồn số liệu và acceptance criteria cho level, equipment, mastery và combat. Quy tắc gameplay nằm ở `12_ORIGINAL_GAMEPLAY_BIBLE.md`.
> Số liệu dưới đây là balance Mốc 1. Mọi thay đổi phải cập nhật definition/config và test cùng lúc.

---

## 1. Character Level

```text
MaxHealth(level) = 100 + (level - 1) × 15
Attack(level)    = 10  + (level - 1) × 2
Defense(level)   = 5   + (level - 1) × 1
EXPNext(level)   = 100 + 20 × (level - 1)
```

| Level | MaxHealth | Attack | Defense | EXP tới level sau |
|---:|---:|---:|---:|---:|
| 1 | 100 | 10 | 5 | 100 |
| 2 | 115 | 12 | 6 | 120 |
| 3 | 130 | 14 | 7 | 140 |
| 4 | 145 | 16 | 8 | 160 |
| 5 | 160 | 18 | 9 | 180 |
| 10 | 235 | 28 | 14 | 280 |

EXP cần để lên level kế tăng đều 20 EXP mỗi level (chốt ở Task B2). EXP dư sau khi lên level được giữ lại cho level kế. Save cũ có EXP ≥ EXPNext theo công thức mới thì lên level tiếp bằng EXP dư khi nạp save — không mất tiến độ, không clamp EXP.

Khi lên level, `CurrentHealth` tăng đúng phần MaxHealth vừa tăng, không hồi đầy. Ví dụ 60/100 lên level 2 thành 75/115.

Recommended level theo Floor là hướng dẫn balance, không là điều kiện khóa. Điều kiện vào Floor lấy từ World State.

```text
Floor N recommended level = 5N - 4 đến 5N
```

## 2. Equipment Power

Equipment là nguồn build thứ ba, sau Level và Mastery.

| Loại | Stat chính | Giới hạn Mốc 1 |
|---|---|---|
| Weapon | Attack, một effect đơn giản | Một weapon equipped. |
| Armor | MaxHealth hoặc Defense | Một armor equipped. |
| Consumable | Heal hoặc utility ngoài combat | Stack, không tạo revive. |
| Material | Craft/quest | Stack, definition quyết định có consume khi turn-in. |

Item instance dùng `instanceId`, `itemId`, `upgradeLevel`, `rolledStats`; không tạo ScriptableObject riêng cho mỗi item sở hữu.

Thiết kế stat roll Mốc 1:

```text
common:   100–110 percent base stat
uncommon: 115–130 percent base stat
rare:     140–165 percent base stat
epic:     175–210 percent base stat
boss bonus: definition fixed effect, không random roll
```

Không để một item roll làm TTK của enemy cùng cấp thay đổi quá 35 phần trăm. Nếu vượt ngưỡng, giảm effect hoặc chuyển power sang utility thay vì cộng Attack thô.

## 3. Weapon Mastery

Mastery là progress riêng cho từng Weapon Discipline, range 0–100. Nó mở Technique; không tự cộng Attack.

| Band | Mastery required | Nội dung mở |
|---|---:|---|
| Foundation | 0 | Basic attack và Core Technique |
| Traverse | 25 | Technique di chuyển/đặt vị trí |
| Control | 55 | Technique kiểm soát hoặc defensive |
| Signature | 85 | Technique mạnh, recovery dài |

Credit:

| Hành động hợp lệ | Credit |
|---|---:|
| Basic hit xác nhận | 1 |
| Technique hit xác nhận | 2 |
| Cùng enemy instance | Tối đa 4 total credit |
| Object/Safe Zone target/mục tiêu chết | 0 |

Player Data lưu `{ disciplineId: masteryValue }`. Technique availability được suy ra bằng `masteryValue >= masteryRequired`; không lưu mảng “đã mở”.

Mốc 1 chỉ build Blade Foundation và Blade Traverse. Các cột còn lại là model data chuẩn bị trước, không là cam kết content Mốc 1.

## 4. Weapon Technique Balance

### 4.1. Input slot và mục đích

| Slot | Combat purpose | Cooldown target | Recovery target |
|---|---|---:|---:|
| Core | Damage có chủ đích, ít thay đổi vị trí | 2–3s | 0.20–0.35s |
| Traverse | Reposition hoặc tiếp cận | 4–6s | 0.30–0.50s |
| Control | Tạo khoảng trống/defense có điều kiện | 6–9s | 0.45–0.70s |
| Signature | Commitment/burst mạnh | 10–14s | 0.80–1.20s |

Technique phải trả lời rõ ba câu hỏi trong definition:

1. Người chơi di chuyển như thế nào trong Wind-up/Active/Recovery?
2. Hit nào có thể trúng cùng một target, mỗi hit gây bao nhiêu multiplier?
3. Đối thủ có punish window gì khi dùng sai?

### 4.2. Damage multiplier

| Slot | Total multiplier PvE | Quy tắc |
|---|---:|---|
| Basic attack | 1.00 | Một active window. |
| Core | 1.15–1.35 | Không vừa dash xa vừa gây full burst. |
| Traverse | 1.00–1.25 | Giá trị chính là vị trí, không phải damage. |
| Control | 1.40–1.80 | Có condition/range/recovery rõ. |
| Signature | 2.20–2.80 | Recovery dài, cooldown dài, telegraph mạnh. |

Damage formula:

```text
rawDamage   = attackerAttack × abilityMultiplier
finalDamage = max(1, round(rawDamage - defenderDefense × defenseWeight))
```

PvE dùng `defenseWeight = 0.3` (CombatBalanceConfig, nạp qua PvEBootstrap.Awake). PvP RuleSet có thể dùng giá trị khác. Đường cong PvE đã chốt ở Task B2 (07 §5.1, §6, §9); không đổi `defenseWeight` đơn lẻ mà không chạy lại sandbox ở §9.

### 4.3. Ví dụ data shape

```text
technique.blade_traverse
  discipline: Blade
  masteryRequired: 25
  inputSlot: Traverse
  cooldownSeconds: 4.5
  windUpSeconds: 0.12
  activeWindow: 0.18 seconds, Step movement, multiplier 1.20
  recoverySeconds: 0.38
  hitPolicy: one hit per target per activation
```

Đây là ví dụ shape, không phải tên hoặc animation content cố định.

### 4.4. Dodge (ADR-025)

Dodge là hành động chung, không phải Technique và không cần mastery; dùng data `DodgeDefinition`:

```text
dodgeDurationSeconds: 0.18    // lướt ngắn
cooldownSeconds: 1.2
recoverySeconds: 0.20
không bất tử (no i-frames)
không xuyên tường/vật cản
```

PvP dùng profile riêng trong `PvPRuleSet` sau; không sửa definition PvE tại runtime.

## 5. Encounter Targets

| Encounter tại recommended level | Target time | Player learning objective |
|---|---:|---|
| Enemy thường | 3–5 basic hit | Di chuyển và commit attack. |
| Enemy ranged/skirmisher | 8–14s | Né và chọn góc tiếp cận. |
| Elite | 12–20s | Nhìn telegraph và dùng Traverse. |
| Boss Floor 1 | 90–150s | Nhận biết pattern và punish window. |

Không balance bằng HP lớn đơn thuần. Nếu boss quá lâu, giảm armor/health hoặc tạo thêm punish window trước khi tăng player multiplier.

### 5.1. Mục tiêu phía người chơi (đã chốt ở Task B2, kiểm bằng sandbox)

| Tình huống | Target |
|---|---|
| Quái thường hạ player đứng im | 10–20 đòn |
| Elite hạ player | 5–10 đòn (đo bằng `enemy.pressure_warden` Tầng 2, xem §9) |
| Số quái mỗi level | 8–15 quái |

Đã chốt ở Task B2 bằng `BalanceSandboxTests`: với EXPNext (§1) và hệ số tầng (§6), Slime và Bat thật ở Tầng 1–10, đầu và cuối dải recommended level (5N − 4 và 5N), `defenseWeight = 0.3`, đều nằm trong khoảng mục tiêu: 3–5 đòn thường để hạ quái (§5), 10–20 đòn để quái hạ player đứng im, 8–15 quái mỗi level, và damage không chạm clamp 1. Kết quả chi tiết ở §9.

Tầng 2 (Task P9-3) có thêm hai quái riêng, được `FloorTwoBalanceSandboxTests` đo ở Lv 6 và Lv 10 (đầu và cuối dải recommended level của floor.02): Guard `enemy.boiler_guard` phải nằm trong 4–7 đòn thường để hạ Guard, 10–20 đòn để Guard hạ người đứng im, 8–15 Guard mỗi level; Elite `enemy.pressure_warden` phải nằm trong 12–20 giây để hạ Elite (ước lượng = số đòn × 0.45s ÷ hiệu suất trúng 70%, cùng giả định với sandbox boss ở §5.2) và 5–10 đòn thường để Elite hạ người đứng im (Elite không bị ràng buộc "quái mỗi level"). Cả hai không được chạm clamp 1. Kết quả thật ở §9.

### 5.2. Boss Floor 1: ba pha, vùng đánh, field data và mô hình ước lượng (Task P8-4)

Chu kỳ boss theo `12` §7.2: **Observe → Punish Window → Pressure Window**, rồi lặp lại. Sau mỗi `pressureEveryNAttacks` đòn thường thì lượt đánh kế là Pressure; vào Pressure thì bộ đếm đòn thường về 0.

| Pha | Boss làm gì | Người chơi làm gì | Màu telegraph |
|---|---|---|---|
| Observe | Đuổi theo; vào tầm thì telegraph đòn thường `attackWindupSeconds` | Đọc pattern, đặt vị trí; lùi ra ngoài vùng đánh trước lúc đòn rơi thì đòn trượt | Vàng/cam |
| Punish Window | Khựng `punishWindowSeconds` sau đòn thường | Phản công | Xanh nhạt |
| Pressure Window | Telegraph `pressureWindupSeconds` (hướng lao khóa khi telegraph kết thúc) → lao `pressureDashDistance` trong `pressureDashSeconds` → recovery `pressureRecoverySeconds` (Punish Window dài) | Né khỏi hành lang lao, rồi phản công trong recovery | Tím đậm |

Quy tắc trúng đòn (`BossHitRules` và `BossDashHitGate`: thuần hình học, test ở `BossHitRulesTests`):

- Đòn thường kiểm vùng đánh TẠI THỜI ĐIỂM đánh: mục tiêu phải còn trong `attackHitRadius` quanh boss. Người chơi lùi ra hoặc né trong lúc telegraph thì trượt. Đòn trượt vẫn tính vào bộ đếm Pressure. `attackHitRadius` (1.8) lớn hơn `attackRange` (1.6) để người chơi đứng yên từ lúc bắt đầu telegraph vẫn dính đòn.
- Đòn lao: hành lang là đoạn từ điểm xuất phát tới vị trí hiện tại của boss, rộng `pressureHitRadius`. Gây `pressureDamageMultiplier` × damage đòn thường (đã trừ giáp, làm tròn như `DamageCalculator`, không dưới 1) đúng MỘT lần mỗi lần lao.
- Boss bắt đầu telegraph Pressure khi mục tiêu cách không quá `pressureDashDistance + pressureHitRadius` (tầm với của một cú lao); xa hơn thì tiếp tục đuổi.
- Mọi damage lên người chơi mang `sourceId = BossDefinition.Id` để permadeath ghi đúng lý do Boss (`09` §1.3).
- Enrage giữ nguyên (HP ≤ `enrageHpThreshold`: Attack ×1.2, cooldown ×0.7, tốc độ ×1.25) và KHÔNG được mô hình hóa ở sandbox.

Field data mới của `BossDefinition` (không magic number; asset cũ thiếu field vẫn mở được và dùng mặc định; `OnValidate` đặt lại về mặc định nếu giá trị ≤ 0):

| Field | Mặc định = giá trị `Boss_Floor01` | Ý nghĩa |
|---|---:|---|
| `attackHitRadius` | 1.8 | Bán kính vùng đánh thường quanh boss |
| `pressureEveryNAttacks` | 2 | Số đòn thường giữa hai lần Pressure |
| `pressureWindupSeconds` | 0.9 | Telegraph Pressure (giây) |
| `pressureDashDistance` | 4.0 | Quãng lao (đơn vị Unity) |
| `pressureDashSeconds` | 0.35 | Thời gian lao hết quãng |
| `pressureHitRadius` | 1.4 | Bán kính hành lang lao |
| `pressureDamageMultiplier` | 1.3 | Hệ số nhân lên damage đòn thường |
| `pressureRecoverySeconds` | 1.6 | Recovery sau lao = Punish Window dài |

Số boss trong `Boss_Floor01.asset` (chỉ chỉnh số boss; không đổi `CombatBalanceConfig`, công thức damage hay chỉ số người chơi): `maxHealth` 250 → **1500**, `attack` 18 → **28**, `defense` 4 → 4 (giữ). Các thời lượng giữ nguyên: `attackWindupSeconds` 0.8, `punishWindowSeconds` 1.2, `attackCooldownSeconds` 2.0, `enrageHpThreshold` 0.35. Đây là số khởi điểm tính tay; sandbox (§9) xác nhận nằm trong khoảng nên không chỉnh thêm. Khoảng cho phép khi chỉnh: `maxHealth` 1200–2000, `attack` 23–36. Với `attack` 28, Lv 5 hạ boss ≤ 150 giây tương ứng `maxHealth` ≲ 1570 (HP 1200 → 114.5s; HP 2000 → 190.8s, vượt) — nên số hiện tại còn dư khoảng 7 giây, không rộng.

HP boss lớn hơn nhiều so với quái thường, nhưng thời gian hạ boss do cửa sổ phản công quyết định chứ không chỉ do HP: mỗi chu kỳ 11.35 giây người chơi chỉ có 10 đòn (≈ 0.88 đòn/giây, trong khi cooldown cho phép ≈ 2.2 đòn/giây).

**Giả định của mô hình** (`BossBalanceModel`, thuần số học, đọc asset thật; mọi giả định dưới đây là hằng có tên hoặc suy ra từ definition):

- Người chơi chỉ đánh trong Punish Window (sau đòn thường) và Recovery (sau lao); trong Observe/Telegraph/lao thì tiếp cận hoặc né, không gây damage. Trong mỗi window: vung đầu tiên ngay khi window mở, rồi mỗi `cooldownSeconds` của `Attack_PlayerBasic` (0.45s) một lần.
- Chỉ `PlayerHitEfficiency` = 70% số đòn thực sự trúng boss (né, lệch hướng, thao tác cảm ứng).
- Mỗi chu kỳ mất `ApproachSecondsPerCycle` = 1.5s để tiếp cận/đổi vị trí, nhưng không ngắn hơn phần cooldown của boss còn lại sau Punish.
- Độ dài chu kỳ lấy từ definition: đòn thường = Observe + `attackWindupSeconds` + `punishWindowSeconds`; Pressure = Observe + `pressureWindupSeconds` + `pressureDashSeconds` + `pressureRecoverySeconds`; chu kỳ lặp = `pressureEveryNAttacks` đòn thường + 1 Pressure.
- Chỉ dùng đòn thường của người chơi, không chí mạng, không Technique; không mô hình người chơi chết, hồi máu hay Enrage. Dùng Technique hoặc chí mạng sẽ hạ boss nhanh hơn ước lượng.

**Sandbox chỉ là ước lượng để định số ban đầu. Bắt buộc thử lại trên điện thoại (Device PvE) và ghi thời gian hạ boss lần đầu bằng đồng hồ thật; số đo thật thắng sandbox.** Nếu trên điện thoại vượt 150 giây thì giảm `maxHealth` (khoảng 10% mỗi lần) trước khi đụng tới chỉ số người chơi.

## 6. Floor Scaling

```text
enemyPowerMultiplier(floor) = 1 + 0.6  × (floor - 1)
expMultiplier(floor)        = 1 + 0.75 × (floor - 1)
```

Hệ số tăng đều (tuyến tính), không tăng theo lũy thừa — chốt ở Task B2. Trong code là hai hằng `FloorScaling.EnemyPowerIncreasePerFloor = 0.6` và `FloorScaling.ExpIncreasePerFloor = 0.75`. Ví dụ: Tầng 2 = 1.6 / 1.75; Tầng 5 = 3.4 / 4.0; Tầng 10 = 6.4 / 7.75 (sức mạnh / EXP).

| Floor | Enemy power | EXP | HP / ATK / DEF / EXP thực tế |
|---:|---:|---:|---|
| 1 (Slime) | 1.00 | 1.00 | 50 / 11 / 0 / 12 |
| 1 (Bat) | 1.00 | 1.00 | 40 / 12 / 0 / 14 |
| 2 (Slime) | 1.60 | 1.75 | 80 / 18 / 0 / 21 |
| 2 (Bat) | 1.60 | 1.75 | 64 / 19 / 0 / 25 |
| 2 (Guard) | 1.60 | 1.75 | 112 / 21 / 6 / 25 |
| 2 (Elite) | 1.60 | 1.75 | 480 / 29 / 10 / 63 |
| 5 (Slime) | 3.40 | 4.00 | 170 / 37 / 0 / 48 |
| 5 (Bat) | 3.40 | 4.00 | 136 / 41 / 0 / 56 |
| 10 (Slime) | 6.40 | 7.75 | 320 / 70 / 0 / 93 |
| 10 (Bat) | 6.40 | 7.75 | 256 / 77 / 0 / 109 |

Số thực tế = số gốc trong EnemyDefinition × hệ số tầng (Defense cũng nhân hệ số; hiện cả hai loại quái có Defense gốc 0). `EnemyController` làm tròn HP, ATK, DEF và EXP về số nguyên bằng `Math.Round(..., MidpointRounding.AwayFromZero)` (ví dụ 11 × 1.6 = 17.6 → 18; 14 × 1.75 = 24.5 → 25). Làm tròn để tránh sai số số thực kiểu 50 × 1.6f = 80.0000012 khiến quái Tầng 2 sống dư một đòn.

Definition owns `enemyPowerMultiplier`, `expMultiplier`, `itemTierId`. Core only applies values; it does not recognize floor numbers. Hiện trạng code: khi chạy game `EnemyController.SetFloor` lấy hệ số từ hàm `FloorScaling` (qua `FloorSpawnRegistrar` → `EnemySpawner`); `scalingOverride` trong `FloorDefinition` để 0 nghĩa là dùng hàm, và `EnemyController` chưa đọc giá trị ghi đè ở đó (muốn ghi đè theo tầng bằng data thì phải nối thêm code và test).

Đường cong đã chốt ở Task B2 và được `BalanceSandboxTests` kiểm cho Tầng 1–10 (xem §9). Muốn đổi hệ số thì đổi hằng ở `FloorScaling`, cập nhật bảng này và chạy lại sandbox.

## 7. Item Availability

| Tier | Normal availability | Boss availability |
|---|---|---|
| Common | Floor 1–2 | Có thể rơi |
| Uncommon | Floor 3–4 | Có thể rơi |
| Rare | Floor 5–6 | Có thể rơi |
| Epic | Floor 7–10 | Có thể rơi |
| Boss bonus | Không random roll | `BossDefinition` chọn drop/effect cố định |

First clear chỉ cấp bonus riêng cho người thỏa điều kiện; main boss drop vẫn farm được từ rematch. Reward claim là Player Data; World State không lưu ownership item.

## 8. PvP Rule

- Duel Mốc 1 dùng build snapshot từ Player Data khi vào trận.
- Casual dùng stat gần với PvE; RuleSet là nơi điều chỉnh nếu thật sự cần.
- Kết thúc khi một fighter dưới 50 phần trăm MaxHealth. Không có `Death`, loot loss hoặc mastery credit.
- Stun, knockback, dash và cooldown có profile PvP riêng khi cần. PvP profile không sửa PvE definition tại runtime.

## 9. Balance Test Matrix

| Test | Phải chứng minh |
|---|---|
| Unit damage | Min damage, defense, multiplier, multi-hit total. |
| Unit mastery | Unlock threshold, per-enemy cap, save/load. |
| Unit curve | Chạy Floor 1–10 ở đầu và cuối dải recommended level (5N − 4 và 5N) với Slime/Bat thật (qua `EnemyController`); fail khi damage chạm clamp 1. |
| Unit curve | Số quái cần để lên mỗi level trong dải recommended level nằm trong 8–15. |
| Unit curve | 3–5 đòn thường để hạ quái; 10–20 đòn để quái hạ player đứng im (`defenseWeight` 0.3). |
| Unit rounding | `EnemyController` làm tròn HP/ATK/DEF/EXP về số nguyên (Slime Tầng 2 = 80 / 18; EXP Bat Tầng 2 = 25). |
| Unit curve (Tầng 2) | `FloorTwoBalanceSandboxTests` với `enemy.boiler_guard` và `enemy.pressure_warden` thật (tìm theo ID), Lv 6 và Lv 10, `defenseWeight` 0.3: Guard 4–7 đòn hạ Guard, 10–20 đòn hạ người, 8–15 Guard/level; Elite 12–20 giây hạ Elite (ước lượng, §5.1), 5–10 đòn hạ người; không chạm clamp 1; `attackWindupSeconds` Guard > Slime; mọi quái trong `spawnEnemyIds` của floor.02 có asset và nằm trong bộ test. |
| Asset | `FloorDefinition` floor.01/floor.02 khớp hàm `FloorScaling` và dải level 5N − 4 … 5N; scene `PvE_Gameplay` nạp `CombatBalanceConfig` có `defenseWeight` 0.3. |
| Sandbox | TTK enemy/elite/boss ở recommended level. |
| Unit boss | Luật trúng đòn của Boss (`BossHitRulesTests`): trong vùng đánh → trúng; lùi ra ngoài vùng trước lúc đánh → trượt; đứng ngoài hành lang lao → trượt, trong → trúng; mỗi lần lao chỉ một lần damage. |
| Sandbox boss | `BossBalanceSandboxTests` với `Boss_Floor01.asset`, `CombatBalanceConfig` (0.3), `CharacterStats` Lv 5/Lv 4 và `Attack_PlayerBasic` thật: hạ boss lần đầu Lv 5 ∈ [90,150]s, Lv 4 ≤ 190s; boss hạ người đứng yên Lv 5 trong 5–8 đòn thường, Lv 4 ≥ 4 đòn; damage không chạm clamp 1. Chỉ là ước lượng (§5.2). |
| Device PvE (boss) | Đo thời gian hạ boss lần đầu bằng đồng hồ trên điện thoại (mục tiêu 90–150s); lùi ra khỏi vùng đánh trong lúc telegraph thì không dính đòn; đọc được telegraph Pressure (màu tím) và né được hành lang lao. Chưa có số đo. |
| Device PvE | Touch input, cooldown feedback, recovery đọc được. |
| Device PvP | Hai thiết bị thấy cùng HP/match result. |

Mỗi change balance ghi: version config, target tested, equipment, level/mastery, số lần thử và kết quả. Không dùng cảm giác chủ quan làm evidence duy nhất.

Kết quả sandbox khi chốt Task B2 (Slime và Bat thật, Tầng 1–10 × hai đầu mút dải level = 20 ca mỗi loại quái, `defenseWeight` 0.3):

| Quái | Đòn thường hạ quái (3–5) | Đòn quái hạ player đứng im (10–20) | Quái mỗi level (8–15) |
|---|---:|---:|---:|
| Slime | 3–5 | 10–20 | 9–15 |
| Bat | 3–4 | 10–18 | 8–13 |

Một số ca nằm sát mép: Slime Tầng 1 Lv 5 = 20 đòn và 15 quái (đúng mức tối đa); Bat Tầng 1 Lv 1 = 8 quái (đúng mức tối thiểu). Đổi số gốc, EXP hay hệ số tầng thì phải chạy lại sandbox.

Kết quả sandbox Tầng 2 (Task P9-3, `FloorTwoBalanceSandboxTests`; số quái là số thực tế sau hệ số tầng 1.6 / 1.75 và làm tròn; `defenseWeight` 0.3; đòn thường người chơi hệ số 1.0, không chí mạng; Giây hạ quái = số đòn × 0.45s ÷ 70% hiệu suất trúng, chỉ là ước lượng đua sát thương, không tính thời gian tiếp cận):

| Quái | Lv người | Quái HP / ATK / DEF / EXP | Dame người → quái | Đòn hạ quái | Giây hạ quái (ước lượng) | Dame quái → người | Đòn hạ người đứng im | Quái mỗi level | Khoảng mục tiêu |
|---|---:|---|---:|---:|---:|---:|---:|---:|---|
| Guard `enemy.boiler_guard` | 6 | 112 / 21 / 6 / 25 | 18 | 7 | 4.5 | 18 | 10 | 8 | 4–7 đòn / 10–20 / 8–15 |
| Guard `enemy.boiler_guard` | 10 | 112 / 21 / 6 / 25 | 26 | 5 | 3.2 | 17 | 14 | 12 | 4–7 đòn / 10–20 / 8–15 |
| Elite `enemy.pressure_warden` | 6 | 480 / 29 / 10 / 63 | 17 | 29 | 18.6 | 26 | 7 | 4 (không ràng buộc) | 12–20 giây / 5–10 đòn |
| Elite `enemy.pressure_warden` | 10 | 480 / 29 / 10 / 63 | 25 | 20 | 12.9 | 25 | 10 | 5 (không ràng buộc) | 12–20 giây / 5–10 đòn |

Người chơi: Lv 6 = 175 HP / 20 ATK / 10 DEF (EXPNext 200); Lv 10 = 235 HP / 28 ATK / 14 DEF (EXPNext 280). Mọi số đo nằm trong khoảng mục tiêu, nhưng một số ca đúng mép: Guard Lv 6 = 7 đòn hạ Guard (tối đa 7), 10 đòn hạ người (tối thiểu 10) và 8 Guard mỗi level (tối thiểu 8); Elite Lv 10 = 10 đòn hạ người (tối đa 10) và hạ Elite trong ≈ 12.9s (sát tối thiểu 12s); Elite Lv 6 ≈ 18.6s (còn dư 1.4s so với tối đa 20s). Chỉnh chỉ số người chơi, `defenseWeight`, hệ số tầng hoặc số gốc của hai quái này sẽ dễ làm test đỏ (xem FB-020 ở `10`). `attackWindupSeconds`: Guard 0.7s, Elite 0.6s, Slime 0.35s (mặc định, asset Slime không ghi field này); telegraph dài hơn là điểm dạy chính của Tầng 2. Lịch sử chỉnh: máu gốc Elite ban đầu là 200 (hiệu lực 320) và test đo bằng 12–20 đòn; theo mô hình §5.2 con số đó chỉ ≈ 8.4–12.2s, thấp hơn mục tiêu 12–20s ở §5, nên đã tăng lên 300 (hiệu lực 480) và test chuyển sang đo bằng giây.

Kết quả sandbox Boss Floor 1 (Task P8-4, `BossBalanceSandboxTests`; `Boss_Floor01` HP 1500 / ATK 28 / DEF 4, `defenseWeight` 0.3, `Attack_PlayerBasic` ×1.0 mỗi 0.45s, hiệu suất trúng 70%, tiếp cận 1.5s/chu kỳ):

| Chỉ số | Lv 5 (chuẩn) | Lv 4 (cận) | Mục tiêu |
|---|---:|---:|---|
| MaxHealth người chơi | 160 | 145 | — |
| Đòn thường người → boss | 17 | 15 | không chạm clamp 1 |
| Đòn thường boss → người | 25 | 26 | không chạm clamp 1 |
| Đòn lao boss → người (×1.3) | 33 | 34 | không chạm clamp 1; không one-shot |
| Đòn thường hạ người đứng yên | 7 | 6 | Lv 5: 5–8; Lv 4: ≥ 4 |
| Đòn lao hạ người đứng yên | 5 | 5 | > 1 |
| Đòn người chơi mỗi Punish / mỗi Recovery | 3 / 4 | 3 / 4 | — |
| Chu kỳ (2 đòn thường + 1 Pressure) | 11.35s | 11.35s | — |
| Damage kỳ vọng mỗi chu kỳ | 119.0 | 105.0 | — |
| **Thời gian hạ boss lần đầu** | **143.1s** | **162.1s** | Lv 5: 90–150s; Lv 4: ≤ 190s |

Đây là ước lượng theo mô hình ở §5.2, chưa phải số đo thật. Phải đo lại trên điện thoại; đổi số boss, `Attack_PlayerBasic`, `defenseWeight` hay chỉ số người chơi thì phải chạy lại sandbox.

## 10. Không được làm

- Đặt multiplier/cooldown trực tiếp trong controller theo từng content ID.
- Cho mastery tăng từ miss, object hoặc farm một enemy vô hạn.
- Tạo Technique vừa dash xa, invulnerable, crowd control và burst cao trong một button.
- Dùng item hoặc technique để revive death PvE.
- Thay balance PvE vì muốn PvP dễ hơn; dùng PvP RuleSet thay thế.
