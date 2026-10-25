# Story 004: Exhaustion Policies (Fixed / RecycleOldest)

> **Epic**: Object Pooling
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1 day
> **Last Updated**: 2026-10-25
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/object-pooling.md` (R7 Chính sách khi cạn, State "Recycling", Formulas `recycle_victim`, R6.3 `staleRelease`)
**Requirement**: `TR-pooling-002`

**ADR Governing Implementation**: ADR-0003: Zero-Allocation Object Pooling (kèm ADR-0001)
**ADR Decision Summary**: `Fixed` trả phiếu rỗng khi cạn; `RecycleOldest` lấy lại phiếu cũ nhất, gọi reset của chủ cũ rồi giao cho yêu cầu mới; phiếu cũ bị vô hiệu.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: lease có kiểm tra số thứ tự; hook trả về chạy trước hook lấy ra khi tái chế.
- Forbidden: không `Instantiate`/`Destroy` ngoài `Prewarm` (không có chính sách Grow trong MVP); không static singleton.
- Guardrails: 0 B GC mỗi khung, kể cả đường tái chế và Get rỗng sau cảnh báo đầu.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none (thuần C# trên đối tượng đã prewarm).

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Pooling/`: `FixedExhaustionPolicy.cs`, `RecycleOldestPolicy.cs`, `IExhaustionStrategy.cs` (nội bộ Foundation), cập nhật `ObjectPoolService.cs`/`PoolGetHandler.cs`; `PoolReturnReason.Recycled` ở `Assets/CozyGachapon/Scripts/Contracts/`
- Test: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolExhaustionTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `PoolDefinition.policy` trong `PoolingConfig`; không knob mới

## Acceptance Criteria

*From Docs/design/gdd/object-pooling.md, scoped to this story:*

- [x] `AC-16 [EditMode]` **GIVEN** pool Fixed có `capacity` = `C` và `C` đối tượng đang mượn, **WHEN** Get, **THEN** trả rỗng, `exhausted` +1, `created` vẫn = `C`, lần cạn đầu ghi đúng một cảnh báo Editor và các lần cạn sau trong phiên không ghi thêm.
- [x] `AC-17 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = `C` đã mượn đủ với seq 1…`C`, **WHEN** Get hai lần, **THEN** lần 1 trả đối tượng seq 1 và lần 2 trả đối tượng seq 2; với mỗi lần, hook trả về (lý do `Recycled`) của chủ cũ được gọi trước hook lấy ra của chủ mới; `recycled` +2; `created` vẫn = `C`.
- [x] `AC-18 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = `C` mượn đủ, rồi trả đối tượng seq 3 và mượn lại (seq `C`+1), **WHEN** Get ba lần, **THEN** victim lần lượt là seq 1, 2, 4.
- [x] `AC-19 [EditMode]` **GIVEN** pool RecycleOldest `capacity` = 1 đang mượn, **WHEN** Get 3 lần, **THEN** cả ba trả cùng một đối tượng với seq tăng dần, `recycled` +3.
- [x] `AC-20 [EditMode]` **GIVEN** đối tượng A bị tái chế cho chủ mới, **WHEN** chủ cũ Release bằng phiếu cũ của A, **THEN** bị bỏ qua, `staleRelease` +1, A vẫn đang mượn và vẫn hiển thị cho chủ mới.

## Implementation Notes

- Namespace `CozyGachapon.Pooling` (hoặc con) cho mọi kiểu Pooling (GDD AC-32). Chọn chính sách theo `PoolDefinition.policy` lúc đăng ký (Story 001).
- Fixed: Get khi không còn Free trả `PoolLease` rỗng, không tạo đối tượng, `exhausted` +1; cờ "đã cảnh báo cạn" theo pool và theo phiên: chỉ lần cạn đầu báo một cảnh báo qua `IPoolDiagnosticsSink` (cảnh báo, không phải lỗi); các lần sau chỉ tăng bộ đếm (giữ không cấp phát).
- RecycleOldest: `recycle_victim` = đối tượng InUse có `acquire_seq` nhỏ nhất. Trình tự trong một lời gọi: chọn victim → hook trả về (`Recycled`) của chủ cũ → vô hiệu phiếu cũ (cập nhật `Sequence` hiện hành của đối tượng) → giao cho yêu cầu mới như Get thường (hook lấy ra, `acquire_seq` mới) → `recycled` +1. `inUse`/`created` không đổi.
- Chọn victim không cấp phát và không LINQ: duy trì cấu trúc cấp trước (vòng tròn/hàng đợi theo `acquire_seq`, hoặc quét mảng cố định ≤ `capacity`); Release giữa chừng (AC-18) phải loại đối tượng khỏi thứ tự victim rồi đối tượng mượn lại có seq mới ở cuối. `acquire_seq` kiểu `long`, tăng 1 mỗi Get thành công kể cả qua tái chế; duy nhất trong pool.
- Phiếu stale: Release với `Sequence` không khớp `Sequence` hiện hành của đối tượng (đối tượng vẫn InUse cho chủ khác) → bỏ qua, `staleRelease` +1, một lỗi qua sink (như `doubleRelease`), đối tượng giữ nguyên trạng thái hiện/đang mượn. Phân biệt với `doubleRelease` (đối tượng đang Free) ở `PoolLeaseValidator` của Story 003.
- Pool `forbid_recycle` không bao giờ là RecycleOldest — đã chặn ở đăng ký (Story 001); không có nhánh Grow.
- Không thời gian: thứ tự chỉ dựa trên `acquire_seq` (Story 005 kiểm AC-23).
- Dự án: không comment, không `Debug.Log*` (cảnh báo/lỗi qua `IPoolDiagnosticsSink`, test đọc sink), không LINQ, file ≤ 250 dòng.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 003]: Get/Release thường, `doubleRelease`, `foreignRelease`.
- [Story 005]: `lost` (Destroy từ ngoài), `created` không đổi qua nhiều chu kỳ, bất biến không cấp phát, phép đo GC.
- AC-27 (Feedback đẩy số bay vượt dung lượng với pool thật), AC-34 (cô lập `feedback.coinFizzle` khỏi `plinko.coin`) — cần Feedback & Juice.
- AC-26 (Plinko Saturated không cạn) — cần Plinko Board.

