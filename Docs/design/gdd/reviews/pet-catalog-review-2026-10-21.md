# Review — pet-catalog — 2026-10-21 — Verdict: APPROVED
Round: Round 1 (đợt review độc lập đầu tiên của workflow này; ghi đè báo cáo cùng tên trước đó, bản đó ghi "Round 3", verdict NEEDS REVISION, mục chặn PC3-B1)
Document: Docs/design/gdd/pet-catalog.md (git hash-object 209bcc519a1a8961afc7678db16a056437068c5f)
Registry: Docs/design/registry/entities.yaml (git hash-object e78ec78919d10f47b7cb6ad8f020cf07f4ced159)
Scope signal: L
Specialists: none (lean review)
Blocking items: 0 | Recommended: 19 (0 NEEDS-USER)
Prior verdict resolved: Yes. PC3-B1 (vế "không +0" của AC-39) đã sửa ở Revision 5; câu NEEDS-USER về `scope` đã đóng theo (a) (quyết định user, `decision-log.md`).
Summary: Revision 5 sửa đúng mục chặn duy nhất của báo cáo trước. UI Requirements giờ định nghĩa quy tắc định dạng số trên thẻ thú, Economy Core sở hữu bộ định dạng. AC-39 nêu chuỗi kỳ vọng sau định dạng, và mọi chuỗi đều tính lại đúng (Thỏ "+0.017", Thiên Nga "+0.0068", Hamster "7.41", Thỏ ở n = 100 "+0.318", Mèo Chiêu Tài "+56", 1234.5 → "1.23K"). `scope = Both` bị khóa cho live-ops bằng `PetCatalog.scopeBothMinTier`, có lỗi validator và AC-8 (h)/(i). Mười công thức thay số ở biên vẫn khớp ví dụ và AC. Hợp đồng với Bootstrap, Plinko và Economy khớp trên đĩa, và không có mâu thuẫn registry nào ngoài các thay đổi user đã duyệt. Không còn mục chặn. Còn 19 khuyến nghị: phần lớn giữ lại từ các vòng trước, thêm hai mục mới (knob `scopeBothMinTier = None`, biên 999.5–1000 của quy tắc định dạng). Nên xử lý trước khi viết GDD Shelf & Synergy, nhất là các mục 1, 3–6 và 8.
Findings:
- [RECOMMENDED] R2 / R11.1: `PetCatalog.scopeBothMinTier` = `None` không phải lỗi (R2 chỉ kể "ba knob bậc phát hành"); khi đó `releaseTier < None` không bao giờ đúng, nên `scope = Both` mở cho mọi thú mà không có cảnh báo, và quyết định (a) không còn được validator giữ.
- [RECOMMENDED] R6 / R10 / `plinko_effect_uplift`: Hamster `AutoDropRate` khi không chạm cho ≈ 1.93 xu/s ở n = 50 và ≈ 7.66 xu/s ở n = 100 (≈ 3.8 lần Vịt Gỗ cùng 1.0 đơn vị), mạnh ở kênh Idle, trái câu "Người chơi không thể có một thú mạnh ở cả hai kênh". Open Questions không có mục này.
- [RECOMMENDED] Formulas `channel_parity` / R10.3: chỉ kiểm tại n_ref = 50 (0.222 ở n = 0, 4.147 ở n = 100); chưa báo các mốc `EconomyBalanceTargets.spinCheckpoints`.
- [RECOMMENDED] R10.2 / AC-22: ở roster MVP, Mèo Hoàng Gia chỉ đạt 2.61 / 15 = 0.17 ngân sách, đúng lúc kiểm giả thuyết MVP (Pillar 2). Validator không báo, Open Questions không ghi.
- [RECOMMENDED] R6 `PegHitBonus`: mỗi `PegHit` được tính sinh một `SubmitIncome` và một `IncomeCredited` (≈ 140/s mỗi thú ở trần), nhưng `IncomeCredited` có `coinId` = none nên Feedback không đặt được số bay lên đúng xu.
- [RECOMMENDED] R11: thiếu ràng buộc chéo giữa các knob: phẩm cấp chồng lấn ở biên safe range, `weightMin` < `weightMax`, `effectLimits[BouncinessModifier].max` ≥ `bouncinessCap` / `basePegBounciness`, `effectLimits[FlatPerCoin].max` ≤ `plinkoFlatPerCoinCap`.
- [RECOMMENDED] R6.3 / AC-14 / AC-15: chưa yêu cầu Shelf dùng `sourceId` của modifier hiệu ứng thú khác với `sourceId` của combo nhóm; ánh xạ `scope` sang khóa nguồn `shelf.slot<i>.idle` / `.plinko` hay sang trường `channel` của Economy R4.3 còn mở.
- [RECOMMENDED] R2 / R11 / R12.1: phần còn hở của `None = 0`. Chưa có AC cho knob bậc phát hành = `None`. `role`, `group` hoặc `rarity` = `None` còn sinh thêm lỗi luật nhóm hoặc lỗi "target = 0", trái câu "mỗi trường thiếu cho đúng một lỗi". Lúc chạy, bản ghi có `releaseTier = None` vẫn lọt qua bộ lọc R9.1.
- [RECOMMENDED] Acceptance Criteria / R5.5 / Edge Cases / UI Requirements: còn literal chưa là knob hoặc hằng có lý do: "± 0.001" (AC-20, AC-37), "± 0.0001" (AC-34), "± 0.3" (AC-38), "≥ 1 thú Idle / Plinko" (R5.5), "10.9% / 20% / 1.83 lần" (Edge Cases), ngưỡng 1000 (quy tắc định dạng).
- [RECOMMENDED] UI Requirements "Định dạng số trên thẻ thú": giá trị trong [999.5, 1000) làm tròn ra "1000", không phải "1.00K". Quy tắc này chưa có trong `economy-core.md` R13, dù Economy Core sở hữu bộ định dạng.
- [RECOMMENDED] AC-29 / AC-40: AC-29 dùng "máy tham chiếu của dự án" thay vì `BootTestTargets.perfReferenceDevice`; AC-40 không nêu khóa màu `PetRarityConfig.rarities[legendary]`.
- [RECOMMENDED] R8 / Tuning Knobs: `inGachaPool` của 13 thú không được ghi, trong khi AC-16 và AC-18 ngầm định true.
- [RECOMMENDED] UI Requirements: mẫu `FlatPerCoin` ghi "mỗi xu vào hũ", lệch R6 và Economy (áp cho mọi `CoinResolved`, gồm Stuck và Overflow).
- [RECOMMENDED] R11.2 / `collection_spins_estimate`: chưa nói validator dùng roster `buildTier` hay `balanceRosterTier` (MVP 43.4 chỉ cách cận dưới 40 là 3.4).
- [RECOMMENDED] `support_power_estimate`: `referenceNeighborTargetUnits` 2.5 lớn hơn mọi đích hợp lệ của Cáo trong roster (Thỏ 2.475, Mèo Chiêu Tài 2.25, Thiên Nga 0.99).
- [RECOMMENDED] Dependencies: không có hàng Audio, dù Catalog định nghĩa hậu tố cue `Gacha.RarityReveal.<phẩm cấp>`. `audio.md` không liệt kê Pet Catalog.
- [RECOMMENDED] Dependencies / Open Questions: ghi chú lỗi thời. Hàng HUD vẫn ghi "Chưa có trong systems index" và hàng Sync vẫn ghi "thêm UX flag", nhưng cả hai đã có trong systems index.
- [RECOMMENDED] Sync: header GDD và systems index đặt `Approved`. Registry: bỏ "(provisional)" ở `pet.cozy_fox` và `pet.chubby_king_cat`; áp các thay đổi đã duyệt (`auto_drop_interval.output`, `coin_rate_ceiling`, `raw_rate_ceiling`); đăng ký `bounce_uplift_at_cap`.
- [RECOMMENDED] GDD khác (Sync): `economy-core.md` (Open Questions dòng 715, "Hai chiều" dòng 486, Cross-References dòng 609) vẫn ghi Catalog còn phải chuyển `bounceUpliftAtCapAssumed`. `plinko-board.md` (output `raw_rate_ceiling`, Cross-References, AC-13c) vẫn tham chiếu `PetBalanceTargets.bounceUpliftAtCapAssumed`, một knob đã xóa.

