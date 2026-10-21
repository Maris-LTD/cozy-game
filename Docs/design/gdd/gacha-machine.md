# Gacha Machine (Máy Trứng Gachapon)

> **Status**: In Review
> **Author**: user + game-designer (gdd-pipeline, đợt GDD 2)
> **Last Updated**: 2026-10-21
> **Last Verified**: 2026-10-21
> **Implements Pillar**: Pillar 2 — Gacha Thrill (hồi hộp có kiểm soát: tỷ lệ công khai, bảo hiểm, nút tua, không bán lượt quay bằng tiền thật); tôn trọng Anti-Pillar "không mất mát vĩnh viễn"

## Summary

Gacha Machine là sink xu chính: người chơi trả giá quay do Economy Core tính, máy chọn phẩm cấp theo tỷ lệ công khai 55/30/12/3 (có bảo hiểm Huyền Thoại sau N lượt và lượt đầu chắc ra thú Thường vai Idle), chọn một thú trong pool của phẩm cấp đó, ghi ngay vào Inventory rồi mới mở modal bóc trứng do Feedback & Juice dựng sân khấu.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Economy Core (hard: TryPaySpin, GetSpinPrice, CanAfford, SpinPriceChanged)`, `Pet Catalog (hard: Gacha pool R9.2)`, `Inventory (hard: AddPet)`, `Bootstrap & Scene Flow (hard: bước gacha.machine, AcquireTabLock)`, `HUD & Tab Navigation (hard: ModalHost, PetCardView)`, `Feedback & Juice (hard về luồng: RequestReveal / RevealFinished)`

## Overview

Máy trứng nằm ở tab Bàn Chơi, dưới bàn Plinko. Một lượt quay là **một thao tác đồng bộ**: trả tiền qua `TryPaySpin`, quay phẩm cấp, chọn thú, gọi `Inventory.AddPet`, phát `GachaRolled`. Chỉ sau đó máy mới trình bày: mở modal qua `ModalHost` của HUD, khóa tab qua Bootstrap và nhờ Feedback & Juice chạy nhịp "Cạch… Cạch… BỤP!" theo `RevealProfile` của phẩm cấp. Vì kết quả đã ghi trước hoạt cảnh, tải lại trang WebGL hay tắt app giữa chừng không làm mất thú và không cho quay lại kết quả (chống save-scum).

Máy sở hữu `GachaConfig`: tỷ lệ phẩm cấp, trọng số trong phẩm cấp, ngưỡng bảo hiểm, luật lượt đầu, luật tua và bảng tỷ lệ hiển thị. Máy **không** sở hữu giá quay (Economy D1/D6), dữ liệu và pool thú (Pet Catalog R9), số bản (Inventory), sân khấu bóc trứng (Feedback FB D3), khung popup (HUD D3), Sao (Star Progression, VS) hay lượt quay qua quảng cáo (Alpha, chỉ chừa API). Bán lượt quay bằng tiền thật bị cấm vĩnh viễn (Anti-Pillar).

## Detailed Design

### Core Rules

**G1. Ranh giới và dữ liệu.** `GachaConfig` (ScriptableObject) giữ mọi knob của hệ (Tuning Knobs). Bảng tỷ lệ của Gacha **thay** giả định `PetBalanceTargets.assumedRarityRates` của Pet Catalog: Catalog tham chiếu `GachaConfig.rarityRates` khi tính `collection_spins_estimate` (việc sửa Catalog thuộc bước Sync, GDD này không sửa). Nguồn ngẫu nhiên được tiêm qua interface, có seed cho test, không lưu trạng thái RNG (→ ADR).

**G2. Bước khởi động.** `IBootStep` với `Id = gacha.machine`, `Phase = GameplayInit`, `RunsAfter` rỗng (khớp Bootstrap AC-2), `IsCritical = true`. `Run()` nạp `GachaConfig`, đọc Gacha pool của 4 phẩm cấp từ Catalog (đã `Ready` ở `DataLoad`), tính `effective_rarity_rates` và bảng tỷ lệ hiển thị, đăng ký `SpinPriceChanged`. `Run()` không gọi `AddPet` (Inventory R4.4). Máy chỉ nhận lượt quay sau `Interactive`.

**G3. Nút vặn và giá (Economy D1/D6).** Nút vặn hiện `GetSpinPrice()` (chuỗi định dạng của Economy) và làm mới mỗi `SpinPriceChanged`. Nút mờ khi `CanAfford(coin, giá)` = false; bấm khi mờ **không** gọi `TryPaySpin`, chỉ lắc nút (`deniedShakeSeconds`) — không tạo `SpendRejected` thừa.

**G4. Một lượt quay trả xu — `TrySpin(playerAction)`.** Chỉ chạy khi máy ở `Idle`. Trình tự đồng bộ, cùng một khung:
1. Kiểm tra trước khi thu tiền: Inventory `Ready`, ít nhất một phẩm cấp có pool khác rỗng. Sai → trả `Unavailable`, không gọi Economy.
2. `Economy.TryPaySpin(playerAction)`. `false` → trả `PaymentRejected`, không đổi gì (Economy tự phát `SpendRejected`; nút lắc như G3).
3. Chọn phẩm cấp theo thứ tự ưu tiên: luật lượt đầu (G6) → bảo hiểm (G7) → `rarity_roll`.
4. Chọn thú trong pool phẩm cấp đó bằng `pet_pick`.
5. `Inventory.AddPet(petId, copiesPerSpin)` → `Added { isNew, copies }` (Inventory phát `PetAcquired` trước khi trả về).
6. Cập nhật bộ đếm của Gacha: `lifetimeSpins` + 1, `spinsSinceLegendary` (G7), `firstSpinGuaranteeUsed`.
7. Phát `GachaRolled { petId, rarity, isNew, copies, wasPity, spinsSincePity, wasFree }` (`copies` lấy từ bước 5, `spinsSincePity` = bộ đếm **sau** lượt).
8. Bắt đầu trình bày (G8) và trả `Rolled`.

**G5. Lượt quay miễn phí — `TryGrantFreeSpin(source)`.** Giống G4 nhưng bỏ bước 2: không gọi `TryPaySpin`, không tăng `spinsThisShop`, `wasFree = true`. MVP không có nguồn gọi (Ads: Alpha; Tutorial: VS). Trả `false` khi không ở `Idle`. Mọi lượt (trả xu hay miễn phí) đều tính vào `lifetimeSpins`, luật lượt đầu và bảo hiểm.

**G6. Lượt đầu tiên (Gacha D4).** Khi `firstSpinGuaranteeEnabled` và `firstSpinGuaranteeUsed` = false, lượt đó ra phẩm cấp `firstSpinRarity` (Thường) và chọn thú trong tập con pool có vai `firstSpinRole` (Idle), bằng `pet_pick`. Lượt này vẫn trả tiền, vẫn tính bảo hiểm (không phải Huyền Thoại). Tập con rỗng → quay bình thường, validator cảnh báo. Luật áp dụng một lần trong đời save, không lặp lại sau Prestige. MVP: Vịt Gỗ hoặc Ếch Cốm, mỗi con 50%. Dòng "Lượt đầu tiên luôn ra thú Thường vai Idle" luôn hiện cạnh bảng tỷ lệ khi luật bật và tập con khác rỗng.

**G7. Bảo hiểm Huyền Thoại (Gacha D2).** `spinsSinceLegendary` (x) đếm số lượt liên tiếp không ra Huyền Thoại. Lượt bị ép Huyền Thoại khi `pity_forced` đúng. Ra Huyền Thoại (tự nhiên hay ép) → x = 0; ngược lại x + 1. Máy hiện `x/N` liên tục. Pool Huyền Thoại rỗng → bảo hiểm tạm dừng: x đứng yên, không ép, bộ đếm hiện "—". Bộ đếm giữ qua Prestige.

**G8. Trình bày lượt quay (Gacha D5, FB D3, HUD D3).**
1. `AcquireTabLock(owner = gacha.machine)`; `ModalHost.Open(ModalRequest { modalId = gacha.reveal, contentView = GachaRevealView, dismissible = false })`. Host lấy token `AcquireInputBlock` và đẩy Back handler (HUD H9), nên chạm bàn Plinko và nav bị chặn; Back bị nuốt.
2. `GachaRevealView` chứa `PetCardView(petId)` của HUD và dòng của Gacha ("MỚI!" hoặc "×`copies` — dùng để lên Sao (sắp có)", nhãn "Bảo hiểm" khi `wasPity`). Gacha là bên duy nhất bật thẻ: thẻ ẩn tới khi nhận `RevealSettleStarted { handle }` khớp từ Feedback (đầu pha `Settle`, kể cả sau tua — không lộ kết quả trước `Burst`); nếu `RevealFinished` đến trước (Feedback `Failed`/bận) thẻ hiện cùng lúc đó.
3. Vào `Presenting.Revealing` và ghi `pendingReveal` (petId, `awaitingHandle` = true) **trước** khi gọi `Feedback.RequestReveal(petId, rarity, canSkip)` với `canSkip = lifetimeSpins ≥ skipUnlockSpinIndex` (nút tua từ lượt 2). Lời gọi trả về → lưu `RevealHandle`, `awaitingHandle` = false. Nút "Tua" gọi `SkipReveal()`.
4. Nhận `RevealFinished { handle, … }` khớp handle đang chờ, **hoặc** đến trong lúc `awaitingHandle` = true (Feedback phát đồng bộ bên trong `RequestReveal`) → `CardShown`: nút "Nhận" hiện, bấm được sau `claimButtonDelaySeconds` (chống chạm xuyên từ nút Tua). Bấm → `ModalHost.Close(handle)`; khi `ModalClosed` → nhả khóa tab, về `Idle`.
5. Trong suốt modal, xu đang rơi, auto-drop và thu nhập Idle vẫn chạy (Gacha D5); chỉ chạm tay bị chặn.

**G9. Thú trùng (Gacha D3).** MVP: trùng chỉ cộng số bản qua `AddPet`, thẻ báo "dùng để lên Sao (sắp có)"; không hoàn xu, không quy đổi. Star Progression (VS) đọc `copies` từ Inventory và nghe `GachaRolled`.

**G10. Bảng tỷ lệ công khai.** Nút "i" trên máy mở modal `dismissible = true` qua `ModalHost`: % theo phẩm cấp (`effective_rarity_rates`), % từng thú (`pet_display_probability`), dòng bảo hiểm "Chắc chắn Huyền Thoại trong tối đa N lượt (x/N)" kèm tỷ lệ thực tế `legendary_effective_rate`, và dòng lượt đầu (G6). Mọi số tính từ `GachaConfig` + pool hiện hành, không gõ tay.

**G11. Chỉ vặn từng quả (Gacha D6).** Không có quay ×10 ở MVP. Lần bấm thứ hai trong cùng khung hoặc khi đang trình bày bị bỏ qua (`Busy`).

### States and Transitions

| State | Entry condition | Exit condition | Behavior |
|---|---|---|---|
| Uninitialized | Scene nạp | Bootstrap gọi `Run()` | Nút ẩn; `TrySpin` → `Unavailable` |
| Waiting | `Run()` xong | `Interactive` → Idle | Nút hiện giá nhưng không nhận bấm (rèm còn che) |
| Idle | `Interactive`; hoặc `ModalClosed` của modal bóc trứng | `TrySpin`/`TryGrantFreeSpin` thành công → Presenting | Nhận lượt quay; nút theo `CanAfford` |
| Presenting.Revealing | Modal mở, ngay **trước** lời gọi `RequestReveal` (G8.3) | `RevealFinished` khớp handle hoặc đến trong lời gọi → CardShown | Nút máy Busy; thẻ ẩn tới `RevealSettleStarted` khớp; nút Tua nếu `canSkip` |
| Presenting.CardShown | `RevealFinished` (thẻ hiện nếu chưa hiện) | Bấm "Nhận" → `ModalClosed` → Idle | Nút "Nhận" bấm được sau `claimButtonDelaySeconds` |
| PendingPresentation | `ModalHost.Open` trả `Rejected` sau khi kết quả đã ghi | `ModalClosed` của modal đang chặn → thử `Open` lại → Presenting | Kết quả đã nằm trong Inventory; nút máy Busy |
| Failed | `Run()` lỗi (config không hợp lệ, mọi pool rỗng) | Không thoát trong phiên | Nút "Máy đang bảo trì" mờ; không bao giờ gọi `TryPaySpin` |

`Paused`/`Resumed` không đổi trạng thái: modal, khóa tab và đồng hồ `claimButtonDelaySeconds` đứng yên cùng sân khấu của Feedback.

### Interactions with Other Systems

| System | Gacha gọi / đọc | Gacha phát / cung cấp | Owner |
|---|---|---|---|
| Economy Core | `TryPaySpin(playerAction)`, `GetSpinPrice()`, `CanAfford`, nghe `SpinPriceChanged` | — | Economy (đường giá neo, n) |
| Pet Catalog | Gacha pool theo phẩm cấp (R9.2), phẩm cấp, vai, sprite | — | Catalog (pool); Gacha (tỷ lệ, trọng số) |
| Inventory | `AddPet(petId, copiesPerSpin)`, `IsOwned` | — | Inventory (ghi, `PetAcquired`); Gacha (khi nào gọi, luật trùng) |
| Bootstrap & Scene Flow | Bước `gacha.machine`, `AcquireTabLock`/`Release`, `Interactive`, `Paused`/`Resumed` | — | Bootstrap (cơ chế); Gacha (chính sách khóa) |
| HUD & Tab Navigation | `ModalHost.Open/Close`, `ModalClosed`, `PetCardView` | Nội dung modal | HUD (khung, token, Back); Gacha (nội dung) |
| Feedback & Juice | `RequestReveal(petId, rarity, canSkip)`, `SkipReveal()`, nghe `RevealSettleStarted`, `RevealFinished` | `GachaRolled` | Feedback (sân khấu, `RevealProfile`, cue); Gacha (luồng, thẻ, tua) |
| Star Progression (VS), Tutorial (VS) | — | `GachaRolled`; `TryGrantFreeSpin` (Tutorial) | Hệ nghe |
| Save/Load (VS) | Dữ liệu đã lưu khi khởi tạo | `lifetimeSpins`, `spinsSinceLegendary`, `firstSpinGuaranteeUsed` | Save/Load (định dạng); Gacha (nội dung) |

## Formulas

Ký hiệu: r ∈ [`common`, `rare`, `epic`, `legendary`] (ID phẩm cấp của Catalog, thứ tự cố định); `GC.` = `GachaConfig`; `s(v)` = v nếu hữu hạn và ≥ 0, ngược lại 0 (đồng nhất Economy R4.5); `ne[r]` = 1 nếu pool r có ít nhất một thú với trọng số > 0, ngược lại 0. Giá quay không có công thức ở đây: `spin_price(n)` thuộc Economy Core.

### effective_rarity_rates

`rate_eff[r] = s(GC.rarityRates[r]) × ne[r] / Σ_k ( s(GC.rarityRates[k]) × ne[k] )`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| GC.rarityRates[r] | float[4] | 0.01–0.7 mỗi phần, tổng = 1 | SO field `GachaConfig.rarityRates` | tỷ lệ công bố (Gacha D1) |
| ne[r] | int | 0–1 | calculated (pool Catalog R9.2 + `pet_pick` trọng số) | phẩm cấp có thú quay được |
| rate_eff[r] | double | 0–1 | calculated | tỷ lệ thật dùng để quay và để hiển thị |

**Output range:** mỗi `rate_eff` trong [0, 1], tổng = 1 khi mẫu số > 0. Mẫu số = 0 (mọi pool rỗng hoặc mọi tỷ lệ 0) → máy vào `Failed`, không bao giờ thu tiền.
**Example:** mặc định, 4 pool khác rỗng → 0.55 / 0.30 / 0.12 / 0.03. Pool Huyền Thoại rỗng → mẫu 0.97 → 0.5670 / 0.3093 / 0.1237 / 0.

### rarity_roll

`cum[r] = Σ_{k ≤ r} rate_eff[k]`, dựng **một lần trong `Run()`**; phần tử của r cuối có ne = 1 gán đúng `1.0` (hằng: biên trên của u). `rarity = r đầu tiên theo thứ tự có ne[r] = 1 và u < cum[r]`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| u | double | [0, 1) | calculated (RNG tiêm, một lần rút) | số ngẫu nhiên phẩm cấp |
| cum[r] | double | 0–1 | calculated (`effective_rarity_rates`, bảng dựng sẵn) | tỷ lệ cộng dồn; phần tử cuối khác rỗng = 1.0 chính xác |

**Output range:** một trong 4 ID phẩm cấp, không bao giờ là phẩm cấp có pool rỗng; vì cum cuối = 1.0 > u nên luôn có r thỏa. Biên: u = 0 → `common` (nếu khác rỗng). Biên đúng 0.55/0.97 lệch theo sai số float của `GC.rarityRates` (0.55f+0.30f+0.12f ≠ 0.97), nên test kiểm hai phía biên cách 1e-4.
**Example:** u = 0.62 → cum 0.55, 0.85 → `rare`. u = 0.9701 ≥ cum[epic] ≈ 0.97 → `legendary`. Lượt bị G6/G7 ép không rút u.

### pity_forced

`pity_forced = (GC.pityThreshold > 0) ∧ ne[legendary] ∧ (x + 1 ≥ GC.pityThreshold)`; sau lượt: `x' = 0` nếu rarity = `legendary`; `x' = x` nếu ne[legendary] = 0; ngược lại `x' = x + 1`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| x | int | 0 – `pityThreshold` − 1 | calculated (`spinsSinceLegendary`, lưu từ VS) | số lượt liên tiếp không Huyền Thoại |
| GC.pityThreshold | int | 30–90; 0 = tắt (chỉ test) | SO field `GachaConfig.pityThreshold` | N của bảo hiểm cứng (Gacha D2) |

