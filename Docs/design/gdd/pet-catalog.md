# Pet Catalog (Danh Mục Thú)

> **Status**: In Review
> **Author**: Maris-LTD + Claude (game-designer)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 3 — Meaningful Constraints (nhóm, vai, hai kênh thu nhập); hỗ trợ Pillar 2 — Gacha Thrill (phẩm cấp nâng trần)
> **Decisions**: D1–D6 của user (2026-10-21, `Docs/production/session-logs/decision-log.md`); sau design run 2: mỗi thú Hỗ trợ buff đúng một kênh (a); `idleCpsUnit` hằng số trong tiệm (Economy A)
> [CD-GDD-ALIGN] skipped (lean)
> game-designer / qa-lead không được gọi riêng (workflow không cho subagent): Formulas và Acceptance Criteria do game-designer viết trực tiếp, chưa qua review chuyên môn.
> **Revision 2 — 2026-10-21** (đợt 2, theo `Docs/production/session-logs/gdd-consistency-2026-10-21-run1.json` C1[1], C1[8], C1[12], C1[13], C1[15]): viết nốt mọi mục còn trống; `rarity_coef[common]` = 1 là hằng số, `PetRarityConfig.rarityCoefficients` chỉ giữ Hiếm / Sử Thi / Huyền Thoại; `groupRuleMinTier` = `V1_0`; `plinko_effect_uplift` đọc giả định đo của Plinko từ `EconomyBalanceTargets` (xóa hai knob `PetBalanceTargets` trùng); Shelf đăng ký auto-drop / độ nảy qua `RegisterAutoDropRate` / `RegisterBouncinessFactor` của Plinko; `JackpotBonus` lọc theo trường `origin` của `JackpotHit`; Catalog nạp bằng bước `pet.catalog` ở pha `DataLoad` của Bootstrap.
> **Revision 3 — 2026-10-21** (fix round 1 theo `Docs/design/gdd/reviews/pet-catalog-review-2026-10-21.md`): PC1-B1 — AC-14 ghi rõ ô kệ 1–5; PC1-B2 — mọi enum của Catalog có `None = 0` (giá trị hợp lệ bắt đầu từ 1), định nghĩa mặc định của `PetEffect` và luật trường bắt buộc theo loại hiệu ứng (R2, R6, R11, Edge Cases, AC-3, AC-8).
> **Revision 4 — 2026-10-21** (fix round 2 theo cùng báo cáo review): PC2-B1 — AC-14 / AC-15 chỉ đếm modifier có `sourceId` thuộc hiệu ứng `PetEffect` của thú Hỗ trợ, không tính modifier combo nhóm của Shelf (R4.2); vế "không modifier tầng Boost / Prestige / Global" giữ cho mọi modifier.
> **Revision 5 — 2026-10-21** (fix round 3 theo cùng báo cáo review Round 3 và `Docs/production/session-logs/gdd-consistency-2026-10-21-run2.json`; quyết định user sau design run 2 trong `decision-log.md`): PC3-B1 — quy tắc định dạng số trên thẻ thú (số < 1000 dùng `EconomyConfig.displaySigDigits` chữ số có nghĩa, làm tròn; ≥ 1000 theo `economy-core.md` R13), Economy Core sở hữu bộ định dạng, AC-39 nêu chuỗi kỳ vọng sau định dạng; đóng NEEDS-USER `scope` theo (a) — bỏ *provisional* ở R6.5, R8, bảng knob theo từng thú, `scope = Both` chỉ dành cho live-ops (knob `PetCatalog.scopeBothMinTier`, lỗi validator, AC-8); đóng phụ thuộc `idleCpsUnit` theo Economy (A) — `channel_parity` không còn nhãn NEEDS-USER, rủi ro theo dõi bằng mô phỏng; C2[11] — ngân sách Kẹo Bảy Màu ghi cùng ba con số với `plinko-board.md` (mục tiêu +32% ≈ 26, ước tính +40% ≈ 27.6, biên ≤ 30.7 × `baseCoinValue`/s); C2[12] — `bounce_uplift_at_cap` đọc `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`, xóa `PetBalanceTargets.bounceUpliftAtCapAssumed`.
> **Revision 6 — 2026-10-21** (theo `gdd-consistency-2026-10-21-run5.json` C5[11]): bỏ knob `PetBalanceTargets.assumedRarityRates`; `collection_spins_estimate` và validator đọc `GachaConfig.rarityRates` (`gacha-machine.md`).

## Summary

