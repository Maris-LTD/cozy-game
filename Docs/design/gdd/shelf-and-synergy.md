# Shelf & Synergy

> **Status**: In Review
> **Author**: user + game-designer (gdd-pipeline, đợt GDD 2)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 3 — Meaningful Constraints (phụ: Pillar 1 qua thú Plinko, Pillar 4 qua "không ai mất thú")
> **Quyết định nguồn**: Shelf D1–D6 và minorChoices trong `Docs/production/session-logs/decision-log.md` / `gdd-brief-2026-10-21-batch2.json`

## Summary

Shelf & Synergy là kệ gỗ một hàng 3–5 ô ở Tab 2: chỉ thú đứng trên kệ mới sinh thu nhập hay buff. Hệ này là nơi **duy nhất** thi hành hiệu ứng thú của Pet Catalog (Pet D1) và tính synergy: đăng ký nguồn Idle và modifier tầng Base/Synergy vào Income Pipeline của Economy Core với khóa theo ô `shelf.slot<i>`, gọi API modifier bàn của Plinko, và gửi thưởng sự kiện thú qua `SubmitIncome`. Combo nhóm đếm ngưỡng 2/3/4/5 thú cùng nhóm, buff nhẹ từng thú trên kênh của chính nó; Cáo buff ô kề, Mèo Hoàng Gia buff nhóm Hoàng Gia. Hai kênh (Idle / Plinko) cố ý cho hai đội hình tốt nhất khác nhau.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Economy Core, Pet Catalog, Inventory, Plinko Board, Bootstrap & Scene Flow (hard)`; `Star Progression, Save/Load (soft, VS)`

## Overview

Người chơi kéo thú từ dải Kho lên một trong các ô kệ, đổi chỗ, gỡ xuống — miễn phí, không hồi chiêu (D6). Mỗi lần kệ đổi, Shelf dựng lại toàn bộ "bản kế hoạch đăng ký" (nguồn, modifier, đóng góp bàn) từ trạng thái kệ và dữ liệu Catalog, rồi đẩy sang Economy Core và Plinko Board. Economy sở hữu công thức thu nhập và trần; Plinko sở hữu luật cộng dồn và trần vật lý; Catalog sở hữu độ lớn hiệu ứng. Shelf sở hữu: số ô và giá mở ô (D4), luật đặt thú (mỗi loài một ô, D2), bố cục một hàng và tính kề (D3), bảng combo nhóm (D1), khóa nguồn `shelf.*`, và cách so sánh trước/sau khi đổi kệ (D5: Δ CPS Idle chính xác, kênh Plinko chỉ nhãn buff).

Pillar 3 được kiểm bằng cấu trúc và bằng test: Mèo Hoàng Gia chỉ buff kênh Idle, Cáo chỉ buff kênh Plinko, thú chỉnh bàn không có nguồn để buff, nên một test EditMode duyệt hết các đội hình phải tìm ra hai đội hình tốt nhất khác nhau cho hai kênh.

## Detailed Design

### Core Rules

**S1. Ô kệ và bố cục (D3, D4)**
1. Kệ là **một hàng ngang**; ô đánh số 1…`slotCount` từ trái sang phải. `slotCount` bắt đầu ở `ShelfConfig.slotCountStart` (3), tối đa `ShelfConfig.slotCountMax` (5). Ô có số > `slotCount` là ô khóa (hiện như kệ trống có ổ khóa từ VS, ẩn ở MVP).
2. MVP giữ `slotCount` = 3 cố định; mở ô 4–5 qua Tiệm là Vertical Slice.
3. Mở ô: Shop gọi `TryUnlockNextSlot(playerAction)`. Shelf tính giá `slot_unlock_price` (Formulas), gọi `Economy.TrySpend(currency.coin, price, ShopPurchase, playerAction)`; thành công → `slotCount` + 1, phát `ShelfSlotUnlocked { newSlotCount }` và `ShelfChanged { reason = Unlock }`. Đã ở `slotCountMax` → trả `AtMax`, không trừ xu. Xu thiếu → trả `Rejected` (Economy phát `SpendRejected`), kệ không đổi.
4. Hai ô là **kề** khi chỉ số liền nhau (i, i ± 1) và **cả hai đều có thú**; ô trống cắt quan hệ kề (không nhảy qua ô trống). Ô đầu/cuối có tối đa một ô kề; `neighbor_slots` = 2 (hằng số của Pet Catalog) giữ đúng.

**S2. Đặt, gỡ, đổi chỗ (D2, D6)**
1. Chỉ đặt được thú `Inventory.IsOwned(petId)` = đúng và `PetCatalog.TryGetPet` tìm thấy. Mỗi **loài** đứng tối đa **một** ô (D2); `copies` không ảnh hưởng (bản trùng là Sao).
2. Kéo thú từ Kho vào ô trống → đặt. Vào ô có thú → thay; thú cũ về Kho. Kéo thú đã ở ô A sang ô B → đổi chỗ (B trống thì chỉ di chuyển). Kéo thú ra khỏi kệ (thả trên dải Kho hoặc ngoài kệ) → gỡ. Thả vào ô khóa hoặc ngoài vùng hợp lệ → hủy, kệ giữ nguyên.
3. Thao tác thay thế cho chạm: chạm chọn thú (Kho hoặc ô) rồi chạm ô đích; chạm lại thú đang chọn hoặc chạm ngoài → bỏ chọn.
4. Mọi thao tác miễn phí, không giới hạn, không hồi chiêu (D6). Giá không phụ thuộc đội hình (Economy D2) nên đổi tự do không tạo khai thác.
5. Thú loài mới nhận từ Gacha (`PetAcquired` với `copies` từ 0 lên 1) được **tự đặt** vào ô trống trái nhất nếu có, khi `ShelfConfig.autoPlaceNewSpecies` = đúng (Assumed, Open Questions). Không còn ô trống → chỉ vào Kho.
6. Preset đội hình (D6): từ VS, `ShelfConfig.presetCount` (2) bộ "Chủ động" / "Treo máy"; áp preset = một lần dựng lại kệ, bỏ qua thú không còn hợp lệ. MVP không có preset.

**S3. Nguồn thu và khóa (D2, minorChoices)**
1. Mỗi ô i có thú p có tối đa hai nguồn: `shelf.slot<i>.idle` khi `idle_base_units(p)` > 0 (cả thú vai Plinko có `plinkoRoleIdleShare`), và `shelf.slot<i>.plinko` khi p có `PegHitBonus` hoặc `JackpotBonus`.
2. Nguồn `.idle` đăng ký bằng `Economy.RegisterIncomeSource(shelf.slot<i>.idle, slot_idle_base_rate)`. Nguồn `.plinko` không cần đăng ký trước: nó là `sourceKey` của `SubmitIncome` và là đích modifier.
3. Modifier do Shelf đăng ký (bản ghi Economy R4.3 `{ sourceId, channel, tier, value, target }`, tên API → ADR):
   - `sourceId` = `shelf.slot<i>.fx` cho hiệu ứng thú ở ô i (`GroupLeader`, `NeighborBoost`, `FlatPerCoin`);
   - `sourceId` = `shelf.combo.<group>` cho combo nhóm.
   Tiền tố `shelf.` chỉ vào tầng Base/Synergy (Economy `tierOwnerPrefixes`); Shelf không bao giờ đăng ký Boost/Prestige/Global.
4. Với Plinko, `sourceId` = `shelf.slot<i>` cho `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`.
5. Hệ số Sao: đọc `IStarMultiplierSource.GetStarMultiplier(petId)` (Star Progression, VS); chưa có nguồn → 1.0. Giá trị làm sạch: không hữu hạn hoặc < 1 → 1.

**S4. Thi hành hiệu ứng (Pet Catalog R6 — Shelf là nơi duy nhất)**

| `type` | Shelf làm gì |
|---|---|
| `PegHitBonus` | Mỗi `PegHit` có `scored = true`: `SubmitIncome(Plinko, shelf.slot<i>.plinko, pet_event_bonus)` |
| `JackpotBonus` | Mỗi `JackpotHit`: bỏ qua nếu `origin = JackpotBurst` và `includeBurstCoins = false`; còn lại `SubmitIncome(Plinko, shelf.slot<i>.plinko, pet_event_bonus)` |
| `FlatPerCoin` | Modifier tầng Base cộng phẳng, kênh Plinko, đích `plinko.coin`, `value = sanitize(m)` (Economy kẹp `plinkoFlatPerCoinCap`) |
| `AutoDropRate` | `Plinko.RegisterAutoDropRate(shelf.slot<i>, sanitize(m))` |
| `BouncinessModifier` | `Plinko.RegisterBouncinessFactor(shelf.slot<i>, m)` (Plinko làm sạch và kẹp) |
| `GroupLeader` | Với mỗi thú **khác** q trên kệ có `group(q) = targetGroup`: modifier Synergy `+m`, đích nguồn kênh `scope` của q (Both → cả hai) nếu nguồn đó tồn tại |
| `NeighborBoost` | Với mỗi thú ở ô kề (S1.4): modifier Synergy `+m`, đích nguồn kênh `scope` của thú đó nếu nguồn tồn tại, mọi nhóm |

1. Buff không bao giờ nhắm chính thú phát buff, không nhắm kênh (`target = Channel` không dùng), không nhắm hiệu ứng chỉnh bàn hay `plinko.coin` (Pet R6.2, R6.4).
2. `PegHit` với `scored = false` và mọi sự kiện khi ô không còn thú bị bỏ qua. Shelf không nghe `CoinResolved` ở v1.0 (`FlatPerCoin` đi qua modifier của Economy).
3. `baseCoinValue` đọc từ Economy **tại thời điểm sự kiện**. Đường xử lý sự kiện không cấp phát bộ nhớ (bảng ô → hiệu ứng sự kiện dựng sẵn khi kệ đổi).
4. Trường `PetEffect` không dùng theo `type` bị bỏ qua; `magnitude` hỏng → `sanitize` (NaN/âm/vô cực → 0).

**S5. Combo nhóm (D1)**
1. `count_g` = số thú thuộc nhóm g đang trên kệ, **gồm** thú Hỗ trợ và thú chỉnh bàn (chúng mở ngưỡng).
2. `count_g` ≥ `ShelfConfig.groupComboMinCount` (2) → mỗi thú của nhóm g nhận `group_combo_bonus(g)` (bậc nhẹ +0.25/+0.5/+0.75/+1.0 cho 2/3/4/5 thú) tầng Synergy, **chỉ** trên nguồn kênh vai của nó: vai Idle → `.idle`; vai Plinko → `.plinko` nếu có; vai Hỗ trợ và thú Plinko không có `.plinko` (Kẹo, Hamster, Flan) không nhận.
3. Bậc nhẹ giữ tổng Synergy dưới trần: tối đa Idle 1 + 1.8 + 1.0 = 3.8, Plinko 1 + 1.2 + 1.0 = 3.2 < `synergyTierCap` 4.0 (validator `synergy_headroom`).
4. Khi bậc combo của một nhóm đổi, phát `ComboTierChanged { group, previousCount, newCount, bonus }` (cho Feedback).

**S6. Dựng lại khi kệ đổi (minorChoices)**
1. Chỉ dựng lại khi kệ đổi (đặt, gỡ, đổi chỗ, mở ô, preset, reset, nạp trạng thái, Sao đổi) — không theo khung.
2. Economy: gỡ mọi `sourceId`/`sourceKey` `shelf.*` đã đăng ký rồi đăng ký lại cả lô (N ≤ 5) trong cùng lời gọi; Economy tính lại theo cờ bẩn (R5.1).
3. Plinko: **so khác** — `Unregister` các `shelf.slot<i>` không còn đóng góp, `Register…` chỉ cho đóng góp mới hoặc đổi giá trị, để nhịp auto-drop không bị ngắt vô cớ.
4. Sau khi dựng lại: phát `ShelfChanged { reason, slotCount }` đúng một lần.

**S7. Xem trước khi đổi kệ (D5)**
1. Khi thú đang kéo/được chọn rơi vào một ô đích hợp lệ (hoặc vùng gỡ), Shelf dựng **bản kế hoạch giả định** bằng cùng bộ lập kế hoạch S3–S5 và hỏi Economy `PreviewIdleCps(plan)` (hàm thuần, *provisional* — Economy cần thêm, xem Dependencies); hiển thị `idle_delta_preview` chính xác.
2. Kênh Plinko chỉ hiện **nhãn định tính**: buff thú sẽ nhận/mất ("+120% Thỏ · Cáo", "Combo Động Vật 2: +25%"), hiệu ứng bàn ("Độ nảy ↑", "Tự thả xu"). Không bao giờ hiện số xu/s Plinko.
3. Xem trước tính lại chỉ khi ô đích đổi, không mỗi khung.

**S8. Reset và lưu**
1. `ResetForNewShop()` (Prestige gọi): `slotCount` về `slotCountStart`; giữ thú ở ô 1…`slotCountStart`, thú ở ô bị mất về Kho. Không ai mất thú (Pillar 4).
2. `ShelfState { unlockedSlots, slotPetIds[] }` (ID `pet.<slug>`, ô trống = rỗng; VS thêm `presets[]`). MVP không lưu; từ VS Save/Load gọi `GetState()` / `LoadState(state)`.

**S9. Khởi động**: bước `IBootStep` `Id = shelf.core`, `Phase = GameplayInit`, `RunsAfter = [economy.core, inventory.core, plinko.board]`, `IsCritical = true`. `Run()` nạp `ShelfConfig`, dựng kệ trống (MVP) hoặc từ `ShelfState` (VS), dựng lại đăng ký (S6), đăng ký sự kiện Plinko/Inventory, rồi `IsDone`.

### States and Transitions

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Scene nạp | Bootstrap gọi `Run()` | Không nhận API |
| Ready | `Run()` xong | `Interactive` → Running | API hoạt động, đăng ký đã đẩy sang Economy/Plinko; chưa có sự kiện Plinko vì bàn chưa chạy |
| Running | `Interactive`, hoặc `Resumed` | `Paused` → Ready; Prestige → Resetting | Nhận thao tác kệ, xử lý `PegHit`/`JackpotHit` |
| Resetting | `ResetForNewShop()` | Dựng lại xong (cùng khung) → Running | Thu kệ (S8.1), không nhận thao tác |

Trạng thái thao tác (lớp UI của Tab 2):

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Idle | Mặc định | Nhấn giữ + kéo vượt `dragStartDistanceDp` → Dragging; chạm nhả → Selected | Hiện kệ, Kho |
| Dragging | Kéo bắt đầu | Thả → Commit/Cancel; `TabChanged`, `Paused`, token chặn input → Cancel | Thẻ theo ngón; ô đích sáng; xem trước S7 khi ô đích đổi |
| Selected | Chạm chọn một thú | Chạm ô đích → Commit; chạm lại/ngoài → Idle | Thú chọn nổi lên; Δ CPS Idle hiện trên mọi ô hợp lệ (tính một lần khi chọn, ≤ `slotCountMax` lần xem trước) |
| Commit | Thả/chạm hợp lệ | Cùng khung → Idle | Áp S2, dựng lại S6, phát sự kiện |
| Cancel | Thả không hợp lệ hoặc bị ngắt | Cùng khung → Idle | Kệ không đổi; thẻ trượt về chỗ cũ |

### Interactions with Other Systems

| System | Shelf gọi / nghe | Shelf cung cấp | Owner |
|---|---|---|---|
| Economy Core | `RegisterIncomeSource`, đăng ký modifier Base/Synergy, `Unregister`, `SubmitIncome(Plinko, …)`, `TrySpend`, đọc `idleCpsUnit`, `baseCoinValue`, `derived_price`; `PreviewIdleCps`, `GetSourceIdleCps` (*provisional*) | Nguồn và giá trị modifier `shelf.*` | Economy: hợp đồng, tầng, trần; Shelf: giá trị |
| Pet Catalog | `TryGetPet`: nhóm, vai, `idle_base_units`, `effects` | — | Catalog: dữ liệu, ngữ nghĩa; Shelf: thi hành |
| Inventory | `IsOwned`, `GetOwnedSpecies`, `PetAcquired` | — | Inventory: sở hữu; Shelf: vị trí, luật một loài/kệ |
| Plinko Board | `RegisterAutoDropRate`, `RegisterBouncinessFactor`, `Unregister`; nghe `PegHit`, `JackpotHit` | — | Plinko: API, cộng dồn, trần |
| Bootstrap & Scene Flow | Bước `shelf.core`; `Interactive`/`Paused`/`Resumed`, `TabChanged`, cổng chặn input | Nội dung Tab `Shelf` | Bootstrap: vòng đời, tab, input |
| Feedback & Juice | — | `ShelfChanged`, `ComboTierChanged`, `ShelfSlotUnlocked`, `IShelfSlotAnchor.Get(sourceKey)` | Feedback: trình bày |
| HUD & Tab Navigation | `PetCardView`, `ModalHost` cho thẻ thú | Vùng nội dung Tab 2 | HUD: khung, modal |
| Shop (VS) | — | `TryUnlockNextSlot`, `GetSlotCount`, `GetMaxSlots`, giá ô kế | Shelf: giá và số ô (D4) |
| Prestige (Alpha) | — | `ResetForNewShop()` | Prestige: khi nào reset |
| Star Progression (VS) | `IStarMultiplierSource` | — | Star: hệ số Sao |
| Save/Load (VS) | — | `GetState` / `LoadState` | Save/Load: định dạng file |

## Formulas

Ký hiệu: `SC.` = `ShelfConfig`, `ST.` = `ShelfBalanceTargets`, `EC.` = `EconomyConfig`; p, q = thú; i, j = ô; g = nhóm; c = kênh. `sanitize(v)` = v nếu hữu hạn và ≥ 0, ngược lại 0 (đồng nhất Economy R4.5). `M_Synergy`, `idle_cps`, `derived_price` thuộc Economy; Shelf chỉ cấp đầu vào.

### slot_idle_base_rate

`slot_idle_base_rate(i) = idle_base_units(p_i) × idle_cps_unit × star_multiplier(p_i)` — `baseRate` của `shelf.slot<i>.idle`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| idle_base_units | float | 0–18 | calculated (Pet Catalog `idle_base_units`) | 0 → không có nguồn `.idle` |
| idle_cps_unit | double (xu/s) | 1–5 | SO field `EC.idleCpsUnit` (Economy sở hữu, 2.0) | đơn vị CPS |
| star_multiplier | double | ≥ 1 | calculated (`IStarMultiplierSource`, mặc định 1) | hệ số Sao, làm sạch theo S3.5 |

**Output range:** 0 đến 18 × 5 × trần Sao; roster v1.0 ở 1 Sao tối đa 13.2 xu/s (Công).
**Example:** Ếch Cốm 1.2 × 2.0 × 1 = **2.4 xu/s**; Mèo Chiêu Tài 0.25 × 2.0 = **0.5 xu/s**; Mèo Hoàng Gia 0 → không có nguồn.

### group_combo_bonus

`group_combo_bonus(g) = count_g ≥ SC.groupComboMinCount ? SC.groupComboBonuses[min(count_g, SC.slotCountMax) − SC.groupComboMinCount] : 0`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| count_g | int | 0–5 | calculated | số thú nhóm g trên kệ (S5.1) |
| groupComboMinCount | int | 2–3 | SO field `SC.groupComboMinCount` | ngưỡng đầu |
| groupComboBonuses | float[] | mỗi phần tử 0.1–0.75 (phần tử cuối tới 1.0), không giảm | SO field `SC.groupComboBonuses` | bậc cho count = min … max |

**Output range:** 0 đến 1.0 (mặc định); bảng ngắn hơn số bậc → validator lỗi, lúc chạy dùng phần tử cuối.
**Example:** kệ 3 ô Ếch + Mèo Chiêu Tài + Mèo Hoàng Gia → count_royal = 3 → **+0.5**; Cáo + Thỏ → count_animal = 2 → **+0.25**.

### slot_synergy_sum

`synergy_sum(s) = [s là nguồn kênh vai của q] × group_combo_bonus(group(q)) + Σ_{l ≠ q, GroupLeader, targetGroup(l) = group(q), scope(l) ∋ c} sanitize(m_l) + Σ_{n kề ô của q, NeighborBoost, scope(n) ∋ c} sanitize(m_n)`, với s = nguồn kênh c của thú q; Economy tính `M_Synergy(s) = min(1 + synergy_sum(s), EC.synergyTierCap)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| group_combo_bonus | float | 0–1.0 | calculated (trên) | chỉ cho nguồn kênh vai |
| m_l, m_n | float | `PetCatalog.effectLimits` | SO field `PetDefinition.effects[0].magnitude` | Mèo Hoàng Gia 1.8 (Idle), Cáo 1.2 (Plinko) |
| synergyTierCap | double | 2–8 | SO field `EC.synergyTierCap` (4.0, khóa registry) | trần do Economy kẹp |