**Output range:** x không bao giờ vượt N − 1 khi bảo hiểm bật; khoảng cách giữa hai Huyền Thoại ≤ N lượt. Bộ đếm hiển thị `x/N`.
**Example:** N = 50, x = 49 → ép Huyền Thoại, `wasPity` = true, x' = 0. x = 12, quay tự nhiên ra Huyền Thoại → `wasPity` = false, x' = 0.

### pet_pick

`p(i | r) = w_i / Σ_{j ∈ pool(r)} w_j` với `w_i = s(GC.dropWeightOverrides[i])` nếu có, ngược lại `GC.defaultDropWeight`; chọn bằng `v = u₂ × Σw` trên pool sắp theo `id` ordinal tăng dần

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| w_i | float | 0.25–4.0 (0 = loại khỏi pool) | SO field `GachaConfig.dropWeightOverrides` / `GachaConfig.defaultDropWeight` | trọng số rơi (`dropWeight`) trong phẩm cấp, không phải `PetDefinition.weight` |
| pool(r) | list | 0–N thú | calculated (Catalog R9.2; với G6 lọc thêm vai `firstSpinRole`) | ứng viên |
| u₂ | double | [0, 1) | calculated (RNG tiêm, luôn rút một lần) | số ngẫu nhiên chọn thú |

