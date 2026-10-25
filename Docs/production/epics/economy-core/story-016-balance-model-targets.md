# Story 016: Balance Model Targets (first spin, seconds per spin)

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (Formulas `first_spin_time`, `seconds_per_spin`, `plinko_rate_model`, Tuning Knobs "Mục tiêu cân bằng (chỉ test/mô phỏng)")
**Requirement**: `TR-economy-004`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: đường giá `100 * 1.05^n` và `baseCoinValue` tăng chậm hơn giữ nhịp quay hợp lý; mục tiêu cân bằng nằm ở asset test, không ở logic.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: tham số cân bằng trong ScriptableObject; test được trong EditMode; tự kiểm tra dữ liệu.
- Forbidden: `EconomyBalanceTargets` không ảnh hưởng gameplay (không tham chiếu từ runtime logic); không literal cân bằng trong logic.
- Guardrails: file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`EconomyBalanceModel.cs`, `EconomyBalanceTargetsValidator.cs`); `EconomyBalanceTargets.cs` đã tạo ở Story 001
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/FirstSpinTimeTests.cs`, `SecondsPerSpinTests.cs`
- Data: `Assets/CozyGachapon/Data/Economy/EconomyBalanceTargets.asset` (`realisticTapRate`, `plinkoEvEffectiveAssumed`, `plinkoRawRateCeilingAssumed`, `firstSpinMaxSeconds`, `spinCheckpoints`, `plinkoOnlySpinSecondsMax`, …)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-40 [EditMode]` **GIVEN** `C` và `T` mặc định, **WHEN** tính `first_spin_time`, **THEN** kết quả (13.8 s) ≤ `T.firstSpinMaxSeconds` (30).
- [ ] `AC-41 [EditMode]` **GIVEN** `T.spinCheckpoints`, **WHEN** tính `seconds_per_spin` tại từng mốc, **THEN** test in bảng (11.3 / 31.3 / 84.4 s) và mỗi giá trị ≤ phần tử tương ứng của `T.plinkoOnlySpinSecondsMax` (30 / 60 / 120).

## Implementation Notes

- `first_spin_time = max(0, spinBasePrice − startingCoins) / (realisticTapRate × plinkoEvEffectiveAssumed × base_coin_value(0)) + median_fall_time`; ví dụ mặc định 100 / (3 × 2.96 × 1) + 2.5 = 13.8 s.
- `seconds_per_spin(n) = spin_price(n) / (realisticTapRate × plinkoEvEffectiveAssumed × base_coin_value(n))`; mặc định n = 0, 50, 100 → 11.3, 31.3, 84.4 s. Đây là cận trên cho kịch bản chỉ chạm Plinko (chưa tính Idle, thú kênh Plinko, boost; `idleCpsUnit` hằng số theo quyết định (A)). Dùng lại `SpinPriceCurve` và `BaseCoinValueCalculator` của Story 003/004, không viết công thức thứ hai.
- `median_fall_time` do Plinko sở hữu (`PlinkoBalanceTargets.targetMedianFallTime`, 2.5 s) và Plinko Board chưa có: hàm `FirstSpinTime` nhận giá trị này làm tham số; test truyền 2.5 làm dữ liệu test và ghi vào Completion Notes. Khi Plinko epic xong thì test đọc từ asset của Plinko (Open Questions của EPIC).
- Test "in bảng": ghi ba giá trị `seconds_per_spin` ra kết quả test (qua `TestContext.WriteLine` hoặc thông điệp assert), không `Debug.Log*`. Mỗi giá trị so với phần tử tương ứng của `plinkoOnlySpinSecondsMax`.
- `EconomyBalanceTargetsValidator` (cùng mẫu `EconomyConfigValidator`) kiểm safe range của từng knob mục tiêu; lớp mô hình không được runtime nào tham chiếu (chỉ test và công cụ mô phỏng). Mô hình `plinko_rate_model` đã dùng ở Story 015; story này không thêm kiểm tra cho nó ngoài việc cung cấp hàm thuần `PlinkoRateModel.Compute(k, n)` đọc `plinkoRawRateCeilingAssumed[k] × base_coin_value(n)` nếu Story 015 chưa tạo.
- Quy tắc dự án: không comment, không `Debug.Log*`, không LINQ, file ≤ 250 dòng, SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 015]: trần mềm Plinko (AC-47) và tốc độ công bố.
- AC-42 (Play mode với Plinko thật): xem Deferred acceptance criteria trong EPIC.
- Mô phỏng bảng tính chung Economy + Pet Catalog + Shelf + Star (Open Questions của GDD): thuộc game-designer, ngoài epic.

## QA Test Cases

- **AC-40**: thời gian lượt quay đầu
  - Given: `EconomyConfig` và `EconomyBalanceTargets` mặc định, `median_fall_time` = 2.5 (dữ liệu test)
  - When: tính `first_spin_time`
  - Then: ≈ 13.8 s và ≤ `firstSpinMaxSeconds` (30)
  - Edge cases: `startingCoins` ≥ `spinBasePrice` thì tử số 0 (chỉ còn `median_fall_time`)
- **AC-41**: nhịp quay theo mốc
  - Given: `spinCheckpoints` = [0, 50, 100], `plinkoOnlySpinSecondsMax` = [30, 60, 120]
  - When: tính `seconds_per_spin` tại từng mốc
  - Then: 11.3 / 31.3 / 84.4 s (sai số hiển thị 0.1), in bảng, mỗi giá trị ≤ trần tương ứng
  - Edge cases: mốc n ngoài [0, 200] bị validator cảnh báo

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/FirstSpinTimeTests.cs`, `SecondsPerSpinTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003, Story 004
- Unlocks: None