## QA Test Cases

- **AC-16**: Fixed cạn
  - Given: pool Fixed `capacity` = `C`, mượn đủ `C`
  - When: Get nhiều lần
  - Then: mỗi Get trả rỗng; `exhausted` +1 mỗi lần; `created` = `C`; sink nhận đúng 1 cảnh báo ở lần cạn đầu và không thêm sau đó
  - Edge cases: trả một đối tượng rồi Get lại thành công, sau đó cạn lần nữa trong cùng phiên → vẫn không thêm cảnh báo, `exhausted` tiếp tục tăng
- **AC-17**: tái chế hai lần
  - Given: pool RecycleOldest `capacity` = `C` mượn đủ (seq 1…`C`), hook giả ghi thứ tự lời gọi `(Return, Recycled)` và `(Acquire)`
  - When: Get hai lần
  - Then: lần 1 trả đối tượng seq 1, lần 2 seq 2; mỗi lần hook trả về của chủ cũ chạy trước hook lấy ra của chủ mới; `recycled` +2; `created` = `C`
  - Edge cases: `inUse` giữ = `C` suốt quá trình
- **AC-18**: tái chế sau khi trả giữa chừng
  - Given: pool mượn đủ `C`; trả đối tượng seq 3; mượn lại (nhận seq `C`+1)
  - When: Get ba lần
  - Then: victim lần lượt seq 1, 2, 4
  - Edge cases: đối tượng seq 3 cũ không bị chọn làm victim sau khi đã trả
- **AC-19**: dung lượng 1
  - Given: pool RecycleOldest `capacity` = 1, đang mượn
  - When: Get 3 lần
  - Then: cả ba trả cùng một đối tượng với seq tăng dần; `recycled` +3
  - Edge cases: không exception, `created` = 1
- **AC-20**: phiếu cũ sau tái chế
  - Given: đối tượng A bị tái chế cho chủ mới (chủ cũ còn giữ phiếu cũ)
  - When: chủ cũ Release bằng phiếu cũ
  - Then: bị bỏ qua; `staleRelease` +1; A vẫn đang mượn và vẫn hiển thị cho chủ mới; hook trả về không gọi thêm
  - Edge cases: chủ mới Release bằng phiếu mới vẫn thành công

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolExhaustionTests.cs`, passing in `unity-harness tests`

**Evidence**: [x] `PoolExhaustionTests.cs`, `PoolRecycleTests.cs` (EditMode 232/232)

## Dependencies

- Depends on: Story 003
- Unlocks: Story 005

## Completion Notes
**Completed**: 2026-10-25
**Criteria**: 5/5 passing (AC-16..AC-20 covered by EditMode tests)
**Harness**: unity-harness all PASS (rules, compile, EditMode 232/232); smoke PASS in workflow run (0 console errors)
**Run result**: N/A - pure pooling logic, no pool registered in MainGame yet
**Deviations**: ADVISORY - first Fixed exhaustion is reported as a sink WARNING per the story/GDD (ADR-0003 text says error); policies behind internal `IExhaustionStrategy` (`FixedExhaustionPolicy`, `RecycleOldestPolicy`) with shared `ExhaustionReporter`; stale release after recycle reuses the Story 003 stale path
**Test Evidence**: `Assets/CozyGachapon/Tests/EditMode/Pooling/PoolExhaustionTests.cs`, `PoolRecycleTests.cs` (+ `RecordedHookCall.cs`), passing
**Visual sign-off**: n/a (Logic)
**Code Review**: unity-dev review, 1 minor (duplicated count-and-warn) fixed via `ExhaustionReporter`; workflow "needs-attention" only for the 3 pre-existing Unity noise files; LP-CODE-REVIEW skipped (lean)
**Untested criteria**: none