**Output range:** một `petId` thuộc pool(r) có w > 0; mặc định đều nhau (khớp mô hình `collection_spins_estimate` p_i = rate / pool_count của Catalog).
**Example:** pool `rare` MVP {`pet.bouncy_bunny`, `pet.lucky_cat`} đều nhau → 0.5 / 0.5; override `pet.lucky_cat` = 3.0 → 0.25 / 0.75. Lượt đầu MVP: tập con Thường ∧ Idle = {`pet.dreamy_frog`, `pet.woody_duck`} → 0.5 / 0.5.

### pet_display_probability

`P(i) = rate_eff[r(i)] × p(i | r(i))`; hiển thị `round_half_up(100 × P(i), GC.ratePercentDecimals)` %

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| GC.ratePercentDecimals | int | 0–2 | SO field `GachaConfig.ratePercentDecimals` | số chữ số thập phân hiển thị |

**Output range:** 0–100 %; dòng phẩm cấp là số chuẩn, dòng từng thú làm tròn độc lập nên tổng có thể lệch ≤ 0.05 × số thú (Edge Cases).
**Example:** MVP: Thường 27.5 % mỗi con (Vịt Gỗ, Ếch Cốm), Hiếm 15.0 %, Sử Thi 6.0 %, Mèo Hoàng Gia 3.0 %. v1.0 (6/3/3/1): Thường 9.2 %, Hiếm 10.0 %, Sử Thi 4.0 %, Huyền Thoại 3.0 %.

### legendary_effective_rate

`E_N = (1 − (1 − p)^N) / p`; `rate_with_pity = 1 / E_N`; N = 0 → `rate_with_pity = p`; p = 0 → không hiển thị

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| p | float | 0–0.7 | calculated (`rate_eff[legendary]`) | tỷ lệ Huyền Thoại mỗi lượt |
| N | int | 0, 30–90 | SO field `GachaConfig.pityThreshold` | ngưỡng bảo hiểm |
| E_N | double | 1 – 1/p | calculated | số lượt kỳ vọng giữa hai Huyền Thoại |

**Output range:** `rate_with_pity` ∈ [p, 1]. Mặc định 3.84 %; N = 30 → 5.01 %; N = 90 → 3.21 %. Xác suất bảo hiểm phải kích hoạt trong một chu kỳ = (1 − p)^(N−1) = 22.5 % với mặc định. Không tính luật lượt đầu (chỉ một lượt trong đời save).
**Example:** p = 0.03, N = 50: 0.97^50 = 0.2181 → E = 0.7819 / 0.03 = 26.06 lượt → 3.84 %, hiện trong bảng tỷ lệ là "≈ 3.8 % tính cả bảo hiểm".

## Edge Cases

