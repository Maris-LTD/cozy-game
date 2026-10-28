# Story 003: Vùng thả & bộ kiểm soát nhận chạm (Drop Zone & Tap Admission Limiter)

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
**ADR Decision Summary**: Vùng thả ở giữa bàn được giới hạn bề rộng kết hợp độ lệch ngang ngẫu nhiên dropJitter; rate limiter kiểm soát nhịp chạm tay tapCooldown và ngăn chặn autoclicker spam.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Kiểm soát nhịp chạm tay `tap_cooldown` và bộ đệm admission chống spam autoclicker.
- Forbidden: Không cho phép người chơi thả xu ngoài vùng quy định hoặc nhắm chính xác hũ; không dời mốc cooldown khi chạm bị bỏ.
- Guardrails: Cú chạm hợp lệ sinh xu với độ trễ ≤ 50 ms.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoAdmissionTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-5 [EditMode]**: GIVEN RNG giả trả −1 / 0 / +1, WHEN tính `drop_x` với `tap_x` ngoài vùng thả, THEN x = mép vùng gần nhất + {−1, 0, +1} × `C.dropJitter * C.pegSpacing`; với `T.simCoinCountLarge` mẫu RNG thật, mọi x ∈ `[wall_left + C.coinRadius, wall_right - C.coinRadius]`.
- [ ] **AC-6 [EditMode]**: GIVEN chạm có y bất kỳ, hoặc nguồn Auto/Rain/JackpotBurst, WHEN sinh `T.simCoinCountLarge` xu, THEN y = vạch thả và x trước jitter ∈ `[zone_min, zone_max]`; với nguồn không có điểm chạm, chia vùng thả thành `T.dropUniformityBins` ô bằng nhau, mỗi ô nhận `1 / T.dropUniformityBins ± T.dropUniformityTolerance` (1.5%) số xu.
- [ ] **AC-7 [EditMode]**: GIVEN bốn đường sinh xu, WHEN mỗi xu được giải quyết, THEN `CoinResolved.origin` đúng nguồn (`Manual` / `Auto` / `Rain` / `JackpotBurst`).
- [ ] **AC-8 [EditMode]**: GIVEN đồng hồ inject, WHEN chạm ở t = 0, t = `C.tapCooldown - ε`, t = `C.tapCooldown`, THEN kết quả lần lượt nhận / bỏ / nhận, và cú bị bỏ không dời mốc cooldown.
- [ ] **AC-12 [EditMode]**: GIVEN autoclicker ở từng tần suất trong `T.autoclickerRatesPerSecond` ([30, 60, 240] chạm/giây; có nhiều chạm trong cùng khung), WHEN sim `T.autoclickerSimSeconds` (10 s), THEN số xu Manual được nhận ≤ `floor(T.autoclickerSimSeconds / C.tapCooldown) + 1` và số xu hoạt động ≤ `C.maxActiveCoins` ở mọi bước.
- [ ] **AC-44 [EditMode]**: GIVEN `acceptManualTaps = false`, hàng đợi Rain không rỗng và một nguồn auto-drop, WHEN gửi `WorldPress` hợp lệ đã qua cooldown rồi bật lại cờ và gửi tiếp sau `C.tapCooldown`, THEN lần đầu 0 xu Manual, 0 `BoardBusy`, mốc cooldown không đổi, xu Rain và Auto vẫn sinh; lần sau 1 xu Manual được nhận.

## Implementation Notes

- Cài đặt `DropZoneCalculator` chịu trách nhiệm kẹp x vào `[zone_min, zone_max]` và cộng độ lệch đều `±dropJitter * pegSpacing`, đảm bảo kẹp an toàn cách tường tối thiểu `coinRadius`.
- Cài đặt `PlinkoAdmissionController` xử lý kiểm tra `tapCooldown`, số lượng xu hoạt động và cờ `acceptManualTaps`.
- Thiết kế hỗ trợ đồng hồ giả lập `IClock` (injected clock) để test EditMode kiểm tra chính xác các mốc thời gian cooldown mà không dùng thời gian thực `Time.time`.

## Out of Scope

- [Story 004]: Hàng đợi loạt (Burst Queue) và cơ chế ưu tiên slot cho chạm tay.
- [Story 005]: Xử lý va chạm đinh.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoAdmissionTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 002 must be Complete
- Unlocks: Story 004

