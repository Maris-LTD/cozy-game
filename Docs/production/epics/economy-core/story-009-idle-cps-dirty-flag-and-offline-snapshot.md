# Story 009: Idle CPS Computation, Dirty Flag & Offline Snapshot

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 2 days
> **Last Updated**: 2026-10-26
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R6.1, R7.1, R7.4, R9.3, R11.1, Formulas `idle_cps`, `offline_rate_snapshot`, Edge Cases "kệ trống", "Giá và tiến độ")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: CPS tính từ gốc cộng phẳng rồi các tầng nhân có trần; chỉ tính lại khi nguồn hoặc modifier đổi; giá không phụ thuộc CPS.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: tính CPS qua pipeline phân tầng; số `double`.
- Forbidden: không tính lại mỗi khung; không để giá phụ thuộc đội hình kệ; không static singleton.
- Guardrails: tính CPS < 1 microsecond, 0 B GC; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`IdleCpsCalculator.cs`, `IdleSourceRegistry.cs`, `PipelineDirtyTracker.cs`, `OfflineRateCalculator.cs`); `Assets/CozyGachapon/Scripts/Contracts/Economy/` (mở rộng `IIncomePipeline.cs`: `IdleCps`, `OfflineRateSnapshot`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/IdleCpsTests.cs`, `IdleRecomputeCounterTests.cs`, `OfflineRateSnapshotTests.cs`, `PriceIndependenceTests.cs`
- Data: `EconomyConfig.idleCpsUnit` (Story 001)

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-20 [EditMode]` **GIVEN** `C.idleCpsUnit` = 2, nguồn A gốc 10 với Synergy nhắm A +0.5, nguồn B gốc 4 với cộng phẳng 1 đơn vị, Synergy kênh Idle +0.5, Trà +1.0, **WHEN** tính, **THEN** `idle_cps` = 58.
- [x] `AC-24 [EditMode]` **GIVEN** bộ đếm số lần tính lại `idle_cps`, **WHEN** chạy 600 khung không đăng ký/gỡ gì, rồi đăng ký 3 modifier, **THEN** bộ đếm tăng 0 trong 600 khung đầu và ≤ 3 sau đó.
- [x] `AC-27 [EditMode]` **GIVEN** cấu hình của AC-20 (có Trà), **WHEN** đọc `OfflineRateSnapshot`, **THEN** = 29; gỡ Trà → bằng `idle_cps`; luôn ≤ `idle_cps`.
- [x] `AC-32 [EditMode]` **GIVEN** n = 30 và các giá đã tính, **WHEN** đăng ký/gỡ nguồn Idle làm `idle_cps` tăng 10 lần rồi giảm về 0, **THEN** `spin_price` và mọi `derived_price` không đổi (D2, chống khai thác tháo kệ).

## Implementation Notes

- `RegisterIncomeSource(sourceKey, baseRate)` đăng ký một nguồn Idle (R6.1) và `Unregister` gỡ; đăng ký lại cùng khóa thay bản cũ. Công thức `idle_cps = [ Σ_s∈Idle (base_rate_s + flat_units_s × idleCpsUnit) × M_Synergy(s) ] × M_Boost(Idle) × M_Prestige(Idle) × M_Global(Idle)`. `flat_units_s` = Σ sanitize(v) modifier tầng Base nhắm s. Ví dụ AC-20: A = 10 × 2.0 = 20; B = (4 + 2) × 1.5 = 9; tổng 29; Trà +1.0 → 58.
- Cờ bẩn (`PipelineDirtyTracker`): chỉ tính lại `idle_cps` khi có đăng ký/gỡ nguồn hoặc modifier (R6.1), không tính lại mỗi khung. Bộ đếm `IdleRecomputeCount` (đọc được để test, thuộc phần diagnostics counters) tăng đúng một lần mỗi lần tính lại thật. AC-24: 600 khung gọi truy vấn `IdleCps` mà không đổi gì → bộ đếm không tăng; sau đăng ký 3 modifier liên tiếp, bộ đếm tăng tối đa 3 (cho phép gộp thành ít hơn nếu tính lười).
- `OfflineRateSnapshot` = `idle_cps` tính lại với mọi modifier `isTemporary` bị bỏ (toàn bộ tầng Boost); luôn ≤ `idle_cps`; tính theo yêu cầu (không đặt cờ bẩn của `idle_cps`, không đổi bộ đếm tính lại chính). Hệ số offline và trần giờ thuộc Offline Earnings (ngoài epic).
- Giá không phụ thuộc CPS (D2, R9.3): `spin_price` và `derived_price` chỉ đọc n; test AC-32 dùng n = 30 nạp qua `Initialize` của Story 004 và `DerivedPriceCalculator` của Story 003, đăng ký các nguồn làm `idle_cps` tăng 10 lần rồi gỡ, so sánh giá trước/sau. Không có tham chiếu từ module giá sang module Idle (kiểm bằng cấu trúc code: module giá không nhận `IIncomePipeline`).
- `idleCpsUnit` là hằng số trong suốt một tiệm, không co giãn theo n (quyết định (A), R11.2): không có code nào nhân `idleCpsUnit` với hàm của n.
- Kệ trống: `idle_cps` = 0, không lỗi, Plinko vẫn hoạt động (Edge Case).
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng, SRP, không static singleton, `IdleCps` đọc không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 008]: kiểm quyền vào tầng khi đăng ký (story này dùng cổng đăng ký của Story 007, chưa kiểm tiền tố).
- [Story 010]: `GetSourceIdleCps`, `PreviewIdleCps`.
- [Story 013]: cộng Idle mỗi khung (`idle_accrual`), trạng thái Running/Ready/Paused.
- [Story 015]: `PlinkoRateAvg`, `IncomeRatesChanged`.
- AC-23 (Play mode): xem Deferred acceptance criteria trong EPIC.