- **If bấm nút vặn khi `CanAfford` = false**: không gọi `TryPaySpin`, không sự kiện, nút lắc `deniedShakeSeconds`, không cue (Open Questions).
- **If `TryPaySpin` trả false dù `CanAfford` vừa đúng** (chi tiêu khác trong cùng khung): `PaymentRejected`, máy giữ `Idle`, không bộ đếm nào đổi; Economy phát `SpendRejected` (Feedback phát `Ui.Rejected`), nút lắc.
- **If hai lần bấm trong cùng khung**: lần đầu chạy G4 và đưa máy sang Presenting trước khi trả về; lần hai trả `Busy`, đúng một `TryPaySpin`.
- **If một phẩm cấp có pool rỗng** (mọi thú `deprecated`, `inGachaPool = false` hoặc trọng số 0): `ne[r]` = 0, tỷ lệ chia lại (`effective_rarity_rates`), bảng hiển thị dùng tỷ lệ đã chia lại và ẩn dòng phẩm cấp đó; không bao giờ có "trứng rỗng" sau khi đã thu xu.
- **If pool Huyền Thoại rỗng**: bảo hiểm tạm dừng, x đứng yên, bộ đếm hiện "—", dòng bảo hiểm ẩn khỏi bảng; x cũ dùng lại khi pool có thú trở lại (bản cập nhật).
- **If mọi pool rỗng hoặc tổng tỷ lệ = 0**: `Run()` báo `Failed`; validator báo lỗi lúc Edit time.
- **If `GC.rarityRates` không cộng đúng 1** (sai số > 0.001) hoặc có phần âm/NaN: validator báo lỗi; lúc chạy chuẩn hóa theo tổng sau `s()` nên vẫn quay đúng tỷ lệ tương đối.
- **If `dropWeightOverrides` có ID không thuộc pool nào hoặc trùng ID**: bỏ qua dòng lạ, dòng trùng lấy dòng đầu; validator cảnh báo.
- **If tập con lượt đầu (Thường ∧ Idle) rỗng**: lượt đầu quay bình thường, `firstSpinGuaranteeUsed` vẫn thành true, dòng lượt đầu ẩn; validator cảnh báo.
- **If lượt đầu trùng lúc bảo hiểm đủ ngưỡng**: không xảy ra với dải an toàn (N ≥ 30); nếu cấu hình test đặt N = 1, luật lượt đầu thắng (G4.3) và x' = x + 1.
- **If `AddPet` trả `Rejected` sau khi đã thu xu**: không đạt được khi điều kiện G4.1 đúng (ID lấy từ pool Catalog, amount = `copiesPerSpin` ≥ 1, Inventory `Ready`); nếu vẫn xảy ra: ghi lỗi, không đổi bộ đếm, không phát `GachaRolled`, không trình bày, máy về `Idle` (Open Questions: hoàn xu).
- **If `ModalHost.Open` trả `Rejected(StackFull)`**: kết quả đã ghi; máy vào `PendingPresentation`, giữ khóa tab, thử `Open` lại ở mỗi `ModalClosed` cho tới khi được; không bao giờ quay lại lượt.
- **If tải lại trang / tắt app giữa hoạt cảnh**: thú đã nằm trong Inventory; lần sau không phát lại hoạt cảnh và không cho quay lại (MVP chưa có save, nên mất cả ví lẫn thú như mọi trạng thái khác; từ VS ảnh chụp lưu chứa cả Inventory và bộ đếm Gacha).
- **If `Paused` khi đang trình bày**: modal, khóa tab, sân khấu và đồng hồ nút "Nhận" đứng yên; `Resumed` chạy tiếp từ đúng chỗ.
- **If Feedback ở `Failed`** hoặc bận/từ chối reveal: `RevealFinished(skipped = true)` đến ngay trong `RequestReveal`, trước khi Gacha có handle; máy đã ở `Revealing` với `awaitingHandle` = true nên chấp nhận, hiện thẻ, sang `CardShown`; luồng không treo.
- **If `RevealFinished`/`RevealSettleStarted` đến với handle khác handle đang chờ, ngoài lời gọi `RequestReveal`**: bỏ qua.
- **If bấm Back khi modal bóc trứng mở**: HUD nuốt phím (`dismissible = false`); chỉ nút "Nhận" đóng modal.
- **If Tutorial (VS) gọi `RequestTab` khi đang trình bày**: Bootstrap trả `Locked` (Gacha giữ khóa tab).
- **If Prestige (`ResetForNewShop`)**: n và giá quay về gốc (Economy); `lifetimeSpins`, `spinsSinceLegendary`, `firstSpinGuaranteeUsed` giữ nguyên.
- **If `TryGrantFreeSpin` khi máy không ở `Idle`** hoặc chưa `Interactive`: trả false, không xếp hàng.
- **If `copies` của thú đã ở INT_MAX**: Inventory kẹp và vẫn `Added`; thẻ hiện "×2 147 483 647", không lỗi.
- **If dòng % từng thú cộng không ra đúng % phẩm cấp** (làm tròn độc lập): chấp nhận; dòng phẩm cấp là số chuẩn và được ghi rõ "đã làm tròn".

## Dependencies

| System | Direction | Hard or soft | Interface (data in / out, owner) |
|---|---|---|---|
| Economy Core | Gacha phụ thuộc Economy | Hard | In: `TryPaySpin(playerAction)` → `bool`, `GetSpinPrice()`, `CanAfford(coin, amount)`, `SpinPriceChanged { spinsThisShop, spinPrice, baseCoinValue }`. Owner: Economy (đường giá neo `spin_price`, n, D1/D6). Gacha không định nghĩa giá. |
| Pet Catalog | Gacha phụ thuộc Catalog | Hard | In: Gacha pool theo phẩm cấp (R9.2), ID phẩm cấp `common/rare/epic/legendary`, vai, sprite. Out (dữ liệu tham chiếu): `GachaConfig.rarityRates` thay `PetBalanceTargets.assumedRarityRates`. Owner: Catalog (pool); Gacha (tỷ lệ, trọng số trong phẩm cấp, xử lý pool rỗng). |
| Inventory | Gacha phụ thuộc Inventory | Hard | Out (Gacha gọi): `AddPet(petId, copiesPerSpin)` → `Added { isNew, copies }` / `Rejected`; đọc `IsOwned`. In: `PetAcquired` (Gacha không cần nghe, dùng giá trị trả về). Owner: Inventory (ghi, sự kiện); Gacha (khi nào gọi, luật trùng). |
| Bootstrap & Scene Flow | Gacha phụ thuộc Bootstrap | Hard | Out: bước `gacha.machine` pha `GameplayInit`, `RunsAfter` rỗng. In: `Interactive`, `Paused`/`Resumed`, `AcquireTabLock`/`Release`. Owner: Bootstrap (cơ chế); Gacha (chính sách: khóa tab suốt modal bóc trứng). |
| HUD & Tab Navigation | Gacha phụ thuộc HUD | Hard (popup) | In: `ModalHost.Open(ModalRequest)` → `ModalHandle`/`Rejected`, `ModalHost.Close`, `ModalClosed`, `PetCardView(petId)`. Out: nội dung modal `gacha.reveal` và `gacha.rates`. Owner: HUD (khung, scrim, token chặn input, Back); Gacha (nội dung, khi nào mở/đóng, xử lý `Rejected`). |
| Feedback & Juice | Gacha phụ thuộc Feedback (một chiều; Feedback không phụ thuộc Gacha) | Hard | Out: `GachaRolled`, lời gọi `RequestReveal(petId, rarity, canSkip)`, `SkipReveal()`. In: `RevealHandle` (giá trị trả về của `RequestReveal`), `RevealSettleStarted { handle }`, `RevealFinished { handle, petId, rarity, skipped }` (Sync: Feedback thêm sự kiện và trường `handle`). Owner: Feedback (sân khấu, `RevealProfileTable`, cue, haptic); Gacha (kết quả, thẻ, giữ luồng, nút tua). |
| Audio | Gián tiếp qua Feedback | Soft | Cue `Gacha.Crank`, `Gacha.EggPop`, `Gacha.RarityReveal.<phẩm cấp>` do Feedback gọi; Gacha không tham chiếu Audio. |
| Plinko Board | Không gọi nhau | — | Cùng tab Bàn Chơi; chạm tay bị chặn bằng token của modal (HUD/Bootstrap), mô phỏng không dừng (D5). |
| Star Progression (VS) | Star phụ thuộc Gacha | Soft (VS) | Out: `GachaRolled` (`isNew`, `copies`). Owner: Star (công thức Sao). |
| Tutorial & Onboarding (VS) | Tutorial phụ thuộc Gacha | Soft (VS) | Out: `GachaRolled`, `TryGrantFreeSpin(source)`, trạng thái máy. Owner: Tutorial (kịch bản). |
| Save/Load (VS) | Gacha đăng ký dữ liệu | Soft (VS) | Out: `lifetimeSpins`, `spinsSinceLegendary`, `firstSpinGuaranteeUsed` (int, int, bool; không Dictionary). Owner: Save/Load (định dạng); Gacha (nội dung, kẹp khi nạp). |
| Ads / Monetization (Alpha), Prestige (Alpha) | Phụ thuộc Gacha | Soft | Out: `TryGrantFreeSpin(source)` (Ads); bộ đếm giữ qua Prestige. Không có API bán lượt quay bằng tiền thật. |