---

## Design Review: Pet Catalog (Danh Mục Thú) — Round 1
Specialists consulted: none (lean review)
Re-review: Yes. Báo cáo trước cùng ngày (ghi "Round 3") có verdict NEEDS REVISION. Theo yêu cầu workflow, review này bỏ qua freshness shortcut và làm đầy đủ. Tài liệu đã đổi (`09e98855…` → `209bcc51…`, Revision 5); registry cũng đổi (`f9be023f…` → `e78ec789…`).

### Theo dõi mục chặn của báo cáo trước
| Mục trước | Trạng thái | Bằng chứng |
|---|---|---|
| PC3-B1: vế "không +0" của AC-39 không đạt được với `display_format` của Economy R13 | **Đã xử lý** | UI Requirements, "Định dạng số trên thẻ thú" (dòng 504–509): Economy Core sở hữu bộ định dạng; số < 1000 hiện `EconomyConfig.displaySigDigits` chữ số có nghĩa, làm tròn gần nhất, bỏ số 0 thừa; ≥ 1000 theo R13. AC-39 nêu chuỗi kỳ vọng. Tính lại với 3 chữ số có nghĩa: 0.017 → "0.017"; 0.0068 → "0.0068"; 0.086 → "0.086"; 2 / 0.27 = 7.407 → "7.41"; 1.7 → "1.7"; 0.85 → "0.85"; 180 → "180%"; 0.017 × 18.679 = 0.31755 → "0.318"; 3 × 18.679 = 56.04 → "56"; 0.002 → "0.002"; 1234.5 → "1.23K" (khớp Economy AC-37). Câu "HUD sở hữu" đã bỏ. Phần còn hở nhỏ ở khuyến nghị 10. |
| NEEDS-USER `scope` (khuyến nghị 1 trước) | **Đã đóng** | User chọn (a). R6.5 và R8 không còn *provisional*; `PetCatalog.scopeBothMinTier` = `LiveOps`; lỗi R11.1; AC-8 (h)/(i). |

