# Story 008: Jackpot & chuỗi Bùng Xu không lặp (Jackpot & Chain-Free Burst Cascades)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-003`, `TR-plinko-004`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Khi xu rơi vào hai hũ mép ngoài cùng (hũ jackpot), hệ thống phát sự kiện JackpotHit và xếp hàng jackpotBurstCount xu JackpotBurst; các xu JackpotBurst khi trúng jackpot vẫn nhận thưởng x5 nhưng tuyệt đối không kích hoạt Bùng Xu tiếp.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Nhận diện chính xác 2 hũ ngoài cùng là hũ Jackpot (`bucket_is_jackpot`); phát `JackpotHit` trước `CoinResolved`.
- Forbidden: Tuyệt đối không cho phép tạo phản ứng dây chuyền vô hạn (chain reaction) từ các xu mang origin `JackpotBurst`.
- Guardrails: Số lượng xu Bùng Xu được giới hạn theo `jackpot_burst_count` và tuân thủ cơ chế admission của hàng đợi loạt.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoJackpotTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-22 [EditMode]**: GIVEN xu origin Manual, Auto hoặc Rain, WHEN vào hũ jackpot, THEN phát đúng 1 `JackpotHit` và hàng đợi loạt tăng đúng `C.jackpotBurstCount`.
- [ ] **AC-23 [EditMode]**: GIVEN stub "mọi hũ đều là jackpot", WHEN thả 1 xu Manual và sim tới khi bàn Idle, THEN tổng số xu JackpotBurst = `C.jackpotBurstCount`; mỗi xu JackpotBurst có `rawValue = C.baseCoinValue * hệ số hũ` và phát `JackpotHit` nhưng làm hàng đợi tăng 0 (không dây chuyền).

## Implementation Notes

- Cài đặt logic kiểm tra `isJackpot`: `bucketIndex == 0 || bucketIndex == bucketMultipliers.Length - 1`.
- Khi xu chạm cảm biến hũ jackpot:
  - Phát `JackpotHitEvent { coinId, origin, bucketIndex, worldPosition }`.
  - Nếu `origin != CoinOrigin.JackpotBurst`: gọi `EnqueueBurst(jackpotBurstCount, CoinOrigin.JackpotBurst)` đưa vào hàng đợi loạt FIFO.
  - Nếu `origin == CoinOrigin.JackpotBurst`: vẫn nhận multiplier và phát `JackpotHitEvent`, nhưng bỏ qua việc đưa xu vào hàng đợi.

## Out of Scope

- [Story 009]: Đăng ký nguồn modifier bên ngoài và Auto-Drop.
- [Story 010]: Phát đầy đủ sự kiện và hoàn trả đối tượng pool.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoJackpotTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 007 must be Complete
- Unlocks: Story 009