**Hai chiều**: Economy (Dependencies "Gacha Machine", R8), Inventory (Dependencies, Interactions), Pet Catalog (Dependencies, R9), Bootstrap (bảng phụ thuộc "Gacha Machine", AC-2), HUD (Dependencies "Gacha Machine", H9) và Feedback (R11, Dependencies; Feedback không phụ thuộc ngược) đã ghi Gacha với đúng các tên trên; các dòng đó còn nhãn *provisional / chưa có GDD* — bước Sync gỡ nhãn.

## Tuning Knobs

| Knob | Asset.field | Type | Current | Safe range | Too high | Too low | Interacts with |
|---|---|---|---|---|---|---|---|
| Tỷ lệ phẩm cấp | `GachaConfig.rarityRates` | float[4] | [0.55, 0.30, 0.12, 0.03] | mỗi phần 0.01–0.7, tổng = 1; Huyền Thoại 0.01–0.05 | Hiếm/Huyền Thoại rẻ, bộ sưu tập xong sớm | Tường gacha, `collectionSpinsBand` vượt 90 | `pityThreshold`, Catalog `collectionSpinsBand`, roster |
| Trọng số rơi mặc định | `GachaConfig.defaultDropWeight` | float | 1.0 | 0.25–4.0 (chỉ ý nghĩa tương đối) | — | — | `dropWeightOverrides` |
| Trọng số rơi riêng theo thú (khác `PetDefinition.weight`) | `GachaConfig.dropWeightOverrides` | list {petId, weight} | rỗng | 0.25–4.0; 0 = loại khỏi pool | Một thú át phẩm cấp | Thú gần như không ra | `pet_display_probability` |
| Ngưỡng bảo hiểm Huyền Thoại | `GachaConfig.pityThreshold` | int | 50 | 30–90 (0 = tắt, chỉ test) | Bảo hiểm vô nghĩa | Huyền Thoại quá dễ, phá trần sức mạnh | `rarityRates[legendary]`, `legendary_effective_rate` |
| Bật luật lượt đầu | `GachaConfig.firstSpinGuaranteeEnabled` | bool | true | true ở mọi bản phát hành | — | Lượt đầu có thể ra thú Hỗ trợ 0 CPS | `firstSpinRarity`, `firstSpinRole` |
| Phẩm cấp lượt đầu | `GachaConfig.firstSpinRarity` | enum | `common` | `common` | Lượt đầu "quá may", lệch kỳ vọng | — | Catalog pool |
| Vai lượt đầu | `GachaConfig.firstSpinRole` | enum | `Idle` | `Idle` | — | — | Shelf (CPS đầu tiên) |
| Lượt mở khóa nút tua | `GachaConfig.skipUnlockSpinIndex` | int | 2 | 1–5 | Người quen phải xem lại nhiều lần | Người mới bỏ qua khoảnh khắc đầu | Feedback `revealSkipSettleSeconds` |
| Trễ nút "Nhận" | `GachaConfig.claimButtonDelaySeconds` | float (s) | 0.3 | 0–0.6 | Cảm giác bị giữ lại | Chạm xuyên đóng thẻ trước khi kịp nhìn | Feedback `settleSeconds` |
| Số bản mỗi lượt | `GachaConfig.copiesPerSpin` | int | 1 | 1 (MVP, Gacha D3) | Sao tăng quá nhanh (VS) | — (1 là sàn) | Inventory `AddPet.amount`, Star Progression |
| Chữ số thập phân của % | `GachaConfig.ratePercentDecimals` | int | 1 | 0–2 | Bảng rối | 9.2 % hiện "9 %", lệch tổng | `pet_display_probability` |
| Thời lượng lắc khi thiếu xu | `GachaConfig.deniedShakeSeconds` | float (s) | 0.25 | 0.1–0.4 | Rườm rà | Không thấy | HUD `spendFlashSeconds` |
| Biên độ lắc | `GachaConfig.deniedShakeDp` | float (dp) | 6 | 2–10 | Giật mạnh, trái tông cozy | Không thấy | — |
| Thu nhỏ khi bấm nút vặn | `GachaConfig.pressScale` | float | 0.94 | 0.90–0.98 | Không cảm giác bấm | Nút "lún" quá sâu | `pressTweenSeconds` |
| Thời lượng tween bấm | `GachaConfig.pressTweenSeconds` | float (s) | 0.08 | 0.05–0.15 | Ì | Không thấy | — |
| Cỡ mẫu test phân phối | `GachaTestTargets.sampleSize` | int | 100 000 | 50 000–1 000 000 | Test chậm | Nhiễu thống kê | `rateTolerance` |
| Dung sai tỷ lệ trong test | `GachaTestTargets.rateTolerance` | float | 0.005 | 0.003–0.01 | Lọt lỗi phân phối | Test chập chờn khi đổi seed | `sampleSize` |

**Knob lắc/nhấn (`deniedShake*`, `press*`) giữ trong `GachaConfig`**: hiệu ứng riêng của nút máy, Feedback không sở hữu (chỉ có `busyShake*` của bàn Plinko); biên độ nhân `motion_scale` của Feedback khi ReduceMotion.

**Tham chiếu, không phải knob của Gacha:** giá quay `EconomyConfig.spinBasePrice` (100), `spinGrowth` (1.05), `priceNiceSigDigits` (2) thuộc Economy Core; thời lượng sân khấu (`revealChargeSeconds`, `RevealProfileTable[r].*`, `revealMaxSeconds` 4.0, `revealSkipSettleSeconds` 0.25) thuộc Feedback & Juice; `modalStackMax`, `modalOpenSeconds`, `scrimAlpha` thuộc HUD. **Hằng:** 4 phẩm cấp và thứ tự `common → legendary` là ID của Catalog (không phải cân bằng); INT_MAX là giới hạn kiểu của Inventory.

## Visual/Audio Requirements

| Event | Visual feedback | Audio feedback | Priority |
|---|---|---|---|
| Bấm nút vặn (đủ xu) | Nút nhún `pressScale` trong `pressTweenSeconds`; tay quay máy xoay một vòng | Không cue riêng (`Gacha.Crank` phát ở pha `Charge` của Feedback) | MVP |
| Bấm khi thiếu xu / `PaymentRejected` | Nút lắc ngang `deniedShakeDp` trong `deniedShakeSeconds`; nhãn giá nháy một lần | Không (thiếu xu); `Ui.Rejected` qua Feedback khi Economy phát `SpendRejected` | MVP |
| `GachaRolled` → modal bóc trứng | Scrim (HUD) + sân khấu `Charge → Burst → Settle` (Feedback, `RevealProfile` theo phẩm cấp, không lộ màu trước `Burst`) | `Gacha.Crank` → `Gacha.EggPop` + `Gacha.RarityReveal.<phẩm cấp>` (Feedback map) | MVP |
| Thẻ thú mới | `PetCardView` + nhãn "MỚI!" màu phẩm cấp (art bible) | Thuộc `RarityReveal` | MVP |
| Thẻ thú trùng | `PetCardView` + "×`copies` — dùng để lên Sao (sắp có)" | Không cue thêm | MVP |
| Lượt bảo hiểm | Nhãn "Bảo hiểm" nhỏ trên thẻ; bộ đếm máy về `0/N` | Không cue thêm | MVP |
| Bộ đếm `x/N` tăng | Số nảy nhẹ; khi x = N − 1 viền bộ đếm chuyển màu vàng nhạt kèm "Lượt tới chắc chắn Huyền Thoại" | Không | MVP |
| `SpinPriceChanged` | Giá trên nút đổi bằng tween ngắn (Economy V/A) | Không | MVP |

Tua không cắt cue đang phát; `RarityReveal.<phẩm cấp>` vẫn phát đúng một lần (Feedback R11) — trả lời câu hỏi mở của Audio.

## Game Feel