Khuyến nghị trước: mục "7.4 s vs 7.41 s" (nice-to-have) đã xử lý. Mục `bounceUpliftAtCapAssumed` → `EconomyBalanceTargets` đã xử lý phía Catalog (Formulas, AC-34, Dependencies). Các khuyến nghị còn lại chưa được xử lý (Revision 5 chỉ sửa mục chặn và đóng các quyết định), nên được giữ lại dưới đây.

### Completeness: 6/6 required sections present (workflow standard)
Overview ✓, Detailed Design (Core Rules R1–R12, States and Transitions, Interactions) ✓, Formulas ✓ (bắt buộc vì hệ có luật số; 10 công thức), Edge Cases ✓, Dependencies ✓, Acceptance Criteria ✓ (AC-1 – AC-40, mọi AC có tag [EditMode] / [Perf] / [Screenshot] và dạng GIVEN/WHEN/THEN). Summary ✓.
Advisory at standard: không có `## Player Fantasy` (dòng "Implements Pillar" ở header thay thế). Tuning Knobs ✓: có Asset.field, current, safe range, too high / too low; hằng số có lý do, gồm `None = 0`. Game Feel ghi "không áp dụng" kèm lý do; Visual/Audio, UI Requirements, Cross-References có mặt.

### Dependency graph
- ✓ bootstrap-and-scene-flow.md exists. Liệt kê Pet Catalog (Hard, bước `DataLoad` cốt lõi: dòng 39, 56, 190, 323, 378, 390, 480; AC-9, AC-16 dùng `pet.catalog`). `IBootStep` và thứ tự pha ServiceInit → DataLoad → SaveLoad → GameplayInit → Prewarm khớp R12.1, AC-25, AC-26. Payload `BootFailed {stepId, phase}` và knob `stepTimeoutSeconds` cũng khớp.
- ✓ economy-core.md exists. "Pet Catalog: không phụ thuộc trực tiếp" (dòng 474) khớp. Khớp tiếp: R4.3 modifier `{sourceId, channel, tier, value, target, isTemporary}`; R4.7 (Base gốc / cộng phẳng `plinko.coin` / Synergy; thú không vào Boost / Prestige / Global); R5.3 `SubmitIncome`; `SpinPriceChanged {spinsThisShop, spinPrice, baseCoinValue}`. Knob khớp: `idleCpsUnit` 2.0, `plinkoFlatPerCoinCap` 2.0, `synergyTierCap` 4.0, `displaySigDigits` 3, `EconomyBalanceTargets.plinkoEvEffectiveAssumed` 2.96, `.plinkoJackpotProbMeanAssumed` 0.109, `.plinkoScoredPegHitsPerCoinAssumed` 14, `.realisticTapRate` 3.0, `.plinkoBounceUpliftAtCapAssumed` 0.40 (0.2–0.55), `.valueRelTolerance`, `.spinCheckpoints`. Lỗi thời: dòng 486, 609, 715 vẫn ghi Catalog còn phải chuyển `bounceUpliftAtCapAssumed` (khuyến nghị 19).
- ✓ plinko-board.md exists. "Hai chiều" (dòng 421) khớp. Payload R12 khớp: `JackpotHit {coinId, origin, bucketIndex, worldPosition}`, `CoinResolved {…, scoredPegHits, reason}`; `JackpotHit` phát cho cả xu `JackpotBurst` (R7.2). API khớp: R12.4 `RegisterAutoDropRate` / `RegisterBouncinessFactor` / `Unregister`; R12.5 tổng / tích, kẹp và đăng ký lại cùng `sourceId` thì thay giá trị. R5.3 kẹp hệ số nảy < 1 lên 1 (khớp `BouncinessModifier` ≥ 1). `auto_drop_interval` không có trần trên (Hamster 7.41 s). `raw_rate_ceiling` ở trần nảy (≈ 26 / 27.6 / ≤ 30.7) khớp R8 và budget_ratio của Catalog. Lỗi thời: còn tham chiếu `PetBalanceTargets.bounceUpliftAtCapAssumed` (khuyến nghị 19).
- ✓ audio.md exists. Có cue `Gacha.RarityReveal.<phẩm cấp>` (provisional), nhưng không liệt kê Pet Catalog và không nói hậu tố là ID phẩm cấp của Catalog (khuyến nghị 16).
- ✓ game-concept.md exists. Khớp: Pillar 2, 3, 4; design test của Pillar 3 ("5 con hiếm nhất không bao giờ là đáp án cho cả hai kênh"); MVP (kệ 3 ô, một combo nhóm/kề); Quyết định 5; Zero Art; Anti-Pillar "không nâng chỉ số tĩnh".
- ✗ inventory.md, gacha-machine.md, shelf-and-synergy.md, star-progression.md, neighbor-pranks.md NOT FOUND. Đây là các hệ downstream đang Not Started; GDD ghi rõ *(chưa có)*, nên không chặn.