**Output range:** 0 đến 2.8 (Idle) / 2.2 (Plinko) với roster v1.0 → `M_Synergy` 1.0–3.8, không chạm trần.
**Example:** bộ ba Hoàng Gia: Ếch `.idle` = 0.5 + 1.8 = 2.3 → M = **3.3** → 2.4 × 3.3 = 7.92 xu/s; Mèo Chiêu Tài `.idle` = 0 + 1.8 → 0.5 × 2.8 = 1.4 xu/s; Mèo Chiêu Tài `.plinko` = 0.5 → M = **1.5**. Tổng idle **9.32 xu/s**.

### pet_event_bonus (Pet Catalog sở hữu, Shelf thi hành)

`baseAmount = sanitize(m) × base_coin_value(n)` cho mỗi `PegHit` được tính (`PegHitBonus`) hoặc `JackpotHit` hợp lệ (`JackpotBonus`)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| m | float | 0.002–0.05 / 0.5–10 | SO field `PetDefinition.effects[0].magnitude` | bội số `baseCoinValue` |
| base_coin_value | double | ≥ 1 | calculated (Economy, tại thời điểm sự kiện) | |

**Output range:** 0 đến 10 × `base_coin_value` mỗi sự kiện; tần suất tối đa ≈ `PlinkoConfig.maxScoredPegHits` (20) × số xu/s.
**Example:** Thỏ, b = 18.679 → 0.017 × 18.679 = **0.3175** mỗi chạm; Economy nhân tiếp `M_Synergy(shelf.slot<i>.plinko)`.