- **Feel reference:** tay quay máy gashapon thật (Bandai) — hai nấc "cạch" chắc tay rồi quả trứng rơi; nhịp bóc trứng ngắn của Pokémon GO (rung → bung sáng → thẻ). **Anti-reference:** loot box lộ màu phẩm cấp trước khi mở hoặc giả "suýt trúng" (Genshin Impact sao băng đổi màu): bị cấm bởi Feedback R11.
- Input responsiveness:

| Action | Max input-to-response (ms) | Frames at target fps | Notes |
|---|---|---|---|
| Bấm nút vặn → nút nhún | 17 | 1 | Tween bắt đầu cùng khung |
| Bấm nút vặn → scrim bắt đầu hiện | 100 | 6 | G4 đồng bộ; mở modal kéo dài `modalOpenSeconds` (HUD) |
| Bấm khi thiếu xu → bắt đầu lắc | 17 | 1 | — |
| Bấm "Tua" → vào `Settle` | 33 | 2 | Còn lại `revealSkipSettleSeconds` |
| Bấm "Nhận" → modal bắt đầu đóng, token nhả | 17 | 1 | Chạm Plinko được ngay khung sau |

- Animation feel targets:

| Animation | Startup | Active | Recovery frames | Feel goal |
|---|---|---|---|---|
| Nút vặn nhún | 0 | `pressTweenSeconds` (≈ 5 khung) | ≈ 5 | Bấm "chắc", như nút nhựa |
| Lắc khi thiếu xu | 0 | `deniedShakeSeconds` (≈ 15 khung) | 0 | Nhẹ, không trách |
| Sân khấu bóc trứng | 0 | Feedback `reveal_timeline` (Thường 1.6 s → Huyền Thoại 3.2 s, ≤ 4.0 s) | — | Hồi hộp có trần |
| Nút "Nhận" xuất hiện | `claimButtonDelaySeconds` | fade 0.15 s (Feedback/HUD tween chuẩn) | 0 | Kịp nhìn thẻ |

- Impact moments: haptic theo phẩm cấp ở `Burst` (`RevealProfile.hapticMs`, Feedback). Không hit-stop, không đổi time scale: bàn Plinko phía sau vẫn chạy (D5).
- Weight and responsiveness profile: nút vặn nhẹ và tức thì; khoảnh khắc trứng chậm có kiểm soát; luôn có lối thoát nhanh (tua từ lượt 2).

## UI Requirements

| Information | Display location | Update frequency | Condition |
|---|---|---|---|
| Giá lượt quay (chuỗi Economy) | Trên nút vặn, tab Bàn Chơi | Mỗi `SpinPriceChanged` | Luôn hiện từ `Waiting` |
| Trạng thái nút: Sẵn sàng / Mờ (thiếu xu) / Bận / Bảo trì | Nút vặn | Mỗi `WalletChanged`, đổi trạng thái máy | Mờ khi `CanAfford` = false; Bận khi Presenting/PendingPresentation; Bảo trì khi `Failed` |
| Bộ đếm bảo hiểm `x/N` | Mặt máy, cạnh nút vặn | Mỗi `GachaRolled` | "—" khi pool Huyền Thoại rỗng hoặc N = 0 |
| Nút "i" bảng tỷ lệ | Góc máy | Tĩnh | Từ `Waiting`; mở modal `gacha.rates` |
| Bảng tỷ lệ: % phẩm cấp, % từng thú, dòng bảo hiểm + ≈ % thực tế, dòng lượt đầu | Modal `gacha.rates` (`dismissible`) | Tính lúc mở | Ẩn dòng phẩm cấp rỗng; ghi "đã làm tròn" |
| Sân khấu trứng, thẻ thú, "MỚI!" / "×copies — dùng để lên Sao (sắp có)", nhãn "Bảo hiểm" | Modal `gacha.reveal` | Mỗi lượt | Gacha bật thẻ khi `RevealSettleStarted` khớp (hoặc `RevealFinished` nếu đến trước) |
| Nút "Tua" | Đáy modal `gacha.reveal` | — | `canSkip` và đang `Revealing` |
| Nút "Nhận" (cùng vị trí nút Tua) | Đáy modal `gacha.reveal` | — | `CardShown`, sau `claimButtonDelaySeconds` |

Kích thước nút theo `navMinTouchDp` của HUD. UX flag: máy trứng, modal bóc trứng và bảng tỷ lệ cần board Penpot (dùng chung thẻ thú với Kho/Kệ) trước khi làm epic.

## Cross-References

| This document references | Target GDD | Element referenced | Nature |
|---|---|---|---|
| Giá quay, thu tiền | `economy-core.md` | R8, `spin_price`, `TryPaySpin`, `CanAfford`, `SpinPriceChanged`, `spinBasePrice` | Gacha gọi; Economy sở hữu đường neo |
| Lượt miễn phí không tăng n | `economy-core.md` | R8.4, AC-35, Open Questions (Assumed) | Ràng buộc G5 |
| Gacha pool, ID phẩm cấp, vai | `pet-catalog.md` | R9.2, R9.3, Edge Case pool rỗng | Dữ liệu vào |
| Tỷ lệ thay giả định | `pet-catalog.md` | `PetBalanceTargets.assumedRarityRates`, `collection_spins_estimate` | Catalog tham chiếu `GachaConfig.rarityRates` (Sync) |
| Ghi kết quả | `inventory.md` | R2 `AddPet`, R5 `PetAcquired`, R4.4 | Hợp đồng ghi |
| Bước khởi động, khóa tab | `bootstrap-and-scene-flow.md` | B3 `GameplayInit`, `AcquireTabLock`, AC-2 | Gacha quyết định chính sách |
| Modal, thẻ thú | `hud-and-tab-navigation.md` | H9 `ModalHost`, `ModalClosed`, `PetCardView`, `modalStackMax` | Gacha cấp nội dung |
| Sân khấu bóc trứng | `feedback-and-juice.md` | R11 `RequestReveal` / `SkipReveal` / `RevealFinished`, `reveal_timeline`, `RevealProfileTable` | Gacha giữ luồng |
| Cue Gacha | `audio.md` | `Gacha.Crank`, `Gacha.EggPop`, `Gacha.RarityReveal.<phẩm cấp>` | Qua Feedback |
| Pillar, Anti-Pillar | `game-concept.md` | Pillar 2, "NOT bán gacha bằng tiền thật" | Ràng buộc thiết kế |

## Acceptance Criteria

Ký hiệu: `GC.` = `GachaConfig`, `GT.` = `GachaTestTargets`. Test EditMode dùng Economy giả (ví, `TryPaySpin`, `CanAfford`, n), Catalog giả với 7 thú MVP, Inventory thật trên Catalog giả, `ModalHost` giả (có thể trả `Rejected`), Feedback giả (`RevealFinished` điều khiển tay), Bootstrap giả và RNG seed cố định. Assembly test: `CozyGachapon.Tests.EditMode`.