Pet Catalog là danh mục tĩnh, chỉ đọc, của 13 thú v1.0 (MVP dùng 7): mỗi thú có ID bất biến `pet.<slug>`, phẩm cấp, đúng một nhóm, một vai (Idle / Plinko / Hỗ trợ), trọng số và tối đa một hiệu ứng thuộc bộ từ vựng đóng. Sức mạnh mọi thú đi từ hệ số phẩm cấp × trọng số; Shelf & Synergy là nơi duy nhất thi hành hiệu ứng, Economy Core cấp đơn vị, Plinko cấp sự kiện. Validator trong Editor chặn dữ liệu sai và kiểm ngân sách sức mạnh để giữ Pillar 3 (không thú nào mạnh ở cả hai kênh).

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Bootstrap & Scene Flow (pha DataLoad)`; dữ liệu cho Inventory, Gacha Machine, Shelf & Synergy, Star Progression, Neighbor Pranks

## Overview

Pet Catalog là danh mục tĩnh, chỉ đọc, của mọi thú trong game: mỗi thú là một bản ghi dữ liệu có **ID bất biến** (`pet.<slug>`), tên, **phẩm cấp** (Thường / Hiếm / Sử Thi / Huyền Thoại), đúng một **nhóm** (Động Vật / Đồ Ngọt / Hoàng Gia), một **vai** (Idle / Plinko / Hỗ trợ), một **trọng số** riêng và tối đa một **hiệu ứng** viết bằng bộ từ vựng đóng (loại hiệu ứng + tham số số). Catalog không chạy logic: Shelf & Synergy đọc dữ liệu này để tính sản lượng và synergy, Economy Core xếp tầng cộng/nhân, Gacha bốc từ pool, Inventory và Star Progression khóa theo ID. Người chơi không thao tác với Catalog; họ gặp nó qua thẻ thú (tên, phẩm cấp, nhóm, vai, mô tả hiệu ứng có số thật) khi bóc trứng, trong Kho và trên Kệ.

Catalog là nơi Pillar 3 được đặt bằng cấu trúc: sức mạnh của mọi thú đi từ một bảng hệ số phẩm cấp nhân trọng số (độ hiếm nâng trần một cách bắc cầu), mỗi thú chỉ phục vụ một kênh thu nhập hoặc chỉ buff thú khác, và không có loại hiệu ứng nào là buff toàn cục — nên lỗi "Mèo Hoàng Gia x2 mọi thứ" và "Kẹo Bảy Màu nảy > 1.0" của FirstGDD không thể viết ra bằng dữ liệu. Thiếu Catalog, mỗi hệ sẽ tự giữ một bản dữ liệu thú, số liệu lệch nhau và không có chỗ nào kiểm được "không có đội hình tối ưu cho cả hai kênh".

Roster v1.0 có 13 thú (3 nhóm × [2 Thường + 1 Hiếm + 1 Sử Thi] + 1 Huyền Thoại); MVP dùng 7 thú FirstGDD đã gán lại nhóm (D5). Mọi giá trị là **tạm** cho tới mô phỏng bảng tính chung với Economy Core và Shelf & Synergy.

## Detailed Design

### Core Rules

Mọi tên trong `backtick` là knob ở `PetRarityConfig`, `PetCatalog`, `PetDefinition` (runtime) hoặc `PetBalanceTargets` (chỉ validator/test), xem Tuning Knobs. Không có con số nào nằm trong logic.

**R1. Phạm vi và ID (D2)**
1. Catalog chỉ chứa thú. Đồ hàng xóm v1.1 nằm ở một danh mục riêng với tiền tố `gadget.`; không có lớp "vật phẩm chung" ở v1.0.
2. ID là chuỗi bất biến dạng `pet.<slug>` (slug: chữ thường a–z, số, gạch dưới). ID là khóa mà Inventory và Save/Load lưu; tên hiển thị tách khỏi ID và đổi được tự do.
3. Một ID **không bao giờ** bị đổi, xóa hay tái sử dụng. Thú gỡ khỏi game chỉ được đánh dấu `deprecated`; ID của bản ghi đã xóa nhầm phải nằm trong `PetCatalog.retiredIds` để validator chặn tái sử dụng.

**R2. Bản ghi thú (`PetDefinition`)** — mỗi thú một ScriptableObject:

| Trường | Kiểu | Luật |
|---|---|---|
| `id` | string | R1 |
| `displayName` | string | Tên UI tiếng Anh (7 thú cũ đã duyệt, D5) |
| `nameVi` | string | Tên tiếng Việt |
| `nameLocKey`, `flavorLocKey` | string | Khóa bản địa hóa (cơ chế → ADR) |
| `flavorVi` | string | Một câu, ≤ `PetCatalog.flavorMaxChars` ký tự |
| `rarity` | enum | `None` (= 0, chưa đặt) / `common` / `rare` / `epic` / `legendary` (R3) |
| `group` | enum | `None` (= 0, chưa đặt) / `animal` / `sweet` / `royal` (R4) |
| `role` | enum | `None` (= 0, chưa đặt) / `Idle` / `Plinko` / `Support` (R5) |
| `weight` | float | Trong [`PetRarityConfig.weightMin`, `PetRarityConfig.weightMax`] (D4) |
| `effects` | PetEffect[] | R6; số phần tử theo vai |
| `releaseTier` | enum | `None` (= 0, chưa đặt) / `Mvp` / `VerticalSlice` / `V1_0` / `V1_1` / `LiveOps` (R9) |
| `inGachaPool` | bool | R9 |
| `deprecated` | bool | R1.3, R9 |
| `sprite` | Sprite | Emoji trong khung gỗ (Zero Art); màu phẩm cấp lấy theo `rarity` từ art bible, không đặt riêng từng thú |

**Giá trị "chưa đặt" của enum.** Unity serialize một trường enum mới bằng 0, nên mọi enum của Catalog — `rarity`, `group`, `role`, `releaseTier` ở đây, `PetEffect.type`, `.scope`, `.targetGroup` ở R6 — có thành viên `None = 0` và các giá trị hợp lệ bắt đầu từ 1. Nhờ đó validator phân biệt "chưa đặt" với một giá trị thật (`common`, `animal`, `Idle`, `Mvp`).
- Trường bắt buộc: `id`, `displayName`, `nameVi`, `sprite` (rỗng / `null` là thiếu) và `rarity`, `group`, `role`, `releaseTier` (`None` là thiếu). Thiếu → lỗi "thiếu trường bắt buộc" nêu tên trường (R11); các kiểm tra suy ra từ chính trường đó (định dạng ID khi `id` rỗng, số hiệu ứng theo vai khi `role = None`, loại hiệu ứng theo vai và `effectLimits` khi `PetEffect.type = None`) bị bỏ qua, nên mỗi trường thiếu cho đúng một lỗi.
- `None` không bao giờ là giá trị hợp lệ lúc chạy: Catalog không có nhóm, phẩm cấp, vai hay bậc phát hành `None`, và không thú nào thuộc chúng. Ba knob bậc phát hành của `PetCatalog` (`buildTier`, `balanceRosterTier`, `groupRuleMinTier`) dùng cùng enum `releaseTier`; `None` ở các knob đó cũng là lỗi.

**R3. Phẩm cấp và bảng hệ số (D4)**
1. Bốn phẩm cấp với ID ổn định `common`, `rare`, `epic`, `legendary`; tên UI và khóa màu ở `PetRarityConfig.rarities`.
2. Mỗi phẩm cấp có một hệ số: Thường = 1 là **hằng số** chuẩn hóa (đổi nó tương đương đổi `idleCpsUnit` của Economy Core); ba phẩm cấp còn lại là knob `PetRarityConfig.rarityCoefficients` = [Hiếm 2.5, Sử Thi 6, Huyền Thoại 15]. Hệ số tăng nghiêm ngặt theo phẩm cấp, bắt đầu từ trên 1 (cân bằng bắc cầu: độ hiếm nâng trần).
3. **Đơn vị sức mạnh** của một thú = hệ số phẩm cấp × trọng số (`pet_power_units`). Đây là "ngân sách" mà sản lượng Idle, hiệu ứng Plinko và buff Hỗ trợ của thú đó cùng được đo theo (R10).
4. Tỷ lệ rơi theo phẩm cấp và trọng số trong từng phẩm cấp thuộc Gacha Machine; Catalog không chứa (validator đọc `GachaConfig.rarityRates`).
5. Catalog không chứa đường Sao. Mọi giá trị trong Catalog là giá trị ở 1 Sao; hệ số Sao và trần thuộc Star Progression.

**R4. Nhóm**
1. Mỗi thú thuộc **đúng một** nhóm: `animal` (Động Vật), `sweet` (Đồ Ngọt), `royal` (Hoàng Gia). Tên UI và biểu tượng ở `PetCatalog.groups`.
2. Combo theo số thú cùng nhóm (ngưỡng, độ lớn) thuộc Shelf & Synergy; Catalog chỉ cấp thông tin nhóm. Riêng hiệu ứng `GroupLeader` của một thú cụ thể (Mèo Hoàng Gia) là dữ liệu Catalog (R6).

**R5. Vai (D3)**
1. `Idle`: có sản lượng CPS = đơn vị sức mạnh × `idleCpsUnit` của Economy Core (Shelf nhân thêm hệ số Sao). Không có hiệu ứng.
2. `Plinko`: phản ứng sự kiện Plinko hoặc chỉnh tham số bàn bằng đúng một hiệu ứng Plinko; sản lượng CPS gần như bằng 0: chỉ phần `PetRarityConfig.plinkoRoleIdleShare` của đơn vị sức mạnh.
3. `Support`: không có sản lượng riêng (tỷ phần Idle = 0, cố định theo D3); đúng một hiệu ứng Hỗ trợ, chỉ buff thú khác cùng nhóm hoặc ở ô kề. Chiếm một ô kệ là chi phí cơ hội thật.
4. Tỷ phần Idle theo vai là `role_idle_share`: Idle = 1 (định nghĩa), Plinko = `plinkoRoleIdleShare`, Support = 0 (định nghĩa).
5. Với mọi bậc phát hành t ≥ `PetCatalog.groupRuleMinTier` (= `V1_0`), roster bậc t (thú có `releaseTier` ≤ t, `deprecated = false`) phải có ở mỗi nhóm ≥ 1 thú Idle, ≥ 1 thú Plinko và ≥ `PetCatalog.minCommonsPerGroup` thú Thường, để nhóm nào cũng có đường vào cả hai kênh và thú Thường mở được combo (Pillar 3). Roster MVP và Vertical Slice được miễn: MVP cố ý dùng đúng 7 thú FirstGDD đã gán lại nhóm (D5) để kiểm vòng lặp trên kệ 3 ô với một combo nhóm/kề nhau (concept MVP), và trong 7 thú đó nhóm Đồ Ngọt chỉ có Kẹo Bảy Màu (Plinko), không có thú Thường nào; thêm thú vào MVP chỉ để thỏa luật này là phình phạm vi.

**R6. Hiệu ứng: bộ từ vựng đóng (D1)** — *provisional cho tới GDD Shelf & Synergy*

Mỗi `PetEffect` = `{ type, magnitude, scope, targetGroup, includeBurstCoins }`. Không có loại hiệu ứng nào khác; thêm loại mới là thay đổi code của Shelf và phải sửa GDD này.

Kiểu và mặc định của từng trường (một `PetEffect` mới tạo mang đúng các giá trị mặc định này):

| Trường | Kiểu | Mặc định | Dùng bởi | Luật |
|---|---|---|---|---|
| `type` | enum `None` / bảy loại ở bảng dưới | `None` | mọi hiệu ứng | Bắt buộc: `None` → lỗi "thiếu trường bắt buộc" |
| `magnitude` | float | 0 | mọi hiệu ứng | Trong `PetCatalog.effectLimits[type]` (R11) |
| `scope` | enum `None` / `Idle` / `Plinko` / `Both` | `None` | `GroupLeader`, `NeighborBoost` | Bắt buộc với hai loại này: `None` → lỗi "thiếu trường bắt buộc" |
| `targetGroup` | enum `None` / `animal` / `sweet` / `royal` (cùng enum với `group`, R2) | `None` | `GroupLeader` | Bắt buộc với loại này: `None` → lỗi "thiếu trường bắt buộc" |
| `includeBurstCoins` | bool | false | `JackpotBonus` | Không bắt buộc; false = không thưởng jackpot của xu Bùng Xu |

Trường mà `type` không dùng phải giữ mặc định ở bảng trên (`scope = None`, `targetGroup = None`, `includeBurstCoins = false`); khác mặc định → validator cảnh báo và Shelf bỏ qua trường đó.

| `type` | Vai | Kích hoạt | `magnitude` nghĩa là | Đi vào đâu (khớp `economy-core.md` R4.7 / `plinko-board.md`) |
|---|---|---|---|---|
| `PegHitBonus` | Plinko | Mỗi `PegHit` có `scored = true`, xu mọi origin | Thưởng = magnitude × `baseCoinValue` mỗi lần | `SubmitIncome(Plinko, nguồn của thú, thưởng)` → tầng Base gốc |
| `JackpotBonus` | Plinko | Mỗi `JackpotHit { coinId, origin, bucketIndex, worldPosition }`; khi `origin = JackpotBurst` chỉ tính nếu `includeBurstCoins = true` (lọc bằng trường `origin`, `plinko-board.md` R7, R12) | Thưởng = magnitude × `baseCoinValue` mỗi jackpot | `SubmitIncome(Plinko, …)` → tầng Base gốc |
| `FlatPerCoin` | Plinko | Liên tục khi trên kệ (áp cho mọi `CoinResolved`) | +magnitude × `baseCoinValue` mỗi xu | Modifier tầng Base cộng phẳng, đích `plinko.coin`, kẹp bởi `plinkoFlatPerCoinCap` của Economy |
| `AutoDropRate` | Plinko | Liên tục khi trên kệ | Tốc độ auto-drop của nguồn (1.0 = một xu mỗi `autoDropBaseInterval`) | Shelf gọi `RegisterAutoDropRate(sourceId, magnitude)` của Plinko khi thú lên kệ, `Unregister(sourceId)` khi rời; Plinko cộng mọi nguồn thành `auto_rate_multiplier` và kẹp sàn chu kỳ (`plinko-board.md` R12.4–R12.5) |
| `BouncinessModifier` | Plinko | Liên tục khi trên kệ | Hệ số nhân độ nảy (≥ 1) | Shelf gọi `RegisterBouncinessFactor(sourceId, magnitude)` / `Unregister(sourceId)`; Plinko nhân mọi nguồn thành `bounciness_modifier` và kẹp tại `bounciness_cap` |
| `GroupLeader` | Support | Liên tục | +magnitude (1.0 = +100%) cho **mỗi thú khác** thuộc `targetGroup` trên kệ, trên kênh `scope` | Modifier tầng Synergy, đích = nguồn kênh `scope` của từng thú đích |
| `NeighborBoost` | Support | Liên tục | +magnitude cho thú ở ô liền trái và liền phải, trên kênh `scope`, mọi nhóm | Modifier tầng Synergy, đích = nguồn kênh `scope` của từng thú kề |

Quy tắc chung (Shelf & Synergy thi hành, Catalog chỉ mô tả):
1. Thưởng sự kiện tính bằng `baseCoinValue` **tại thời điểm sự kiện** (`pet_event_bonus`), nên tự co giãn theo tiến độ (D4).
2. Buff Hỗ trợ chỉ nhân **thu nhập có nguồn là chính thú đích** (CPS của nó; thưởng `SubmitIncome` của nó). Nó không nhân hiệu ứng chỉnh bàn (`BouncinessModifier`, `AutoDropRate`) hay cộng phẳng mỗi xu (`FlatPerCoin`) — các hiệu ứng đó không có "nguồn" trong pipeline.
3. Để `scope` có nghĩa, mỗi thú trên kệ có tối đa hai nguồn tách theo kênh (đề xuất `shelf.slot<i>.idle` và `shelf.slot<i>.plinko`); buff `scope = Idle` chỉ đích nguồn `.idle`. Tên khóa do Shelf & Synergy chốt.
4. Hỗ trợ không bao giờ buff chính nó và không có buff toàn cục (D6): mọi đích là một thú cụ thể. Thú và Shelf không được vào tầng Boost / Prestige / Global (Economy R4.7).
5. Mỗi thú Hỗ trợ buff **đúng một kênh** (quyết định (a) của user, 2026-10-21, `decision-log.md`): Cáo Khăn Len `scope = Plinko`, Mèo Hoàng Gia `scope = Idle`. `scope = Both` giữ trong từ vựng nhưng **chỉ dành cho live-ops**: validator báo lỗi khi một hiệu ứng có `scope = Both` trên thú có `releaseTier` thấp hơn `PetCatalog.scopeBothMinTier` (= `LiveOps`), nên roster v1.0 (và mọi bậc tới `V1_1`) không thể dùng nó. Shelf vẫn phải thi hành `Both` (đích cả hai nguồn) để live-ops mở mà không đổi code.

**R7. Số hiệu ứng theo vai**: `Idle` 0 hiệu ứng; `Plinko` đúng 1 hiệu ứng thuộc nhóm Plinko; `Support` đúng 1 hiệu ứng thuộc nhóm Support. Trần chung `PetCatalog.maxEffectsPerPet` (1 ở v1.0).

**R8. Roster v1.0 (D5, D6)** — số trong ngoặc là knob `PetDefinition.weight` / `PetDefinition.effects[0].magnitude`; tên 6 thú mới là *Assumed*.

| # | ID | Tên UI / tên Việt | Nhóm | Phẩm cấp | Vai | Trọng số | Hiệu ứng | Phát hành |
|---|---|---|---|---|---|---|---|---|
| 1 | `pet.woody_duck` | Woody Duck / Vịt Gỗ 🐥 | animal | common | Idle | 1.0 | — | Mvp |
| 2 | `pet.sleepy_hamster` | Sleepy Hamster / Chuột Hamster Ngủ Gật 🐹 | animal | common | Plinko | 1.0 | `AutoDropRate` 0.27 (bánh xe tự thả xu) | V1_0 |
| 3 | `pet.bouncy_bunny` | Bouncy Bunny / Thỏ Nhào Lộn 🐰 | animal | rare | Plinko | 1.1 | `PegHitBonus` 0.017 | Mvp |
| 4 | `pet.cozy_fox` | Cozy Fox / Cáo Khăn Len 🦊 | animal | epic | Support | 1.0 | `NeighborBoost` 1.2, scope Plinko | Mvp |
| 5 | `pet.dango_trio` | Dango Trio / Bánh Dango Ba Viên 🍡 | sweet | common | Idle | 1.1 | — | V1_0 |
| 6 | `pet.wobbly_pudding` | Wobbly Pudding / Bánh Flan Rung Rinh 🍮 | sweet | common | Plinko | 1.0 | `FlatPerCoin` 0.086 | V1_0 |
| 7 | `pet.sprinkle_cupcake` | Sprinkle Cupcake / Bánh Cupcake Rắc Cốm 🧁 | sweet | rare | Idle | 1.0 | — | V1_0 |
| 8 | `pet.rainbow_candy` | Rainbow Candy / Kẹo Bảy Màu 🍬 | sweet | epic | Plinko | 1.2 | `BouncinessModifier` 1.7 (chạm `bounciness_cap`) | Mvp |
| 9 | `pet.dreamy_frog` | Dreamy Frog / Ếch Cốm 🐸 | royal | common | Idle | 1.2 | — | Mvp |
| 10 | `pet.velvet_swan` | Velvet Swan / Thiên Nga Nhung 🦢 | royal | common | Plinko | 1.1 | `PegHitBonus` 0.0068 | V1_0 |
| 11 | `pet.lucky_cat` | Lucky Cat / Mèo Chiêu Tài 🐱 | royal | rare | Plinko | 1.0 | `JackpotBonus` 3.0, `includeBurstCoins = false` | Mvp |
| 12 | `pet.proud_peacock` | Proud Peacock / Công Kiêu Hãnh 🦚 | royal | epic | Idle | 1.1 | — | V1_0 |
| 13 | `pet.chubby_king_cat` | Chubby King Cat / Mèo Hoàng Gia Béo Múp 👑 | royal | legendary | Support | 1.0 | `GroupLeader` 1.8, `targetGroup = royal`, scope Idle | Mvp |

Ghi chú cân lại so với FirstGDD (Quyết định 5 của concept):
- **Mèo Hoàng Gia** (D6): bỏ +35 CPS và "x2 mọi thứ"; thành thủ lĩnh nhóm Hoàng Gia, không sản lượng riêng, +180% cho mỗi thú Hoàng Gia khác trên kệ (kể cả Thường), không có buff toàn cục.
- **Kẹo Bảy Màu**: vai Plinko, CPS ≈ 0; hệ số nảy 1.7 đưa độ nảy hiệu lực tới đúng trần 0.85 (registry `effective_bounciness`), không bao giờ ≥ 1.0. Ngân sách kênh Plinko (cùng ba con số với `raw_rate_ceiling` của `plinko-board.md`, chạm liên tục không auto-drop, ≈ 19.7 × `baseCoinValue`/s khi không thú): **mục tiêu** +32% (`plinko_target_uplift` 0.324 → ≈ 26 × `baseCoinValue`/s), **ước tính hiện tại** +40% (`EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` 0.40, `budget_ratio` 1.235 → ≈ 27.6), **biên** ≤ 30.7 ở trần mục tiêu của Plinko (`PlinkoBalanceTargets.capEvMeanMax`, `.capJackpotProbMeanMax`; ≈ +55%, trần trên của safe range knob giả định). Plinko AC-13c đo giá trị thật và cập nhật knob giả định.
- **Thỏ Nhào Lộn**: bỏ "+1 xu mỗi lần nảy"; chỉ tính `PegHit` được tính (≤ `max_scored_peg_hits` = 20 mỗi xu), theo bội số `baseCoinValue`.
- **Mèo Chiêu Tài**: bỏ "+50 xu"; 3 × `baseCoinValue` mỗi jackpot, không tính jackpot của xu Bùng Xu.
- **Cáo Khăn Len**: từ "x2 CPS mọi thú Động Vật" thành buff ô kề (mọi nhóm) — để có ít nhất một cơ chế vị trí trên kệ và để Động Vật không trùng vai thủ lĩnh với Mèo Hoàng Gia.
- Mỗi nhóm có đúng 1 Thường Idle + 1 Thường Plinko, nên thú Thường mở combo nhóm ở cả hai kênh (Pillar 3).

**R9. Cờ phát hành và pool**
1. Bậc phát hành của bản build là `PetCatalog.buildTier`. Thú có `releaseTier` cao hơn bậc build không được đưa vào catalog của build đó (với hệ khác, ID của nó là ID lạ).
2. Gacha pool của một phẩm cấp = thú đã phát hành, `inGachaPool = true`, `deprecated = false`, cùng phẩm cấp.
3. Thú `deprecated` vẫn tra được bằng ID và vẫn hoạt động đầy đủ cho người đã sở hữu (không mất đồ), chỉ rời Gacha pool.

**R10. Ngân sách sức mạnh (kiểm bằng validator, không chạy lúc chơi)**
1. Mỗi thú phải có giá trị mô hình nằm trong `1 ± PetBalanceTargets.budgetTolerance` lần ngân sách của nó (`budget_ratio`): thú Idle đạt bằng định nghĩa; thú Plinko so `plinko_effect_uplift` với `plinko_target_uplift`; thú Hỗ trợ so `support_power_estimate` với `pet_power_units`.
2. Kiểm ngân sách dùng roster của bậc `PetCatalog.balanceRosterTier` (V1_0), không dùng bậc build, vì cân bằng được thiết kế cho roster đầy đủ.
3. Kênh Idle và kênh Plinko được neo ngang nhau tại mốc tiến độ `PetBalanceTargets.parityCheckpointSpins` (`channel_parity`).

**R11. Validator trong Editor** — chặn build khi lỗi, cảnh báo khi chỉ lệch khuyến nghị:
1. Lỗi: ID sai định dạng, trùng, hoặc nằm trong `retiredIds`; thiếu trường bắt buộc (R2: chuỗi rỗng, `sprite` `null`, enum bắt buộc = `None`; R6: `type = None`, `scope = None` ở `GroupLeader` / `NeighborBoost`, `targetGroup = None` ở `GroupLeader`); `scope = Both` trên thú có `releaseTier` < `PetCatalog.scopeBothMinTier` (R6.5); knob `buildTier` / `balanceRosterTier` / `groupRuleMinTier` = `None`; `weight` ngoài khoảng; số hiệu ứng sai theo vai (R7); `magnitude` ngoài khoảng `PetCatalog.effectLimits` của loại đó; hệ số phẩm cấp không tăng nghiêm ngặt; `deprecated = true` mà `inGachaPool = true`; nhóm có < `PetCatalog.minCommonsPerGroup` thú Thường hoặc thiếu vai Idle/Plinko (R5.5) ở bậc ≥ `groupRuleMinTier`; `GroupLeader` mà `targetGroup` (trong roster bậc `balanceRosterTier`) có ít hơn `PetBalanceTargets.referenceGroupLeaderTargets` thú khác không phải Hỗ trợ; tổng `FlatPerCoin` của cả roster > `plinkoFlatPerCoinCap`.
2. Cảnh báo: `budget_ratio` ngoài dung sai; `channel_parity` ngoài `PetBalanceTargets.parityBand`; `collection_spins_estimate` ngoài `PetBalanceTargets.collectionSpinsBand`; trường không dùng của hiệu ứng khác mặc định (R6: `scope` ≠ `None`, `targetGroup` ≠ `None` hoặc `includeBurstCoins` = true ở loại không dùng trường đó); hai thú trùng `displayName`; thiếu khóa bản địa hóa; `flavorVi` dài hơn `PetCatalog.flavorMaxChars`; một phẩm cấp có Gacha pool rỗng ở bậc `buildTier`.

**R12. Truy cập lúc chạy**
1. Catalog tự đăng ký một bước khởi động `IBootStep` theo hợp đồng pha của Bootstrap (`bootstrap-and-scene-flow.md` B2, B3): `Id = pet.catalog`, `Phase = DataLoad`, `RunsAfter` rỗng, `IsCritical = true`. `Run()` nạp `PetCatalog`, lọc theo `buildTier` (R9.1) và lập bảng tra theo ID; xong → `IsDone`. Thứ tự pha bảo đảm Catalog Ready trước mọi bước `GameplayInit` (Inventory, Gacha, Shelf). Nạp lỗi (asset thiếu, phần tử `null`, ID trùng lọt qua validator) → `IsFailed`, Bootstrap vào BootFailed với `stepId = pet.catalog`. Sau đó Catalog **chỉ đọc**, không có trạng thái, không đăng ký với Save/Load. Bootstrap không tham chiếu Catalog; Catalog phụ thuộc Bootstrap qua hợp đồng pha.
2. API hành vi: `TryGetPet(id)` (trả "không tìm thấy" cho ID lạ, không bao giờ ném lỗi), danh sách thú đã phát hành, Gacha pool theo phẩm cấp, thú theo nhóm, `pet_power_units` và `idle_base_units` của một thú. Không có API sửa.
3. Tra theo ID không cấp phát GC. Cơ chế nạp (tham chiếu trực tiếp, không cần Addressables) và bản địa hóa → ADR.

**Người chơi không thể**: có một thú mạnh ở cả hai kênh cùng lúc, có buff toàn cục từ thú, làm bàn nảy ≥ 1.0 hay nhận thưởng chạm đinh vô hạn, mất một thú vì nó bị gỡ khỏi game.

### States and Transitions

Catalog không có trạng thái gameplay. Có hai vòng đời: của bản ghi thú (lúc làm nội dung) và của Catalog (lúc chạy).

Vòng đời một bản ghi thú:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Draft | Tạo `PetDefinition` mới, chưa có trong `PetCatalog.pets` hoặc validator còn lỗi | Validator sạch lỗi và được thêm vào `pets` → Released (nếu `releaseTier` ≤ `buildTier`) hoặc Scheduled | Không có trong build |
| Scheduled | Hợp lệ nhưng `releaseTier` > `buildTier` | `buildTier` được nâng → Released | Bị loại khỏi build (R9.1) |
| Released | Hợp lệ, `releaseTier` ≤ `buildTier`, `deprecated = false` | Đặt `deprecated = true` → Deprecated | Tra được; vào Gacha pool nếu `inGachaPool` |
| Deprecated | `deprecated = true` | Không có (một chiều; ID giữ vĩnh viễn) | Tra được, hoạt động đầy đủ cho người sở hữu, không vào Gacha pool |

Không có chuyển trạng thái "Deleted": xóa asset của một ID đã phát hành là lỗi nội dung; ID phải vào `retiredIds` (R1.3).

Vòng đời Catalog lúc chạy:

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Unloaded | Scene `MainGame` nạp; bước `pet.catalog` đã đăng ký nhưng chưa tới lượt | Bootstrap gọi `Run()` ở pha `DataLoad` → Loading | Mọi truy vấn trả "chưa sẵn sàng"; không hệ nào được truy vấn ở trạng thái này (thứ tự pha do Bootstrap bảo đảm) |
| Loading | `Run()` được gọi | Nạp và lập bảng tra xong → Ready (`IsDone`); lỗi nạp → Failed (`IsFailed`) | Truy vấn vẫn trả "chưa sẵn sàng" |
| Ready | Nạp xong, đã lập bảng tra theo ID | Không có (tới khi thoát ứng dụng) | Chỉ đọc; trả cùng kết quả cho cùng truy vấn suốt phiên |
| Failed | Lỗi nạp, hoặc quá `BootConfig.stepTimeoutSeconds` (Bootstrap tính) | "Thử lại" nạp lại scene → Unloaded | Bootstrap ở BootFailed; không hệ gameplay nào khởi tạo |

### Interactions with Other Systems

| Hệ | Catalog nhận vào | Catalog cấp ra | Chủ interface |
|---|---|---|---|
| Inventory | — | `TryGetPet(id)`, danh sách thú đã phát hành (hiện bộ sưu tập, kiểm ID trong save) | Catalog sở hữu dữ liệu; Inventory sở hữu sở hữu/số bản trùng và cách giữ ID lạ |
| Gacha Machine | — | Gacha pool theo phẩm cấp (R9.2), phẩm cấp, sprite, khóa màu phẩm cấp cho hoạt cảnh mở trứng | Catalog (pool); Gacha (tỷ lệ, trọng số trong phẩm cấp, luật trùng) |
| Shelf & Synergy | — | Vai, nhóm, `pet_power_units`, `idle_base_units`, hiệu ứng (loại + tham số) | Catalog sở hữu dữ liệu và ngữ nghĩa từ vựng; Shelf là nơi **duy nhất** thi hành hiệu ứng và tính synergy (D1) |
| Economy Core | — (Catalog không gọi Economy) | Gián tiếp qua Shelf: `idle_base_units` × `idleCpsUnit`; thưởng sự kiện = magnitude × `baseCoinValue`; `FlatPerCoin` → tầng Base; Hỗ trợ → tầng Synergy | Economy sở hữu đơn vị, tầng, trần (khớp `economy-core.md` R4, R11) |
| Plinko Board | — (không phụ thuộc lúc chạy) | Ngữ nghĩa tham số khớp hợp đồng Plinko: `PegHit` (`scored`), `JackpotHit` (`origin`), `CoinResolved`, độ lớn mà Shelf truyền vào `RegisterAutoDropRate` / `RegisterBouncinessFactor` | Plinko sở hữu sự kiện, API modifier, luật cộng dồn và trần; Catalog sở hữu độ lớn hiệu ứng của thú |
| Star Progression (VS) | — | ID, phẩm cấp, giá trị 1 Sao | Star sở hữu hệ số Sao, số bản/cấp, trần |
| HUD / Feedback & Juice / màn Kho | — | Tên, nhóm, vai, phẩm cấp, sprite, mẫu mô tả hiệu ứng | Catalog (dữ liệu); UI (trình bày) |
| Bootstrap & Scene Flow | Lượt chạy bước `pet.catalog` ở pha `DataLoad` (`Run()`) | `IsDone` / `IsFailed` của bước | Bootstrap (hợp đồng `IBootStep`, thứ tự pha); Catalog (nội dung bước nạp) |
| Neighbor Pranks (v1.1) | — | ID, nhóm, phẩm cấp của thú (để chọn mục tiêu "dụ đi chơi") | Neighbor sở hữu luật |
| Settings & Accessibility (Alpha) | Ngôn ngữ đang chọn | Chuỗi đã bản địa hóa qua khóa | ADR bản địa hóa |

Validator (chỉ Editor) đọc thêm `EconomyConfig`, `EconomyBalanceTargets` và `PlinkoConfig` để chạy mô hình ngân sách (R10). Đây là phụ thuộc lúc làm nội dung, không phải lúc chạy.

→ Thành ADR: cách nạp Catalog (tham chiếu trực tiếp ScriptableObject, không Addressables — rủi ro thấp trên WebGL); bản địa hóa tên/flavor (Unity Localization hay bảng riêng); bảng tra ID không cấp phát. Không có engine reference cho domain scripting: feasibility chưa kiểm theo Unity 6000.3.16f1; chưa có ADR nào.

## Formulas

Ký hiệu: r = phẩm cấp, w = trọng số, m = `magnitude` của hiệu ứng, n = `spinsThisShop` (Economy). `sanitize(v)` = v nếu v hữu hạn và ≥ 0, ngược lại 0 (đồng nhất Economy R4.5 / Plinko R5.3). Các giá trị Plinko và Economy (EV hiệu dụng 2.96, p_jackpot 10.9%, Bùng Xu 5, `baseCoinValue`) lấy từ `plinko-board.md`, `economy-core.md` và registry; Catalog không sở hữu chúng. Công thức 1–3 dùng lúc chạy (Shelf tính); 4–9 là **mô hình cân bằng** cho validator và test, không ảnh hưởng gameplay.

### pet_power_units

`pet_power_units(p) = rarity_coef[r] × w`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| rarity_coef[common] | float | 1 | constant | mốc chuẩn hóa (R3.2): đổi nó tương đương đổi `idleCpsUnit` của Economy, nên cố định; không có trong `rarityCoefficients` |
| rarity_coef[rare / epic / legendary] | float | 2–3 / 4.5–8 / 10–20 | SO field `PetRarityConfig.rarityCoefficients[0..2]` | hệ số phẩm cấp, tăng nghiêm ngặt và > 1 (D4) |
| w | float | 0.8–1.2 | SO field `PetDefinition.weight` (biên ở `PetRarityConfig.weightMin/Max`) | trọng số riêng của thú |

**Output range:** 0.8 (Thường, w 0.8) đến 18 (Huyền Thoại, w 1.2) với mặc định; thú phẩm cấp cao hơn ở w thấp nhất vẫn có ít nhất 1.6 lần đơn vị của thú phẩm cấp liền dưới ở w cao nhất (Sử Thi 6 × 0.8 = 4.8 so với Hiếm 2.5 × 1.2 = 3.0; hai cặp kia ≈ 1.67), nên các phẩm cấp không chồng lấn.
**Example:** Công Kiêu Hãnh (Sử Thi, w 1.1) → 6 × 1.1 = **6.6**. Mèo Hoàng Gia (Huyền Thoại, w 1.0) → **15**.

### idle_base_units

`idle_base_units(p) = pet_power_units(p) × role_idle_share[role]`; Shelf đăng ký tốc độ gốc `base_rate = idle_base_units × idle_cps_unit × star_multiplier` (Economy `idle_cps`)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| role_idle_share[Idle] | float | 1 | constant | định nghĩa vai Idle (D3) |
| role_idle_share[Plinko] | float | 0–0.2 | SO field `PetRarityConfig.plinkoRoleIdleShare` | "gần như không có CPS" (D3) |
| role_idle_share[Support] | float | 0 | constant | Hỗ trợ không có sản lượng riêng (D3) |
| idle_cps_unit | double (xu/s) | 1–5 | SO field `EconomyConfig.idleCpsUnit` (Economy sở hữu) | đơn vị CPS gốc; hằng số trong một tiệm (Economy (A), R11.2) |
| star_multiplier | double | ≥ 1 | calculated (Star Progression sở hữu) | 1 khi chưa có Sao (MVP) |

**Output range:** 0 (Hỗ trợ) đến 18 đơn vị; trong roster v1.0 tối đa 6.6 (Công) = 13.2 xu/s ở 1 Sao với `idleCpsUnit` 2.
**Example:** Vịt Gỗ 1.0 × 1 = **1.0** → 2.0 xu/s. Thỏ Nhào Lộn 2.75 × 0.1 = **0.275** → 0.55 xu/s. Mèo Hoàng Gia 15 × 0 = **0**.

### pet_event_bonus

`pet_event_bonus = sanitize(m) × base_coin_value(n)` — `baseAmount` mà Shelf gửi qua `SubmitIncome(Plinko, …)` cho `PegHitBonus` (mỗi `PegHit` được tính) và `JackpotBonus` (mỗi `JackpotHit` hợp lệ)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| m | float | `PegHitBonus` 0.002–0.05; `JackpotBonus` 0.5–10 | SO field `PetDefinition.effects[i].magnitude` | bội số `baseCoinValue` mỗi sự kiện |
| base_coin_value(n) | double | ≥ 1 | calculated (Economy `base_coin_value`, tại thời điểm sự kiện) | giá trị xu gốc hiện tại |

**Output range:** 0 đến 10 × `base_coin_value`; Economy nhân tiếp các tầng Synergy/Boost/Prestige/Global.
**Example:** Mèo Chiêu Tài, n = 0 → 3.0 × 1 = **3.0**; n = 100 → 3.0 × 18.679 = **56.04**. Thỏ Nhào Lộn, n = 100 → 0.017 × 18.679 = **0.3175** mỗi lần chạm; một xu có 14 lần được tính → 4.45.

### plinko_target_uplift

`plinko_target_uplift(p) = pet_power_units(p) × plinko_uplift_per_unit × (1 − plinko_role_idle_share)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| plinko_uplift_per_unit | float | 0.03–0.08 | SO field `PetRarityConfig.plinkoUpliftPerUnit` | phần trăm thu nhập kênh Plinko (khi chạm thực tế, không thú) mà một đơn vị sức mạnh đáng giá |
| plinko_role_idle_share | float | 0–0.2 | SO field `PetRarityConfig.plinkoRoleIdleShare` | phần ngân sách đã dùng cho CPS |