### slot_unlock_price

`slot_unlock_price(k) = derived_price(SC.slotPriceCoefs[k − SC.slotCountStart − 1], n)` với k = ô sắp mở (`slotCount` + 1)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| slotPriceCoefs | double[2] | 2–20 mỗi phần tử, tăng dần | SO field `SC.slotPriceCoefs` | hệ số × giá quay (*provisional*, chốt bằng mô phỏng VS) |
| n | int | ≥ 0 | calculated (Economy `spinsThisShop`) | |

**Output range:** coef × `spin_price_raw(n)` làm tròn số đẹp; coef không hợp lệ → "không có giá", `TryUnlockNextSlot` trả `Rejected`.
**Example:** `spin_price_raw` = 120, ô 4 → coef 4 → **480**; ô 5 → coef 10 → **1 200**.

### idle_delta_preview

`idle_delta_preview = PreviewIdleCps(plan_after) − idle_cps_now`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| plan_after | struct | ≤ 5 nguồn, ≤ 25 modifier | calculated (bộ lập kế hoạch S3–S5 trên kệ giả định) | |
| idle_cps_now | double | ≥ 0 | calculated (Economy `idle_cps`) | gồm Boost/Prestige/Global hiện tại |

**Output range:** −idle_cps_now (gỡ hết) đến + vài nghìn; hiển thị bằng `display_format` của Economy, có dấu + / −.
**Example:** kệ [Ếch, Mèo Hoàng Gia, trống]: Ếch 2.4 × (1 + 1.8 + 0.25) = 7.32. Kéo Mèo Chiêu Tài vào ô 3 → 9.32 → **+2.0 xu/s**.

### synergy_headroom (validator Editor)

`worst_sum(c) = max(SC.groupComboBonuses) + Σ_{thú Hỗ trợ s trong roster, scope(s) ∋ c} sanitize(m_s)`; cảnh báo khi `1 + worst_sum(c) > EC.synergyTierCap`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| m_s | float | theo `effectLimits` | SO field `PetDefinition.effects[0].magnitude` | chặn trên: mọi thú Hỗ trợ cùng chạm một đích |

**Output range:** v1.0: Idle 1 + 1.0 + 1.8 = **3.8**, Plinko 1 + 1.0 + 1.2 = **3.2** → không cảnh báo. Bậc mạnh (+2.0) → Idle 4.8 và Plinko 4.2 → 2 cảnh báo (buff thừa bị Economy cắt, người chơi không hiểu vì sao).