Systems index: hàng Pet Catalog có Bootstrap, Economy và Plinko (soft), Settings (soft). Inventory, Gacha, Shelf, Star, Neighbor, HUD (soft), Feedback (soft) đều liệt kê Pet Catalog, nên khớp. Status `Needs Revision` khác header `In Review` (khuyến nghị 18).

### Registry
- Khớp: `rarity_coefficients` [1, 2.5, 6, 15]; `pet_power_units` 0.8–18; `idle_base_units` 0–18 (v1.0 tối đa 6.6); `pet_event_bonus` 0–10 × `base_coin_value`; `plinko_effect_uplift` 0–≈ 0.55 (biến gồm `bounce_uplift_at_cap`); 13 entity (nhóm, phẩm cấp, vai, trọng số, hiệu ứng, bậc phát hành); `effective_bounciness` "0.5-0.85, always < 1.0"; `bounciness_cap` 0.85; `jackpot_burst_count` 5; `max_scored_peg_hits` 20; `base_peg_bounciness` 0.5; `auto_drop_base_interval` 2.0; `auto_drop_min_interval` 0.4; `idle_cps_unit` 2.0; `plinko_flat_per_coin_cap` 2.0; `synergy_tier_cap` 4.0; bốn giả định đo của Economy; `raw_rate_ceiling` "~26 to <= 30.7 (bounciness at cap)"; `base_coin_value` (≈ 18.7 tại n = 100).
- Thay đổi user đã duyệt (không tính là mâu thuẫn): `auto_drop_interval.output` → "≥ `auto_drop_min_interval` (0.4 s), không có trần trên; dừng khi tổng tốc độ = 0" (khớp Hamster 7.41 s ở R8 / AC-13 / AC-39); `coin_rate_ceiling` variables/output là trần mềm; `raw_rate_ceiling.output`. Catalog không tham chiếu `coin_rate_ceiling`.
- Lỗi thời (không phải mâu thuẫn giá trị): `pet.cozy_fox` và `pet.chubby_king_cat` vẫn ghi "(provisional)" ở `effect`, trong khi `scope` đã đóng theo (a) (khuyến nghị 18).

### Kiểm tra công thức (thay biên min/max)
- `pet_power_units`: 0.8 (Thường w 0.8) đến 18 (Huyền Thoại w 1.2). Tỷ số phẩm cấp liền kề 1.667 / 1.6 / 1.667 > 1 ✓; `weightMax / weightMin` 1.5 < 2.4 ✓. Ở biên safe range (Sử Thi 4.5 × 0.7 = 3.15 < Hiếm 3 × 1.3 = 3.9) có chồng lấn mà validator không bắt (khuyến nghị 6).
- `idle_base_units`: Vịt 2.0 xu/s, Công 13.2, Thỏ 0.55 (0 ở share 0, 1.1 ở share 0.2), Hỗ trợ 0 ✓.
- `pet_event_bonus`: Mèo Chiêu Tài 3.0 / 56.04 (n = 0 / 100); Thỏ 0.31755; m = NaN / −1 / +∞ → 0; trần 10 × `base_coin_value` ✓.
- `plinko_target_uplift`: 0.036 đến 0.81; Thỏ 0.12375 (0.1375 ở share 0); Kẹo 0.324; Mèo Chiêu Tài 0.1125 ✓.
- `plinko_effect_uplift`: `coins_per_primary` = 1.545. Thỏ 0.12423, Thiên Nga 0.04969, Mèo Chiêu Tài 0.11047 (0.17068 khi tính Bùng Xu), Bánh Flan 0.04489, Hamster 0.0450. Kẹo: m 1.0 → 0, m 1.3 → 0.1714, m ≥ 1.7 → 0.40 ✓. Mẫu số `bounciness_cap − base_peg_bounciness` > 0 trong safe range ✓. Biên: `JackpotBonus` m = 10 tính cả Bùng Xu → 0.569 > "≈ 0.55" (nice-to-have).
- `support_power_estimate`: Cáo 6.0. Mèo Hoàng Gia scope Idle: 6.6 + 1.2 + 0.25 = 8.05 → 14.49; scope Both: 6.6 + 2.5 + 1.2 → 18.54; roster MVP: (1.2 + 0.25) × 1.8 = 2.61; `NeighborBoost` m = 0.2 → 1.0 ✓. `referenceNeighborTargetUnits` 2.5 không đích nào trong roster đạt tới (khuyến nghị 15).
- `budget_ratio`: Hamster 1.000, Thỏ 1.004, Bánh Flan 0.998, Thiên Nga 1.004, Mèo Chiêu Tài 0.982, Kẹo 1.235 (sát 1.25, đã có Open Question), Cáo 1.000, Mèo Hoàng Gia 0.966. AC-21: 0.36537 / 0.12375 = 2.952 ✓. Target = 0 → báo lỗi, không chia ✓.
- `channel_parity`: 0.444 × 4.3219 / 2 = 0.9595 ở n = 50; 0.222 ở n = 0; 1.286 ở n = 60; 4.147 ở n = 100; `plinkoUpliftPerUnit` 0.08 → 1.535 ✓. Nằm trong [0.8, 1.25] khi n ≈ 43.8–59.0 ✓ (khớp "≈ 44–59").
- `collection_spins_estimate`: 59.9 (V1_0) / 43.4 (MVP) / 1 (một thú, p = 1). Với [0.5799, 0.30, 0.12, 0.0001] (tổng = 1), p Huyền Thoại = 1e-4 chạm giới hạn tích phân → cảnh báo, không treo ✓.
- AC số: AC-6 (4 / 4 / 5), AC-7, AC-16, AC-18 (pool MVP 2 / 2 / 2 / 1; V1_0 6 / 3 / 3 / 1), AC-20, AC-22, AC-23 (mặc định 0 lỗi, 0 cảnh báo: mọi tỷ số trong dung sai, parity 0.959, spins 59.9 / 43.4 trong [40, 90], FlatPerCoin 0.086 ≤ 2.0, không pool rỗng ở `Mvp`), AC-24 (a) 3 < 4 và (b) 0.086 > 0.05, AC-30 – AC-39: tính lại đều khớp.