**Output range:** 0.036 (Thường, w 0.8) đến 0.81 (Huyền Thoại, w 1.2); roster v1.0: 0.045 – 0.324.
**Example:** Thỏ Nhào Lộn 2.75 × 0.05 × 0.9 = **0.12375** (+12.4% kênh Plinko). Kẹo Bảy Màu 7.2 × 0.05 × 0.9 = **0.324**.

### plinko_effect_uplift

`coins_per_primary = 1 + p_jackpot_mean × jackpot_burst_count` (số xu được giải quyết trên mỗi xu sơ cấp — Manual/Auto/Rain — kể cả Bùng Xu)

| `type` | `uplift(m)` |
|---|---|
| `PegHitBonus` | m × avg_scored_hits × coins_per_primary / ev_eff |
| `JackpotBonus` | m × p_jackpot_mean × (includeBurstCoins ? coins_per_primary : 1) / ev_eff |
| `FlatPerCoin` | m × coins_per_primary / ev_eff |
| `AutoDropRate` | (m / auto_drop_base_interval) / realistic_tap_rate |
| `BouncinessModifier` | bounce_uplift_at_cap × clamp((min(m × base_peg_bounciness, bounciness_cap) − base_peg_bounciness) / (bounciness_cap − base_peg_bounciness), 0, 1) |

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| m | float | theo `PetCatalog.effectLimits` | SO field `PetDefinition.effects[i].magnitude` | độ lớn hiệu ứng |
| p_jackpot_mean | float | 0.08–0.15 | SO field `EconomyBalanceTargets.plinkoJackpotProbMeanAssumed` (Economy sở hữu, 0.109) | xác suất jackpot trung bình mỗi xu (Plinko `bucket_probability_and_ev`, cập nhật theo đo AC-30 của Plinko) |
| jackpot_burst_count | int | 3–8 | SO field `PlinkoConfig.jackpotBurstCount` (Plinko sở hữu) | số xu Bùng Xu |
| avg_scored_hits | float | 10–20 | SO field `EconomyBalanceTargets.plinkoScoredPegHitsPerCoinAssumed` (Economy sở hữu, 14) | số `PegHit` được tính trung bình mỗi xu (Plinko: 12–16; cập nhật theo AC-30 của Plinko) |
| ev_eff | float | 2.6–3.3 | SO field `EconomyBalanceTargets.plinkoEvEffectiveAssumed` (Economy sở hữu) | EV hiệu dụng mỗi xu sơ cấp, đơn vị `baseCoinValue` |
| auto_drop_base_interval | float (s) | 1–4 | SO field `PlinkoConfig.autoDropBaseInterval` (Plinko sở hữu) | chu kỳ ở 1 đơn vị tốc độ |
| realistic_tap_rate | float (chạm/s) | 2–5 | SO field `EconomyBalanceTargets.realisticTapRate` (Economy sở hữu) | nhịp chạm thực tế |
| base_peg_bounciness, bounciness_cap | float | 0.3–0.6, 0.7–0.9 | SO field `PlinkoConfig.basePegBounciness`, `.bouncinessCap` (Plinko sở hữu) | độ nảy gốc, trần |
| bounce_uplift_at_cap | float | 0.2–0.55 | SO field `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (Economy sở hữu, 0.40; chuyển từ `PetBalanceTargets`, C2[12]) | tăng EV hiệu dụng khi độ nảy ở trần; trần trên 0.554 suy từ mục tiêu Plinko (EV_mean ≤ 2.3, p_jackpot ≤ 20%); cập nhật theo đo Plinko AC-13c |

**Output range:** 0 đến ≈ 0.55 mỗi thú; mô hình tuyến tính, bỏ qua tương tác giữa các thú (ví dụ Kẹo tăng p_jackpot làm Mèo Chiêu Tài mạnh hơn — xem Edge Cases). `AutoDropRate` chỉ đúng khi tổng chu kỳ còn trên sàn `auto_drop_min_interval` của Plinko.
**Example:** coins_per_primary = 1 + 0.109 × 5 = 1.545. Thỏ: 0.017 × 14 × 1.545 / 2.96 = **0.1242**. Thiên Nga: 0.0068 × 21.63 / 2.96 = **0.0497**. Mèo Chiêu Tài: 3.0 × 0.109 / 2.96 = **0.1105**. Bánh Flan: 0.086 × 1.545 / 2.96 = **0.0449**. Hamster: 0.27 / 2.0 / 3.0 = **0.0450**. Kẹo: min(1.7 × 0.5, 0.85) = 0.85 → hệ số 1 → **0.40**.

### support_power_estimate

`NeighborBoost: estimate = m × neighbor_slots × reference_neighbor_units`
`GroupLeader: estimate = m × Σ_{K lớn nhất} scoped_units(t, scope)`, t chạy qua thú khác thuộc `targetGroup` trong roster bậc `balanceRosterTier`, vai ≠ Support
`scoped_units(t, Idle) = idle_base_units(t)`; `scoped_units(t, Plinko) = pet_power_units(t) × (1 − plinko_role_idle_share)` nếu t là thú Plinko có `PegHitBonus`/`JackpotBonus`, ngược lại 0; `scoped_units(t, Both)` = tổng hai giá trị

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| m | float | `GroupLeader` 0.2–2.5; `NeighborBoost` 0.2–1.5 | SO field `PetDefinition.effects[i].magnitude` | +1.0 = +100% tầng Synergy |
| neighbor_slots | int | 2 | constant | kệ là một hàng ngang nên mỗi ô có tối đa 2 ô kề; xét lại nếu Shelf đổi bố cục |
| reference_neighbor_units | float | 1–6 | SO field `PetBalanceTargets.referenceNeighborTargetUnits` | đơn vị (trong kênh `scope`) của một thú kề điển hình |
| K | int | 2–4 | SO field `PetBalanceTargets.referenceGroupLeaderTargets` | số thú cùng nhóm điển hình đặt cạnh thủ lĩnh trên kệ 5 ô |

**Output range:** 0 (không có đích phù hợp) đến ≈ 2.5 × 3 × 6.6; roster v1.0: 6.0 (Cáo) và 14.49 (Mèo Hoàng Gia). Lúc chạy, giá trị thật bị kẹp bởi `synergyTierCap` của Economy (4.0).
**Example:** Cáo, scope Plinko: 1.2 × 2 × 2.5 = **6.0**. Mèo Hoàng Gia, scope Idle: các thú Hoàng Gia khác có `scoped_units` Công 6.6, Ếch 1.2, Mèo Chiêu Tài 0.25, Thiên Nga 0.11 → 3 lớn nhất = 8.05 → 1.8 × 8.05 = **14.49**. (Nếu scope Both: Công 6.6, Mèo Chiêu Tài 2.5, Ếch 1.2 → 1.8 × 10.3 = 18.54.)

### budget_ratio

`budget_ratio(p) = estimate(p) / target(p)`; hợp lệ khi `|budget_ratio − 1| ≤ budget_tolerance`
`estimate, target`: Plinko → `plinko_effect_uplift` / `plinko_target_uplift`; Support → `support_power_estimate` / `pet_power_units`; Idle → 1 / 1 (đạt bằng định nghĩa)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| budget_tolerance | float | 0.1–0.4 | SO field `PetBalanceTargets.budgetTolerance` | dung sai ngân sách (validator cảnh báo khi vượt) |
| target(p) | float | > 0 | calculated | 0 hoặc không hữu hạn → validator báo lỗi thay vì chia |

**Output range:** mục tiêu [0.75, 1.25] với mặc định.
**Example:** Hamster 1.000; Thỏ 0.1242 / 0.12375 = 1.004; Bánh Flan 0.998; Thiên Nga 1.004; Mèo Chiêu Tài 0.1105 / 0.1125 = 0.982; Kẹo 0.40 / 0.324 = **1.235** (sát trần; mục tiêu +32% ≈ 26, ước tính +40% ≈ 27.6, biên ≤ 30.7 × `baseCoinValue`/s — khớp `plinko-board.md` `raw_rate_ceiling`, R8, Open Questions); Cáo 1.000; Mèo Hoàng Gia 14.49 / 15 = **0.966**.

### channel_parity

`channel_parity(n_ref) = plinko_uplift_per_unit × realistic_tap_rate × ev_eff × base_coin_value(n_ref) / idle_cps_unit`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| n_ref | int | 20–100 | SO field `PetBalanceTargets.parityCheckpointSpins` | mốc tiến độ mà hai kênh được neo ngang nhau |
| base_coin_value(n_ref) | double | ≥ 1 | calculated (Economy `base_coin_value`) | 4.3219 tại n = 50 (α 0.6) |
| parity_band | float[2] | [0.6–0.9, 1.1–1.5] | SO field `PetBalanceTargets.parityBand` | khoảng chấp nhận |

**Output range:** tăng theo `base_coin_value(n)`: kênh Idle mạnh hơn đầu tiệm, kênh Plinko mạnh hơn cuối tiệm (khi người chơi chạm); mục tiêu nằm trong `parity_band` tại `n_ref`.
**Example:** n = 50: 0.05 × 3 × 2.96 × 4.3219 / 2 = **0.959** (trong [0.8, 1.25]). Tham khảo: n = 0 → 0.222; n = 60 → 1.286; n = 100 → 4.147. `idle_cps_unit` là hằng số trong một tiệm (quyết định (A) của Economy, `economy-core.md` R11.2), nên `channel_parity` tăng theo `base_coin_value(n)` và chỉ nằm trong `parity_band` khi n ≈ 44–59 với knob mặc định. Đây là **rủi ro theo dõi bằng mô phỏng** (Open Questions), không phải câu hỏi mở: phần chênh cuối tiệm phải được thú phẩm cấp cao và Sao bù ở kênh Idle.

### collection_spins_estimate (tham khảo — Gacha sở hữu tỷ lệ)

`p_i = assumed_rate[r_i] / pool_count[r_i]`; `E[spins] = ∫₀^∞ [1 − Π_i (1 − e^(−p_i·t))] dt`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| assumed_rate[r] | float[4] | tổng = 1 | SO field `GachaConfig.rarityRates` (Gacha sở hữu, `gacha-machine.md` G1; đọc trực tiếp, Catalog không giữ bản sao) | tỷ lệ công bố của Gacha; mặc định 55/30/12/3 |
| pool_count[r] | int | ≥ 1 | calculated (R9.2) | số thú trong pool của phẩm cấp r |
| collection_spins_band | float[2] | [30–60, 60–150] | SO field `PetBalanceTargets.collectionSpinsBand` | khoảng mục tiêu số lượt để đủ bộ (chưa tính Sao) |
| bước tích phân / sai số dừng / giới hạn tích phân | float | 0.1 lượt / 1e-6 / 10,000 lượt | constant | tham số số học của test, không phải knob thiết kế |

**Output range:** roster v1.0 ≈ 60 lượt (59.9); MVP ≈ 43 lượt (43.4); mục tiêu trong `collectionSpinsBand` [40, 90]. Biên: một thú duy nhất với p = 1 → 1 lượt; khi một p_i → 0 thì E[spins] → ∞ (tích phân dừng khi phần dư < sai số dừng hoặc t vượt giới hạn tích phân; chạm giới hạn → trả "vượt giới hạn" và validator cảnh báo ngoài dải, không treo).
**Example:** v1.0: 6 Thường (0.55 / 6 = 0.0917 mỗi con), 3 Hiếm (0.10), 3 Sử Thi (0.04), 1 Huyền Thoại (0.03) → tích phân số ≈ **60** lượt (giá quay ở lượt 60 ≈ 1,900 xu theo đường neo của Economy).

## Edge Cases

**Dữ liệu và validator**
- **If hai `PetDefinition` cùng `id`, hoặc `id` nằm trong `retiredIds`**: validator báo lỗi, build bị chặn. Nếu vẫn lọt tới lúc chạy (asset sửa ngoài Editor), bước `pet.catalog` báo `IsFailed` → BootFailed; Catalog không bao giờ chọn ngầm một trong hai bản ghi.
- **If `PetCatalog.pets` rỗng hoặc có phần tử `null`**: validator báo lỗi; lúc chạy bước nạp `IsFailed` → BootFailed (Catalog là bước cốt lõi, không có chế độ "chạy không thú").
- **If `weight` ngoài [`weightMin`, `weightMax`], hoặc `magnitude` ngoài `effectLimits` của loại đó, hoặc là NaN / vô cực / âm**: validator báo lỗi. Catalog không kẹp lúc chạy (dữ liệu chỉ đọc); nếu giá trị hỏng vẫn lọt, Shelf làm sạch theo `sanitize` (NaN / âm → 0) và Plinko làm sạch đóng góp theo R5.3 của nó (tốc độ → 0, hệ số nảy → 1), nên không có thu nhập âm hay vô hạn.
- **If `rarityCoefficients` không tăng nghiêm ngặt hoặc Hiếm ≤ 1**: validator báo lỗi (độ hiếm phải nâng trần, R3.2).
- **If số hiệu ứng sai theo vai** (Idle có hiệu ứng, Plinko mang `GroupLeader`, Hỗ trợ có 2 hiệu ứng): validator báo lỗi (R7). Không có trường hợp thú vừa Idle vừa buff.
- **If một trường enum bắt buộc còn `None`** (asset `PetDefinition` mới tạo chưa điền `rarity` / `group` / `role` / `releaseTier`, hoặc `PetEffect` mới có `type = None`): lỗi "thiếu trường bắt buộc" nêu tên trường, build bị chặn; validator không bao giờ coi `None` là `common` / `animal` / `Idle` / `Mvp` (R2).
- **If hiệu ứng có trường không dùng khác mặc định** (ví dụ `PegHitBonus` có `targetGroup = royal`, hoặc `FlatPerCoin` có `includeBurstCoins = true`): cảnh báo; Shelf bỏ qua trường đó.
- **If `GroupLeader` có `targetGroup = None`, hoặc `GroupLeader` / `NeighborBoost` có `scope = None`**: lỗi (trường bắt buộc của loại này, R6).
- **If một hiệu ứng Hỗ trợ có `scope = Both` trên thú có `releaseTier` < `scopeBothMinTier`** (ví dụ đổi Mèo Hoàng Gia sang `Both` trong roster v1.0): lỗi, build bị chặn (R6.5); `Both` chỉ mở cho thú bậc `LiveOps`.
- **If một phẩm cấp có Gacha pool rỗng ở `buildTier`** (ví dụ mọi thú Huyền Thoại bị `deprecated`): Catalog trả danh sách rỗng, không lỗi; validator cảnh báo; cách xử lý lượt quay trúng phẩm cấp rỗng thuộc Gacha Machine.
- **If một thú `deprecated = true` mà `inGachaPool = true`**: lỗi; thú `deprecated` luôn rời pool (R9.2).
- **If roster MVP thiếu thú Idle ở nhóm Đồ Ngọt**: hợp lệ, vì luật nhóm chỉ áp từ `groupRuleMinTier` (`V1_0`, R5.5); cùng lý do MVP được miễn `minCommonsPerGroup`.
- **If `budget_ratio` có target = 0 hoặc không hữu hạn** (ví dụ `plinkoRoleIdleShare` = 1): validator báo lỗi thay vì chia; không có cảnh báo ngân sách giả.

**Lúc chạy**
- **If một hệ truy vấn trước Ready** (lỗi thứ tự khởi tạo): trả "chưa sẵn sàng", không ném exception, không trả dữ liệu rỗng giả như "không tìm thấy".
- **If `TryGetPet` nhận ID lạ** (save có ID của thú `releaseTier` > `buildTier`, ID gõ sai, chuỗi rỗng, `null`): trả "không tìm thấy", không ném lỗi; Inventory quyết định giữ hay ẩn bản ghi đó (Inventory sở hữu).
- **If thú đã `deprecated` vẫn có trong save hoặc trên kệ**: tra được và hoạt động đầy đủ (R9.3), chỉ không vào Gacha pool. Không ai mất thú (Pillar 4).
- **If đổi `displayName` / `nameVi` giữa hai bản build**: save không bị ảnh hưởng vì mọi hệ khóa theo `id` (R1.2).

**Tương tác hiệu ứng (Shelf thi hành, Catalog định nghĩa ngữ nghĩa)**
- **If Mèo Chiêu Tài và xu `JackpotBurst` trúng hũ jackpot**: Plinko phát `JackpotHit` với `origin = JackpotBurst`; vì `includeBurstCoins = false`, Shelf không gửi thưởng. Jackpot từ xu Manual / Auto / Rain vẫn được thưởng.
- **If Thỏ Nhào Lộn với `PegHit` có `scored = false`, hoặc xu Overflow (không có `PegHit`)**: không thưởng. Thỏ tối đa `max_scored_peg_hits` (20) lần thưởng mỗi xu; xu Bùng Xu vẫn tính vì `PegHitBonus` áp cho mọi origin.
- **If Kẹo Bảy Màu và một nguồn nảy khác (boost) cùng hoạt động**: Plinko nhân các hệ số rồi kẹp tại `bounciness_cap`; phần vượt bị bỏ, độ nảy hiệu lực không bao giờ ≥ 1.0. Khi Kẹo đã đưa độ nảy tới trần, nguồn nảy thứ hai gần như vô ích — đánh đổi có chủ đích.
- **If Hamster và boost auto-drop cùng hoạt động**: Plinko cộng các tốc độ và kẹp chu kỳ tại `auto_drop_min_interval` (0.4 s); Catalog không có trần riêng.
- **If Kẹo Bảy Màu và Mèo Chiêu Tài cùng trên kệ**: Kẹo tăng p_jackpot từ ≈ 10.9% lên tối đa ≈ 20% (trần mục tiêu của Plinko ở `bounciness_cap`), nên thưởng tuyệt đối của Mèo Chiêu Tài tăng tối đa ≈ 1.83 lần. Mô hình ngân sách tuyến tính (`plinko_effect_uplift`) không thấy combo này; đây là combo lai trong kênh Plinko có chủ đích, bị chặn bởi trần p_jackpot của Plinko, không cần trần riêng. Đo lại khi có mô phỏng chung (Open Questions).
- **If thú thủ lĩnh hoặc Cáo đứng cạnh / cùng nhóm với thú Hỗ trợ khác**: không có gì xảy ra; thú Hỗ trợ không có nguồn thu nhập nên không nhận buff, và thú Hỗ trợ không bao giờ buff chính nó (R6.4). Hai thú Hỗ trợ không thể tạo vòng nhân.
- **If Cáo Khăn Len (scope Plinko) đứng cạnh thú Plinko không có thưởng sự kiện** (Kẹo, Hamster, Bánh Flan) **hoặc thú Idle**: buff không có đích (R6.2 — hiệu ứng chỉnh bàn và cộng phẳng không có nguồn; thú Idle không có nguồn `.plinko`), nên Cáo chỉ có giá trị cạnh Thỏ, Thiên Nga hoặc Mèo Chiêu Tài. Đây là ràng buộc vị trí Pillar 3 muốn.
- **If Cáo ở ô đầu hoặc cuối kệ**: chỉ có một ô kề, buff một thú.
- **If Mèo Hoàng Gia lên kệ mà không có thú Hoàng Gia nào khác**: buff = 0; thú chiếm một ô mà không sinh gì (chi phí cơ hội thật của vai Hỗ trợ).
- **If buff Synergy cộng dồn trên một thú vượt `synergyTierCap`** (Mèo Hoàng Gia 1.8 + combo nhóm của Shelf): Economy kẹp tầng Synergy tại 4.0; phần vượt bị bỏ.

## Dependencies

Upstream lúc chạy chỉ có Bootstrap. Economy Core và Plinko Board là phụ thuộc **lúc làm nội dung** (validator và ngữ nghĩa hiệu ứng), không phải lúc chạy.

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Bootstrap & Scene Flow | Catalog phụ thuộc Bootstrap (hợp đồng pha) | Hard | In: lượt chạy ở pha `DataLoad`, trước mọi bước `GameplayInit`; hạn `BootConfig.stepTimeoutSeconds`. Out: bước `IBootStep` `pet.catalog` (`Phase = DataLoad`, `IsCritical = true`, `RunsAfter` rỗng) báo `IsDone` / `IsFailed` (R12.1). Owner: Bootstrap (hợp đồng, thứ tự pha, BootFailed); Catalog (nội dung bước nạp). |
| Inventory | Inventory phụ thuộc Catalog | Hard (với Inventory) | Out: `TryGetPet(id)`, danh sách thú đã phát hành, nhóm/phẩm cấp để lọc bộ sưu tập. Owner: Catalog (dữ liệu, ID); Inventory (sở hữu, số bản trùng, cách giữ ID lạ). *GDD Inventory chưa có.* |
| Gacha Machine | Gacha phụ thuộc Catalog | Hard (với Gacha) | Out: Gacha pool theo phẩm cấp (R9.2), ID phẩm cấp (cũng là hậu tố cue `Gacha.RarityReveal.<phẩm cấp>` của Audio), sprite, khóa màu phẩm cấp. Owner: Catalog (pool); Gacha (tỷ lệ phẩm cấp, trọng số trong phẩm cấp, luật trùng, phẩm cấp có pool rỗng). |
| Shelf & Synergy | Shelf phụ thuộc Catalog | Hard (với Shelf) | Out: vai, nhóm, `pet_power_units`, `idle_base_units`, `effects` (`type`, `magnitude`, `scope`, `targetGroup`, `includeBurstCoins`). Shelf thi hành: `RegisterIncomeSource` / modifier tầng Base, Synergy / `SubmitIncome(Plinko, …)` của Economy; `RegisterAutoDropRate(sourceId, magnitude)` / `RegisterBouncinessFactor(sourceId, magnitude)` / `Unregister(sourceId)` của Plinko (R12.4); nghe `PegHit` (`scored`), `JackpotHit` (lọc `origin` theo `includeBurstCoins`), `CoinResolved`. Owner: Catalog (dữ liệu, ngữ nghĩa từ vựng); Shelf (nơi duy nhất thi hành, D1). *GDD chưa có.* |
| Star Progression (VS) | Star phụ thuộc Catalog | Hard (với Star) | Out: ID, phẩm cấp, giá trị 1 Sao (`idle_base_units`, `magnitude`). Owner: Star (hệ số Sao, số bản/cấp, trần). *GDD chưa có.* |
| Neighbor Pranks (v1.1) | Neighbor phụ thuộc Catalog | Hard (với Neighbor) | Out: ID, nhóm, phẩm cấp (chọn mục tiêu "dụ đi chơi"). Đồ hàng xóm ở danh mục `gadget.` riêng (D2), không nằm trong Catalog. *GDD chưa có.* |
| Plinko Board | Ngữ nghĩa hiệu ứng của Catalog dựa trên hợp đồng Plinko; không phụ thuộc lúc chạy theo chiều nào | Soft (lúc làm nội dung) | Catalog dựa vào: trường `scored` của `PegHit`; **trường `origin` của `JackpotHit { coinId, origin, bucketIndex, worldPosition }`** (`includeBurstCoins` của `JackpotBonus` chỉ có nghĩa nhờ trường này, `plinko-board.md` R7.2, R12); API `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister` và luật cộng dồn của Plinko (R12.5); trần `bounciness_cap`, `max_scored_peg_hits`, `auto_drop_min_interval`. Validator đọc `PlinkoConfig` (`jackpotBurstCount`, `autoDropBaseInterval`, `basePegBounciness`, `bouncinessCap`). Owner: Plinko (sự kiện, API, trần); Catalog (độ lớn hiệu ứng thú). |
| Economy Core | Không phụ thuộc lúc chạy; validator của Catalog đọc knob Economy | Soft (lúc làm nội dung) | Validator đọc `EconomyConfig.idleCpsUnit`, `.plinkoFlatPerCoinCap`, `.synergyTierCap` và `EconomyBalanceTargets.plinkoEvEffectiveAssumed`, `.plinkoJackpotProbMeanAssumed`, `.plinkoScoredPegHitsPerCoinAssumed`, `.realisticTapRate`, `.plinkoBounceUpliftAtCapAssumed`; thẻ thú dùng `EconomyConfig.displaySigDigits` và `display_format` (R13) để định dạng số (UI Requirements). Lúc chạy, Shelf nhân `idle_base_units` × `idleCpsUnit` và thưởng sự kiện theo `baseCoinValue` (D4). Owner: Economy (đơn vị, tầng, trần, giả định đo của Plinko); Catalog (số không thứ nguyên). |
| HUD & Tab Navigation / Feedback & Juice / màn Kho | UI đọc Catalog qua Inventory / Shelf / Gacha | Soft | Out: tên, nhóm, vai, phẩm cấp, sprite, khóa mẫu mô tả hiệu ứng (UI Requirements). Owner: Catalog (dữ liệu); UI (trình bày). *Chưa có trong systems index — báo bước Sync.* |
| Settings & Accessibility (Alpha) | Catalog đọc ngôn ngữ đang chọn | Soft | In: ngôn ngữ. Out: chuỗi bản địa hóa qua `nameLocKey` / `flavorLocKey`. Owner: ADR bản địa hóa. |
| Save/Load (VS) | Không có | — | Catalog không lưu gì; Inventory lưu `id` (R1.2, R12.1). |

**Hai chiều**: Bootstrap & Scene Flow (`bootstrap-and-scene-flow.md` B3, bảng Dependencies) đã liệt kê Pet Catalog là bước `DataLoad` cốt lõi — khớp. Plinko Board (`plinko-board.md` Dependencies, "Hai chiều") ghi Catalog không phụ thuộc lúc chạy, chỉ khớp ngữ nghĩa qua `origin` của `JackpotHit` — khớp. Economy Core (`economy-core.md` Dependencies) ghi "Pet Catalog: không phụ thuộc trực tiếp" — khớp. Systems index liệt kê Pet Catalog trong Depends On của Inventory, Gacha, Shelf, Star, Neighbor, và hàng Pet Catalog có Bootstrap — khớp. Khi viết, GDD Inventory, Gacha, Shelf, Star, Neighbor phải liệt kê Pet Catalog với interface ở trên.

## Tuning Knobs

Bốn asset: `PetRarityConfig` (bảng phẩm cấp, runtime), `PetCatalog` (danh sách và luật nội dung, runtime + validator), `PetDefinition` (mỗi thú một asset, runtime) và `PetBalanceTargets` (chỉ validator / test, không ảnh hưởng gameplay — cùng mẫu với `PlinkoBalanceTargets`, `EconomyBalanceTargets`). Không có số nào trong code. Mọi giá trị là **tạm** cho tới mô phỏng chung với Economy Core và Shelf & Synergy.

**Bảng phẩm cấp và luật nội dung**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Hệ số Hiếm / Sử Thi / Huyền Thoại | `PetRarityConfig.rarityCoefficients` | float[3] | [2.5, 6, 15] | 2–3 / 4.5–8 / 10–20, tăng nghiêm ngặt, > 1 | Thú hiếm làm thú Thường vô dụng, kệ chỉ còn một đáp án | Bóc Huyền Thoại không sướng (Pillar 2); phẩm cấp chồng lấn | weightMin/Max, idleCpsUnit, plinkoUpliftPerUnit |
| Trọng số thấp nhất | `PetRarityConfig.weightMin` | float | 0.8 | 0.7–0.95 | Mọi thú cùng phẩm cấp như nhau | Phẩm cấp chồng lấn (cần `weightMax`/`weightMin` < tỷ số hệ số liền kề nhỏ nhất, 2.4) | rarityCoefficients |
| Trọng số cao nhất | `PetRarityConfig.weightMax` | float | 1.2 | 1.05–1.3 | Phẩm cấp chồng lấn | Mọi thú cùng phẩm cấp như nhau | rarityCoefficients |
| Tỷ phần CPS của vai Plinko | `PetRarityConfig.plinkoRoleIdleShare` | float | 0.1 | 0–0.2 | Thú Plinko mạnh cả hai kênh (phá Pillar 3) | Thú Plinko vô dụng khi người chơi không chạm (idle) | plinkoUpliftPerUnit |
| Giá trị một đơn vị sức mạnh trên kênh Plinko | `PetRarityConfig.plinkoUpliftPerUnit` | float | 0.05 | 0.03–0.08 | Kênh Plinko lấn Idle (`channel_parity` cao) | Thú Plinko không đáng một ô | idleCpsUnit, realisticTapRate, plinkoEvEffectiveAssumed |
| Bậc phát hành của build | `PetCatalog.buildTier` | enum | `Mvp` (build MVP) | `Mvp` … `LiveOps` | Thú chưa cân lọt vào build | Thú đã hứa vắng mặt | releaseTier mỗi thú |
| Bậc roster dùng để cân bằng | `PetCatalog.balanceRosterTier` | enum | `V1_0` | `V1_0` … `LiveOps` | — | Cân theo roster thiếu (MVP) làm lệch ngân sách v1.0 | referenceGroupLeaderTargets |
| Bậc thấp nhất được dùng `scope = Both` | `PetCatalog.scopeBothMinTier` | enum | `LiveOps` | `V1_1` … `LiveOps` | — | Thủ lĩnh / Cáo buff cả hai kênh trong roster chính, thành đáp án an toàn (phá Pillar 3, trái quyết định (a)) | R6.5, AC-8 |
| Bậc bắt đầu luật nhóm | `PetCatalog.groupRuleMinTier` | enum | `V1_0` | `V1_0` … `LiveOps` | Roster v1.0 có thể thiếu vai trong một nhóm | MVP vi phạm (Đồ Ngọt không có thú Idle, không có thú Thường) | minCommonsPerGroup |
| Số thú Thường tối thiểu mỗi nhóm | `PetCatalog.minCommonsPerGroup` | int | 2 | 1–2 | Phải làm thêm thú Thường (ngoài phạm vi 13 thú) | Combo nhóm chỉ mở bằng thú hiếm | groupRuleMinTier |
| Số hiệu ứng tối đa mỗi thú | `PetCatalog.maxEffectsPerPet` | int | 1 | 1 (> 1 phải sửa R7 và Shelf) | Thú phục vụ hai kênh (phá Pillar 3) | — | R7 |
| Độ dài flavor tối đa | `PetCatalog.flavorMaxChars` | int | 80 | 60–120 | Tràn thẻ thú trên khung 9:16 | Flavor cụt, mất duyên | UI thẻ thú |
| Giới hạn độ lớn theo loại hiệu ứng | `PetCatalog.effectLimits[type]` | float[2] mỗi loại | `PegHitBonus` [0.002, 0.05]; `JackpotBonus` [0.5, 10]; `FlatPerCoin` [0.01, 0.5]; `AutoDropRate` [0.05, 1.0]; `BouncinessModifier` [1.0, 2.0]; `GroupLeader` [0.2, 2.5]; `NeighborBoost` [0.2, 1.5] | min ≥ 0 (`BouncinessModifier` min ≥ 1); `FlatPerCoin` max ≤ `plinkoFlatPerCoinCap`; `BouncinessModifier` max ≥ `bouncinessCap` / `basePegBounciness` (1.7) để chạm được trần; phần trên mức đó bị Plinko kẹp, vô ích | Một thú có thể vượt ngân sách mà validator không bắt | Không cân được thú theo ngân sách | budgetTolerance, trần của Plinko và Economy |
| Danh sách ID đã khai tử | `PetCatalog.retiredIds` | string[] | rỗng | chỉ thêm, không bao giờ xóa | — | — | R1.3 |

Dữ liệu không phải số (không phải knob cân bằng): `PetRarityConfig.rarities` (tên UI, khóa màu theo art bible), `PetCatalog.groups` (tên UI, biểu tượng), `PetCatalog.pets` (danh sách asset), `PetCatalog.effectDescriptionKeys` (khóa mẫu mô tả hiệu ứng, UI Requirements).

**Knob theo từng thú** (`PetDefinition`; safe range của `weight` = [`weightMin`, `weightMax`], của `magnitude` = `effectLimits` theo loại; quá cao / quá thấp đo bằng `budget_ratio`)

| Thú (`id`) | `weight` | `effects[0].magnitude` | Cờ khác |
|---|---|---|---|
| `pet.woody_duck` | 1.0 | — | — |
| `pet.sleepy_hamster` | 1.0 | `AutoDropRate` 0.27 | — |
| `pet.bouncy_bunny` | 1.1 | `PegHitBonus` 0.017 | — |
| `pet.cozy_fox` | 1.0 | `NeighborBoost` 1.2 | `scope = Plinko` |
| `pet.dango_trio` | 1.1 | — | — |
| `pet.wobbly_pudding` | 1.0 | `FlatPerCoin` 0.086 | — |
| `pet.sprinkle_cupcake` | 1.0 | — | — |
| `pet.rainbow_candy` | 1.2 | `BouncinessModifier` 1.7 | — |
| `pet.dreamy_frog` | 1.2 | — | — |
| `pet.velvet_swan` | 1.1 | `PegHitBonus` 0.0068 | — |
| `pet.lucky_cat` | 1.0 | `JackpotBonus` 3.0 | `includeBurstCoins = false` |
| `pet.proud_peacock` | 1.1 | — | — |
| `pet.chubby_king_cat` | 1.0 | `GroupLeader` 1.8 | `targetGroup = royal`, `scope = Idle` |

**Mục tiêu cân bằng (chỉ validator / test)**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Dung sai ngân sách | `PetBalanceTargets.budgetTolerance` | float | 0.25 | 0.1–0.4 | Thú lệch ngân sách lọt qua | Cảnh báo giả vì mô hình tuyến tính thô | mọi magnitude |
| Mốc neo hai kênh | `PetBalanceTargets.parityCheckpointSpins` | int | 50 | 20–100 | Kênh Idle lấn suốt đầu tiệm | Kênh Plinko lấn từ sớm | parityBand, spinGrowth, coinValueExponent |
| Dải chấp nhận cân hai kênh | `PetBalanceTargets.parityBand` | float[2] | [0.8, 1.25] | thấp 0.6–0.9, cao 1.1–1.5 | Một kênh thống trị lọt qua | Cảnh báo liên tục khi đổi đường giá | plinkoUpliftPerUnit, idleCpsUnit |
| Dải lượt quay để đủ bộ | `PetBalanceTargets.collectionSpinsBand` | float[2] | [40, 90] | thấp 30–60, cao 60–150 | Tường gacha (rủi ro concept) | Bộ sưu tập xong quá nhanh | `GachaConfig.rarityRates`, roster |
| Đơn vị một thú kề điển hình | `PetBalanceTargets.referenceNeighborTargetUnits` | float | 2.5 | 1–6 | `NeighborBoost` bị cân quá yếu | `NeighborBoost` bị cân quá mạnh | magnitude Cáo |
| Số thú cùng nhóm điển hình cạnh thủ lĩnh | `PetBalanceTargets.referenceGroupLeaderTargets` | int | 3 | 2–4 | Thủ lĩnh bị cân yếu khi kệ nhỏ | Thủ lĩnh mạnh quá khi kệ đầy | số ô kệ (Shelf), magnitude Mèo Hoàng Gia |
| Thời gian nạp Catalog tối đa | `PetBalanceTargets.catalogLoadMaxMs` | float (ms) | 50 | 20–200 | Ăn vào ngân sách khởi động 2.0 s | Test trượt trên máy yếu | `BootConfig.maxBootToInteractiveSeconds` |
| Số lần gọi trong test GC | `PetBalanceTargets.lookupBenchmarkCalls` | int | 10,000 | 1,000–100,000 | Test chậm | Không bắt được cấp phát lẻ | — |

**Knob của hệ khác mà Catalog dùng (không lặp lại ở đây)**: `EconomyConfig.idleCpsUnit` (2.0), `.plinkoFlatPerCoinCap` (2.0), `.synergyTierCap` (4.0); `EconomyBalanceTargets.plinkoEvEffectiveAssumed` (2.96), `.plinkoJackpotProbMeanAssumed` (0.109), `.plinkoScoredPegHitsPerCoinAssumed` (14), `.realisticTapRate` (3.0), `.plinkoBounceUpliftAtCapAssumed` (0.40), `.valueRelTolerance` (dung sai so sánh số thực trong test); `EconomyConfig.displaySigDigits` (3, định dạng số trên thẻ thú); `PlinkoConfig.jackpotBurstCount` (5), `.autoDropBaseInterval` (2.0 s), `.autoDropMinInterval` (0.4 s), `.basePegBounciness` (0.5), `.bouncinessCap` (0.85), `.maxScoredPegHits` (20); `BootConfig.stepTimeoutSeconds` (10 s). Hai knob cũ `PetBalanceTargets.pJackpotMeanAssumed` và `.avgScoredPegHitsPerCoin` đã xóa (C1[12]); `PetBalanceTargets.bounceUpliftAtCapAssumed` đã xóa, thay bằng `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (C2[12]).

