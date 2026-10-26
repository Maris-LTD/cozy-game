# Story 010: Per-Source Idle CPS & Preview Functions

> **Epic**: Economy Core
> **Status**: Complete
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-26
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R7.5, Formulas `preview_idle_cps`, Dependencies hàng Shelf & Synergy, Assumed "revision C5[13]")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: CPS tính qua pipeline phân tầng có trần; hai hàm thuần chỉ đọc cho Shelf dùng cùng công thức và cùng trần.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: tính qua pipeline phân tầng; số `double`; test được trong EditMode.
- Forbidden: hàm thuần không đổi ví/modifier/cờ bẩn, không phát sự kiện; không static singleton.
- Guardrails: không cấp phát khi gọi; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Contracts/Economy/` (`IIdlePlan.cs`, `IdlePlanSource.cs`, mở rộng `IIncomePipeline.cs`: `GetSourceIdleCps`, `PreviewIdleCps`); `Assets/CozyGachapon/Scripts/Core/Economy/` (`SourceIdleCpsCalculator.cs`, `IdlePreviewCalculator.cs`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/SourceIdleCpsTests.cs`, `PreviewIdleCpsTests.cs`, `FakeIdlePlan.cs`
- Data: dùng asset Story 001

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [x] `AC-50 [EditMode]` **GIVEN** cấu hình của AC-20 (`idle_cps` = 58) và nguồn `shelf.slot1.idle` = A, `shelf.slot2.idle` = B, **WHEN** gọi `GetSourceIdleCps` cho A, B và một khóa chưa đăng ký, rồi gỡ B, **THEN** = 40, 18, 0; sau khi gỡ B, A = 40 và Σ = `IdleCps`; mỗi lần Σ các giá trị = `IdleCps` trong sai số `T.valueRelTolerance`.
- [x] `AC-51 [EditMode]` **GIVEN** cấu hình của AC-20, bộ đếm ví/modifier/sự kiện và bộ đếm tính lại `idle_cps` (AC-24), **WHEN** gọi `PreviewIdleCps` với (a) plan = trạng thái hiện tại; (b) plan thêm nguồn C gốc 4 không modifier; (c) plan bỏ B; (d) plan có `baseRate` NaN hoặc modifier −0.5; ở Running, Ready và Paused, **THEN** (a) = 58; (b) = 58 + 4 × 2 (Trà) = 66; (c) = 40; (d) bằng kết quả khi giá trị đó coi là 0; sau mọi lời gọi `IdleCps`, số dư, danh sách modifier không đổi, 0 sự kiện, bộ đếm tính lại không tăng.

## Implementation Notes