### Required before implementation
Không có.

### Recommended revisions
1. [main review, mới] **Knob `scopeBothMinTier` = `None`.** R2 ghi "ba knob bậc phát hành của `PetCatalog` (`buildTier`, `balanceRosterTier`, `groupRuleMinTier`) … `None` ở các knob đó cũng là lỗi", và R11.1 lặp lại đúng ba knob. Revision 5 thêm knob thứ tư `scopeBothMinTier` cùng enum. Nếu knob này về `None` (0), điều kiện R6.5 "`releaseTier` < `scopeBothMinTier`" không bao giờ đúng: `scope = Both` mở cho mọi thú mà không có lỗi lẫn cảnh báo, nên quyết định (a) không còn được giữ bằng dữ liệu. **Sửa:** R2 thành "bốn knob" và thêm `scopeBothMinTier` vào lỗi R11.1. AC-8 thêm ca (j): `PC.scopeBothMinTier` = `None` → đúng 1 lỗi.
2. [main review, giữ lại] **Hamster `AutoDropRate` trở thành thú kênh Idle (Pillar 3).** Ngân sách đo theo `(m / autoDropBaseInterval) / realisticTapRate` = 0.045. Nhưng xu tự thả vẫn chạy khi người chơi không chạm và co giãn theo `base_coin_value(n)`, còn kênh Idle thì không (quyết định (A), `idleCpsUnit` hằng số trong tiệm). Khi không chạm, Hamster cho 0.135 xu/s × 2.96 × `base_coin_value` + 0.2 xu/s CPS: ≈ 1.93 xu/s ở n = 50 và ≈ 7.66 xu/s ở n = 100, gấp ≈ 3.8 lần Vịt Gỗ cùng 1.0 đơn vị. Nó còn kích hoạt Thỏ, Mèo Chiêu Tài, Bánh Flan và Kẹo khi người chơi vắng. Điều này trái dòng "Người chơi không thể: có một thú mạnh ở cả hai kênh" (R12, dòng 162), và Open Questions không có mục này. **Sửa:** thêm cảnh báo validator `auto_drop_idle_equivalent(n) = (m / autoDropBaseInterval) × ev_eff × base_coin_value(n) / idle_cps_unit + idle_base_units`, so với `pet_power_units × (1 + budgetTolerance)` tại `EconomyBalanceTargets.spinCheckpoints`, kèm một AC. Nếu cố ý, ghi rõ `AutoDropRate` là "cầu nối idle" ở R5 / R6, sửa câu "Người chơi không thể" và thêm vào Open Question mô phỏng chung (dòng 602).
3. [main review, giữ lại] **`channel_parity` chỉ kiểm một mốc.** parity(n) = 0.222 × 1.05^(0.6n): bằng 0.222 ở n = 0, 4.147 ở n = 100, chỉ trong dải khi n ≈ 44–59; AC-23 cho "0 cảnh báo" vì chỉ nhìn n_ref = 50. Rủi ro đã ghi trong Open Questions, nhưng validator không báo. **Sửa:** tính và in `channel_parity` tại mọi `EconomyBalanceTargets.spinCheckpoints` (chỉ cảnh báo ở n_ref, các mốc khác chỉ báo cáo); AC-37 kiểm bảng in ra.
4. [main review, giữ lại] **Cân bằng MVP.** Build MVP có 7 thú và kệ 3 ô. Ở roster này Mèo Hoàng Gia chỉ đạt 2.61 / 15 = 0.17 ngân sách (AC-22): thú Huyền Thoại duy nhất của MVP yếu đúng lúc kiểm giả thuyết (Pillar 2). **Sửa:** validator báo thêm `budget_ratio` theo roster `buildTier` (chỉ báo cáo), và ghi rủi ro vào Open Questions hoặc kế hoạch playtest MVP. Nếu chấp nhận có chủ đích, ghi lý do.
5. [main review, giữ lại] **Lưu lượng `PegHitBonus`.** Mỗi `PegHit` được tính sinh một `SubmitIncome` và một `IncomeCredited` (Economy R5.4). Với ≈ 10 xu/s × 14 lần chạm, đó là ≈ 140 lần/s mỗi thú (Thỏ + Thiên Nga ≈ 280/s). Ngoài ra `IncomeCredited` của `SubmitIncome` có `coinId` = none, nên Feedback & Juice không đặt hay gộp được số bay lên xu. **Sửa:** trả một lần khi có `CoinResolved` = `m × scoredPegHits × base_coin_value` (payload Plinko đã có `scoredPegHits`), hoặc yêu cầu Shelf gộp theo xu. Sửa theo R6, Edge Case Thỏ, AC-12, AC-32 và mẫu thẻ.
6. [main review, giữ lại] **Ràng buộc chéo giữa các knob.** R11 chỉ kiểm "tăng nghiêm ngặt". **Sửa:** thêm các lỗi sau, kèm ca trong AC-4, AC-5, AC-10:
   - "phẩm cấp chồng lấn" khi `rarity_coef[r+1] × weightMin ≤ rarity_coef[r] × weightMax`;
   - `weightMin` < `weightMax`;
   - `effectLimits[BouncinessModifier].max` ≥ `bouncinessCap` / `basePegBounciness` (với 0.3 / 0.9 cần 3.0 > 2.0);
   - `effectLimits[FlatPerCoin].max` ≤ `plinkoFlatPerCoinCap`.