**Hằng số (không phải knob)**: `rarity_coef[common]` = 1 (mốc chuẩn hóa, R3.2); `role_idle_share[Idle]` = 1 và `[Support]` = 0 (định nghĩa vai, D3); `neighbor_slots` = 2 (kệ một hàng ngang); bước / sai số dừng / giới hạn tích phân của `collection_spins_estimate` (tham số số học); bốn phẩm cấp, ba nhóm, ba vai, bảy loại hiệu ứng và năm bậc phát hành (cấu trúc của hợp đồng dữ liệu, đổi là đổi GDD); thành viên `None = 0` của mọi enum Catalog (giá trị "chưa đặt" để validator phát hiện trường bắt buộc bị bỏ trống, vì Unity serialize enum mới bằng 0; R2, R6).

## Visual/Audio Requirements

Catalog không phát sự kiện lúc chạy và không tự vẽ hay phát tiếng; nó chỉ cấp **dữ liệu** để các hệ trình bày dùng nhất quán. art-director không được gọi (lean, workflow không cho subagent): màu phẩm cấp chờ art bible.

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Thú xuất hiện ở bất kỳ đâu (thẻ mở trứng, Kho, Kệ) | Emoji giấy phép mở của thú trong khung gỗ bo tròn (Zero Art); viền/khung theo **màu phẩm cấp** lấy từ `rarity` qua art bible, không đặt riêng từng thú; nền gỗ kem bơ, không màu gắt | — | MVP |
| Mở trứng ra thú (Gacha trình bày) | Ánh sáng phẩm cấp theo khóa màu của `rarity` | Cue `Gacha.RarityReveal.<phẩm cấp>` của Audio; hậu tố là ID phẩm cấp ổn định `common` / `rare` / `epic` / `legendary` (R3.1) | MVP |
| Hiển thị nhóm của thú | Biểu tượng nhóm từ `PetCatalog.groups` (Động Vật / Đồ Ngọt / Hoàng Gia), kèm chữ — không dựa vào màu | — | MVP |
| Hiển thị vai của thú | Biểu tượng vai (Idle / Plinko / Hỗ trợ) kèm chữ, khác hình dạng để đọc được khi mù màu | — | MVP |
| Thú Huyền Thoại (Mèo Hoàng Gia) trên thẻ | Khung phẩm cấp Huyền Thoại; không thêm hiệu ứng riêng cho từng thú (tránh vỡ Zero Art) | Lớp ngân dài của cue Huyền Thoại thuộc Audio | MVP |

