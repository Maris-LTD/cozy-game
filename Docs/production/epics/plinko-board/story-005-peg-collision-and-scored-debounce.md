# Story 005: Va chạm đinh & tính điểm có debounce (Peg Collision & Scored Debounce)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-004`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Va chạm xu với đinh phát sự kiện PegHit; kiểm soát debounce theo cặp xu-đinh và trần số lần tính điểm để ngăn lạm phát giá trị kinh tế.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Bảng debounce theo cặp xu-đinh và bộ đệm sự kiện được cấp phát sẵn theo `maxActiveCoins`, không cấp phát trong Update/Callback; cờ `scored` được tính chính xác.
- Forbidden: Tuyệt đối không gọi `GetComponent` trong physics collision callbacks.
- Guardrails: Va đinh phát `PegHit`; va thành tường phẳng không phát `PegHit`; nửa đinh gắn tường vẫn tính là đinh.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPegHitTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-20 [EditMode]**: GIVEN logic `peg_hit_scored`, WHEN xét: đã đạt `C.maxScoredPegHits` / lặp cùng đinh trong `< C.pegHitDebounce` / lặp cùng đinh sau `≥ C.pegHitDebounce` / đinh chưa chạm, THEN `scored` = false / false / true / true, và `PegHit` vẫn phát trong cả 4 trường hợp.
- [ ] **AC-21 [Play mode]**: GIVEN một xu rơi, WHEN xu va đinh rồi va tường, THEN mỗi va chạm xu-đinh phát đúng một `PegHit` kèm vị trí world; va tường không phát `PegHit`.

## Implementation Notes

- Cài đặt `PegHitScorer` quản lý lịch sử va chạm cho mỗi xu: lưu trữ `pegId` và timestamp va chạm gần nhất.
- Bảng tra cứu debounce sử dụng bộ đệm mảng cố định (fixed-size array hoặc pre-allocated circular buffer) gắn với mỗi xu trong pool để đảm bảo 0 B GC.
- Nhận diện đinh và nửa đinh gắn tường qua collider tag hoặc layer mà không dùng `GetComponent`.
- Kiểm tra điều kiện `scored`: `(scoredCount < maxScoredPegHits) && (chưa từng chạm pegId này || (thời gian - lần chạm trước >= pegHitDebounce))`.

## Out of Scope

- [Story 006]: Watchdog phát hiện xu kẹt.
- [Story 007]: Cảm biến hũ và tính toán trả thưởng.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPegHitTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004 must be Complete
- Unlocks: Story 006