### lineup_idle_score (chỉ test/mô phỏng)

`idle_score(L) = Σ_{nguồn .idle s của L} idle_base_units(p_s) × min(1 + synergy_sum(s), EC.synergyTierCap)` (đơn vị CPS, Sao = 1, không Boost)

**Output range:** 0 đến 18 × 3.8 × 5. **Example:** bộ ba Hoàng Gia = 1.2 × 3.3 + 0.25 × 2.8 = **4.66**; [Ếch, Mèo Hoàng Gia, Vịt] = 1.2 × 3.05 + 1.0 = **4.66** (hòa).

### lineup_plinko_score (chỉ test/mô phỏng)

`plinko_score(L) = 1 + Σ_{p ∈ L} uplift(p) × (p có nguồn .plinko ? min(1 + synergy_sum(p.plinko), EC.synergyTierCap) : 1)` — bội số thu nhập Plinko khi không có thú

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| uplift(p) | float | 0–0.55 | calculated (Pet Catalog `plinko_effect_uplift`, đọc giả định của `EconomyBalanceTargets`) | mô hình tuyến tính, bỏ qua tương tác Kẹo × Mèo Chiêu Tài |

**Output range:** 1.0 đến ≈ 4. **Example:** [Kẹo, Cáo, Thỏ] = 1 + 0.40 + 0.1242 × (1 + 1.2 + 0.25) = **1.7043**; [Kẹo, Thỏ, Mèo Chiêu Tài] = **1.6347**; [Thỏ, Cáo, Mèo Chiêu Tài] = **1.5474**.

### cross_channel_share (kiểm Pillar 3)

`idle_cross = max_{L_P ∈ argmax plinko_score} idle_score(L_P) / max idle_score`; `plinko_cross = max_{L_I ∈ argmax idle_score} (plinko_score(L_I) − 1) / (max plinko_score − 1)`. Đạt khi cả hai ≤ `ST.maxCrossChannelShare` **và** không phần tử argmax Idle nào có cùng tập loài với một phần tử argmax Plinko.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| maxCrossChannelShare | float | 0.3–0.8 | SO field `ST.maxCrossChannelShare` (0.5) | đội hình giỏi một kênh chỉ giữ tối đa tỷ lệ này ở kênh kia |
| L | ordered list | `lineup_count` phần tử | calculated | thú sở hữu = cả roster của bậc thử |

**Output range:** 0 đến 1; 1 = một đội hình tốt nhất cả hai kênh (vi phạm Pillar 3).
**Example (MVP, 7 thú, 3 ô):** argmax Idle = {Ếch, Mèo Hoàng Gia, Mèo Chiêu Tài} và {Ếch, Mèo Hoàng Gia, Vịt}; argmax Plinko = {Kẹo, Cáo, Thỏ}; idle_cross = 0.995 / 4.66 = **0.21**; plinko_cross = (0.1105 × 1.5) / 0.7043 = **0.24** → đạt.

### lineup_count

`lineup_count = N! / (N − k)!` (đội hình có thứ tự vì vị trí quan trọng qua Cáo), N = số thú của bậc thử, k = số ô

**Output range / Example:** MVP 7P3 = **210**; v1.0 13P5 = **154 440** — đủ nhỏ để duyệt hết trong EditMode.

## Edge Cases

- **If thả thú vào ô đang có chính nó**: không đổi, không dựng lại, không sự kiện.
- **If kéo thẻ Kho của loài đang trên kệ**: coi như kéo từ ô cũ → di chuyển/đổi chỗ; không bao giờ có hai ô cùng loài (D2).
- **If Mèo Hoàng Gia lên kệ không có thú Hoàng Gia khác**: 0 modifier `GroupLeader`; vẫn tính vào `count_royal`.
- **If Cáo kề thú không có nguồn kênh `scope`** (thú Idle, Kẹo, Hamster, Flan, thú Hỗ trợ): không modifier cho đích đó (Pet AC-14).
- **If ô giữa trống** ([Cáo, trống, Thỏ]): Cáo và Thỏ không kề (S1.4).
- **If cùng khung có `PegHit` và thao tác kệ**: sự kiện dùng bảng ô hiện hành lúc xử lý; dựng lại đồng bộ nên không có trạng thái nửa vời. Thú đã rời kệ không nhận thưởng; khoản đã `SubmitIncome` giữ nguyên (Economy Edge Case).
- **If `JackpotHit` có `origin = JackpotBurst`** và `includeBurstCoins = false`: không thưởng; cờ đúng → thưởng.
- **If `baseCoinValue` không hữu hạn hoặc ≤ 0**: `baseAmount` ≤ 0 → Economy bỏ qua khoản đó; Shelf không tự sửa.
- **If đang kéo khi `TabChanged`, `Paused` hoặc popup lấy token chặn input**: hủy kéo, kệ không đổi.
- **If `TryUnlockNextSlot` ở `slotCountMax`**: trả `AtMax`, không gọi `TrySpend`. Hai lời gọi cùng khung: lần hai tính giá ô kế tiếp và chỉ mở khi đủ xu.
- **If `LoadState` có ID không có trong Catalog build** (thú bản mới hơn): ô coi như trống cho gameplay và UI nhưng giữ ID trong `ShelfState` tới khi người chơi đặt thú khác vào đó (tương thích tiến, Pillar 4).
- **If `LoadState` có thú chưa sở hữu, loài trùng hai ô, `unlockedSlots` ngoài [`slotCountStart`, `slotCountMax`], hoặc `slotPetIds` dài hơn `unlockedSlots`**: bỏ thú chưa sở hữu; giữ ô trái nhất của loài trùng; kẹp `unlockedSlots`; thú thừa về Kho. Không ném lỗi.
- **If thú `deprecated`**: hoạt động đầy đủ trên kệ (Pet R9.3).
- **If `ResetForNewShop` khi đang kéo**: hủy kéo trước, rồi thu kệ.
- **If `groupComboBonuses` rỗng hoặc có NaN/âm**: validator lỗi; lúc chạy phần tử hỏng → 0 (không combo), không ném lỗi.
- **If Economy từ chối một đăng ký** (sai tầng do cấu hình): Shelf bỏ modifier đó, các modifier khác vẫn áp; validator của Economy báo lỗi.
- **If Plinko chưa `Interactive`**: đăng ký vẫn được nhận (Plinko R11.4.4); auto-drop chỉ chạy sau `Interactive`.
- **If `PreviewIdleCps` / `GetSourceIdleCps` chưa có** (Economy chưa cài): ẩn số Δ và nhãn CPS từng ô, chỉ hiện nhãn định tính — không chép công thức `idle_cps` vào Shelf.

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Economy Core | Shelf phụ thuộc Economy | Hard | Out (Shelf gọi): `RegisterIncomeSource(shelf.slot<i>.idle, baseRate)`, modifier Base/Synergy (`shelf.slot<i>.fx`, `shelf.combo.<group>`), `Unregister`, `SubmitIncome(Plinko, shelf.slot<i>.plinko, baseAmount)`, `TrySpend(currency.coin, price, ShopPurchase, playerAction)`, `PreviewIdleCps(plan)`, `GetSourceIdleCps(sourceKey)` (CPS Idle của một nguồn sau mọi tầng; thuần; *provisional, API mới*). In: `idleCpsUnit`, `baseCoinValue`, `derived_price`, `idle_cps`. Owner: Economy (hợp đồng, tầng, trần); Shelf (giá trị). |
| Pet Catalog | Shelf phụ thuộc Catalog | Hard | In: `TryGetPet` → nhóm, vai, `idle_base_units`, `effects`. Owner: Catalog (dữ liệu, ngữ nghĩa R6); Shelf (thi hành duy nhất, Pet D1). |
| Inventory | Shelf phụ thuộc Inventory | Hard | In: `IsOwned`, `GetOwnedSpecies` (truy vấn R4), `PetAcquired`, `CollectionProgress` (dải Kho Tab 2). Owner: Inventory (sở hữu); Shelf (vị trí, một loài/kệ — Inv D3). |
| Plinko Board | Shelf phụ thuộc Plinko | Hard | Out: `RegisterAutoDropRate(shelf.slot<i>, rate)`, `RegisterBouncinessFactor(shelf.slot<i>, factor)`, `Unregister(shelf.slot<i>)`. In: `PegHit { coinId, pegId, rowIndex, scored, worldPosition }`, `JackpotHit { coinId, origin, bucketIndex, worldPosition }`. `CoinResolved` không dùng ở v1.0. Owner: Plinko. |
| Bootstrap & Scene Flow | Shelf phụ thuộc Bootstrap | Hard | Bước `shelf.core` (`GameplayInit`, `RunsAfter` `economy.core`, `inventory.core`, `plinko.board`, cốt lõi); In: `Interactive`/`Paused`/`Resumed`, `TabChanged`, cổng chặn input. Owner: Bootstrap. |
| Star Progression (VS) | Shelf phụ thuộc Star | Soft | In: `IStarMultiplierSource.GetStarMultiplier(petId)`; Star phát thay đổi Sao → Shelf dựng lại. Owner: Star. |
| Save/Load (VS) | Save/Load phụ thuộc Shelf | Soft | Out: `GetState` / `LoadState(ShelfState)`. Owner: Shelf (nội dung), Save/Load (file). |
| HUD & Tab Navigation | Shelf phụ thuộc HUD (không ngược lại) | Soft | In: `PetCardView`, `ModalHost` (thẻ thú), khung Tab 2. HUD không đọc dữ liệu Shelf ở MVP (HUD D4). |
| Feedback & Juice | Feedback phụ thuộc Shelf | Soft (với Feedback) | Out: `IShelfSlotAnchor.Get(sourceKey)` (nhận mọi khóa bắt đầu bằng `shelf.slot<i>`), `ShelfChanged { reason, slotCount }`, `ComboTierChanged { group, previousCount, newCount, bonus }`, `ShelfSlotUnlocked { newSlotCount }`. Owner: Shelf (sự kiện, vị trí ô); Feedback (trình bày, cue). |
| Shop (VS) | Shop phụ thuộc Shelf | Hard (với Shop) | Out: `GetSlotCount`, `GetMaxSlots`, `GetNextSlotPrice`, `TryUnlockNextSlot(playerAction)` → `Unlocked` / `AtMax` / `Rejected`. Owner: Shelf (giá, số ô — D4). |
| Offline Earnings (VS) | Gián tiếp | Soft | Đọc `idle_cps` của Economy; không quy đổi auto-drop của thú (Economy Open Question). |
| Tutorial (VS) | Tutorial phụ thuộc Shelf | Soft | Out: `ShelfChanged`, `ShelfSlotUnlocked` để biết bước "bày thú", "mở ô 4" xong. |
| Prestige (Alpha) | Prestige phụ thuộc Shelf | Hard (với Prestige) | Out: `ResetForNewShop()`. |
| Neighbor Pranks (v1.1) | Pranks phụ thuộc Shelf | Hard (với Pranks) | Chưa định nghĩa: ô tạm giữ / thú "đi chơi" sẽ mở rộng `ShelfState` (Open Questions). |