Ràng buộc art: mỗi thú đúng một sprite emoji; không vẽ riêng. Màu phẩm cấp phải hài hòa palette Tiệm Gỗ Kem Bơ và chỉ dùng cho phần thưởng (nguyên tắc "Màu là thông tin" của concept). Board Penpot cũ có thể còn 7 thú FirstGDD với số cũ — cần cập nhật theo R8.

## Game Feel

Không áp dụng — Pet Catalog là dữ liệu tĩnh chỉ đọc ở tầng Foundation: không có input, hoạt cảnh hay khoảnh khắc va chạm nào của riêng nó. Cảm giác mở trứng thuộc Gacha Machine, kéo thả thú lên kệ thuộc Shelf & Synergy, phản hồi xu/đinh thuộc Plinko Board và Feedback & Juice.

## UI Requirements

Catalog không sở hữu màn hình. Nó định nghĩa **thông tin của thẻ thú** mà Gacha (thẻ mở trứng), Inventory (màn Kho) và Shelf (chi tiết ô kệ) cùng hiển thị, để ba nơi đọc giống nhau.

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Emoji + khung màu phẩm cấp | Thẻ thú (mở trứng, Kho, chi tiết kệ) | Tĩnh | Luôn |
| Tên (theo ngôn ngữ, khóa `nameLocKey`, dự phòng `nameVi`) | Thẻ thú, nhãn dưới ô kệ | Khi đổi ngôn ngữ | Luôn |
| Phẩm cấp (chữ + màu) | Thẻ thú | Tĩnh | Luôn |
| Nhóm (biểu tượng + chữ) | Thẻ thú, góc ô kệ | Tĩnh | Luôn — người chơi cần nó để xếp combo |
| Vai (biểu tượng + chữ: Idle / Plinko / Hỗ trợ) | Thẻ thú, góc ô kệ | Tĩnh | Luôn — Pillar 3 phải đọc được không cần mở thẻ |
| Mô tả hiệu ứng có số thật | Thẻ thú | Khi thẻ mở và khi `SpinPriceChanged` (số theo `baseCoinValue`) | Thú có hiệu ứng |
| Sản lượng CPS ở Sao hiện tại | Thẻ thú | Khi thẻ mở | Thú vai Idle (và phần nhỏ của vai Plinko) |
| Flavor (≤ `flavorMaxChars`) | Thẻ thú (Kho, mở trứng) | Khi đổi ngôn ngữ | Luôn |

