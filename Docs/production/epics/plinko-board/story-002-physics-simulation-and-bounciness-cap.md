# Story 002: Thiết lập vật lý Box2D & trần độ nảy (Physics Simulation & Bounciness Cap)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-001`, `TR-plinko-003`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Mô phỏng vật lý 2D trong World Space camera riêng; độ nảy va chạm được kẹp trần nghiêm ngặt < 1.0; vận tốc tuyến tính sử dụng Rigidbody2D.linearVelocity (Unity 6).
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Sử dụng `Rigidbody2D.linearVelocity` (không dùng thuộc tính `velocity` đã deprecated của Unity 6); kẹp độ nảy hiệu lực của đinh `bounciness_cap < 1.0`.
- Forbidden: Xu không va chạm với nhau (Coin-Coin collision ignore); không cho phép va chạm sinh thêm cơ năng.
- Guardrails: Tốc độ tối đa của xu bị kẹp ở `maxCoinSpeed` tại mỗi bước vật lý.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: Rigidbody2D.linearVelocity là API chuẩn trong Unity 6.

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPhysicsTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-4 [EditMode]**: GIVEN ma trận layer của project, WHEN đọc `Physics2D.GetIgnoreLayerCollision`, THEN Coin-Coin bị bỏ qua; Coin-Peg, Coin-Wall, Coin-Divider va chạm bình thường.
- [ ] **AC-14 [EditMode]**: GIVEN modifier ∈ {0.5, 1, 2, 3, 100}, WHEN tính `effective_bounciness`, THEN kết quả = `min(C.basePegBounciness * max(m, 1), C.bouncinessCap)` (m = 0.5 cho đúng `C.basePegBounciness`) và `PhysicsMaterial2D` của đinh mang đúng giá trị đó; validator báo lỗi khi `C.bouncinessCap >= 1.0`.
- [ ] **AC-15 [EditMode]**: GIVEN modifier nảy là âm, NaN, vô cực, 0 hoặc 0.5, hoặc tốc độ auto là âm, NaN hoặc vô cực, WHEN áp vào bàn, THEN `effective_bounciness = min(C.basePegBounciness, C.bouncinessCap)` và timer auto-drop dừng.
- [ ] **AC-16 [EditMode]**: GIVEN `T.simCoinCountSmall` xu sim ở `C.bouncinessCap`, WHEN mỗi xu sinh ra và rơi, THEN `gravityScale = C.coinGravityScale`, vận tốc ban đầu = 0, và `|v| <= C.maxCoinSpeed + T.speedTolerance` sau mọi bước.
- [ ] **AC-19b [EditMode, Slow]**: GIVEN `T.simCoinsPerDropX` xu thả sát tường (`x_sinh = wall ± C.coinRadius`), WHEN sim xong, THEN tỉ lệ xu kẹt ≤ `T.stuckRateMax` và không xu nào bị kẹt nêm giữa đinh tầng chẵn và tường.

## Implementation Notes

- Cài đặt cấu hình Physics2D layer matrix (hoặc runner thiết lập ma trận layer cô lập cho test).
- Triển khai logic tính toán độ nảy hiệu lực `EffectiveBouncinessCalculator` với quy tắc làm sạch modifier không hợp lệ (NaN, inf, < 1 kẹp lên 1).
- Áp dụng `PhysicsMaterial2D` lên đinh sao cho độ nảy chỉ lấy theo đinh (xu mang độ nảy 0, Combine Mode đảm bảo không nhân tăng năng lượng).
- Kẹp vận tốc của Rigidbody2D bằng `linearVelocity` trong FixedUpdate / sau mỗi bước mô phỏng vật lý `PhysicsScene2D.Simulate`.

## Out of Scope

- [Story 003]: Vùng thả và bộ kiểm soát nhận chạm.
- [Story 005]: Xử lý va chạm đinh và tính điểm debounce.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoPhysicsTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 001 must be Complete
- Unlocks: Story 003

