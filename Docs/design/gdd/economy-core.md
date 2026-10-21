# Economy Core (Lõi Kinh Tế)

> **Status**: In Review
> **Author**: Maris-LTD + Claude (game-designer)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 3 — Meaningful Constraints (hai kênh thu nhập, không nâng chỉ số tĩnh); Pillar 4 — Cozy Pranks (không hệ nào trừ xu trong ví); hỗ trợ Pillar 2 (gacha chỉ tiêu xu trong game)
> **Resolves review**: BLOCKING §7 Formulas (công thức thu nhập, thứ tự buff, Multiplier_Synergy, xu khởi đầu), BLOCKING §5.5/§8 (giá cố định), phần đường giá của BLOCKING §7.1 (`Docs/design/gdd/reviews/first-gdd-review-2026-10-21.md`)
> **Decisions**: D1–D6 của user (2026-10-21, `Docs/production/session-logs/decision-log.md`)
> **Revision 2026-10-21** (đợt 2, theo `Docs/production/session-logs/gdd-consistency-2026-10-21-run2.json` C2[0], C2[10], C2[12], C2[15] và quyết định sau design run 2 trong `decision-log.md`): kênh Plinko là trần **mềm** — bỏ mọi chỗ coi ≈ 20 × `baseCoinValue` là trần cứng, cân bằng theo `raw_rate_ceiling` từng cấu hình của Plinko (R5.5, công thức `plinko_rate_model`, knob `EconomyBalanceTargets.plinkoRawRateCeilingAssumed`, AC-47); `idleCpsUnit` là hằng số trong tiệm (A) — bỏ nhãn provisional ở R11.2, `idle_cps`, `seconds_per_spin`, ghi rủi ro `channel_parity`; knob dự phòng `PlinkoConfig.baseCoinValue` = 1 chỉ cho test, khớp Plinko; nhận `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` từ Pet Catalog; dọn ghi chú hai chiều, Cross-References và Open Questions đã lỗi thời.
> **Fix round 1 (2026-10-21, `reviews/economy-core-review-2026-10-21.md` EC-R1-B1)**: theo Bootstrap & Scene Flow Revision 4 (B6.5) — Idle chỉ cộng khi `IsSimulationRunning` của Bootstrap đúng (dt = 0 ở mọi khung khác); thêm trạng thái **Ready** sau `Run()` (API hoạt động, không cộng Idle) → Running khi `Interactive` hoặc khi `Resumed` mà `IsSimulationRunning` đúng; `Resumed` về `Booting.Curtain` giữ Ready (R6.2–R6.3, R16.3–R16.4, States, `idle_accrual`, Interactions/Dependencies hàng Bootstrap, "Hai chiều", AC-22, AC-46).
> **Revision 2026-10-21** (đợt 3, `gdd-consistency-2026-10-21-run3.json` C3[6], C3[7] và quyết định user (b)): tiêu thụ `AdClosed` bù Idle (R6.4, `ad_idle_compensation`, AC-48); `CoinResolved(Overflow)` có thể đến ở Ready/Paused (R10.4, States, AC-49); Pet Catalog phụ thuộc mềm vào Economy.
> **Revision 2026-10-21** (đợt 4, `gdd-consistency-2026-10-21-run5.json` C5[13]): thêm hàm thuần `PreviewIdleCps(plan)` / `GetSourceIdleCps(sourceKey)` (R7.5, `preview_idle_cps`, AC-50, AC-51), `TrySpend(ShopPurchase)` mở ô kệ; đồng bộ hàng Shelf, Gacha, Inventory (Interactions, Dependencies) với `shelf-and-synergy.md`, `gacha-machine.md`, `inventory.md`.
> **Fix round 1b (2026-10-21, EC-1)**: `ResetForNewShop` phát thêm `WalletChanged(ShopReset)` và `SpinPriceChanged` trước `ShopEconomyReset`; định nghĩa payload (R12.2, R14, R15, AC-36).
> [CD-GDD-ALIGN] skipped (lean)
> game-designer / qa-lead không được gọi riêng (workflow không cho subagent): Formulas và Acceptance Criteria do game-designer viết trực tiếp, chưa qua review chuyên môn.

## Summary

Economy Core là "két tiền" và "bảng giá" của tiệm: nó giữ ví (Xu, sau này thêm Tem Tiệm), cộng tiền từ hai kênh — CPS của thú trên kệ và từng đồng xu Plinko — qua một chuỗi buff có thứ tự và có trần, và đặt mọi giá theo một đường duy nhất là giá quay trứng. Người chơi cảm nhận nó qua con số xu luôn chảy, số "+n" bay lên đúng bằng số nhận được, và giá luôn có ý nghĩa dù tiến độ đã tăng gấp trăm lần. Game cần nó để hai cách chơi (chạm Plinko / để kệ tự chạy) cùng đáng giá mà không cái nào thống trị (Pillar 3), và để không hệ nào được lấy xu khỏi ví người chơi (Pillar 4).

> **Quick reference** — Layer: `Foundation` (systems index; phụ thuộc Plinko qua `CoinResolved`, `game-systems` xem lại tầng) · Priority: `MVP` · Key deps: `Bootstrap & Scene Flow` (Hard: bước `GameplayInit` qua hợp đồng pha, `Interactive`/`Paused`/`Resumed`, truy vấn `IsSimulationRunning`), `Save/Load` (soft, VS), `Plinko Board` (sự kiện `CoinResolved`; Economy Core cài đặt `IBaseCoinValueSource` của Plinko); depended on by Gacha, Shelf & Synergy, HUD, Feedback & Juice, Boosts, Shop, Offline, Rewarded Ads, Prestige, Neighbor Pranks

## Overview

Economy Core là lớp kinh tế mà mọi hệ khác cắm vào. Nó giữ **ví đa tiền tệ** (Xu ở MVP, Tem Tiệm từ Alpha), chạy **Income Pipeline** biến mọi nguồn thu thành xu qua hai kênh — **Idle** (CPS từ thú trên kệ, cộng liên tục) và **Plinko** (mỗi đồng xu Plinko giải quyết, gồm cả tự thả, mưa xu và Bùng Xu) — và sở hữu **đường giá neo**: giá quay Gacha tăng theo số lần quay trong tiệm, mọi giá và phần thưởng khác là một hệ số nhân với giá quay hiện tại, còn giá trị xu gốc của Plinko (`baseCoinValue`) tăng chậm hơn theo cùng đường đó. Người chơi không thao tác trực tiếp với hệ này; họ thấy nó qua số xu trên header, CPS, giá trên nút vặn trứng và trong Tiệm, và qua con số "+n" bay lên sau mỗi đồng xu.

Thiếu Economy Core, mỗi hệ sẽ tự cộng tiền theo một thứ tự riêng: buff nhân chồng nhân thành lạm phát (lỗi "Mèo Hoàng Gia x2 mọi thứ"), giá cố định mất ý nghĩa khi giá quay tăng hàm mũ, và không có chỗ nào chặn được việc một hệ trừ xu khỏi ví người chơi (Pillar 4). Hệ này chỉ định nghĩa **hợp đồng** (kênh, tầng, trần, họ công thức giá); giá trị buff của từng thú thuộc Pet Catalog / Shelf & Synergy, hệ số giá của từng món thuộc hệ bán món đó (D1). Upstream duy nhất là Plinko Board qua sự kiện `CoinResolved`; chiều ngược lại Economy Core chỉ cài đặt interface `IBaseCoinValueSource` (thuộc hợp đồng của Plinko) để Plinko tự đọc `baseCoinValue` lúc giải quyết xu (mô hình kéo, R10).

Mọi con số trong tài liệu này là **tạm** cho tới khi có mô phỏng bảng tính sau Pet Catalog và Shelf & Synergy; GDD sẽ được mở lại lần nữa khi thiết kế Star Progression và Prestige (systems index).

## Detailed Design

### Core Rules

Mọi tên trong `backtick` là knob ở `EconomyConfig` (runtime) hoặc `EconomyBalanceTargets` (chỉ test/mô phỏng), xem Tuning Knobs. Không có con số nào nằm trong logic.

**R1. Ví đa tiền tệ (D5)**
1. Ví lưu một bảng `currencyId → double`. Mỗi tiền tệ là một `CurrencyDefinition` trong `currencies` (id chuỗi bất biến, tên hiển thị, biểu tượng, `releaseTier`, `resetsOnNewShop`).
2. MVP chỉ đăng ký `currency.coin` (Xu, `resetsOnNewShop = true`). `currency.shop_stamp` (Tem Tiệm, `resetsOnNewShop = false`) được đăng ký từ Alpha cùng Prestige; thêm tiền tệ không đổi định dạng lưu.
3. Số dư luôn hữu hạn, ≥ 0 và ≤ `walletSafeCap`. Ví giữ phần lẻ (CPS 2.5/s không bị mất); chỉ phần hiển thị làm tròn xuống (R13).
4. Lúc bắt đầu game mới, Xu = `startingCoins`; các tiền tệ khác = 0.

**R2. Cộng vào ví — `Add(currencyId, amount, reason)`**
1. Dùng cho mọi khoản cộng không đi qua Income Pipeline: thu nhập offline, quà quảng cáo, Tem Tiệm từ prestige, hoàn tiền. Thu nhập Idle/Plinko đi qua pipeline (R4) rồi mới gọi `Add` nội bộ.
2. `amount` âm, 0, NaN hoặc vô cực → bị từ chối (trả `false`, không đổi số dư, không phát sự kiện). `currencyId` chưa đăng ký → từ chối.
3. Nếu số dư mới vượt `walletSafeCap` thì kẹp tại `walletSafeCap`.
4. `reason` thuộc bảng faucet (R14); mỗi lần cộng thành công phát `WalletChanged`.

**R3. Chi tiêu — chỉ người chơi chủ động (D5, Pillar 4)**
1. Cách duy nhất làm giảm số dư là `TrySpend(currencyId, amount, reason, playerAction)` (kể cả `TryPaySpin`, vốn gọi `TrySpend`, R8.3) và `ResetForNewShop(playerAction)` (R12). Không có API trừ, đặt số dư hay "phạt" nào khác.
2. `TrySpend` chỉ chạy khi có ngữ cảnh hành động người chơi (một cú bấm UI đã xác nhận: vặn trứng, mua trong Tiệm, trả tiền kẹo v1.1). Thiếu ngữ cảnh đó → `false`, không đổi số dư. Cơ chế cụ thể của "ngữ cảnh hành động" → ADR.
3. `TrySpend` là nguyên tử: số dư ≥ `amount` → trừ đúng `amount`, trả `true`, phát `WalletChanged`; ngược lại trả `false`, số dư giữ nguyên, phát `SpendRejected` (để HUD/Feedback báo "chưa đủ xu"). So sánh dùng giá trị double chính xác, không dùng giá trị hiển thị.
4. `amount` không hợp lệ (âm, 0, NaN, vô cực) → `false`, không sự kiện.
5. Neighbor Pranks (v1.1) không được trừ ví. Nếu cần "giữ tạm xu", nó chỉ được chuyển hướng một phần **thu nhập mới sinh** vào một khoản giữ tạm qua interface dự trữ ở R4.9; xu đã nằm trong ví không bao giờ bị lấy.

**R4. Income Pipeline theo tầng (D3)**
1. Có hai **kênh**: `Idle` (tốc độ liên tục, xu/giây) và `Plinko` (khoản rời rạc, mỗi sự kiện một khoản).
2. Mỗi khoản thu có một **nguồn** (`sourceKey`, ví dụ `shelf.slot2`, `plinko.coin`) và một giá trị gốc. Giá trị cuối của nguồn `s` trên kênh `c` = `(gốc_s + cộng phẳng_s) × M_Synergy(s) × M_Boost(c) × M_Prestige(c) × M_Global(c)` (công thức `channel_income`).
3. **Modifier** là bản ghi `{ sourceId, channel (Idle | Plinko | Both), tier, value, target (Channel | sourceKey), isTemporary }`. Hệ đăng ký modifier tự gỡ nó (`Unregister(sourceId)` gỡ mọi modifier của `sourceId`), kể cả khi hết hạn (đồng hồ do Time & Timers / hệ đăng ký giữ, không do Economy Core).
4. Các **tầng**, theo thứ tự áp: `Base` (cộng phẳng), `Synergy`, `Boost`, `Prestige`, `Global`. Trong một tầng nhân, các `value` **cộng** lại: `M_tầng = min(1 + Σ value, trần tầng)`; giữa các tầng thì **nhân**. Mỗi tầng có trần riêng (`synergyTierCap`, `boostTierCap`, `prestigeTierCap`, `globalTierCap`); cộng phẳng kênh Plinko có trần `plinkoFlatPerCoinCap`.
5. `value` là phần thưởng thêm: 1.0 = +100% (tức "x2"). Không có modifier âm: `value` âm, NaN hoặc vô cực được coi là 0 (trung tính, nhân 1 / cộng 0), đồng nhất với Plinko R5.3.
6. Đơn vị cộng phẳng: kênh Idle tính theo bội số `idleCpsUnit` (CPS), kênh Plinko tính theo bội số `baseCoinValue` mỗi xu — để cộng phẳng tự co giãn theo tiến độ.
7. **Ai được vào tầng nào** (D3) — Economy Core từ chối đăng ký sai tầng, kiểm bằng tiền tố `sourceId` theo `tierOwnerPrefixes`:

| Tầng | Nguồn được phép | Hệ đăng ký (tiền tố `sourceId`) | Đích | Tạm thời? | Trần | Có từ |
|---|---|---|---|---|---|---|
| Base — gốc | Sản lượng gốc của thú trên kệ (phẩm cấp × trọng số × `role_idle_share` × `idleCpsUnit` × hệ số Sao, tức `idle_base_units` × `idleCpsUnit` × hệ số Sao theo `pet-catalog.md`); `rawValue` của mỗi xu Plinko; thưởng sự kiện của thú kênh Plinko (Thỏ theo `PegHit` được tính, Mèo Chiêu Tài theo `JackpotHit`) | Shelf & Synergy (`shelf.`), Plinko qua `CoinResolved` (Economy Core tự chuyển) | Nguồn | Không | — (Pet Catalog / Star giữ trần của mình) | MVP |
| Base — cộng phẳng | Hiệu ứng "+N" của thú (Idle: nhắm một thú; Plinko: nhắm nguồn `plinko.coin`, tức "+N mỗi xu") | Shelf & Synergy (`shelf.`) | Nguồn | Không | `plinkoFlatPerCoinCap` (Plinko) | MVP |
| Synergy | Combo nhóm, ô kề nhau, thủ lĩnh nhóm (Mèo Hoàng Gia), Cáo | Shelf & Synergy (`shelf.`) | Nguồn hoặc kênh | Không | `synergyTierCap` | MVP |
| Boost | Trà Sốt Dẻo, boost nhận qua quảng cáo | Boosts (`boost.`), Rewarded Ads (`ad.`) | Kênh | Luôn tạm thời | `boostTierCap` | VS / Alpha |
| Prestige | Nâng cấp mua bằng Tem Tiệm | Prestige (`prestige.`) | Kênh | Không | `prestigeTierCap` | Alpha |
| Global | Dự trữ cho sự kiện live-ops; MVP rỗng | Chỉ `event.` | Kênh | Tùy sự kiện | `globalTierCap` | sau v1.1 |

   Thú và Shelf **không bao giờ** được đăng ký vào Boost, Prestige hay Global: đây là chốt chặn cấu trúc chống lại kiểu "một thú x2 mọi thứ" (Pillar 3).
8. `Multiplier_Synergy` của FirstGDD §7 chính là `M_Synergy(s)` (công thức `multiplier_synergy`), tính **theo nguồn**: cộng mọi `value` tầng Synergy nhắm vào nguồn `s` hoặc vào kênh của `s`, rồi kẹp tại `synergyTierCap`.
9. *(v1.1, dự trữ)* Interface "giữ tạm thu nhập" cho Neighbor Pranks: một phần thu nhập **mới** của một kênh có thể được chuyển vào một khoản giữ tạm thay vì vào ví và luôn trả lại được. Tỷ lệ, trần và đường gỡ thuộc GDD Neighbor Pranks; Economy Core chỉ đảm bảo nguồn là thu nhập mới, không phải số dư.

**R5. Kênh Plinko**
1. Economy Core nghe `CoinResolved { coinId, origin, bucketIndex | none, multiplier, rawValue, scoredPegHits, reason }` của Plinko Board, với `reason ∈ {Bucket, Stuck, Overflow}`. **Mọi** `origin` (`Manual`, `Auto`, `Rain`, `JackpotBurst`) và **mọi** `reason` (`Bucket` vào hũ, `Stuck` kẹt, `Overflow` xu loạt trả ngay không qua mô phỏng — tràn hàng đợi hoặc xả hàng đợi loạt khi `Paused`, Plinko R9.3/R11.3) đi qua kênh Plinko với `rawValue` làm giá trị gốc; Economy Core không tính lại `rawValue`.
2. Modifier được đánh giá **tại thời điểm giải quyết**: xu thả trước khi bật Trà nhưng vào hũ khi Trà còn hiệu lực thì được x2; xu vào hũ sau khi Trà hết thì không.
3. Thưởng sự kiện của thú kênh Plinko đi vào bằng `SubmitIncome(channel = Plinko, sourceKey, baseAmount)` do Shelf & Synergy gọi; khoản này đi qua cùng các tầng của kênh Plinko (không cộng phẳng mỗi xu). `baseAmount` không hữu hạn hoặc ≤ 0 → bỏ qua.
4. Mỗi khoản Plinko được cộng vào ví ngay trong cùng khung và phát `IncomeCredited { channel, coinId, bucketIndex, origin, reason, sourceKey, baseAmount, creditedAmount }` đúng một lần. `coinId` và `bucketIndex` sao chép nguyên từ `CoinResolved` (để Feedback & Juice đặt và gộp số bay trên đúng hũ); khoản đến từ `SubmitIncome` có `coinId` = none và `bucketIndex` = none. Số bay "+n" hiển thị `creditedAmount` (giá trị sau modifier — trả lời Open Question của Plinko).
5. **Không có trần cứng về thông lượng** (quyết định Plinko (a), `decision-log.md`): Economy Core không kẹp, không gộp bớt và không bỏ khoản Plinko nào vì tổng thu kênh Plinko mỗi giây cao. Thông lượng thô do Plinko quyết định và chỉ có trần **mô hình** `raw_rate_ceiling` theo cấu hình (auto-drop, độ nảy hiệu lực; `plinko-board.md`). Economy Core giữ kênh trong tầm bằng cân bằng: mô hình `plinko_rate_model` (Formulas) với giả định `EconomyBalanceTargets.plinkoRawRateCeilingAssumed`, cùng các trần tầng và `plinkoFlatPerCoinCap` sẵn có.