## QA Test Cases

- **AC-20**: `idle_cps` tổng
  - Given: `idleCpsUnit` = 2; nguồn A gốc 10 + Synergy nhắm A +0.5; nguồn B gốc 4 + cộng phẳng 1 đơn vị; Synergy kênh Idle +0.5; Trà +1.0 (Boost, Both)
  - When: đọc `IdleCps`
  - Then: 58
  - Edge cases: kệ trống = 0; modifier NaN không đổi kết quả
- **AC-24**: tính lại theo cờ bẩn
  - Given: cấu hình đã ổn định, `IdleRecomputeCount` ghi nhận
  - When: 600 lần đọc `IdleCps` không đổi gì; sau đó đăng ký 3 modifier
  - Then: 0 lần tính lại trong 600 khung đầu; ≤ 3 sau đó
  - Edge cases: gỡ một nguồn cũng đặt cờ bẩn; đọc `OfflineRateSnapshot` không tăng bộ đếm chính
- **AC-27**: snapshot offline
  - Given: cấu hình AC-20 (có Trà)
  - When: đọc `OfflineRateSnapshot`, gỡ Trà, đọc lại
  - Then: 29; sau khi gỡ bằng `idle_cps`; luôn ≤ `idle_cps`
  - Edge cases: không có modifier tạm thời thì bằng `idle_cps`; modifier tạm thời ở tầng khác Boost vẫn bị bỏ khi `isTemporary` = true
- **AC-32**: giá bất biến theo đội hình
  - Given: n = 30, các giá đã tính
  - When: đăng ký nguồn Idle làm `idle_cps` tăng 10 lần rồi gỡ về 0
  - Then: `spin_price` và mọi `derived_price` bằng giá trước đó ở mọi bước
  - Edge cases: đổi modifier Boost không đổi giá

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/IdleCpsTests.cs`, `IdleRecomputeCounterTests.cs`, `OfflineRateSnapshotTests.cs`, `PriceIndependenceTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] Assets/CozyGachapon/Tests/EditMode/Economy/IdleCpsTests.cs, IdleRecomputeCounterTests.cs, OfflineRateSnapshotTests.cs, PriceIndependenceTests.cs passing in unity-harness tests (CozyGachapon.Tests.EditMode 954/954)

## Completion Notes

**Completed**: 2026-10-26
**Criteria**: 4/4 passing
**Harness**: unity-harness all PASS; rules PASS; compile PASS; tests CozyGachapon.Tests.EditMode 954/954
**Run result**: N/A - Logic story with no scene or UI surface
**Deviations**: None
**Test Evidence**: Assets/CozyGachapon/Tests/EditMode/Economy/IdleCpsTests.cs, IdleRecomputeCounterTests.cs, OfflineRateSnapshotTests.cs, PriceIndependenceTests.cs passing
**Visual sign-off**: n/a (not Visual/Feel or UI)
**Code Review**: unity-reviewer: 0 blockers, APPROVE. LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none

- `idle_cps` tính theo cờ bẩn: chỉ tính lại khi đăng ký/gỡ nguồn hoặc modifier.
- `OfflineRateSnapshot` tính theo yêu cầu, loại bỏ toàn bộ modifier `isTemporary == true` và tầng Boost, không ảnh hưởng cờ bẩn hay bộ đếm tính lại chính.
- `idleCpsUnit` là hằng số đọc từ `EconomyConfig`, không co giãn theo số lần quay n.

## Dependencies

- Depends on: Story 003, Story 004, Story 007
- Unlocks: Story 008, Story 010, Story 013