- `GetSourceIdleCps(sourceKey)` = `source_idle_cps(s) = (base_rate_s + flat_units_s × idleCpsUnit) × M_Synergy(s) × M_Boost(Idle) × M_Prestige(Idle) × M_Global(Idle)`, 0 nếu khóa chưa đăng ký (gồm khóa lạ và nguồn đã gỡ). Σ mọi `GetSourceIdleCps` = `IdleCps` trong sai số `T.valueRelTolerance`. Đọc theo trạng thái hiện tại, không cache lệch: đọc được cùng khung sau khi Shelf đăng ký/gỡ. Dùng lại `ModifierBook` và `IdleCpsCalculator` của Story 007 và 009, không viết công thức thứ hai.
- `PreviewIdleCps(IIdlePlan plan)`: tính `idle_cps` như thể `plan` thay toàn bộ nguồn và modifier có tiền tố `shelf.` hiện có; tầng Boost/Prestige/Global lấy từ trạng thái thật (cùng trần). Giá trị không hợp lệ của plan đi qua `sanitize` (baseRate NaN hoặc âm, modifier −0.5 coi là 0).
- Cấu trúc `plan` do Shelf & Synergy định nghĩa (Assumed C5[13]); Economy chỉ đọc. Vì Shelf chưa có, story này định nghĩa interface đọc `IIdlePlan` ở Contracts (đếm nguồn, lấy nguồn thứ i `{ sourceKey, baseRate, flatUnits }`, đếm modifier, lấy modifier thứ i dạng `IncomeModifier`) để đọc không cấp phát và không phụ thuộc kiểu cụ thể của Shelf; test dùng `FakeIdlePlan`. Shelf cài `IIdlePlan` khi tới lượt (Open Question của EPIC).
- Cả hai hàm thuần, chỉ đọc: không đổi ví, danh sách modifier, cờ bẩn, bộ đếm tính lại; không phát sự kiện; dùng được ở Ready, Running và Paused (kể cả Uninitialized trả 0 một cách an toàn). Tính trên dữ liệu cấp sẵn, không cấp phát ở cuộc gọi.
- AC-51 chạy ở ba trạng thái Running, Ready, Paused: dựng trạng thái bằng `EconomyCore` của Story 013 (hoặc bộ điều khiển trạng thái test của nó).
- Hai hàm không có knob mới; ngân sách CPU `ShelfBalanceTargets.previewMaxMs` do Shelf giữ (ngoài epic).
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng, SRP, không static singleton.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 009]: `idle_cps` tổng, cờ bẩn, `OfflineRateSnapshot`.
- [Story 013]: trạng thái Ready/Running/Paused (AC-51 cần để dựng ba trạng thái).
- Shelf & Synergy: cài `IIdlePlan` thật, quyết định cách hiển thị Δ và nhãn CPS ô.

## QA Test Cases

- **AC-50**: CPS theo nguồn
  - Given: cấu hình AC-20 với `shelf.slot1.idle` = A và `shelf.slot2.idle` = B, `idle_cps` = 58
  - When: `GetSourceIdleCps` cho A, B, khóa lạ; gỡ B; gọi lại
  - Then: 40, 18, 0; sau khi gỡ B, A = 40 và Σ = `IdleCps`; mọi lần Σ = `IdleCps` trong `valueRelTolerance`
  - Edge cases: đăng ký rồi đọc cùng khung; nguồn đăng ký lại cùng khóa
- **AC-51**: xem trước kế hoạch
  - Given: cấu hình AC-20; bộ đếm ví, modifier, sự kiện và tính lại; ba trạng thái Running, Ready, Paused
  - When: `PreviewIdleCps` với plan (a) hiện tại, (b) thêm C gốc 4, (c) bỏ B, (d) có `baseRate` NaN hoặc modifier −0.5
  - Then: 58; 66; 40; bằng kết quả coi giá trị xấu là 0; sau mọi lời gọi `IdleCps`, số dư, danh sách modifier không đổi, 0 sự kiện, bộ đếm tính lại không tăng
  - Edge cases: plan rỗng → 0; plan null → 0; modifier `shelf.` hiện có bị thay hoàn toàn bởi plan

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/SourceIdleCpsTests.cs`, `PreviewIdleCpsTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] Passed 991/991 tests in `unity-harness tests` (`SourceIdleCpsTests`, `PreviewIdleCpsTests`).

## Completion Notes

- **Review Gate**: `[LP-CODE-REVIEW] skipped (lean)`.
- **Harness Verification**: Rules PASS (63 files), Compile PASS, EditMode tests PASS (991/991).
- **Core Additions**:
  - `IdlePlanSource.cs` & `IIdlePlan.cs`: allocation-free plan contract for Shelf & Synergy.
  - `SourceIdleCpsCalculator.cs` & `IdlePreviewCalculator.cs`: pure, read-only calculators that evaluate per-source CPS and plan preview CPS without mutating dirty flags, recompute counters, or wallet state.
  - `IncomePipeline.cs` & `EconomyCore.cs`: exposed `GetSourceIdleCps` and `PreviewIdleCps`.

## Dependencies

- Depends on: Story 009, Story 013
- Unlocks: None