- `AC-1 [EditMode]` **GIVEN** Bootstrap giả, **WHEN** đăng ký bước, **THEN** `Id` = `gacha.machine`, `Phase` = `GameplayInit`, `RunsAfter` rỗng, `IsCritical` = true, và `Run()` gọi `AddPet` 0 lần.
- `AC-2 [EditMode]` `effective_rarity_rates` — **GIVEN** `GC` mặc định, **WHEN** 4 pool khác rỗng / pool `legendary` rỗng / mọi pool rỗng, **THEN** 0.55, 0.30, 0.12, 0.03 / 0.5670, 0.3093, 0.1237, 0 (± 1e-4) / máy `Failed`.
- `AC-3 [EditMode]` `rarity_roll` — **GIVEN** `rate_eff` mặc định, **WHEN** u = 0, 0.5499, 0.5501, 0.62, 0.9699, 0.9701, 0.9999999, **THEN** `common`, `common`, `rare`, `rare`, `epic`, `legendary`, `legendary`, và `cum[legendary]` == 1.0 chính xác; **GIVEN** pool `legendary` rỗng, **WHEN** u = 0.9999999, **THEN** `epic` và `cum[epic]` == 1.0.
- `AC-4 [EditMode]` **GIVEN** `GC.pityThreshold` = 0, luật lượt đầu tắt, seed cố định, **WHEN** quay `GT.sampleSize` lượt, **THEN** tần suất mỗi phẩm cấp lệch `rate_eff` ≤ `GT.rateTolerance`.
- `AC-5 [EditMode]` `pet_pick` — **GIVEN** pool `rare` MVP trọng số đều, **WHEN** u₂ = 0.49 / 0.5, **THEN** `pet.bouncy_bunny` / `pet.lucky_cat`; **GIVEN** override `pet.lucky_cat` = 3.0, **WHEN** quay `GT.sampleSize` lượt `rare`, **THEN** tỷ lệ Mèo Chiêu Tài = 0.75 ± `GT.rateTolerance`; override 0 → không bao giờ ra.
- `AC-6 [EditMode]` **GIVEN** Xu = 50, giá 100 (`CanAfford` = false), **WHEN** bấm nút vặn, **THEN** `TryPaySpin` được gọi 0 lần, 0 `GachaRolled`, 0 `SpendRejected`, máy `Idle`, nút phát hiệu ứng lắc.
- `AC-7 [EditMode]` **GIVEN** Xu = 150, n = 0, Inventory rỗng, **WHEN** `TrySpin`, **THEN** trả `Rolled`, `TryPaySpin` 1 lần, `AddPet(petId, 1)` 1 lần, `GachaRolled` 1 lần với `isNew` = true, `copies` = 1, `wasFree` = false; thứ tự ghi nhận: `TryPaySpin` → `AddPet` → `PetAcquired` → `GachaRolled` → `ModalHost.Open` → `RequestReveal`.
- `AC-8 [EditMode]` **GIVEN** Economy giả trả `TryPaySpin` = false, **WHEN** `TrySpin`, **THEN** `PaymentRejected`, 0 `AddPet`, 0 `GachaRolled`, 0 `Open`, `lifetimeSpins` và `spinsSinceLegendary` không đổi.
- `AC-9 [EditMode]` **GIVEN** một lượt vừa `Rolled`, Feedback giả chưa phát `RevealFinished`, **WHEN** đọc Inventory rồi hủy modal và đối tượng trình bày, **THEN** `GetCopies(petId)` đã tăng 1 trước hoạt cảnh và vẫn giữ sau khi hủy; không API nào của Gacha cho quay lại lượt đó.
- `AC-10 [EditMode]` G6 — **GIVEN** game mới, luật bật, **WHEN** lượt đầu với 1 000 seed khác nhau, **THEN** 100 % kết quả là `pet.woody_duck` hoặc `pet.dreamy_frog`, mỗi con 0.5 ± 0.05, Xu bị trừ đúng giá; lượt thứ hai dùng `rarity_roll`; sau Prestige giả luật không áp dụng lại.
- `AC-11 [EditMode]` **GIVEN** Catalog giả không có thú Thường vai Idle, **WHEN** validator chạy và quay lượt đầu, **THEN** đúng 1 cảnh báo, lượt quay bình thường, `firstSpinGuaranteeUsed` = true, dòng lượt đầu không có trong bảng tỷ lệ.
- `AC-12 [EditMode]` `pity_forced` — **GIVEN** N = 50, RNG giả luôn trả u = 0 (Thường), luật lượt đầu tắt, **WHEN** quay 50 lượt, **THEN** lượt 1–49 ra Thường với `spinsSincePity` 1…49, lượt 50 ra `legendary`, `wasPity` = true, `spinsSincePity` = 0.
- `AC-13 [EditMode]` **GIVEN** x = 12, **WHEN** u = 0.98 (Huyền Thoại tự nhiên), **THEN** `wasPity` = false, x' = 0.
- `AC-14 [EditMode]` **GIVEN** pool `legendary` rỗng, x = 20, **WHEN** quay 100 lượt, **THEN** x vẫn 20, không lượt nào bị ép, bộ đếm hiển thị "—".
- `AC-15 [EditMode]` `legendary_effective_rate` — **GIVEN** p = 0.03, **WHEN** N = 50 / 30 / 90 / 0, **THEN** E_N = 26.06 / 19.97 / 31.18 / — và rate = 3.84 % / 5.01 % / 3.21 % / 3.00 % (± 0.01); **GIVEN** N = 50, **WHEN** mô phỏng `GT.sampleSize` lượt với luật lượt đầu tắt, **THEN** tỷ lệ Huyền Thoại = 0.0384 ± `GT.rateTolerance`.
- `AC-16 [EditMode]` G9 — **GIVEN** `pet.woody_duck` có 1 bản, Xu = 1 000, **WHEN** một lượt ra `pet.woody_duck`, **THEN** `isNew` = false, `copies` = 2, nội dung thẻ chứa chuỗi "dùng để lên Sao (sắp có)", Xu giảm đúng giá và không có `Add` hoàn xu.
- `AC-17 [EditMode]` G8 — **GIVEN** một lượt `Rolled`, **WHEN** trong `Presenting` gọi `RequestTab(Shelf)`, **THEN** `Locked`; `ModalRequest.dismissible` = false; sau "Nhận" và `ModalClosed`, khóa tab đã nhả và `RequestTab(Shelf)` → `Changed`.
- `AC-18 [EditMode]` **GIVEN** `lifetimeSpins` trước lượt = 0 rồi = 1, `GC.skipUnlockSpinIndex` = 2, **WHEN** quay, **THEN** `RequestReveal.canSkip` = false rồi true; `SkipReveal()` chỉ được gọi khi `canSkip`.
- `AC-19 [EditMode]` **GIVEN** `RevealFinished` vừa đến, **WHEN** bấm "Nhận" tại t = 0.29 s và t = 0.31 s (`claimButtonDelaySeconds` = 0.3), **THEN** lần đầu không làm gì, lần hai gọi `ModalHost.Close` đúng 1 lần và máy về `Idle`; đồng hồ đứng yên trong `Paused`.
- `AC-20 [EditMode]` **GIVEN** máy `Idle`, Xu đủ hai lượt, **WHEN** `TrySpin` hai lần trong cùng khung, **THEN** lần hai trả `Busy`, `TryPaySpin` 1 lần; `TryGrantFreeSpin` khi `Presenting` → false.
- `AC-21 [EditMode]` **GIVEN** `ModalHost` giả trả `Rejected(StackFull)`, **WHEN** `TrySpin`, **THEN** `AddPet` và `GachaRolled` đã xảy ra, máy ở `PendingPresentation`, khóa tab giữ; **WHEN** phát `ModalClosed` của modal khác và `Open` thành công, **THEN** `RequestReveal` được gọi đúng 1 lần.
- `AC-22 [EditMode]` G5 — **GIVEN** n = 5, Xu = 0, **WHEN** `TryGrantFreeSpin("test")`, **THEN** true, `TryPaySpin` 0 lần, n = 5, Xu = 0, `GachaRolled.wasFree` = true, `lifetimeSpins` + 1, x tăng theo `pity_forced`.
- `AC-23 [EditMode]` `pet_display_probability` — **GIVEN** pool MVP / v1.0, `GC` mặc định, **WHEN** dựng bảng tỷ lệ, **THEN** phẩm cấp 55.0 / 30.0 / 12.0 / 3.0 %; từng thú MVP 27.5 / 15.0 / 6.0 / 3.0 %, v1.0 9.2 / 10.0 / 4.0 / 3.0 %; có dòng bảo hiểm "50" kèm "≈ 3.8 %" và dòng lượt đầu; **GIVEN** pool `legendary` rỗng, **THEN** 56.7 / 30.9 / 12.4 %, không dòng Huyền Thoại, không dòng bảo hiểm.
- `AC-24 [EditMode]` **GIVEN** `lifetimeSpins` = 40, x = 17, `firstSpinGuaranteeUsed` = true, **WHEN** Economy giả `ResetForNewShop`, **THEN** ba giá trị không đổi và giá trên nút về `spinBasePrice` sau `SpinPriceChanged`.
- `AC-25 [EditMode]` **GIVEN** `GC.rarityRates` = [0, 0, 0, 0] hoặc Catalog giả không có thú nào trong pool, **WHEN** `Run()`, **THEN** bước báo lỗi, máy `Failed`, `TrySpin` → `Unavailable` và `TryPaySpin` 0 lần; validator báo lỗi.
- `AC-26 [EditMode]` validator — **GIVEN** `rarityRates` tổng 1.01 / phần −0.1 / override ID lạ / override trùng ID, **WHEN** validator chạy, **THEN** lỗi / lỗi / cảnh báo / cảnh báo; lúc chạy với tổng 1.01 tỷ lệ được chuẩn hóa (tổng `rate_eff` = 1 ± 1e-9).
- `AC-27 [EditMode]` **GIVEN** Feedback giả ở `Failed` (phát `RevealFinished(skipped = true)` bên trong `RequestReveal`, trước khi trả handle), **WHEN** `TrySpin`, **THEN** thứ tự ghi nhận trong cùng lời gọi: máy vào `Revealing` → `RequestReveal` bắt đầu → `RevealFinished` → thẻ hiện, máy `CardShown` → `RequestReveal` trả về; máy không treo ở `Revealing`; **GIVEN** `Revealing` với handle h đã trả về, **WHEN** `RevealFinished`/`RevealSettleStarted` có handle ≠ h, **THEN** bỏ qua, máy vẫn `Revealing`, thẻ vẫn ẩn.
- `AC-33 [EditMode]` **GIVEN** `Revealing` với handle h, **WHEN** Feedback giả phát `RevealSettleStarted { h }`, **THEN** thẻ hiện, máy vẫn `Revealing`, nút "Nhận" chưa hiện; **WHEN** sau đó phát `RevealFinished { h }`, **THEN** `CardShown` và thẻ không bật lại lần hai.
- `AC-28 [EditMode]` **GIVEN** assembly runtime của Gacha, **WHEN** liệt kê bằng reflection mọi phương thức public tạo ra thú, **THEN** chỉ có `TrySpin` và `TryGrantFreeSpin`, không phương thức nào nhận tham số sản phẩm IAP/tiền thật (Anti-Pillar).
- `AC-29 [Play mode]` **GIVEN** scene Bàn Chơi, Xu đủ, 10 xu Plinko đang rơi, auto-drop bật (giả), **WHEN** `InputTestFixture` bấm nút vặn rồi chạm bàn Plinko trong lúc reveal, **THEN** scrim bắt đầu hiện trong ≤ 100 ms, chạm bàn không sinh `CoinSpawned` origin Manual, các xu đang rơi vẫn phát `CoinResolved` và Xu Idle vẫn tăng.
- `AC-30 [Screenshot]` **GIVEN** tab Bàn Chơi ở ba trạng thái (đủ xu, thiếu xu, x = 49 với N = 50), **WHEN** chụp Game view, **THEN** thấy giá trên nút, nút mờ ở trạng thái thiếu xu, bộ đếm "49/50" viền vàng nhạt kèm "Lượt tới chắc chắn Huyền Thoại", nút "i".
- `AC-31 [Screenshot]` **GIVEN** reveal ép phẩm cấp `epic` thú mới và `common` thú trùng 3 bản, **WHEN** chụp ở `CardShown`, **THEN** thẻ đầu có "MỚI!", thẻ sau có "×3 — dùng để lên Sao (sắp có)"; ảnh chụp ở pha `Charge` không có màu phẩm cấp nào.
- `AC-32 [Perf]` **GIVEN** bản build WebGL và Android tầm trung, 24 xu trên bàn, reveal `legendary`, **WHEN** đo trong suốt reveal, **THEN** ≤ 100 draw call, FPS trung bình ≥ 60 và không khung nào > 33 ms, GC alloc 0 B mỗi khung sau khung mở modal.