**R6. Kênh Idle**
1. Shelf & Synergy đăng ký mỗi thú trên kệ là một nguồn Idle bằng `RegisterIncomeSource(sourceKey, baseRate)` và gỡ khi thú rời kệ. Economy Core tính `idle_cps` (Formulas) và chỉ tính lại khi có đăng ký/gỡ nguồn hoặc modifier (cờ "bẩn"), không tính lại mỗi khung.
2. Mỗi khung mà Economy Core ở Running **và** truy vấn `IsSimulationRunning` của Bootstrap & Scene Flow đúng (B6.5), ví cộng `idle_cps × min(dt, maxFrameDelta)` (công thức `idle_accrual`). Ở mọi khung khác dt = 0: không cộng Idle, kể cả khi một sự kiện vòng đời bị lỡ (truy vấn là chốt chặn cuối, đọc mỗi khung, không cấp phát). Không có "tick" theo giây: tiền chảy liên tục.
3. **Idle chỉ chạy khi vòng đời Bootstrap là `Running`** (Bootstrap B6.5). Không có Idle nào dưới rèm (`Booting.Steps`, `Booting.Curtain`), ở `BootFailed` hay `Quitting`:
   - sau `Run()` (pha `GameplayInit`, dưới rèm) Economy Core ở **Ready** — API hoạt động nhưng không cộng Idle;
   - Bootstrap phát `Interactive` (rèm kéo xong, lần đầu vào Running) → Economy Core vào Running và bắt đầu cộng Idle từ khung kế tiếp;
   - Bootstrap phát `Paused {reason}` (cùng tín hiệu làm Plinko dừng — `reason` = `Hidden` khi tab trình duyệt ẩn, `Background` khi app xuống nền; Bootstrap B6.2; có thể phát ở `Booting.Curtain` hoặc `Running`, B6.4) → Economy Core vào Paused, dừng cộng Idle;
   - Bootstrap phát `Resumed {reason, awaySeconds}` → Economy Core **đọc `IsSimulationRunning`** thay vì coi `Resumed` là "chạy lại": đúng (vòng đời về `Running`) → Running; sai (vòng đời về `Booting.Curtain`, B6.9) → Ready, chờ `Interactive`.
   Economy Core không dùng `awaySeconds` (thuộc Offline Earnings); delta khung đầu sau khi vào Running vẫn bị kẹp bởi `maxFrameDelta`. Chuyển giữa Tab 1/2/3 không dừng cộng Idle. Thu nhập trong thời gian tạm dừng/vắng mặt thuộc Offline Earnings; thời gian dưới rèm không phải thu nhập của ai (người chơi chưa vào tiệm).
4. **Bù thời gian xem ad** (quyết định user (b), Bootstrap B6.10.5): khi nhận `AdClosed {adSeconds}` mà `IsSimulationRunning` đúng, `Add(currency.coin, ad_idle_compensation, AdIdleCompensation)` (công thức `ad_idle_compensation`) = `idle_cps` × số giây chưa mô phỏng, kẹp bởi `adCompensationMaxSeconds`. Chỉ kênh Idle: **không bù kênh Plinko**. Không cộng hai lần với `maxFrameDelta`: trong lúc `IsAdOpen` đúng, R6.2 vẫn cộng bình thường và Economy Core cộng dồn `adSimulatedSeconds` = Σ `min(dt, maxFrameDelta)` đã cộng; chỉ phần `adSeconds` chưa nằm trong đó được bù (phần delta bị `maxFrameDelta` cắt không được bù ở đường nào khác). `AdClosed` khi `IsSimulationRunning` sai, hoặc bù = 0 → bỏ qua.

**R7. Tốc độ công bố cho hệ khác**
1. `IdleCps`: kết quả `idle_cps` hiện tại (gồm mọi tầng).
2. `PlinkoRateAvg`: tổng `creditedAmount` kênh Plinko trung bình trượt trên `plinkoRateWindowSeconds` (công thức `plinko_rate_avg`).
3. Hai tốc độ được công bố **riêng**, không cộng gộp, để người chơi so sánh từng kênh trước/sau khi đổi kệ (Pillar 3). HUD quyết định cách hiển thị. Sự kiện `IncomeRatesChanged { idleCps, plinkoRateAvg }` phát không dày hơn một lần mỗi `ratesPublishInterval`.
4. `OfflineRateSnapshot`: `idle_cps` tính lại khi bỏ mọi modifier `isTemporary` (toàn bộ tầng Boost). Hệ số offline và trần giờ thuộc Offline Earnings. Thu nhập auto-drop của Plinko không được quy đổi sang offline.
5. **Hai hàm thuần cho Shelf (Shelf D5, S7)**, chỉ đọc, không đổi ví/modifier/cờ "bẩn", không phát sự kiện, dùng được ở Ready, Running và Paused:
   - `PreviewIdleCps(plan)`: `plan` là bản kế hoạch giả định của Shelf = danh sách nguồn Idle `{sourceKey, baseRate, flatUnits}` + modifier tầng Base/Synergy `shelf.` giả định. Trả `idle_cps` như thể `plan` thay toàn bộ nguồn và modifier `shelf.` hiện có; tầng Boost/Prestige/Global lấy từ trạng thái thật (công thức `preview_idle_cps`).
   - `GetSourceIdleCps(sourceKey)`: phần `idle_cps` của một nguồn Idle đang đăng ký, sau Synergy và ba tầng kênh (Σ mọi nguồn = `IdleCps`); `sourceKey` chưa đăng ký → 0. Giá trị theo trạng thái hiện tại, không cache lệch: đọc được cùng khung sau khi Shelf đăng ký/gỡ.

**R8. Đường giá neo — giá quay Gacha (D1, D6)**
1. Economy Core giữ bộ đếm `spinsThisShop` (n), số lượt quay **đã trả bằng xu** trong tiệm hiện tại.
2. Giá quay hiện tại = `spin_price(n)` = giá gốc `spinBasePrice` × `spinGrowth`^n, làm tròn lên số đẹp (Formulas). Đây là giá **tạm**: hình dạng mũ nhẹ đã chốt (D6), giá trị sẽ chốt bằng mô phỏng sau Pet Catalog / Shelf.
3. Gacha gọi `TryPaySpin(playerAction)`: Economy Core gọi `TrySpend(currency.coin, spin_price(n), GachaSpin, playerAction)`; thành công thì tăng n lên 1 trong cùng thao tác, phát `SpinPriceChanged { spinsThisShop, spinPrice, baseCoinValue }` và trả `true`. Thất bại thì n giữ nguyên.
4. Lượt quay miễn phí (nếu Gacha/Ads có) không tăng n.

**R9. Họ công thức giá (D1, D2)**
1. Mọi giá ngoài lượt quay = `nice_ceil(hệ số × spin_price_raw(n))` (`derived_price`), tính lại mỗi khi n đổi. Hệ số thuộc hệ bán món (ví dụ `BoostDefinition.priceCoef` của Trà = 1.5); Economy Core không biết danh sách món hàng.
2. Phần thưởng xu cần co giãn theo tiến độ (quà Bác Bảo Vệ) dùng cùng công thức với hệ số của hệ cấp thưởng.
3. Giá **không** phụ thuộc CPS hay đội hình kệ: tháo/lắp thú trước khi mua không đổi giá (D2).
4. Hệ số ≤ 0, NaN hoặc vô cực → món đó "không có giá hợp lệ": API trả về không hợp lệ và hệ bán món phải khóa nút mua.

**R10. Giá trị xu gốc của Plinko (D4)**
1. `baseCoinValue = (spin_price_raw(n) / spinBasePrice)^coinValueExponent` (`base_coin_value`), bằng 1 khi n = 0, dùng giá **chưa làm tròn** để đường cong trơn.
2. **Mô hình kéo, không đẩy.** Plinko Board sở hữu interface `IBaseCoinValueSource { double BaseCoinValue { get; } }` trong hợp đồng của mình (cùng chỗ với `CoinResolved`). Economy Core cài đặt interface đó: getter trả `base_coin_value(n)` của n **hiện tại**, giá trị được cache và tính lại ngay trong thao tác làm n đổi (`TryPaySpin` thành công, `ResetForNewShop`, nạp save trong `Run()`), getter không cấp phát và luôn trả số hữu hạn ≥ 1. Economy Core không bao giờ ghi vào Plinko: không có input, setter hay lời gọi nào từ Economy Core sang Plinko.
3. Plinko đọc `IBaseCoinValueSource.BaseCoinValue` **tại thời điểm xu được giải quyết** để tính `rawValue` (Plinko R9.2); Economy Core không sửa `rawValue` sau đó. Khi không có nguồn nào được đăng ký (test, prototype, Plinko chạy không có Economy Core), Plinko dùng `PlinkoConfig.baseCoinValue` (= 1) làm dự phòng. Vì không có bước "đặt giá trị", không có giá trị nào bị fallback của config ghi đè, và giá trị luôn khớp n sau khi nạp save (VS).
4. Economy Core đăng ký nguồn này qua cùng cơ chế đăng ký dịch vụ với event bus (→ ADR) trong `Run()` của bước khởi động (R16.3), trước khi báo `IsDone`, và giữ đăng ký suốt phiên (kể cả Paused và ResettingShop). Không cần thứ tự khởi động giữa bước của Economy Core và bước của Plinko: xu mô phỏng chỉ sinh sau `Interactive`, nhưng `CoinResolved(Overflow)` (`RequestBurst` tràn hàng đợi, trả ngay) có thể đến trước `Interactive` và khi Paused (Plinko R11.4.4, R11.3), nên Economy Core xử lý `CoinResolved` ở Ready và Paused như ở Running.

**R11. Tham chiếu sản lượng gốc**
1. `idleCpsUnit` là đơn vị CPS mà bảng phẩm cấp × trọng số của Pet Catalog nhân vào (Pet Catalog D4). Shelf & Synergy làm phép nhân khi đăng ký nguồn; Pet Catalog chỉ giữ số không thứ nguyên.
2. **Quyết định (A)** của user (2026-10-21, `decision-log.md`): `idleCpsUnit` là hằng số trong suốt một tiệm (không co giãn theo n); tăng trưởng kênh Idle trong tiệm chỉ đến từ thú phẩm cấp cao hơn và Sao, giữ "tường mềm" báo hiệu prestige. Hệ quả được theo dõi: kênh Plinko tự co giãn theo `baseCoinValue` còn `idleCpsUnit` thì không, nên `channel_parity` của `pet-catalog.md` (tính theo một đơn vị sức mạnh) tăng theo `spinGrowth^(coinValueExponent × n)` và ra khỏi `PetBalanceTargets.parityBand` (trên 1.25) từ n ≈ 59 với knob mặc định (0.959 tại n = 50, 1.285 tại n = 60). Phần chênh phải được thú phẩm cấp cao và Sao bù; đây là rủi ro kiểm bằng mô phỏng (Open Questions), không phải câu hỏi mở về (A).

**R12. Khởi đầu và tiệm mới**
1. Game mới: Xu = `startingCoins`, n = 0, `baseCoinValue` = 1, không có modifier.
2. `ResetForNewShop(playerAction)` (Prestige gọi sau khi người chơi xác nhận "Mở Tiệm Mới"): mọi tiền tệ có `resetsOnNewShop = true` về giá trị khởi đầu, n = 0, giá và `baseCoinValue` tính lại. Phát theo thứ tự, cùng khung, sau khi trạng thái đã nhất quán: `WalletChanged { currencyId, previous, current, delta, reason = ShopReset }` cho mỗi tiền tệ reset có số dư đổi (Xu về `startingCoins`; không phát nếu bằng nhau), `SpinPriceChanged { 0, spinBasePrice, 1 }` (luôn phát, kể cả khi n đã = 0), rồi `ShopEconomyReset { resetCurrencyIds[], spinsThisShop = 0 }`. Tem Tiệm và modifier tầng Prestige giữ nguyên. Modifier của Shelf/Boosts do chính các hệ đó gỡ hoặc đăng ký lại theo luật reset của Prestige.
3. Đây là thao tác giảm số dư duy nhất ngoài `TrySpend` và chỉ chạy với ngữ cảnh hành động người chơi (R3.1).

**R13. Định dạng số**
1. Số dư và giá: dưới 1,000 hiện số nguyên làm tròn xuống; từ 1,000 hiện `displaySigDigits` chữ số có nghĩa (cắt, không làm tròn lên) kèm hậu tố K, M, B, T, rồi hai chữ cái aa, ab … az, ba … (công thức `display_format`).
2. Tốc độ (CPS, Plinko/s): dưới 1,000 hiện `rateDecimals` chữ số thập phân (cắt); từ 1,000 dùng cùng định dạng hậu tố.
3. Định dạng không cấp phát GC mỗi khung (bộ đệm ký tự cấp sẵn) — ràng buộc cho HUD và ADR.

**R14. Bản đồ faucet / sink**

| Loại | Nguồn | Tiền tệ | Kênh / API | Có từ |
|---|---|---|---|---|
| Faucet | Plinko (Manual, Auto, Rain, JackpotBurst) + thưởng sự kiện thú kênh Plinko | Xu | Pipeline Plinko | MVP |
| Faucet | CPS thú trên kệ | Xu | Pipeline Idle | MVP |
| Faucet | Thu nhập offline (×2 qua ads từ Alpha) | Xu | `Add(Offline)` | VS |
| Faucet | Bù Idle thời gian xem rewarded ad | Xu | `Add(AdIdleCompensation)` (R6.4) | Alpha |
| Faucet | Quà Bác Bảo Vệ, phần thưởng quảng cáo | Xu | `Add(AdReward)` | Alpha |
| Faucet | Prestige "Mở Tiệm Mới" | Tem Tiệm | `Add(Prestige)` | Alpha |
| Sink (không phải chi tiêu) | `ResetForNewShop`: Xu về `startingCoins` (R12.2), chỉ khi người chơi xác nhận | Xu | `WalletChanged(ShopReset)` | Alpha |
| Sink chính | Vặn trứng Gacha | Xu | `TryPaySpin` | MVP |
| Sink | Mở ô kệ (Shelf `TryUnlockNextSlot`, giá `slot_unlock_price` của Shelf), boost, Flash Deal | Xu | `TrySpend(ShopPurchase)` | VS / Alpha |
| Sink | Nâng cấp vĩnh viễn | Tem Tiệm | `TrySpend(PrestigeUpgrade)` | Alpha |
| Sink | Tiền kẹo đón thú về (người chơi tự chọn trả) | Xu | `TrySpend(NeighborCandy)` | v1.1 |

Cân bằng faucet/sink nhắm phiên 5–15 phút: mục tiêu nhịp lượt quay nằm ở `EconomyBalanceTargets` (Formulas `seconds_per_spin`). Mọi `reason` của `WalletChanged` phải thuộc bảng này.