7. [main review, giữ lại] **`sourceId` và khóa nguồn mà AC-14 / AC-15 dựa vào.** Hai AC này phân biệt "modifier có `sourceId` thuộc hiệu ứng của Mèo Hoàng Gia hoặc Cáo" với "modifier combo nhóm", nhưng R6 không yêu cầu Shelf dùng `sourceId` riêng cho hiệu ứng thú (Economy chỉ yêu cầu tiền tố `shelf.`). R6.3 đề xuất khóa nguồn `shelf.slot<i>.idle` / `.plinko`, trong khi Economy R4.2 / AC-19 dùng `shelf.slot2` và modifier đã có trường `channel`. **Sửa:** R6.3 yêu cầu Shelf cho modifier từ hiệu ứng thú một `sourceId` nhận diện được thú hoặc hiệu ứng nguồn (ví dụ `shelf.slot<i>.effect`), khác `sourceId` của combo nhóm. Nêu thêm cách ánh xạ `scope`: khóa nguồn tách kênh, hoặc trường `channel` của Economy nếu Economy xác nhận `channel` lọc được modifier nhắm `sourceKey`. Ghi vào Open Question dòng 609.
8. [main review, giữ lại và mở rộng] **Phần còn hở của `None = 0`.**
   - (a) Lỗi "knob bậc phát hành = `None`" (R11.1) không có AC; thêm vào AC-7 hoặc một AC mới (cùng ca của khuyến nghị 1).
   - (b) R2 hứa "mỗi trường thiếu cho đúng một lỗi", nhưng chỉ bỏ qua ba loại kiểm tra. Thực tế, `role = None` trên Vịt Gỗ làm nhóm `animal` thiếu thú Idle (thêm lỗi luật nhóm); `group = None` làm nhóm cũ thiếu thú; `rarity = None` không có hệ số, nên có thể sinh lỗi "target = 0". **Sửa:** ghi rõ thú có enum bắt buộc = `None` bị loại khỏi luật nhóm, đích `GroupLeader`, mô hình ngân sách và pool.
   - (c) Lúc chạy, bộ lọc R9.1 "`releaseTier` ≤ `buildTier`" cho bản ghi có `releaseTier = None` (= 0) đi qua. **Sửa:** R12.1 coi enum bắt buộc = `None` là lỗi nạp (`IsFailed`) hoặc loại bản ghi đó, kèm một ca trong AC-26.
9. [main review, giữ lại] **Literal chưa là knob hoặc hằng có lý do** (vi phạm luật "mọi con số là knob"):
   - AC: "± 0.001" (AC-20, AC-37), "± 0.0001" (AC-34), "± 0.3" và "± bước tích phân" (AC-38). AC-32 ghi 0.3175 (chính xác 0.31755) không có ngoặc, không có sai số. Mặc định so sánh của Catalog là `ET.valueRelTolerance` = 1e-9.
   - Luật và mô tả: R5.5 "≥ 1 thú Idle, ≥ 1 thú Plinko"; Edge Cases "≈ 10.9% … ≈ 20% … 1.83 lần"; ngưỡng 1000 trong quy tắc định dạng thẻ.
   - **Sửa:** thêm `PetBalanceTargets.reportedValueAbsTolerance` (0.001) và `.collectionSpinsAbsTolerance` (0.3), rồi tham chiếu trong AC; hoặc đặt mọi số kỳ vọng trong ngoặc và so bằng `ET.valueRelTolerance`. Gắn R5.5 với hằng có lý do hoặc knob `PetCatalog.minRolePetsPerGroup`. Edge Cases tham chiếu `EconomyBalanceTargets.plinkoJackpotProbMeanAssumed` và `PlinkoBalanceTargets.capJackpotProbMeanMax`. Ghi 1000 là hằng: ngưỡng hậu tố của Economy R13.
