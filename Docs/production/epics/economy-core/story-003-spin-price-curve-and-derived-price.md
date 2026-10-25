# Story 003: Spin Price Curve, nice_ceil & Derived Price

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R8 Đường giá neo, R9 Họ công thức giá, Formulas `spin_price`, `nice_ceil`, `derived_price`, `base_coin_value`)
**Requirement**: `TR-economy-004`, `TR-economy-003`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: giá quay tăng mũ theo số lượt quay (`100 * 1.05^n`), mọi giá khác là hệ số nhân của giá quay; `baseCoinValue = (giá quay / giá gốc)^alpha`.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: số `double`; tham số cân bằng trong ScriptableObject; công thức là C# thuần test được trong EditMode.
- Forbidden: không literal cân bằng trong logic; không `float` cho giá; không static singleton.
- Guardrails: 0 B GC; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: chỉ dùng `System.Math`.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`SpinPriceCurve.cs`, `NiceCeiling.cs`, `DerivedPriceCalculator.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/SpinPriceCurveTests.cs`, `NiceCeilingTests.cs`, `DerivedPriceTests.cs`
- Data: `EconomyConfig.spinBasePrice`, `spinGrowth`, `coinValueExponent`, `priceNiceSigDigits`, `walletSafeCap` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-28 [EditMode]` **GIVEN** `C` mặc định, **WHEN** tính `spin_price(n)` với n = 0, 1, 2, 3, 10, 20, 50, 100, 150, **THEN** = 100, 110, 120, 120, 170, 270, 1,200, 14,000, 160,000; và với n = 0…2000 dãy không giảm.
- [x] `AC-29 [EditMode]` **GIVEN** `nice_ceil`, **WHEN** đầu vào (105, 2), (1,146.74, 2), (7.2, 2), (100, 2), (100 × (1 + 1e-12), 2), (0, 2), (−5, 2), (13,150.1, 3), **THEN** = 110, 1,200, 8, 100, 100, 0, 0, 13,200.
- [x] `AC-31 [EditMode]` **GIVEN** coef 1.5, **WHEN** n = 0 và n = 50, **THEN** `derived_price` = 150 và 1,800; coef 2.0 tại n = 100 → 27,000; coef ∈ {0, −1, NaN} → trả về "không có giá".

## Implementation Notes

- `SpinPriceCurve`: `RawPrice(n) = spinBasePrice × spinGrowth^n` (double, giữ để Story 005 tính `baseCoinValue`), `Price(n) = NiceCeiling.Apply(RawPrice(n), priceNiceSigDigits)`. Khi `RawPrice` > `walletSafeCap`, giá kẹp tại `walletSafeCap` (Edge Case, n ≈ 14 060). `Math.Pow` với n int; n âm được coi là 0.
- `NiceCeiling.Apply(x, d)`: 0 nếu x ≤ 0 (kể cả NaN); ngược lại `ceil(x / step − nice_epsilon) × step`, `step = 10^max(floor(log10 x) − (d − 1), 0)`. `nice_epsilon` = 1e-9 là hằng số kỹ thuật trong code (GDD: không phải knob thiết kế), đặt tên `const` rõ nghĩa. Dưới 10^(d−1) chỉ làm tròn lên số nguyên.
- `DerivedPriceCalculator.TryGet(double coef, int n, out double price)`: coef ≤ 0, NaN, vô cực → trả `false` ("không có giá", R9.4), không bao giờ trả giá 0; ngược lại `price = NiceCeiling.Apply(coef × RawPrice(n))`. Giá không phụ thuộc CPS hay kệ (D2): hàm không nhận bất kỳ tham số nào ngoài coef và n. Kết quả "không có giá" biểu diễn bằng `bool` trả về, không dùng giá trị sentinel.
- Mọi giá trị mặc định lấy từ `EconomyConfig` (spinBasePrice 100, spinGrowth 1.05, priceNiceSigDigits 2); test dựng config bằng `SetXForTest`, không hardcode trong logic. Test AC-28 dùng giá trị kỳ vọng literal của GDD (giá trị kỳ vọng là dữ liệu test, không phải logic).
- Giá không bao giờ giảm khi n tăng; hai lượt kề nhau có thể cùng giá (n = 2 và 3 đều 120).
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng, SRP (ba lớp nhỏ), không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 004]: bộ đếm `spinsThisShop` và khởi tạo giá.
- [Story 005]: `base_coin_value`, `TryPaySpin`, `IBaseCoinValueSource`, `GetDerivedPrice` ở mức API dịch vụ (story này chỉ có hàm thuần).
- [Story 016]: `first_spin_time`, `seconds_per_spin`.
- Hệ số giá từng món (`priceCoef`): thuộc Boosts/Shop/Shelf, ngoài epic.

## QA Test Cases

- **AC-28**: bảng giá quay
  - Given: `EconomyConfig` mặc định
  - When: `Price(n)` cho n = 0, 1, 2, 3, 10, 20, 50, 100, 150; rồi lặp n = 0…2000
  - Then: 100, 110, 120, 120, 170, 270, 1,200, 14,000, 160,000; dãy không giảm
  - Edge cases: n âm coi là 0; n rất lớn kẹp tại `walletSafeCap` và vẫn không giảm
- **AC-29**: `nice_ceil`
  - Given: các cặp (x, d) của AC
  - When: gọi `NiceCeiling.Apply`
  - Then: 110, 1,200, 8, 100, 100, 0, 0, 13,200
  - Edge cases: NaN → 0; 100 × (1 + 1e-12) không nhảy thành 110
- **AC-31**: giá phái sinh
  - Given: n = 0, 50, 100
  - When: `TryGet(1.5, 0)`, `TryGet(1.5, 50)`, `TryGet(2.0, 100)`, `TryGet` với coef 0, −1, NaN
  - Then: 150, 1,800, 27,000; ba ca cuối trả `false`
  - Edge cases: coef +∞ trả `false`; không có ca nào trả giá 0 với `true`

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/SpinPriceCurveTests.cs`, `NiceCeilingTests.cs`, `DerivedPriceTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `SpinPriceCurveTests.cs`, `NiceCeilingTests.cs`, `DerivedPriceTests.cs`, `PriceMathZeroAllocTests.cs` (EditMode 768/768)

## Dependencies

- Depends on: Story 001
- Unlocks: Story 004, Story 005, Story 009, Story 016

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 3/3 passing (AC-28, AC-29, AC-31 + QA edge cases)
**Harness**: unity-harness all PASS (rules, compile, EditMode 768/768); smoke n/a (no scene change)
**Run result**: N/A - price math
**Deviations**: ADVISORY - `NiceCeiling`, `SpinPriceCurve`, `DerivedPriceCalculator` as three small allocation-free classes; `nice_epsilon` 1e-9 is a named technical const (GDD: not a design knob); price clamps at `walletSafeCap` for huge n; no asset change (all knobs already existed)
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/SpinPriceCurveTests.cs`, `NiceCeilingTests.cs`, `DerivedPriceTests.cs`, `PriceMathZeroAllocTests.cs`, passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 2 minor test-quality fixes (random invariant now allows nice_epsilon; tiny-coef test asserts the implication); LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
