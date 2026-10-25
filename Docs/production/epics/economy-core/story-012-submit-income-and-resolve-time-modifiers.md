# Story 012: SubmitIncome, Resolve-Time Modifiers & Invalid Input

> **Epic**: Economy Core
> **Status**: Ready
> **Layer**: Foundation (systems index; code nằm ở assembly `CozyGachapon.Core`)
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: —
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/economy-core.md` (R5.2, R5.3, Formulas `plinko_credit` (`credit_submit`), Edge Cases "Trà hết hạn giữa lúc xu đang rơi", "`CoinResolved` có `rawValue` âm, NaN hoặc vô cực", "thú rời kệ")
**Requirement**: `TR-economy-002`

**ADR Governing Implementation**: ADR-0005: Layered Income Pipeline & Double Currency Format
**ADR Decision Summary**: mọi khoản thu đi qua pipeline phân tầng có trần; modifier áp theo trạng thái lúc giải quyết, không "dính" theo xu.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: thu nhập qua pipeline; số `double`; đầu vào xấu bị chặn trước khi chạm ví.
- Forbidden: không bypass ví; không static singleton.
- Guardrails: 0 B GC; file ≤ 250 dòng.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: không có API sau cutoff.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Economy/` (`SubmitIncomeHandler.cs`, mở rộng `PlinkoCreditHandler.cs`, `IncomeInputGuard.cs`); `Assets/CozyGachapon/Scripts/Contracts/Economy/` (mở rộng `IIncomePipeline.cs`: `SubmitIncome`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Economy/SubmitIncomeTests.cs`, `ResolveTimeModifierTests.cs`, `InvalidIncomeInputTests.cs`
- Data: dùng asset Story 001

## Acceptance Criteria

*From Docs/design/gdd/economy-core.md, scoped to this story:*

- [ ] `AC-16 [EditMode]` **GIVEN** xu A sinh trước khi Trà (+1.0, Both) được đăng ký và giải quyết khi Trà còn, xu B giải quyết sau khi Trà bị gỡ, cùng rawValue r, **WHEN** giải quyết, **THEN** A được 2r, B được r.
- [ ] `AC-19 [EditMode]` **GIVEN** Synergy nhắm `shelf.slot1` +0.5 và Trà +1.0, **WHEN** `SubmitIncome(Plinko, shelf.slot1, 10)`, **THEN** cộng 30; `SubmitIncome` với baseAmount ≤ 0 / NaN và `CoinResolved` với rawValue âm / NaN / vô cực bị bỏ qua, số dư không đổi.

## Implementation Notes

- `SubmitIncome(IncomeChannel channel, string sourceKey, double baseAmount)` (Shelf gọi cho thưởng sự kiện của thú kênh Plinko, R5.3): `credit_submit = baseAmount × M_Synergy(sourceKey) × M_Boost(Plinko) × M_Prestige(Plinko) × M_Global(Plinko)`; không có cộng phẳng mỗi xu. Phát đúng một `IncomeCredited` với `coinId` = none, `bucketIndex` = none; `origin` và `reason` cho khoản này GDD không định nghĩa (Open Question của EPIC), dùng giá trị trung tính của enum (ví dụ `CoinOrigin.None`, `CoinResolveReason.None`) và ghi vào Completion Notes. `baseAmount` không hữu hạn hoặc ≤ 0 → bỏ qua (không ví, không sự kiện). Kênh Idle của `SubmitIncome` không có trong GDD (Idle đi qua `RegisterIncomeSource`): `channel` khác Plinko trả `false`.
- Modifier được đánh giá **tại thời điểm giải quyết** (R5.2): handler đọc bảng modifier hiện tại ở lúc xử lý, không giữ ảnh chụp theo xu. AC-16: xu A (sinh trước khi bật Trà) giải quyết khi Trà còn → 2r; xu B giải quyết sau khi gỡ Trà → r. Đây là kiểm chứng rằng `CoinResolved` không mang modifier hay thời điểm sinh.
- `IncomeInputGuard` chung cho cả `CoinResolved` (rawValue âm, NaN, vô cực → bỏ qua, không cộng ví) và `SubmitIncome`; rawValue = 0 hợp lệ (xem Story 011). Báo sink `InvalidIncomeInput` khi bỏ qua, không `Debug.Log*`.
- Thú rời kệ ngay khi xu đang rơi: thưởng chưa gửi thì không tính, đã `SubmitIncome` thì giữ (Edge Case; hệ quả của việc Economy không giữ trạng thái chờ, không cần code riêng).
- Quy tắc dự án: không comment, không LINQ, file ≤ 250 dòng, SRP, không static singleton, không cấp phát.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 011]: `CoinResolved` → ví, công thức `plinko_credit`, mọi origin × reason.
- [Story 013]: trạng thái Ready/Paused cho `CoinResolved`/`SubmitIncome`.
- [Story 015]: `PlinkoRateAvg` và trần mềm (AC-47).
- Shelf & Synergy (epic khác): bên gọi thật của `SubmitIncome`.

## QA Test Cases

- **AC-16**: modifier tại thời điểm giải quyết
  - Given: xu A sinh trước khi Trà (+1.0, Both) được đăng ký; xu B sinh khi Trà còn nhưng giải quyết sau khi Trà bị gỡ; cùng rawValue r
  - When: giải quyết A khi Trà còn, giải quyết B sau khi gỡ
  - Then: A được 2r, B được r
  - Edge cases: đăng ký Trà giữa hai xu cùng khung; hai xu giải quyết cùng khung với trạng thái modifier khác nhau
- **AC-19**: `SubmitIncome` và đầu vào xấu
  - Given: Synergy nhắm `shelf.slot1` +0.5 và Trà +1.0, số dư đã biết
  - When: `SubmitIncome(Plinko, shelf.slot1, 10)`; rồi `SubmitIncome` với baseAmount 0, −1, NaN; `CoinResolved` với rawValue −1, NaN, +∞
  - Then: cộng 30; năm ca sau bị bỏ qua, số dư không đổi, không `IncomeCredited`
  - Edge cases: baseAmount +∞; `sourceKey` lạ vẫn nhận (M_Synergy = 1)

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Economy/SubmitIncomeTests.cs`, `ResolveTimeModifierTests.cs`, `InvalidIncomeInputTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 008, Story 011
- Unlocks: Story 015, Story 017