**R15. Sự kiện công bố**
`WalletChanged { currencyId, previous, current, delta, reason }` (cộng Idle gộp tối đa một sự kiện mỗi khung), `IncomeCredited` (R5.4, chỉ kênh Plinko và `SubmitIncome`), `SpendRejected { currencyId, amount, reason }`, `SpinPriceChanged`, `IncomeRatesChanged`, `ShopEconomyReset { resetCurrencyIds[], spinsThisShop }` (R12.2). Cơ chế phát sự kiện (C# event / bus) → ADR, dùng chung với Plinko.

**R16. Khởi động qua hợp đồng pha của Bootstrap** (C1[14], C1[15])
1. Economy Core tự đăng ký **một** bước khởi động theo hợp đồng `IBootStep` của Bootstrap & Scene Flow (B2): `Id` = `economy.core`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = đúng. Bootstrap chỉ biết interface, không tham chiếu Economy Core; Economy Core phụ thuộc Bootstrap, không có chiều ngược.
2. Bước chạy sau pha `SaveLoad` (Bootstrap B3), nên từ VS dữ liệu ví và `spinsThisShop` đã được Save/Load đọc trước khi Economy Core nạp (kẹp như Edge Cases); ở MVP pha `SaveLoad` rỗng và Economy Core bắt đầu như game mới (R12.1). Các hệ gọi Economy Core ngay trong bước khởi động của chúng (Inventory, Shelf & Synergy) xếp sau bằng `RunsAfter` của chính chúng. Plinko Board **không** khai `RunsAfter` `economy.core` (một `RunsAfter` trỏ tới Id không tồn tại là BootFailed theo Bootstrap B3.4, nên sẽ làm hỏng Plinko khi chạy không có Economy Core): Plinko kéo `baseCoinValue` lúc giải quyết xu và có dự phòng (R10.3, R10.4).
3. `Run()` đọc `EconomyConfig`, đăng ký tiền tệ (R1), tính giá quay và `baseCoinValue`, đăng ký `IBaseCoinValueSource` (R10.4), rồi đặt `IsDone` trong cùng khung; Economy Core vào **Ready** (không phải Running: vòng đời Bootstrap còn ở `Booting.Steps`, R6.3). Thiếu `EconomyConfig` hoặc `currencies` không có `currency.coin` → `IsFailed`; vì bước là cốt lõi, Bootstrap vào BootFailed (Bootstrap B4.3) và không có xu nào được cộng.
4. Economy Core nghe `Interactive`, `Paused`, `Resumed` và `AdClosed` của Bootstrap và đọc truy vấn `IsSimulationRunning` (R6.3, Bootstrap B5.3, B6.5). `Paused` / `Resumed` chỉ phát sau `BootCompleted` nhưng có thể phát khi vòng đời còn ở `Booting.Curtain` (Bootstrap B6.4, B6.6), nên sau `Resumed` Economy Core luôn đọc `IsSimulationRunning` để chọn Running hay Ready. `Interactive` phát đúng một lần mỗi lần nạp scene `MainGame`; nhận `Interactive` khi đã ở Running thì không làm gì. Khi vào Ready (cuối `Run()` hoặc sau `Resumed`), nếu `IsSimulationRunning` đã đúng thì Economy Core vào Running ngay (hệ đăng ký muộn vẫn đọc được trạng thái, Bootstrap B6.5).

**Người chơi không thể**: mua chỉ số "+1 CPS" lặp lại (Anti-Pillar), làm giá rẻ đi bằng cách tháo thú khỏi kệ, cộng dồn boost vượt `boostTierCap`, mất xu đã có trong ví vì bất kỳ hệ nào ngoài lựa chọn chi tiêu của chính mình.

### States and Transitions

Trạng thái của Economy Core (một thực thể cho cả phiên):

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Trước khi bước `economy.core` (pha `GameplayInit`) được Bootstrap chạy | `Run()` của bước xong (sau pha `SaveLoad`, nên từ VS đã có dữ liệu save) → Ready; `IsFailed` → giữ Uninitialized, Bootstrap vào BootFailed | Không nhận `CoinResolved`, không có API nào hoạt động; chưa đăng ký `IBaseCoinValueSource` (Plinko, nếu được gọi, dùng dự phòng — R10.3) |
| Ready | `Run()` xong (vòng đời Bootstrap ở `Booting.Steps`, dưới rèm); hoặc nhận `Resumed` khi `IsSimulationRunning` sai (vòng đời về `Booting.Curtain`, Bootstrap B6.9) | Nhận `Interactive` → Running; `IsSimulationRunning` đúng lúc vào Ready → Running ngay (R16.4); nhận `Paused` (trang ẩn khi rèm còn, Bootstrap B6.6) → Paused | API hoạt động: `Add`, `TrySpend`/`TryPaySpin`, truy vấn giá/số dư, đăng ký nguồn và modifier, `IBaseCoinValueSource` đã đăng ký; `CoinResolved`/`SubmitIncome` được xử lý khi đến, gồm `CoinResolved(Overflow)` của `RequestBurst` tràn hàng đợi trước `Interactive` (Plinko R11.4.4); **không cộng Idle** (dt = 0, R6.2) |
| Running | Nhận `Interactive` từ Ready; hoặc nhận `Resumed` khi `IsSimulationRunning` đúng; hoặc ResettingShop hoàn tất | Nhận `Paused` của Bootstrap → Paused; Prestige gọi `ResetForNewShop` → ResettingShop | Cộng Idle mỗi khung khi `IsSimulationRunning` đúng (R6.2), xử lý `CoinResolved`/`SubmitIncome`, nhận `TrySpend` |
| Paused | Nhận `Paused {reason}` (tab ẩn / app xuống nền) khi đang Ready hoặc Running | Nhận `Resumed {reason, awaySeconds}` → đọc `IsSimulationRunning`: đúng → Running, sai → Ready (R6.3) | Không cộng Idle; `TrySpend`/`Add` vẫn hợp lệ (không có input trong lúc này nên thực tế không xảy ra); **vẫn xử lý `CoinResolved`** như Running: khi nhận `Paused`, Plinko xả hàng đợi loạt bằng `CoinResolved(reason = Overflow)` (Plinko R11.3) và thứ tự hai hệ nhận `Paused` không xác định; `RequestBurst` đến khi Paused vẫn trả ngay `CoinResolved(Overflow)` (hàng đợi tràn), chỉ không có xu mô phỏng mới vì mô phỏng Plinko dừng |
| ResettingShop | `ResetForNewShop(playerAction)` hợp lệ (cần chạm người chơi, nên chỉ xảy ra ở Running) | Hoàn tất trong cùng khung → Running | Đặt lại tiền tệ `resetsOnNewShop`, n = 0, tính lại giá và `baseCoinValue`, phát `ShopEconomyReset` |

Trạng thái của một modifier: `Registered` (đang tính) → `Unregistered` (hệ đăng ký gỡ). Không có trạng thái "hết hạn" bên trong Economy Core; hết hạn là việc của hệ đăng ký.

### Interactions with Other Systems

| Hệ | Economy Core nhận vào | Economy Core phát ra | Chủ interface |
|---|---|---|---|
| Plinko Board | `CoinResolved { coinId, origin, bucketIndex \| none, multiplier, rawValue, scoredPegHits, reason }` (`reason ∈ {Bucket, Stuck, Overflow}`) | Cài đặt `IBaseCoinValueSource` (đăng ký trong `Run()`); Plinko tự đọc `BaseCoinValue` lúc giải quyết xu (kéo, R10.2–R10.4) — Economy Core không ghi gì vào Plinko | Plinko sở hữu sự kiện `CoinResolved` và interface `IBaseCoinValueSource`; Economy Core sở hữu cài đặt và công thức `base_coin_value` |
| Gacha Machine | `TryPaySpin(playerAction)` → `bool`, `GetSpinPrice()`, `CanAfford(coin, amount)` | `SpinPriceChanged { spinsThisShop, spinPrice, baseCoinValue }`, `SpendRejected` (khi `TryPaySpin` thất bại) | Economy Core (đường neo); Gacha không định nghĩa giá |
| Shelf & Synergy | `RegisterIncomeSource` / `Unregister`, modifier tầng Base và Synergy, `SubmitIncome(Plinko)` cho thưởng sự kiện thú, `TrySpend(coin, price, ShopPurchase, playerAction)` mở ô kệ (giá do Shelf tính), `PreviewIdleCps(plan)`, `GetSourceIdleCps(sourceKey)` (R7.5) | `idleCpsUnit`, `baseCoinValue` (để tính thưởng thú theo bội số), `derived_price`, `IncomeRatesChanged`, `SpendRejected`, kết quả hai hàm thuần | Economy Core sở hữu hợp đồng modifier và hai hàm thuần; Shelf sở hữu giá trị, bản kế hoạch và giá ô |
| Inventory | — (không có dữ liệu; bước `inventory.core` khai `RunsAfter` `economy.core`) | — | Bootstrap (thứ tự khởi tạo); Inventory không gọi Economy Core, không đọc ví |
| Pet Catalog | — (Catalog không gọi Economy Core) | `idleCpsUnit` được Shelf nhân với số của Catalog | Pet Catalog sở hữu bảng phẩm cấp × trọng số |
| HUD & Tab Navigation | Truy vấn số dư, `IdleCps`, `PlinkoRateAvg`, chuỗi đã định dạng | `WalletChanged`, `IncomeRatesChanged`, `SpinPriceChanged` | Economy Core (dữ liệu, bộ định dạng); HUD (cách hiển thị) |
| Feedback & Juice | — | `IncomeCredited` (`creditedAmount` cho số bay; `coinId`, `bucketIndex` để đặt và gộp số bay theo hũ), `SpendRejected` | Economy Core |
| Boosts (VS) | Modifier tầng Boost (`boost.`, `isTemporary`), `TrySpend(ShopPurchase)`, `GetDerivedPrice(priceCoef)` | Giá đã tính | Economy Core (họ giá); Boosts (hệ số, thời lượng) |
| Shop (VS) | `TrySpend(ShopPurchase)`, `GetDerivedPrice` | Giá đã tính, `SpinPriceChanged` để làm mới bảng giá | Economy Core / Shop |
| Star Progression (VS) | — trực tiếp (hệ số Sao đi vào gốc qua Shelf) | — | Star sở hữu hệ số Sao và trần |
| Offline Earnings (VS) | `Add(currency.coin, amount, Offline)` | `OfflineRateSnapshot` | Economy Core (tốc độ); Offline (hệ số, trần giờ) |
| Save/Load (VS) | Dữ liệu đã lưu khi khởi tạo | Bảng ví `id → double`, `spinsThisShop` | Save/Load sở hữu định dạng file; Economy Core sở hữu nội dung |
| Rewarded Ads (Alpha) | `Add(AdReward)`, modifier tầng Boost (`ad.`), `GetDerivedPrice` cho quà | Giá/quà đã tính | Economy Core |
| Prestige (Alpha) | `ResetForNewShop(playerAction)`, `Add(currency.shop_stamp, Prestige)`, modifier tầng Prestige, `TrySpend(PrestigeUpgrade)` | `ShopEconomyReset`, `spinsThisShop` (đọc để đặt điều kiện prestige) | Prestige sở hữu điều kiện và phần thưởng |
| Neighbor Pranks (v1.1) | `TrySpend(NeighborCandy)` do người chơi trả; interface giữ tạm thu nhập mới (R4.9) | — | Neighbor sở hữu luật; Economy Core sở hữu giới hạn "không trừ ví" |
| Bootstrap & Scene Flow | Hợp đồng bước khởi động `IBootStep` (thứ tự pha), `Interactive` (Ready → Running), `Paused {reason}` / `Resumed {reason, awaySeconds}`, `AdClosed {adSeconds}` (bù Idle, R6.4), truy vấn `IsSimulationRunning` (chỉ cộng Idle khi đúng; đọc sau `Resumed`, B6.5) | Bước `economy.core` (pha `GameplayInit`, cốt lõi) do Economy Core tự đăng ký (R16) | Bootstrap sở hữu hợp đồng pha và sự kiện vòng đời; Economy Core sở hữu bước của mình |
| Analytics Hooks (v1.1) | — | Mọi sự kiện R15 | Analytics nghe |

→ Thành ADR: cơ chế sự kiện, đăng ký dịch vụ (`IBaseCoinValueSource`) và đăng ký modifier (C# event / bus, không singleton — dùng chung với Plinko); "ngữ cảnh hành động người chơi" cho `TrySpend`/`ResetForNewShop`; bộ định dạng số không cấp phát GC; định dạng lưu `id → double` (JsonUtility không serialize trực tiếp Dictionary → cần mảng cặp id/giá trị). Không có engine reference cho domain scripting: feasibility chưa kiểm theo Unity 6000.3.16f1; chưa có ADR nào.

## Formulas

Ký hiệu: n = `spinsThisShop`; c = kênh (`Idle` | `Plinko`); s = nguồn. Giá trị mặc định là tạm (D6), sẽ chốt bằng mô phỏng bảng tính sau Pet Catalog / Shelf & Synergy. Giá trị Plinko (EV hiệu dụng 2.96, thời gian rơi 2.5 s, trần mô hình `raw_rate_ceiling` theo cấu hình ≈ 20 / 27 / 30.7 / 42 × `base_coin_value` xu thô/s — trần mềm, không phải trần cứng, R5.5) lấy từ `plinko-board.md` / registry, không thuộc hệ này. `sanitize(v)` = v nếu v hữu hạn và ≥ 0, ngược lại 0 (R4.5).

### spin_price

`spin_price_raw(n) = spin_base_price × spin_growth^n`
`spin_price(n) = nice_ceil(spin_price_raw(n), price_nice_sig_digits)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| n | int | 0 – ~14 000 | calculated (`spinsThisShop`, lưu) | số lượt quay đã trả bằng xu trong tiệm này |
| spin_base_price | double | 50–200 | SO field `EconomyConfig.spinBasePrice` | giá lượt đầu (đường neo) |
| spin_growth | double | 1.03–1.08 | SO field `EconomyConfig.spinGrowth` | hệ số tăng mỗi lượt (D6: mũ nhẹ) |
| price_nice_sig_digits | int | 2–3 | SO field `EconomyConfig.priceNiceSigDigits` | số chữ số có nghĩa của giá đẹp |

**Output range:** 100 (n = 0) → 1,200 (n = 50) → 14,000 (n = 100) → 160,000 (n = 150); không giảm khi n tăng (các lượt kề nhau có thể cùng giá vì làm tròn). Khi `spin_price_raw` > `walletSafeCap`, giá kẹp tại `walletSafeCap` (n ≈ 14 060 với mặc định — không thể đạt trong thực tế).
**Example:** n = 1 → 100 × 1.05 = 105 → `nice_ceil` 2 chữ số → **110**. n = 20 → 265.3 → **270**. n = 100 → 13,150.1 → **14,000**.

### nice_ceil

`nice_ceil(x, d) = 0 nếu x ≤ 0; ngược lại ceil(x / step − nice_epsilon) × step`, với `step = 10^max(floor(log10 x) − (d − 1), 0)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| x | double | 0 – `walletSafeCap` | calculated | giá trị trước khi làm tròn |
| d | int | 2–3 | SO field `EconomyConfig.priceNiceSigDigits` | số chữ số có nghĩa |
| nice_epsilon | double | 1e-9 | constant | dung sai dấu phẩy động để 100.0000000001 không thành 110; không phải knob thiết kế |

**Output range:** ≥ x, sai lệch tối đa < 10^(1−d) tương đối (d = 2: < +10%); dưới 10^(d−1) chỉ làm tròn lên số nguyên.
**Example:** 1,146.74 → step 100 → ceil(11.4674) = 12 → **1,200**. 7.2 → step 1 → **8**. 100 → **100**.

### derived_price

`derived_price(coef, n) = nice_ceil(coef × spin_price_raw(n), price_nice_sig_digits)`; coef không hợp lệ (≤ 0, NaN, vô cực) → "không có giá" (R9.4)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| coef | double | 0.1–50 | SO field của hệ bán món (ví dụ `BoostDefinition.priceCoef`) | hệ số so với giá quay hiện tại (D1, D2) |
| spin_price_raw(n) | double | ≥ `spinBasePrice` | calculated | xem `spin_price` |

**Output range:** coef × 100 ở n = 0, tăng cùng tốc độ với giá quay; không phụ thuộc CPS hay kệ.
**Example:** Trà Sốt Dẻo coef 1.5: n = 0 → **150**; n = 50 → 1.5 × 1,146.74 = 1,720.1 → **1,800**. Mưa Đồng Xu coef 2.0: n = 100 → 26,300.2 → **27,000**. Quà Bác Bảo Vệ (phần thưởng) coef 1.5: n = 0 → **150** (khớp FirstGDD).

### base_coin_value

`base_coin_value(n) = (spin_price_raw(n) / spin_base_price)^coin_value_exponent = spin_growth^(coin_value_exponent × n)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| coin_value_exponent (α) | double | 0.5–0.7 | SO field `EconomyConfig.coinValueExponent` | độ dốc dưới tuyến tính so với giá quay (D4) |
| spin_growth, n | — | — | xem `spin_price` | |

**Output range:** 1 (n = 0) → 4.32 (n = 50) → 18.68 (n = 100) → 80.7 (n = 150) với α = 0.6; tại n = 100: α = 0.5 → 11.47, α = 0.7 → 30.43. Luôn ≥ 1. Thay thế hằng số registry `base_coin_value = 1` (user duyệt, D4).
**Example:** n = 1 → 1.05^0.6 = **1.0297**. n = 100 → 1.05^60 = **18.679**; một xu vào hũ x5 có `rawValue` = 93.40.

### multiplier_synergy

`M_Synergy(s) = min(1 + Σ sanitize(v_m), synergy_tier_cap)` với m chạy qua mọi modifier tầng Synergy có (target = s) hoặc (target = Channel và channel(m) ∈ {channel(s), Both})

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| v_m | double | 0–5 mỗi modifier | calculated (giá trị do Shelf & Synergy đăng ký) | +100% = 1.0 |
| synergy_tier_cap | double | 2–8 | SO field `EconomyConfig.synergyTierCap` | trần tầng Synergy |

**Output range:** 1.0 (không synergy) đến `synergy_tier_cap` (4.0).
**Example:** hai nguồn kênh +1.0 (Cáo, combo nhóm) → min(1 + 2, 4) = **3.0**. Tổng +4.2 → min(5.2, 4) = **4.0**. Một modifier NaN → bị coi là 0.

### tier_multiplier

`M_T(c) = min(1 + Σ sanitize(v_m), cap_T)` với m chạy qua mọi modifier tầng T có channel(m) ∈ {c, Both}; T ∈ {Boost, Prestige, Global}

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| cap_Boost | double | 2–5 | SO field `EconomyConfig.boostTierCap` | trần tầng Boost |
| cap_Prestige | double | 3–50 | SO field `EconomyConfig.prestigeTierCap` | trần tầng Prestige (*provisional*, Alpha) |
| cap_Global | double | 1–3 | SO field `EconomyConfig.globalTierCap` | trần tầng Global (MVP rỗng) |

**Output range:** mỗi tầng 1.0 đến trần; tích ba tầng tối đa 3 × 10 × 2 = 60 với mặc định.
**Example:** Trà (+1.0, Both) và một boost quảng cáo (+1.0, Plinko) cùng bật → kênh Plinko: min(1 + 2, 3) = **3.0** (không phải 4); kênh Idle: **2.0**.

### channel_income

`income_s = (base_s + flat_s) × M_Synergy(s) × M_Boost(c) × M_Prestige(c) × M_Global(c)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| base_s | double | ≥ 0 | calculated (Shelf: tốc độ gốc của thú; Plinko: `rawValue`; `SubmitIncome.baseAmount`) | giá trị gốc của nguồn |
| flat_s | double | ≥ 0 | calculated (`idle_cps`, `plinko_credit`) | cộng phẳng tầng Base nhắm vào s |
| M_* | double | 1–cap | calculated | xem hai công thức trên |

**Output range:** 0 đến base × (1 + cộng phẳng) × 4 × 3 × 10 × 2.
**Example (D3):** gốc 50, hai nguồn Synergy +1.0, Trà +1.0, một nguồn Global +1.0 → 50 × 3 × 2 × 1 × 2 = **600** (toàn nhân sẽ là 800, toàn cộng 250).

### idle_cps

`idle_cps = [ Σ_s∈Idle (base_rate_s + flat_units_s × idle_cps_unit) × M_Synergy(s) ] × M_Boost(Idle) × M_Prestige(Idle) × M_Global(Idle)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| base_rate_s | double (xu/s) | 0–~1e6 | calculated (Shelf: `idle_base_units` của Pet Catalog = phẩm cấp × trọng số × `role_idle_share`, nhân `idle_cps_unit` × hệ số Sao) | tốc độ gốc một thú |
| flat_units_s | double | ≥ 0 | calculated (Σ sanitize(v) tầng Base nhắm s) | cộng phẳng theo bội số đơn vị |
| idle_cps_unit | double (xu/s) | 1–5 | SO field `EconomyConfig.idleCpsUnit` | đơn vị CPS gốc cho Pet Catalog (R11); hằng số trong một tiệm, không co giãn theo n (quyết định (A), R11.2) |

**Output range:** 0 (kệ trống) đến vài nghìn trong MVP; trần thực tế do Pet Catalog / Star / trần tầng.
**Example:** thú A gốc 10 (Synergy nhắm A +0.5 và combo kênh +0.5 → 2.0) = 20; thú B gốc 4, cộng phẳng 1 đơn vị (= 2 xu/s), combo kênh → (4 + 2) × 1.5 = 9; tổng 29; Trà +1.0 → **58 xu/s**.

### preview_idle_cps

`source_idle_cps(s) = (base_rate_s + flat_units_s × idle_cps_unit) × M_Synergy(s) × M_Boost(Idle) × M_Prestige(Idle) × M_Global(Idle)`; `GetSourceIdleCps(s)` = `source_idle_cps(s)` (0 nếu chưa đăng ký); `PreviewIdleCps(plan)` = `idle_cps` với Σ_s thay bằng nguồn và modifier `shelf.` của `plan`, các tầng Boost/Prestige/Global giữ nguyên

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| plan | danh sách nguồn + modifier `shelf.` | — | calculated (Shelf, S7) | trạng thái kệ giả định; giá trị không hợp lệ qua `sanitize` |
| các biến còn lại | — | — | xem `idle_cps` | cùng định nghĩa, cùng trần tầng |

**Output range:** ≥ 0; Σ `GetSourceIdleCps` = `idle_cps`; `PreviewIdleCps(plan hiện tại)` = `idle_cps`.
**Example:** cấu hình AC-20: `GetSourceIdleCps(A)` = 10 × (1 + 0.5 + 0.5) × 2.0 (Trà) = **40**; B = (4 + 2) × 1.5 × 2 = **18**; tổng **58**.

### plinko_credit

`credit_coin = (rawValue + min(flat_units_coin, plinko_flat_per_coin_cap) × base_coin_value) × M_Synergy(plinko.coin) × M_Boost(Plinko) × M_Prestige(Plinko) × M_Global(Plinko)`
`credit_submit = baseAmount × M_Synergy(sourceKey) × M_Boost(Plinko) × M_Prestige(Plinko) × M_Global(Plinko)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| rawValue | double | 0–5 × `base_coin_value` | calculated (Plinko `CoinResolved`) | đã gồm hệ số hũ / kẹt / trả ngay |
| flat_units_coin | double | ≥ 0 | calculated (Σ tầng Base nhắm `plinko.coin`) | "+N × xu gốc mỗi xu" |
| plinko_flat_per_coin_cap | double | 0–5 | SO field `EconomyConfig.plinkoFlatPerCoinCap` | trần cộng phẳng mỗi xu (bội số `base_coin_value`) |
| baseAmount | double | > 0 | calculated (Shelf `SubmitIncome`) | thưởng sự kiện của thú kênh Plinko |

**Output range:** mỗi xu 0 đến (5 + 2) × `base_coin_value` × 60. Thông lượng thô **không** có trần cứng: Plinko chỉ cho trần mô hình `raw_rate_ceiling` theo cấu hình (≈ 20 × `base_coin_value` xu/s ở độ nảy gốc không auto-drop; ≈ 27 khi auto-drop ở sàn; ≈ 26–30.7 ở trần nảy; ≤ ≈ 42 khi cả hai — `plinko-board.md`, registry `raw_rate_ceiling`); Economy Core cân bằng theo `plinko_rate_model`, không kẹp (R5.5).
**Example:** n = 100, xu vào hũ x5: rawValue = 93.40; combo kênh Plinko +0.5; Trà +1.0 → 93.40 × 1.5 × 2 = **280.2**. Thêm cộng phẳng 0.4 → (93.40 + 0.4 × 18.679) × 3 = **302.6**.

### idle_accrual

`delta_coin = idle_cps × min(dt, max_frame_delta)` khi Economy Core ở Running **và** `IsSimulationRunning` của Bootstrap đúng (B6.5); `delta_coin` = 0 (dt = 0) ở mọi khung khác — Uninitialized, Ready (dưới rèm), Paused

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| dt | double (s) | ≥ 0 | calculated (delta khung mô phỏng; = 0 khi `IsSimulationRunning` sai) | thời gian khung |
| IsSimulationRunning | bool | đúng / sai | Bootstrap & Scene Flow (truy vấn, B6.5) | đúng ⇔ vòng đời Bootstrap = `Running` |
| max_frame_delta | double (s) | 0.1–0.5 | SO field `EconomyConfig.maxFrameDelta` | trần delta một khung, chống nhảy vọt sau giật hình |

**Output range:** 0 đến `idle_cps` × 0.25 mỗi khung.
**Example:** 58 xu/s, 60 khung × 1/60 s → **+58.0**; một khung giật 1.0 s → **+14.5** (phần 0.75 s còn lại không được cộng).

### ad_idle_compensation

`ad_idle_compensation = idle_cps × min(max(sanitize(ad_seconds) − ad_simulated_seconds, 0), ad_comp_max_seconds)`; chỉ khi `AdClosed` đến lúc `IsSimulationRunning` đúng (R6.4)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| ad_seconds | double (s) | ≥ 0 | `AdClosed.adSeconds` (Bootstrap, đã làm sạch) | thời lượng thô cờ ad bật |
| ad_simulated_seconds | double (s) | ≥ 0 | calculated (Σ `min(dt, max_frame_delta)` đã cộng ở `idle_accrual` khi `IsAdOpen` đúng; về 0 sau mỗi `AdClosed`) | phần đã được mô phỏng thật (web), trừ để không bù hai lần |
| ad_comp_max_seconds | double (s) | 30–300 | SO field `EconomyConfig.adCompensationMaxSeconds` | trần giây bù |

**Output range:** 0 đến `idle_cps` × `ad_comp_max_seconds`; không bao giờ âm.
**Example:** `idle_cps` 58, `adSeconds` 30, chưa mô phỏng giây nào (mobile) → **1,740**; web mô phỏng đủ 30 s → **0**; `adSeconds` 200 → 58 × 90 = **5,220**.

### plinko_rate_avg

`plinko_rate_avg = Σ credit (Plinko, trong plinko_rate_window gần nhất) / clamp(running_time, rate_bin, plinko_rate_window)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| plinko_rate_window | double (s) | 5–30 | SO field `EconomyConfig.plinkoRateWindowSeconds` | cửa sổ trung bình trượt |
| rate_bin | double (s) | 1 | constant | độ phân giải bộ đệm vòng (1 ô / giây), đủ cho HUD, cấp phát sẵn |
| running_time | double (s) | ≥ 0 | calculated | thời gian Running từ đầu phiên |

**Output range:** 0 đến khoảng `plinko_rate_model(k, n)` × 60 xu/s của cấu hình đang xét (≈ 20 → 42 × `base_coin_value` × 60); không có trần cứng (R5.5) — một đợt Mưa Đồng Xu hoặc Bùng Xu có thể đẩy trung bình trượt tạm vượt mô hình.
**Example:** 10 s, mỗi giây 100 xu → **100/s**. Mới chạy 3 s, tổng 300 → 300 / 3 = **100/s** (không báo thấp 30/s).

### plinko_rate_model (mô hình cân bằng, trần mềm kênh Plinko)

`plinko_rate_model(k, n) = plinko_raw_rate_ceiling_assumed[k] × base_coin_value(n)` (xu thô/s, trước modifier); thu nhập mô hình của kênh = `plinko_rate_model(k, n)` × M_Synergy(`plinko.coin`) × M_Boost(Plinko) × M_Prestige(Plinko) × M_Global(Plinko), như `plinko_credit`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| k | enum | {BaseNoAuto, AutoFloor, BounceCap, AutoFloorAndBounceCap} | calculated (cấu hình đang mô phỏng) | độ nảy gốc không auto-drop / auto-drop ở sàn / độ nảy ở trần / cả hai (theo các ca của `raw_rate_ceiling` trong `plinko-board.md`) |
| plinko_raw_rate_ceiling_assumed | double[4] (× `base_coin_value` / s) | xem Tuning Knobs | SO field `EconomyBalanceTargets.plinkoRawRateCeilingAssumed` | bản giả định của `raw_rate_ceiling` của Plinko theo cấu hình; cập nhật theo đo Plinko AC-13 / AC-13b / AC-13c |
| base_coin_value(n) | double | ≥ 1 | calculated | xem `base_coin_value` |

**Output range:** ≈ 20 → 42 × `base_coin_value(n)` xu thô/s với mặc định. Đây là **trần mềm**: Economy Core dùng nó để mô phỏng và cân bằng (so với `idle_cps`, `seconds_per_spin`, ngân sách Kẹo Bảy Màu của `pet-catalog.md`), không dùng để kẹp thu nhập (R5.5).
**Example:** n = 100 (`base_coin_value` = 18.679): BaseNoAuto 20 × 18.679 = **373.6 xu thô/s**; AutoFloorAndBounceCap 42 × 18.679 = **784.5 xu thô/s**; thêm Trà (+1.0) → 1,569.0 xu/s vào ví.

### offline_rate_snapshot

`offline_rate = idle_cps` tính lại khi bỏ mọi modifier `isTemporary` (toàn bộ tầng Boost)

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| idle_cps | double | — | calculated | xem trên |

**Output range:** 0 đến `idle_cps`; luôn ≤ `idle_cps`.
**Example:** ví dụ `idle_cps` = 58 với Trà → `offline_rate` = **29 xu/s**. Offline Earnings áp hệ số và trần giờ của nó.

### display_format

`k = floor(log10 v / 3)`; `m = v / 1000^k`; hiển thị = cắt m còn `display_sig_digits` chữ số có nghĩa + hậu tố(k). Hậu tố: k = 1..4 → K, M, B, T; k ≥ 5 → hai chữ cái: chữ thứ nhất = `(k − 5) div 26`, chữ thứ hai = `(k − 5) mod 26` (a = 0). v < 1000 → floor(v) (số dư/giá) hoặc cắt `rate_decimals` chữ số thập phân (tốc độ). NaN/âm → "0".

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| v | double | 0 – `walletSafeCap` | calculated | giá trị cần hiện |
| display_sig_digits | int | 3–4 | SO field `EconomyConfig.displaySigDigits` | chữ số có nghĩa sau hậu tố |
| rate_decimals | int | 0–2 | SO field `EconomyConfig.rateDecimals` | chữ số thập phân của tốc độ < 1000 |
| 1000, 26 | int | — | constant | cơ số hậu tố, số chữ cái Latin |

**Output range:** chuỗi ≤ 6 ký tự (ví dụ "999dr"); 1e300 → "1.00dr".
**Example:** 999.9 → "999"; 1,234.5 → "1.23K"; 12,345 → "12.3K"; 999,999.6 → "999K" (cắt, không nhảy thành "1000K"); 1e15 → "1.00aa"; tốc độ 6.58 → "6.5/s".

### wallet_add

`balance' = min(balance + amount, wallet_safe_cap)` nếu amount hữu hạn và > 0; ngược lại từ chối

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| wallet_safe_cap | double | 1e100–1e305 | SO field `EconomyConfig.walletSafeCap` | trần an toàn tránh tràn double khi nhân nhiều tầng |

**Output range:** 0 đến `wallet_safe_cap`.
**Example:** số dư 0, cộng 2.5 hai lần → **5.0**; số dư 1e300 − 1 cộng 1e299 → **1e300**.

### first_spin_time (mô hình cân bằng)

`first_spin_time = max(0, spin_base_price − starting_coins) / (realistic_tap_rate × plinko_ev_effective × base_coin_value(0)) + median_fall_time`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| starting_coins | double | 0–100 | SO field `EconomyConfig.startingCoins` | xu khởi đầu |
| realistic_tap_rate | double (chạm/s) | 2–5 | SO field `EconomyBalanceTargets.realisticTapRate` | nhịp chạm thực tế của người chơi casual |
| plinko_ev_effective | double | 2.6–3.3 | SO field `EconomyBalanceTargets.plinkoEvEffectiveAssumed` | EV hiệu dụng mỗi xu (Plinko, gồm Bùng Xu); cập nhật theo AC-30 của Plinko |
| median_fall_time | double (s) | 1.8–3.5 | SO field `PlinkoBalanceTargets.targetMedianFallTime` (Plinko sở hữu) | trễ từ chạm tới tiền vào ví |
| first_spin_max | double (s) | 15–45 | SO field `EconomyBalanceTargets.firstSpinMaxSeconds` | mục tiêu onboarding |

**Output range:** mục tiêu ≤ `first_spin_max` (30 s).
**Example:** 100 / (3 × 2.96 × 1) + 2.5 = 11.3 + 2.5 = **13.8 s** ≤ 30 s.

### seconds_per_spin (mô hình cân bằng, chỉ Plinko chủ động)

`seconds_per_spin(n) = spin_price(n) / (realistic_tap_rate × plinko_ev_effective × base_coin_value(n))` ∝ `spin_growth^((1 − α) × n)`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| spin_seconds_max_at | double[3] (s) | mỗi mốc 15–300 | SO field `EconomyBalanceTargets.plinkoOnlySpinSecondsMax` | trần thời gian một lượt tại n = 0 / 50 / 100 khi chỉ chạm Plinko, không thú, không boost; viết theo quyết định (A) — `idleCpsUnit` hằng số trong tiệm (R11.2) |
| checkpoints | int[3] | 0–200 | SO field `EconomyBalanceTargets.spinCheckpoints` | các mốc n kiểm tra |

**Output range:** là cận trên (chưa tính Idle, thú kênh Plinko, boost); tăng chậm theo 1.05^(0.4n).
**Example:** n = 0 → 100 / 8.88 = **11.3 s**; n = 50 → 1,200 / (8.88 × 4.32) = **31.3 s**; n = 100 → 14,000 / (8.88 × 18.68) = **84.4 s**. Trần mặc định 30 / 60 / 120 s.

## Edge Cases

**Ví và chi tiêu**
- **If số dư bằng đúng giá** (100 xu, giá 100): `TrySpend` thành công, số dư còn 0.
- **If số dư thật là 99.7 (hiện "99") và giá là 100**: `TrySpend` thất bại. Vì hiển thị luôn cắt xuống, số hiện ra không bao giờ lớn hơn số thật, nên người chơi không thấy "đủ tiền mà không mua được". HUD dùng cùng phép so sánh `CanAfford(currencyId, amount)` để bật/tắt nút.
- **If hai yêu cầu chi tiêu đến trong cùng khung** (bấm đúp nút vặn trứng): xử lý tuần tự; yêu cầu thứ hai gặp giá mới (n đã tăng) và số dư mới; thiếu thì `SpendRejected`. Không bao giờ trừ hai lần cho một lượt quay.
- **If một hệ gọi `TrySpend` không có ngữ cảnh hành động người chơi** (ví dụ Neighbor muốn "phạt"): từ chối, số dư giữ nguyên (Pillar 4).
- **If `Add` hoặc `TrySpend` nhận số âm, 0, NaN hoặc vô cực**: từ chối, không sự kiện. Không có đường nào đưa NaN vào ví.
- **If số dư đạt `walletSafeCap`**: phần vượt bị bỏ; đây là giới hạn kỹ thuật (≈ 1e300) không thể đạt trong thiết kế hiện tại, không phải luật chơi.
- **If save chứa `currencyId` chưa đăng ký** (save từ bản mới hơn, hoặc Tem Tiệm trong bản MVP): giữ nguyên cặp id/giá trị trong dữ liệu lưu (không hiện, không tiêu được) để không mất khi nâng cấp.
- **If save có số dư âm, NaN hoặc vượt trần** (file bị sửa/hỏng): kẹp về [0, `walletSafeCap`], NaN → 0.

**Pipeline và modifier**
- **If một modifier có `value` âm, NaN hoặc vô cực**: coi là 0 (trung tính); các modifier khác vẫn tính.
- **If một hệ đăng ký sai tầng** (`shelf.` vào Boost/Global, `boost.` vào Synergy): đăng ký bị từ chối, không ảnh hưởng thu nhập; validator trong Editor báo lỗi cấu hình.
- **If đăng ký hai lần cùng `sourceId` và cùng đích**: bản sau thay bản trước (không cộng đôi). `Unregister` một `sourceId` không tồn tại: không làm gì.
- **If tổng modifier một tầng vượt trần** (ví dụ ba boost +1.0 cùng lúc): `M_tầng` = trần; phần vượt bị bỏ. Boosts/Shop nên báo người chơi khi một boost mới không còn tác dụng (UI Requirements).
- **If kệ trống**: `idle_cps` = 0; modifier kênh Idle không có gì để nhân; Plinko vẫn kiếm tiền. Người chơi không bao giờ kẹt vì Plinko luôn sinh xu (không có trạng thái thua).
- **If Trà hết hạn giữa lúc xu đang rơi**: xu được tính với modifier tại thời điểm giải quyết (R5.2) — không có ×2 "dính" theo xu.
- **If thú rời kệ ngay khi xu của nó đang rơi**: thưởng sự kiện chưa gửi thì không được tính; thưởng đã `SubmitIncome` thì giữ.
- **If `CoinResolved` có `rawValue` âm, NaN hoặc vô cực** (lỗi Plinko): bỏ qua khoản đó, không cộng ví; `rawValue` = 0 (hũ hệ số 0 trong cấu hình thử) → cộng 0, vẫn phát `IncomeCredited` với 0.
- **If cùng một xu phát `CoinResolved` hai lần** (vi phạm Plinko R9.1): Economy Core cộng cả hai; chặn trùng là trách nhiệm của Plinko (Plinko AC-26). Ghi nhận để test tích hợp bắt được.
- **If hàng chục xu được giải quyết trong một khung** (Mưa Đồng Xu): mỗi xu cộng riêng, `IncomeCredited` phát cho từng xu; `WalletChanged` có thể gộp theo khung cho HUD (R15) nhưng tổng delta phải bằng tổng `creditedAmount`.

**Thời gian**
- **If khung hình giật (dt > `maxFrameDelta`)**: chỉ cộng `idle_cps × maxFrameDelta`; phần còn lại mất. Chấp nhận vì giật hình ngắn; khoảng vắng dài thuộc Offline Earnings.
- **If dt âm hoặc NaN** (đồng hồ hệ thống nhảy lùi): coi là 0.
- **If trang bị ẩn khi rèm khởi động còn (vòng đời `Booting.Curtain`) rồi hiện lại**: `Paused` → Paused; `Resumed` đưa vòng đời về `Booting.Curtain` nên `IsSimulationRunning` sai → Economy Core về Ready, không cộng Idle cho tới `Interactive` (R6.3, AC-22).
- **If ứng dụng bị ẩn / xuống nền**: không cộng Idle (Paused); khi quay lại, Offline Earnings tính phần vắng mặt. Không có khoảng thời gian nào được cộng hai lần (Idle và Offline loại trừ nhau theo cùng tín hiệu tạm dừng).
- **If chuyển sang Tab 2/3**: Idle vẫn cộng, Plinko vẫn giải quyết xu (Plinko R11.1).

**Giá và tiến độ**
- **If hai lượt quay liên tiếp có cùng giá** (làm tròn đẹp, ví dụ n = 2 và n = 3 đều 120): hợp lệ; giá không bao giờ giảm.
- **If người chơi tháo thú khỏi kệ trước khi mua boost**: giá không đổi (D2) — không có lợi ích khai thác.
- **If người chơi trì hoãn mua ô kệ / boost để quay nhiều lần**: giá món đó tăng theo giá quay. Đây là chủ ý (mọi giá đi cùng một đường neo), Shop nên hiện giá đang tăng ở nút (UI Requirements).
- **If n đạt mức làm `spin_price_raw` vượt `walletSafeCap`**: giá kẹp tại trần (không thể mua) — chỉ xảy ra sau ~14 000 lượt trong một tiệm; Prestige đến rất lâu trước đó.
- **If hệ số giá của một món ≤ 0 hoặc không hợp lệ**: món đó không có giá; hệ bán món phải khóa nút. Không bao giờ bán miễn phí do lỗi dữ liệu.
- **If người chơi prestige khi đang có Trà**: `ResetForNewShop` không tự gỡ modifier; Boosts quyết định boost có giữ qua tiệm mới hay không (Prestige GDD). Economy Core chỉ đặt lại xu, n và giá.
- **If prestige xảy ra khi còn xu đang rơi trên bàn Plinko**: các xu đó giải quyết sau reset với `baseCoinValue` mới (= 1) và cộng vào ví tiệm mới. Prestige GDD có thể chọn khóa nút "Mở Tiệm Mới" khi bàn còn xu (Open Question).

**Chiến lược suy biến**
- **Chỉ chạm Plinko, bỏ kệ**: được (không phạt), nhưng `seconds_per_spin` tăng theo 1.05^(0.4n) nên giữa tiệm cần thú; đây là "tường mềm" có chủ ý.
- **Chỉ idle, không chạm**: được; kênh Plinko vẫn có auto-drop nếu xếp thú auto-drop. Hai kênh có hệ số riêng nên không có đội hình tối ưu cho cả hai (Pillar 3 — kiểm bằng mô phỏng ở Shelf).
- **Autoclicker trên Plinko**: Plinko chặn xu `Manual` ở `tapCooldown` (6.67 chạm/s), nhưng thông lượng thô của kênh Plinko **không** có trần cứng: xu Bùng Xu tràn hàng đợi được trả ngay (Overflow), nên thu nhập thô đi theo `raw_rate_ceiling` của cấu hình (≈ 20 × `baseCoinValue`/s ở độ nảy gốc không auto-drop, tới ≤ ≈ 42 khi auto-drop ở sàn và độ nảy ở trần). Đây là trần mềm có chủ ý (quyết định Plinko (a)): Economy Core không kẹp (R5.5) mà giữ kênh trong tầm bằng cân bằng — `plinko_rate_model`, trần tầng kênh Plinko, `plinkoFlatPerCoinCap` — kiểm bằng mô phỏng (Open Questions).
- **Chồng boost**: bị chặn bởi `boostTierCap` và vì các boost cùng tầng cộng chứ không nhân.

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Plinko Board | **Economy Core phụ thuộc hợp đồng của Plinko** (upstream duy nhất về gameplay): sự kiện `CoinResolved` và interface `IBaseCoinValueSource`, cả hai do Plinko sở hữu. Economy Core chỉ tham chiếu hợp đồng, không tham chiếu cài đặt Plinko và không ghi gì vào Plinko; Plinko không tham chiếu Economy Core | Hard (với Economy Core); Soft (với Plinko: chạy được với dự phòng `PlinkoConfig.baseCoinValue`, không khai `RunsAfter` `economy.core`) | In: `CoinResolved { coinId, origin, bucketIndex \| none, multiplier, rawValue, scoredPegHits, reason ∈ {Bucket, Stuck, Overflow} }`. Out: cài đặt `IBaseCoinValueSource` (getter `BaseCoinValue` = `base_coin_value(n)` hiện tại), đăng ký qua cơ chế đăng ký dịch vụ (ADR); Plinko kéo giá trị lúc giải quyết xu (R10.2–R10.4). Owner: Plinko (sự kiện, interface, dự phòng); Economy Core (cài đặt, modifier, ví, công thức). Thông lượng: Plinko chỉ cho trần mô hình `raw_rate_ceiling`, không trần cứng (R5.5). Khớp `plinko-board.md` Revision 3–4 (R9.2, R12, `raw_rate_ceiling`, AC-45). |
| Bootstrap & Scene Flow | Economy Core phụ thuộc (Bootstrap không tham chiếu Economy Core) | Hard | In: hợp đồng bước khởi động `IBootStep` và thứ tự pha (bước `GameplayInit` chạy sau `SaveLoad`); `Paused {reason}` / `Resumed {reason, awaySeconds}` (cùng tín hiệu với Plinko R11); `Interactive` + `IsSimulationRunning` (chỉ cộng Idle khi vòng đời `Running`; đọc `IsSimulationRunning` sau `Resumed`, B6.5 — R6.2–R6.3, trạng thái Ready); `AdClosed {adSeconds}` (B6.10.5, R6.4). Out: bước `economy.core` (pha `GameplayInit`, cốt lõi, R16). Owner: Bootstrap (hợp đồng pha, sự kiện vòng đời); Economy Core (bước của mình). Khớp hàng Economy Core trong Dependencies của `bootstrap-and-scene-flow.md`. |
| Save/Load (VS) | Economy Core phụ thuộc (đăng ký dữ liệu) | Soft, VS (MVP chạy không lưu) | Out: mảng cặp `currencyId / double`, `spinsThisShop`. Owner: Save/Load (định dạng file, version); Economy Core (nội dung, kẹp khi nạp). |
| Gacha Machine | Gacha phụ thuộc Economy Core | Hard (với Gacha) | In: `TryPaySpin(playerAction)` → `bool`, `GetSpinPrice()`, `CanAfford`. Out: `SpinPriceChanged { spinsThisShop, spinPrice, baseCoinValue }`, `SpendRejected`. Owner: Economy Core (đường neo, D1); khớp `gacha-machine.md` (G3, G4). |
| Shelf & Synergy | Shelf phụ thuộc Economy Core | Hard (với Shelf) | In: `RegisterIncomeSource`, modifier tầng Base/Synergy (`shelf.`), `SubmitIncome(Plinko)`, `TrySpend(ShopPurchase)` mở ô. Out: `idleCpsUnit`, `baseCoinValue`, `derived_price`, `IncomeRatesChanged`, `PreviewIdleCps(plan)`, `GetSourceIdleCps(sourceKey)` (khóa như `shelf.slot2` → `shelf.slot2.idle`; Shelf ẩn Δ và nhãn CPS ô khi thiếu hai hàm). Owner: Economy Core (hợp đồng, trần tầng, hai hàm thuần); Shelf (giá trị, giá ô). Khớp `shelf-and-synergy.md` S7. |
| Inventory | Inventory chỉ khai thứ tự khởi tạo sau Economy Core | Soft (chỉ thứ tự) | Không có dữ liệu vào/ra; `inventory.core` `RunsAfter` `economy.core` (R16.2). Owner: Bootstrap. Khớp `inventory.md`. |
| Pet Catalog | Pet Catalog phụ thuộc Economy Core | Soft (lúc làm nội dung: validator của Pet đọc `EconomyConfig`; runtime Economy không gọi Catalog) | Catalog giữ hệ số không thứ nguyên; Shelf nhân với `idleCpsUnit`, thưởng kênh Plinko theo bội số `baseCoinValue` (Pet D4). |
| HUD & Tab Navigation | HUD phụ thuộc Economy Core | Hard (với HUD) | Out: số dư, `IdleCps`, `PlinkoRateAvg`, chuỗi định dạng, `WalletChanged`, `IncomeRatesChanged`, `SpinPriceChanged`. Owner: Economy Core. |
| Feedback & Juice | Feedback phụ thuộc Economy Core | Hard (với Feedback, cho số bay) | Out: `IncomeCredited` (`creditedAmount`, `coinId`, `bucketIndex`), `SpendRejected`. Owner: Economy Core. |
| Star Progression (VS) | Gián tiếp qua Shelf | Soft | Hệ số Sao vào tốc độ gốc do Shelf đăng ký; Economy Core không có interface riêng cho Star trừ khi Star có chi phí bằng xu (khi đó dùng `derived_price`). |
| Boosts (VS) | Boosts phụ thuộc Economy Core | Hard (với Boosts) | In: modifier tầng Boost (`boost.`, `isTemporary`), `TrySpend`, `GetDerivedPrice(priceCoef)`. Owner: Economy Core (họ giá, trần tầng); Boosts (hệ số giá, giá trị, thời lượng). |
| Shop (VS) | Shop phụ thuộc Economy Core | Hard (với Shop) | In: `TrySpend(ShopPurchase)`, `GetDerivedPrice`. Out: `SpinPriceChanged` để làm mới giá. |
| Offline Earnings (VS) | Offline phụ thuộc Economy Core | Hard (với Offline) | Out: `OfflineRateSnapshot`. In: `Add(Offline)`. |
| Rewarded Ads (Alpha) | Ads phụ thuộc Economy Core | Hard (với Ads) | In: `Add(AdReward)`, modifier tầng Boost (`ad.`), `GetDerivedPrice` cho quà. |
| Prestige (Alpha) | Prestige phụ thuộc Economy Core | Hard (với Prestige) | In: `ResetForNewShop(playerAction)`, `Add(currency.shop_stamp, Prestige)`, modifier tầng Prestige, `TrySpend(PrestigeUpgrade)`. Out: `spinsThisShop`, `ShopEconomyReset`. |
| Neighbor Pranks (v1.1) | Neighbor phụ thuộc Economy Core | Hard (với Neighbor) | In: `TrySpend(NeighborCandy)` do người chơi trả; interface giữ tạm thu nhập mới (R4.9). Không có API trừ ví. |
| Analytics Hooks (v1.1) | Analytics nghe | Soft | Out: mọi sự kiện R15. |

**Hai chiều**: Plinko Board liệt kê Economy Core khớp GDD này — Revision 3: `IBaseCoinValueSource` nằm trong hợp đồng của Plinko (R12), Plinko kéo `baseCoinValue` lúc giải quyết xu (R9.2), `CoinResolved` có `coinId` và enum `reason`, AC-45 cho dự phòng; Revision 4: `raw_rate_ceiling` là trần mô hình, khớp R5.5. Pet Catalog liệt kê Economy Core (Revision 2: `plinko_effect_uplift` đọc giả định đo từ `EconomyBalanceTargets`); còn chuyển nốt `bounceUpliftAtCapAssumed` sang `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` (Open Questions). Bootstrap & Scene Flow (Revision 4) liệt kê Economy Core (bước `GameplayInit` cốt lõi, `Paused`/`Resumed`, `Interactive` + `IsSimulationRunning` theo B6.5) khớp R6.2–R6.3, R16 và trạng thái Ready của bản fix round 1; phần Economy của Q8 trong `bootstrap-and-scene-flow.md` đã được xử lý ở đây, Bootstrap đóng phần đó ở bước Sync (phần Plinko / Audio / Pooling của Q8 vẫn mở). Systems index đã ghi Plinko Board, Bootstrap & Scene Flow và Save/Load trong Depends On của Economy Core, và Economy Core trong Depends On của Feedback & Juice. Các hệ downstream còn lại chưa có GDD; khi viết, mỗi GDD phải liệt kê Economy Core với interface ở trên.

## Tuning Knobs

Hai asset: `EconomyConfig` (runtime) và `EconomyBalanceTargets` (chỉ test/mô phỏng, không ảnh hưởng gameplay), cùng mẫu với `PlinkoConfig` / `PlinkoBalanceTargets`. Tiền tệ là các asset `CurrencyDefinition` được `EconomyConfig.currencies` liệt kê. Validator trong Editor cảnh báo giá trị ngoài safe range. Mọi giá trị là **tạm** cho tới mô phỏng sau Pet Catalog / Shelf.

**Ví và tiền tệ**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Danh sách tiền tệ | `EconomyConfig.currencies` | CurrencyDefinition[] | `currency.coin` (MVP), `currency.shop_stamp` (Alpha) | id bất biến, không trùng | — | — | Save/Load, Prestige |
| Xu reset khi tiệm mới | `CurrencyDefinition.resetsOnNewShop` | bool | Xu true, Tem false | — | — | — | Prestige |
| Xu khởi đầu | `EconomyConfig.startingCoins` | double | 0 | 0–100 | Bỏ qua bước "chạm để có xu" của onboarding | — (0 là sàn) | spinBasePrice, firstSpinMaxSeconds |
| Trần an toàn ví | `EconomyConfig.walletSafeCap` | double | 1e300 | 1e100–1e305 | Tràn double khi nhân tầng | Người chơi lâu năm chạm trần | prestigeTierCap |

**Đường giá neo và giá trị xu gốc**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Giá quay gốc | `EconomyConfig.spinBasePrice` | double | 100 | 50–200 | Lượt quay đầu > 30 s, hụt hook | Gacha quá dễ, bộ sưu tập xong sớm | firstSpinMaxSeconds, mọi `priceCoef` |
| Hệ số tăng giá quay | `EconomyConfig.spinGrowth` | double | 1.05 | 1.03–1.08 | Tường gắt, prestige quá sớm | Không có tường mềm, prestige mất lý do | coinValueExponent, Star, Prestige |
| Số mũ giá trị xu gốc (α) | `EconomyConfig.coinValueExponent` | double | 0.6 | 0.5–0.7 | Plinko theo kịp giá, thú kênh Plinko thừa | Plinko vô nghĩa giữa tiệm | spinGrowth, thú kênh Plinko |
| Chữ số có nghĩa của giá đẹp | `EconomyConfig.priceNiceSigDigits` | int | 2 | 2–3 | Giá lẻ khó đọc | — (2 là sàn) | spin_price |
| Đơn vị CPS gốc | `EconomyConfig.idleCpsUnit` | double (xu/s) | 2.0 | 1–5 | Kênh Idle lấn Plinko | Kệ không có cảm giác | Pet Catalog (bảng phẩm cấp), Plinko EV; hằng số trong tiệm (R11.2, quyết định (A)) |

**Income Pipeline**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Trần tầng Synergy | `EconomyConfig.synergyTierCap` | double | 4.0 | 2–8 | Một đội hình combo thống trị (Pillar 3) | Combo không đáng xếp | Shelf & Synergy |
| Trần tầng Boost | `EconomyConfig.boostTierCap` | double | 3.0 | 2–5 | Chồng boost lạm phát | Boost thứ hai vô dụng | Boosts, Rewarded Ads |
| Trần tầng Prestige | `EconomyConfig.prestigeTierCap` | double | 10.0 (*provisional*) | 3–50 | Tiệm mới quá nhanh, mất nhịp | Tem Tiệm không đáng | Prestige |
| Trần tầng Global | `EconomyConfig.globalTierCap` | double | 2.0 | 1–3 | Sự kiện phá cân bằng | — | live-ops |
| Trần cộng phẳng mỗi xu Plinko | `EconomyConfig.plinkoFlatPerCoinCap` | double (× `baseCoinValue`) | 2.0 | 0–5 | Kênh Plinko lạm phát | Hiệu ứng "+N mỗi xu" vô nghĩa | bucketMultipliers (Plinko) |
| Chủ sở hữu theo tầng | `EconomyConfig.tierOwnerPrefixes` | bảng tầng → tiền tố | Base/Synergy: `shelf.`; Boost: `boost.`, `ad.`; Prestige: `prestige.`; Global: `event.` | không cho `shelf.`/`pet.` vào Boost/Prestige/Global | — | — | R4.7 |

**Thời gian, tốc độ, hiển thị**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Trần delta một khung | `EconomyConfig.maxFrameDelta` | double (s) | 0.25 | 0.1–0.5 | Nhảy tiền sau giật hình | Thiết bị yếu (< 4 fps) mất thu nhập | targetFps |
| Trần giây bù thời gian xem ad | `EconomyConfig.adCompensationMaxSeconds` | double (s) | 90 | 30–300 | Xem ad thành nguồn Idle lớn, lấn Plinko (Pillar 3) | Quảng cáo dài không được bù, người chơi thấy thiệt | Rewarded Ads, `idle_cps` |
| Cửa sổ trung bình Plinko | `EconomyConfig.plinkoRateWindowSeconds` | double (s) | 10 | 5–30 | Số chậm phản ứng khi đổi kệ | Số nhảy loạn theo jackpot | HUD |
| Nhịp công bố tốc độ | `EconomyConfig.ratesPublishInterval` | double (s) | 0.5 | 0.25–1.0 | So sánh trước/sau trễ | Nhiều sự kiện, tốn CPU | HUD |
| Chữ số có nghĩa hiển thị | `EconomyConfig.displaySigDigits` | int | 3 | 3–4 | Chuỗi dài, tràn header | — (3 là sàn) | HUD layout |
| Chữ số thập phân của tốc độ | `EconomyConfig.rateDecimals` | int | 1 | 0–2 | Rối mắt | CPS 2.5 hiện "2" | HUD |

**Mục tiêu cân bằng (chỉ test/mô phỏng)**

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Nhịp chạm thực tế | `EconomyBalanceTargets.realisticTapRate` | double (chạm/s) | 3.0 | 2–5 | Mô hình lạc quan | Mô hình bi quan | tapCooldown (Plinko) |
| EV hiệu dụng giả định của Plinko | `EconomyBalanceTargets.plinkoEvEffectiveAssumed` | double | 2.96 | 2.6–3.3 | — | — | Plinko AC-30 (cập nhật theo kết quả đo) |
| Tăng EV hiệu dụng khi độ nảy ở trần (giả định) | `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed` | double | 0.40 | 0.2–0.55 | Kẹo Bảy Màu bị cân yếu | Kẹo Bảy Màu bị cân mạnh | `bouncinessCap` (Plinko), cập nhật theo Plinko AC-13c / AC-30; dùng bởi `pet-catalog.md` `bounce_uplift_at_cap` (chuyển từ `PetBalanceTargets.bounceUpliftAtCapAssumed`, C2[12]) |
| Trần mô hình thông lượng thô kênh Plinko theo cấu hình (giả định) | `EconomyBalanceTargets.plinkoRawRateCeilingAssumed` | double[4] (× `baseCoinValue` / s) | [20, 27.1, 30.7, 42] (BaseNoAuto, AutoFloor, BounceCap, AutoFloorAndBounceCap) | [18–24, 24–32, 26–36, 36–48] | Mô phỏng lạc quan cho Plinko, Idle bị cân yếu | Mô phỏng bỏ sót lạm phát kênh Plinko | `raw_rate_ceiling` (Plinko), `PlinkoBalanceTargets.maxRawPayoutPerSecondPerBaseCoin*`, cập nhật theo Plinko AC-13 / AC-13b / AC-13c; `plinko_rate_model` (R5.5) |
| Xác suất jackpot trung bình giả định của Plinko | `EconomyBalanceTargets.plinkoJackpotProbMeanAssumed` | double | 0.109 | 0.08–0.15 | — | — | cập nhật theo Plinko AC-30 (p_jackpot trung bình vùng thả, `plinko-board.md`); dùng bởi `pet-catalog.md` `plinko_effect_uplift` |
| Số `PegHit` được tính trung bình mỗi xu (giả định) | `EconomyBalanceTargets.plinkoScoredPegHitsPerCoinAssumed` | double | 14 | 10–20 | — | — | cập nhật theo Plinko AC-30 (Plinko ghi 12–16); dùng bởi `pet-catalog.md` `plinko_effect_uplift` |
| Trần thời gian lượt quay đầu | `EconomyBalanceTargets.firstSpinMaxSeconds` | double (s) | 30 | 15–45 | Hook chậm | — | spinBasePrice, startingCoins |
| Mốc n kiểm tra | `EconomyBalanceTargets.spinCheckpoints` | int[3] | [0, 50, 100] | 0–200 | — | — | spinGrowth |
| Trần giây/lượt chỉ Plinko tại mốc | `EconomyBalanceTargets.plinkoOnlySpinSecondsMax` | double[3] (s) | [30, 60, 120] | 15–300 | Tường đến muộn | Bắt buộc dùng kệ quá sớm | spinGrowth, coinValueExponent |
| Ngân sách thời gian pipeline (p95) | `EconomyBalanceTargets.pipelineFrameBudgetMs` | double (ms) | 0.2 | 0.1–0.5 | Lấn ngân sách 16.7 ms | — | số modifier, maxActiveCoins |
| Dung sai số học của test | `EconomyBalanceTargets.valueRelTolerance` | double | 1e-9 | 1e-12–1e-6 | Không bắt được lỗi tính | Test trượt do làm tròn double | — |
| Hệ số quá tải của test trần mềm | `EconomyBalanceTargets.plinkoSoftCapTestFactor` | double | 1.5 | 1.1–3.0 | — | Test không vượt mọi cấu hình mô hình | `plinkoRawRateCeilingAssumed`, AC-47 |

**Knob của hệ khác mà Economy Core tham chiếu** (không lặp lại ở đây; giá trị là gợi ý tạm để hệ sở hữu xác nhận)

| Knob | Asset.field (hệ sở hữu) | Gợi ý | Ghi chú |
|---|---|---|---|
| Hệ số giá Trà Sốt Dẻo | `BoostDefinition.priceCoef` (Boosts) | 1.5 | FirstGDD 150 xu ở giá quay 100 |
| Hệ số giá Mưa Đồng Xu | `BoostDefinition.priceCoef` (Boosts) | 2.0 | FirstGDD 200 xu |
| Hệ số quà Bác Bảo Vệ | `ShopConfig.guardGiftCoef` (Shop / Rewarded Ads) | 1.5 | FirstGDD 150 xu |
| Hệ số giá ô kệ thứ 4, 5 | `ShelfConfig.slotPriceCoefs` (Shelf / Shop) | — | do Shop/Shelf đặt |
| Thời gian rơi trung vị | `PlinkoBalanceTargets.targetMedianFallTime` (Plinko) | 2.5 s | dùng trong `first_spin_time` |
| Xu gốc dự phòng | `PlinkoConfig.baseCoinValue` (Plinko) | 1 (safe range = 1, như `plinko-board.md`) | chỉ cho test/prototype, dùng khi không có `IBaseCoinValueSource` nào được đăng ký (R10.3); không phải giá trị runtime — giá trị runtime theo `base_coin_value`, không có trần trên |

## Visual/Audio Requirements

Economy Core không vẽ gì; nó phát sự kiện, còn HUD & Tab Navigation và Feedback & Juice quyết định hình và tiếng. Bảng dưới đây là ý đồ để các GDD đó nhận (art-director không được gọi — lean, Category Economy). Theo concept: vàng mật ong `#F59E0B` cho xu/phần thưởng, không dùng coral cho thông báo "chưa đủ".

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| `IncomeCredited` (Plinko) | Số bay "+n" = `creditedAmount` (định dạng R13); khi đang có boost, số bay màu mật ong | Thuộc Plinko (tiếng hũ) | MVP |
| `WalletChanged` tăng | Số xu trên header lăn lên (count-up) thay vì nhảy | Không (tránh spam âm thanh khi Idle cộng mỗi khung) | MVP |
| `SpendRejected` | Nút vừa bấm lắc nhẹ, số xu trên header nháy một lần; không chữ đỏ, không coral | "Tok" gỗ bị bịt rất khẽ (cùng họ với "bàn bận" của Plinko) | MVP |
| `TryPaySpin` thành công | Số xu trên header trừ đi có tween ngắn; giá trên nút vặn trứng đổi sang giá mới bằng tween | Thuộc Gacha (tiếng vặn cần) | MVP |
| `SpinPriceChanged` | Giá trên nút vặn trứng và các giá trong Tiệm cập nhật | Không | MVP |
| `ShopEconomyReset` | Thuộc Prestige (hoạt cảnh "Mở Tiệm Mới") | Thuộc Prestige | Alpha |

## Game Feel

Không bắt buộc (Category Economy, không phải Gameplay/UI). Ràng buộc cảm giác duy nhất của hệ này là **độ trễ**: tiền phải vào ví và phát sự kiện trong cùng khung với `CoinResolved`, để số bay của Plinko (`resolveToFeedbackMaxMs` = 34 ms của Plinko) hiện đúng giá trị sau modifier.

| Action | Max input-to-response (ms) | Frames at target fps | Notes |
|---|---|---|---|
| `CoinResolved` → `IncomeCredited` + `WalletChanged` | 0 (cùng khung) | 0 | Đo bằng AC-17 |
| Bấm vặn trứng → kết quả `TryPaySpin` | 0 (đồng bộ) | 0 | Gacha nhận `bool` ngay |
| Đổi kệ → `IncomeRatesChanged` | `ratesPublishInterval` (500) | 30 | Cho so sánh CPS trước/sau |

Feel reference: số tiền lăn mượt trong Cookie Clicker (tiền luôn "chảy"). Anti-reference: idle game hiện ký hiệu khoa học (1.2e15) với khán giả casual.

## UI Requirements

Economy Core cung cấp dữ liệu và chuỗi đã định dạng; vị trí và kiểu hiển thị thuộc HUD & Tab Navigation, Gacha, Shop.

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Số Xu (định dạng R13) | Header, ô 🪙 | Mỗi `WalletChanged` (tối đa 1 lần/khung) | Luôn hiện |
| CPS Idle (`IdleCps`, "/s") | Header, ô +CPS | Mỗi `IncomeRatesChanged` | Luôn hiện |
| Tốc độ Plinko (`PlinkoRateAvg`) | Header hoặc Tab 1 (HUD quyết định) | Mỗi `IncomeRatesChanged` | Khi > 0 |
| CPS trước/sau khi đổi kệ, từng kênh | Tab 2 (Kệ) | Khi thả thú vào ô | Shelf & Synergy quyết định cách so sánh |
| Giá lượt quay hiện tại | Nút vặn trứng, Tab 1 | Mỗi `SpinPriceChanged` | Luôn hiện; nút tắt khi `CanAfford` = false |
| Giá món trong Tiệm (`derived_price`) | Tab 3 | Mỗi `SpinPriceChanged` | VS |
| Báo boost mới không còn tác dụng (tầng Boost đã chạm trần) | Nút mua boost, Tab 3 | Khi tầng Boost = `boostTierCap` | VS |
| Số Tem Tiệm | Màn Prestige | Mỗi `WalletChanged` của `currency.shop_stamp` | Alpha |

UX flag: this system has UI. A UX spec per screen is recommended before epics; UX authoring is not in this plugin yet, so describe each screen in this section or a short spec next to the GDD. Màn hình thực tế thuộc HUD & Tab Navigation (header) và Shop (Tab 3); Penpot là đặc tả bố cục hiện có.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Pillars, Quyết định 1/4/5, MVP | `Docs/design/gdd/game-concept.md` | Pillar 3 (hai kênh), Pillar 4 (không trừ ví), Anti-Pillar nâng chỉ số tĩnh, Tem Tiệm | Ràng buộc thiết kế |
| Hợp đồng sự kiện Plinko | `Docs/design/gdd/plinko-board.md` | `CoinResolved` (R9.2), origin/reason, `raw_payout`, `coin_rate_ceiling`, `raw_rate_ceiling` (trần mô hình, không trần cứng — R5.5), EV hiệu dụng 2.96, `targetMedianFallTime`, `PlinkoConfig.baseCoinValue` (dự phòng = 1, chỉ cho test), `IBaseCoinValueSource` (R9.2, R12), AC-45 (dự phòng) | Economy Core tiêu thụ `CoinResolved`; cài đặt `IBaseCoinValueSource` để Plinko kéo `baseCoinValue` |
| Registry | `Docs/design/registry/entities.yaml` | `base_coin_value` (chuyển từ hằng số sang công thức, D4), `raw_payout`, `coin_rate_ceiling`, `raw_rate_ceiling`, `bucket_multipliers` | Tham chiếu / thay thế có duyệt |
| Sản lượng gốc thú | `Docs/design/gdd/pet-catalog.md` | `idle_base_units` (phẩm cấp × trọng số × `role_idle_share`) nhân `idleCpsUnit` (Pet D4); `plinko_effect_uplift` đọc `EconomyBalanceTargets.plinko*Assumed`; `bounce_uplift_at_cap` sẽ đọc `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`; `channel_parity` / `PetBalanceTargets.parityBand` (rủi ro của R11.2) | Catalog dùng đơn vị và giả định đo của Economy Core |
| Synergy, nguồn Idle | `Docs/design/gdd/shelf-and-synergy.md` *(chưa có)* | Đăng ký nguồn, modifier Base/Synergy, `SubmitIncome` | Shelf đăng ký vào pipeline |
| Giá quay | `Docs/design/gdd/gacha-machine.md` *(chưa có)* | `TryPaySpin`, `SpinPriceChanged` | Gacha tham chiếu đường neo |
| Header | `Docs/design/gdd/hud-and-tab-navigation.md` *(chưa có)* | Số dư, CPS, định dạng không GC | HUD hiển thị |
| Số bay | `Docs/design/gdd/feedback-and-juice.md` *(chưa có)* | `IncomeCredited.creditedAmount` | Feedback hiển thị |
| Boost, Tiệm, Offline, Ads, Prestige, Hàng xóm | `boosts.md`, `shop.md`, `offline-earnings.md`, `rewarded-ads.md`, `prestige.md`, `neighbor-pranks.md` *(chưa có)* | Họ giá, tầng Boost/Prestige, `OfflineRateSnapshot`, `ResetForNewShop`, giữ tạm thu nhập | Các hệ đó dùng API của Economy Core |
| Khởi động và tạm dừng mô phỏng | `Docs/design/gdd/bootstrap-and-scene-flow.md` | Hợp đồng `IBootStep` (B2), thứ tự pha (B3), `Interactive` (B5.3), `Paused` / `Resumed` (B6), `IsSimulationRunning` (B6.5), trạng thái trả về (B6.9) | Economy Core đăng ký bước `GameplayInit` và nghe sự kiện vòng đời |
| Lỗi chặn đã xử lý | `Docs/design/gdd/reviews/first-gdd-review-2026-10-21.md` | BLOCKING §7, §5.5/§8, phần giá của §7.1 | Truy vết |

## Acceptance Criteria

Viết bởi game-designer (qa-lead không được gọi — workflow không cho subagent). Ký hiệu `C.` = `EconomyConfig`, `T.` = `EconomyBalanceTargets`; số trong ngoặc là giá trị hiện tại của knob, không phải literal của test. So sánh số thực dùng sai số tương đối `T.valueRelTolerance`. Test EditMode dùng đồng hồ inject, Plinko giả phát `CoinResolved` và Bootstrap giả phát `Interactive` / `Paused` / `Resumed` và trả `IsSimulationRunning`; trừ khi AC ghi khác, Economy Core ở Running (sau `Run()` và `Interactive`, `IsSimulationRunning` đúng). "Ngữ cảnh hành động" là token test do ADR định nghĩa.

**Ví (R1, R2, R3, wallet_add)**
- `AC-1 [EditMode]` **GIVEN** `C` mặc định và game mới, **WHEN** khởi tạo, **THEN** chỉ các tiền tệ trong `C.currencies` có `releaseTier` ≤ bản build được đăng ký (MVP: đúng `currency.coin`), Xu = `C.startingCoins` (0), `spinsThisShop` = 0, `baseCoinValue` = 1, giá quay = `C.spinBasePrice` (100).
- `AC-2 [EditMode]` **GIVEN** Xu = 0, **WHEN** `Add(coin, 2.5)` hai lần, rồi từ 0 `Add(coin, 0.4)` ba lần, **THEN** số dư lần lượt là 5.0 và 1.2; chuỗi hiển thị "5" và "1"; mỗi lần `Add` thành công phát đúng một `WalletChanged` có `delta` = amount và đúng `reason`.
- `AC-3 [EditMode]` **GIVEN** Xu = 10, **WHEN** `Add` với amount ∈ {−1, 0, NaN, +∞} hoặc với `currencyId` chưa đăng ký, **THEN** trả `false`, số dư vẫn 10, không có sự kiện nào.
- `AC-4 [EditMode]` **GIVEN** Xu = `C.walletSafeCap` − 1, **WHEN** `Add(coin, C.walletSafeCap / 10)`, **THEN** Xu = `C.walletSafeCap`.
- `AC-5 [EditMode]` **GIVEN** Xu = 100 và có ngữ cảnh hành động, **WHEN** `TrySpend(coin, 100)`, **THEN** `true`, Xu = 0, một `WalletChanged` với delta −100; **GIVEN** Xu = 99.7, **WHEN** `TrySpend(coin, 100)`, **THEN** `false`, Xu = 99.7, đúng một `SpendRejected`; trong cả hai ca `CanAfford(coin, 100)` trước lệnh trả cùng kết quả với `TrySpend`.
- `AC-6 [EditMode]` **GIVEN** Xu = 500, **WHEN** gọi `TrySpend(coin, 100)` hoặc `ResetForNewShop` **không** có ngữ cảnh hành động, hoặc `TrySpend` với amount ∈ {−1, 0, NaN}, **THEN** trả `false` / không làm gì, Xu vẫn 500.
- `AC-7a [EditMode]` **GIVEN** assembly runtime chứa Economy Core, **WHEN** liệt kê bằng reflection mọi phương thức public (kể cả accessor của property) của mọi kiểu public trong assembly đó thuộc ví / Economy Core, **THEN** tập tên phương thức có ít nhất một tham số thuộc kiểu ngữ cảnh hành động người chơi (tên kiểu do ADR chốt) đúng bằng {`TrySpend`, `TryPaySpin`, `ResetForNewShop`}; không có property số dư nào có setter public, không có field public không `readonly` nào chứa số dư hay bảng ví, và không phương thức public nào trả về tham chiếu ghi được (`ref`) tới số dư (Pillar 4).
- `AC-7b [EditMode]` **GIVEN** Xu = 500 (Tem = 7 khi đã đăng ký), Economy Core ở Running, **WHEN** gọi bằng reflection mọi phương thức public còn lại (không thuộc tập của AC-7a), mỗi tham số lần lượt nhận từng giá trị biên — số thực {0, −1, NaN, +∞, −∞, `double.MaxValue`}, số nguyên {0, −1, `int.MaxValue`}, chuỗi/id {rỗng, null, `currency.unknown`, `currency.coin`, `sourceKey` lạ}, mọi giá trị của mỗi enum — trong khi các tham số khác giữ giá trị hợp lệ mặc định, rồi thêm một lần với mọi tham số cùng ở giá trị biên, **THEN** sau từng lời gọi Xu ≥ 500 và Tem ≥ 7, không có ngoại lệ nào thoát ra ngoài Economy Core, và không có `WalletChanged` nào có `delta` < 0.
- `AC-8 [EditMode]` **GIVEN** Xu = 0.1, Tem = 7, một id lạ `currency.future` = 3, `spinsThisShop` = 37 (VS), **WHEN** lưu rồi nạp lại, **THEN** mọi giá trị bằng nhau tuyệt đối (kể cả 1e300 trong lần chạy thứ hai), `currency.future` vẫn còn trong dữ liệu lưu lần sau; dữ liệu nạp có Xu = −5 / NaN / 1e306 được kẹp thành 0 / 0 / `C.walletSafeCap`.

**Pipeline (R4, multiplier_synergy, tier_multiplier, channel_income)**
- `AC-9 [EditMode]` **GIVEN** nguồn Idle A, B và nguồn `plinko.coin`, mỗi ca bắt đầu không có modifier, **WHEN** đăng ký (a) không gì; (b) hai Synergy kênh Idle +1.0; (c) Synergy kênh Idle tổng +4.2; (d) một Synergy nhắm A +0.5; (e) một Synergy kênh Idle +0.5; (f) một Synergy kênh Both +0.5, **THEN** (a) `M_Synergy(A)` = 1; (b) = 3; (c) = `C.synergyTierCap` (4.0); (d) `M_Synergy(A)` = 1.5, `M_Synergy(B)` = 1; (e) `M_Synergy(plinko.coin)` = 1; (f) `M_Synergy(A)` = `M_Synergy(plinko.coin)` = 1.5.
- `AC-10 [EditMode]` **GIVEN** hai modifier Boost +1.0 (một Both, một Plinko), **WHEN** tính, **THEN** `M_Boost(Plinko)` = min(3, `C.boostTierCap`) = 3 và `M_Boost(Idle)` = 2; tương tự tầng Prestige và Global bị kẹp tại `C.prestigeTierCap` và `C.globalTierCap` khi tổng vượt trần.
- `AC-11 [EditMode]` **GIVEN** một nguồn gốc 50, hai Synergy kênh +1.0, Boost +1.0, Global `event.test` +1.0, **WHEN** tính `channel_income`, **THEN** kết quả = 600.
- `AC-12 [EditMode]` **GIVEN** một cấu hình modifier hợp lệ cho kết quả X, **WHEN** thêm một modifier có value −0.5, NaN hoặc +∞ ở bất kỳ tầng nào, **THEN** kết quả vẫn bằng X.
- `AC-13 [EditMode]` **GIVEN** `C.tierOwnerPrefixes` mặc định, **WHEN** `shelf.x` đăng ký vào Boost, Prestige hoặc Global, hoặc `boost.x` đăng ký vào Synergy, **THEN** bị từ chối và `idle_cps` không đổi; `ad.x` vào Boost và `shelf.x` vào Synergy được nhận; validator báo lỗi nếu cấu hình cho `shelf.` vào Global.
- `AC-14 [EditMode]` **GIVEN** `idle_cps` = X, **WHEN** đăng ký hai lần cùng `sourceId` + đích với value 0.5 rồi `Unregister(sourceId)`, **THEN** sau lần đăng ký thứ hai chỉ tính một lần +0.5 (không +1.0), sau khi gỡ `idle_cps` = X; `Unregister` một id không tồn tại không đổi gì.

**Kênh Plinko (R5, plinko_credit)**
- `AC-15 [EditMode]` **GIVEN** modifier đã biết, **WHEN** Plinko giả phát `CoinResolved` cho mỗi tổ hợp origin {Manual, Auto, Rain, JackpotBurst} × reason {Bucket, Stuck, Overflow}, **THEN** mỗi sự kiện cộng đúng `plinko_credit`, phát đúng một `IncomeCredited` với `baseAmount` = rawValue, `coinId`, `bucketIndex` (kể cả none), origin và reason sao chép nguyên, và `creditedAmount` = delta của ví; **WHEN** `SubmitIncome(Plinko, shelf.slot1, 10)`, **THEN** `IncomeCredited` có `coinId` = none và `bucketIndex` = none.
- `AC-16 [EditMode]` **GIVEN** xu A sinh trước khi Trà (+1.0, Both) được đăng ký và giải quyết khi Trà còn, xu B giải quyết sau khi Trà bị gỡ, cùng rawValue r, **WHEN** giải quyết, **THEN** A được 2r, B được r.
- `AC-17 [EditMode]` **GIVEN** listener đếm sự kiện, **WHEN** `CoinResolved` được xử lý, **THEN** khi lệnh xử lý trả về, số dư đã tăng và `IncomeCredited` + `WalletChanged` đã phát trong cùng lời gọi (trễ 0 khung).
- `AC-18 [EditMode]` **GIVEN** n = 100, Synergy kênh Plinko +0.5, Trà +1.0, **WHEN** xu vào hũ x5 (rawValue = 5 × `base_coin_value(100)`), **THEN** credit = 280.2 (± 0.1); thêm cộng phẳng `plinko.coin` 0.4 → 302.6 (± 0.1); cộng phẳng 10 → dùng `C.plinkoFlatPerCoinCap` (2.0).
- `AC-19 [EditMode]` **GIVEN** Synergy nhắm `shelf.slot1` +0.5 và Trà +1.0, **WHEN** `SubmitIncome(Plinko, shelf.slot1, 10)`, **THEN** cộng 30; `SubmitIncome` với baseAmount ≤ 0 / NaN và `CoinResolved` với rawValue âm / NaN / vô cực bị bỏ qua, số dư không đổi.
- `AC-47 [EditMode]` (trần mềm, R5.5) **GIVEN** game mới (n = 0, `base_coin_value` = 1), không modifier, Plinko giả phát mỗi bước dt = 1/60 s một `CoinResolved` (`Bucket`) với `rawValue` = `T.plinkoSoftCapTestFactor` × `T.plinkoRawRateCeilingAssumed[3]` / 60 (tổng 1.5 × 42 = 63 xu thô/s, cao hơn mọi cấu hình mô hình), **WHEN** chạy `C.plinkoRateWindowSeconds` (10 s) mô phỏng, **THEN** số dư tăng đúng Σ `rawValue` (630) trong sai số `T.valueRelTolerance`, số `IncomeCredited` bằng số `CoinResolved` (600) và mỗi `creditedAmount` = `rawValue` (không khoản nào bị kẹp hay bỏ), và `PlinkoRateAvg` = 63.

**Kênh Idle (R6, idle_cps, idle_accrual)**
- `AC-20 [EditMode]` **GIVEN** `C.idleCpsUnit` = 2, nguồn A gốc 10 với Synergy nhắm A +0.5, nguồn B gốc 4 với cộng phẳng 1 đơn vị, Synergy kênh Idle +0.5, Trà +1.0, **WHEN** tính, **THEN** `idle_cps` = 58.
- `AC-21 [EditMode]` **GIVEN** `idle_cps` = 58, **WHEN** 60 bước dt = 1/60 s; một bước dt = 1.0 s; một bước dt âm hoặc NaN, **THEN** số dư tăng lần lượt 58, 58 × `C.maxFrameDelta` (14.5), 0.
- `AC-22 [EditMode]` **GIVEN** `idle_cps` > 0, bước dt = 1/60 s và Bootstrap giả điều khiển được `IsSimulationRunning`, **WHEN** (a) ngay sau `Run()` (`IsSimulationRunning` sai, vòng đời giả ở `Booting.Curtain`) chạy 600 bước, rồi phát `Interactive` (`IsSimulationRunning` đúng) và chạy 60 bước; (b) từ Running phát `Paused {Hidden}`, chạy 600 bước, rồi phát `Resumed {Hidden, 10}` với `IsSimulationRunning` đúng và chạy 60 bước; (c) trước `Interactive`, phát `Paused {Hidden}` (vòng đời giả ở `Booting.Curtain`), chạy 600 bước, phát `Resumed {Hidden, 10}` với `IsSimulationRunning` sai, chạy 600 bước, rồi phát `Interactive` và chạy 60 bước; (d) ở Running, Bootstrap giả đặt `IsSimulationRunning` sai mà không phát sự kiện nào, chạy 60 bước, **THEN** (a) 600 bước đầu: Economy Core ở Ready, số dư không đổi, không có `WalletChanged`; sau `Interactive`: Running, số dư tăng `idle_cps` × 1 s (sai số `T.valueRelTolerance`); (b) không có `WalletChanged` và số dư không đổi trong lúc tạm dừng; sau `Resumed`: Running, cộng Idle chạy lại ở bước kế tiếp, tổng tăng `idle_cps` × 1 s, không cộng gì cho `awaySeconds`; (c) sau `Resumed` Economy Core ở Ready (không phải Running), số dư không đổi trong cả 1,200 bước trước `Interactive`, và chỉ tăng (`idle_cps` × 1 s) trong 60 bước sau `Interactive`; (d) số dư không đổi (dt = 0, R6.2).
- `AC-48 [EditMode]` **GIVEN** Running, `idle_cps` = 58, `C.adCompensationMaxSeconds` (90), **WHEN** `AdClosed` đến: (a) `{30}` sau khi cờ ad giả bật mà không có khung nào chạy; (b) `{30}` sau 1,800 bước dt = 1/60 s với cờ ad bật; (c) `{200}` như (a); (d) `{30}` khi `IsSimulationRunning` sai (Ready); (e) `{NaN}` và `{−5}`, **THEN** phần bù (ngoài 58 × giây đã cộng ở `idle_accrual`) lần lượt = 1,740 (một `WalletChanged` với `reason` = AdIdleCompensation), 0 (tổng tăng đúng 58 × 30, không cộng đôi), 5,220, 0, 0; `PlinkoRateAvg` và mọi modifier Plinko không đổi.
- `AC-23 [Play mode]` **GIVEN** scene đầy đủ, `idle_cps` = 10, **WHEN** chuyển sang Tab 2 trong 5 s thời gian mô phỏng, **THEN** số dư tăng 50 ± 10 × `C.maxFrameDelta`.
- `AC-24 [EditMode]` **GIVEN** bộ đếm số lần tính lại `idle_cps`, **WHEN** chạy 600 khung không đăng ký/gỡ gì, rồi đăng ký 3 modifier, **THEN** bộ đếm tăng 0 trong 600 khung đầu và ≤ 3 sau đó.

**Tốc độ (R7, plinko_rate_avg, offline_rate_snapshot)**
- `AC-25 [EditMode]` **GIVEN** `C.plinkoRateWindowSeconds` = 10, **WHEN** cộng 100 xu Plinko mỗi giây trong 10 s; hoặc 300 trong 3 s đầu; hoặc 50 trong 0.4 s đầu; hoặc ngừng cộng thêm 10 s, **THEN** `PlinkoRateAvg` lần lượt = 100, 100, 50, 0.
- `AC-26 [EditMode]` **GIVEN** modifier đổi mỗi khung trong 10 s, **WHEN** đếm `IncomeRatesChanged`, **THEN** số sự kiện ≤ ceil(10 / `C.ratesPublishInterval`) + 1, và giá trị mới xuất hiện trong ≤ `C.ratesPublishInterval` sau mỗi thay đổi.
- `AC-27 [EditMode]` **GIVEN** cấu hình của AC-20 (có Trà), **WHEN** đọc `OfflineRateSnapshot`, **THEN** = 29; gỡ Trà → bằng `idle_cps`; luôn ≤ `idle_cps`.

**Đường giá neo, họ giá, xu gốc (R8, R9, R10, spin_price, nice_ceil, derived_price, base_coin_value)**
- `AC-28 [EditMode]` **GIVEN** `C` mặc định, **WHEN** tính `spin_price(n)` với n = 0, 1, 2, 3, 10, 20, 50, 100, 150, **THEN** = 100, 110, 120, 120, 170, 270, 1,200, 14,000, 160,000; và với n = 0…2000 dãy không giảm.
- `AC-29 [EditMode]` **GIVEN** `nice_ceil`, **WHEN** đầu vào (105, 2), (1,146.74, 2), (7.2, 2), (100, 2), (100 × (1 + 1e-12), 2), (0, 2), (−5, 2), (13,150.1, 3), **THEN** = 110, 1,200, 8, 100, 100, 0, 0, 13,200.
- `AC-30 [EditMode]` **GIVEN** Xu = 150, n = 0, **WHEN** gọi `TryPaySpin` hai lần trong cùng khung có ngữ cảnh hành động, **THEN** lần 1 `true`, Xu = 50, n = 1, `SpinPriceChanged { 1, 110, 1.0297 }`; lần 2 `false` (giá 110 > 50), Xu = 50, n = 1, một `SpendRejected`.
- `AC-31 [EditMode]` **GIVEN** coef 1.5, **WHEN** n = 0 và n = 50, **THEN** `derived_price` = 150 và 1,800; coef 2.0 tại n = 100 → 27,000; coef ∈ {0, −1, NaN} → trả về "không có giá".
- `AC-32 [EditMode]` **GIVEN** n = 30 và các giá đã tính, **WHEN** đăng ký/gỡ nguồn Idle làm `idle_cps` tăng 10 lần rồi giảm về 0, **THEN** `spin_price` và mọi `derived_price` không đổi (D2, chống khai thác tháo kệ).
- `AC-33 [EditMode]` **GIVEN** α = `C.coinValueExponent`, **WHEN** tính `base_coin_value`, **THEN** n = 0 → 1, n = 1 → 1.0297, n = 100 → 18.679 (α 0.6), 11.467 (α 0.5), 30.43 (α 0.7) (sai số 1e-3 tương đối). **GIVEN** registry dịch vụ giả và Plinko giả chỉ đọc `IBaseCoinValueSource` (không có kênh ghi nào từ Economy Core), **WHEN** (a) sau `Run()` với game mới; (b) sau một `TryPaySpin` thành công; (c) sau một `TryPaySpin` thất bại; (d) sau `Run()` với dữ liệu nạp có `spinsThisShop` = 100 (VS), **THEN** `BaseCoinValue` đọc được lần lượt = 1; = `base_coin_value(1)` (1.0297); không đổi so với trước lệnh; = `base_coin_value(100)` (18.679), và Plinko giả không bao giờ dùng `PlinkoConfig.baseCoinValue` trong cả bốn ca. (Hành vi dự phòng khi không có nguồn nào là AC-45 của Plinko.)
- `AC-34 [Play mode]` **GIVEN** scene có Plinko thật, đã trả 10 lượt quay, **WHEN** một xu vào hũ x2, **THEN** `CoinResolved.rawValue` = 2 × 1.05^(0.6 × 10) = 2.680 (± 1e-3) và `IncomeCredited.creditedAmount` = 2.680 khi không có modifier.
- `AC-35 [EditMode]` **GIVEN** n = 5, **WHEN** gọi mọi API public của Economy Core ngoại trừ `TryPaySpin` thành công và `ResetForNewShop`, **THEN** n vẫn là 5 (lượt quay miễn phí không tăng giá).

**Tiệm mới (R12)**
- `AC-36 [EditMode]` **GIVEN** Xu = 5,000, Tem = 7, n = 40, một modifier `prestige.x` +1.0, **WHEN** `ResetForNewShop` có ngữ cảnh hành động, **THEN** Xu = `C.startingCoins`, Tem = 7, n = 0, giá quay = `C.spinBasePrice`, Plinko giả đọc `IBaseCoinValueSource.BaseCoinValue` = 1 ngay sau lời gọi (trước n = 40 nó đọc `base_coin_value(40)`), modifier `prestige.x` vẫn tính, đúng một `WalletChanged` (`currency.coin`, previous 5,000, current `C.startingCoins`, reason `ShopReset`), đúng một `SpinPriceChanged { 0, C.spinBasePrice, 1 }` và đúng một `ShopEconomyReset { [currency.coin], 0 }`, theo thứ tự đó.

**Định dạng (R13, display_format)**
- `AC-37 [EditMode]` **GIVEN** `C.displaySigDigits` = 3, **WHEN** định dạng số dư 0, 999.9, 1,000, 1,234.5, 12,345, 999,999.6, 1e6, 1e9, 1e12, 1e15, 1e18, 1e300, NaN, **THEN** = "0", "999", "1.00K", "1.23K", "12.3K", "999K", "1.00M", "1.00B", "1.00T", "1.00aa", "1.00ab", "1.00dr", "0".
- `AC-38 [EditMode]` **GIVEN** `C.rateDecimals` = 1, **WHEN** định dạng tốc độ 6.58, 0, 1,234.5, **THEN** = "6.5/s", "0.0/s", "1.23K/s".

**Faucet/sink và sự kiện (R14, R15)**
- `AC-39 [EditMode]` **GIVEN** phiên kịch bản 60 s (Idle, 200 xu Plinko, 3 lượt quay, 1 lần `SpendRejected`), **WHEN** ghi mọi `WalletChanged`, **THEN** mỗi sự kiện có delta = current − previous và `reason` thuộc bảng faucet/sink R14; Idle phát ≤ 1 `WalletChanged` mỗi khung; tổng delta trong mỗi khung = Σ `creditedAmount` + phần Idle − chi tiêu của khung đó.

**Cân bằng (first_spin_time, seconds_per_spin)**
- `AC-40 [EditMode]` **GIVEN** `C` và `T` mặc định, **WHEN** tính `first_spin_time`, **THEN** kết quả (13.8 s) ≤ `T.firstSpinMaxSeconds` (30).
- `AC-41 [EditMode]` **GIVEN** `T.spinCheckpoints`, **WHEN** tính `seconds_per_spin` tại từng mốc, **THEN** test in bảng (11.3 / 31.3 / 84.4 s) và mỗi giá trị ≤ phần tử tương ứng của `T.plinkoOnlySpinSecondsMax` (30 / 60 / 120).
- `AC-42 [Play mode]` **GIVEN** game mới, seed cố định, **WHEN** `InputTestFixture` chạm Plinko với nhịp `T.realisticTapRate`, **THEN** Xu ≥ `C.spinBasePrice` trước `T.firstSpinMaxSeconds` và nút vặn trứng chuyển sang trạng thái bấm được ngay khi `CanAfford` = true.

**Hàm thuần cho Shelf (R7.5, preview_idle_cps)**
- `AC-50 [EditMode]` **GIVEN** cấu hình của AC-20 (`idle_cps` = 58) và nguồn `shelf.slot1.idle` = A, `shelf.slot2.idle` = B, **WHEN** gọi `GetSourceIdleCps` cho A, B và một khóa chưa đăng ký, rồi gỡ B, **THEN** = 40, 18, 0; sau khi gỡ B, A = 40 và Σ = `IdleCps`; mỗi lần Σ các giá trị = `IdleCps` trong sai số `T.valueRelTolerance`.
- `AC-51 [EditMode]` **GIVEN** cấu hình của AC-20, bộ đếm ví/modifier/sự kiện và bộ đếm tính lại `idle_cps` (AC-24), **WHEN** gọi `PreviewIdleCps` với (a) plan = trạng thái hiện tại; (b) plan thêm nguồn C gốc 4 không modifier; (c) plan bỏ B; (d) plan có `baseRate` NaN hoặc modifier −0.5; ở Running, Ready và Paused, **THEN** (a) = 58; (b) = 58 + 4 × 2 (Trà) = 66; (c) = 40; (d) bằng kết quả khi giá trị đó coi là 0; sau mọi lời gọi `IdleCps`, số dư, danh sách modifier không đổi, 0 sự kiện, bộ đếm tính lại không tăng.

**Hiệu năng**
- `AC-43 [Perf]` **GIVEN** trạng thái ổn định, Idle cộng mỗi khung, 24 `CoinResolved`/s, số dư và hai tốc độ được định dạng mỗi khung, **WHEN** ghi 600 khung bằng `ProfilerRecorder` "GC Allocated In Frame", **THEN** 0 B mỗi khung do Economy Core và bộ định dạng.
- `AC-44 [Perf]` **GIVEN** bản WebGL trên thiết bị tham chiếu của ADR hiệu năng, 40 modifier, 5 nguồn Idle và 24 `CoinResolved` trong một khung, **WHEN** đo 60 s, **THEN** thời gian CPU của Economy Core p95 ≤ `T.pipelineFrameBudgetMs` (0.2 ms).

**Hiển thị**
- `AC-45 [Screenshot]` **GIVEN** Tab 1 ở 1080×1920, Xu = 12,345.9, `idle_cps` = 6.58, n = 1, **WHEN** chụp, **THEN** header hiện "12.3K" và "6.5/s", nút vặn trứng hiện giá "110".

**Khởi động (R16)**
- `AC-49 [EditMode]` **GIVEN** Economy Core ở (a) Ready (sau `Run()`, chưa `Interactive`) hoặc (b) Paused (sau `Paused {Hidden}`), modifier đã biết, **WHEN** Plinko giả phát `CoinResolved` với `reason` = Overflow, `bucketIndex` = none, `rawValue` r, **THEN** cộng đúng `plinko_credit(r)`, phát đúng một `IncomeCredited` (`bucketIndex` = none, `reason` = Overflow), trạng thái giữ nguyên Ready / Paused và không có khoản Idle nào được cộng.
- `AC-46 [EditMode]` **GIVEN** registry bước khởi động giả của Bootstrap, **WHEN** scene nạp và Economy Core đăng ký, **THEN** có đúng một bước `Id` = `economy.core`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = đúng; trước `Run()` Economy Core ở Uninitialized, `Add`/`TrySpend` trả `false` và registry dịch vụ giả không có `IBaseCoinValueSource`; sau `Run()` bước báo `IsDone` trong cùng lời gọi, Economy Core ở **Ready** (không phải Running), `Add` và `TrySpend` (có ngữ cảnh hành động) hợp lệ, registry có đúng một `IBaseCoinValueSource` là Economy Core, và với một nguồn Idle có `idle_cps` > 0 chạy 60 bước khi Bootstrap giả giữ `IsSimulationRunning` sai thì số dư không đổi — Idle chỉ bắt đầu cộng sau khi Bootstrap giả phát `Interactive`; với `EconomyConfig` thiếu `currency.coin`, bước báo `IsFailed` và số dư không đổi.

## Open Questions

**Cần user quyết định**

| Question | Owner | Target date |
|---|---|---|
| ~~`idleCpsUnit` có co giãn theo tiến độ trong một tiệm không? (A) hằng số / (B) theo giá quay / (C) theo bộ sưu tập~~ **Đã đóng** — user chọn (A) (`decision-log.md`, 2026-10-21). R11.2, `idle_cps` và `seconds_per_spin` viết theo (A), không còn provisional; rủi ro `channel_parity` theo dõi ở Câu hỏi mở. | User | 2026-10-21 (đã đóng) |
| ~~Trần thu nhập kênh Plinko có phải cứng không (NEEDS-USER của `plinko-board.md`)?~~ **Đã đóng** — user chọn (a) trần mềm (`decision-log.md`, 2026-10-21): R5.5, `plinko_rate_model`, Edge Case "Autoclicker trên Plinko". | User | 2026-10-21 (đã đóng) |

**Câu hỏi mở**

| Question | Owner | Target date |
|---|---|---|
| Mô phỏng bảng tính giá quay vs thu nhập (Plinko + Idle + Sao) để chốt `spinGrowth`, `coinValueExponent`, `idleCpsUnit`, trần các tầng và `plinkoOnlySpinSecondsMax`; mở lại GDD này sau Pet Catalog + Shelf, rồi sau Star Progression + Prestige. | game-designer | 2026-11-11 |
| ADR: cơ chế sự kiện và đăng ký modifier (dùng chung với Plinko); "ngữ cảnh hành động người chơi" cho `TrySpend` / `ResetForNewShop`; bộ định dạng số không cấp phát GC; định dạng lưu `id → double` (JsonUtility không serialize Dictionary). | technical-director → `game-adr` | 2026-11-04 |
| ~~Plinko Board cần sửa theo GDD này (`IBaseCoinValueSource`, mô hình kéo, dự phòng, AC dự phòng, đóng Open Question về `baseCoinValue` / số bay / origin / offline)~~ **Đã đóng** — Plinko Revision 3: R9.2 (kéo lúc giải quyết xu, không `RunsAfter` `economy.core`), R12 (interface trong hợp đồng), AC-45 (dự phòng), Open Question `baseCoinValue` đã đóng. Knob dự phòng `PlinkoConfig.baseCoinValue` thống nhất = 1, chỉ cho test (C2[10]). | Plinko owner | 2026-10-21 (đã đóng) |
| Phần còn lại của mục trên: Bootstrap B3 (bảng pha `GameplayInit`) vẫn ghi "Economy Core → Inventory → Plinko Board, …"; nên ghi rõ Plinko không khai `RunsAfter` `economy.core` (R16.2). Plinko: Edge Case "giá trị xu gốc bị đặt bằng 0 … hợp lệ" trái với dự phòng = 1 và getter ≥ 1 (R10.2) — hệ số hũ = 0 vẫn hợp lệ, `baseCoinValue` < 1 thì validator báo lỗi; Revision 2 và cột Source của `coin_payout` còn chữ "do Economy Core đặt" (C2[10]). | Bootstrap owner, Plinko owner / bước Sync | 2026-10-28 |
| Rủi ro theo dõi (hệ quả của (A), R11.2): `channel_parity` ra khỏi `PetBalanceTargets.parityBand` từ n ≈ 59. Mô phỏng chung Economy + Pet Catalog + Shelf + Star phải cho thấy thú phẩm cấp cao và Sao giữ tỷ lệ thu nhập thực Idle / Plinko trong dải tới mốc prestige; nếu không, chỉnh `coinValueExponent`, `PetRarityConfig.plinkoUpliftPerUnit` hoặc hệ số Sao, không đổi (A). | game-designer | 2026-11-11 |
| Mô phỏng trần mềm kênh Plinko (R5.5): với từng cấu hình `plinkoRawRateCeilingAssumed`, kênh Plinko (gồm trần tầng và `plinkoFlatPerCoinCap`) không thống trị Idle quá mức mục tiêu Pillar 3; đo lại bốn giá trị theo Plinko AC-13 / AC-13b / AC-13c. | game-designer | 2026-11-11 |
| Pet Catalog: đổi Source của `bounce_uplift_at_cap` sang `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`, xóa `PetBalanceTargets.bounceUpliftAtCapAssumed`, sửa AC-34 và Open Question tương ứng của Pet (C2[12]). | Pet Catalog owner / bước Sync | 2026-10-28 |
| Prestige: boost đang chạy có giữ qua "Mở Tiệm Mới" không; có khóa nút prestige khi bàn Plinko còn xu đang rơi không (hiện các xu đó cộng vào tiệm mới với `baseCoinValue` = 1). | Prestige GDD | 2026-12-02 |
| Offline Earnings: xác nhận không quy đổi auto-drop Plinko sang thu nhập offline (giá trị của thú auto-drop chỉ có khi đang chơi). | Offline Earnings + Shelf & Synergy GDD | 2026-11-11 |
| Tầng của Economy Core trong systems index: Foundation (hiện tại) hay Core, vì nó phụ thuộc Plinko (Core) qua `CoinResolved`. | `game-systems` | 2026-10-28 |
| ~~Plinko Board R9.2 cần khớp R5.1 (C1[10]): `coinId`, enum `reason`, hàng Economy Core trong Interactions~~ **Đã đóng** — Plinko Revision 3 (R9.2, R12, Interactions). | Plinko owner | 2026-10-21 (đã đóng) |
| ~~Pet Catalog `plinko_effect_uplift` chuyển sang đọc `EconomyBalanceTargets.plinkoJackpotProbMeanAssumed` / `.plinkoScoredPegHitsPerCoinAssumed` (C1[12])~~ **Đã đóng** — Pet Catalog Revision 2. | Pet Catalog owner | 2026-10-21 (đã đóng) |

**Lựa chọn nhỏ đã giả định** (theo `minorChoices` của brief 2026-10-21 và lựa chọn khuyến nghị của game-designer)

| Question | Owner | Target date |
|---|---|---|
| Assumed: `adCompensationMaxSeconds` = 90 (≈ 3 × rewarded ad 30 s, đủ bù ad dài mà chặn lỗi cờ ad kẹt thành nguồn Idle vô hạn); Economy tự trừ `ad_simulated_seconds` (Bootstrap B6.10.5). Offline Earnings (VS) cũng nghe `AdClosed` — GDD Offline không được bù lại cùng `adSeconds`. | game-designer + Offline Earnings GDD | 2026-11-11 |
| Assumed: tên file `economy-core.md`, tiêu đề "Economy Core (Lõi Kinh Tế)" (kebab-case theo skill, khớp systems index). | game-designer | 2026-10-21 |
| Assumed: double cho mọi lượng tiền; chặn NaN / vô cực / âm ở đầu vào; trần `walletSafeCap` = 1e300 (concept đã chốt double; trần tránh tràn khi nhân nhiều tầng). | game-designer | 2026-10-21 |
| Assumed: dưới 1,000 hiện số nguyên; sau đó K, M, B, T rồi aa, ab…; 3 chữ số có nghĩa, giữ số 0 cuối ("1.00K") để header không nhảy độ rộng; không cấp phát GC (khán giả casual 10+ quen hậu tố hơn ký hiệu khoa học). | game-designer | 2026-10-21 |
| Assumed: ví giữ phần lẻ; hiển thị làm tròn xuống; giá làm tròn lên 2 chữ số có nghĩa (CPS lẻ không mất; giá dễ đọc). | game-designer | 2026-10-21 |
| Assumed: cộng CPS liên tục theo delta khung, kẹp `maxFrameDelta`, dừng theo tín hiệu tạm dừng mô phỏng của Bootstrap (tab ẩn / app xuống nền, theo quyết định Boot D3) thay vì "mất focus" — để khớp Plinko và không chồng với Offline. | game-designer | 2026-10-21 |
| Assumed: mọi origin của Plinko (Manual, Auto, Rain, JackpotBurst) đi qua kênh Plinko (một hợp đồng `CoinResolved`; thú buff Plinko được lợi cả khi auto). | game-designer | 2026-10-21 |
| Assumed: modifier kênh Plinko áp cho mọi `reason` (hũ, kẹt, tràn), đánh giá tại thời điểm giải quyết (Plinko đã đặt trả kẹt/tràn < EV nên không khai thác được). | game-designer | 2026-10-21 |
| Assumed: modifier âm / NaN / vô cực → trung tính (đồng nhất Plinko R5.3); không có modifier âm. | game-designer | 2026-10-21 |
| Assumed: chỉ người chơi được trừ ví, qua `TrySpend` có ngữ cảnh hành động (Pillar 4, Quyết định 1 của concept). | game-designer | 2026-10-21 |
| Assumed: xu khởi đầu 0; mục tiêu lượt quay đầu ≤ 30 s ở nhịp chạm thực tế (onboarding concept; Plinko đã đặt mốc 30 s). | game-designer | 2026-10-21 |
| Assumed: công bố riêng `IdleCps` và `PlinkoRateAvg` (trung bình trượt); HUD quyết định hiển thị (so sánh từng kênh cho Pillar 3). | game-designer | 2026-10-21 |
| Assumed: Offline dùng `idle_cps` không gồm modifier tạm thời; hệ số và trần giờ thuộc Offline Earnings (tránh lặp knob; boost hết hạn không kéo dài qua offline). | game-designer | 2026-10-21 |
| Assumed: asset `EconomyConfig` (runtime) + `EconomyBalanceTargets` (test/mô phỏng), cùng mẫu với Plinko. | game-designer | 2026-10-21 |
| Assumed: modifier có `sourceId`, kênh, tầng, giá trị, đích, `isTemporary`; hệ đăng ký tự gỡ khi hết hạn (gỡ vòng phụ thuộc như systems index đã định; cơ chế → ADR). | game-designer | 2026-10-21 |
| Assumed: bản đồ faucet/sink R14, cân trên phiên 5–15 phút. | game-designer | 2026-10-21 |
| Assumed: trong một tầng nhân có tên, các nguồn **cộng**; cộng phẳng luôn nhắm một nguồn (`plinko.coin` cho "+N mỗi xu") và tính theo bội số `idleCpsUnit` / `baseCoinValue` (khớp ví dụ D3 và Pet D4). | game-designer | 2026-10-21 |
| Assumed: quyền vào tầng kiểm bằng tiền tố `sourceId` (`tierOwnerPrefixes`); thú/Shelf không bao giờ vào Boost, Prestige, Global (chốt cấu trúc cho Pillar 3). | game-designer | 2026-10-21 |
| Assumed: `idleCpsUnit` = 2.0 (thú Thường vai Idle ≈ 2 CPS như Vịt Gỗ FirstGDD; thú Idle mạnh nhất v1.0 là Sử Thi — Công Kiêu Hãnh 6.6 đơn vị = 13.2 xu/s ở 1 Sao theo `pet-catalog.md` `idle_base_units`; Huyền Thoại duy nhất là vai Hỗ trợ, `role_idle_share` = 0, không có CPS); trần tầng Synergy 4 / Boost 3 / Prestige 10 / Global 2 (Boost 3 cho phép hai boost x2 cộng thành x3, không x4). | game-designer | 2026-10-21 |
| Assumed: lượt quay miễn phí không tăng `spinsThisShop` (xem quảng cáo không làm giá đắt lên). | game-designer + Gacha GDD | 2026-10-21 |
| Assumed: phần thưởng co giãn (quà Bác Bảo Vệ) dùng cùng `derived_price`, làm tròn lên (có lợi cho người chơi). | game-designer + Shop GDD | 2026-10-21 |
| Assumed: số bay hiển thị `creditedAmount` (sau modifier) — trả lời Open Question của Plinko; Economy Core cấp qua `IncomeCredited`. | game-designer + Feedback & Juice GDD | 2026-10-21 |
| Assumed: `PlinkoRateAvg` chia cho thời gian đã chạy khi phiên còn ngắn hơn cửa sổ (không báo thấp lúc mở game). | game-designer | 2026-10-21 |
| Assumed: `ResetForNewShop` chỉ đặt lại tiền tệ `resetsOnNewShop`, n và giá; không tự gỡ modifier của hệ khác (Prestige / Boosts / Shelf tự xử lý). | game-designer + Prestige GDD | 2026-10-21 |
| Assumed: bước khởi động `Id` = `economy.core`, `RunsAfter` rỗng, `IsCritical` = đúng, hoàn tất trong một khung (tiền tố theo hệ như Bootstrap B2; Bootstrap B3 xếp Economy Core đầu `GameplayInit` và coi lỗi của nó là BootFailed). | game-designer | 2026-10-21 |
| Assumed (fix round 1, EC-B1): `baseCoinValue` đến Plinko theo **mô hình kéo** — Plinko sở hữu `IBaseCoinValueSource`, Economy Core cài đặt và đăng ký trong `Run()`, Plinko đọc lúc giải quyết xu và dùng `PlinkoConfig.baseCoinValue` khi không có nguồn; không có `RunsAfter` giữa hai bước (khuyến nghị của review; giữ đúng Economy D4 "Economy Core sở hữu, Plinko nhận vào" mà không cần đẩy). | game-designer | 2026-10-21 |
| Assumed (fix round 1, EC-B2): AC-7 tách thành AC-7a (reflection theo kiểu tham số ngữ cảnh hành động) và AC-7b (gọi mọi API còn lại với giá trị biên, số dư không giảm); giữ số AC-7 để không đánh số lại. | game-designer | 2026-10-21 |
| Assumed: `IncomeCredited` từ `SubmitIncome` mang `coinId` = none và `bucketIndex` = none (thưởng sự kiện của thú không gắn với một xu/hũ; Feedback & Juice tự chọn vị trí số bay cho trường hợp này). | game-designer + Feedback & Juice GDD | 2026-10-21 |
| Assumed (revision C2[0]): knob cân bằng của Economy cho trần mềm kênh Plinko là `EconomyBalanceTargets.plinkoRawRateCeilingAssumed` (bốn cấu hình của `raw_rate_ceiling`) + công thức `plinko_rate_model` + R5.5 + AC-47 với `plinkoSoftCapTestFactor` (cùng mẫu với các knob giả định đo khác; Plinko giữ mô hình, Economy giữ bản giả định để mô phỏng). | game-designer | 2026-10-21 |
| Assumed (revision C2[10]): safe range của dự phòng `PlinkoConfig.baseCoinValue` là "= 1, chỉ cho test" như `plinko-board.md` hiện tại, thay "1–10" của bản trước (giá trị runtime không có trần nên khoảng rộng hơn cho dự phòng không có ích). | game-designer | 2026-10-21 |
| Assumed (revision C2[12]): tên knob `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`, giá trị 0.40, safe range 0.2–0.55 giữ nguyên từ Pet Catalog (tiền tố `plinko` như ba knob giả định đo đã chuyển trước). | game-designer | 2026-10-21 |
| Assumed (revision C2[0]): AC-43 / AC-44 giữ tải 24 `CoinResolved` làm kịch bản tải hiệu năng, không phải trần thông lượng; xem lại khi ADR hiệu năng chốt kịch bản xấu nhất (Mưa Đồng Xu + auto-drop + trần nảy). | game-designer + technical-director | 2026-11-04 |
| Assumed (revision C5[13]): `GetSourceIdleCps` trả phần của nguồn sau Synergy **và** ba tầng kênh (Σ = `IdleCps`, nhãn ô khớp header); `PreviewIdleCps` chỉ thay nguồn/modifier `shelf.`, giữ Boost/Prestige/Global thật; cấu trúc `plan` do Shelf định nghĩa, Economy chỉ đọc. Hai hàm không có knob mới (ngân sách CPU `ShelfBalanceTargets.previewMaxMs` do Shelf giữ). | game-designer + Shelf & Synergy GDD | 2026-10-21 |
| Assumed (fix round 1, EC-R1-B1): tên trạng thái mới là **Ready** (sau `Run()`, API hoạt động, không cộng Idle); Economy Core đọc `IsSimulationRunning` mỗi khung làm chốt chặn cuối (dt = 0 khi sai) ngoài việc nghe `Interactive` / `Paused` / `Resumed`, và vào Running ngay nếu truy vấn đã đúng lúc vào Ready (theo lời "truy vấn kèm sự kiện cho hệ đăng ký muộn" của Bootstrap B6.5); `Interactive` khi đã Running không làm gì. | game-designer | 2026-10-21 |