**Hai chiều**: Economy, Pet Catalog, Plinko, Inventory, Bootstrap, Feedback, HUD đã liệt kê Shelf với interface khớp bảng trên, trừ các điểm **cần Sync** (không sửa ở đây): (1) Economy thêm hàm thuần `PreviewIdleCps(plan)` (D5) và `GetSourceIdleCps(sourceKey)` (nhãn CPS từng ô); (2) Bootstrap B3 / bảng phụ thuộc: `shelf.core` `RunsAfter` thêm `plinko.board`, bỏ *provisional*; (3) Plinko Dependencies: Shelf không tiêu thụ `CoinResolved` ở v1.0; (4) Pet Catalog: đóng câu hỏi khóa nguồn (`shelf.slot<i>.idle/.plinko`), R6 hết *provisional*; (5) Inventory: đóng câu hỏi chủ màn Kho (Shelf vẽ, Open Questions) và yêu cầu `RunsAfter`; (6) Feedback: `IShelfSlotAnchor` hết *provisional*, thêm `ComboTierChanged`; (7) systems-index hàng 9 và registry (`slotCountStart`, `slotCountMax`, `groupComboBonuses`, khóa nguồn).

## Tuning Knobs

Hai asset: `ShelfConfig` (runtime) và `ShelfBalanceTargets` (chỉ test/mô phỏng), cùng mẫu `PlinkoConfig`/`PlinkoBalanceTargets`. Không có số nào trong code; validator Editor cảnh báo ngoài safe range. Độ lớn hiệu ứng thú thuộc `PetDefinition`, trần thuộc Economy/Plinko, knob trình bày thuộc `FeedbackConfig` (FB D1) — không lặp ở đây. Mọi giá trị là **tạm** tới mô phỏng chung với Economy (2026-11-11).

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Số ô ban đầu | `ShelfConfig.slotCountStart` | int | 3 | 3–4 | Lựa chọn hết chật (Pillar 3) | Không đủ chỗ cho combo 2 + thú khác | `slotPriceCoefs`, Prestige |
| Số ô tối đa | `ShelfConfig.slotCountMax` | int | 5 | 4–5 | Ô quá nhỏ trên 9:16; Cáo ước lượng sai | Không có mục tiêu mở ô | `referenceGroupLeaderTargets` (Pet) |
| Ngưỡng combo đầu | `ShelfConfig.groupComboMinCount` | int | 2 | 2–3 | Combo hiếm khi có ở MVP 3 ô | — (< 2 vô nghĩa) | `groupComboBonuses` |
| Bậc combo (2/3/4/5 thú) | `ShelfConfig.groupComboBonuses` | float[] | [0.25, 0.5, 0.75, 1.0] | mỗi bậc 0.1–1.0, không giảm; `synergy_headroom` không cảnh báo | Chạm trần 4.0, một nhóm thống trị | Combo không đáng xếp | `synergyTierCap`, Mèo Hoàng Gia 1.8 |
| Hệ số giá ô 4, 5 | `ShelfConfig.slotPriceCoefs` | double[2] | [4, 10] (*provisional*) | 2–20, tăng dần | Ô 4 không mua nổi trong phiên | Mở ô quá dễ, hết mục tiêu | `spinGrowth` (Economy) |
| Tự đặt loài mới | `ShelfConfig.autoPlaceNewSpecies` | bool | true | — | — | Người mới không biết kéo thú | Tutorial (VS) |
| Số preset (VS) | `ShelfConfig.presetCount` | int | 2 | 1–3 | UI rối | Mất lối chuyển chế độ một chạm | D6 |
| Ngưỡng bắt đầu kéo | `ShelfConfig.dragStartDistanceDp` | float | 8 | 4–16 | Kéo cảm giác dính | Chạm nhầm thành kéo | tap-select |
| Đệm vùng thả ô | `ShelfConfig.slotDropPaddingDp` | float | 8 | 0–16 | Thả nhầm ô bên | Khó trúng ô trên mobile | `slotCountMax` |
| Số nhãn Plinko tối đa | `ShelfConfig.previewPlinkoLabelMax` | int | 3 | 2–5 | Bảng xem trước rối | Thiếu thông tin buff | D5 |
| Tỷ lệ chéo kênh tối đa | `ShelfBalanceTargets.maxCrossChannelShare` | float | 0.5 | 0.3–0.8 | Test chấp nhận đội hình "giỏi cả hai" | Test quá khắt khe, chặn nội dung hợp lý | `groupComboBonuses`, hiệu ứng thú |
| Ca kiểm Pillar 3 | `ShelfBalanceTargets.pillarTestCases` | list (bậc, số ô) | [(Mvp, 3), (V1_0, 5)] | mỗi bậc phát hành có ít nhất một ca | Test chậm | Bỏ sót bậc | `lineup_count` |
| Ngân sách CPU xem trước | `ShelfBalanceTargets.previewMaxMs` | float | 1.0 | 0.5–2 | Kéo khựng khi đổi ô đích | — | `PreviewIdleCps` |
| Tốc độ sự kiện test tải | `ShelfBalanceTargets.stressPegHitsPerSecond` | int | 180 | 100–400 | — | Không phủ trần bàn | `maxScoredPegHits` (Plinko) |

**Hằng số (không phải knob)**: một hàng ngang, kề = chỉ số ± 1 (D3; `neighbor_slots` = 2 của Pet Catalog); tối đa 2 nguồn mỗi thú (hai kênh); một loài/kệ (D2); cấp phát bộ nhớ trên đường xử lý sự kiện = 0 byte (ngân sách 60 FPS).

## Visual/Audio Requirements