## Open Questions

| Question | Owner | Target date |
|---|---|---|
| Assumed: `GachaConfig.pityThreshold` = 50 (≈ 2 × kỳ vọng 33 lượt ở 3 %; tỷ lệ thực tế 3.84 %, giữ `collectionSpinsBand` [40, 90]) — chốt bằng mô phỏng cùng Economy/Shelf. | game-designer | 2026-11-04 |
| Assumed: sự kiện bóc trứng theo phương án (A) của Feedback (FB D3): Gacha chỉ phát `GachaRolled`, gọi `RequestReveal`/`SkipReveal`, nghe `RevealSettleStarted`/`RevealFinished` (chọn sự kiện Settle để giữ nhịp `settleSeconds`); bỏ `GachaRevealStarted/Skipped/Finished` của brief (một chủ, không trùng). | game-designer + Feedback GDD | 2026-10-28 |
| Assumed: dùng `PetCardView` của HUD cho thẻ kết quả, Gacha chỉ thêm dòng "MỚI!"/số bản/"Bảo hiểm"; xử lý `Rejected(StackFull)` bằng `PendingPresentation` (trả lời hai câu hỏi mở của HUD). | game-designer + HUD GDD | 2026-11-11 |
| Assumed: modal bóc trứng `dismissible = false` suốt đời modal, Back bị nuốt, chỉ "Nhận" đóng (đơn giản, khớp HUD H9.4). | game-designer | 2026-11-04 |
| Assumed: bấm khi thiếu xu không phát cue (tránh sự kiện mới cho Feedback); chỉ lắc nút. Feedback có thể map sau nếu playtest thấy thiếu. | game-designer + Feedback | 2026-11-04 |
| Assumed: `IsCritical = true` cho `gacha.machine` (game không có máy trứng là hỏng vòng lặp chính). | game-designer + Bootstrap | 2026-10-28 |
| Assumed: lượt miễn phí cũng tính luật lượt đầu, bảo hiểm và `lifetimeSpins` (một luật cho mọi lượt, bảng tỷ lệ không cần ngoại lệ). | game-designer | 2026-11-04 |
| Assumed: không hoàn xu khi `AddPet` bị từ chối vì nhánh này không đạt được (G4.1); nếu QA tìm được đường tới nó, cần lý do `GachaRefund` trong bảng `reason` của Economy. | game-designer + Economy | 2026-11-11 |
| Assumed: dòng lượt đầu luôn hiện trong bảng tỷ lệ (kể cả sau khi đã dùng) để công khai trọn vẹn; nhãn "Bảo hiểm" hiện trên thẻ lượt bị ép. | game-designer | 2026-11-04 |
| Assumed: đổi tên trọng số thành `dropWeight` (tránh nhầm `PetDefinition.weight`); knob lắc/nhấn giữ ở `GachaConfig` vì Feedback không có knob tương đương cho nút máy. | game-designer | 2026-11-04 |
| Theo dõi playtest: tỷ lệ lượt trùng trong 15 phút đầu khi chưa có Sao (Gacha D3); thú Hỗ trợ 0 CPS khi đứng một mình ở các lượt sớm (cần Shelf & Synergy xác nhận). | game-designer + Shelf GDD | 2026-11-11 |

Cần Sync cập nhật (không sửa ở đây): `pet-catalog.md` — `assumedRarityRates` → tham chiếu `GachaConfig.rarityRates`, câu hỏi pool rỗng đã đóng; `feedback-and-juice.md`, `hud-and-tab-navigation.md`, `bootstrap-and-scene-flow.md`, `economy-core.md`, `inventory.md`, `audio.md` — gỡ "(chưa có)/provisional" ở dòng Gacha; `entities.yaml` — thêm `gacha_rarity_rates`, `gacha_pity_threshold` và các công thức trên; `systems-index.md` — Gacha Machine → trạng thái mới.
