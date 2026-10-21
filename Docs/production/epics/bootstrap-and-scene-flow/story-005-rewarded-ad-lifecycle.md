# Story 005: Rewarded Ad Lifecycle & AdClosed Integration

> **Epic**: Bootstrap & Scene Flow
> **Status**: Ready
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: 1-2 days
> **Last Updated**: [set by game-dev when implementation begins]
> **Manifest Version**: 2026-10-21

## Context

**Design source**: `Docs/design/gdd/bootstrap-and-scene-flow.md`
**Requirement**: `TR-boot-002`

**ADR Governing Implementation**: ADR-0002: Bootstrap Composition Root & Phased Lifecycle
**ADR Decision Summary**: Khi quảng cáo có thưởng hiển thị, cờ `IsAdOpen` ngăn chặn Bootstrap phát `Paused`, giữ cho mô phỏng nền tiếp tục chạy, và phát sự kiện `AdClosed` kèm thời lượng khi đóng ad.
**ADR Version**: 2026-10-21

**Control Manifest Rules (this layer)**:
- Required: Đảm bảo tính nhất quán của vòng đời mô phỏng khi tích hợp với SDK quảng cáo.
- Forbidden: Không phát `Paused`/`Resumed` trong lúc quảng cáo đang mở.
- Guardrails: Cung cấp `adSeconds` chuẩn xác để tầng kinh tế tính toán bù trừ thu nhập.

**Engine**: Unity 6000.3.16f1 | **Risk**: LOW
**Engine Notes**: none

**Unity hints**:
- Code: `Assets/CozyGachapon/Scripts/Foundation/Bootstrap/` (`CozyGachapon.Foundation`)
- Test: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/AdLifecycleIntegrationTests.cs` (`CozyGachapon.Tests.EditMode`)
- Data: `SceneFlowConfig.asset`

## Acceptance Criteria

*From Docs/design/gdd/bootstrap-and-scene-flow.md, scoped to this story:*

- [ ] `AC-42 [EditMode]`: Khi vòng đời đang `Running`, nếu SDK nền tảng gọi `SetAdOpen(true)`, cờ `IsAdOpen` chuyển sang `true`; trong suốt khoảng thời gian này, các sự kiện ẩn/hiện trang web hoặc ứng dụng xuống nền/trở lại đều không phát `Paused` hay `Resumed`; vòng đời vẫn giữ `Running` và `IsSimulationRunning` vẫn là `true` cho tới khi gọi `SetAdOpen(false)`. Nếu lúc đóng ad mà trang web vẫn đang bị ẩn, hệ thống kích hoạt ngay `Paused {reason = Hidden}`.
- [ ] `AC-43 [EditMode]`: Khi `SetAdOpen(false)` được gọi, hệ thống phát đúng 1 sự kiện `AdClosed {adSeconds}`, trong đó `adSeconds` là khoảng thời gian thực UTC giữa hai lần mở và đóng ad (kẹp ≥ 0, nếu đồng hồ lùi thì bằng 0). Nếu trang đang ẩn tại thời điểm đóng ad, sự kiện `AdClosed` được phát trước sự kiện `Paused` trong cùng khung hình.

## Implementation Notes

- Cài đặt hai phương thức `SetAdOpen(bool isOpen)` và thuộc tính `IsAdOpen` trên controller vòng đời.
- Lưu dấu mốc thời gian `adOpenedUtc` khi ad bắt đầu mở để tính toán chính xác `adSeconds` khi ad đóng.
- Tích hợp điều kiện kiểm tra `IsAdOpen` trong các bộ lắng nghe sự kiện ẩn/hiện trang.

## Out of Scope

*Handled by neighbouring stories, do not implement here:*

- [Story 004]: Bộ theo dõi vòng đời ẩn/hiện thông thường.
- [Feature/Ads]: Logic gọi SDK quảng cáo và hiển thị phần thưởng.

## QA Test Cases

*N/A — no qa-lead specs at this review mode; implement against the Acceptance Criteria above*

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/CozyGachapon/Tests/EditMode/Bootstrap/AdLifecycleIntegrationTests.cs`, passing in `unity-harness tests`

**Evidence**: [ ] Not yet created

## Dependencies

- Depends on: Story 004
- Unlocks: Story 006