Mẫu mô tả hiệu ứng (khóa ở `PetCatalog.effectDescriptionKeys`, số do UI tính từ knob và định dạng theo quy tắc "Định dạng số trên thẻ thú" bên dưới; số trong ngoặc là giá trị mặc định ở `baseCoinValue` = 1):
- `PegHitBonus`: "+{magnitude × baseCoinValue} xu mỗi lần xu chạm đinh (tối đa {maxScoredPegHits} lần mỗi xu)" (Thỏ: +0.017).
- `JackpotBonus`: "+{magnitude × baseCoinValue} xu mỗi lần trúng hũ BONUS" — thêm "(cả xu thưởng)" khi `includeBurstCoins = true` (Mèo Chiêu Tài: +3).
- `FlatPerCoin`: "+{magnitude × baseCoinValue} xu cho mỗi xu vào hũ" (Bánh Flan: +0.086).
- `AutoDropRate`: "Tự thả 1 xu mỗi {autoDropBaseInterval / magnitude} giây" (Hamster: 7.41 s).
- `BouncinessModifier`: "Đinh nảy hơn ×{magnitude} (độ nảy tối đa {bouncinessCap})" (Kẹo: ×1.7).
- `GroupLeader`: "+{magnitude × 100}% thu nhập {kênh} cho mỗi thú {nhóm} khác trên kệ" (Mèo Hoàng Gia: +180%, Idle, Hoàng Gia).
- `NeighborBoost`: "+{magnitude × 100}% thu nhập {kênh} cho thú ở hai ô kề" (Cáo: +120%, Plinko).

**Định dạng số trên thẻ thú.** Economy Core sở hữu bộ định dạng số (`economy-core.md` R13, `display_format`); R13 chỉ định nghĩa số dư, giá và tốc độ (số dư < 1000 làm tròn xuống thành số nguyên, tốc độ cắt `rateDecimals`), nên áp nguyên cho mô tả hiệu ứng thì Thỏ 0.017, Thiên Nga 0.0068, Bánh Flan 0.086 thành "+0". Catalog yêu cầu thêm một quy tắc riêng cho số trong mô tả hiệu ứng (xu mỗi sự kiện, giây, hệ số ×, phần trăm), dùng lại knob của Economy:
1. Giá trị v với |v| < 1000: hiện `EconomyConfig.displaySigDigits` (3) chữ số có nghĩa, **làm tròn gần nhất** (nửa làm tròn ra xa 0), bỏ số 0 thừa sau dấu thập phân và bỏ dấu thập phân nếu không còn chữ số lẻ; không hậu tố.
2. |v| ≥ 1000: định dạng hậu tố của R13 (`display_format`, `displaySigDigits` chữ số có nghĩa, K / M / B …).
3. Hệ quả: mọi v > 0 hiện ít nhất một chữ số khác 0, nên thẻ không bao giờ hiện "+0" cho hiệu ứng có magnitude > 0 (validator đã chặn magnitude dưới cận dưới `effectLimits`, ví dụ `PegHitBonus` 0.002 → "+0.002").
4. Ví dụ ở `baseCoinValue` = 1: Thỏ "+0.017", Thiên Nga "+0.0068", Bánh Flan "+0.086", Mèo Chiêu Tài "+3", Hamster "7.41" giây, Kẹo "×1.7" (trần "0.85"), Mèo Hoàng Gia "+180%", Cáo "+120%"; ở `baseCoinValue` = 18.679 (n = 100): Thỏ "+0.318", Mèo Chiêu Tài "+56".
Số dư ví và tốc độ CPS trên thẻ (dòng "Sản lượng CPS") vẫn dùng nguyên R13. Biểu tượng nhóm và vai có chữ đi kèm và khác hình dạng, không chỉ khác màu.

UX flag: hệ này có UI (thẻ thú dùng chung). Đặc tả UX theo màn hình chưa có trong plugin — thẻ thú cần một board Penpot dùng chung cho Gacha / Kho / Kệ trước khi làm epic; ghi chú cho hàng Pet Catalog trong systems index (bước Sync).

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Bước nạp ở pha dữ liệu | `Docs/design/gdd/bootstrap-and-scene-flow.md` | B2 (`IBootStep`: `Id`, `Phase`, `RunsAfter`, `IsCritical`, `IsDone` / `IsFailed`), B3 (pha `DataLoad` trước `GameplayInit`), B4 (BootFailed, "Thử lại"), `BootConfig.stepTimeoutSeconds`, `maxBootToInteractiveSeconds` | Catalog đăng ký bước `pet.catalog`; Bootstrap sở hữu hợp đồng |
| Đơn vị và tầng thu nhập | `Docs/design/gdd/economy-core.md` | R4.4–R4.7 (tầng Base / Synergy, `plinkoFlatPerCoinCap`, `synergyTierCap`), R5.3 (`SubmitIncome`), R11 (`idleCpsUnit`, hằng số trong tiệm — quyết định (A)), R13 (`display_format`, `displaySigDigits`), `base_coin_value`, `idle_cps` | Catalog cấp số không thứ nguyên; Economy sở hữu đơn vị, tầng, trần |
| Giả định đo của Plinko | `Docs/design/gdd/economy-core.md` | `EconomyBalanceTargets.plinkoEvEffectiveAssumed`, `.plinkoJackpotProbMeanAssumed`, `.plinkoScoredPegHitsPerCoinAssumed`, `.realisticTapRate`, `.plinkoBounceUpliftAtCapAssumed`, `.valueRelTolerance` | Validator / test của Catalog đọc (C1[12], C2[12]) |
| Sự kiện thú phản ứng | `Docs/design/gdd/plinko-board.md` | R12 `PegHit` (`scored`), `JackpotHit { coinId, origin, bucketIndex, worldPosition }`, `CoinResolved`; R7.2 (xu `JackpotBurst` phát `JackpotHit` với `origin = JackpotBurst`) | `includeBurstCoins` lọc theo `origin` (C1[8]) |
| API modifier bàn | `Docs/design/gdd/plinko-board.md` | R12.4–R12.5 `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`, luật cộng / nhân và kẹp | Shelf đăng ký độ lớn của Catalog (C1[13]) |
| Trần và công thức bàn | `Docs/design/gdd/plinko-board.md` | `effective_bounciness`, `bounciness_cap` (0.85), `max_scored_peg_hits` (20), `auto_drop_interval`, `auto_drop_min_interval`, `jackpot_burst_count` (5), `bucket_probability_and_ev`, AC-13c, AC-30 | Giá trị khóa trong registry; Catalog không được vượt |
| Cue phẩm cấp | `Docs/design/gdd/audio.md` | `Gacha.RarityReveal.<phẩm cấp>` | Hậu tố cue = ID phẩm cấp của Catalog |
| Thi hành hiệu ứng, combo nhóm, khóa nguồn `shelf.slot<i>.idle/.plinko` | `Docs/design/gdd/shelf-and-synergy.md` *(chưa có)* | R6 (từ vựng hiệu ứng), `idle_base_units`, `pet_event_bonus` | Shelf là nơi duy nhất thi hành (D1) |
| Gacha pool, tỷ lệ phẩm cấp | `Docs/design/gdd/gacha-machine.md` | R9.2, `GachaConfig.rarityRates` | Catalog đọc `GachaConfig.rarityRates` (Gacha sở hữu) |
| Sở hữu, ID lạ | `Docs/design/gdd/inventory.md` *(chưa có)* | R1, R12.2 `TryGetPet` | Inventory khóa theo `id` |
| Hệ số Sao | `Docs/design/gdd/star-progression.md` *(chưa có)* | Giá trị 1 Sao, `star_multiplier` | Star sở hữu đường Sao |
| Pillar, roster MVP, Quyết định 5 | `Docs/design/gdd/game-concept.md` | Pillar 2, 3, 4; MVP; Visual identity (Zero Art, màu phẩm cấp) | Nguồn của D3, D5, D6 |

## Acceptance Criteria

Viết bởi game-designer (qa-lead không được gọi — workflow không cho subagent). Ký hiệu: `RC.` = `PetRarityConfig`, `PC.` = `PetCatalog`, `PT.` = `PetBalanceTargets`, `EC.` = `EconomyConfig`, `ET.` = `EconomyBalanceTargets`, `PL.` = `PlinkoConfig`. "Catalog mặc định" = 13 asset `PetDefinition` và mọi knob ở giá trị hiện tại của Tuning Knobs. Giá trị kỳ vọng được test tính lại từ knob theo công thức; số trong ngoặc là giá trị mặc định làm tròn để đọc, so sánh số thực dùng sai số tương đối `ET.valueRelTolerance` (hoặc sai số ghi rõ). Assembly test: `CozyGachapon.Tests.EditMode`. Tiêu chí ghi "(test của Shelf)" là hợp đồng ngữ nghĩa Catalog định nghĩa nhưng chỉ chạy được khi Shelf & Synergy có code; GDD Shelf phải nhận lại các tiêu chí này.

**ID và bản ghi (R1, R2)**
- `AC-1 [EditMode]` **GIVEN** catalog test gồm bản ghi hợp lệ với lần lượt các ID `pet.woody_duck`, `pet.a1_b2`, `Pet.Duck`, `pet.`, `duck`, `pet.vịt`, `pet.a-b`, `gadget.basket`, **WHEN** chạy validator, **THEN** hai ID đầu không có lỗi và mỗi ID trong sáu ID còn lại có đúng 1 lỗi "ID sai định dạng".
- `AC-2 [EditMode]` **GIVEN** (a) hai bản ghi cùng `id = pet.woody_duck`, (b) một bản ghi có `id` nằm trong `PC.retiredIds`, (c) catalog mặc định, **WHEN** chạy validator và bước kiểm trước build, **THEN** (a) có đúng 1 lỗi "ID trùng", (b) đúng 1 lỗi "ID đã khai tử", và bước kiểm build báo thất bại ở (a), (b); (c) có 0 lỗi và build tiếp tục.
- `AC-3 [EditMode]` **GIVEN** một bản ghi hợp lệ, **WHEN** lần lượt bỏ trống từng trường bắt buộc (`id`, `displayName`, `nameVi` = chuỗi rỗng; `sprite` = `null`; `rarity`, `group`, `role`, `releaseTier` = `None`), **THEN** mỗi lần có đúng 1 lỗi "thiếu trường bắt buộc" nêu tên trường đó; **WHEN** tạo một asset `PetDefinition` mới (mọi trường ở giá trị serialize mặc định), **THEN** `rarity`, `group`, `role`, `releaseTier` đọc ra `None` và validator có đúng 1 lỗi "thiếu trường bắt buộc" cho mỗi trường trong 8 trường bắt buộc, không trường enum nào được coi là `common` / `animal` / `Idle` / `Mvp`; **WHEN** `flavorVi` dài đúng `PC.flavorMaxChars` (80) ký tự, **THEN** 0 cảnh báo; dài `PC.flavorMaxChars` + 1 → đúng 1 cảnh báo và 0 lỗi; thiếu `nameLocKey` → đúng 1 cảnh báo.

**Phẩm cấp, nhóm, vai (R3, R4, R5)**
- `AC-4 [EditMode]` **GIVEN** `RC.rarityCoefficients` lần lượt = [2.5, 6, 15], [2.5, 2.5, 15], [6, 2.5, 15], [1.0, 6, 15], [0.9, 6, 15], **WHEN** chạy validator, **THEN** bộ đầu có 0 lỗi, mỗi bộ còn lại có đúng 1 lỗi "hệ số phẩm cấp không tăng nghiêm ngặt"; hệ số Thường đọc ra luôn = 1 và không có trường nào để sửa nó.
- `AC-5 [EditMode]` **GIVEN** một thú với `weight` lần lượt = `RC.weightMin` (0.8), `RC.weightMax` (1.2), `RC.weightMin` × 0.99, `RC.weightMax` × 1.01, NaN, **WHEN** chạy validator, **THEN** hai giá trị đầu có 0 lỗi, ba giá trị sau mỗi giá trị đúng 1 lỗi "weight ngoài khoảng".
- `AC-6 [EditMode]` **GIVEN** catalog mặc định, `PC.buildTier` = `V1_0`, **WHEN** lấy thú theo từng nhóm, **THEN** ba danh sách rời nhau, hợp lại đúng 13 thú; `animal` và `sweet` mỗi nhóm 4 thú (2 Thường, 1 Hiếm, 1 Sử Thi), `royal` 5 thú (thêm Mèo Hoàng Gia Huyền Thoại); `PC.groups` có tên UI và biểu tượng cho cả 3 nhóm.
- `AC-7 [EditMode]` **GIVEN** catalog mặc định và `PC.groupRuleMinTier` = `V1_0`, **WHEN** chạy validator, **THEN** 0 lỗi luật nhóm (roster MVP: nhóm `sweet` không có thú Idle và không có thú Thường nhưng được miễn); **WHEN** đặt `releaseTier` của `pet.dango_trio` và `pet.sprinkle_cupcake` = `V1_1`, **THEN** có lỗi nêu nhóm `sweet` thiếu vai Idle và có 1 < `PC.minCommonsPerGroup` (2) thú Thường ở bậc `V1_0`, và không có lỗi luật nhóm ở bậc `V1_1`, `LiveOps`; **WHEN** khôi phục mặc định rồi đặt `PC.groupRuleMinTier` = `Mvp`, **THEN** có lỗi luật nhóm cho `sweet` ở bậc `Mvp`.