Shelf chỉ phát sự kiện; Feedback & Juice ánh xạ sang hình và cue (Audio D1, FB D4).

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Đặt thú (`ShelfChanged` Place/Move/Swap) | Thẻ "đặt xuống" có nảy nhẹ, bụi gỗ nhỏ | "cộp" gỗ mềm | MVP |
| Gỡ thú | Thẻ bay về dải Kho | "sột" nhẹ | MVP |
| Thả không hợp lệ | Thẻ trượt về chỗ cũ | `Ui.Rejected` | MVP |
| Combo lên bậc (`ComboTierChanged` tăng) | Dải nơ màu nhóm nối các thú cùng nhóm, nhãn "+25%" | Hợp âm marimba tăng theo bậc | MVP |
| Combo xuống bậc | Dải nơ mờ đi, không nhấp nháy | Không | MVP |
| Thưởng thú kênh Plinko khi ở tab Kệ | Số bay gộp + punch ô (Feedback FB D2) | Theo Feedback | MVP |
| Mở ô (`ShelfSlotUnlocked`) | Ổ khóa rơi, ô mới sáng | Chuông nhỏ | VS |

## Game Feel

- **Feel reference**: kéo đổi chỗ thú trong cửa hàng của *Super Auto Pets* — nhặt, rê, thả là xong, không xác nhận. **Anti-reference**: trình sửa đội hình phải bấm "Lưu"/hộp thoại xác nhận mỗi lần đổi.
- **Input responsiveness**:

| Action | Max input-to-response (ms) | Frames at 60 fps | Notes |
|---|---|---|---|
| Nhặt thẻ khi vượt `dragStartDistanceDp` | 17 | 1 | Thẻ bám ngón từ khung kế |
| Hiện Δ CPS khi ô đích đổi | 50 | 3 | ≤ `previewMaxMs` CPU |
| Thả → nhãn CPS ô / Δ cập nhật | 17 | 1 | Phản hồi cùng khung (`GetSourceIdleCps` sau dựng lại) |
| Thả → CPS header cập nhật | ≤ `EC.ratesPublishInterval` (500) | 30 | `IncomeRatesChanged` bị Economy giới hạn nhịp |

- **Animation feel targets** (giá trị thuộc `FeedbackConfig`, đề xuất cho Feedback thêm): nhặt phóng 1.1× trong 80 ms; thả về ô 120 ms ease-out-back; trượt về khi hủy 150 ms.
- **Impact moments**: chỉ haptic nhẹ khi đặt (FB D5, tôn trọng `ReduceMotion`); không rung màn hình, không hit-stop — kệ là nơi "sắp đồ" yên tĩnh (cozy).
- **Profile**: nhẹ, tức thì, có thể làm lại vô hạn. Tiêu chí: AC-40.

## UI Requirements

Shelf sở hữu nội dung Tab 2 "Kệ & Kho" (quyết định của user, decision-log 2026-10-21): hàng ô kệ phía trên, dải Kho cuộn ngang phía dưới; HUD chỉ cấp khung tab và `PetCardView`. Dữ liệu Kho đọc từ Inventory: truy vấn R4 (`IsOwned`, `GetCopies`, `GetOwnedSpecies`), sự kiện `PetAcquired` (thêm thẻ mới) và `CollectionProgress` (tiến độ bộ sưu tập ở đầu dải Kho).

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Emoji, khung màu phẩm cấp, biểu tượng nhóm + vai | Mỗi ô kệ, mỗi thẻ Kho | Khi kệ đổi | Luôn (Pet UI Requirements) |
| CPS Idle của từng ô (sau Synergy) | Nhãn dưới ô | Đọc `GetSourceIdleCps(shelf.slot<i>.idle)` sau mỗi lần dựng lại (S6) | Ô có nguồn `.idle`; API chưa có → ẩn |
| Nhãn "không nhận combo" | Góc ô | Khi kệ đổi | Thú Hỗ trợ / chỉnh bàn trong nhóm đang có combo |
| Dải combo theo nhóm ("Hoàng Gia ×3 · +50%") | Trên hàng kệ | Khi `ComboTierChanged` | count ≥ ngưỡng |
| Δ CPS Idle xem trước (+/−) | Trên ô đích hoặc mọi ô (tap) | Khi ô đích đổi | Đang kéo / chọn |
| Nhãn buff kênh Plinko (≤ `previewPlinkoLabelMax`) | Cạnh Δ | Khi ô đích đổi | Đang kéo / chọn; không bao giờ có số xu/s |
| Ô khóa + giá mở | Ô > `slotCount` | Khi giá quay đổi | VS |
| Thẻ chi tiết thú | `ModalHost` + `PetCardView` (HUD) | Khi chạm giữ ô | Luôn |

UX flag: cần board Penpot cho Tab 2 (kệ 5 ô ở 9:16, dải Kho, trạng thái kéo/xem trước) trước epic. Công nghệ UI (UGUI / UI Toolkit) → ADR.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Pipeline, tầng, trần | `economy-core.md` | R4, `multiplier_synergy`, `idle_cps`, `derived_price`, `tierOwnerPrefixes`, `SubmitIncome` | Shelf đăng ký, Economy tính |
| Hiệu ứng, roster | `pet-catalog.md` | R6, R8, `idle_base_units`, `pet_event_bonus`, `plinko_effect_uplift`, AC-11…AC-15 | Shelf thi hành |
| Sự kiện, API bàn | `plinko-board.md` | R12 (`PegHit`, `JackpotHit`, `RegisterAutoDropRate`, `RegisterBouncinessFactor`) | Shelf tiêu thụ / gọi |
| Sở hữu thú | `inventory.md` | `IsOwned`, `GetOwnedSpecies`, `PetAcquired`, Inv D3 | Shelf đọc |
| Bước khởi động, tab | `bootstrap-and-scene-flow.md` | B2, B3, `TabChanged`, cổng input | Shelf đăng ký |
| Neo ô, số bay | `feedback-and-juice.md` | `IShelfSlotAnchor`, FB D2 | Feedback đọc |
| Thẻ thú, modal | `hud-and-tab-navigation.md` | `PetCardView`, `ModalHost` | Shelf dùng |
| Pillar 3 | `game-concept.md` | Design test "5 con hiếm nhất" | Kiểm bằng AC-36, AC-37 |

## Acceptance Criteria

Ký hiệu như Formulas; "kệ mặc định" = `ShelfConfig` ở giá trị hiện tại, Economy/Plinko giả có ghi lại mọi lời gọi, `EC.idleCpsUnit` = 2.0, mọi thú đã sở hữu.