10. [main review, mới] **Biên của quy tắc định dạng thẻ và chủ sở hữu quy tắc.** (a) Quy tắc 1 làm tròn 3 chữ số có nghĩa cho |v| < 1000, nên v trong [999.5, 1000) ra "1000": bốn chữ số, không hậu tố, khác "1.00K" của quy tắc 2. **Sửa:** áp quy tắc 1 khi giá trị đã làm tròn < 1000, ngược lại dùng quy tắc 2; thêm ca 999.7 vào AC-39. (b) Bộ định dạng thuộc Economy Core, nhưng quy tắc này chỉ có trong Catalog, nên story của Economy sẽ không xây nó. **Sửa:** ghi vào danh sách Sync để mở lại `economy-core.md` R13 (thêm quy tắc "số mô tả", AC tương ứng), hoặc ghi rõ HUD hay thẻ thú là nơi cài và Catalog sở hữu quy tắc.
11. [main review, giữ lại] **AC-29 / AC-40.** AC-29 ghi "máy tham chiếu của dự án". **Sửa:** dùng `BootTestTargets.perfReferenceDevice` (đã có ở Bootstrap) và nêu nguồn số đo (thời lượng bước trong báo cáo khởi động của Bootstrap). AC-40 cần "khung màu Huyền Thoại". **Sửa:** kiểm theo khóa màu `PetRarityConfig.rarities[legendary]`, hoặc ghi art bible là điều kiện tiên quyết.
12. [main review, giữ lại] **`inGachaPool` của 13 thú không được ghi.** **Sửa:** thêm cột `inGachaPool` (true cho cả 13) vào R8 và bảng knob theo từng thú; AC-16 kiểm thêm cột này. AC-18 hiện ngầm định true.
13. [main review, giữ lại] **Mẫu mô tả `FlatPerCoin`.** R6 và `credit_coin` của Economy cộng phẳng cho mọi `CoinResolved` (Bucket, Stuck, Overflow), nhưng thẻ ghi "mỗi xu vào hũ". **Sửa:** đổi thành "mỗi xu Plinko", hoặc giới hạn luật vào `reason = Bucket` (khi đó Economy phải đổi theo).
14. [main review, giữ lại] **Roster của `collection_spins_estimate`.** R11.2 cảnh báo khi giá trị ngoài `collectionSpinsBand`, nhưng không nói dùng pool bậc `buildTier` hay `balanceRosterTier`. Formulas đưa cả 59.9 và 43.4; MVP 43.4 chỉ cách cận dưới 40 là 3.4. **Sửa:** ghi rõ (ví dụ: cảnh báo theo `balanceRosterTier`, chỉ báo cáo theo `buildTier`); AC-23 và AC-38 nêu roster tương ứng.
15. [main review, giữ lại] **`referenceNeighborTargetUnits` 2.5 không đạt được trong roster.** Đích hợp lệ của Cáo (scope Plinko, chỉ thú Plinko có thưởng sự kiện) có `scoped_units` Thỏ 2.475, Mèo Chiêu Tài 2.25, Thiên Nga 0.99, đều < 2.5. Hai đích tốt nhất cho 1.2 × 4.725 = 5.67 (tỷ số 0.945). **Sửa:** tính `NeighborBoost` từ roster như `GroupLeader` (tổng `neighbor_slots` đích hợp lệ lớn nhất trong roster `balanceRosterTier`), hoặc ghi lý do giữ giá trị giả định và giới hạn safe range ≤ `scoped_units` lớn nhất.
16. [main review, giữ lại] **Audio.** Catalog định nghĩa hậu tố cue `Gacha.RarityReveal.<phẩm cấp>` = ID phẩm cấp (Visual/Audio, Open Questions dòng 608), nhưng Dependencies không có hàng Audio, và `audio.md` không liệt kê Pet Catalog. **Sửa:** thêm hàng Audio (soft, Out: ID phẩm cấp làm hậu tố cue) vào Dependencies; báo bước Sync để `audio.md` ghi lại khi mở lại (hoặc qua Gacha GDD).
17. [main review, mới] **Ghi chú lỗi thời trong GDD.** Hàng HUD trong Dependencies vẫn ghi "*Chưa có trong systems index — báo bước Sync*", và hàng Sync (Open Questions dòng 613) vẫn ghi "thêm UX flag". Systems index đã có cả hai: HUD và Feedback có Pet Catalog (soft), UX flag thẻ thú ở dòng 5. **Sửa:** đổi thành "đã có" hoặc xóa.
18. [main review, Sync] **Việc cho bước Sync (không sửa ở bước này).**
   - (a) Header GDD và hàng 2 của systems index cùng đặt `Approved`.
   - (b) entities.yaml: bỏ "(provisional)" trong `effect` của `pet.cozy_fox` và `pet.chubby_king_cat`.
   - (c) Áp ba thay đổi user đã duyệt: `auto_drop_interval.output`; `coin_rate_ceiling` variables/output là trần mềm, `referenced_by` thêm `economy-core.md`, `audio.md`; `raw_rate_ceiling.output` nếu đổi.
   - (d) Đăng ký `bounce_uplift_at_cap` (source `economy-core.md`, referenced_by `pet-catalog.md`, `plinko-board.md`).
   - (e) Thêm `pet-catalog.md` vào `referenced_by` của `idle_cps_unit` (đã có), `display_format` (nếu đăng ký) và `raw_rate_ceiling` (Catalog dùng ba con số của nó ở R8 / budget_ratio).
