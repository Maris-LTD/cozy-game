# Story 012: Kiểm chứng thống kê & mô hình cân bằng (Balance Models & Statistical Verification)

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
**ADR Decision Summary**: Kiểm chứng mô hình Galton, bảo toàn cơ năng, tỉ lệ nhắm thưởng (aim ratio) và trần thông lượng theo 4 cấu hình để đảm bảo kinh tế không bị phá vỡ trên WebGL.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Kiểm tra bảo toàn năng lượng cơ học qua mọi va chạm xu-đinh (`energyGainTolerance`); thông lượng thu nhập Plinko tuân thủ trần mềm theo cấu hình.
- Forbidden: Không cho phép xu sinh thêm năng lượng sau va chạm; không cho phép vượt trần thông lượng quy định.
- Guardrails: Các bài test nặng đánh dấu danh mục NUnit `[Category("Slow")]` để chạy định kỳ hoặc khi nghiệm thu.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoBalanceTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`, `PlinkoBalanceTargets.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-13 [EditMode, Slow]**: GIVEN spam ở tần suất lớn nhất của `T.autoclickerRatesPerSecond` trong `T.throughputSimSeconds` sim ở độ nảy gốc, không auto-drop, có jackpot, WHEN tính thông lượng, THEN số Manual nhận mỗi giây ≤ `1 / C.tapCooldown` và tổng `rawValue / giây` (gồm mọi reason, kể cả Overflow) ≤ `T.maxRawPayoutPerSecondPerBaseCoin * C.baseCoinValue`.
- [ ] **AC-13b [EditMode, Slow]**: GIVEN cùng kịch bản spam như AC-13, độ nảy gốc, thêm auto-drop ở sàn, WHEN tính thông lượng, THEN số Auto nhận mỗi giây ≤ `1 / C.autoDropMinInterval`, số Manual nhận mỗi giây ≤ `1 / C.tapCooldown`, và tổng `rawValue / giây` ≤ `T.maxRawPayoutPerSecondPerBaseCoinWithAuto * C.baseCoinValue`.
- [ ] **AC-13c [EditMode, Slow]**: GIVEN cùng kịch bản spam như AC-13, không auto-drop, độ nảy ở trần `C.bouncinessCap`, WHEN tính thông lượng, THEN tổng `rawValue / giây` ≤ `T.maxRawPayoutPerSecondPerBaseCoinAtCap * C.baseCoinValue`, và test in tỉ lệ so với AC-13.
- [ ] **AC-13d [EditMode, Slow]**: GIVEN cùng kịch bản spam như AC-13, có cả auto-drop ở sàn và độ nảy ở trần, WHEN tính thông lượng, THEN tổng `rawValue / giây` ≤ `T.maxRawPayoutPerSecondPerBaseCoinAutoAndCap * C.baseCoinValue`.
- [ ] **AC-17 [EditMode, Slow]**: GIVEN độ nảy hiệu lực = `C.bouncinessCap`, WHEN sim `T.simDropXSamples * T.simCoinsPerDropX` xu, THEN qua mọi va chạm xu-đinh năng lượng cơ học `1/2*v^2 + g*gravityScale*h` không tăng quá `T.energyGainTolerance`; 100% xu Resolved trước `C.coinMaxLifetime`; mọi xu có `scoredPegHits <= C.maxScoredPegHits`.
- [ ] **AC-18 [EditMode, Slow]**: GIVEN cùng sim ở trần nảy, WHEN tổng hợp, THEN `p_jackpot` trung bình ≤ `T.capJackpotProbMeanMax`, `EV_mean <= T.capEvMeanMax`, và `rawValue` mọi xu ≤ `max(C.bucketMultipliers) * C.baseCoinValue`.
- [ ] **AC-30 [EditMode, Slow]**: GIVEN `T.simDropXSamples` điểm x0 rải đều trên vùng thả, mỗi điểm `T.simCoinsPerDropX` xu, seed cố định, độ nảy gốc, WHEN sim xong, THEN test in bảng `p_jackpot`, EV trực tiếp và EV hiệu dụng theo từng x0; assert: `mean p_jackpot ∈ [T.jackpotProbMeanMin, T.jackpotProbMeanMax]`; `max p_jackpot theo x0 <= T.jackpotProbMaxAtAnyX`; `p_jackpot tại tâm >= T.jackpotProbMinAtCenter`; `EV_mean ∈ [T.evMeanMin, T.evMeanMax]`; `max / min EV hiệu dụng theo x0 (aim_ratio) <= T.evRatioMax`; tỉ lệ kẹt ≤ `T.stuckRateMax`.

## Implementation Notes

- Cài đặt harness test EditMode chạy trong scene vật lý cục bộ `LocalPhysicsMode.Physics2D` với bước gọi thủ công `PhysicsScene2D.Simulate(fixedDeltaTime)`.
- Đo lường cơ năng của xu trước và sau từng bước va chạm để đảm bảo tính ổn định của solver Box2D.
- Tự động hóa thống kê phân bố rơi Galton và kiểm tra biên độ `aim_ratio` (chênh lệch EV giữa nhắm mép và nhắm giữa) để bảo vệ Pillar 1 & 3.

## Out of Scope

- [Story 013]: Viewport camera, định tuyến chạm và giao diện.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoBalanceTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 011 must be Complete
- Unlocks: Story 013