- `AC-1 [EditMode]` **GIVEN** kệ mặc định mới, **WHEN** đọc `GetSlotCount` và thả thú vào ô 4, **THEN** `GetSlotCount` = `SC.slotCountStart` (3), thao tác trả Cancel và không có lời gọi Economy/Plinko nào.
- `AC-2 [EditMode]` **GIVEN** `slotCount` = 3, Xu đủ cho đúng một ô, **WHEN** gọi `TryUnlockNextSlot` hai lần, **THEN** lần 1 = `Unlocked` (`TrySpend` đúng 1 lần với `slot_unlock_price(4)`, `slotCount` = 4, một `ShelfSlotUnlocked`), lần 2 = `Rejected`, `slotCount` = 4; ở `slotCount` = `SC.slotCountMax` trả `AtMax` và 0 lần `TrySpend`.
- `AC-3 [EditMode]` **GIVEN** [Cáo, trống, Thỏ], **WHEN** dựng lại, **THEN** 0 modifier từ `shelf.slot1.fx`; đặt Vịt vào ô 2 → vẫn 0 modifier nhắm Thỏ từ Cáo (Cáo chỉ kề Vịt).
- `AC-4 [EditMode]` **GIVEN** `IsOwned(pet.lucky_cat)` = sai, **WHEN** `TryPlace(pet.lucky_cat, 1)`, **THEN** trả lỗi, kệ và đăng ký không đổi.
- `AC-5 [EditMode]` **GIVEN** Ếch ở ô 1, **WHEN** `TryPlace(pet.dreamy_frog, 3)`, **THEN** ô 1 trống, ô 3 = Ếch, không ô nào khác chứa Ếch, một `ShelfChanged`.
- `AC-6 [EditMode]` **GIVEN** [Ếch, Vịt, trống], **WHEN** lần lượt đặt Thỏ vào ô 2, đổi ô 1↔2, gỡ ô 1, **THEN** kệ lần lượt là [Ếch, Thỏ, trống] (Vịt về Kho), [Thỏ, Ếch, trống], [trống, Ếch, trống].
- `AC-7 [Play mode]` **GIVEN** Tab Kệ, **WHEN** chạm thẻ Vịt ở Kho rồi chạm ô 2, **THEN** ô 2 = Vịt; chạm thẻ rồi chạm lại chính nó → không đổi.
- `AC-8 [EditMode]` **GIVEN** kệ 3 ô có thú, **WHEN** 100 thao tác đặt/đổi/gỡ ngẫu nhiên, **THEN** 0 lời gọi `TrySpend`, số dư không đổi, không thao tác nào bị từ chối vì thời gian.
- `AC-9 [EditMode]` **GIVEN** kệ [Ếch, trống, trống], `autoPlaceNewSpecies` = đúng, **WHEN** `PetAcquired` cho Thỏ (copies 0 → 1) rồi cho Thỏ lần nữa (1 → 2), **THEN** ô 2 = Thỏ sau sự kiện đầu, kệ không đổi sau sự kiện hai; cờ = sai → kệ không đổi.
- `AC-10 [EditMode]` (VS) **GIVEN** preset "Treo máy" lưu [Ếch, Mèo Hoàng Gia, Vịt], **WHEN** áp preset khi Vịt không còn hợp lệ, **THEN** kệ = [Ếch, Mèo Hoàng Gia, trống] và đúng một `ShelfChanged { reason = Preset }`.
- `AC-11 [EditMode]` **GIVEN** kệ [Thỏ, Ếch, Mèo Hoàng Gia], **WHEN** dựng lại, **THEN** đúng 2 `RegisterIncomeSource`: (`shelf.slot1.idle`, 0.55) và (`shelf.slot2.idle`, 2.4) (`slot_idle_base_rate`); không nguồn nào cho ô 3.
- `AC-12 [EditMode]` **GIVEN** mọi đội hình 3 ô của roster MVP, **WHEN** dựng lại, **THEN** mọi `sourceId`/`sourceKey` gửi Economy bắt đầu bằng `shelf.`, mọi modifier ở tầng Base hoặc Synergy, mọi `target` là một nguồn (không `Channel`).
- `AC-13 [EditMode]` **GIVEN** Kẹo ở ô 3, **WHEN** dựng lại, **THEN** Plinko nhận `RegisterBouncinessFactor("shelf.slot3", 1.7)`.
- `AC-14 [EditMode]` **GIVEN** nguồn Sao giả trả lần lượt 1, 2, NaN, 0.5 cho Ếch, **WHEN** dựng lại, **THEN** `baseRate` của Ếch = 2.4, 4.8, 2.4, 2.4.
- `AC-15 [EditMode]` **GIVEN** Thỏ ở ô 1, `base_coin_value` b = 18.679, **WHEN** Plinko giả phát 20 `PegHit` cho một xu (14 `scored = true`, 6 `scored = false`), **THEN** đúng 14 lần `SubmitIncome(Plinko, shelf.slot1.plinko, 0.017 × b = 0.3175)`, 0 lần cho 6 `PegHit` không tính; gỡ Thỏ rồi phát thêm 1 `PegHit` scored → 0 lần gọi mới.
- `AC-16 [EditMode]` **GIVEN** Mèo Chiêu Tài ở ô 2, b = 18.679, **WHEN** Plinko giả phát 4 `JackpotHit` với `origin` lần lượt Manual, Auto, Rain, JackpotBurst, **THEN** đúng 3 lần `SubmitIncome(Plinko, shelf.slot2.plinko, 3.0 × b = 56.04)`, không lần nào cho `JackpotBurst`; đặt `includeBurstCoins` = đúng rồi phát lại 4 sự kiện → đúng 4 lần.
- `AC-17 [EditMode]` **GIVEN** Flan (`FlatPerCoin` 0.086) ở ô 2, **WHEN** dựng lại, **THEN** đúng 1 modifier `{ sourceId = shelf.slot2.fx, channel = Plinko, tier = Base (cộng phẳng), value = 0.086, target = plinko.coin }`.
- `AC-18 [EditMode]` **GIVEN** kệ trống, Plinko giả ghi lại lời gọi, **WHEN** đặt Kẹo ở ô 1 và Hamster ở ô 2 rồi gỡ cả hai, **THEN** Plinko nhận đúng 1 `RegisterBouncinessFactor("shelf.slot1", 1.7)` và đúng 1 `RegisterAutoDropRate("shelf.slot2", 0.27)` (không đăng ký lặp), sau khi gỡ có đúng 1 `Unregister("shelf.slot1")` và 1 `Unregister("shelf.slot2")`.
- `AC-19 [EditMode]` **GIVEN** kệ 5 ô [Cáo, Ếch, Mèo Hoàng Gia, Công, Mèo Chiêu Tài], **WHEN** dựng lại, **THEN** modifier `shelf.slot3.fx` (chỉ tính hiệu ứng `PetEffect`, không tính `shelf.combo.*`) có đúng 3 bản ghi Synergy +1.8 nhắm `shelf.slot2.idle`, `shelf.slot4.idle`, `shelf.slot5.idle`; `shelf.slot1.fx` (Cáo kề Ếch, Ếch không có `.plinko`) có 0 bản ghi; không bản ghi hiệu ứng nào nhắm slot1/slot3 hay nguồn `.plinko`.
- `AC-20 [EditMode]` **GIVEN** kệ [Thỏ, Cáo, Vịt], **WHEN** dựng lại, **THEN** `shelf.slot2.fx` có đúng 1 bản ghi Synergy +1.2 nhắm `shelf.slot1.plinko`, 0 bản ghi nhắm Vịt (không có `.plinko`); **WHEN** đổi chỗ thành [Cáo, Thỏ, Vịt], **THEN** `shelf.slot1.fx` vẫn đúng 1 bản ghi +1.2 nhắm `shelf.slot2.plinko`; ở cả hai bố cục không modifier nào ở tầng Boost, Prestige hay Global.
- `AC-21 [Perf]` **GIVEN** Thỏ và Mèo Chiêu Tài trên kệ, **WHEN** phát `ST.stressPegHitsPerSecond` `PegHit` (scored) và 10 `JackpotHit` mỗi giây trong 10 s, **THEN** GC Alloc của đường xử lý sự kiện Shelf = 0 byte mỗi khung (Profiler) và khung giữ ≤ 16.7 ms trên máy chuẩn WebGL.
- `AC-22 [EditMode]` **GIVEN** [Ếch, Mèo Chiêu Tài, Mèo Hoàng Gia], **WHEN** dựng lại, **THEN** modifier `shelf.combo.royal` có đúng 2 bản ghi +0.5: nhắm `shelf.slot1.idle` và `shelf.slot2.plinko`; không bản nào nhắm Mèo Hoàng Gia hay `shelf.slot2.idle`.
- `AC-23 [EditMode]` **GIVEN** [Ếch, trống, trống], **WHEN** đặt Mèo Chiêu Tài rồi Mèo Hoàng Gia rồi gỡ Ếch, **THEN** `ComboTierChanged` royal lần lượt (1→2, +0.25), (2→3, +0.5), (3→2, +0.25); đặt Vịt (animal 0→1) → không sự kiện.
- `AC-24 [EditMode]` **GIVEN** kệ 3 thú, **WHEN** chạy 600 khung không thao tác, **THEN** 0 lời gọi đăng ký/gỡ tới Economy và Plinko.
- `AC-25 [EditMode]` **GIVEN** Kẹo ở ô 1, **WHEN** đặt Hamster vào ô 2, **THEN** Plinko nhận đúng 1 `RegisterAutoDropRate("shelf.slot2", 0.27)` và 0 lời gọi cho `shelf.slot1`; đúng một `ShelfChanged`.
- `AC-26 [EditMode]` **GIVEN** kệ [Ếch, Mèo Hoàng Gia, trống] (`idle_cps` = 7.32), **WHEN** xem trước Mèo Chiêu Tài vào ô 3 rồi thả, **THEN** `idle_delta_preview` = +2.0 (sai số ≤ 1e-9) và bằng `idle_cps` sau thả − trước thả; xem trước không đổi số dư, không gọi `RegisterIncomeSource`.
- `AC-27 [Screenshot]` **GIVEN** 9:16, đang kéo Cáo tới ô cạnh Thỏ, **WHEN** chụp Game view, **THEN** thấy Δ CPS Idle có dấu và nhãn "+120% Thỏ"; không có số xu/s nào cho kênh Plinko; chữ không bị cắt.
- `AC-28 [EditMode]` **GIVEN** đang kéo qua cùng một ô đích 60 khung rồi sang ô khác, **WHEN** đếm `PreviewIdleCps`, **THEN** đúng 2 lần.
- `AC-29 [EditMode]` **GIVEN** `slotCount` = 5, kệ đầy, **WHEN** `ResetForNewShop()`, **THEN** `slotCount` = 3, ô 1–3 giữ thú cũ, 2 thú còn lại vẫn `IsOwned`, mọi đăng ký `shelf.slot4/5` đã gỡ ở Economy và Plinko.
- `AC-30 [EditMode]` **GIVEN** `IsOwned(pet.lucky_cat)` = sai, `pet.future_x` không có trong Catalog, `ShelfState { unlockedSlots = 9, slotPetIds = [pet.dreamy_frog, pet.dreamy_frog, pet.future_x, pet.lucky_cat, pet.woody_duck, pet.cozy_fox] }`, **WHEN** `LoadState`, **THEN** `slotCount` = 5, kệ = [Ếch, trống, (trống, giữ `pet.future_x`), trống, Vịt], không ngoại lệ; `GetState` trả lại `pet.future_x` ở ô 3.
- `AC-31 [EditMode]` **GIVEN** registry bước giả của Bootstrap, **WHEN** Shelf đăng ký, **THEN** đúng một bước `shelf.core`, `GameplayInit`, `RunsAfter` = {`economy.core`, `inventory.core`, `plinko.board`}, `IsCritical` = đúng.
- `AC-32 [EditMode]` **GIVEN** bảng mặc định, **WHEN** tính `group_combo_bonus` với count 0…5, **THEN** 0, 0, 0.25, 0.5, 0.75, 1.0; với `groupComboMinCount` = 3 → 0, 0, 0, 0.25, 0.5, 0.75; bảng có NaN → 0 cho bậc đó.
- `AC-33 [EditMode]` **GIVEN** Economy thật, **WHEN** kệ = bộ ba Hoàng Gia, **THEN** `idle_cps` = 9.32; **WHEN** kệ 5 ô = Ếch, Thiên Nga, Mèo Chiêu Tài, Công, Mèo Hoàng Gia, **THEN** `M_Synergy(.idle của Ếch)` = 3.8 (< `synergyTierCap`, không bị kẹp).
- `AC-34 [EditMode]` **GIVEN** `spin_price_raw(n)` giả = 120, **WHEN** `GetNextSlotPrice` ở `slotCount` 3 và 4, **THEN** bằng `derived_price(4, n)` và `derived_price(10, n)`; `slotPriceCoefs[0]` = 0 → `TryUnlockNextSlot` trả `Rejected`, 0 `TrySpend`.
- `AC-35 [EditMode]` **GIVEN** roster v1.0, **WHEN** validator chạy với bảng mặc định, **THEN** 0 cảnh báo `synergy_headroom`; với `groupComboBonuses` = [0.5, 1.0, 1.5, 2.0] → đúng 2 cảnh báo `synergy_headroom`: Idle (4.8 > 4.0) và Plinko (4.2 > 4.0) (chỉ đếm loại cảnh báo này; cảnh báo safe range bỏ qua).
- `AC-36 [EditMode]` (Pillar 3, MVP) **GIVEN** roster MVP 7 thú, 3 ô, **WHEN** duyệt cả 210 đội hình theo `lineup_idle_score` / `lineup_plinko_score`, **THEN** không đội hình nào thuộc cả hai argmax (theo tập loài), `idle_cross` ≤ `ST.maxCrossChannelShare` và `plinko_cross` ≤ `ST.maxCrossChannelShare` (hiện 0.21 / 0.24), và đội hình 3 thú phẩm cấp cao nhất không thuộc argmax nào.
- `AC-37 [EditMode, Slow]` (Pillar 3, v1.0) **GIVEN** mỗi ca trong `ST.pillarTestCases` còn lại (13 thú, 5 ô), **WHEN** duyệt cả 154 440 đội hình, **THEN** cùng ba điều kiện như AC-36, test in hai argmax và hai tỷ lệ, và chạy xong ≤ 30 s.
- `AC-38 [EditMode]` **GIVEN** bộ liệt kê đội hình, **WHEN** N = 7, k = 3 và N = 13, k = 5, **THEN** số đội hình = 210 và 154 440, không trùng, mỗi đội hình không lặp loài.
- `AC-39 [Screenshot]` **GIVEN** 9:16, kệ 3 ô [Ếch, Mèo Chiêu Tài, Mèo Hoàng Gia], **WHEN** chụp Tab Kệ, **THEN** thấy 3 ô với emoji, biểu tượng nhóm và vai, nhãn CPS mỗi ô có nguồn `.idle` bằng `GetSourceIdleCps` (Ếch 7.92, Mèo Chiêu Tài 1.4), dải "Hoàng Gia ×3 · +50%", nhãn "không nhận combo" trên Mèo Hoàng Gia, và dải Kho bên dưới.
- `AC-40 [Play mode]` **GIVEN** Tab Kệ trên WebGL ở 60 FPS, **WHEN** kéo thẻ qua 3 ô rồi thả, **THEN** thẻ bám ngón từ khung sau khi vượt `dragStartDistanceDp`, Δ hiện ≤ 3 khung sau khi đổi ô đích, nhãn CPS ô đổi trong khung thả, CPS header đổi ≤ `EC.ratesPublishInterval` sau thả, mỗi lần xem trước ≤ `ST.previewMaxMs`.
- `AC-41 [EditMode]` **GIVEN** đang kéo Vịt tới ô 2, **WHEN** `TabChanged`, `Paused` hoặc một token chặn input xuất hiện, **THEN** kéo bị hủy, kệ và đăng ký không đổi, không `ShelfChanged`.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| Assumed: `autoPlaceNewSpecies` = đúng — loài mới tự vào ô trống trái nhất (MVP chưa có Tutorial; người mới thấy CPS chạy ngay, làm lại được miễn phí). | game-designer | 2026-11-04 |
| Đã chốt (user, decision-log 2026-10-21): Shelf vẽ cả Tab 2 "Kệ & Kho" (kệ + dải Kho đọc Inventory) — đóng câu hỏi chủ màn Kho của Inventory; HUD chỉ cấp khung và `PetCardView`. | — | Đã đóng |
| Assumed: `sourceId` modifier = `shelf.slot<i>.fx` (hiệu ứng thú) và `shelf.combo.<group>` (combo); nguồn = `shelf.slot<i>.idle/.plinko`; Plinko = `shelf.slot<i>` (một gốc theo ô, D2). | game-designer | 2026-10-28 |
| Assumed: `shelf.core` cốt lõi, `RunsAfter` thêm `plinko.board` (để khôi phục `ShelfState` gọi API bàn ở VS). | game-designer + Bootstrap | 2026-10-28 |
| Assumed: Economy dựng lại cả lô, Plinko so khác (không ngắt nhịp auto-drop; khớp Pet AC-13 "đúng 1 lần"). | game-designer | 2026-10-28 |
| Assumed: Shelf không nghe `CoinResolved` ở v1.0 (không hiệu ứng nào cần). | game-designer | 2026-10-28 |
| Assumed: `slotPriceCoefs` = [4, 10] và `maxCrossChannelShare` = 0.5 là tạm, chốt bằng mô phỏng chung với Economy. | game-designer | 2026-11-11 |
| Economy cần thêm `PreviewIdleCps(plan)` (D5) và `GetSourceIdleCps(sourceKey)` thuần; trước khi có, UI ẩn số Δ và nhãn CPS ô (Edge Cases). | Economy Core owner | 2026-11-11 |
| Rủi ro: AC-37 (v1.0, 5 ô) chưa được tính tay; nếu một đội hình Hoàng Gia + Plinko trội cả hai kênh, chỉnh `groupComboBonuses` hoặc độ lớn thú, không nới `maxCrossChannelShare` quá 0.6. | game-designer | 2026-11-11 |
| Hoãn: ô tạm giữ / thú "đi chơi" của Neighbor Pranks mở rộng `ShelfState` ở GDD Neighbor Pranks (v1.1). | Neighbor Pranks GDD | v1.1 |
