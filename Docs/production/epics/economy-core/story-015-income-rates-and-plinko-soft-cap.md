# Story 015: Published Income Rates & Plinko Soft Cap

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R5.5, R7.2–R7.3, R15 `IncomeRatesChanged`, Formulas `plinko_rate_avg`, `plinko_rate_model`, Game Feel "Đổi kệ → IncomeRatesChanged")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: tốc độ công bố cho HUD được làm mới theo nhịp giới hạn, không cấp phát; không kẹp thu nhập Plinko ngoài trần tầng và trần cộng phẳng.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: bộ đệm cấp sẵn, công bố giới hạn nhịp; số `double`.
- Forbidden: không kẹp, không gộp bớt, không bỏ khoản Plinko vì thông lượng cao (R5.5); không static singleton.
- Guardrails: 0 B GC mỗi khung; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`IncomeRatesChangedEvent.cs`, mở rộng `IIncomePipeline.cs`: `PlinkoRateAvg`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`PlinkoRateWindow.cs`, `IncomeRatesPublisher.cs`, mở rộng `EconomyCore.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/PlinkoRateAvgTests.cs`, `IncomeRatesPublishTests.cs`, `PlinkoSoftCapTests.cs`
- Data: `EconomyConfig.plinkoRateWindowSeconds`, `ratesPublishInterval`; `EconomyBalanceTargets.plinkoRawRateCeilingAssumed`, `plinkoSoftCapTestFactor`, `valueRelTolerance`

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-25 [EditMode]` **GIVEN** `C.plinkoRateWindowSeconds` = 10, **WHEN** cộng 100 xu Plinko mỗi giây trong 10 s; hoặc 300 trong 3 s đầu; hoặc 50 trong 0.4 s đầu; hoặc ngừng cộng thêm 10 s, **THEN** `PlinkoRateAvg` lần lượt = 100, 100, 50, 0.
- [x] `AC-26 [EditMode]` **GIVEN** modifier đổi mỗi khung trong 10 s, **WHEN** đếm `IncomeRatesChanged`, **THEN** số sự kiện ≤ ceil(10 / `C.ratesPublishInterval`) + 1, và giá trị mới xuất hiện trong ≤ `C.ratesPublishInterval` sau mỗi thay đổi.
- [x] `AC-47 [EditMode]` (trần mềm, R5.5) **GIVEN** game mới (n = 0, `base_coin_value` = 1), không modifier, Plinko giả phát mỗi bước dt = 1/60 s một `CoinResolved` (`Bucket`) với `rawValue` = `T.plinkoSoftCapTestFactor` × `T.plinkoRawRateCeilingAssumed[3]` / 60 (tổng 1.5 × 42 = 63 xu thô/s, cao hơn mọi cấu hình mô hình), **WHEN** chạy `C.plinkoRateWindowSeconds` (10 s) mô phỏng, **THEN** số dư tăng đúng Σ `rawValue` (630) trong sai số `T.valueRelTolerance`, số `IncomeCredited` bằng số `CoinResolved` (600) và mỗi `creditedAmount` = `rawValue` (không khoản nào bị kẹp hay bỏ), và `PlinkoRateAvg` = 63.

## Implementation Notes

- `plinko_rate_avg = Σ credit (Plinko, trong plinkoRateWindowSeconds gần nhất) / clamp(running_time, rate_bin, plinkoRateWindowSeconds)`, `rate_bin` = 1 s là hằng số kỹ thuật (độ phân giải bộ đệm vòng, 1 ô/giây). `PlinkoRateWindow` là bộ đệm vòng `double[]` cấp sẵn đủ `ceil(plinkoRateWindowSeconds)` ô khi dựng, ghi `creditedAmount` của mỗi khoản Plinko (kể cả `SubmitIncome`) vào ô hiện tại, xoay ô theo thời gian mô phỏng của `Tick`. `running_time` = thời gian Running tích lũy từ đầu phiên (Σ dt đã áp ở Running), nên phiên ngắn chia cho thời gian đã chạy, không báo thấp (Assumed "PlinkoRateAvg chia cho thời gian đã chạy"): 300 trong 3 s → 100; 50 trong 0.4 s → 50 vì mẫu số kẹp tối thiểu `rate_bin`.
- Chỉ kênh Plinko; `IdleCps` và `PlinkoRateAvg` công bố **riêng**, không cộng gộp (R7.3, Pillar 3). Kênh Idle không đưa vào cửa sổ này.
- `IncomeRatesChanged { idleCps, plinkoRateAvg }` phát không dày hơn một lần mỗi `ratesPublishInterval` (tích lũy dt trong `Tick`). Khi có thay đổi (modifier, nguồn, credit) giá trị mới phải hiện trong ≤ một chu kỳ: dùng cờ "đã đổi" và phát ở lần `Tick` đầu tiên khi đủ khoảng cách kể từ lần phát trước, không phát khi không đổi. AC-26: modifier đổi mỗi khung trong 10 s với interval 0.5 → ≤ ceil(10/0.5) + 1 = 21 sự kiện.
- Trần mềm (R5.5, AC-47): Economy không kẹp, không gộp bớt, không bỏ khoản Plinko nào vì thông lượng cao; giới hạn duy nhất là trần tầng và `plinkoFlatPerCoinCap` đã có. `plinko_rate_model(k, n) = plinkoRawRateCeilingAssumed[k] × base_coin_value(n)` là công thức dùng để mô phỏng và cân bằng (Story 016 dùng), không dùng để kẹp thu nhập. AC-47 đọc `T.plinkoRawRateCeilingAssumed[3]` (42) và `T.plinkoSoftCapTestFactor` (1.5) từ `EconomyBalanceTargets` (Story 001), không literal trong test.
- AC-47 chạy 600 bước dt = 1/60 nên cần Economy ở Running (Story 013) để `running_time` tăng; mỗi bước gọi `Handle(CoinResolved)` rồi `Tick(dt)`.
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng (tách `PlinkoRateWindow`, `IncomeRatesPublisher`), SRP, không static singleton, không cấp phát (struct sự kiện, bộ đệm cấp sẵn).

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 011], [Story 012]: sinh `IncomeCredited`/credit từ `CoinResolved` và `SubmitIncome` (story này chỉ đọc kết quả).
- [Story 013]: `Tick`, trạng thái Running.
- [Story 016]: kiểm mô hình cân bằng dùng `plinko_rate_model` và `EconomyBalanceTargets`.
- Mô phỏng bảng tính trần mềm kênh Plinko (Open Questions của GDD): thuộc game-designer, ngoài epic.
- HUD: cách hiển thị hai tốc độ.

## QA Test Cases

- **AC-25**: trung bình trượt
  - Given: `plinkoRateWindowSeconds` = 10, Economy Running
  - When: (a) 100 xu Plinko mỗi giây trong 10 s; (b) 300 xu trong 3 s đầu; (c) 50 xu trong 0.4 s đầu; (d) ngừng cộng thêm 10 s
  - Then: `PlinkoRateAvg` = 100, 100, 50, 0
  - Edge cases: bộ đệm xoay hết vòng không rò dữ liệu cũ; credit 0 không đổi tốc độ
- **AC-26**: giới hạn nhịp công bố
  - Given: `ratesPublishInterval` = 0.5, modifier đổi mỗi khung trong 10 s
  - When: đếm `IncomeRatesChanged`, ghi thời điểm đổi và thời điểm sự kiện
  - Then: số sự kiện ≤ 21; mỗi thay đổi hiện trong ≤ 0.5 s
  - Edge cases: không đổi gì thì không phát; thay đổi ngay sau lần phát vẫn được phát ở chu kỳ kế
- **AC-47**: trần mềm không kẹp
  - Given: game mới, không modifier, `rawValue` mỗi bước = 1.5 × 42 / 60 = 1.05
  - When: 600 bước dt = 1/60 (10 s), mỗi bước một `CoinResolved(Bucket)`
  - Then: số dư tăng 630 (± `valueRelTolerance`), 600 `IncomeCredited` với `creditedAmount` = `rawValue`, `PlinkoRateAvg` = 63
  - Edge cases: không khoản nào bị bỏ hoặc kẹp; phép chia không làm tròn sai ở bước cuối cửa sổ

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/PlinkoRateAvgTests.cs`, `IncomeRatesPublishTests.cs`, `PlinkoSoftCapTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] Verified (`PlinkoRateAvgTests.cs`, `IncomeRatesPublishTests.cs`, `PlinkoSoftCapTests.cs` — 1002/1002 EditMode tests PASS, `unity-harness rules` PASS, 2026-10-27)

## Dependencies

- Depends on: Story 011, Story 012, Story 013
- Unlocks: Story 017, Story 018