19. [main review, Sync / GDD khác] **Tham chiếu lỗi thời ở GDD khác.**
   - `economy-core.md`: Open Questions dòng 715 và "Hai chiều" dòng 486 vẫn ghi Catalog "còn chuyển nốt" `bounceUpliftAtCapAssumed`; Cross-References dòng 609 ghi "sẽ đọc". Catalog đã làm xong ở Revision 5, nên đóng các mục này.
   - `plinko-board.md`: output `raw_rate_ceiling` (dòng 357), Cross-References (dòng 625) và AC-13c vẫn ghi `PetBalanceTargets.bounceUpliftAtCapAssumed`, knob đã bị xóa; đổi thành `EconomyBalanceTargets.plinkoBounceUpliftAtCapAssumed`.
   - Catalog đã ghi việc này ở Open Questions (dòng 600).

### Specialist disagreements
Không có (lean review, không gọi specialist).

### Nice-to-have
- R12.2 / AC-27 `TryGetPet(null)`: `Dictionary.TryGetValue(null)` ném `ArgumentNullException`. Ghi rõ phải kiểm `null` / rỗng trước khi tra.
- R12.3 chỉ nói "tra theo ID không cấp phát", nhưng AC-28 đo cả "lấy pool theo phẩm cấp". Ghi rõ pool trả danh sách chỉ đọc đã cấp sẵn.
- AC-19: khi `pet.chubby_king_cat` bị `deprecated`, ghi rõ `inGachaPool = false`; nếu không, R9.2 sinh thêm lỗi ngoài cảnh báo "pool rỗng".
- R9.1 "không được đưa vào catalog của build đó": thú Scheduled vẫn nằm trong build (tham chiếu từ `PetCatalog.pets`), chỉ bị lọc lúc chạy. Nói rõ điều này, hoặc lọc ở bước tiền build nếu không muốn lộ thú chưa phát hành trên web.
- Formulas `pet_power_units`: Range của `w` ghi 0.8–1.2, trong khi safe range của knob cho phép 0.7–1.3. Output `support_power_estimate` "≈ 2.5 × 3 × 6.6" không phải cận trên theo safe range (K tới 4, `scoped_units` tới 18).
- Formulas `plinko_effect_uplift`: Output ghi "≈ 0.55", nhưng `JackpotBonus` m = 10 tính cả Bùng Xu cho 0.569.
- AC-10: nêu thú dùng cho từng loại hiệu ứng (thú có vai khớp), để tránh lỗi vai đi kèm.
- Inspector của Unity sao chép phần tử cuối khi bấm "+" trên mảng `effects`; AC-8 (c) nên ghi rõ là `new PetEffect()` trong code.
- Visual: emoji giấy phép mở (ví dụ Twemoji CC-BY 4.0) cần dòng ghi công trong credits.
- Completeness: thiếu `## Player Fantasy` (advisory ở workflow standard).

### Unity
Thiết kế dựa vào các tính năng sau, và `unity-reference` không đánh dấu tính năng nào là đã đổi hoặc deprecated ở Unity 6000.3:
- ScriptableObject; enum serialize mặc định 0 (đúng hành vi Unity, là cơ sở của `None = 0`); `Dictionary` tra theo chuỗi.
- Kiểm tra trong Editor chặn build (dự kiến `IPreprocessBuildWithReport` / `BuildFailedException`).
- Đo GC bằng `GC.GetAllocatedBytesForCurrentThread` / `ProfilerRecorder`.
- Tùy chọn package Localization.

Không có module scripting riêng; GDD đã ghi feasibility chưa kiểm và các điểm cần ADR (nạp Catalog không qua Addressables, bản địa hóa, bảng tra không cấp phát). Mọi AC là [EditMode] / [Perf] / [Screenshot], chạy được trong assembly `CozyGachapon.Tests.EditMode`. AC-29 cần build WebGL development.

### Senior verdict [creative-director]
Không áp dụng (lean review).

### Scope signal
Rough scope signal: L (producer should verify before sprint planning)
- 10 công thức (3 lúc chạy, 7 mô hình validator). Validator Editor có mô hình cân bằng đọc config của Economy và Plinko. Cấp dữ liệu cho 5 hệ downstream cộng UI thẻ thú dùng chung. Cần ADR nạp và bản địa hóa (có thể dùng chung ADR khởi động).

### Verdict: APPROVED
Mục chặn của báo cáo trước (PC3-B1) đã xử lý; câu NEEDS-USER về `scope` đã được user đóng theo (a). Không còn mục chặn và không còn câu hỏi NEEDS-USER mở. Nên xử lý trước khi viết GDD Shelf & Synergy:
- khuyến nghị 1 và 8 (để validator giữ được quyết định (a) và hợp đồng `None`);
- khuyến nghị 7 (hợp đồng `sourceId` / kênh mà Shelf phải cài);
- khuyến nghị 2–5 (cân bằng kênh, Hamster, MVP, lưu lượng sự kiện), vì chúng ảnh hưởng mô phỏng chung với Economy Core và Shelf & Synergy.
