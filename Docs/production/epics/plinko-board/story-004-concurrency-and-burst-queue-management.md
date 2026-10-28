# Story 004: Quản lý sức chứa & hàng đợi loạt (Concurrency & Burst Queue Management)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-002`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Giới hạn trần số xu hoạt động đồng thời (maxActiveCoins); hàng đợi loạt FIFO cho Rain và JackpotBurst luôn để dành slot cho chạm tay (manualReservedSlots); tràn hàng đợi trả ngay với lý do Overflow.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Bắt buộc tuân thủ trần xu hoạt động đồng thời; hàng đợi loạt FIFO với trần `burstQueueMax`.
- Forbidden: Không bao giờ để hàng đợi loạt hoặc auto-drop chiếm hết chỗ của người chơi chạm tay.
- Guardrails: Xu tràn hàng đợi phải được trả ngay (instant payout) không qua mô phỏng, không được để mất xu hay mất tiền thưởng của người chơi.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoQueueTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-9 [EditMode]**: GIVEN số xu hoạt động = `C.maxActiveCoins`, WHEN có cú chạm đã qua cooldown và một nhịp Auto, THEN cả hai bị bỏ; chỉ cú chạm phát `BoardBusy`; không có gì được xếp hàng; nhịp Auto không dồn sang nhịp sau.
- [ ] **AC-10 [EditMode]**: GIVEN hàng đợi loạt không rỗng, đã qua `C.burstSpawnInterval`, WHEN sim đủ lâu, THEN số xu hoạt động do hàng đợi và Auto sinh ra không bao giờ làm tổng vượt `C.maxActiveCoins - C.manualReservedSlots`; khi tổng = `C.maxActiveCoins - C.manualReservedSlots`, hàng đợi và nhịp Auto chờ.
- [ ] **AC-10b [EditMode]**: GIVEN `RequestBurst(C.maxActiveCoins + 1, Rain)` đang chạy (hàng đợi đã lấp tới `C.maxActiveCoins - C.manualReservedSlots`), WHEN người chơi chạm `C.manualReservedSlots` lần cách nhau ≥ `C.tapCooldown`, THEN cả `C.manualReservedSlots` cú chạm được nhận và không phát `BoardBusy`; cú chạm kế tiếp khi bàn đủ `C.maxActiveCoins` mới phát `BoardBusy`.
- [ ] **AC-11 [EditMode]**: GIVEN `RequestBurst(N)` với N > `C.burstQueueMax`, WHEN sim tới khi hàng đợi rỗng, THEN hai lần sinh liên tiếp cách nhau ≥ `C.burstSpawnInterval`, hàng đợi luôn ≤ `C.burstQueueMax`, phần dư nhận ngay `CoinResolved(reason = Overflow)`, tổng số `CoinResolved` = N.
- [ ] **AC-33 [EditMode]**: GIVEN Boosts giả gọi `RequestBurst(count, Rain)`, WHEN xử lý xong, THEN số xu sinh + số Overflow = count, mọi xu mang origin Rain; `count <= 0` bị bỏ qua, không sự kiện nào phát.

## Implementation Notes

- Cài đặt `PlinkoBurstQueue` (hàng đợi FIFO) lưu trữ các yêu cầu sinh xu từ `Rain` và `JackpotBurst`.
- Quản lý cấp phát `coinId` tuần tự duy nhất ngay khi xu được xếp hàng hoặc nhận vào bàn.
- Thiết lập quy tắc sinh theo thứ tự trong mỗi bước mô phỏng: Hàng đợi loạt sinh trước (nếu `< maxActiveCoins - manualReservedSlots`), sau đó Auto sinh, sau đó Manual sinh (nếu `< maxActiveCoins`).
- Khi xếp hàng vượt trần `burstQueueMax`, các phần tử vượt trần lập tức được hoàn trả tức thì thông qua `CoinResolved` với lý do `CoinResolveReason.Overflow` và multiplier = `instantPayoutMultiplier`.

## Out of Scope

- [Story 005]: Xử lý va chạm đinh.
- [Story 007]: Cảm biến hũ và tính toán trả thưởng.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoQueueTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 003 must be Complete
- Unlocks: Story 005

