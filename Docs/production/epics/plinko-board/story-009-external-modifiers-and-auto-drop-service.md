# Story 009: Dịch vụ đăng ký nguồn ngoài & Auto-Drop (External Modifiers & Auto-Drop Service)

> **Epic**: Plinko Board
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: 2026-10-27
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/plinko-board.md`
**Requirement**: `TR-plinko-002`, `TR-plinko-003`

**ADR Governing Implementation**: ADR-0006: World Space 2D Physics Plinko Board vs Canvas UI Separation
**ADR Decision Summary**: Plinko sở hữu luật cộng dồn: tính tổng tốc độ auto-drop và tích các hệ số nảy từ các nguồn đã đăng ký; tự động kẹp biên an toàn và quản lý bộ hẹn giờ thả tự động.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Bảng nguồn được cấp phát sẵn, không cấp phát bộ nhớ khi đăng ký hoặc cập nhật; làm sạch modifier đầu vào theo quy tắc R5.3.
- Forbidden: Không cho phép hệ bên ngoài tự ý tính toán cộng dồn trước khi gửi; không cho phép hệ số nảy làm đinh nảy yếu hơn độ nảy gốc.
- Guardrails: Tốc độ auto-drop có sàn `auto_drop_min_interval`; độ nảy hiệu lực có trần `bounciness_cap < 1.0`.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Core/Plinko/` (CozyGachapon.Core)
- Test: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoModifierTests.cs` (CozyGachapon.Tests.EditMode)
- Data: `PlinkoConfig.cs`

## Acceptance Criteria

*From `Docs/design/gdd/plinko-board.md`, scoped to this story:*

- [ ] **AC-27 [EditMode]**: GIVEN `auto_rate_multiplier` = 0 / 1 / 2 / 100, WHEN tính chu kỳ, THEN lần lượt timer dừng / `C.autoDropBaseInterval` / `C.autoDropBaseInterval ÷ 2` / `C.autoDropMinInterval`; gỡ nguồn cuối khi đang chờ nhịp → không sinh thêm xu Auto, xu đang rơi vẫn được giải quyết.
- [ ] **AC-43 [EditMode]**: GIVEN bàn không có nguồn nào, WHEN lần lượt `RegisterAutoDropRate("a", 1)`, `RegisterAutoDropRate("b", 1.5)`, `RegisterAutoDropRate("a", 0.5)`, `RegisterBouncinessFactor("c", 1.7)`, `RegisterBouncinessFactor("d", 1.2)`, `RegisterBouncinessFactor("e", NaN)`, `Unregister("b")`, `Unregister("khong-co")`, THEN sau 3 lệnh đầu `auto_rate_multiplier` lần lượt = 1, 2.5, 2.0, và sau `Unregister("b")` = 0.5; `bounciness_modifier` cuối = `1.7 * 1.2 * 1 = 2.04` và `effective_bounciness = min(C.basePegBounciness * 2.04, C.bouncinessCap)`; chu kỳ auto-drop = `max(C.autoDropBaseInterval / 0.5, C.autoDropMinInterval)`; `Unregister` nguồn chưa đăng ký không đổi gì và không phát sinh ngoại lệ.

## Implementation Notes

- Cài đặt `PlinkoModifierRegistry`:
  - Quản lý 2 từ điển/danh sách nguồn với key là `string sourceId`: `autoDropRates` và `bouncinessFactors`.
  - Cài đặt API:
    - `RegisterAutoDropRate(string sourceId, float rate)`: làm sạch giá trị (nếu âm, NaN, vô cực thì coi bằng 0). Cập nhật tổng `auto_rate_multiplier = sum(rates)`.
    - `RegisterBouncinessFactor(string sourceId, float factor)`: làm sạch giá trị (nếu âm, NaN, vô cực hoặc < 1.0 thì coi bằng 1.0). Cập nhật tích `bounciness_modifier = product(factors)`.
    - `Unregister(string sourceId)`: gỡ bỏ `sourceId` khỏi cả 2 danh sách và tính lại hệ số.
- Cài đặt `PlinkoAutoDropTimer`:
  - Khi `auto_rate_multiplier > 0`: chu kỳ = `max(autoDropBaseInterval / auto_rate_multiplier, autoDropMinInterval)`.
  - Khi `auto_rate_multiplier == 0`: dừng bộ đếm timer ngay lập tức.

## Out of Scope

- [Story 010]: Phát đầy đủ sự kiện và hoàn trả đối tượng pool.
- [Story 011]: Vòng đời mô phỏng và tích hợp Bootstrap.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Plinko/PlinkoModifierTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 008 must be Complete
- Unlocks: Story 010