**Hiệu ứng (R6, R7)**
- `AC-8 [EditMode]` **GIVEN** catalog mặc định, **WHEN** lần lượt (mỗi lần từ catalog mặc định) (a) đặt `targetGroup = royal` cho hiệu ứng `PegHitBonus` của Thỏ, (b) đặt `targetGroup = None` cho `GroupLeader` của Mèo Hoàng Gia, (c) tạo một `PetEffect` mới, (d) đặt `scope = None` cho `NeighborBoost` của Cáo Khăn Len, (e) đặt `scope = None` cho `GroupLeader` của Mèo Hoàng Gia, (f) đặt `scope = Idle` cho `JackpotBonus` của Mèo Chiêu Tài, (g) đặt `type = None` cho hiệu ứng của Thỏ, (h) đặt `scope = Both` cho `GroupLeader` của Mèo Hoàng Gia (`releaseTier = Mvp` < `PC.scopeBothMinTier`), (i) như (h) nhưng đặt thêm `releaseTier` của Mèo Hoàng Gia = `PC.scopeBothMinTier` (`LiveOps`), **THEN** (a) đúng 1 cảnh báo "trường không dùng khác mặc định", 0 lỗi; (b) đúng 1 lỗi "thiếu trường bắt buộc" nêu `targetGroup`; (c) `type` = `None`, `scope` = `None`, `targetGroup` = `None`, `includeBurstCoins` = false; (d), (e) mỗi trường hợp đúng 1 lỗi "thiếu trường bắt buộc" nêu `scope`; (f) đúng 1 cảnh báo, 0 lỗi; (g) đúng 1 lỗi "thiếu trường bắt buộc" nêu `type`; (h) đúng 1 lỗi "`scope = Both` chưa mở ở bậc này"; (i) 0 lỗi loại này; catalog mặc định có 0 lỗi và 0 cảnh báo loại này.
- `AC-9 [EditMode]` **GIVEN** lần lượt: thú Idle có 1 hiệu ứng; thú Plinko có 0 hiệu ứng; thú Plinko mang `GroupLeader`; thú Hỗ trợ mang `PegHitBonus`; thú Hỗ trợ có 2 hiệu ứng (`PC.maxEffectsPerPet` = 1), **WHEN** chạy validator, **THEN** mỗi trường hợp có đúng 1 lỗi "số/loại hiệu ứng sai theo vai"; catalog mặc định có 0 lỗi loại này.
- `AC-10 [EditMode]` **GIVEN** với mỗi loại trong 7 loại hiệu ứng, `magnitude` lần lượt = cận dưới và cận trên của `PC.effectLimits[type]`, cận dưới × 0.99, cận trên × 1.01, NaN, **WHEN** chạy validator, **THEN** hai giá trị đầu có 0 lỗi, ba giá trị sau mỗi giá trị đúng 1 lỗi "magnitude ngoài khoảng" (riêng `BouncinessModifier` cận dưới là 1.0, nên 0.99 cũng lỗi).
- `AC-11 [EditMode]` (test của Shelf) **GIVEN** Mèo Chiêu Tài trên kệ, `base_coin_value` = b, Plinko giả phát `JackpotHit` lần lượt với `origin` Manual, Auto, Rain, JackpotBurst, **WHEN** Shelf xử lý, **THEN** có đúng 3 lần `SubmitIncome(Plinko, nguồn của Mèo Chiêu Tài, 3.0 × b)` và không lần nào cho `origin = JackpotBurst`; khi đổi `includeBurstCoins = true` → đúng 4 lần.
- `AC-12 [EditMode]` (test của Shelf) **GIVEN** Thỏ Nhào Lộn trên kệ, `base_coin_value` = b, Plinko giả phát 14 `PegHit` có `scored = true` và 6 có `scored = false` cho một xu, **WHEN** Shelf xử lý, **THEN** có đúng 14 lần `SubmitIncome(Plinko, nguồn của Thỏ, 0.017 × b)`.
- `AC-13 [EditMode]` (test của Shelf + Plinko) **GIVEN** bàn không có nguồn, **WHEN** Kẹo Bảy Màu và Hamster lên kệ rồi cùng rời kệ, **THEN** Plinko nhận đúng 1 `RegisterBouncinessFactor(nguồn Kẹo, 1.7)` và đúng 1 `RegisterAutoDropRate(nguồn Hamster, 0.27)`; khi cả hai trên kệ `effective_bounciness` = min(`PL.basePegBounciness` × 1.7, `PL.bouncinessCap`) (0.85, < 1.0) và chu kỳ auto-drop = `PL.autoDropBaseInterval` / 0.27 (7.41 s); sau khi rời kệ có `Unregister` cho cả hai nguồn, `bounciness_modifier` = 1 và `auto_rate_multiplier` = 0.
- `AC-14 [EditMode]` (test của Shelf + Economy) **GIVEN** kệ 5 ô theo thứ tự ô 1 = Cáo Khăn Len, ô 2 = Ếch Cốm, ô 3 = Mèo Hoàng Gia, ô 4 = Công Kiêu Hãnh, ô 5 = Mèo Chiêu Tài (ô kề duy nhất của Cáo là Ếch — thú Idle, không có nguồn `.plinko` — nên `NeighborBoost` scope Plinko của Cáo không có đích, R6.2), **WHEN** Shelf đăng ký modifier, **THEN** chỉ xét modifier đăng ký từ hiệu ứng `PetEffect` của thú Hỗ trợ (`sourceId` thuộc hiệu ứng của Mèo Hoàng Gia hoặc của Cáo; modifier combo nhóm của Shelf, R4.2, không tính vào các vế đếm này): từ hiệu ứng của Mèo Hoàng Gia có đúng 3 modifier tầng Synergy, mỗi modifier giá trị +1.8, nhắm nguồn `.idle` của Ếch, Công và Mèo Chiêu Tài; từ hiệu ứng của Cáo có 0 modifier; không modifier nào từ hiệu ứng Hỗ trợ nhắm Mèo Hoàng Gia, Cáo hay bất kỳ nguồn `.plinko` nào; và trong **mọi** modifier Shelf đăng ký (kể cả combo nhóm) không có modifier tầng Boost, Prestige hay Global.
- `AC-15 [EditMode]` (test của Shelf) **GIVEN** kệ 5 ô, Cáo Khăn Len ở ô 2 giữa Thỏ (ô 1) và Vịt Gỗ (ô 3), **WHEN** Shelf đăng ký modifier, **THEN** chỉ xét modifier có `sourceId` thuộc hiệu ứng `NeighborBoost` của Cáo (modifier combo nhóm của Shelf, R4.2, không tính): có đúng 1 modifier tầng Synergy +1.2 nhắm nguồn `.plinko` của Thỏ, không modifier nào từ hiệu ứng của Cáo nhắm Vịt; **WHEN** Cáo chuyển sang ô 1 cạnh Thỏ ở ô 2, **THEN** vẫn đúng 1 modifier từ hiệu ứng của Cáo (một ô kề); ở cả hai bố cục, trong mọi modifier Shelf đăng ký (kể cả combo nhóm) không có modifier tầng Boost, Prestige hay Global.

**Roster và phát hành (R8, R9)**
- `AC-16 [EditMode]` **GIVEN** catalog mặc định, **WHEN** đọc toàn bộ, **THEN** có đúng 13 bản ghi với `id`, nhóm, phẩm cấp, vai, `weight`, loại và `magnitude` hiệu ứng, cờ khác khớp bảng R8 và bảng knob theo từng thú; đúng 7 thú có `releaseTier = Mvp`: `pet.woody_duck`, `pet.bouncy_bunny`, `pet.cozy_fox`, `pet.rainbow_candy`, `pet.dreamy_frog`, `pet.lucky_cat`, `pet.chubby_king_cat`; mỗi nhóm có đúng 1 Thường Idle và 1 Thường Plinko; đúng 1 thú Huyền Thoại.
- `AC-17 [EditMode]` **GIVEN** catalog mặc định, **WHEN** xét từng thú, **THEN** không thú nào vừa có `role_idle_share` > `RC.plinkoRoleIdleShare` vừa có hiệu ứng Plinko, và không hiệu ứng nào nhắm toàn cục (mọi `GroupLeader`/`NeighborBoost` có đích là thú cụ thể) — Pillar 3 bằng cấu trúc.
- `AC-18 [EditMode]` **GIVEN** catalog mặc định, `PC.buildTier` = `Mvp`, **WHEN** nạp, **THEN** danh sách đã phát hành là đúng 7 thú MVP; `TryGetPet("pet.dango_trio")` trả "không tìm thấy"; pool `common` = {`pet.woody_duck`, `pet.dreamy_frog`}, `rare` = {`pet.bouncy_bunny`, `pet.lucky_cat`}, `epic` = {`pet.cozy_fox`, `pet.rainbow_candy`}, `legendary` = {`pet.chubby_king_cat`}; **WHEN** `PC.buildTier` = `V1_0`, **THEN** 13 thú và pool có 6 / 3 / 3 / 1 thú.
- `AC-19 [EditMode]` **GIVEN** `PC.buildTier` = `Mvp`, `pet.lucky_cat` có `deprecated = true`, `inGachaPool = false`, **WHEN** nạp, **THEN** `TryGetPet("pet.lucky_cat")` trả bản ghi đầy đủ, pool `rare` = {`pet.bouncy_bunny`}; **WHEN** đặt `inGachaPool = true` cho thú đó, **THEN** validator có đúng 1 lỗi; **WHEN** thay vào đó `pet.chubby_king_cat` bị `deprecated`, **THEN** pool `legendary` là danh sách rỗng, không exception, validator có đúng 1 cảnh báo "pool rỗng".

**Ngân sách và validator (R10, R11)**
- `AC-20 [EditMode]` **GIVEN** catalog mặc định và knob mặc định của Economy, Plinko, **WHEN** validator tính `budget_ratio` trên roster `PC.balanceRosterTier`, **THEN** với mọi thú |ratio − 1| ≤ `PT.budgetTolerance` (0.25), và (± 0.001): Hamster 1.000, Thỏ 1.004, Bánh Flan 0.998, Thiên Nga 1.004, Mèo Chiêu Tài 0.982, Kẹo Bảy Màu 1.235, Cáo 1.000, Mèo Hoàng Gia 0.966, mọi thú Idle 1.000.
- `AC-21 [EditMode]` **GIVEN** catalog mặc định, **WHEN** đặt `magnitude` của Thỏ = 0.05 (trong `effectLimits`), **THEN** `budget_ratio` của Thỏ ≈ 2.952 và validator có đúng 1 cảnh báo ngân sách, 0 lỗi.
- `AC-22 [EditMode]` **GIVEN** catalog mặc định, `PC.buildTier` = `Mvp`, **WHEN** validator tính `support_power_estimate` của Mèo Hoàng Gia, **THEN** kết quả = 14.49 (roster `V1_0`), không phải 2.61 (roster MVP).
- `AC-23 [EditMode]` **GIVEN** catalog mặc định và knob mặc định, **WHEN** chạy validator, **THEN** 0 lỗi và 0 cảnh báo (`channel_parity` trong `PT.parityBand`, `collection_spins_estimate` trong `PT.collectionSpinsBand`, tổng `FlatPerCoin` 0.086 ≤ `EC.plinkoFlatPerCoinCap`).
- `AC-24 [EditMode]` **GIVEN** catalog mặc định, **WHEN** (a) đặt `releaseTier` của `pet.proud_peacock` = `V1_1` và `PT.referenceGroupLeaderTargets` = 4, (b) khôi phục rồi đặt `EC.plinkoFlatPerCoinCap` = 0.05, **THEN** (a) có đúng 1 lỗi "GroupLeader không đủ đích" (3 < 4), (b) đúng 1 lỗi "tổng FlatPerCoin vượt trần".

**Truy cập lúc chạy và khởi động (R12)**
- `AC-25 [EditMode]` **GIVEN** Bootstrap với bước giả cho mọi pha và bước thật `pet.catalog`, catalog mặc định, **WHEN** khởi động tới `BootCompleted`, **THEN** bước `pet.catalog` có `Phase = DataLoad`, `IsCritical = true`, `RunsAfter` rỗng; `Run()` được gọi đúng 1 lần, sau mọi bước `ServiceInit` và trước mọi bước `GameplayInit`; `IsDone` = true trước khi bước `GameplayInit` đầu tiên được gọi.
- `AC-26 [EditMode]` **GIVEN** `PC.pets` có một phần tử `null` (bỏ qua validator), **WHEN** khởi động, **THEN** bước `pet.catalog` báo `IsFailed`, Bootstrap phát đúng 1 `BootFailed { stepId = "pet.catalog", phase = DataLoad }` và không bước `GameplayInit` nào được gọi.
- `AC-27 [EditMode]` **GIVEN** Catalog ở Unloaded, **WHEN** gọi `TryGetPet("pet.woody_duck")`, **THEN** trả "chưa sẵn sàng" (khác "không tìm thấy"), không exception; **GIVEN** Ready, **WHEN** gọi với `""`, `null`, `pet.khong_co`, **THEN** mỗi lần trả "không tìm thấy", không exception; gọi `pet.woody_duck` hai lần trả cùng bản ghi; Catalog không đăng ký mục nào với Save/Load và không có API công khai nào sửa dữ liệu.
- `AC-28 [Perf]` **GIVEN** Catalog Ready (catalog mặc định), **WHEN** gọi `TryGetPet` `PT.lookupBenchmarkCalls` (10,000) lần xen kẽ 13 ID hợp lệ và 1 ID lạ, cùng số lần lấy pool theo phẩm cấp, **THEN** bộ nhớ cấp phát GC trên luồng chính trong khoảng đo = 0 byte.
- `AC-29 [Perf]` **GIVEN** build WebGL development trên máy tham chiếu của dự án, catalog mặc định, **WHEN** khởi động, **THEN** thời gian từ `Run()` tới `IsDone` của bước `pet.catalog` ≤ `PT.catalogLoadMaxMs` (50 ms).

