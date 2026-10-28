# Story 006: Watchdog phát hiện xu kẹt & despawn (Stuck Watchdog & Despawn Timer)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-005`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Đồng xu bị kẹt (vận tốc thấp kéo dài hoặc quá tuổi thọ tối đa) được watchdog tự động despawn, trả thưởng an toàn và thu hồi về pool mà không gây kẹt bàn.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Xu kẹt quá thời gian quy định phải được thu hồi an toàn và trả thưởng theo `stuck_payout_multiplier`.
- Forbidden: Xu kẹt tuyệt đối không phát `JackpotHit` và không tạo Bùng Xu.
- Guardrails: Tự động giải phóng slot để không làm giảm thông lượng bàn chơi.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoWatchdogTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-24 [EditMode]**: GIVEN xu có `|v| < C.stuckSpeedThreshold` liên tục `C.stuckTime` (thời gian sim), hoặc xu rung trên ngưỡng đến tuổi `C.coinMaxLifetime`, WHEN tới mốc, THEN xu thành Stuck, `rawValue = C.baseCoinValue * C.stuckPayoutMultiplier`, không phát `JackpotHit`, không có Bùng Xu.
- [ ] **AC-31 [EditMode, Slow]**: GIVEN sim `T.simDropXSamples * T.simCoinsPerDropX` xu, WHEN đo thời gian từ sinh tới Resolved, THEN trung vị ∈ `T.targetMedianFallTime ± T.fallTimeTolerance`, `p95 <= T.fallTimeP95Max`, và validator xác nhận `T.fallTimeP95Max <= C.coinMaxLifetime - T.lifetimeMarginSeconds`.

## Implementation Notes

- Cài đặt `PlinkoStuckWatchdog` kiểm tra trạng thái từng xu đang rơi (Falling):
  - Theo dõi thời gian vận tốc tuyến tính `|linearVelocity| < stuckSpeedThreshold`. Nếu liên tục đạt `stuckTime`, đánh dấu là kẹt.
  - Theo dõi tổng thời gian tồn tại kể từ khi sinh `lifeTime >= coinMaxLifetime`, đánh dấu là hết hạn/kẹt.
- Khi một xu bị đánh dấu là kẹt:
  - Phát sự kiện `CoinStuck { coinId, origin, worldPosition }`.
  - Giải quyết xu với `CoinResolvedEvent(reason = CoinResolveReason.Stuck, multiplier = stuckPayoutMultiplier, bucketIndex = NoBucket)`.
  - Thu hồi xu về trạng thái Resolved (trả về pool ngay trong bước mô phỏng này).

## Out of Scope

- [Story 007]: Cảm biến miệng hũ và tích hợp ví Economy Core.
- [Story 008]: Xử lý trúng Jackpot.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoWatchdogTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 005 must be Complete
- Unlocks: Story 007

