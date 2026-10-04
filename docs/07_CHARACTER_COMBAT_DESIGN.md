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
EXPNext(level)   = round(100 × level ^ 1.5)
```

| Level | MaxHealth | Attack | Defense | EXP tới level sau |
|---:|---:|---:|---:|---:|
| 1 | 100 | 10 | 5 | 100 |
| 2 | 115 | 12 | 6 | 283 |
| 3 | 130 | 14 | 7 | 520 |
| 4 | 145 | 16 | 8 | 800 |
| 5 | 160 | 18 | 9 | 1,118 |
| 10 | 235 | 28 | 14 | 3,162 |

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

Mốc 1 dùng `defenseWeight = 1.0`. PvP RuleSet có thể dùng giá trị khác, nhưng không thay trực tiếp dữ liệu PvE. Không đổi `defenseWeight` đơn lẻ — điều chỉnh đường cong thuộc Task B (07 §5.1, §6, §9).

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

### 5.1. Mục tiêu phía người chơi (số tạm — Task B chốt bằng sandbox)

| Tình huống | Target tạm |
|---|---|
| Quái thường hạ player đứng im | 10–20 đòn |
| Elite hạ player | 5–10 đòn |
| Số quái mỗi level | 8–15 quái |

Đây là số khởi điểm cho Task B (Integrator); chốt bằng sandbox trước nghiệm thu Wave 2A và trước Phase 11.

Lưu ý: với EXPNext (§1) và EXP quái Floor 1 = 10 (§6), lên level 5 cần khoảng 170 quái, mâu thuẫn mục tiêu 8–15 quái/level. Task B phải chỉnh EXP quái hoặc EXPNext, không chỉ chỉnh defenseWeight.

## 6. Floor Scaling

```text
enemyPowerMultiplier(floor) = 1.15 ^ (floor - 1)
expMultiplier(floor)        = 1.20 ^ (floor - 1)
```

| Floor | Enemy power | EXP | Base enemy HP/ATK/EXP (30/5/10) |
|---:|---:|---:|---|
| 1 | 1.00 | 1.00 | 30 / 5 / 10 |
| 2 | 1.15 | 1.20 | 35 / 6 / 12 |
| 5 | 1.75 | 2.07 | 53 / 9 / 21 |
| 10 | 3.52 | 5.16 | 106 / 18 / 52 |

Definition owns `enemyPowerMultiplier`, `expMultiplier`, `itemTierId`. Core only applies values; it does not recognize floor numbers.

`1.15^n`/`1.20^n` chỉ là giá trị khởi điểm; số thật nằm trong `FloorDefinition`. Task B (Integrator) chốt đường cong bằng sandbox trước nghiệm thu Wave 2A và trước Phase 11.

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
| Unit curve | Chạy Floor 1–10 ở đầu và cuối dải recommended level; fail khi damage chạm clamp 1. |
| Unit curve | Số quái cần để lên mỗi level trong dải recommended level nằm trong 8–15. |
| Sandbox | TTK enemy/elite/boss ở recommended level. |
| Device PvE | Touch input, cooldown feedback, recovery đọc được. |
| Device PvP | Hai thiết bị thấy cùng HP/match result. |

Mỗi change balance ghi: version config, target tested, equipment, level/mastery, số lần thử và kết quả. Không dùng cảm giác chủ quan làm evidence duy nhất.

## 10. Không được làm

- Đặt multiplier/cooldown trực tiếp trong controller theo từng content ID.
- Cho mastery tăng từ miss, object hoặc farm một enemy vô hạn.
- Tạo Technique vừa dash xa, invulnerable, crowd control và burst cao trong một button.
- Dùng item hoặc technique để revive death PvE.
- Thay balance PvE vì muốn PvP dễ hơn; dùng PvP RuleSet thay thế.