**Công thức**
- `AC-30 [EditMode]` `pet_power_units` — **GIVEN** `RC` mặc định, **WHEN** tính, **THEN** Thường w = `RC.weightMin` → 0.8; Huyền Thoại w = `RC.weightMax` → 18; Công Kiêu Hãnh → 6.6; Mèo Hoàng Gia → 15; với mỗi cặp phẩm cấp liền kề, (hệ số trên × `weightMin`) / (hệ số dưới × `weightMax`) > 1 (nhỏ nhất 1.6 ở Sử Thi/Hiếm).
- `AC-31 [EditMode]` `idle_base_units` — **GIVEN** `EC.idleCpsUnit` = 2, hệ số Sao 1, **WHEN** tính tốc độ gốc, **THEN** Vịt Gỗ 2.0 xu/s, Công 13.2, Thỏ 0.55, Mèo Hoàng Gia 0, Cáo 0; **WHEN** `RC.plinkoRoleIdleShare` = 0, **THEN** Thỏ 0; = 0.2 → Thỏ 1.1.
- `AC-32 [EditMode]` `pet_event_bonus` — **GIVEN** m = 0.017, **WHEN** `base_coin_value` = 1 / 18.679 (n = 0 / 100 với knob Economy mặc định), **THEN** 0.017 / 0.3175; **GIVEN** m = NaN, −1, +∞, **THEN** 0 (không có `SubmitIncome`); **GIVEN** m = cận trên `JackpotBonus` (10), b = 1, **THEN** 10.
- `AC-33 [EditMode]` `plinko_target_uplift` — **GIVEN** `RC` mặc định, **WHEN** tính, **THEN** Thỏ 0.12375, Kẹo 0.324, Thường w 0.8 → 0.036, Huyền Thoại w 1.2 → 0.81; với `RC.plinkoRoleIdleShare` = 0 → Thỏ 0.1375.
- `AC-34 [EditMode]` `plinko_effect_uplift` — **GIVEN** knob mặc định (`coins_per_primary` = 1.545), **WHEN** tính cho roster, **THEN** Thỏ 0.1242, Thiên Nga 0.0497, Mèo Chiêu Tài 0.1105 (0.1707 khi `includeBurstCoins = true`), Bánh Flan 0.0449, Hamster 0.0450 (± 0.0001); `BouncinessModifier` m = 1.0 → 0, m = 1.3 → 0.1714, m = 1.7 → `ET.plinkoBounceUpliftAtCapAssumed` (0.40), m = 2.0 → 0.40 (kẹp tại trần).
- `AC-35 [EditMode]` `support_power_estimate` — **GIVEN** roster `V1_0`, **WHEN** tính, **THEN** Cáo 6.0; Mèo Hoàng Gia scope Idle 14.49, scope Both 18.54; `NeighborBoost` m = 0.2 → 1.0; khi mọi thú Hoàng Gia khác bị gỡ khỏi roster → 0.
- `AC-36 [EditMode]` `budget_ratio` — **GIVEN** stub ước lượng cho một thú có target 1, **WHEN** estimate = 1 + `PT.budgetTolerance` và 1 − `PT.budgetTolerance`, **THEN** không cảnh báo; estimate vượt 1 + `budgetTolerance` một lượng bất kỳ > `ET.valueRelTolerance` → đúng 1 cảnh báo; **GIVEN** `RC.plinkoRoleIdleShare` = 1 (target thú Plinko = 0), **THEN** mỗi thú Plinko có đúng 1 lỗi "target = 0", không exception chia cho 0.
- `AC-37 [EditMode]` `channel_parity` — **GIVEN** knob mặc định, **WHEN** n_ref = 50 / 0 / 100, **THEN** 0.959 / 0.222 / 4.147 (± 0.001) và n = 50 nằm trong `PT.parityBand`; **WHEN** `RC.plinkoUpliftPerUnit` = 0.08, **THEN** 1.535 ở n = 50 và validator có đúng 1 cảnh báo cân hai kênh.
- `AC-38 [EditMode]` `collection_spins_estimate` — **GIVEN** `GachaConfig.rarityRates` mặc định (0.55 / 0.30 / 0.12 / 0.03), **WHEN** tính cho pool `V1_0` / pool MVP / một thú duy nhất với p = 1, **THEN** 59.9 / 43.4 (± 0.3) / 1.0 (± bước tích phân); **GIVEN** `GachaConfig.rarityRates` = [0.5799, 0.30, 0.12, 0.0001], **WHEN** tính cho pool `V1_0`, **THEN** hàm trả trong giới hạn tích phân, kết quả ngoài `PT.collectionSpinsBand` và validator có đúng 1 cảnh báo, không treo.

**Trình bày thẻ thú (UI Requirements)**
- `AC-39 [EditMode]` **GIVEN** `base_coin_value` = 1 và knob mặc định, **WHEN** tạo mô tả hiệu ứng cho 7 thú có hiệu ứng, **THEN** số trong mô tả là: Thỏ 0.017, Thiên Nga 0.0068, Mèo Chiêu Tài 3, Bánh Flan 0.086, Hamster `PL.autoDropBaseInterval` / 0.27 (7.41 s), Kẹo ×1.7 với trần `PL.bouncinessCap`, Mèo Hoàng Gia 180% kênh Idle nhóm Hoàng Gia, Cáo 120% kênh Plinko — trước khi định dạng; và chuỗi số sau định dạng theo quy tắc "Định dạng số trên thẻ thú" (UI Requirements, `EC.displaySigDigits` = 3) đúng bằng: Thỏ "+0.017", Thiên Nga "+0.0068", Mèo Chiêu Tài "+3", Bánh Flan "+0.086", Hamster "7.41", Kẹo "×1.7" và "0.85", Mèo Hoàng Gia "+180%", Cáo "+120%"; **WHEN** `base_coin_value` = `base_coin_value(100)` (18.679), **THEN** Thỏ "+0.318", Mèo Chiêu Tài "+56"; **WHEN** magnitude của Thỏ = cận dưới `PC.effectLimits[PegHitBonus]` (0.002) và `base_coin_value` = 1, **THEN** "+0.002"; **WHEN** định dạng giá trị 1234.5 bằng cùng quy tắc, **THEN** "1.23K" (khớp `display_format` của Economy R13).
- `AC-40 [Screenshot]` (chụp khi màn Kho của Inventory có) **GIVEN** màn Kho đang mở thẻ Mèo Hoàng Gia ở khung 9:16, **WHEN** chụp Game view, **THEN** thấy emoji 👑 trong khung màu Huyền Thoại, tên, biểu tượng kèm chữ "Hoàng Gia" và "Hỗ trợ", và dòng mô tả "+180% thu nhập Idle cho mỗi thú Hoàng Gia khác trên kệ", không chữ nào bị cắt.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| ~~**NEEDS-USER** — Kênh (`scope`) của buff Hỗ trợ.~~ **Đã đóng** — user chọn (a) (`decision-log.md`, 2026-10-21): mỗi thú Hỗ trợ buff đúng một kênh; R6.5, R8 và bảng knob theo từng thú không còn *provisional*; `scope = Both` giữ trong từ vựng, chỉ dành cho live-ops (`PetCatalog.scopeBothMinTier` = `LiveOps`, lỗi validator R11.1, AC-8 (h)/(i)). Nội dung cũ: Cáo Khăn Len `NeighborBoost` scope **Plinko**, Mèo Hoàng Gia `GroupLeader` scope **Idle**; `scope = Both` có trong từ vựng nhưng roster không dùng. Lựa chọn: (a) mỗi thú Hỗ trợ buff đúng một kênh như hiện tại — đọc thẻ dễ, giữ Pillar 3 (thủ lĩnh không mạnh cả hai kênh), Cáo tạo ràng buộc vị trí riêng cho kênh Plinko; (b) Mèo Hoàng Gia scope Both, `magnitude` giảm từ 1.8 còn ≈ 1.45 để giữ ngân sách 15 đơn vị (1.45 × 10.3) — "Huyền Thoại" sướng hơn (Pillar 2) nhưng thành đáp án an toàn cho cả hai kênh; (c) cả hai thú Hỗ trợ scope Both với magnitude giảm tương ứng — linh hoạt nhất, nhưng thú Hỗ trợ gần như luôn đáng một ô (phá Pillar 3); (d) bỏ `Both` khỏi từ vựng v1.0 — đơn giản nhất cho Shelf, khó mở lại khi live-ops cần. **Khuyến nghị: (a)**, giữ `Both` trong từ vựng để dành. R6.5, R8, bảng knob theo từng thú và AC-14, AC-15, AC-35, AC-39, AC-40 đang viết theo (a). | User | 2026-10-21 (đã đóng) |
| Assumed: `scope = Both` được giữ trong từ vựng và chỉ mở cho thú bậc `LiveOps` qua knob `PetCatalog.scopeBothMinTier` (lỗi validator khi thú bậc thấp hơn dùng nó), thay vì xóa khỏi enum (khó mở lại khi live-ops cần) hay chỉ ghi chú (không kiểm được). Shelf vẫn thi hành `Both`. | game-designer | 2026-10-21 |
| Assumed (PC3-B1): số trong mô tả hiệu ứng trên thẻ thú (< 1000) dùng `EconomyConfig.displaySigDigits` chữ số có nghĩa, **làm tròn gần nhất**, bỏ số 0 thừa; ≥ 1000 theo `display_format` của Economy R13 (UI Requirements). Chọn thay cho "bỏ vế sau định dạng khỏi AC-39" vì giữ được yêu cầu "không bao giờ +0" kiểm được ngay, và giữ số xu thật thay vì đổi sang % (thẻ vẫn cập nhật theo `baseCoinValue`). Làm tròn (không cắt) vì đây là số mô tả, không phải số dư, và tránh "0.00679" do sai số dấu phẩy động khi cắt. Đề xuất Economy Core đưa quy tắc này vào R13 khi mở lại (không chặn; Catalog dùng lại knob, không thêm knob). | game-designer + Economy Core owner | 2026-10-28 |
| Assumed: `rarity_coef[common]` = 1 là **hằng số**, `PetRarityConfig.rarityCoefficients` chỉ giữ Hiếm / Sử Thi / Huyền Thoại (đổi hệ số Thường tương đương đổi `idleCpsUnit` của Economy, nên một knob là đủ; C1[1]). | game-designer | 2026-10-21 |
| Assumed: `PetCatalog.groupRuleMinTier` = `V1_0` (MVP cố ý dùng 7 thú FirstGDD — D5, concept MVP chỉ cần một combo nhóm/kề trên kệ 3 ô — và nhóm Đồ Ngọt MVP chỉ có Kẹo Bảy Màu; thêm thú vào MVP chỉ để thỏa luật là phình phạm vi; C1[1]). | game-designer | 2026-10-21 |
| Assumed: giữ Kẹo Bảy Màu `BouncinessModifier` = 1.7 (hệ số nhỏ nhất chạm `bounciness_cap`, minorChoice của brief) dù `budget_ratio` = 1.235 sát cận 1.25. Con số dựa trên `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (0.40) chưa đo; ba con số ngân sách (mục tiêu +32% ≈ 26, ước tính +40% ≈ 27.6, biên ≤ 30.7 × `baseCoinValue`/s) ghi giống `plinko-board.md` `raw_rate_ceiling` (C2[11], R8). Đo lại bằng Plinko AC-13c / AC-30; nếu tăng thu nhập thực ở trần > 0.405 thì hạ hệ số dưới trần (Kẹo không còn chạm trần) hoặc chấp nhận và nới `budgetTolerance` cho riêng loại này. Liên quan quyết định trần **mềm** (a) của kênh Plinko (`plinko-board.md`, đã đóng): phần vượt của Kẹo được Economy cân theo `raw_rate_ceiling` từng cấu hình, Plinko AC-13c có trần test riêng. | game-designer + Plinko Board | 2026-11-11 |
| ~~`PetBalanceTargets.bounceUpliftAtCapAssumed` chuyển sang `EconomyBalanceTargets`~~ **Đã đóng** (C2[12]): `economy-core.md` đã có `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (0.40, 0.2–0.55); Formulas `bounce_uplift_at_cap`, AC-34, Dependencies, Cross-References đọc knob đó; knob của `PetBalanceTargets` đã xóa. Còn lại cho `plinko-board.md` (output range `raw_rate_ceiling`, Cross-References, AC-13c vẫn ghi `PetBalanceTargets.bounceUpliftAtCapAssumed`) — đổi tên tham chiếu khi Plinko mở lại. | Plinko Board owner / bước Sync | 2026-10-28 |
| Combo Kẹo Bảy Màu + Mèo Chiêu Tài (p_jackpot ≈ 10.9% → ≤ 20%, thưởng Mèo tăng tối đa ≈ 1.83 lần) không có trong mô hình ngân sách tuyến tính. Đo trong mô phỏng bảng tính chung; nếu combo lai này thống trị kênh Plinko thì thêm hệ số tương tác vào validator. | game-designer | 2026-11-11 |
| Mô phỏng bảng tính chung (Economy + Catalog + Shelf) để chốt `rarityCoefficients`, `plinkoUpliftPerUnit`, `plinkoRoleIdleShare` và độ lớn 13 thú. Rủi ro theo dõi (không phải câu hỏi mở): `idleCpsUnit` là hằng số trong tiệm (Economy (A)), nên `channel_parity` chỉ trong `parityBand` khi n ≈ 44–59 (0.222 ở n = 0, 1.286 ở n = 60, 4.147 ở n = 100); mô phỏng phải cho thấy thú phẩm cấp cao và Sao bù đủ cho kênh Idle cuối tiệm, nếu không thì chỉnh `plinkoUpliftPerUnit` / `rarityCoefficients` hoặc đường giá của Economy. | game-designer | 2026-11-11 |
| Assumed: tên 6 thú mới (Hamster, Bánh Dango, Bánh Flan, Cupcake, Thiên Nga, Công) và tên tiếng Anh của chúng — đổi tự do vì mọi hệ khóa theo `id` (R1.2). | game-designer | 2026-10-21 |
| Assumed: giới hạn `effectLimits` mới cho `FlatPerCoin` [0.01, 0.5], `AutoDropRate` [0.05, 1.0], `BouncinessModifier` [1.0, 2.0] (bao quanh giá trị roster, dưới trần của Economy/Plinko); bốn loại còn lại giữ khoảng đã có ở Formulas. | game-designer | 2026-10-21 |
| Assumed: `PetCatalog.flavorMaxChars` = 80 (một câu tiếng Việt vừa thẻ thú ở khung 9:16); chốt lại khi có board Penpot thẻ thú. | game-designer + art-director | 2026-10-28 |
| Assumed: `PetBalanceTargets.catalogLoadMaxMs` = 50 ms (13 SO nhỏ, ≤ 2.5% ngân sách khởi động 2.0 s của Bootstrap) và `lookupBenchmarkCalls` = 10,000. | game-designer | 2026-10-21 |
| Assumed: bước `pet.catalog` là cốt lõi (`IsCritical = true`) — không có chế độ chơi không thú; lỗi nạp → BootFailed, "Thử lại" (Bootstrap B4). Thêm trạng thái Loading / Failed vào vòng đời Catalog để khớp hợp đồng bước. | game-designer | 2026-10-21 |
| Assumed: hậu tố cue `Gacha.RarityReveal.<phẩm cấp>` của Audio dùng đúng ID phẩm cấp `common` / `rare` / `epic` / `legendary` (một nguồn ID, không bảng ánh xạ). | game-designer + Audio | 2026-10-21 |
| Tên khóa nguồn của thú trên kệ (đề xuất `shelf.slot<i>.idle` / `.plinko`, R6.3) và cách kệ có hai bản cùng một thú hay không — Shelf & Synergy chốt. | Shelf & Synergy GDD | 2026-11-04 |
| Màu phẩm cấp (art bible) và cập nhật board Penpot còn 7 thú FirstGDD với số cũ; board thẻ thú dùng chung cho Gacha / Kho / Kệ (UX flag). | art-director | 2026-10-28 |
| ADR: cách nạp Catalog (tham chiếu trực tiếp SO, không Addressables), bản địa hóa tên/flavor (Unity Localization hay bảng riêng), bảng tra ID không cấp phát, cơ chế đăng ký `IBootStep` (dùng chung ADR khởi động của Bootstrap). | game-adr | 2026-10-28 |
| Cho bước Sync (không sửa trong bước này): systems index — hàng Pet Catalog thêm UX flag (thẻ thú dùng chung), Key deps đã có Bootstrap; registry — ứng viên entities 13 thú, constants `rarity_coefficients` (1 / 2.5 / 6 / 15), formulas `pet_power_units`, `idle_base_units`, `pet_event_bonus`; thêm `pet-catalog.md` vào `referenced_by` của `auto_drop_interval` (Hamster, AC-13, AC-39); giữ `pet-catalog.md` trong `referenced_by` của `effective_bounciness`, `bounciness_cap`, `max_scored_peg_hits`, `jackpot_burst_count`, `base_coin_value`; constant `idle_cps_unit`, `plinko_flat_per_coin_cap`, `synergy_tier_cap` (C1[6]) nhận `pet-catalog.md` vào `referenced_by`. Revision 5: systems index — hàng Pet Catalog không còn NEEDS-USER `scope` (đã đóng theo (a)); registry — nếu đăng ký `bounce_uplift_at_cap`, source là `economy-core.md` (`EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` 0.40), referenced_by `pet-catalog.md`, `plinko-board.md`; `display_format` (nếu có trong registry) nhận `pet-catalog.md` vào `referenced_by`. | bước Sync | 2026-10-21 |
